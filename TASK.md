# MacWinUI — TASK.md

```text
Development Mode: Incremental
Baseline: Current Repository
Regression Policy: No regression allowed
```

## 当前项目基线

当前仓库不是空项目，也不是 v0.1.0 初始状态。

当前已经实现并应当保留的功能包括：

- v0.1.1 Dock Visual Polish
- Floating Dock
- Dock 半透明、圆角、渐变、内高光与阴影
- Gaussian Dock Magnification 与 RenderTransform 动画
- 真实应用图标读取与稳定占位符 fallback
- 应用发现与异步启动
- 应用运行状态与当前活动状态
- MenuBar
- 系统时间、网络、电池状态与音量图标
- Light、Dark、Auto Theme 与集中式 ResourceDictionary
- 外观配置模型
- 单实例保护
- App / Core / Windows / Tests 分层架构
- 其他已经能够正常工作的现有功能

这些功能属于当前项目 baseline。

后续开发不得为了让代码重新符合旧版本任务描述而删除、禁用、回退或重写这些已经实现且正常工作的功能。

---

## 增量开发原则

本任务必须基于当前仓库状态进行增量开发。

执行顺序：

1. 首先检查当前 Solution、项目结构和已有代码。
2. 执行 `dotnet build`，确认当前基线状态。
3. 识别已经实现的功能。
4. 将现有功能视为 baseline。
5. 只实现当前任务中相对于 baseline 尚未完成的部分。
6. 已存在并正常工作的实现应复用，而不是重新创建。
7. 如果现有实现与新架构存在差异，优先兼容和渐进式重构。
8. 不允许为了匹配旧目录结构而大规模移动或删除已有文件。
9. 不允许因为某功能原计划属于更高版本，就删除已经提前实现的功能。
10. 每个主要修改完成后运行 `dotnet build`。
11. 最终运行 `dotnet test`。

原则：

```text
Existing working functionality > old milestone boundaries.
```

版本号表示开发路线，而不是强制仓库回退到对应历史状态。

---

## 禁止回退

除非当前 TASK.md 明确要求删除某项功能，否则禁止：

- 删除现有 MenuBar
- 删除真实图标支持
- 恢复为只使用测试占位图标
- 删除 Dock Visual Polish
- 删除已经存在的 Theme Resources
- 删除已经工作的 Services
- 删除已有 Settings 或配置模型
- 将已经实现的功能替换成更低版本的简化实现
- 为匹配旧 TASK 恢复旧代码结构
- 大规模重建 Solution
- 无理由删除现有项目文件

如果当前实现已经超过 TASK.md 描述：

```text
保留现有实现，并在其基础上继续开发。
```

---

## Current Milestone

```text
v0.2.15 — Safe Application Exit
```

## 当前目标

以已完成的 v0.2.14 Dock Organization & Runtime Resilience 为基线，增加清晰、一致且可恢复的程序退出入口。

本里程碑不重新实现 Dock，也不删除任何 baseline 功能。

## 当前任务必须完成

### 1. Baseline 验证

- 检查当前 Solution 与项目引用方向。
- 运行 `dotnet build`。
- 识别并复用现有 MenuBar、系统状态、Theme 与 DI 实现。

### 2. 完善 MenuBar

- 保留现有时间、网络、电池和音量显示。
- 增加明确的 Control Center 入口。
- MenuBar 继续保持无边框、置顶、不显示任务栏按钮。
- 不承诺搬运第三方应用的原生菜单。

### 3. Control Center 基础窗口

新增可切换显示的 Floating Panel，至少包含：

- 网络状态
- 电池状态（没有电池时隐藏）
- 当前系统音量与音量调节
- MacWinUI Light / Dark / Auto Theme 切换
- 打开 Windows 网络、蓝牙或声音设置的安全入口

窗口要求：

- 无边框、圆角、半透明、轻阴影
- 从 MenuBar 右上区域打开
- 不显示 Windows 任务栏按钮
- 再次点击入口、按 Escape 或失去焦点时可以关闭
- 不要求管理员权限

### 4. Windows 平台集成

- 系统音量优先使用官方 Core Audio API。
- 网络、电池继续复用现有低频状态服务。
- Windows 设置入口使用受支持的 `ms-settings:` URI。
- Windows API、COM 和 Shell 集成只能放在 MacWinUI.Windows。
- 所有失败必须安全降级，不能导致 Dock 或 MenuBar 退出。

### 5. Theme Integration

