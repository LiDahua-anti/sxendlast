using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 表单套件.
/// </summary>
[SugarTable("BASE_VISUAL_KIT")]
public class VisualKitEntity : CLDSEntityBase
{
    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 表单套件Json.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_DATA")]
    public string FormData { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string Icon { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}
