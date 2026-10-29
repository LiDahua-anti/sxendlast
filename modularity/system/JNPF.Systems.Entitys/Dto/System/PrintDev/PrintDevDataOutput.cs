using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Model.PrintDev;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板配置数据输出.
/// </summary>
[SuppressSniffer]
public class PrintDevDataOutput
{
    /// <summary>
    /// 模板名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// sql数据.
    /// </summary>
    public object printData { get; set; }

    /// <summary>
    /// 模板数据.
    /// </summary>
    public string printTemplate { get; set; }

    /// <summary>
    /// 数据转换配置.
    /// </summary>
    public string convertConfig { get; set; }

    /// <summary>
    /// 全局设置.
    /// </summary>
    public string globalConfig { get; set; }

    /// <summary>
    /// 审批数据.
    /// </summary>
    public List<PrintDevDataModel> operatorRecordList { get; set; } = new List<PrintDevDataModel>();
}