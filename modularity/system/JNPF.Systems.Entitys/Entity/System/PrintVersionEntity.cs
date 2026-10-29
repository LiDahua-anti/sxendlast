using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 打印模板版本.
/// </summary>
[SugarTable("BASE_PRINT_VERSION")]
[Tenant(ClaimConst.TENANTID)]
public class PrintVersionEntity : CLDEntityBase
{
    /// <summary>
    /// 模板id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_ID")]
    public string TemplateId { get; set; }

    /// <summary>
    /// 状态：0-设计中，1-启用中，2-已归档.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATE")]
    public int State { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    [SugarColumn(ColumnName = "F_VERSION")]
    public int Version { get; set; }

    /// <summary>
    /// 打印模板json.
    /// </summary>
    [SugarColumn(ColumnName = "F_PRINT_TEMPLATE")]
    public string PrintTemplate { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 转换配置.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONVERT_CONFIG")]
    public string ConvertConfig { get; set; }

    /// <summary>
    /// 全局配置.
    /// </summary>
    [SugarColumn(ColumnName = "F_GLOBAL_CONFIG")]
    public string GlobalConfig { get; set; }
}