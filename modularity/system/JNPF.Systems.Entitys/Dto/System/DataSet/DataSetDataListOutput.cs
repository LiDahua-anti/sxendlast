using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.DataSet;

namespace JNPF.Systems.Entitys.Dto.DataSet;

/// <summary>
/// 数据集数据列表输出.
/// </summary>
[SuppressSniffer]
public class DataSetDataListOutput : DataSetInfoOutput
{
    /// <summary>
    /// 字段.
    /// </summary>
    public List<DataSetFieldModel> children { get; set; } = new List<DataSetFieldModel>();
}