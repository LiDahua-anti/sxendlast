using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;

namespace JNPF.WorkFlow.Entitys.Dto.Operator;

public class OperatorOutput
{
    /// <summary>
    /// 异常节点.
    /// </summary>
    public List<CandidateModel> errorCodeList { get; set; } = new List<CandidateModel>();

    /// <summary>
    /// 是否结束.
    /// </summary>
    public bool isEnd { get; set; }

    public WorkFlowParamter wfParamter { get; set; }

    public string taskId { get; set; }
}
