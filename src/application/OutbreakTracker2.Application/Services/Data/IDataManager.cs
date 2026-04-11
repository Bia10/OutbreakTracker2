using OutbreakTracker2.PCSX2.Client;

namespace OutbreakTracker2.Application.Services.Data;

/// <summary>
/// Full contract. Only <see cref="DataManager"/> implements this.
/// Prefer <see cref="IDataObservableSource"/> or <see cref="IDataSnapshot"/> at injection sites.
/// </summary>
public interface IDataManager : IDataObservableSource, IDataSnapshot
{
    ValueTask InitializeAsync(IGameClient gameClient, CancellationToken cancellationToken);
}
