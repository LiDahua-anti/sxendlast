using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 用户数据导入 输入.
/// </summary>
[SuppressSniffer]
public class UserImportDataInput
{
    /// <summary>
    /// 导入的数据列表.
    /// </summary>
    public List<UserListImportDataInput> list { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 是否跳过预览.
    /// </summary>
    public bool type { get; set; }
}