namespace JNPF.VisualData.Entitys.Dto.ScreenMap;

/// <summary>
/// 大屏数据列表查询输入.
/// </summary>
public class ScreenMapListQueryInput
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 父级id.
    /// </summary>
    public string parentId { get; set; }
}
