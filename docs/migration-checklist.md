# AStar.Development.Utilities — migration checklist

Work through these steps in order. Each step should leave the repo in a
buildable state — commit after each one so you have clean rollback points.

────────────────────────────────────────────────────────────────────────────
STEP 1 — Create the folder structure
────────────────────────────────────────────────────────────────────────────

  mkdir -p packages/core/AStar.Development.Utilities
  mkdir -p packages/core/AStar.Development.Utilities.Tests.Unit

────────────────────────────────────────────────────────────────────────────
STEP 2 — Place the .csproj files
────────────────────────────────────────────────────────────────────────────

  Copy AStar.Development.Utilities.csproj
    → packages/core/AStar.Development.Utilities/AStar.Development.Utilities.csproj

  Copy AStar.Development.Utilities.Tests.Unit.csproj
    → packages/core/AStar.Development.Utilities.Tests.Unit/AStar.Development.Utilities.Tests.Unit.csproj

────────────────────────────────────────────────────────────────────────────
STEP 3 — REMOVED
────────────────────────────────────────────────────────────────────────────

  REMOVED

────────────────────────────────────────────────────────────────────────────
STEP 4 — Place the package README
────────────────────────────────────────────────────────────────────────────

  Copy AStar.Development.Utilities.README.md
    → packages/core/AStar.Development.Utilities/README.md

  Fill in the real Usage examples from the existing package source.
  The placeholder sections (string extensions, collection helpers, guards)
  should reflect the actual public API surface.

────────────────────────────────────────────────────────────────────────────
STEP 5 — Copy source files
────────────────────────────────────────────────────────────────────────────

  From the existing standalone repo, copy all .cs files:

    Source files  → packages/core/AStar.Development.Utilities/
    Test files    → packages/core/AStar.Development.Utilities.Tests.Unit/

  Do NOT change namespaces yet. Get it building green first.
  Namespace refactoring (if desired) is a separate commit after verification.

  Do NOT copy:
    - bin/ obj/ .vs/ .idea/ folders
    - Any version-related files (AssemblyInfo.cs version attributes etc.)

────────────────────────────────────────────────────────────────────────────
STEP 6 — Add both projects to MonoRepo.slnx
────────────────────────────────────────────────────────────────────────────

  Open MonoRepo.slnx and add inside the appropriate folder:

    <Project Path="packages/core/AStar.Development.Utilities/AStar.Development.Utilities.csproj" />
    <Project Path="packages/core/AStar.Development.Utilities.Tests.Unit/AStar.Development.Utilities.Tests.Unit.csproj" />

  See slnx-entries.txt for the recommended folder nesting.

────────────────────────────────────────────────────────────────────────────
STEP 7 — CSPROJ Updates
────────────────────────────────────────────────────────────────────────────

- Ensure the Directory.Packages.props includes any new NuGet packages
- Remove any versions of NuGet packages in the relevant csproj files
- Update the test project(s) to reference the new location - currently, this is just removing "\src" from the path
- Add <NoWarn>CA1716</NoWarn> if necessary
- Ensure casing of the README (and the ACTUAL file of course):
    - <PackageReadmeFile>README.md</PackageReadmeFile>
    - <PackageProjectUrl>https://github.com/astar-development/astar-dev-mono//tree/main/packages/<AREA>></PackageProjectUrl>
    - <PackageId>AStar.Dev.Functional.Extensions</PackageId> - need to check the AStar.Dev.Utilities deployment to see if it has it or not
- Remove any of the following from the production project:
    - <PackageIcon>astar.png</PackageIcon>
        <Authors>AStar Development, Jason Barden</Authors>
        <Company>AStar Development</Company>
        <Copyright>AStar Development 2025</Copyright>
        <Version>0.4.5</Version>
        <None Include="..\..\astar.png" Pack="True" PackagePath="/" Link="astar.png" />

────────────────────────────────────────────────────────────────────────────
STEP 8 — Verify: build, test, pack
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
STEP 9 — Update consumers (within the repo)
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
