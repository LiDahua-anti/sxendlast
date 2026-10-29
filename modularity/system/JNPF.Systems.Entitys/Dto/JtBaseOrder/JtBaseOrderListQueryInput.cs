using JNPF.Common.Filter;

namespace JNPF.example.Entitys.Dto.JtBaseOrder;

/// <summary>
/// 排产计划（二开）列表查询输入.
/// </summary>
public class JtBaseOrderListQueryInput : PageInputBase
{
    /// <summary>
    /// 高级查询.
    /// </summary>
    public string superQueryJson { get; set; }

    /// <summary>
    /// 选择导出数据ids.
    /// </summary>
    public string selectIds { get; set; }

    /// <summary>
    /// 选择导出数据key.
    /// </summary>
    public string selectKey { get; set; }

    /// <summary>
    /// 导出类型.
    /// </summary>
    public int dataType { get; set; }
    
    /// <summary>
    /// 关键词查询.
    /// </summary>
    public string jnpfKeyword { get; set; }
    

    /// <summary>
    /// 产线V.
    /// </summary>
    public string production_type { get; set; }

    /// <summary>
    /// 开工日期V.
    /// </summary>
    public List<DateTime> start_date { get; set; }

    /// <summary>
    /// 完成品编码/型番V.
    /// </summary>
    public string product_code { get; set; }

    /// <summary>
    /// 完成品型号V.
    /// </summary>
    public string product_name { get; set; }

    /// <summary>
    /// 生产状态(0未生产 1生产中 2已完成).
    /// </summary>
    public int production_status { get; set; }

    /// <summary>
    /// 班次类型.
    /// </summary>
    public string class_type { get; set; }

}