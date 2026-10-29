using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.System;

/// <summary>
/// 系统开发人员输出.
/// </summary>
[SuppressSniffer]
public class SystemDevUserOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string realName { get; set; }

    /// <summary>
    /// 账号.
    /// </summary>
    public string account { get; set; }

    /// <summary>
    /// 姓名账号.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 头像.
    /// </summary>
    public string headIcon { get; set; }
}
