## ADDED Requirements

### Requirement: Image thumbnail
For files whose extension maps to `FileType.Image`, the system SHALL display a 150×150 px thumbnail decoded from the file at `LocalPath`. The thumbnail SHALL be decoded at the target width (using `Bitmap.DecodeToWidth(150)`) to avoid loading full-resolution data into memory. Thumbnail decoding SHALL occur on a background thread; the UI SHALL display a loading placeholder until decoding completes.

#### Scenario: Image file with local copy present
- **WHEN** the result is an image file and `LocalPath` points to an existing file
- **THEN** a 150×150 thumbnail decoded from that file is displayed

#### Scenario: Image file not yet downloaded
- **WHEN** the result is an image file but `LocalPath` does not exist on disk
- **THEN** a generic image placeholder icon is displayed and click-to-open is disabled

#### Scenario: Thumbnail loads on background thread
- **WHEN** the card scrolls into view
- **THEN** the thumbnail begins decoding on a background thread, and the UI remains responsive until decoding completes

### Requirement: File type icon
For files whose extension does not map to `FileType.Image`, the system SHALL display a 150×150 icon representative of the file type (e.g. document, spreadsheet, video, audio, archive, code, unknown). The icon SHALL be a vector or high-resolution asset and SHALL NOT require access to `LocalPath`.

#### Scenario: Document file
- **WHEN** the result has extension `.pdf`, `.docx`, or `.txt`
- **THEN** a document-type icon is displayed

#### Scenario: Video file
- **WHEN** the result has extension `.mp4`, `.mkv`, or `.mov`
- **THEN** a video-type icon is displayed

#### Scenario: Unknown extension
- **WHEN** the result has an extension not in the recognised set
- **THEN** a generic file icon is displayed

### Requirement: Click to open
Clicking the result card SHALL open the file at `LocalPath` in the OS default application. The system SHALL use the platform-appropriate launcher (`xdg-open` on Linux, `open` on macOS, `explorer` on Windows). If `LocalPath` does not exist on disk the click SHALL be a no-op and the card SHALL appear visually disabled.

#### Scenario: Local file exists
- **WHEN** the user clicks a result card whose `LocalPath` exists on disk
- **THEN** the OS default application for that file type launches with the file

#### Scenario: Local file missing
- **WHEN** the user clicks a result card whose `LocalPath` does not exist on disk
- **THEN** no application is launched and the card appears disabled

#### Scenario: Cross-platform launcher
- **WHEN** the application runs on Linux
- **THEN** `xdg-open <LocalPath>` is invoked
- **WHEN** the application runs on macOS
- **THEN** `open <LocalPath>` is invoked
- **WHEN** the application runs on Windows
- **THEN** `explorer <LocalPath>` is invoked

### Requirement: Result card metadata
Each result card SHALL display the filename, formatted file size, and the most-specific classification tag alongside the thumbnail or icon.

#### Scenario: Metadata displayed
- **WHEN** a search result card is rendered
- **THEN** it shows the filename (final segment of `RemotePath`), the formatted size (e.g. `"4.2 MB"`), and the `TagName` of the first associated classification (or `"Unclassified"` if none)

### Requirement: Duplicate warning
When the duplicates-only filter is active the system SHALL display a disclaimer stating that duplicates are identified by name and size only and that verification is recommended before taking action.

#### Scenario: Disclaimer visible with duplicates filter
- **WHEN** the duplicates-only toggle is active
- **THEN** the disclaimer text is visible in the results panel

#### Scenario: Disclaimer hidden without duplicates filter
- **WHEN** the duplicates-only toggle is inactive
- **THEN** the disclaimer text is not shown
