using JNPF.Common.Security;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 在线开发下拉框输出.
/// </summary>
public class VisualDevSelectorOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// web页面地址.
    /// </summary>
    public string webAddress { get; set; }

    /// <summary>
    /// app页面地址.
    /// </summary>
    public string appAddress { get; set; }

    /// <summary>
    /// 页面类型（1、纯表单，2、表单加列表，4、数据视图）.
    /// </summary>
    public int? webType { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? SortCode { get; set; }
}
