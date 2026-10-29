using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.DataSet;

/// <summary>
/// 数据接口字段模型.
/// </summary>
[SuppressSniffer]
public class DataInterfaceFieldModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 映射字段.
    /// </summary>
    public string defaultValue { get; set; }

}
