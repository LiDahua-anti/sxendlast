using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 验证规则模型.
/// </summary>
[SuppressSniffer]
public class RegListModel
{
    /// <summary>
    /// 正则表达式.
    /// </summary>
    public string pattern { get; set; }

    /// <summary>
    /// 错误提示.
    /// </summary>
    public string message { get; set; }

    /// <summary>
    /// 错误提示 I18nCode.
    /// </summary>
    public string messageI18nCode { get; set; }
}