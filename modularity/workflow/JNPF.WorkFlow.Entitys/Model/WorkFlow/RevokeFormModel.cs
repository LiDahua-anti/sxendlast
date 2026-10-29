namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

public class RevokeFormModel
{
    /// <summary>
    /// 审批编号.
    /// </summary>
    public string billRule { get; set; }

    /// <summary>
    /// 提交时间.
    /// </summary>
    public long creatorTime { get; set; }

    /// <summary>
    /// 撤销理由.
    /// </summary>
    public string handleOpinion { get; set; }

    /// <summary>
    /// 关联流程任务id.
    /// </summary>
    public string revokeTaskId { get; set; }

    /// <summary>
    /// 关联流程任务名称.
    /// </summary>
    public string revokeTaskName { get; set; }
}
