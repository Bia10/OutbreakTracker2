using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.MemoryWatcherIntegration;

namespace OutbreakTracker2.UnitTests;

public sealed class AppSettingsServiceTests
{
    [Test]
    public async Task ExportAsync_WritesCurrentEffectiveSettingsDocument()
    {
        using TestSettingsEnvironment environment = new();
        using AppSettingsService service = environment.CreateService();
        await using MemoryStream exportStream = new();

        await service.SaveAsync(
            new OutbreakTrackerSettings
            {
                Notifications = new NotificationSettings { EnableToastAlerts = false },
                Display = new DisplaySettings
                {
                    ShowGameplayUiDuringTransitions = true,
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = false },
                    ScenarioItemsDock = new ScenarioItemsDockSettings
                    {
                        OnlyShowCurrentPlayerRoom = false,
                        ProjectAllOntoMap = true,
                    },
                },
                AlertRules = new AlertRuleSettings
                {
                    Players = new PlayerAlertRuleSettings { VirusWarningThreshold = 62, VirusCriticalThreshold = 88 },
                    Lobby = new LobbyAlertRuleSettings
                    {
                        GameCreated = true,
                        NameMatchCreated = true,
                        NameMatchFilter = "Training",
                        ScenarioMatchCreated = true,
                        ScenarioMatchFilter = "Wild things",
                    },
                },
                RunReports = new RunReportSettings
                {
                    GenerateRunReports = true,
                    OutputDirectory = "custom-reports",
                    WriteMarkdown = true,
                    WriteCsv = false,
                    WriteHtml = true,
                },
                DataManager = new DataManagerSettings { FastUpdateIntervalMs = 111, SlowUpdateIntervalMs = 777 },
                MemoryWatcher = new MemoryWatcherSettings
                {
                    Backend = MemoryBackendMode.MemoryWatcher,
                    NativeLibraryPath = "C:\\mw\\MemoryWatcher.RemoteAot.dll",
                    AllowIntrusiveBackends = true,
                    EventBufferCapacity = 1024,
                    HashBlockSizeBytes = 128,
                    UseHashIndex = false,
                },
            }
        );

        await service.ExportAsync(exportStream);

        exportStream.Position = 0;
        using JsonDocument jsonDocument = await JsonDocument.ParseAsync(exportStream);
        JsonElement settingsElement = jsonDocument.RootElement.GetProperty("OutbreakTracker");

