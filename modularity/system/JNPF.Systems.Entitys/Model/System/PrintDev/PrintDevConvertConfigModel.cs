using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.PrintDev;

/// <summary>
/// 打印模板转换配置模型.
/// </summary>
[SuppressSniffer]
public class PrintDevConvertConfigModel
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 配置.
    /// </summary>
    public ConvertConfig config { get; set; }
}

public class ConvertConfig
{
    /// <summary>
    /// 数据类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 选项.
    /// </summary>
    public List<Option> options { get; set; }

    /// <summary>
    /// 字典类型.
    /// </summary>
    public string dictionaryType { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public string propsValue { get; set; }

    /// <summary>
    /// 数据接口id.
    /// </summary>
    public string propsUrl { get; set; }

    /// <summary>
    /// 回显字段.
    /// </summary>
    public string propsLabel { get; set; }

    /// <summary>
    /// 时间格式.
    /// </summary>
    public string format { get; set; }

    /// <summary>
    /// 小数位数.
    /// </summary>
    public int precision { get; set; }

    /// <summary>
    /// 千位分隔.
    /// </summary>
    public bool thousands { get; set; }
}

public class Option
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }
}