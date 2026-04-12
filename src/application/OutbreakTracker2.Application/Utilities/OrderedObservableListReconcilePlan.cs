namespace OutbreakTracker2.Application.Utilities;

internal sealed record OrderedObservableListReconcilePlan<TModel, TViewModel, TKey>(
    IReadOnlyList<TModel> Models,
    IReadOnlyList<TViewModel> DesiredViewModels,
    IReadOnlyList<TKey> CacheKeysToRemove
)
    where TKey : notnull;
