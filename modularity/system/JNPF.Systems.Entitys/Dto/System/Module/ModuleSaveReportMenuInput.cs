using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 报表发布菜单输入.
/// </summary>
[SuppressSniffer]
public class ModuleSaveReportMenuInput
{
    /// <summary>
    /// 报表id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 同步至pc端菜单（0：不同步，1：同步）.
    /// </summary>
    public int? pc { get; set; }

    /// <summary>
    /// 同步至app端菜单（0：不同步，1：同步）.
    /// </summary>
    public int? app { get; set; }

    /// <summary>
    /// 同步菜单pc上级id.
    /// </summary>
    public List<string> pcModuleParentId { get; set; }

    /// <summary>
    /// 同步菜单app上级id.
    /// </summary>
    public List<string> appModuleParentId { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    public string platformRelease { get; set; }
}
