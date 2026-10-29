using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 报工明细表（生产制造-报工记录第3层级）.
    /// 多人同组报工按人数均分：组内每人一条记录（good_qty/work_hours 为均分后值）；
    /// 清洗工序按清洗合并码配置人数均分；member_snapshot 保留原人员快照可追溯.
    /// </summary>
    [SugarTable("sx_work_report")]
    public class WorkReportEntity : CLDSEntityBase
    {
        /// <summary>
        /// 流程卡工序流转ID（sx_flow_card_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "card_process_id", IsNullable = true, ColumnDescription = "/// 流程卡工序流转ID（sx_flow_card_process.F_ID）. ///")]
        [Description("流程卡工序流转ID")]
        public string CardProcessId { get; set; }

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
        /// 设备ID（sx_device_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "device_id", IsNullable = true, ColumnDescription = "/// 设备ID（sx_device_master.F_ID）. ///")]
        [Description("设备ID")]
        public string DeviceId { get; set; }

        /// <summary>
        /// 设备编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "device_code", IsNullable = true, ColumnDescription = "/// 设备编码（冗余）. ///")]
        [Description("设备编码")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 设备名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "device_name", IsNullable = true, ColumnDescription = "/// 设备名称（冗余）. ///")]
        [Description("设备名称")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 工位.
        /// </summary>
        [SugarColumn(ColumnName = "station", IsNullable = true, ColumnDescription = "/// 工位. ///")]
        [Description("工位")]
        public string Station { get; set; }

        /// <summary>
        /// 报工人ID（平台用户ID）.
        /// </summary>
        [SugarColumn(ColumnName = "worker_id", IsNullable = true, ColumnDescription = "/// 报工人ID（平台用户ID）. ///")]
        [Description("报工人ID")]
        public string WorkerId { get; set; }

        /// <summary>
        /// 报人工号.
        /// </summary>
        [SugarColumn(ColumnName = "worker_no", IsNullable = true, ColumnDescription = "/// 报人工号. ///")]
        [Description("报人工号")]
        public string WorkerNo { get; set; }

        /// <summary>
        /// 报工人姓名.
        /// </summary>
        [SugarColumn(ColumnName = "worker_name", IsNullable = true, ColumnDescription = "/// 报工人姓名. ///")]
        [Description("报工人姓名")]
        public string WorkerName { get; set; }

        /// <summary>
        /// 是否临时作业员：0否 1是.
        /// </summary>
        [SugarColumn(ColumnName = "is_temp_worker", IsNullable = true, ColumnDescription = "/// 是否临时作业员：0否 1是. ///")]
        [Description("是否临时作业员:0否 1是")]
        public int? IsTempWorker { get; set; }

        /// <summary>
        /// 报工批次号（同一次多人报工共享）.
        /// </summary>
        [SugarColumn(ColumnName = "group_no", IsNullable = true, ColumnDescription = "/// 报工批次号（同一次多人报工共享）. ///")]
        [Description("报工批次号")]
        public string GroupNo { get; set; }

        /// <summary>
        /// 批次报工总数量（均分前）.
        /// </summary>
        [SugarColumn(ColumnName = "group_qty", IsNullable = true, ColumnDescription = "/// 批次报工总数量（均分前）. ///")]
        [Description("批次报工总数量")]
        public decimal? GroupQty { get; set; }

        /// <summary>
        /// 批次数（均分人数）.
        /// </summary>
        [SugarColumn(ColumnName = "member_count", IsNullable = true, ColumnDescription = "/// 批次数（均分人数）. ///")]
        [Description("均分人数")]
        public int? MemberCount { get; set; }

        /// <summary>
        /// 良品数（本记录均分后计入）.
        /// </summary>
        [SugarColumn(ColumnName = "good_qty", IsNullable = true, ColumnDescription = "/// 良品数（本记录均分后计入）. ///")]
        [Description("良品数(均分后)")]
        public decimal? GoodQty { get; set; }

        /// <summary>
        /// 不良数.
        /// </summary>
        [SugarColumn(ColumnName = "bad_qty", IsNullable = true, ColumnDescription = "/// 不良数. ///")]
        [Description("不良数")]
        public decimal? BadQty { get; set; }

        /// <summary>
        /// 开始时间.
        /// </summary>
        [SugarColumn(ColumnName = "start_time", IsNullable = true, ColumnDescription = "/// 开始时间. ///")]
        [Description("开始时间")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间.
        /// </summary>
        [SugarColumn(ColumnName = "end_time", IsNullable = true, ColumnDescription = "/// 结束时间. ///")]
        [Description("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 工时（小时，均分后）.
        /// </summary>
        [SugarColumn(ColumnName = "work_hours", IsNullable = true, ColumnDescription = "/// 工时（小时，均分后）. ///")]
        [Description("工时(小时,均分后)")]
        public decimal? WorkHours { get; set; }

        /// <summary>
        /// 是否清洗合并报工：0否 1是（按清洗合并码配置均分）.
        /// </summary>
        [SugarColumn(ColumnName = "is_clean_merge", IsNullable = true, ColumnDescription = "/// 是否清洗合并报工：0否 1是（按清洗合并码配置均分）. ///")]
        [Description("是否清洗合并报工:0否 1是")]
        public int? IsCleanMerge { get; set; }

        /// <summary>
        /// 清洗合并码配置ID（历史报工保留原配置版本）.
        /// </summary>
        [SugarColumn(ColumnName = "merge_config_id", IsNullable = true, ColumnDescription = "/// 清洗合并码配置ID（历史报工保留原配置版本）. ///")]
        [Description("清洗合并码配置ID")]
        public string MergeConfigId { get; set; }

        /// <summary>
        /// 报工人员快照（配置变更后可追溯原人员，JSON）.
        /// </summary>
        [SugarColumn(ColumnName = "member_snapshot", IsNullable = true, ColumnDescription = "/// 报工人员快照（配置变更后可追溯原人员，JSON）. ///")]
        [Description("报工人员快照")]
        public string MemberSnapshot { get; set; }

        /// <summary>
        /// 状态：1有效 0作废（订单删除时标记作废保留历史）.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1有效 0作废（订单删除时标记作废保留历史）. ///")]
        [Description("状态:1有效 0作废")]
        public int? Status { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
