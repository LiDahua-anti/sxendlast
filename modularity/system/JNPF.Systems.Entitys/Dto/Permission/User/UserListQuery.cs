using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 用户列表查询输入.
/// </summary>
[SuppressSniffer]
public class UserListQuery : CommonInput
{
    /// <summary>
    /// 机构ID.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 岗位ID.
    /// </summary>
    public string positionId { get; set; }

    /// <summary>
    /// 用户组ID.
    /// </summary>
    public string groupId { get; set; }

    /// <summary>
    /// 角色ID.
    /// </summary>
    public string roleId { get; set; }

    /// <summary>
    /// 是否显示子组织用户.
    /// </summary>
    public int? showSubOrganize { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    public int? gender { get; set; }
}