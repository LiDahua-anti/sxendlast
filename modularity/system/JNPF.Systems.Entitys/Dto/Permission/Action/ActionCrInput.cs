using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Permission.Action;

/// <summary>
/// 动作管理创建输入.
/// </summary>
[SuppressSniffer]
public class ActionCrInput
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }
}
