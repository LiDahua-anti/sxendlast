using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程节点记录.
/// </summary>
[SugarTable("WORKFLOW_NODE_RECORD")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowNodeRecordEntity : CLDEntityBase
{
    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }

    /// <summary>
    /// 节点ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_ID")]
    public string? NodeId { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_NAME")]
    public string? NodeName { get; set; }

    /// <summary>
    /// 节点状态 1-已提交 2-已通过 3-已拒绝 4-审批中 5-已退回 6-已撤回..
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_STATUS")]
    public int NodeStatus { get; set; }
}
