using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 已发布菜单名称输出.
/// </summary>
[SuppressSniffer]
public class VisualDevReleaseMenuOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 是否发布.
    /// </summary>
    public int? isRelease { get; set; }

    /// <summary>
    /// Pc是否发布.
    /// </summary>
    public int pcIsRelease { get; set; }

    /// <summary>
    /// App是否发布.
    /// </summary>
    public int appIsRelease { get; set; }

    /// <summary>
    /// 已发布Pc菜单名称.
    /// </summary>
    public string pcReleaseName;

    /// <summary>
    /// 已发布App菜单名称.
    /// </summary>
    public string appReleaseName;

    /// <summary>
    /// 上次选择的平台发布.
    /// </summary>
    public string platformRelease;
}
