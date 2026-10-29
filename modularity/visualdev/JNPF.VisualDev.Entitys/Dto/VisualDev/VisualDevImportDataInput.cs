using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 可视化开发导入数据输入.
/// </summary>
[SuppressSniffer]
public class VisualDevImportDataInput
{
    /// <summary>
    /// 菜单id.
    /// </summary>
    public string menuId { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 数据集合.
    /// </summary>
    public List<Dictionary<string, object>> list { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 是否跳过预览.
    /// </summary>
    public bool type { get; set; }

    /// <summary>
    /// 是否触发集成助手参数
    /// 误删.
    /// </summary>
    public bool isInteAssis { get; set; } = true;
}
