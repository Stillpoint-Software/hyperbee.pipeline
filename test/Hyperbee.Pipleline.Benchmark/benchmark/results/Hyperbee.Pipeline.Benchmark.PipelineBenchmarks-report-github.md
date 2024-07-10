```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3672/23H2/2023Update/SunValley3)
12th Gen Intel Core i9-12900HK, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.300
  [Host]   : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.6 (8.0.624.26715), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                   | Mean        | Error      | StdDev   | Gen0   | Gen1   | Allocated |
|------------------------- |------------:|-----------:|---------:|-------:|-------:|----------:|
| PipelineExecution        |    229.2 ns |   225.0 ns | 12.33 ns | 0.1535 | 0.0010 |   1.88 KB |
| PipelineCancellation     |    351.8 ns |   145.4 ns |  7.97 ns | 0.2046 | 0.0010 |   2.51 KB |
| PipelineMiddleware       |    569.4 ns |   378.8 ns | 20.76 ns | 0.2289 | 0.0010 |   2.81 KB |
| PipelineEnumeration      |  1,471.7 ns |   521.1 ns | 28.56 ns | 0.5054 | 0.0038 |   6.21 KB |
| PipelineDistributedCache |  2,223.4 ns |   673.6 ns | 36.92 ns | 0.3052 | 0.3014 |   3.77 KB |
| PipelineMemoryCache      |  2,727.5 ns |   299.6 ns | 16.42 ns | 0.3510 | 0.3471 |   4.32 KB |
| PipelineAuth             | 15,849.4 ns | 1,125.6 ns | 61.70 ns | 0.4272 | 0.3967 |   5.59 KB |
