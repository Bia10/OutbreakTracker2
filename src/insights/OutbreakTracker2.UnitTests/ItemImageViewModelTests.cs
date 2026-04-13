using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Common;
using OutbreakTracker2.Application.Views.Common.Item;

namespace OutbreakTracker2.UnitTests;

public sealed class ItemImageViewModelTests
{
    [Test]
    public async Task Dispose_UnsubscribesFromInnerImageViewModel()
    {
        ItemImageViewModel viewModel = new(NullLogger<ItemImageViewModel>.Instance, new StubImageViewModelFactory());
        ImageViewModel innerImageViewModel = GetInnerImageViewModel(viewModel);

        await Assert.That(GetPropertyChangedSubscriberCount(innerImageViewModel)).IsEqualTo(1);

        viewModel.Dispose();

        await Assert.That(GetPropertyChangedSubscriberCount(innerImageViewModel)).IsEqualTo(0);
    }

    private static ImageViewModel GetInnerImageViewModel(ItemImageViewModel viewModel)
    {
        PropertyInfo? property = typeof(ItemImageViewModel).GetProperty(
            "ImageViewModel",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        return property?.GetValue(viewModel) as ImageViewModel
            ?? throw new InvalidOperationException("Unable to access the inner ImageViewModel for testing.");
    }

    private static int GetPropertyChangedSubscriberCount(ImageViewModel imageViewModel)
    {
        Type? currentType = imageViewModel.GetType();

        while (currentType is not null)
        {
            FieldInfo? field = currentType.GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(imageViewModel) is PropertyChangedEventHandler handler)
                return handler.GetInvocationList().Length;

            currentType = currentType.BaseType;
        }

        return 0;
    }

    private sealed class StubImageViewModelFactory : IImageViewModelFactory
    {
        public ImageViewModel Create() =>
            new(NullLogger<ImageViewModel>.Instance, new MissingAtlasService(), new ImmediateDispatcherService());
    }

    private sealed class MissingAtlasService : ITextureAtlasService
    {
        public ITextureAtlas GetAtlas(string name) => null!;

        public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => new Dictionary<string, ITextureAtlas>();

        public Task LoadAtlasesAsync() => Task.CompletedTask;
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
        public bool CheckAccess() => true;

        public bool IsOnUIThread() => true;

        public void PostOnUI(Action action) => action();

        public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
            return Task.CompletedTask;
        }

        public Task<TResult?> InvokeOnUIAsync<TResult>(
            Func<TResult> action,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TResult?>(action());
        }
    }
}
