using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.DataSet;

namespace JNPF.Systems.Entitys.Model.PrintDev;

/// <summary>
/// 打印模板数据集模型.
/// </summary>
[SuppressSniffer]
public class PrintDevDataSetModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 数据连接id.
    /// </summary>
    public string dbLinkId { get; set; }

    /// <summary>
    /// 数据sql语句.
    /// </summary>
    public string dataConfigJson { get; set; }

    /// <summary>
    /// 输入参数.
    /// </summary>
    public string parameterJson { get; set; }

    /// <summary>
    /// 显示字段.
    /// </summary>
    public string fieldJson { get; set; }

    /// <summary>
    /// 配置式.
    /// </summary>
    public string visualConfigJson { get; set; }

    /// <summary>
    /// 条件配置.
    /// </summary>
    public string filterConfigJson { get; set; }

    /// <summary>
    /// 数据接口id.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 数据接口名称.
    /// </summary>
    public string treePropsName { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public List<DataSetFieldModel> children { get; set; } = new List<DataSetFieldModel>();
}