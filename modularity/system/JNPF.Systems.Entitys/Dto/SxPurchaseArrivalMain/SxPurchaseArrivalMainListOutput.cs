namespace JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;

/// <summary>
/// sx_purchase_arrival_main输入参数.
/// </summary>
public class SxPurchaseArrivalMainListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 业务类型.
    /// </summary>
    public string? business_type { get; set; }

    /// <summary>
    /// 采购类型.
    /// </summary>
    public string? purchase_type { get; set; }

    /// <summary>
    /// 到货日期.
    /// </summary>
    public string arrival_date { get; set; }

    /// <summary>
    /// 单据号.
    /// </summary>
    public string? bill_no { get; set; }

    /// <summary>
    /// 供应商.
    /// </summary>
    public string? supplier_name { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string? purchase_remark { get; set; }

    /// <summary>
    /// 流程卡打印状态：0未打印 1部分打印 2已打印.
    /// </summary>
    public int? flow_card_print_status { get; set; }

    /// <summary>
    /// 流程卡打印状态名称.
    /// </summary>
    public string? flow_card_print_status_text { get; set; }

    /// <summary>
    /// 打印进度文本（如 2/3）.
    /// </summary>
    public string? flow_card_printed_text { get; set; }

}