using JNPF.AspNetCore;
using JNPF.Common.Filter;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.WorkFlow.Entitys.Dto.Template;

public class TemplateListQuery : CommonInput

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
    /// 所属流程.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 流程类型.
    /// </summary>
    public string? type { get; set; }

    /// <summary>
    /// 权限过滤 0-否 1-是.
    /// </summary>
    public int isAuthority { get; set; } = 1;

    /// <summary>
    /// 是否委托代理列表.（1-委托代理列表）.
    /// </summary>
    public int isDelegate { get; set; }

    /// <summary>
    /// 是否发起列表.（0-否 1-是）.
    /// </summary>
    public int isLaunch { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string? systemId { get; set; }
    /// 是否验证系统表单.（0-否 1-是）.
    /// </summary>
    public int isSystemFrom { get; set; }
}
