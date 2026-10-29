using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.AI;

/// <summary>
/// AI 表单输出.
/// </summary>
[SuppressSniffer]
public class VisualDevAIFormOutput
{
    /// <summary>
    /// 模型编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 模型名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// AI 模型列表.
    /// </summary>
    public List<AIFormModel> aiModelList { get; set; }
}

public class AIFormModel
{
    public string tableTitle { get; set; }
    public string tableName { get; set; }
    public bool isMain { get; set; }
    public List<AIFormFieldModel> fields { get; set; }
}

public class AIFormFieldModel
{
    public string fieldTitle { get; set; }
    public string fieldName { get; set; }
    public string fieldDbType { get; set; }
    public string fieldComponent { get; set; }
    public List<AIFormFieldOptionModel> fieldOptions { get; set; }
}

public class AIFormFieldOptionModel
{
    public string fullName { get; set; }
    public string id { get; set; }
}
