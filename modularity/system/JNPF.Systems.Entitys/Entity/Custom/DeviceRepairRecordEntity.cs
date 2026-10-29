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
    /// <summary>
    /// 设备维修记录
    /// </summary>
    [SugarTable("sx_device_repair_record")]
    public class DeviceRepairRecordEntity : CLDSEntityBase
    {
        /// <summary>
        /// 维修单号
        /// </summary>
        [SugarColumn(ColumnName = "repair_record_no", IsNullable = true, ColumnDescription = "/// 维修单号 ///")]
        [Description("维修单号")]
        public string RepairRecordNo { get; set; }

        /// <summary>
        /// 故障分类id
        /// </summary>
        [SugarColumn(ColumnName = "fault_category_id", IsNullable = true, ColumnDescription = "/// 故障分类id ///")]
        [Description("故障分类id")]
        public string FaultCategoryId { get; set; }

        /// <summary>
        /// 故障部位
        /// </summary>
        [SugarColumn(ColumnName = "fault_location", IsNullable = true, ColumnDescription = "/// 故障部位 ///")]
        [Description("故障部位")]
        public string FaultLocation { get; set; }

        /// <summary>
        /// 故障原因
        /// </summary>
        [SugarColumn(ColumnName = "fault_reason", IsNullable = true, ColumnDescription = "/// 故障原因 ///")]
        [Description("故障原因")]
        public string FaultReason { get; set; }

        /// <summary>
        /// 维修状态id
        /// </summary>
        [SugarColumn(ColumnName = "repair_status_id", IsNullable = true, ColumnDescription = "/// 维修状态id ///")]
        [Description("维修状态id")]
        public string RepairStatusId { get; set; }

        /// <summary>
        /// 维修人id
        /// </summary>
        [SugarColumn(ColumnName = "repair_person_id", IsNullable = true, ColumnDescription = "/// 维修人id ///")]
        [Description("维修人id")]
        public string RepairPersonId { get; set; }

        /// <summary>
        /// 维修受理时间
        /// </summary>
        [SugarColumn(ColumnName = "accept_time", IsNullable = true, ColumnDescription = "/// 维修受理时间 ///")]
        [Description("维修受理时间")]
        public DateTime? AcceptTime { get; set; }

        /// <summary>
        /// 维修完成时间
        /// </summary>
        [SugarColumn(ColumnName = "complete_time", IsNullable = true, ColumnDescription = "/// 维修完成时间 ///")]
        [Description("维修完成时间")]
        public DateTime? CompleteTime { get; set; }

        /// <summary>
        /// 等待维修时长
        /// </summary>
        [SugarColumn(ColumnName = "waiting_duration", IsNullable = true, ColumnDescription = "/// 等待维修时长 ///")]
        [Description("等待维修时长")]
        public string? WaitingDuration { get; set; }

        /// <summary>
        /// 维修时长
        /// </summary>
        [SugarColumn(ColumnName = "repair_duration", IsNullable = true, ColumnDescription = "/// 维修时长 ///")]
        [Description("维修时长")]
        public string? RepairDuration { get; set; }

        /// <summary>
        /// 维修停机时长
        /// </summary>
        [SugarColumn(ColumnName = "downtime_duration", IsNullable = true, ColumnDescription = "/// 维修停机时长 ///")]
        [Description("维修停机时长")]
        public string? DowntimeDuration { get; set; }

        /// <summary>
        /// 维修费用
        /// </summary>
        [SugarColumn(ColumnName = "repair_cost", IsNullable = true, ColumnDescription = "/// 维修费用 ///")]
        [Description("维修费用")]
        public string? RepairCost { get; set; }

        /// <summary>
        /// 维修内容描述
        /// </summary>
        [SugarColumn(ColumnName = "repair_description", IsNullable = true, ColumnDescription = "/// 维修内容描述 ///")]
        [Description("维修内容描述")]
        public string RepairDescription { get; set; }

        /// <summary>
        /// 外委维修单位
        /// </summary>
        [SugarColumn(ColumnName = "outsourcing_company", IsNullable = true, ColumnDescription = "/// 外委维修单位 ///")]
        [Description("外委维修单位")]
        public string OutsourcingCompany { get; set; }

        /// <summary>
        /// 外委维修费用
        /// </summary>
        [SugarColumn(ColumnName = "outsourcing_cost", IsNullable = true, ColumnDescription = "/// 外委维修费用 ///")]
        [Description("外委维修费用")]
        public string? OutsourcingCost { get; set; }

        /// <summary>
        /// 综合事务分类id
        /// </summary>
        [SugarColumn(ColumnName = "general_affairs_category_id", IsNullable = true, ColumnDescription = "/// 综合事务分类id ///")]
        [Description("综合事务分类id")]
        public string GeneralAffairsCategoryId { get; set; }

        /// <summary>
        /// 是否停机维修(0 否  1是)
        /// </summary>
        [SugarColumn(ColumnName = "is_downtime_repair", IsNullable = false, ColumnDescription = "/// 是否停机维修(0 否 1是) ///")]
        [Description("是否停机维修(0 否  1是)")]
        public int IsDowntimeRepair { get; set; }

        /// <summary>
        /// 综合事务停机时长
        /// </summary>
        [SugarColumn(ColumnName = "general_affairs_downtime", IsNullable = true, ColumnDescription = "/// 综合事务停机时长 ///")]
        [Description("综合事务停机时长")]
        public string? GeneralAffairsDowntime { get; set; }

        /// <summary>
        /// 停机机种设备编号
        /// </summary>
        [SugarColumn(ColumnName = "downtime_device_no", IsNullable = true, ColumnDescription = "/// 停机机种设备编号 ///")]
        [Description("停机机种设备编号")]
        public string DowntimeDeviceNo { get; set; }

        /// <summary>
        /// 停机机种设备名称
        /// </summary>
        [SugarColumn(ColumnName = "downtime_device_name", IsNullable = true, ColumnDescription = "/// 停机机种设备名称 ///")]
        [Description("停机机种设备名称")]
        public string DowntimeDeviceName { get; set; }

        /// <summary>
        /// 报修repairId
        /// </summary>
        [SugarColumn(ColumnName = "repair_id", IsNullable = false, ColumnDescription = "/// 报修repairId ///")]
        [Description("报修repairId")]
        public string RepairId { get; set; }

    }

}
