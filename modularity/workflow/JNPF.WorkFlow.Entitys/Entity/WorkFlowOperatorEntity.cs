using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程经办.
/// </summary>
[SugarTable("WORKFLOW_OPERATOR")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowOperatorEntity : CLDEntityBase
{
    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 父级主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string? ParentId { get; set; } = "0";

    /// <summary>
    /// flowable节点主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_ID")]
    public string? NodeId { get; set; }

    /// <summary>
    /// 引擎类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENGINE_TYPE")]
    public int? EngineType { get; set; }

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
    /// 是否完成【0-未处理、1-已审核】.
    /// </summary>
    [SugarColumn(ColumnName = "F_COMPLETION")]
    public int? Completion { get; set; }

    /// <summary>
    /// 经办人主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_ID")]
    public string? HandleId { get; set; }

    /// <summary>
    /// 全部处理人.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_ALL")]
    public string? HandleAll { get; set; }

    /// <summary>
    /// 处理状态：【0-拒绝、1-同意】.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_STATUS")]
    public int? HandleStatus { get; set; }

    /// <summary>
    /// 处理时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_TIME")]
    public DateTime? HandleTime { get; set; }

    /// <summary>
    /// 开始处理时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_START_HANDLE_TIME")]
    public DateTime? StartHandleTime { get; set; }

    /// <summary>
    /// 签收时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_SIGN_TIME")]
    public DateTime? SignTime { get; set; }

    /// <summary>
    /// 处理参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_PARAMETER")]
    public string? HandleParameter { get; set; }

    /// <summary>
    /// 状态：参考WorkFlowOperatorStatusEnum.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int? Status { get; set; }

    /// <summary>
    /// 草稿数据.
    /// </summary>
    [SugarColumn(ColumnName = "F_DRAFT_DATA")]
    public string? DraftData { get; set; }

    /// <summary>
    /// 截止时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_DUEDATE")]
    public DateTime? Duedate { get; set; }

    /// <summary>
    /// 是否办理节点(0否 1是).
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_PROCESSING")]
    public int? IsProcessing { get; set; }
}
