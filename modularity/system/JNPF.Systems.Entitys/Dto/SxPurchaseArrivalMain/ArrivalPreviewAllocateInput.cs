namespace JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;

/// <summary>
/// 打印预览分配入参（按总托数平均分配投产数量）.
/// </summary>
public class ArrivalPreviewAllocateInput
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
    /// 投产数量（总定量）.
    /// </summary>
    public decimal TotalQty { get; set; }

    /// <summary>
    /// 总托数.
    /// </summary>
    public int TotalPallets { get; set; }
}

/// <summary>
/// 打印预览分配单卡输出.
/// </summary>
public class ArrivalPreviewAllocateCardItem
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
    /// 本卡定量（平均分配，最后一卡补齐余数）.
    /// </summary>
    public decimal Qty { get; set; }
}

/// <summary>
/// 打印预览分配输出.
/// </summary>
public class ArrivalPreviewAllocateOutput
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
    /// 投产数量.
    /// </summary>
    public decimal TotalQty { get; set; }

    /// <summary>
    /// 总托数.
    /// </summary>
    public int TotalPallets { get; set; }

    /// <summary>
    /// 每卡平均定量.
    /// </summary>
    public decimal QtyPerCard { get; set; }

    /// <summary>
    /// 分配结果列表.
    /// </summary>
    public List<ArrivalPreviewAllocateCardItem> Cards { get; set; }
}
