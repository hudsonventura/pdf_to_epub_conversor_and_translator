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

    public ReactiveCommand<Unit, Unit> SelectFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ConvertCommand { get; }

    // Delegate to open file dialog, set by the View
    public Func<string?, Task<string?>>? ShowOpenFileDialog { get; set; }

    public MainWindowViewModel()
    {
        // Load saved preferences
        LoadPreferences();

        SelectFileCommand = ReactiveCommand.CreateFromTask(SelectFileAsync);
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
        _lastDirectory = prefs.LastDirectory;
        
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
            TranslateBeforeConvert = TranslateBeforeConvert
        };
        _preferencesService.Save(prefs);
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
            string epubOutputPath = Path.Combine(
                Path.GetDirectoryName(input) ?? "",
                Path.GetFileNameWithoutExtension(input) + epubSuffix + ".epub"
            );

            await Task.Run(() =>
            {
                var epubConverter = new MarkdownToEpubConverter();
                epubConverter.Convert(finalMd, epubOutputPath, coverBytes);
            });

            StatusMessage = $"Success! Saved to: {epubOutputPath}";

            // Auto-cleanup original and md files
            try
            {
                if (File.Exists(input)) File.Delete(input);
                if (File.Exists(outputPath)) File.Delete(outputPath);
                StatusMessage += " (Cleanup done)";
            }
            catch (Exception cleanupEx)
            {
                StatusMessage += $" (Cleanup failed: {cleanupEx.Message})";
            }

            ProgressText = "Done";

            // Enable/Setup for email
            GeneratedPdfPath = epubOutputPath;
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
