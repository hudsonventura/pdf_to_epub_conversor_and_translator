using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BookTranslator.Services;

/// <summary>
/// Provides ebook conversion capabilities using Calibre's ebook-convert command-line tool.
/// This is used to convert EPUB to AZW3 format for better Kindle compatibility.
/// </summary>
public class CalibreConverter
{
    private const string EbookConvertCommand = "ebook-convert";

    /// <summary>
    /// Checks if Calibre's ebook-convert is available on the system.
    /// </summary>
    public static bool IsCalibreInstalled()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = EbookConvertCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // On Windows, use "where" instead of "which"
            if (OperatingSystem.IsWindows())
            {
                startInfo.FileName = "where";
            }

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts an EPUB file to AZW3 format using Calibre.
    /// </summary>
    /// <param name="epubPath">Path to the source EPUB file</param>
    /// <param name="azw3Path">Path where the AZW3 file should be saved (optional, defaults to same directory)</param>
    /// <param name="progress">Optional progress callback</param>
    /// <returns>Path to the generated AZW3 file</returns>
    /// <exception cref="InvalidOperationException">Thrown when Calibre is not installed</exception>
    /// <exception cref="Exception">Thrown when conversion fails</exception>
    public async Task<string> ConvertEpubToAzw3Async(
        string epubPath, 
        string? azw3Path = null,
        IProgress<string>? progress = null)
    {
        if (!IsCalibreInstalled())
        {
            throw new InvalidOperationException(
                "Calibre is not installed. Please install Calibre to enable AZW3 conversion.\n" +
                "Linux: sudo apt install calibre\n" +
                "Windows/Mac: Download from https://calibre-ebook.com/download");
        }

        if (!File.Exists(epubPath))
        {
            throw new FileNotFoundException("EPUB file not found", epubPath);
        }

        // Generate output path if not provided
        azw3Path ??= Path.ChangeExtension(epubPath, ".azw3");

        progress?.Report("Starting AZW3 conversion with Calibre...");

        var startInfo = new ProcessStartInfo
        {
            FileName = EbookConvertCommand,
            Arguments = $"\"{epubPath}\" \"{azw3Path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        var output = string.Empty;
        var error = string.Empty;

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output += e.Data + Environment.NewLine;
                // Report progress for key stages
                if (e.Data.Contains("%"))
                {
                    progress?.Report(e.Data);
                }
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error += e.Data + Environment.NewLine;
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Calibre conversion failed (exit code {process.ExitCode}): {error}");
        }

        if (!File.Exists(azw3Path))
        {
            throw new Exception($"Conversion completed but AZW3 file was not created: {azw3Path}");
        }

        progress?.Report("AZW3 conversion complete!");
        return azw3Path;
    }

    /// <summary>
    /// Converts an EPUB file to a "polished" EPUB using Calibre.
    /// This can fix EPUB validation issues that cause Amazon to reject the file.
    /// </summary>
    public async Task<string> PolishEpubAsync(
        string inputEpubPath,
        string? outputEpubPath = null,
        IProgress<string>? progress = null)
    {
        if (!IsCalibreInstalled())
        {
            throw new InvalidOperationException(
                "Calibre is not installed. Please install Calibre to enable EPUB polishing.");
        }

        if (!File.Exists(inputEpubPath))
        {
            throw new FileNotFoundException("EPUB file not found", inputEpubPath);
        }

        // Generate output path if not provided (add _polished suffix)
        outputEpubPath ??= Path.Combine(
            Path.GetDirectoryName(inputEpubPath) ?? "",
            Path.GetFileNameWithoutExtension(inputEpubPath) + "_polished.epub");

        progress?.Report("Polishing EPUB with Calibre...");

        // Convert EPUB to EPUB - this "polishes" the file and fixes issues
        var startInfo = new ProcessStartInfo
        {
            FileName = EbookConvertCommand,
            Arguments = $"\"{inputEpubPath}\" \"{outputEpubPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        var error = string.Empty;

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error += e.Data + Environment.NewLine;
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Calibre polishing failed (exit code {process.ExitCode}): {error}");
        }

        progress?.Report("EPUB polishing complete!");
        return outputEpubPath;
    }

    /// <summary>
    /// Converts an AZW3 file back to EPUB format using Calibre.
    /// This is used for the EPUB → AZW3 → EPUB round-trip that fixes validation issues.
    /// </summary>
    public async Task<string> ConvertAzw3ToEpubAsync(
        string azw3Path,
        string? epubPath = null,
        IProgress<string>? progress = null)
    {
        if (!IsCalibreInstalled())
        {
            throw new InvalidOperationException(
                "Calibre is not installed. Please install Calibre to enable format conversion.");
        }

        if (!File.Exists(azw3Path))
        {
            throw new FileNotFoundException("AZW3 file not found", azw3Path);
        }

        // Generate output path if not provided
        epubPath ??= Path.ChangeExtension(azw3Path, ".epub");

        progress?.Report("Converting AZW3 to EPUB...");

        var startInfo = new ProcessStartInfo
        {
            FileName = EbookConvertCommand,
            Arguments = $"\"{azw3Path}\" \"{epubPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        var error = string.Empty;

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("%"))
            {
                progress?.Report(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error += e.Data + Environment.NewLine;
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Calibre conversion failed (exit code {process.ExitCode}): {error}");
        }

        if (!File.Exists(epubPath))
        {
            throw new Exception($"Conversion completed but EPUB file was not created: {epubPath}");
        }

        progress?.Report("EPUB conversion complete!");
        return epubPath;
    }
}
