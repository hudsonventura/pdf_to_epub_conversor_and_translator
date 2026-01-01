using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EpubCore;

namespace BookTranslator.Services;

public class MarkdownToEpubConverter
{
    public void Convert(string markdownContent, string outputPath, byte[]? coverImageBytes = null, string? title = null)
    {
        var bookTitle = title ?? Path.GetFileNameWithoutExtension(outputPath);
        
        var writer = new EpubWriter();
        writer.SetTitle(bookTitle);
        writer.AddAuthor("Book Translator");

        // Add cover image if available
        if (coverImageBytes != null && coverImageBytes.Length > 0)
        {
            writer.SetCover(coverImageBytes, ImageFormat.Jpeg);
        }

        // Convert markdown to HTML chapters
        var chapters = ParseMarkdownToChapters(markdownContent);
        
        int chapterIndex = 1;
        foreach (var chapter in chapters)
        {
            var htmlContent = ConvertMarkdownToHtml(chapter.Content, chapter.Title);
            var chapterTitle = chapter.Title ?? $"Capítulo {chapterIndex}";
            writer.AddChapter(chapterTitle, htmlContent);
            chapterIndex++;
        }

        // Build and save the EPUB
        writer.Write(outputPath);
    }

    private List<ChapterInfo> ParseMarkdownToChapters(string markdownContent)
    {
        var chapters = new List<ChapterInfo>();
        var lines = markdownContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        
        var currentChapter = new ChapterInfo { Title = "Introdução" };
        var contentBuilder = new StringBuilder();

        foreach (var line in lines)
        {
            // Check for chapter headers (## or #)
            if (line.TrimStart().StartsWith("## "))
            {
                // Save previous chapter if it has content
                if (contentBuilder.Length > 0)
                {
                    currentChapter.Content = contentBuilder.ToString();
                    chapters.Add(currentChapter);
                }
                
                // Start new chapter
                currentChapter = new ChapterInfo 
                { 
                    Title = line.TrimStart().Substring(3).Trim() 
                };
                contentBuilder.Clear();
            }
            else if (line.TrimStart().StartsWith("# ") && !line.TrimStart().StartsWith("## "))
            {
                // Main title - save previous and start new
                if (contentBuilder.Length > 0)
                {
                    currentChapter.Content = contentBuilder.ToString();
                    chapters.Add(currentChapter);
                }
                
                currentChapter = new ChapterInfo 
                { 
                    Title = line.TrimStart().Substring(2).Trim() 
                };
                contentBuilder.Clear();
            }
            else
            {
                contentBuilder.AppendLine(line);
            }
        }

        // Add the last chapter
        if (contentBuilder.Length > 0)
        {
            currentChapter.Content = contentBuilder.ToString();
            chapters.Add(currentChapter);
        }

        // If no chapters were found, create one with all content
        if (chapters.Count == 0)
        {
            chapters.Add(new ChapterInfo 
            { 
                Title = "Conteúdo", 
                Content = markdownContent 
            });
        }

        return chapters;
    }

    private string ConvertMarkdownToHtml(string markdownContent, string? title)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
        sb.AppendLine("<head>");
        sb.AppendLine($"  <title>{EscapeHtml(title ?? "Chapter")}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: Georgia, serif; font-size: 1em; line-height: 1.6; margin: 1em; }");
        sb.AppendLine("    h1 { font-size: 1.5em; margin-bottom: 0.5em; }");
        sb.AppendLine("    h2 { font-size: 1.3em; margin-bottom: 0.4em; }");
        sb.AppendLine("    p { text-align: justify; margin-bottom: 0.8em; text-indent: 1.5em; }");
        sb.AppendLine("    p:first-of-type { text-indent: 0; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        
        if (!string.IsNullOrEmpty(title))
        {
            sb.AppendLine($"  <h1>{EscapeHtml(title)}</h1>");
        }

        // Convert markdown paragraphs to HTML
        var paragraphs = markdownContent.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var paragraph in paragraphs)
        {
            var text = paragraph.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Skip already processed headers
            if (text.StartsWith("## ") || text.StartsWith("# "))
            {
                continue;
            }

            // Handle bold text **text**
            text = System.Text.RegularExpressions.Regex.Replace(
                text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            
            // Handle italic text *text*
            text = System.Text.RegularExpressions.Regex.Replace(
                text, @"\*(.+?)\*", "<em>$1</em>");

            // Replace newlines within paragraph with line breaks
            text = text.Replace("\r\n", "<br/>").Replace("\n", "<br/>");

            sb.AppendLine($"  <p>{EscapeHtmlPreserveFormatting(text)}</p>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }

    private string EscapeHtml(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text);
    }

    private string EscapeHtmlPreserveFormatting(string text)
    {
        // Don't escape < and > for our formatting tags
        return text
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;");
    }

    private class ChapterInfo
    {
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
