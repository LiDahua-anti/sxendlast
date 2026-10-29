using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 功能信息输出.
/// </summary>
[SuppressSniffer]
public class VisualDevInfoOutput
{
    /// <summary>
    /// 功能id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 功能名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 功能编码.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 分类id.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// 功能类型
    /// 1-自定义表单，2-系统表单.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// 表单JSON包.
    /// </summary>
    public string? formData { get; set; }

    /// <summary>
    /// 列表JSON包.
    /// </summary>
    public string? columnData { get; set; }

    /// <summary>
    /// App列表JSON包.
    /// </summary>
    public string? appColumnData { get; set; }

    /// <summary>
    /// 数据表JSON包.
    /// </summary>
    public string? tables { get; set; }

    /// <summary>
    /// 状态
    /// 0-禁用，1-开启.
    /// </summary>
    public int state { get; set; }

    /// <summary>
    /// 1-纯表单,2-列表表单,3-工作流表单.
    /// </summary>
    public int webType { get; set; }

    /// <summary>
    /// 数据源id.
    /// </summary>
    public string? dbLinkId { get; set; }

    /// <summary>
    /// 工作流模板Json.
    /// </summary>
    public string? flowTemplateJson { get; set; }

    /// <summary>
    /// 引擎ID.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long sortCode { get; set; }

    /// <summary>
    /// 是否发布.
    /// </summary>
    public int isRelease { get; set; }

    /// <summary>
    /// 接口id.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 接口名称.
    /// </summary>
    public string interfaceName { get; set; }

    /// <summary>
    /// 接口参数.
    /// </summary>
    public string interfaceParam { get; set; }

    /// <summary>
    /// Web地址.
    /// </summary>
    public string urlAddress { get; set; }

    /// <summary>
    /// App地址.
    /// </summary>
    public string appUrlAddress { get; set; }

    /// <summary>
    /// 接口路径.
    /// </summary>
    public string interfaceUrl { get; set; }

    /// <summary>
    /// 启用流程.
    /// </summary>
    public int enableFlow { get; set; }

    /// <summary>
    /// 按钮json.
    /// </summary>
    public string buttonData { get; set; }

    /// <summary>
    /// app按钮json.
    /// </summary>
    public string appButtonData { get; set; }

    /// <summary>
    /// web页面地址.
    /// </summary>
    public string webAddress { get; set; }

    /// <summary>
    /// app页面地址.
    /// </summary>
    public string appAddress { get; set; }
}
