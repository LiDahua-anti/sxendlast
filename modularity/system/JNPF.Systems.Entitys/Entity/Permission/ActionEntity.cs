using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 动作管理基类.
/// </summary>
[SugarTable("BASE_ACTION")]
public class ActionEntity : CLDEntityBase
{
    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME", ColumnDescription = "身份名称")]
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE", ColumnDescription = "身份编码")]
    public string EnCode { get; set; }

    /// <summary>
    /// 是否系统（1-系统，2-自定义）.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE", ColumnDescription = "是否系统（1-系统，2-自定义）")]
    public int Type { get; set; } = 1;

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION", ColumnDescription = "说明")]
    public string Description { get; set; }
}