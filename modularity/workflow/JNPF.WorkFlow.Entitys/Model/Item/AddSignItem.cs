namespace JNPF.WorkFlow.Entitys.Model.Item;

public class AddSignItem
{
    /// <summary>
    /// 加签人.
    /// </summary>
    public List<string> addSignUserIdList { get; set; }

    /// <summary>
    /// 加签类型 1 前 2 后.
    /// </summary>
    public int addSignType { get; set; }

    /// <summary>
    /// 审批类型（0：或签 1：会签 2：依次） .
    /// </summary>
    public int? counterSign { get; set; }

    /// <summary>
    /// 会签比例.
    /// </summary>
    public int auditRatio { get; set; } = 100;

    /// <summary>
    /// 加签层级.
    /// </summary>
    public int addSignLevel { get; set; }

    /// <summary>
    /// 加签回退前加签id.
    /// </summary>
    public string rollBackId { get; set; }

    /// <summary>
    /// 选择分支.
    /// </summary>
    public List<string> branchList { get; set; } = new List<string>();
}
