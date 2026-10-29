using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.UsersCurrent;

/// <summary>
/// 当前用户 所属组织 输出.
/// </summary>
[SuppressSniffer]
public class CurrentUserOrganizesOutput
{
    /// <summary>
    /// 主键Id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 是否默认 0：否，1：是.
    /// </summary>
    public bool isDefault { get; set; } = false;

    /// <summary>
    /// 上级责任人.
    /// </summary>
    public string managerName { get; set; }

    /// <summary>
    /// 所属组织ids.
    /// </summary>
    public string orgTreeIds { get; set; }

    /// <summary>
    /// 所属组织.
    /// </summary>
    public string orgTreeName { get; set; }

    /// <summary>
    /// 上级岗位.
    /// </summary>
    public string parentName { get; set; }

    /// <summary>
    /// 组织id.
    /// </summary>
    public string organizesId { get; set; }
}