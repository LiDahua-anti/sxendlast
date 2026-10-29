using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.VisualKit;

/// <summary>
/// 表单套件列表输出.
/// </summary>
[SuppressSniffer]
public class VisualKitListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 门户名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 状态(1-可用,0-不可用).
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }
}
