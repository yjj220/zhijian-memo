# 纸间 · Windows 桌面备忘录

把随手记贴在桌面，把有期限的事情交给提醒。

**纸间（ZhijianMemo）** 是一个面向 Windows 的桌面备忘录，包含独立便签和带截止时间的待办清单。支持中文界面，内容保存在本机，无需注册账号。

本项目基于 [BaeTab / StickyPad](https://github.com/BaeTab/stickypad_windows) 二次开发，沿用其便签基础，在此基础上增加独立待办面板、截止时间与提醒等功能。原作者版权声明及 MIT 许可证完整保留，具体见 [项目来源与许可](#项目来源与许可)。

**当前版本：2.4.2** · **Windows 10 / 11 x64** · **C# / .NET 8 / WPF**

## 功能

### 桌面便签

- 多张独立便签，可拖动、调整大小、换色、设置透明度和置顶。
- 富文本编辑，支持字号、粗体、斜体、列表等格式。
- 自动保存内容和窗口位置。
- 支持 Markdown、HTML 预览，以及 Markdown 文件关联编辑。
- 提供便签列表、搜索、标签、备份与导出。
- 便签关闭按钮用于隐藏，空白便签也会保留；明确删除后进入回收站。

### 待办与提醒

- 新建事项、补充说明、设置截止日期和时间。
- 提醒可选：截止时、提前 5 分钟、15 分钟、1 小时、1 天，或不提醒。
- 到期显示提醒窗口，并尝试发送托盘通知和提示音。
- 提醒窗口支持「已完成」「10 分钟后提醒」「知道了」。
- 按未完成、今天到期、逾期、已完成或全部事项筛选，支持文字搜索。
- 待办面板支持置顶，并记住位置和大小。
- 待办支持单独导入、导出备份。

### 日常使用

- 托盘常驻，隐藏窗口后仍可接收提醒。
- 单击或双击托盘图标，唤回已有便签；没有便签时打开待办面板。
- 支持可选的开机启动和可配置的全局快捷键。
- 便签与待办保存在本地，基础功能可离线使用。

## 开始使用

### 运行程序

如果本仓库已提供 Release，可在仓库的 **Releases** 页面下载 Windows x64 发布包，解压后双击 **`ZhijianMemo.exe`**。

按本文方式构建的发布包自带 .NET 运行时，无需安装开发工具。直接下载仓库源码并不会获得可运行的程序，需要按下方步骤构建。

Markdown / HTML 预览和相关 PDF 导出依赖 **Microsoft Edge WebView2 Runtime**。普通文本便签、待办和提醒不依赖这些预览功能。

### 常用操作

| 想做什么 | 操作 |
| --- | --- |
| 写便签 | 待办面板「写张便签」，或托盘右键「新建便签」 |
| 建立定时待办 | 待办面板「新建待办」，填写事项并设置截止时间 |
| 隐藏便签 | 点击便签右上角关闭按钮，或按 `Alt+F4` |
| 恢复便签 | 单击 / 双击托盘图标，或托盘右键「显示全部便签」 |
| 打开待办面板 | 托盘右键「待办与提醒」，或再次启动程序 |
| 开启开机启动 | 在「设置」中启用「开机自动启动」 |
| 完全退出 | 托盘右键「退出纸间」 |

默认全局快捷键：`Ctrl+Shift+N` 新建便签，`Ctrl+Shift+L` 打开便签列表。可在设置中修改或关闭。

更多说明见 [使用说明](使用说明.md)。

## 提醒如何工作

应用运行且电脑未休眠时，每 5 秒检查一次待办提醒。隐藏便签或待办面板不会停止检查。

- 程序退出、电脑关机或休眠时，不会唤醒电脑或在后台独立发送提醒。
- 下次启动或恢复运行时，会补发已到提醒时间、尚未确认且未完成的事项。
- 「知道了」和关闭提醒窗口会确认本次提醒，但不会将事项标记为完成。
- 「10 分钟后提醒」只推迟提醒时间，原截止时间保持不变。
- Windows 通知设置可能屏蔽托盘通知，应用内提醒窗口仍可用于处理事项。

**便签中的 Markdown 勾选清单与独立待办面板是两种记录方式。** 如需截止时间和通知，请在「待办与提醒」面板中创建事项。

## 从源码运行

开发环境需要 **Windows** 和 **.NET 8 SDK**。在项目根目录执行：

```powershell
dotnet restore stickypad.sln
dotnet run --project stickypad/stickypad.csproj
```

也可用 Visual Studio 打开 `stickypad.sln`；需要安装支持 .NET 8 的「.NET 桌面开发」工作负载。

### 运行测试

```powershell
dotnet test stickypad.Tests/stickypad.Tests.csproj -c Release
```

测试包含待办持久化、提醒确认与延后、备份恢复，以及便签隐藏、恢复、窗口重建和回收站等场景。窗口生命周期测试需要可创建 WPF 窗口的 Windows 环境。

### 构建免安装发布包

在项目根目录执行：

```powershell
dotnet publish stickypad/stickypad.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o "publish/纸间"
Copy-Item LICENSE, THIRD-PARTY-NOTICES.md, 使用说明.md -Destination "publish/纸间"
```

输出程序位于 `publish/纸间/ZhijianMemo.exe`。发布时请将许可证、第三方声明及使用说明一并附上。程序包含运行时，因此体积会大于仅依赖系统运行时的构建。

`publish/`、编译缓存和可执行文件已由 `.gitignore` 排除。GitHub 仓库用于保存源码，编译好的发布包可另行上传到 **Releases**。

### 修改 Markdown 编辑器

仓库已包含构建好的编辑器资源，普通 .NET 构建不需要 Node.js。只有修改 `tools/mdeditor/` 下的编辑器源码时，才需要重新生成资源：

```powershell
cd tools/mdeditor
npm ci
npm test
npm run build
Copy-Item dist/mdeditor.bundle.js ../../stickypad/Assets/mdeditor/mdeditor.bundle.js
```

## 数据与备份

默认数据目录：

```text
%LOCALAPPDATA%\ZhijianMemo\
├─ notes.db          便签与便签回收站
├─ tasks.json        待办、截止时间及提醒状态
├─ tasks.json.bak    待办的上一次写入备份
├─ settings.json     语言、窗口位置及其他设置
└─ logs\             运行日志
```

- **便签备份**：在托盘菜单中导出或导入。
- **待办备份**：在待办面板右上角的更多菜单中导出或导入。
- 待办文件采用原子替换写入，并保留上一份备份；发现主文件损坏时，尝试恢复备份并保留损坏文件。
- 若选择便签的 Markdown 文件夹存储模式，便签内容保存在选定文件夹中，待办仍保存在上述默认目录。

个人便签、待办、导出备份及日志不应上传到公开仓库。开发测试可通过环境变量 `ZHIJIAN_DATA_DIR` 指定独立数据目录。

## 项目结构

```text
stickypad/                 WPF 应用源码
├─ Models/                 便签、待办、设置等数据模型
├─ Views/                  便签、待办、提醒及设置窗口
├─ ViewModels/             便签和列表等界面逻辑
├─ Services/               存储、窗口管理、托盘与提醒服务
├─ Resources/              多语言资源
├─ Assets/                 内嵌编辑器等资源
└─ Utils/                  文本处理、渲染等辅助代码
stickypad.Tests/           自动化测试
tools/mdeditor/            Markdown 编辑器源码与构建工具
stickypad.sln              Visual Studio / .NET 解决方案
使用说明.md                用户使用说明
LICENSE                    MIT 许可证及原作者版权声明
THIRD-PARTY-NOTICES.md     上游来源与依赖说明
```

源码目录和部分内部命名保留了 `StickyPad`，便于追溯上游；面向用户的程序名称为「纸间」，可执行文件为 `ZhijianMemo.exe`。

## 项目来源与许可

纸间是 [StickyPad](https://github.com/BaeTab/stickypad_windows) 的衍生项目，基于上游声明版本 **2.4.0** 的源码进行定制，不是从零编写的独立实现。

| 来源 | 内容 |
| --- | --- |
| 沿用 StickyPad | 便签窗口、富文本编辑、Markdown 相关能力、颜色与置顶、本地便签存储、托盘管理、搜索及导出等基础功能 |
| 本项目新增或调整 | 简体中文资源、独立待办面板、截止时间、到期与延后提醒、待办备份恢复、便签隐藏与恢复行为、界面名称统一 |

本项目保留原作者 **BaeTab** 的版权声明和 [MIT 许可证](LICENSE)。分发源码或程序时，请保留许可证及版权声明；第三方依赖仍适用各自的许可。完整来源说明见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

感谢 StickyPad 及其依赖项目的作者。本定制版不会通过上游的自动更新机制替换自身。

