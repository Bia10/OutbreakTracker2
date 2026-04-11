using System.Collections.Specialized;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemy;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.InGameEnemies;

public interface IEnemyCardCollectionSource
{
    IEnumerable<InGameEnemyViewModel> Enemies { get; }

    event NotifyCollectionChangedEventHandler? CollectionChanged;
}
