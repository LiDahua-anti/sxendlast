using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 获取指定类型的菜单列表.
/// </summary>
[SuppressSniffer]
public class ModuleSelectorPageOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 菜单名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 扩展属性.
    /// </summary>
    public string propertyJson { get; set; }
}