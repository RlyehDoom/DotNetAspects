using DotNetAspects.Tests.Aspects;
using DotNetAspects.Tests.Services;

namespace DotNetAspects.Tests;

/// <summary>
/// Tests for MethodInterceptionAspect with Fody weaving.
/// These tests verify that aspects are correctly woven at compile time
/// and behave as expected at runtime.
/// </summary>
public class MethodInterceptionTests
{
    #region Basic Logging Aspect Tests

    [Fact]
    public void LoggingAspect_ShouldLogBeforeAndAfter_WhenMethodCalled()
    {
        // Arrange
        LoggingAspect.ClearLogs();
        var service = new LoggingService();

        // Act
        var result = service.GetMessage("World");

        // Assert
        Assert.Equal("Hello, World!", result);
        Assert.Contains("Before: GetMessage", LoggingAspect.Logs);
        Assert.Contains("After: GetMessage, Result: Hello, World!", LoggingAspect.Logs);
    }

    [Fact]
    public void LoggingAspect_ShouldWorkWithMultipleArguments()
    {
        // Arrange
        LoggingAspect.ClearLogs();
        var service = new LoggingService();

        // Act
        var result = service.Add(5, 3);

        // Assert
        Assert.Equal(8, result);
        Assert.Contains("Before: Add", LoggingAspect.Logs);
        Assert.Contains("After: Add, Result: 8", LoggingAspect.Logs);
    }

    [Fact]
    public void LoggingAspect_ShouldWorkWithVoidMethods()
    {
        // Arrange
        LoggingAspect.ClearLogs();
        var service = new LoggingService();

        // Act
        service.DoSomething();

        // Assert
        Assert.Contains("Before: DoSomething", LoggingAspect.Logs);
        Assert.Contains("After: DoSomething, Result: ", LoggingAspect.Logs);
    }

    #endregion

    #region Return Value Modification Tests

    [Fact]
    public void DoubleReturnValueAspect_ShouldDoubleReturnValue()
    {
        // Arrange
        var service = new CalculatorService();

        // Act
        var result = service.GetNumber(10);

        // Assert - original returns 10, aspect doubles it to 20
        Assert.Equal(20, result);
    }

    [Fact]
    public void DoubleReturnValueAspect_ShouldDoubleMultiplyResult()
    {
        // Arrange
        var service = new CalculatorService();

        // Act
        var result = service.Multiply(3, 4); // 3*4=12, doubled=24

        // Assert
        Assert.Equal(24, result);
    }

    #endregion

    #region Skip Method Execution Tests

