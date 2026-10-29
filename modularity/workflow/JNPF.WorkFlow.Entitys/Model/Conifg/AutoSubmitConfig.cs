namespace JNPF.WorkFlow.Entitys.Model.Conifg;

public class AutoSubmitConfig
{
    /// <summary>
    /// 相邻节点审批人重复.
    /// </summary>
    public bool adjacentNodeApproverRepeated { get; set; }

    /// <summary>
    /// 审批人审批过该流程.
    /// </summary>
    public bool approverHasApproval { get; set; }

    /// <summary>
    /// 发起人与审批人重复.
    /// </summary>
    public bool initiatorApproverRepeated { get; set; }
}
