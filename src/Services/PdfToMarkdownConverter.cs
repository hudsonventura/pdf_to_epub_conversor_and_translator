using System;
using System.Text;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace BookTranslator.Services;

public class PdfToMarkdownConverter
{
    public string Convert(string pdfPath)
    {
        var sb = new StringBuilder();

        try
        {
            using (var pdf = PdfDocument.Open(pdfPath))
            {
                foreach (var page in pdf.GetPages())
                {
                    var words = page.GetWords().ToList();
                    if (!words.Any()) continue;

                    // 1. Group words into lines (vertical clustering)
                    // We use a small tolerance for Y coordinate
                    var lines = words.GroupBy(w => (int)(w.BoundingBox.Bottom / 5))
                                     .OrderByDescending(g => g.Key) // Top to Bottom
                                     .Select(g => new
                                     {
                                         Y = g.Key,
                                         Words = g.OrderBy(w => w.BoundingBox.Left).ToList(),
                                         MaxFontSize = g.Max(w => w.Letters.FirstOrDefault()?.PointSize ?? 0)
                                     })
                                     .ToList();

                    // Calculate average/median font size to detect headers
                    // Filter out very small fonts (artifacts) if needed, but simple avg is okay
                    var allFontSizes = words.Select(w => w.Letters.FirstOrDefault()?.PointSize ?? 0).Where(s => s > 0).ToList();
                    double avgFontSize = allFontSizes.Any() ? allFontSizes.Average() : 10;
                    double headerThreshold = avgFontSize * 1.2;

                    double? previousLineBottom = null;
                    var currentParagraph = new StringBuilder();

                    foreach (var line in lines)
                    {
                        var lineText = string.Join(" ", line.Words.Select(w => w.Text));

                        // Header detection
                        if (line.MaxFontSize >= headerThreshold && lineText.Length < 100)
                        {
                            // Flush previous paragraph if any
                            if (currentParagraph.Length > 0)
                            {
                                sb.AppendLine(currentParagraph.ToString());
                                sb.AppendLine();
                                currentParagraph.Clear();
                            }

                            sb.AppendLine();
                            sb.AppendLine($"## {lineText}");
                            sb.AppendLine();
                            previousLineBottom = null; // Reset grouping
                            continue;
                        }

                        // Text Body Logic
                        // Get actual Bottom of this line from its words to calculate distance
                        // (Note: line.Y is already an approximation, let's use the average bottom of words in this line)
                        double currentLineTop = line.Words.Max(w => w.BoundingBox.Top);
                        double currentLineBottom = line.Words.Min(w => w.BoundingBox.Bottom);

                        bool isNewParagraph = false;

                        if (previousLineBottom.HasValue)
                        {
                            // Calculate gap. BoundingBox.Top is higher value than Bottom in PdfPig?
                            // PdfPig coordinates: usually logical PDF coords (0,0 at bottom-left).
                            // So Top > Bottom.
                            // Distance between previous line (above) and this line (below).
                            // Previous line's Bottom should be > Current line's Top. 
                            // Wait, lines are sorted Top to Bottom.
                            // So Previous Line Y > Current Line Y.

                            double verticalGap = previousLineBottom.Value - currentLineTop;

                            // Average height of a line is roughly avgFontSize.
                            // If gap is significantly larger than line spacing, it's a new paragraph.
                            // Standard line spacing is often 1.2 * fontSize.
                            // If gap > 1.5 * fontSize, likely a new paragraph.

                            if (verticalGap > avgFontSize * 1.5)
                            {
                                isNewParagraph = true;
                            }
                        }
                        else
                        {
                            isNewParagraph = true; // First line of page/section
                        }

                        if (isNewParagraph)
                        {
                            if (currentParagraph.Length > 0)
                            {
                                sb.AppendLine(currentParagraph.ToString());
                                sb.AppendLine();
                                currentParagraph.Clear();
                            }
                            currentParagraph.Append(lineText);
                        }
                        else
                        {
                            // Continuation
                            // Handle hyphenation
                            if (currentParagraph.Length > 0 && currentParagraph[currentParagraph.Length - 1] == '-')
                            {
                                // Remove hyphen and join directly
                                currentParagraph.Remove(currentParagraph.Length - 1, 1);
                                currentParagraph.Append(lineText);
                            }
                            else
                            {
                                currentParagraph.Append(" ");
                                currentParagraph.Append(lineText);
                            }
                        }

                        previousLineBottom = currentLineBottom;
                    }

                    // Flush end of page
                    if (currentParagraph.Length > 0)
                    {
                        sb.AppendLine(currentParagraph.ToString());
                        sb.AppendLine();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Error processing PDF: {ex.Message}");
        }

        return sb.ToString();
    }
    public byte[]? ExtractCoverImage(string pdfPath)
    {
        try
        {
            using (var pdf = PdfDocument.Open(pdfPath))
            {
                // check first 3 pages for an image
                int pagesToCheck = Math.Min(3, pdf.NumberOfPages);

                for (int i = 1; i <= pagesToCheck; i++)
                {
                    var page = pdf.GetPage(i);
                    var images = page.GetImages();
                    var firstImage = images.FirstOrDefault();

                    if (firstImage != null)
                    {
                        // Found a cover candidate
                        // In PdfPig, RawBytes usually gives the raw stream data (e.g. jpeg/png)
                        // If it's a defined filter pipeline, we might need to decode, but often RawBytes is enough for direct embedding if it's a standard format.
                        // However, QuestPDF expects standard image formats.
                        // Ideally checking TryGetPng or TryGetJpeg would be safer, but let's try RawBytes first.
                        return firstImage.RawBytes.ToArray();
                    }
                }
            }
        }
        catch
        {
            // Ignore errors for cover extraction
        }

        return null;
    }
}
