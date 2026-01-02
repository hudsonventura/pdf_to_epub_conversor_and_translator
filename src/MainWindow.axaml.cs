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
        vm.ShowOpenFilesDialog = async (lastDirectory) =>
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
                Title = "Select PDF Files",
                AllowMultiple = true,
                FileTypeFilter = new[] { FilePickerFileTypes.Pdf },
                SuggestedStartLocation = startLocation
            });

            if (files == null || files.Count == 0) return null;

            // Convert to array of file paths
            return files.Select(f => f.Path.IsAbsoluteUri ? f.Path.LocalPath : f.Path.ToString()).ToArray();
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

        // Inject file dialog logic for EPUB files (Send to Kindle)
        vm.ShowOpenEpubFilesDialog = async (lastDirectory) =>
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

            // Define EPUB file type
            var epubFileType = new FilePickerFileType("EPUB Files")
            {
                Patterns = new[] { "*.epub" },
                MimeTypes = new[] { "application/epub+zip" }
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select EPUB Files to Send to Kindle",
                AllowMultiple = true,
                FileTypeFilter = new[] { epubFileType },
                SuggestedStartLocation = startLocation
            });

            if (files == null || files.Count == 0) return null;

            // Convert to array of file paths
            return files.Select(f => f.Path.IsAbsoluteUri ? f.Path.LocalPath : f.Path.ToString()).ToArray();
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

        // Wire up SMTP Settings button
        var smtpSettingsButton = this.FindControl<Button>("SmtpSettingsButton");
        if (smtpSettingsButton != null)
        {
            smtpSettingsButton.Click += async (s, e) =>
            {
                var settingsWindow = new SmtpSettingsWindow();
                await settingsWindow.ShowDialog(this);
            };
        }
    }
}