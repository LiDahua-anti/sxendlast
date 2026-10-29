namespace JNPF.Common.Dtos;

/// <summary>
/// 数据导入输入.
/// </summary>
public class DataImportInput
{
    /// <summary>
    /// 菜单Id.
    /// </summary>
    public string menuId { get; set; }

    /// <summary>
    /// 流程Id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 数据集合.
    /// </summary>
    public List<Dictionary<string, object>> list { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 是否跳过预览.
    /// </summary>
    public bool type { get; set; }
}