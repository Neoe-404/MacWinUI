using MacWinUI.Core.Dock;
using MacWinUI.Core.Models;

namespace MacWinUI.Core.Interfaces;

public interface IWindowMaterialService
{
    bool TryApply(
        nint windowHandle,
        WindowMaterial material,
        DockTheme requestedTheme,
        bool enabled);

    void Clear(nint windowHandle);
}
