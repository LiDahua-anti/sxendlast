using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.System;

/// <summary>
/// 系统偏好配置输入.
/// </summary>
[SuppressSniffer]
public class SystemPreferenceSaveInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 偏好配置.
    /// </summary>
    public string preferenceJson { get; set; }
}
