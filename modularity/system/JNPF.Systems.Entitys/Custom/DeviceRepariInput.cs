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
    public class DeviceRepairQuery : PageInputBase
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
        /// 报修状态 (默认-1，（0  未处理  1已处理）
        /// </summary>
        public int? EnabledMark { get; set; }

        /// <summary>
        /// 故障发生开始时间
        /// </summary>
        public string? FaultOccurStartTime { get; set; }

        /// <summary>
        /// 故障发生结束时间
        /// </summary>
        public string? FaultOccurEndTime { get; set; }

        /// <summary>
        /// 申请开始时间
        /// </summary>
        public string? ApplicationStartTime { get; set; }

        /// <summary>
        /// 申请结束时间
        /// </summary>
        public string? ApplicationEndTime { get; set; }

    }

    /// <summary>
    /// 列表查询返回
    /// </summary>
    public class DeviceRepairOutput
    {
        public string Id { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceNo { get; set; }

        /// <summary>
        /// 固定资产编号
        /// </summary>
        public string AssetsNumber { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 设备厂家
        /// </summary>
        public string DeviceManufacturer { get; set; }

        /// <summary>
        /// 设备型号
        /// </summary>
        public string DeviceModel { get; set; }

        /// <summary>
        /// 供应厂家
        /// </summary>
        public string DeviceSupplier { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string? DeviceTypeName { get; set; }

        /// <summary>
        /// 使用场所
        /// </summary>
        public string UseAddress { get; set; }

        /// <summary>
        /// 使用部门
        /// </summary>
        public string UseDpt { get; set; }

        /// <summary>
        /// 报修时间
        /// </summary>
        public string ApplicationTime { get; set; }

        /// <summary>
        /// 报修状态
        /// </summary>
        public int? EnabledMark { get; set; }

        /// <summary>
        /// 是否停机故障（0-否,1-是）
        /// </summary>
        public int? IsShutdownFault { get; set; }

        /// <summary>
        /// 故障发生时间
        /// </summary>
        public string? FaultOccurTime { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        public string? Images { get; set; }

        /// <summary>
        /// 故障紧急程度id
        /// </summary>
        public string? FaultUrgencyLevelId { get; set; }
    }

    /// <summary>
    /// 导出列表查询返回
    /// </summary>
    public class DeviceRepairExportOutput
    {
        public string Id { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceNo { get; set; }

        /// <summary>
        /// 固定资产编号
        /// </summary>
        public string AssetsNumber { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 设备厂家
        /// </summary>
        public string DeviceManufacturer { get; set; }

        /// <summary>
        /// 设备型号
        /// </summary>
        public string DeviceModel { get; set; }

        /// <summary>
        /// 供应厂家
        /// </summary>
        public string DeviceSupplier { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string? DeviceTypeName { get; set; }

        /// <summary>
        /// 使用场所
        /// </summary>
        public string UseAddress { get; set; }

        /// <summary>
        /// 使用部门
        /// </summary>
        public string UseDpt { get; set; }

        /// <summary>
        /// 报修时间
        /// </summary>
        public string ApplicationTime { get; set; }

        /// <summary>
        /// 报修状态(默认-1，（0  未处理  1已处理）
        /// </summary>
        public int? EnabledMark { get; set; }

        /// <summary>
        /// 是否停机故障（0-否,1-是）
        /// </summary>
        public int? IsShutdownFault { get; set; }

        /// <summary>
        /// 故障发生时间
        /// </summary>
        public string? FaultOccurTime { get; set; }

        /// <summary>
        /// 故障紧急程度id
        /// </summary>
        public string? FaultUrgencyLevelId { get; set; }

        /// <summary>
        /// 报修单号
        /// </summary>
        public string RepairNo { get; set; }

        /// <summary>
        /// 申请报修人id
        /// </summary>
        public string? ApplicantId { get; set; }

        /// <summary>
        /// 指派人
        /// </summary>
        public string RepairPersonId { get; set; }

        /// <summary>
        /// 维修停机时长
        /// </summary>
        public string? DowntimeDuration { get; set; }


        /// <summary>
        /// 故障描述
        /// </summary>
        public string? FaultDescription { get; set; }

    }

    /// <summary>
    /// 报修入参
    /// </summary>
    public class DeviceRepariInput
    {
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
        /// 使用场所id
        /// </summary>
        public string? UsageLocationId { get; set; }

        /// <summary>
        /// 故障紧急程度id
        /// </summary>
        public string? FaultUrgencyLevelId { get; set; }

        /// <summary>
        /// 是否停机故障（0-否,1-是）
        /// </summary>
        public int? IsShutdownFault { get; set; }

        /// <summary>
        /// 使用部门id
        /// </summary>
        public string? UsageDepartmentId { get; set; }

        /// <summary>
        /// 故障描述
        /// </summary>
        public string? FaultDescription { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        public string? Images { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 报修文件
        /// </summary>
        public List<FileContentResult> fileContent { get; set; }

    }

    public class FileContentResult
    {
        /// <summary>
        /// 文件访问URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 保存后的文件名
        /// </summary>
        public string FileName { get; set; }
    }
}
