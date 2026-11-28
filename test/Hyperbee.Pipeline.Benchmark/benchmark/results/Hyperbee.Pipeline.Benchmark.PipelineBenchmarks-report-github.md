```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7462)
12th Gen Intel Core i9-12900HK 2.50GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.101
  [Host]  : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2 [AttachedDebugger]
  .NET 10 : .NET 10.0.1 (10.0.125.57005), X64 RyuJIT AVX2
  .NET 8  : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

IterationCount=3  LaunchCount=1  WarmupCount=3 

```
| Job     | Runtime   | Method                   | Mean        | Error       | StdDev    | Gen0   | Gen1   | Gen2   | Allocated |
|-------- |---------- |------------------------- |------------:|------------:|----------:|-------:|-------:|-------:|----------:|
| .NET 8  | .NET 8.0  | PipelineExecution        |    265.6 ns |   192.50 ns |  10.55 ns | 0.1621 | 0.0005 |      - |   1.99 KB |
| .NET 8  | .NET 8.0  | PipelineCancellation     |    425.1 ns |   118.94 ns |   6.52 ns | 0.2294 | 0.0010 |      - |   2.81 KB |
| .NET 8  | .NET 8.0  | PipelineMiddleware       |    555.3 ns |   249.51 ns |  13.68 ns | 0.2403 | 0.0010 |      - |   2.95 KB |
| .NET 8  | .NET 8.0  | PipelineEnumeration      |  1,761.6 ns |   611.74 ns |  33.53 ns | 0.5627 | 0.0038 |      - |   6.91 KB |
| .NET 8  | .NET 8.0  | PipelineDistributedCache |  2,855.9 ns | 1,541.26 ns |  84.48 ns | 0.4044 | 0.3967 | 0.0038 |   4.95 KB |
| .NET 8  | .NET 8.0  | PipelineMemoryCache      |  3,436.0 ns |   748.86 ns |  41.05 ns | 0.4463 | 0.4387 | 0.0076 |   5.44 KB |
| .NET 8  | .NET 8.0  | PipelineAuth             | 20,689.3 ns | 3,660.02 ns | 200.62 ns | 0.6409 | 0.6104 |      - |   7.95 KB |

| .NET 10 | .NET 10.0 | PipelineExecution        |    223.6 ns |    60.20 ns |   3.30 ns | 0.1581 | 0.0005 |      - |   1.94 KB |
| .NET 10 | .NET 10.0 | PipelineCancellation     |    363.9 ns |    85.29 ns |   4.68 ns | 0.2246 | 0.0010 |      - |   2.76 KB |
| .NET 10 | .NET 10.0 | PipelineMiddleware       |    368.9 ns |   316.19 ns |  17.33 ns | 0.2365 | 0.0010 |      - |    2.9 KB |
| .NET 10 | .NET 10.0 | PipelineEnumeration      |  1,153.8 ns |   287.96 ns |  15.78 ns | 0.5589 | 0.0038 |      - |   6.86 KB |
| .NET 10 | .NET 10.0 | PipelineDistributedCache |  2,792.7 ns |   718.21 ns |  39.37 ns | 0.4082 | 0.4044 | 0.0076 |   4.93 KB |
| .NET 10 | .NET 10.0 | PipelineMemoryCache      |  3,104.6 ns |   596.82 ns |  32.71 ns | 0.4349 | 0.4311 | 0.0038 |   5.35 KB |
| .NET 10 | .NET 10.0 | PipelineAuth             | 12,378.4 ns | 3,355.84 ns | 183.95 ns | 0.6104 | 0.5951 |      - |   7.56 KB |
