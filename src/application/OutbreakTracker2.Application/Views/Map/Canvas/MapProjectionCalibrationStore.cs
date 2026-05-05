using System.Text.Json;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

internal static class MapProjectionCalibrationStore
{
    private static readonly System.Threading.Lock SyncRoot = new();

    private static Dictionary<string, MapProjectionCalibration>? _defaultCalibrations;
    private static Dictionary<string, string>? _defaultCalibrationGroups;
    private static Dictionary<string, MapProjectionCalibration>? _overrideCalibrations;
    private static Dictionary<string, string>? _overrideCalibrationGroups;

    public static MapProjectionCalibration Resolve(string scenarioName, string? relativePath)
    {
        string scenarioKey = MapAssetNameUtility.GetScenarioSlug(scenarioName);
        string? assetKey = MapAssetNameUtility.GetAssetCalibrationKey(relativePath);

        EnsureLoaded();

        lock (SyncRoot)
        {
            string calibrationKey = ResolveCalibrationTargetKey_NoLock(scenarioName, relativePath);

            if (TryResolve(_overrideCalibrations!, calibrationKey, out MapProjectionCalibration resolvedCalibration))
                return resolvedCalibration;

            if (TryResolve(_defaultCalibrations!, calibrationKey, out resolvedCalibration))
                return resolvedCalibration;

            if (TryResolve(_overrideCalibrations!, assetKey, scenarioKey, out resolvedCalibration))
                return resolvedCalibration;

            if (TryResolve(_defaultCalibrations!, assetKey, scenarioKey, out resolvedCalibration))
                return resolvedCalibration;
        }

        return MapProjectionCalibration.Default;
    }

    public static string ResolveCalibrationTargetKey(string scenarioName, string? relativePath)
    {
        EnsureLoaded();

        lock (SyncRoot)
            return ResolveCalibrationTargetKey_NoLock(scenarioName, relativePath);
    }

    public static MapProjectionCalibration SaveCalibration(
        string scenarioName,
        string? relativePath,
        MapProjectionCalibration calibration
    )
    {
        EnsureLoaded();

        lock (SyncRoot)
        {
            string calibrationKey = ResolveCalibrationTargetKey_NoLock(scenarioName, relativePath);
            if (string.IsNullOrWhiteSpace(calibrationKey))
                return calibration;

            _overrideCalibrations![calibrationKey] = calibration;
            SaveOverrideCalibrations_NoLock();
        }

        return calibration;
    }

    public static MapProjectionCalibration ResetCalibration(string scenarioName, string? relativePath)
    {
        EnsureLoaded();

        lock (SyncRoot)
        {
            string calibrationKey = ResolveCalibrationTargetKey_NoLock(scenarioName, relativePath);
            if (!string.IsNullOrWhiteSpace(calibrationKey))
                _overrideCalibrations!.Remove(calibrationKey);

            SaveOverrideCalibrations_NoLock();
        }

        return Resolve(scenarioName, relativePath);
    }

    private static void EnsureLoaded()
    {
        if (_defaultCalibrations is not null && _overrideCalibrations is not null)
            return;

        lock (SyncRoot)
        {
            if (_defaultCalibrations is null || _defaultCalibrationGroups is null)
                LoadDefaultData_NoLock();

            if (_overrideCalibrations is null || _overrideCalibrationGroups is null)
                LoadOverrideData_NoLock();
        }
    }

    private static void LoadDefaultData_NoLock()
    {
        _defaultCalibrations = new Dictionary<string, MapProjectionCalibration>(StringComparer.OrdinalIgnoreCase);
        _defaultCalibrationGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        bool anyFound = false;
        foreach (string path in MapProjectionCalibrationFilePaths.GetDefaultProfilesPaths())
        {
            if (!File.Exists(path))
                continue;

            anyFound = true;
            MapProjectionCalibrationDocument document = LoadCalibrationDocument(path);

            foreach (KeyValuePair<string, MapProjectionCalibration> kvp in document.Calibrations ?? [])
                _defaultCalibrations[kvp.Key] = kvp.Value;

            foreach (KeyValuePair<string, string> kvp in document.CalibrationGroups ?? [])
                _defaultCalibrationGroups[kvp.Key] = kvp.Value;
        }

        if (!anyFound)
            System.Diagnostics.Trace.TraceWarning(
                "[MapProjectionCalibrationStore] No map-projection-profiles.json found under any Assets/Maps/<scenario>/ folder. Map geometry will not render."
            );
    }

