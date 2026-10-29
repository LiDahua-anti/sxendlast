using JNPF.DependencyInjection;

namespace JNPF.VisualData.Entitys.Dto.ScreenAssets;

/// <summary>
/// 静态资源创建输入.
/// </summary>
[SuppressSniffer]
public class ScreenAssetsCrInput
{
    /// <summary>
    /// 资源名称.
    /// </summary>
    public string assetsName { get; set; }

    /// <summary>
    /// 资源大小 1M.
    /// </summary>
    public string assetsSize { get; set; }

    /// <summary>
    /// 资源上传时间.
    /// </summary>
    public string assetsTime { get; set; }

    /// <summary>
    /// 资源后缀名.
    /// </summary>
    public string assetsType { get; set; }

    /// <summary>
    /// 资源地址.
    /// </summary>
    public string assetsUrl { get; set; }
}
