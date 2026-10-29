using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.RoleRelation;

/// <summary>
/// 角色分页列表输出.
/// </summary>
[SuppressSniffer]
public class RolePageListOutput
{
    /// <summary>
    /// 角色id.
    /// </summary>
    public string roleId { get; set; }

    /// <summary>
    /// 角色名称.
    /// </summary>
    public string roleName { get; set; }
}