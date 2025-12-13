using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using DotNetAspects.LoadTests.Services;

namespace DotNetAspects.LoadTests.Benchmarks;

/// <summary>
/// Benchmarks to measure the overhead of aspect interception.
/// Compares baseline (no aspect) vs various aspect configurations.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[RankColumn]
public class AspectOverheadBenchmarks
{
    private BaselineService _baselineService = null!;
    private MinimalAspectService _minimalService = null!;
    private LightProcessingService _lightService = null!;
    private ParameterizedAspectService _parameterizedService = null!;

    [GlobalSetup]
    public void Setup()
    {
        _baselineService = new BaselineService();
        _minimalService = new MinimalAspectService();
        _lightService = new LightProcessingService();
        _parameterizedService = new ParameterizedAspectService();
    }

    #region Simple Integer Addition

    [Benchmark(Baseline = true, Description = "Add - No Aspect")]
    public int Add_Baseline() => _baselineService.Add(10, 20);

    [Benchmark(Description = "Add - Minimal Aspect")]
    public int Add_MinimalAspect() => _minimalService.Add(10, 20);

    [Benchmark(Description = "Add - Light Processing")]
    public int Add_LightProcessing() => _lightService.Add(10, 20);

    [Benchmark(Description = "Add - Parameterized")]
    public int Add_Parameterized() => _parameterizedService.Add(10, 20);

    #endregion

    #region String Concatenation

    [Benchmark(Description = "Concat - No Aspect")]
    public string Concat_Baseline() => _baselineService.Concat("Hello", "World");

    [Benchmark(Description = "Concat - Minimal Aspect")]
    public string Concat_MinimalAspect() => _minimalService.Concat("Hello", "World");

    [Benchmark(Description = "Concat - Light Processing")]
    public string Concat_LightProcessing() => _lightService.Concat("Hello", "World");

    #endregion

    #region Complex Calculation (Banking-like)

    [Benchmark(Description = "Calculate - No Aspect")]
    public decimal Calculate_Baseline() => _baselineService.Calculate(100.00m, 5, 0.07m);

    [Benchmark(Description = "Calculate - Minimal Aspect")]
    public decimal Calculate_MinimalAspect() => _minimalService.Calculate(100.00m, 5, 0.07m);

    [Benchmark(Description = "Calculate - Light Processing")]
    public decimal Calculate_LightProcessing() => _lightService.Calculate(100.00m, 5, 0.07m);

    [Benchmark(Description = "Calculate - Parameterized")]
    public decimal Calculate_Parameterized() => _parameterizedService.Calculate(100.00m, 5, 0.07m);

    #endregion

    #region Void Method

    [Benchmark(Description = "DoNothing - No Aspect")]
    public void DoNothing_Baseline() => _baselineService.DoNothing();

    [Benchmark(Description = "DoNothing - Minimal Aspect")]
    public void DoNothing_MinimalAspect() => _minimalService.DoNothing();

    [Benchmark(Description = "DoNothing - Light Processing")]
    public void DoNothing_LightProcessing() => _lightService.DoNothing();

    #endregion
}

/// <summary>
/// Custom benchmark configuration for consistent results.
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.ShortRun
            .WithWarmupCount(3)
            .WithIterationCount(10)
            .WithLaunchCount(1));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(RankColumn.Arabic);

        // Add statistical columns
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
    }
}
