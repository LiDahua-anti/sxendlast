using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 出库单主表（单据管理）.
    /// PDA扫码出库后自动生成，分销售出库、材料出库（生产领料）、其他出库三类；
    /// 出库遵循先入先出原则，审核后推送用友（模拟）.
    /// </summary>
    [SugarTable("sx_out_order")]
    public class OutOrderEntity : CLDSEntityBase
    {
        /// <summary>
        /// 出库单号（业务唯一）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = false, ColumnDescription = "/// 出库单号（业务唯一）. ///")]
        [Description("出库单号")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 出库类型：1销售出库 2其他出库 3材料出库.
        /// </summary>
        [SugarColumn(ColumnName = "out_type", IsNullable = true, ColumnDescription = "/// 出库类型：1销售出库 2其他出库 3材料出库. ///")]
        [Description("出库类型:1销售出库 2其他出库 3材料出库")]
        public int? OutType { get; set; }

        /// <summary>
        /// 客户（销售出库）.
        /// </summary>
        [SugarColumn(ColumnName = "customer", IsNullable = true, ColumnDescription = "/// 客户（销售出库）. ///")]
        [Description("客户")]
        public string Customer { get; set; }

        /// <summary>
        /// 关联生产订单ID.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 关联生产订单ID. ///")]
        [Description("关联生产订单ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 关联生产订单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no_ref", IsNullable = true, ColumnDescription = "/// 关联生产订单号（冗余）. ///")]
        [Description("关联生产订单号")]
        public string OrderNoRef { get; set; }

        /// <summary>
        /// 流程卡号.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_no", IsNullable = true, ColumnDescription = "/// 流程卡号. ///")]
        [Description("流程卡号")]
        public string FlowCardNo { get; set; }

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
        /// 状态：1草稿 2待审核 3已出库 4已作废.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1草稿 2待审核 3已出库 4已作废. ///")]
        [Description("状态:1草稿 2待审核 3已出库 4已作废")]
        public int? Status { get; set; }

        /// <summary>
        /// 审核人.
        /// </summary>
        [SugarColumn(ColumnName = "audit_user", IsNullable = true, ColumnDescription = "/// 审核人. ///")]
        [Description("审核人")]
        public string AuditUser { get; set; }

        /// <summary>
        /// 审核时间.
        /// </summary>
        [SugarColumn(ColumnName = "audit_time", IsNullable = true, ColumnDescription = "/// 审核时间. ///")]
        [Description("审核时间")]
        public DateTime? AuditTime { get; set; }

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
