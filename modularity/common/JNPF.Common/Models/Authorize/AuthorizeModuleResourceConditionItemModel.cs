using JNPF.DependencyInjection;

namespace JNPF.Common.Models.Authorize;

/// <summary>
/// 数据权限条件字段.
/// </summary>
[SuppressSniffer]
public class AuthorizeModuleResourceConditionItemModel
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 表名称.
    /// </summary>
    public string tableName { get; set; }

}

public class AuthorizeModuleResourceConditionItemModelInput
{
    /// <summary>
    /// ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 表名称.
    /// </summary>
    public string tableName { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string fieldId { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// jnpf识别符.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 条件.
    /// </summary>
    public string symbol { get; set; }

    /// <summary>
    /// 条件名称.
    /// </summary>
    public string symbolName { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public string fieldValue { get; set; }

    public int? fieldValueType { get; set; } = 4;

    public string fieldValueJnpfKey { get; set; }

    public bool disabled { get; set; }

    public bool multiple { get; set; }

    public bool required { get; set; }

    public string cellKey { get; set; }

    public configModel __config__ { get; set; }
}

public class configModel
{
    public string jnpfKey { get; set; }

    public string label { get; set; }
}