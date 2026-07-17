# Wrish

一款为 Windows 打造的极简沉浸式写作工具。

> 没有菜单栏、没有工具栏、没有干扰。只有你，和文字。

---

## 预览

<!-- 建议替换为实际截图 -->
<!-- ![Wrish 暗黑模式](screenshots/dark.png) -->
<!-- ![Wrish 明亮模式](screenshots/light.png) -->

## 功能特性

### 极致沉浸的界面
- **零边框窗口**：`WindowStyle="None"` + `AllowsTransparency="True"`，完全无系统边框
- **全屏编辑器**：整个窗口只有一个占满空间的 RichTextBox
- **自由缩放**：支持按住 6px 边缘拖拽改变窗口大小
- **字体无极调整**：`Ctrl + 鼠标滚轮` 实时调整字体大小（8pt ~ 72pt）

### 打字机模式与焦点暗淡
- **绝对居中锁定**：光标所在行始终锁定在窗口可视区域垂直正中央
- **智能 Padding**：即使只有一行文本，也能精确居中
- **沉浸式焦点模式**：当前行正常高亮，其余行自动暗淡融入背景
- **防抖处理**：2px 像素阈值 + `DispatcherPriority.Render` 调度，消除输入抖动

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
前往 [Releases](https://github.com/YOUR_USERNAME/wrish/releases) 页面下载最新版 `Wrish.exe`。

**运行要求**：Windows 10/11 x64，且已安装 [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

### 方式二：自行编译

```bash
# 克隆仓库
git clone https://github.com/YOUR_USERNAME/wrish.git
cd wrish

# 编译
dotnet build Wrish/Wrish.csproj

# 运行
dotnet run --project Wrish/Wrish.csproj

# 发布（框架依赖，约 400KB）
dotnet publish Wrish/Wrish.csproj -c Release -o publish --self-contained false

# 发布（完全独立，约 150MB，无需 .NET Runtime）
dotnet publish Wrish/Wrish.csproj -c Release -o publish --self-contained true
```

> **注意**：项目使用 `net10.0-windows` 目标框架，请确保本地已安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

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

- **.NET 10**
- **WPF** (`WindowStyle="None"`, `AllowsTransparency="True"`)
- **Windows Forms** (`NotifyIcon` 系统托盘)
- **RichTextBox** + 动态 `TextRange` 格式化

---

## 项目结构

```
wrish/
├── Wrish/
│   ├── App.xaml              # 应用入口
│   ├── App.xaml.cs
│   ├── MainWindow.xaml       # 无边框窗口 + ScrollViewer + Padding 布局
│   ├── MainWindow.xaml.cs    # 核心逻辑（居中、暗淡、自动保存、托盘等）
│   └── Wrish.csproj          # 项目文件
├── wrish.ico                 # 程序图标
├── .gitignore
├── LICENSE                   # MIT
└── README.md                 # 本文件
```

---

## 开源协议

[MIT License](LICENSE)

---

## 致谢

灵感来源于 [iA Writer](https://ia.net/writer)、[Typora](https://typora.io) 等极简写作工具。