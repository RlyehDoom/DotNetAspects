# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DotNetAspects is a lightweight AOP (Aspect-Oriented Programming) library for .NET, designed as a drop-in replacement for PostSharp. It uses Fody for compile-time IL weaving.

**Current Version:** 1.3.2
**Targets:** `netstandard2.0` and `net8.0`
**Strong-name token:** `97f295f398ec39b7`

## Build Commands

```bash
# Build entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Run all tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~MethodInterceptionTests.LoggingAspect_ShouldLogBeforeAndAfter"

# Create NuGet package
dotnet pack src/DotNetAspects/DotNetAspects.csproj -c Release -o nupkg

# Publish to NuGet (requires .env with NUGET_API_KEY)
./publish-nuget.sh 1.3.2
```

## Architecture

### Two-Project Structure

1. **src/DotNetAspects/** - Core library containing:
   - `Interception/` - Base aspect classes (`MethodInterceptionAspect`, `OnMethodBoundaryAspect`, `LocationInterceptionAspect`)
   - `Args/` - Argument classes passed to aspect methods (`MethodInterceptionArgs`, `MethodExecutionArgs`, `LocationInterceptionArgs`)
   - `Extensibility/` - `MulticastAttributeUsageAttribute` and related enums
   - `Internals/` - Generic `Arguments<T>` implementations for typed argument access

2. **src/DotNetAspects.Fody/** - IL Weaver that transforms code at compile-time:
   - `ModuleWeaver.cs` (~1270 lines) - Main weaving logic using Mono.Cecil
   - Processes methods/properties with aspect attributes and rewrites IL

### How Weaving Works

When a method has an aspect attribute:
1. Original method is cloned (e.g., `Method` → `Method$Original` or `Method$Boundary`)
2. Original method body is replaced with interception logic
3. Interception creates aspect instance, builds args, calls aspect method (`OnInvoke`, `OnEntry`, etc.)
4. Aspect can call `Proceed()` to execute the original code

### Three Aspect Types

| Aspect | Purpose | Key Method(s) |
|--------|---------|---------------|
| `MethodInterceptionAspect` | Full method replacement | `OnInvoke(MethodInterceptionArgs)` |
| `OnMethodBoundaryAspect` | Code at method boundaries | `OnEntry`, `OnSuccess`, `OnException`, `OnExit` |
| `LocationInterceptionAspect` | Property/field access | `OnGetValue`, `OnSetValue` |

### Package Structure

The NuGet package is unified - it includes both the library DLLs and the Fody weaver:
- `lib/` - DotNetAspects.dll for both frameworks
- `weaver/` - DotNetAspects.Fody.dll (weaver)
- `build/` - MSBuild props/targets that auto-register the weaver (no FodyWeavers.xml needed)

### Test Project Configuration

Tests reference the weaver specially to enable weaving during test compilation:
```xml
<ProjectReference Include="...\DotNetAspects.Fody.csproj">
  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  <OutputItemType>WeaverFiles</OutputItemType>
</ProjectReference>
```

## Key Files

- `src/DotNetAspects.Fody/ModuleWeaver.cs` - All IL weaving logic
- `src/DotNetAspects/Args/MethodInterceptionArgs.cs` - Contains `Proceed()` and `Invoke()` logic
- `src/DotNetAspects/build/DotNetAspects.props` - Auto-registers weaver with Fody
- `tests/DotNetAspects.Tests/Aspects/` - Example aspects for reference
