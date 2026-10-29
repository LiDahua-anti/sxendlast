namespace JNPF.Common.Models.User;

/// <summary>
/// 用户工作交接输出.
/// </summary>
public class UserWorkHandoverModel
{
    /// <summary>
    /// 待办事宜.
    /// </summary>
    public List<FlowWorkModel> flowTask { get; set; } = new List<FlowWorkModel>();

    /// <summary>
    /// 流程列表.
    /// </summary>
    public List<FlowWorkModel> flow { get; set; } = new List<FlowWorkModel>();

    /// <summary>
    /// 权限组.
    /// </summary>
    public List<SystemListSelector> app { get; set; } = new List<SystemListSelector>();

    /// <summary>
    /// 是否应用权限.
    /// </summary>
    public bool isAppShow { get; set; }
}

/// <summary>
/// 流程相关.
/// </summary>
public class FlowWorkModel
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 子集.
    /// </summary>
    public List<FlowWorkModel> children { get; set; }
}

/// <summary>
/// 应用相关.
/// </summary>
public class SystemListSelector
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }
}