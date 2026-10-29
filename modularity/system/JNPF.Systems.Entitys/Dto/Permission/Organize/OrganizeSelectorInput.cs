using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Organize;

/// <summary>
/// 获取组织下拉输入.
/// </summary>
[SuppressSniffer]
public class OrganizeSelectorInput : PageInputBase
{
    /// <summary>
    /// 父级id.
    /// </summary>
    public string parentId { get; set; }
}
