# CheemsControl 开发规范

> 适用范围：`CheemsControl.sln` 下所有项目。改动代码前先读本文；Code Review / PR 以规则编号为准（如「违反 R3.3」）。
> 结构性调整（新增目录、新增规则）必须同步更新本文，并登记文末变更记录。
>
> 最后更新：2026-08-25

## 1. 总原则（P）

| 编号 | 规则 |
|---|---|
| P1 | **零依赖**：类库 `CheemsControl` 禁止引用任何 NuGet 包，只依赖 WPF / BCL。Demo App 不受此限制。 |
| P2 | **扁平命名空间**：所有对外类型位于根命名空间 `CheemsControl`。文件夹只做物理归类，不映射命名空间。将来拆分包时命名空间也不变，保证使用者代码零改动。 |
| P3 | **资源三层分离**：色值 → 语义画刷 → 控件样式，只允许上层引用下层，见 §3。 |
| P4 | **新增控件固定流程**：类 + 样式 + Generic.xaml 合并行 + Demo 页，同一提交完成，见 §6。 |
| P5 | **目标框架底线**：类库 `net6.0-windows`，禁止使用 .NET 6 之后才有的 API（将来改多目标时用 `#if` 隔离）。Demo 当前为 `net8.0-windows`。 |
| P6 | **视觉状态完整**：默认样式必须覆盖控件用到的所有状态（Normal / MouseOver / Pressed / Disabled / Focused 按需），缺状态的控件不合入。 |
| P7 | **CSS/HTML 移植规范**：从网页源码转换 WPF 控件时，必须遵守根目录 `CSS_TO_WPF_CONVERSION_RULES.md`，先完成结构/状态/动画映射，再实施与验收。 |

## 2. 目录职责（D）

```
CheemsControl/                       # 控件类库
├── Controls/                        # 对外成品控件（public）
├── Primitives/                      # 控件基类/中间层（internal/protected，不承诺稳定）
├── AttachedProperties/              # 附加属性
├── Converters/                      # 通用值转换器
├── Extensions/                      # 扩展方法
├── Helpers/                         # 内部工具（视觉树查找、DPI 等）
├── Interop/                         # Win32 互操作（预留）
├── Resources/
│   ├── Fonts/                       # 字体文件（ttf/otf，Build Action=Resource）
│   └── Images/                      # 图片等静态资源
├── Themes/
│   ├── Generic.xaml                 # 只做合并入口
│   ├── Basic/                       # Colors / Brushes / Fonts 三件套
│   ├── Controls/                    # 每控件一个默认样式，文件名=类名
│   ├── Icons.xaml                   # 几何图标（预留）
│   └── Light.xaml / Dark.xaml       # 主题变体（预留）
├── CheemsKeys.cs                    # 资源键常量
└── AssemblyInfo.cs                  # ThemeInfo + XmlnsDefinition

CheemsControl.App/                   # Demo：控件的活文档
├── MainWindow.xaml                  # 仅导航壳（左列表 + 右内容区）
├── Infrastructure/
│   ├── ShowcaseControl.*            # 复杂控件的大展示容器
│   └── CompactShowcaseControl.*     # 简单控件的透明横向小展示容器
└── Pages/                           # 按控件类别组织的示例页
```

- **D1** 每个目录职责单一。出现放不进现有目录的新类别时，先在本文登记新目录，再动代码。
- **D2** 类库内禁止出现演示/UI 样例代码；演示一律进 Demo App。
- **D3** 依赖方向单向：App → Lib。类库禁止引用 Demo 的任何类型。

## 3. 资源三层（R）

```
L1  Themes/Basic/Colors.xaml     只放 <Color>，键 Cheems.Color.*
                ↑ StaticResource
L2  Themes/Basic/Brushes.xaml    只放画刷，键 Cheems.Brush.*（含控件级 Cheems.Brush.控件名.用途）
                ↑ DynamicResource
L3  Themes/Controls/*.xaml       控件默认样式/模板
```

