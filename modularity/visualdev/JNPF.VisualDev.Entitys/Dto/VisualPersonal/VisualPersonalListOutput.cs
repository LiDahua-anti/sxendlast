using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualPersonal;

/// <summary>
/// 列表个性视图列表查询输出.
/// </summary>
[SuppressSniffer]
public class VisualPersonalListOutput
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
    /// 类型：0-系统，1-其他.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 状态：0-其他，1-默认.
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 查询字段.
    /// </summary>
    public string searchList { get; set; }

    /// <summary>
    /// 列表字段.
    /// </summary>
    public string columnList { get; set; }
}