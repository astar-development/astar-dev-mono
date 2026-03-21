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
STEP 3 — Place version.json
────────────────────────────────────────────────────────────────────────────

  Copy version.json
    → packages/core/AStar.Development.Utilities/version.json

  The Tests.Unit project inherits this version automatically.
  No separate version.json needed for the test project.

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
    - The old .csproj (replaced by the new lean version)
    - bin/ obj/ .vs/ .idea/ folders
    - Any version-related files (AssemblyInfo.cs version attributes etc.)
      Nerdbank.GitVersioning generates these automatically.

────────────────────────────────────────────────────────────────────────────
STEP 6 — Add both projects to MonoRepo.slnx
────────────────────────────────────────────────────────────────────────────

  Open MonoRepo.slnx and add inside the appropriate folder:

    <Project Path="packages/core/AStar.Development.Utilities/AStar.Development.Utilities.csproj" />
    <Project Path="packages/core/AStar.Development.Utilities.Tests.Unit/AStar.Development.Utilities.Tests.Unit.csproj" />

  See slnx-entries.txt for the recommended folder nesting.

────────────────────────────────────────────────────────────────────────────
STEP 7 — Wire up Nerdbank.GitVersioning
────────────────────────────────────────────────────────────────────────────

  a) Add to Directory.Packages.props (see packages-props-addition.txt):
       <PackageVersion Include="Nerdbank.GitVersioning" Version="3.7.115" />

  b) Install the nbgv CLI tool if not already installed:
       dotnet tool install -g nbgv

  c) Verify the version resolves correctly:
       nbgv get-version packages/core/AStar.Development.Utilities

     Expected output includes:
       NuGetPackageVersion: 1.6.3-pre.{height}
       AssemblyVersion: 1.6.0.0

  d) Commit version.json before running a build — Nerdbank.GitVersioning
     requires the file to be in git history to calculate the version height.

       git add packages/core/AStar.Development.Utilities/version.json
       git commit -m "chore(packages/core): add AStar.Development.Utilities to mono-repo at 1.6.3-pre"

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
