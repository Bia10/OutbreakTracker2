using Avalonia.Controls;
using Avalonia.Interactivity;
using SukiUI.Controls;
using SukiUI.Enums;
using SukiUI.Models;
using System.Runtime.CompilerServices;

namespace OutbreakTracker2.App.Views;

public partial class OutbreakTracker2View : SukiWindow
{
    public OutbreakTracker2View()
    {
        InitializeComponent();

        if (RuntimeFeature.IsDynamicCodeCompiled is false)
            Title += " (native)";
    }

    private void ThemeMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OutbreakTracker2ViewModel vm) return;
        if (e.Source is not MenuItem mItem) return;
        if (mItem.DataContext is not SukiColorTheme cTheme) return;

        vm.ChangeTheme(cTheme);
    }

    private void BackgroundMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OutbreakTracker2ViewModel vm) return;
        if (e.Source is not MenuItem mItem) return;
        if (mItem.DataContext is not SukiBackgroundStyle cStyle) return;

        vm.BackgroundStyle = cStyle;
    }
}