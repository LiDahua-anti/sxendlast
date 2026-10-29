using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 出库记录表（先进先出锁定最早批次）.
    /// 销售出库/其他出库/材料出库；先进先出自动锁定最早入库批次库位；
    /// 「材料出库」由PDA领料记录自动生成关联（picking_id），不重复录入.
    /// </summary>
    [SugarTable("sx_out_stock")]
    public class OutStockEntity : CLDSEntityBase
    {
        /// <summary>
        /// 出库单号（业务唯一，如CK20261010001）.
        /// </summary>
        [SugarColumn(ColumnName = "out_no", IsNullable = false, ColumnDescription = "/// 出库单号（业务唯一，如CK20261010001）. ///")]
        [Description("出库单号")]
        public string OutNo { get; set; }

        /// <summary>
        /// 出库类型：1销售出库 2其他出库 3材料出库.
        /// </summary>
        [SugarColumn(ColumnName = "out_type", IsNullable = true, ColumnDescription = "/// 出库类型：1销售出库 2其他出库 3材料出库. ///")]
        [Description("出库类型:1销售出库 2其他出库 3材料出库")]
        public int? OutType { get; set; }

        /// <summary>
        /// 关联生产订单ID（领料/产成品）.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 关联生产订单ID（领料/产成品）. ///")]
        [Description("关联生产订单ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 关联订单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = true, ColumnDescription = "/// 关联订单号（冗余）. ///")]
        [Description("关联订单号")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 流程卡ID.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_id", IsNullable = true, ColumnDescription = "/// 流程卡ID. ///")]
        [Description("流程卡ID")]
        public string FlowCardId { get; set; }

        /// <summary>
        /// 流程卡号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_no", IsNullable = true, ColumnDescription = "/// 流程卡号（冗余）. ///")]
        [Description("流程卡号")]
        public string FlowCardNo { get; set; }

        /// <summary>
        /// 关联领料记录ID（材料出库由领料自动生成，不重复录入）.
        /// </summary>
        [SugarColumn(ColumnName = "picking_id", IsNullable = true, ColumnDescription = "/// 关联领料记录ID（材料出库由领料自动生成，不重复录入）. ///")]
        [Description("关联领料记录ID")]
        public string PickingId { get; set; }

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
        /// 出库数量.
        /// </summary>
        [SugarColumn(ColumnName = "qty", IsNullable = true, ColumnDescription = "/// 出库数量. ///")]
        [Description("出库数量")]
        public decimal? Qty { get; set; }

        /// <summary>
        /// 单位.
        /// </summary>
        [SugarColumn(ColumnName = "unit", IsNullable = true, ColumnDescription = "/// 单位. ///")]
        [Description("单位")]
        public string Unit { get; set; }

        /// <summary>
        /// 仓库ID.
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_id", IsNullable = true, ColumnDescription = "/// 仓库ID. ///")]
        [Description("仓库ID")]
        public string WarehouseId { get; set; }

        /// <summary>
        /// 仓库名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_name", IsNullable = true, ColumnDescription = "/// 仓库名称（冗余）. ///")]
        [Description("仓库名称")]
        public string WarehouseName { get; set; }

        /// <summary>
        /// 货位ID（先进先出锁定）.
        /// </summary>
        [SugarColumn(ColumnName = "location_id", IsNullable = true, ColumnDescription = "/// 货位ID（先进先出锁定）. ///")]
        [Description("货位ID")]
        public string LocationId { get; set; }

        /// <summary>
        /// 库位（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "location_code", IsNullable = true, ColumnDescription = "/// 库位（冗余）. ///")]
        [Description("库位")]
        public string LocationCode { get; set; }

        /// <summary>
        /// 批次号（最早入库批次）.
        /// </summary>
        [SugarColumn(ColumnName = "batch_no", IsNullable = true, ColumnDescription = "/// 批次号（最早入库批次）. ///")]
        [Description("批次号")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 先进先出校验：1已通过.
        /// </summary>
        [SugarColumn(ColumnName = "fifo_check", IsNullable = true, ColumnDescription = "/// 先进先出校验：1已通过. ///")]
        [Description("先进先出校验:1已通过")]
        public int? FifoCheck { get; set; }

        /// <summary>
        /// 操作人.
        /// </summary>
        [SugarColumn(ColumnName = "operator_name", IsNullable = true, ColumnDescription = "/// 操作人. ///")]
        [Description("操作人")]
        public string OperatorName { get; set; }

        /// <summary>
        /// 出库时间.
        /// </summary>
        [SugarColumn(ColumnName = "out_time", IsNullable = true, ColumnDescription = "/// 出库时间. ///")]
        [Description("出库时间")]
        public DateTime? OutTime { get; set; }

        /// <summary>
        /// 状态：1待审核 2已出库.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1待审核 2已出库. ///")]
        [Description("状态:1待审核 2已出库")]
        public int? Status { get; set; }

        /// <summary>
        /// 用友同步状态：0待同步 1已同步 2同步失败.
        /// </summary>
        [SugarColumn(ColumnName = "sync_status", IsNullable = true, ColumnDescription = "/// 用友同步状态：0待同步 1已同步 2同步失败. ///")]
        [Description("用友同步状态:0待同步 1已同步 2同步失败")]
        public int? SyncStatus { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
