using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Permission.Action;

/// <summary>
/// 动作管理修改输入.
/// </summary>
[SuppressSniffer]
public class ActionUpInput : ActionCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}
