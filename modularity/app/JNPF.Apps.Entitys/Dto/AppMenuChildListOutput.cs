using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Apps.Entitys.Dto;

/// <summary>
/// 获取菜单的子菜单输出.
/// </summary>
[SuppressSniffer]
public class AppMenuChildListOutput : TreeModel
{
    /// <summary>
    /// 类型(1-目录 2-页面 3-功能 4-字典 5-报表 6-大屏 7-外链 8-门户).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 功能名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 功能编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 功能地址.
    /// </summary>
    public string urlAddress { get; set; }

    /// <summary>
    /// 扩展属性.
    /// </summary>
    public string propertyJson { get; set; }

    /// <summary>
    /// 菜单图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 路由地址.
    /// </summary>
    public string pageAddress { get; set; }
}