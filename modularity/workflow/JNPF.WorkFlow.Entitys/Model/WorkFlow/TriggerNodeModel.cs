namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

public class TriggerNodeModel
{
    /// <summary>
    /// 节点id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 节点属性Json.
    /// </summary>
    public string nodeJson { get; set; }

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string flowName { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 引擎id.
    /// </summary>
    public string engineId { get; set; }
}
