namespace JNPF.Extras.Thirdparty.AI.Internal;

/// <summary>
/// AI 请求参数.
/// </summary>
public class AIParameter
{
    /// <summary>
    /// AI 模型.
    /// </summary>
    public string model { get; set; }

    /// <summary>
    /// 请求内容.
    /// </summary>
    public List<MessageModel> messages { get; set; }
}

/// <summary>
/// 请求内容.
/// </summary>
public class MessageModel
{
    /// <summary>
    /// 角色.
    /// </summary>
    public string role { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public string content { get; set; }
}