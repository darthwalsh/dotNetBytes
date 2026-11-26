To get AppVeyor build logs:

```
slug='darthwalsh/dotnetbytes'
branch="$(git branch --show-current)"
job_id="$(curl -sS "https://ci.appveyor.com/api/projects/${slug}/branch/${branch}" | jq -r '.build.jobs[0].jobId')"
curl -sS "https://ci.appveyor.com/api/buildjobs/${job_id}/log" 
```

Docs: https://www.appveyor.com/docs/api/projects-builds/

Build config lives in the AppVeyor project UI, not in the repo.
An `appveyor.yml` was tried on the `net10` branch and AppVeyor never applied it (no `init` echo, UI steps ran, even with **Ignore appveyor.yml** off), so it was deleted.
`GET /api/projects/darthwalsh/dotnetbytes/settings` needs a Bearer token and returns the UI settings (`buildMode`, `beforeBuildScripts`, ...).
