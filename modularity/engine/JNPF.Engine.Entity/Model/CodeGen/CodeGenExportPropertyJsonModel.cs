using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

[SuppressSniffer]
public class CodeGenExportPropertyJsonModel
{
    public string fieldName { get; set; }

    public string jnpfKey { get; set; }

    public string fieldId { get; set; }

    public string tableName { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool required { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool multiple { get; set; }
}