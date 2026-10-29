using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.VisualKit;

/// <summary>
/// 表单套件列表下拉输出.
/// </summary>
[SuppressSniffer]
public class VisualKitSelectorOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 模板.
    /// </summary>
    public string formData { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? SortCode { get; set; }
}
