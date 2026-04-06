using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Dock.Controls.ProportionalStackPanel;

namespace OutbreakTracker2.Application.Behaviors;

/// <summary>
/// When placed inside a control template, collapses the direct
/// <see cref="ProportionalStackPanel"/> slot that contains the TemplatedParent
/// whenever that parent's <see cref="Visual.IsVisibleProperty"/> is false.
///
/// Initialization is deferred to <c>AttachedToVisualTree</c> because Avalonia
/// sets <c>TemplatedParent</c> on template children AFTER <c>Build()</c>
/// returns, so the property is still null inside <c>OnAttached()</c>.
/// </summary>
public sealed class CollapseInPspWhenHiddenBehavior : Behavior<Control>
{
    private Control? _tracked;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is null)
            return;

        // TemplatedParent is null here — Avalonia assigns it after Build().
        // Wait for AttachedToVisualTree; by then the template has been fully
        // wired up and TemplatedParent points to the ToolDockControl.
        AssociatedObject.AttachedToVisualTree += OnAssociatedAttachedToVisualTree;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
            AssociatedObject.AttachedToVisualTree -= OnAssociatedAttachedToVisualTree;

        if (_tracked is not null)
        {
            _tracked.PropertyChanged -= OnTrackedPropertyChanged;
            SetPspChildCollapsed(_tracked, collapsed: false);
            _tracked = null;
        }

        base.OnDetaching();
    }

    private void OnAssociatedAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (AssociatedObject is null)
            return;

        // TemplatedParent is now correctly set.
        var newTracked = (AssociatedObject.TemplatedParent as Control) ?? AssociatedObject;

        if (_tracked == newTracked)
        {
            SyncCollapse();
            return;
        }

        if (_tracked is not null)
            _tracked.PropertyChanged -= OnTrackedPropertyChanged;

        _tracked = newTracked;
        _tracked.PropertyChanged += OnTrackedPropertyChanged;
        SyncCollapse();
    }

    private void OnTrackedPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty)
            SyncCollapse();
    }

    private void SyncCollapse()
    {
        if (_tracked is null)
            return;

        SetPspChildCollapsed(_tracked, collapsed: !_tracked.IsVisible);
    }

    private static void SetPspChildCollapsed(Control start, bool collapsed)
    {
        Control? pspChild = FindDirectPspChild(start);
        if (pspChild is not null)
            ProportionalStackPanel.SetIsCollapsed(pspChild, collapsed);
    }

    /// <summary>
    /// Walks the VISUAL tree from <paramref name="start"/> and returns the
    /// first ancestor whose visual parent is a <see cref="ProportionalStackPanel"/>.
    /// Must use visual parent (not logical <c>.Parent</c>) because the
    /// <c>ContentPresenter</c>'s logical parent is the owning <c>ItemsControl</c>,
    /// whereas its visual parent IS the <see cref="ProportionalStackPanel"/>.
    /// </summary>
    private static Control? FindDirectPspChild(Control? start)
    {
        for (var current = start; current is not null; current = current.GetVisualParent() as Control)
        {
            if (current.GetVisualParent() is ProportionalStackPanel)
                return current;
        }

        return null;
    }
}
