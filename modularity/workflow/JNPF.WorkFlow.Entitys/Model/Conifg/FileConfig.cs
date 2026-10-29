namespace JNPF.WorkFlow.Entitys.Model.Conifg;

public class FileConfig
{
    /// <summary>
    /// 是否开启.
    /// </summary>
    public bool on { get; set; }

    /// <summary>
    /// 查看权限 1：当前流程所有人  2：流程发起人  3：最后节点审批人.
    /// </summary>
    public int permissionType { get; set; } = 1;

    /// <summary>
    /// 归档路径.
    /// </summary>
    public string? parentId { get; set; }

    /// <summary>
    /// 归档模板.
    /// </summary>
    public string? templateId { get; set; }
}
