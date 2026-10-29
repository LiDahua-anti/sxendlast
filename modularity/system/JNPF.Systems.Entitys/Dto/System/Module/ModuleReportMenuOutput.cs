using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 报表已发布菜单名称输出.
/// </summary>
[SuppressSniffer]
public class ModuleReportMenuOutput
{
    /// <summary>
    /// 已发布Pc菜单名称.
    /// </summary>
    public string pcNames { get; set; }

    /// <summary>
    /// 已发布App菜单名称.
    /// </summary>
    public string appNames { get; set; }
}
