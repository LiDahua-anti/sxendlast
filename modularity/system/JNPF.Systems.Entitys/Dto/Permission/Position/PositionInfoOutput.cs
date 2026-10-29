using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Position;

/// <summary>
/// 岗位信息输出 .
/// </summary>
[SuppressSniffer]
public class PositionInfoOutput
{
    /// <summary>
    /// ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 机构ID.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 岗位名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 岗位编号.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 岗位类型.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 所属组织名称.
    /// </summary>
    public string organizeName { get; set; }

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
    /// 是否被设定为先决岗位.
    /// </summary>
    public bool isPrePosition { get; set; }
}