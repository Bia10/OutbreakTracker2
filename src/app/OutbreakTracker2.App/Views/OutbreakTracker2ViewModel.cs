using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OutbreakTracker2.App.Pages;
using OutbreakTracker2.App.Services;
using OutbreakTracker2.App.Utilities;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Enums;
using SukiUI.Models;
using SukiUI.Theme.Shadcn;
using SukiUI.Toasts;
using ZLinq;

namespace OutbreakTracker2.App.Views;

internal partial class OutbreakTracker2ViewModel : ObservableObject
{
    public IAvaloniaReadOnlyList<PageBase> Pages { get; }
    public IAvaloniaReadOnlyList<SukiColorTheme> Themes { get; }
    public IAvaloniaReadOnlyList<SukiBackgroundStyle> BackgroundStyles { get; }

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

    [ObservableProperty]
    private SukiBackgroundStyle _backgroundStyle = SukiBackgroundStyle.GradientSoft;

    [ObservableProperty]
    private bool _animationsEnabled;

    [ObservableProperty]
    private string? _customShaderFile;

    [ObservableProperty]
    private bool _transitionsEnabled;

    [ObservableProperty]
    private double _transitionTime;

    [ObservableProperty]
    private bool _showTitleBar = true;

    [ObservableProperty]
    private bool _showBottomBar = true;

    private readonly SukiTheme _theme;

    public OutbreakTracker2ViewModel(
        IEnumerable<PageBase> demoPages,
        PageNavigationService pageNavigationService,
        ISukiToastManager toastManager,
        ISukiDialogManager dialogManager)
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        Pages = new AvaloniaList<PageBase>(demoPages.OrderBy(x => x.Index).ThenBy(x => x.DisplayName));
        BackgroundStyles = new AvaloniaList<SukiBackgroundStyle>(Enum.GetValues<SukiBackgroundStyle>());
        _theme = SukiTheme.GetInstance();

        // Subscribe to the navigation service (when a page navigation is requested)
        pageNavigationService.NavigationRequested += pageType =>
        {
            PageBase? page = Pages.AsValueEnumerable()
            .FirstOrDefault(pageBase => pageBase.GetType() == pageType);

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
            ToastManager.CreateSimpleInfoToast()
                .WithTitle("Theme Changed")
                .WithContent($"Theme has changed to {variant}.")
                .Queue();
        };

        // Subscribe to the color theme changed events
        _theme.OnColorThemeChanged += theme => ToastManager.CreateSimpleInfoToast()
            .WithTitle("Color Changed")
            .WithContent($"Color has changed to {theme.DisplayName}.")
            .Queue();
    }

    [RelayCommand]
    private void ToggleAnimations()
    {
        AnimationsEnabled = !AnimationsEnabled;
        ToastManager.CreateSimpleInfoToast()
            .WithTitle(AnimationsEnabled ? "Animation Enabled" : "Animation Disabled")
            .WithContent(AnimationsEnabled ? "Background animations are now enabled." : "Background animations are now disabled.")
            .Queue();
    }

    [RelayCommand]
    private void ToggleTransitions()
    {
        TransitionsEnabled = !TransitionsEnabled;
        ToastManager.CreateSimpleInfoToast()
            .WithTitle(TransitionsEnabled ? "Transitions Enabled" : "Transitions Disabled")
            .WithContent(TransitionsEnabled ? "Background transitions are now enabled." : "Background transitions are now disabled.")
            .Queue();
    }

    [RelayCommand]
    private void ToggleBaseTheme()
        => _theme.SwitchBaseTheme();

    public void ChangeTheme(SukiColorTheme theme)
        => _theme.ChangeColorTheme(theme);

    [RelayCommand]
    private void ShadCnMode()
    {
        if (Application.Current is not null)
            Shadcn.Configure(Application.Current, Application.Current.ActualThemeVariant);
        else
            ToastManager.CreateToast()
                .WithTitle("Configuration Error")
                .WithContent("Application or ThemeVariant is null. Unable to configure Shadcn mode.")
                .Queue();
    }

    [RelayCommand]
    private void ToggleWindowLock()
    {
        WindowLocked = !WindowLocked;
        ToastManager.CreateSimpleInfoToast()
            .WithTitle($"Window {(WindowLocked ? "Locked" : "Unlocked")}")
            .WithContent($"Window has been {(WindowLocked ? "locked" : "unlocked")}.")
            .Queue();
    }

    [RelayCommand]
    private void ToggleTitleBackground()
    {
        ShowTitleBar = !ShowTitleBar;
        ShowBottomBar = !ShowBottomBar;
    }

    [RelayCommand]
    private void ToggleTitleBar()
    {
        TitleBarVisible = !TitleBarVisible;
        ToastManager.CreateSimpleInfoToast()
            .WithTitle($"Title Bar {(TitleBarVisible ? "Visible" : "Hidden")}")
            .WithContent($"Window title bar has been {(TitleBarVisible ? "shown" : "hidden")}.")
            .Queue();
    }

    [RelayCommand]
    private void ToggleRightToLeft()
        => _theme.IsRightToLeft = !_theme.IsRightToLeft;

    [RelayCommand]
    private static void OpenUrl(string url)
        => UrlUtilities.OpenUrl(url);
}
