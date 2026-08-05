<div align="center">

# ✦ GuideCraft · Guided AI Assistant

**Like a senior technical consultant — turn vague ideas into runnable Python automation through Q&A**

<br/>

🌐 [简体中文](README.md) · **English**

<br/>

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-0B93E8?logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/desktop/)
[![Qwen](https://img.shields.io/badge/Model-Qwen%20%2F%20DeepSeek-4D6BFE)](https://dashscope.aliyun.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/YYRMMAYO/GuideCraft)](https://github.com/YYRMMAYO/GuideCraft/releases)

**No programming required.** Product, ops, and founder friendly — say what you want, GuideCraft does the rest.

</div>

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🤖 **Guided Q&A** | The AI actively asks about purpose, inputs, outputs, trigger conditions, etc., in 8 dimensions |
| 📋 **Requirement Summary** | Auto-generates a structured requirement document once information is complete |
| 🐍 **Runnable Output** | Single-file Python script with comments, `main()` entry, and dependencies list |
| 🔄 **Iterate by Words** | "Add error handling" — the AI updates the code naturally |
| 💾 **Local Persistence** | Conversations saved to local SQLite; restart and continue |
| 🔐 **Key Security** | API Key encrypted with Windows DPAPI (CurrentUser scope) |
| 🎨 **Dual Themes** | Light / dark theme with smooth runtime switching |
| 🌍 **Bilingual UI** | 中文 / English runtime switchable |
| 📦 **One-click Export** | Export the complete project as a ZIP |
| 🧭 **Configurable Sidebar** | Left or right placement, default right |
| 🎓 **First-run Tutorial** | 4-step animated walkthrough for new users |

## 🎯 What it does for you

```
"I want a script that auto-organizes my daily emails"
        │
        ▼
🤖 Great — please tell me your email provider and frequency?
        │
        ▼
"Gmail, every morning at 8 AM"
        │
        ▼
🤖 ### Requirement Summary
   **Goal**: auto-organize unread Gmail emails every morning at 8 AM...
   Please confirm or refine.
        │
        ▼
"Confirm"
        │
        ▼
🤖 ✅ Python script generated (full code + dependencies)
        │
        ▼
📦 One-click export ZIP → pip install && python main.py
```

## 🖥️ Quick Start

### Option A: Installer (recommended)

1. Download `GuideCraft-Setup-1.1.0.exe` from [Releases](https://github.com/YYRMMAYO/GuideCraft/releases)
2. Double-click to install, then launch GuideCraft
3. Click ⚙ **Settings** in the top-right corner:
   - Choose your provider: **Qwen (recommended · free tier)** or **DeepSeek**
   - Get an API Key from [Alibaba Bailian](https://bailian.console.aliyun.com/) or [DeepSeek Platform](https://platform.deepseek.com) (free tier is enough)
   - Paste it and click **Test Connection**
4. Describe your automation idea in the chat area

### Option B: From source

```bash
git clone https://github.com/YYRMMAYO/GuideCraft.git
cd GuideCraft
dotnet run
```

## 🛠️ Tech Stack

| Layer | Choice |
|-------|--------|
| Framework | .NET 10 + WPF |
| Architecture | MVVM (`CommunityToolkit.Mvvm`) + Dependency Injection |
| Storage | SQLite (`Microsoft.Data.Sqlite`) + DPAPI encryption |
| AI Backend | OpenAI-compatible REST (Qwen DashScope / DeepSeek), SSE streaming |
| Markdown | Markdig parser + custom `FlowDocument` renderer |
| Code Highlighting | AvalonEdit |
| Theming | XAML `ResourceDictionary` + `DynamicResource` (light / dark, runtime swap) |
| Localization | XAML string resources (zh-CN / en-US), runtime switch |

## 📁 Project Structure

```
GuideCraft/
├── App.xaml(.cs)               # Entry: DI, theme, language, global exception handler
├── MainWindow.xaml(.cs)        # Three-zone layout + first-run tutorial animation
├── Models/                     # Data entities (conversations, messages, settings)
├── ViewModels/                 # MVVM view-models + guided conversation state machine
├── Services/
│   ├── LlmApiClient            # OpenAI-compatible client (Qwen / DeepSeek / any)
│   ├── LlmCatalog              # Single source of truth for providers and models
│   ├── ChatService             # Guided Q&A orchestration & phase detection
│   ├── RequirementSummarizer   # Generate structured requirement doc
│   ├── CodeGenerator           # Generate & parse Python source
│   ├── ProjectExporter         # ZIP packaging
│   ├── LocalStorageService     # SQLite DAO
│   ├── SettingsService         # DPAPI-encrypted settings (Key, language, theme, ...)
│   ├── UpdateChecker           # GitHub Releases latest-version check
│   ├── PromptTemplates         # System prompts (product differentiation)
│   └── ThemeManager            # Theme runtime switching
├── Views/                      # SettingsDialog, CodeBlockView
├── Controls/                   # Custom MarkdownView (FlowDocument)
├── Localization/               # zh-CN / en-US string resources + LocalizationManager
├── Themes/                     # Light / Dark color dictionaries + global styles
├── Converters/                 # XAML value converters
├── Utils/                      # Utilities (UiStrings facade, PasswordBoxHelper)
├── Assets/app.ico              # Application icon
└── installer/setup.iss         # Inno Setup installer script
```

## 🏗️ Build & Publish

```bash
# Debug build
dotnet build

# Self-contained single-file release
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Output
bin/Release/net10.0-windows/win-x64/publish/GuideCraft.exe

# Installer (requires Inno Setup 6)
"installer/setup.iss" → dist/GuideCraft-Setup-1.1.0.exe
```

## ⚠️ Notes

- Generated code requires **Python 3.8+**: `pip install -r requirements.txt && python main.py`
- API Key is stored locally only (DPAPI encrypted). It is never uploaded.
- Data file location: `%AppData%\GuideCraft\guidecraft.db`
- See [SECURITY.md](SECURITY.md) for the full security audit.

## 📄 License

[MIT](LICENSE) © 2026 YYRMMAYO