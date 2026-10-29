using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.HomeData;

/// <summary>
/// 首页模型.
/// </summary>
[SuppressSniffer]
public class HomepageModel
{
    /// <summary>
    /// 编码.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 是否启用.
    /// </summary>
    public int enable { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public object data { get; set; }

    /// <summary>
    /// 是否展示流程.
    /// </summary>
    public bool flowEnabled { get; set; }

    /// <summary>
    /// 流程数据.
    /// </summary>
    public object flowList { get; set; }

    /// <summary>
    /// 应用数据.
    /// </summary>
    public object appList { get; set; }
}
