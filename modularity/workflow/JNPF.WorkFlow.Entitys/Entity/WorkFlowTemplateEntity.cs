using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程定义.
/// </summary>
[SugarTable("WORKFLOW_TEMPLATE")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowTemplateEntity : CLDSEntityBase
{
    /// <summary>
    /// 流程编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 流程类型（0：标准，1：简单）.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int Type { get; set; } = 0;

    /// <summary>
    /// 流程分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string? Category { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string? Icon { get; set; }

    /// <summary>
    /// 图标背景色.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON_BACKGROUND")]
    public string? IconBackground { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 流程主版本id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }

    /// <summary>
    /// 流程主版本.
    /// </summary>
    [SugarColumn(ColumnName = "F_VERSION")]
    public string? Version { get; set; }

    /// <summary>
    /// activiti部署id.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACTIVITI_ID")]
    public string? ActivitiId { get; set; }

    /// <summary>
    /// flowable部署id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOWABLE_ID")]
    public string? FlowableId { get; set; }

    /// <summary>
    /// camunda部署id.
    /// </summary>
    [SugarColumn(ColumnName = "F_CAMUNDA_ID")]
    public string? CamundaId { get; set; }

    /// <summary>
    /// 流程参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_CONFIG")]
    public string? FlowConfig { get; set; }

    /// <summary>
    /// 可见类型 (1-全部 2-权限).
    /// </summary>
    [SugarColumn(ColumnName = "F_VISIBLE_TYPE")]
    public int? VisibleType { get; set; }

    /// <summary>
    /// 显示类型 (0-全部 1-流程 2-菜单).
    /// </summary>
    [SugarColumn(ColumnName = "F_SHOW_TYPE")]
    public int? ShowType { get; set; }

    /// <summary>
    /// 状态(0.未上架,1.上架,2.下架-继续审批，3.下架-隐藏审批).
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int? Status { get; set; }

    /// <summary>
    /// 应用主建.
    /// </summary>
    [SugarColumn(ColumnName = "F_SYSTEM_ID")]
    public string? SystemId { get; set; }
}
