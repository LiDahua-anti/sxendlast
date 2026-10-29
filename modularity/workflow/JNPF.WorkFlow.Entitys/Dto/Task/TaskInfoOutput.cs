using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;

namespace JNPF.WorkFlow.Entitys.Dto.Task;

[SuppressSniffer]
public class TaskInfoOutput
{
    /// <summary>
    /// 表单数据.
    /// </summary>
    public object formData { get; set; }

    /// <summary>
    /// 审批草稿数据.
    /// </summary>
    public object draftData { get; set; }

    /// <summary>
    /// 当前节点属性.
    /// </summary>
    public object nodeProperties { get; set; }

    /// <summary>
    /// 表单详情.
    /// </summary>
    public FormModel formInfo { get; set; }

    /// <summary>
    /// 流程详情.
    /// </summary>
    public FlowModel flowInfo { get; set; }

    /// <summary>
    /// 流程任务.
    /// </summary>
    public TaskModel? taskInfo { get; set; }

    /// <summary>
    /// 流程任务节点.
    /// </summary>
    public List<NodeModel>? nodeList { get; set; } = new List<NodeModel>() { };

    /// <summary>
    /// 流程任务经办记录.
    /// </summary>
    public List<RecordModel>? recordList { get; set; } = new List<RecordModel>() { };

    /// <summary>
    /// 进度.
    /// </summary>
    public List<ProgressModel>? progressList { get; set; } = new List<ProgressModel>() { };

    /// <summary>
    /// 当前节点权限.
    /// </summary>
    public List<object> formOperates { get; set; } = new List<object>();

    /// <summary>
    /// 按钮权限.
    /// </summary>
    public BtnProperties btnInfo { get; set; } = new BtnProperties();

    /// <summary>
    /// 走过的线.
    /// </summary>
    public List<string> lineKeyList { get; set; } = new List<string>();
}
