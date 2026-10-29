namespace JNPF.Systems.Entitys.Dto.Permission.Standing;

/// <summary>
/// 身份列表输出.
/// </summary>
public class StandingListOutput
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
    /// 是否系统.
    /// </summary>
    public int isSystem { get; set; }
}
