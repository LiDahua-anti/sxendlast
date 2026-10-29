namespace JNPF.Extras.Thirdparty.AI.Internal;

/// <summary>
/// AI 返回模型.
/// </summary>
public class AIOutputModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 模型.
    /// </summary>
    public string model { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public List<Choice> choices { get; set; }
}

public class Choice
{
    public Message message { get; set; }
}

public class Message
{
    public string role { get; set; }

    public string content { get; set; }
}
