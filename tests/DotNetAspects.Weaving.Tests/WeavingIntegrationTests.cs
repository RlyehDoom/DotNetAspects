using DotNetAspects.Args;
using DotNetAspects.Interception;
using DotNetAspects.Serialization;
using Xunit;

namespace DotNetAspects.Weaving.Tests;

/// <summary>
/// Integration tests that verify Fody weaving actually works at runtime.
/// These tests check that aspects properly intercept method calls.
/// </summary>
public class WeavingIntegrationTests
{
    #region Test Aspects

    /// <summary>
    /// A simple logging aspect that tracks when methods are called and what they return.
    /// </summary>
    [PSerializable]
    public class LoggingAspect : MethodInterceptionAspect
    {
        public static List<string> Log { get; } = new();

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            Log.Add($"Before: {args.Method?.Name}");
            args.Proceed();
            Log.Add($"After: {args.Method?.Name}, Result: {args.ReturnValue}");
        }
    }

    /// <summary>
    /// An aspect that modifies the return value by doubling integers.
    /// </summary>
    [PSerializable]
    public class DoubleReturnValueAspect : MethodInterceptionAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {
            args.Proceed();
            if (args.ReturnValue is int value)
            {
                args.ReturnValue = value * 2;
            }
        }
    }

    /// <summary>
    /// An aspect that can skip the original method execution entirely.
    /// </summary>
    [PSerializable]
    public class SkipExecutionAspect : MethodInterceptionAspect
    {
        public string ReturnInstead { get; set; } = "Intercepted!";

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            // Don't call Proceed() - skip original method
            args.ReturnValue = ReturnInstead;
        }
    }

    #endregion

    #region Service Classes to Test

    /// <summary>
    /// A service class with methods decorated with the LoggingAspect.
    /// After weaving, calls to these methods should be intercepted.
    /// </summary>
    public class LoggedService
    {
        [LoggingAspect]
        public string Greet(string name)
        {
            return $"Hello, {name}!";
        }

        [LoggingAspect]
        public int Add(int a, int b)
        {
            return a + b;
        }
    }

    /// <summary>
    /// A calculator service with method that has its return value doubled.
    /// </summary>
    public class Calculator
    {
        [DoubleReturnValueAspect]
        public int Square(int n)
        {
            return n * n;
        }
    }

    /// <summary>
    /// A service where the original method is skipped.
    /// </summary>
    public class SkippedService
    {
        [SkipExecutionAspect(ReturnInstead = "Custom Value")]
        public string GetValue()
        {
            return "Original Value";
        }
    }

    #endregion

    #region Tests

    [Fact]
    public void LoggingAspect_ShouldInterceptMethodCall()
    {
        // Arrange
        LoggingAspect.Log.Clear();
        var service = new LoggedService();

        // Act
        var result = service.Greet("World");

        // Assert
        Assert.Equal("Hello, World!", result);
        Assert.Equal(2, LoggingAspect.Log.Count);
        Assert.Equal("Before: Greet", LoggingAspect.Log[0]);
        Assert.Equal("After: Greet, Result: Hello, World!", LoggingAspect.Log[1]);
    }

    [Fact]
    public void LoggingAspect_ShouldWorkWithMultipleParameters()
    {
        // Arrange
        LoggingAspect.Log.Clear();
        var service = new LoggedService();

        // Act
        var result = service.Add(5, 3);

        // Assert
        Assert.Equal(8, result);
        Assert.Contains("Before: Add", LoggingAspect.Log);
        Assert.Contains("After: Add, Result: 8", LoggingAspect.Log);
    }

    [Fact]
    public void DoubleReturnValueAspect_ShouldModifyReturnValue()
    {
        // Arrange
        var calculator = new Calculator();

        // Act
        var result = calculator.Square(5);

        // Assert - 5*5=25, then doubled = 50
        Assert.Equal(50, result);
    }

    [Fact]
    public void SkipExecutionAspect_ShouldSkipOriginalMethod()
    {
        // Arrange
        var service = new SkippedService();

        // Act
        var result = service.GetValue();

        // Assert - should return custom value, not original
        Assert.Equal("Custom Value", result);
    }

    #endregion
}
