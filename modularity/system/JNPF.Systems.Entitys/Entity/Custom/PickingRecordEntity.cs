using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 领料记录表（仓库管理-生产领料，主数据）.
    /// PDA「领料生产」扫码产生，按先进先出取最早批次库位；
    /// 材料出库记录由此自动生成（out_stock_id 关联，不重复录入）。
    /// </summary>
    [SugarTable("sx_picking_record")]
    public class PickingRecordEntity : CLDSEntityBase
    {
        /// <summary>
        /// 领料单号（业务唯一，如LL20261010001）.
        /// </summary>
        [SugarColumn(ColumnName = "picking_no", IsNullable = false, ColumnDescription = "/// 领料单号（业务唯一，如LL20261010001）. ///")]
        [Description("领料单号")]
        public string PickingNo { get; set; }

        /// <summary>
        /// 生产订单ID.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 生产订单ID. ///")]
        [Description("生产订单ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 生产订单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = true, ColumnDescription = "/// 生产订单号（冗余）. ///")]
        [Description("生产订单号(冗余)")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 流程卡ID（sx_flow_card.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_id", IsNullable = true, ColumnDescription = "/// 流程卡ID（sx_flow_card.F_ID）. ///")]
        [Description("流程卡ID")]
        public string FlowCardId { get; set; }

        /// <summary>
        /// 流程卡号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_no", IsNullable = true, ColumnDescription = "/// 流程卡号（冗余）. ///")]
        [Description("流程卡号(冗余)")]
        public string FlowCardNo { get; set; }

        /// <summary>
        /// 工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 工序ID（sx_base_process.F_ID）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序（冗余）. ///")]
        [Description("工序(冗余)")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 物料ID（sx_inventory_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_id", IsNullable = true, ColumnDescription = "/// 物料ID（sx_inventory_master.F_ID）. ///")]
        [Description("物料ID")]
        public string InventoryId { get; set; }

        /// <summary>
        /// 物料编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_code", IsNullable = true, ColumnDescription = "/// 物料编码（冗余）. ///")]
        [Description("物料编码(冗余)")]
        public string InventoryCode { get; set; }

        /// <summary>
        /// 物料名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_name", IsNullable = true, ColumnDescription = "/// 物料名称（冗余）. ///")]
        [Description("物料名称(冗余)")]
        public string InventoryName { get; set; }

        /// <summary>
        /// 应发数量.
        /// </summary>
        [SugarColumn(ColumnName = "should_qty", IsNullable = true, ColumnDescription = "/// 应发数量. ///")]
        [Description("应发数量")]
        public decimal? ShouldQty { get; set; }

        /// <summary>
        /// 实发数量.
        /// </summary>
        [SugarColumn(ColumnName = "actual_qty", IsNullable = true, ColumnDescription = "/// 实发数量. ///")]
        [Description("实发数量")]
        public decimal? ActualQty { get; set; }

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
        [Description("仓库名称(冗余)")]
        public string WarehouseName { get; set; }

        /// <summary>
        /// 货位ID.
        /// </summary>
        [SugarColumn(ColumnName = "location_id", IsNullable = true, ColumnDescription = "/// 货位ID. ///")]
        [Description("货位ID")]
        public string LocationId { get; set; }

        /// <summary>
        /// 库位（先进先出取最早批次库位）.
        /// </summary>
        [SugarColumn(ColumnName = "location_code", IsNullable = true, ColumnDescription = "/// 库位（先进先出取最早批次库位）. ///")]
        [Description("库位")]
        public string LocationCode { get; set; }

        /// <summary>
        /// 批次号（最早入库批次）.
        /// </summary>
        [SugarColumn(ColumnName = "batch_no", IsNullable = true, ColumnDescription = "/// 批次号（最早入库批次）. ///")]
        [Description("批次号")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 领料人.
        /// </summary>
        [SugarColumn(ColumnName = "picker_name", IsNullable = true, ColumnDescription = "/// 领料人. ///")]
        [Description("领料人")]
        public string PickerName { get; set; }

        /// <summary>
        /// 领料时间.
        /// </summary>
        [SugarColumn(ColumnName = "pick_time", IsNullable = true, ColumnDescription = "/// 领料时间. ///")]
        [Description("领料时间")]
        public DateTime? PickTime { get; set; }

        /// <summary>
        /// 状态：1待审核 2已审核 3已作废.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1待审核 2已审核 3已作废. ///")]
        [Description("状态:1待审核 2已审核 3已作废")]
        public int? Status { get; set; }

        /// <summary>
        /// 关联材料出库记录ID（自动生成）.
        /// </summary>
        [SugarColumn(ColumnName = "out_stock_id", IsNullable = true, ColumnDescription = "/// 关联材料出库记录ID（自动生成）. ///")]
        [Description("关联材料出库记录ID")]
        public string OutStockId { get; set; }

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
