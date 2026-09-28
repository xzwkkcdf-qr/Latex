# LatexVault — LaTeX 编辑存储管理器设计

日期：2026-09-28  
状态：待用户审阅  
技术栈：C# / .NET 8 / WPF  

## 1. 目标与范围

个人 LaTeX 知识库：在本地真实文件夹树上管理 `.tex`，内置编辑与编译预览。视觉对齐 `E:\TPS助手\PMTAll\PMTAll\Themes\Registration` 的黑白杂志线条风格。

### 已确认决策

| 项 | 选择 |
|----|------|
| 核心定位 | 个人知识库（管理 + 编辑 + 归档） |
| 编译 | 本机 `latexmk` / `xelatex` / `pdflatex` + 内嵌 PDF 预览 |
| 库组织 | 任意深度树；支持拖拽移动、重命名 |
| 文档形态 | 以单个 `.tex` 为主；同目录可放图片等附属文件 |
| 体验优先级 | 库管理 / 编辑 / 编译预览均达日常可用 |
| 架构 | 单体 WPF + AvalonEdit + PDFium 内嵌预览 |

### 非目标（第一版不做）

- 云同步、多人协作、Git 集成 UI
- 完整 TeX 语义解析 / 补全服务器（可后续加）
- 多文件「项目」向导（`main.tex` 工程模板）
- UI 自动化测试套件

## 2. 壳与布局

### 窗口

- 无边框自定义标题栏（Registration 式）：CaptionHeight 40 DIP，直角，线稿最小化 / 最大化 / 关闭
- 背景纸白 `#FFFFFF`，前景墨色 `#111111`
- 默认约 1280×900，可最大化

### 工作区结构

自上而下：标题栏 → 工具条 → 三栏主区 → 状态栏。

| 区域 | 尺寸 | 内容 |
|------|------|------|
| 左栏「库」 | 约 280px，可拖拽调宽 | 库根路径、文件夹+`.tex` 树、新建文件/文件夹、名称过滤 |
| 中栏「编辑」 | 弹性 | 多标签 AvalonEdit（行号、LaTeX 高亮、查找替换）；未保存圆点 |
| 右栏「预览」 | 约 360px，可隐藏/调宽 | 内嵌 PDF；编译中为线稿进度态 |

### 顶栏动作

打开库 · 保存 · 编译 · 引擎下拉（latexmk / xelatex / pdflatex）· 显示或隐藏预览。  
主按钮墨底白字；次级按钮白底细线描边。

### 底栏

当前文件路径 · 编译状态一行 · 当前引擎名。

### 视觉令牌（自 Registration 移植，前缀 `LatexVault*`）

- Ink `#111111` · Paper `#FFFFFF` · PaperMuted `#F2F2F2`
- Line `#D6D6D6` · StructureLine `#C8C8C8`
- Body：Segoe UI Variable Text / Microsoft YaHei UI
- Display：Georgia / Microsoft YaHei UI
- Mono：Cascadia Mono / Consolas
- 无圆角、细线分隔、杂志风章节标题（Display 小号）

## 3. 模块与数据流

### 目录结构

```
LatexVault/
  Themes/       # LatexVaultTokens / Controls / Theme
  Views/        # Shell、Workspace、确认/设置对话框
  ViewModels/   # Shell、LibraryTree、EditorTabs、Compile、Preview
  Services/     # Library、FileIO、Compile、Pdf、Settings
  Models/       # LibraryNode、DocumentTab、CompileResult
```

### 职责

| 模块 | 职责 | 边界 |
|------|------|------|
| `LibraryService` | 扫树、`FileSystemWatcher`、新建/重命名/移动/删除 | 不解析 TeX |
| `EditorSession` | 多标签缓冲、脏标记、保存、关闭确认 | 不直接调编译 |
| `CompileService` | 引擎与参数、工作目录=`.tex` 所在目录、捕获输出 | 不画 UI |
| `PdfPreviewService` | 加载/刷新 PDF；经临时副本打开以免锁源文件 | 不编译 |
| `SettingsStore` | 库根、默认引擎、窗体几何 → `%AppData%\LatexVault\settings.json` | — |

### 主数据流

1. 启动 → 读设置 → 打开上次库根（无则引导选择）→ 建树  
2. 单击 `.tex` → 未打开则新建标签并读入；已打开则激活  
3. 保存 → 写回磁盘；可选「保存后自动编译」（设置项，默认关）  
4. 编译 → 进程执行 → 成功刷新 PDF；失败保留上次成功 PDF（若有）并展开日志抽屉  
5. 树拖拽/重命名 → 同步已打开标签路径；冲突时提示  

### 存储原则

库 = 真实文件系统，不另建文档数据库。应用仅持久化设置 JSON。

## 4. 错误处理与边界

### 编译

- 引擎不在 PATH：顶栏轻提示 + 引导到设置填写可执行路径  
- 失败：日志抽屉展开并定位首个 `error` / `!` 行  
- 编译中再次编译：取消旧进程再启动  
- PDF 占用：复制到 `%Temp%\LatexVault\` 再预览  

### 文件与库

- 外部增删改：刷新树；当前文件缺失则标签标「缺失」，内容可另存  
- 未保存关闭：杂志风确认框（保存 / 不保存 / 取消）  
- 非法拖拽（如拖入自身子树）：拒绝 + 底栏一句说明  
- 库根失效：提示重选；保留设置中旧路径  

### 编辑

- 大文件（>2MB）：可打开，状态栏提示高亮可能变慢  
- 编码：默认 UTF-8；保留 BOM；异常时按 UTF-8 并提示  

## 5. 测试策略（第一版）

- 单测：路径移动合法性、编译参数拼装、设置读写  
- UI：手工验收「开库 → 编辑 → 编译 → 预览」主路径  

## 6. 依赖（预期）

- .NET 8 WPF  
- AvalonEdit（编辑与 LaTeX 高亮）  
- PDFium 绑定（如 PdfiumViewer / Docnet，实现时选维护活跃的包）做内嵌预览；不采用 WebView2，避免浏览器壳与杂志线稿风格冲突  
- 本机 TeX 发行版（MiKTeX / TeX Live）  

## 7. 成功标准

- 可指定库根并树形浏览任意深度文件夹与 `.tex`  
- 可新建/重命名/移动/删除；拖拽移动可用  
- 多标签编辑，语法高亮与保存可靠  
- 选定引擎可编译当前 `.tex`，右栏刷新 PDF  
- 视觉与 Registration 黑白杂志线稿一致（令牌、直角、细线、字体层级）  


## 后续已实现（相对初版）

- 默认库路径为应用目录下 Library/（无需启动时选文件夹）
- 工具条 **Import TeX** 导入单个 .tex 到库
- EngineLocator 自动探测本机 MiKTeX / TeX Live 引擎路径
- PDF 预览缩放；导出竖向长图与逐页 PNG