- **R3.1** L1 只允许 `<Color>` 元素；L2 只允许画刷元素。
- **R3.2** L2 的颜色只能 `{StaticResource Cheems.Color.*}` 引用 L1，禁止字面色值。
- **R3.3** L3 模板内禁止出现十六进制色值和具体字体名字符串，一律 `{DynamicResource Cheems.Brush.*}` / `{DynamicResource Cheems.FontFamily.*}`（透明度蒙版等确实无语义的色可用 `#00000000` 形式并加行内注释说明用途）。
- **R3.4** 引用方向固定：L2→L1 用 StaticResource；L3→语义资源一律 DynamicResource——这是运行时换肤的前提。
- **R3.5** 所有资源键必须以 `Cheems.` 开头（控件库资源会被合并进宿主程序，防撞名）。
- **R3.6** `Generic.xaml` 只允许 MergedDictionaries 合并行，不定义任何样式；合并顺序固定：Colors → Brushes → Fonts →（Icons）→ Controls/*。
- **R3.7** 排版令牌只在 Fonts.xaml 定义：`Cheems.FontFamily.*`、`Cheems.FontSize.*`。
- **R3.8** Freezable 上的颜色属性（如 `GradientStop.Color`）不支持 DynamicResource，允许 StaticResource 引用 L1 色值；带透明度的变体色也必须登记为 L1 键。由代码端动画驱动的颜色同样豁免字面色值限制，但初始值仍须引用 L1 键，且动画颜色须从资源或控件公开属性实时计算，不得硬编码。
- **R3.9** **字典自包含**：任何字典文件如果含 StaticResource 引用其他文件的键，必须在自身顶部 `MergedDictionaries` 合并那些依赖文件。**库内字典的合并源一律使用程序集绝对 pack URI**（`/CheemsControl;component/...`），禁止相对路径与 `../`——字典经 ThemeInfo 主题字典路径（控件 DefaultStyleKey 样式加载）实例化时，相对 Source 会按应用根目录解析而失败。程序集改名时须全局替换这些 URI。R3.6 的合并顺序仅作语义约定，不得被任何文件依赖。

## 4. 命名（N）

- **N1** 控件类用 `Cheems` 前缀：`CheemsDashedButton`、`CheemsSoftButton`、`CheemsTypewriterLoader`、`CheemsWashingMachineLoader`、`CheemsDayNightSwitch`、`CheemsAmPmToggle`、`CheemsStarRating`、`CheemsLedSwitch`。
- **N2** 样式文件名 = 控件类名：`CheemsDashedButton.xaml`。
- **N3** 资源键格式 `Cheems.类别.名称[.状态]`，状态用 MouseOver / Pressed / Disabled，例：`Cheems.Brush.Button.Background.MouseOver`。
- **N4** Demo 页面：`Pages/{类别复数}Page.xaml`，如 `ButtonsPage`、`InputsPage`。
- **N5** 转换器暴露单例：`public static readonly XxxConverter Instance = new();`，XAML 用 `{x:Static conv:XxxConverter.Instance}`。
- **N6** C# 代码引用资源键必须经 `CheemsKeys.*` 常量，禁止裸写字符串键。

## 5. 字体与图标（F）

- **F1** 字体文件只能放 `Resources/Fonts/`，仅限 ttf / otf；Build Action = Resource（csproj 已按通配配置，新文件自动打包）。WPF 不支持 woff/woff2，禁止引入。
- **F2** 字体的 pack URI 只允许在 `Fonts.xaml` 出现（全库唯一）；其他任何地方引用 `Cheems.FontFamily.*` 资源。
- **F3** pack URI 中 `#` 后是**字体内部家族名**（字体文件属性页里的名称），不是文件名。
- **F4** 图标两类方案并存：
  - 成套业务图标 → 图标字体，与普通字体同等对待（F1/F2/F3）；
  - 控件内部装饰性小图标（箭头、关闭等）→ 几何方案：`Themes/Icons.xaml` 中 `Cheems.Icon.*` 键的 `StreamGeometry`，模板用 `Path` 呈现。优先几何方案（矢量可着色、无版权负担）。
- **F5** 授权：字体嵌入 DLL 即再分发，只允许 OFL 等明确允许再分发的字体；新增字体必须在 `Fonts.xaml` 对应行注释「字体名 | 来源 | 授权」。

## 6. 新增控件流程（固定四步，同一提交完成）

1. `Controls/CheemsXxx.cs`：类 + 静态构造中 `DefaultStyleKeyProperty.OverrideMetadata`。
2. `Themes/Controls/CheemsXxx.xaml`：默认样式，视觉状态齐全，只引用语义资源键。
3. `Themes/Generic.xaml`：按 R3.6 规定顺序追加一行合并。
4. Demo `Pages/` 新增示例页（覆盖属性与各状态）并挂到 MainWindow 导航。

## 7. 代码（C）

- **C1** public 只允许出现在 `Controls/`、`Converters/`、`Extensions/`、`AttachedProperties/` 以及程序集根的 `CheemsKeys`；`Primitives/`、`Helpers/`、`Interop/` 一律 internal（或 protected）。
- **C2** 依赖属性样板：`public static readonly DependencyProperty XxxProperty` + CLR 包装属性，包装内只有 `GetValue/SetValue`。
- **C3** 控件 C# 代码禁止引用具体颜色/字体；需要取资源时用 `TryFindResource(CheemsKeys.xxx)`。
- **C4** XAML 引用类库统一用 `xmlns:cheems="https://cheemscontrol.com/wpf"`（已在 AssemblyInfo 注册 XmlnsDefinition / XmlnsPrefix），禁止用 clr-namespace 引类库类型。
- **C5** 模板内部件命名 `Part` 前缀（`PartRoot`、`PartLabel`），类上用 `[TemplatePart]` 标注。
- **C6** **循环动画（`RepeatBehavior="Forever"`）只允许挂在 `EventTrigger RoutedEvent="Loaded"` 上，禁止放进属性触发器（Trigger/DataTrigger）的 EnterActions/ExitActions**。曾因此导致渲染线程约 10 秒后栈溢出崩溃（0xC00000FD，事件日志表现为 dwrite.dll 崩溃）。状态过渡（悬停/按下/选中）用属性触发器 + 一次性动画；循环动画与状态过渡若作用于同一元素，须分别使用不同属性（如循环走 Transform、过渡走 Opacity）避免冲突。

## 8. Demo App（A）

- **A1** 每个对外控件必须有示例页，随控件同一提交（示例页即控件的活文档）。
- **A2** `MainWindow` 只承担导航职责，不放控件演示内容。
- **A3** Demo 不受 P1 / P5 约束，可自由使用第三方库与更高版本 API。
- **A4** 一个对外控件只允许一个演示容器，可选大容器 `ShowcaseControl` 或小容器 `CompactShowcaseControl`，禁止为同一控件按基本用法、尺寸或状态重复建容器。
- **A5** 按钮、开关、单选/评分等结构简单、单实例即可说明交互的控件必须使用 `CompactShowcaseControl`：容器无标题、无边框、无背景，仅横向排列“控件 + 右侧源码按钮”；页面使用 `WrapPanel`，允许一行放置多个小容器。
- **A6** Loader、复杂动画或确需标题、展示台背景、必要变体的控件使用 `ShowcaseControl`。简单控件默认只展示一个代表实例；只有能说明不同公开属性或关键行为时，复杂控件才可在同一大容器中集中展示最少数量的变体。
- **A7** 大、小展示器的源码按钮统一使用 `Cheems.App.Style.CodeButton`：仅显示文本，不得绘制背景、边框、阴影、悬停底色或焦点框；预览、复制和结果反馈功能保持一致。

## 9. 红线速查

- 类库引用 NuGet 包 ✗
- L3 模板写死色值 / 字体名 ✗
- 资源键不带 `Cheems.` 前缀 ✗
- `Generic.xaml` 里写具体样式 ✗
- 字体文件放 `Resources/Fonts/` 之外，或使用 woff ✗
- C# 裸写资源键字符串 ✗
- 控件没有 Demo 页就合入 ✗
- 类库中使用 .NET 7+ 专属 API（当前单目标 net6.0-windows）✗

## 10. 演进路线（按需推进，不提前建设）

1. **当前**：`CheemsDashedButton`、`CheemsSoftButton`、两个 Loader、`CheemsDayNightSwitch`、`CheemsAmPmToggle`、`CheemsStarRating` 与 `CheemsLedSwitch` 全链路样板（代码 → 样式 → 资源 → Demo）。
2. **主题**：Light / Dark 同名键字典 + 运行时切换。键名规范已就绪，届时控件样式零改动。
3. **分发**：多目标（net6 / net8，可选 net472）+ NuGet 打包 + XML 文档注释。
4. **规模化**：必要时拆 Core / Controls 多包；因 P2 命名空间不变，不影响使用者。

## 11. 变更记录

| 日期 | 变更 |
|---|---|
| 2026-08-25 | 初版：目录结构、三层资源、字体规范、命名约定、新增控件流程定稿 |
| 2026-08-25 | 新增 R3.8：Freezable 颜色属性与代码端动画颜色的资源引用例外 |
| 2026-08-25 | 新增 R3.9 字典自包含原则：修复控件字典经 App 合并延迟实例化时 `Cheems.Color.*` 解析失败导致的 XamlParseException |
| 2026-08-25 | R3.9 补充：库内字典合并源必须用绝对 pack URI，避免 ThemeInfo 加载时按应用根错误解析相对 Source |
| 2026-08-25 | 新增 C6：Forever 循环动画只允许挂 Loaded EventTrigger（属性触发器 EnterActions 中的循环动画曾致渲染线程栈溢出崩溃） |
| 2026-08-26 | 新增 P7 与 `CSS_TO_WPF_CONVERSION_RULES.md`：固化网页控件高还原度转换、视觉对比和运行验收流程 |
| 2026-08-26 | 精简控件集：仅保留 `CheemsDashedButton` 与 `CheemsDayNightSwitch`，移除标准按钮、霓虹按钮和加载控件 |
| 2026-08-26 | 按 CSS 转 WPF 专项规范新增 `CheemsTypewriterLoader`（Uiverse Nawsome 打字机加载动画） |
| 2026-08-26 | 新增 `CheemsWashingMachineLoader`（Uiverse Shoh2008 洗衣机加载动画） |
| 2026-08-26 | 新增 Demo 单容器规则：一个控件一个 Showcase，必要变体集中展示 |
| 2026-08-26 | 按 Uiverse mobinkakei 源码新增 `CheemsAmPmToggle`（AM/PM 弹性切换动画） |
| 2026-08-26 | 按 Uiverse ke1221 源码新增 `CheemsSoftButton` |
| 2026-08-26 | 按 Uiverse PriyanshuGupta28 源码新增 `CheemsStarRating` 五星 RadioButton 评分组 |
| 2026-08-26 | 按 Uiverse chase2k25 源码新增 `CheemsLedSwitch` LED 软拟态开关 |
| 2026-08-26 | 新增透明横向 `CompactShowcaseControl`：简单控件单实例展示，多个小容器可在同一行自动换行 |
| 2026-08-26 | 大、小展示器的源码按钮统一改为无背景、无边框的极简文本按钮 |
| 2026-08-26 | 重写 `CheemsLedSwitch` 阴影：使用圆角孔洞几何模拟 CSS inset box-shadow，并按像素对比校准拨块投影 |
