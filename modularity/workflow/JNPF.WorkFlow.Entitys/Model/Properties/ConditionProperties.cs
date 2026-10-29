using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

[SuppressSniffer]
public class ConditionProperties
{
    /// <summary>
    /// 节点编码.
    /// </summary>
    public string? nodeId { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    public string nodeName { get; set; }

    /// <summary>
    /// 条件明细.
    /// </summary>
    public List<GropsItem>? conditions { get; set; } = new List<GropsItem>();

    /// <summary>
    /// 关联方式.
    /// </summary>
    public string? matchLogic { get; set; }

    /// <summary>
    /// 默认分支.
    /// </summary>
    public bool isDefault { get; set; }
}
