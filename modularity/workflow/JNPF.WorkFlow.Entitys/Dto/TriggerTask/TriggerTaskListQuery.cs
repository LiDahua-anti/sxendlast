using JNPF.AspNetCore;
using JNPF.Common.Filter;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.WorkFlow.Entitys.Dto.TriggerTask;

public class TriggerTaskListQuery : PageInputBase
{
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
    /// 系统id.
    /// </summary>
    public string? systemId { get; set; }
}
