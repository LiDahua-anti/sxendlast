using JNPF.Common.Filter;

namespace JNPF.Systems.Entitys.Dto.Permission.Standing;

/// <summary>
/// 身份管理列表查询输入.
/// </summary>
public class StandingListInput : PageInputBase
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }
}
