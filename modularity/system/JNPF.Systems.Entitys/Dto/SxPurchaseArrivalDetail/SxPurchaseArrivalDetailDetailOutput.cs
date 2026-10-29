namespace JNPF.example.Entitys.Dto.SxPurchaseArrivalDetail;

/// <summary>
/// 到货列表详情输出参数.
/// </summary>
public class SxPurchaseArrivalDetailDetailOutput
{
    /// <summary>
    /// 订单号.
    /// </summary>
    public string? order_no { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string? inventory_code { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string? inventory_name { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string? spec_model { get; set; }

    /// <summary>
    /// 主计量.
    /// </summary>
    public string? main_unit { get; set; }

    /// <summary>
    /// 到货数量.
    /// </summary>
    public decimal? arrival_qty { get; set; }

    /// <summary>
    /// 流程卡打印次数.
    /// </summary>
    public int? print_count { get; set; }

    /// <summary>
    /// 投产数量（总定量）.
    /// </summary>
    public decimal? total_qty { get; set; }

    /// <summary>
    /// 总托数.
    /// </summary>
    public int? total_pallets { get; set; }

    /// <summary>
    /// 工作令号.
    /// </summary>
    public string? work_order_no { get; set; }

    /// <summary>
    /// 制造编号.
    /// </summary>
    public string? make_no { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string? type_text { get; set; }

    /// <summary>
    /// 批号.
    /// </summary>
    public string? batch_no { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string? warehouse_name { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string? detail_remark { get; set; }

    /// <summary>
    /// 外键.
    /// </summary>
    public string? arrival_main_id { get; set; }

}