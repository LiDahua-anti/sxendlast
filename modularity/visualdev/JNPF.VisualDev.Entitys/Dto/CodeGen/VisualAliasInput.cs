using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.CodeGen;

/// <summary>
/// 命名规范输入.
/// </summary>
[SuppressSniffer]
public class VisualAliasInput
{
    /// <summary>
    /// 命名规范表或字段命名集合.
    /// </summary>
    public List<VisualAliasTableModel>? tableList { get; set; }
}