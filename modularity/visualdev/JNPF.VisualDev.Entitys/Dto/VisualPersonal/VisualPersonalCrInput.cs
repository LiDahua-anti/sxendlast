using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualPersonal;

/// <summary>
/// 列表个性视图新建输入.
/// </summary>
[SuppressSniffer]
public class VisualPersonalCrInput
{
    /// <summary>
    /// 菜单id.
    /// </summary>
    public string menuId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 类型：0-系统，1-其他.
    /// </summary>
    public int? type { get; set; } = 1;

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