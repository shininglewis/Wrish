# Wrish

一款为 Windows 打造的极简沉浸式写作工具。

> 没有菜单栏、没有工具栏、没有干扰。只有你，和文字。

---

## 预览

![Wrish 暗黑模式](screenshots/dark.png)
![Wrish 明亮模式](screenshots/light.png)

---

## 两个版本

本项目提供两个实现版本，功能完全一致，根据你的需求选择：

| | **WPF 版** | **Tauri 版** |
|--|-----------|-------------|
| **路径** | `Wrish/` | `wrish-tauri/` |
| **技术栈** | C# + WPF + WinForms | Rust + Tauri + HTML/CSS/JS |
| **体积** | ~400KB（需 .NET 10） | **~2-3MB（完全独立）** |
| **依赖** | .NET 10 Desktop Runtime | **仅 WebView2（Win10/11 预装）** |
| **前端渲染** | WPF RichTextBox | WebView2 + CSS |
| **系统 API** | .NET P/Invoke | Rust 原生 |
| **适用场景** | 已装 .NET 的环境 | **零依赖部署** |

---

## 功能特性

### 极致沉浸的界面
- **零边框窗口**：完全无系统边框，透明背景
- **全屏编辑器**：整个窗口只有一个占满空间的编辑器
- **自由缩放**：支持拖拽改变窗口大小
- **字体无极调整**：`Ctrl + 鼠标滚轮` 实时调整字体大小（10px ~ 72px）

### 打字机模式与焦点暗淡
- **绝对居中锁定**：光标所在行始终锁定在窗口可视区域垂直正中央
- **段落级高亮**：光标所在段落正常高亮，其余所有段落自动暗淡融入背景
- **平滑滚动**：基于 `offsetTop` 的精准定位 + CSS `scroll-behavior: smooth`

### 主题切换
- `Ctrl + T` 一键切换暗黑 / 明亮模式
- **暗黑模式**：背景 `#1E1E1E`，高亮文本 `#D4D4D4`，暗淡 `#3C3C3C`
- **明亮模式**：背景 `#FFFFFF`，高亮文本 `#1E1E1E`，暗淡 `#DCDCDC`

### 文档管理与智能自动保存
| 快捷键 | 功能 |
|--------|------|
| `Ctrl + N` | 新建文档 |
| `Ctrl + O` | 打开文档（支持 `.txt` / `.md`） |
| `Ctrl + S` | 保存文档（未命名时自动弹出另存为） |
| `Ctrl + Shift + S` | 另存为 |
| **自动保存** | 键盘输入停顿 2000ms 后自动触发，已命名文档静默覆盖，未命名文档写入 `.temp_draft.txt` |

### 极简字数统计
- 右下角显示当前字数（仅数字，如 `2500`）
- 默认透明度仅 `0.08`，绝不喧宾夺主
- `F4` 一键隐藏 / 显示

### 老板键与系统托盘
- `Esc` 键瞬间隐藏窗口，无动画延迟
- 系统托盘驻留 `wrish.ico` 图标
- **双击托盘**：恢复窗口
- **右键托盘**：显示 / 退出

---

## 下载与使用

### 方式一：下载 Release（推荐）
前往 [Releases](https://github.com/shininglewis/Wrish/releases) 页面下载：
- **WPF 版**：`Wrish.exe`（需 [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)）
- **Tauri 版**：`wrish-tauri.exe`（完全独立，无依赖）

### 方式二：自行编译 WPF 版

```bash
# 编译
dotnet build Wrish/Wrish.csproj

# 运行
dotnet run --project Wrish/Wrish.csproj

# 发布（框架依赖，约 400KB）
dotnet publish Wrish/Wrish.csproj -c Release -o publish --self-contained false

# 发布（完全独立，约 150MB）
dotnet publish Wrish/Wrish.csproj -c Release -o publish --self-contained true
```

> 需要 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 方式三：自行编译 Tauri 版

```bash
# 1. 安装 Rust
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh

# 2. 安装 Tauri CLI
cargo install tauri-cli

# 3. 编译（注意：必须使用 cargo tauri build，不是 cargo build！）
cd wrish-tauri
cargo tauri build --release

# 输出: target/release/wrish-tauri.exe（约 2-3MB）
```

> 需要 [Rust](https://rustup.rs/) 和 Windows WebView2（Win10/11 默认已装）

---

## 快捷键速查

| 快捷键 | 功能 |
|--------|------|
| `Ctrl + N` | 新建文档 |
| `Ctrl + O` | 打开文档 |
| `Ctrl + S` | 保存 |
| `Ctrl + Shift + S` | 另存为 |
| `Ctrl + T` | 切换暗黑/明亮主题 |
| `Ctrl + 滚轮` | 调整字体大小 |
| `F4` | 显示/隐藏字数统计 |
| `Esc` | 老板键（隐藏到托盘） |
| `Ctrl + Z` | 撤销 |
| `Ctrl + Y` | 重做 |
| `Ctrl + A` | 全选 |
| `Ctrl + C/V/X` | 复制/粘贴/剪切 |

---

## 技术栈

### WPF 版
- **.NET 10**
- **WPF** (`WindowStyle="None"`, `AllowsTransparency="True"`)
- **Windows Forms** (`NotifyIcon` 系统托盘)
- **RichTextBox** + 动态 `TextRange` 格式化

### Tauri 版
- **Rust** + **Tauri v2**（系统托盘、文件对话框、全局快捷键）
- **HTML5 `contenteditable`**（富文本编辑）
- **CSS 变量**（主题切换、焦点暗淡）
- **WebView2**（Windows 系统预装）

---

## 项目结构

```
wrish/
├── Wrish/                      # WPF 版本
│   ├── App.xaml                # 应用入口
│   ├── App.xaml.cs
│   ├── MainWindow.xaml         # 无边框窗口 + ScrollViewer + Padding 布局
│   ├── MainWindow.xaml.cs      # 核心逻辑（居中、暗淡、自动保存、托盘等）
│   └── Wrish.csproj            # 项目文件
├── wrish-tauri/                # Tauri 版本
│   ├── Cargo.toml              # Rust 项目配置 + Release 优化
│   ├── tauri.conf.json         # 窗口配置：无边框、透明、WebView2
│   ├── build.rs                # Tauri 构建脚本
│   ├── icons/
│   │   └── icon.ico            # 系统托盘图标
│   └── src/
│       ├── main.rs             # Rust 后端：托盘、文件、快捷键、自动保存
│       ├── index.html          # 前端入口：contenteditable 编辑器
│       ├── styles.css          # 主题、打字机居中、焦点暗淡
│       └── main.js             # 光标追踪、防抖滚动、快捷键处理
├── screenshots/
│   ├── dark.png                # 暗黑模式截图
│   └── light.png               # 明亮模式截图
├── .github/workflows/
│   ├── release.yml             # WPF 发布（v* tag）
│   └── release-tauri.yml       # Tauri 发布（tauri-v* tag）
├── wrish.ico                   # 程序图标
├── .gitignore
├── LICENSE                     # MIT
└── README.md                   # 本文件
```

---

## 自动发布

推送 tag 即可触发 GitHub Actions 自动编译并发布 Release：

```bash
# WPF 版
git tag v1.0.0
git push origin v1.0.0

# Tauri 版
git tag tauri-v1.0.0
git push origin tauri-v1.0.0
```

---

## 开源协议

[MIT License](LICENSE)

---

## 致谢

灵感来源于 [iA Writer](https://ia.net/writer)、[Typora](https://typora.io) 等极简写作工具。