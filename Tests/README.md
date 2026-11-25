# Tests

[`Lib/`](../Lib/README.md) changes are tested by [AppVeyor](https://ci.appveyor.com/project/darthwalsh/dotnetbytes).
Configured in [`appveyor.yml`](appveyor.yml). (Just Windows for now.)

See CI builds in https://ci.appveyor.com/project/darthwalsh/dotnetbytes/history
See: [`AGENTS.md`](../AGENTS.md) for bash snippet to get build logs.

## Coverage

Start vscode "Coverage Gutters: Watch", then run:

```bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=lcov
```

## Testing in Docker

Not yet supported on Linux, see [Future Work](../README.md#future-work)
