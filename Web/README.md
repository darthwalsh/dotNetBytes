# ASP.NET Core MVC Function

Runs [`Lib/`](../Lib/) disassembly library as an ASP.NET Core app .
[Deployed](../README.md#deploying) in Docker on Fly.

## Local Server

```bash
dotnet run
```

Local data server will be running at http://127.0.0.1:8080. Send data to it with:
```bash
curl -sS -X POST --data-binary @view/Program.dat http://127.0.0.1:8080 | jq '.Children[].Name'
```

Interact with in web app, by following steps in [`README.md`](../README.md#manual-testing-for-web-app)
