using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 质量检验（OQC抽检）主表.
    /// PDA「品质抽检」扫码（员工码+流程卡+工序选项卡CNC/清洗/检包/入库）产生；
    /// 口径区分：本表=抽检（按批抽样判定），与报工记录末道工序全检（检包检验记录）两套并行；
    /// 判定合格后订单状态变为「待入库」。
    /// </summary>
    [SugarTable("sx_oqc")]
    public class OqcEntity : CLDSEntityBase
    {
        /// <summary>
        /// 抽检单号（业务唯一，如OQC20261010001）.
        /// </summary>
        [SugarColumn(ColumnName = "check_no", IsNullable = false, ColumnDescription = "/// 抽检单号（业务唯一，如OQC20261010001）. ///")]
        [Description("抽检单号")]
        public string CheckNo { get; set; }

        /// <summary>
        /// 流程卡ID（sx_flow_card.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_id", IsNullable = true, ColumnDescription = "/// 流程卡ID（sx_flow_card.F_ID）. ///")]
        [Description("流程卡ID")]
        public string FlowCardId { get; set; }

        /// <summary>
        /// 流程卡号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "flow_card_no", IsNullable = true, ColumnDescription = "/// 流程卡号（冗余）. ///")]
        [Description("流程卡号(冗余)")]
        public string FlowCardNo { get; set; }

        /// <summary>
        /// 订单ID（sx_production_order.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 订单ID（sx_production_order.F_ID）. ///")]
        [Description("订单ID")]
        public string OrderId { get; set; }

        /// <summary>
        /// 订单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "order_no", IsNullable = true, ColumnDescription = "/// 订单号（冗余）. ///")]
        [Description("订单号(冗余)")]
        public string OrderNo { get; set; }

        /// <summary>
        /// 工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 工序ID（sx_base_process.F_ID）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序（CNC/清洗/检包/入库）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序（CNC/清洗/检包/入库）. ///")]
        [Description("工序(CNC/清洗/检包/入库)")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 检验类型：1全检 2抽检（本表默认抽检，按批抽样）.
        /// </summary>
        [SugarColumn(ColumnName = "check_type", IsNullable = true, ColumnDescription = "/// 检验类型：1全检 2抽检（本表默认抽检，按批抽样）. ///")]
        [Description("检验类型:1全检 2抽检")]
        public int? CheckType { get; set; }

        /// <summary>
        /// 检查项目摘要（如：基准 OK / 厚度 OK）.
        /// </summary>
        [SugarColumn(ColumnName = "check_items_summary", IsNullable = true, ColumnDescription = "/// 检查项目摘要（如：基准 OK / 厚度 OK）. ///")]
        [Description("检查项目摘要")]
        public string CheckItemsSummary { get; set; }

        /// <summary>
        /// 抽检数（按批抽样）.
        /// </summary>
        [SugarColumn(ColumnName = "sample_qty", IsNullable = true, ColumnDescription = "/// 抽检数（按批抽样）. ///")]
        [Description("抽检数(按批抽样)")]
        public decimal? SampleQty { get; set; }

        /// <summary>
        /// 不良数.
        /// </summary>
        [SugarColumn(ColumnName = "bad_qty", IsNullable = true, ColumnDescription = "/// 不良数. ///")]
        [Description("不良数")]
        public decimal? BadQty { get; set; }

        /// <summary>
        /// 判定：1合格 2不合格.
        /// </summary>
        [SugarColumn(ColumnName = "check_result", IsNullable = true, ColumnDescription = "/// 判定：1合格 2不合格. ///")]
        [Description("判定:1合格 2不合格")]
        public int? CheckResult { get; set; }

        /// <summary>
        /// 质检员.
        /// </summary>
        [SugarColumn(ColumnName = "checker_name", IsNullable = true, ColumnDescription = "/// 质检员. ///")]
        [Description("质检员")]
        public string CheckerName { get; set; }

        /// <summary>
        /// 抽检时间.
        /// </summary>
        [SugarColumn(ColumnName = "check_time", IsNullable = true, ColumnDescription = "/// 抽检时间. ///")]
        [Description("抽检时间")]
        public DateTime? CheckTime { get; set; }

        /// <summary>
        /// 关联报工记录ID（sx_work_report.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "report_id", IsNullable = true, ColumnDescription = "/// 关联报工记录ID（sx_work_report.F_ID）. ///")]
        [Description("关联报工记录ID")]
        public string ReportId { get; set; }

        /// <summary>
        /// 不良处理状态：0无需处理 1待处理 2处理中 3已处理.
        /// </summary>
        [SugarColumn(ColumnName = "bad_handle_status", IsNullable = true, ColumnDescription = "/// 不良处理状态：0无需处理 1待处理 2处理中 3已处理. ///")]
        [Description("不良处理状态:0无需处理 1待处理 2处理中 3已处理")]
        public int? BadHandleStatus { get; set; }

        /// <summary>
        /// 不良处理说明.
        /// </summary>
        [SugarColumn(ColumnName = "bad_handle_remark", IsNullable = true, ColumnDescription = "/// 不良处理说明. ///")]
        [Description("不良处理说明")]
        public string BadHandleRemark { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
