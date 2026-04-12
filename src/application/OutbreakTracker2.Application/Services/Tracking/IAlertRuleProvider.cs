using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public interface IAlertRuleProvider<T>
    where T : IHasId
{
    void Register(IEntityTracker<T> tracker);
}
