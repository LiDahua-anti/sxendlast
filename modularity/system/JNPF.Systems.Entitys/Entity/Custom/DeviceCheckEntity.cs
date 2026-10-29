using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

/// <summary>
/// 点检内容表
/// </summary>
[SugarTable("sx_device_check")]
public class DeviceCheckEntity : CLDSEntityBase
{
    /// <summary>
    /// 点检任务id
    /// </summary>
    [SugarColumn(ColumnName = "task_id", IsNullable = false, ColumnDescription = "/// 点检任务id ///")]
    [Description("点检任务id")]
    public string TaskId { get; set; }

    /// <summary>
    /// 检查项id
    /// </summary>
    [SugarColumn(ColumnName = "sub_id", IsNullable = false, ColumnDescription = "/// 检查项id ///")]
    [Description("点检子项id")]
    public string SubId { get; set; }

    /// <summary>
    /// 点检者
    /// </summary>
    [SugarColumn(ColumnName = "check_person", Length = 50, IsNullable = true, ColumnDescription = "/// 点检者 ///")]
    [Description("点检者")]
    public string? CheckPerson { get; set; }

    /// <summary>
    /// 点检结果
    /// </summary>
    [SugarColumn(ColumnName = "check_result", Length = 500, IsNullable = true, ColumnDescription = "/// 点检结果 ///")]
    [Description("点检结果")]
    public string? CheckResult { get; set; }

    /// <summary>
    /// 判定
    /// </summary>
    [SugarColumn(ColumnName = "judgment", Length = 50, IsNullable = true, ColumnDescription = "/// 判定 ///")]
    [Description("判定")]
    public string? Judgment { get; set; }

    /// <summary>
    /// 异常项目描述
    /// </summary>
    [SugarColumn(ColumnName = "exception_desc", Length = 1000, IsNullable = true, ColumnDescription = "/// 异常项目描述 ///")]
    [Description("异常项目描述")]
    public string? ExceptionDesc { get; set; }

    // 31个值字段
    [SugarColumn(ColumnName = "value1", Length = 200, IsNullable = true)]
    [Description("值1")]
    public string? Value1 { get; set; }

    [SugarColumn(ColumnName = "value2", Length = 200, IsNullable = true)]
    [Description("值2")]
    public string? Value2 { get; set; }

    [SugarColumn(ColumnName = "value3", Length = 200, IsNullable = true)]
    [Description("值3")]
    public string? Value3 { get; set; }

    [SugarColumn(ColumnName = "value4", Length = 200, IsNullable = true)]
    [Description("值4")]
    public string? Value4 { get; set; }

    [SugarColumn(ColumnName = "value5", Length = 200, IsNullable = true)]
    [Description("值5")]
    public string? Value5 { get; set; }

    [SugarColumn(ColumnName = "value6", Length = 200, IsNullable = true)]
    [Description("值6")]
    public string? Value6 { get; set; }

    [SugarColumn(ColumnName = "value7", Length = 200, IsNullable = true)]
    [Description("值7")]
    public string? Value7 { get; set; }

    [SugarColumn(ColumnName = "value8", Length = 200, IsNullable = true)]
    [Description("值8")]
    public string? Value8 { get; set; }

    [SugarColumn(ColumnName = "value9", Length = 200, IsNullable = true)]
    [Description("值9")]
    public string? Value9 { get; set; }

    [SugarColumn(ColumnName = "value10", Length = 200, IsNullable = true)]
    [Description("值10")]
    public string? Value10 { get; set; }

    [SugarColumn(ColumnName = "value11", Length = 200, IsNullable = true)]
    [Description("值11")]
    public string? Value11 { get; set; }

    [SugarColumn(ColumnName = "value12", Length = 200, IsNullable = true)]
    [Description("值12")]
    public string? Value12 { get; set; }

    [SugarColumn(ColumnName = "value13", Length = 200, IsNullable = true)]
    [Description("值13")]
    public string? Value13 { get; set; }

    [SugarColumn(ColumnName = "value14", Length = 200, IsNullable = true)]
    [Description("值14")]
    public string? Value14 { get; set; }

    [SugarColumn(ColumnName = "value15", Length = 200, IsNullable = true)]
    [Description("值15")]
    public string? Value15 { get; set; }

    [SugarColumn(ColumnName = "value16", Length = 200, IsNullable = true)]
    [Description("值16")]
    public string? Value16 { get; set; }

    [SugarColumn(ColumnName = "value17", Length = 200, IsNullable = true)]
    [Description("值17")]
    public string? Value17 { get; set; }

    [SugarColumn(ColumnName = "value18", Length = 200, IsNullable = true)]
    [Description("值18")]
    public string? Value18 { get; set; }

    [SugarColumn(ColumnName = "value19", Length = 200, IsNullable = true)]
    [Description("值19")]
    public string? Value19 { get; set; }

    [SugarColumn(ColumnName = "value20", Length = 200, IsNullable = true)]
    [Description("值20")]
    public string? Value20 { get; set; }

    [SugarColumn(ColumnName = "value21", Length = 200, IsNullable = true)]
    [Description("值21")]
    public string? Value21 { get; set; }

    [SugarColumn(ColumnName = "value22", Length = 200, IsNullable = true)]
    [Description("值22")]
    public string? Value22 { get; set; }

    [SugarColumn(ColumnName = "value23", Length = 200, IsNullable = true)]
    [Description("值23")]
    public string? Value23 { get; set; }

    [SugarColumn(ColumnName = "value24", Length = 200, IsNullable = true)]
    [Description("值24")]
    public string? Value24 { get; set; }

    [SugarColumn(ColumnName = "value25", Length = 200, IsNullable = true)]
    [Description("值25")]
    public string? Value25 { get; set; }

    [SugarColumn(ColumnName = "value26", Length = 200, IsNullable = true)]
    [Description("值26")]
    public string? Value26 { get; set; }

    [SugarColumn(ColumnName = "value27", Length = 200, IsNullable = true)]
    [Description("值27")]
    public string? Value27 { get; set; }

    [SugarColumn(ColumnName = "value28", Length = 200, IsNullable = true)]
    [Description("值28")]
    public string? Value28 { get; set; }

    [SugarColumn(ColumnName = "value29", Length = 200, IsNullable = true)]
    [Description("值29")]
    public string? Value29 { get; set; }

    [SugarColumn(ColumnName = "value30", Length = 200, IsNullable = true)]
    [Description("值30")]
    public string? Value30 { get; set; }

    [SugarColumn(ColumnName = "value31", Length = 200, IsNullable = true)]
    [Description("值31")]
    public string? Value31 { get; set; }
}