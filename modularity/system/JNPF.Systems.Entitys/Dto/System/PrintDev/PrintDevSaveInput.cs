using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.PrintDev;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板保存输入.
/// </summary>
[SuppressSniffer]
public class PrintDevSaveInput
{
    /// <summary>
    /// 打印模板id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 版本id.
    /// </summary>
    public string versionId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 打印模板json.
    /// </summary>
    public string printTemplate { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public int? version { get; set; }

    /// <summary>
    /// 0 保存，1 发布.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 转换配置.
    /// </summary>
    public string convertConfig { get; set; }

    /// <summary>
    /// 全局设置.
    /// </summary>
    public string globalConfig { get; set; }

    /// <summary>
    /// 数据集.
    /// </summary>
    public List<PrintDevDataSetModel> dataSetList { get; set; }
}