using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.CodeGen;

/// <summary>
/// 获取命名规范输出.
/// </summary>
[SuppressSniffer]
public class VisualAliasTableModel
{
    public string table { get; set; }

    public string comment { get; set; }

    public List<VisualAliasTableFields> fields { get; set; }

    /// <summary>
    /// 表别名.
    /// </summary>
    public string aliasName { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string fieldName { get; set; }
}

public class VisualAliasTableFields
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 字段别名.
    /// </summary>
    public string aliasName { get; set; }

    /// <summary>
    /// 字段注释.
    /// </summary>
    public string fieldName { get; set; }

}