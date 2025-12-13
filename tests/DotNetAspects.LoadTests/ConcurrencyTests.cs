using System.Collections.Concurrent;
using System.Diagnostics;
using DotNetAspects.LoadTests.Services;

namespace DotNetAspects.LoadTests;

/// <summary>
/// Concurrency stress tests for DotNetAspects.
/// Tests thread safety and performance under concurrent load.
/// </summary>
public class ConcurrencyTests
{
    /// <summary>
    /// Tests that aspects work correctly with many concurrent threads.
    /// </summary>
    [Theory]
    [InlineData(10, 1000)]   // 10 threads, 1000 ops each = 10,000 total
    [InlineData(50, 1000)]   // 50 threads, 1000 ops each = 50,000 total
    [InlineData(100, 500)]   // 100 threads, 500 ops each = 50,000 total
    public async Task ConcurrentMethodInterception_ShouldMaintainCorrectness(int threadCount, int operationsPerThread)
    {
        // Arrange
        var service = new LightProcessingService();
        LightProcessingAspect.ResetCallCount();
        var results = new ConcurrentBag<int>();
        var exceptions = new ConcurrentBag<Exception>();
        var expectedTotal = threadCount * operationsPerThread;
        var callsBefore = LightProcessingAspect.CallCount;

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(threadId =>
            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var result = service.Add(threadId, i);
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(expectedTotal, results.Count);
        // Check that at least expectedTotal calls were made (may be more due to other tests running)
        var callsDelta = LightProcessingAspect.CallCount - callsBefore;
        Assert.True(callsDelta >= expectedTotal,
            $"Expected at least {expectedTotal} aspect calls, but only {callsDelta} were made");
    }

    /// <summary>
    /// Tests banking transaction service under concurrent load.
    /// </summary>
    [Theory]
    [InlineData(20, 500)]   // 20 threads, 500 transactions each = 10,000 transactions
    [InlineData(50, 200)]   // 50 threads, 200 transactions each = 10,000 transactions
    public async Task ConcurrentBankingTransactions_ShouldProcessAllTransactions(int threadCount, int transactionsPerThread)
    {
        // Arrange
        var service = new BankingTransactionService();
        LightProcessingAspect.ResetCallCount();
        var transactions = new ConcurrentBag<TransactionResult>();
        var exceptions = new ConcurrentBag<Exception>();
        var expectedTotal = threadCount * transactionsPerThread;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(threadId =>
            Task.Run(() =>
            {
                try
                {
                    var accountId = $"ACC{threadId:D8}";
                    for (int i = 0; i < transactionsPerThread; i++)
                    {
                        var result = service.ProcessTransaction(accountId, 100.00m + i, "USD");
                        transactions.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(expectedTotal, transactions.Count);

        // Verify all transaction IDs are unique
        var uniqueIds = transactions.Select(t => t.TransactionId).Distinct().Count();
        Assert.Equal(expectedTotal, uniqueIds);

        // Log performance metrics
        var tps = expectedTotal / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Processed {expectedTotal} transactions in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {tps:F2} TPS");
        Console.WriteLine($"Avg latency: {stopwatch.Elapsed.TotalMilliseconds / expectedTotal:F4}ms per transaction");
    }

    /// <summary>
    /// Tests mixed read/write operations concurrently.
    /// </summary>
    [Fact]
    public async Task ConcurrentMixedOperations_ShouldHandleAllOperations()
    {
        // Arrange
        var service = new BankingTransactionService();
        LightProcessingAspect.ResetCallCount();
        var operationCount = 0;
        var exceptions = new ConcurrentBag<Exception>();
        var stopwatch = Stopwatch.StartNew();

        // Act - simulate real banking workload
        var tasks = new List<Task>();

        // Transaction processors (write-heavy)
        for (int i = 0; i < 10; i++)
        {
            var accountBase = i * 100;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 200; j++)
                    {
                        var accountId = $"ACC{accountBase + (j % 10):D8}";
                        service.ProcessTransaction(accountId, 100.00m + j, "USD");
                        Interlocked.Increment(ref operationCount);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        // Balance checkers (read-heavy)
        for (int i = 0; i < 20; i++)
        {
            var accountBase = i * 50;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var accountId = $"ACC{accountBase + (j % 10):D8}";
                        service.GetBalance(accountId);
                        Interlocked.Increment(ref operationCount);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        // Validators (mixed)
        for (int i = 0; i < 10; i++)
        {
            var accountBase = i * 100;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 150; j++)
                    {
                        var accountId = $"ACC{accountBase + (j % 10):D8}";
                        service.ValidateTransaction(accountId, 100.00m + j);
                        Interlocked.Increment(ref operationCount);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Empty(exceptions);
        var expectedOps = (10 * 200) + (20 * 100) + (10 * 150); // 5500 operations
        Assert.Equal(expectedOps, operationCount);

        // Log performance
        var opsPerSecond = operationCount / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Completed {operationCount} mixed operations in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {opsPerSecond:F2} ops/sec");
    }

    /// <summary>
    /// Tests rapid creation of services (simulates request handling in web API).
    /// </summary>
    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task RapidServiceCreation_ShouldHandleHighRate(int requestCount)
    {
        // Arrange
        var callsBefore = LightProcessingAspect.CallCount;
        var results = new ConcurrentBag<int>();
        var exceptions = new ConcurrentBag<Exception>();
        var stopwatch = Stopwatch.StartNew();

        // Act - simulate creating a new service per request (like DI in web API)
        var tasks = Enumerable.Range(0, requestCount).Select(i =>
            Task.Run(() =>
            {
                try
                {
                    // Create new service instance (simulates DI container behavior)
                    var service = new LightProcessingService();
                    var result = service.Add(i, i);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(requestCount, results.Count);
        var callsDelta = LightProcessingAspect.CallCount - callsBefore;
        Assert.True(callsDelta >= requestCount,
            $"Expected at least {requestCount} aspect calls, but only {callsDelta} were made");

        var rps = requestCount / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Handled {requestCount} requests in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {rps:F2} requests/sec");
    }

    /// <summary>
    /// Tests that parameterized aspects work correctly under concurrent load.
    /// </summary>
    [Fact]
    public async Task ConcurrentParameterizedAspects_ShouldMaintainConfiguration()
    {
        // Arrange
        var service = new ParameterizedAspectService();
        var results = new ConcurrentBag<int>();
        var exceptions = new ConcurrentBag<Exception>();
        var threadCount = 50;
        var opsPerThread = 100;

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(threadId =>
            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < opsPerThread; i++)
                    {
                        var result = service.Add(threadId, i);
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(threadCount * opsPerThread, results.Count);
    }
}
