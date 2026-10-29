using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 流程卡表（生产制造管理）.
    /// 由到货单「打印流程卡」生成（原卡），生产过程中支持合并/拆分/重印；
    /// qty 为流程卡定量（勾稽上限）：Σ报工良品=Σ产成品入库=Σ销售/其他出库≤qty。
    /// </summary>
    [SugarTable("sx_flow_card")]
    public class FlowCardEntity : CLDSEntityBase
    {
        /// <summary>
        /// 流程卡号（业务唯一，如LCK0001）.
        /// </summary>
        [SugarColumn(ColumnName = "card_no", IsNullable = false, ColumnDescription = "/// 流程卡号（业务唯一，如LCK0001）. ///")]
        [Description("流程卡号")]
        public string CardNo { get; set; }

        /// <summary>
        /// 类型：1原卡 2合并卡 3拆分卡 4重印卡.
        /// </summary>
        [SugarColumn(ColumnName = "card_type", IsNullable = true, ColumnDescription = "/// 类型：1原卡 2合并卡 3拆分卡 4重印卡. ///")]
        [Description("类型:1原卡 2合并卡 3拆分卡 4重印卡")]
        public int? CardType { get; set; }

        /// <summary>
        /// 生产订单ID（sx_production_order.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 生产订单ID（sx_production_order.F_ID）. ///")]
        [Description("生产订单ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 订单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = true, ColumnDescription = "/// 订单号（冗余）. ///")]
        [Description("订单号(冗余)")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 到货单主表ID（可空）.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_id", IsNullable = true, ColumnDescription = "/// 到货单主表ID（可空）. ///")]
        [Description("到货单主表ID")]
        public string ArrivalId { get; set; }

        /// <summary>
        /// 到货单明细ID（可空）.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_detail_id", IsNullable = true, ColumnDescription = "/// 到货单明细ID（可空）. ///")]
        [Description("到货单明细ID")]
        public string ArrivalDetailId { get; set; }

        /// <summary>
        /// 产品（图番/产品名称）.
        /// </summary>
        [SugarColumn(ColumnName = "product_name", IsNullable = true, ColumnDescription = "/// 产品（图番/产品名称）. ///")]
        [Description("产品")]
        public string ProductName { get; set; }

        /// <summary>
        /// 图番.
        /// </summary>
        [SugarColumn(ColumnName = "drawing_no", IsNullable = true, ColumnDescription = "/// 图番. ///")]
        [Description("图番")]
        public string DrawingNo { get; set; }

        /// <summary>
        /// 数量（流程卡定量，勾稽上限）.
        /// </summary>
        [SugarColumn(ColumnName = "qty", IsNullable = true, ColumnDescription = "/// 数量（流程卡定量，勾稽上限）. ///")]
        [Description("数量(流程卡定量)")]
        public decimal? Qty { get; set; }

        /// <summary>
        /// 单位（套/件）.
        /// </summary>
        [SugarColumn(ColumnName = "unit", IsNullable = true, ColumnDescription = "/// 单位（套/件）. ///")]
        [Description("单位")]
        public string Unit { get; set; }

        /// <summary>
        /// 打印人.
        /// </summary>
        [SugarColumn(ColumnName = "print_user", IsNullable = true, ColumnDescription = "/// 打印人. ///")]
        [Description("打印人")]
        public string PrintUser { get; set; }

        /// <summary>
        /// 打印时间.
        /// </summary>
        [SugarColumn(ColumnName = "print_time", IsNullable = true, ColumnDescription = "/// 打印时间. ///")]
        [Description("打印时间")]
        public DateTime? PrintTime { get; set; }

        /// <summary>
        /// 状态：1未使用 2生产中 3已完工 4已入库 5作废.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1未使用 2生产中 3已完工 4已入库 5作废. ///")]
        [Description("状态:1未使用 2生产中 3已完工 4已入库 5作废")]
        public int? Status { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
