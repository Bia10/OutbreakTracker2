using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutbreakTracker2.Application.Pages;
using OutbreakTracker2.Application.Services;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.Dashboard.ClientOverview.EmbeddedGame;
using OutbreakTracker2.Application.Views.Settings;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Models;
using SukiUI.Toasts;

namespace OutbreakTracker2.Application.Views;

internal sealed partial class OutbreakTracker2ViewModel : ObservableObject, IDisposable
{
    public IAvaloniaReadOnlyList<PageBase> Pages { get; }
    public IAvaloniaReadOnlyList<SukiColorTheme> Themes { get; }

    public ISukiToastManager ToastManager { get; }
    public ISukiDialogManager DialogManager { get; }

    [ObservableProperty]
    private ThemeVariant _baseTheme;

    [ObservableProperty]
    private PageBase? _activePage;

    [ObservableProperty]
    private bool _windowLocked;

    [ObservableProperty]
    private bool _titleBarVisible = true;

    private readonly SukiTheme _theme;
    private readonly EmbeddedGameViewModel _embeddedGameViewModel;
    private readonly PageNavigationService _pageNavigationService;
    private readonly Action<Type> _onNavigationRequested;
    private readonly Action<ThemeVariant> _onBaseThemeChanged;
    private readonly Action<SukiColorTheme> _onColorThemeChanged;
    private bool _disposed;

    public OutbreakTracker2ViewModel(
        IEnumerable<PageBase> pages,
        PageNavigationService pageNavigationService,
        ISukiToastManager toastManager,
        ISukiDialogManager dialogManager,
        IAppSettingsService settingsService,
        EmbeddedGameViewModel embeddedGameViewModel
    )
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        _embeddedGameViewModel = embeddedGameViewModel;
        _pageNavigationService = pageNavigationService;
        Pages = new AvaloniaList<PageBase>(pages.AsValueEnumerable().OrderBy(x => x.Index).ToArray());
        _theme = SukiTheme.GetInstance();

        // Subscribe to the navigation service (when a page navigation is requested)
        _onNavigationRequested = pageType =>
        {
            PageBase? page = Pages.AsValueEnumerable().FirstOrDefault(pageBase => pageBase.GetType() == pageType);

            if (page is null || ActivePage?.GetType() == pageType)
                return;

            ActivePage = page;
        };
        pageNavigationService.NavigationRequested += _onNavigationRequested;

        Themes = _theme.ColorThemes;
        BaseTheme = _theme.ActiveBaseTheme;

        // Subscribe to the base theme changed events
        _onBaseThemeChanged = variant =>
        {
            BaseTheme = variant;
            ToastManager
                .CreateSimpleInfoToast()
                .WithTitle("Theme Changed")
                .WithContent($"Theme has changed to {variant}.")
                .Queue();
        };
        _theme.OnBaseThemeChanged += _onBaseThemeChanged;

        // Subscribe to the color theme changed events
        _onColorThemeChanged = theme =>
            ToastManager
                .CreateSimpleInfoToast()
                .WithTitle("Color Changed")
                .WithContent($"Color has changed to {theme.DisplayName}.")
                .Queue();
        _theme.OnColorThemeChanged += _onColorThemeChanged;

        OpenSettingsCommand = new RelayCommand(() =>
        {
            bool wasEmbedded = _embeddedGameViewModel.IsEmbedded;

            if (wasEmbedded)
            {
                _embeddedGameViewModel.RequestUnembedCommand.Execute(null);

                // Minimize the released PCSX2 window so it stays out of the way while the
                // settings dialog is open, without moving it to a different position.
                _embeddedGameViewModel.MinimizeTrackedWindow();
            }

            void OnDismissed(object? sender, SukiDialogManagerEventArgs e)
            {
                DialogManager.OnDialogDismissed -= OnDismissed;

                if (wasEmbedded && _embeddedGameViewModel.TrackedPid > 0)
                {
                    // Restore the minimized PCSX2 window before re-embedding so it is
                    // in a normal state when SetParent reparents it into the container.
                    _embeddedGameViewModel.RestoreTrackedWindow();

                    _embeddedGameViewModel.RequestEmbedCommand.Execute(null);
                }
            }

            DialogManager.OnDialogDismissed += OnDismissed;

            DialogManager
                .CreateDialog()
                .WithTitle("Settings")
                .WithViewModel(dialog => new AppSettingsDialogViewModel(settingsService, ToastManager, dialog), false)
                .Dismiss()
                .ByClickingBackground()
                .TryShow();
        });
    }

    public IRelayCommand OpenSettingsCommand { get; }

    [RelayCommand]
    private void ToggleBaseTheme() => _theme.SwitchBaseTheme();

    [RelayCommand]
    private void ToggleWindowLock()
    {
        WindowLocked = !WindowLocked;
        ToastManager
            .CreateSimpleInfoToast()
            .WithTitle(WindowLocked ? "Window Locked" : "Window Unlocked")
            .WithContent(WindowLocked ? "Window has been locked." : "Window has been unlocked.")
            .Queue();
    }

    [RelayCommand]
    private void ToggleTitleBar()
    {
        TitleBarVisible = !TitleBarVisible;
        ToastManager
            .CreateSimpleInfoToast()
            .WithTitle(TitleBarVisible ? "Title Bar Shown" : "Title Bar Hidden")
            .WithContent(TitleBarVisible ? "Window title bar has been shown." : "Window title bar has been hidden.")
            .Queue();
    }

    public void ChangeTheme(SukiColorTheme theme) => _theme.ChangeColorTheme(theme);

    [RelayCommand]
    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _pageNavigationService.NavigationRequested -= _onNavigationRequested;
        _theme.OnBaseThemeChanged -= _onBaseThemeChanged;
        _theme.OnColorThemeChanged -= _onColorThemeChanged;
        _disposed = true;
    }
}
