using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.OnineLog;

/// <summary>
/// 数据日志列表查询输入.
/// </summary>
[SuppressSniffer]
public class OnlineLogListInput : PageInputBase
{
    /// <summary>
    /// 模板id.
    /// </summary>
    public string modelId { get; set; }

    /// <summary>
    /// 数据id.
    /// </summary>
    public string dataId { get; set; }
}