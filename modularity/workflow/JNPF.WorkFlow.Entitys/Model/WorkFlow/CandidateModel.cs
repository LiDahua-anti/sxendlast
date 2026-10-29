using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

[SuppressSniffer]
public class CandidateModel
{
    /// <summary>
    /// 节点编码.
    /// </summary>
    public string? nodeCode { get; set; }

    /// <summary>
    /// 节点名.
    /// </summary>
    public string? nodeName { get; set; }

    /// <summary>
    /// 是否候选人.
    /// </summary>
    public bool isCandidates { get; set; }

    /// <summary>
    /// 是否有候选人.
    /// </summary>
    public bool hasCandidates { get; set; }

    /// <summary>
    /// 已选审批人.
    /// </summary>
    public string selected { get; set; }

    /// <summary>
    /// 是否条件分支.
    /// </summary>
    public bool isBranchFlow { get; set; }
}
