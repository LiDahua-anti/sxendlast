using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataSet;

/// <summary>
/// 数据集数据输入.
/// </summary>
[SuppressSniffer]
public class DataSetDataInput
{
    /// <summary>
    /// 打印模板id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 表单id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 数据来源.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 查询列.
    /// </summary>
    public string queryList { get; set; }

    /// <summary>
    /// 数据集配置.
    /// </summary>
    public string convertConfig { get; set; }

    /// <summary>
    /// 其他参数.
    /// </summary>
    public Dictionary<string, object> map { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 其他参数.
    /// </summary>
    public string mapStr { get; set; }

    /// <summary>
    /// 雪花id.
    /// </summary>
    public string snowFlakeId { get; set; }
}