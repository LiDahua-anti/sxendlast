using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印版本列表输出.
/// </summary>
[SuppressSniffer]
public class PrintVersionListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? state { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public int? version { get; set; }

    /// <summary>
    /// 打印模板json.
    /// </summary>
    public string printTemplate { get; set; }
}