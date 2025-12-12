# DotNetAspects

[![NuGet](https://img.shields.io/nuget/v/DotNetAspects.svg)](https://www.nuget.org/packages/DotNetAspects/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A lightweight AOP (Aspect-Oriented Programming) library for .NET, designed as a drop-in replacement for PostSharp's core interception APIs. Uses Fody for compile-time IL weaving.

**Working in Progress**: Version 1.3.5 - This is the recommended version to start using it.

**Migrating from PostSharp?** See the [Migration Guide](MIGRATION_GUIDE.md) for step-by-step instructions.

**Critical Fixes**: 
- Old version had LocationInterceptionArgs as private and Fody need to access to them. 
- Issue was that Func<object> and Action<object> delegates cannot be directly created from methods that return/accept value types (like bool, int, etc.).

## Features

- **MethodInterceptionAspect**: Intercept method calls with full control over execution
- **OnMethodBoundaryAspect**: Execute code at method boundaries (entry, success, exception, exit)
- **LocationInterceptionAspect**: Intercept property and field access
- **PostSharp-compatible API**: Minimal code changes required for migration
- **Compile-time IL weaving**: Using Fody for zero runtime overhead
- **Strong-named assembly**: PublicKeyToken `97f295f398ec39b7`
- **Multi-targeting**: Supports `netstandard2.0` and `net8.0`

## Installation

Install the unified NuGet package (includes both library and Fody weaver):

```bash
dotnet add package DotNetAspects --version 1.3.2
dotnet add package Fody
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="DotNetAspects" Version="1.3.2" />
  <PackageReference Include="Fody" Version="6.8.2">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

The package automatically:
- Registers the DotNetAspects weaver with Fody
- Configures weaving in memory (no `FodyWeavers.xml` file needed)

## Quick Start

### 1. Create an Aspect

```csharp
using DotNetAspects.Interception;
using DotNetAspects.Args;

public class LoggingAspect : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        Console.WriteLine($"Before: {args.Method.Name}");
        args.Proceed();  // Execute original method
        Console.WriteLine($"After: {args.Method.Name}, Result: {args.ReturnValue}");
    }
}
```

### 2. Apply to Methods

```csharp
public class MyService
{
    [LoggingAspect]
    public string GetData(int id) => $"Data for {id}";
}
```

### 3. Build and Run

The Fody weaver automatically weaves aspects at compile time. No runtime configuration needed!

## Migration from PostSharp

Replace PostSharp namespaces with DotNetAspects equivalents:

```csharp
// Before (PostSharp)
using PostSharp.Aspects;

// After (DotNetAspects)
using DotNetAspects.Interception;
using DotNetAspects.Args;
```

> **Note:** `[PSerializable]` is not required in DotNetAspects. It's included only for PostSharp compatibility during migration.

See the complete [Migration Guide](MIGRATION_GUIDE.md) for detailed instructions.

### Quick Reference

| PostSharp | DotNetAspects |
|-----------|---------------|
| `PostSharp.Aspects.MethodInterceptionAspect` | `DotNetAspects.Interception.MethodInterceptionAspect` |
| `PostSharp.Aspects.OnMethodBoundaryAspect` | `DotNetAspects.Interception.OnMethodBoundaryAspect` |
| `PostSharp.Aspects.LocationInterceptionAspect` | `DotNetAspects.Interception.LocationInterceptionAspect` |
| `PostSharp.Aspects.MethodInterceptionArgs` | `DotNetAspects.Args.MethodInterceptionArgs` |
| `PostSharp.Aspects.MethodExecutionArgs` | `DotNetAspects.Args.MethodExecutionArgs` |
| `PostSharp.Serialization.PSerializableAttribute` | Not required (optional for compatibility) |
| `PostSharp.Extensibility.MulticastAttributeUsageAttribute` | `DotNetAspects.Extensibility.MulticastAttributeUsageAttribute` |

## Usage Examples

### Method Interception

```csharp
public class LoggingAspect : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        Console.WriteLine($"Before: {args.Method.Name}");
        args.Proceed();
        Console.WriteLine($"After: {args.Method.Name}, Result: {args.ReturnValue}");
    }
}
```

### Method Boundary

```csharp
public class TimingAspect : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        args.Tag = Stopwatch.StartNew();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        var sw = (Stopwatch)args.Tag!;
        sw.Stop();
        Console.WriteLine($"{args.Method.Name} took {sw.ElapsedMilliseconds}ms");
    }
}
```

### Property Interception

```csharp
public class NotifyPropertyChangedAspect : LocationInterceptionAspect
{
    public override void OnSetValue(LocationInterceptionArgs args)
    {
        var oldValue = args.GetCurrentValue();
        args.ProceedSetValue();

        if (!Equals(oldValue, args.Value))
        {
            Console.WriteLine($"Property {args.LocationName} changed");
        }
    }
}
```

## API Reference

### MethodInterceptionArgs

| Property/Method | Description |
|-----------------|-------------|
| `Instance` | The object instance (null for static methods) |
| `Method` | The MethodBase of the intercepted method |
| `Arguments` | The method arguments (IArguments) |
| `ReturnValue` | The return value (get/set) |
| `Proceed()` | Execute the original method with current arguments |
| `Invoke(IArguments)` | Execute the original method with custom arguments |

### MethodExecutionArgs

| Property/Method | Description |
|-----------------|-------------|
| `Instance` | The object instance (null for static methods) |
| `Method` | The MethodBase of the executing method |
| `Arguments` | The method arguments (IArguments) |
| `ReturnValue` | The return value (available in OnSuccess/OnExit) |
| `Exception` | The exception (available in OnException) |
| `FlowBehavior` | Control flow after aspect method returns |
| `Tag` | Pass state between aspect methods |

### LocationInterceptionArgs

| Property/Method | Description |
|-----------------|-------------|
| `Instance` | The object instance (null for static members) |
| `Location` | Information about the property/field |
| `Value` | The value being get/set |
| `GetCurrentValue()` | Get the current value from the location |
| `SetNewValue(value)` | Set the value to the location |
| `ProceedGetValue()` | Proceed with getting the value |
| `ProceedSetValue()` | Proceed with setting the value |

## Package Structure

The unified NuGet package contains:

```
DotNetAspects.nupkg
├── lib/
│   ├── net8.0/DotNetAspects.dll
│   └── netstandard2.0/DotNetAspects.dll
├── build/
│   ├── DotNetAspects.props   (registers weaver and configures in-memory weaving)
│   └── DotNetAspects.targets
└── weaver/
    └── netstandard2.0/DotNetAspects.Fody.dll
```

## Development

### Publishing a New Version

Use the provided script to build and publish to NuGet:

```bash
# Linux/macOS/Git Bash/Windows
./publish-nuget.sh 1.2.0
```

Configure your NuGet API key:
```bash
cp .env.example .env
# Edit .env and add your NUGET_API_KEY
```

### Project Structure

```
src/
├── DotNetAspects/              # Core library
│   ├── Args/                   # Argument classes
│   ├── Interception/           # Aspect base classes
│   ├── Serialization/          # PSerializable attribute
│   ├── Extensibility/          # Multicast attributes
│   └── Internals/              # Generic Arguments<T>
└── DotNetAspects.Fody/         # Fody weaver (IL weaving)
```

## Related Documentation

- [Migration Guide](MIGRATION_GUIDE.md) - Detailed migration instructions from PostSharp
- [Fody Documentation](https://github.com/Fody/Fody)

## License

MIT License - See [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
