using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 子流程发起参数(依次发起).
/// </summary>
[SugarTable("WORKFLOW_SUBTASK_DATA")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowSubTaskDataEntity : CLDEntityBase
{
    /// <summary>
    /// 任务数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUBTASK_JSON")]
    public string? SubTaskJson { get; set; }

    /// <summary>
    /// 经办数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 退回节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }
}
