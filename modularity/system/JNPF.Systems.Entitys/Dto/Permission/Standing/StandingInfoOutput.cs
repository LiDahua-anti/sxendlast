namespace JNPF.Systems.Entitys.Dto.Permission.Standing;

/// <summary>
/// 身份信息输出.
/// </summary>
public class StandingInfoOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}
