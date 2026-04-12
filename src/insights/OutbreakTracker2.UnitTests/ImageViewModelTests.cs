using Microsoft.Extensions.Logging.Abstractions;
using OutbreakTracker2.Application.Services.Atlas;
using OutbreakTracker2.Application.Services.Dispatcher;
using OutbreakTracker2.Application.Views.Common;

namespace OutbreakTracker2.UnitTests;

public sealed class ImageViewModelTests
{
    [Test]
    public async Task Constructor_DoesNotThrow_WhenAtlasesAreMissing()
    {
        ImageViewModel viewModel = new(
            NullLogger<ImageViewModel>.Instance,
            new MissingAtlasService(),
            new ImmediateDispatcherService()
        );

        await Assert.That(viewModel.SourceImage).IsNull();
    }

    [Test]
    public async Task UpdateImageAsync_MissingItemAtlas_LeavesImageEmpty()
    {
        ImageViewModel viewModel = new(
            NullLogger<ImageViewModel>.Instance,
            new MissingAtlasService(),
            new ImmediateDispatcherService()
        );

        await viewModel.UpdateImageAsync("FileTwo/Handgun", "test-item-atlas");

        await Assert.That(viewModel.SourceImage).IsNull();
    }

    [Test]
    public async Task UpdateImageAsync_MissingUiAtlas_LeavesImageEmpty()
    {
        ImageViewModel viewModel = new(
            NullLogger<ImageViewModel>.Instance,
            new MissingAtlasService(),
            new ImmediateDispatcherService()
        );

        await viewModel.UpdateImageAsync("bustAlyssa", "test-ui-atlas");

        await Assert.That(viewModel.SourceImage).IsNull();
    }

    private sealed class MissingAtlasService : ITextureAtlasService
    {
        public ITextureAtlas GetAtlas(string name) => null!;

        public IReadOnlyDictionary<string, ITextureAtlas> GetAllAtlases() => new Dictionary<string, ITextureAtlas>();

        public Task LoadAtlasesAsync() => Task.CompletedTask;
    }

    private sealed class ImmediateDispatcherService : IDispatcherService
    {
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
