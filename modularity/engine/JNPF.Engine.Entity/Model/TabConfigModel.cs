using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 标签面板配置模型.
/// </summary>
[SuppressSniffer]
public class TabConfigModel
{
    /// <summary>
    /// 是否开启标签面板.
    /// </summary>
    public bool on { get; set; }

    /// <summary>
    /// 关联字段.
    /// </summary>
    public string relationField { get; set; }

    /// <summary>
    /// 是否显示全部.
    /// </summary>
    public bool hasAllTab { get; set; }
}