using DotNetAspects.Tests.Aspects;

namespace DotNetAspects.Tests.Services;

/// <summary>
/// Service with logging aspect applied.
/// </summary>
public class LoggingService
{
    [LoggingAspect]
    public string GetMessage(string name)
    {
        return $"Hello, {name}!";
    }

    [LoggingAspect]
    public int Add(int a, int b)
    {
        return a + b;
    }

    [LoggingAspect]
    public void DoSomething()
    {
        // void method
    }
}

/// <summary>
/// Service to test return value modification.
/// </summary>
public class CalculatorService
{
    [DoubleReturnValueAspect]
    public int GetNumber(int value)
    {
        return value;
    }

    [DoubleReturnValueAspect]
    public int Multiply(int a, int b)
    {
        return a * b;
    }
}

/// <summary>
/// Service to test skipping method execution.
/// </summary>
public class SkippableService
{
    [SkipMethodAspect(FixedReturnValue = 100)]
    public int GetValue()
    {
        // This should never be called
        return 999;
    }

    [SkipMethodAspect]
    public int GetDefaultSkipValue()
    {
        return 999;
    }
}

/// <summary>
/// Service to test argument access.
/// </summary>
public class ArgumentService
{
    [ArgumentLoggingAspect]
    public string Concat(string a, string b, string c)
    {
        return a + b + c;
    }

    [ArgumentLoggingAspect]
    public int Sum(int a, int b, int c, int d)
    {
        return a + b + c + d;
    }
}

/// <summary>
/// Service with configurable aspect properties.
/// </summary>
public class ConfiguredService
{
    [ConfigurableAspect(Prefix = "[INFO]", LogEnabled = true)]
    public string GetInfo()
    {
        return "information";
    }

    [ConfigurableAspect(Prefix = "[DEBUG]", LogEnabled = true)]
    public int Calculate(int x)
    {
        return x * 2;
    }

    [ConfigurableAspect(LogEnabled = false)]
    public string Silent()
    {
        return "silent";
    }
}

/// <summary>
/// Service to test exception handling.
/// </summary>
public class ExceptionService
{
    [ExceptionHandlingAspect]
    public int DivideWithHandling(int a, int b)
    {
        if (b == 0) throw new DivideByZeroException("Cannot divide by zero");
        return a / b;
    }

    [ExceptionHandlingAspect]
    public string ThrowWithHandling()
    {
        throw new InvalidOperationException("Test exception");
    }
}

/// <summary>
/// Service to test argument modification.
/// </summary>
public class ArgumentModifyService
{
    [ArgumentModifyingAspect(AddToFirstArg = 5)]
    public int GetValue(int input)
    {
        return input;
    }

    [ArgumentModifyingAspect(AddToFirstArg = 100)]
    public int AddNumbers(int a, int b)
    {
        return a + b;
    }
}

/// <summary>
/// Static service to test static method interception.
/// </summary>
public static class StaticService
{
    [LoggingAspect]
    public static string StaticMethod(string input)
    {
        return $"Static: {input}";
    }

    [DoubleReturnValueAspect]
    public static int StaticCalculation(int value)
    {
        return value + 1;
    }
}
