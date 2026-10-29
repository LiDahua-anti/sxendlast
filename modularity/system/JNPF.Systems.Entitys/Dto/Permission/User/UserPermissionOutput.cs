using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 用户权限类型信息输出.
/// </summary>
[SuppressSniffer]
public class UserPermissionOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 子集.
    /// </summary>
    public List<UserPermissionOutput> children { get; set; }

    public bool hasChildren { get; set; }

    public string type { get; set; }
}