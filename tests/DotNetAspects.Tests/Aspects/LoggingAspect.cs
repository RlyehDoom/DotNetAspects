using DotNetAspects.Args;
using DotNetAspects.Interception;
using DotNetAspects.Serialization;

namespace DotNetAspects.Tests.Aspects;

/// <summary>
/// Simple logging aspect that tracks method invocations.
/// </summary>
[PSerializable]
public class LoggingAspect : MethodInterceptionAspect
{
    public static List<string> Logs { get; } = new();

    public static void ClearLogs() => Logs.Clear();

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        Logs.Add($"Before: {args.Method?.Name}");
        args.Proceed();
        Logs.Add($"After: {args.Method?.Name}, Result: {args.ReturnValue}");
    }
}

/// <summary>
/// Aspect that modifies the return value.
/// </summary>
[PSerializable]
public class DoubleReturnValueAspect : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        args.Proceed();

        if (args.ReturnValue is int intValue)
        {
            args.ReturnValue = intValue * 2;
        }
    }
}

/// <summary>
/// Aspect that skips the original method and returns a fixed value.
/// </summary>
[PSerializable]
public class SkipMethodAspect : MethodInterceptionAspect
{
    public int FixedReturnValue { get; set; } = 42;

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        // Don't call Proceed() - skip the method
        args.ReturnValue = FixedReturnValue;
    }
}

/// <summary>
/// Aspect that accesses and logs method arguments.
/// </summary>
[PSerializable]
public class ArgumentLoggingAspect : MethodInterceptionAspect
{
    public static List<object?[]> CapturedArguments { get; } = new();

    public static void ClearCaptured() => CapturedArguments.Clear();

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        var argValues = new object?[args.Arguments.Count];
        for (int i = 0; i < args.Arguments.Count; i++)
        {
            argValues[i] = args.Arguments[i];
        }
        CapturedArguments.Add(argValues);

        args.Proceed();
    }
}

/// <summary>
/// Aspect with configurable properties (like ICBanking aspects).
/// </summary>
[PSerializable]
public class ConfigurableAspect : MethodInterceptionAspect
{
    public string Prefix { get; set; } = "[DEFAULT]";
    public bool LogEnabled { get; set; } = true;

    public static List<string> Messages { get; } = new();

    public static void ClearMessages() => Messages.Clear();

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        if (LogEnabled)
        {
            Messages.Add($"{Prefix} Calling: {args.Method?.Name}");
        }

        args.Proceed();

        if (LogEnabled)
        {
            Messages.Add($"{Prefix} Returned: {args.ReturnValue}");
        }
    }
}

/// <summary>
/// Aspect that handles exceptions.
/// </summary>
[PSerializable]
public class ExceptionHandlingAspect : MethodInterceptionAspect
{
    public static List<Exception> CaughtExceptions { get; } = new();

    public static void ClearExceptions() => CaughtExceptions.Clear();

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        try
        {
            args.Proceed();
        }
        catch (Exception ex)
        {
            CaughtExceptions.Add(ex);
            // Get return type from MethodInfo and set default value
            if (args.Method is System.Reflection.MethodInfo methodInfo)
            {
                args.ReturnValue = GetDefaultValue(methodInfo.ReturnType);
            }
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(void)) return null;
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}

/// <summary>
/// Aspect that modifies arguments before proceeding.
/// </summary>
[PSerializable]
public class ArgumentModifyingAspect : MethodInterceptionAspect
{
    public int AddToFirstArg { get; set; } = 10;

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        if (args.Arguments.Count > 0 && args.Arguments[0] is int firstArg)
        {
            args.Arguments.SetArgument(0, firstArg + AddToFirstArg);
        }

        args.Proceed();
    }
}
