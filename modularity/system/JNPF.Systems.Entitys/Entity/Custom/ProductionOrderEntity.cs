using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 生产订单表（生产计划管理）.
    /// 状态流转：1待排产→2已排产→3已发布→4生产中→5待入库→6已完工；逾期7；
    /// 来源：用友U8同步或手工新增.
    /// </summary>
    [SugarTable("sx_production_order")]
    public class ProductionOrderEntity : CLDSEntityBase
    {
        /// <summary>
        /// 订单号（业务唯一,如MO202609001）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = false, ColumnDescription = "/// 订单号（业务唯一,如MO202609001）. ///")]
        [Description("订单号")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 图番（图纸编号,如XC01）.
        /// </summary>
        [SugarColumn(ColumnName = "drawing_no", IsNullable = true, ColumnDescription = "/// 图番（图纸编号,如XC01）. ///")]
        [Description("图番")]
        public string DrawingNo { get; set; }

        /// <summary>
        /// 产品名称.
        /// </summary>
        [SugarColumn(ColumnName = "product_name", IsNullable = true, ColumnDescription = "/// 产品名称. ///")]
        [Description("产品名称")]
        public string ProductName { get; set; }

        /// <summary>
        /// 计划数量.
        /// </summary>
        [SugarColumn(ColumnName = "plan_qty", IsNullable = true, ColumnDescription = "/// 计划数量. ///")]
        [Description("计划数量")]
        public decimal? PlanQty { get; set; }

        /// <summary>
        /// 单位（套/件）.
        /// </summary>
        [SugarColumn(ColumnName = "unit", IsNullable = true, ColumnDescription = "/// 单位（套/件）. ///")]
        [Description("单位")]
        public string Unit { get; set; }

        /// <summary>
        /// 当前工序（冗余,最新进度）.
        /// </summary>
        [SugarColumn(ColumnName = "current_process", IsNullable = true, ColumnDescription = "/// 当前工序（冗余,最新进度）. ///")]
        [Description("当前工序")]
        public string CurrentProcess { get; set; }

        /// <summary>
        /// 负责班组.
        /// </summary>
        [SugarColumn(ColumnName = "team_name", IsNullable = true, ColumnDescription = "/// 负责班组. ///")]
        [Description("负责班组")]
        public string TeamName { get; set; }

        /// <summary>
        /// 计划完工日期.
        /// </summary>
        [SugarColumn(ColumnName = "plan_finish_date", IsNullable = true, ColumnDescription = "/// 计划完工日期. ///")]
        [Description("计划完工日期")]
        public DateTime? PlanFinishDate { get; set; }

        /// <summary>
        /// 订单状态：1待排产 2已排产 3已发布 4生产中 5待入库 6已完工 7逾期.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 订单状态：1待排产 2已排产 3已发布 4生产中 5待入库 6已完工 7逾期. ///")]
        [Description("订单状态:1待排产 2已排产 3已发布 4生产中 5待入库 6已完工 7逾期")]
        public int? Status { get; set; }

        /// <summary>
        /// 发布状态：0未发布 1已发布.
        /// </summary>
        [SugarColumn(ColumnName = "publish_status", IsNullable = true, ColumnDescription = "/// 发布状态：0未发布 1已发布. ///")]
        [Description("发布状态:0未发布 1已发布")]
        public int? PublishStatus { get; set; }

        /// <summary>
        /// 来源：1用友U8同步 2手工新增.
        /// </summary>
        [SugarColumn(ColumnName = "source", IsNullable = true, ColumnDescription = "/// 来源：1用友U8同步 2手工新增. ///")]
        [Description("来源:1用友U8同步 2手工新增")]
        public int? Source { get; set; }

        /// <summary>
        /// 客户.
        /// </summary>
        [SugarColumn(ColumnName = "customer", IsNullable = true, ColumnDescription = "/// 客户. ///")]
        [Description("客户")]
        public string Customer { get; set; }

        /// <summary>
        /// 最近同步时间.
        /// </summary>
        [SugarColumn(ColumnName = "sync_time", IsNullable = true, ColumnDescription = "/// 最近同步时间. ///")]
        [Description("最近同步时间")]
        public DateTime? SyncTime { get; set; }

        /// <summary>
        /// 用友U8订单内码（同步映射用）.
        /// </summary>
        [SugarColumn(ColumnName = "u8_order_id", IsNullable = true, ColumnDescription = "/// 用友U8订单内码（同步映射用）. ///")]
        [Description("用友U8订单内码")]
        public string U8OrderId { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
