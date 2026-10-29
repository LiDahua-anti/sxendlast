using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Organize;

/// <summary>
/// 获取部门列表输入.
/// </summary>
[SuppressSniffer]
public class OrganizeCurrInput : PageInputBase
{
    /// <summary>
    /// 当前节点组织id.
    /// </summary>
    public string? currOrgId { get; set; }
}
