using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程实例.
/// </summary>
[SugarTable("WORKFLOW_TASK")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowTaskEntity : CLDEntityBase
{
    /// <summary>
    /// 父级实例id.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 流程引擎实例id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INSTANCE_ID")]
    public string? InstanceId { get; set; }

    /// <summary>
    /// 任务编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 任务标题.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    [SugarColumn(ColumnName = "F_URGENT")]
    public int? Urgent { get; set; }

    /// <summary>
    /// 流程主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }

    /// <summary>
    /// 流程编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_CODE")]
    public string? FlowCode { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_NAME")]
    public string? FlowName { get; set; }

    /// <summary>
    /// 流程类型（0：标准，1：简单）.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_TYPE")]
    public int? FlowType { get; set; }

    /// <summary>
    /// 流程分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_CATEGORY")]
    public string? FlowCategory { get; set; }

    /// <summary>
    /// 流程版本.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_VERSION")]
    public string? FlowVersion { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_TIME")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_END_TIME")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 当前节点.
    /// </summary>
    [SugarColumn(ColumnName = "F_CURRENT_NODE_NAME")]
    public string? CurrentNodeName { get; set; }

    /// <summary>
    /// 当前节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_CURRENT_NODE_CODE")]
    public string? CurrentNodeCode { get; set; }

    /// <summary>
    /// 任务状态：【参考WorkFlowTaskStatusEnum】.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int Status { get; set; } = 0;

    /// <summary>
    /// 历史任务状态：【参考WorkFlowTaskStatusEnum】.
    /// </summary>
    [SugarColumn(ColumnName = "F_HIS_STATUS")]
    public int HisStatus { get; set; } = 0;

    /// <summary>
    /// 同步异步（0：同步，1：异步）.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_ASYNC")]
    public int? IsAsync { get; set; }

    /// <summary>
    /// 是否能恢复（0：能，1：不能）.
    /// </summary>
    [SugarColumn(ColumnName = "F_RESTORE")]
    public int? Restore { get; set; }

    /// <summary>
    /// 委托发起人.
    /// </summary>
    [SugarColumn(ColumnName = "F_DELEGATE_USER_ID")]
    public string? DelegateUserId { get; set; }

    /// <summary>
    /// 流程引擎类型;1.flowable,2,activity,3.canmda.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENGINE_TYPE")]
    public int? EngineType { get; set; }

    /// <summary>
    /// 流程主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_ID")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// 流程引擎主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENGINE_ID")]
    public string? EngineId { get; set; }

    /// <summary>
    /// 退回节点id(当前节点审批).
    /// </summary>
    [SugarColumn(ColumnName = "F_REJECT_DATA_ID")]
    public string? RejectDataId { get; set; }

    /// <summary>
    /// 子流程节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUB_CODE")]
    public string? SubCode { get; set; }

    /// <summary>
    /// 子流程参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_SUB_PARAMETER")]
    public string? SubParameter { get; set; }

    /// <summary>
    /// 全局参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_GLOBAL_PARAMETER")]
    public string? GlobalParameter { get; set; }

    /// <summary>
    /// 是否归档（null-未配置 0-未归档 1-已归档）.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_FILE")]
    public int? IsFile { get; set; }

    /// <summary>
    /// 类型;0-功能任务 1.发起任务,.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }
}
