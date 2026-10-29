using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 出库单明细表.
    /// </summary>
    [SugarTable("sx_out_order_detail")]
    public class OutOrderDetailEntity : CLDSEntityBase
    {
        /// <summary>
        /// 出库单主表ID（sx_out_order.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = false, ColumnDescription = "/// 出库单主表ID（sx_out_order.F_ID）. ///")]
        [Description("出库单主表ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 存货ID.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_id", IsNullable = true, ColumnDescription = "/// 存货ID. ///")]
        [Description("存货ID")]
        public string InventoryId { get; set; }

        /// <summary>
        /// 存货编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_code", IsNullable = true, ColumnDescription = "/// 存货编码（冗余）. ///")]
        [Description("存货编码")]
        public string InventoryCode { get; set; }

        /// <summary>
        /// 存货名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_name", IsNullable = true, ColumnDescription = "/// 存货名称（冗余）. ///")]
        [Description("存货名称")]
        public string InventoryName { get; set; }

        /// <summary>
        /// 规格型号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "spec", IsNullable = true, ColumnDescription = "/// 规格型号（冗余）. ///")]
        [Description("规格型号")]
        public string Spec { get; set; }

        /// <summary>
        /// 单位（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "unit", IsNullable = true, ColumnDescription = "/// 单位（冗余）. ///")]
        [Description("单位")]
        public string Unit { get; set; }

        /// <summary>
        /// 出库数量.
        /// </summary>
        [SugarColumn(ColumnName = "qty", IsNullable = true, ColumnDescription = "/// 出库数量. ///")]
        [Description("出库数量")]
        public decimal? Qty { get; set; }

        /// <summary>
        /// 批次号（先进先出）.
        /// </summary>
        [SugarColumn(ColumnName = "batch_no", IsNullable = true, ColumnDescription = "/// 批次号（先进先出）. ///")]
        [Description("批次号")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 库位.
        /// </summary>
        [SugarColumn(ColumnName = "location_code", IsNullable = true, ColumnDescription = "/// 库位. ///")]
        [Description("库位")]
        public string LocationCode { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
