using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程驳回数据.
/// </summary>
[SugarTable("WORKFLOW_REJECT_DATA")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowRejectDataEntity : CLDEntityBase
{
    /// <summary>
    /// 任务数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_JSON")]
    public string? TaskJson { get; set; }

    /// <summary>
    /// 经办数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_OPERATOR_JSON")]
    public string? OperatorJson { get; set; }

    /// <summary>
    /// 退回节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }

    /// <summary>
    /// 外部节点数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_EVENT_LOG_JSON")]
    public string? EventLogJson { get; set; }
}
