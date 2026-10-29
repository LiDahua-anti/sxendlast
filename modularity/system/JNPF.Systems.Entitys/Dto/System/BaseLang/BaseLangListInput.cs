using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.BaseLang;

/// <summary>
/// 翻译管理列表输入.
/// </summary>
[SuppressSniffer]
public class BaseLangListInput : PageInputBase
{
    /// <summary>
    /// 类型：0-客户端，1-服务端.
    /// </summary>
    public int? type { get; set; }
}