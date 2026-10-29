using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程传阅.
/// </summary>
[SugarTable("WORKFLOW_CIRCULATE")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowCirculateEntity : CLDEntityBase
{
    /// <summary>
    /// 是否已读.
    /// </summary>
    [SugarColumn(ColumnName = "F_READ")]
    public int? Read { get; set; }

    /// <summary>
    /// 抄送人员.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string? UserId { get; set; }

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
    /// 节点主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_ID")]
    public string? NodeId { get; set; }

    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 经办主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OPERATOR_ID")]
    public string? OperatorId { get; set; }
}
