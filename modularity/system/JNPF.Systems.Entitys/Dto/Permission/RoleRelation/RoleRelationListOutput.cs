using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.RoleRelation;

/// <summary>
/// 角色用户关系列表.
/// </summary>
[SuppressSniffer]
public class RoleUserRelationListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string account { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string realName { get; set; }

    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    public string gender { get; set; }

    /// <summary>
    /// 手机.
    /// </summary>
    public string mobilePhone { get; set; }

    /// <summary>
    /// 岗位.
    /// </summary>
    public string position { get; set; }
}

/// <summary>
/// 角色组织关系列表.
/// </summary>
[SuppressSniffer]
public class RoleOrganizeRelationListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 组织+名称.
    /// </summary>
    public string orgNameTree { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }
}

/// <summary>
/// 角色岗位关系列表.
/// </summary>
[SuppressSniffer]
public class RolePositionRelationListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 组织+名称.
    /// </summary>
    public string orgNameTree { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }
}