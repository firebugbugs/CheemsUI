# CheemsUI

一套注重动效与质感的 WPF 自绘控件库。所有控件均以 ControlTemplate + Storyboard 纯 WPF 实现，不依赖任何第三方库；配色、字体走语义化资源键，换主题只改资源不动模板。

[![QQ群](https://img.shields.io/badge/QQ%E7%BE%A4-1094431427-12B7F5?logo=tencentqq&logoColor=white)](CONTACT.md)

![](docs/gallery/Loaders/CheemsBounceBallLoader.gif)
![](docs/gallery/Loaders/CheemsEarthLoader.gif)
![](docs/gallery/Loaders/CheemsHamsterWheelLoader.gif)

## 快速开始

库目标框架 `net6.0-windows`（Demo 为 net8.0）。项目引用：

```xml
<ItemGroup>
  <ProjectReference Include="..\src\CheemsUI\CheemsUI.csproj" />
</ItemGroup>
```

XAML 中统一使用语义命名空间（无需记程序集名），默认样式通过 `ThemeInfo` 自动生效，不需要手动合并任何字典：

```xml
xmlns:cheems="https://cheemsui.com/wpf"
```

```xml
<StackPanel Width="260">
    <cheems:CheemsShineButton Content="SHINE" />
    <cheems:CheemsBounceBallLoader Margin="0,24" />
    <cheems:CheemsGlowInput Placeholder="Type here" />
    <cheems:CheemsCosmicProgressBar Value="40" Height="12" Margin="0,16" />
    <cheems:CheemsCheckToggle IsChecked="True" Margin="0,16" />
</StackPanel>
```

运行 Demo 浏览器交互查看全部控件：

```
dotnet run --project src/CheemsUI.App
```

## 特性

- **40+ 控件**：按钮、加载动画（Loader）、开关、输入框、进度条五大类
- **纯 WPF 实现**：无第三方依赖，模板即全部视觉，可随意拆改
- **状态即动效**：悬停、按下、开启等状态都带过渡动画，不是贴图切换
- **语义化主题**：`Cheems.Brush.*` / `Cheems.FontFamily.*` 等资源键全局覆盖，一处改色处处生效
- **自带工具链**：Demo 浏览器 + 命令行截图导出（本页所有图都由它生成）

## 效果预览

### Loader（GIF 实录）

| | | |
|---|---|---|
| ![](docs/gallery/Loaders/CheemsNewtonsCradleLoader.gif) | ![](docs/gallery/Loaders/CheemsWaveBarsLoader.gif) | ![](docs/gallery/Loaders/CheemsAiMatrixLoader.gif) |
| ![](docs/gallery/Loaders/CheemsWashingMachineLoader.gif) | ![](docs/gallery/Loaders/CheemsOrbitDotsLoader.gif) | ![](docs/gallery/Loaders/CheemsRainbowBarsLoader.gif) |

### 按钮 · 三态实录（常态 / 悬停 / 按下）

| 常态 | 悬停 | 按下 |
|---|---|---|
| <img src="docs/gallery/Buttons/CheemsLayered3DButton.jpg" width="150"> | <img src="docs/gallery/Buttons/CheemsLayered3DButton-hover.jpg" width="150"> | <img src="docs/gallery/Buttons/CheemsLayered3DButton-press.jpg" width="150"> |

### 开关 · 关 / 开

| 关 | 开 |
|---|---|
| <img src="docs/gallery/Inputs/CheemsDayNightSwitch.jpg" width="130"> | <img src="docs/gallery/Inputs/CheemsDayNightSwitch-on.jpg" width="130"> |

昼夜开关（DayNightSwitch）自带完整的日月形态渐变：太阳脉冲、月相移动、星星闪烁，全程矢量动画。

### 输入与进度

<img src="docs/gallery/Inputs/CheemsSearchBox-hover.jpg" width="260"> <img src="docs/gallery/Progress/CheemsWaveProgressBall.jpg" width="110">

## 控件总览

- **Buttons（8）**：`CheemsShineButton`、`CheemsLayered3DButton`、`CheemsSoftButton`、`CheemsDashedButton`、`CheemsSubscribeButton`、`CheemsDeleteButton`、`CheemsPixelHandButton`、`CheemsLeafButton`
- **Loaders（17）**：`CheemsAiMatrixLoader`、`CheemsBlobLoader`、`CheemsBounceBallLoader`、`CheemsCubeLoadingLoader`、`CheemsDominoLoader`、`CheemsEarthLoader`、`CheemsGlitchLoader`、`CheemsHamsterWheelLoader`、`CheemsJumpingSquareLoader`、`CheemsNewtonsCradleLoader`、`CheemsOrbitDotsLoader`、`CheemsPolylineLoader`、`CheemsPulseDotsLoader`、`CheemsRainbowBarsLoader`、`CheemsTypewriterLoader`、`CheemsWashingMachineLoader`、`CheemsWaveBarsLoader`
- **Inputs（13）**：`CheemsDayNightSwitch`、`CheemsCheckToggle`、`CheemsAmPmToggle`、`CheemsGenderToggle`、`CheemsIosStretchSwitch`、`CheemsLedSwitch`、`CheemsMechanicalToggle`、`CheemsMetalSwitch`、`CheemsPixelSwitch`、`CheemsPixelCoinSwitch`、`CheemsScaleSwitch`、`CheemsGlowInput`、`CheemsSearchBox`
- **Progress（4）**：`CheemsCosmicProgressBar`、`CheemsCircuitProgressBar`、`CheemsMonoProgressBar`、`CheemsWaveProgressBall`（均继承自 `ProgressBar`，直接绑定 `Value` 使用）

Loader 全部为无限循环动画，放入界面即自动播放；每个控件在 `Themes/Controls/*.xaml` 里有对应的专属配色键可覆盖。

## 主题定制

在 App 级合并主题字典并覆盖语义键，所有控件即时生效（DynamicResource）：

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/CheemsUI;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>

        <SolidColorBrush x:Key="Cheems.Brush.Primary" Color="#7C5CFF" />
        <SolidColorBrush x:Key="Cheems.Brush.Accent" Color="#FF6B9E" />
    </ResourceDictionary>
</Application.Resources>
```

常用键：颜色 `Cheems.Brush.Primary` / `Accent` / `Text.*` / `Background.*`，字体 `Cheems.FontFamily.Default` / `Mono` / `Icon`，字号 `Cheems.FontSize.*`。完整清单见 `src/CheemsUI/CheemsKeys.cs` 与 `src/CheemsUI/Themes/Basic/*.xaml`。

## 截图与 GIF 导出

本页所有图片由 Demo 程序的导出模式自动生成，可随时重跑刷新：

```
CheemsUI.App.exe --export            # 全量导出到 docs/gallery
CheemsUI.App.exe --export --only=CheemsShineButton,CheemsCheckToggle
```

命名约定：`控件名.jpg` 常态、`-hover.jpg` 悬停、`-on.jpg` 开启态（开关类）、`-press.jpg` 按下态（按钮类）；Loader 为实录 GIF（24fps，自动按运动最大边界取景）。

## 交流

QQ 交流群：**1094431427** —— 问题反馈、控件需求、动效讨论都可以进来聊，也欢迎直接提 PR。

[![QQ群](https://img.shields.io/badge/QQ%E7%BE%A4-1094431427-12B7F5?logo=tencentqq&logoColor=white)](CONTACT.md)
