using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

/// <summary>
/// 点检任务表
/// </summary>
[SugarTable("sx_device_check_task")]
public class DeviceCheckTaskEntity : CLDSEntityBase
{
    /// <summary>
    /// 项目主表id
    /// </summary>
    [SugarColumn(ColumnName = "main_id", IsNullable = false, ColumnDescription = "/// 项目主表id ///")]
    [Description("项目主表id")]
    public string MainId { get; set; }

    /// <summary>
    /// 设备id
    /// </summary>
    [SugarColumn(ColumnName = "device_id", Length = 200, IsNullable = false, ColumnDescription = "/// 设备id ///")]
    [Description("设备id")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// 日期
    /// </summary>
    [SugarColumn(ColumnName = "check_date", IsNullable = false, ColumnDescription = "/// 日期 ///")]
    [Description("日期")]
    public string CheckDate { get; set; }

    /// <summary>
    /// 类型（0日、1月、2年点检）
    /// </summary>
    [SugarColumn(ColumnName = "check_type", Length = 20, IsNullable = false, ColumnDescription = "/// 类型（0日、1月、2年点检） ///")]
    [Description("类型（日、月、年点检）")]
    public int? CheckType { get; set; }

    /// <summary>
    /// 版本名称
    /// </summary>
    [SugarColumn(ColumnName = "version_name", Length = 100, IsNullable = true, ColumnDescription = "/// 版本名称 ///")]
    [Description("版本名称")]
    public string? VersionName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "f_note", Length = 500, IsNullable = true, ColumnDescription = "/// 备注 ///")]
    [Description("备注")]
    public string? Note { get; set; }

    /// <summary>
    /// 是否生成文件过
    /// </summary>
    [SugarColumn(ColumnName = "has_generated_file", IsNullable = false, ColumnDescription = "/// 是否生成文件过 ///")]
    [Description("是否生成文件过")]
    public string HasGeneratedFile { get; set; }

    /// <summary>
    /// 生成次数
    /// </summary>
    [SugarColumn(ColumnName = "generate_count", IsNullable = false, ColumnDescription = "/// 生成次数 ///")]
    [Description("生成次数")]
    public int GenerateCount { get; set; }

    /// <summary>
    /// 课长人
    /// </summary>
    [SugarColumn(ColumnName = "section_chief", Length = 50, IsNullable = true, ColumnDescription = "/// 课长人 ///")]
    [Description("课长人")]
    public string? SectionChiefId { get; set; }

    /// <summary>
    /// 班长人
    /// </summary>
    [SugarColumn(ColumnName = "team_leader", Length = 50, IsNullable = true, ColumnDescription = "/// 班长人 ///")]
    [Description("班长人")]
    public string? TeamLeader { get; set; }

    /// <summary>
    /// 担当人
    /// </summary>
    [SugarColumn(ColumnName = "responsible_person", Length = 50, IsNullable = true, ColumnDescription = "/// 担当人 ///")]
    [Description("担当人")]
    public string? ResponsiblePerson { get; set; }

    /// <summary>
    /// 工时
    /// </summary>
    [SugarColumn(ColumnName = "work_hour", Length = 100, IsNullable = true, ColumnDescription = "/// 工时 ///")]
    [Description("工时")]
    public string? WorkHour { get; set; }

    /// <summary>
    /// 保存年限
    /// </summary>
    [SugarColumn(ColumnName = "save_years", IsNullable = true, ColumnDescription = "/// 保存年限 ///")]
    [Description("保存年限")]
    public string? SaveYears { get; set; }

    /// <summary>
    /// 表单编号
    /// </summary>
    [SugarColumn(ColumnName = "form_number", Length = 100, IsNullable = true, ColumnDescription = "/// 表单编号 ///")]
    [Description("表单编号")]
    public string? FormNumber { get; set; }

    /// <summary>
    /// 特记事项
    /// </summary>
    [SugarColumn(ColumnName = "special_notes", Length = 1000, IsNullable = true, ColumnDescription = "/// 特记事项 ///")]
    [Description("特记事项")]
    public string? SpecialNotes { get; set; }

    /// <summary>
    /// 设备缩略图
    /// </summary>
    [SugarColumn(ColumnName = "device_thumbnail", Length = 500, IsNullable = true, ColumnDescription = "/// 设备缩略图 ///")]
    [Description("设备缩略图")]
    public string? DeviceThumbnail { get; set; }

    /// <summary>
    /// 综合判定结果
    /// </summary>
    [SugarColumn(ColumnName = "overall_judgment", Length = 200, IsNullable = true, ColumnDescription = "/// 综合判定结果 ///")]
    [Description("综合判定结果")]
    public string? OverallJudgment { get; set; }

    /// <summary>
    /// 点检日
    /// </summary>
    [SugarColumn(ColumnName = "check_day", IsNullable = true, ColumnDescription = "/// 点检日 ///")]
    [Description("点检日")]
    public string? CheckDay { get; set; }

    /// <summary>
    /// 点检者
    /// </summary>
    [SugarColumn(ColumnName = "check_person", Length = 50, IsNullable = true, ColumnDescription = "/// 点检者 ///")]
    [Description("点检者")]
    public string? CheckPerson { get; set; }

    // 31个点检者字段
    [SugarColumn(ColumnName = "checkperson1", Length = 200, IsNullable = true)]
    [Description("点检者1")]
    public string? CheckPerson1 { get; set; }

    [SugarColumn(ColumnName = "checkperson2", Length = 200, IsNullable = true)]
    [Description("点检者2")]
    public string? CheckPerson2 { get; set; }

    [SugarColumn(ColumnName = "checkperson3", Length = 200, IsNullable = true)]
    [Description("点检者3")]
    public string? CheckPerson3 { get; set; }

    [SugarColumn(ColumnName = "checkperson4", Length = 200, IsNullable = true)]
    [Description("点检者4")]
    public string? CheckPerson4 { get; set; }

    [SugarColumn(ColumnName = "checkperson5", Length = 200, IsNullable = true)]
    [Description("点检者5")]
    public string? CheckPerson5 { get; set; }

    [SugarColumn(ColumnName = "checkperson6", Length = 200, IsNullable = true)]
    [Description("点检者6")]
    public string? CheckPerson6 { get; set; }

    [SugarColumn(ColumnName = "checkperson7", Length = 200, IsNullable = true)]
    [Description("点检者7")]
    public string? CheckPerson7 { get; set; }

    [SugarColumn(ColumnName = "checkperson8", Length = 200, IsNullable = true)]
    [Description("点检者8")]
    public string? CheckPerson8 { get; set; }

    [SugarColumn(ColumnName = "checkperson9", Length = 200, IsNullable = true)]
    [Description("点检者9")]
    public string? CheckPerson9 { get; set; }

    [SugarColumn(ColumnName = "checkperson10", Length = 200, IsNullable = true)]
    [Description("点检者10")]
    public string? CheckPerson10 { get; set; }

    [SugarColumn(ColumnName = "checkperson11", Length = 200, IsNullable = true)]
    [Description("点检者11")]
    public string? CheckPerson11 { get; set; }

    [SugarColumn(ColumnName = "checkperson12", Length = 200, IsNullable = true)]
    [Description("点检者12")]
    public string? CheckPerson12 { get; set; }

    [SugarColumn(ColumnName = "checkperson13", Length = 200, IsNullable = true)]
    [Description("点检者13")]
    public string? CheckPerson13 { get; set; }

    [SugarColumn(ColumnName = "checkperson14", Length = 200, IsNullable = true)]
    [Description("点检者14")]
    public string? CheckPerson14 { get; set; }

    [SugarColumn(ColumnName = "checkperson15", Length = 200, IsNullable = true)]
    [Description("点检者15")]
    public string? CheckPerson15 { get; set; }

    [SugarColumn(ColumnName = "checkperson16", Length = 200, IsNullable = true)]
    [Description("点检者16")]
    public string? CheckPerson16 { get; set; }

    [SugarColumn(ColumnName = "checkperson17", Length = 200, IsNullable = true)]
    [Description("点检者17")]
    public string? CheckPerson17 { get; set; }

    [SugarColumn(ColumnName = "checkperson18", Length = 200, IsNullable = true)]
    [Description("点检者18")]
    public string? CheckPerson18 { get; set; }

    [SugarColumn(ColumnName = "checkperson19", Length = 200, IsNullable = true)]
    [Description("点检者19")]
    public string? CheckPerson19 { get; set; }

    [SugarColumn(ColumnName = "checkperson20", Length = 200, IsNullable = true)]
    [Description("点检者20")]
    public string? CheckPerson20 { get; set; }

    [SugarColumn(ColumnName = "checkperson21", Length = 200, IsNullable = true)]
    [Description("点检者21")]
    public string? CheckPerson21 { get; set; }

    [SugarColumn(ColumnName = "checkperson22", Length = 200, IsNullable = true)]
    [Description("点检者22")]
    public string? CheckPerson22 { get; set; }

    [SugarColumn(ColumnName = "checkperson23", Length = 200, IsNullable = true)]
    [Description("点检者23")]
    public string? CheckPerson23 { get; set; }

    [SugarColumn(ColumnName = "checkperson24", Length = 200, IsNullable = true)]
    [Description("点检者24")]
    public string? CheckPerson24 { get; set; }

    [SugarColumn(ColumnName = "checkperson25", Length = 200, IsNullable = true)]
    [Description("点检者25")]
    public string? CheckPerson25 { get; set; }

    [SugarColumn(ColumnName = "checkperson26", Length = 200, IsNullable = true)]
    [Description("点检者26")]
    public string? CheckPerson26 { get; set; }

    [SugarColumn(ColumnName = "checkperson27", Length = 200, IsNullable = true)]
    [Description("点检者27")]
    public string? CheckPerson27 { get; set; }

    [SugarColumn(ColumnName = "checkperson28", Length = 200, IsNullable = true)]
    [Description("点检者28")]
    public string? CheckPerson28 { get; set; }

    [SugarColumn(ColumnName = "checkperson29", Length = 200, IsNullable = true)]
    [Description("点检者29")]
    public string? CheckPerson29 { get; set; }

    [SugarColumn(ColumnName = "checkperson30", Length = 200, IsNullable = true)]
    [Description("点检者30")]
    public string? CheckPerson30 { get; set; }

    [SugarColumn(ColumnName = "checkperson31", Length = 200, IsNullable = true)]
    [Description("点检者31")]
    public string? CheckPerson31 { get; set; }

}