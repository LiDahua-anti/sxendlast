using JNPF.DependencyInjection;

namespace JNPF.Common.Models.Module;

/// <summary>
/// 菜单的扩展属性模型.
/// </summary>
[SuppressSniffer]
public class ModulePropertyModel
{
    /// <summary>
    /// 背景颜色.
    /// </summary>
    public string iconBackgroundColor { get; set; } = string.Empty;

    /// <summary>
    /// 是否树形.
    /// </summary>
    public int isTree { get; set; }

    /// <summary>
    /// 关联id.
    /// </summary>
    public string moduleId { get; set; }

    /// <summary>
    /// 表单类型（在线开发）.
    /// </summary>
    public int? webType { get; set; }
}
