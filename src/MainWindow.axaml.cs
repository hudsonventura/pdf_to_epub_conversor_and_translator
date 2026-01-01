using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BookTranslator.ViewModels;
using System.Linq;
using System;

namespace BookTranslator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Wire up ViewModel
        var vm = new MainWindowViewModel();
        DataContext = vm;

        // Inject file dialog logic with last directory support
        vm.ShowOpenFileDialog = async (lastDirectory) =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return null;

            IStorageFolder? startLocation = null;
            if (!string.IsNullOrEmpty(lastDirectory))
            {
                try
                {
                    startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(lastDirectory);
                }
                catch
                {
                    // Ignore if folder doesn't exist
                }
            }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select PDF",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.Pdf },
                SuggestedStartLocation = startLocation
            });

            var file = files.FirstOrDefault();
            if (file == null) return null;

            // Handle file path (Avalonia 11+ way)
            if (file.Path.IsAbsoluteUri)
            {
                return file.Path.LocalPath;
            }
            return file.Path.ToString();
        };

        // Inject folder dialog logic for output directory
        vm.ShowSelectFolderDialog = async (lastDirectory) =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return null;

            IStorageFolder? startLocation = null;
            if (!string.IsNullOrEmpty(lastDirectory))
            {
                try
                {
                    startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(lastDirectory);
                }
                catch
                {
                    // Ignore if folder doesn't exist
                }
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Output Folder",
                AllowMultiple = false,
                SuggestedStartLocation = startLocation
            });

            var folder = folders.FirstOrDefault();
            if (folder == null) return null;

            if (folder.Path.IsAbsoluteUri)
            {
                return folder.Path.LocalPath;
            }
            return folder.Path.ToString();
        };

        // Wire up About button
        var aboutButton = this.FindControl<Button>("AboutButton");
        if (aboutButton != null)
        {
            aboutButton.Click += async (s, e) =>
            {
                var aboutWindow = new AboutWindow();
                await aboutWindow.ShowDialog(this);
            };
        }
    }
}