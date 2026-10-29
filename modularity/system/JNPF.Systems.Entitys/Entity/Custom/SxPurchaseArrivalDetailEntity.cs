using JNPF.Common.CodeGenUpload;
using SqlSugar;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.Common.Contracts;

namespace JNPF.example.Entitys;

/// <summary>
/// 到货列表实体.
/// </summary>
[SugarTable("sx_purchase_arrival_detail")]
public class SxPurchaseArrivalDetailEntity : CLDSEntityBase
{
    /// <summary>
    /// 订单号.
    /// </summary>
    [SugarColumn(ColumnName = "order_no", ColumnDescription = "/// 订单号. ///")]
    public string? Order_no { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    [SugarColumn(ColumnName = "inventory_code", ColumnDescription = "/// 存货编码. ///")]
    public string? Inventory_code { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    [SugarColumn(ColumnName = "inventory_name", ColumnDescription = "/// 存货名称. ///")]
    public string? Inventory_name { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    [SugarColumn(ColumnName = "spec_model", ColumnDescription = "/// 规格型号. ///")]
    public string? Spec_model { get; set; }

    /// <summary>
    /// 主计量.
    /// </summary>
    [SugarColumn(ColumnName = "main_unit", ColumnDescription = "/// 主计量. ///")]
    public string? Main_unit { get; set; }

    /// <summary>
    /// 到货数量.
    /// </summary>
    [SugarColumn(ColumnName = "arrival_qty", IsNullable = true, ColumnDescription = "/// 到货数量. ///")]
    public decimal? Arrival_qty { get; set; }

    /// <summary>
    /// 流程卡打印次数.
    /// </summary>
    [SugarColumn(ColumnName = "print_count", IsNullable = true, ColumnDescription = "/// 流程卡打印次数. ///")]
    public int? Print_count { get; set; }

    /// <summary>
    /// 投产数量（总定量）.
    /// </summary>
    [SugarColumn(ColumnName = "total_qty", IsNullable = true, ColumnDescription = "/// 投产数量（总定量）. ///")]
    public decimal? Total_qty { get; set; }

    /// <summary>
    /// 总托数.
    /// </summary>
    [SugarColumn(ColumnName = "total_pallets", IsNullable = true, ColumnDescription = "/// 总托数. ///")]
    public int? Total_pallets { get; set; }

    /// <summary>
    /// 工作令号.
    /// </summary>
    [SugarColumn(ColumnName = "work_order_no", IsNullable = true, ColumnDescription = "/// 工作令号. ///")]
    public string? Work_order_no { get; set; }

    /// <summary>
    /// 制造编号.
    /// </summary>
    [SugarColumn(ColumnName = "make_no", IsNullable = true, ColumnDescription = "/// 制造编号. ///")]
    public string? Make_no { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    [SugarColumn(ColumnName = "type_text", IsNullable = true, ColumnDescription = "/// 类型. ///")]
    public string? Type_text { get; set; }

    /// <summary>
    /// 批号.
    /// </summary>
    [SugarColumn(ColumnName = "batch_no", ColumnDescription = "/// 批号. ///")]
    public string? Batch_no { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    [SugarColumn(ColumnName = "warehouse_name", ColumnDescription = "/// 仓库名称. ///")]
    public string? Warehouse_name { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    [SugarColumn(ColumnName = "detail_remark", ColumnDescription = "/// 备注. ///")]
    public string? Detail_remark { get; set; }

    /// <summary>
    /// 外键.
    /// </summary>
    [SugarColumn(ColumnName = "arrival_main_id", ColumnDescription = "/// 外键. ///")]
    public string? Arrival_main_id { get; set; }


    /// <summary>
    /// 获取或设置 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id", IsPrimaryKey = true)]
    public string TenantId { get; set; }

}