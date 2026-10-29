using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 库存台账表（仓库管理-MES实时库存）.
    /// 批次独立建档（同存货多批次多条记录）；可用量=在库-占用；
    /// 出库先进先出按入库时间（in_time）排序锁定最早批次库位.
    /// </summary>
    [SugarTable("sx_inventory_stock")]
    public class InventoryStockEntity : CLDSEntityBase
    {
        /// <summary>
        /// 存货ID（sx_inventory_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_id", IsNullable = false, ColumnDescription = "/// 存货ID（sx_inventory_master.F_ID）. ///")]
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
        /// 仓库ID（sx_warehouse.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_id", IsNullable = true, ColumnDescription = "/// 仓库ID（sx_warehouse.F_ID）. ///")]
        [Description("仓库ID")]
        public string WarehouseId { get; set; }

        /// <summary>
        /// 仓库编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_code", IsNullable = true, ColumnDescription = "/// 仓库编码（冗余）. ///")]
        [Description("仓库编码")]
        public string WarehouseCode { get; set; }

        /// <summary>
        /// 仓库名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_name", IsNullable = true, ColumnDescription = "/// 仓库名称（冗余）. ///")]
        [Description("仓库名称")]
        public string WarehouseName { get; set; }

        /// <summary>
        /// 货位ID（sx_warehouse_location.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "location_id", IsNullable = true, ColumnDescription = "/// 货位ID（sx_warehouse_location.F_ID）. ///")]
        [Description("货位ID")]
        public string LocationId { get; set; }

        /// <summary>
        /// 货位编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "location_code", IsNullable = true, ColumnDescription = "/// 货位编码（冗余）. ///")]
        [Description("货位编码")]
        public string LocationCode { get; set; }

        /// <summary>
        /// 批次号（同存货多批次独立建档）.
        /// </summary>
        [SugarColumn(ColumnName = "batch_no", IsNullable = true, ColumnDescription = "/// 批次号（同存货多批次独立建档）. ///")]
        [Description("批次号")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 在库数量.
        /// </summary>
        [SugarColumn(ColumnName = "stock_qty", IsNullable = true, ColumnDescription = "/// 在库数量. ///")]
        [Description("在库数量")]
        public decimal? StockQty { get; set; }

        /// <summary>
        /// 占用数量（可用量=在库-占用）.
        /// </summary>
        [SugarColumn(ColumnName = "occupied_qty", IsNullable = true, ColumnDescription = "/// 占用数量（可用量=在库-占用）. ///")]
        [Description("占用数量")]
        public decimal? OccupiedQty { get; set; }

        /// <summary>
        /// 安全库存（低于此值预警）.
        /// </summary>
        [SugarColumn(ColumnName = "safe_qty", IsNullable = true, ColumnDescription = "/// 安全库存（低于此值预警）. ///")]
        [Description("安全库存")]
        public decimal? SafeQty { get; set; }

        /// <summary>
        /// 入库时间（先进先出排序依据）.
        /// </summary>
        [SugarColumn(ColumnName = "in_time", IsNullable = true, ColumnDescription = "/// 入库时间（先进先出排序依据）. ///")]
        [Description("入库时间")]
        public DateTime? InTime { get; set; }

        /// <summary>
        /// 生产日期.
        /// </summary>
        [SugarColumn(ColumnName = "produce_date", IsNullable = true, ColumnDescription = "/// 生产日期. ///")]
        [Description("生产日期")]
        public DateTime? ProduceDate { get; set; }

        /// <summary>
        /// 状态：1正常 0冻结.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1正常 0冻结. ///")]
        [Description("状态:1正常 0冻结")]
        public int? Status { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
