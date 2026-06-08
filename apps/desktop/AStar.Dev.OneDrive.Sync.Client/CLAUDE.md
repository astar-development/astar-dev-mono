# Rules for Updates

## BugFix

When a bug is fixed, the `appsettings.json` must be update so the `AStarDevOneDriveClient > ApplicationVersion` changes: old ApplicationVersion: 0.3.3, after bug fix: ApplicationVersion: 0.3.4

## Feature

When a feature is implemented, the `appsettings.json` must be update so the `AStarDevOneDriveClient > ApplicationVersion` changes: old ApplicationVersion: 0.3.3, after feature implementation: ApplicationVersion: 0.4.0

## Text Blocks

All text displayed in the application must be supplied from the localisation service, NOT hard-coded.
