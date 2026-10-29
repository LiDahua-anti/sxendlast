using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.ModuleButton;

/// <summary>
/// 功能按钮选择动作输入.
/// </summary>
[SuppressSniffer]
public class ModuleButtonSelectActionsInput
{
    /// <summary>
    /// 菜单 id.
    /// </summary>
    public string menuId { get; set; }

    /// <summary>
    /// 动作 ids.
    /// </summary>
    public List<ActionModel> ids { get; set; }
}

public class ActionModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }
}