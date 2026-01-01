# 📚 Book Translator

<p align="center">
  <strong>🔄 Convert • 🌍 Translate • 📖 Read Anywhere</strong>
</p>

<p align="center">
  A desktop application that converts PDF books to EPUB format with built-in translation support — perfect for sending to your Kindle!
</p>

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 📄 **PDF to EPUB Conversion** | Seamlessly convert your PDF books to the popular EPUB format |
| 🌐 **Multi-Language Translation** | Translate your books into multiple languages before conversion |
| 📱 **Send to Kindle** | One-click email your converted books directly to your Kindle device |
| 💾 **Smart Preferences** | Remembers your last used directory and language preferences |
| 🎨 **Modern UI** | Beautiful, cross-platform interface built with Avalonia UI |

---

## 🚀 Getting Started



## 🚀 Building

### Prerequisites

- [.NET 10.0](https://dotnet.microsoft.com/download) or later
- A Kindle device (optional, for the Send to Kindle feature)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/hudsonventura/pdf_to_epub_conversor_and_translator.git
   cd pdf_to_epub_conversor_and_translator
   ```

2. **Build the project**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/BookTranslator.csproj
   ```

---

## 📖 How to Use

### Basic Conversion
1. 🖱️ Click **"Browse..."** to select your PDF file
2. ⚡ Click **"Convert to EPUB"** to start the conversion
3. ✅ Your EPUB file will be saved in the same directory as the original PDF

### 🌍 Translate Your Books
> **Perfect for reading foreign books in your native language or learning a new language!**

1. ✅ Check the **"Translate before converting"** checkbox
2. 🌐 Select your target language from the dropdown menu
3. 📄 Select your PDF file and convert
4. 🎉 Your book will be translated and converted to EPUB!

### 📱 Send to Kindle
> **Read your translated books on your Kindle device!**

1. 📧 Configure your email settings in `appsettings.json`
2. 📄 Convert your PDF to EPUB (with or without translation)
3. 📨 Click **"Send to Kindle"** to email the book to your Kindle
4. 📚 Your book will appear in your Kindle library!

---

## ⚙️ Configuration

### Email Settings (for Kindle delivery)

Edit the `src/appsettings.json` file to configure your email settings:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password"
  }
}
```

> ⚠️ **Note:** For Gmail, you'll need to use an [App Password](https://support.google.com/accounts/answer/185833) instead of your regular password.

---

## 🌐 Supported Languages

The translation feature supports a wide variety of languages including:

| Language | Code | Language | Code |
|----------|------|----------|------|
| 🇺🇸 English | `en` | 🇧🇷 Portuguese | `pt` |
| 🇪🇸 Spanish | `es` | 🇫🇷 French | `fr` |
| 🇩🇪 German | `de` | 🇮🇹 Italian | `it` |
| 🇯🇵 Japanese | `ja` | 🇨🇳 Chinese | `zh` |
| 🇰🇷 Korean | `ko` | 🇷🇺 Russian | `ru` |

*...and many more!*

---

## 🛠️ Tech Stack

| Technology | Purpose |
|------------|---------|
| ![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet&logoColor=white) | Runtime & Framework |
| ![Avalonia](https://img.shields.io/badge/Avalonia-11.3-8B44AC?style=flat) | Cross-Platform UI |
| ![PdfPig](https://img.shields.io/badge/PdfPig-0.1.12-FF6B6B?style=flat) | PDF Processing |
| ![EpubCore](https://img.shields.io/badge/EpubCore-1.5.0-4CAF50?style=flat) | EPUB Generation |
| ![GTranslate](https://img.shields.io/badge/GTranslate-2.3.1-4285F4?style=flat) | Translation Engine |

---

## 📁 Project Structure

```
📦 pdf_to_epub_conversor_and_translator
├── 📂 src
│   ├── 📜 Program.cs              # Application entry point
│   ├── 🪟 MainWindow.axaml        # Main UI window
│   ├── ℹ️ AboutWindow.axaml        # About dialog
│   ├── 📂 Services
│   │   ├── 📄 PdfToMarkdownConverter.cs    # PDF text extraction
│   │   ├── 📕 MarkdownToEpubConverter.cs   # EPUB generation
│   │   ├── 🌐 TranslationService.cs        # Translation logic
│   │   ├── 📧 EmailService.cs              # Kindle email delivery
│   │   └── ⚙️ PreferencesService.cs        # User preferences
│   └── 📂 ViewModels
│       └── 🎛️ MainWindowViewModel.cs       # UI logic
└── 📜 README.md
```

---

## 🤝 Contributing

Contributions are welcome! Feel free to:

1. 🍴 Fork the repository
2. 🌿 Create a feature branch (`git checkout -b feature/amazing-feature`)
3. 💾 Commit your changes (`git commit -m 'Add amazing feature'`)
4. 📤 Push to the branch (`git push origin feature/amazing-feature`)
5. 🔃 Open a Pull Request

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).

---

## 💡 Tips & Tricks

- 📊 **Large PDFs**: For very large books, translation may take some time. The progress bar will keep you informed!
- 🔤 **Better Results**: PDFs with clean, selectable text produce the best conversion results
- 📧 **Gmail Users**: Remember to enable 2FA and create an App Password for the Send to Kindle feature

---

## 🙋 FAQ

<details>
<summary><b>Why isn't my PDF converting correctly?</b></summary>

Some PDFs, especially scanned documents or those with complex layouts, may not convert perfectly. For best results, use PDFs with selectable text.
</details>

<details>
<summary><b>How do I find my Kindle email address?</b></summary>

1. Go to [Amazon's Manage Your Content and Devices](https://www.amazon.com/hz/mycd/digital-console/contentlist/allcontent)
2. Click on the "Devices" tab
3. Select your Kindle device
4. Find your Kindle email (ends with @kindle.com)
</details>

<details>
<summary><b>Is the translation feature free?</b></summary>

Yes! The translation uses free translation services. However, there may be rate limits for very large books.
</details>

---

<p align="center">
  Made with ❤️ for book lovers everywhere
</p>

<p align="center">
  ⭐ Star this repo if you find it useful!
</p>
