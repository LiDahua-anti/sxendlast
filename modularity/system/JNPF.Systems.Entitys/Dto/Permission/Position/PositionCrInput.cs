using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Position;

/// <summary>
/// 创建岗位输出.
/// </summary>
[SuppressSniffer]
public class PositionCrInput
{
    /// <summary>
    /// 上级岗位.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 机构ID.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 岗位名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 岗位编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 岗位约束(0-关闭，1启用).
    /// </summary>
    public int isCondition { get; set; }

    /// <summary>
    /// 约束内容.
    /// </summary>
    public string conditionJson { get; set; }

    /// <summary>
    /// 是否默认岗位（0-否1-是）.
    /// </summary>
    public int defaultMark { get; set; } = 0;

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}