using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 门户下拉框输出.
/// </summary>
[SuppressSniffer]
public class PortalSelectOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }
}
