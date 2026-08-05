<div align="center">

# ✦ GuideCraft · 引导式AI助手

**像一位资深技术顾问，通过多轮对话帮你把模糊想法变成可运行的 Python 自动化脚本**

<br/>

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-0B93E8?logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/desktop/)
[![DeepSeek](https://img.shields.io/badge/Model-DeepSeek%20V4--Flash-4D6BFE?logo=deepseek&logoColor=white)](https://platform.deepseek.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/YYRMMAYO/GuideCraft)](https://github.com/YYRMMAYO/GuideCraft/releases)

**不会编程也能做自动化** — 产品、运营、创业者，把你的想法说出来，剩下的交给 GuideCraft。

</div>

---

## ✨ 核心特性

| 特性 | 说明 |
|------|------|
| 🤖 **引导式对话** | AI 主动提问，逐步澄清用途、输入、输出、触发条件等 8 个需求维度，每轮最多 2 问 |
| 📋 **需求摘要** | 信息齐备后自动生成结构化中文需求文档，确认后才动手 |
| 🐍 **可运行产物** | 生成单文件 Python 脚本，含完整中文注释、main 入口、依赖清单 |
| 🔄 **迭代修改** | 用自然语言提修改意见，AI 自动更新代码 |
| 💾 **本地持久化** | 会话、产物保存在本机 SQLite，重启可恢复继续迭代 |
| 🔐 **Key 安全** | DeepSeek API Key 经 Windows DPAPI 加密存储，绝不明文落盘 |
| 🎨 **双主题** | 现代简洁 UI，浅色 / 深色主题运行时一键切换 |
| 📦 **一键导出** | 导出完整项目 ZIP（main.py + requirements.txt + README + 需求文档） |

## 🎯 它能帮你做什么

```
「我想做一个自动整理每天邮件的脚本」
        │
        ▼
🤖 好的，请告诉我邮件来源和整理频率？
        │
        ▼
「Gmail 邮箱，每天早上 8 点」
        │
        ▼
🤖 ### 需求摘要
   **一句话目标**：每天早上 8 点自动整理 Gmail 未读邮件…
   请确认以上理解是否正确？
        │
        ▼
「确认」
        │
        ▼
🤖 ✅ 已生成 Python 脚本（代码 + 依赖说明）
        │
        ▼
📦 一键导出项目 ZIP → pip install && python main.py
```

## 🖥️ 快速开始

### 方式一：安装包（推荐）

1. 从 [GitHub Releases](https://github.com/YYRMMAYO/GuideCraft/releases) 下载 `GuideCraft-Setup.exe`
2. 双击安装，启动 GuideCraft
3. 点击右上角 **⚙ 设置** → 打开 [DeepSeek 开放平台](https://platform.deepseek.com) 免费申请 API Key → 填入 → 测试连接
4. 在对话区描述你的自动化想法

### 方式二：源码运行

```bash
git clone https://github.com/YYRMMAYO/GuideCraft.git
cd GuideCraft
dotnet run
```

## 🛠️ 技术栈

| 层 | 选型 |
|----|------|
| 框架 | .NET 10 + WPF |
| 架构 | MVVM（CommunityToolkit.Mvvm）+ 依赖注入（Microsoft.Extensions.DependencyInjection） |
| 存储 | SQLite（Microsoft.Data.Sqlite）+ DPAPI 加密 |
| AI 接入 | DeepSeek OpenAI 兼容 API（HttpClient + System.Text.Json，SSE 流式） |
| Markdown | Markdig 解析 + 自研 FlowDocument 渲染器 |
| 代码高亮 | AvalonEdit |

## 📁 项目结构

```
GuideCraft/
├── App.xaml(.cs)               # 入口：DI 容器、主题、全局异常
├── MainWindow.xaml(.cs)        # 主窗口三区布局
├── Models/                     # 数据实体（会话/消息/产物/设置）
├── ViewModels/                 # MVVM 视图模型 + 引导式状态机
├── Services/
│   ├── DeepSeekApiClient       # DeepSeek API 客户端（流式 SSE）
│   ├── ChatService             # 引导式对话编排与阶段判定
│   ├── RequirementSummarizer   # 需求摘要生成
│   ├── CodeGenerator           # Python 代码生成与解析
│   ├── ProjectExporter         # 项目 ZIP 导出
│   ├── LocalStorageService     # SQLite 持久化
│   ├── SettingsService         # DPAPI 加密设置
│   ├── PromptTemplates         # 核心 System Prompt（产品差异化）
│   └── ThemeManager            # 主题运行时切换
├── Views/                      # 设置对话框、代码块控件
├── Controls/                   # 自研 Markdown 渲染控件
├── Themes/                     # 浅/深色颜色字典 + 全局样式
├── Converters/                 # XAML 值转换器
└── Utils/                      # 工具类
```

## 🏗️ 构建与发布

```bash
# 调试编译
dotnet build

# 发布自包含单文件
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# 产物位置
bin/Release/net10.0-windows/win-x64/publish/GuideCraft.exe
```

## ⚠️ 使用须知

- 生成的代码需安装 **Python 3.8+** 后运行：`pip install -r requirements.txt && python main.py`
- DeepSeek API 按用量计费，`deepseek-v4-flash` 模型价格极低（约 1 元 / 百万 tokens 输入）
- 数据文件位置：`%AppData%\GuideCraft\guidecraft.db`
- API Key 仅保存在本机，不会上传

## 📄 许可

[MIT](LICENSE) © 2026 YYRMMAYO
