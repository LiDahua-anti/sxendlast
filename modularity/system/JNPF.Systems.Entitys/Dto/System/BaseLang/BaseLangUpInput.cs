using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.BaseLang;

/// <summary>
/// 翻译管理修改输入.
/// </summary>
[SuppressSniffer]
public class BaseLangUpInput : BaseLangCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}