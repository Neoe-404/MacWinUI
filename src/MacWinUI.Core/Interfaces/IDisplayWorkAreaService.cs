using MacWinUI.Core.Display;

namespace MacWinUI.Core.Interfaces;

public interface IDisplayWorkAreaService
{
    DisplayWorkArea GetActiveWorkArea();

    DisplayWorkArea GetPrimaryWorkArea();
}
