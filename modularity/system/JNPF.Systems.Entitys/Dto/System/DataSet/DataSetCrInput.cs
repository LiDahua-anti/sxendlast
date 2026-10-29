using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataSet;

/// <summary>
/// 数据集创建输入.
/// </summary>
[SuppressSniffer]
public class DataSetCrInput
{
    /// <summary>
    /// 关联数据id.
    /// </summary>
    public string objectId { get; set; }

    /// <summary>
    /// 关联数据类型.
    /// </summary>
    public string objectType { get; set; }

    /// <summary>
    /// sql语句.
    /// </summary>
    public string dataConfigJson { get; set; }

    /// <summary>
    /// 连接id.
    /// </summary>
    public string dbLinkId { get; set; }

    /// <summary>
    /// 显示字段.
    /// </summary>
    public string fieldJson { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 输入参数.
    /// </summary>
    public string parameterJson { get; set; }

    /// <summary>
    /// 数据接口id.
    /// </summary>
    public string interfaceId { get; set; }
}