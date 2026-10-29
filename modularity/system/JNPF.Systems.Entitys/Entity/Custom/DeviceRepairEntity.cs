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
    [SugarTable("sx_device_repair")]
    public class DeviceRepairEntity : CLDSEntityBase
    {
        /// <summary>
        /// 报修单号
        /// </summary>
        [SugarColumn(ColumnName = "repair_no", IsNullable = true, ColumnDescription = "/// 报修单号 ///")]
        [Description("报修单号")]
        public string RepairNo { get; set; }

        /// <summary>
        /// 故障发生时间
        /// </summary>
        [SugarColumn(ColumnName = "fault_occur_time", IsNullable = true, ColumnDescription = "/// 故障发生时间 ///")]
        [Description("故障发生时间")]
        public string? FaultOccurTime { get; set; }

        /// <summary>
        /// 申请时间
        /// </summary>
        [SugarColumn(ColumnName = "application_time", IsNullable = true, ColumnDescription = "/// 申请时间 ///")]
        [Description("申请时间")]
        public string? ApplicationTime { get; set; }

        /// <summary>
        /// 申请部门id
        /// </summary>
        [SugarColumn(ColumnName = "application_department_id", IsNullable = true, ColumnDescription = "/// 申请部门id ///")]
        [Description("申请部门id")]
        public string? ApplicationDepartmentId { get; set; }

        /// <summary>
        /// 申请人id
        /// </summary>
        [SugarColumn(ColumnName = "applicant_id", IsNullable = true, ColumnDescription = "/// 申请人id ///")]
        [Description("申请人id")]
        public string? ApplicantId { get; set; }

        /// <summary>
        /// 使用场所id
        /// </summary>
        [SugarColumn(ColumnName = "usage_location_id", IsNullable = true, ColumnDescription = "/// 使用场所id ///")]
        [Description("使用场所id")]
        public string? UsageLocationId { get; set; }

        /// <summary>
        /// 故障紧急程度id
        /// </summary>
        [SugarColumn(ColumnName = "fault_urgency_level_id", IsNullable = true, ColumnDescription = "/// 故障紧急程度id ///")]
        [Description("故障紧急程度id")]
        public string? FaultUrgencyLevelId { get; set; }

        /// <summary>
        /// 是否停机故障（0-否,1-是）
        /// </summary>
        [SugarColumn(ColumnName = "is_shutdown_fault", IsNullable = true, ColumnDescription = "/// 是否停机故障（0-否,1-是） ///")]
        [Description("是否停机故障（0-否,1-是）")]
        public int? IsShutdownFault { get; set; }

        /// <summary>
        /// 使用部门id
        /// </summary>
        [SugarColumn(ColumnName = "usage_department_id", IsNullable = true, ColumnDescription = "/// 使用部门id ///")]
        [Description("使用部门id")]
        public string? UsageDepartmentId { get; set; }

        /// <summary>
        /// 故障描述
        /// </summary>
        [SugarColumn(ColumnName = "fault_description", IsNullable = true, ColumnDescription = "/// 故障描述 ///")]
        [Description("故障描述")]
        public string? FaultDescription { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        [SugarColumn(ColumnName = "images", IsNullable = true, ColumnDescription = "/// 图片 ///")]
        [Description("图片")]
        public string? Images { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        [SugarColumn(ColumnName = "device_id", IsNullable = true, ColumnDescription = "/// 设备id ///")]
        [Description("设备id")]
        public string DeviceId { get; set; }

    }
}
