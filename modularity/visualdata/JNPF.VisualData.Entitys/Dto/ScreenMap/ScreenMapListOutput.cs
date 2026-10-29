namespace JNPF.VisualData.Entitys.Dto.ScreenMap;

/// <summary>
/// 大屏地图列表输出.
/// </summary>
public class ScreenMapListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public string data { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 祖编码.
    /// </summary>
    public string ancestors { get; set; }

    /// <summary>
    /// 级别.
    /// </summary>
    public int level { get; set; }

    /// <summary>
    /// 是否删除.
    /// </summary>
    public int isDeleted { get; set; }

    /// <summary>
    /// 是否有子级.
    /// </summary>
    public bool hasChildren { get; set; }

    /// <summary>
    /// 父级id.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 父级名称.
    /// </summary>
    public string parentName { get; set; }

    /// <summary>
    /// 父级编码.
    /// </summary>
    public string parentCode { get; set; }
}
