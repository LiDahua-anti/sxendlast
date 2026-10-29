using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SysConfig;

/// <summary>
/// 同步配置输出.
/// </summary>
[SuppressSniffer]
public class GetSocialsOutput : SaveSocialsInput
{
    public bool dingDisabled { get; set; }
    public bool qyhDisabled { get; set; }
}
