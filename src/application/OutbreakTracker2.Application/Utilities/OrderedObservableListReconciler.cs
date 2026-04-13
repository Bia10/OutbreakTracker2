using ObservableCollections;

namespace OutbreakTracker2.Application.Utilities;

internal static class OrderedObservableListReconciler
{
    public static OrderedObservableListReconcilePlan<TModel, TViewModel, TKey> BuildPlan<TModel, TViewModel, TKey>(
        IReadOnlyList<TModel> desiredModels,
        IReadOnlyDictionary<TKey, TViewModel> cache,
        Func<TModel, TKey> modelKeySelector,
        Func<TModel, TViewModel> createViewModel
    )
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(desiredModels);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(modelKeySelector);
        ArgumentNullException.ThrowIfNull(createViewModel);

        List<TViewModel> desiredViewModels = new(desiredModels.Count);
        HashSet<TKey> desiredKeys = [];

        foreach (TModel model in desiredModels)
        {
            TKey key = modelKeySelector(model);
            if (!desiredKeys.Add(key))
                throw new InvalidOperationException($"Duplicate reconcile key '{key}' is not supported.");

            if (!cache.TryGetValue(key, out TViewModel? viewModel))
                viewModel = createViewModel(model);

            desiredViewModels.Add(viewModel);
        }

        List<TKey> cacheKeysToRemove = [];
        foreach (TKey existingKey in cache.Keys)
        {
            if (!desiredKeys.Contains(existingKey))
                cacheKeysToRemove.Add(existingKey);
        }

        return new OrderedObservableListReconcilePlan<TModel, TViewModel, TKey>(
            desiredModels,
            desiredViewModels,
            cacheKeysToRemove
        );
    }

    public static void ApplyPlan<TModel, TViewModel, TKey>(
        OrderedObservableListReconcilePlan<TModel, TViewModel, TKey> plan,
        ObservableList<TViewModel> currentList,
        IDictionary<TKey, TViewModel> cache,
        Func<TViewModel, TKey> viewModelKeySelector,
        Action<TViewModel, TModel> updateViewModel
    )
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(currentList);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(viewModelKeySelector);
        ArgumentNullException.ThrowIfNull(updateViewModel);

        if (plan.Models.Count != plan.DesiredViewModels.Count)
            throw new InvalidOperationException("Reconcile plan models and view models must have matching counts.");

        foreach (TKey key in plan.CacheKeysToRemove)
            cache.Remove(key);

        for (int i = 0; i < plan.DesiredViewModels.Count; i++)
        {
            TViewModel viewModel = plan.DesiredViewModels[i];
            cache[viewModelKeySelector(viewModel)] = viewModel;
            updateViewModel(viewModel, plan.Models[i]);
        }

        ReconcileList(currentList, plan.DesiredViewModels, viewModelKeySelector);
    }

    public static void ApplyViewModels<TViewModel, TKey>(
        ObservableList<TViewModel> currentList,
        IReadOnlyList<TViewModel> desiredViewModels,
        Func<TViewModel, TKey> viewModelKeySelector
    )
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(currentList);
        ArgumentNullException.ThrowIfNull(desiredViewModels);
        ArgumentNullException.ThrowIfNull(viewModelKeySelector);

        ReconcileList(currentList, desiredViewModels, viewModelKeySelector);
    }

    private static void ReconcileList<TViewModel, TKey>(
        ObservableList<TViewModel> currentList,
        IReadOnlyList<TViewModel> desiredViewModels,
        Func<TViewModel, TKey> keySelector
    )
        where TKey : notnull
    {
        HashSet<TKey> desiredKeys = [];
        foreach (TViewModel viewModel in desiredViewModels)
        {
            if (!desiredKeys.Add(keySelector(viewModel)))
                throw new InvalidOperationException("Desired view model keys must be unique during reconciliation.");
        }

        for (int i = currentList.Count - 1; i >= 0; i--)
        {
            if (!desiredKeys.Contains(keySelector(currentList[i])))
                currentList.RemoveAt(i);
        }

        for (int desiredIndex = 0; desiredIndex < desiredViewModels.Count; desiredIndex++)
        {
            TViewModel desiredViewModel = desiredViewModels[desiredIndex];
            TKey desiredKey = keySelector(desiredViewModel);
            int currentIndex = FindIndex(currentList, desiredKey, keySelector);

            if (currentIndex < 0)
            {
                if (desiredIndex <= currentList.Count)
                    currentList.Insert(desiredIndex, desiredViewModel);
                else
                    currentList.Add(desiredViewModel);

                continue;
            }

            if (currentIndex != desiredIndex)
                currentList.Move(currentIndex, desiredIndex);
        }

        for (int i = currentList.Count - 1; i >= desiredViewModels.Count; i--)
            currentList.RemoveAt(i);
    }

    private static int FindIndex<TViewModel, TKey>(
        ObservableList<TViewModel> currentList,
        TKey key,
        Func<TViewModel, TKey> keySelector
    )
        where TKey : notnull
    {
        for (int i = 0; i < currentList.Count; i++)
        {
            if (EqualityComparer<TKey>.Default.Equals(keySelector(currentList[i]), key))
                return i;
        }

        return -1;
    }
}
