using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.RoleRelation;

/// <summary>
/// 角色关系输入.
/// </summary>
[SuppressSniffer]
public class RoleRelationInput
{
    /// <summary>
    /// 角色列表.
    /// </summary>
    public List<string> ids { get; set; }
}