using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Permission.Standing;

/// <summary>
/// 获取身份列表输入.
/// </summary>
[SuppressSniffer]
public class StandingConditionInput : KeywordInput
{
    /// <summary>
    /// 选择的岗位id、角色id、动态参数.
    /// </summary>
    public List<string> ids { get; set; } = new List<string>();

    /// <summary>
    /// 类型 .
    /// </summary>
    public string type { get; set; }
}
