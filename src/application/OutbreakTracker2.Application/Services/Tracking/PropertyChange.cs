using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed record PropertyChange<T, TProp>(T Entity, TProp OldValue, TProp NewValue, string PropertyName)
    where T : IHasId;
