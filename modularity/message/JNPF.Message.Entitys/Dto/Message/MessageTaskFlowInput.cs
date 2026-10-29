using JNPF.Common.Dtos.Message;
using JNPF.DependencyInjection;

namespace JNPF.Message.Entitys.Dto.Message;

public class MessageTaskFlowInput
{
    /// <summary>
    /// 信息配置.
    /// </summary>
    public MessageConfig mesConfig { get; set; }

    /// <summary>
    /// 消息编码.
    /// </summary>
    public string msgEnCode { get; set; }

    /// <summary>
    /// 默认标题.
    /// </summary>
    public string defaultTitle { get; set; }

    /// <summary>
    /// 通知人类型.
    /// </summary>
    public List<int> msgUserType { get; set; }

    /// <summary>
    /// 通知人.
    /// </summary>
    public List<string> msgUserIds { get; set; }

    /// <summary>
    /// 创建人ID.
    /// </summary>
    public string creatorUserId { get; set; }

    /// <summary>
    /// 模板配置json.
    /// </summary>
    public List<MessageSendModel>? templateJson { get; set; }

    /// <summary>
    /// 数据字符串.
    /// </summary>
    public string data { get; set; }
}

/// <summary>
/// 消息配置.
/// </summary>
[SuppressSniffer]
public class MessageConfig
{
    /// <summary>
    /// 通知类型
    /// 3-默认,1-自定义,0-关闭.
    /// </summary>
    public int on { get; set; }

    /// <summary>
    /// 消息id.
    /// </summary>
    public string msgId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string msgName { get; set; }

    /// <summary>
    /// 模板json.
    /// </summary>
    public List<MessageSendModel> templateJson { get; set; }
}
