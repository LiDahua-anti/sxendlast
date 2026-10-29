using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Document;

/// <summary>
/// 共享文件.
/// </summary>
[SuppressSniffer]
public class DocumentActionsShareInput : DocumentBatchInput
{
    /// <summary>
    /// 共享用户.
    /// </summary>
    public List<string> userIds { get; set; }
}