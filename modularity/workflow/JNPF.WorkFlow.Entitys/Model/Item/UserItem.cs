namespace JNPF.WorkFlow.Entitys.Model.Item;

public class UserItem
{
    /// <summary>
    /// 审批人名.
    /// </summary>
    public string? userName { get; set; }

    /// <summary>
    /// 头像.
    /// </summary>
    public string headIcon { get; set; }

    /// <summary>
    /// 审批人.
    /// </summary>
    public string? userId { get; set; }

    /// <summary>
    /// 审批类型.
    /// </summary>
    public int? handleType { get; set; }
}
