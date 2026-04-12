using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace OutbreakTracker2.Application.Behaviors;

public sealed class ScrollToDataGridItemBehavior : Behavior<DataGrid>
{
    public static readonly StyledProperty<object?> ItemToScrollToProperty = AvaloniaProperty.Register<
        ScrollToDataGridItemBehavior,
        object?
    >(nameof(ItemToScrollTo), defaultValue: null, inherits: false, (BindingMode)BindingPriority.LocalValue);

    // Cancelled in OnDetaching() to prevent the Render-priority post from executing
    // against a DataGrid that has already been removed from the visual tree.
    private CancellationTokenSource? _cts;

    public object? ItemToScrollTo
    {
        get => GetValue(ItemToScrollToProperty);
        set => SetValue(ItemToScrollToProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        _cts = new CancellationTokenSource();
    }

    protected override void OnDetaching()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        base.OnDetaching();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemToScrollToProperty)
            OnItemToScrollToChanged(change.GetNewValue<object?>());
    }

    private void OnItemToScrollToChanged(object? item)
    {
        DataGrid? dataGrid = AssociatedObject;
        CancellationToken token = _cts?.Token ?? CancellationToken.None;

        if (item is not null && dataGrid is not null)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    if (!dataGrid.Columns.Any())
                        return;

                    DataGridColumn? firstColumn = dataGrid.Columns.FirstOrDefault();

                    try
                    {
                        dataGrid.ScrollIntoView(item, firstColumn);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning($"Error scrolling DataGrid: {ex}");
                    }
                },
                DispatcherPriority.Render
            );
        }
    }
}
