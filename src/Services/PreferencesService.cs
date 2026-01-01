using System;
using System.IO;
using System.Text.Json;

namespace BookTranslator.Services;

public class PreferencesService
{
    private static readonly string PreferencesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BookTranslator",
        "preferences.json"
    );

    public UserPreferences Load()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                var json = File.ReadAllText(PreferencesPath);
                return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
            }
        }
        catch
        {
            // If loading fails, return defaults
        }
        return new UserPreferences();
    }

    public void Save(UserPreferences preferences)
    {
        try
        {
            var directory = Path.GetDirectoryName(PreferencesPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(PreferencesPath, json);
        }
        catch
        {
            // Silently fail if we can't save preferences
        }
    }
}

public class UserPreferences
{
    public string SelectedLanguageCode { get; set; } = "pt";
    public string? LastDirectory { get; set; }
    public string? OutputDirectory { get; set; }
    public bool TranslateBeforeConvert { get; set; } = true;
    public bool ConvertToAzw3 { get; set; } = true;
    public string OutputFormat { get; set; } = "Epub";  // "Pdf" or "Epub"
}
