using DotNetAspects.Args;
using DotNetAspects.Interception;
using DotNetAspects.Serialization;
using Xunit;

namespace DotNetAspects.Weaving.Tests;

/// <summary>
/// Integration tests that verify async-aware weaving for MethodInterceptionAspect
/// (OnInvokeAsync/ProceedAsync) and OnMethodBoundaryAspect on Task / Task&lt;T&gt; methods.
/// </summary>
public class AsyncWeavingTests
{
    #region Aspects

    [PSerializable]
    public class AsyncLoggingAspect : MethodInterceptionAspect
    {
        public static List<string> Log { get; } = new();

        public override async Task OnInvokeAsync(MethodInterceptionArgs args)
        {
            Log.Add($"Before: {args.Method?.Name}");
            await args.ProceedAsync();
            Log.Add($"After: {args.Method?.Name}, Result: {args.ReturnValue}");
        }
    }

    [PSerializable]
    public class AsyncDoubleAspect : MethodInterceptionAspect
    {
        public override async Task OnInvokeAsync(MethodInterceptionArgs args)
        {
            await args.ProceedAsync();
            if (args.ReturnValue is int v)
                args.ReturnValue = v * 2;
        }
    }

    [PSerializable]
    public class AsyncBoundaryTracerAspect : OnMethodBoundaryAspect
    {
        public static List<string> Log { get; } = new();

        public override void OnEntry(MethodExecutionArgs args) => Log.Add($"Entry:{args.Method?.Name}");
        public override void OnSuccess(MethodExecutionArgs args) => Log.Add($"Success:{args.ReturnValue}");
        public override void OnException(MethodExecutionArgs args) => Log.Add($"Exception:{args.Exception?.Message}");
        public override void OnExit(MethodExecutionArgs args) => Log.Add($"Exit:{args.Method?.Name}");
    }

    #endregion

    #region Services

    public class AsyncService
    {
        [AsyncLoggingAspect]
        public async Task<string> GreetAsync(string name)
        {
            await Task.Delay(10);
            return $"Hello, {name}!";
        }

        [AsyncDoubleAspect]
        public async Task<int> SquareAsync(int n)
        {
            await Task.Delay(10);
            return n * n;
        }

        [AsyncLoggingAspect]
        public async Task DoWorkAsync()
        {
            await Task.Delay(10);
        }
    }

    public class AsyncBoundaryService
    {
        [AsyncBoundaryTracerAspect]
        public async Task<int> ComputeAsync(int x)
        {
            await Task.Delay(10);
            return x + 1;
        }

        [AsyncBoundaryTracerAspect]
        public async Task FailAsync()
        {
            await Task.Delay(10);
            throw new InvalidOperationException("boom");
        }
    }

    #endregion

    #region Tests

    [Fact]
    public async Task AsyncInterception_AwaitsBeforeAndAfter()
    {
        AsyncLoggingAspect.Log.Clear();
        var service = new AsyncService();

        var result = await service.GreetAsync("World");

        Assert.Equal("Hello, World!", result);
        Assert.Equal(new[]
        {
            "Before: GreetAsync",
            "After: GreetAsync, Result: Hello, World!"
        }, AsyncLoggingAspect.Log);
    }

    [Fact]
    public async Task AsyncInterception_CanModifyReturnValue()
    {
        var service = new AsyncService();

        var result = await service.SquareAsync(5);

        Assert.Equal(50, result); // 25 doubled
    }

    [Fact]
    public async Task AsyncInterception_NonGenericTask_Works()
    {
        AsyncLoggingAspect.Log.Clear();
        var service = new AsyncService();

        await service.DoWorkAsync();

        Assert.Contains("Before: DoWorkAsync", AsyncLoggingAspect.Log);
        Assert.Contains("After: DoWorkAsync, Result: ", AsyncLoggingAspect.Log);
    }

    [Fact]
    public async Task AsyncBoundary_RunsSuccessAfterCompletion()
    {
        AsyncBoundaryTracerAspect.Log.Clear();
        var service = new AsyncBoundaryService();

        var result = await service.ComputeAsync(41);

        Assert.Equal(42, result);
        Assert.Equal(new[]
        {
            "Entry:ComputeAsync",
            "Success:42",
            "Exit:ComputeAsync"
        }, AsyncBoundaryTracerAspect.Log);
    }

    [Fact]
    public async Task AsyncBoundary_CatchesAsyncException()
    {
        AsyncBoundaryTracerAspect.Log.Clear();
        var service = new AsyncBoundaryService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.FailAsync());

        Assert.Equal(new[]
        {
            "Entry:FailAsync",
            "Exception:boom",
            "Exit:FailAsync"
        }, AsyncBoundaryTracerAspect.Log);
    }

    #endregion
}