- Control Center 使用现有 DynamicResource 主题系统。
- Light / Dark / Auto 只改变 MacWinUI 自身外观。
- 本里程碑不修改 Windows 全局主题。
- 不在新控件中散落硬编码主题颜色。

### 6. Tests

- 为新增的 Core 平台无关逻辑增加单元测试。
- 不要求复杂 WPF UI 自动化测试。
- 最终运行 `dotnet test`。

## v0.2.1 当前增量范围

- 使用受支持的 DWM Window Attribute 尝试应用窗口材质、圆角与深浅色适配。
- 不支持 DWM 材质时保留现有 XAML 半透明材质作为安全降级。
- Dock、MenuBar 和 Control Center 使用当前活动显示器工作区定位。
- 应用声明 Per-Monitor V2 DPI awareness，且保持 `asInvoker` 权限级别。
- MenuBar 音量图标根据真实音量和静音状态更新。
- MenuBar 电池图形根据真实电量比例填充。
- Control Center 打开时 MenuBar 入口显示选中状态。
- 新增的平台无关窗口定位逻辑必须有单元测试。

## v0.2.2 当前增量范围

- 在 Control Center 中提供 Dock 图标尺寸和透明度调节。
- 提供 Dock Magnification、Window Material 和 Reduce Motion 开关。
- Reduce Motion 必须禁用 Dock 入场、点击弹跳、放大动画和 Control Center 弹出位移动画。
- 外观设置保存到当前用户 LocalApplicationData，不写注册表、不要求管理员权限。
- 设置文件损坏或不可访问时安全回退默认值，不影响 Dock 启动。
- 高频滑块修改使用防抖保存，应用正常退出时刷新最终设置。
- 设置快照与恢复逻辑必须有平台无关单元测试。

## v0.2.3 当前增量范围

- 设置快照包含 SchemaVersion，遇到未来版本设置时安全使用默认值。
- 损坏 JSON 自动移动为 `appearance.json.broken`，保留恢复线索。
- Control Center 提供带二次确认的恢复默认设置操作。
- 提供 Follow cursor 与 Primary display 两种窗口放置模式。
- 显示器模式修改后重新定位 Dock、MenuBar 和可见的 Control Center。
- Control Center 高度根据目标显示器工作区限制，并在小屏幕上使用垂直滚动。
- MenuBar 支持 12/24 小时制以及网络、音量、电池项目显示开关。
- 不修改 Windows 全局时间格式、显示设置或任务栏设置。

## v0.2.4 当前增量范围

- 监听 Windows 工作区、虚拟屏幕与 DPI 变化并重新定位现有窗口。
- 读取 Windows ClientAreaAnimation；系统关闭动画时自动启用有效 Reduce Motion。
- 读取 Windows HighContrast；高对比度时使用系统颜色并禁用 DWM 材质。
- 所有系统辅助设置只读，不修改 Windows 全局配置。
- Control Center 支持循环 Tab 导航和键盘焦点环。
- 主要按钮、滑块、开关、列表与 Dock 项目提供 AutomationProperties 名称。
- Reset 确认层打开时，Escape 优先返回而不是直接关闭 Control Center。
- 系统辅助行为判断放在 Core 并增加平台无关测试。

## v0.2.5 当前增量范围

- 在 Control Center 提供选择本机 `.exe` 并添加到 Dock 的入口。
- 用户添加的应用复用现有 Windows Shell 真实图标、异步启动和运行状态检测。
- 自定义应用使用动态集合增量加入，不删除或替换任何默认 Dock 项。
- 防止同一路径的应用重复加入 Dock。
- 在 Control Center 显示已添加应用，并允许用户安全移除。
- 自定义应用保存到当前用户 `%LocalAppData%\MacWinUI\dock-apps.json`。
- 配置使用版本化快照、临时文件原子替换和损坏文件 `.broken` 备份。
- 已不存在或无效的可执行文件在启动时安全跳过，不影响 Dock、MenuBar 或 Control Center。
- 文件选择只接受现有 `.exe`，不要求管理员权限，不修改 Windows 任务栏或系统文件。
- 稳定自定义 Dock ID 与映射逻辑必须有平台无关单元测试。

## v0.2.6 当前增量范围

