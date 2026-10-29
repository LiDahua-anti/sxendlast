using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.System;

/// <summary>
/// 系统功能创建输入.
/// </summary>
[SuppressSniffer]
public class SystemCrInput
{
    /// <summary>
    /// id.
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
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 偏好配置.
    /// </summary>
    public string preferenceJson { get; set; }

    /// <summary>
    /// 扩展字段.
    /// </summary>
    public string propertyJson { get; set; }

    /// <summary>
    /// 导航图标.
    /// </summary>
    public string navigationIcon { get; set; }

    /// <summary>
    /// logo图标.
    /// </summary>
    public string workLogoIcon { get; set; }

    /// <summary>
    /// 背景颜色.
    /// </summary>
    public string backgroundColor { get; set; }

    /// <summary>
    /// pc门户.
    /// </summary>
    public string portalId { get; set; }

    /// <summary>
    /// app门户.
    /// </summary>
    public string appPortalId { get; set; }
}
