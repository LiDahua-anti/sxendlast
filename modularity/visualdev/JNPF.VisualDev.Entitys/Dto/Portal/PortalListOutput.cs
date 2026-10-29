using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.Portal;

/// <summary>
/// 获取门户列表输出.
/// </summary>
[SuppressSniffer]
public class PortalListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    /// <returns></returns>
    public string id { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 最后修改人.
    /// </summary>
    public string lastModifyUser { get; set; }

    /// <summary>
    /// 最后修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 类型(0-页面设计,1-自定义路径).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 锁定（0-锁定，1-自定义）.
    /// </summary>
    public int? enabledLock { get; set; }

    /// <summary>
    /// 状态（0-未发步，1-已发布，2-已修改）.
    /// </summary>
    public int? state { get; set; }

    /// <summary>
    /// 发布状态（0-未发步，1-已发布，2-已修改）.
    /// </summary>
    public int? isRelease { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    public string platformRelease { get; set; }
}
