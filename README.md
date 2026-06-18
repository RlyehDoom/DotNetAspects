# DotNetAspects

<div align="center">

**A lightweight, high-performance AOP library for .NET**

*The free, open-source alternative to PostSharp*

[![NuGet](https://img.shields.io/nuget/v/DotNetAspects.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/DotNetAspects/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DotNetAspects.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/DotNetAspects/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%20.NET%208%20%7C%20.NET%209-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)

[Features](#features) | [Quick Start](#quick-start) | [Performance](#performance) | [Migration from PostSharp](#migration-from-postsharp) | [Documentation](#documentation)

</div>

---

## Why DotNetAspects?

| | PostSharp | DotNetAspects |
|---|---|---|
| **Cost** | $499-$4,999/year per developer | **Free forever** |
| **License** | Proprietary | MIT (Open Source) |
| **Weaving** | Compile-time IL | Compile-time IL (Fody) |
| **API Compatibility** | - | PostSharp-compatible |
| **Performance** | Excellent | Excellent |

**DotNetAspects** provides the same powerful AOP capabilities as PostSharp with a compatible API, making migration straightforward while eliminating licensing costs.

---

## Features

- **MethodInterceptionAspect** - Full control over method execution with `Proceed()` / `ProceedAsync()` support
- **OnMethodBoundaryAspect** - Execute code at method entry, success, exception, and exit
- **LocationInterceptionAspect** - Intercept property and field get/set operations
- **Async support** - First-class weaving for `Task` / `Task<T>` methods via `OnInvokeAsync`/`ProceedAsync` (interception) and async-correct boundary callbacks
- **Cross-assembly aspects** - Aspects defined in one assembly can be applied in another, even without a direct reference to DotNetAspects (transitive type resolution)
- **PostSharp-Compatible API** - Minimal code changes for migration
- **Compile-Time IL Weaving** - Zero runtime reflection overhead using [Fody](https://github.com/Fody/Fody)
- **Aspect Instance Caching** - Optimized for high-throughput scenarios
- **Strong-Named Assembly** - Enterprise-ready with PublicKeyToken `97f295f398ec39b7`
- **Multi-Targeting** - Supports `netstandard2.0`, `net8.0` and `net9.0`

---

## Quick Start

### 1. Install the package

```bash
dotnet add package DotNetAspects
dotnet add package Fody
```

### 2. Create an aspect

```csharp
using DotNetAspects.Interception;
using DotNetAspects.Args;

public class LoggingAspect : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        Console.WriteLine($"Calling: {args.Method.Name}");

        args.Proceed();  // Execute the original method

        Console.WriteLine($"Result: {args.ReturnValue}");
    }
}
```

### 3. Apply to your code

```csharp
public class OrderService
{
    [LoggingAspect]
    public Order ProcessOrder(int orderId, decimal amount)
    {
        // Your business logic here
        return new Order { Id = orderId, Total = amount };
    }
}
```

### 4. Build and run

```bash
dotnet build
```

That's it! The aspect is woven at compile time - no runtime configuration needed.

---

## Performance

DotNetAspects v1.4.0 is optimized for high-throughput enterprise scenarios:

### Benchmarks

| Scenario | Operations | Time | Throughput |
|----------|------------|------|------------|
| Method Interception | 10,000 | 27ms | ~370,000 ops/sec |
| Concurrent Access (100 threads) | 50,000 | 38ms | ~1.3M ops/sec |
| Banking Transactions | 3,000 | <5ms | ~600,000 ops/sec |

### Optimizations in v1.4.0

- **Aspect Instance Caching** - Aspects are created once and reused (lazy singleton pattern)
- **Zero-Copy Arguments** - `GetRawArray()` avoids unnecessary array allocations
- **Cached Method Binding** - Reduces object allocation in hot paths
- **Minimal GC Pressure** - Sustained high throughput without Gen2 collections

### Running Benchmarks

```bash
cd tests/DotNetAspects.LoadTests
dotnet run -c Release
```

---

## Installation

### Package Reference

```xml
<ItemGroup>
  <PackageReference Include="DotNetAspects" Version="1.5.0" />
  <PackageReference Include="Fody" Version="6.8.2">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### CLI

```bash
dotnet add package DotNetAspects --version 1.5.0
dotnet add package Fody
```

> **Note:** No `FodyWeavers.xml` is required - the weaver is configured automatically.

---

## Usage Examples

### Method Interception

Intercept any method call with full control:

```csharp
public class CachingAspect : MethodInterceptionAspect
{
    private static readonly Dictionary<string, object> Cache = new();

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        var key = $"{args.Method.Name}:{string.Join(",", args.Arguments)}";

        if (Cache.TryGetValue(key, out var cached))
        {
            args.ReturnValue = cached;
            return;  // Skip original method
        }

        args.Proceed();
        Cache[key] = args.ReturnValue;
    }
}
```

### Method Boundary

Execute code at specific points in method execution:

```csharp
public class TimingAspect : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        args.Tag = Stopwatch.StartNew();
    }

    public override void OnSuccess(MethodExecutionArgs args)
    {
        Console.WriteLine($"{args.Method.Name} completed successfully");
    }

    public override void OnException(MethodExecutionArgs args)
    {
        Console.WriteLine($"{args.Method.Name} threw: {args.Exception.Message}");
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        var sw = (Stopwatch)args.Tag;
        Console.WriteLine($"{args.Method.Name} took {sw.ElapsedMilliseconds}ms");
    }
}
```

### Property Interception

Intercept property get/set operations:

```csharp
public class NotifyChangedAspect : LocationInterceptionAspect
{
    public override void OnGetValue(LocationInterceptionArgs args)
    {
        args.ProceedGetValue();
        Console.WriteLine($"Get {args.LocationName}: {args.Value}");
    }

    public override void OnSetValue(LocationInterceptionArgs args)
    {
        var oldValue = args.GetCurrentValue();
        args.ProceedSetValue();

        if (!Equals(oldValue, args.Value))
        {
            Console.WriteLine($"{args.LocationName} changed: {oldValue} -> {args.Value}");
        }
    }
}
```

### Async Methods

For `async Task` / `async Task<T>` methods, override `OnInvokeAsync` and `await args.ProceedAsync()`:

```csharp
public class AsyncLoggingAspect : MethodInterceptionAspect
{
    public override async Task OnInvokeAsync(MethodInterceptionArgs args)
    {
        Console.WriteLine($"Before: {args.Method?.Name}");
        await args.ProceedAsync();              // awaits the real async work
        Console.WriteLine($"After: {args.Method?.Name}, Result: {args.ReturnValue}");
    }
}

// Usage
[AsyncLoggingAspect]
public async Task<string> GreetAsync(string name)
{
    await Task.Delay(10);
    return $"Hello, {name}!";
}
```

**Modifying the result of an async method** — read/replace `args.ReturnValue` after awaiting:

```csharp
public class DoubleResultAspect : MethodInterceptionAspect
{
    public override async Task OnInvokeAsync(MethodInterceptionArgs args)
    {
        await args.ProceedAsync();              // ReturnValue is now the unwrapped result
        if (args.ReturnValue is int value)
            args.ReturnValue = value * 2;       // caller awaits and receives the doubled value
    }
}

[DoubleResultAspect]
public async Task<int> SquareAsync(int n)
{
    await Task.Delay(10);
    return n * n;                                // SquareAsync(5) => 50
}
```

**Async boundary aspects** — `OnMethodBoundaryAspect` is async-aware automatically. `OnEntry` runs
before the method; `OnSuccess`, `OnException` and `OnExit` fire **after** the awaited task completes
(not when the `Task` is returned), and `OnException` captures faults from the asynchronous continuation:

```csharp
public class AsyncTimingAspect : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
        => args.Tag = Stopwatch.StartNew();

    public override void OnSuccess(MethodExecutionArgs args)
        => Console.WriteLine($"{args.Method?.Name} returned: {args.ReturnValue}");

    public override void OnException(MethodExecutionArgs args)
        => Console.WriteLine($"{args.Method?.Name} faulted: {args.Exception?.Message}");

    public override void OnExit(MethodExecutionArgs args)
        => Console.WriteLine($"{args.Method?.Name} took {((Stopwatch)args.Tag!).ElapsedMilliseconds}ms");
}

