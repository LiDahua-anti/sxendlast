namespace JNPF.Common.Models.User;

/// <summary>
/// 用户身份.
/// </summary>
public class UserStanding
{
    /// <summary>
    /// 身份Id, 1 表示超管.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 身份描述.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 是否当前身份.
    /// </summary>
    public bool currentStanding { get; set; }

    public string icon { get; set; }
}
