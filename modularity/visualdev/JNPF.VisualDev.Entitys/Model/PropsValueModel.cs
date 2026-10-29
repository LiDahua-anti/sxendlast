using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Model;

/// <summary>
/// 存储字段模型.
/// </summary>
[SuppressSniffer]
public class PropsValueModel
{
    public string field { get; set; }

    public string fieldName { get; set; }
}
