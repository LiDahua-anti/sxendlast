using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SynThirdInfo;

/// <summary>
/// 第三方同步未成功输出.
/// </summary>
[SuppressSniffer]
public class SynThirdHandleNumOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 本系统对象名称.
    /// </summary>
    public string systemObjectName { get; set; }

    /// <summary>
    /// 第三方系统对象名称.
    /// </summary>
    public string thirdName { get; set; }

    /// <summary>
    /// 异常描述.
    /// </summary>
    public string description { get; set; }
}