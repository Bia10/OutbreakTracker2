using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using System;
using System.Linq;

namespace OutbreakTracker2.App.Behaviors;

public class ScrollToDataGridItemBehavior : Behavior<DataGrid>
{
    public static readonly StyledProperty<object?> ItemToScrollToProperty
        = AvaloniaProperty.Register<ScrollToDataGridItemBehavior, object?>
            (nameof(ItemToScrollTo), null, false, (BindingMode)BindingPriority.LocalValue);

    public object? ItemToScrollTo
    {
        get => GetValue(ItemToScrollToProperty);
        set => SetValue(ItemToScrollToProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not null)
        {
            this.GetObservable(ItemToScrollToProperty).Subscribe(OnItemToScrollToChanged);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
    }

    private void OnItemToScrollToChanged(object? item)
    {
        DataGrid? dataGrid = AssociatedObject;

        if (item is not null && dataGrid is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!dataGrid.Columns.Any()) return;

                DataGridColumn? firstColumn = dataGrid.Columns.FirstOrDefault();

                try
                {
                    dataGrid.ScrollIntoView(item, firstColumn);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scrolling DataGrid: {ex.Message}");
                }
            }, DispatcherPriority.Render);
        }
    }
}