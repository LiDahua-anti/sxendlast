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
    [SugarTable("sx_maintenance_projects")]
    public class MaintenanceProjectsEntity : CLDSEntityBase
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        [SugarColumn(ColumnName = "task_name", IsNullable = true, ColumnDescription = "/// 任务名称 ///")]
        [Description("任务名称")]
        public string? TaskName { get; set; }

        /// <summary>
        /// 执行周期id
        /// </summary>
        [SugarColumn(ColumnName = "execution_cycle_id", IsNullable = true, ColumnDescription = "/// 执行周期id ///")]
        [Description("执行周期id")]
        public string? ExecutionCycleId { get; set; }

        /// <summary>
        /// 任务时长（h）
        /// </summary>
        [SugarColumn(ColumnName = "task_duration", IsNullable = true, ColumnDescription = "/// 任务时长（h） ///")]
        [Description("任务时长（h）")]
        public string? TaskDuration { get; set; }

        /// <summary>
        /// 执行方式id
        /// </summary>
        [SugarColumn(ColumnName = "execution_method_id", IsNullable = true, ColumnDescription = "/// 执行方式id ///")]
        [Description("执行方式id")]
        public string? ExecutionMethodId { get; set; }

        /// <summary>
        /// 执行部位id
        /// </summary>
        [SugarColumn(ColumnName = "execution_part_id", IsNullable = true, ColumnDescription = "/// 执行部位id ///")]
        [Description("执行部位id")]
        public string? ExecutionPartId { get; set; }

        /// <summary>
        /// 是否停机（0 否  1是）
        /// </summary>
        [SugarColumn(ColumnName = "is_shutdown", IsNullable = true, ColumnDescription = "/// 是否停机（0 否 1是） ///")]
        [Description("是否停机（0-是,1-否）")]
        public int? IsShutdown { get; set; }

    }
}
