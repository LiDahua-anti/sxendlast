namespace JNPF.WorkFlow.Entitys.Dto.Flowable;

/// <summary>
/// flowable流程定义请求实体.
/// </summary>
public class FlowableDefinitionRequest
{
    /// <summary>
    /// bpmnXml.
    /// </summary>
    public string bpmnXml { get; set; }

    /// <summary>
    /// flowableId.
    /// </summary>
    public string deploymentId { get; set; }
}