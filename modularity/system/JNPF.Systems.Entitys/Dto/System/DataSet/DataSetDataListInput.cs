using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataSet;

/// <summary>
/// 数据集数据列表输入.
/// </summary>
[SuppressSniffer]
public class DataSetDataListInput
{
    /// <summary>
    /// 关联数据类型.
    /// </summary>
    public string objectType { get; set; }

    /// <summary>
    /// 关联数据id.
    /// </summary>
    public string objectId { get; set; }
}