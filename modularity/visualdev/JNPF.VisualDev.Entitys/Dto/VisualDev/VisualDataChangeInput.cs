using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 在线开发详情输入.
/// </summary>
[SuppressSniffer]
public class VisualDataChangeInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public object id { get; set; }

    /// <summary>
    /// 菜单id.
    /// </summary>
    public string menuId { get; set; }

    /// <summary>
    /// 关联表单字段.
    /// </summary>
    public string propsValue { get; set; }
}
