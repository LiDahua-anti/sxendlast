using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Permission.Standing;

/// <summary>
/// 身份修改输入.
/// </summary>
[SuppressSniffer]
public class StandingUpInput : StandingCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}