- 深色主题采用海军蓝 MenuBar、石墨黑 Control Center 与低明度 Dock 材质。
- 提高白色文字和系统图标对比度，次要文字使用冷灰色层级。
- 面板和卡片使用细灰边框、紧凑圆角、深色阴影与克制的高光。
- MenuBar 高度调整为 34 DIP，并与 Control Center 顶部锚点保持一致。
- MenuBar 使用环形品牌标记、紧凑状态图标和更明确的选中状态。
- Dock 缩小外围留白、卡片圆角、图标阴影和分隔线间距，保持现有放大交互。
- Control Center 使用更紧凑的卡片、蓝色开关、粗轨道滑块与带边框按钮。
- Light、Dark、Auto 和 High Contrast 行为必须保留，不硬编码到单一窗口。
- 所有新增主题颜色继续集中在 `ThemeResources.xaml`。
- 不复制参考图中的壁纸、第三方品牌、广告栏或未实现的系统开关。

## v0.2.7 当前增量范围

- 将应用或普通文件拖入 Dock 空白区域时固定到 Dock。
- 普通文件固定后使用 Windows 默认关联程序安全打开。
- 将一个或多个文件拖到支持文件参数的应用图标时，用该应用打开文件。
- 文件参数使用 `ProcessStartInfo.ArgumentList` 传递，不手工拼接命令行。
- 拖放到 Dock 或应用图标时显示明确的蓝色落点反馈。
- 自定义 Dock 配置升级为 schema v2，并继续兼容 v1 应用条目。
- 显式使用 WPF 默认硬件合成路径；硬件不可用时由 WPF 安全软件回退。
- Dock 与 Control Center 的入场、点击动画使用三次贝塞尔时间曲线。
- Control Center 提供 Material intensity 调节，影响 XAML 材质密度。
- Windows 官方 DWM 背景材质强度由系统控制，不使用未文档化 API 伪造模糊半径。
- 不迁移或重写当前 WPF Solution 为 WinUI；WinUI 迁移必须作为未来独立里程碑评估。
- 不接管所有第三方窗口的最小化动画，不使用全局 Hook、注入或 Shell 替换。

## v0.2.8 当前增量范围

- 新增独立 `BigSur` 主题，不删除或覆盖现有 Auto、Light、Dark 和 High Contrast。
- 新配置默认使用 BigSur；已有配置继续保留用户此前选择的主题。
- MenuBar 使用 30 DIP 高度、全宽浅色磨砂材质与紧凑黑色文字。
- MenuBar 左侧使用 MacWinUI 品牌标记、活动应用名称和 File/Edit/View/Go/Window/Help 视觉分组。
- MenuBar 右侧保留真实网络、音量、电池、Control Center 和日期时间状态。
- 日期时间使用星期、月份、日期和 12/24 小时偏好格式。
- Dock 使用浅紫灰玻璃胶囊、亮边、柔和紫色阴影和更宽松的水平内边距。
- 应用图标使用统一半透明圆角底板，并继续显示真实 Windows Shell 图标。
- 运行与活动指示器改为贴近参考图的底部小圆点，系统区域继续使用细分隔线。
- 保留 Dock magnification、拖入固定、拖到应用打开文件和自定义项目移除。
- 不复制 Apple 商标、系统应用图标、壁纸或受保护品牌资产。
- 不隐藏 Windows 原生任务栏，不修改 explorer.exe 或系统文件。

## v0.2.9 当前增量范围

- 将 MacWinUI、File、Edit、View、Go、Window、Help 从 TextBlock 替换为真正的 WPF MenuItem。
- MenuBar 初次显示不抢占焦点，用户点击菜单时允许获得菜单焦点。
- 下拉面板使用现有动态主题、圆角、边框、阴影与键盘导航。
- MacWinUI 菜单提供 About、Control Center 和安全退出。
- File 菜单提供打开资源管理器以及选择一个或多个程序/文件加入 Dock。
- Edit 菜单提供 Appearance/Dock 设置和 Windows 声音设置入口。
- View 菜单提供 BigSur、Auto、Light、Dark 主题切换和 Dock Magnification 开关。
- 主题及 Magnification 的选中状态必须与实时配置同步。
- Go 菜单使用 Windows Shell 安全打开 Home、Desktop 和 Downloads。
- Window 菜单提供显示 Dock、显示 Control Center 和重新定位 MacWinUI 窗口。
- Help 菜单提供 Dock 拖放说明和 About。
- 文件路径继续使用结构化模型传递，不引入管理员权限、Hook 或 Shell 替换。

## v0.2.10 当前增量范围

