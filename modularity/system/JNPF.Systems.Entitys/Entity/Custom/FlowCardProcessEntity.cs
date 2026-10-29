using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 流程卡工序流转表（报工记录第2层级）.
    /// 流程卡下各工序的流转状态与完成数量；报工明细（sx_work_report）逐条累计至 complete_qty。
    /// </summary>
    [SugarTable("sx_flow_card_process")]
    public class FlowCardProcessEntity : CLDSEntityBase
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
        [Description("流程卡号(冗余)")]
        public string FlowCardNo { get; set; }

        /// <summary>
        /// 工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 工序ID（sx_base_process.F_ID）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_code", IsNullable = true, ColumnDescription = "/// 工序编码（冗余）. ///")]
        [Description("工序编码(冗余)")]
        public string ProcessCode { get; set; }

        /// <summary>
        /// 工序名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序名称（冗余）. ///")]
        [Description("工序名称(冗余)")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 工序顺序号.
        /// </summary>
        [SugarColumn(ColumnName = "seq_no", IsNullable = true, ColumnDescription = "/// 工序顺序号. ///")]
        [Description("工序顺序号")]
        public int? SeqNo { get; set; }

        /// <summary>
        /// 是否检查工序（最后一步检查）：0否 1是.
        /// </summary>
        [SugarColumn(ColumnName = "is_check_process", IsNullable = true, ColumnDescription = "/// 是否检查工序（最后一步检查）：0否 1是. ///")]
        [Description("是否检查工序:0否 1是")]
        public int? IsCheckProcess { get; set; }

        /// <summary>
        /// 状态：1未开始 2生产中 3已完成.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1未开始 2生产中 3已完成. ///")]
        [Description("状态:1未开始 2生产中 3已完成")]
        public int? Status { get; set; }

        /// <summary>
        /// 目标数量（流程卡定量）.
        /// </summary>
        [SugarColumn(ColumnName = "target_qty", IsNullable = true, ColumnDescription = "/// 目标数量（流程卡定量）. ///")]
        [Description("目标数量")]
        public decimal? TargetQty { get; set; }

        /// <summary>
        /// 完成数量（Σ报工良品）.
        /// </summary>
        [SugarColumn(ColumnName = "complete_qty", IsNullable = true, ColumnDescription = "/// 完成数量（Σ报工良品）. ///")]
        [Description("完成数量(Σ报工良品)")]
        public decimal? CompleteQty { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
