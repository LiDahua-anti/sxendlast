using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.Template;
[SuppressSniffer]
public class TemplateTreeOutput : TreeModel
{
    /// <summary>
    /// 流程编号.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? fullName { get; set; }

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
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }
}