[AsyncTimingAspect]
public async Task<int> ComputeAsync(int x)
{
    await Task.Delay(50);                        // OnSuccess/OnExit fire AFTER this completes
    return x + 1;
}
```

> **Note:** For async methods override `OnInvokeAsync` (not the synchronous `OnInvoke`). Supported
> return types in this release are `Task` and `Task<T>` (`ValueTask` is not yet supported).

### Aspects in a separate assembly

You can define aspects in a dedicated library and apply them in other projects. The weaver resolves
DotNetAspects types transitively, so the consuming assembly does **not** need a direct reference to
DotNetAspects — only to the library that defines the aspect.

### Aspect with Configuration

```csharp
public class RetryAspect : MethodInterceptionAspect
{
    public int MaxRetries { get; set; } = 3;
    public int DelayMs { get; set; } = 1000;

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        for (int i = 0; i <= MaxRetries; i++)
        {
            try
            {
                args.Proceed();
                return;
            }
            catch when (i < MaxRetries)
            {
                Thread.Sleep(DelayMs);
            }
        }
    }
}

// Usage
[RetryAspect(MaxRetries = 5, DelayMs = 500)]
public void UnreliableOperation() { }
```

---

## Migration from PostSharp

DotNetAspects provides a PostSharp-compatible API for easy migration:

### Namespace Changes

```diff
- using PostSharp.Aspects;
- using PostSharp.Serialization;
+ using DotNetAspects.Interception;
+ using DotNetAspects.Args;
```

### Code Changes

```diff
- [PSerializable]  // Not required in DotNetAspects
  public class MyAspect : MethodInterceptionAspect
  {
      // No changes needed to the aspect logic!
  }
