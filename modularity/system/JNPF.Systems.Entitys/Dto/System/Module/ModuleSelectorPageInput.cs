using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 获取指定类型的菜单列表.
/// </summary>
[SuppressSniffer]
public class ModuleSelectorPageInput
{
    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }
}