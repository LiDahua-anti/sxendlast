using JNPF.DependencyInjection;

namespace JNPF.Common.Dtos.VisualDev;

/// <summary>
/// 在线功能开发数据修改输入.
/// </summary>
[SuppressSniffer]
public class VisualDevModelDataUpInput : VisualDevModelDataCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 集成助手 idList
    /// </summary>
    public List<string>? idList { get; set; }

    /// <summary>
    /// 任务流程修改删除不存在的子表数据.
    /// </summary>
    public bool isDelSubTableData { get; set; } = true;
}
