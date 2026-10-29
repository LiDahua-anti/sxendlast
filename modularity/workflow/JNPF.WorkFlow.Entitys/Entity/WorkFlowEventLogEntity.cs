using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程外部事件.
/// </summary>
[SugarTable("WORKFLOW_EVENT_LOG")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowEventLogEntity : CLDEntityBase
{
    /// <summary>
    /// 任务id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 节点id.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_ID")]
    public string? NodeId { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_NAME")]
    public string? NodeName { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }

    /// <summary>
    /// 上一节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_UP_NODE")]
    public string? UpCode { get; set; }

    /// <summary>
    /// 事件类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public string? Type { get; set; }

    /// <summary>
    /// 接口id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTERFACE_ID")]
    public string? InterfaceId { get; set; }

    /// <summary>
    /// 请求参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA")]
    public string? Data { get; set; }

    /// <summary>
    /// 执行结果.
    /// </summary>
    [SugarColumn(ColumnName = "F_RESULT")]
    public string? result { get; set; }

    /// <summary>
    /// 执行状态.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int? Status { get; set; }
}
