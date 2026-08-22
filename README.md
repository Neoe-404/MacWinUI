# MacWinUI

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
- .NET 8 SDK

## Build, test, and run

```powershell
dotnet restore .\MacWinUI.sln
dotnet build .\MacWinUI.sln
dotnet test .\MacWinUI.sln
dotnet run --project .\src\MacWinUI.App\MacWinUI.App.csproj
```

The Dock prefers cached Windows Shell icons and falls back to stable glyphs when
an executable icon is unavailable.

The default visual preset for a new profile is `BigSur`. Existing profiles keep
their saved theme; select `BigSur` in Control Center → Appearance to apply the
light frosted menu bar and Dock presentation.

The MenuBar labels are interactive. Use `File` to open Explorer or add Dock
items, `View` to switch themes and magnification, `Go` for common folders,
`Window` to restore MacWinUI placement, and `Help` for drag-and-drop guidance.

Right-click any Dock item to open it, reveal its containing folder, or remove it
from the visible Dock. Right-click the empty Dock surface to add items, open Dock
settings, toggle magnification, restore the Dock position, or recover hidden
default items.

`Reserve screen space` is enabled by default in Control Center. It registers the
MenuBar with the supported Windows AppBar API so maximized windows begin below
it. Turning the option off or exiting MacWinUI releases the reservation and
restores the original desktop work area.

Create a verified framework-dependent release with:

```powershell
.\scripts\publish.ps1
```

The output is written to `artifacts\publish` after Release build and tests pass.

MacWinUI can be closed from the top application menu, the empty Dock context
menu, or the bottom of Control Center. Every entry asks for confirmation and
uses the normal shutdown path so settings and reserved screen space are restored.

To add your own application, open Control Center from the top MenuBar, find
**Dock Items**, select **Add…**, and choose an application or file. You can also
drag files or applications onto the empty Dock surface to pin them. Drop a file
directly onto a compatible application icon to open it with that application.
Custom items can be removed from the same Control Center section. The selection
is stored for the current user in `%LocalAppData%\MacWinUI\dock-apps.json`.

## Development mode

Development is incremental. Existing working functionality is the baseline and
must not be removed to match an older milestone document. The current planned
milestone is `v0.2.15 — Safe Application Exit`; see
[`TASK.md`](TASK.md) for the authoritative scope and regression policy.
