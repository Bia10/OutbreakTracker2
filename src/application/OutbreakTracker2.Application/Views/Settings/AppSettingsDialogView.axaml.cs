using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace OutbreakTracker2.Application.Views.Settings;

public partial class AppSettingsDialogView : UserControl
{
    private static readonly FilePickerFileType JsonFileType = new("JSON files")
    {
        Patterns = ["*.json"],
        MimeTypes = ["application/json"],
    };

    public AppSettingsDialogView()
    {
        InitializeComponent();
    }

    private async void OnImportSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AppSettingsDialogViewModel viewModel)
            return;

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            viewModel.NotifyFileDialogUnavailable("Import");
            return;
        }

        IReadOnlyList<IStorageFile> files = await topLevel
            .StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "Import tracker settings",
                    FileTypeFilter = [JsonFileType],
                }
            )
            .ConfigureAwait(true);

        IStorageFile? file = files.FirstOrDefault();
        if (file is null)
            return;

        Stream stream = await file.OpenReadAsync().ConfigureAwait(true);
        await using (stream.ConfigureAwait(false))
        {
            await viewModel.ImportAsync(stream).ConfigureAwait(true);
        }
    }

    private async void OnExportSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AppSettingsDialogViewModel viewModel)
            return;

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            viewModel.NotifyFileDialogUnavailable("Export");
            return;
        }

        IStorageFile? file = await topLevel
            .StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export tracker settings",
                    SuggestedFileName = Path.GetFileName(viewModel.UserSettingsPath),
                    DefaultExtension = "json",
                    ShowOverwritePrompt = true,
                    FileTypeChoices = [JsonFileType],
                }
            )
            .ConfigureAwait(true);

        if (file is null)
            return;

        Stream stream = await file.OpenWriteAsync().ConfigureAwait(true);
        await using (stream.ConfigureAwait(false))
        {
            await viewModel.ExportAsync(stream).ConfigureAwait(true);
        }
    }
}
