using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    [SugarTable("sx_maintenance_record")]
    public class MaintenanceRecordEntity : CLDSEntityBase
    {
        /// <summary>
        /// 维护计划plan id
        /// </summary>
        [SugarColumn(ColumnName = "maintenance_plan_id", IsNullable = false, ColumnDescription = "/// 维护计划plan id ///")]
        [Description("维护计划plan id")]
        public string MaintenancePlanId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        [SugarColumn(ColumnName = "device_id", IsNullable = false, ColumnDescription = "/// 设备id ///")]
        [Description("设备id")]
        public string DeviceId { get; set; }

        /// <summary>
        /// 维护项目id
        /// </summary>
        [SugarColumn(ColumnName = "maintenance_project_id", IsNullable = false, ColumnDescription = "/// 维护项目id ///")]
        [Description("维护项目id")]
        public string MaintenanceProjectId { get; set; }

        /// <summary>
        /// 现场照片1
        /// </summary>
        [SugarColumn(ColumnName = "photo_1", IsNullable = true, ColumnDescription = "/// 现场照片1 ///")]
        [Description("现场照片1")]
        public string? Photo1 { get; set; }

        /// <summary>
        /// 现场照片2
        /// </summary>
        [SugarColumn(ColumnName = "photo_2", IsNullable = true, ColumnDescription = "/// 现场照片2 ///")]
        [Description("现场照片2")]
        public string? Photo2 { get; set; }

        /// <summary>
        /// 现场照片3
        /// </summary>
        [SugarColumn(ColumnName = "photo_3", IsNullable = true, ColumnDescription = "/// 现场照片3 ///")]
        [Description("现场照片3")]
        public string? Photo3 { get; set; }

        /// <summary>
        /// 现场照片4
        /// </summary>
        [SugarColumn(ColumnName = "photo_4", IsNullable = true, ColumnDescription = "/// 现场照片4 ///")]
        [Description("现场照片4")]
        public string? Photo4 { get; set; }

        /// <summary>
        /// 现场照片5
        /// </summary>
        [SugarColumn(ColumnName = "photo_5", IsNullable = true, ColumnDescription = "/// 现场照片5 ///")]
        [Description("现场照片5")]
        public string? Photo5 { get; set; }

        /// <summary>
        /// 委外PM单位
        /// </summary>
        [SugarColumn(ColumnName = "outsourcing_pm_company", IsNullable = true, ColumnDescription = "/// 委外PM单位 ///")]
        [Description("委外PM单位")]
        public string? OutsourcingPmCompany { get; set; }

        /// <summary>
        /// 委外PM费用
        /// </summary>
        [SugarColumn(ColumnName = "outsourcing_pm_cost", IsNullable = true, ColumnDescription = "/// 委外PM费用 ///")]
        [Description("委外PM费用")]
        public string? OutsourcingPmCost { get; set; }

        /// <summary>
        /// 执行开始时间
        /// </summary>
        [SugarColumn(ColumnName = "execution_start_time", IsNullable = true, ColumnDescription = "/// 执行开始时间 ///")]
        [Description("执行开始时间")]
        public DateTime? ExecutionStartTime { get; set; }

        /// <summary>
        /// 执行结束时间
        /// </summary>
        [SugarColumn(ColumnName = "execution_end_time", IsNullable = true, ColumnDescription = "/// 执行结束时间 ///")]
        [Description("执行结束时间")]
        public DateTime? ExecutionEndTime { get; set; }

        /// <summary>
        /// 用时时长（h）
        /// </summary>
        [SugarColumn(ColumnName = "duration_hours", IsNullable = true, ColumnDescription = "/// 用时时长（h） ///")]
        [Description("用时时长（h）")]
        public string? DurationHours { get; set; }

        /// <summary>
        /// 停机时长（h）
        /// </summary>
        [SugarColumn(ColumnName = "downtime_hours", IsNullable = true, ColumnDescription = "/// 停机时长（h） ///")]
        [Description("停机时长（h）")]
        public string? DowntimeHours { get; set; }

        /// <summary>
        /// 是否异常（0-否,1-是）
        /// </summary>
        [SugarColumn(ColumnName = "is_abnormal", IsNullable = false, ColumnDescription = "/// 是否异常（0-否,1-是） ///")]
        [Description("是否异常（0-否,1-是）")]
        public int IsAbnormal { get; set; } = 0;

        /// <summary>
        /// 是否影响停机（0-否,1-是）
        /// </summary>
        [SugarColumn(ColumnName = "affects_downtime", IsNullable = false, ColumnDescription = "/// 是否影响停机（0-否,1-是） ///")]
        [Description("是否影响停机（0-否,1-是）")]
        public int AffectsDowntime { get; set; } = 0;

        /// <summary>
        /// 备件是否确认（0-否,1-是）
        /// </summary>
        [SugarColumn(ColumnName = "spare_status", IsNullable = false, ColumnDescription = "/// 备件是否确认（0-否,1-是） ///")]
        [Description("备件是否确认（0-否,1-是）")]
        public int SpareStatus { get; set; } = 0;
    }
}
