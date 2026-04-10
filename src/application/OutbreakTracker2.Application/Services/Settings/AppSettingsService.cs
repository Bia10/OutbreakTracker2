using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using R3;

namespace OutbreakTracker2.Application.Services.Settings;

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly IConfigurationRoot _configurationRoot;
    private readonly ILogger<AppSettingsService> _logger;
    private readonly string _userSettingsPath;
    private readonly IDisposable _reloadRegistration;
    private readonly ReactiveProperty<OutbreakTrackerSettings> _settings;

    public AppSettingsService(IConfiguration configuration, ILogger<AppSettingsService> logger)
        : this(configuration, logger, AppSettingsFilePaths.GetUserSettingsPath()) { }

    public AppSettingsService(IConfiguration configuration, ILogger<AppSettingsService> logger, string userSettingsPath)
    {
        _configurationRoot =
            configuration as IConfigurationRoot
            ?? throw new InvalidOperationException("App settings require an IConfigurationRoot instance.");
        _logger = logger;
        _userSettingsPath = string.IsNullOrWhiteSpace(userSettingsPath)
            ? throw new ArgumentException("User settings path cannot be null or whitespace.", nameof(userSettingsPath))
            : userSettingsPath;

        OutbreakTrackerSettings currentSettings = LoadValidatedSettings();
        _settings = new ReactiveProperty<OutbreakTrackerSettings>(currentSettings);
        _reloadRegistration = ChangeToken.OnChange(_configurationRoot.GetReloadToken, ReloadSettings);
    }

    public string UserSettingsPath => _userSettingsPath;

    public OutbreakTrackerSettings Current => _settings.Value;

    public Observable<OutbreakTrackerSettings> SettingsObservable => _settings;

    public async ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default)
    {
        Validate(settings);

        string? directoryPath = Path.GetDirectoryName(_userSettingsPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);

        FileStream stream = new(_userSettingsPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using (stream.ConfigureAwait(false))
        {
            await WriteSettingsDocumentAsync(stream, settings, cancellationToken).ConfigureAwait(false);
        }

        _configurationRoot.Reload();
        _settings.Value = LoadValidatedSettings();
    }

    public async ValueTask ExportAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new InvalidOperationException("The selected export destination cannot be written to.");

        await WriteSettingsDocumentAsync(destination, Current, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<OutbreakTrackerSettings> ImportAsync(
        Stream source,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
            throw new InvalidOperationException("The selected import source cannot be read.");

        if (source.CanSeek)
            source.Position = 0;

        try
        {
            using JsonDocument jsonDocument = await JsonDocument
                .ParseAsync(source, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            OutbreakTrackerSettings settings = DeserializeImportedSettings(jsonDocument.RootElement);
            await SaveAsync(settings, cancellationToken).ConfigureAwait(false);
            return settings;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("The selected file is not valid JSON.", ex);
        }
    }

    public ValueTask<OutbreakTrackerSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(_userSettingsPath))
            File.Delete(_userSettingsPath);

        _configurationRoot.Reload();

        OutbreakTrackerSettings settings = LoadValidatedSettings();
        _settings.Value = settings;
        _logger.LogInformation("Application settings reset to bundled defaults.");
        return ValueTask.FromResult(settings);
    }

    public void Dispose()
    {
        _reloadRegistration.Dispose();
        _settings.Dispose();
    }

    private void ReloadSettings()
    {
        try
        {
            OutbreakTrackerSettings settings = LoadValidatedSettings();
            _settings.Value = settings;
            _logger.LogInformation("Application settings reloaded from configuration files.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ignoring invalid application settings reload.");
        }
    }

    private OutbreakTrackerSettings LoadValidatedSettings()
    {
        OutbreakTrackerSettings settings =
            _configurationRoot.GetSection(OutbreakTrackerSettings.SectionName).Get<OutbreakTrackerSettings>()
            ?? new OutbreakTrackerSettings();

        if (File.Exists(_userSettingsPath))
        {
            using FileStream stream = new(_userSettingsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            UserSettingsDocument? document = JsonSerializer.Deserialize(
                stream,
                SettingsJsonContext.Default.UserSettingsDocument
            );

            settings =
                document?.OutbreakTracker
                ?? throw new InvalidOperationException(
                    "The user settings file does not contain an OutbreakTracker section."
                );
        }

        Validate(settings);
        return settings;
    }

    private static void Validate(OutbreakTrackerSettings settings)
    {
        if (!settings.TryValidate(out string? error))
            throw new InvalidOperationException(error);
    }

    private static async ValueTask WriteSettingsDocumentAsync(
        Stream destination,
        OutbreakTrackerSettings settings,
        CancellationToken cancellationToken
    )
    {
        if (destination.CanSeek)
        {
            destination.SetLength(0);
            destination.Position = 0;
        }

        UserSettingsDocument document = new() { OutbreakTracker = settings };

        await JsonSerializer
            .SerializeAsync(destination, document, SettingsJsonContext.Default.UserSettingsDocument, cancellationToken)
            .ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static OutbreakTrackerSettings DeserializeImportedSettings(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("The selected file must contain a JSON object at the root.");

        JsonElement settingsElement = TryGetSettingsSection(root, out JsonElement section) ? section : root;
        OutbreakTrackerSettings? settings = settingsElement.Deserialize(
            SettingsJsonContext.Default.OutbreakTrackerSettings
        );
        if (settings is null)
            throw new InvalidOperationException("The selected file does not contain valid tracker settings.");

        Validate(settings);
        return settings;
    }

    private static bool TryGetSettingsSection(JsonElement root, out JsonElement settingsSection)
    {
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, OutbreakTrackerSettings.SectionName, StringComparison.OrdinalIgnoreCase))
                continue;

            settingsSection = property.Value;
            return true;
        }

        settingsSection = default;
        return false;
    }
}
