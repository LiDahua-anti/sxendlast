using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程节点.
/// </summary>
[SugarTable("WORKFLOW_NODE")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowNodeEntity : CLDEntityBase
{
    /// <summary>
    /// 版本主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }

    /// <summary>
    /// 表单id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_ID")]
    public string? FormId { get; set; }

    /// <summary>
    /// 节点类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_TYPE")]
    public string? NodeType { get; set; }

    /// <summary>
    /// 节点属性Json.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_JSON")]
    public string? NodeJson { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }
}
