using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Document;

/// <summary>
/// 共享用户调整输入.
/// </summary>
[SuppressSniffer]
public class DocumentShareAdjustmentInput
{
    /// <summary>
    /// 共享用户.
    /// </summary>
    public List<string> userIds { get; set; }
}