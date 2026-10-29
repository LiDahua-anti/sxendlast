using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 订单操作审计日志表（生产计划管理）.
    /// 排产保存/发布/删除清理的操作快照（JSON），删除影响范围可追溯。
    /// </summary>
    [SugarTable("sx_production_schedule_log")]
    public class ProductionScheduleLogEntity : CLDSEntityBase
    {
        /// <summary>
        /// 订单ID.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = false, ColumnDescription = "/// 订单ID. ///")]
        [Description("订单ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 订单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = true, ColumnDescription = "/// 订单号（冗余）. ///")]
        [Description("订单号(冗余)")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 操作类型：1排产保存 2发布 3删除清理.
        /// </summary>
        [SugarColumn(ColumnName = "operate_type", IsNullable = true, ColumnDescription = "/// 操作类型：1排产保存 2发布 3删除清理. ///")]
        [Description("操作类型:1排产保存 2发布 3删除清理")]
        public int? OperateType { get; set; }

        /// <summary>
        /// 操作快照内容（JSON）.
        /// </summary>
        [SugarColumn(ColumnName = "content", IsNullable = true, ColumnDescription = "/// 操作快照内容（JSON）. ///")]
        [Description("操作快照内容(JSON)")]
        public string Content { get; set; }
    }
}
