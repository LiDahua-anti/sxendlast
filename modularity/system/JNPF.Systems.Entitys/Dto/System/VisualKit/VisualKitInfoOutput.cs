using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.VisualKit;

/// <summary>
/// 表单套件信息输出.
/// </summary>
[SuppressSniffer]
public class VisualKitInfoOutput
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
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 表单套件json.
    /// </summary>
    public string formData { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 状态(1-可用,0-不可用).
    /// </summary>
    public int? enabledMark { get; set; }
}
