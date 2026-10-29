using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Permission.Standing;

/// <summary>
/// 身份创建输入.
/// </summary>
[SuppressSniffer]
public class StandingCrInput
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
    /// 是否系统.
    /// </summary>
    public int isSystem { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

}
