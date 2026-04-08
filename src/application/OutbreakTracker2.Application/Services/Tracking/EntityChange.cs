using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Tracking;

public sealed record EntityChange<T>(T Previous, T Current)
    where T : IHasId;
