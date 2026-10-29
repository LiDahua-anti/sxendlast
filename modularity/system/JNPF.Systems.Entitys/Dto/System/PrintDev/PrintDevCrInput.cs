using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.PrintDev;

/// <summary>
/// 打印模板创建输入.
/// </summary>
[SuppressSniffer]
public class PrintDevCrInput
{
    /// <summary>
    /// 名称.
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
    /// 通用-将该模板设为通用(0-表单用，1-业务打印模板用).
    /// </summary>
    public int? commonUse { get; set; }

    /// <summary>
    /// 发布范围：1-公开，2-权限设置.
    /// </summary>
    public int? visibleType { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 图标颜色.
    /// </summary>
    public string iconBackground { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}