using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BookTranslator.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        _configuration = builder.Build();
    }

    public async Task SendToKindleAsync(string filePath)
    {
        var settings = _configuration.GetSection("EmailSettings");
        var smtpServer = settings["SmtpServer"];
        var smtpPort = int.Parse(settings["SmtpPort"] ?? "587");
        var senderEmail = settings["SenderEmail"];
        var senderPassword = settings["SenderPassword"];
        var toEmail = "hudsonventura@kindle.com";

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
        {
            throw new InvalidOperationException("Email settings are not configured. Please check appsettings.json.");
        }

        var message = new MailMessage(senderEmail, toEmail)
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

        using (var client = new SmtpClient(smtpServer, smtpPort))
        {
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(senderEmail, senderPassword);
            await client.SendMailAsync(message);
        }
    }
}
