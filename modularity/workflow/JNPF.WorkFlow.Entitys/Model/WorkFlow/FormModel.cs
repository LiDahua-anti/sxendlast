namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

public class FormModel
{
    /// <summary>
    /// 表单id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 类型
    /// 1-Web设计,2-App设计,3-流程表单,4-Web表单,5-App表单.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }

    /// <summary>
    /// Web地址.
    /// </summary>
    public string? urlAddress { get; set; }

    /// <summary>
    /// APP地址.
    /// </summary>
    public string? appUrlAddress { get; set; }

    /// <summary>
    /// 表单json.
    /// </summary>
    public string? formData { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 关联表单.
    /// </summary>
    public string? visualTables { get; set; }

    /// <summary>
    /// 数据源id.
    /// </summary>
    public string? dbLinkId { get; set; }

    /// <summary>
    /// 接口路径.
    /// </summary>
    public string? interfaceUrl { get; set; }

    /// <summary>
    /// 引擎id.
    /// </summary>
    public string? flowId { get; set; }
}
