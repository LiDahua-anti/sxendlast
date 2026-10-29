namespace JNPF.WorkFlow.Entitys.Dto.Template;

public class TemplateInfoOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

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
    /// 流程分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string? icon { get; set; }

    /// <summary>
    /// 图标背景.
    /// </summary>
    public string? iconBackground { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 流程权限（1全局  2权限）.
    /// </summary>
    public int? visibleType { get; set; }

    /// <summary>
    /// 流程显示类型（0-全局 1-流程 2-菜单）.
    /// </summary>
    public int? showType { get; set; }

}
