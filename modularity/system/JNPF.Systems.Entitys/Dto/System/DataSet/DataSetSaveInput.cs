using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.PrintDev;

namespace JNPF.Systems.Entitys.Dto.DataSet;

/// <summary>
/// 数据集批量保存输入.
/// </summary>
[SuppressSniffer]
public class DataSetSaveInput
{
    /// <summary>
    /// 关联数据类型.
    /// </summary>
    public string objectType { get; set; }

    /// <summary>
    /// 关联数据id.
    /// </summary>
    public string objectId { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public List<PrintDevDataSetModel> list { get; set; }
}