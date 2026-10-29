using JNPF.Common.Filter;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Dto.Custom
{
    /// <summary>
    /// 列表查询
    /// </summary>
    public class DeviceRecordsQuery : PageInputBase
    {
        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceNo { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 维修状态id (下拉框)
        /// </summary>
        public string RepairStatusId { get; set; }

        /// <summary>
        /// 维修完成开始时间
        /// </summary>
        public string CompleteStartTime { get; set; }


        /// <summary>
        /// 维修完成结束时间
        /// </summary>
        public string CompleteEndTime { get; set; }

    }

    /// <summary>
    /// 列表查询
    /// </summary>
    public class DeviceRecordsOutput
    {
        public string Id { get; set; }

        public string DeviceNo { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 发生时间--repair
        /// </summary>
        public string? FaultOccurTime { get; set; }

        /// <summary>
        /// 报修时间
        /// </summary>
        public string? ApplicationTime { get; set; }

        /// <summary>
        /// 报修人
        /// </summary>
        public string? ApplicantName { get; set; }

        /// <summary>
        /// 报修单号
        /// </summary>
        public string RepairNo { get; set; }

        /// <summary>
        /// 维修单号
        /// </summary>
        public string RepairRecordNo { get; set; }

        /// <summary>
        /// 故障紧急程度Name
        /// </summary>
        public string? FaultUrgencyLevelName { get; set; }

        /// <summary>
        /// 维修状态Name
        /// </summary>
        public string RepairStatusName { get; set; }

        /// <summary>
        /// 维修人name
        /// </summary>
        public string RepairPersonName { get; set; }

        /// <summary>
        /// 维修完成时间
        /// </summary>
        public string? CompleteTime { get; set; }

        /// <summary>
        /// 维修费用
        /// </summary>
        public string? RepairCost { get; set; }

        /// <summary>
        /// 是否停机故障（0-否,1-是）
        /// </summary>
        public int? IsShutdownFault { get; set; }

    }

    /// <summary>
    ///  ById返回参数
    /// </summary>
    public class DeviceRecordsByIdOutput
    {
        public string Id { get; set; }

        /// <summary>
        /// 设备单号
        /// </summary>
        public string DeviceNo { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 故障报修单号
        /// </summary>
        public string RepairNo { get; set; }

        /// <summary>
        /// 故障发生时间
        /// </summary>
        public string? FaultOccurTime { get; set; }

        /// <summary>
        /// 申请时间
        /// </summary>
        public string? ApplicationTime { get; set; }

        /// <summary>
        /// 申请部门id
        /// </summary>
        public string? ApplicationDepartmentId { get; set; }

        /// <summary>
        /// 申请人id
        /// </summary>
        public string? ApplicantId { get; set; }

        /// <summary>
        /// 故障紧急程度id
        /// </summary>
        public string? FaultUrgencyLevelId { get; set; }

        /// <summary>
        /// 是否停机故障（0-否,1-是）
        /// </summary>
        public int? IsShutdownFault { get; set; }

        /// <summary>
        /// 故障描述
        /// </summary>
        public string? FaultDescription { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 维修单号
        /// </summary>
        public string RepairRecordNo { get; set; }

        /// <summary>
        /// 故障分类id
        /// </summary>
        public string FaultCategoryId { get; set; }

        /// <summary>
        /// 故障部位
        /// </summary>
        public string FaultLocation { get; set; }

        /// <summary>
        /// 故障原因
        /// </summary>
        public string FaultReason { get; set; }

        /// <summary>
        /// 维修状态id
        /// </summary>
        public string RepairStatusId { get; set; }

        /// <summary>
        /// 维修人id
        /// </summary>
        public string RepairPersonId { get; set; }

        /// <summary>
        /// 维修受理时间
        /// </summary>
        public string? AcceptTime { get; set; }

        /// <summary>
        /// 维修完成时间
        /// </summary>
        public string? CompleteTime { get; set; }

        /// <summary>
        /// 等待维修时长
        /// </summary>
        public string? WaitingDuration { get; set; }

        /// <summary>
        /// 维修时长
        /// </summary>
        public string? RepairDuration { get; set; }

        /// <summary>
        /// 维修停机时长
        /// </summary>
        public string? DowntimeDuration { get; set; }

        /// <summary>
        /// 维修费用
        /// </summary>
        public string? RepairCost { get; set; }

        /// <summary>
        /// 维修描述内容
        /// </summary>
        public string RepairDescription { get; set; }

        /// <summary>
        /// 外委维修费用
        /// </summary>
        public string? OutsourcingCost { get; set; }

        /// <summary>
        /// 综合事务分类id
        /// </summary>
        public string GeneralAffairsCategoryId { get; set; }

        /// <summary>
        /// 是否停机维修(0 否  1是)
        /// </summary>
        public int IsDowntimeRepair { get; set; }

        /// <summary>
        /// 综合事务停机时长
        /// </summary>
        public string? GeneralAffairsDowntime { get; set; }

        /// <summary>
        /// 停机机种设备编号
        /// </summary>
        public string DowntimeDeviceNo { get; set; }

        /// <summary>
        /// 停机机种设备名称
        /// </summary>
        public string DowntimeDeviceName { get; set; }

        /// <summary>
        /// 备件信息
        /// </summary>
        public List<SpareInfo> SpareDatas { get; set; }
    }

    /// <summary>
    /// 编辑入参
    /// </summary>
    public class DeviceRecordsInput
    {
        public string Id { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 维修单号
        /// </summary>
        public string RepairRecordNo { get; set; }

        /// <summary>
        /// 故障分类id
        /// </summary>
        public string FaultCategoryId { get; set; }

        /// <summary>
        /// 故障部位
        /// </summary>
        public string FaultLocation { get; set; }

        /// <summary>
        /// 故障原因
        /// </summary>
        public string FaultReason { get; set; }

        /// <summary>
        /// 维修状态id
        /// </summary>
        public string RepairStatusId { get; set; }

        /// <summary>
        /// 维修人id
        /// </summary>
        public string RepairPersonId { get; set; }

        /// <summary>
        /// 维修受理时间
        /// </summary>
        public string? AcceptTime { get; set; }

        /// <summary>
        /// 维修完成时间
        /// </summary>
        public string? CompleteTime { get; set; }

        /// <summary>
        /// 等待维修时长
        /// </summary>
        public string? WaitingDuration { get; set; }

        /// <summary>
        /// 维修时长
        /// </summary>
        public string? RepairDuration { get; set; }

        /// <summary>
        /// 维修停机时长
        /// </summary>
        public string? DowntimeDuration { get; set; }

        /// <summary>
        /// 维修费用
        /// </summary>
        public string? RepairCost { get; set; }

        /// <summary>
        /// 维修描述内容
        /// </summary>
        public string RepairDescription { get; set; }

        /// <summary>
        /// 外委维修费用
        /// </summary>
        public string? OutsourcingCost { get; set; }

        /// <summary>
        /// 综合事务分类id
        /// </summary>
        public string GeneralAffairsCategoryId { get; set; }

        /// <summary>
        /// 是否停机维修(0 否  1是)
        /// </summary>
        public int IsDowntimeRepair { get; set; }

        /// <summary>
        /// 综合事务停机时长
        /// </summary>
        public string? GeneralAffairsDowntime { get; set; }

        /// <summary>
        /// 停机机种设备编号
        /// </summary>
        public string DowntimeDeviceNo { get; set; }

        /// <summary>
        /// 停机机种设备名称
        /// </summary>
        public string DowntimeDeviceName { get; set; }

        /// <summary>
        /// 备件信息
        /// </summary>
        public List<SpareInfo> SpareDatas { get; set; }

        /// <summary>
        /// 报修文件
        /// </summary>
        public List<FileContentResult> fileContent { get; set; }

    }

}