    private static void LoadOverrideData_NoLock()
    {
        MapProjectionCalibrationDocument document = LoadCalibrationDocument(
            MapProjectionCalibrationFilePaths.GetOverrideProfilesPath()
        );
        _overrideCalibrations = new Dictionary<string, MapProjectionCalibration>(
            document.Calibrations ?? [],
            StringComparer.OrdinalIgnoreCase
        );
        _overrideCalibrationGroups = new Dictionary<string, string>(
            document.CalibrationGroups ?? [],
            StringComparer.OrdinalIgnoreCase
        );
    }

    private static MapProjectionCalibrationDocument LoadCalibrationDocument(string path)
    {
        if (!File.Exists(path))
            return new MapProjectionCalibrationDocument();

        try
        {
            using FileStream stream = File.OpenRead(path);
            return JsonSerializer.Deserialize(
                    stream,
                    MapProjectionCalibrationJsonContext.Default.MapProjectionCalibrationDocument
                ) ?? new MapProjectionCalibrationDocument();
        }
        catch (JsonException)
        {
            return new MapProjectionCalibrationDocument();
        }
        catch (IOException)
        {
            return new MapProjectionCalibrationDocument();
        }
    }

    private static void SaveOverrideCalibrations_NoLock()
    {
        string path = MapProjectionCalibrationFilePaths.GetOverrideProfilesPath();
        string? directoryPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);

        string json = JsonSerializer.Serialize(
            new MapProjectionCalibrationDocument
            {
                Calibrations = _overrideCalibrations!,
                CalibrationGroups = _overrideCalibrationGroups!,
            },
            MapProjectionCalibrationJsonContext.Default.MapProjectionCalibrationDocument
        );

        File.WriteAllText(path, json);
    }

    private static string ResolveCalibrationTargetKey_NoLock(string scenarioName, string? relativePath)
    {
        Dictionary<string, string> calibrationGroups = new(StringComparer.OrdinalIgnoreCase);

        if (_defaultCalibrationGroups is { Count: > 0 })
        {
            foreach ((string assetKey, string groupKey) in _defaultCalibrationGroups)
                calibrationGroups[assetKey] = groupKey;
        }

        if (_overrideCalibrationGroups is { Count: > 0 })
        {
            foreach ((string assetKey, string groupKey) in _overrideCalibrationGroups)
                calibrationGroups[assetKey] = groupKey;
        }

        return MapAssetNameUtility.GetCalibrationTargetKey(scenarioName, relativePath, calibrationGroups);
    }

    private static bool TryResolve(
        Dictionary<string, MapProjectionCalibration> calibrations,
        string calibrationKey,
        out MapProjectionCalibration calibration
    )
    {
        if (
            !string.IsNullOrWhiteSpace(calibrationKey)
            && calibrations.TryGetValue(calibrationKey, out MapProjectionCalibration? resolvedCalibration)
        )
        {
            calibration = resolvedCalibration;
            return true;
        }

        calibration = MapProjectionCalibration.Default;
        return false;
    }

    private static bool TryResolve(
        Dictionary<string, MapProjectionCalibration> calibrations,
        string? assetKey,
        string scenarioKey,
        out MapProjectionCalibration calibration
    )
    {
        if (
            !string.IsNullOrWhiteSpace(assetKey)
            && calibrations.TryGetValue(assetKey, out MapProjectionCalibration? assetCalibration)
        )
        {
            calibration = assetCalibration;
            return true;
        }

        if (
            !string.IsNullOrWhiteSpace(scenarioKey)
            && calibrations.TryGetValue(scenarioKey, out MapProjectionCalibration? scenarioCalibration)
        )
        {
            calibration = scenarioCalibration;
            return true;
        }

        calibration = MapProjectionCalibration.Default;
        return false;
    }
}
