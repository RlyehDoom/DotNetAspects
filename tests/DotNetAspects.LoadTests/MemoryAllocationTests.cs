using System.Diagnostics;
using DotNetAspects.LoadTests.Services;

namespace DotNetAspects.LoadTests;

/// <summary>
/// Tests for measuring memory allocation and GC pressure.
/// These tests help identify memory-related performance issues.
/// </summary>
public class MemoryAllocationTests
{
    /// <summary>
    /// Measures memory allocation over many operations.
    /// Lower allocation indicates better performance for high-throughput scenarios.
    /// </summary>
    [Fact]
    public void MeasureAllocation_MinimalAspect_ManyOperations()
    {
        // Arrange
        var service = new MinimalAspectService();
        var operationCount = 100_000;

        // Warm up
        for (int i = 0; i < 1000; i++)
        {
            service.Add(i, i);
        }

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(true);
        var initialCollections0 = GC.CollectionCount(0);
        var initialCollections1 = GC.CollectionCount(1);
        var initialCollections2 = GC.CollectionCount(2);

        // Act
        for (int i = 0; i < operationCount; i++)
        {
            service.Add(i, i);
        }

        var finalMemory = GC.GetTotalMemory(false);
        var finalCollections0 = GC.CollectionCount(0);
        var finalCollections1 = GC.CollectionCount(1);
        var finalCollections2 = GC.CollectionCount(2);

        // Report
        var memoryDelta = finalMemory - initialMemory;
        var gen0Collections = finalCollections0 - initialCollections0;
        var gen1Collections = finalCollections1 - initialCollections1;
        var gen2Collections = finalCollections2 - initialCollections2;

        Console.WriteLine($"=== Memory Allocation Report ===");
        Console.WriteLine($"Operations: {operationCount:N0}");
        Console.WriteLine($"Memory delta: {memoryDelta:N0} bytes");
        Console.WriteLine($"Avg allocation per op: {(double)memoryDelta / operationCount:F2} bytes");
        Console.WriteLine($"Gen 0 collections: {gen0Collections}");
        Console.WriteLine($"Gen 1 collections: {gen1Collections}");
        Console.WriteLine($"Gen 2 collections: {gen2Collections}");

        // Assert - no Gen2 collections is ideal for high-throughput
        // Gen0 collections are expected and acceptable
        Assert.True(gen2Collections <= 1, $"Too many Gen2 collections: {gen2Collections}");
    }

    /// <summary>
    /// Compares allocation between baseline and aspect-enabled services.
    /// </summary>
    [Fact]
    public void CompareAllocation_BaselineVsAspect()
    {
        // Arrange
        var baselineService = new BaselineService();
        var aspectService = new MinimalAspectService();
        var operationCount = 50_000;

        // Measure baseline
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baselineStartMem = GC.GetTotalMemory(true);

        for (int i = 0; i < operationCount; i++)
        {
            baselineService.Add(i, i);
        }

        var baselineEndMem = GC.GetTotalMemory(false);
        var baselineAllocation = baselineEndMem - baselineStartMem;

        // Measure aspect-enabled
        GC.Collect(2, GCCollectionMode.Forced, true);
        var aspectStartMem = GC.GetTotalMemory(true);

        for (int i = 0; i < operationCount; i++)
        {
            aspectService.Add(i, i);
        }

        var aspectEndMem = GC.GetTotalMemory(false);
        var aspectAllocation = aspectEndMem - aspectStartMem;

        // Report
        var overhead = aspectAllocation - baselineAllocation;
        var overheadPerOp = (double)overhead / operationCount;

        Console.WriteLine($"=== Allocation Comparison ===");
        Console.WriteLine($"Operations: {operationCount:N0}");
        Console.WriteLine($"Baseline allocation: {baselineAllocation:N0} bytes");
        Console.WriteLine($"Aspect allocation: {aspectAllocation:N0} bytes");
        Console.WriteLine($"Overhead: {overhead:N0} bytes total");
        Console.WriteLine($"Overhead per operation: {overheadPerOp:F2} bytes");

        // Assert - aspect overhead should be reasonable (< 200 bytes per op)
        Assert.True(overheadPerOp < 200, $"Allocation overhead too high: {overheadPerOp:F2} bytes/op");
    }

