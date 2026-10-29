using JNPF.example.Entitys.Dto.SxPurchaseArrivalDetail;
using JNPF.JsonSerialization;
using Newtonsoft.Json;
 
namespace JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;

/// <summary>
/// sx_purchase_arrival_main修改输入参数.
/// </summary>
public class SxPurchaseArrivalMainCrInput
{
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
    public string? arrival_date { get; set; }

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
    /// 到货单子表.
    /// </summary>
    public List<SxPurchaseArrivalDetailCrInput> tableField7c2932 { get; set; }

}