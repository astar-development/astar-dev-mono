# 003 — Avalonia for cross-platform desktop

**Date**: 2026-03-20
**Status**: Accepted

---

## Context

AStar Development produces 3–4 desktop applications targeting Windows, macOS,
and Linux. The requirements for the desktop UI framework are:

- Genuine cross-platform support — a single codebase producing native-feeling
  applications on all three major desktop operating systems
- C# and .NET — consistent with the rest of the portfolio, avoiding a context
  switch to a different language or runtime
- XAML-based UI definition — the team has existing XAML knowledge from prior
  WPF experience and prefers a declarative UI model
- Active development and a viable long-term future — framework abandonment
  is a serious risk for desktop applications which tend to have long lifetimes
- Compatibility with .NET 10 and the mono-repo's shared packages

---

## Decision

Use [Avalonia UI](https://avaloniaui.net) for all cross-platform desktop
applications.

Avalonia targets `net10.0` with platform-specific runtime identifiers
(`win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64`, `linux-arm64`).
A single `.csproj` produces all platform builds via `dotnet publish -r <rid>`.

ReactiveUI is adopted as the MVVM framework, as it is Avalonia's recommended
and best-supported option and provides strong support for reactive data binding
patterns.

Desktop apps are configured in `apps/desktop/Directory.Build.props` to publish
as self-contained single-file executables in Release configuration, requiring
no .NET installation on end-user machines.

---

## Consequences

**Becomes easier:**

- One codebase, one set of tests, one CI pipeline covers all three platforms
- Shared packages (domain logic, data access, auth) are consumed directly via
  `<ProjectReference>` — no platform abstraction layer needed
- XAML skills and patterns transfer directly from WPF; the learning curve is
  the delta between WPF and Avalonia rather than an entirely new paradigm
- Avalonia's headless testing support allows UI logic to be tested without a
  display server, which is important for Linux CI runners

**Becomes harder:**

- Platform-specific features (Windows shell integration, macOS menu bar,
  Linux desktop file registration) require platform-conditional code that
  WPF developers familiar with Windows-only development may not expect
- The Avalonia ecosystem is smaller than WPF's; some third-party controls
  available for WPF have no Avalonia equivalent and must be built or sourced
  differently
- Debugging UI issues that manifest on only one platform requires access to
  that platform or a CI matrix — the single-codebase benefit has a debugging
  cost when platform behaviour diverges

**Risks and mitigations:**

- _Risk_: Avalonia is not a Microsoft product; long-term stewardship depends
  on AvaloniaUI Ltd and the open-source community.
  _Mitigation_: Avalonia has commercial backing, an established enterprise
  customer base, and a track record of stable releases over several years.
  The XAML-based abstraction also means migration to a future alternative
  framework would be less disruptive than migrating from a proprietary API.

- _Risk_: `PublishTrimmed=true` in Release builds may cause runtime failures
  if referenced libraries are not trim-compatible.
  _Mitigation_: Avalonia 11+ is officially trim-compatible. Any trim
  incompatibilities in other dependencies will surface during Release builds
  in CI and can be addressed with `<TrimmerRootDescriptor>` entries.

---

## Alternatives considered

### WPF

WPF is Windows-only. Given the requirement for macOS and Linux support it is
not a viable option regardless of its maturity or ecosystem size.

### .NET MAUI

MAUI targets mobile (iOS, Android) primarily, with desktop support added later.
Its desktop story on Linux is limited and its primary abstraction is designed
around mobile UI patterns. For applications that are desktop-first and
require Linux support, Avalonia is the more appropriate choice.

### Electron (with C# backend)

Electron produces cross-platform desktop apps using web technologies with a
separate backend process. This would introduce JavaScript/TypeScript as a
second UI language alongside C#, require IPC between the UI and backend
processes, and significantly increase the runtime footprint of each application.
The existing XAML skills and the desire for a consistent C# codebase make
Electron a poor fit.

### Uno Platform

Uno Platform supports cross-platform development from a WinUI/WPF codebase and
has strong Windows compatibility. Its cross-platform rendering on macOS and
Linux is less mature than Avalonia's and its licensing and commercial model are
less straightforward for an independent developer. Avalonia's cleaner
cross-platform story and MIT licence make it the preferred choice.
