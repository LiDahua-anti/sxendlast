using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 用户关系映射.
/// </summary>
[SugarTable("BASE_USER_RELATION")]
public class UserRelationEntity : CLDEntityBase
{
    /// <summary>
    /// 获取或设置 用户编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID", ColumnDescription = "用户编号")]
    public string UserId { get; set; }

    /// <summary>
    /// 获取或设置 对象类型(组织 oragnize、岗位 position、分组 group).
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_TYPE", ColumnDescription = "对象类型(组织 oragnize、岗位 position、分组 group)")]
    public string ObjectType { get; set; }

    /// <summary>
    /// 获取或设置 对象主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID", ColumnDescription = "对象主键")]
    public string ObjectId { get; set; }
}