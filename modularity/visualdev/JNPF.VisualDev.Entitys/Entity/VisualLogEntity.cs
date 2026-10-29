using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.VisualDev.Entitys;

/// <summary>
/// 数据日志.
/// </summary>
[SugarTable("BASE_VISUAL_LOG")]
public class VisualLogEntity : OnlyCLDEntityBase
{
    /// <summary>
    /// 模板id.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODEL_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 日志类型：0-新建，1-编辑.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 数据id.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_ID")]
    public string DataId { get; set; }

    /// <summary>
    /// 日志内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_LOG")]
    public string DataLog { get; set; }
}
