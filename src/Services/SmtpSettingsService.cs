using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BookTranslator.Services;

public class SmtpSettings
{
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = "";
    public string SenderPassword { get; set; } = "";
    public string KindleEmail { get; set; } = "";
}

public class SmtpSettingsService
{
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BookTranslator",
        "settings.db"
    );

    public SmtpSettingsService()
    {
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        var directory = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS SmtpSettings (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                SmtpServer TEXT NOT NULL DEFAULT '',
                SmtpPort INTEGER NOT NULL DEFAULT 587,
                SenderEmail TEXT NOT NULL DEFAULT '',
                SenderPassword TEXT NOT NULL DEFAULT '',
                KindleEmail TEXT NOT NULL DEFAULT ''
            );
            INSERT OR IGNORE INTO SmtpSettings (Id) VALUES (1);
        ";
        command.ExecuteNonQuery();
    }

    public SmtpSettings GetSettings()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT SmtpServer, SmtpPort, SenderEmail, SenderPassword, KindleEmail FROM SmtpSettings WHERE Id = 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new SmtpSettings
            {
                SmtpServer = reader.GetString(0),
                SmtpPort = reader.GetInt32(1),
                SenderEmail = reader.GetString(2),
                SenderPassword = reader.GetString(3),
                KindleEmail = reader.GetString(4)
            };
        }

        return new SmtpSettings();
    }

    public void SaveSettings(SmtpSettings settings)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE SmtpSettings SET 
                SmtpServer = $server,
                SmtpPort = $port,
                SenderEmail = $email,
                SenderPassword = $password,
                KindleEmail = $kindle
            WHERE Id = 1
        ";
        command.Parameters.AddWithValue("$server", settings.SmtpServer);
        command.Parameters.AddWithValue("$port", settings.SmtpPort);
        command.Parameters.AddWithValue("$email", settings.SenderEmail);
        command.Parameters.AddWithValue("$password", settings.SenderPassword);
        command.Parameters.AddWithValue("$kindle", settings.KindleEmail);
        command.ExecuteNonQuery();
    }

    public bool IsConfigured()
    {
        var settings = GetSettings();
        return !string.IsNullOrEmpty(settings.SmtpServer) &&
               !string.IsNullOrEmpty(settings.SenderEmail) &&
               !string.IsNullOrEmpty(settings.SenderPassword) &&
               !string.IsNullOrEmpty(settings.KindleEmail);
    }
}
