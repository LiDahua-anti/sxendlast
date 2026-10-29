using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

[SuppressSniffer]
public class NodeModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string? nodeCode { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    public string? nodeName { get; set; }

    /// <summary>
    /// 节点类型.
    /// </summary>
    public string? nodeType { get; set; }

    /// <summary>
    /// 经办人集合.
    /// </summary>
    public string? userName { get; set; }

    /// <summary>
    /// 流程图节点颜色类型(0:绿色，1：蓝色，其他：灰色).
    /// </summary>
    public string? type { get; set; }
}
