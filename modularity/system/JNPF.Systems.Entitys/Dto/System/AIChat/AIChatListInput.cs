using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.AIChat;

/// <summary>
/// AI 对话列表输出.
/// </summary>
[SuppressSniffer]
public class AIChatListInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUserId { get; set; }
}