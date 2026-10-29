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
    public class MaintenanceProjectInput
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// 执行周期id
        /// </summary>
        public string ExecutionCycleId { get; set; }

        /// <summary>
        /// 执行方式id
        /// </summary>
        public string ExecutionMethodId { get; set; }

        public string? TaskDuration { get; set; }

        /// <summary>
        /// 执行部位id
        /// </summary>
        public string ExecutionPartId { get; set; }

        /// <summary>
        /// 是否停机  0 否  1是
        /// </summary>
        public string? IsShutdown { get; set; }

        /// <summary>
        ///  0-禁用,1-启用.
        /// </summary>
        public int? EnabledMark { get; set; }
    }

    /// <summary>
    /// 列表查询参数
    /// </summary>
    public class MaintenanceProjectQuery : PageInputBase
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// 执行周期id
        /// </summary>
        public string ExecutionCycleId { get; set; }

        /// <summary>
        /// 执行方式id
        /// </summary>
        public string ExecutionMethodId { get; set; }

        /// <summary>
        /// 执行部位id
        /// </summary>
        public string ExecutionPartId { get; set; }
    }

    /// <summary>
    /// 新增编辑参数
    /// </summary>
    public class MaintenanceProjectOutput
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string? TaskName { get; set; }

        /// <summary>
        /// 执行周期id
        /// </summary>
        public string? ExecutionCycleName { get; set; }

        /// <summary>
        /// 执行方式id
        /// </summary>
        public string? ExecutionMethodName { get; set; }

        public string? TaskDuration { get; set; }
        public string? ExecutionPartId { get; set; }

        

        /// <summary>
        /// 执行部位id
        /// </summary>
        public string ExecutionPartName { get; set; }

        /// <summary>
        /// 是否停机  0 否  1是
        /// </summary>
        public int? IsShutdown { get; set; }

        /// <summary>
        ///  0-禁用,1-启用.
        /// </summary>
        public int? EnabledMark { get; set; }

    }

    /// <summary>
    /// 分配设备
    /// </summary>
    public class AllotDeviceInput
    {
        public string Id { get; set; }

        /// <summary>
        /// 设备id    
        /// </summary>
        public List<string> DeviceIds { get; set; }
    }

    /// <summary>
    /// 查看
    /// </summary>
    public class MaintenanceProjectByIdOutput
    {
        /// <summary>
        /// 项目id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string? TaskName { get; set; }

        /// <summary>
        /// 执行周期id
        /// </summary>
        public string? ExecutionCycleId { get; set; }

        /// <summary>
        /// 执行方式id
        /// </summary>
        public string? ExecutionMethodId { get; set; }

        public string? TaskDuration { get; set; }

        /// <summary>
        /// 执行部位id
        /// </summary>
        public string ExecutionPartId { get; set; }

        /// <summary>
        /// 是否停机  0 否  1是
        /// </summary>
        public string? IsShutdown { get; set; }

        /// <summary>
        ///  0-禁用,1-启用.
        /// </summary>
        public int? EnabledMark { get; set; }

        public List<string> DeviceNames { get; set; }


        /// <summary>
        ///  0-不提示,1-提示
        /// </summary>
        public int? IsConfirm { get; set; }

    }

}
