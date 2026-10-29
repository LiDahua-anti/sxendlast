using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Dto.Custom
{
    /// <summary>
    /// 维护计划新增入参
    /// </summary>
    public class MaintenancePlanInput
    {
        /// <summary>
        /// 计划id
        /// </summary>
        public string Id { get; set; }
     
        public string MaintenanceProjectId { get; set; }

        public string DeviceId { get; set; }

        [Required]
        public DateTime TaskDate { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

    }

    /// <summary>
    /// 维护计划复制入参
    /// </summary>
    public class MaintenancePlanCopyInput
    {
        /// <summary>
        /// 计划id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 复制日期
        /// </summary>
        [Required]
        public DateTime CopyDate { get; set; }

    }


    /// <summary>
    /// 维护计划查询入参
    /// </summary>
    public class MaintenancePlanQuery
    {
        /// <summary>
        /// 复制日期
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// 维护计划查询出
    /// </summary>
    public class MaintenancePlanQueryOutput
    {
        /// <summary>
        /// 日期
        /// </summary>
        public string Date { get; set; }

        public List<CalendarData> List { get; set; }
    }

    /// <summary>
    /// 维护计划查询出
    /// </summary>
    public class CalendarData
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Id{ get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string PlanName  { get; set; }

        /// <summary>
        /// 维护项目id
        /// </summary>
        public string MaintenanceProjectId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 任务日期
        /// </summary>
        public string TaskDate { get; set; }

        /// <summary>
        /// 任务开始时间
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 任务结束时间
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// 是否逾期（0 否  1是）
        /// </summary>
        public int? IsOverDue { get; set; }
    }

}
