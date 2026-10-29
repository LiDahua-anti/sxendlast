using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Position;

/// <summary>
/// 岗位约束模型 .
/// </summary>
[SuppressSniffer]
public class PositionConstraintModel
{
    /// <summary>
    /// 0 互斥约束、1 基数约束、先决约束.
    /// </summary>
    public List<int>? constraintType { get; set; }

    /// <summary>
    /// 互斥 岗位Id.
    /// </summary>
    public List<string>? mutualExclusion { get; set; }

    /// <summary>
    /// 用户基数 最多加几个用户.
    /// </summary>
    public int? userNum { get; set; }

    /// <summary>
    /// 权限基数 最多加几个菜单权限.
    /// </summary>
    public int? permissionNum { get; set; }

    /// <summary>
    /// 先决 岗位Id.
    /// </summary>
    public List<string>? prerequisite { get; set; }
}