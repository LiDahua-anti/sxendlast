using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程撤销.
/// </summary>
[SugarTable("WORKFLOW_REVOKE")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowRevokeEntity : CLDEntityBase
{
    /// <summary>
    /// 撤销任务id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 撤销关联任务id.
    /// </summary>
    [SugarColumn(ColumnName = "F_REVOKE_TASK_ID")]
    public string? RevokeTaskId { get; set; }

    /// <summary>
    /// 撤销表单数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_DATA")]
    public string? FormData { get; set; }
}
