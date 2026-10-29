using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.VisualKit;

/// <summary>
/// 表单套件修改输入.
/// </summary>
[SuppressSniffer]
public class VisualKitUpInput : VisualKitCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}