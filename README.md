# M3u Editor

WinUI 3 application for lossless editing of `.m3u` and `.m3u8` playlists. The solution is split into three projects:

- **M3uEditor.Core** – parsing, writing, diagnostics, find/replace, projections, and edit helpers.
- **M3uEditor.App** – WinUI 3 UI with MVVM Toolkit.
- **M3uEditor.Core.Tests** – xUnit regression suite.

## Build and run

1. Install the .NET 10 SDK (see `global.json`).
2. Restore and build:
   ```bash
   dotnet restore
   dotnet build M3uEditor.sln
   dotnet test M3uEditor.sln
   ```
3. Run the WinUI 3 app from Visual Studio 2022 (17.10+) or via `dotnet run --project M3uEditor.App` on Windows 10 20H1 or later.

## Playlist modes and detection

The parser distinguishes playlist types automatically:

- **PlainM3u** – no `#EXT` tags.
- **ExtendedM3u** – contains `#EXTINF` without HLS tags.
- **HlsMaster** – contains `#EXT-X-STREAM-INF`.
- **HlsMedia** – any other `#EXT-X-` usage.

The detected kind is shown in the status bar and drives which editor view is loaded.

## Lossless model

- Line-based AST preserves every raw line, comment, unknown tag, and blank line.
- Roundtrip (`Parse` → `Write`) keeps original order, spacing, and newline style (`\n` vs `\r\n`).
- UTF-8 BOM presence is detected and preserved when writing.
- Unmodified lines are written verbatim; only edited lines are regenerated.

## Editing helpers

- **IPTV/Extended editor:** edits title, duration, URI, and IPTV attributes (`tvg-id`, `tvg-name`, `tvg-logo`, `group-title`) without dropping unknown attributes.
- **HLS Master editor:** updates URIs and `#EXT-X-STREAM-INF` attributes while preserving additional attributes.
- **HLS Media editor:** edits segment duration/title/URI while keeping leading tags (KEY/BYTERANGE/DISCONTINUITY/PROGRAM-DATE-TIME/MAP) and header tags intact.
- Raw view always available as a fallback and highlights diagnostics/find matches.

## Find/Replace

- Keyboard shortcuts: **Ctrl+F** (open), **Ctrl+H** (toggle), **F3** (find next).
- Options: match case, whole word, regex.
- Operations: find next (wrap-around), replace current, replace all. Edits only the touched lines and updates tag/URI values accordingly.

## Diagnostics

- Syntactic checks: missing URI after `EXTINF`/`STREAM-INF`, invalid durations, empty URIs, malformed quotes, missing/invalid `TARGETDURATION` or `MEDIA-SEQUENCE`, and unknown tags (info).
- Semantic checks: duplicate URIs, HLS segment duration exceeding target duration, missing master attributes (e.g., BANDWIDTH).
- Diagnostics include VS-like spans (line/index/length) and drive navigation/highlighting.

## Self-check

- UI stack is exclusively WinUI 3; there are no `System.Windows.*` or `System.Windows.Forms.*` usages.
- Key NuGet packages: `Microsoft.WindowsAppSDK`, `CommunityToolkit.Mvvm`, `CommunityToolkit.WinUI` (including UI.Controls for DataGrid), `xunit`, and `FluentAssertions` for tests.
- To run and test: `dotnet restore`, `dotnet build M3uEditor.sln`, and `dotnet test M3uEditor.sln` (Windows with .NET 10 SDK installed).
