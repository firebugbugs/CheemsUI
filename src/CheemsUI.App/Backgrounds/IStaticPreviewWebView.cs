namespace CheemsUI.App.Backgrounds;

/// <summary>按正确顺序停止设置更新并释放已转为 WPF 图片的预览 WebView。</summary>
internal interface IStaticPreviewWebView
{
    void ReleasePreview();
}
