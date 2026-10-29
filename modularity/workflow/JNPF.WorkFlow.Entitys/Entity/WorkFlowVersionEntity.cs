using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程引擎.
/// </summary>
[SugarTable("WORKFLOW_VERSION")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowVersionEntity : CLDEntityBase
{
    /// <summary>
    /// 流程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_ID")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// flowableId.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOWABLE_ID")]
    public string? FlowableId { get; set; }

    /// <summary>
    /// activitiId.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACTIVITI_ID")]
    public string? ActivitiId { get; set; }

    /// <summary>
    /// camundaId.
    /// </summary>
    [SugarColumn(ColumnName = "F_CAMUNDA_ID")]
    public string? CamundaId { get; set; }

    /// <summary>
    /// 流程版本.
    /// </summary>
    [SugarColumn(ColumnName = "F_VERSION")]
    public string? Version { get; set; }

    /// <summary>
    /// 流程模板.
    /// </summary>
    [SugarColumn(ColumnName = "F_XML")]
    public string? Xml { get; set; }

    /// <summary>
    /// 状态(0.设计,1.启用,2.历史).
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int? Status { get; set; }

    /// <summary>
    /// 消息配置id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SEND_CONFIG_IDS")]
    public string? SendConfigIds { get; set; }
}
