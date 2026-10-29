using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

/// <summary>
/// 页签表单配置输入.
/// </summary>
[SuppressSniffer]
public class VisualDevModelConfigInput
{
    public string menuId { get; set; }

    public string systemId { get; set; }
}
