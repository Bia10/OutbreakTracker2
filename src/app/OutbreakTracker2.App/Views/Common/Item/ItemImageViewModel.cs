using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Views.Common.Item;

public class ItemImageViewModel : ObservableObject
{
    private readonly ILogger<ItemImageViewModel> _logger;

    private ImageViewModel ImageViewModel { get; }

    public CroppedBitmap? ItemImage => ImageViewModel.SourceImage;

    public ItemImageViewModel(
        ILogger<ItemImageViewModel> logger,
        IImageViewModelFactory imageViewModelFactory)
    {
        _logger = logger;
        ImageViewModel = imageViewModelFactory.Create();

        ImageViewModel.PropertyChanged += OnImageViewModelSourceImageChanged;

        _logger.LogInformation("ItemImageViewModel initialized");
    }

    public ValueTask UpdateImageAsync(string itemName)
    {
        _logger.LogDebug("Requesting item image update for item: {SpriteName}", itemName);

        return ImageViewModel.UpdateImageAsync(itemName, $"Item Image for {itemName}");
    }

    private void OnImageViewModelSourceImageChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (sender is not ImageViewModel _)
        {
            _logger.LogWarning("[{MethodName}] Unexpected sender: {Sender}",
                nameof(OnImageViewModelSourceImageChanged), sender?.GetType());
            return;
        }

        if (string.IsNullOrEmpty(eventArgs.PropertyName))
        {
            _logger.LogWarning("[{MethodName}] PropertyChangedEventArgs.PropertyName is null or empty: {PropertyName}",
                nameof(OnImageViewModelSourceImageChanged), eventArgs.PropertyName);
            return;
        }

        if (eventArgs.PropertyName.Equals(nameof(ImageViewModel.SourceImage), StringComparison.Ordinal))
            OnPropertyChanged(nameof(ItemImage));
    }
}