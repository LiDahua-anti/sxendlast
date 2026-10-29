using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 库存流水表（出入库实时更新）.
    /// 出入库审核后写入，记录变动前后数量，审核后推送用友变更现有量（模拟，见 sx_yonyou_sync_log）.
    /// </summary>
    [SugarTable("sx_stock_flow")]
    public class StockFlowEntity : CLDSEntityBase
    {
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
        /// 仓库名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_name", IsNullable = true, ColumnDescription = "/// 仓库名称（冗余）. ///")]
        [Description("仓库名称")]
        public string WarehouseName { get; set; }

        /// <summary>
        /// 货位编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "location_code", IsNullable = true, ColumnDescription = "/// 货位编码（冗余）. ///")]
        [Description("货位编码")]
        public string LocationCode { get; set; }

        /// <summary>
        /// 批次号.
        /// </summary>
        [SugarColumn(ColumnName = "batch_no", IsNullable = true, ColumnDescription = "/// 批次号. ///")]
        [Description("批次号")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 业务类型：1采购入库 2产成品入库 3其它入库 4销售出库 5其他出库 6领料出库.
        /// </summary>
        [SugarColumn(ColumnName = "biz_type", IsNullable = true, ColumnDescription = "/// 业务类型：1采购入库 2产成品入库 3其它入库 4销售出库 5其他出库 6领料出库. ///")]
        [Description("业务类型:1采购入库 2产成品入库 3其它入库 4销售出库 5其他出库 6领料出库")]
        public int? BizType { get; set; }

        /// <summary>
        /// 方向：1入库 2出库.
        /// </summary>
        [SugarColumn(ColumnName = "direction", IsNullable = true, ColumnDescription = "/// 方向：1入库 2出库. ///")]
        [Description("方向:1入库 2出库")]
        public int? Direction { get; set; }

        /// <summary>
        /// 变动数量.
        /// </summary>
        [SugarColumn(ColumnName = "qty", IsNullable = true, ColumnDescription = "/// 变动数量. ///")]
        [Description("变动数量")]
        public decimal? Qty { get; set; }

        /// <summary>
        /// 变动前在库数量.
        /// </summary>
        [SugarColumn(ColumnName = "before_qty", IsNullable = true, ColumnDescription = "/// 变动前在库数量. ///")]
        [Description("变动前在库数量")]
        public decimal? BeforeQty { get; set; }

        /// <summary>
        /// 变动后在库数量.
        /// </summary>
        [SugarColumn(ColumnName = "after_qty", IsNullable = true, ColumnDescription = "/// 变动后在库数量. ///")]
        [Description("变动后在库数量")]
        public decimal? AfterQty { get; set; }

        /// <summary>
        /// 关联业务单据ID（入库记录/出库记录/领料记录）.
        /// </summary>
        [SugarColumn(ColumnName = "biz_id", IsNullable = true, ColumnDescription = "/// 关联业务单据ID（入库记录/出库记录/领料记录）. ///")]
        [Description("关联业务单据ID")]
        public string BizId { get; set; }

        /// <summary>
        /// 关联业务单号（入库单号/出库单号/领料单号）.
        /// </summary>
        [SugarColumn(ColumnName = "biz_no", IsNullable = true, ColumnDescription = "/// 关联业务单号（入库单号/出库单号/领料单号）. ///")]
        [Description("关联业务单号")]
        public string BizNo { get; set; }

        /// <summary>
        /// 关联生产订单号（领料/产成品入库）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = true, ColumnDescription = "/// 关联生产订单号（领料/产成品入库）. ///")]
        [Description("关联生产订单号")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 关联流程卡号.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_no", IsNullable = true, ColumnDescription = "/// 关联流程卡号. ///")]
        [Description("关联流程卡号")]
        public string FlowCardNo { get; set; }

        /// <summary>
        /// 操作人.
        /// </summary>
        [SugarColumn(ColumnName = "operator_name", IsNullable = true, ColumnDescription = "/// 操作人. ///")]
        [Description("操作人")]
        public string OperatorName { get; set; }

        /// <summary>
        /// 操作时间.
        /// </summary>
        [SugarColumn(ColumnName = "operate_time", IsNullable = true, ColumnDescription = "/// 操作时间. ///")]
        [Description("操作时间")]
        public DateTime? OperateTime { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
