using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.System;

/// <summary>
/// 系统列表输出.
/// </summary>
[SuppressSniffer]
public class SystemListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 系统名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 系统编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 背景颜色.
    /// </summary>
    public string backgroundColor { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 所属用户.
    /// </summary>
    public string userId { get; set; }

    /// <summary>
    /// 是否有修改权限.
    /// </summary>
    public int hasUpdate { get; set; }

    /// <summary>
    /// 是否有删除权限.
    /// </summary>
    public int hasDelete { get; set; }
}
