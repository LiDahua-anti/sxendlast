using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.BaseLang;

/// <summary>
/// 翻译管理创建输入.
/// </summary>
[SuppressSniffer]
public class BaseLangCrInput
{
    /// <summary>
    /// 翻译标记.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 类型：0-客户端，1-服务端.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 语种，内容.
    /// </summary>
    public Dictionary<string, string> map { get; set; }
}