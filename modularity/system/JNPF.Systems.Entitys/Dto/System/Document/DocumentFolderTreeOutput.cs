using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Document;

/// <summary>
/// 获取知识管理列表（文件夹树）.
/// </summary>
[SuppressSniffer]
public class DocumentFolderTreeOutput : TreeModel
{
    /// <summary>
    /// 图标.
    /// </summary>
    public string? icon { get; set; } = "fa fa-folder";

    /// <summary>
    /// 文件名.
    /// </summary>
    public string? fullName { get; set; }
}