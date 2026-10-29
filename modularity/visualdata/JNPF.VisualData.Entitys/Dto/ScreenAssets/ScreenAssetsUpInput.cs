using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenAssets;

/// <summary>
/// 静态资源更新输入.
/// </summary>
[SuppressSniffer]
public class ScreenAssetsUpInput : ScreenAssetsCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}
