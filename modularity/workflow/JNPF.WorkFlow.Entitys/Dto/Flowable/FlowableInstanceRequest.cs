namespace JNPF.WorkFlow.Entitys.Dto.Flowable;

public class FlowableInstanceRequest
{
    /// <summary>
    /// flowable流程id.
    /// </summary>
    public string deploymentId { get; set; }

    /// <summary>
    /// flowable流程参数.
    /// </summary>
    public object variables { get; set; }

    /// <summary>
    /// deleteReason.
    /// </summary>
    public string deleteReason { get; set; }

    /// <summary>
    /// flowable实例id.
    /// </summary>
    public string instanceId { get; set; }
}
