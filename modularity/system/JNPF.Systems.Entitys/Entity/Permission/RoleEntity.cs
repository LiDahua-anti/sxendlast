using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 角色信息基类.
/// </summary>
[SugarTable("BASE_ROLE")]
public class RoleEntity : CLDSEntityBase
{
    /// <summary>
    /// 获取或设置 角色名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME", ColumnDescription = "角色名称")]
    public string FullName { get; set; }

    /// <summary>
    /// 获取或设置 角色编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE", ColumnDescription = "角色编号")]
    public string EnCode { get; set; }

    /// <summary>
    /// 获取或设置 角色类型(用户 user,组织 organize,岗位 position).
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE", ColumnDescription = "角色类型(用户 user,组织 organize,岗位 position)")]
    public string Type { get; set; }

    /// <summary>
    /// 获取或设置 扩展属性.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON", ColumnDescription = "扩展属性")]
    public string PropertyJson { get; set; }

    /// <summary>
    /// 获取或设置 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION", ColumnDescription = "描述")]
    public string Description { get; set; }

    /// <summary>
    /// 获取或设置 是否系统角色：1是，其他不是.
    /// </summary>
    [SugarColumn(ColumnName = "F_GLOBAL_MARK", ColumnDescription = "是否系统角色：1是，其他不是")]
    public int GlobalMark { get; set; }

    /// <summary>
    /// 是否岗位约束(0-关闭,1启用).
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_CONDITION", ColumnDescription = "是否岗位约束(0-关闭,1启用)")]
    public int? IsCondition { get; set; }

    /// <summary>
    /// 约束条件(json格式存储).
    /// </summary>
    [SugarColumn(ColumnName = "F_CONDITION_JSON", ColumnDescription = "约束条件(json格式存储)")]
    public string ConditionJson { get; set; }

}