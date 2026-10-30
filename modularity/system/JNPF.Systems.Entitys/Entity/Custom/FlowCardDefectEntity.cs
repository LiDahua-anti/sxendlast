using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 流程卡不良记录表（PDA生产过程中不良处理）.
    /// 记录每道工序产生的不良品：不良类型、数量、处理方式（报废/返工/让步接收）；
    /// 不良数量累计至工序流转，报废数量从流程卡可报工上限中扣减.
    /// </summary>
    [SugarTable("sx_flow_card_defect")]
    public class FlowCardDefectEntity : CLDSEntityBase
    {
        /// <summary>
        /// 流程卡ID（sx_flow_card.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_id", IsNullable = false, ColumnDescription = "/// 流程卡ID（sx_flow_card.F_ID）. ///")]
        [Description("流程卡ID")]
        public string FlowCardId { get; set; }

        /// <summary>
        /// 流程卡号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_no", IsNullable = true, ColumnDescription = "/// 流程卡号（冗余）. ///")]
        [Description("流程卡号")]
        public string FlowCardNo { get; set; }

        /// <summary>
        /// 流程卡工序流转ID（sx_flow_card_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "card_process_id", IsNullable = true, ColumnDescription = "/// 流程卡工序流转ID（sx_flow_card_process.F_ID）. ///")]
        [Description("工序流转ID")]
        public string CardProcessId { get; set; }

        /// <summary>
        /// 工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 工序ID（sx_base_process.F_ID）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序名称（冗余）. ///")]
        [Description("工序名称")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 不良类型：材料不良/加工不良/其他.
        /// </summary>
        [SugarColumn(ColumnName = "defect_type", IsNullable = true, ColumnDescription = "/// 不良类型：材料不良/加工不良/其他. ///")]
        [Description("不良类型")]
        public string DefectType { get; set; }

        /// <summary>
        /// 不良数量.
        /// </summary>
        [SugarColumn(ColumnName = "defect_qty", IsNullable = true, ColumnDescription = "/// 不良数量. ///")]
        [Description("不良数量")]
        public decimal? DefectQty { get; set; }

        /// <summary>
        /// 不良描述.
        /// </summary>
        [SugarColumn(ColumnName = "defect_description", IsNullable = true, ColumnDescription = "/// 不良描述. ///")]
        [Description("不良描述")]
        public string DefectDescription { get; set; }

        /// <summary>
        /// 处理方式：1报废 2返工 3让步接收.
        /// </summary>
        [SugarColumn(ColumnName = "disposition", IsNullable = true, ColumnDescription = "/// 处理方式：1报废 2返工 3让步接收. ///")]
        [Description("处理方式:1报废 2返工 3让步接收")]
        public int? Disposition { get; set; }

        /// <summary>
        /// 处理说明.
        /// </summary>
        [SugarColumn(ColumnName = "handle_remark", IsNullable = true, ColumnDescription = "/// 处理说明. ///")]
        [Description("处理说明")]
        public string HandleRemark { get; set; }

        /// <summary>
        /// 处理人姓名.
        /// </summary>
        [SugarColumn(ColumnName = "handler_name", IsNullable = true, ColumnDescription = "/// 处理人姓名. ///")]
        [Description("处理人姓名")]
        public string HandlerName { get; set; }

        /// <summary>
        /// 处理时间.
        /// </summary>
        [SugarColumn(ColumnName = "handle_time", IsNullable = true, ColumnDescription = "/// 处理时间. ///")]
        [Description("处理时间")]
        public DateTime? HandleTime { get; set; }

        /// <summary>
        /// 订单ID（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 订单ID（冗余）. ///")]
        [Description("订单ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 订单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = true, ColumnDescription = "/// 订单号（冗余）. ///")]
        [Description("订单号")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
