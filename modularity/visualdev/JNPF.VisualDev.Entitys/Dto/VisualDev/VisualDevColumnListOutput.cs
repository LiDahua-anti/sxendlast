using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 字段列表模型.
/// </summary>
[SuppressSniffer]
public class VisualDevColumnListOutput
{
    public string fieldId { get; set; }

    public string fieldName { get; set; }

    public string jnpfKey { get; set; }

    public string tableName { get; set; }

    public bool required { get; set; }

    public bool multiple { get; set; }
}