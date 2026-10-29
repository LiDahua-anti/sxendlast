using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Core.Manager;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JNPF.Systems.Interfaces.Custom;
using JNPF.DynamicApiController;
using JNPF.DependencyInjection;
using SqlSugar;
using JNPF.Common.Manager;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.FriendlyException;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.LinqBuilder;
using Org.BouncyCastle.Crypto;
using Mapster;
using SqlSugar.Extensions;
using Aop.Api.Domain;
using Microsoft.AspNetCore.Hosting;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 预防维护项目
    /// </summary>
    [ApiDescriptionSettings(Tag = "Maintenance", Name = "", Order = 200)]
    [Route("api/[controller]")]
    public class MaintenanceService : IMaintenanceService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 服务基础仓储.
        /// </summary>
        private readonly ISqlSugarRepository<MaintenanceProjectsEntity> _maintenanceProjectsRepository;
        private readonly ISqlSugarRepository<MaintenanceProjectDeviceEntity> _maintenanceProjectDeviceRepository;
        private readonly ISqlSugarRepository<MaintenanceRecordEntity> _maintenanceRecordRepository;
        private readonly ISqlSugarRepository<MaintenancePlanEntity> _maintenancePlanRepository;
        private readonly ISqlSugarRepository<MaintenanceRecordSpareEntity> _maintenanceRecordSpareRepository;
        private readonly ISqlSugarRepository<OrganizeEntity> _organizeRepository;
        private readonly ISqlSugarRepository<DeviceMasterEntity> _deviceMasterRepository;
        private readonly ISqlSugarRepository<DictionaryTypeEntity> _dictionaryTypeRepository;
        private readonly ISqlSugarRepository<DictionaryDataEntity> _dictionaryDataRepository;
        private readonly ISqlSugarRepository<WarehouseReceiveEntity> _warehouseReceiveRepository;
        private readonly ISqlSugarRepository<DocumentEntity> _documentRepository;
        private readonly ISqlSugarRepository<DeviceTypeMasterEntity> _deviceTypeMasterRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// 文件服务.
        /// </summary>
        private readonly IFileManager _fileManager;

        /// <summary>
        /// 缓存管理器.
        /// </summary>
        private readonly ICacheManager _cacheManager;

        /// <summary>
        /// 用户管理.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="DeviceService"/>类型的新实例.
        /// </summary>
        public MaintenanceService(
            ISqlSugarRepository<DeviceMasterEntity> deviceMasterRepository,
            ISqlSugarRepository<MaintenanceProjectsEntity> maintenanceProjectsRepository,
            ISqlSugarRepository<MaintenanceProjectDeviceEntity> maintenanceProjectDeviceRepository,
            ISqlSugarRepository<OrganizeEntity> organizeRepository,
            ISqlSugarRepository<DictionaryTypeEntity> dictionaryTypeRepository,
            ISqlSugarRepository<MaintenancePlanEntity> maintenancePlanRepository,
            ISqlSugarRepository<MaintenanceRecordSpareEntity> maintenanceRecordSpareRepository,
            ISqlSugarRepository<MaintenanceRecordEntity> maintenanceRecordRepository,
            ISqlSugarRepository<DictionaryDataEntity> dictionaryDataRepository,
            ISqlSugarRepository<WarehouseReceiveEntity> warehouseReceiveRepository,
            ICacheManager cacheManager,
            IWebHostEnvironment webHostEnvironment,
            ISqlSugarRepository<DeviceTypeMasterEntity> deviceTypeMasterRepository,
            ISqlSugarRepository<DocumentEntity> documentRepository,
            IUserManager userManager,
            IFileManager fileManager)
        {
            _deviceMasterRepository = deviceMasterRepository;
            _maintenanceProjectsRepository = maintenanceProjectsRepository;
            _maintenanceProjectDeviceRepository = maintenanceProjectDeviceRepository;
            _maintenancePlanRepository = maintenancePlanRepository;
            _dictionaryTypeRepository = dictionaryTypeRepository;
            _warehouseReceiveRepository = warehouseReceiveRepository;
            _dictionaryDataRepository = dictionaryDataRepository;
            _maintenanceRecordRepository = maintenanceRecordRepository;
            _deviceTypeMasterRepository = deviceTypeMasterRepository;
            _maintenanceRecordSpareRepository = maintenanceRecordSpareRepository;
            _organizeRepository = organizeRepository;
            _cacheManager = cacheManager;
            _documentRepository = documentRepository;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _fileManager = fileManager;
        }

        #region 预防维护项目

        /// <summary>
        /// 获取所有维修项目
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("project/GetList")]
        public async Task<dynamic> GetList([FromBody] MaintenanceProjectQuery input)
        {
            // 构建查询条件
            var whereLambda = LinqExpression.And<MaintenanceProjectsEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            // 关键词查询
            if (input.TaskName.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m =>
                    m.TaskName.Contains(input.TaskName));
            }
            if (input.ExecutionCycleId.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m =>
                    m.ExecutionCycleId.Contains(input.ExecutionCycleId));
            }
            if (input.ExecutionMethodId.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m =>
                    m.ExecutionMethodId.Contains(input.ExecutionMethodId));
            }
            if (input.ExecutionPartId.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m =>
                    m.ExecutionPartId.Contains(input.ExecutionPartId));
            }

            var type = await _dictionaryTypeRepository.GetListAsync(m =>
                     m.FullName == "执行周期" ||
                     m.FullName == "执行方式");
            var ids = type.Select(m => m.Id).ToList();
            var typeData = await _dictionaryDataRepository.GetListAsync(m => ids.Contains(m.DictionaryTypeId) && m.DeleteMark == null);
            var typeDataDict = typeData.ToDictionary(x => x.Id, x => x.FullName);

            // 第一步：获取分页数据
            var list = await _maintenanceProjectsRepository.AsQueryable()
               .Where(whereLambda)
               .OrderBy(it => it.CreatorTime, OrderByType.Desc)
               .LeftJoin<DictionaryDataEntity>((p, cycle) => p.ExecutionCycleId == cycle.Id && cycle.DeleteMark == null)
               .LeftJoin<DictionaryDataEntity>((p, cycle, method) => p.ExecutionMethodId == method.Id && method.DeleteMark == null)
               .LeftJoin<DictionaryDataEntity>((p, cycle, method, part) => p.ExecutionPartId == part.Id && part.DeleteMark == null)
               .Select((p, cycle, method, part) => new MaintenanceProjectOutput
               {
                   Id = p.Id,
                   TaskName = p.TaskName,
                   IsShutdown = p.IsShutdown,
                   TaskDuration = p.TaskDuration,
                   EnabledMark = p.EnabledMark,
                   ExecutionCycleName = cycle.FullName,
                   ExecutionMethodName = method.FullName,
                   ExecutionPartName = part.FullName,
                   ExecutionPartId = p.ExecutionPartId,
               })
               .Mapper(async (item) =>
               {
                   if (!string.IsNullOrEmpty(item.ExecutionPartId))
                   {
                       item.ExecutionPartName = GetFullPathById(item.ExecutionPartId);
                   }
               })
               .ToPagedListAsync(input.currentPage, input.pageSize);
            return list;
        }

        /// <summary>
        /// 新增/编辑项目
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("project/Insert")]
        public async Task Insert([FromBody] MaintenanceProjectInput input)
        {
            if (input.Id.IsNullOrEmpty())
            {
                //新增
                //验证项目名称不可重复
                var nameIsReCount = await _maintenanceProjectsRepository.CountAsync(m => m.TaskName == input.TaskName && m.DeleteMark == null);
                if (nameIsReCount > 0)
                {
                    throw Oops.Oh(ErrorCode.D8002);
                }
                MaintenanceProjectsEntity entity = input.Adapt<MaintenanceProjectsEntity>();
                int isOk = await _maintenanceProjectsRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
            }
            else
            {
                //编辑
                var nameIsReCount = await _maintenanceProjectsRepository.CountAsync(m => m.TaskName == input.TaskName && m.DeleteMark == null && m.Id != input.Id);
                if (nameIsReCount > 0)
                {
                    throw Oops.Oh(ErrorCode.D8002);
                }
                var existing = await _maintenanceProjectsRepository.GetSingleAsync(m => m.Id == input.Id);
                existing = input.Adapt(existing);
                int isOk = await _maintenanceProjectsRepository.AsSugarClient().Updateable(existing).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6002);
            }

        }

        /// <summary>
        /// 根据id获取
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet("project/GetById")]
        public async Task<dynamic> GetById(string Id)
        {
            var plans = await _maintenancePlanRepository.AsQueryable().Where(m => m.DeleteMark == null && m.MaintenanceProjectId == Id).ToListAsync();
            int isconfirm = 0;
            if (plans.Any())
            {
                isconfirm = 1;
            }

            var data = await _maintenanceProjectsRepository.GetFirstAsync(x => x.Id == Id && x.DeleteMark == null);
            // 先映射对象，然后赋值
            var output = data.Adapt<MaintenanceProjectByIdOutput>();
            output.IsConfirm = isconfirm;
            return output;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet("project/Delete")]
        public async Task Delete(string Id)
        {
            if (Id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            MaintenanceProjectsEntity? entity = await _maintenanceProjectsRepository.GetSingleAsync(p => p.Id == Id && p.DeleteMark == null);
            //获取这个项目下所有设备是否有关联记录
            var recordDevices = await _maintenanceRecordRepository.GetListAsync(m => m.DeleteMark == null && m.MaintenanceProjectId == Id);
            if (recordDevices.Any())
            {
                throw Oops.Oh(ErrorCode.COM1034);
            }

            if (entity != null && entity.EnabledMark == 1) //==1是启用，
            {
                throw Oops.Oh(ErrorCode.COM1038);
            }

            int isOk = await _maintenanceProjectsRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
                throw Oops.Oh(ErrorCode.COM1002);
        }

        /// <summary>
        /// 获取分配设备列表
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("project/GetDeviceList")]
        public async Task<dynamic> GetDeviceList([FromBody] DeviceInput input)
        {
            // 构建查询条件
            var whereLambda = LinqExpression.And<DeviceMasterEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            //查询条件
            if (!string.IsNullOrEmpty(input.DeviceNo))
            {
                whereLambda = whereLambda.And(m => m.DeviceNo.Contains(input.DeviceNo));
            }

            if (!string.IsNullOrEmpty(input.AssetsNumber))
            {
                whereLambda = whereLambda.And(m => m.AssetsNumber.Contains(input.AssetsNumber));
            }

            if (!string.IsNullOrEmpty(input.DeviceName))
            {
                whereLambda = whereLambda.And(m => m.DeviceName.Contains(input.DeviceName));
            }

            if (!string.IsNullOrEmpty(input.DeviceTypeId))
            {
                whereLambda = whereLambda.And(m => m.DeviceTypeId.Equals(input.DeviceTypeId));
            }

            if (!string.IsNullOrEmpty(input.DeviceSupplier))
            {
                whereLambda = whereLambda.And(m => m.DeviceSupplier.Contains(input.DeviceSupplier));
            }

            if (!string.IsNullOrEmpty(input.DeviceManufacturer))
            {
                whereLambda = whereLambda.And(m => m.DeviceManufacturer.Contains(input.DeviceManufacturer));
            }

            if (!string.IsNullOrEmpty(input.BuyStartDate))
            {
                whereLambda = whereLambda.And(m => m.BuyDate.CompareTo(input.BuyStartDate) >= 0);
            }

            if (!string.IsNullOrEmpty(input.BuyEndDate))
            {
                whereLambda = whereLambda.And(m => m.BuyDate.CompareTo(input.BuyEndDate) <= 0);
            }

            if (!string.IsNullOrEmpty(input.UseAddress))
            {
                whereLambda = whereLambda.And(m => m.UseAddress.Contains(input.UseAddress));
            }

            if (!string.IsNullOrEmpty(input.UseDpt))
            {
                whereLambda = whereLambda.And(m => m.UseDpt.Contains(input.UseDpt));
            }

            if (!string.IsNullOrEmpty(input.DeviceStatus))
            {
                whereLambda = whereLambda.And(m => m.DeviceStatus == input.DeviceStatus);
            }

            var type = await _dictionaryTypeRepository.GetListAsync(m =>
                       m.FullName == "设备状态" ||
                       m.FullName == "设备重要程度" ||
                       m.FullName == "使用场所");
            var ids = type.Select(m => m.Id).ToList();
            var typeData = await _dictionaryDataRepository.GetListAsync(m => ids.Contains(m.DictionaryTypeId));
            var dept = await _organizeRepository.GetListAsync(m => m.DeleteMark == null && m.Category == "department");
            var deviceType = await _deviceTypeMasterRepository.GetListAsync();

            var typeDataDict = typeData.ToDictionary(x => x.Id, x => x.FullName);
            var deptDict = dept.ToDictionary(x => x.Id, x => x.FullName);
            var deviceTypeDict = deviceType.ToDictionary(x => x.Id, x => x.DevicetypeName);
            var projectDevice = await _maintenanceProjectDeviceRepository.GetListAsync(m => m.MaintenanceProjectId == input.Id && m.DeleteMark == null);

            // 获取已选中的设备ID列表，用于后续判断和排序
            var checkedDeviceIds = projectDevice.Select(m => m.DeviceId).ToList();

            var query = _deviceMasterRepository.AsQueryable()
                .Where(whereLambda)
                .LeftJoin<DeviceTypeMasterEntity>((d, dt) => d.DeviceTypeId == dt.Id)
                .LeftJoin<DictionaryDataEntity>((d, dt, addr) => d.UseAddress == addr.Id)
                .LeftJoin<DictionaryDataEntity>((d, dt, addr, imp) => d.DeviceImportant == imp.Id)
                .LeftJoin<DictionaryDataEntity>((d, dt, addr, imp, stat) => d.DeviceStatus == stat.Id)
                .LeftJoin<OrganizeEntity>((d, dt, addr, imp, stat, dept) => d.UseDpt == dept.Id);

            // 先按是否选中排序，再按创建时间排序
            var list = await query
                .OrderByDescending(d => checkedDeviceIds.Contains(d.Id))
                .Select((d, dt, addr, imp, stat, dept) => new DeviceCruInput2
                {
                    Id = d.Id,
                    AssetsNumber = d.AssetsNumber,
                    DeviceNo = d.DeviceNo,
                    DeviceName = d.DeviceName,
                    DeviceManufacturer = d.DeviceManufacturer,
                    DeviceModel = d.DeviceModel,
                    DeviceSupplier = d.DeviceSupplier,
                    FNote = d.FNote,
                    TwoDimensionalCode = d.TwoDimensionalCode,
                    DeviceImage = d.DeviceImage,
                    BuyDate = d.BuyDate,
                    DeviceTypeId = dt.DevicetypeName ?? "",
                    UseAddress = addr.FullName ?? "",
                    DeviceImportant = imp.FullName ?? "",
                    DeviceStatus = stat.FullName ?? "",
                    UseDpt = dept.FullName ?? "",
                    IsCheck = checkedDeviceIds.Contains(d.Id) ? 1 : 0
                })
                .ToPagedListAsync(input.currentPage, input.pageSize);

            return list;
        }

        /// <summary>
        /// 分配设备
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("project/Allot")]
        public async Task Allot([FromBody] AllotDeviceInput input)
        {
            if (input.Id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            //验证有设备的记录不能取消设备
            var recordDevices = await _maintenanceRecordRepository.GetListAsync(m => m.DeleteMark == null && m.MaintenanceProjectId == input.Id);
            var existingDeviceIds = recordDevices.Select(m => m.DeviceId).ToList();
            if (existingDeviceIds.Count() != input.DeviceIds.Count() && existingDeviceIds.Count() > 0)
            {
                throw Oops.Oh(ErrorCode.COM1034);
            }
            var insertDeviceIds = input.DeviceIds.Except(existingDeviceIds).ToList();

            //验证取消关联的设备是否需要删除
            var pDevices = await _maintenanceProjectDeviceRepository.GetListAsync(m => m.MaintenanceProjectId == input.Id);
            var existingPDeviceIds = pDevices.Select(m => m.DeviceId).ToList();

            // 1. 删除所有关联
            if (existingPDeviceIds.Any())
            {
                await _maintenanceProjectDeviceRepository.DeleteAsync(m =>
               m.MaintenanceProjectId == input.Id && existingPDeviceIds.Contains(m.DeviceId));
            }

            // 2. 重新关联
            if (input.DeviceIds.Any())
            {
                var entitiesToAdd = input.DeviceIds.Select(deviceId => new MaintenanceProjectDeviceEntity
                {
                    DeviceId = deviceId,
                    MaintenanceProjectId = input.Id
                }).ToList();

                await _maintenanceProjectDeviceRepository.AsInsertable(entitiesToAdd)
                     .CallEntityMethod(m => m.Creator())
                     .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 查看分配设备
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet("project/Allot")]
        public async Task<dynamic> GetAllotDevices(string Id)
        {
            if (Id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var pDevices = await _maintenanceProjectDeviceRepository.GetListAsync(m => m.MaintenanceProjectId == Id);
            var existingPDeviceIds = pDevices.Select(m => m.DeviceId).ToList();
            return existingPDeviceIds;

        }

        #endregion

        #region 预防维护计划

        /// <summary>
        /// 新增/编辑计划
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("plan/check")]
        public async Task<dynamic> Check([FromBody] MaintenancePlanInput input)
        {
            var existingProject = await _maintenancePlanRepository.AsQueryable().Where(m => m.MaintenanceProjectId == input.MaintenanceProjectId && m.DeviceId == input.DeviceId && m.DeleteMark == null).ToListAsync();
            if (input.Id.IsNullOrEmpty())
            {
                //提示是否创建过该计划
                if (existingProject.Any())
                {
                    return "-1";
                }
            }
            else
            {
                if (existingProject.Where(m => m.Id != input.Id).Any())
                {
                    return "-1";
                }
            }
            return "0";
        }

        /// <summary>
        /// 新增/编辑计划
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("plan/Insert")]
        public async Task InsertPlan([FromBody] MaintenancePlanInput input)
        {
            var existingProject = await _maintenancePlanRepository.AsQueryable().Where(m => m.MaintenanceProjectId == input.MaintenanceProjectId && m.DeviceId == input.DeviceId && m.DeleteMark == null).ToListAsync();
            if (input.Id.IsNullOrEmpty())
            {
                //新增
                MaintenancePlanEntity entity = input.Adapt<MaintenancePlanEntity>();
                int isOk = await _maintenancePlanRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                MaintenanceRecordEntity recordEntity = new MaintenanceRecordEntity();
                recordEntity.MaintenanceProjectId = input.MaintenanceProjectId;
                recordEntity.DeviceId = input.DeviceId;
                recordEntity.MaintenancePlanId = entity.Id;
                await _maintenanceRecordRepository.AsSugarClient().Insertable(recordEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
            }
            else
            { //编辑
                var existing = await _maintenancePlanRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                //验证是否变更，变更：验证是否已完成记录（已完成不可变更，未完成变更为新内容），未变更：更新时间
                if (input.MaintenanceProjectId != existing.MaintenanceProjectId || input.DeviceId != existing.DeviceId)
                {
                    var record = await _maintenanceRecordRepository.GetSingleAsync(m => m.MaintenancePlanId == input.Id);
                    if (record != null && record.ExecutionEndTime.IsNullOrEmpty())
                    {
                        throw Oops.Oh(ErrorCode.COM1035);
                    }

                    //更新
                    record.MaintenanceProjectId = input.MaintenanceProjectId;
                    record.DeviceId = input.DeviceId;
                    int isOkRecord = await _maintenanceRecordRepository.AsSugarClient().Updateable(record).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                    if (!(isOkRecord > 0)) throw Oops.Oh(ErrorCode.D6002);
                }

                existing = input.Adapt(existing);
                int isOk = await _maintenancePlanRepository.AsSugarClient().Updateable(existing).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6002);
            }
        }

        /// <summary>
        /// 删除计划
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet("plan/Delete")]
        public async Task DeletePlan(string Id)
        {
            if (Id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _maintenancePlanRepository.GetSingleAsync(m => m.Id == Id && m.DeleteMark == null);

            //判断计划是否完成
            var record = await _maintenanceRecordRepository.GetSingleAsync(m => m.MaintenancePlanId == Id);
            if (record != null && !record.ExecutionEndTime.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1035);
            }

            if (record != null)
            {
                //删除记录、报告、备件信息、图片
                var spareList = await _maintenanceRecordSpareRepository.GetListAsync(m => m.DeleteMark != null && m.MaintenanceRecordId == record.Id);
                await _maintenanceRecordSpareRepository.AsSugarClient().Updateable(spareList).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
                //var fileList = await _reportFilesRepository.GetListAsync(m => m.DeleteMark != null && m.ExternalId == record.Id);
                //await _reportFilesRepository.AsSugarClient().Updateable(fileList).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
                await _maintenanceRecordRepository.AsSugarClient().Updateable(record).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            int isOk = await _maintenancePlanRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
                throw Oops.Oh(ErrorCode.COM1002);

        }

        /// <summary>
        /// 复制计划
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("plan/Copy")]
        public async Task CopyPlan([FromBody] MaintenancePlanCopyInput input)
        {
            var entity = await _maintenancePlanRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
            entity.Id = string.Empty;
            if (input.CopyDate != entity.TaskDate)
            {
                entity.TaskDate = input.CopyDate;
                int isOk = await _maintenancePlanRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                MaintenanceRecordEntity recordEntity = new MaintenanceRecordEntity();
                recordEntity.MaintenanceProjectId = entity.MaintenanceProjectId;
                recordEntity.DeviceId = entity.DeviceId;
                recordEntity.MaintenancePlanId = entity.Id;
                await _maintenanceRecordRepository.AsSugarClient().Insertable(recordEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
            }
        }

        /// <summary>
        /// 移动计划
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("plan/Cut")]
        public async Task CutPlan([FromBody] MaintenancePlanCopyInput input)
        {
            var entity = await _maintenancePlanRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
            if (input.CopyDate != entity.TaskDate)
            {
                entity.TaskDate = input.CopyDate;
                int isOk = await _maintenancePlanRepository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6002);
            }
        }

        /// <summary>
        /// 按照日期获取计划
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("plan/GetPlansByDate")]
        public async Task<dynamic> GetPlansByDate([FromBody] MaintenancePlanQuery input)
        {
            var planList = await _maintenancePlanRepository.GetListAsync(m => m.DeleteMark == null && m.TaskDate >= input.StartDate && m.TaskDate <= input.EndDate);

            // 1. 生成指定时间段的所有日期列表
            var allDates = GenerateDateRange(input.StartDate, input.EndDate);

            // 2. 获取设备-项目关联信息
            var deviceProjectList = await _maintenanceProjectDeviceRepository.AsQueryable()
                .Where(mpd => mpd.DeleteMark == null)
                .LeftJoin<DeviceMasterEntity>((mpd, dm) => mpd.DeviceId == dm.Id && dm.DeleteMark == null)
                .LeftJoin<MaintenanceProjectsEntity>((mpd, dm, mp) => mpd.MaintenanceProjectId == mp.Id && mp.DeleteMark == null)
                .LeftJoin<MaintenanceRecordEntity>((mpd, dm, mp, mr) => mp.Id == mr.MaintenanceProjectId && mp.DeleteMark == null)
                .Select((mpd, dm, mp, mr) => new {
                    DeviceId = mpd.DeviceId,
                    MaintenanceProjectId = mpd.MaintenanceProjectId,
                    CombinedName = (dm.DeviceNo ?? "未知设备") + "_" + (mp.TaskName ?? "未知项目"),
                    ExecutionEndTime = mr.ExecutionEndTime,
                })
                .ToListAsync();

            // 创建设备-项目字典便于查找
            var deviceProjectDict = deviceProjectList
            .GroupBy(x => new { x.DeviceId, x.MaintenanceProjectId })
            .ToDictionary(
                g => g.Key,
                g => new {
                    CombinedName = g.First().CombinedName,
                    LatestExecutionEndTime = g.Max(x => x.ExecutionEndTime) // 获取最新的执行结束时间
                }
            );

            // 按照日期分组已有的计划
            var planDict = planList
                .GroupBy(p => p.TaskDate.Date)
                .ToDictionary(g => g.Key, g => g.Select(p =>
                {
                    var key = new { p.DeviceId, p.MaintenanceProjectId };
                    var combinedName = "未知设备_未知项目";
                    var IsOverDueFlag = 1;

                    if (deviceProjectDict.ContainsKey(key))
                    {
                        var deviceProjectInfo = deviceProjectDict[key];
                        combinedName = deviceProjectInfo.CombinedName;
                        if (deviceProjectInfo.LatestExecutionEndTime.HasValue)
                        {
                            IsOverDueFlag = 0;
                        }
                    }
                    return new CalendarData
                    {
                        Id = p.Id,
                        PlanName = combinedName,
                        MaintenanceProjectId = p.MaintenanceProjectId,
                        DeviceId = p.DeviceId,
                        TaskDate = p.TaskDate.ToString("yyyy-MM-dd"),
                        StartTime = p.StartTime.ToString("HH:mm:ss") ?? "",
                        EndTime = p.EndTime.ToString("HH:mm:ss") ?? "",
                        IsOverDue = IsOverDueFlag,
                    };
                }).ToList());

            //构建包含所有日期的结果
            var result = allDates.Select(date => new MaintenancePlanQueryOutput
            {
                Date = date.ToString("yyyy-MM-dd"),
                List = planDict.ContainsKey(date) ? planDict[date] : new List<CalendarData>()
            }).ToList();

            return result;
        }

        /// <summary>
        /// 定期任务下拉列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("plan/GetProjects")]
        public async Task<dynamic> GetProjects()
        {
            var result = await _maintenanceProjectDeviceRepository.AsQueryable()
           .LeftJoin<DeviceMasterEntity>((mpd, dm) => mpd.DeviceId == dm.Id)
           .InnerJoin<MaintenanceProjectsEntity>((mpd, dm, mp) => mpd.MaintenanceProjectId == mp.Id && mp.DeleteMark == null && mp.EnabledMark == 0)
           .Select((mpd, dm, mp) => new {
               DeviceId = mpd.DeviceId,
               MaintenanceProjectId = mpd.MaintenanceProjectId,
               CombinedName = dm.DeviceNo + "_" + mp.TaskName,
           })
           .ToListAsync();

            return result;
        }

        #endregion

        #region 预防维护记录

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("record/GetList")]
        public async Task<dynamic> GetRecords([FromBody] MaintenanceRecordQuery input)
        {
            // 构建查询条件
            var whereLambda = LinqExpression.And<MaintenanceRecordEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            var query = _maintenanceRecordRepository.AsQueryable()
               .Where(whereLambda)
               .LeftJoin<DeviceMasterEntity>((mr, dm) => mr.DeviceId == dm.Id && dm.DeleteMark == null)
               .LeftJoin<DeviceTypeMasterEntity>((mr, dm, dtm) => dm.DeviceTypeId == dtm.Id && dtm.DeleteMark == null)
               .LeftJoin<MaintenanceProjectsEntity>((mr, dm, dtm, mp) => mr.MaintenanceProjectId == mp.Id && mp.DeleteMark == null)
               // 修改关联条件，确保一对一关系
               .LeftJoin<MaintenancePlanEntity>((mr, dm, dtm, mp, mpl) => mpl.MaintenanceProjectId == mp.Id && mpl.Id == mr.MaintenancePlanId) // 添加具体关联条件
               .LeftJoin<DictionaryDataEntity>((mr, dm, dtm, mp, mpl, d1) => mp.ExecutionCycleId == d1.Id)
               .LeftJoin<DictionaryDataEntity>((mr, dm, dtm, mp, mpl, d1, d2) => mp.ExecutionMethodId == d2.Id)
               .LeftJoin<DictionaryDataEntity>((mr, dm, dtm, mp, mpl, d1, d2, d3) => mp.ExecutionPartId == d3.Id)
               .OrderBy((mr, dm, dtm, mp) => mr.CreatorTime, OrderByType.Desc);
            //.OrderBy((mr, dm, dtm, mp) => mr.CreatorTime, OrderByType.Desc);
            var typeList = await _dictionaryTypeRepository.GetListAsync();

            // 动态过滤条件
            if (!string.IsNullOrEmpty(input.DeviceNo))
            {
                query = query.Where((mr, dm, dtm, mp, mpl) => dm.DeviceNo.Contains(input.DeviceNo));
            }

            if (!string.IsNullOrEmpty(input.DeviceName))
            {
                query = query.Where((mr, dm, dtm, mp, mpl) => dm.DeviceName.Contains(input.DeviceName));
            }

            if (!string.IsNullOrEmpty(input.DeviceTypeId))
            {
                query = query.Where((mr, dm, dtm, mp, mpl) => dm.DeviceTypeId == input.DeviceTypeId);
            }

            if (!string.IsNullOrEmpty(input.TaskName))
            {
                query = query.Where((mr, dm, dtm, mp, mpl) => mp.TaskName.Contains(input.TaskName));
            }
            if (!string.IsNullOrEmpty(input.ExecutionCycleId))
            {
                query = query.Where((mr, dm, dtm, mp, mpl) => mp.ExecutionCycleId == input.ExecutionCycleId);
            }

            var list = await query.Select((mr, dm, dtm, mp, mpl, d1, d2, d3) => new MaintenanceRecordOutput
            {
                // 主记录信息
                Id = mr.Id,
                ExecutionStartTime = mr.ExecutionStartTime.Value.ToString("yyyy-MM-dd hh:mm:ss"),
                ExecutionEndTime = mr.ExecutionEndTime.Value.ToString("yyyy-MM-dd hh:mm:ss"),
                IsOverDue = mr.ExecutionEndTime != null ? 1 : 0,
                DurationHours = mr.DurationHours,
                // 设备信息
                DeviceNo = dm.DeviceNo,
                DeviceName = dm.DeviceName,
                DeviceTypeName = dtm.DevicetypeName,
                // 计划信息
                TaskName = mp.TaskName,
                StartTime = mpl.StartTime.ToString("yyyy-MM-dd hh:mm:ss"),
                EndTime = mpl.EndTime.ToString("yyyy-MM-dd hh:mm:ss"),
                PlanDurationHours = (mpl.StartTime != null && mpl.EndTime != null)
                ? SqlFunc.DateDiff(DateType.Hour, mpl.StartTime, mpl.EndTime)//(mpl.EndTime - mpl.StartTime).Hours
                : 0,
                ExecutionCycleName = d1.FullName,
                ExecutionMethodName = d2.FullName,
                ExecutionPartName = mp.ExecutionPartId,
                EnabledMark = mp.EnabledMark,
                IsShutdown = mp.IsShutdown,
            })
            .Mapper(async (item) =>
            {
                if (!string.IsNullOrEmpty(item.ExecutionPartName))
                {
                    item.ExecutionPartName = GetFullPathById(item.ExecutionPartName);
                }
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);
            return list;

        }

        /// <summary>
        /// 根据id获取记录
        /// </summary>
        /// <returns></returns>
        [HttpGet("record/GetById")]
        public async Task<dynamic> GetRecordById(string Id)
        {
            // 构建查询条件
            var whereLambda = LinqExpression.And<MaintenanceRecordEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null && x.Id == Id);

            var query = _maintenanceRecordRepository.AsQueryable()
               .Where(whereLambda)
               .LeftJoin<DeviceMasterEntity>((mr, dm) => mr.DeviceId == dm.Id && dm.DeleteMark == null)
               .LeftJoin<DeviceTypeMasterEntity>((mr, dm, dtm) => dm.DeviceTypeId == dtm.Id && dtm.DeleteMark == null)
               .LeftJoin<MaintenanceProjectsEntity>((mr, dm, dtm, mp) => mr.MaintenanceProjectId == mp.Id && mp.DeleteMark == null)
               // 修改关联条件，确保一对一关系
               .LeftJoin<MaintenancePlanEntity>((mr, dm, dtm, mp, mpl) => mpl.MaintenanceProjectId == mp.Id && mpl.Id == mr.MaintenancePlanId) // 添加具体关联条件
               .LeftJoin<DictionaryTypeEntity>((mr, dm, dtm, mp, mpl, d1) => mp.ExecutionCycleId == d1.Id)
               .LeftJoin<DictionaryTypeEntity>((mr, dm, dtm, mp, mpl, d1, d2) => mp.ExecutionMethodId == d2.Id)
               .LeftJoin<DictionaryTypeEntity>((mr, dm, dtm, mp, mpl, d1, d2, d3) => mp.ExecutionPartId == d3.Id)
               .OrderBy((mr, dm, dtm, mp) => mr.CreatorTime, OrderByType.Desc);

            //获取备件信息
            var spareList = _maintenanceRecordSpareRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.MaintenanceRecordId == Id)
                .LeftJoin<WarehouseBomEntity>((s, bom) => s.BomId == bom.Id);

            var spareInfoList = spareList.Select((s, bom) => new SpareInfo
            {
                Id = s.Id,
                BomId = s.BomId,
                Quantity = s.Quantity ?? "0",
                Prices = s.Prices ?? "0",
                PartsManufacturer = bom.PartsManufacturer,
                //LocationId = s.LocationId,
                //LocationName ="", //loc.WarehouseName,
                PartsModel = bom.PartsModel,
                PartsName = bom.PartsName,
            }).ToList();

            // 返回单条记录
            var result = await query.Select((mr, dm, dtm, mp, mpl, d1, d2, d3) => new MaintenanceRecordByIdOutput
            {
                //设备基本信息
                DeviceNo = dm.DeviceNo,
                DeviceName = dm.DeviceName,
                DeviceTypeName = dtm.DevicetypeName,
                //任务基本信息
                TaskName = mp.TaskName,
                StartTime = mpl.StartTime.ToString("yyyy-MM-dd hh:mm:ss"),
                EndTime = mpl.EndTime.ToString("yyyy-MM-dd hh:mm:ss"),
                PlanDurationHours = (mpl.StartTime != null && mpl.EndTime != null)
                    ? SqlFunc.DateDiff(DateType.Hour, mpl.StartTime, mpl.EndTime)
                    : 0,
                ExecutionCycleId = mp.ExecutionCycleId,
                ExecutionMethodId = mp.ExecutionMethodId,
                ExecutionPartId = mp.ExecutionPartId,
                EnabledMark = mp.EnabledMark,
                IsShutdown = mp.IsShutdown,
                // 主记录信息
                Id = mr.Id,
                ExecutionStartTime = mr.ExecutionStartTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                ExecutionEndTime = mr.ExecutionEndTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                DurationHours = mr.DurationHours,
                MaintenanceProjectId = mr.MaintenanceProjectId,
                DeviceId = mr.DeviceId,
                MaintenancePlanId = mr.MaintenancePlanId,
                Photo1 = mr.Photo1,
                Photo2 = mr.Photo2,
                Photo3 = mr.Photo3,
                Photo4 = mr.Photo4,
                Photo5 = mr.Photo5,
                OutsourcingPmCompany = mr.OutsourcingPmCompany,
                OutsourcingPmCost = mr.OutsourcingPmCost,
                DowntimeHours = mr.DowntimeHours,
                IsAbnormal = mr.IsAbnormal,
                AffectsDowntime = mr.AffectsDowntime,
            }).FirstAsync();
            result.SpareDatas = spareInfoList;
            return result;
        }

        /// <summary>
        /// 获取委外报告
        /// </summary>
        /// <returns></returns>
        [HttpGet("record/GetRecordFileById")]
        public async Task<dynamic> GetRecordFileById(string Id)
        {
            List<RecordFileResult> list = new List<RecordFileResult>();
            var docList = await _documentRepository.GetListAsync(p => p.Description == Id && p.EnabledMark == 1 && p.DeleteMark == null);
            foreach (var item in docList)
            {
                RecordFileResult fileContentResult = new RecordFileResult();
                fileContentResult.Id = item.Id;
                fileContentResult.Url = item.UploadUrl;
                fileContentResult.FileExtension = item.FileExtension;
                fileContentResult.FileSize = item.FileSize;
                fileContentResult.FileName = item.FullName;
                fileContentResult.CreatorTime = item.CreatorTime;
                fileContentResult.CreatorUserName = _userManager.GetUserName(item.CreatorUserId);
                list.Add(fileContentResult);
            }
            return list;
        }

        /// <summary>
        /// 编辑记录
        /// </summary>
        /// <returns></returns>
        /// <returns></returns>
        [HttpPost("record/Insert")]
        public async Task InsertRecord(MaintenanceRecordInsertInput input)
        {
            //添加(弃用)
            if (input.Id.IsNullOrEmpty())
            {
                MaintenanceRecordEntity entity = input.Adapt<MaintenanceRecordEntity>();
                int isOk = await _maintenanceRecordRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
                //添加备件
                List<MaintenanceRecordSpareEntity> spareEntity = input.SpareDatas.Adapt<List<MaintenanceRecordSpareEntity>>();
                List<WarehouseReceiveEntity> receiveEntities = new List<WarehouseReceiveEntity>();
                foreach (var item in spareEntity)
                {
                    item.MaintenanceRecordId = entity.Id;
                    await _maintenanceRecordRepository.AsSugarClient().Insertable(item).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    //添加领用
                    WarehouseReceiveEntity warehouseReceiveEntity = new WarehouseReceiveEntity();
                    warehouseReceiveEntity.ExternalId = item.Id;
                    warehouseReceiveEntity.BomId = item.BomId;
                    warehouseReceiveEntity.ReceiveQuantity = item.Quantity;
                    warehouseReceiveEntity.Status = 1; // 未确认
                    //warehouseReceiveEntity.LocationId = item.LocationId;
                    warehouseReceiveEntity.ReceiveType = 0; // 预防维护
                    warehouseReceiveEntity.DeviceId = input.DeviceId;
                    await _warehouseReceiveRepository.AsSugarClient().Insertable(warehouseReceiveEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
                ////添加文件
                // ReportFilesEntity filesEntity = input.FileDatas.Adapt<ReportFilesEntity>();
                // filesEntity.ExternalId = entity.Id;
                // await _reportFilesRepository.AsSugarClient().Insertable(filesEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
            else
            {
                //处理备件和领用
                List<MaintenanceRecordSpareEntity> spareEntity = input.SpareDatas.Adapt<List<MaintenanceRecordSpareEntity>>();
                foreach (var item in spareEntity)
                {
                    //添加备件
                    item.MaintenanceRecordId = input.Id;
                    await _maintenanceRecordSpareRepository.AsSugarClient().Insertable(item).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    //添加领用
                    WarehouseReceiveEntity warehouseReceiveEntity = new WarehouseReceiveEntity();
                    warehouseReceiveEntity.ExternalId = item.Id;//存储是RecordSpareId
                    warehouseReceiveEntity.BomId = item.BomId;
                    warehouseReceiveEntity.ReceiveQuantity = item.Quantity;
                    warehouseReceiveEntity.Status = 1; // 未确认
                    //warehouseReceiveEntity.LocationId = item.LocationId;
                    warehouseReceiveEntity.ReceiveType = 0; // 预防维护
                    warehouseReceiveEntity.DeviceId = input.DeviceId;
                    await _warehouseReceiveRepository.AsSugarClient().Insertable(warehouseReceiveEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }

                //修改记录
                var existing = await _maintenanceRecordRepository.GetSingleAsync(m => m.Id == input.Id);
                existing = input.Adapt(existing);
                int isOk = await _maintenanceRecordRepository.AsSugarClient().Updateable(existing).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6002);

                //处理上传文件
                if (input.fileContent.Any())
                {
                    var mianDocId = string.Empty;
                    var mainDoc = await _documentRepository.GetSingleAsync(p => p.FullName == "预防维护管理文件" && p.DeleteMark == null);
                    if (mainDoc == null)
                    {
                        //创建主分支
                        var mianDocEntity = new DocumentEntity() { FullName = "预防维护管理文件", Type = 0, ParentId = "0", EnabledMark = 1, TenantId = "0" };
                        await _documentRepository.AsSugarClient().Insertable(mianDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                        mianDocId = mianDocEntity.Id;
                    }
                    else
                    {
                        mianDocId = mainDoc.Id;
                    }

                    var projectDocId = string.Empty;
                    var projectDoc = await _documentRepository.GetSingleAsync(p => p.Description == input.MaintenanceProjectId && p.ParentId == mianDocId && p.DeleteMark == null);
                    if (projectDoc == null)
                    {
                        //创建第二层级（项目）
                        var projectMaster = await _maintenanceProjectsRepository.GetSingleAsync(p => p.Id == input.MaintenanceProjectId && p.DeleteMark == null);
                        var devDocEntity = new DocumentEntity() { FullName = projectMaster.TaskName, Type = 0, ParentId = mianDocId, EnabledMark = 1, TenantId = "0", Description = input.MaintenanceProjectId };
                        await _documentRepository.AsSugarClient().Insertable(devDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                        projectDocId = devDocEntity.Id;
                    }
                    else
                    {
                        projectDocId = projectDoc.Id;
                    }

                    var deviceDocId = string.Empty;
                    var deviceDoc = await _documentRepository.GetSingleAsync(p => p.Description == input.DeviceId && p.ParentId == mianDocId && p.DeleteMark == null);
                    if (deviceDoc == null)
                    {
                        //创建第三层级（设备）有设备号文件就不创建
                        var deviceMaster = await _deviceMasterRepository.GetSingleAsync(p => p.Id == input.DeviceId && p.DeleteMark == null);
                        var devDocEntity = new DocumentEntity() { FullName = deviceMaster.DeviceNo, Type = 0, ParentId = projectDocId, EnabledMark = 1, TenantId = "0", Description = input.DeviceId };
                        await _documentRepository.AsSugarClient().Insertable(devDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                        deviceDocId = devDocEntity.Id;
                    }
                    else
                    {
                        deviceDocId = deviceDoc.Id;
                    }

                    //处理文件将文件放在指定地址下
                    foreach (var item in input.fileContent)
                    {
                        var newUrl = string.Empty;
                        var webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
                        if (item.Url.Contains("TempFiles"))
                        {
                            // 获取临时文件的完整路径
                            var relativePath = item.Url.TrimStart('/', '\\');
                            var tempFilePath = Path.Combine(webRootPath, relativePath);

                            // 目标文件夹路径
                            var targetDirectory = @"D:\wwwroot\net_Resources\DocumentFile";
                            if (!Directory.Exists(targetDirectory))
                                Directory.CreateDirectory(targetDirectory);

                            // 目标文件路径
                            var targetFilePath = Path.Combine(targetDirectory, item.FileName);

                            // 如果目标文件已存在，生成新文件名（避免冲突）
                            if (File.Exists(targetFilePath))
                            {
                                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(item.FileName);
                                var fileExtension = Path.GetExtension(item.FileName);
                                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                                var newFileName = $"{fileNameWithoutExt}_{timestamp}{fileExtension}";
                                targetFilePath = Path.Combine(targetDirectory, newFileName);
                            }

                            // 移动文件
                            File.Move(tempFilePath, targetFilePath);

                            // 构建新的URL路径
                            newUrl = $"/api/file/Image/document/{Path.GetFileName(targetFilePath)}";

                            // 删除临时文件（如果移动成功）
                            if (File.Exists(tempFilePath))
                            {
                                File.Delete(tempFilePath);
                            }

                            var DocEntity = new DocumentEntity() { FullName = item.FileName, Type = 1, ParentId = deviceDocId, EnabledMark = 1, FileExtension = item.FileExtension, FileSize = item.FileSize.ToString(), TenantId = "0", UploadUrl = newUrl, FilePath = item.FileName, Description = existing.Id };
                            await _documentRepository.AsSugarClient().Insertable(DocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 删除备件
        /// </summary>
        /// <returns></returns>
        [HttpGet("record/DeleteSpare")]
        public async Task DeleteSpare(string Id)
        {
            if (Id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }
            //验证当前备件在领用操作中未审批可删除
            var receiveList = await _warehouseReceiveRepository.GetSingleAsync(m => m.DeleteMark == null && m.ExternalId == Id);
            if (receiveList != null)
            {
                if (receiveList.Status == 0)
                {
                    throw Oops.Oh(ErrorCode.COM1036);
                }
                else if (receiveList.Status == 1)
                {
                    //删除领用记录
                    int isOkReceive = await _warehouseReceiveRepository.AsSugarClient().Updateable(receiveList).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
                    if (!(isOkReceive > 0))
                        throw Oops.Oh(ErrorCode.COM1002);
                }
            }
            MaintenanceRecordSpareEntity? entity = await _maintenanceRecordSpareRepository.GetSingleAsync(p => p.Id == Id && p.DeleteMark == null);
            int isOk = await _maintenanceRecordSpareRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
                throw Oops.Oh(ErrorCode.COM1002);
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 生成指定日期范围内的所有日期
        /// </summary>
        private List<DateTime> GenerateDateRange(DateTime startDate, DateTime endDate)
        {
            var dates = new List<DateTime>();

            // 确保日期范围合理
            if (startDate > endDate)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                dates.Add(currentDate);
                currentDate = currentDate.AddDays(1);
            }

            return dates;
        }

        /// <summary>
        /// 根据ID获取完整层级路径（同步版本）- 去掉第一级别
        /// </summary>
        private string GetFullPathById(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;

            List<string> fullPath = BuildFullPath(id);

            // 移除第一级别
            return RemoveFirstLevel(fullPath);
        }

        /// <summary>
        /// 构建完整路径
        /// </summary>
        private List<string> BuildFullPath(string id)
        {
            var pathList = new List<string>();

            // 先尝试作为字典数据ID查询
            var data = _dictionaryDataRepository.GetSingle(m => m.Id == id && m.DeleteMark == null);
            if (data != null)
            {
                // 获取类型的层级路径
                GetParentPathRecursive(data.DictionaryTypeId, pathList);
                pathList.Reverse();
                pathList.Add(data.FullName);
                return pathList;
            }

            // 尝试作为字典类型ID查询
            var type = _dictionaryTypeRepository.GetSingle(m => m.Id == id && m.DeleteMark == null);
            if (type != null)
            {
                // 获取类型的层级路径
                GetParentPathRecursive(type.Id, pathList);
                pathList.Reverse();
                return pathList;
            }

            return pathList;
        }

        /// <summary>
        /// 移除路径的第一级别
        /// </summary>
        private string RemoveFirstLevel(List<string> pathList)
        {
            if (pathList == null || pathList.Count == 0)
                return string.Empty;

            // 如果路径只有一级，根据需求决定是否返回
            if (pathList.Count == 1)
            {
                // 选项1：移除唯一的一级，返回空
                // return string.Empty;

                // 选项2：保留唯一的一级
                return pathList[0];

                // 选项3：如果要求必须有多级才显示，返回空
                // return pathList.Count > 1 ? string.Join("-", pathList.Skip(1)) : string.Empty;
            }

            // 移除第一级别后拼接
            return string.Join("-", pathList.Skip(1));
        }

        /// <summary>
        /// 递归获取父级路径（同步版本）
        /// </summary>
        private void GetParentPathRecursive(string typeId, List<string> pathList)
        {
            if (string.IsNullOrEmpty(typeId)) return;

            var currentType = _dictionaryTypeRepository.GetSingle(m => m.Id == typeId && m.DeleteMark == null);
            if (currentType == null) return;

            pathList.Add(currentType.FullName);

            if (!string.IsNullOrEmpty(currentType.ParentId))
            {
                GetParentPathRecursive(currentType.ParentId, pathList);
            }
        }

        #endregion
    }
}
