using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 权限数据修改输入.
/// </summary>
[SuppressSniffer]
public class AuthorizeDataUpInput
{
    /// <summary>
    /// 类型 organize , role.
    /// </summary>
    public string objectType { get; set; }

    /// <summary>
    /// 按钮.
    /// </summary>
    public List<string> button { get; set; }

    /// <summary>
    /// 列表.
    /// </summary>
    public List<string> column { get; set; }

    /// <summary>
    /// 模块.
    /// </summary>
    public List<string> module { get; set; }

    /// <summary>
    /// 表单.
    /// </summary>
    public List<string> form { get; set; }

    /// <summary>
    /// 资源.
    /// </summary>
    public List<string> resource { get; set; }

    /// <summary>
    /// 流程.
    /// </summary>
    public List<string> flow { get; set; }
}