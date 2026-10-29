using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualPersonal;

/// <summary>
/// 列表个性视图修改输入.
/// </summary>
[SuppressSniffer]
public class VisualPersonalUpInput : VisualPersonalCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}