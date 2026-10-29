using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.VisualDev.Entitys;

/// <summary>
/// 单据配置.
/// </summary>
[SugarTable("BASE_BILL_NUM")]
public class BillNumEntity : OnlyCLDEntityBase
{
    /// <summary>
    /// 单据规则id.
    /// </summary>
    [SugarColumn(ColumnName = "F_RULE_ID")]
    public string RuleId { get; set; }

    /// <summary>
    /// 功能id.
    /// </summary>
    [SugarColumn(ColumnName = "F_VISUAL_ID")]
    public string VisualId { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string FlowId { get; set; }

    /// <summary>
    /// 规则配置.
    /// </summary>
    [SugarColumn(ColumnName = "F_RULE_CONFIG")]
    public string RuleConfig { get; set; }

    /// <summary>
    /// 时间规则值：用于判断是否重置.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATE_VALUE")]
    public string DateValue { get; set; }

    /// <summary>
    /// 单据递增序号.
    /// </summary>
    [SugarColumn(ColumnName = "F_NUM")]
    public int? Num { get; set; }
}
