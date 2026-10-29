using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

/// <summary>
/// 点检项目子表
/// </summary>
[SugarTable("sx_device_item_sub")]
public class DeviceItemSubEntity : CLDSEntityBase
{
    /// <summary>
    /// 项目主表id
    /// </summary>
    [SugarColumn(ColumnName = "main_id", IsNullable = false, ColumnDescription = "/// 项目主表id ///")]
    [Description("项目主表id")]
    public string MainId { get; set; }

    /// <summary>
    /// 父级id
    /// </summary>
    [SugarColumn(ColumnName = "parent_id", IsNullable = true, ColumnDescription = "/// 父级id ///")]
    [Description("父级id")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 点检项
    /// </summary>
    [SugarColumn(ColumnName = "check_item", Length = 200, IsNullable = false, ColumnDescription = "/// 点检项 ///")]
    [Description("点检项")]
    public string? CheckItem { get; set; }

    /// <summary>
    /// 点检场所
    /// </summary>
    [SugarColumn(ColumnName = "check_place", Length = 200, IsNullable = true, ColumnDescription = "/// 点检场所 ///")]
    [Description("点检场所")]
    public string? CheckPlace { get; set; }

    /// <summary>
    /// 点检方法
    /// </summary>
    [SugarColumn(ColumnName = "check_method", Length = 500, IsNullable = true, ColumnDescription = "/// 点检方法 ///")]
    [Description("点检方法")]
    public string? CheckMethod { get; set; }

    /// <summary>
    /// 判定基准
    /// </summary>
    [SugarColumn(ColumnName = "judgment_standard", Length = 500, IsNullable = true, ColumnDescription = "/// 判定基准 ///")]
    [Description("判定基准")]
    public string? JudgmentStandard { get; set; }

    /// <summary>
    /// No
    /// </summary>
    [SugarColumn(ColumnName = "item_no", Length = 50, IsNullable = true, ColumnDescription = "/// No ///")]
    [Description("No")]
    public string? ItemNo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "f_note", Length = 500, IsNullable = true, ColumnDescription = "/// 备注 ///")]
    [Description("备注")]
    public string? Note { get; set; }

}