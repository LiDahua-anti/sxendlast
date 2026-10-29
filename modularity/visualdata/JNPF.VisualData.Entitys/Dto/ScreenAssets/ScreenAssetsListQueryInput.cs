using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenAssets;

/// <summary>
/// 静态资源列表输入.
/// </summary>
[SuppressSniffer]
public class ScreenAssetsListQueryInput
{
    /// <summary>
    /// 查询.
    /// </summary>
    public string assetsName { get; set; }

    /// <summary>
    /// 当前页码:pageIndex.
    /// </summary>
    public virtual int current { get; set; } = 1;

    /// <summary>
    /// 每页行数.
    /// </summary>
    public virtual int size { get; set; } = 50;
}
