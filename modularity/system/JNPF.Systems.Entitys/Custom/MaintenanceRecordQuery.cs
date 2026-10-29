using JNPF.Common.Filter;
using JNPF.Systems.Entitys.Entity.Custom;
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
    /// 入参查询条件
    /// </summary>
    public class MaintenanceRecordQuery : PageInputBase
    {
        /// <summary>
        /// 设备编号
        /// </summary>
        public string? DeviceNo { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// 设备类型ID
        /// </summary>
        public string? DeviceTypeId { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string? TaskName { get; set; }

        /// <summary>
        /// 执行周期id
        /// </summary>
        public string? ExecutionCycleId { get; set; }
    }

    /// <summary>
    /// 查询返回输出参数
    /// </summary>
    public class MaintenanceRecordOutput
    {
        // 主记录信息
        public string? Id { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        public string? DeviceNo { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string? DeviceTypeName { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string? TaskName { get; set; }

        /// <summary>
        /// 执行开始
        /// </summary>
        public string ExecutionStartTime { get; set; }

        /// <summary>
        /// 执行结束时间
        /// </summary>
        public string ExecutionEndTime { get; set; }

        /// <summary>
        /// 用时时长（h）
        /// </summary>
        public string? DurationHours { get; set; }

        /// <summary>
        /// 任务开始时间
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 任务结束时间
        /// </summary>
        public string EndTime { get; set; }

        public int? PlanDurationHours { get; set; }

        /// <summary>
        /// 执行周期
        /// </summary>
        public string? ExecutionCycleName { get; set; }

        /// <summary>
        /// 执行方式
        /// </summary>
        public string? ExecutionMethodName { get; set; }

        /// <summary>
        /// 执行部位
        /// </summary>
        public string? ExecutionPartName { get; set; }

        /// <summary>
        /// 是否停机（0 否  1是）
        /// </summary>
        public int? IsShutdown { get; set; }

        /// <summary>
        /// 是否启用（0 否  1是）
        /// </summary>
        public int? EnabledMark { get; set; }

        /// <summary>
        /// 是否逾期（0 否  1是）
        /// </summary>
        public int? IsOverDue { get; set; }
    }

    /// <summary>
    /// byid返回参数
    /// </summary>
    public class MaintenanceRecordByIdOutput
    {
        /// <summary>
        /// 设备编号
        /// </summary>
        public string? DeviceNo { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string? DeviceTypeName { get; set; }

        public string? TaskName { get; set; }

        /// <summary>
        /// 执行周期
        /// </summary>
        public string? ExecutionCycleId { get; set; }

        /// <summary>
        /// 执行方式
        /// </summary>
        public string? ExecutionMethodId { get; set; }

        /// <summary>
        /// 执行部位
        /// </summary>
        public string? ExecutionPartId { get; set; }

        /// <summary>
        /// 是否停机（0 否  1是）
        /// </summary>
        public int? IsShutdown { get; set; }

        /// <summary>
        /// 是否启用（0 否  1是）
        /// </summary>
        public int? EnabledMark { get; set; }

        /// <summary>
        /// 任务开始时间
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 任务结束时间
        /// </summary>
        public string EndTime { get; set; }

        public int? PlanDurationHours { get; set; }



        // 主记录信息
        public string? Id { get; set; }

        public string MaintenancePlanId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 维护项目id
        /// </summary>
        public string MaintenanceProjectId { get; set; }

        /// <summary>
        /// 现场照片1
        /// </summary>
        public string? Photo1 { get; set; }

        /// <summary>
        /// 现场照片2
        /// </summary>
        public string? Photo2 { get; set; }

        /// <summary>
        /// 现场照片3
        /// </summary>
        public string? Photo3 { get; set; }

        /// <summary>
        /// 现场照片4
        /// </summary>
        public string? Photo4 { get; set; }

        /// <summary>
        /// 现场照片5
        /// </summary>
        public string? Photo5 { get; set; }

        /// <summary>
        /// 委外PM单位
        /// </summary>
        public string? OutsourcingPmCompany { get; set; }

        /// <summary>
        /// 委外PM费用
        /// </summary>
        public string? OutsourcingPmCost { get; set; }

        /// <summary>
        /// 执行开始时间
        /// </summary>
        public string? ExecutionStartTime { get; set; }

        /// <summary>
        /// 执行结束时间
        /// </summary>
        public string? ExecutionEndTime { get; set; }

        /// <summary>
        /// 用时时长（h）
        /// </summary>
        public string? DurationHours { get; set; }

        /// <summary>
        /// 停机时长（h）
        /// </summary>
        public string? DowntimeHours { get; set; }

        /// <summary>
        /// 是否异常（0-否,1-是）
        /// </summary>
        public int IsAbnormal { get; set; } = 0;

        /// <summary>
        /// 是否影响停机（0-否,1-是）
        /// </summary>
        public int AffectsDowntime { get; set; } = 0;
        /// <summary>
        /// 备件信息
        /// </summary>
        public List<SpareInfo> SpareDatas { get; set; }

        /// <summary>
        /// 报告记录
        /// </summary>
        //public List<ReportFileInfo> FileDatas { get; set; }


    }

    /// <summary>
    /// 添加编辑入参
    /// </summary>
    public class MaintenanceRecordInsertInput
    {
        // 主记录信息
        public string? Id { get; set; }

        public string MaintenancePlanId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 维护项目id
        /// </summary>
        public string MaintenanceProjectId { get; set; }

        /// <summary>
        /// 现场照片1
        /// </summary>
        public string? Photo1 { get; set; }

        /// <summary>
        /// 现场照片2
        /// </summary>
        public string? Photo2 { get; set; }

        /// <summary>
        /// 现场照片3
        /// </summary>
        public string? Photo3 { get; set; }

        /// <summary>
        /// 现场照片4
        /// </summary>
        public string? Photo4 { get; set; }

        /// <summary>
        /// 现场照片5
        /// </summary>
        public string? Photo5 { get; set; }

        /// <summary>
        /// 委外PM单位
        /// </summary>
        public string? OutsourcingPmCompany { get; set; }

        /// <summary>
        /// 委外PM费用
        /// </summary>
        public string? OutsourcingPmCost { get; set; }

        /// <summary>
        /// 执行开始时间
        /// </summary>
        public string? ExecutionStartTime { get; set; }

        /// <summary>
        /// 执行结束时间
        /// </summary>
        public string? ExecutionEndTime { get; set; }

        /// <summary>
        /// 用时时长（h）
        /// </summary>
        public string? DurationHours { get; set; }

        /// <summary>
        /// 停机时长（h）
        /// </summary>
        public string? DowntimeHours { get; set; }

        /// <summary>
        /// 是否异常（0-否,1-是）
        /// </summary>
        public int IsAbnormal { get; set; } = 0;

        /// <summary>
        /// 是否影响停机（0-否,1-是）
        /// </summary>
        public int AffectsDowntime { get; set; } = 0;

        /// <summary>
        /// 备件信息
        /// </summary>
        public List<SpareInfo> SpareDatas { get; set; }

        /// <summary>
        /// 报修文件
        /// </summary>
        public List<FileContentResult> fileContent { get; set; }
    }

    /// <summary>
    /// 备件信息
    /// </summary>
    public class SpareInfo
    {
        /// <summary>
        /// 备件id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// bomid
        /// </summary>
        public string BomId { get; set; }

        /// <summary>
        /// 使用数量
        /// </summary>
        public string Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public string Prices { get; set; }

        public string PartsManufacturer { get; set; }


        /// <summary>
        /// 库位
        /// </summary>
        //public string LocationId { get; set; }

        public string? PartsName { get; set; }
        //public string? LocationName { get; set; }

        /// <summary>
        /// 零部件型号
        /// </summary>
        public string? PartsModel { get; set; }

    }

    /// <summary>
    /// 文件信息
    /// </summary>
    public class ReportFileInfo
    {
        /// <summary>
        /// 文件id
        /// </summary>
        public string Id { get; set; }

        public string FilePath { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        public string FileName { get; set; }

        public virtual DateTime? CreatorTime { get; set; }

        /// <summary>
        /// 获取或设置 创建用户.
        /// </summary>
        public string? CreatorUserName { get; set; }

    }

    /// <summary>
    /// 委外报告返回结果
    /// </summary>
    public class RecordFileResult
    {
        /// <summary>
        /// 文件id
        /// </summary>
        public string Id { get; set; }

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
        public string FileSize { get; set; }

        /// <summary>
        /// 保存后的文件名
        /// </summary>
        public string FileName { get; set; }
        public DateTime? CreatorTime { get; set; }

        /// <summary>
        /// 获取或设置 创建用户.
        /// </summary>
        public string? CreatorUserName { get; set; }
    }



}
