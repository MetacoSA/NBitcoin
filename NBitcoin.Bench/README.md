# NBitcoin.Bench

This project use BenchmarkDotnet to measure performance of parts of NBitcoin implementation.

You can generate flamegraph to view with Perfview on Windows with the following command:
```powershell
dotnet run -c Release -- --runtimes netcoreapp2.1 --filter *GolombRiceFilters* --profiler ETW
```

Where `GolombRiceFilters` is the name of the benchmark class you are insterested in.

For generating flamegraph on linux, see [this comment](https://github.com/MetacoSA/NBitcoin/pull/656#issuecomment-462927805).