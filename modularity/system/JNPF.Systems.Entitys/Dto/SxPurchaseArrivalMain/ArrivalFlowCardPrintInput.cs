namespace JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;

/// <summary>
/// 到货明细打印流程卡输入.
/// </summary>
public class ArrivalFlowCardPrintInput
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
    /// 流程卡条数（含已投产卡；不足补印、多余删除）.
    /// </summary>
    public int CardCount { get; set; }

    /// <summary>
    /// 每张流程卡定量.
    /// </summary>
    public decimal QtyPerCard { get; set; }
}

/// <summary>
/// 删除一行流程卡输入.
/// </summary>
public class DeleteFlowCardInput
{
    /// <summary>
    /// 流程卡ID.
    /// </summary>
    public string CardId { get; set; }
}
