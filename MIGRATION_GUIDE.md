# Migration Guide: PostSharp to DotNetAspects

> **Back to main documentation:** [README.md](README.md)

This guide explains how to migrate from PostSharp to DotNetAspects for method interception and AOP.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Migration Steps](#migration-steps)
- [Enterprise Migration](#enterprise-migration)
- [API Compatibility](#api-compatibility)
- [Supported Features](#supported-features)
- [Examples](#examples)
- [Troubleshooting](#troubleshooting)

## Overview

DotNetAspects is a lightweight, open-source replacement for PostSharp that provides:

- **PostSharp-compatible API**: Same class names and method signatures
- **Compile-time IL weaving**: Using Fody for zero runtime overhead
- **Strong-named assembly**: PublicKeyToken `97f295f398ec39b7`
- **Single unified package**: Library + weaver in one NuGet package

### Package Structure

```
DotNetAspects (v1.4.0 - Stable)
├── DotNetAspects.dll      # Core library with aspects
└── DotNetAspects.Fody.dll # Fody weaver for IL weaving
```

## Installation

### Step 1: Remove PostSharp

Remove PostSharp packages and configuration from your project:

```xml
<!-- Remove these -->
<PackageReference Include="PostSharp" Version="x.x.x" />
<PackageReference Include="PostSharp.Redist" Version="x.x.x" />

<!-- Also remove if present -->
<DontImportPostSharp>True</DontImportPostSharp>
<SkipPostSharp>True</SkipPostSharp>
<SkipPostSharp>False</SkipPostSharp>
```

### Step 2: Add DotNetAspects

Add the unified DotNetAspects package:

```xml
<ItemGroup>
  <PackageReference Include="DotNetAspects" Version="1.4.0" />
  <PackageReference Include="Fody" Version="6.8.2">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Or via CLI:
```bash
dotnet add package DotNetAspects --version 1.4.0
dotnet add package Fody
```

The package automatically:
- Registers the DotNetAspects weaver with Fody
- Configures weaving in memory (no `FodyWeavers.xml` file needed)

## Migration Steps

### Step 1: Update Namespace Imports

Replace PostSharp namespaces with DotNetAspects equivalents:

```diff
- using PostSharp.Aspects;
- using PostSharp.Serialization;
- using PostSharp.Extensibility;
+ using DotNetAspects.Interception;
+ using DotNetAspects.Args;
+ using DotNetAspects.Extensibility;  // Only if using MulticastAttributeUsage
```

> **Note:** `[PSerializable]` is not required in DotNetAspects. You can remove it or keep it for compatibility.

### Step 2: Verify Aspect Definitions

Your aspect classes should work with minimal changes (just remove `[PSerializable]` if desired):

```csharp
using DotNetAspects.Interception;
using DotNetAspects.Args;

public class MyAspect : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        // Before logic
        args.Proceed();  // Call original method
        // After logic
    }
}
```

### Step 3: Build and Test

Build the project. Fody will run the DotNetAspects weaver during compilation:

```
Fody/DotNetAspects: Weaving method: MyNamespace.MyClass.MyMethod
```

## Enterprise Migration

When migrating large enterprise projects with multiple layers and shared libraries, you must migrate **the entire dependency chain** that uses PostSharp types.

### Understanding the Dependency Chain

In enterprise applications, `MethodInterceptionArgs` often flows through multiple layers:

```
┌─────────────────────────────────────────────────────────────────┐
│  Aspects Project (SmartAttributes)                              │
│  - Uses: MethodInterceptionAspect, MethodInterceptionArgs       │
│  - Passes args to: Interfaces, MethodParameters                 │
└─────────────────────────┬───────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────┐
│  Interfaces Project                                             │
│  - Defines: ITaskHelper.Execute(MethodInterceptionArgs args)    │
│  - References: MethodInterceptionArgs in method signatures      │
└─────────────────────────┬───────────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────────┐
│  MethodParameters Project                                       │
│  - Defines: ExecuteTaskIn { MethodInterceptionArgs Args; }      │
│  - DTOs that contain MethodInterceptionArgs                     │
└─────────────────────────────────────────────────────────────────┘
```

**All projects in the chain must be migrated together**, or you'll get type mismatch errors:
```
error CS0029: Cannot implicitly convert type
  'DotNetAspects.Args.MethodInterceptionArgs' to
  'PostSharp.Aspects.MethodInterceptionArgs'
```

### Migration Order

Migrate projects in this order (bottom-up):

1. **MethodParameters/DTOs** - Classes that contain `MethodInterceptionArgs` as properties
2. **Interfaces** - Interfaces with `MethodInterceptionArgs` in method signatures
3. **Aspects/Attributes** - The actual aspect classes
4. **Business Logic** - Implementations that use the interfaces

### Real-World Example

Here's what was required to migrate a banking application's aspects:

| Project | Files Changed | Description |
|---------|---------------|-------------|
| `Framework.MethodParameters` | 7 files | DTOs containing `MethodInterceptionArgs` |
| `ApplicationServer.Interfaces` | 4 files | Interfaces with `MethodInterceptionArgs` params |
| `ApplicationServer.SmartAttributes` | 8 files | Actual aspect classes |

### Migration Checklist for Each Project

For **each project** in the dependency chain:

**1. Update .csproj:**
```xml
<!-- REMOVE -->
<PackageReference Include="PostSharp" Version="x.x.x" />
<DontImportPostSharp>True</DontImportPostSharp>
<SkipPostSharp>True</SkipPostSharp>

<!-- ADD -->
<PackageReference Include="DotNetAspects" Version="1.4.0" />
<!-- Only add Fody to projects that DEFINE aspects, not projects that just use types -->
```

**2. Update all .cs files:**
```csharp
// REMOVE
using PostSharp.Aspects;
using PostSharp.Serialization;
using PostSharp.Extensibility;

// ADD
using DotNetAspects.Args;           // For MethodInterceptionArgs
using DotNetAspects.Interception;   // For aspect base classes
using DotNetAspects.Extensibility;  // For MulticastAttributeUsage
```

**3. Remove `[PSerializable]`** from all classes (optional but recommended)

### Finding All Files to Migrate

Use these commands to find all files that need migration:

```bash
# Find all projects referencing PostSharp
grep -r "PostSharp" --include="*.csproj" .

# Find all C# files using PostSharp
grep -r "using PostSharp" --include="*.cs" .

# Find all classes with [PSerializable]
grep -r "\[PSerializable\]" --include="*.cs" .
```

### Common Enterprise Patterns

**Pattern 1: Args in DTOs**
```csharp
// Before
using PostSharp.Aspects;

public class ExecuteTaskIn : BaseMethodIn
{
    public MethodInterceptionArgs Arguments { get; set; }
}

// After
using DotNetAspects.Args;

public class ExecuteTaskIn : BaseMethodIn
{
    public MethodInterceptionArgs Arguments { get; set; }
}
```

**Pattern 2: Args in Interfaces**
```csharp
// Before
using PostSharp.Aspects;

public interface ITaskHelper
{
    TaskOut Execute(MethodInterceptionArgs args, TaskIn input);
}

// After
using DotNetAspects.Args;

public interface ITaskHelper
{
    TaskOut Execute(MethodInterceptionArgs args, TaskIn input);
}
```

**Pattern 3: Helper Classes**
```csharp
// Before
using PostSharp.Aspects;

public class AttributeHelper
{
    public static T ExtractArgument<T>(MethodInterceptionArgs args)
    {
        return (T)args.Arguments[0];
    }
}

// After
using DotNetAspects.Args;

public class AttributeHelper
{
    public static T ExtractArgument<T>(MethodInterceptionArgs args)
    {
        return (T)args.Arguments[0];
    }
}
```

### Fody Package Placement

**Important:** Only add the Fody package to projects that **define** aspects (inherit from `MethodInterceptionAspect`, etc.). Projects that only **use** the types (like DTOs or interfaces) only need `DotNetAspects`:

| Project Type | DotNetAspects | Fody |
|--------------|---------------|------|
| Aspects/Attributes | ✅ | ✅ |
| Interfaces | ✅ | ❌ |
| MethodParameters/DTOs | ✅ | ❌ |
| Business Logic (uses aspects) | ✅ | ✅ |

## API Compatibility

### Namespace Mapping

| PostSharp Namespace | DotNetAspects Namespace |
|---------------------|------------------------|
| `PostSharp.Aspects` | `DotNetAspects.Interception` |
| `PostSharp.Aspects` | `DotNetAspects.Args` |
| `PostSharp.Serialization` | `DotNetAspects.Serialization` |
| `PostSharp.Extensibility` | `DotNetAspects.Extensibility` |
| `PostSharp.Aspects.Internals` | `DotNetAspects.Internals` |

### Class Mapping

| PostSharp | DotNetAspects | Notes |
|-----------|---------------|-------|
| `MethodInterceptionAspect` | `MethodInterceptionAspect` | Same API |
| `OnMethodBoundaryAspect` | `OnMethodBoundaryAspect` | Same API |
| `LocationInterceptionAspect` | `LocationInterceptionAspect` | Same API |
| `MethodInterceptionArgs` | `MethodInterceptionArgs` | Same API |
| `MethodExecutionArgs` | `MethodExecutionArgs` | Same API |
| `LocationInterceptionArgs` | `LocationInterceptionArgs` | Same API |
| `PSerializableAttribute` | Not required | Can be removed |
| `MulticastAttributeUsageAttribute` | `MulticastAttributeUsageAttribute` | Same API |
| `Arguments<T>` | `Arguments<T>` | Same API |

### Method Compatibility

| Method/Property | Behavior |
|-----------------|----------|
| `args.Proceed()` | Executes original method |
| `args.Invoke(args)` | Executes with custom arguments |
| `args.Arguments` | Access method arguments |
| `args.ReturnValue` | Get/set return value |
| `args.Instance` | Object instance (null for static) |
| `args.Method` | MethodBase information |

## Supported Features

### Fully Supported (v1.4.0)

- **MethodInterceptionAspect**: Method interception with `OnInvoke`
- **OnMethodBoundaryAspect**: Method boundaries with `OnEntry`, `OnSuccess`, `OnException`, `OnExit`
- **LocationInterceptionAspect**: Property interception with `OnGetValue`, `OnSetValue`
- `Proceed()` and `Invoke()` methods
- Access to method arguments
- Modifying return values
- Skipping original method execution
- Aspect properties (serialized at compile time)
- **Constructor arguments** (e.g., `[ConfigurationAccessor("key")]`)
- **Aspect instance caching** for high-throughput scenarios
- **Performance optimizations** (zero-copy arguments, cached bindings)
- Strong-named assemblies
- Multi-targeting: netstandard2.0 and net8.0
- In-memory weaver configuration (no FodyWeavers.xml needed)

### Not Yet Implemented

- Assembly-wide aspect application
- Aspect multicasting with patterns
- Async method interception

## Examples

### Before (PostSharp)

```csharp
using PostSharp.Aspects;
using PostSharp.Serialization;

[PSerializable]
public class LoggingAspect : MethodInterceptionAspect
{
    public string Prefix { get; set; } = "[LOG]";

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        Console.WriteLine($"{Prefix} Calling {args.Method.Name}");
        args.Proceed();
        Console.WriteLine($"{Prefix} Returned {args.ReturnValue}");
    }
}

public class MyService
{
    [LoggingAspect(Prefix = "[DEBUG]")]
    public string GetData(int id) => $"Data for {id}";
}
```

### After (DotNetAspects)

```csharp
using DotNetAspects.Interception;
using DotNetAspects.Args;

public class LoggingAspect : MethodInterceptionAspect
{
    public string Prefix { get; set; } = "[LOG]";

    public override void OnInvoke(MethodInterceptionArgs args)
    {
        Console.WriteLine($"{Prefix} Calling {args.Method.Name}");
        args.Proceed();
        Console.WriteLine($"{Prefix} Returned {args.ReturnValue}");
    }
}

public class MyService
{
    [LoggingAspect(Prefix = "[DEBUG]")]
    public string GetData(int id) => $"Data for {id}";
}
```

**Changes:** Only namespace imports and `[PSerializable]` removed.

## Troubleshooting

### Error: "No weavers found"

Starting with v1.3.2, `FodyWeavers.xml` is no longer required. The weaver is configured automatically via MSBuild properties.

If you're using an older version or have issues, you can manually create `FodyWeavers.xml`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">
  <DotNetAspects />
</Weavers>
```

### Error: "Could not load weaver assembly"

Verify DotNetAspects package is correctly installed:
```bash
dotnet restore
dotnet build
```

### Method not being woven

Check that:
1. The aspect inherits from `MethodInterceptionAspect`, `OnMethodBoundaryAspect`, or `LocationInterceptionAspect`
2. The method/property has the aspect attribute applied
3. Build output shows weaving messages

### Proceed() throws InvalidOperationException

The weaver must be properly configured. Check build output for weaving messages.

### Strong name warning (CS8002)

DotNetAspects is strong-named. Use the stable version:
```xml
<PackageReference Include="DotNetAspects" Version="1.4.0" />
```

PublicKeyToken: `97f295f398ec39b7`

### Build is slow

Fody runs during each build. For faster development:
- Use incremental builds
- Consider `<FodyDeferOptionalWeavers>true</FodyDeferOptionalWeavers>`

### Error: Cannot convert 'DotNetAspects.Args.MethodInterceptionArgs' to 'PostSharp.Aspects.MethodInterceptionArgs'

This error means you have a **partial migration**. Some projects still reference PostSharp while others use DotNetAspects.

**Solution:** Migrate all projects in the dependency chain. See [Enterprise Migration](#enterprise-migration) for details.

Common causes:
- DTOs/MethodParameters project still uses PostSharp
- Interface project still uses PostSharp
- Missing `using DotNetAspects.Args;` in some files

Find all remaining PostSharp references:
```bash
grep -r "using PostSharp" --include="*.cs" .
grep -r "PostSharp" --include="*.csproj" .
```

---

> **Back to main documentation:** [README.md](README.md)
