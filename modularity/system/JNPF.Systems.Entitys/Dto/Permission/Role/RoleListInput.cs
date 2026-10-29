using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Role;

/// <summary>
/// 角色列表输入.
/// </summary>
[SuppressSniffer]
public class RoleListInput : PageInputBase
{
    /// <summary>
    /// 组织id.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 是否显示子组织.
    /// </summary>
    public int showSubOrganize { get; set; }

    /// <summary>
    /// 岗位id.
    /// </summary>
    public string positionId { get; set; }

    /// <summary>
    /// 角色id.
    /// </summary>
    public string roleId { get; set; }

    /// <summary>
    /// 角色类型.
    /// </summary>
    public string type { get; set; }
}