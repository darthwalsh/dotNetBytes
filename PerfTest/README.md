# Performance Testing

On main branch `CloudFunction` run: `dotnet run -c Release -- --port 7777`

On WIP branch `CloudFunction`  run: `dotnet run -c Release`

Run test in this folder with:

    sudo dotnet run -c Release

## Latest result:

With WIPproj at 3db3eafb1 and Mainproj at ab3b12ffa, got these results, where each ~200ms execution is parsing 34 different EXEs. This means WIP took 142% the runtime, but at 6ms per HTTP call that is not at all significant.

``` ini
BenchmarkDotNet=v0.13.1, OS=macOS Big Sur 11.6 (20G165) [Darwin 20.6.0]
Intel Core i9-9880H CPU 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-BTIYNY : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT

LaunchCount=2  RunStrategy=ColdStart  
```
|   Method |     Mean |   Error |   StdDev |   Median |
|--------- |---------:|--------:|---------:|---------:|
|  WIPproj | 211.1 ms | 7.23 ms | 30.62 ms | 198.7 ms |
| Mainproj | 146.9 ms | 6.32 ms | 26.77 ms | 141.1 ms |
