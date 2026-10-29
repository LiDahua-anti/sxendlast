using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Model;

/// <summary>
/// 表单回传设置模型.
/// </summary>
[SuppressSniffer]
public class VisualHcSetModel
{
    public string fieldName { get; set; }
    public string fieldId { get; set; }
    public string tableName { get; set; }
}

/// <summary>
/// 表单回传表单字段模型.
/// </summary>
[SuppressSniffer]
public class VisualHcFormSetModel : VisualHcSetModel
{
    public string jnpfKey { get; set; }
    public bool required { get; set; }
    public bool multiple { get; set; }
}

/// <summary>
/// 表单回传按钮设置模型.
/// </summary>
[SuppressSniffer]
public class VisualHcButtonSetModel
{
    public string fullName { get; set; }
    public string enCode { get; set; }
    public bool show { get; set; }
}