using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualPersonal;

/// <summary>
/// 列表个性视图列表查询输入.
/// </summary>
[SuppressSniffer]
public class VisualPersonalListInput
{
    /// <summary>
    /// 菜单id.
    /// </summary>
    public string menuId { get; set; }
}