        await Assert
            .That(settingsElement.GetProperty("Notifications").GetProperty("EnableToastAlerts").GetBoolean())
            .IsFalse();
        await Assert
            .That(settingsElement.GetProperty("Display").GetProperty("ShowGameplayUiDuringTransitions").GetBoolean())
            .IsTrue();
        await Assert
            .That(
                settingsElement
                    .GetProperty("Display")
                    .GetProperty("EntitiesDock")
                    .GetProperty("OnlyShowCurrentPlayerRoom")
                    .GetBoolean()
            )
            .IsFalse();
        await Assert
            .That(
                settingsElement
                    .GetProperty("Display")
                    .GetProperty("ScenarioItemsDock")
                    .GetProperty("ProjectAllOntoMap")
                    .GetBoolean()
            )
            .IsTrue();
        await Assert
            .That(
                settingsElement
                    .GetProperty("Display")
                    .GetProperty("ScenarioItemsDock")
                    .GetProperty("OnlyShowCurrentPlayerRoom")
                    .GetBoolean()
            )
            .IsFalse();
        await Assert
            .That(
                settingsElement
                    .GetProperty("AlertRules")
                    .GetProperty("Players")
                    .GetProperty("VirusWarningThreshold")
                    .GetDouble()
            )
            .IsEqualTo(62);
        await Assert
            .That(
                settingsElement
                    .GetProperty("AlertRules")
                    .GetProperty("Players")
                    .GetProperty("VirusCriticalThreshold")
                    .GetDouble()
            )
            .IsEqualTo(88);
        await Assert
            .That(settingsElement.GetProperty("RunReports").GetProperty("OutputDirectory").GetString())
            .IsEqualTo("custom-reports");
        await Assert
            .That(settingsElement.GetProperty("DataManager").GetProperty("FastUpdateIntervalMs").GetInt32())
            .IsEqualTo(111);
        await Assert
            .That(settingsElement.GetProperty("DataManager").GetProperty("SlowUpdateIntervalMs").GetInt32())
            .IsEqualTo(777);
        await Assert
            .That(settingsElement.GetProperty("MemoryWatcher").GetProperty("Backend").GetString())
            .IsEqualTo("MemoryWatcher");
        await Assert
            .That(settingsElement.GetProperty("MemoryWatcher").GetProperty("EventBufferCapacity").GetInt32())
            .IsEqualTo(1024);
        await Assert
            .That(
                settingsElement
                    .GetProperty("AlertRules")
                    .GetProperty("Lobby")
                    .GetProperty("NameMatchCreated")
                    .GetBoolean()
            )
            .IsTrue();
        await Assert
            .That(
                settingsElement
                    .GetProperty("AlertRules")
                    .GetProperty("Lobby")
                    .GetProperty("NameMatchFilter")
                    .GetString()
            )
            .IsEqualTo("Training");
    }

    [Test]
    public async Task ImportAsync_PersistsImportedSettingsAndUpdatesCurrent()
    {
        using TestSettingsEnvironment environment = new();
        using AppSettingsService service = environment.CreateService();
        await using MemoryStream importStream = new(
            Encoding.UTF8.GetBytes(
                """
                {
                  "OutbreakTracker": {
                    "Display": {
                      "ShowGameplayUiDuringTransitions": true
                    },
                    "Notifications": {
                      "EnableToastAlerts": false
                    },
                    "AlertRules": {
                      "Players": {
                        "VirusWarningThreshold": 61,
                        "VirusCriticalThreshold": 89
                      },
                      "Lobby": {
                        "NameMatchCreated": true,
                        "NameMatchFilter": "Night",
                        "ScenarioMatchCreated": true,
                        "ScenarioMatchFilter": "Wild things"
                      }
                    },
                    "RunReports": {
                      "GenerateRunReports": true,
                      "OutputDirectory": "memory-reports",
                      "WriteMarkdown": false,
                      "WriteCsv": true,
                      "WriteHtml": true
                    },
                    "DataManager": {
                      "FastUpdateIntervalMs": 123,
                      "SlowUpdateIntervalMs": 456
                    },
                    "MemoryWatcher": {
                      "Backend": "MemoryWatcher",
                      "NativeLibraryPath": "/tmp/mw/libMemoryWatcher.RemoteAot.so",
                      "AllowIntrusiveBackends": true,
                      "EventBufferCapacity": 2048,
                      "HashBlockSizeBytes": 96,
                      "UseHashIndex": false
                    }
                  }
                }
                """
            )
        );

        OutbreakTrackerSettings importedSettings = await service.ImportAsync(importStream);

        await Assert.That(importedSettings.Notifications.EnableToastAlerts).IsFalse();
        await Assert.That(service.Current.Notifications.EnableToastAlerts).IsFalse();
        await Assert.That(service.Current.AlertRules.Players.VirusWarningThreshold).IsEqualTo(61);
        await Assert.That(service.Current.AlertRules.Players.VirusCriticalThreshold).IsEqualTo(89);
        await Assert.That(service.Current.AlertRules.Lobby.NameMatchCreated).IsTrue();
        await Assert.That(service.Current.AlertRules.Lobby.NameMatchFilter).IsEqualTo("Night");
        await Assert.That(service.Current.AlertRules.Lobby.ScenarioMatchCreated).IsTrue();
        await Assert.That(service.Current.AlertRules.Lobby.ScenarioMatchFilter).IsEqualTo("Wild things");
        await Assert.That(importedSettings.Display.ShowGameplayUiDuringTransitions).IsTrue();
        await Assert.That(service.Current.Display.ShowGameplayUiDuringTransitions).IsTrue();
        await Assert.That(importedSettings.Display.EntitiesDock.OnlyShowCurrentPlayerRoom).IsTrue();
        await Assert.That(service.Current.Display.EntitiesDock.OnlyShowCurrentPlayerRoom).IsTrue();
        await Assert.That(importedSettings.Display.ScenarioItemsDock.OnlyShowCurrentPlayerRoom).IsTrue();
        await Assert.That(service.Current.Display.ScenarioItemsDock.OnlyShowCurrentPlayerRoom).IsTrue();
        await Assert.That(importedSettings.Display.ScenarioItemsDock.ProjectAllOntoMap).IsFalse();
        await Assert.That(service.Current.Display.ScenarioItemsDock.ProjectAllOntoMap).IsFalse();
        await Assert.That(importedSettings.RunReports.OutputDirectory).IsEqualTo("memory-reports");
        await Assert.That(importedSettings.RunReports.WriteMarkdown).IsFalse();
        await Assert.That(importedSettings.RunReports.WriteCsv).IsTrue();
        await Assert.That(service.Current.RunReports.OutputDirectory).IsEqualTo("memory-reports");
        await Assert.That(service.Current.DataManager.FastUpdateIntervalMs).IsEqualTo(123);
        await Assert.That(service.Current.DataManager.SlowUpdateIntervalMs).IsEqualTo(456);
        await Assert.That(service.Current.MemoryWatcher.Backend).IsEqualTo(MemoryBackendMode.MemoryWatcher);
        await Assert
            .That(service.Current.MemoryWatcher.NativeLibraryPath)
            .IsEqualTo("/tmp/mw/libMemoryWatcher.RemoteAot.so");
        await Assert.That(service.Current.MemoryWatcher.AllowIntrusiveBackends).IsTrue();
        await Assert.That(service.Current.MemoryWatcher.EventBufferCapacity).IsEqualTo(2048);
        await Assert.That(service.Current.MemoryWatcher.HashBlockSizeBytes).IsEqualTo(96);
        await Assert.That(service.Current.MemoryWatcher.UseHashIndex).IsFalse();
        await Assert.That(File.Exists(environment.UserSettingsPath)).IsTrue();

        using JsonDocument jsonDocument = JsonDocument.Parse(await File.ReadAllTextAsync(environment.UserSettingsPath));
        await Assert
            .That(
                jsonDocument
                    .RootElement.GetProperty("OutbreakTracker")
                    .GetProperty("Notifications")
                    .GetProperty("EnableToastAlerts")
                    .GetBoolean()
            )
            .IsFalse();
    }

    [Test]
    public async Task ResetToDefaultsAsync_RemovesUserOverridesAndRestoresBundledDefaults()
    {
        using TestSettingsEnvironment environment = new();
        using AppSettingsService service = environment.CreateService();

        await service.SaveAsync(
            new OutbreakTrackerSettings
            {
                Notifications = new NotificationSettings { EnableToastAlerts = false },
                Display = new DisplaySettings
                {
                    ShowGameplayUiDuringTransitions = true,
                    EntitiesDock = new EntitiesDockSettings { OnlyShowCurrentPlayerRoom = false },
                    ScenarioItemsDock = new ScenarioItemsDockSettings
                    {
                        OnlyShowCurrentPlayerRoom = false,
                        ProjectAllOntoMap = true,
                    },
                },
                AlertRules = new AlertRuleSettings
                {
                    Players = new PlayerAlertRuleSettings { VirusWarningThreshold = 64, VirusCriticalThreshold = 93 },
                    Lobby = new LobbyAlertRuleSettings
                    {
                        NameMatchCreated = true,
                        NameMatchFilter = "City",
                        ScenarioMatchCreated = true,
                        ScenarioMatchFilter = "Decisions, decisions",
                    },
                },
            }
        );

        await Assert.That(service.Current.Notifications.EnableToastAlerts).IsFalse();

        OutbreakTrackerSettings resetSettings = await service.ResetToDefaultsAsync();

        await Assert.That(File.Exists(environment.UserSettingsPath)).IsFalse();
        await Assert.That(resetSettings.Notifications.EnableToastAlerts).IsTrue();
        await Assert.That(resetSettings.AlertRules.Players.VirusWarningThreshold).IsEqualTo(50);
        await Assert.That(resetSettings.AlertRules.Players.VirusCriticalThreshold).IsEqualTo(75);
        await Assert.That(resetSettings.AlertRules.Lobby.NameMatchCreated).IsFalse();
        await Assert.That(resetSettings.AlertRules.Lobby.NameMatchFilter).IsEqualTo(string.Empty);
        await Assert.That(resetSettings.AlertRules.Lobby.ScenarioMatchCreated).IsFalse();
        await Assert.That(resetSettings.AlertRules.Lobby.ScenarioMatchFilter).IsEqualTo(string.Empty);
        await Assert.That(resetSettings.Display.ShowGameplayUiDuringTransitions).IsFalse();
        await Assert.That(resetSettings.Display.EntitiesDock.OnlyShowCurrentPlayerRoom).IsTrue();
        await Assert.That(resetSettings.Display.ScenarioItemsDock.OnlyShowCurrentPlayerRoom).IsTrue();
        await Assert.That(resetSettings.Display.ScenarioItemsDock.ProjectAllOntoMap).IsFalse();
        await Assert.That(resetSettings.RunReports.OutputDirectory).IsEqualTo("reports");
        await Assert.That(resetSettings.DataManager.FastUpdateIntervalMs).IsEqualTo(250);
        await Assert.That(resetSettings.DataManager.SlowUpdateIntervalMs).IsEqualTo(500);
        await Assert.That(resetSettings.MemoryWatcher.Backend).IsEqualTo(MemoryBackendMode.MemoryWatcher);
        await Assert.That(resetSettings.MemoryWatcher.EventBufferCapacity).IsEqualTo(256);
        await Assert.That(resetSettings.MemoryWatcher.HashBlockSizeBytes).IsEqualTo(64);
        await Assert.That(service.Current.Notifications.EnableToastAlerts).IsTrue();
    }

    [Test]
    public async Task CreateService_LoadsUserOverrides_ForRunReportsDataManagerAndMemoryWatcher()
    {
        using TestSettingsEnvironment environment = new(
            """
            {
                "OutbreakTracker": {
                    "RunReports": {
                        "OutputDirectory": "overridden-reports",
                        "WriteCsv": false
                    },
                    "DataManager": {
                        "FastUpdateIntervalMs": 99,
                        "SlowUpdateIntervalMs": 199
                    },
                    "MemoryWatcher": {
                        "Backend": "MemoryWatcher",
                        "EventBufferCapacity": 4096,
                        "HashBlockSizeBytes": 160,
                        "UseHashIndex": false
                    }
                }
            }
            """
        );

        using AppSettingsService service = environment.CreateService();

        await Assert.That(service.Current.RunReports.OutputDirectory).IsEqualTo("overridden-reports");
        await Assert.That(service.Current.RunReports.WriteCsv).IsFalse();
        await Assert.That(service.Current.DataManager.FastUpdateIntervalMs).IsEqualTo(99);
        await Assert.That(service.Current.DataManager.SlowUpdateIntervalMs).IsEqualTo(199);
        await Assert.That(service.Current.MemoryWatcher.Backend).IsEqualTo(MemoryBackendMode.MemoryWatcher);
        await Assert.That(service.Current.MemoryWatcher.EventBufferCapacity).IsEqualTo(4096);
        await Assert.That(service.Current.MemoryWatcher.HashBlockSizeBytes).IsEqualTo(160);
        await Assert.That(service.Current.MemoryWatcher.UseHashIndex).IsFalse();
    }

    [Test]
    public async Task TryValidate_Fails_WhenLobbyNameMatchRuleIsEnabledWithoutAFilter()
    {
        OutbreakTrackerSettings settings = new()
        {
            AlertRules = new AlertRuleSettings
            {
                Lobby = new LobbyAlertRuleSettings { NameMatchCreated = true, NameMatchFilter = "   " },
            },
        };

        bool isValid = settings.TryValidate(out string? error);

        await Assert.That(isValid).IsFalse();
        await Assert
            .That(error)
            .IsEqualTo(
                "OutbreakTracker:AlertRules:Lobby:NameMatchFilter cannot be empty when NameMatchCreated is enabled."
            );
    }

    [Test]
    public async Task TryValidate_Fails_WhenRunReportGenerationIsEnabledWithoutAnyOutputFormat()
    {
        OutbreakTrackerSettings settings = new()
        {
            RunReports = new RunReportSettings
            {
                GenerateRunReports = true,
                WriteMarkdown = false,
                WriteCsv = false,
                WriteHtml = false,
            },
        };

        bool isValid = settings.TryValidate(out string? error);

        await Assert.That(isValid).IsFalse();
        await Assert
            .That(error)
            .IsEqualTo("OutbreakTracker:RunReports must enable at least one output format when generation is enabled.");
    }

    [Test]
    public async Task CreateService_Throws_WhenUserSettingsAlertRulesSectionIsNull()
    {
        using TestSettingsEnvironment environment = new(
            """
            {
                "OutbreakTracker": {
                    "AlertRules": null
                }
            }
            """
        );

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => environment.CreateService())!;

        await Assert.That(ex.Message).IsEqualTo("OutbreakTracker:AlertRules cannot be null.");
    }

    [Test]
    public async Task CreateService_Throws_WhenUserSettingsDocumentOmitsOutbreakTrackerSection()
    {
        using TestSettingsEnvironment environment = new(
            """
            {
                "OtherSection": {
                    "Enabled": true
                }
            }
            """
        );

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => environment.CreateService())!;

        await Assert.That(ex.Message).IsEqualTo("The user settings file must contain an OutbreakTracker object.");
    }

    [Test]
    public async Task SaveAsync_DoesNotReplaceLiveSettings_WhenSerializedDocumentFailsRoundTripValidation()
    {
        using TestSettingsEnvironment environment = new(
            """
            {
                "OutbreakTracker": {
                    "Notifications": {
                        "EnableToastAlerts": false
                    }
                }
            }
            """
        );
        using AppSettingsService service = environment.CreateService(serializer: new InvalidWriteSettingsSerializer());

        InvalidOperationException? ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.SaveAsync(
                new OutbreakTrackerSettings { Notifications = new NotificationSettings { EnableToastAlerts = true } }
            )
        )!;

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.Message).IsEqualTo("The user settings file must contain an OutbreakTracker object.");
        await Assert.That(service.Current.Notifications.EnableToastAlerts).IsFalse();

        using JsonDocument jsonDocument = JsonDocument.Parse(await File.ReadAllTextAsync(environment.UserSettingsPath));
        await Assert
            .That(
                jsonDocument
                    .RootElement.GetProperty("OutbreakTracker")
                    .GetProperty("Notifications")
                    .GetProperty("EnableToastAlerts")
                    .GetBoolean()
            )
            .IsFalse();
    }

    private sealed class TestSettingsEnvironment : IDisposable
    {
        private readonly string _directoryPath = Path.Combine(
            Path.GetTempPath(),
            $"OutbreakTracker2.SettingsTests.{Guid.NewGuid():N}"
        );

        private const string DefaultAppSettingsJson = """
            {
                "OutbreakTracker": {
                    "Notifications": {
                        "EnableToastAlerts": true
                    },
                    "Display": {
                        "EntitiesDock": {
                            "OnlyShowCurrentPlayerRoom": true
                        },
                        "ScenarioItemsDock": {
                            "OnlyShowCurrentPlayerRoom": true
                        }
                    },
                    "AlertRules": {
                        "Players": {
                            "VirusWarningEnabled": true,
                            "VirusWarningThreshold": 50,
                            "VirusCriticalEnabled": true,
                            "VirusCriticalThreshold": 75
                        },
                        "Lobby": {
                            "GameCreated": true,
                            "NameMatchCreated": false,
                            "NameMatchFilter": "",
                            "ScenarioMatchCreated": false,
                            "ScenarioMatchFilter": ""
                        }
                    },
                    "RunReports": {
                        "GenerateRunReports": true,
                        "OutputDirectory": "reports",
                        "WriteMarkdown": true,
                        "WriteCsv": true,
                        "WriteHtml": true
                    },
                    "DataManager": {
                        "FastUpdateIntervalMs": 250,
                        "SlowUpdateIntervalMs": 500
                    },
                    "MemoryWatcher": {
                        "Backend": "Legacy",
                        "PreferredBackend": "Auto",
                        "PreferredPrecision": "SnapshotBitExact",
                        "AllowFallback": true,
                        "NativeLibraryPath": "",
                        "AllowIntrusiveBackends": false,
                        "EventBufferCapacity": 256,
                        "HashBlockSizeBytes": 64,
                        "UseHashIndex": true
                    }
                }
            }
            """;

        public string UserSettingsPath => Path.Combine(_directoryPath, "user-settings.json");

        public TestSettingsEnvironment(string? userSettingsJson = null)
        {
            Directory.CreateDirectory(_directoryPath);
            File.WriteAllText(Path.Combine(_directoryPath, "appsettings.json"), DefaultAppSettingsJson);

            if (!string.IsNullOrWhiteSpace(userSettingsJson))
                File.WriteAllText(UserSettingsPath, userSettingsJson);
        }

        public AppSettingsService CreateService()
        {
            IConfigurationRoot configuration = CreateConfiguration();

            return new AppSettingsService(configuration, NullLogger<AppSettingsService>.Instance, UserSettingsPath);
        }

        public AppSettingsService CreateService(
            ISettingsSerializer? serializer = null,
            ISettingsValidator? validator = null,
            ISettingsPersistence? persistence = null
        )
        {
            IConfigurationRoot configuration = CreateConfiguration();

            return new AppSettingsService(
                configuration,
                NullLogger<AppSettingsService>.Instance,
                serializer ?? new SettingsJsonSerializer(),
                validator ?? new SettingsValidator(),
                persistence ?? new FileSettingsPersistence(UserSettingsPath)
            );
        }

        public IConfigurationRoot CreateConfiguration() =>
            new ConfigurationBuilder()
                .SetBasePath(_directoryPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("user-settings.json", optional: true, reloadOnChange: false)
                .Build();

        public void Dispose()
        {
            if (Directory.Exists(_directoryPath))
                Directory.Delete(_directoryPath, recursive: true);
        }
    }

    private sealed class InvalidWriteSettingsSerializer : ISettingsSerializer
    {
        private readonly SettingsJsonSerializer _inner = new();

        public async ValueTask SerializeAsync(
            Stream destination,
            OutbreakTrackerSettings settings,
            CancellationToken cancellationToken
        )
        {
            ArgumentNullException.ThrowIfNull(destination);

            if (destination.CanSeek)
            {
                destination.SetLength(0);
                destination.Position = 0;
            }

            await using StreamWriter writer = new(destination, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync("{}".AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public OutbreakTrackerSettings DeserializeSettings(JsonElement settingsElement) =>
            _inner.DeserializeSettings(settingsElement);

        public bool TryGetSettingsSection(JsonElement root, out JsonElement settingsSection) =>
            _inner.TryGetSettingsSection(root, out settingsSection);
    }
}
