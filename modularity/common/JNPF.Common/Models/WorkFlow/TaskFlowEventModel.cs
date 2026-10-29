namespace JNPF.Common.Models.WorkFlow;

/// <summary>
/// 任务流程事件模型.
/// </summary>
public class TaskFlowEventModel
{
    /// <summary>
    /// 用户ID.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 租户ID.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// 模版ID.
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 任务参数.
    /// </summary>
    public string ParamterJson { get; set; }

    /// <summary>
    /// 触发节点类型.
    /// </summary>]
    public string TriggerType { get; set; }

    /// <summary>
    /// 事件触发动作(1.新增 2.修改 3.删除).
    /// 审批事件触发动作(1.同意 2.拒绝 3.退回 4.办理).
    /// </summary>]
    public int ActionType { get; set; }

    /// <summary>
    /// 触发初始数据.
    /// </summary>
    public List<Dictionary<string, object>> taskFlowData { get; set; } = new List<Dictionary<string, object>>();

    /// <summary>
    /// 节点数据源.
    /// </summary>
    public Dictionary<string, object> dataSource { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 已修改数据字段.
    /// </summary>
    public List<string> upDateFieldList { get; set; } = new List<string>();

    /// <summary>
    /// 异常节点.
    /// </summary>
    public List<string> errorNodeCodeList { get; set; } = new List<string>();

    /// <summary>
    /// 可触发的删除节点.
    /// </summary>
    public List<string> delNodeIdList { get; set; } = new List<string>();

    /// <summary>
    /// 是否重试.
    /// </summary>]
    public bool isRetry { get; set; }

    public DateTime? parentTime { get; set; }
}
