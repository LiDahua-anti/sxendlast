namespace JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;

/// <summary>
/// 新增单张流程卡入参.
/// </summary>
public class ArrivalGenerateFlowCardInput
{
    /// <summary>
    /// 到货单主表ID.
    /// </summary>
    public string ArrivalId { get; set; }

    /// <summary>
    /// 到货明细ID.
    /// </summary>
    public string ArrivalDetailId { get; set; }
}

/// <summary>
/// 新增单张流程卡输出.
/// </summary>
public class ArrivalGenerateFlowCardOutput
{
    /// <summary>
    /// 流程卡ID.
    /// </summary>
    public string CardId { get; set; }

    /// <summary>
    /// 流程卡号（全局唯一，考虑历史卡号）.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 提示信息.
    /// </summary>
    public string Message { get; set; }
}
