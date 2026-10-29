using JNPF.WorkFlow.Entitys.Model.Properties;

namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

public class WorkFlowNodeModel
{
    /// <summary>
    /// 引擎任务主键.
    /// </summary>
    public string nodeId { get; set; }

    /// <summary>
    /// 表单id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 节点类型.
    /// </summary>
    public string nodeType { get; set; }

    /// <summary>
    /// 节点属性Json.
    /// </summary>
    public NodeProperties nodePro { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string nodeCode { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string nodeName { get; set; }

    /// <summary>
    /// 节点属性Json.
    /// </summary>
    public string nodeJson { get; set; }
}
