using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 获取用户列表输入.
/// </summary>
[SuppressSniffer]
public class UserConditionInput
{
    /// <summary>
    /// 部门id.
    /// </summary>
    public List<string> departIds { get; set; } = new List<string>();

    /// <summary>
    /// 岗位id.
    /// </summary>
    public List<string> positionIds { get; set; } = new List<string>();

    /// <summary>
    /// 用户id.
    /// </summary>
    public List<string> userIds { get; set; } = new List<string>();

    /// <summary>
    /// 角色Id.
    /// </summary>
    public List<string> roleIds { get; set; } = new List<string>();

    /// <summary>
    /// 分组Id.
    /// </summary>
    public List<string> groupIds { get; set; } = new List<string>();

    /// <summary>
    /// 分页和搜索.
    /// </summary>
    public PageInputBase pagination { get; set; }

    /// <summary>
    /// 分类-工作流专用 ( 空值-不过滤,2-同一部门 3:同一岗位 4-主管 5-下属 6:同一公司 7-角色 8-分组).
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 过滤基准人员.
    /// </summary>
    public string userId { get; set; }
}