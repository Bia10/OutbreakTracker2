namespace OutbreakTracker2.Application.Services.Embedding;

/// <summary>
/// Provides debugging information about the embedded window for a given process.
/// Separated from <see cref="IWindowEmbedder"/> so consumers that only need
/// diagnostic queries are not forced to depend on the full embedding API.
/// </summary>
public interface IWindowEmbedderDiagnostics
{
    /// <summary>
    /// Returns a human-readable summary of all top-level windows currently owned by
    /// <paramref name="pid"/>. Intended for debugging embedding failures.
    /// Safe to call from a background thread. Returns an empty string when not supported.
    /// </summary>
    string GetDiagnosticInfo(int pid);
}
