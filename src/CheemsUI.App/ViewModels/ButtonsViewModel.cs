using CheemsUI.App.Infrastructure;

namespace CheemsUI.App.ViewModels;

/// <summary>
/// 按钮类页面 VM：当前为空，控件示例的演示状态/命令放这里。
/// </summary>
public sealed class ButtonsViewModel : SearchablePageViewModel
{
    public ButtonsViewModel() : base(new Dictionary<string, string>
    {
        ["Dashed"] = "CheemsDashedButton dashed button 虚线按钮 按钮 uiverse css button",
        ["Soft"] = "CheemsSoftButton soft button neumorphism 拟态按钮 软按钮 click me ke1221 uiverse",
        ["Shine"] = "CheemsShineButton shine button glossy glow shimmer 光泽 闪光 扫光 绿色 按钮 mobinkakei uiverse",
        ["Delete"] = "CheemsDeleteButton delete trash bin remove danger 删除 垃圾桶 移除 危险 红色 按钮 vinodjangid07 uiverse",
        ["Subscribe"] = "CheemsSubscribeButton subscribe follow gold outline button 订阅 关注 金色 描边 扩散 按钮 gharsh11032000 uiverse",
        ["Layered3D"] = "CheemsLayered3DButton layered 3d button stack perspective green 分层 三维 立体 堆叠 绿色 按钮 adamgiebl uiverse",
        ["PixelHand"] = "CheemsPixelHandButton pixel hand cursor dotted yellow button 像素 手形 指针 点阵 黄色 按钮 augustin_4687 uiverse",
        ["Creepy"] = "CheemsCreepyButton creepy eyes pupil tracking blink blue button 怪诞 眼睛 瞳孔 跟随 眨眼 蓝色 按钮 jkantner codepen"
    })
    {
    }

    public bool IsDashedVisible => IsControlVisible("Dashed");
    public bool IsSoftVisible => IsControlVisible("Soft");
    public bool IsShineVisible => IsControlVisible("Shine");
    public bool IsDeleteVisible => IsControlVisible("Delete");
    public bool IsSubscribeVisible => IsControlVisible("Subscribe");
    public bool IsLayered3DVisible => IsControlVisible("Layered3D");
    public bool IsPixelHandVisible => IsControlVisible("PixelHand");
    public bool IsCreepyVisible => IsControlVisible("Creepy");

    protected override void OnSearchFilterChanged()
    {
        OnPropertyChanged(nameof(IsDashedVisible));
        OnPropertyChanged(nameof(IsSoftVisible));
        OnPropertyChanged(nameof(IsShineVisible));
        OnPropertyChanged(nameof(IsDeleteVisible));
        OnPropertyChanged(nameof(IsSubscribeVisible));
        OnPropertyChanged(nameof(IsLayered3DVisible));
        OnPropertyChanged(nameof(IsPixelHandVisible));
        OnPropertyChanged(nameof(IsCreepyVisible));
    }
}
