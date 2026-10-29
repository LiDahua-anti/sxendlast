namespace JNPF.WorkFlow.Entitys.Model.Conifg;

public class CounterSignRuleConfig
{
    /// <summary>
    /// 会签同意规则 1-比例 2-人数.
    /// </summary>
    public int auditType { get; set; }

    /// <summary>
    /// 同意比例.
    /// </summary>
    public int auditRatio { get; set; }

    /// <summary>
    /// 同意人数.
    /// </summary>
    public int auditNum { get; set; }

    /// <summary>
    /// 会签不同意规则 0-无 1-比例 2-人数.
    /// </summary>
    public int rejectType { get; set; }

    /// <summary>
    /// 不同意比例.
    /// </summary>
    public int rejectRatio { get; set; }

    /// <summary>
    /// 不同意人数.
    /// </summary>
    public int rejectNum { get; set; }

    /// <summary>
    /// 会签计算规则 1-实时计算 2-延后计算.
    /// </summary>
    public int calculateType { get; set; }
}
