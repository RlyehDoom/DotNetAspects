# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2024-12-12

### Added
- **Aspect Instance Caching**: Aspects are now cached in static fields using lazy initialization, significantly reducing object allocation in high-throughput scenarios
- **`GetRawArray()` method**: New method in `IArguments` to access the internal array without copying, enabling zero-allocation argument access
- **Load Testing Project**: New `DotNetAspects.LoadTests` project with:
  - BenchmarkDotNet performance benchmarks
  - Concurrency stress tests (up to 100 threads)
  - Memory allocation tests
  - GC pressure monitoring

### Changed
- **Optimized `MethodInterceptionArgs.Invoke()`**: Now uses `GetRawArray()` internally to avoid unnecessary array allocations
- **Cached `MethodBinding` property**: The `Binding` property is now cached on first access instead of creating a new object each time
- **Optimized `Arguments.ToArray()`**: For arrays with 0-1 elements, returns the internal array directly

### Performance
- ~370,000 ops/sec for method interception
- ~1.3M ops/sec with 100 concurrent threads
- Minimal GC pressure under sustained high load
- No Gen2 collections during normal operation

## [1.3.6] - 2024-12-11

### Fixed
- **Constructor Arguments Support**: The weaver now properly handles aspect attributes with constructor arguments (e.g., `[ConfigurationAccessor("key")]`)
- Added `EmitAspectCreation` method to correctly emit constructor calls with arguments
- Added `EmitLdcValue` to handle various value types in constructor arguments

## [1.3.5] - 2024-12-11

### Fixed
- **LocationInterceptionAspect Value Type Boxing**: Fixed `FieldAccessException` when intercepting properties that return value types (bool, int, etc.)
- Added `CreateGetterWrapper` and `CreateSetterWrapper` methods to properly handle boxing/unboxing
- Made `LocationInterceptionArgs` fields public for Fody weaver access

## [1.3.4] - 2024-12-10

### Fixed
- **LocationInterceptionAspect NullReferenceException**: Fixed null reference when accessing `Location` property
- Improved initialization of `LocationInfo` in property interception

## [1.3.2] - 2024-12-09

### Added
- In-memory weaver configuration (no `FodyWeavers.xml` required)
- Auto-registration of DotNetAspects weaver via MSBuild props

### Changed
- Unified NuGet package includes both library and Fody weaver
- Simplified installation process

## [1.3.0] - 2024-12-08

### Added
- **LocationInterceptionAspect**: Full support for property interception with `OnGetValue` and `OnSetValue`
- `LocationInterceptionArgs` with `GetCurrentValue()`, `SetNewValue()`, `ProceedGetValue()`, `ProceedSetValue()`
- `LocationInfo` class for property/field metadata

## [1.2.0] - 2024-12-07

### Added
- **OnMethodBoundaryAspect**: Support for method boundary interception
- `OnEntry`, `OnSuccess`, `OnException`, `OnExit` methods
- `MethodExecutionArgs` with `Tag`, `FlowBehavior`, `Exception` properties

## [1.1.0] - 2024-12-06

### Added
- Named property support in aspects (e.g., `[LoggingAspect(Prefix = "DEBUG")]`)
- Strong-named assembly with PublicKeyToken `97f295f398ec39b7`

## [1.0.0] - 2024-12-05

### Added
- Initial release
- **MethodInterceptionAspect** with `OnInvoke` and `Proceed()` support
- `MethodInterceptionArgs` with `Instance`, `Method`, `Arguments`, `ReturnValue`
- `IArguments` interface with indexer and `ToArray()`
- PostSharp-compatible API for easy migration
- Fody-based compile-time IL weaving
- Support for `netstandard2.0` and `net8.0`

[1.4.0]: https://github.com/RlyehDoom/DotNetAspects/compare/v1.3.6...v1.4.0
[1.3.6]: https://github.com/RlyehDoom/DotNetAspects/compare/v1.3.5...v1.3.6
[1.3.5]: https://github.com/RlyehDoom/DotNetAspects/compare/v1.3.4...v1.3.5
[1.3.4]: https://github.com/RlyehDoom/DotNetAspects/compare/v1.3.2...v1.3.4
[1.3.2]: https://github.com/RlyehDoom/DotNetAspects/compare/v1.3.0...v1.3.2
[1.3.0]: https://github.com/RlyehDoom/DotNetAspects/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/RlyehDoom/DotNetAspects/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/RlyehDoom/DotNetAspects/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/RlyehDoom/DotNetAspects/releases/tag/v1.0.0
