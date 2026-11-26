To get AppVeyor build logs:

```
slug='darthwalsh/dotnetbytes'
branch="$(git branch --show-current)"
job_id="$(curl -sS "https://ci.appveyor.com/api/projects/${slug}/branch/${branch}" | jq -r '.build.jobs[0].jobId')"
curl -sS "https://ci.appveyor.com/api/buildjobs/${job_id}/log" 
```

Docs: https://www.appveyor.com/docs/api/projects-builds/

Debug YAML vs UI: log should show `echo dotNetBytes appveyor.yml loaded` from `init:` when YAML is active.
If you see `nuget.exe restore` first, the UI pipeline ran (often UI **Before build** script); `GET /api/projects/darthwalsh/dotnetbytes/settings` needs a Bearer token and returns `ignoreAppveyorYml`, `beforeBuildScripts`, `buildMode`, and custom yml URL.
