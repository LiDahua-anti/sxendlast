using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 单据规则配置模型.
/// </summary>
[SuppressSniffer]
public class RuleConfigModel
{
    /// <summary>
    /// 前缀.
    /// </summary>
    public List<FieldConfig> prefixList { get; set; }

    /// <summary>
    /// 方式设置.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 时间格式.
    /// </summary>
    public string dateFormat { get; set; }

    /// <summary>
    /// 起始值位数.
    /// </summary>
    public int? digit { get; set; }

    /// <summary>
    /// 起始值.
    /// </summary>
    public string startNumber { get; set; }

    /// <summary>
    /// 随机数位数.
    /// </summary>
    public int? randomDigit { get; set; }

    /// <summary>
    /// 随机数类型（1：数字，2：字母+数字）.
    /// </summary>
    public int? randomType { get; set; }

    /// <summary>
    /// 后缀.
    /// </summary>
    public List<FieldConfig> suffixList { get; set; }

}

public class FieldConfig
{
    /// <summary>
    /// 来源（1：字段，2：自定义）.
    /// </summary>
    public int? sourceType { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public string relationField { get; set; }
}