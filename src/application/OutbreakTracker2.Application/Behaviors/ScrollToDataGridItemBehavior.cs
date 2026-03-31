using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace OutbreakTracker2.Application.Behaviors;

public class ScrollToDataGridItemBehavior : Behavior<DataGrid>
{
    public static readonly StyledProperty<object?> ItemToScrollToProperty = AvaloniaProperty.Register<
        ScrollToDataGridItemBehavior,
        object?
    >(nameof(ItemToScrollTo), defaultValue: null, inherits: false, (BindingMode)BindingPriority.LocalValue);

    public object? ItemToScrollTo
    {
        get => GetValue(ItemToScrollToProperty);
        set => SetValue(ItemToScrollToProperty, value);
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

        if (item is not null && dataGrid is not null)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (!dataGrid.Columns.Any())
                        return;

                    DataGridColumn? firstColumn = dataGrid.Columns.FirstOrDefault();

                    try
                    {
                        dataGrid.ScrollIntoView(item, firstColumn);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error scrolling DataGrid: {ex}");
                    }
                },
                DispatcherPriority.Render
            );
        }
    }
}
