using System;
using System.Collections.Generic;
using System.IO;
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

public class MainWindowViewModel : ReactiveObject
{
    private readonly PreferencesService _preferencesService = new();

    private string _inputFilePath = string.Empty;
    public string InputFilePath
    {
        get => _inputFilePath;
        set => this.RaiseAndSetIfChanged(ref _inputFilePath, value);
    }

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

    // Check if Calibre is installed for AZW3 conversion
    public bool IsCalibreInstalled => CalibreConverter.IsCalibreInstalled();

    // Language selection
    public List<LanguageItem> AvailableLanguages { get; } = new()
    {
        new LanguageItem { Code = "pt", Name = "Portuguese (Brazil)", FileSuffix = "_PT-Br" },
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

    // Delegate to open file dialog, set by the View
    public Func<string?, Task<string?>>? ShowOpenFileDialog { get; set; }
    // Delegate to open folder dialog, set by the View
    public Func<string?, Task<string?>>? ShowSelectFolderDialog { get; set; }

    public MainWindowViewModel()
    {
        // Load saved preferences
        LoadPreferences();

        SelectFileCommand = ReactiveCommand.CreateFromTask(SelectFileAsync);
        SelectOutputDirectoryCommand = ReactiveCommand.CreateFromTask(SelectOutputDirectoryAsync);
        // Enable convert when file path is valid and not busy
        var canConvert = this.WhenAnyValue(
            x => x.InputFilePath,
            x => x.IsBusy,
            (path, busy) => !string.IsNullOrEmpty(path) && !busy
        );
        ConvertCommand = ReactiveCommand.CreateFromTask(ConvertAsync, canConvert);

        SendToKindleCommand = ReactiveCommand.CreateFromTask(SendToKindleAsync, this.WhenAnyValue(x => x.IsBusy, busy => !busy));
    }

    private void LoadPreferences()
    {
        var prefs = _preferencesService.Load();
        _translateBeforeConvert = prefs.TranslateBeforeConvert;
        _convertToAzw3 = prefs.ConvertToAzw3;
        _lastDirectory = prefs.LastDirectory;
        _outputDirectory = prefs.OutputDirectory;
        
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
            ConvertToAzw3 = ConvertToAzw3
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

    private async Task SelectFileAsync()
    {
        if (ShowOpenFileDialog != null)
        {
            var file = await ShowOpenFileDialog.Invoke(LastDirectory);
            if (!string.IsNullOrEmpty(file))
            {
                InputFilePath = file;
                StatusMessage = $"Selected: {Path.GetFileName(file)}";
                ProgressText = "";
                ProgressValue = 0;

                // Save the directory for next time
                LastDirectory = Path.GetDirectoryName(file);
                SavePreferences();
            }
        }
    }

    private async Task ConvertAsync()
    {
        try
        {
            IsBusy = true;
            ProgressValue = 0;
            ProgressMax = 100; // Placeholder
            ProgressText = "Preparing...";

            StatusMessage = "Converting PDF to text...";
            var input = InputFilePath; // Capture for thread safety

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
                StatusMessage = $"Translating to {SelectedLanguage.Name}...";

                var progress = new Progress<(int current, int total)>(p =>
                {
                    ProgressValue = p.current;
                    ProgressMax = p.total;
                    ProgressText = $"{p.current}/{p.total}";
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

            // Generate EPUB
            StatusMessage = "Generating EPUB...";
            
            // Use output directory if specified, otherwise use same folder as input
            string outputDir = !string.IsNullOrEmpty(OutputDirectory) 
                ? OutputDirectory 
                : Path.GetDirectoryName(input) ?? "";
            
            string epubOutputPath = Path.Combine(
                outputDir,
                Path.GetFileNameWithoutExtension(input) + epubSuffix + ".epub"
            );

            await Task.Run(() =>
            {
                var epubConverter = new MarkdownToEpubConverter();
                epubConverter.Convert(finalMd, epubOutputPath, coverBytes);
            });

            string finalOutputPath = epubOutputPath;

            // Convert via AZW3 for better Kindle compatibility (EPUB → AZW3 → EPUB round-trip)
            if (ConvertToAzw3)
            {
                if (CalibreConverter.IsCalibreInstalled())
                {
                    var calibreProgress = new Progress<string>(msg =>
                    {
                        ProgressText = msg;
                    });

                    var calibreConverter = new CalibreConverter();
                    
                    // Step 1: EPUB → AZW3
                    StatusMessage = "Converting EPUB to AZW3...";
                    ProgressText = "EPUB → AZW3...";
                    string azw3OutputPath = Path.ChangeExtension(epubOutputPath, ".azw3");
                    await calibreConverter.ConvertEpubToAzw3Async(epubOutputPath, azw3OutputPath, calibreProgress);
                    
                    // Step 2: AZW3 → EPUB (this fixes validation issues)
                    StatusMessage = "Converting AZW3 back to EPUB...";
                    ProgressText = "AZW3 → EPUB...";
                    string polishedEpubPath = Path.Combine(
                        Path.GetDirectoryName(epubOutputPath) ?? "",
                        Path.GetFileNameWithoutExtension(epubOutputPath) + "_kindle.epub"
                    );
                    await calibreConverter.ConvertAzw3ToEpubAsync(azw3OutputPath, polishedEpubPath, calibreProgress);
                    
                    finalOutputPath = polishedEpubPath;
                    StatusMessage = $"Success! Kindle-compatible EPUB saved to: {polishedEpubPath}";

                    // Clean up intermediate files (original EPUB and AZW3)
                    try
                    {
                        if (File.Exists(epubOutputPath)) File.Delete(epubOutputPath);
                        if (File.Exists(azw3OutputPath)) File.Delete(azw3OutputPath);
                    }
                    catch { /* Ignore cleanup errors */ }
                }
                else
                {
                    StatusMessage = $"EPUB saved: {epubOutputPath} (Install Calibre for Kindle optimization)";
                }
            }
            else
            {
                StatusMessage = $"Success! Saved to: {epubOutputPath}";
            }

            // Clean up intermediate markdown file (keep original PDF)
            try
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
            catch
            {
                // Ignore cleanup errors
            }

            ProgressText = "Done";

            // Enable/Setup for email
            GeneratedPdfPath = finalOutputPath;
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
        if (ShowOpenFileDialog == null) return;

        // User requested to select a file ensuring we pick what they want
        var fileToSend = await ShowOpenFileDialog.Invoke(LastDirectory);
        if (string.IsNullOrEmpty(fileToSend)) return;

        try
        {
            IsBusy = true;
            StatusMessage = $"Sending {Path.GetFileName(fileToSend)} to Kindle...";

            var emailService = new EmailService();
            await Task.Run(() => emailService.SendToKindleAsync(fileToSend));

            StatusMessage = "Email sent successfully!";
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
