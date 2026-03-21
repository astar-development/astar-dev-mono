# AStar Development — Mono-repo

A single repository containing all AStar Development products and shared libraries:
**Blazor** web applications, **Next.js** web applications, **Avalonia** desktop
applications, and ~50 published **NuGet packages**.

---

## Contents

- [Repo map](#repo-map)
- [Prerequisites](#prerequisites)
- [First-time setup](#first-time-setup)
- [Building](#building)
- [Running](#running)
- [Testing](#testing)
- [Publishing NuGet packages](#publishing-nuget-packages)
- [Infrastructure](#infrastructure)
- [CI/CD](#cicd)
- [Conventions](#conventions)
- [Troubleshooting](#troubleshooting)

---

## Repo map

```
/
├── apps/
│   ├── web/
│   │   ├── [AppName].Blazor/          # Blazor WebAssembly / Server apps
│   │   └── [appname]-next/            # Next.js apps
│   └── desktop/
│       └── [AppName].Desktop/         # Avalonia cross-platform desktop apps
│
├── packages/
│   ├── core/                          # Domain models, business logic
│   ├── infra/                         # Data access, auth, logging, HTTP
│   └── ui/                            # Shared UI components
│
├── infra/
│   └── terraform/
│       ├── staging/                   # Staging environment
│       └── prod/                      # Production environment
│
├── scripts/                           # Build, release, and automation scripts
├── tests/                             # Repo-wide integration / E2E tests
├── docs/                              # Architecture decisions, guides
│
├── .github/
│   └── workflows/
│       ├── dotnet-ci.yml              # Build + test on every push
│       ├── nuget-publish.yml          # Publish NuGet packages on version tag
│       └── infra-deploy.yml          # Terraform plan/apply
│
├── Directory.Build.props              # Shared MSBuild properties (all projects)
├── Directory.Build.targets            # Shared MSBuild targets (all projects)
├── Directory.Packages.props           # Central NuGet package version management
├── global.json                        # Pins .NET SDK version
├── NuGet.Config                       # NuGet feed configuration
└── MonoRepo.slnx                      # Solution file — opens everything in Rider/VS Code
```

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.0.x | Pinned via `global.json`. Install from [dot.net](https://dot.net) |
| Node.js | 24.x LTS | Required for Next.js apps only |
| Terraform | 1.10.x | Required for infra changes only. Install via [tfenv](https://github.com/tfutils/tfenv) |
| Rider | Latest | Recommended .NET IDE |
| VS Code | Latest | With extensions from `.vscode/extensions.json` |

> **First time with VS Code?** Open the repo and accept the prompt to install recommended
> extensions — everything in `.vscode/extensions.json` will be installed automatically.

---

## First-time setup

### 1. Clone

```bash
git clone https://github.com/astar.development/astar-dev-mono.git
cd astar-dev-mono
```

### 2. Authenticate to GitHub Packages

Your one-time local setup. Create a GitHub
[Personal Access Token](https://github.com/settings/tokens) with `read:packages` scope,
then run:

```bash
dotnet nuget add source \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_PAT_TOKEN \
  --store-password-in-clear-text \
  --name github \
  "https://nuget.pkg.github.com/astar.development/index.json"
```

This writes to your user-level NuGet config (`~/.nuget/NuGet/NuGet.Config`) and is
picked up by both Rider and VS Code automatically.

### 3. Restore all dependencies

```bash
# .NET packages
dotnet restore

# Node packages (Next.js apps)
cd apps/web/[appname]-next && npm install
```

### 4. Verify the build

```bash
dotnet build
```

A clean build with no errors confirms the setup is correct.

---

## Building

### All .NET projects

```bash
# Debug (default)
dotnet build

# Release
dotnet build --configuration Release
```

### A specific app or package

```bash
dotnet build apps/web/[AppName].Blazor
dotnet build packages/core/[PackageName]
```

### Next.js apps

```bash
cd apps/web/[appname]-next
npm run build
```

---

## Running

### Blazor apps

```bash
dotnet run --project apps/web/[AppName].Blazor
```

Or use the **Run** configurations in Rider / VS Code (`launch.json`) — press F5
to build and launch with the debugger attached.

### Avalonia desktop apps

```bash
dotnet run --project apps/desktop/[AppName].Desktop
```

### Next.js apps

```bash
cd apps/web/[appname]-next
npm run dev
```

---

## Testing

### Run all tests

```bash
dotnet test
```

### Run tests for a specific project

```bash
dotnet test tests/[ProjectName].Tests
```

### With coverage

```bash
dotnet test \
  --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Coverage reports are written to `TestResults/`. Open
`TestResults/**/coverage.cobertura.xml` in your IDE's coverage viewer.

### Avalonia UI tests

Avalonia headless tests require a virtual display on Linux/macOS CI. Locally on
Windows and macOS the standard display is used. If a UI test hangs, check the
blame output in `TestResults/` — VSTest blame mode is enabled globally via
`Directory.Build.targets`.

---

## Publishing NuGet packages

Packages are published automatically by CI when a version tag is pushed. **Do not
run `dotnet pack` / `dotnet nuget push` manually** for releases.

### Release process

```bash
# 1. Ensure main is clean and all tests pass
git checkout main && git pull

# 2. Tag the release (triggers nuget-publish.yml)
git tag v1.2.3
git push origin v1.2.3
```

The workflow will:
1. Build in Release with the tag-derived version
2. Run all tests (blocks publish if any fail)
3. Pack all packable projects
4. Push `.nupkg` and `.snupkg` to GitHub Packages
5. Create a GitHub Release with auto-generated release notes

### Pre-release versions

```bash
git tag v2.0.0-beta.1
git push origin v2.0.0-beta.1
```

Tags containing a hyphen are automatically marked as pre-release on GitHub.

### Local development — consuming your own packages

During active development, prefer `<ProjectReference>` over `<PackageReference>`
so changes propagate immediately without a publish cycle:

```xml
<!-- In a .csproj that consumes one of your packages locally -->
<ProjectReference Include="../../../packages/core/MyPackage/MyPackage.csproj" />
```

Switch back to `<PackageReference>` only when testing against a specific
published version.

---

## Infrastructure

Terraform configuration lives in `infra/terraform/`. There are two environments:

| Environment | Folder | Apply trigger |
|-------------|--------|---------------|
| Staging | `infra/terraform/staging/` | Merge to `main` |
| Production | `infra/terraform/prod/` | Merge to `main` + required reviewer approval |

### Running Terraform locally

```bash
cd infra/terraform/staging   # or prod

terraform init
terraform plan
terraform apply              # Staging only — prod requires CI approval gate
```

> **State is remote** — never run `terraform apply` against prod from a local
> machine. Use the `workflow_dispatch` trigger in GitHub Actions with the
> prod environment approval gate.

---

## CI/CD

| Workflow | File | Trigger |
|----------|------|---------|
| .NET build + test | `dotnet-ci.yml` | Push/PR touching `.cs`, `.csproj`, `*.slnx`, MSBuild config |
| NuGet publish | `nuget-publish.yml` | Push of a `v*` tag |
| Infra deploy | `infra-deploy.yml` | Push/PR touching `infra/**` |

All workflows require explicit permissions and run with least-privilege
`GITHUB_TOKEN` scopes. See each workflow file for details.

---

## Conventions

### NuGet package naming

All packages follow the `AStar.Dev.[Area].[Name]` pattern:

```
AStar.Dev.Core.Domain
AStar.Dev.Infra.DataAccess
AStar.Dev.Infra.Auth
AStar.Dev.Ui.Components
```

### Project structure

Each package project contains:

```
packages/[area]/[PackageName]/
├── [PackageName].csproj       # IsPackable=true, Description, PackageTags, PackageLicenseExpression
├── [PackageName].cs           # or appropriate entry point
└── README.md                  # Package-level docs (shown on NuGet / GitHub Packages)
```

### Versioning

This repo uses a single repo-wide version tag (strategy A). All packages in a
release share the same version number. See the [publishing section](#publishing-nuget-packages)
for the tagging workflow.

### Branch strategy

| Branch | Purpose |
|--------|---------|
| `main` | Always deployable. Direct pushes require passing CI. |
| `release/x.y` | Release stabilisation branches |
| `feat/...` | Feature branches — short-lived, merged via PR |
| `fix/...` | Bug fix branches |

### Commit messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(packages/core): add pagination support to IRepository
fix(apps/web/Portal.Blazor): correct auth redirect on session expiry
chore: bump Avalonia to 11.2.3
```

GitHub Actions `generate_release_notes: true` in `nuget-publish.yml` uses these
to build the release changelog automatically.

---

## Troubleshooting

**`dotnet restore` fails with 401 Unauthorized**
Your GitHub Packages credentials have expired or the PAT has been revoked.
Re-run the `dotnet nuget add source` command from [First-time setup](#first-time-setup)
with a fresh PAT.

**Build fails with `Version '0.0.1-local' detected` warning**
You ran `dotnet pack` without supplying a version. Supply one explicitly:
`dotnet pack -p:Version=1.2.3` or use the CI tag-based workflow.

**Avalonia designer not working in VS Code**
Ensure the `avaloniaui.avaloniaui-vscode` extension is installed and the project
has been built at least once (`dotnet build`). The designer requires a built
assembly to reflect against.

**Terraform `state lock` error**
A previous apply was interrupted. Check the remote state backend for a lock and
release it manually. For Terraform Cloud: Workspaces → [workspace] → Locks.

**`Directory.Build.props` changes not picked effect**
MSBuild caches evaluated props. Run `dotnet build --no-incremental` or clean
first: `dotnet clean && dotnet build`.
