namespace JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

/// <summary>
/// 在线开发导入预览输出.
/// </summary>
public class VisualDevModelDataPreviewOutput
{
    /// <summary>
    /// 数据.
    /// </summary>
    public List<Dictionary<string, object>> dataRow { get; set; }

    /// <summary>
    /// 列名.
    /// </summary>
    public List<dynamic> headerRow { get; set; } = new List<dynamic>();
}
