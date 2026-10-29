using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 常用流程.
/// </summary>
[SugarTable("WORKFLOW_COMMON")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowCommonEntity : CLDEntityBase
{
    /// <summary>
    /// 流程版本id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }
}
