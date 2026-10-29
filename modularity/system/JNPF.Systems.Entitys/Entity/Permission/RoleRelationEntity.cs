using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 用户关系映射.
/// </summary>
[SugarTable("BASE_ROLE_RELATION")]
public class RoleRelationEntity : CLDEntityBase
{
    /// <summary>
    /// 获取或设置 角色Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_ROLE_ID", ColumnDescription = "角色Id")]
    public string RoleId { get; set; }

    /// <summary>
    /// 对象类型（用户 user、组织 oragnize、岗位 position）.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_TYPE", ColumnDescription = "对象类型（用户 user、组织 oragnize、岗位 position）")]
    public string ObjectType { get; set; }

    /// <summary>
    /// 获取或设置 对象主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID", ColumnDescription = "对象主键")]
    public string ObjectId { get; set; }
}