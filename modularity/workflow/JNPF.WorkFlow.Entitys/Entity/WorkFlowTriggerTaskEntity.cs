using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 触发流程任务.
/// </summary>
[SugarTable("WORKFLOW_TRIGGER_TASK")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowTriggerTaskEntity : CLDEntityBase
{
    /// <summary>
    /// 任务标题.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 重试任务主键id.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 重试任务开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_TIME")]
    public DateTime? ParentTime { get; set; }

    /// <summary>
    /// 标准任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 同步异步（0：同步，1：异步）.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_ASYNC")]
    public int? IsAsync { get; set; }

    /// <summary>
    /// 触发任务上级节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }

    /// <summary>
    /// 触发任务上级节点id.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_ID")]
    public string? NodeId { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_TIME")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 流程主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA")]
    public string? Data { get; set; }

    /// <summary>
    /// 数据主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_ID")]
    public string? DataId { get; set; }

    /// <summary>
    /// 流程引擎实例id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INSTANCE_ID")]
    public string? InstanceId { get; set; }

    /// <summary>
    /// 流程引擎类型;1.flowable,2,activity,3.canmda.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENGINE_TYPE")]
    public int? EngineType { get; set; }

    /// <summary>
    /// 状态 1-进行中 2-通过 10-异常 4-终止.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int Status { get; set; } = 0;
}
