namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 在线开发集成助手列表.
/// </summary>
public class VisualDevIntergrateListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 是否引用：0-否，1-是.
    /// </summary>
    public int isQuote { get; set; }
}