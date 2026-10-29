namespace JNPF.VisualDev.Entitys.Dto.Dashboard;

/// <summary>
/// 获取通知公告输入.
/// </summary>
public class DashboardNoticeInput
{
    /// <summary>
    /// 待签.
    /// </summary>
    public List<string> flowToSignType { get; set; }

    /// <summary>
    /// 待办.
    /// </summary>
    public List<string> flowTodoType { get; set; }

    /// <summary>
    /// 在办.
    /// </summary>
    public List<string> flowDoingType { get; set; }

    /// <summary>
    /// 已办.
    /// </summary>
    public List<string> flowDoneType { get; set; }

    /// <summary>
    /// 抄送.
    /// </summary>
    public List<string> flowCirculateType { get; set; }

    /// <summary>
    /// 公告分类.
    /// </summary>
    public List<string> typeList { get; set; }
}
