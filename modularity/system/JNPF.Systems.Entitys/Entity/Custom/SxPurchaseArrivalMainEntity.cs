using JNPF.Common.CodeGenUpload;
using SqlSugar;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.Common.Contracts;

namespace JNPF.example.Entitys;

/// <summary>
/// sx_purchase_arrival_main实体.
/// </summary>
[SugarTable("sx_purchase_arrival_main")]
public class SxPurchaseArrivalMainEntity : CLDSEntityBase
{
    /// <summary>
    /// 业务类型.
    /// </summary>
    [SugarColumn(ColumnName = "business_type", ColumnDescription = "/// 业务类型. ///")]
    public string? Business_type { get; set; }

    /// <summary>
    /// 采购类型.
    /// </summary>
    [SugarColumn(ColumnName = "purchase_type", ColumnDescription = "/// 采购类型. ///")]
    public string? Purchase_type { get; set; }

    /// <summary>
    /// 到货日期.
    /// </summary>
    [SugarColumn(ColumnName = "arrival_date", ColumnDescription = "/// 到货日期. ///")]
    public string? Arrival_date { get; set; }

    /// <summary>
    /// 单据号.
    /// </summary>
    [SugarColumn(ColumnName = "bill_no", ColumnDescription = "/// 单据号. ///")]
    public string? Bill_no { get; set; }

    /// <summary>
    /// 供应商.
    /// </summary>
    [SugarColumn(ColumnName = "supplier_name", ColumnDescription = "/// 供应商. ///")]
    public string? Supplier_name { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    [SugarColumn(ColumnName = "purchase_remark", ColumnDescription = "/// 备注. ///")]
    public string? Purchase_remark { get; set; }

    /// <summary>
    /// 流程引擎ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID", ColumnDescription = "/// 流程引擎ID. ///")]
    public string FlowId { get; set; }
    
    /// <summary>
    /// 获取或设置 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id", IsPrimaryKey = true)]
    public string TenantId { get; set; }

    /// <summary>
    /// 到货单子表.
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SxPurchaseArrivalDetailEntity.Arrival_main_id), nameof(Id))]
    public List<SxPurchaseArrivalDetailEntity> SxPurchaseArrivalDetailList { get; set; }

}