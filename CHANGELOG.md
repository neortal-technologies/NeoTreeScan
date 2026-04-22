# Changelog

All notable changes to NeoTreescan are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- CONTRIBUTING, SECURITY, CODE_OF_CONDUCT, NOTICE, and CHANGELOG files.
- GitHub issue templates, PR template, and CI build workflow.

### Changed
- License changed from MIT to Apache License 2.0 for stronger patent and
  trademark protection.
- README expanded with usage guide, FAQ, troubleshooting, and disclaimer
  sections.

## [1.0.0] - 2026-04-22

Initial public release.

### Added
- **Win32DirectoryScanner** - recursive scan via `FindFirstFileExW` with
  `FIND_FIRST_EX_LARGE_FETCH` and parallel BFS at the top two levels.
- **Long path support** via the `\\?\` prefix.
- **UNC / network share support**.
- **Admin mode** - enables `SeBackupPrivilege` + `SeRestorePrivilege` +
  `SeSecurityPrivilege` so protected paths (System Volume Information,
  $Recycle.Bin, other-user profiles) are enumerated.
- **MftScanner** (stub) - placeholder for a future raw-NTFS scanner;
  currently falls back to Win32DirectoryScanner and reports as
  `MFT(fallback)` in scan results.
- **Folder tree** with per-folder inline percent bars.
- **Squarified treemap** with file-level rendering, click-to-drill-in, and
  breadcrumb navigation.
- **Right-click folder context menu**: Open in Explorer, Copy path, Set as
  scan root.
- **Drag-and-drop** folder scanning.
- **Analysis tabs**:
  - By file type (extension, count, size, %)
  - By age bucket (<7d, 7-30d, 30-90d, 90-365d, 1-2y, >2y)
  - By size bucket (<4KB, 4KB-1MB, 1MB-100MB, 100MB-1GB, 1-10GB, >10GB)
  - Top 1000 files
- **Excel (.xlsx) export** via ClosedXML. Six sheets: Summary, Folders,
  File Types, Age Buckets, Size Buckets, Top Files.
- **Keyboard shortcuts**: F5 (scan), Esc (cancel), Ctrl+O (browse), Ctrl+E
  (export), Ctrl+L (focus path).
- **Dark theme** including Windows 11 immersive dark title bar via DWM.
- **Per-monitor-v2 DPI awareness** via app manifest.
- **Single-file self-contained EXE** via .NET 9 PublishSingleFile with
  compression enabled.
- **Icon builder tool** (`tools/iconbuilder/`) that regenerates the
  multi-resolution .ico programmatically.
- **Branding.cs** - single rebrand point for product name, company, version,
  copyright, description, license, website.
- **No telemetry, no network calls, no installer.**

[Unreleased]: ../../compare/v1.0.0...HEAD
[1.0.0]: ../../releases/tag/v1.0.0
