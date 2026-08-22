# MacWinUI

[![.NET Desktop CI](https://github.com/Neoe-404/MacWinUI/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/Neoe-404/MacWinUI/actions/workflows/dotnet-desktop.yml)

MacWinUI is a safe, macOS-inspired desktop enhancement for Windows 10 and
Windows 11. It runs above the normal Windows shell and does not replace
`explorer.exe`, hide the Windows taskbar, modify system files, or require
administrator privileges.

## Current baseline

- Floating, translucent, rounded Dock centered above the native Windows taskbar
- Five default applications with explicit Terminal and VS Code fallbacks
- User-selected `.exe` applications that can be added to or removed from the Dock
- Drag-and-drop pinning for applications and files
- Drop files onto compatible Dock applications to open them with structured arguments
- Versioned per-user custom Dock persistence with malformed-file recovery
- Cached Windows Shell icon extraction with stable glyph fallbacks
- Embedded executable icon fallback and file-version-aware icon cache invalidation
- Asynchronous executable, URI, and shell launching
- Gaussian distance-based magnification with frame-rate-independent RenderTransform animation
- Running and active application dots with distinct semantic colors
- Light/Dark semantic theme resources with automatic Windows theme selection
- A graphite-and-navy dark visual system with compact, high-contrast chrome
- A Big Sur-inspired light glass theme with a full-width menu bar and capsule Dock
- Functional themed MenuBar dropdowns for files, appearance, folders, windows, and help
- Themed Dock context menus for opening, revealing, removing, adding, and configuring items
- Explicit WPF hardware-composition mode and cubic Bézier popup/click motion
- Adjustable XAML material density with safe Windows DWM blur fallback
- Centralized Dock styles, tooltips, separator, shadow, and appear animation
- Bindable Dock appearance settings for future Settings UI integration
- A macOS-inspired top MenuBar with clock, network, volume, and battery status
- A floating Control Center with live audio control, mute, system status,
  MacWinUI theme switching, and safe Windows Settings shortcuts
- Optional Windows 11 DWM material adaptation with automatic XAML fallback
- Active-monitor work-area placement with Per-Monitor V2 DPI awareness
- Dynamic MenuBar volume, mute, and proportional battery indicators
- Control Center preferences for Dock size, opacity, magnification, material,
  and reduced motion
- Debounced per-user appearance persistence in LocalApplicationData
- Versioned settings with malformed-file backup and confirmed reset to defaults
- Primary-display or follow-cursor placement selection
- Configurable MenuBar clock, network, volume, and battery visibility
- Work-area-aware scrolling for the Control Center on smaller displays
- Reversible Windows AppBar reservation that keeps native window controls below the MenuBar
- Explorer-restart AppBar recovery and runtime rendering/DPI diagnostics
- Persistent Dock drag ordering, drag-out removal, and optional edge auto-hide
- 256px Shell imagery with executable and shortcut fallbacks
- Existing-window activation and per-application visible window shortcuts
- Portable settings export/import with automatic previous-version backups
- Windows-language-aware Chinese and English primary UI resources
- Reproducible Release publishing through `scripts\publish.ps1`
- Confirmed exit actions in the MenuBar, Dock context menu, and Control Center
- Live work-area and DPI repositioning when Windows display metrics change
- Windows animation and high-contrast preference integration
- Keyboard focus rings, cyclic Tab navigation, and screen-reader labels
- Simple named-mutex single-instance protection
- MVVM plus App/Core/Windows project separation
- Unit-tested platform-independent magnification, audio, and window-placement logic

## Architecture

```text
MacWinUI.App     -> MacWinUI.Core
MacWinUI.App     -> MacWinUI.Windows
MacWinUI.Windows -> MacWinUI.Core
Core.Tests       -> MacWinUI.Core
```

`MacWinUI.Core` does not reference WPF or Windows APIs. Platform-specific
application discovery and launching are isolated in `MacWinUI.Windows`.

## Requirements

- Windows 10 or Windows 11 x64
- Git and PowerShell 7 (for source-based deployment)
- .NET 8 SDK (for build and development)
- .NET 8 Desktop Runtime x64 (for the framework-dependent published build)

## Deployment

### 1. Clone the repository

```powershell
git clone https://github.com/Neoe-404/MacWinUI.git
Set-Location .\MacWinUI
```

MacWinUI is Windows-only. Build and run it from a normal, non-administrator
PowerShell session.

### 2. Restore, build, and verify

```powershell
dotnet restore .\MacWinUI.sln
dotnet build .\MacWinUI.sln -c Release
dotnet test .\MacWinUI.sln -c Release --no-build
```

### 3. Publish a deployable folder

The repository includes a script that runs Release build, tests, and publish in
that order:

```powershell
.\scripts\publish.ps1
```

The framework-dependent output is created in `artifacts\publish`. Start it with:

```powershell
.\artifacts\publish\MacWinUI.App.exe
```

Do not copy only the `.exe`; keep all files in the publish folder together. The
current format requires the .NET 8 Desktop Runtime x64 on the target computer.

### 4. Development run

```powershell
dotnet run --project .\src\MacWinUI.App\MacWinUI.App.csproj
```

MacWinUI allows one running instance per Windows session. Close the existing
instance before testing a newly built version.

### 5. Optional startup shortcut

1. Press `Win+R` and open `shell:startup`.
2. Create a shortcut to the published `MacWinUI.App.exe`.
3. Keep the publish folder at a stable path.

This requires no administrator privileges and is reversed by deleting the
shortcut.

### 6. Upgrade

1. Quit MacWinUI through the MenuBar, Dock context menu, or Control Center.
2. Back up `%LocalAppData%\MacWinUI` if the layout is important.
3. Pull or download the newer source and publish it into a new folder.
4. Replace the old publish folder, then start the new executable.

Settings use versioned schemas. Previous valid files are preserved with
`.backup` or `.broken` suffixes when recovery is needed.

### 7. Configuration and recovery

Per-user state is stored in:

```text
%LocalAppData%\MacWinUI\appearance.json
%LocalAppData%\MacWinUI\dock-apps.json
```

Use Control Center to export or import a portable `.macwinui.json` bundle. If a
layout is damaged, use **Reset preferences** or restore a `.backup` file while
MacWinUI is not running.

If the MenuBar reservation is not desired, disable **Reserve screen space** in
Control Center. A normal exit releases the AppBar and restores the Windows work
area.

### 8. Uninstall

1. Quit MacWinUI normally so its AppBar reservation is released.
2. Remove any shortcut from `shell:startup`.
3. Delete the cloned or published application folder.
4. Optionally delete `%LocalAppData%\MacWinUI` to remove personal settings.

MacWinUI does not replace Explorer, hide the native Windows taskbar, install a
service, modify system DLLs, or require registry cleanup.

### Troubleshooting

- **Nothing happens when starting:** quit the already-running instance first.
- **Missing .NET error:** install the .NET 8 Desktop Runtime x64 or build with the
  .NET 8 SDK.
- **MenuBar overlaps window controls:** enable **Reserve screen space**, then use
  `Window → Reposition MacWinUI Windows`.
- **Application icon is generic:** restart MacWinUI to retry Shell extraction.
- **Display/DPI issue:** open `Help → Runtime Diagnostics` and record the render
  tier, DPI scale, work area, and AppBar status.
## Usage

- Select `BigSur`, `Auto`, `Light`, or `Dark` in Control Center → Appearance.
- Drag applications or files onto the empty Dock surface to pin them.
- Drop files directly onto compatible application icons to open them.
- Drag Dock icons to reorder them; drag an icon out of the Dock to remove it.
- Right-click a Dock item for open, window activation, reveal, and remove actions.
- Right-click the empty Dock for add, settings, auto-hide, restore, and quit actions.
- Use **Reserve screen space** to keep maximized window controls below the MenuBar.
- Quit through the application menu, Dock context menu, or Control Center so
  settings are saved and the AppBar reservation is released.
## Development mode

Development is incremental. Existing working functionality is the baseline and
must not be removed to match an older milestone document. The current planned
milestone is `v0.2.15 — Safe Application Exit`; see
[`TASK.md`](TASK.md) for the authoritative scope and regression policy.
