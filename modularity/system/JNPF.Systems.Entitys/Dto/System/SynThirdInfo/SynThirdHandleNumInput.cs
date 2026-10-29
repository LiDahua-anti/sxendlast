using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SynThirdInfo;

/// <summary>
/// 第三方同步未成功输入.
/// </summary>
[SuppressSniffer]
public class SynThirdHandleNumInput : PageInputBase
{
    /// <summary>
    /// 第三方类型.
    /// </summary>
    public int? thirdType { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 结果类型.
    /// </summary>
    public int? resultType { get; set; }
}