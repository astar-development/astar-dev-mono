# Rules for Updates

## Versioning

Version is no longer tracked in `appsettings.json`. It is git-tag driven (`-p:Version=$(GitTag)`, tag format `v1.2.3`) and packaged/delivered via Velopack — see #744. Bump the tag per Conventional Commits semantics (patch for `fix`, minor for `feat`); do not add an `ApplicationVersion` setting back to `appsettings.json`.
