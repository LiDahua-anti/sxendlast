namespace JNPF.Common.Models;

/// <summary>
/// 数据导出导入表头列名模型.
/// </summary>
public class ExportImportHelperModel
{
    /// <summary>
    /// 字段Key.
    /// </summary>
    public string ColumnKey { get; set; }

    /// <summary>
    /// 字段名.
    /// </summary>
    public string ColumnValue { get; set; }

    /// <summary>
    /// 是否必填.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// 是否列冻结.
    /// </summary>
    public bool FreezePane { get; set; }

    /// <summary>
    /// 下拉选择值.
    /// </summary>
    public List<string> SelectList { get; set; } = new List<string>();
}