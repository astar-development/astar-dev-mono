// =============================================================================
// SPIKE 4 — Trim + single-file compatibility
// =============================================================================
// ASSUMPTION: MSAL and Microsoft.Graph SDK are trim-compatible with the
//             existing desktop build config (PublishTrimmed + PublishSingleFile)
//
// HOW TO RUN:
//   dotnet publish -r linux-x64 -c Release
//   ./artifacts/publish/AStar.Dev.Spikes.TrimCompatibility/release_linux-x64/AStar.Dev.Spikes.TrimCompatibility
//
// WHAT TO CHECK (tick each box):
//   [ ] `dotnet publish` completes with zero ILLink/trim warnings
//       (TreatWarningsAsErrors=true in the csproj means warnings = build failure)
//   [ ] Published binary runs without TypeLoadException or MissingMethodException
//   [ ] MSAL token acquisition succeeds in the trimmed binary
//   [ ] Graph client call succeeds in the trimmed binary
//   [ ] Single-file output: only one executable file in the publish output
//
// IF TRIM WARNINGS APPEAR:
//   Document each warning with its assembly and member.
//   Resolution options (in order of preference):
//     1. Add [DynamicDependency] or [RequiresUnreferencedCode] to the calling code
//     2. Add a TrimmerRootDescriptor XML file to preserve specific members
//     3. As a last resort: set <PublishTrimmed>false</PublishTrimmed> in the app
//        csproj and document the trade-off (larger binary, but safe)
// =============================================================================

using Avalonia;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

// Verify the types are reachable after trimming
Console.WriteLine("Checking that key types survived trimming...");

// MSAL
var msalType = typeof(PublicClientApplicationBuilder);
Console.WriteLine($"✓ MSAL:         {msalType.FullName}");

// MSAL Extensions (token cache persistence)
var msalExtType = typeof(MsalCacheHelper);
Console.WriteLine($"✓ MSAL Ext:     {msalExtType.FullName}");

// Microsoft.Graph
var graphType = typeof(GraphServiceClient);
Console.WriteLine($"✓ Microsoft.Graph: {graphType.FullName}");

// Avalonia
var avaloniaType = typeof(AppBuilder);
Console.WriteLine($"✓ Avalonia:     {avaloniaType.FullName}");

Console.WriteLine();
Console.WriteLine("Type resolution OK. Now run a real auth + Graph call to confirm.");
Console.WriteLine("See Spike 3 (msal-auth) for the full auth flow.");
Console.WriteLine();
Console.WriteLine("If this binary was produced with `dotnet publish -c Release -r linux-x64`,");
Console.WriteLine("and it reached this line, trim compatibility is confirmed for these types.");
