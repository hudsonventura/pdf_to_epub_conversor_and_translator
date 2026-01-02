using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using BookTranslator.Services;

namespace BookTranslator.ViewModels;

public class LanguageItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string FileSuffix { get; set; } = "";
    
    public override string ToString() => Name;
}

public enum OutputFormat
{
    Pdf,
    Epub
}

public class MainWindowViewModel : ReactiveObject
{
    private readonly PreferencesService _preferencesService = new();

    // List of selected files
    private ObservableCollection<string> _selectedFiles = new();
    public ObservableCollection<string> SelectedFiles
    {
        get => _selectedFiles;
        set => this.RaiseAndSetIfChanged(ref _selectedFiles, value);
    }

    // Display text for selected files
    public string SelectedFilesDisplay => SelectedFiles.Count == 0 
        ? "" 
        : SelectedFiles.Count == 1 
            ? Path.GetFileName(SelectedFiles[0])
            : $"{SelectedFiles.Count} files selected";

    private string _statusMessage = "Ready to convert.";
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private int _progressValue;
    public int ProgressValue
    {
        get => _progressValue;
        set => this.RaiseAndSetIfChanged(ref _progressValue, value);
    }

    private int _progressMax = 100;
    public int ProgressMax
    {
        get => _progressMax;
        set => this.RaiseAndSetIfChanged(ref _progressMax, value);
    }

    private string _progressText = "";
    public string ProgressText
    {
        get => _progressText;
        set => this.RaiseAndSetIfChanged(ref _progressText, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    private bool _translateBeforeConvert = true;
    public bool TranslateBeforeConvert
    {
        get => _translateBeforeConvert;
        set
        {
            this.RaiseAndSetIfChanged(ref _translateBeforeConvert, value);
            SavePreferences();
        }
    }

    // Convert to AZW3 for better Kindle compatibility (requires Calibre)
    private bool _convertToAzw3 = true;
    public bool ConvertToAzw3
    {
        get => _convertToAzw3;
        set
        {
            this.RaiseAndSetIfChanged(ref _convertToAzw3, value);
            SavePreferences();
        }
    }

    // Output format selection (PDF or EPUB)
    private OutputFormat _selectedOutputFormat = OutputFormat.Epub;
    public OutputFormat SelectedOutputFormat
    {
        get => _selectedOutputFormat;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedOutputFormat, value);
            this.RaisePropertyChanged(nameof(IsPdfSelected));
            this.RaisePropertyChanged(nameof(IsEpubSelected));
            SavePreferences();
        }
    }

    // Helper properties for radio button binding
    public bool IsPdfSelected
    {
        get => SelectedOutputFormat == OutputFormat.Pdf;
        set { if (value) SelectedOutputFormat = OutputFormat.Pdf; }
    }

    public bool IsEpubSelected
    {
        get => SelectedOutputFormat == OutputFormat.Epub;
        set { if (value) SelectedOutputFormat = OutputFormat.Epub; }
    }

    // Check if Calibre is installed for AZW3 conversion
    public bool IsCalibreInstalled => CalibreConverter.IsCalibreInstalled();

    // Language selection
    public List<LanguageItem> AvailableLanguages { get; } = new()
    {
        new LanguageItem { Code = "pt", Name = "Portuguese (Brazil)", FileSuffix = "_PT-Br" },
        new LanguageItem { Code = "pt-pt", Name = "Portuguese (Portugal)", FileSuffix = "_PT-Pt" },
        new LanguageItem { Code = "en-us", Name = "English (US)", FileSuffix = "_EN-US" },
        new LanguageItem { Code = "en-uk", Name = "English (UK)", FileSuffix = "_EN-UK" },
        new LanguageItem { Code = "es", Name = "Spanish", FileSuffix = "_ES" },
        new LanguageItem { Code = "fr", Name = "French", FileSuffix = "_FR" },
        new LanguageItem { Code = "de", Name = "German", FileSuffix = "_DE" },
        new LanguageItem { Code = "it", Name = "Italian", FileSuffix = "_IT" },
        new LanguageItem { Code = "ru", Name = "Russian", FileSuffix = "_RU" },
        new LanguageItem { Code = "zh", Name = "Chinese (Simplified)", FileSuffix = "_ZH" },
        new LanguageItem { Code = "ja", Name = "Japanese", FileSuffix = "_JA" },
        new LanguageItem { Code = "ko", Name = "Korean", FileSuffix = "_KO" },
        new LanguageItem { Code = "ar", Name = "Arabic", FileSuffix = "_AR" },
    };

