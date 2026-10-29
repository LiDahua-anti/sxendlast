using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 入库记录表（PDA出入库实时更新）.
    /// 采购入库/产成品入库/其他入库；统一按「批次建档 + 货位归位」处理；
    /// 数据校验：到货单号非空的入库单流程卡必填（到货必须先打印流程卡），
    /// 无到货单来源（盘盈/退料/其他）才允许无流程卡.
    /// </summary>
    [SugarTable("sx_stock_in")]
    public class StockInEntity : CLDSEntityBase
    {
        /// <summary>
        /// 入库单号（业务唯一，如RK20261010001）.
        /// </summary>
        [SugarColumn(ColumnName = "in_no", IsNullable = false, ColumnDescription = "/// 入库单号（业务唯一，如RK20261010001）. ///")]
        [Description("入库单号")]
        public string InNo { get; set; }

        /// <summary>
        /// 入库类型：1采购入库 2产成品入库 3其他入库.
        /// </summary>
        [SugarColumn(ColumnName = "in_type", IsNullable = true, ColumnDescription = "/// 入库类型：1采购入库 2产成品入库 3其他入库. ///")]
        [Description("入库类型:1采购入库 2产成品入库 3其他入库")]
        public int? InType { get; set; }

        /// <summary>
        /// 入库方式：1扫码流程卡入库 2直接入库.
        /// </summary>
        [SugarColumn(ColumnName = "in_mode", IsNullable = true, ColumnDescription = "/// 入库方式：1扫码流程卡入库 2直接入库. ///")]
        [Description("入库方式:1扫码流程卡入库 2直接入库")]
        public int? InMode { get; set; }

        /// <summary>
        /// 到货单ID（sx_sx_purchase_arrival_main.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_id", IsNullable = true, ColumnDescription = "/// 到货单ID（sx_sx_purchase_arrival_main.F_ID）. ///")]
        [Description("到货单ID")]
        public string ArrivalId { get; set; }

        /// <summary>
        /// 到货单号（非空时流程卡必填）.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_no", IsNullable = true, ColumnDescription = "/// 到货单号（非空时流程卡必填）. ///")]
        [Description("到货单号")]
        public string ArrivalNo { get; set; }

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
        [Description("流程卡号")]
        public string FlowCardNo { get; set; }

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
        /// 入库数量.
        /// </summary>
        [SugarColumn(ColumnName = "qty", IsNullable = true, ColumnDescription = "/// 入库数量. ///")]
        [Description("入库数量")]
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
        /// 货位ID（扫库位码落位）.
        /// </summary>
        [SugarColumn(ColumnName = "location_id", IsNullable = true, ColumnDescription = "/// 货位ID（扫库位码落位）. ///")]
        [Description("货位ID")]
        public string LocationId { get; set; }

        /// <summary>
        /// 货位编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "location_code", IsNullable = true, ColumnDescription = "/// 货位编码（冗余）. ///")]
        [Description("货位编码")]
        public string LocationCode { get; set; }

        /// <summary>
        /// 批次号（批次建档）.
        /// </summary>
        [SugarColumn(ColumnName = "batch_no", IsNullable = true, ColumnDescription = "/// 批次号（批次建档）. ///")]
        [Description("批次号")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 有无零头箱（产成品入库）：0无 1有.
        /// </summary>
        [SugarColumn(ColumnName = "has_odd_box", IsNullable = true, ColumnDescription = "/// 有无零头箱（产成品入库）：0无 1有. ///")]
        [Description("有无零头箱:0无 1有")]
        public int? HasOddBox { get; set; }

        /// <summary>
        /// 操作人.
        /// </summary>
        [SugarColumn(ColumnName = "operator_name", IsNullable = true, ColumnDescription = "/// 操作人. ///")]
        [Description("操作人")]
        public string OperatorName { get; set; }

        /// <summary>
        /// 入库时间.
        /// </summary>
        [SugarColumn(ColumnName = "in_time", IsNullable = true, ColumnDescription = "/// 入库时间. ///")]
        [Description("入库时间")]
        public DateTime? InTime { get; set; }

        /// <summary>
        /// 状态：1待审核 2已入库.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1待审核 2已入库. ///")]
        [Description("状态:1待审核 2已入库")]
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
