using JNPF.Common.Enums;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Schedule;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Weixin.Annotations;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Common.Core.Job
{
    /// <summary>
    /// 维修定时生成跨日文件定时器
    /// </summary>
    public class RepairGenerationJob : IJob
    {
        private readonly ILogger<RepairGenerationJob> _logger;
        private readonly ISqlSugarRepository<DeviceItemMainEntity> _deviceItemMainRepository;
        private readonly ISqlSugarRepository<DeviceItemSubEntity> _deviceItemSubRepository;
        private readonly ISqlSugarRepository<DeviceItemAllotEntity> _deviceItemAllotRepository;
        private readonly ISqlSugarRepository<DeviceCheckTaskEntity> _deviceCheckTaskRepository;
        private readonly ISqlSugarRepository<DeviceCheckEntity> _deviceCheckRepository;
        private readonly ISqlSugarRepository<DeviceCheckTaskVersionEntity> _deviceCheckTaskVersionRepository;
        private readonly ISqlSugarRepository<DeviceRepairEntity> _deviceRepairRepository;
        private readonly ISqlSugarRepository<DeviceRepairRecordEntity> _deviceRepairRecordRepository;
        private readonly ISqlSugarRepository<DictionaryDataEntity> _dictionaryDataRepository;

        private readonly ISqlSugarRepository<MaintenanceRecordEntity> _maintenanceRecordRepository;
        private readonly ISqlSugarRepository<MaintenanceProjectsEntity> _maintenanceProjectsRepository;
        private readonly ISqlSugarRepository<MaintenancePlanEntity> _maintenancePlanRepository;
        private readonly ISqlSugarRepository<MaintenanceProjectDeviceEntity> _maintenanceProjectDeviceRepository;

        public RepairGenerationJob(
            ILogger<RepairGenerationJob> logger,
            ISqlSugarRepository<DeviceItemMainEntity> deviceItemMainRepository,
            ISqlSugarRepository<DeviceItemSubEntity> deviceItemSubRepository,
            ISqlSugarRepository<DeviceItemAllotEntity> deviceItemAllotRepository,
            ISqlSugarRepository<DeviceCheckTaskEntity> deviceCheckTaskRepository,
            ISqlSugarRepository<DeviceCheckEntity> deviceCheckRepository,
            ISqlSugarRepository<DeviceCheckTaskVersionEntity> deviceCheckTaskVersionRepository,
            ISqlSugarRepository<DictionaryDataEntity> dictionaryDataRepository,
            ISqlSugarRepository<DeviceRepairEntity> deviceRepairRepository,
            ISqlSugarRepository<DeviceRepairRecordEntity> deviceRepairRecordRepository,

            ISqlSugarRepository<MaintenanceRecordEntity> maintenanceRecordRepository,
            ISqlSugarRepository<MaintenanceProjectsEntity> maintenanceProjectsRepository,
            ISqlSugarRepository<MaintenancePlanEntity> maintenancePlanRepository,
            ISqlSugarRepository<MaintenanceProjectDeviceEntity> maintenanceProjectDeviceRepository

            )
        {
            _logger = logger;
            _deviceItemMainRepository = deviceItemMainRepository;
            _deviceItemSubRepository = deviceItemSubRepository;
            _deviceItemAllotRepository = deviceItemAllotRepository;
            _deviceCheckTaskRepository = deviceCheckTaskRepository;
            _deviceCheckRepository = deviceCheckRepository;
            _deviceRepairRepository = deviceRepairRepository;
            _dictionaryDataRepository = dictionaryDataRepository;
            _deviceRepairRecordRepository = deviceRepairRecordRepository;
            _deviceCheckTaskVersionRepository = deviceCheckTaskVersionRepository;

            _maintenanceRecordRepository = maintenanceRecordRepository;
            _maintenanceProjectsRepository = maintenanceProjectsRepository;
            _maintenancePlanRepository = maintenancePlanRepository;
            _maintenanceProjectDeviceRepository = maintenanceProjectDeviceRepository;
        }

        public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
        {
            try
            {
                await GenerateMaintenanceRecordTasks();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "维修记录生成失败");
                throw;
            }
        }

        /// <summary>
        /// 按照预防维护项目
        /// </summary>
        /// <returns></returns>
        private async Task GenerateMaintenanceRecordTasks()
        {
            try
            {
                var today = DateTime.Today;

                // 1. 获取所有可用的维护项目（EnabledMark == 1，根据你的说明0是禁用，1是启用）
                var enabledProjects = await _maintenanceProjectsRepository
                    .AsQueryable()
                    .Where(x => x.DeleteMark == null && x.EnabledMark == 1)
                    .ToListAsync();

                if (!enabledProjects.Any())
                {
                    _logger.LogInformation("没有可用的维护项目需要生成任务");
                    return;
                }

                // 2. 获取字典数据，建立执行周期映射


                _logger.LogInformation($"找到 {enabledProjects.Count} 个可用维护项目，开始生成任务...");

                // 3. 遍历每个项目，生成任务
                int generatedCount = 0;
                foreach (var project in enabledProjects)
                {
                    try
                    {

                        var cycleDict = await _dictionaryDataRepository.GetSingleAsync(x => x.DeleteMark == null && x.Id == project.ExecutionCycleId);
                        if (cycleDict == null)
                        {
                            _logger.LogWarning($"项目 {project.TaskName}({project.Id}) 没有配置执行周期或周期配置无效");
                            continue;
                        }

                        string cycleName = cycleDict.FullName;

                        // 获取项目关联的设备
                        var deviceRelations = await _maintenanceProjectDeviceRepository
                            .AsQueryable()
                            .Where(x => x.MaintenanceProjectId == project.Id && x.DeleteMark == null)
                            .ToListAsync();

                        if (!deviceRelations.Any())
                        {
                            _logger.LogWarning($"项目 {project.TaskName} 没有关联的设备");
                            continue;
                        }

                        // 为每个设备生成任务
                        foreach (var deviceRelation in deviceRelations)
                        {
                            bool generated = await ProcessDeviceMaintenance(project, deviceRelation.DeviceId, cycleName, today);
                            if (generated) generatedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"处理项目 {project.TaskName} 时发生异常，继续处理其他项目");
                        // 继续处理下一个项目
                    }
                }

                _logger.LogInformation($"维护记录生成完成，处理了 {enabledProjects.Count} 个项目，生成了 {generatedCount} 条记录");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成维护记录任务时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 处理单个设备的维护任务生成
        /// </summary>
        private async Task<bool> ProcessDeviceMaintenance(
            MaintenanceProjectsEntity project,
            string deviceId,
            string cycleName,
            DateTime today)
        {
            try
            {
                // 1. 检查今天是否已经有记录
                var todayRecordExists = await _maintenanceRecordRepository
                    .AsQueryable()
                    .Where(x => x.DeleteMark == null
                        && x.MaintenanceProjectId == project.Id
                        && x.DeviceId == deviceId
                        && x.CreatorTime >= today
                        && x.CreatorTime < today.AddDays(1))
                    .AnyAsync();

                if (todayRecordExists)
                {
                    _logger.LogDebug($"设备 {deviceId} 的项目 {project.TaskName} 今天已有记录，跳过生成");
                    return false;
                }

                // 2. 根据周期类型判断是否需要生成
                bool shouldGenerate = false;

                switch (cycleName)
                {
                    case "日次":
                        shouldGenerate = await ShouldGenerateForDaily(project, deviceId, today);
                        break;

                    case "周次":
                        shouldGenerate = await ShouldGenerateForWeekly(project, deviceId, today);
                        break;

                    case "月次":
                        shouldGenerate = await ShouldGenerateForMonthly(project, deviceId, today);
                        break;

                    case "年次":
                        shouldGenerate = await ShouldGenerateForYearly(project, deviceId, today);
                        break;

                    default:
                        _logger.LogWarning($"未知的执行周期类型: {cycleName}");
                        return false;
                }

                if (!shouldGenerate)
                {
                    return false;
                }

                // 3. 创建维护计划
                var planId = await CreateMaintenancePlan(project, deviceId, today);

                // 4. 创建维护记录
                await CreateMaintenanceRecord(project, deviceId, planId, today);

                _logger.LogInformation($"已为设备 {deviceId} 的项目 {project.TaskName} 生成维护任务");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理设备 {deviceId} 的项目 {project.TaskName} 时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 日次任务生成判断
        /// </summary>
        private async Task<bool> ShouldGenerateForDaily(
            MaintenanceProjectsEntity project,
            string deviceId,
            DateTime today)
        {
            // 日次每天生成
            return true;
        }

        /// <summary>
        /// 周次任务生成判断
        /// </summary>
        private async Task<bool> ShouldGenerateForWeekly(
            MaintenanceProjectsEntity project,
            string deviceId,
            DateTime today)
        {
            // 查找最近的计划任务
            var lastPlan = await _maintenancePlanRepository
                .AsQueryable()
                .Where(x => x.DeleteMark == null
                    && x.MaintenanceProjectId == project.Id
                    && x.DeviceId == deviceId
                    && x.TaskDate < today)
                .OrderByDescending(x => x.TaskDate)
                .FirstAsync();

            if (lastPlan == null)
            {
                // 如果没有历史计划，不生成（按你的要求）
                _logger.LogDebug($"设备 {deviceId} 的项目 {project.TaskName} 没有历史计划，周次任务不生成");
                return false;
            }

            // 检查距离上次任务是否满一周（7天）
            var daysSinceLast = (today - lastPlan.TaskDate.Date).TotalDays;
            bool shouldGenerate = daysSinceLast >= 7;

            _logger.LogDebug($"设备 {deviceId} 的项目 {project.TaskName}，上次计划时间：{lastPlan.TaskDate:yyyy-MM-dd}，间隔天数：{daysSinceLast}，是否生成：{shouldGenerate}");

            return shouldGenerate;
        }

        /// <summary>
        /// 月次任务生成判断
        /// </summary>
        private async Task<bool> ShouldGenerateForMonthly(
            MaintenanceProjectsEntity project,
            string deviceId,
            DateTime today)
        {
            // 查找最近的计划任务
            var lastPlan = await _maintenancePlanRepository
                .AsQueryable()
                .Where(x => x.DeleteMark == null
                    && x.MaintenanceProjectId == project.Id
                    && x.DeviceId == deviceId
                    && x.TaskDate < today)
                .OrderByDescending(x => x.TaskDate)
                .FirstAsync();

            if (lastPlan == null)
            {
                // 如果没有历史计划，不生成（按你的要求）
                _logger.LogDebug($"设备 {deviceId} 的项目 {project.TaskName} 没有历史计划，月次任务不生成");
                return false;
            }

            // 检查距离上次任务是否满一个月
            var monthsSinceLast = ((today.Year - lastPlan.TaskDate.Year) * 12) +
                                  today.Month - lastPlan.TaskDate.Month;
            bool shouldGenerate = monthsSinceLast >= 1;

            _logger.LogDebug($"设备 {deviceId} 的项目 {project.TaskName}，上次计划时间：{lastPlan.TaskDate:yyyy-MM-dd}，间隔月数：{monthsSinceLast}，是否生成：{shouldGenerate}");

            return shouldGenerate;
        }

        /// <summary>
        /// 年次任务生成判断
        /// </summary>
        private async Task<bool> ShouldGenerateForYearly(
            MaintenanceProjectsEntity project,
            string deviceId,
            DateTime today)
        {
            // 查找最近的计划任务
            var lastPlan = await _maintenancePlanRepository
                .AsQueryable()
                .Where(x => x.DeleteMark == null
                    && x.MaintenanceProjectId == project.Id
                    && x.DeviceId == deviceId
                    && x.TaskDate < today)
                .OrderByDescending(x => x.TaskDate)
                .FirstAsync();

            if (lastPlan == null)
            {
                // 如果没有历史计划，不生成（按你的要求）
                _logger.LogDebug($"设备 {deviceId} 的项目 {project.TaskName} 没有历史计划，年次任务不生成");
                return false;
            }

            // 检查距离上次任务是否满一年
            var yearsSinceLast = today.Year - lastPlan.TaskDate.Year;
            bool shouldGenerate = yearsSinceLast >= 1;

            _logger.LogDebug($"设备 {deviceId} 的项目 {project.TaskName}，上次计划时间：{lastPlan.TaskDate:yyyy-MM-dd}，间隔年数：{yearsSinceLast}，是否生成：{shouldGenerate}");

            return shouldGenerate;
        }

        /// <summary>
        /// 创建维护计划
        /// </summary>
        private async Task<string> CreateMaintenancePlan(
            MaintenanceProjectsEntity project,
            string deviceId,
            DateTime today)
        {
            try
            {

                var planEntity = new MaintenancePlanEntity
                {
                    MaintenanceProjectId = project.Id,
                    DeviceId = deviceId,
                    TaskDate = today,
                    StartTime = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0),
                    EndTime = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59)
                };

                // 使用统一的插入方法，自动调用 Creator() 方法
                int result = await _maintenancePlanRepository.AsSugarClient()
                    .Insertable(planEntity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();

                if (result <= 0)
                {
                    throw new Exception($"创建维护计划失败，项目：{project.TaskName}，设备：{deviceId}");
                }

                return planEntity.Id;

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// 创建维护记录
        /// </summary>
        private async Task CreateMaintenanceRecord(
            MaintenanceProjectsEntity project,
            string deviceId,
            string planId,
            DateTime today)
        {
            var recordEntity = new MaintenanceRecordEntity
            {
                MaintenancePlanId = planId,
                DeviceId = deviceId,
                MaintenanceProjectId = project.Id,
                IsAbnormal = 0,
                AffectsDowntime = 0,
                SpareStatus = 0
                // 其他字段保持为 null/默认值
            };

            // 使用统一的插入方法，自动调用 Creator() 方法
            int result = await _maintenanceRecordRepository.AsSugarClient()
                .Insertable(recordEntity)
                .CallEntityMethod(m => m.Creator())
                .ExecuteCommandAsync();

            if (result <= 0)
            {
                throw new Exception($"创建维护记录失败，项目：{project.TaskName}，设备：{deviceId}，计划ID：{planId}");
            }
        }
    }
}