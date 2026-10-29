namespace JNPF.WorkFlow.Entitys.Dto.Flowable;

public class FlowableNodeRequest
{
    /// <summary>
    /// flowableId.
    /// </summary>
    public string deploymentId { get; set; }

    /// <summary>
    /// flowable连线编码.
    /// </summary>
    public string flowKey { get; set; }

    /// <summary>
    /// flowable节点编码.
    /// </summary>
    public string taskKey { get; set; }

    /// <summary>
    /// flowable节点id.
    /// </summary>
    public string taskId { get; set; }

    /// <summary>
    /// flowable流程参数.
    /// </summary>
    public object variables { get; set; }

    /// <summary>
    /// 指定节点id.
    /// </summary>
    public string targetKey { get; set; }
}
