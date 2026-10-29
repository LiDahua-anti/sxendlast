using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

[SuppressSniffer]
public class ProgressModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 节点id.
    /// </summary>
    public string? nodeId { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string? nodeCode { get; set; }

    /// <summary>
    /// 节点类型.
    /// </summary>
    public string? nodeType { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    public string? nodeName { get; set; }

    /// <summary>
    /// 节点状态 1-已提交 2-已通过 3-已拒绝 4-审批中 5-已退回 6-已撤回 7-等待中 8-办理中.
    /// </summary>
    public int? nodeStatus { get; set; }

    /// <summary>
    /// 审批类型（0：或签 1：会签 2：依次审批）.
    /// </summary>
    public int? counterSign { get; set; }

    /// <summary>
    /// 审批人.
    /// </summary>
    public List<UserItem> approver { get; set; }

    /// <summary>
    /// 审批人数.
    /// </summary>
    public int? approverCount { get; set; }

    /// <summary>
    /// 审批人数.
    /// </summary>
    public bool showTaskFlow { get; set; }

    /// <summary>
    /// 外部节点请求结果.
    /// </summary>
    public bool outSideStatus { get; set; }

    /// <summary>
    /// 错误提示.
    /// </summary>
    public string errorTip { get; set; }

    /// <summary>
    /// 错误数据.
    /// </summary>
    public string errorData { get; set; }

    /// <summary>
    /// 外部节点请求可否重试.
    /// </summary>
    public bool isRetry { get; set; }

    /// <summary>
    /// 审批人员类型.
    /// </summary>
    public int assigneeType { get; set; }
}
