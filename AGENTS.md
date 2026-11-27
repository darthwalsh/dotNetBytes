To get AppVeyor build logs for job 0 (there might be multiple jobs per build):

```
slug='darthwalsh/dotnetbytes'
branch="$(git branch --show-current)"
job_id="$(curl -sS "https://ci.appveyor.com/api/projects/${slug}/branch/${branch}" | jq -r '.build.jobs[0].jobId')"
curl -sS "https://ci.appveyor.com/api/buildjobs/${job_id}/log"
```

Docs: https://www.appveyor.com/docs/api/projects-builds/
