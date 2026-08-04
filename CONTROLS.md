# 控件总览

CheemsUI 全部 **42 个控件**的清单与动效演示。图片均由 Demo 程序自动实录生成（按钮为完整交互 GIF：常态 → 悬停 → 按下 0.5s → 释放停留 0.5s → 离开，带鼠标光标演示；Loader 为循环 GIF；其余为关键状态截图）。

[![返回 README](https://img.shields.io/badge/%E2%86%90_%E8%BF%94%E5%9B%9E_README-18181B?style=flat-square)](README.md)

## Buttons（8）

每个按钮为完整交互实录 GIF（光标移入 → 悬停 → 按下 0.5s → 释放停留 0.5s → 移出）：

| 控件 | 交互实录 |
|---|---|
| `CheemsShineButton`<br>流光扫过式主按钮 | <img src="docs/gallery/Buttons/CheemsShineButton.gif" width="200"> |
| `CheemsLayered3DButton`<br>层叠立体按压按钮 | <img src="docs/gallery/Buttons/CheemsLayered3DButton.gif" width="200"> |
| `CheemsSoftButton`<br>柔和底色按钮，按下带形变 | <img src="docs/gallery/Buttons/CheemsSoftButton.gif" width="200"> |
| `CheemsDashedButton`<br>虚线描边按钮 | <img src="docs/gallery/Buttons/CheemsDashedButton.gif" width="200"> |
| `CheemsSubscribeButton`<br>订阅按钮，点击后勾选动效 | <img src="docs/gallery/Buttons/CheemsSubscribeButton.gif" width="200"> |
| `CheemsDeleteButton`<br>删除按钮，悬停出确认叉 | <img src="docs/gallery/Buttons/CheemsDeleteButton.gif" width="200"> |
| `CheemsPixelHandButton`<br>像素风手指按钮 | <img src="docs/gallery/Buttons/CheemsPixelHandButton.gif" width="200"> |
| `CheemsLeafButton`<br>叶形按钮 | <img src="docs/gallery/Buttons/CheemsLeafButton.gif" width="200"> |

## Loaders（17）

全部为无限循环动画，放入界面即自动播放；每个控件在 `Themes/Controls/*.xaml` 里有对应的专属配色键可覆盖。

| | | |
|---|---|---|
| ![](docs/gallery/Loaders/CheemsAiMatrixLoader.gif) | ![](docs/gallery/Loaders/CheemsBlobLoader.gif) | ![](docs/gallery/Loaders/CheemsBounceBallLoader.gif) |
| **AiMatrix** | **Blob** | **BounceBall** |
| ![](docs/gallery/Loaders/CheemsCubeLoadingLoader.gif) | ![](docs/gallery/Loaders/CheemsDominoLoader.gif) | ![](docs/gallery/Loaders/CheemsEarthLoader.gif) |
| **CubeLoading** | **Domino** | **Earth** |
| ![](docs/gallery/Loaders/CheemsGlitchLoader.gif) | ![](docs/gallery/Loaders/CheemsHamsterWheelLoader.gif) | ![](docs/gallery/Loaders/CheemsJumpingSquareLoader.gif) |
| **Glitch** | **HamsterWheel** | **JumpingSquare** |
| ![](docs/gallery/Loaders/CheemsNewtonsCradleLoader.gif) | ![](docs/gallery/Loaders/CheemsOrbitDotsLoader.gif) | ![](docs/gallery/Loaders/CheemsPolylineLoader.gif) |
| **NewtonsCradle** | **OrbitDots** | **Polyline** |
| ![](docs/gallery/Loaders/CheemsPulseDotsLoader.gif) | ![](docs/gallery/Loaders/CheemsRainbowBarsLoader.gif) | ![](docs/gallery/Loaders/CheemsTypewriterLoader.gif) |
| **PulseDots** | **RainbowBars** | **Typewriter** |
| ![](docs/gallery/Loaders/CheemsWashingMachineLoader.gif) | ![](docs/gallery/Loaders/CheemsWaveBarsLoader.gif) | |
| **WashingMachine** | **WaveBars** | |

## Inputs（13）

开关类展示 关 / 开 两态：

| 控件 | 关 | 开 |
|---|---|---|
| `CheemsDayNightSwitch`<br>昼夜形态渐变开关 | <img src="docs/gallery/Inputs/CheemsDayNightSwitch.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsDayNightSwitch-on.jpg" width="120"> |
| `CheemsCheckToggle`<br>经典勾选开关 | <img src="docs/gallery/Inputs/CheemsCheckToggle.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsCheckToggle-on.jpg" width="120"> |
| `CheemsAmPmToggle`<br>上午 / 下午开关 | <img src="docs/gallery/Inputs/CheemsAmPmToggle.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsAmPmToggle-on.jpg" width="120"> |
| `CheemsGenderToggle`<br>性别选择开关 | <img src="docs/gallery/Inputs/CheemsGenderToggle.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsGenderToggle-on.jpg" width="120"> |
| `CheemsIosStretchSwitch`<br>iOS 风拉伸开关 | <img src="docs/gallery/Inputs/CheemsIosStretchSwitch.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsIosStretchSwitch-on.jpg" width="120"> |
| `CheemsLedSwitch`<br>LED 指示开关 | <img src="docs/gallery/Inputs/CheemsLedSwitch.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsLedSwitch-on.jpg" width="120"> |
| `CheemsMechanicalToggle`<br>机械拨杆开关 | <img src="docs/gallery/Inputs/CheemsMechanicalToggle.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsMechanicalToggle-on.jpg" width="120"> |
| `CheemsMetalSwitch`<br>金属质感开关 | <img src="docs/gallery/Inputs/CheemsMetalSwitch.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsMetalSwitch-on.jpg" width="120"> |
| `CheemsPixelSwitch`<br>像素风开关 | <img src="docs/gallery/Inputs/CheemsPixelSwitch.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsPixelSwitch-on.jpg" width="120"> |
| `CheemsPixelCoinSwitch`<br>像素投币开关 | <img src="docs/gallery/Inputs/CheemsPixelCoinSwitch.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsPixelCoinSwitch-on.jpg" width="120"> |
| `CheemsScaleSwitch`<br>天平式开关 | <img src="docs/gallery/Inputs/CheemsScaleSwitch.jpg" width="120"> | <img src="docs/gallery/Inputs/CheemsScaleSwitch-on.jpg" width="120"> |

输入框展示 常态 / 悬停（聚焦）两态：

| 控件 | 常态 | 聚焦 |
|---|---|---|
| `CheemsGlowInput`<br>聚焦发光输入框（`Placeholder` 属性） | <img src="docs/gallery/Inputs/CheemsGlowInput.jpg" width="220"> | <img src="docs/gallery/Inputs/CheemsGlowInput-hover.jpg" width="220"> |
| `CheemsSearchBox`<br>搜索框（`Label` / `Text`，带清除按钮） | <img src="docs/gallery/Inputs/CheemsSearchBox.jpg" width="220"> | <img src="docs/gallery/Inputs/CheemsSearchBox-hover.jpg" width="220"> |

## Progress（4）

均继承自 `ProgressBar`，直接绑定 `Value` / `Minimum` / `Maximum` 使用：

| 控件 | 演示 |
|---|---|
| `CheemsCosmicProgressBar` | <img src="docs/gallery/Progress/CheemsCosmicProgressBar.jpg" width="240"> |
| `CheemsCircuitProgressBar` | <img src="docs/gallery/Progress/CheemsCircuitProgressBar.jpg" width="240"> |
| `CheemsMonoProgressBar` | <img src="docs/gallery/Progress/CheemsMonoProgressBar.jpg" width="240"> |
| `CheemsWaveProgressBall` | <img src="docs/gallery/Progress/CheemsWaveProgressBall.jpg" width="240"> |