- 自定义 Dock 项继续使用选中程序或文件的绝对路径作为 IconSourcePath。
- Windows Shell 图标提取使用大图标和 Shell 推荐图标尺寸标志。
- `SHGetFileInfo` 无法返回图标时，对可执行文件使用 `ExtractIconEx` 内嵌图标回退。
- 图标缓存键包含完整路径、文件大小和最后修改时间，程序更新后自动失效。
- 图标提取返回空值或异常时移除缓存项，允许后续重新尝试。
- 每一个 HICON 必须调用 DestroyIcon 释放，不能泄漏原生图形资源。
- 最终仍无法提取时使用现有稳定占位符，不影响应用启动和 Dock 运行。
- 不要求管理员权限，不读取或修改受保护的系统资源。

## v0.2.11 当前增量范围

- DockItemControl 截获右键事件并向 DockWindow 发送明确的项目上下文请求。
- 右击 Dock 图标提供 Open、Show in File Explorer 和 Dock Settings。
- 只有用户自定义 Dock 项显示 Remove from Dock，不允许删除内置基线项。
- Show in File Explorer 仅对本地绝对路径启用，并通过 Windows Shell 打开所在目录。
- 右击 Dock 空白区域提供 Add Application or File、Dock Settings、Magnification 和 Reposition Dock。
- Magnification 菜单项显示并修改实时选中状态。
- 右键菜单复用 DynamicResource，支持 BigSur、Light、Dark 和 High Contrast。
- 打开右键菜单时平滑复位 Dock magnification，避免图标停留在放大状态。
- 不提供强制结束第三方进程，不使用 Hook、注入、管理员权限或 Shell 替换。

## v0.2.12 当前增量范围

- 所有可见 Dock 图标的右键菜单都显示 Remove from Dock，包括内置默认项目。
- 自定义项目移除后从自定义配置中删除；内置项目使用隐藏 ID，不删除项目代码或文件。
- 隐藏默认项目保存到 dock-apps.json schema v3，重新启动后继续保持隐藏。
- 读取 schema v1/v2 配置时 HiddenDefaultItemIds 默认为空，保持升级兼容。
- 右击 Dock 空白区域继续提供 Add Application or File。
- 空白区域增加 Restore Default Items；没有隐藏默认项时该命令禁用。
- 用户重新添加一个已隐藏的默认程序时恢复默认项，不创建重复图标。
- 重建可见集合时保持默认应用、用户自定义项目和系统区域的分组顺序。
- 不删除磁盘上的应用或文件，不结束第三方进程，不修改 Windows 原生任务栏。

## v0.2.13 当前增量范围

- 使用 Windows 官方 SHAppBarMessage 注册 MenuBar，而不是覆盖最大化窗口的标题栏。
- AppBar 在目标显示器顶部预留与 MenuBar 实际 DPI 高度一致的工作区。
- Follow cursor 模式跟随当前鼠标显示器；Primary 模式固定到主显示器。
- 使用 ABM_QUERYPOS 与 ABM_SETPOS 协调其他 AppBar 和 Windows 原生任务栏。
- MenuBar 关闭、应用退出或功能关闭时调用 ABM_REMOVE 恢复原工作区。
- Control Center 增加 Reserve screen space 开关，默认开启并持久化。
- 设置快照升级为 schema v4；旧配置缺少字段时安全采用开启默认值。
- 预留成功后 Control Center 从新工作区顶部定位，不重复增加 MenuBar 高度。
- Dock 继续根据调整后的工作区定位在 Windows 原生任务栏上方。
- AppBar API 不可用时安全回退现有悬浮定位，不影响 Dock 或 MenuBar 启动。
- 不隐藏 Windows 原生任务栏，不修改 explorer.exe，不要求管理员权限。

## v0.2.14 当前增量范围

