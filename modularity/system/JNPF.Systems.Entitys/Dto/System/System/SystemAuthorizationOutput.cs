using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.System;

/// <summary>
/// 系统应用权限输出.
/// </summary>
[SuppressSniffer]
public class SystemAuthorizationOutput
{
    /// <summary>
    /// 创建人id.
    /// </summary>
    public string createUserId { get; set; }

    /// <summary>
    /// 创建人名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 授权成员.
    /// </summary>
    public List<string> devUsers { get; set; }

    /// <summary>
    /// 是否全部开发成员.
    /// </summary>
    public int isAllDevUser { get; set; }
}
