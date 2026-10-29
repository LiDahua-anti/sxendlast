namespace JNPF.WorkFlow.Entitys.Model.Conifg;

public class ApproversConfig
{
    /// <summary>
    /// 审批起点 1：发起人 2：上节点审批人.
    /// </summary>
    public int start { get; set; }

    /// <summary>
    /// 审批终点 1：发起人 2：上节点审批人  3：组织机构的.
    /// </summary>
    public int end { get; set; }

    /// <summary>
    /// 直属主管.
    /// </summary>
    public int level { get; set; } = 1;

    /// <summary>
    /// 组织层级
    /// </summary>
    public int originLevel { get; set; } = 1;
}
