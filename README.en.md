# GuideCraft — Guided AI Assistant

Like a senior technical consultant — turn vague ideas into runnable Python automation through Q&A.

[简体中文](README.md) · **English**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-0B93E8?logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/desktop/)
[![Qwen](https://img.shields.io/badge/Model-Qwen%20%2F%20DeepSeek-4D6BFE)](https://dashscope.aliyun.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/YYRMMAYO/GuideCraft)](https://github.com/YYRMMAYO/GuideCraft/releases)

No programming required. Describe what you want automated; GuideCraft does the rest.

---

## Features

| Feature | Description |
|---------|-------------|
| Guided Q&A | AI actively asks across 8 requirement dimensions, max 2 questions per turn |
| Requirement Summary | Auto-generates a structured requirement document once information is complete |
| Runnable Output | Single-file Python script with comments, main entry, and dependencies |
| Iterate by Words | "Add error handling" — the AI updates the code naturally |
| Local Persistence | Conversations stored in SQLite, encrypted at rest |
| Key Security | API Key encrypted with Windows DPAPI |
| Dual Themes | Light / dark theme with smooth runtime switching |
| Bilingual UI | 中文 / English runtime switchable |
| Configurable Sidebar | Left or right placement, default right |
| Multi-Model | Qwen (recommended, free tier) and DeepSeek via unified OpenAI-compatible client |
| One-click Export | Export the complete project as a ZIP |
| First-run Tutorial | 4-step animated walkthrough for new users |
| Auto Update Check | One-click check against GitHub Releases |

## What it does

```
"I want a script that auto-organizes my daily emails"
        |
        v
AI: Great — please tell me your email provider and frequency?

"..."
        |
        v
AI: ### Requirement Summary
    Goal: auto-organize unread Gmail emails every morning at 8 AM...
    Please confirm or refine.

"Confirm"
        |
        v
AI: Python script generated (full code + dependencies)
        |
        v
One-click export ZIP -> pip install && python main.py
```

## Quick Start

### Option A: Installer (recommended)

1. Download `GuideCraft-Setup-x.x.x.exe` from [Releases](https://github.com/YYRMMAYO/GuideCraft/releases)
2. Double-click to install, launch GuideCraft
3. Click Settings in the top-right corner:
   - Choose Qwen (recommended, free tier) or DeepSeek
   - Get an API Key from [Alibaba Bailian](https://bailian.console.aliyun.com/) or [DeepSeek Platform](https://platform.deepseek.com)
   - Paste it and click Test Connection
4. Describe your automation idea in the chat

### Option B: From source

```bash
git clone https://github.com/YYRMMAYO/GuideCraft.git
cd GuideCraft
dotnet run
```

## Tech Stack

| Layer | Choice |
|-------|--------|
| Framework | .NET 10 + WPF |
| Architecture | MVVM (CommunityToolkit.Mvvm) + Dependency Injection |
| Storage | SQLite (Microsoft.Data.Sqlite) with AES-256-GCM field encryption |
| AI Backend | OpenAI-compatible REST (Qwen DashScope / DeepSeek), SSE streaming |
| Markdown | Markdig parser + custom FlowDocument renderer |
| Code Highlighting | AvalonEdit |
| Theming | XAML ResourceDictionary with DynamicResource (light / dark, runtime swap) |
| Localization | XAML string resources (zh-CN / en-US), runtime switch |

## Build and Publish

```bash
dotnet build                              # Debug build (0 warnings 0 errors)

dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
# Output: bin/Release/net10.0-windows/win-x64/publish/GuideCraft.exe

# Installer (requires Inno Setup 6)
installer/setup.iss -> dist/GuideCraft-Setup-x.x.x.exe
```

## Notes

- Generated code requires Python 3.8+: `pip install -r requirements.txt && python main.py`
- API Key is stored locally only (DPAPI encrypted). Never uploaded.
- Data file: `%AppData%\GuideCraft\guidecraft.db` (AES-256-GCM encrypted)
- Full security audit: [SECURITY.md](SECURITY.md)

## Security

| Item | Status |
|------|--------|
| API Key encrypted at rest (DPAPI) | OK |
| Database field encryption (AES-256-GCM) | OK |
| HTTPS-only endpoints | OK |
| Error messages contain no sensitive data | OK |
| Parameterized SQL queries | OK |
| Markdown rendering safe (no HTML execution) | OK |
| No telemetry, no logging | OK |

See [SECURITY.md](SECURITY.md) for the full audit.

## License

[MIT](LICENSE) © 2026 YYRMMAYO