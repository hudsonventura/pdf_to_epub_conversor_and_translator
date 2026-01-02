using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using BookTranslator.Services;

namespace BookTranslator.Services;

public class EmailService
{
    private readonly SmtpSettingsService _settingsService = new();

    public async Task SendToKindleAsync(string filePath)
    {
        var settings = _settingsService.GetSettings();
        
        if (string.IsNullOrEmpty(settings.SmtpServer) || 
            string.IsNullOrEmpty(settings.SenderEmail) || 
            string.IsNullOrEmpty(settings.SenderPassword) ||
            string.IsNullOrEmpty(settings.KindleEmail))
        {
            throw new InvalidOperationException("SMTP settings are not configured. Please click the gear icon (⚙️) to configure email settings.");
        }

        var message = new MailMessage(settings.SenderEmail, settings.KindleEmail)
        {
            Subject = "convert",
            Body = "Sending converted book to Kindle."
        };

        if (File.Exists(filePath))
        {
            var attachment = new Attachment(filePath);
            message.Attachments.Add(attachment);
        }
        else
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        using (var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort))
        {
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(settings.SenderEmail, settings.SenderPassword);
            await client.SendMailAsync(message);
        }
    }
}
