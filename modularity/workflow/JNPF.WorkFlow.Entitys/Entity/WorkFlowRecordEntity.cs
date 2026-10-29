using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程经办记录.
/// </summary>
[SugarTable("WORKFLOW_RECORD")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowRecordEntity : CLDEntityBase
{
    /// <summary>
    /// 任务主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 经办主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OPERATOR_ID")]
    public string OperatorId { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_CODE")]
    public string? NodeCode { get; set; }

    /// <summary>
    /// 节点ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_ID")]
    public string? NodeId { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_NODE_NAME")]
    public string? NodeName { get; set; }

    /// <summary>
    /// 处理类型：参考WorkFlowRecordTypeEnum.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_TYPE")]
    public int HandleType { get; set; } = 0;

    /// <summary>
    /// 经办人员.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_ID")]
    public string? HandleId { get; set; }

    /// <summary>
    /// 经办时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_TIME")]
    public DateTime? HandleTime { get; set; }

    /// <summary>
    /// 经办理由.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_OPINION")]
    public string? HandleOpinion { get; set; }

    /// <summary>
    /// 流转操作人.
    /// </summary>
    [SugarColumn(ColumnName = "F_HANDLE_USER_ID")]
    public string? HandleUserId { get; set; }

    /// <summary>
    /// 电子签名.
    /// </summary>
    [SugarColumn(ColumnName = "F_SIGN_IMG")]
    public string? SignImg { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILE_LIST")]
    public string? FileList { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public int Status { get; set; }

    /// <summary>
    /// 拓展字段.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXPAND_FIELD")]
    public string? ExpandField { get; set; }
}
