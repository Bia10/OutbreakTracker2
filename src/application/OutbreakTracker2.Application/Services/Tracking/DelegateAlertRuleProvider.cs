using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

internal sealed class DelegateAlertRuleProvider<T>(Action<IEntityTracker<T>> registerAction) : IAlertRuleProvider<T>
    where T : IHasId
{
    private readonly Action<IEntityTracker<T>> _registerAction = registerAction;

    public void Register(IEntityTracker<T> tracker)
    {
        ArgumentNullException.ThrowIfNull(tracker);
        _registerAction(tracker);
    }
}
