namespace JNPF.WorkFlow.Entitys.Dto.Template;

public class TemplateListOutput
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
    /// 可见范围.
    /// </summary>
    public int? visibleType { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string? creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改人.
    /// </summary>
    public string? lastModifyUser { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 标识.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string? icon { get; set; }

    /// <summary>
    /// 图标背景.
    /// </summary>
    public string? iconBackground { get; set; }

    /// <summary>
    /// id.
    /// </summary>
    public string? templateId { get; set; }

    /// <summary>
    /// 是否常用.
    /// </summary>
    public bool? isCommonFlow { get; set; }

    /// <summary>
    /// 状态(0.未上架,1.上架,2.下架-继续审批，3.下架-隐藏审批).
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 是否系统表单（0-否 1-是）.
    /// </summary>
    public int isQuote { get; set; }

}
