using JNPF.Common.Filter;

namespace JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;

/// <summary>
/// sx_purchase_arrival_main列表查询输入.
/// </summary>
public class SxPurchaseArrivalMainListQueryInput : PageInputBase
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
    /// 业务类型.
    /// </summary>
    public string business_type { get; set; }

    /// <summary>
    /// 到货日期.
    /// </summary>
    public List<DateTime> arrival_date { get; set; }

}