using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.VisualKit;

/// <summary>
/// 表单套件列表输入.
/// </summary>
[SuppressSniffer]
public class VisualKitListInput : PageInputBase
{
    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 状态(1-可用,0-不可用).
    /// </summary>
    public int? enabledMark { get; set; }
}