    private LanguageItem? _selectedLanguage;
    public LanguageItem? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
            SavePreferences();
        }
    }

    // Last directory for file dialog
    private string? _lastDirectory;
    public string? LastDirectory
    {
        get => _lastDirectory;
        set => this.RaiseAndSetIfChanged(ref _lastDirectory, value);
    }

    // Output directory for converted files
    private string? _outputDirectory;
    public string? OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            this.RaiseAndSetIfChanged(ref _outputDirectory, value);
            SavePreferences();
        }
    }

    public ReactiveCommand<Unit, Unit> SelectFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ConvertCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectOutputDirectoryCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearFilesCommand { get; }

    // Delegate to open file dialog (supports multiple files), set by the View
    public Func<string?, Task<string[]?>>? ShowOpenFilesDialog { get; set; }
    // Delegate to open folder dialog, set by the View
    public Func<string?, Task<string?>>? ShowSelectFolderDialog { get; set; }
    // Delegate to open file dialog for EPUB files to send to Kindle
    public Func<string?, Task<string[]?>>? ShowOpenEpubFilesDialog { get; set; }

    // Property to check if files are selected
    public bool HasFilesSelected => SelectedFiles.Count > 0;

    public MainWindowViewModel()
    {
        // Load saved preferences
        LoadPreferences();

        SelectFileCommand = ReactiveCommand.CreateFromTask(SelectFilesAsync);
        SelectOutputDirectoryCommand = ReactiveCommand.CreateFromTask(SelectOutputDirectoryAsync);
        ClearFilesCommand = ReactiveCommand.Create(ClearFiles);
        
        // Enable convert when files are selected and not busy
        var canConvert = this.WhenAnyValue(
            x => x.SelectedFiles.Count,
            x => x.IsBusy,
            (count, busy) => count > 0 && !busy
        );
        ConvertCommand = ReactiveCommand.CreateFromTask(ConvertAsync, canConvert);

        SendToKindleCommand = ReactiveCommand.CreateFromTask(SendToKindleAsync, this.WhenAnyValue(x => x.IsBusy, busy => !busy));
        
        // Subscribe to collection changes to update display
        SelectedFiles.CollectionChanged += (_, _) => 
        {
            this.RaisePropertyChanged(nameof(SelectedFilesDisplay));
            this.RaisePropertyChanged(nameof(HasFilesSelected));
        };
    }

    private void LoadPreferences()
    {
        var prefs = _preferencesService.Load();
        _translateBeforeConvert = prefs.TranslateBeforeConvert;
        _convertToAzw3 = prefs.ConvertToAzw3;
        _lastDirectory = prefs.LastDirectory;
        _outputDirectory = prefs.OutputDirectory;
        
        // Load output format
        _selectedOutputFormat = prefs.OutputFormat == "Pdf" ? OutputFormat.Pdf : OutputFormat.Epub;
        
        // Find matching language or default to first
        _selectedLanguage = AvailableLanguages.Find(l => l.Code == prefs.SelectedLanguageCode) 
                            ?? AvailableLanguages[0];
    }

    private void SavePreferences()
    {
        var prefs = new UserPreferences
        {
            SelectedLanguageCode = SelectedLanguage?.Code ?? "pt",
            LastDirectory = LastDirectory,
            OutputDirectory = OutputDirectory,
            TranslateBeforeConvert = TranslateBeforeConvert,
            ConvertToAzw3 = ConvertToAzw3,
            OutputFormat = SelectedOutputFormat.ToString()
        };
        _preferencesService.Save(prefs);
    }

    private async Task SelectOutputDirectoryAsync()
    {
        if (ShowSelectFolderDialog != null)
        {
            var folder = await ShowSelectFolderDialog.Invoke(OutputDirectory ?? LastDirectory);
            if (!string.IsNullOrEmpty(folder))
            {
                OutputDirectory = folder;
                StatusMessage = $"Output: {folder}";
            }
        }
    }

    private async Task SelectFilesAsync()
    {
        if (ShowOpenFilesDialog != null)
        {
            var files = await ShowOpenFilesDialog.Invoke(LastDirectory);
            if (files != null && files.Length > 0)
            {
                SelectedFiles.Clear();
                foreach (var file in files)
                {
                    SelectedFiles.Add(file);
                }
                
                StatusMessage = SelectedFiles.Count == 1 
                    ? $"Selected: {Path.GetFileName(files[0])}"
                    : $"Selected {SelectedFiles.Count} files";
                ProgressText = "";
                ProgressValue = 0;

                // Save the directory for next time
                LastDirectory = Path.GetDirectoryName(files[0]);
                SavePreferences();
            }
        }
    }

    private void ClearFiles()
    {
        SelectedFiles.Clear();
        StatusMessage = "Ready to convert.";
        ProgressText = "";
        ProgressValue = 0;
    }

    private async Task ConvertAsync()
    {
        if (SelectedFiles.Count == 0) return;
        
        var filesToProcess = SelectedFiles.ToList(); // Copy to avoid modification during iteration
        int totalFiles = filesToProcess.Count;
        int successCount = 0;
        int errorCount = 0;
        var lastOutputPath = string.Empty;

        try
        {
            IsBusy = true;

            for (int fileIndex = 0; fileIndex < filesToProcess.Count; fileIndex++)
            {
                var input = filesToProcess[fileIndex];
                var fileName = Path.GetFileName(input);
                
                try
                {
                    ProgressValue = 0;
                    ProgressMax = 100;
                    ProgressText = totalFiles > 1 
                        ? $"File {fileIndex + 1}/{totalFiles}: {fileName}"
                        : fileName;

                    StatusMessage = $"Converting: {fileName}...";

                    // Run on background thread
                    // Also extract cover image here to avoid blocking UI
                    (string englishMd, byte[]? coverBytes) = await Task.Run(() =>
                    {
                        var converter = new PdfToMarkdownConverter();
                        var text = converter.Convert(input);
                        var cover = converter.ExtractCoverImage(input);
                        return (text, cover);
                    });

                    string finalMd;
                    string epubSuffix;

                    if (TranslateBeforeConvert && SelectedLanguage != null)
                    {
                        StatusMessage = $"Translating {fileName} to {SelectedLanguage.Name}...";

                        var progress = new Progress<(int current, int total)>(p =>
                        {
                            ProgressValue = p.current;
                            ProgressMax = p.total;
                            var fileProgress = totalFiles > 1 ? $"File {fileIndex + 1}/{totalFiles}: " : "";
                            ProgressText = $"{fileProgress}{p.current}/{p.total}";
                        });

                        var targetLang = SelectedLanguage.Code;
                        finalMd = await Task.Run(async () =>
                        {
                            var translator = new TranslationService();
                            return await translator.TranslateAsync(englishMd, targetLang, progress);
                        });
                        epubSuffix = SelectedLanguage.FileSuffix;
                    }
                    else
                    {
                        finalMd = englishMd;
                        epubSuffix = "";
                    }

                    // Save to file (same location as PDF, but .md)
                    string outputPath = Path.ChangeExtension(input, ".md");
                    await File.WriteAllTextAsync(outputPath, finalMd);

                    // Use output directory if specified, otherwise use same folder as input
                    string outputDir = !string.IsNullOrEmpty(OutputDirectory) 
                        ? OutputDirectory 
                        : Path.GetDirectoryName(input) ?? "";

                    string finalOutputPath;

                    if (SelectedOutputFormat == OutputFormat.Pdf)
                    {
                        // PDF output: Convert markdown to PDF
                        StatusMessage = $"Generating PDF: {fileName}...";
                        
                        string pdfOutputPath = Path.Combine(
                            outputDir,
                            Path.GetFileNameWithoutExtension(input) + epubSuffix + "_translated.pdf"
                        );
                        
                        await Task.Run(() =>
                        {
                            var pdfConverter = new MarkdownToPdfConverter();
                            pdfConverter.Convert(finalMd, pdfOutputPath);
                        });
                        
                        finalOutputPath = pdfOutputPath;
                    }
                    else
                    {
                        // EPUB output
                        StatusMessage = $"Generating EPUB: {fileName}...";
                        
                        string epubOutputPath = Path.Combine(
                            outputDir,
                            Path.GetFileNameWithoutExtension(input) + epubSuffix + ".epub"
                        );

                        await Task.Run(() =>
                        {
                            var epubConverter = new MarkdownToEpubConverter();
                            epubConverter.Convert(finalMd, epubOutputPath, coverBytes);
                        });

                        finalOutputPath = epubOutputPath;

                        // Convert via AZW3 for better Kindle compatibility (EPUB → AZW3 → EPUB round-trip)
                        if (ConvertToAzw3 && CalibreConverter.IsCalibreInstalled())
                        {
                            var calibreProgress = new Progress<string>(msg =>
                            {
                                var fileProgress = totalFiles > 1 ? $"File {fileIndex + 1}/{totalFiles}: " : "";
                                ProgressText = fileProgress + msg;
                            });

                            var calibreConverter = new CalibreConverter();
                            
                            // Step 1: EPUB → AZW3
                            StatusMessage = $"Converting to AZW3: {fileName}...";
                            string azw3OutputPath = Path.ChangeExtension(epubOutputPath, ".azw3");
                            await calibreConverter.ConvertEpubToAzw3Async(epubOutputPath, azw3OutputPath, calibreProgress);
                            
                            // Step 2: AZW3 → EPUB (this fixes validation issues)
                            StatusMessage = $"Converting AZW3 to EPUB: {fileName}...";
                            string polishedEpubPath = Path.Combine(
                                outputDir,
                                Path.GetFileNameWithoutExtension(input) + epubSuffix + "_kindle.epub"
                            );
                            await calibreConverter.ConvertAzw3ToEpubAsync(azw3OutputPath, polishedEpubPath, calibreProgress);
                            
                            finalOutputPath = polishedEpubPath;

                            // Clean up intermediate files (original EPUB and AZW3)
                            try
                            {
                                if (File.Exists(epubOutputPath)) File.Delete(epubOutputPath);
                                if (File.Exists(azw3OutputPath)) File.Delete(azw3OutputPath);
                            }
                            catch { /* Ignore cleanup errors */ }
                        }
                    }

                    // Clean up intermediate markdown file (keep original PDF)
                    try
                    {
                        if (File.Exists(outputPath)) File.Delete(outputPath);
                    }
                    catch { /* Ignore cleanup errors */ }

                    lastOutputPath = finalOutputPath;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    StatusMessage = $"Error processing {fileName}: {ex.Message}";
                    // Continue with next file
                }
            }

            // Final status message
            if (totalFiles == 1)
            {
                StatusMessage = successCount == 1 
                    ? $"Success! Saved to: {lastOutputPath}"
                    : $"Error processing file";
            }
            else
            {
                StatusMessage = errorCount == 0
                    ? $"Done! Successfully processed {successCount} files."
                    : $"Done! {successCount} succeeded, {errorCount} failed.";
            }

            ProgressText = "Done";
            GeneratedPdfPath = lastOutputPath;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string? _generatedEpubPath;
    public string? GeneratedPdfPath
    {
        get => _generatedEpubPath;
        set => this.RaiseAndSetIfChanged(ref _generatedEpubPath, value);
    }

    public ReactiveCommand<Unit, Unit> SendToKindleCommand { get; }

    private async Task SendToKindleAsync()
    {
        // Open file picker to select EPUB files
        if (ShowOpenEpubFilesDialog == null)
        {
            StatusMessage = "File dialog not available.";
            return;
        }

        var files = await ShowOpenEpubFilesDialog.Invoke(LastDirectory);
        if (files == null || files.Length == 0)
        {
            return; // User cancelled
        }

        // Update last directory
        LastDirectory = Path.GetDirectoryName(files[0]);
        SavePreferences();

        int totalFiles = files.Length;
        int successCount = 0;
        int errorCount = 0;

        try
        {
            IsBusy = true;
            var emailService = new EmailService();

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileName = Path.GetFileName(file);

                try
                {
                    ProgressValue = i;
                    ProgressMax = totalFiles;
                    ProgressText = $"{i + 1}/{totalFiles}";
                    StatusMessage = $"Sending {fileName} to Kindle...";

                    await Task.Run(() => emailService.SendToKindleAsync(file));
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    StatusMessage = $"Failed to send {fileName}: {ex.Message}";
                    // Continue with next file
                }
            }

            // Final status
            ProgressValue = totalFiles;
            if (totalFiles == 1)
            {
                StatusMessage = successCount == 1 
                    ? "Email sent successfully!" 
                    : "Failed to send email.";
            }
            else
            {
                StatusMessage = errorCount == 0
                    ? $"Done! Successfully sent {successCount} files to Kindle."
                    : $"Done! {successCount} sent, {errorCount} failed.";
            }
            ProgressText = "Done";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Email failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

