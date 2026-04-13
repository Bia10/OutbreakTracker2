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
    private readonly ISettingsSerializer _settingsSerializer;
    private readonly ISettingsValidator _settingsValidator;
    private readonly ISettingsPersistence _settingsPersistence;
    private readonly IDisposable _reloadRegistration;
    private readonly ReactiveProperty<OutbreakTrackerSettings> _settings;

    public AppSettingsService(IConfiguration configuration, ILogger<AppSettingsService> logger)
        : this(
            configuration,
            logger,
            new SettingsJsonSerializer(),
            new SettingsValidator(),
            new FileSettingsPersistence(AppSettingsFilePaths.GetUserSettingsPath())
        ) { }

    public AppSettingsService(IConfiguration configuration, ILogger<AppSettingsService> logger, string userSettingsPath)
        : this(
            configuration,
            logger,
            new SettingsJsonSerializer(),
            new SettingsValidator(),
            new FileSettingsPersistence(userSettingsPath)
        ) { }

    internal AppSettingsService(
        IConfiguration configuration,
        ILogger<AppSettingsService> logger,
        ISettingsSerializer settingsSerializer,
        ISettingsValidator settingsValidator,
        ISettingsPersistence settingsPersistence
    )
    {
        _configurationRoot =
            configuration as IConfigurationRoot
            ?? throw new InvalidOperationException("App settings require an IConfigurationRoot instance.");
        _logger = logger;
        _settingsSerializer = settingsSerializer;
        _settingsValidator = settingsValidator;
        _settingsPersistence = settingsPersistence;

        OutbreakTrackerSettings currentSettings = LoadValidatedSettings();
        _settings = new ReactiveProperty<OutbreakTrackerSettings>(currentSettings);
        _reloadRegistration = ChangeToken.OnChange(_configurationRoot.GetReloadToken, ReloadSettings);
    }

    public string UserSettingsPath => _settingsPersistence.UserSettingsPath;

    public OutbreakTrackerSettings Current => _settings.Value;

    public Observable<OutbreakTrackerSettings> SettingsObservable => _settings;

    public async ValueTask SaveAsync(OutbreakTrackerSettings settings, CancellationToken cancellationToken = default)
    {
        settings = NormalizeSettings(settings);
        _settingsValidator.ValidateSettings(settings);

        _settingsPersistence.EnsureDirectoryExists();

        FileStream stream = _settingsPersistence.OpenWrite();
        await using (stream.ConfigureAwait(false))
        {
            await _settingsSerializer.SerializeAsync(stream, settings, cancellationToken).ConfigureAwait(false);
        }

        _configurationRoot.Reload();
        _settings.Value = LoadValidatedSettings();
    }

    public async ValueTask ExportAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new InvalidOperationException("The selected export destination cannot be written to.");

        await _settingsSerializer.SerializeAsync(destination, Current, cancellationToken).ConfigureAwait(false);
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

            OutbreakTrackerSettings settings = NormalizeSettings(DeserializeImportedSettings(jsonDocument.RootElement));
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

        _settingsPersistence.Delete();

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
        OutbreakTrackerSettings settings = NormalizeSettings(
            _configurationRoot.GetSection(OutbreakTrackerSettings.SectionName).Get<OutbreakTrackerSettings>()
                ?? new OutbreakTrackerSettings()
        );

        if (_settingsPersistence.Exists())
        {
            using FileStream stream = _settingsPersistence.OpenRead();
            using JsonDocument document = JsonDocument.Parse(stream);
            JsonElement userSettingsElement = GetRequiredUserSettingsSection(document.RootElement);
            _settingsValidator.ValidateOverridesElement(userSettingsElement, OutbreakTrackerSettings.SectionName);

            OutbreakTrackerSettings userSettings = _settingsSerializer.DeserializeSettings(userSettingsElement);

            settings = MergeSettings(settings, userSettings);
        }

        _settingsValidator.ValidateSettings(settings);
        return settings;
    }

    private static OutbreakTrackerSettings NormalizeSettings(OutbreakTrackerSettings? settings) =>
        MergeSettings(new OutbreakTrackerSettings(), settings);

    private static OutbreakTrackerSettings MergeSettings(
        OutbreakTrackerSettings defaults,
        OutbreakTrackerSettings? overrides
    )
    {
        if (overrides is null)
            return defaults;

        return defaults with
        {
            Notifications = overrides.Notifications ?? defaults.Notifications,
            Display = MergeSettings(defaults.Display, overrides.Display),
            AlertRules = MergeSettings(defaults.AlertRules, overrides.AlertRules),
        };
    }

    private static DisplaySettings MergeSettings(DisplaySettings defaults, DisplaySettings? overrides)
    {
        if (overrides is null)
            return defaults;

        return defaults with
        {
            ShowGameplayUiDuringTransitions = overrides.ShowGameplayUiDuringTransitions,
            EntitiesDock = overrides.EntitiesDock ?? defaults.EntitiesDock,
            ScenarioItemsDock = overrides.ScenarioItemsDock ?? defaults.ScenarioItemsDock,
        };
    }

    private static AlertRuleSettings MergeSettings(AlertRuleSettings defaults, AlertRuleSettings? overrides)
    {
        if (overrides is null)
            return defaults;

        return defaults with
        {
            Players = overrides.Players ?? defaults.Players,
            Enemies = overrides.Enemies ?? defaults.Enemies,
            Doors = overrides.Doors ?? defaults.Doors,
            Lobby = overrides.Lobby ?? defaults.Lobby,
        };
    }

    private OutbreakTrackerSettings DeserializeImportedSettings(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("The selected file must contain a JSON object at the root.");

        JsonElement settingsElement = _settingsSerializer.TryGetSettingsSection(root, out JsonElement section)
            ? section
            : root;
        _settingsValidator.ValidateOverridesElement(settingsElement, OutbreakTrackerSettings.SectionName);

        OutbreakTrackerSettings settings = _settingsSerializer.DeserializeSettings(settingsElement);

        settings = NormalizeSettings(settings);
        _settingsValidator.ValidateSettings(settings);
        return settings;
    }

    private JsonElement GetRequiredUserSettingsSection(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("The user settings file must contain a JSON object at the root.");

        if (!_settingsSerializer.TryGetSettingsSection(root, out JsonElement settingsSection))
            throw new InvalidOperationException("The user settings file must contain an OutbreakTracker object.");

        return settingsSection;
    }
}
