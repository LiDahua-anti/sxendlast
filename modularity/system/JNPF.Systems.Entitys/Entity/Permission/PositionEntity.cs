using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 岗位信息基类.
/// </summary>
[SugarTable("BASE_POSITION")]
public class PositionEntity : CLDSEntityBase
{
    /// <summary>
    /// 获取或设置 岗位名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME", ColumnDescription = "岗位名称")]
    public string FullName { get; set; }

    /// <summary>
    /// 获取或设置 岗位编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE", ColumnDescription = "岗位编号")]
    public string EnCode { get; set; }

    /// <summary>
    /// 获取或设置 岗位类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE", ColumnDescription = "岗位类型")]
    public string Type { get; set; }

    /// <summary>
    /// 获取或设置 岗位Id树.
    /// </summary>
    [SugarColumn(ColumnName = "F_POSITION_ID_TREE", ColumnDescription = "岗位Id树")]
    public string PositionIdTree { get; set; }

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
    /// 机构主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORGANIZE_ID", ColumnDescription = "机构主键")]
    public string OrganizeId { get; set; }

    /// <summary>
    /// 上级岗位.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID", ColumnDescription = "上级岗位")]
    public string ParentId { get; set; }

    /// <summary>
    /// 岗位责任人.
    /// </summary>
    [SugarColumn(ColumnName = "F_DUTY_USER", ColumnDescription = "岗位责任人")]
    public string DutyUserId { get; set; }

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

    /// <summary>
    /// 是否默认岗位（0-否,1-是）.
    /// </summary>
    [SugarColumn(ColumnName = "F_DEFAULT_MARK", ColumnDescription = "是否默认岗位（0-否,1-是）")]
    public int? DefaultMark { get; set; }

}