    [Fact]
    public void SkipMethodAspect_ShouldReturnFixedValue_WithoutCallingMethod()
    {
        // Arrange
        var service = new SkippableService();

        // Act - method returns 999, but aspect returns 100
        var result = service.GetValue();

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public void SkipMethodAspect_ShouldUseDefaultFixedValue()
    {
        // Arrange
        var service = new SkippableService();

        // Act - method returns 999, but aspect returns default 42
        var result = service.GetDefaultSkipValue();

        // Assert
        Assert.Equal(42, result);
    }

    #endregion

    #region Argument Access Tests

    [Fact]
    public void ArgumentLoggingAspect_ShouldCaptureStringArguments()
    {
        // Arrange
        ArgumentLoggingAspect.ClearCaptured();
        var service = new ArgumentService();

        // Act
        var result = service.Concat("Hello", " ", "World");

        // Assert
        Assert.Equal("Hello World", result);
        Assert.Single(ArgumentLoggingAspect.CapturedArguments);

        var capturedArgs = ArgumentLoggingAspect.CapturedArguments[0];
        Assert.Equal(3, capturedArgs.Length);
        Assert.Equal("Hello", capturedArgs[0]);
        Assert.Equal(" ", capturedArgs[1]);
        Assert.Equal("World", capturedArgs[2]);
    }

    [Fact]
    public void ArgumentLoggingAspect_ShouldCaptureIntArguments()
    {
        // Arrange
        ArgumentLoggingAspect.ClearCaptured();
        var service = new ArgumentService();

        // Act
        var result = service.Sum(1, 2, 3, 4);

        // Assert
        Assert.Equal(10, result);
        Assert.Single(ArgumentLoggingAspect.CapturedArguments);

        var capturedArgs = ArgumentLoggingAspect.CapturedArguments[0];
        Assert.Equal(4, capturedArgs.Length);
        Assert.Equal(1, capturedArgs[0]);
        Assert.Equal(2, capturedArgs[1]);
        Assert.Equal(3, capturedArgs[2]);
        Assert.Equal(4, capturedArgs[3]);
    }

    #endregion

    #region Aspect Properties Tests

    [Fact]
    public void ConfigurableAspect_ShouldUseCustomPrefix()
    {
        // Arrange
        ConfigurableAspect.ClearMessages();
        var service = new ConfiguredService();

        // Act
        var result = service.GetInfo();

        // Assert
        Assert.Equal("information", result);
        Assert.Contains("[INFO] Calling: GetInfo", ConfigurableAspect.Messages);
        Assert.Contains("[INFO] Returned: information", ConfigurableAspect.Messages);
    }

    [Fact]
    public void ConfigurableAspect_ShouldUseDebugPrefix()
    {
        // Arrange
        ConfigurableAspect.ClearMessages();
        var service = new ConfiguredService();

        // Act
        var result = service.Calculate(5);

        // Assert
        Assert.Equal(10, result);
        Assert.Contains("[DEBUG] Calling: Calculate", ConfigurableAspect.Messages);
        Assert.Contains("[DEBUG] Returned: 10", ConfigurableAspect.Messages);
    }

    [Fact]
    public void ConfigurableAspect_ShouldNotLog_WhenDisabled()
    {
        // Arrange
        ConfigurableAspect.ClearMessages();
        var service = new ConfiguredService();

        // Act
        var result = service.Silent();

        // Assert
        Assert.Equal("silent", result);
        Assert.Empty(ConfigurableAspect.Messages);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void ExceptionHandlingAspect_ShouldCatchException()
    {
        // Arrange
        ExceptionHandlingAspect.ClearExceptions();
        var service = new ExceptionService();

        // Act
        var result = service.DivideWithHandling(10, 0);

        // Assert
        Assert.Equal(0, result); // Default int value
        Assert.Single(ExceptionHandlingAspect.CaughtExceptions);
        // Exception is wrapped in TargetInvocationException when using reflection
        var caughtException = ExceptionHandlingAspect.CaughtExceptions[0];
        if (caughtException is System.Reflection.TargetInvocationException tie)
        {
            Assert.IsType<DivideByZeroException>(tie.InnerException);
        }
        else
        {
            Assert.IsType<DivideByZeroException>(caughtException);
        }
    }

    [Fact]
    public void ExceptionHandlingAspect_ShouldWorkNormally_WhenNoException()
    {
        // Arrange
        ExceptionHandlingAspect.ClearExceptions();
        var service = new ExceptionService();

        // Act
        var result = service.DivideWithHandling(10, 2);

        // Assert
        Assert.Equal(5, result);
        Assert.Empty(ExceptionHandlingAspect.CaughtExceptions);
    }

    #endregion

    #region Argument Modification Tests

    [Fact]
    public void ArgumentModifyingAspect_ShouldModifyFirstArgument()
    {
        // Arrange
        var service = new ArgumentModifyService();

        // Act - input is 10, aspect adds 5, so method receives 15
        var result = service.GetValue(10);

        // Assert
        Assert.Equal(15, result);
    }

    [Fact]
    public void ArgumentModifyingAspect_ShouldModifyInAddition()
    {
        // Arrange
        var service = new ArgumentModifyService();

        // Act - a=10 becomes 110, b=5 unchanged, result = 110 + 5 = 115
        var result = service.AddNumbers(10, 5);

        // Assert
        Assert.Equal(115, result);
    }

    #endregion

    #region Static Method Tests

    [Fact]
    public void LoggingAspect_ShouldWorkWithStaticMethods()
    {
        // Arrange
        LoggingAspect.ClearLogs();

        // Act
        var result = StaticService.StaticMethod("Test");

        // Assert
        Assert.Equal("Static: Test", result);
        Assert.Contains("Before: StaticMethod", LoggingAspect.Logs);
        Assert.Contains("After: StaticMethod, Result: Static: Test", LoggingAspect.Logs);
    }

    [Fact]
    public void DoubleReturnValueAspect_ShouldWorkWithStaticMethods()
    {
        // Arrange & Act
        var result = StaticService.StaticCalculation(5); // 5+1=6, doubled=12

        // Assert
        Assert.Equal(12, result);
    }

    #endregion

    #region Multiple Calls Tests

    [Fact]
    public void Aspect_ShouldWorkWithMultipleCalls()
    {
        // Arrange
        LoggingAspect.ClearLogs();
        var service = new LoggingService();

        // Act
        var result1 = service.Add(1, 2);
        var result2 = service.Add(3, 4);
        var result3 = service.Add(5, 6);

        // Assert
        Assert.Equal(3, result1);
        Assert.Equal(7, result2);
        Assert.Equal(11, result3);
        Assert.Equal(6, LoggingAspect.Logs.Count); // 3 before + 3 after
    }

    #endregion
}
