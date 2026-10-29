namespace JNPF.WorkFlow.Entitys.Model.Item;

public class TransferItem
{
    /// <summary>
    /// 目标表单字段.
    /// </summary>
    public string targetField { get; set; }

    /// <summary>
    /// 来源类型 1-字段 2-自定义 3-为空 4-系统变量.
    /// </summary>
    public int sourceType { get; set; }

    /// <summary>
    /// 来源字段/自定义值.
    /// </summary>
    public string sourceValue { get; set; }

    /// <summary>
    /// 来源字段/自定义值.
    /// </summary>
    public string sourceField { get; set; }

    /// <summary>
    /// 是否子表(目标表单).
    /// </summary>
    public bool isSubTable_target { get; set; }

    /// <summary>
    /// 子表表名(目标表单).
    /// </summary>
    public string subTableName_target { get; set; }

    /// <summary>
    /// 子表字段(目标表单).
    /// </summary>
    public string subTableField_target { get; set; }

    /// <summary>
    /// 是否子表(来源表单).
    /// </summary>
    public bool isSubTable_source { get; set; }

    /// <summary>
    /// 子表表名(来源表单).
    /// </summary>
    public string subTableName_source { get; set; }

    /// <summary>
    /// 子表字段(来源表单).
    /// </summary>
    public string subTableField_source { get; set; }
}
