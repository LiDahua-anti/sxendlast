using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 分组信息基类.
/// </summary>
[SugarTable("BASE_GROUP")]
public class GroupEntity : CLDSEntityBase
{
    /// <summary>
    /// 获取或设置 分组名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME", ColumnDescription = "分组名称")]
    public string FullName { get; set; }

    /// <summary>
    /// 获取或设置 分组编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE", ColumnDescription = "分组编号")]
    public string EnCode { get; set; }

    /// <summary>
    /// 获取或设置 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION", ColumnDescription = "说明")]
    public string Description { get; set; }
}