using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.DataSet;

/// <summary>
/// 数据集预览数据输出.
/// </summary>
[SuppressSniffer]
public class DataSetPreviewDataOutput
{
    /// <summary>
    /// 列.
    /// </summary>
    public List<Dictionary<string, string>> previewColumns { get; set; } = new List<Dictionary<string, string>>();

    /// <summary>
    /// 数据.
    /// </summary>
    public List<Dictionary<string, object>> previewData { get; set; }

    /// <summary>
    /// sql语句.
    /// </summary>
    public string previewSqlText { get; set; }
}
