using Aprillz.MewUI.Controls;
using MewUI.Generator.Runtime;

namespace SampleApp;

/// <summary>
/// 示例 ViewModel：演示类级别 [AsObservableValue] 用法。
/// 只需在类上标记特性，所有有 get+set 的属性都会自动生成 ObservableValue。
/// </summary>
[AsObservableValue]
public partial class MyViewModel
{
    /// <summary>
    /// 服务器主机地址，支持可空字符串。
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// 服务端口号，默认 8080。
    /// </summary>
    public int Port { get; set; } = 8080;

    public string? Name { get; set; }

    // 实现生成的 partial 钩子方法，监听 Host 值变化
    partial void OnHostChanging(string? value)
    {
        Console.WriteLine($"[Changing] Host: {Host} -> {value}");
    }

    partial void OnHostChanged(string? value)
    {
        Console.WriteLine($"[Changed]  Host = {value}");
    }
}


public partial class MewPropertyModel : FrameworkElement
{

    /// <summary>
    /// 名称
    /// </summary>
    [AsMewProperty(DefaultValue = "100")]
    public partial string Name { get; set; }


    /// <summary>
    /// 值
    /// </summary>
    [AsMewProperty(DefaultValue = 50, MewPropertyOptions = MewPropertyOptions.BindsTwoWayByDefault)]
    public partial int IntTest { get; set; }

    [AsMewProperty]
    public partial double DoubleTest { get; set; }

    [AsMewProperty(10)]
    public partial int Int2Test { get; set; }

    [AsMewProperty(true)]
    public partial bool BoolTest { get; set; }
}
