using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Permission.Action;

/// <summary>
/// 动作管理列表输出.
/// </summary>
[SuppressSniffer]
public class ActionListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 是否系统.
    /// </summary>
    public int type { get; set; }
}