```

### API Mapping

| PostSharp | DotNetAspects |
|-----------|---------------|
| `PostSharp.Aspects.MethodInterceptionAspect` | `DotNetAspects.Interception.MethodInterceptionAspect` |
| `PostSharp.Aspects.OnMethodBoundaryAspect` | `DotNetAspects.Interception.OnMethodBoundaryAspect` |
| `PostSharp.Aspects.LocationInterceptionAspect` | `DotNetAspects.Interception.LocationInterceptionAspect` |
| `PostSharp.Aspects.MethodInterceptionArgs` | `DotNetAspects.Args.MethodInterceptionArgs` |
| `PostSharp.Aspects.MethodExecutionArgs` | `DotNetAspects.Args.MethodExecutionArgs` |
| `[PSerializable]` | Not required |

> **Detailed Guide:** See [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) for enterprise migration strategies.
>
> **Version History:** See [CHANGELOG.md](CHANGELOG.md) for all releases.

---

## Documentation

### API Reference

#### MethodInterceptionArgs

| Property/Method | Description |
|-----------------|-------------|
| `Instance` | Object instance (null for static methods) |
| `Method` | MethodBase of the intercepted method |
| `Arguments` | Method arguments (`IArguments`) |
| `ReturnValue` | Get/set the return value |
| `Proceed()` | Execute original method with current arguments |
| `Invoke(args)` | Execute original method with custom arguments |
| `ProceedAsync()` | Await the original async method; unwraps the result into `ReturnValue` |
| `InvokeAsync(args)` | Await the original async method with custom arguments |

#### MethodExecutionArgs

| Property/Method | Description |
|-----------------|-------------|
| `Instance` | Object instance (null for static methods) |
| `Method` | MethodBase of the executing method |
| `Arguments` | Method arguments (`IArguments`) |
| `ReturnValue` | Return value (in OnSuccess/OnExit) |
| `Exception` | Exception thrown (in OnException) |
| `Tag` | Pass state between aspect methods |
| `FlowBehavior` | Control flow after aspect returns |

#### LocationInterceptionArgs

| Property/Method | Description |
|-----------------|-------------|
| `Instance` | Object instance (null for static) |
| `Location` | PropertyInfo/FieldInfo |
| `LocationName` | Name of the property/field |
| `Value` | Value being get/set |
| `GetCurrentValue()` | Get current value from location |
| `SetNewValue(value)` | Set value to location |
| `ProceedGetValue()` | Proceed with get operation |
| `ProceedSetValue()` | Proceed with set operation |

### Project Structure

```
src/
├── DotNetAspects/           # Core library
│   ├── Args/                # Argument classes
│   ├── Interception/        # Aspect base classes
│   └── Extensibility/       # Multicast attributes
└── DotNetAspects.Fody/      # IL weaver

tests/
├── DotNetAspects.Tests/     # Unit tests
└── DotNetAspects.LoadTests/ # Performance benchmarks
```

---

## Roadmap

- [x] MethodInterceptionAspect with Proceed()
- [x] OnMethodBoundaryAspect
- [x] LocationInterceptionAspect
- [x] Constructor argument support
- [x] Aspect instance caching
- [x] Performance optimizations for high-throughput
- [x] Async method interception (`Task` / `Task<T>`)
- [x] Cross-assembly aspect resolution
- [ ] `ValueTask` / `ValueTask<T>` support
- [ ] Assembly-wide aspect application
- [ ] Aspect multicasting with patterns

---

## Contributing

Contributions are welcome! Here's how to get started:

1. **Fork** the repository
2. **Clone** your fork: `git clone https://github.com/your-username/DotNetAspects.git`
3. **Create** a branch: `git checkout -b feature/amazing-feature`
4. **Make** your changes and add tests
5. **Run** tests: `dotnet test`
6. **Commit**: `git commit -m 'Add amazing feature'`
7. **Push**: `git push origin feature/amazing-feature`
8. **Open** a Pull Request

### Development Setup

```bash
git clone https://github.com/RlyehDoom/DotNetAspects.git
cd DotNetAspects
dotnet build
dotnet test
```

### Running Benchmarks

```bash
cd tests/DotNetAspects.LoadTests
dotnet run -c Release
```

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

- [Fody](https://github.com/Fody/Fody) - The IL weaving framework that makes this possible
- [Mono.Cecil](https://github.com/jbevain/cecil) - The library used for IL manipulation
- PostSharp - For pioneering .NET AOP and inspiring this compatible alternative

---

<div align="center">

**Made with love for the .NET community**

If you find this project useful, please consider giving it a star!

</div>
