using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 门户下拉框输入.
/// </summary>
[SuppressSniffer]
public class PortalSelectInput
{
    /// <summary>
    /// 平台.
    /// </summary>
    public string platform { get; set; }
}
