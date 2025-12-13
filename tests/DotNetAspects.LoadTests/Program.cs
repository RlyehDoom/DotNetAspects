using BenchmarkDotNet.Running;
using DotNetAspects.LoadTests.Benchmarks;

namespace DotNetAspects.LoadTests;

/// <summary>
/// Entry point for running benchmarks.
/// Usage:
///   dotnet run -c Release                     # Run all benchmarks
///   dotnet run -c Release -- --filter *Overhead*  # Run specific benchmarks
///   dotnet run -c Release -- --list flat      # List all benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("DotNetAspects Performance Benchmarks v1.4.0");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Select benchmark to run:");
            Console.WriteLine("  1. Aspect Overhead Benchmarks");
            Console.WriteLine("  2. Throughput Benchmarks");
            Console.WriteLine("  3. All Benchmarks");
            Console.WriteLine("  4. Quick smoke test (no benchmark)");
            Console.WriteLine();
            Console.Write("Enter choice (1-4): ");

            var choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    BenchmarkRunner.Run<AspectOverheadBenchmarks>();
                    break;
                case "2":
                    BenchmarkRunner.Run<ThroughputBenchmarks>();
                    break;
                case "3":
                    BenchmarkRunner.Run<AspectOverheadBenchmarks>();
                    BenchmarkRunner.Run<ThroughputBenchmarks>();
                    break;
                case "4":
                    RunQuickSmokeTest();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Running all benchmarks...");
                    BenchmarkRunner.Run<AspectOverheadBenchmarks>();
                    BenchmarkRunner.Run<ThroughputBenchmarks>();
                    break;
            }
        }
        else
        {
            // Use BenchmarkSwitcher for command-line arguments
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }

    /// <summary>
    /// Quick smoke test to verify everything works without full benchmark run.
    /// </summary>
    private static void RunQuickSmokeTest()
    {
        Console.WriteLine("Running quick smoke test...");
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Test services
        var baseline = new Services.BaselineService();
        var minimal = new Services.MinimalAspectService();
        var light = new Services.LightProcessingService();
        var parameterized = new Services.ParameterizedAspectService();
        var banking = new Services.BankingTransactionService();

        const int iterations = 10_000;

        // Baseline
        for (int i = 0; i < iterations; i++)
        {
            baseline.Add(i, i);
        }
        Console.WriteLine($"Baseline Add: {iterations} iterations OK");

        // Minimal aspect
        for (int i = 0; i < iterations; i++)
        {
            minimal.Add(i, i);
        }
        Console.WriteLine($"Minimal aspect Add: {iterations} iterations OK");

        // Light processing aspect
        Services.LightProcessingAspect.ResetCallCount();
        for (int i = 0; i < iterations; i++)
        {
            light.Add(i, i);
        }
        Console.WriteLine($"Light processing Add: {iterations} iterations OK (calls: {Services.LightProcessingAspect.CallCount})");

        // Parameterized aspect
        for (int i = 0; i < iterations; i++)
        {
            parameterized.Add(i, i);
        }
        Console.WriteLine($"Parameterized aspect Add: {iterations} iterations OK");

        // Banking service
        Services.LightProcessingAspect.ResetCallCount();
        for (int i = 0; i < 1000; i++)
        {
            banking.ProcessTransaction($"ACC{i:D8}", 100.00m, "USD");
            banking.GetBalance($"ACC{i:D8}");
            banking.ValidateTransaction($"ACC{i:D8}", 100.00m);
        }
        Console.WriteLine($"Banking operations: 3000 operations OK (aspect calls: {Services.LightProcessingAspect.CallCount})");

        stopwatch.Stop();
        Console.WriteLine();
        Console.WriteLine($"Smoke test completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("All tests passed!");
    }
}
