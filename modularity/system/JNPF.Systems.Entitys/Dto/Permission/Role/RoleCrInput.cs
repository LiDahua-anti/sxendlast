using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Role;

/// <summary>
/// 角色创建输入.
/// </summary>
[SuppressSniffer]
public class RoleCrInput
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
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 有效标记.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 岗位约束(0-关闭，1启用).
    /// </summary>
    public int isCondition { get; set; }

    /// <summary>
    /// 约束内容.
    /// </summary>
    public string conditionJson { get; set; }

}