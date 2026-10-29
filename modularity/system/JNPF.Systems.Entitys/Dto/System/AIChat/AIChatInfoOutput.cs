using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.AIChat;

/// <summary>
/// AI 对话信息输出.
/// </summary>
[SuppressSniffer]
public class AIChatInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 问题内容.
    /// </summary>
    public string questionText { get; set; }

    /// <summary>
    /// 会话内容.
    /// </summary>
    public string content { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUserId { get; set; }
}