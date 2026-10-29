using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.RoleRelation;

/// <summary>
/// 角色关系查询列表输入.
/// </summary>
[SuppressSniffer]
public class RoleRelationListInput : PageInputBase
{
    /// <summary>
    /// 角色id.
    /// </summary>
    public string roleId { get; set; }

    /// <summary>
    /// 角色类型.
    /// </summary>
    public string type { get; set; }
}