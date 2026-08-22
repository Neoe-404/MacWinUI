using MacWinUI.Core.Dock;

namespace MacWinUI.Core.Interfaces;

public interface IScreenWorkAreaReservationService
{
    uint PositionChangedMessage { get; }

    uint ShellRestartedMessage { get; }

    bool HasActiveReservation { get; }

    bool ReserveTop(
        nint windowHandle,
        DockDisplayMode displayMode,
        double heightDip);

    void Release(nint windowHandle);

    void Invalidate(nint windowHandle);
}
