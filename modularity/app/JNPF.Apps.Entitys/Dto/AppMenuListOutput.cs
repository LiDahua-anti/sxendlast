using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Apps.Entitys.Dto;

/// <summary>
/// app菜单输出.
/// </summary>
[SuppressSniffer]
public class AppMenuListOutput : TreeModel
{
    /// <summary>
    /// 菜单名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 链接地址.
    /// </summary>
    public string urlAddress { get; set; }

    /// <summary>
    /// 菜单类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 扩展字段.
    /// </summary>
    public string propertyJson { get; set; }

    /// <summary>
    /// 路由地址.
    /// </summary>
    public string pageAddress { get; set; }
}