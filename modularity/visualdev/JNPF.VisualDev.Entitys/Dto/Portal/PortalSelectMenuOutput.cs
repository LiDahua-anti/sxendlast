using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 门户侧边栏输出.
/// </summary>
[SuppressSniffer]
public class PortalSelectMenuOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }
}
