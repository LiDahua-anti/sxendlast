using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.MenuData;

[SuppressSniffer]
public class ModuleDataListOutput
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
    /// 菜单编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 菜单图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 菜单类型(1-目录，2-页面).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 链接地址.
    /// </summary>
    public string urlAddress { get; set; }

    /// <summary>
    /// 链接方式(_self,_blank).
    /// </summary>
    public string linkTarget { get; set; }

    /// <summary>
    /// 菜单分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 扩展信息.
    /// </summary>
    public string propertyJson { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 页面地址.
    /// </summary>
    public string pageAddress { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemCode { get; set; }

    /// <summary>
    /// 是否后台.
    /// </summary>
    public int isBackend { get; set; }
}