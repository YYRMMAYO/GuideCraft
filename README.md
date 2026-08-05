# GuideCraft — 引导式AI助手

像资深技术顾问一样，通过多轮对话帮你把模糊想法变成可运行的 Python 自动化脚本。

**简体中文** · [English](README.en.md)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-0B93E8?logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/desktop/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/YYRMMAYO/GuideCraft)](https://github.com/YYRMMAYO/GuideCraft/releases)

不会编程也能做自动化 — 产品、运营、创业者，把你的想法说出来，剩下的交给 GuideCraft。

---

## 核心特性

| 特性 | 说明 |
|------|------|
| 引导式对话 | AI 主动提问，8 个需求维度逐步澄清，每轮最多 2 问 |
| 引导进度可视化 | 对话顶部实时显示 8 维度收集进度、当前阶段与动画进度条 |
| 需求摘要 | 信息齐备后自动输出结构化中文需求文档 |
| 可运行产物 | 单文件 Python 脚本，含中文注释、main 入口、依赖清单 |
| 沙盒试运行 | 生成代码可一键本地试运行（AST 预检 + 子进程隔离 + 超时保护） |
| 用量统计 | 本地记录 API 调用：token / 缓存命中率 / 费用估算，统计面板可视化 |
| 缓存命中优化 | System Prompt 字节级稳定 + 对话摘要持久化，最大化 DeepSeek/Qwen 上下文缓存命中 |
| 自然语言迭代 | "加上错误处理" — AI 自动更新代码 |
| 本地持久化 | 会话与产物保存在本机 SQLite，重启可恢复 |
| Key 安全 | API Key 经 Windows DPAPI 加密；数据链 AES-256-GCM 字段级加密 |
| 双主题 | 浅色 / 深色主题运行时切换 |
| 中英双语 | 简体中文 / English 运行时切换 |
| 导航栏位置 | 侧边栏可在左 / 右，默认右侧 |
| 全球模型 | 千问 / DeepSeek / OpenAI / Claude / Gemini / GLM / Kimi / MiniMax / 混元 / Grok / Mistral，统一 OpenAI 兼容客户端 + 自定义端点 |
| 独立设置窗口 | 参考 Cursor / VS Code 左导航 + 右内容布局，卡片分组 + 开关控件，简约高级 |
| 一键导出 | 完整项目 ZIP（main.py + requirements.txt + README + 需求文档） |
| 动画引导 | 设置窗口淡入缩放、消息进出场动画、引导进度过渡动画 |
| 自动更新检查 | 一键查询 GitHub Releases 最新版本 |

## 它能帮你做什么

```
"我想做一个自动整理每天邮件的脚本"
        |
        v
AI: 好的，请告诉我邮件来源和整理频率？

"..."
        |
        v
AI: ### 需求摘要
    一句话目标：每天早上 8 点自动整理 Gmail 未读邮件...
    请确认或补充。

"确认"
        |
        v
AI: 已生成 Python 脚本（含完整代码 + 依赖说明）
        |
        v
一键导出 ZIP -> pip install && python main.py
```

## 快速开始

### 方式一：安装包（推荐）

1. 从 [GitHub Releases](https://github.com/YYRMMAYO/GuideCraft/releases) 下载 `GuideCraft-Setup-x.x.x.exe`
2. 双击安装，启动 GuideCraft
3. 点击右上角 设置：
   - 模型提供方：推荐 千问 Qwen（新用户有免费额度），或选择 DeepSeek / OpenAI / Claude / Gemini / GLM / Kimi / MiniMax / 混元 / Grok / Mistral 等任意主流模型，也支持自定义 OpenAI 兼容端点
   - 获取 API Key：各平台控制台（设置窗口内提供直达链接）
   - 填入并点击 测试连接
4. 在对话区描述你的自动化想法；生成代码后可点击「▶ 试运行」在本地沙盒执行
5. 在设置 → 用量统计 查看累计 token 消耗与缓存命中率

### 方式二：源码运行

```bash
git clone https://github.com/YYRMMAYO/GuideCraft.git
cd GuideCraft
dotnet run
```

## 技术栈

| 层 | 选型 |
|----|------|
| 框架 | .NET 10 + WPF |
| 架构 | MVVM（CommunityToolkit.Mvvm）+ 依赖注入 |
| 存储 | SQLite（Microsoft.Data.Sqlite）+ AES-256-GCM 字段级加密 |
| AI 接入 | OpenAI 兼容 REST（全球主流提供方 + 自定义端点），SSE 流式，429/5xx 自动重试 |
| 上下文缓存 | System Prompt 字节级稳定 + 摘要持久化，DeepSeek/Qwen 前缀缓存最大化命中 |
| 沙盒试运行 | AST 预检 + 子进程隔离 + 超时终止（参考 Cursor 沙盒模式） |
| 用量统计 | SQLite 本地记录 token / 缓存命中率 / 费用估算 |
| Markdown | Markdig 解析 + 自研 FlowDocument 渲染器 |
| 代码高亮 | AvalonEdit |
| 主题 | XAML ResourceDictionary + DynamicResource（浅 / 深色运行时切换） |
| 国际化 | XAML 字符串资源（zh-CN / en-US），运行时切换 |

## 构建与发布

```bash
dotnet build                              # 调试编译（0 警告 0 错误）

dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
# 产物 bin/Release/net10.0-windows/win-x64/publish/GuideCraft.exe

# 安装包（需 Inno Setup 6）
installer/setup.iss -> dist/GuideCraft-Setup-x.x.x.exe
```

## 使用须知

- 生成的代码需安装 Python 3.8+：`pip install -r requirements.txt && python main.py`
- API Key 仅保存在本机（DPAPI 加密），绝不上传
- 数据文件位置：`%AppData%\GuideCraft\guidecraft.db`（AES-256-GCM 加密存储）
- 完整安全审计：[SECURITY.md](SECURITY.md)

## 安全

| 项 | 状态 |
|----|------|
| API Key DPAPI 加密落盘 | OK |
| 数据链 AES-256-GCM 字段级加密 | OK |
| HTTPS 端点硬编码 | OK |
| 错误消息不含敏感信息 | OK |
| SQL 参数化 | OK |
| Markdown 渲染不含 HTML 执行 | OK |
| 无遥测无日志 | OK |

详见 [SECURITY.md](SECURITY.md)。

## 许可

[MIT](LICENSE) © 2026 YYRMMAYO