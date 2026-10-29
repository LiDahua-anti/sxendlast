using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataSet;

/// <summary>
/// 数据集修改输入.
/// </summary>
[SuppressSniffer]
public class DataSetUpInput : DataSetCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}