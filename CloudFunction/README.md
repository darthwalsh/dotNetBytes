# GCP Serverless Function

### Local testing

```bash
dotnet run
```

Local server will be running at http://127.0.0.1:8080

### Deploy to GCP

Run in `dotNetBytes/CloudFunction/` dir

```bash
gcloud --project dotnetbytes functions deploy parse --entry-point CloudFunction.Function --source .. --runtime dotnet3 --trigger-http --allow-unauthenticated  --set-build-env-vars=GOOGLE_BUILDABLE=CloudFunction
```

Server runs at https://us-central1-dotnetbytes.cloudfunctions.net/parse
