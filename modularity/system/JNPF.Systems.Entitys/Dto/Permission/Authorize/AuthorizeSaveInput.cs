using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 权限数据保存输入.
/// </summary>
[SuppressSniffer]
public class AuthorizeSaveInput
{
    /// <summary>
    /// 授权对象类型.
    /// </summary>
    public string objectType { get; set; }

    /// <summary>
    /// 授权的门户Id.
    /// </summary>
    public List<string> ids { get; set; }
}