using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 获取菜单的表单列表输入.
/// </summary>
[SuppressSniffer]
public class ModuleSelectorFormInput : PageInputBase
{
    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemId { get; set; }
}