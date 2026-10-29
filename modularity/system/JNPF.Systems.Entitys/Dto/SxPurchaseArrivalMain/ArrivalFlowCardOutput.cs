namespace JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;

/// <summary>
/// 到货明细打印流程卡输出.
/// </summary>
public class ArrivalFlowCardPrintOutput
{
    /// <summary>
    /// 到货单主表ID.
    /// </summary>
    public string ArrivalId { get; set; }

    /// <summary>
    /// 到货明细ID.
    /// </summary>
    public string ArrivalDetailId { get; set; }

    /// <summary>
    /// 现有流程卡ID集合（含已投产）.
    /// </summary>
    public List<string> CardIds { get; set; }

    /// <summary>
    /// 现有流程卡号集合（含已投产）.
    /// </summary>
    public List<string> CardNos { get; set; }

    /// <summary>
    /// 现有流程卡总数.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已投产（使用中）流程卡数.
    /// </summary>
    public int UsedCount { get; set; }

    /// <summary>
    /// 提示信息.
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// 到货流程卡单卡明细输出.
/// </summary>
public class ArrivalFlowCardItemOutput
{
    /// <summary>
    /// 流程卡ID.
    /// </summary>
    public string CardId { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 流程卡定量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 状态：1未使用 2生产中 3已完工 4已入库 5作废.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 状态名称.
    /// </summary>
    public string StatusText { get; set; }

    /// <summary>
    /// 打印人.
    /// </summary>
    public string PrintUser { get; set; }

    /// <summary>
    /// 打印时间.
    /// </summary>
    public DateTime? PrintTime { get; set; }
}

/// <summary>
/// 到货明细流程卡状态输出.
/// </summary>
public class ArrivalFlowCardDetailStatusOutput
{
    /// <summary>
    /// 到货明细ID.
    /// </summary>
    public string ArrivalDetailId { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 已打印流程卡数（未作废）.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 已投产（使用中）流程卡数.
    /// </summary>
    public int UsedCount { get; set; }

    /// <summary>
    /// 流程卡号集合.
    /// </summary>
    public List<string> CardNos { get; set; }
}

/// <summary>
/// 到货单流程卡整体状态输出（列表「是否打印流程卡」列）.
/// </summary>
public class ArrivalFlowCardStatusOutput
{
    /// <summary>
    /// 到货单主表ID.
    /// </summary>
    public string ArrivalId { get; set; }

    /// <summary>
    /// 到货明细总行数.
    /// </summary>
    public int TotalDetailCount { get; set; }

    /// <summary>
    /// 已打印流程卡的明细行数.
    /// </summary>
    public int PrintedDetailCount { get; set; }

    /// <summary>
    /// 打印状态：0未打印 1部分打印 2已打印.
    /// </summary>
    public int PrintStatus { get; set; }

    /// <summary>
    /// 打印状态名称.
    /// </summary>
    public string PrintStatusText { get; set; }

    /// <summary>
    /// 已打印显示（如 2/2）.
    /// </summary>
    public string PrintedText { get; set; }

    /// <summary>
    /// 明细级状态.
    /// </summary>
    public List<ArrivalFlowCardDetailStatusOutput> Details { get; set; }
}

/// <summary>
/// 打印流程卡弹窗上下文输出.
/// 字段全部取自到货明细表（sx_purchase_arrival_detail）.
/// </summary>
public class ArrivalPrintContextOutput
{
    /// <summary>
    /// 到货单主表ID.
    /// </summary>
    public string ArrivalId { get; set; }

    /// <summary>
    /// 到货明细ID.
    /// </summary>
    public string ArrivalDetailId { get; set; }

    /// <summary>
    /// 图番（规格型号 spec_model）.
    /// </summary>
    public string DrawingNo { get; set; }

    /// <summary>
    /// 工作令号（detail.work_order_no）.
    /// </summary>
    public string WorkOrderNo { get; set; }

    /// <summary>
    /// 订单号（detail.order_no）.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 材料编号（存货编码 inventory_code）.
    /// </summary>
    public string MaterialCode { get; set; }

    /// <summary>
    /// 材料名称（存货名称 inventory_name）.
    /// </summary>
    public string MaterialName { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 预定纳期（主表到货日期）.
    /// </summary>
    public string PlanDate { get; set; }

    /// <summary>
    /// 制造编号（detail.make_no）.
    /// </summary>
    public string MakeNo { get; set; }

    /// <summary>
    /// 类型（detail.type_text）.
    /// </summary>
    public string TypeText { get; set; }

    /// <summary>
    /// 投产数量（detail.total_qty）.
    /// </summary>
    public decimal? TotalQty { get; set; }

    /// <summary>
    /// 总托数（detail.total_pallets）.
    /// </summary>
    public int? TotalPallets { get; set; }

    /// <summary>
    /// 卡列表.
    /// </summary>
    public List<ArrivalFlowCardItemOutput> Cards { get; set; }
}
