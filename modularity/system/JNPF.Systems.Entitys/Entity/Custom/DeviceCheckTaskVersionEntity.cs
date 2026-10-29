using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

/// <summary>
/// 点检任务版本表
/// </summary>
[SugarTable("sx_device_check_task_version")]
public class DeviceCheckTaskVersionEntity : CLDSEntityBase
{
    /// <summary>
    /// 点检任务id
    /// </summary>
    [SugarColumn(ColumnName = "task_id", IsNullable = false, ColumnDescription = "/// 点检任务id ///")]
    [Description("点检任务id")]
    public string TaskId { get; set; }

    /// <summary>
    /// 改版
    /// </summary>
    [SugarColumn(ColumnName = "revision", Length = 50, IsNullable = false, ColumnDescription = "/// 改版 ///")]
    [Description("改版")]
    public string? Revision { get; set; }

    /// <summary>
    /// 年月日
    /// </summary>
    [SugarColumn(ColumnName = "revision_date", IsNullable = false, ColumnDescription = "/// 年月日 ///")]
    [Description("年月日")]
    public string RevisionDate { get; set; }

    /// <summary>
    /// 理由
    /// </summary>
    [SugarColumn(ColumnName = "reason", Length = 500, IsNullable = true, ColumnDescription = "/// 理由 ///")]
    [Description("理由")]
    public string? Reason { get; set; }

    /// <summary>
    /// 做成
    /// </summary>
    [SugarColumn(ColumnName = "created_by", Length = 50, IsNullable = true, ColumnDescription = "/// 做成 ///")]
    [Description("做成")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 承认者
    /// </summary>
    [SugarColumn(ColumnName = "approver", Length = 50, IsNullable = true, ColumnDescription = "/// 承认者 ///")]
    [Description("承认者")]
    public string? Approver { get; set; }
}