    /// <summary>
    /// Tests allocation in a sustained high-throughput scenario.
    /// </summary>
    [Fact]
    public void SustainedThroughput_ShouldNotCauseMemoryPressure()
    {
        // Arrange
        var service = new BankingTransactionService();
        var batchSize = 10_000;
        var batchCount = 10;
        var totalOperations = batchSize * batchCount;

        // Warm up
        for (int i = 0; i < 1000; i++)
        {
            service.ProcessTransaction($"ACC{i:D8}", 100.00m, "USD");
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        var initialMemory = GC.GetTotalMemory(true);
        var gen2Before = GC.CollectionCount(2);
        var stopwatch = Stopwatch.StartNew();

        // Act - process in batches to simulate sustained load
        for (int batch = 0; batch < batchCount; batch++)
        {
            for (int i = 0; i < batchSize; i++)
            {
                service.ProcessTransaction($"ACC{i % 100:D8}", 100.00m + i, "USD");
            }

            // Check memory after each batch
            var currentMemory = GC.GetTotalMemory(false);
            var memoryGrowth = currentMemory - initialMemory;

            // Memory should not grow unbounded
            Assert.True(memoryGrowth < 100_000_000, // 100MB limit
                $"Memory growing unbounded after batch {batch + 1}: {memoryGrowth / 1024 / 1024}MB");
        }

        stopwatch.Stop();
        var gen2After = GC.CollectionCount(2);
        var finalMemory = GC.GetTotalMemory(false);

        // Report
        Console.WriteLine($"=== Sustained Throughput Report ===");
        Console.WriteLine($"Total operations: {totalOperations:N0}");
        Console.WriteLine($"Duration: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {totalOperations / stopwatch.Elapsed.TotalSeconds:F2} ops/sec");
        Console.WriteLine($"Memory delta: {(finalMemory - initialMemory) / 1024.0:F2}KB");
        Console.WriteLine($"Gen2 collections: {gen2After - gen2Before}");

        // Assert - minimal Gen2 collections indicates good memory management
        Assert.True(gen2After - gen2Before <= 2,
            $"Too many Gen2 collections during sustained load: {gen2After - gen2Before}");
    }

    /// <summary>
    /// Tests for memory leaks during rapid service creation.
    /// </summary>
    [Fact]
    public void RapidServiceCreation_ShouldNotLeakMemory()
    {
        // Arrange
        var creationCount = 10_000;

        // Force initial GC
        GC.Collect(2, GCCollectionMode.Forced, true);
        var initialMemory = GC.GetTotalMemory(true);

        // Act - rapidly create and use services
        for (int i = 0; i < creationCount; i++)
        {
            var service = new LightProcessingService();
            service.Add(i, i);
            // Service goes out of scope here
        }

        // Force GC to collect unused services
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryDelta = finalMemory - initialMemory;

        // Report
        Console.WriteLine($"=== Service Creation Memory Report ===");
        Console.WriteLine($"Services created: {creationCount:N0}");
        Console.WriteLine($"Initial memory: {initialMemory / 1024.0:F2}KB");
        Console.WriteLine($"Final memory: {finalMemory / 1024.0:F2}KB");
        Console.WriteLine($"Memory delta: {memoryDelta / 1024.0:F2}KB");

        // Assert - no significant memory leak after GC
        // Allow up to 1MB growth for runtime overhead
        Assert.True(memoryDelta < 1_000_000,
            $"Possible memory leak detected: {memoryDelta / 1024.0:F2}KB retained");
    }
}
