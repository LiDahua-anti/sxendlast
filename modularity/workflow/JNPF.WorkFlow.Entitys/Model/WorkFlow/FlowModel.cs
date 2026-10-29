using JNPF.WorkFlow.Entitys.Model.Conifg;

namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

public class FlowModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// id.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string? templateId { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string? flowableId { get; set; }

    /// <summary>
    /// 可见范围.
    /// </summary>
    public int? visibleType { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public string? version { get; set; }

    /// <summary>
    /// 流程JOSN包.
    /// </summary>
    public string? flowXml { get; set; }

    /// <summary>
    /// 流程分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 流程编号.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 流程类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 流程设置.
    /// </summary>
    public string? configuration { get; set; }

    /// <summary>
    /// 流程设置.
    /// </summary>
    public FlowConfig? flowConfig { get; set; }

    /// <summary>
    /// 节点列表.
    /// </summary>
    public Dictionary<string, object> flowNodes { get; set; }

    /// <summary>
    /// 流程状态.
    /// </summary>
    public int? status { get; set; }
}
