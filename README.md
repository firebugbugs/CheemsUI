# CheemsUI

一套注重质感的 WPF 自绘控件库。所有控件均以 ControlTemplate + Storyboard 纯 WPF 实现，不依赖任何第三方库；配色、字体走语义化资源键，换主题只改资源不动模板。

[![点击加入 QQ 群：1094431427](https://img.shields.io/badge/QQ%E7%BE%A4-1094431427-12B7F5?logo=tencentqq&logoColor=white)](https://qm.qq.com/q/BWDv2TTZKK)

<p align="center">
  <a href="CONTROLS.md">
    <img src="https://img.shields.io/badge/%E6%8E%A7%E4%BB%B6%E6%80%BB%E8%A7%88-42_%E4%B8%AA%E6%8E%A7%E4%BB%B6-7C5CFF?style=for-the-badge" alt="控件总览 - 42 个控件">
  </a>
</p>

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

- **42 个控件**：按钮、加载动画（Loader）、开关、输入框、进度条五大类
- **纯 WPF 实现**：无第三方依赖，模板即全部视觉，可随意拆改
- **语义化主题**：`Cheems.Brush.*` / `Cheems.FontFamily.*` 等资源键全局覆盖，一处改色处处生效
