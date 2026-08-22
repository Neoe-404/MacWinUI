namespace MacWinUI.Core.Interfaces;

public interface IActiveApplicationService
{
    Task<string> GetActiveApplicationNameAsync(
        CancellationToken cancellationToken = default);
}
