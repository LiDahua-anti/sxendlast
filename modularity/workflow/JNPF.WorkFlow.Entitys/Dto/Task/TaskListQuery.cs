using JNPF.AspNetCore;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.WorkFlow.Entitys.Dto.Task;

[SuppressSniffer]
public class TaskListQuery : PageInputBase
{
    /// <summary>
    /// 发起人员.
    /// </summary>
    public string creatorUserId { get; set; }

    /// <summary>
    /// 所属分类.
    /// </summary>
    public string? flowCategory { get; set; }

    /// <summary>
    /// 所属流程.
    /// </summary>
    public string? templateId { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    [ModelBinder(BinderType = typeof(TimestampToDateTimeModelBinder))]
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    [ModelBinder(BinderType = typeof(TimestampToDateTimeModelBinder))]
    public DateTime? endTime { get; set; }

    /// <summary>
    /// 流程状态.
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? flowUrgent { get; set; }

    /// <summary>
    /// 是否委托发起.
    /// </summary>
    public string delegateType { get; set; }

    /// <summary>
    /// 是否归档.
    /// </summary>
    public int? isFile { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string? systemId { get; set; }
}
