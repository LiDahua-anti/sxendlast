using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板列表输出.
/// </summary>
[SuppressSniffer]
public class PrintDevListOutput
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
    /// 编号.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改人.
    /// </summary>
    public string lastModifyUser { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? state { get; set; }
}