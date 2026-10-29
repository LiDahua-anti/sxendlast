using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.AIChat;

/// <summary>
/// AI 对话保存输入.
/// </summary>
[SuppressSniffer]
public class AIChatSaveInput
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
    /// 数据.
    /// </summary>
    public List<Data> data { get; set; }
}

public class Data
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
    /// 名称.
    /// </summary>
    public int? type { get; set; }
}