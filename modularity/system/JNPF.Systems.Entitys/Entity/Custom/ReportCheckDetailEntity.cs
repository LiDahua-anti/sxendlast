using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 检包检验记录表（检查工序最后一步）.
    /// 末道检查工序报工时填写，材料不良4项/加工不良2项/返工5项；
    /// 检查合格后订单状态变为「待入库」。
    /// </summary>
    [SugarTable("sx_report_check_detail")]
    public class ReportCheckDetailEntity : CLDSEntityBase
    {
        /// <summary>
        /// 报工明细ID（sx_work_report.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "report_id", IsNullable = false, ColumnDescription = "/// 报工明细ID（sx_work_report.F_ID）. ///")]
        [Description("报工明细ID")]
        public string ReportId { get; set; }

        /// <summary>
        /// 流程卡ID（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_id", IsNullable = true, ColumnDescription = "/// 流程卡ID（冗余）. ///")]
        [Description("流程卡ID(冗余)")]
        public string FlowCardId { get; set; }

        /// <summary>
        /// 流程卡号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_no", IsNullable = true, ColumnDescription = "/// 流程卡号（冗余）. ///")]
        [Description("流程卡号(冗余)")]
        public string FlowCardNo { get; set; }

        /// <summary>
        /// 检验日期.
        /// </summary>
        [SugarColumn(ColumnName = "check_date", IsNullable = true, ColumnDescription = "/// 检验日期. ///")]
        [Description("检验日期")]
        public DateTime? CheckDate { get; set; }

        /// <summary>
        /// 工时.
        /// </summary>
        [SugarColumn(ColumnName = "work_hours", IsNullable = true, ColumnDescription = "/// 工时. ///")]
        [Description("工时")]
        public decimal? WorkHours { get; set; }

        /// <summary>
        /// 图番.
        /// </summary>
        [SugarColumn(ColumnName = "drawing_no", IsNullable = true, ColumnDescription = "/// 图番. ///")]
        [Description("图番")]
        public string DrawingNo { get; set; }

        /// <summary>
        /// 制造编号.
        /// </summary>
        [SugarColumn(ColumnName = "make_no", IsNullable = true, ColumnDescription = "/// 制造编号. ///")]
        [Description("制造编号")]
        public string MakeNo { get; set; }

        /// <summary>
        /// 检验内容.
        /// </summary>
        [SugarColumn(ColumnName = "check_content", IsNullable = true, ColumnDescription = "/// 检验内容. ///")]
        [Description("检验内容")]
        public string CheckContent { get; set; }

        /// <summary>
        /// 检验数量.
        /// </summary>
        [SugarColumn(ColumnName = "check_qty", IsNullable = true, ColumnDescription = "/// 检验数量. ///")]
        [Description("检验数量")]
        public decimal? CheckQty { get; set; }

        /// <summary>
        /// 良品数.
        /// </summary>
        [SugarColumn(ColumnName = "good_qty", IsNullable = true, ColumnDescription = "/// 良品数. ///")]
        [Description("良品数")]
        public decimal? GoodQty { get; set; }

        /// <summary>
        /// 材料不良-材料拉伤.
        /// </summary>
        [SugarColumn(ColumnName = "mat_bad_pull", IsNullable = true, ColumnDescription = "/// 材料不良-材料拉伤. ///")]
        [Description("材料不良-材料拉伤")]
        public decimal? MatBadPull { get; set; }

        /// <summary>
        /// 材料不良-气泡.
        /// </summary>
        [SugarColumn(ColumnName = "mat_bad_bubble", IsNullable = true, ColumnDescription = "/// 材料不良-气泡. ///")]
        [Description("材料不良-气泡")]
        public decimal? MatBadBubble { get; set; }

        /// <summary>
        /// 材料不良-接头.
        /// </summary>
        [SugarColumn(ColumnName = "mat_bad_joint", IsNullable = true, ColumnDescription = "/// 材料不良-接头. ///")]
        [Description("材料不良-接头")]
        public decimal? MatBadJoint { get; set; }

        /// <summary>
        /// 材料不良-厚度薄.
        /// </summary>
        [SugarColumn(ColumnName = "mat_bad_thin", IsNullable = true, ColumnDescription = "/// 材料不良-厚度薄. ///")]
        [Description("材料不良-厚度薄")]
        public decimal? MatBadThin { get; set; }

        /// <summary>
        /// 加工不良-伤.
        /// </summary>
        [SugarColumn(ColumnName = "proc_bad_injury", IsNullable = true, ColumnDescription = "/// 加工不良-伤. ///")]
        [Description("加工不良-伤")]
        public decimal? ProcBadInjury { get; set; }

        /// <summary>
        /// 加工不良-其它.
        /// </summary>
        [SugarColumn(ColumnName = "proc_bad_other", IsNullable = true, ColumnDescription = "/// 加工不良-其它. ///")]
        [Description("加工不良-其它")]
        public decimal? ProcBadOther { get; set; }

        /// <summary>
        /// 返工-返清洗.
        /// </summary>
        [SugarColumn(ColumnName = "rework_clean", IsNullable = true, ColumnDescription = "/// 返工-返清洗. ///")]
        [Description("返工-返清洗")]
        public decimal? ReworkClean { get; set; }

        /// <summary>
        /// 返工-返铣面.
        /// </summary>
        [SugarColumn(ColumnName = "rework_mill", IsNullable = true, ColumnDescription = "/// 返工-返铣面. ///")]
        [Description("返工-返铣面")]
        public decimal? ReworkMill { get; set; }

        /// <summary>
        /// 返工-返修品.
        /// </summary>
        [SugarColumn(ColumnName = "rework_repair", IsNullable = true, ColumnDescription = "/// 返工-返修品. ///")]
        [Description("返工-返修品")]
        public decimal? ReworkRepair { get; set; }

        /// <summary>
        /// 返工-修伤.
        /// </summary>
        [SugarColumn(ColumnName = "rework_fix", IsNullable = true, ColumnDescription = "/// 返工-修伤. ///")]
        [Description("返工-修伤")]
        public decimal? ReworkFix { get; set; }

        /// <summary>
        /// 返工-脏污.
        /// </summary>
        [SugarColumn(ColumnName = "rework_dirty", IsNullable = true, ColumnDescription = "/// 返工-脏污. ///")]
        [Description("返工-脏污")]
        public decimal? ReworkDirty { get; set; }

        /// <summary>
        /// 责任人.
        /// </summary>
        [SugarColumn(ColumnName = "duty_person", IsNullable = true, ColumnDescription = "/// 责任人. ///")]
        [Description("责任人")]
        public string DutyPerson { get; set; }

        /// <summary>
        /// 是否临时作业员：0否 1是.
        /// </summary>
        [SugarColumn(ColumnName = "is_temp_worker", IsNullable = true, ColumnDescription = "/// 是否临时作业员：0否 1是. ///")]
        [Description("是否临时作业员:0否 1是")]
        public int? IsTempWorker { get; set; }

        /// <summary>
        /// 操作员.
        /// </summary>
        [SugarColumn(ColumnName = "operator_name", IsNullable = true, ColumnDescription = "/// 操作员. ///")]
        [Description("操作员")]
        public string OperatorName { get; set; }

        /// <summary>
        /// 数量.
        /// </summary>
        [SugarColumn(ColumnName = "qty", IsNullable = true, ColumnDescription = "/// 数量. ///")]
        [Description("数量")]
        public decimal? Qty { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
