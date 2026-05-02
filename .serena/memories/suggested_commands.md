## Key Commands

**Build & Restore:**
- `dotnet restore` - restore dependencies
- `dotnet build` - build all
- `dotnet build packages/core/[PackageName]` - build specific package
- `dotnet build apps/web/[AppName].Blazor` - build web app
- `dotnet clean && dotnet build` - full rebuild (if Directory.Build.props changed)

**Testing:**
- `dotnet test` - run all tests
- `dotnet test tests/[ProjectName].Tests` - run specific test project
- Coverage output: `TestResults/`

**Run Apps:**
- `dotnet run --project apps/web/[AppName].Blazor` - run Blazor web
- `dotnet run --project apps/desktop/[AppName].Desktop` - run Avalonia desktop
- `cd apps/web/[appname] && npm run dev` - run Astro web

**Git:**
- Always create feature branch before coding: `git checkout -b feature/short-desc-issue-num`
- Branch naming: `feature/...`, `bug/...`, `fix/...`, `doc/...`
- Never commit to `main`

**Commit & PR:**
- Conventional Commits: `feat(packages/core): ...`, `fix(apps/web/Portal.Blazor): ...`
- Raise PR after implementation
- Tests must pass, zero build warnings
