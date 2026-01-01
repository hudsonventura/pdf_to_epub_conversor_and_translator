using System;
using System.Text;
using System.Threading.Tasks;
using GTranslate.Results;
using GTranslate.Translators;

namespace BookTranslator.Services;

public class TranslationService
{
    private readonly AggregateTranslator _translator;

    public TranslationService()
    {
        // Use AggregateTranslator to try multiple free services if one fails
        _translator = new AggregateTranslator();
    }

    public async Task<string> TranslateAsync(string markdownText, string targetLanguage = "pt", IProgress<(int current, int total)>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(markdownText)) return markdownText;

        var sb = new StringBuilder();

        // GTranslate (and free APIs) usually have limits on text length per request.
        // We really should chunk this smarter, but line-by-line or paragraph-by-paragraph is safest for now.
        // Since we have newlines in the markdown, let's split by double newline (paragraphs).

        var paragraphs = markdownText.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.None);
        int total = paragraphs.Length;
        int current = 0;

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                sb.AppendLine();
                current++;
                progress?.Report((current, total));
                continue;
            }

            // Check if it's a header
            bool isHeader = paragraph.StartsWith("## ");
            string textToTranslate = isHeader ? paragraph.Substring(3) : paragraph;

            try
            {
                // Target: Portuguese (Brazil) "pt" usually auto-detects.
                // GTranslate uses "pt" for Portuguese. 
                var result = await _translator.TranslateAsync(textToTranslate, targetLanguage, "en");

                if (isHeader)
                {
                    sb.AppendLine($"## {result.Translation}");
                }
                else
                {
                    sb.AppendLine(result.Translation);
                }
            }
            catch (Exception)
            {
                // Fallback: keep original text if translation fails
                sb.AppendLine(paragraph);
                // Ideally log this
            }

            sb.AppendLine();
            current++;
            progress?.Report((current, total));
        }

        return sb.ToString();
    }
}