- MenuBar 处理 AppBar 位置变化消息，并在 Explorer 重启后自动重新注册。
- Help 菜单提供渲染层级、DPI、工作区、AppBar 和显示模式诊断。
- Dock 图标支持内部拖动排序，排序写入 dock-apps.json schema v4。
- 图标拖出 Dock 时移除；按 Escape 取消拖动时不得误删。
- 增加可关闭、可持久化的 Dock 自动隐藏和底边唤出。
- 自动隐藏、显示、添加、点击继续使用贝塞尔/RenderTransform 动画并遵守 Reduce Motion。
- 使用 IShellItemImageFactory 请求 256px Shell 图像，失败时保留 HICON 回退。
- 点击运行中的应用优先恢复并激活现有窗口，找不到窗口时才启动新实例。
- 运行状态优先按完整可执行路径匹配，避免同名进程误判。
- 右击应用图标最多列出六个可见窗口并允许直接激活。
- IApplicationLauncher 支持 AppUserModelId；Store/开始菜单快捷方式可通过 `.lnk` 拖入。
- MenuBar 只读显示当前前台应用名称，不注入或搬运第三方菜单。
- 外观和 Dock 配置可导出/导入单个 `.macwinui.json` 文件。
- 每次覆盖 appearance.json 与 dock-apps.json 前生成 `.backup`。
- MenuBar、Dock 右键和 Control Center 主要文案根据 Windows UI 语言使用中英文资源。
- 提供 `scripts/publish.ps1`，执行 Release build/test/publish 到 artifacts/publish。
- 实际 GUI 行为在无法人工观察时继续报告 NOT VERIFIED，不以诊断代替人工验证结论。
- 保留所有安全约束：无管理员权限、无 Hook、无注入、无系统文件修改、无 Explorer 替换。

## v0.2.15 当前增量范围

- 保留 MenuBar 中原有 Quit MacWinUI，并增加退出确认。
- Dock 空白区域右键菜单增加 Quit MacWinUI。
- Control Center 底部增加醒目的 Quit MacWinUI 按钮。
- 三个入口统一调用 ApplicationExitCoordinator，避免关闭流程不一致。
- 退出前必须二次确认，取消时保持所有窗口和后台任务运行。
- 确认退出后使用 Application.Current.Shutdown 触发现有 OnExit 清理流程。
- 正常退出必须保存外观设置、取消后台任务、释放 COM/窗口资源和 AppBar 工作区。
- 退出标题、说明和按钮提供中英文资源。
- 不强制结束 explorer.exe 或第三方进程，不要求管理员权限。

## 当前任务禁止内容

- 删除、隐藏或替换 Windows 原生任务栏
- Taskbar Replace 模式
- 修改或结束 `explorer.exe`
- 修改 Windows 系统文件或系统 DLL
- 管理员权限功能
- DLL 注入、全局 Hook 或第三方应用注入
- Spotlight
- Launchpad
- Mission Control
- Desktop replacement
- Widgets
- Finder-like File Browser
- DWM Thumbnail
- 完整窗口管理器
- 自动修改 Windows 壁纸或全局系统主题

## 安全与恢复

- 所有系统集成功能必须可关闭、可恢复。
- Control Center 关闭后不得留下后台 UI 状态。
- 调节音量不得改变启动 Shell、任务栏或其他持久系统配置。
- 应用退出时取消所有后台任务并释放 COM/窗口资源。

## Build 流程

每个主要修改后：

```powershell
dotnet build .\MacWinUI.sln
```

最终：

```powershell
dotnet test .\MacWinUI.sln
```

如果失败，修复后再结束。

如果无法人工验证 Windows GUI，报告：

```text
GUI runtime: NOT VERIFIED
```

## 后续路线

```text
Current Baseline
    ↓
v0.2.0  MenuBar & Control Center Integration
v0.2.1  System UI Visual Refinement
v0.2.2  Appearance & Motion Preferences
v0.2.3  Settings, Recovery & Display Control
v0.2.4  Accessibility & Runtime Resilience
v0.2.5  Custom Dock Applications
v0.2.6  Graphite Visual System
v0.2.7  Dock Drag & Composition Polish
v0.2.8  Big Sur Dock & Menu Bar
v0.2.9  Functional Menu Bar
v0.2.10 Reliable Custom Icons
v0.2.11 Dock Context Menus
v0.2.12 Complete Dock Item Management
v0.2.13 Reserved Menu Bar Work Area
v0.2.14 Dock Organization & Runtime Resilience
v0.2.15 Safe Application Exit
v0.3    Spotlight
v0.4    Launchpad
v0.5    Mission Control
v0.6    Desktop + Widgets
v0.7    Window Appearance
v0.8    Finder-like File Browser
v1.0    Integrated Desktop Environment
```

只有更新 TASK.md 的 Current Milestone 后，才允许开始下一个版本。

## 完成后输出格式

### Baseline Preserved

列出已验证且未回退的现有功能。

### Created

列出新文件。

### Modified

列出修改文件。

### Implemented

只列实际完成的增量功能。

### Validation

```text
dotnet build: PASS / FAIL
dotnet test: PASS / FAIL
GUI runtime: VERIFIED / NOT VERIFIED
```

### Known Limitations

只列真实限制。

### Next Recommended Task

只能建议下一步，不自动开始未来版本。
