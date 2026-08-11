# 控件总览

CheemsUI 全部 **42 个控件**的清单与动效演示。所有图片均为 Demo 程序自动实录生成的 GIF（按钮 / 开关 / 输入框为带鼠标光标的完整交互演示，输入框演示逐字输入 "Cheems"；进度条为 0 → 100% 全程 5s；Loader 为循环动画）。

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

开关为完整交互实录 GIF（光标移入 → 悬停 → 点击打开 → 移开）：

| 控件 | 交互实录 |
|---|---|
| `CheemsDayNightSwitch`<br>昼夜形态渐变开关 | <img src="docs/gallery/Inputs/CheemsDayNightSwitch.gif" width="150"> |
| `CheemsCheckToggle`<br>经典勾选开关 | <img src="docs/gallery/Inputs/CheemsCheckToggle.gif" width="150"> |
| `CheemsAmPmToggle`<br>上午 / 下午开关 | <img src="docs/gallery/Inputs/CheemsAmPmToggle.gif" width="150"> |
| `CheemsGenderToggle`<br>性别选择开关 | <img src="docs/gallery/Inputs/CheemsGenderToggle.gif" width="150"> |
| `CheemsIosStretchSwitch`<br>iOS 风拉伸开关 | <img src="docs/gallery/Inputs/CheemsIosStretchSwitch.gif" width="150"> |
| `CheemsLedSwitch`<br>LED 指示开关 | <img src="docs/gallery/Inputs/CheemsLedSwitch.gif" width="150"> |
| `CheemsMechanicalToggle`<br>机械拨杆开关 | <img src="docs/gallery/Inputs/CheemsMechanicalToggle.gif" width="150"> |
| `CheemsMetalSwitch`<br>金属质感开关 | <img src="docs/gallery/Inputs/CheemsMetalSwitch.gif" width="150"> |
| `CheemsPixelSwitch`<br>像素风开关 | <img src="docs/gallery/Inputs/CheemsPixelSwitch.gif" width="150"> |
| `CheemsPixelCoinSwitch`<br>像素投币开关 | <img src="docs/gallery/Inputs/CheemsPixelCoinSwitch.gif" width="150"> |
| `CheemsScaleSwitch`<br>天平式开关 | <img src="docs/gallery/Inputs/CheemsScaleSwitch.gif" width="150"> |

输入框为完整交互实录 GIF（光标移入 → 点击聚焦 → 逐字输入 "Cheems"）：

| 控件 | 交互实录 |
|---|---|
| `CheemsGlowInput`<br>聚焦发光输入框（`Placeholder` 属性） | <img src="docs/gallery/Inputs/CheemsGlowInput.gif" width="240"> |
| `CheemsSearchBox`<br>搜索框（`Label` / `Text`，带清除按钮） | <img src="docs/gallery/Inputs/CheemsSearchBox.gif" width="240"> |

## Progress（4）

均继承自 `ProgressBar`，直接绑定 `Value` / `Minimum` / `Maximum` 使用。GIF 为 0 → 100% 全程 5s 的完整行程实录：

| 控件 | 演示 |
|---|---|
| `CheemsCosmicProgressBar` | <img src="docs/gallery/Progress/CheemsCosmicProgressBar.gif" width="240"> |
| `CheemsCircuitProgressBar` | <img src="docs/gallery/Progress/CheemsCircuitProgressBar.gif" width="240"> |
| `CheemsMonoProgressBar` | <img src="docs/gallery/Progress/CheemsMonoProgressBar.gif" width="240"> |
| `CheemsWaveProgressBall` | <img src="docs/gallery/Progress/CheemsWaveProgressBall.gif" width="240"> |
