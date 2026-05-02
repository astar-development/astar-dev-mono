## AStar.Dev Mono-Repo

**Purpose:** Multi-product platform with Blazor web apps, Astro web apps, Avalonia desktop apps, and ~25 published NuGet packages.

**Tech Stack:**
- C# / .NET 10
- Blazor WebAssembly & Server
- Avalonia (cross-platform desktop)
- Astro (web)
- NuGet packages (GitHub + NuGet.org)

**Solution file:** `AStar.Dev.slnx`

**Key NuGet packages to use:**
- `AStar.Dev.Functional.Extensions`: Result<T>, Option<T>, Map/Bind variants
- `AStar.Dev.Logging.Extensions`: Compile-time LogMessage templates
- `AStar.Dev.Utilities`: String extensions, nullability checks

**Key architectural principle:** DI and logging are NOT afterthoughts—implement from start.
