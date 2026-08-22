namespace MacWinUI.Core.Models;

public readonly record struct ApplicationWindowInfo(
    nint WindowHandle,
    string Title);
