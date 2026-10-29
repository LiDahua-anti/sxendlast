using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程条件历史记录.
/// </summary>
[SugarTable("WORKFLOW_TASK_LINE")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowTaskLineEntity : CLDEntityBase
{
    /// <summary>
    /// 任务id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 条件编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_LINE_KEY")]
    public string? LineKey { get; set; }

    /// <summary>
    /// 条件值.
    /// </summary>
    [SugarColumn(ColumnName = "F_LINE_VALUE")]
    public string? LineValue { get; set; }
}
