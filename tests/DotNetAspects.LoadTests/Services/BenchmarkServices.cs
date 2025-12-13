using DotNetAspects.Args;
using DotNetAspects.Interception;
using DotNetAspects.Serialization;

namespace DotNetAspects.LoadTests.Services;

/// <summary>
/// A minimal aspect for benchmarking - just calls Proceed()
/// </summary>
[PSerializable]
public class MinimalAspect : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        args.Proceed();
    }
}

/// <summary>
/// An aspect that simulates light processing work
/// </summary>
[PSerializable]
public class LightProcessingAspect : MethodInterceptionAspect
{
    private static long _callCount;

    public static long CallCount => Interlocked.Read(ref _callCount);
    public static void ResetCallCount() => Interlocked.Exchange(ref _callCount, 0);

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        Interlocked.Increment(ref _callCount);
        args.Proceed();
    }
}

/// <summary>
/// Aspect with constructor parameter (like ConfigurationAccessor)
/// </summary>
[PSerializable]
public class ParameterizedAspect : MethodInterceptionAspect
{
    public string ConfigKey { get; }

    public ParameterizedAspect(string configKey)
    {
        ConfigKey = configKey;
    }

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        // Simulate config lookup (just validate the key is set)
        if (string.IsNullOrEmpty(ConfigKey))
            throw new InvalidOperationException("ConfigKey not set");

        args.Proceed();
    }
}

/// <summary>
/// Service without any aspects - baseline for comparison
/// </summary>
public class BaselineService
{
    public int Add(int a, int b) => a + b;

    public string Concat(string a, string b) => a + b;

    public void DoNothing() { }

    public decimal Calculate(decimal price, decimal quantity, decimal taxRate)
    {
        return price * quantity * (1 + taxRate);
    }
}

/// <summary>
/// Service with minimal aspect - for measuring aspect overhead
/// </summary>
public class MinimalAspectService
{
    [MinimalAspect]
    public int Add(int a, int b) => a + b;

    [MinimalAspect]
    public string Concat(string a, string b) => a + b;

    [MinimalAspect]
    public void DoNothing() { }

    [MinimalAspect]
    public decimal Calculate(decimal price, decimal quantity, decimal taxRate)
    {
        return price * quantity * (1 + taxRate);
    }
}

/// <summary>
/// Service with light processing aspect - simulates realistic banking aspect
/// </summary>
public class LightProcessingService
{
    [LightProcessingAspect]
    public int Add(int a, int b) => a + b;

    [LightProcessingAspect]
    public string Concat(string a, string b) => a + b;

    [LightProcessingAspect]
    public void DoNothing() { }

    [LightProcessingAspect]
    public decimal Calculate(decimal price, decimal quantity, decimal taxRate)
    {
        return price * quantity * (1 + taxRate);
    }
}

/// <summary>
/// Service with parameterized aspect - like ConfigurationAccessor in ICBanking
/// </summary>
public class ParameterizedAspectService
{
    [ParameterizedAspect("ConnectionStrings")]
    public int Add(int a, int b) => a + b;

    [ParameterizedAspect("AppSettings")]
    public string Concat(string a, string b) => a + b;

    [ParameterizedAspect("SecurityConfig")]
    public decimal Calculate(decimal price, decimal quantity, decimal taxRate)
    {
        return price * quantity * (1 + taxRate);
    }
}

/// <summary>
/// Simulates a banking transaction service with aspects
/// </summary>
public class BankingTransactionService
{
    private static long _transactionCounter;

    [LightProcessingAspect]
    public TransactionResult ProcessTransaction(string accountId, decimal amount, string currency)
    {
        var txId = Interlocked.Increment(ref _transactionCounter);
        return new TransactionResult
        {
            TransactionId = $"TX{txId:D10}",
            AccountId = accountId,
            Amount = amount,
            Currency = currency,
            Status = "COMPLETED",
            Timestamp = DateTime.UtcNow
        };
    }

    [LightProcessingAspect]
    public BalanceResult GetBalance(string accountId)
    {
        return new BalanceResult
        {
            AccountId = accountId,
            Balance = 10000.00m,
            Currency = "USD",
            Timestamp = DateTime.UtcNow
        };
    }

    [LightProcessingAspect]
    public ValidationResult ValidateTransaction(string accountId, decimal amount)
    {
        return new ValidationResult
        {
            IsValid = amount > 0 && amount < 1000000,
            AccountId = accountId,
            MaxAmount = 1000000
        };
    }
}

public class TransactionResult
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class BalanceResult
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public decimal MaxAmount { get; set; }
}
