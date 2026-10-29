using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.OnineLog;

/// <summary>
/// 数据日志列表查询输输出.
/// </summary>
[SuppressSniffer]
public class OnlineLogListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 日志类型：0-新建，1-编辑.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 日志内容.
    /// </summary>
    public string dataLog { get; set; }

    /// <summary>
    /// 用户头像.
    /// </summary>
    public string headIcon { get; set; }

    /// <summary>
    /// 创建用户.
    /// </summary>
    public string creatorUserName { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public string creatorTime { get; set; }
}