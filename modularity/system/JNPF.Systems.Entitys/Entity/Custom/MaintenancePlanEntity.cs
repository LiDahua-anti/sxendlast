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
    [SugarTable("sx_maintenance_plan")]

    public class MaintenancePlanEntity : CLDSEntityBase
    {
        /// <summary>
        /// 维护项目id
        /// </summary>
        [SugarColumn(ColumnName = "maintenance_project_id", IsNullable = false, ColumnDescription = "/// 维护项目id ///")]
        [Description("维护项目id")]
        public string MaintenanceProjectId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        [SugarColumn(ColumnName = "device_id", IsNullable = false, ColumnDescription = "/// 设备id ///")]
        [Description("设备id")]
        public string DeviceId { get; set; }

        /// <summary>
        /// 任务日期
        /// </summary>
        [SugarColumn(ColumnName = "task_date", IsNullable = false, ColumnDescription = "/// 任务日期 ///")]
        [Description("任务日期")]
        public DateTime TaskDate { get; set; }

        /// <summary>
        /// 任务开始时间
        /// </summary>
        [SugarColumn(ColumnName = "start_time", IsNullable = false, ColumnDescription = "/// 任务开始时间 ///")]
        [Description("任务开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 任务结束时间
        /// </summary>
        [SugarColumn(ColumnName = "end_time", IsNullable = false, ColumnDescription = "/// 任务结束时间 ///")]
        [Description("任务结束时间")]
        public DateTime EndTime { get; set; }

    }
}
