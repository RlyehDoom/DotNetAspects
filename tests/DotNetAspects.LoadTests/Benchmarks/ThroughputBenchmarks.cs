using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetAspects.LoadTests.Services;

namespace DotNetAspects.LoadTests.Benchmarks;

/// <summary>
/// Benchmarks to measure throughput for banking-like transaction scenarios.
/// Simulates thousands of operations to measure sustained performance.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class ThroughputBenchmarks
{
    private BankingTransactionService _bankingService = null!;
    private BaselineService _baselineService = null!;
    private readonly string[] _accountIds = Enumerable.Range(1, 100)
        .Select(i => $"ACC{i:D8}")
        .ToArray();

    [Params(100, 1000, 10000)]
    public int TransactionCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bankingService = new BankingTransactionService();
        _baselineService = new BaselineService();
        LightProcessingAspect.ResetCallCount();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Console.WriteLine($"Total aspect calls: {LightProcessingAspect.CallCount}");
    }

    /// <summary>
    /// Simulates processing many transactions with aspects
    /// </summary>
    [Benchmark(Description = "Banking Transactions (with aspects)")]
    public void ProcessTransactions_WithAspects()
    {
        for (int i = 0; i < TransactionCount; i++)
        {
            var accountId = _accountIds[i % _accountIds.Length];
            _bankingService.ProcessTransaction(accountId, 100.00m + i, "USD");
        }
    }

    /// <summary>
    /// Simulates getting balances with aspects
    /// </summary>
    [Benchmark(Description = "Balance Queries (with aspects)")]
    public void GetBalances_WithAspects()
    {
        for (int i = 0; i < TransactionCount; i++)
        {
            var accountId = _accountIds[i % _accountIds.Length];
            _bankingService.GetBalance(accountId);
        }
    }

    /// <summary>
    /// Simulates validation checks with aspects
    /// </summary>
    [Benchmark(Description = "Validation Checks (with aspects)")]
    public void ValidateTransactions_WithAspects()
    {
        for (int i = 0; i < TransactionCount; i++)
        {
            var accountId = _accountIds[i % _accountIds.Length];
            _bankingService.ValidateTransaction(accountId, 100.00m + i);
        }
    }

    /// <summary>
    /// Mixed operations (realistic banking scenario)
    /// </summary>
    [Benchmark(Description = "Mixed Banking Operations")]
    public void MixedOperations_WithAspects()
    {
        for (int i = 0; i < TransactionCount; i++)
        {
            var accountId = _accountIds[i % _accountIds.Length];
            var amount = 100.00m + i;

            // Typical flow: validate -> get balance -> process
            _bankingService.ValidateTransaction(accountId, amount);
            _bankingService.GetBalance(accountId);
            _bankingService.ProcessTransaction(accountId, amount, "USD");
        }
    }

    /// <summary>
    /// Baseline without aspects for comparison
    /// </summary>
    [Benchmark(Baseline = true, Description = "Baseline Calculations (no aspects)")]
    public void BaselineCalculations()
    {
        for (int i = 0; i < TransactionCount; i++)
        {
            _baselineService.Calculate(100.00m + i, 5, 0.07m);
        }
    }
}
