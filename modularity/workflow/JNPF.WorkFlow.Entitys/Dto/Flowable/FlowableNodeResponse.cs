namespace JNPF.WorkFlow.Entitys.Dto.Flowable;

public class FlowableNodeResponse
{
    /// <summary>
    /// flowable任务id(节点id).
    /// </summary>
    public string taskId { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string taskName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string taskKey { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 节点进线编码.
    /// </summary>
    public List<string> incomingList { get; set; } = new List<string>();

    /// <summary>
    /// 节点出线编码.
    /// </summary>
    public List<string> outgoingList { get; set; } = new List<string>();

    /// <summary>
    /// 实例id.
    /// </summary>
    public string instanceId { get; set; }

    public bool isTriggerNode { get; set; }
}
