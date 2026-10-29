using JNPF.DependencyInjection;

namespace JNPF.Apps.Entitys.Dto;

/// <summary>
/// app 系统列表输出.
/// </summary>
[SuppressSniffer]
public class AppSysOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 系统名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 系统编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }
}
