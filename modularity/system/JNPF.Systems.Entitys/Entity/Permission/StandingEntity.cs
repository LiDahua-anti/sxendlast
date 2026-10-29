using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 身份管理信息基类.
/// </summary>
[SugarTable("BASE_STANDING")]
public class StandingEntity : CLDSEntityBase
{
    /// <summary>
    /// 身份名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME", ColumnDescription = "身份名称")]
    public string FullName { get; set; }

    /// <summary>
    /// 身份编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE", ColumnDescription = "身份编码")]
    public string EnCode { get; set; }

    /// <summary>
    /// 是否系统（1-系统，2-自定义）.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_SYSTEM", ColumnDescription = "是否系统（1-系统，2-自定义）")]
    public int IsSystem { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION", ColumnDescription = "说明")]
    public string Description { get; set; }
}