<div align="center">

# ✦ GuideCraft · 引导式AI助手

**像一位资深技术顾问，通过多轮对话帮你把模糊想法变成可运行的 Python 自动化脚本**

<br/>

**简体中文** · 🌐 [English](README.en.md)

<br/>

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-0B93E8?logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/desktop/)
[![Qwen](https://img.shields.io/badge/Model-千问%20%2F%20DeepSeek-4D6BFE)](https://dashscope.aliyun.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/YYRMMAYO/GuideCraft)](https://github.com/YYRMMAYO/GuideCraft/releases)

**不会编程也能做自动化** — 产品、运营、创业者，把你的想法说出来，剩下的交给 GuideCraft。

</div>

---

## ✨ 核心特性（v1.1.0）

| 特性 | 说明 |
|------|------|
| 🤖 **引导式对话** | AI 主动提问，8 个需求维度逐步澄清，每轮≤2 问 |
| 📋 **需求摘要** | 信息齐备后自动输出结构化中文需求文档 |
| 🐍 **可运行产物** | 单文件 Python 脚本（含中文注释、`main()` 入口、依赖清单） |
| 🔄 **自然语言迭代** | 「加上错误处理」→ AI 自动更新代码 |
| 💾 **本地持久化** | 会话/产物保存在本机 SQLite，重启可恢复 |
| 🔐 **Key 安全** | DeepSeek API Key 经 Windows DPAPI 加密 |
| 🎨 **双主题** | 浅色 / 深色主题运行时切换（v1.1 对比度大幅优化） |
| 🌍 **中英双语** | 简体中文 / English 运行时切换 |
| 🧭 **导航栏位置** | 侧边栏可在左/右，默认右侧 |
| 🔌 **多模型支持** | **千问 Qwen**（免费额度优先）+ DeepSeek，统一客户端 |
| 📦 **一键导出** | 完整项目 ZIP（`main.py` + `requirements.txt` + README + 需求文档） |
| 🎓 **首次引导教程** | 4 步演示动画 + 圆点指示器 + 淡入位移缩放过渡 |
| 🔄 **自动更新检查** | 一键查询 GitHub Releases 最新版本 |

## 🎯 它能帮你做什么

```
「我想做一个自动整理每天邮件的脚本」
        │
        ▼
🤖 好的，请告诉我**邮件来源**和**整理频率**？
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

1. 从 [GitHub Releases](https://github.com/YYRMMAYO/GuideCraft/releases) 下载 `GuideCraft-Setup-1.1.0.exe`
2. 双击安装，启动 GuideCraft
3. 点击右上角 ⚙ **设置**：
   - **模型提供方**：推荐**千问 Qwen**（新用户有免费额度），或选择 **DeepSeek**
   - 获取 API Key：[阿里云百炼](https://bailian.console.aliyun.com/) / [DeepSeek 开放平台](https://platform.deepseek.com)（免费额度即可日常使用）
   - 填入 → **测试连接**
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
| 架构 | MVVM（`CommunityToolkit.Mvvm`）+ 依赖注入 |
| 存储 | SQLite（`Microsoft.Data.Sqlite`）+ DPAPI 加密 |
| AI 接入 | **OpenAI 兼容 REST**（千问 DashScope / DeepSeek），SSE 流式 |
| Markdown | Markdig 解析 + 自研 `FlowDocument` 渲染器 |
| 代码高亮 | AvalonEdit |
| 主题 | XAML `ResourceDictionary` + `DynamicResource`（浅/深色运行时切换） |
| 国际化 | XAML 字符串资源（zh-CN / en-US），运行时切换 |

## 📁 项目结构

```
GuideCraft/
├── App.xaml(.cs)               # 入口：DI 容器、主题、语言、全局异常
├── MainWindow.xaml(.cs)        # 三区布局 + 首次引导动画 + 导航栏位置切换
├── Models/                     # 数据实体
├── ViewModels/                 # MVVM 视图模型 + 引导式状态机
├── Services/
│   ├── LlmApiClient            # OpenAI 兼容大模型客户端（千问 / DeepSeek / 任意兼容端点）
│   ├── LlmCatalog              # 提供方与模型的单一事实源
│   ├── ChatService             # 引导式对话编排与阶段判定
│   ├── RequirementSummarizer   # 需求摘要生成
│   ├── CodeGenerator           # 代码生成与解析
│   ├── ProjectExporter         # ZIP 导出
│   ├── LocalStorageService     # SQLite 持久化
│   ├── SettingsService         # DPAPI 加密 + 主题/语言/导航栏位置
│   ├── UpdateChecker           # GitHub Releases 版本检查
│   ├── PromptTemplates         # 核心 System Prompt（产品差异化）
│   └── ThemeManager            # 主题运行时切换
├── Views/                      # SettingsDialog、CodeBlockView
├── Controls/                   # 自研 MarkdownView（FlowDocument 渲染）
├── Localization/               # zh-CN / en-US 字符串资源 + LocalizationManager
├── Themes/                     # 浅/深色颜色字典 + 全局样式
├── Converters/                 # XAML 值转换器
├── Utils/                      # 工具类（UiStrings、PasswordBoxHelper）
├── Assets/app.ico              # 应用图标
└── installer/setup.iss         # Inno Setup 安装脚本
```

## 🏗️ 构建与发布

```bash
# 调试编译
dotnet build

# 发布自包含单文件
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# 产物
bin/Release/net10.0-windows/win-x64/publish/GuideCraft.exe

# 安装包（需 Inno Setup 6）
installer/setup.iss → dist/GuideCraft-Setup-1.1.0.exe
```

## ⚠️ 使用须知

- 生成的代码需安装 **Python 3.8+**：`pip install -r requirements.txt && python main.py`
- API Key 仅保存在本机（DPAPI 加密），绝不上传
- 数据文件：`%AppData%\GuideCraft\guidecraft.db`
- 完整安全审计：[SECURITY.md](SECURITY.md)

## 🔒 安全

| 项 | 状态 |
|----|------|
| API Key 落盘加密（DPAPI） | ✅ |
| HTTPS 端点硬编码 | ✅ |
| 错误消息不含敏感信息 | ✅ |
| SQL 参数化 | ✅ |
| Markdown 渲染不含 HTML 执行 | ✅ |
| 无遥测无日志 | ✅ |

详见 [SECURITY.md](SECURITY.md)。

## 📄 许可

[MIT](LICENSE) © 2026 YYRMMAYO