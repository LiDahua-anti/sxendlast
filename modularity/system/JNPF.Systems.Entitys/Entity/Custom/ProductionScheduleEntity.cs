using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 排产主表（订单 × 流程卡，生产计划管理）.
    /// 保存排产时强制校验「流程卡必选」；状态：1已保存 2已发布。
    /// </summary>
    [SugarTable("sx_production_schedule")]
    public class ProductionScheduleEntity : CLDSEntityBase
    {
        /// <summary>
        /// 生产订单ID（sx_production_order.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = false, ColumnDescription = "/// 生产订单ID（sx_production_order.F_ID）. ///")]
        [Description("生产订单ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 订单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = true, ColumnDescription = "/// 订单号（冗余）. ///")]
        [Description("订单号(冗余)")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 流程卡ID（sx_flow_card.F_ID，必填）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_id", IsNullable = false, ColumnDescription = "/// 流程卡ID（sx_flow_card.F_ID，必填）. ///")]
        [Description("流程卡ID(必填)")]
        public string FlowCardId { get; set; }

        /// <summary>
        /// 流程卡号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_no", IsNullable = true, ColumnDescription = "/// 流程卡号（冗余）. ///")]
        [Description("流程卡号(冗余)")]
        public string FlowCardNo { get; set; }

        /// <summary>
        /// 投产数量.
        /// </summary>
        [SugarColumn(ColumnName = "plan_qty", IsNullable = true, ColumnDescription = "/// 投产数量. ///")]
        [Description("投产数量")]
        public decimal? PlanQty { get; set; }

        /// <summary>
        /// 状态：1已保存 2已发布.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1已保存 2已发布. ///")]
        [Description("状态:1已保存 2已发布")]
        public int? Status { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
