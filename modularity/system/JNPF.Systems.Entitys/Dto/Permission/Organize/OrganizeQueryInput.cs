using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Organize;

/// <summary>
/// 机构查询输入.
/// </summary>
[SuppressSniffer]
public class OrganizeQueryInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 上级id.
    /// </summary>
    public string parentId { get; set; }
}