using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 入库单主表（单据管理）.
    /// PDA扫码入库后自动生成入库单，分采购入库、产成品入库、其他入库三类；
    /// 审核后自动推送用友U8生成对应入库凭证（模拟）.
    /// </summary>
    [SugarTable("sx_in_order")]
    public class InOrderEntity : CLDSEntityBase
    {
        /// <summary>
        /// 入库单号（业务唯一）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = false, ColumnDescription = "/// 入库单号（业务唯一）. ///")]
        [Description("入库单号")]
        public string OrderNo { get; set; }

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
        /// 到货单ID.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_id", IsNullable = true, ColumnDescription = "/// 到货单ID. ///")]
        [Description("到货单ID")]
        public string ArrivalId { get; set; }

        /// <summary>
        /// 到货单号.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_no", IsNullable = true, ColumnDescription = "/// 到货单号. ///")]
        [Description("到货单号")]
        public string ArrivalNo { get; set; }

        /// <summary>
        /// 供应商（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "supplier_name", IsNullable = true, ColumnDescription = "/// 供应商（冗余）. ///")]
        [Description("供应商")]
        public string SupplierName { get; set; }

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
        /// 状态：1草稿 2待审核 3已入库 4已作废.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1草稿 2待审核 3已入库 4已作废. ///")]
        [Description("状态:1草稿 2待审核 3已入库 4已作废")]
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
