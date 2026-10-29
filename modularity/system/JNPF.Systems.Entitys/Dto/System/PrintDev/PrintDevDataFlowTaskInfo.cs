using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板流程任务信息.
/// </summary>
[SuppressSniffer]
public class PrintDevDataFlowTaskInfo
{
    /// <summary>
    /// 参数.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 流程任务id.
    /// </summary>
    public string flowTaskId { get; set; }
}