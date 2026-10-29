using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.DataSet;

/// <summary>
/// 数据集字段模型.
/// </summary>
[SuppressSniffer]
public class DataSetFieldModel
{
    /// <summary>
    /// 字段说明.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 字段说明.
    /// </summary>
    public string fullName { get; set; }
}