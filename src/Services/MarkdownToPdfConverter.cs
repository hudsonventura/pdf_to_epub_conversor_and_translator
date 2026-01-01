using System;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BookTranslator.Services;

/// <summary>
/// Converts markdown content to PDF format using QuestPDF.
/// </summary>
public class MarkdownToPdfConverter
{
    static MarkdownToPdfConverter()
    {
        // Configure QuestPDF license (Community license is free for small businesses)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Converts markdown content to a PDF file.
    /// </summary>
    /// <param name="markdownContent">The markdown content to convert</param>
    /// <param name="outputPath">Path where the PDF should be saved</param>
    /// <param name="title">Optional title for the document</param>
    public void Convert(string markdownContent, string outputPath, string? title = null)
    {
        var documentTitle = title ?? Path.GetFileNameWithoutExtension(outputPath);
        var paragraphs = ParseMarkdownToParagraphs(markdownContent);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("DejaVu Sans", "Liberation Sans", "Arial"));

                page.Header()
                    .Text(documentTitle)
                    .SemiBold()
                    .FontSize(16)
                    .FontColor(Colors.Blue.Darken2);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(10);

                        foreach (var paragraph in paragraphs)
                        {
                            if (paragraph.IsHeader)
                            {
                                column.Item().Text(paragraph.Content)
                                    .Bold()
                                    .FontSize(paragraph.HeaderLevel == 1 ? 16 : 14);
                            }
                            else
                            {
                                column.Item().Text(text =>
                                {
                                    text.Span(paragraph.Content);
                                    text.DefaultTextStyle(x => x.LineHeight(1.5f));
                                });
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        }).GeneratePdf(outputPath);
    }

    private List<ParagraphInfo> ParseMarkdownToParagraphs(string markdownContent)
    {
        var paragraphs = new List<ParagraphInfo>();
        var lines = markdownContent.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if (trimmed.StartsWith("# "))
            {
                paragraphs.Add(new ParagraphInfo
                {
                    Content = trimmed.Substring(2).Trim(),
                    IsHeader = true,
                    HeaderLevel = 1
                });
            }
            else if (trimmed.StartsWith("## "))
            {
                paragraphs.Add(new ParagraphInfo
                {
                    Content = trimmed.Substring(3).Trim(),
                    IsHeader = true,
                    HeaderLevel = 2
                });
            }
            else if (trimmed.StartsWith("### "))
            {
                paragraphs.Add(new ParagraphInfo
                {
                    Content = trimmed.Substring(4).Trim(),
                    IsHeader = true,
                    HeaderLevel = 3
                });
            }
            else
            {
                // Remove markdown formatting for plain text
                var content = trimmed
                    .Replace("**", "")
                    .Replace("*", "")
                    .Replace("_", "");
                
                // Replace line breaks within paragraph
                content = content.Replace("\r\n", " ").Replace("\n", " ");
                
                paragraphs.Add(new ParagraphInfo
                {
                    Content = content,
                    IsHeader = false
                });
            }
        }

        return paragraphs;
    }

    private class ParagraphInfo
    {
        public string Content { get; set; } = string.Empty;
        public bool IsHeader { get; set; }
        public int HeaderLevel { get; set; }
    }
}
