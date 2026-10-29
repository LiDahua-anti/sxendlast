using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 触发任务记录.
/// </summary>
[SugarTable("WORKFLOW_TRIGGER_RECORD")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowTriggerRecordEntity : CLDEntityBase
{
    /// <summary>
    /// 触发主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TRIGGER_ID")]
    public string? TriggerId { get; set; }

    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 节点id.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_ID")]
    public string? NodeId { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_NAME")]
    public string? NodeName { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_TIME")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_END_TIME")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 状态 0-通过 1-异常.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int? Status { get; set; } = 0;

    /// <summary>
    /// 错误提示.
    /// </summary>
    [SugarColumn(ColumnName = "F_ERROR_TIP")]
    public string? ErrorTip { get; set; }

    /// <summary>
    /// 错误数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_ERROR_DATA")]
    public string? ErrorData { get; set; }

}
