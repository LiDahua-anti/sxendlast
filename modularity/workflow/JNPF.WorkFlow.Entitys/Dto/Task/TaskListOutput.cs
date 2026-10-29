using JNPF.JsonSerialization;
using System.Text.Json.Serialization;

namespace JNPF.WorkFlow.Entitys.Dto.Task;

public class TaskListOutput
{
    /// <summary>
    /// 版本.
    /// </summary>
    public string? flowVersion { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string? creatorUser { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string? creatorUserId { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 审批节点.
    /// </summary>
    public string? currentNodeName { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? flowUrgent { get; set; }

    /// <summary>
    /// 流程分类.
    /// </summary>
    public string? flowCategory { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? flowName { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int status { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    [JsonConverter(typeof(NewtonsoftDateTimeJsonConverter))]
    public DateTime? startTime { get; set; }

    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [JsonConverter(typeof(NewtonsoftDateTimeJsonConverter))]
    public DateTime? endTime { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 流程主表id.
    /// </summary>
    public string? templateId { get; set; }

    /// <summary>
    /// 委托发起人.
    /// </summary>
    public string? delegateUser { get; set; }

    /// <summary>
    /// 归档（null：空 0：否，1：是）.
    /// </summary>
    public string? isFile { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string? systemId { get; set; }

    /// <summary>
    /// 所属应用.
    /// </summary>
    public string? systemName { get; set; }
}
