using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Document;

/// <summary>
/// 批量输入.
/// </summary>
[SuppressSniffer]
public class DocumentBatchInput
{
    /// <summary>
    /// 主键id.
    /// </summary>
    public List<string> ids { get; set; }
}