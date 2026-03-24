# AStar.Development.Utilities — migration checklist

Work through these steps in order. Each step should leave the repo in a
buildable state — commit after each one so you have clean rollback points.

────────────────────────────────────────────────────────────────────────────
STEP 1 — Create the folder structure
────────────────────────────────────────────────────────────────────────────

mkdir -p packages/core/AStar.Development.Utilities
mkdir -p packages/core/AStar.Development.Utilities.Tests.Unit

────────────────────────────────────────────────────────────────────────────
STEP 2 — Copy source files
────────────────────────────────────────────────────────────────────────────

- Copy source files
- Ensure README.md in caps

- Do NOT copy:
    - bin/ obj/ .vs/ .idea/ folders
    - Any version-related files (AssemblyInfo.cs version attributes etc.)

────────────────────────────────────────────────────────────────────────────
STEP 3 — Add both projects to MonoRepo.slnx
────────────────────────────────────────────────────────────────────────────

Open MonoRepo.slnx and add inside the appropriate folder. e.g.:

    - dotnet sln add ./packages/core/logging/AStar.Dev.Logging.Extensions/AStar.Dev.Logging.Extensions.csproj
    - dotnet sln add ./packages/core/logging/AStar.Dev.Logging.Extensions.Tests.Unit/AStar.Dev.Logging.Extensions.Tests.Unit.csproj

See slnx-entries.txt for the recommended folder nesting.

────────────────────────────────────────────────────────────────────────────
STEP 4 — CSPROJ Updates
────────────────────────────────────────────────────────────────────────────

- Ensure the Directory.Packages.props includes any new NuGet packages
- Update to .Net10.0 if not already
- Remove any versions of NuGet packages in the relevant csproj files
- Update the test project(s) to reference the new location - currently, this is just removing "\src" from the path
- Add <NoWarn>CA1716</NoWarn> if necessary
- Ensure casing of the README (and the ACTUAL file of course):
    - <PackageReadmeFile>README.md</PackageReadmeFile>
    - <PackageProjectUrl>https://github.com/astar-development/astar-dev-mono/tree/main/packages/<AREA>></PackageProjectUrl>
    - <PackageId>AStar.Dev.Functional.Extensions</PackageId> - need to check the AStar.Dev.Utilities deployment to see if it has it or not
- Remove any of the following from the production project:
    - refer to the directory.build.props
      <None Include="..\..\astar.png" Pack="True" PackagePath="/" Link="astar.png" />

────────────────────────────────────────────────────────────────────────────
STEP 5 — Verify: build, test, pack
────────────────────────────────────────────────────────────────────────────

# Build the package only

dotnet build packages/core/AStar.Development.Utilities

# Run unit tests

dotnet test packages/core/AStar.Development.Utilities.Tests.Unit

# Pack and inspect — confirm version is 1.6.3-pre.{height}

dotnet pack packages/core/AStar.Development.Utilities --configuration Release
ls artifacts/bin/AStar.Development.Utilities/

# Build the entire solution — confirm nothing else broke

dotnet build

────────────────────────────────────────────────────────────────────────────
STEP 6 — Update consumers (within the repo)
────────────────────────────────────────────────────────────────────────────

Any other package or app in the mono-repo that currently references
AStar.Development.Utilities via <PackageReference> should be updated to use
<ProjectReference> instead:

    REMOVE:
      <PackageReference Include="AStar.Development.Utilities" />

    ADD:
      <ProjectReference Include="../../core/AStar.Development.Utilities/AStar.Development.Utilities.csproj" />

(Adjust the relative path to match each consumer's location in the repo.)

External consumers continue to use <PackageReference> from GitHub Packages
or NuGet.org — no change required on their side.

────────────────────────────────────────────────────────────────────────────
DONE
────────────────────────────────────────────────────────────────────────────

The package is now a full mono-repo citizen. CI will pick it up
automatically on the next push via the path filters in dotnet-ci.yml.

To publish a release, tag the commit:
git tag v1.6.3
git push origin v1.6.3

The nuget-publish.yml workflow will handle the rest.
