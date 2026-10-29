namespace JNPF.Systems.Entitys.Dto.Permission.User;

/// <summary>
/// 用户工作交接输入.
/// </summary>
public class UserWorkHandoverInput
{
    /// <summary>
    /// 移交人.
    /// </summary>
    public string fromId { get; set; }

    /// <summary>
    /// 交接人.
    /// </summary>
    public string handoverUser { get; set; }

    /// <summary>
    /// 应用交接人.
    /// </summary>
    public string appHandoverUser { get; set; }

    /// <summary>
    /// 待办事宜.
    /// </summary>
    public List<string> flowTaskList { get; set; }

    /// <summary>
    /// 负责流程.
    /// </summary>
    public List<string> flowList { get; set; }

    /// <summary>
    /// 交接应用.
    /// </summary>
    public List<string> appList { get; set; }
}
