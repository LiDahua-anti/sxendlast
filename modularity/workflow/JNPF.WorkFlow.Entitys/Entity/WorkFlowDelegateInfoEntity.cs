using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程委托.
/// </summary>
[SugarTable("WORKFLOW_DELEGATE_INFO")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowDelegateInfoEntity : CLDEntityBase
{
    /// <summary>
    /// 委托主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_DELEGATE_ID")]
    public string? DelegateId { get; set; }

    /// <summary>
    /// 接收人用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TO_USER_ID")]
    public string? ToUserId { get; set; }

    /// <summary>
    /// 接收人用户名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_TO_USER_NAME")]
    public string? ToUserName { get; set; }

    /// <summary>
    /// 状态(0.待确认 1.已接受 2.已拒绝).
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int? Status { get; set; }
}
