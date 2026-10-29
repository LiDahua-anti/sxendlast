using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

[SuppressSniffer]
public class TaskModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 任务名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? flowUrgent { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? flowName { get; set; }

    /// <summary>
    /// 流程类型.
    /// </summary>
    public int? flowType { get; set; }

    /// <summary>
    /// 流程分类.
    /// </summary>
    public string? flowCategory { get; set; }

    /// <summary>
    /// 发起时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 发起人.
    /// </summary>
    public string? creatorUser { get; set; }

    /// <summary>
    /// 当前节点名称.
    /// </summary>
    public string? currentNodeName { get; set; }

    /// <summary>
    /// 当前节点编码.
    /// </summary>
    public string? currentNodeCode { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 发起人头像.
    /// </summary>
    public string headIcon { get; set; }

    /// <summary>
    /// 是否撤销任务.
    /// </summary>
    public bool isRevokeTask { get; set; }
}
