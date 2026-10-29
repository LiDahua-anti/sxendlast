using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.PrintDev;

/// <summary>
/// 打印模板全局配置模型.
/// </summary>
[SuppressSniffer]
public class PrintDevGlobalConfigModel
{
    /// <summary>
    /// 多联打印设置.
    /// </summary>
    public List<SliceConfig> sliceConfig { get; set; }
}

public class SliceConfig
{
    /// <summary>
    /// 数据集.
    /// </summary>
    public string dataSet { get; set; }

    /// <summary>
    /// 每页条数.
    /// </summary>
    public int limit { get; set; }
}
