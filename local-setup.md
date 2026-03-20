# Local NuGet Updates

Run this locally - once per setup:

```text
dotnet nuget add source \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_PAT_TOKEN \
  --store-password-in-clear-text \
  --name github \
  "https://nuget.pkg.github.com/your-org/index.json"
```

The PAT needs only read:packages scope for restore. write:packages is only needed if you're pushing manually rather than via CI.

