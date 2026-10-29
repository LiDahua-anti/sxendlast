using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.PrintDev;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印版本信息输出.
/// </summary>
[SuppressSniffer]
public class PrintVersionInfoOutput
{
    /// <summary>
    /// 打印模板id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 打印模板名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 打印模板编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 打印模板分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 版本id.
    /// </summary>
    public string versionId { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? state { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public int? version { get; set; }

    /// <summary>
    /// 打印模板json.
    /// </summary>
    public string printTemplate { get; set; }

    /// <summary>
    /// 转换配置.
    /// </summary>
    public string convertConfig { get; set; }

    /// <summary>
    /// 全局设置.
    /// </summary>
    public string globalConfig { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 数据源集合.
    /// </summary>
    public List<PrintDevDataSetModel> dataSetList { get; set; }
}