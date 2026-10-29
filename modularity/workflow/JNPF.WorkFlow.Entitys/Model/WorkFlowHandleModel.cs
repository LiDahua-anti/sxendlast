using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Model;

[SuppressSniffer]
public class WorkFlowHandleModel : PageInputBase
{
    /// <summary>
    /// 审批类型（0-拒绝 1-同意）.
    /// </summary>
    public int handleStatus { get; set; } = 1;

    /// <summary>
    /// 意见.
    /// </summary>
    public string? handleOpinion { get; set; }

    /// <summary>
    /// 电子签名.
    /// </summary>
    public string? signImg { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    public List<object> fileList { get; set; } = new List<object>();

    /// <summary>
    /// 表单数据.
    /// </summary>
    public object? formData { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 自定义抄送人.
    /// </summary>
    public string? copyIds { get; set; }

    /// <summary>
    /// 处理人.
    /// </summary>
    public string? handleIds { get; set; }

    /// <summary>
    /// 候选人.
    /// </summary>
    public Dictionary<string, List<string>>? candidateList { get; set; }

    /// <summary>
    /// 异常处理人.
    /// </summary>
    public Dictionary<string, List<string>> errorRuleUserList { get; set; } = new Dictionary<string, List<string>>();

    /// <summary>
    /// 指定节点.
    /// </summary>
    public string? nodeCode { get; set; }

    /// <summary>
    /// 加签参数.
    /// </summary>
    public AddSignItem? addSignParameter { get; set; }

    /// <summary>
    /// 批量id.
    /// </summary>
    public List<string> ids { get; set; } = new List<string>();

    /// <summary>
    /// 批量类型.
    /// </summary>
    public int batchType { get; set; }

    /// <summary>
    /// 退回节点.
    /// </summary>
    public string backNodeCode { get; set; }

    /// <summary>
    /// 退回类型.
    /// </summary>
    public string backType { get; set; }

    /// <summary>
    /// false(暂停所有流程包含子流程) true(暂停所有流程包含子流程(不包含异步子流程)).
    /// </summary>
    public bool pause { get; set; }

    /// <summary>
    /// 设置默认电子签名.
    /// </summary>
    public bool useSignNext { get; set; }

    /// <summary>
    /// 电子签名id.
    /// </summary>
    public string? signId { get; set; }

    /// <summary>
    /// 任务id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 拓展字段.
    /// </summary>
    public List<object>? approvalField { get; set; } = new List<object>();

    /// <summary>
    /// 委托发起人.
    /// </summary>
    public string delegateUser { get; set; }

    /// <summary>
    /// 选择分支.
    /// </summary>
    public List<string> branchList { get; set; } = new List<string>();
}
