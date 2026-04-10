using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutbreakTracker2.Application.Pages;
using OutbreakTracker2.Application.Services;
using OutbreakTracker2.Application.Services.Settings;
using OutbreakTracker2.Application.Views.Settings;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Models;
using SukiUI.Toasts;

namespace OutbreakTracker2.Application.Views;

internal sealed partial class OutbreakTracker2ViewModel : ObservableObject
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

    public OutbreakTracker2ViewModel(
        IEnumerable<PageBase> pages,
        PageNavigationService pageNavigationService,
        ISukiToastManager toastManager,
        ISukiDialogManager dialogManager,
        IAppSettingsService settingsService
    )
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        Pages = new AvaloniaList<PageBase>(pages.AsValueEnumerable().OrderBy(x => x.Index).ToArray());
        _theme = SukiTheme.GetInstance();

        // Subscribe to the navigation service (when a page navigation is requested)
        pageNavigationService.NavigationRequested += pageType =>
        {
            PageBase? page = Pages.AsValueEnumerable().FirstOrDefault(pageBase => pageBase.GetType() == pageType);

            if (page is null || ActivePage?.GetType() == pageType)
                return;

            ActivePage = page;
        };

        Themes = _theme.ColorThemes;
        BaseTheme = _theme.ActiveBaseTheme;

        // Subscribe to the base theme changed events
        _theme.OnBaseThemeChanged += variant =>
        {
            BaseTheme = variant;
            ToastManager
                .CreateSimpleInfoToast()
                .WithTitle("Theme Changed")
                .WithContent($"Theme has changed to {variant}.")
                .Queue();
        };

        // Subscribe to the color theme changed events
        _theme.OnColorThemeChanged += theme =>
            ToastManager
                .CreateSimpleInfoToast()
                .WithTitle("Color Changed")
                .WithContent($"Color has changed to {theme.DisplayName}.")
                .Queue();

        OpenSettingsCommand = new RelayCommand(() =>
        {
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
}
