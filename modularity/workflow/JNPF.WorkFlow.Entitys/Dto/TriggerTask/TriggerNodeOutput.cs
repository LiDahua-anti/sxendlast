using JNPF.UnifyResult;

namespace JNPF.WorkFlow.Entitys.Dto.TriggerTask;

public class TriggerNodeOutput
{
    public bool isComplete { get; set; }

    public RESTfulResult<object> result { get; set; }
}
