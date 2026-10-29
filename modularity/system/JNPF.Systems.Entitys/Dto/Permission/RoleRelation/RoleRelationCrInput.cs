using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.RoleRelation;

/// <summary>
/// 角色关系创建输入.
/// </summary>
[SuppressSniffer]
public class RoleRelationCrInput
{
    /// <summary>
    /// 角色id.
    /// </summary>
    public string roleId { get; set; }

    /// <summary>
    /// objectId.
    /// </summary>
    public string objectId { get; set; }

    /// <summary>
    /// 关系类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 角色数组.
    /// </summary>
    public List<string> ids { get; set; }
}
