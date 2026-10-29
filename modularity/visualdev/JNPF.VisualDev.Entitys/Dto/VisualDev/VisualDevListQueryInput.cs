using JNPF.Common.Filter;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 在线开发列表查询输入.
/// </summary>
public class VisualDevListQueryInput : PageInputBase
{
    /// <summary>
    /// 功能类型
    /// 1-自定义表单，2-系统表单.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 页面类型
    /// 1、普通表单，2、流程表单，4、数据视图.
    /// </summary>
    public int? webType { get; set; }

    /// <summary>
    /// 状态(0-未发步，1-已发布，2-已修改).
    /// </summary>
    public string? isRelease { get; set; }

    /// <summary>
    /// 是否为流程首节点.
    /// </summary>
    public bool flowStart { get; set; }
}