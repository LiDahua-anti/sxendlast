using Aop.Api.Domain;
using Aspose.Words;
using JNPF.Common.Contracts;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Custom;
using Mapster;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using SqlSugar;
using NPOI.HSSF.UserModel;
using System.Data;
using System.Threading.Tasks;
using JNPF.Common.Helper;
using Yitter.IdGenerator;
using System.Text;
using JNPF.Common.Net;
using Microsoft.AspNetCore.Authorization;


namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 点检报表
    /// </summary>
    [ApiDescriptionSettings(Tag = "Check", Name = "", Order = 200)]
    [Route("api/[controller]")]
    public class CheckService : ICheckService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 服务基础仓储.
        /// </summary>
        private readonly ISqlSugarRepository<DeviceMasterEntity> _deviceMasterRepository;
        private readonly ISqlSugarRepository<DeviceTypeMasterEntity> _deviceTypeMasterRepository;
        private readonly ISqlSugarRepository<OrganizeEntity> _organizeRepository;
        private readonly ISqlSugarRepository<DictionaryTypeEntity> _dictionaryTypeRepository;
        private readonly ISqlSugarRepository<DictionaryDataEntity> _dictionaryDataRepository;
        private readonly ISqlSugarRepository<DeviceRepairEntity> _deviceRepairRepository;
        private readonly ISqlSugarRepository<DeviceRepairRecordEntity> _deviceRepairRecordRepository;
        private readonly ISqlSugarRepository<MaintenanceRecordSpareEntity> _maintenanceRecordSpareRepository;
        private readonly ISqlSugarRepository<WarehouseReceiveEntity> _warehouseReceiveRepository;
        private readonly ISqlSugarRepository<DocumentEntity> _documentRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        //点检仓储
        private readonly ISqlSugarRepository<DeviceItemMainEntity> _deviceItemMainRepository;
        private readonly ISqlSugarRepository<DeviceItemSubEntity> _deviceItemSubRepository;
        private readonly ISqlSugarRepository<DeviceItemAllotEntity> _deviceItemAllotRepository;
        private readonly ISqlSugarRepository<DeviceCheckTaskEntity> _deviceCheckTaskRepository;
        private readonly ISqlSugarRepository<DeviceCheckEntity> _deviceCheckRepository;
        private readonly ISqlSugarRepository<DeviceCheckTaskVersionEntity> _deviceCheckTaskVersionRepository;

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
        /// 初始化一个<see cref="CheckService"/>类型的新实例.
        /// </summary>
        public CheckService(
            ISqlSugarRepository<DeviceMasterEntity> deviceMasterRepository,
            ISqlSugarRepository<DeviceTypeMasterEntity> deviceTypeMasterRepository,
            ISqlSugarRepository<OrganizeEntity> organizeRepository,
            ISqlSugarRepository<DictionaryTypeEntity> dictionaryTypeRepository,
            ISqlSugarRepository<MaintenanceRecordSpareEntity> maintenanceRecordSpareRepository,
            ISqlSugarRepository<DictionaryDataEntity> dictionaryDataRepository,
            ISqlSugarRepository<DeviceRepairEntity> deviceRepairRepository,
            ISqlSugarRepository<DeviceRepairRecordEntity> deviceRepairRecordRepository,
            ISqlSugarRepository<WarehouseReceiveEntity> warehouseReceiveRepository,
            ISqlSugarRepository<DocumentEntity> documentRepository,
            IWebHostEnvironment webHostEnvironment,
            ISqlSugarRepository<DeviceItemMainEntity> deviceItemMainRepository,
            ISqlSugarRepository<DeviceItemSubEntity> deviceItemSubRepository,
            ISqlSugarRepository<DeviceItemAllotEntity> deviceItemAllotRepository,
            ISqlSugarRepository<DeviceCheckTaskEntity> deviceCheckTaskRepository,
            ISqlSugarRepository<DeviceCheckEntity> deviceCheckRepository,
            ISqlSugarRepository<DeviceCheckTaskVersionEntity> deviceCheckTaskVersionRepository,
            ICacheManager cacheManager,
            IUserManager userManager,
            IFileManager fileManager)
        {
            _documentRepository = documentRepository;
            _deviceMasterRepository = deviceMasterRepository;
            _deviceTypeMasterRepository = deviceTypeMasterRepository;
            _dictionaryTypeRepository = dictionaryTypeRepository;
            _dictionaryDataRepository = dictionaryDataRepository;
            _warehouseReceiveRepository = warehouseReceiveRepository;
            _organizeRepository = organizeRepository;
            _deviceRepairRepository = deviceRepairRepository;
            _webHostEnvironment = webHostEnvironment;
            _deviceItemMainRepository = deviceItemMainRepository;
            _deviceItemSubRepository = deviceItemSubRepository;
            _deviceItemAllotRepository = deviceItemAllotRepository;
            _deviceCheckTaskRepository = deviceCheckTaskRepository;
            _deviceCheckRepository = deviceCheckRepository;
            _deviceCheckTaskVersionRepository = deviceCheckTaskVersionRepository;
            _maintenanceRecordSpareRepository = maintenanceRecordSpareRepository;
            _deviceRepairRecordRepository = deviceRepairRecordRepository;
            _cacheManager = cacheManager;
            _userManager = userManager;
            _fileManager = fileManager;
        }

        #region 检查项目

        /// <summary>
        /// 检查项目main添加
        /// </summary>
        /// <returns></returns>
        [HttpPost("item/insert")]
        public async Task<dynamic> MainInsert(CheckMainInsertInput input)
        {
            // 检查项目名称是否重复
            var existingItem = await _deviceItemMainRepository.GetSingleAsync(m =>
                m.DeleteMark == null &&
                m.ItemName == input.ItemName &&
                m.Id != input.Id);

            if (existingItem != null)
            {
                throw Oops.Oh(ErrorCode.D1006);
            }

            var dbClient = _deviceItemMainRepository.AsSugarClient();

            if (string.IsNullOrEmpty(input.Id))
            {
                // 新增
                var entity = input.Adapt<DeviceItemMainEntity>();
                int result = await dbClient.Insertable(entity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();

                return entity.Id;
            }
            else
            {
                // 编辑
                var existing = await _deviceItemMainRepository.GetSingleAsync(m => m.Id == input.Id);
                if (existing == null) throw Oops.Oh(ErrorCode.D6002);

                input.Adapt(existing);
                int result = await dbClient.Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true)
                    .CallEntityMethod(m => m.LastModify())
                    .ExecuteCommandAsync();
                return existing.Id;
            }
        }

        /// <summary>
        /// 检查项目父级添加
        /// </summary>
        /// <returns></returns>
        [HttpPost("item/parent/insert")]
        public async Task<dynamic> ParentInsert(CheckSubParentInsertInput input)
        {
            // 验证主表是否存在
            if (string.IsNullOrEmpty(input.MainId))
            {
                throw Oops.Oh(ErrorCode.D1002);
            }

            var mainExists = await _deviceItemMainRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == input.MainId);
            if (mainExists == null)
            {
                throw Oops.Oh(ErrorCode.D1002);
            }

            // 检查点检项是否重复（同一主表下）
            var existingItem = await _deviceItemSubRepository.GetSingleAsync(m =>
                m.DeleteMark == null &&
                m.CheckItem == input.CheckItem &&
                m.MainId == input.MainId &&
                m.Id != input.Id);

            if (existingItem != null)
            {
                throw Oops.Oh(ErrorCode.D1006); // 重复项错误
            }

            var dbClient = _deviceItemSubRepository.AsSugarClient();

            if (string.IsNullOrEmpty(input.Id))
            {
                // 新增逻辑
                var entity = input.Adapt<DeviceItemSubEntity>();
                entity.ParentId = "0"; // 父级项，ParentId为空
                int result = await dbClient.Insertable(entity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();
                return entity.Id;
            }
            else
            {
                // 编辑逻辑
                var existing = await _deviceItemSubRepository.GetSingleAsync(m =>
                    m.DeleteMark == null && m.Id == input.Id);
                if (existing == null) throw Oops.Oh(ErrorCode.D6002); // 记录不存在

                input.Adapt(existing);
                int result = await dbClient.Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true)
                    .CallEntityMethod(m => m.LastModify())
                    .ExecuteCommandAsync();
                return existing.Id;
            }

        }

        /// <summary>
        /// 检查项目子级添加
        /// </summary>
        /// <returns></returns>
        [HttpPost("item/son/insert")]
        public async Task<dynamic> SonInsert(CheckSubSonInsertInput input)
        {
            // 验证主表是否存在
            var mainExists = await _deviceItemMainRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == input.MainId);
            if (mainExists == null)
            {
                throw Oops.Oh(ErrorCode.D1003); // 主表不存在
            }

            // 验证父级记录是否存在
            var parentExists = await _deviceItemSubRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == input.ParentId);
            if (parentExists == null)
            {
                throw Oops.Oh(ErrorCode.D1005); // 父级记录不存在
            }

            // 检查点检项是否重复（同一父级下）
            var existingItem = await _deviceItemSubRepository.GetSingleAsync(m =>
                m.DeleteMark == null &&
                m.CheckItem == input.CheckItem &&
                m.ParentId == input.ParentId &&
                m.Id != input.Id);

            if (existingItem != null)
            {
                throw Oops.Oh(ErrorCode.D1006); // 重复项错误
            }

            var dbClient = _deviceItemSubRepository.AsSugarClient();

            if (string.IsNullOrEmpty(input.Id))
            {
                // 新增逻辑
                var entity = input.Adapt<DeviceItemSubEntity>();

                int result = await dbClient.Insertable(entity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();
                return entity.Id;
            }
            else
            {
                // 编辑逻辑
                var existing = await _deviceItemSubRepository.GetSingleAsync(m =>
                    m.DeleteMark == null && m.Id == input.Id);
                if (existing == null) throw Oops.Oh(ErrorCode.D6002); // 记录不存在

                input.Adapt(existing);
                int result = await dbClient.Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true)
                    .CallEntityMethod(m => m.LastModify())
                    .ExecuteCommandAsync();
                return existing.Id;

            }

        }

        /// <summary>
        /// 导入点检项目
        /// </summary>
        [HttpPost("item/import")]
        public async Task Import(IFormFile file)
        {
            //string path = Upload(file);
            string path = await Upload(file); // 添加 await
            // 使用NPOI读取Excel文件的所有Sheet
            var allSheetData = ReadExcelWithNPOI(path);

            // 2. 按设备分组处理
            var deviceGroups = allSheetData.GroupBy(x => new { x.DeviceNo, x.SheetName });

            foreach (var deviceGroup in deviceGroups)
            {
                var sheetData = deviceGroup.ToList();
                var firstItem = sheetData.First();

                // 3. 查询设备信息
                var deviceInfo = await _deviceMasterRepository.AsQueryable()
                    .Where(x => x.DeviceNo == firstItem.DeviceNo && x.DeleteMark == null)
                    .LeftJoin<DeviceTypeMasterEntity>((dm, dt) => dm.DeviceTypeId == dt.Id)
                    .Select((dm, dt) => new DeviceInfoDto
                    {
                        DeviceId = dm.Id,
                        DeviceNo = dm.DeviceNo,
                        DeviceName = dm.DeviceName,
                        DeviceTypeId = dt.Id,
                        DeviceTypeName = dt.DevicetypeName
                    }).FirstAsync();

                if (deviceInfo == null)
                {
                    throw new Exception($"未找到设备编号为 {firstItem.DeviceNo} 的设备信息");
                }

                // 4. 处理并保存数据
                await ProcessAndSaveData(sheetData, deviceInfo, firstItem.FormNo);
            }
        }

        /// <summary>
        /// 删除父级项目
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet("item/parent/delete")]
        public async Task ParentDelete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw Oops.Oh(ErrorCode.D1002); // ID为空
            }

            // 检查父级项目是否存在
            var parentEntity = await _deviceItemSubRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == Id && m.ParentId == "0");

            if (parentEntity == null)
            {
                throw Oops.Oh(ErrorCode.D1003); // 父级项目不存在
            }

            var children = await _deviceItemSubRepository.GetListAsync(m =>
            m.DeleteMark == null && m.ParentId == Id);

            await _deviceItemSubRepository.AsSugarClient().Updateable(children).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            await _deviceItemSubRepository.AsSugarClient().Updateable(parentEntity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 删除子级项目
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet("item/son/delete")]
        public async Task SonDelete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw Oops.Oh(ErrorCode.D1002); // ID为空
            }

            // 检查父级项目是否存在
            var parentEntity = await _deviceItemSubRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == Id);

            if (parentEntity == null)
            {
                throw Oops.Oh(ErrorCode.D1003);
            }

            await _deviceItemSubRepository.AsSugarClient().Updateable(parentEntity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 删除主项目
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet("item/maindelete")]
        public async Task MainDelete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw Oops.Oh(ErrorCode.D1002); // ID为空
            }

            // 检查主项目是否存在
            var mainEntity = await _deviceItemMainRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == Id);

            if (mainEntity == null)
            {
                throw Oops.Oh(ErrorCode.D1002); // 主项目不存在
            }

            // 检查是否存在关联的子项目
            var relatedSubItems = await _deviceItemSubRepository.GetListAsync(m =>
                m.DeleteMark == null && m.MainId == Id);

            // 删除关联的所有子项目（逻辑删除）
            if (relatedSubItems.Any())
            {
                await _deviceItemSubRepository.AsSugarClient().Updateable(relatedSubItems)
                    .CallEntityMethod(m => m.Delete())
                    .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                    .ExecuteCommandAsync();
            }

            // 删除主项目（逻辑删除）
            await _deviceItemMainRepository.AsSugarClient().Updateable(mainEntity)
                .CallEntityMethod(m => m.Delete())
                .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 分配设备
        /// </summary>
        /// <returns></returns>
        [HttpPost("item/allot")]
        public async Task Allot(CheckDeviceAllotInput input)
        {
            if (string.IsNullOrWhiteSpace(input.MainId))
            {
                throw Oops.Oh(ErrorCode.D1002); // 主表ID为空
            }

            if (input.DeviceId == null || !input.DeviceId.Any())
            {
                throw Oops.Oh(ErrorCode.D1002); // 设备列表为空
            }

            // 验证主表是否存在
            var mainExists = await _deviceItemMainRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == input.MainId);
            if (mainExists == null)
            {
                throw Oops.Oh(ErrorCode.D1003); // 主表不存在
            }

            var dbClient = _deviceItemAllotRepository.AsSugarClient();

            // 开启事务
            await dbClient.Ado.UseTranAsync(async () =>
            {
                // 获取当前已分配的设备
                var existingAllots = await _deviceItemAllotRepository.GetListAsync(m =>
                m.DeleteMark == null && m.MainId == input.MainId);

                var existingDeviceIds = existingAllots.Select(m => m.DeviceId).ToList();

                // 需要新增的设备
                var devicesToAdd = input.DeviceId.Except(existingDeviceIds).ToList();

                // 需要删除的设备
                var devicesToRemove = existingDeviceIds.Except(input.DeviceId).ToList();

                // 验证要删除的设备是否在点检任务中使用过
                if (devicesToRemove.Any())
                {
                    var usedDevices = await _deviceCheckTaskRepository.GetListAsync(m =>
                        m.DeleteMark == null &&
                        m.MainId == input.MainId &&
                        devicesToRemove.Contains(m.DeviceId));

                    if (usedDevices.Any())
                    {
                        throw Oops.Oh(ErrorCode.COM1034); // 设备已在点检任务中使用，不能删除
                    }
                }

                // 执行删除操作
                if (devicesToRemove.Any())
                {
                    var deleteResult = await dbClient.Updateable<DeviceItemAllotEntity>()
                        .Where(m => m.DeleteMark == null &&
                                   m.MainId == input.MainId &&
                                   devicesToRemove.Contains(m.DeviceId))
                        .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                        .CallEntityMethod(m => m.Delete())
                        .ExecuteCommandAsync();
                    if (deleteResult <= 0) throw Oops.Oh(ErrorCode.D6003);
                }

                // 执行新增操作
                if (devicesToAdd.Any())
                {
                    var entitiesToAdd = devicesToAdd.Select(deviceId => new DeviceItemAllotEntity
                    {
                        MainId = input.MainId,
                        DeviceId = deviceId
                    }).ToList();

                    var insertResult = await dbClient.Insertable(entitiesToAdd).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    if (insertResult <= 0) throw Oops.Oh(ErrorCode.D6001);
                }
            });
        }

        /// <summary>
        /// 获取已分配的设备列表
        /// </summary>
        /// <param name="mainId">项目主表ID</param>
        /// <returns></returns>
        [HttpGet("item/getallots")]
        public async Task<List<string>> GetAllottedDevices(string mainId)
        {
            if (string.IsNullOrWhiteSpace(mainId))
            {
                return new List<string>();
            }

            var allottedDevices = await _deviceItemAllotRepository.GetListAsync(m =>
                m.DeleteMark == null && m.MainId == mainId);

            return allottedDevices.Select(m => m.DeviceId).ToList();
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("item/getmains")]
        public async Task<dynamic> GetMains([FromBody] MainTypeQuery input)
        {
            // 构建查询条件
            var whereLambda = LinqExpression.And<DeviceItemMainEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            // 关键词查询
            if (input.keyword.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m =>
                    m.ItemName.Contains(input.keyword));
            }

            if (input.Type != "-1")
            {
                whereLambda = whereLambda.And(m => m.Type == input.Type);
            }

            var list = await _deviceItemMainRepository.AsQueryable()
           .Where(whereLambda)
           .LeftJoin<UserEntity>((p, user) => p.CreatorUserId == user.Id)
           .OrderByDescending(p => p.CreatorTime) // 按创建时间倒序
           .Select((p, user) => new DeviceItemMainOutput
           {
               Id = p.Id,
               ItemName = p.ItemName,
               Type = p.Type,
               Remark = p.Remark,
               Belongs = p.Belongs,
               CreatorUserId = user.RealName,
               CreatorTime = p.CreatorTime.Value.ToString("yyyy-MM-dd hh:mm:ss"),
           })
           .ToPagedListAsync(input.currentPage, input.pageSize);
            return list;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("item/getdetailbyid")]
        public async Task<dynamic> GetDetailById(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw Oops.Oh(ErrorCode.D1002); // ID为空
            }

            // 获取主表信息
            var mainEntity = await _deviceItemMainRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == Id);

            if (mainEntity == null)
            {
                throw Oops.Oh(ErrorCode.D1003); // 主表不存在
            }

            var result = mainEntity.Adapt<CheckMainByIdOutput>();

            // 获取所有子表数据
            var allSubEntities = await _deviceItemSubRepository.GetListAsync(m =>
                m.DeleteMark == null && m.MainId == Id);

            if (!allSubEntities.Any())
            {
                result.SubList = new List<CheckSubByMainIdOutput>();
                return result;
            }

            // 使用 AutoMapper 或 Mapster 进行映射
            var parentEntities = allSubEntities
                .Where(m => m.ParentId == "0")
                .Adapt<List<CheckSubByMainIdOutput>>();

            var sonEntities = allSubEntities
                .Where(m => !string.IsNullOrEmpty(m.ParentId))
                .Adapt<List<CheckSubSonByParentIdOutput>>();

            // 构建层级关系
            foreach (var parent in parentEntities)
            {
                parent.SonList = sonEntities
                    .Where(son => son.ParentId == parent.Id)
                    .ToList();
            }

            result.SubList = parentEntities;
            return result;
        }

        #endregion

        #region 检查内容

        /// <summary>
        /// 项目下拉框
        /// </summary>
        /// <returns></returns>
        [HttpGet("task/selectmain")]
        public async Task<dynamic> SelectMain()
        {
            var list = await _deviceItemMainRepository.AsQueryable()
            .Where(x => x.DeleteMark == null)
            .Select(p => new DeviceItemMainEntity
            {
                Id = p.Id,
                ItemName = p.ItemName,
                Type = p.Type,
                DeviceId = p.DeviceId,
                //Remark = p.Remark,
                //CreatorTime = p.CreatorTime,
            })
            .OrderBy(m => m.CreatorTime)
            .ToListAsync();
            return list;
        }

        /// <summary>
        /// 设备下拉框
        /// </summary>
        /// <returns></returns>
        [HttpGet("task/selectdevice")]
        public async Task<dynamic> SelectDevice(string mainId)
        {
            var query = _deviceItemAllotRepository.AsQueryable()
            .Where(x => x.DeleteMark == null && x.MainId == mainId);

            var joinedQuery = query
                .LeftJoin<DeviceMasterEntity>((da, dm) => da.DeviceId == dm.Id);

            var list = await joinedQuery
                 .Select((da, dm) => new {
                     Id = dm.Id,
                     Name = dm.DeviceName,
                     //CreatorTime = da.CreatorTime
                 })
                 .ToListAsync();

            return list;
        }

        /// <summary>
        /// 检查任务添加
        /// </summary>
        /// <returns></returns>
        [HttpPost("task/insert")]
        public async Task TaskInsert([FromBody] CheckTaskInsertInput input)
        {
            // 检查同一设备、类型、日期的任务是否存在，并生成递增版本号
            var existingTasks = await _deviceCheckTaskRepository.GetListAsync(m =>
                m.DeleteMark == null &&
                m.DeviceId == input.DeviceId &&
                m.CheckType == input.CheckType &&
                m.CheckDate == input.CheckDate);

            // 生成新版本号
            int newVersion = 1;
            if (existingTasks.Any())
            {
                var maxVersion = existingTasks
                    .Select(e => e.VersionName)
                    .Where(v => v.StartsWith("Version"))
                    .Select(v => int.TryParse(v.Substring(7), out int num) ? num : 0)
                    .Max();
                newVersion = maxVersion + 1;
            }

            // 创建新任务
            var entity = input.Adapt<DeviceCheckTaskEntity>();
            entity.VersionName = $"Version{newVersion}";
            entity.EnabledMark = 0;  //未启用
            entity.HasGeneratedFile = "0"; // 初始值设为未生成文件

            int result = await _deviceCheckTaskRepository.AsSugarClient().Insertable(entity)
                .CallEntityMethod(m => m.Creator())
                .ExecuteCommandAsync();

            if (result <= 0) throw Oops.Oh(ErrorCode.D6001);
        }

        /// <summary>
        /// 检查任务列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("task/gettasks")]
        public async Task<dynamic> GetTasks([FromBody] CheckTaskQuery input)
        {
            //关联用户表和设备表记得要分页  返回参数参考CheckTaskOutput
            var query = _deviceCheckTaskRepository.AsQueryable()
           .Where(x => x.DeleteMark == null)
           .LeftJoin<DeviceItemMainEntity>((task, main) => task.MainId == main.Id)
           .LeftJoin<DeviceMasterEntity>((task, main, device) => main.DeviceId == device.Id)
           .LeftJoin<UserEntity>((task, main, device, user) => task.CreatorUserId == user.Id);

            // 查询条件
            if (input.CheckType != -1)
            {
                query = query.Where((task, main, device, user) => task.CheckType == input.CheckType.Value);
            }

            if (input.DeviceId.IsNotEmptyOrNull())
            {
                query = query.Where((task, main, device, user) => device.DeviceName == input.DeviceId);
            }

            if (input.keyword.IsNotEmptyOrNull())
            {
                query = query.Where((task, main, device, user) => main.ItemName == input.keyword);
            }

            if (input.CheckStartDate.IsNotEmptyOrNull())
            {
                query = query.Where((task, main, device, user) => task.CheckDate.CompareTo(input.CheckStartDate) >= 0);
            }

            if (input.CheckEndDate.IsNotEmptyOrNull())
            {
                query = query.Where((task, main, device, user) => task.CheckDate.CompareTo(input.CheckEndDate) <= 0);
            }

            // 排序
            query = query.OrderBy((task, main, device, user) => task.CheckDate, OrderByType.Desc)
                         .OrderBy((task, main, device, user) => task.CreatorTime, OrderByType.Desc);
            // 分页查询
            var list = await query.Select((task, main, device, user) => new CheckTaskOutput
            {
                Id = task.Id,
                MainId = task.MainId,
                ItemName = main.ItemName,
                DeviceName = device.DeviceName,
                CheckType = task.CheckType,
                CheckDate = task.CheckDate,
                EnabledMark = task.EnabledMark,
                VersionName = task.VersionName,
                CreatorUserName = user.RealName,
                CreatorTime = task.CreatorTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                Note = task.Note
            }).ToPagedListAsync(input.currentPage, input.pageSize);

            return list;
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet("item/taskdelete")]
        public async Task TaskDelete(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw Oops.Oh(ErrorCode.D1002); // ID为空
            }

            // 检查任务是否存在
            var taskEntity = await _deviceCheckTaskRepository.GetSingleAsync(m =>
                m.DeleteMark == null && m.Id == Id);

            if (taskEntity == null)
            {
                throw Oops.Oh(ErrorCode.D1003); // 任务不存在
            }

            // 检查是否存在关联的版本记录
            var relatedVersions = await _deviceCheckTaskVersionRepository.GetListAsync(m =>
                m.DeleteMark == null && m.TaskId == Id);

            // 删除关联的所有版本记录（逻辑删除）
            if (relatedVersions.Any())
            {
                await _deviceCheckTaskVersionRepository.AsSugarClient().Updateable(relatedVersions)
                    .CallEntityMethod(m => m.Delete())
                    .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                    .ExecuteCommandAsync();
            }

            // 删除任务（逻辑删除）
            await _deviceCheckTaskRepository.AsSugarClient().Updateable(taskEntity)
                .CallEntityMethod(m => m.Delete())
                .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 检查任务状态变更
        /// </summary>
        /// <returns></returns>
        [HttpGet("task/status")]
        public async Task Status(string Id, int status)
        {
            // 获取当前任务信息
            var currentTask = await _deviceCheckTaskRepository.GetSingleAsync(m => m.Id == Id && m.DeleteMark == null);
            if (currentTask == null)
            {
                throw Oops.Oh(ErrorCode.COM1005);
            }

            // 如果要启用该任务
            if (status == 1)
            {
                // 检查同一设备、同一日期、同一点检类型是否已有启用版本
                var enabledTask = await _deviceCheckTaskRepository.GetSingleAsync(m =>
                    m.DeleteMark == null &&
                    m.DeviceId == currentTask.DeviceId &&
                    m.CheckDate == currentTask.CheckDate &&
                    m.CheckType == currentTask.CheckType &&
                    m.EnabledMark == 1 && // 启用状态
                    m.Id != Id); // 排除当前任务

                if (enabledTask != null)
                {
                    throw Oops.Oh(ErrorCode.COM1040);
                }
            }

            // 更新状态
            var result = await _deviceCheckTaskRepository.AsSugarClient()
                .Updateable<DeviceCheckTaskEntity>()
                .SetColumns(m => m.EnabledMark == status)
                .Where(m => m.Id == Id)
                .ExecuteCommandAsync();

            if (result <= 0)
            {
                throw Oops.Oh(ErrorCode.D6002);
            }

        }

        /// <summary>
        /// 获取检查版本
        /// </summary>
        /// <returns></returns>
        [HttpGet("task/version/getversions")]
        public async Task<dynamic> GetTaskVersions(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                throw Oops.Oh(ErrorCode.D1000);
            }

            var list = await _deviceCheckTaskVersionRepository.GetListAsync(m =>
                m.DeleteMark == null && m.TaskId == taskId);

            var result = list.Select(m => new CheckTaskVersionOutput
            {
                Id = m.Id,
                TaskId = m.TaskId,
                Revision = m.Revision,
                RevisionDate = m.RevisionDate,
                Reason = m.Reason,
                CreatedBy = m.CreatedBy,
                Approver = m.Approver,
                CreatorTime = m.CreatorTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
            }).OrderByDescending(m => m.CreatorTime).ToList();
            return result;
        }

        /// <summary>
        /// 任务版本添加、编辑
        /// </summary>
        /// <returns></returns>
        [HttpPost("task/version/insert")]
        public async Task TaskVersionInsert([FromBody] CheckTaskVersionInput input)
        {
            int result;
            if (input.Id.IsNullOrEmpty())
            {
                // 新增
                var entity = input.Adapt<DeviceCheckTaskVersionEntity>();
                result = await _deviceCheckTaskVersionRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();
            }
            else
            {
                // 编辑
                var entity = input.Adapt<DeviceCheckTaskVersionEntity>();
                result = await _deviceCheckTaskVersionRepository.AsSugarClient().Updateable(entity)
                    .IgnoreColumns(ignoreAllNullColumns: true)
                    .ExecuteCommandAsync();
            }
            if (result <= 0)
            {
                throw Oops.Oh(ErrorCode.D6001);
            }
        }

        /// <summary>
        /// 保存年限添加
        /// </summary>
        /// <returns></returns>
        [HttpGet("task/version/insertyear")]
        public async Task InsertYear(string taskId, string saveYear)
        {
            // 获取当前任务信息
            var currentTask = await _deviceCheckTaskRepository.GetSingleAsync(m => m.Id == taskId && m.DeleteMark == null);
            if (currentTask == null)
            {
                throw Oops.Oh(ErrorCode.COM1005);
            }
            // 更新年份
            var result = await _deviceCheckTaskRepository.AsSugarClient()
                .Updateable<DeviceCheckTaskEntity>()
                .SetColumns(m => m.SaveYears == saveYear)
                .Where(m => m.Id == taskId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 任务版本删除
        /// </summary>
        /// <returns></returns>
        [HttpGet("task/version/delete")]
        public async Task TaskVersionDelete(string Id)
        {
            var entity = await _deviceCheckTaskVersionRepository.GetSingleAsync(m => m.Id == Id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.D6000);
            }

            await _deviceCheckTaskVersionRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 获取检查内容(日)
        /// </summary>
        /// <returns></returns>
        [HttpGet("task/GetTaskDayDetailById")]
        public async Task<dynamic> GetTaskDayDetailById(string Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                throw Oops.Oh(ErrorCode.D1000);
            }

            // 1. 获取点检任务主信息（关联设备表）
            var taskQuery = _deviceCheckTaskRepository.AsQueryable()
                .Where(x => x.Id == Id && x.DeleteMark == null)
                .LeftJoin<DeviceMasterEntity>((task, device) => task.DeviceId == device.Id)
                .LeftJoin<DeviceTypeMasterEntity>((task, device, type) => device.DeviceTypeId == type.Id)
                .LeftJoin<OrganizeEntity>((task, device, type, dept) => device.UseDpt == dept.Id)
                .LeftJoin<DictionaryDataEntity>((task, device, type, dept, dic) => dic.Id == device.DeviceImportant)
                .LeftJoin<DeviceItemMainEntity>((task, device, type, dept, dic, main) => task.MainId == main.Id);

            var taskInfo = await taskQuery.Select((task, device, type, dept, dic, main) => new {
                Id = task.Id,
                MainId = task.MainId,
                Belongs = type.DevicetypeName,
                DeviceId = task.DeviceId,
                DeviceName = device.DeviceName,
                DeviceNo = device.DeviceNo,
                DeviceManufacturer = device.DeviceManufacturer,
                UseDpt = dept.FullName,
                CheckDate = task.CheckDate,
                CheckType = task.CheckType,
                SectionChiefId = task.SectionChiefId,
                TeamLeader = task.TeamLeader,
                ResponsiblePerson = task.ResponsiblePerson,
                WorkHour = task.WorkHour,
                SaveYears = task.SaveYears,
                FormNumber = main.Belongs,
                SpecialNotes = task.SpecialNotes,
                DeviceThumbnail = task.DeviceThumbnail,
                OverallJudgment = task.OverallJudgment,
                CheckPerson = task.CheckPerson,
                CheckDay = task.CheckDay,

                // 追加上checkperson1-31字段
                CheckPerson1 = task.CheckPerson1,
                CheckPerson2 = task.CheckPerson2,
                CheckPerson3 = task.CheckPerson3,
                CheckPerson4 = task.CheckPerson4,
                CheckPerson5 = task.CheckPerson5,
                CheckPerson6 = task.CheckPerson6,
                CheckPerson7 = task.CheckPerson7,
                CheckPerson8 = task.CheckPerson8,
                CheckPerson9 = task.CheckPerson9,
                CheckPerson10 = task.CheckPerson10,
                CheckPerson11 = task.CheckPerson11,
                CheckPerson12 = task.CheckPerson12,
                CheckPerson13 = task.CheckPerson13,
                CheckPerson14 = task.CheckPerson14,
                CheckPerson15 = task.CheckPerson15,
                CheckPerson16 = task.CheckPerson16,
                CheckPerson17 = task.CheckPerson17,
                CheckPerson18 = task.CheckPerson18,
                CheckPerson19 = task.CheckPerson19,
                CheckPerson20 = task.CheckPerson20,
                CheckPerson21 = task.CheckPerson21,
                CheckPerson22 = task.CheckPerson22,
                CheckPerson23 = task.CheckPerson23,
                CheckPerson24 = task.CheckPerson24,
                CheckPerson25 = task.CheckPerson25,
                CheckPerson26 = task.CheckPerson26,
                CheckPerson27 = task.CheckPerson27,
                CheckPerson28 = task.CheckPerson28,
                CheckPerson29 = task.CheckPerson29,
                CheckPerson30 = task.CheckPerson30,
                CheckPerson31 = task.CheckPerson31
            }).FirstAsync();

            if (taskInfo == null)
            {
                throw Oops.Oh(ErrorCode.D6000);
            }

            // 2. 获取父级点检项目（ParentId为null的项）
            var parentItems = await _deviceItemSubRepository.GetListAsync(m =>
                m.MainId == taskInfo.MainId &&
                m.ParentId == "0" &&
                m.DeleteMark == null);

            var parentResults = new List<DeviceCheckItemResult>();

            foreach (var parent in parentItems)
            {
                // 3. 获取每个父项下的子项
                var subItems = await _deviceItemSubRepository.GetListAsync(m =>
                    m.ParentId == parent.Id &&
                    m.DeleteMark == null);

                var subResults = new List<DeviceCheckItemSubResult>();

                foreach (var sub in subItems)
                {
                    // 4. 获取点检结果数据
                    var checkData = await _deviceCheckRepository.GetSingleAsync(m =>
                        m.TaskId == Id &&
                        m.SubId == sub.Id &&
                        m.DeleteMark == null);

                    subResults.Add(new DeviceCheckItemSubResult
                    {
                        Id = sub.Id,
                        CheckItem = sub.CheckItem,
                        CheckPlace = sub.CheckPlace,
                        CheckMethod = sub.CheckMethod,
                        JudgmentStandard = sub.JudgmentStandard,
                        ItemNo = sub.ItemNo,
                        CheckPerson = checkData?.CheckPerson, // 从点检结果表取
                        CheckResult = checkData?.CheckResult,
                        Judgment = checkData?.Judgment,
                        ExceptionDesc = checkData?.ExceptionDesc,
                        // 值字段映射
                        Value1 = checkData?.Value1,
                        Value2 = checkData?.Value2,
                        Value3 = checkData?.Value3,
                        Value4 = checkData?.Value4,
                        Value5 = checkData?.Value5,
                        Value6 = checkData?.Value6,
                        Value7 = checkData?.Value7,
                        Value8 = checkData?.Value8,
                        Value9 = checkData?.Value9,
                        Value10 = checkData?.Value10,
                        Value11 = checkData?.Value11,
                        Value12 = checkData?.Value12,
                        Value13 = checkData?.Value13,
                        Value14 = checkData?.Value14,
                        Value15 = checkData?.Value15,
                        Value16 = checkData?.Value16,
                        Value17 = checkData?.Value17,
                        Value18 = checkData?.Value18,
                        Value19 = checkData?.Value19,
                        Value20 = checkData?.Value20,
                        Value21 = checkData?.Value21,
                        Value22 = checkData?.Value22,
                        Value23 = checkData?.Value23,
                        Value24 = checkData?.Value24,
                        Value25 = checkData?.Value25,
                        Value26 = checkData?.Value26,
                        Value27 = checkData?.Value27,
                        Value28 = checkData?.Value28,
                        Value29 = checkData?.Value29,
                        Value30 = checkData?.Value30,
                        Value31 = checkData?.Value31
                    });
                }

                parentResults.Add(new DeviceCheckItemResult
                {
                    Id = parent.Id,
                    CheckItem = parent.CheckItem,
                    Sons = subResults
                });
            }

            // 5. 组合最终结果
            var result = new CheckTaskContentOutput
            {
                Id = taskInfo.Id,
                MainId = taskInfo.MainId,
                DeviceId = taskInfo.DeviceId,
                DeviceName = taskInfo.DeviceName,
                Belongs = taskInfo.Belongs,
                DeviceNo = taskInfo.DeviceNo,
                DeviceManufacturer = taskInfo.DeviceManufacturer,
                UseDpt = taskInfo.UseDpt,
                CheckDate = taskInfo.CheckDate,
                CheckType = taskInfo.CheckType,
                SectionChiefId = taskInfo.SectionChiefId,
                TeamLeader = taskInfo.TeamLeader,
                ResponsiblePerson = taskInfo.ResponsiblePerson,
                WorkHour = taskInfo.WorkHour,
                SaveYears = taskInfo.SaveYears,
                FormNumber = taskInfo.FormNumber,
                SpecialNotes = taskInfo.SpecialNotes,
                DeviceThumbnail = taskInfo.DeviceThumbnail,
                OverallJudgment = taskInfo.OverallJudgment,
                CheckPerson = taskInfo.CheckPerson,
                CheckDay = taskInfo.CheckDay,
                // 加上checkperson1-31字段
                CheckPerson1 = taskInfo.CheckPerson1,
                CheckPerson2 = taskInfo.CheckPerson2,
                CheckPerson3 = taskInfo.CheckPerson3,
                CheckPerson4 = taskInfo.CheckPerson4,
                CheckPerson5 = taskInfo.CheckPerson5,
                CheckPerson6 = taskInfo.CheckPerson6,
                CheckPerson7 = taskInfo.CheckPerson7,
                CheckPerson8 = taskInfo.CheckPerson8,
                CheckPerson9 = taskInfo.CheckPerson9,
                CheckPerson10 = taskInfo.CheckPerson10,
                CheckPerson11 = taskInfo.CheckPerson11,
                CheckPerson12 = taskInfo.CheckPerson12,
                CheckPerson13 = taskInfo.CheckPerson13,
                CheckPerson14 = taskInfo.CheckPerson14,
                CheckPerson15 = taskInfo.CheckPerson15,
                CheckPerson16 = taskInfo.CheckPerson16,
                CheckPerson17 = taskInfo.CheckPerson17,
                CheckPerson18 = taskInfo.CheckPerson18,
                CheckPerson19 = taskInfo.CheckPerson19,
                CheckPerson20 = taskInfo.CheckPerson20,
                CheckPerson21 = taskInfo.CheckPerson21,
                CheckPerson22 = taskInfo.CheckPerson22,
                CheckPerson23 = taskInfo.CheckPerson23,
                CheckPerson24 = taskInfo.CheckPerson24,
                CheckPerson25 = taskInfo.CheckPerson25,
                CheckPerson26 = taskInfo.CheckPerson26,
                CheckPerson27 = taskInfo.CheckPerson27,
                CheckPerson28 = taskInfo.CheckPerson28,
                CheckPerson29 = taskInfo.CheckPerson29,
                CheckPerson30 = taskInfo.CheckPerson30,
                CheckPerson31 = taskInfo.CheckPerson31,
                Parents = parentResults
            };
            return result;
        }

        /// <summary>
        /// 获取检查内容(月年)
        /// </summary>
        /// <returns></returns>
        [HttpGet("task/GetTaskYearDetailById")]
        public async Task<dynamic> GetTaskYearDetailById(string Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                throw Oops.Oh(ErrorCode.D1000);
            }

            // 1. 获取点检任务主信息（关联设备表）
            var taskQuery = _deviceCheckTaskRepository.AsQueryable()
                .Where(x => x.Id == Id && x.DeleteMark == null)
                .LeftJoin<DeviceMasterEntity>((task, device) => task.DeviceId == device.Id)
                .LeftJoin<DeviceTypeMasterEntity>((task, device, type) => device.DeviceTypeId == type.Id)
                .LeftJoin<OrganizeEntity>((task, device, type, dept) => device.UseDpt == dept.Id)
                .LeftJoin<DictionaryDataEntity>((task, device, type, dept, dic) => dic.Id == device.DeviceImportant)
                .LeftJoin<DeviceItemMainEntity>((task, device, type, dept, dic, main) => task.MainId == main.Id);

            var taskInfo = await taskQuery.Select((task, device, type, dept, dic, main) => new {
                Id = task.Id,
                MainId = task.MainId,
                Belongs = type.DevicetypeName,
                DeviceId = task.DeviceId,
                DeviceName = device.DeviceName,
                DeviceNo = device.DeviceNo,
                DeviceManufacturer = device.DeviceManufacturer,
                DeviceImportant = dic.FullName,
                UseDpt = dept.FullName,
                CheckDate = task.CheckDate,//点检年月日
                CheckType = task.CheckType,
                SectionChiefId = task.SectionChiefId,
                TeamLeader = task.TeamLeader,
                ResponsiblePerson = task.ResponsiblePerson,
                WorkHour = task.WorkHour,
                SaveYears = task.SaveYears,
                FormNumber = main.Belongs,
                SpecialNotes = task.SpecialNotes,
                DeviceThumbnail = task.DeviceThumbnail,
                OverallJudgment = task.OverallJudgment,
                CheckPerson = task.CheckPerson,
                CheckDay = task.CheckDay,  //点检日
                CheckPerson1 = task.CheckPerson1  //第二个点检者
            }).FirstAsync();

            if (taskInfo == null)
            {
                throw Oops.Oh(ErrorCode.D6000);
            }

            // 2. 获取父级点检项目（ParentId为null的项）
            var parentItems = await _deviceItemSubRepository.GetListAsync(m =>
                m.MainId == taskInfo.MainId &&
                m.ParentId == "0" &&
                m.DeleteMark == null);

            var parentResults = new List<DeviceCheckItemResult>();

            foreach (var parent in parentItems)
            {
                // 3. 获取每个父项下的子项
                var subItems = await _deviceItemSubRepository.GetListAsync(m =>
                    m.ParentId == parent.Id &&
                    m.DeleteMark == null);

                var subResults = new List<DeviceCheckItemSubResult>();

                foreach (var sub in subItems)
                {
                    // 4. 获取点检结果数据
                    var checkData = await _deviceCheckRepository.GetSingleAsync(m =>
                        m.TaskId == Id &&
                        m.SubId == sub.Id &&
                        m.DeleteMark == null);

                    subResults.Add(new DeviceCheckItemSubResult
                    {
                        Id = sub.Id,
                        CheckItem = sub.CheckItem,
                        CheckPlace = sub.CheckPlace,
                        CheckMethod = sub.CheckMethod,
                        JudgmentStandard = sub.JudgmentStandard,
                        ItemNo = sub.ItemNo,
                        //CheckPerson = checkData?.CheckPerson, // 从点检结果表取
                        CheckResult = checkData?.CheckResult,
                        Judgment = checkData?.Judgment,
                        ExceptionDesc = checkData?.ExceptionDesc,
                    });
                }

                parentResults.Add(new DeviceCheckItemResult
                {
                    Id = parent.Id,
                    CheckItem = parent.CheckItem,
                    Sons = subResults
                });
            }

            // 5. 组合最终结果
            var result = new CheckTaskContentOutput
            {
                Id = taskInfo.Id,
                MainId = taskInfo.MainId,
                DeviceId = taskInfo.DeviceId,
                DeviceName = taskInfo.DeviceName,
                Belongs = taskInfo.Belongs,
                DeviceNo = taskInfo.DeviceNo,
                DeviceManufacturer = taskInfo.DeviceManufacturer,
                ImportantLevel = taskInfo.DeviceImportant,
                UseDpt = taskInfo.UseDpt,
                CheckDate = taskInfo.CheckDate,
                CheckType = taskInfo.CheckType,
                SectionChiefId = taskInfo.SectionChiefId,
                TeamLeader = taskInfo.TeamLeader,
                ResponsiblePerson = taskInfo.ResponsiblePerson,
                WorkHour = taskInfo.WorkHour,
                SaveYears = taskInfo.SaveYears,
                FormNumber = taskInfo.FormNumber,
                SpecialNotes = taskInfo.SpecialNotes,
                DeviceThumbnail = taskInfo.DeviceThumbnail,
                OverallJudgment = taskInfo.OverallJudgment,
                CheckPerson = taskInfo.CheckPerson,
                CheckDay = taskInfo.CheckDay,
                CheckPerson1 = taskInfo.CheckPerson1,
                Parents = parentResults
            };
            return result;
        }

        /// <summary>
        /// 点检主信息入库
        /// </summary>
        /// <returns></returns>
        [HttpPost("update")]
        public async Task Update([FromBody] CheckTaskContentInput input)
        {
            // 检查点检任务是否存在
            var existingTask = await _deviceCheckTaskRepository.GetSingleAsync(m =>
                m.Id == input.Id && m.DeleteMark == null);

            if (existingTask == null)
            {
                throw Oops.Oh(ErrorCode.D6000);
            }

            // 使用AutoMapper或Adapt进行对象映射
            var entity = input.Adapt<DeviceCheckTaskEntity>();

            // 更新操作
            int result = await _deviceCheckTaskRepository.AsSugarClient()
                .Updateable(entity)
                .IgnoreColumns(ignoreAllNullColumns: true) // 忽略空值，只更新有值的字段
                .Where(m => m.Id == input.Id)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 点检项入库
        /// </summary>
        /// <returns></returns>
        [HttpPost("point")]
        public async Task Point([FromBody] CheckTaskContentItemInput input)
        {
            // 检查点检任务是否存在
            //var existingTask = await _deviceCheckTaskRepository.GetSingleAsync(m =>
            //    m.Id == input.Id && m.DeleteMark == null);

            //if (existingTask == null)
            //{
            //    throw Oops.Oh(ErrorCode.D6000);
            //}

            // 检查点检项是否存在
            var existingSubItem = await _deviceItemSubRepository.GetSingleAsync(m =>
                m.Id == input.SubId && m.DeleteMark == null);

            if (existingSubItem == null)
            {
                throw Oops.Oh("检查项不存在");
            }

            var existingCheck = await _deviceCheckRepository.GetSingleAsync(m =>
            m.TaskId == input.Id &&
            m.SubId == input.SubId &&
            m.DeleteMark == null);

            int result;

            if (existingCheck == null)
            {
                // 新增点检记录
                var entity = new DeviceCheckEntity
                {
                    TaskId = input.Id,
                    SubId = input.SubId,
                    CheckPerson = input.CheckPerson,
                    CheckResult = input.CheckResult,
                    Judgment = input.Judgment,
                    ExceptionDesc = input.ExceptionDesc
                };

                // 动态设置值字段
                SetValueField(entity, input.PropDateName, input.Value);

                result = await _deviceCheckRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();
            }
            else
            {
                // 更新点检记录 - 使用动态更新方式
                var update = _deviceCheckRepository.AsSugarClient()
                    .Updateable<DeviceCheckEntity>()
                    .SetColumns(m => new DeviceCheckEntity
                    {
                        CheckPerson = input.CheckPerson,
                        CheckResult = input.CheckResult,
                        Judgment = input.Judgment,
                        ExceptionDesc = input.ExceptionDesc
                    });

                // 动态设置值字段的更新
                update = SetValueFieldUpdate(update, input.PropDateName, input.Value);

                result = await update
                    .Where(m => m.TaskId == input.Id && m.SubId == input.SubId)
                    .ExecuteCommandAsync();
            }

        }

        /// <summary>
        /// 生成文档
        /// </summary>
        /// <returns></returns>
        [HttpGet("generate")]
        [AllowAnonymous]
        public async Task<dynamic> Generate(string Id, int checkType)
        {
            if (string.IsNullOrEmpty(Id))
            {
                throw Oops.Oh(ErrorCode.D1000);
            }

            if (checkType < 0)
            {
                throw Oops.Oh(ErrorCode.D1000);
            }

            // 1. 获取点检任务详情数据
            CheckTaskContentOutput taskDetail;
            switch (checkType)
            {
                case 0:
                    taskDetail = await GetTaskDayDetailById(Id);
                    break;
                case 1:
                    // 如果没有专门的月点检方法，可以使用日点检或年点检方法
                    taskDetail = await GetTaskYearDetailById(Id); // 或者创建月点检方法
                    break;
                case 2:
                    taskDetail = await GetTaskYearDetailById(Id);
                    break;
                default:
                    taskDetail = await GetTaskDayDetailById(Id);
                    break;
            }

            if (taskDetail == null)
            {
                throw Oops.Oh(ErrorCode.D6000);
            }

            // 2. 根据点检类型选择模板文件
            string templateFileName = checkType switch
            {
                0 => "（保全）日_设备点检保养.xlsx",
                1 => "（保全）月_设备点检保养.xlsx",
                2 => "（保全）年_设备点检保养.xlsx",
            };

            string templatePath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot", "SoftTemplate", templateFileName);
            if (!File.Exists(templatePath))
            {
                throw Oops.Oh($"点检模板文件不存在: {templateFileName}");
            }

            // 3. 创建临时文件
            string tempDir = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot", "TempFiles");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            // 生成带时间戳和任务ID的文件名
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(templatePath);
            string fileExtension = Path.GetExtension(templatePath);
            string fileName = $"{fileNameWithoutExt}_{taskDetail.DeviceNo}_{timestamp}{fileExtension}";
            string tempFilePath = Path.Combine(tempDir, fileName);
            File.Copy(templatePath, tempFilePath, overwrite: true);

            byte[] templateBytes;
            using (var templateStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                templateBytes = new byte[templateStream.Length];
                templateStream.Read(templateBytes, 0, (int)templateStream.Length);
            }
            var memoryStream = new MemoryStream(templateBytes);
            var workbook = new XSSFWorkbook(memoryStream);
            var sheet = workbook.GetSheetAt(0);

            if (checkType == 0)
            {
                FillBasicForDayInfo(sheet, taskDetail);
                // 填充点检项目数据，并获取增加的行数
                int addedRows = FillCheckItemsData(sheet, taskDetail.Parents);
                // 填充点检版本数据，传入增加的行数
                var versions = await _deviceCheckTaskVersionRepository.GetListAsync(m => m.TaskId == Id && m.DeleteMark == null);
                FillCheckVersionsData(sheet, versions, addedRows);
            }
            else
            {
                // 月和年检逻辑
                FillBasicForMonthInfo(sheet, taskDetail); // 年点检可能使用与月点检相同的基础信息
                int addedRows = FillMonthCheckItemsData(sheet, taskDetail.Parents);
                var versions = await _deviceCheckTaskVersionRepository.GetListAsync(m => m.TaskId == Id && m.DeleteMark == null);
                FillMonthCheckVersionsData(sheet, versions, addedRows);
            }

            try
            {
                string fileSize = string.Empty;
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fileStream);
                    //fileSize = fileStream.Length.ToString(); // 在写入时直接获取文件大小
                }

                workbook?.Close();
                memoryStream?.Dispose();

                if (File.Exists(tempFilePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(tempFilePath);
                    fileSize = fileBytes.Length.ToString();
                }

                //将生成的文件放在指定文件路径下
                var targetDirectory = @"D:\wwwroot\net_Resources\DocumentFile";
                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                var targetFilePath = Path.Combine(targetDirectory, fileName);
                File.Move(tempFilePath, targetFilePath);

                // 构建新的URL路径
                string newUrl = $"/api/file/Image/document/{Path.GetFileName(targetFilePath)}";

                // 删除临时文件（如果移动成功）
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                string parentId = checkType switch
                {
                    0 => "1982987176916422656",
                    1 => "1982987196411547648",
                    2 => "1982987770766954496",
                };

                var DocEntity = new DocumentEntity() { FullName = fileName, Type = 1, ParentId = parentId, EnabledMark = 1, FileExtension = fileExtension, FileSize = fileSize, TenantId = "0", UploadUrl = newUrl, FilePath = fileName, Description = "" };
                await _documentRepository.AsSugarClient().Insertable(DocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                await _deviceCheckTaskRepository.AsSugarClient()
                 .Updateable<DeviceCheckTaskEntity>()
                 .SetColumns(m => m.HasGeneratedFile == "1")
                 .SetColumns(m => m.GenerateCount == m.GenerateCount + 1)
                 .Where(m => m.Id == Id)
                 .ExecuteCommandAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #region 月年点检私有函数

        /// <summary>
        /// 填充月点检基本信息
        /// </summary>
        private void FillBasicForMonthInfo(ISheet sheet, CheckTaskContentOutput taskDetail)
        {
            // 使用字典映射字段名和值
            var fieldMappings = new Dictionary<string, string>
           {
           { "Belongs", taskDetail.Belongs },
           { "DeviceName", taskDetail.DeviceName },
           { "DeviceManufacturer", taskDetail.DeviceManufacturer },
           { "DeviceNo", taskDetail.DeviceNo },
           { "ImportantLevel", taskDetail.ImportantLevel },
           { "FormNumber", taskDetail.FormNumber },
           { "OverallJudgment", taskDetail.OverallJudgment },
           { "ResponsiblePerson", taskDetail.ResponsiblePerson },
           { "CheckPerson", taskDetail.CheckPerson },
           { "CheckDay", taskDetail.CheckDay },
           { "CheckDate", taskDetail.CheckDate },
           { "CheckPerson1", taskDetail.CheckPerson1 },
           { "WorkHour", taskDetail.WorkHour },
           { "SpecialNotes", taskDetail.SpecialNotes },
           { "SaveYears", taskDetail.SaveYears }
           };

            // 遍历所有单元格，替换 {字段名} 为实际值
            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                for (int j = 0; j < row.LastCellNum; j++)
                {
                    var cell = row.GetCell(j);
                    if (cell != null)
                    {
                        string cellValue = cell.ToString();
                        if (!string.IsNullOrEmpty(cellValue) && cellValue.Contains("{"))
                        {
                            foreach (var mapping in fieldMappings)
                            {
                                string searchText = "{" + mapping.Key + "}";
                                if (cellValue.Contains(searchText))
                                {
                                    string newValue = cellValue.Replace(searchText, mapping.Value ?? "");
                                    cell.SetCellValue(newValue);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            // 在 C9 单元格插入设备缩略图
            InsertDeviceThumbnail(sheet, taskDetail.DeviceThumbnail);
        }

        /// <summary>
        /// 在 C9 单元格插入设备缩略图
        /// </summary>
        private void InsertDeviceThumbnail(ISheet sheet, string deviceThumbnail)
        {
            if (string.IsNullOrEmpty(deviceThumbnail))
            {
                return;
            }

            // 构建完整的图片路径
            string imagePath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot" + deviceThumbnail);

            if (!File.Exists(imagePath))
            {
                return;
            }

            // 读取图片文件
            byte[] imageBytes = File.ReadAllBytes(imagePath);

            // 添加图片到工作簿
            int pictureIndex = sheet.Workbook.AddPicture(imageBytes, PictureType.JPEG);

            // 获取图片创建辅助类
            var helper = sheet.Workbook.GetCreationHelper();
            var drawing = sheet.CreateDrawingPatriarch();

            // C9 单元格位置（行8，列2）
            int rowIndex = 8;  // 第9行，索引从0开始
            int colIndex = 2;  // C列，索引从0开始

            // 创建锚点，设置图片位置和大小
            var anchor = helper.CreateClientAnchor();
            anchor.Col1 = colIndex;      // 起始列
            anchor.Row1 = rowIndex;      // 起始行
            anchor.Col2 = colIndex + 9;  // 结束列（宽度）
            anchor.Row2 = rowIndex + 19; // 结束行（高度）

            // 插入图片
            var picture = drawing.CreatePicture(anchor, pictureIndex);
        }

        /// <summary>
        /// 填充月点检项目数据
        /// </summary>
        private int FillMonthCheckItemsData(ISheet sheet, List<DeviceCheckItemResult> parents)
        {
            int startRow = 28; // 月点检模板从第26行开始是点检项目数据
            int dataRowIndex = startRow;

            foreach (var parentItem in parents)
            {
                // 记录父项开始行
                int parentStartRow = dataRowIndex;

                // 为每个父项创建新行
                var row = GetOrCreateRow(sheet, dataRowIndex);

                // B列填充分类（父项名称）
                SetCellValue(row, 1, parentItem.CheckItem);

                // 填充所有子项
                foreach (var subItem in parentItem.Sons)
                {
                    // 第一个子项使用父项所在行，后续子项创建新行
                    if (subItem != parentItem.Sons.First())
                    {
                        dataRowIndex++;
                        row = GetOrCreateRow(sheet, dataRowIndex);
                    }

                    // D列填充点检场所
                    SetCellValue(row, 3, subItem.CheckPlace);

                    // F列填充No
                    SetCellValue(row, 6, subItem.ItemNo);

                    // G列填充点检项目
                    SetCellValue(row, 7, subItem.CheckItem);

                    // I列填充点检方法
                    SetCellValue(row, 10, subItem.CheckMethod);

                    // L列填充判定基准
                    SetCellValue(row, 12, subItem.JudgmentStandard);

                    // N列填充点检结果
                    SetCellValue(row, 14, subItem.CheckResult);

                    // P列填充判定
                    SetCellValue(row, 16, subItem.Judgment);

                    // Q列填充异常项目描述
                    SetCellValue(row, 17, subItem.ExceptionDesc);
                }

                // 记录父项结束行
                int parentEndRow = dataRowIndex;

                RemoveConflictingMergedRegions(sheet, parentStartRow, parentEndRow, 1, 2);

                // 合并BC列，从父项开始行到结束行
                MergeParentCells(sheet, parentStartRow, parentEndRow, 1, 2);

                dataRowIndex++; // 移动到下一父项
            }

            // 计算增加的行数
            int totalRowsUsed = dataRowIndex - startRow;
            int originalExpectedRows = parents.Count;
            int addedRows = totalRowsUsed - originalExpectedRows;

            return Math.Max(0, addedRows);
        }

        /// <summary>
        /// 移除冲突的合并区域
        /// </summary>
        private void RemoveConflictingMergedRegions(ISheet sheet, int startRow, int endRow, int startCol, int endCol)
        {
            for (int i = sheet.NumMergedRegions - 1; i >= 0; i--)
            {
                var mergedRegion = sheet.GetMergedRegion(i);

                // 检查是否存在重叠的合并区域
                bool overlaps = (mergedRegion.FirstRow <= endRow && mergedRegion.LastRow >= startRow) &&
                               (mergedRegion.FirstColumn <= endCol && mergedRegion.LastColumn >= startCol);

                if (overlaps)
                {
                    sheet.RemoveMergedRegion(i);
                }
            }
        }

        /// <summary>
        /// 合并父级单元格（多列合并）
        /// </summary>
        private void MergeParentCells(ISheet sheet, int startRow, int endRow, int startCol, int endCol)
        {
            // 只有当开始行和结束行不同时才需要合并
            if (startRow < endRow)
            {
                try
                {
                    var mergeRegion = new NPOI.SS.Util.CellRangeAddress(startRow, endRow, startCol, endCol);
                    sheet.AddMergedRegion(mergeRegion);

                    // 设置合并后单元格的垂直居中样式
                    var row = sheet.GetRow(startRow);
                    if (row != null)
                    {
                        var cell = row.GetCell(startCol);
                        if (cell != null)
                        {
                            var style = cell.CellStyle;
                            style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                            cell.CellStyle = style;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录合并失败，但不中断程序
                    Console.WriteLine($"合并单元格失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 填充月点检版本数据
        /// </summary>
        private void FillMonthCheckVersionsData(ISheet sheet, List<DeviceCheckTaskVersionEntity> versions, int addedRowsFromItems)
        {
            if (addedRowsFromItems == 1)
            {
                addedRowsFromItems = 0;
            }

            int baseStartRow = 41; // 月点检模板版本信息基础起始行（第46行）
            int startRow = baseStartRow + addedRowsFromItems; // 动态计算起始行

            int maxRecords = 4; // 最多4条记录

            // 列索引定义（月点检模板）
            const int COL_REVISION = 13;     // N列：改版
            const int COL_REVISION_DATE = 14; // O列：年月日
            const int COL_REASON = 15;       // P列：理由
            const int COL_CREATED_BY = 17;   // R列：做成
            const int COL_APPROVER = 18;     // S列：承认

            // 取前4条数据
            var versionList = versions.Take(maxRecords).ToList();

            for (int i = 0; i < versionList.Count; i++)
            {
                var version = versionList[i];
                int currentRow = startRow + i;

                // 获取或创建行
                var row = sheet.GetRow(currentRow) ?? sheet.CreateRow(currentRow);

                // 填充各列数据
                SetCellValue(row, COL_REVISION, version.Revision);         // N列：改版
                SetCellValue(row, COL_REVISION_DATE, version.RevisionDate); // O列：年月日
                SetCellValue(row, COL_REASON, version.Reason);             // P列：理由
                SetCellValue(row, COL_CREATED_BY, version.CreatedBy);      // R列：做成
                SetCellValue(row, COL_APPROVER, version.Approver);         // S列：承认
            }
        }

        #endregion

        #region 日点检私有函数

        /// <summary>
        /// 填充基本信息
        /// </summary>
        private void FillBasicForDayInfo(ISheet sheet, CheckTaskContentOutput taskDetail)
        {
            // 使用字典映射字段名和值
            var fieldMappings = new Dictionary<string, string>
            {
               { "大所属", taskDetail.Belongs}, // 根据实际情况调整
               { "设备名称", taskDetail.DeviceName },
               { "设备厂家", taskDetail.DeviceManufacturer },
               { "设备编号", taskDetail.DeviceNo },
               { "所属", taskDetail.UseDpt },
               { "年月日", taskDetail.CheckDate },
               { "课长", taskDetail.SectionChiefId },
               { "班长", taskDetail.TeamLeader },
               { "担当", taskDetail.ResponsiblePerson },
               { "点检者", taskDetail.CheckPerson },
               { "点检日", taskDetail.CheckDay },
               { "特记事项", taskDetail.SpecialNotes },
               { "版本年", taskDetail.SaveYears },
               { "表单编号", taskDetail.FormNumber },
               { "综合判定结果", taskDetail.OverallJudgment },
           };

            // 遍历所有单元格，替换 {字段名} 为实际值
            for (int i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                for (int j = 0; j < row.LastCellNum; j++)
                {
                    var cell = row.GetCell(j);
                    if (cell != null)
                    {
                        string cellValue = cell.ToString();
                        if (!string.IsNullOrEmpty(cellValue) && cellValue.Contains("{"))
                        {
                            foreach (var mapping in fieldMappings)
                            {
                                string searchText = "{" + mapping.Key + "}";
                                if (cellValue.Contains(searchText))
                                {
                                    string newValue = cellValue.Replace(searchText, mapping.Value ?? "");
                                    cell.SetCellValue(newValue);
                                    break; // 一个单元格只替换一个字段
                                }
                            }
                        }
                    }
                }
            }

            // 在第33行的E列开始填充点检者数据
            FillCheckPersonsRow(sheet, taskDetail);
        }

        /// <summary>
        /// 在第33行E列开始填充点检者数据
        /// </summary>
        private void FillCheckPersonsRow(ISheet sheet, CheckTaskContentOutput taskDetail)
        {
            int targetRow = 32; // 第33行，索引从0开始
            int startCol = 4;   // E列，索引为4

            var row = sheet.GetRow(targetRow);
            if (row == null)
            {
                row = sheet.CreateRow(targetRow);
            }

            // 使用反射获取所有CheckPerson属性并按数字排序
            var checkPersonProperties = typeof(CheckTaskContentOutput).GetProperties()
                .Where(p => p.Name.StartsWith("CheckPerson") && p.Name != "CheckPerson")
                .OrderBy(p =>
                {
                    var numberStr = p.Name.Substring(11); // "CheckPerson".Length = 11
                    return int.Parse(numberStr);
                })
                .ToList();

            // 从E列开始填充点检者数据
            for (int i = 0; i < checkPersonProperties.Count && i < 31; i++) // 最多31个点检者
            {
                var value = checkPersonProperties[i].GetValue(taskDetail)?.ToString();
                SetCellValue(row, startCol + i, value);
            }
        }

        private int FillCheckItemsData(ISheet sheet, List<DeviceCheckItemResult> parents)
        {
            int startRow = 6; // 从第7行开始
            int initialLastRow = 25; // 第26行是初始最后一行
            int originalDataRowCount = 20; // 26 - 6 = 20行原始数据区域

            int currentRow = startRow;

            // 计算总行数需求
            int totalRequiredRows = parents.Sum(p => Math.Max(1, p.Sons.Count));

            // 填充数据
            foreach (var parentItem in parents)
            {
                int parentStartRow = currentRow;
                var row = GetOrCreateRow(sheet, currentRow);
                SetCellValue(row, 1, parentItem.CheckItem);

                // 处理子项
                foreach (var subItem in parentItem.Sons)
                {
                    if (subItem != parentItem.Sons.First())
                    {
                        currentRow++;
                        row = GetOrCreateRow(sheet, currentRow);
                    }
                    SetCellValue(row, 2, subItem.CheckItem);
                    FillCheckValues(row, subItem);
                }

                // 合并父项单元格
                if (parentItem.Sons.Count > 1)
                {
                    MergeParentCell(sheet, parentStartRow, currentRow, 1);
                }

                currentRow++;
            }

            // 实际使用的行数 = 当前行 - 起始行
            int actualUsedRows = currentRow - startRow;

            // 增加的行数 = 实际使用的行数 - 原始数据行数
            int addedRows = actualUsedRows - originalDataRowCount;

            return Math.Max(0, addedRows);
        }

        /// <summary>
        /// 填充点检版本数据（动态计算起始行）
        /// </summary>
        private void FillCheckVersionsData(ISheet sheet, List<DeviceCheckTaskVersionEntity> versions, int addedRowsFromItems)
        {

            int baseStartRow = 36; // 基础起始行（第37行）
            int startRow = baseStartRow + addedRowsFromItems; // 动态计算起始行

            int maxRecords = 7; // 最多7条记录

            // 列索引定义
            const int COL_REVISION = 21;     // V列：改版
            const int COL_REVISION_DATE = 23; // X列：年月日
            const int COL_REASON = 25;       // Z列：理由
            const int COL_CREATED_BY = 31;   // AF列：做成
            const int COL_APPROVER = 33;     // AH列：承认者

            // 取前7条数据
            var versionList = versions.Take(maxRecords).ToList();

            for (int i = 0; i < versionList.Count; i++)
            {
                var version = versionList[i];
                int currentRow = startRow + i;

                // 获取或创建行
                var row = sheet.GetRow(currentRow) ?? sheet.CreateRow(currentRow);

                // 填充各列数据
                SetCellValue(row, COL_REVISION, version.Revision);         // V列：改版
                SetCellValue(row, COL_REVISION_DATE, version.RevisionDate); // X列：年月日
                SetCellValue(row, COL_REASON, version.Reason);             // Z列：理由
                SetCellValue(row, COL_CREATED_BY, version.CreatedBy);      // AF列：做成
                SetCellValue(row, COL_APPROVER, version.Approver);         // AH列：承认者
            }

        }

        /// <summary>
        /// 合并父级单元格
        /// </summary>
        /// <param name="sheet">工作表</param>
        /// <param name="startRow">开始行索引</param>
        /// <param name="endRow">结束行索引</param>
        /// <param name="columnIndex">列索引（B列为1）</param>
        private void MergeParentCell(ISheet sheet, int startRow, int endRow, int columnIndex)
        {
            // 只有当开始行和结束行不同时才需要合并
            if (startRow < endRow)
            {
                var mergeRegion = new NPOI.SS.Util.CellRangeAddress(startRow, endRow, columnIndex, columnIndex);
                sheet.AddMergedRegion(mergeRegion);

                // 可选：设置合并后单元格的垂直居中样式
                var row = sheet.GetRow(startRow);
                if (row != null)
                {
                    var cell = row.GetCell(columnIndex);
                    if (cell != null)
                    {
                        var style = cell.CellStyle;
                        style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                        cell.CellStyle = style;
                    }
                }
            }
        }

        /// <summary>
        /// 获取或创建行（复制第32行的样式）
        /// </summary>
        private IRow GetOrCreateRow(ISheet sheet, int rowIndex)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null)
            {
                row = sheet.CreateRow(rowIndex);

                // 复制第32行的样式到新行
                var templateRow = sheet.GetRow(32);
                if (templateRow != null)
                {
                    for (int i = 0; i < templateRow.LastCellNum; i++)
                    {
                        var templateCell = templateRow.GetCell(i);
                        if (templateCell != null)
                        {
                            var newCell = row.CreateCell(i);
                            newCell.CellStyle = templateCell.CellStyle;
                        }
                    }
                }
            }
            return row;
        }

        /// <summary>
        /// 填充点检信息值（从E列开始对应Value1到Value31）
        /// </summary>
        private void FillCheckValues(IRow row, DeviceCheckItemSubResult subItem)
        {
            // 从E列（索引4）开始，对应Value1到Value31
            SetCellValue(row, 4, subItem.Value1);   // E列
            SetCellValue(row, 5, subItem.Value2);   // F列
            SetCellValue(row, 6, subItem.Value3);   // G列
            SetCellValue(row, 7, subItem.Value4);   // H列
            SetCellValue(row, 8, subItem.Value5);   // I列
            SetCellValue(row, 9, subItem.Value6);   // J列
            SetCellValue(row, 10, subItem.Value7);  // K列
            SetCellValue(row, 11, subItem.Value8);  // L列
            SetCellValue(row, 12, subItem.Value9);  // M列
            SetCellValue(row, 13, subItem.Value10); // N列
            SetCellValue(row, 14, subItem.Value11); // O列
            SetCellValue(row, 15, subItem.Value12); // P列
            SetCellValue(row, 16, subItem.Value13); // Q列
            SetCellValue(row, 17, subItem.Value14); // R列
            SetCellValue(row, 18, subItem.Value15); // S列
            SetCellValue(row, 19, subItem.Value16); // T列
            SetCellValue(row, 20, subItem.Value17); // U列
            SetCellValue(row, 21, subItem.Value18); // V列
            SetCellValue(row, 22, subItem.Value19); // W列
            SetCellValue(row, 23, subItem.Value20); // X列
            SetCellValue(row, 24, subItem.Value21); // Y列
            SetCellValue(row, 25, subItem.Value22); // Z列
            SetCellValue(row, 26, subItem.Value23); // AA列
            SetCellValue(row, 27, subItem.Value24); // AB列
            SetCellValue(row, 28, subItem.Value25); // AC列
            SetCellValue(row, 29, subItem.Value26); // AD列
            SetCellValue(row, 30, subItem.Value27); // AE列
            SetCellValue(row, 31, subItem.Value28); // AF列
            SetCellValue(row, 32, subItem.Value29); // AG列
            SetCellValue(row, 33, subItem.Value30); // AH列
            SetCellValue(row, 34, subItem.Value31); // AI列
        }

        #endregion

        #endregion

        #region 私有函数

        /// <summary>
        /// 设置单元格值（保留样式）
        /// </summary>
        private void SetCellValue(IRow row, int cellIndex, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            var cell = row.GetCell(cellIndex);
            if (cell == null)
            {
                cell = row.CreateCell(cellIndex);
                // 复制相邻单元格的样式
                var styleCell = row.GetCell(cellIndex - 1);
                if (styleCell != null)
                {
                    cell.CellStyle = styleCell.CellStyle;
                }
            }
            cell.SetCellValue(value);
        }

        /// <summary>
        /// 使用NPOI读取Excel文件的所有Sheet
        /// </summary>
        private List<DeviceItemImportDto> ReadExcelWithNPOI(string filePath)
        {
            var result = new List<DeviceItemImportDto>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook;

                // 根据文件扩展名创建对应的Workbook
                if (Path.GetExtension(filePath).ToLower() == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else
                {
                    workbook = new HSSFWorkbook(fs);
                }

                // 遍历所有Sheet
                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    ISheet sheet = workbook.GetSheetAt(sheetIndex);
                    string sheetName = sheet.SheetName;

                    // 根据sheet名称确定点检类型
                    string type = GetCheckTypeBySheetName(sheetName);

                    // 【更新位置1】获取设备编号和表单编号
                    string deviceNo = GetCellValue(sheet.GetRow(0)?.GetCell(0)); // A1: 设备编号
                    string formNo = GetCellValue(sheet.GetRow(1)?.GetCell(0));   // A2: 表单编号

                    // 解析Sheet数据
                    if (type == "0") // 日点检
                    {
                        ParseDailySheet(sheet, sheetName, type, result, formNo, deviceNo);
                    }
                    else if (type == "1") // 月点检
                    {
                        ParseMonthlySheet(sheet, sheetName, type, result, formNo, deviceNo);
                    }
                    else if (type == "2") // 年点检
                    {
                        ParseYearlySheet(sheet, sheetName, type, result, formNo, deviceNo);
                    }

                }

                workbook.Close();
            }


            return result;
        }

        /// <summary>
        /// 解析日点检Sheet
        /// </summary>
        private void ParseDailySheet(ISheet sheet, string sheetName, string type, List<DeviceItemImportDto> result, string formNo, string deviceNo)
        {
            // 从第4行开始（跳过表头）
            for (int rowIndex = 3; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                // 日点检只有两列
                var mainItem = GetCellValue(row.GetCell(0)); // A列
                var checkItem = GetCellValue(row.GetCell(1)); // B列

                if (string.IsNullOrEmpty(mainItem) && string.IsNullOrEmpty(checkItem))
                    continue;

                result.Add(new DeviceItemImportDto
                {
                    SheetName = sheetName,
                    FormNo = formNo,
                    DeviceNo = deviceNo,
                    Type = type,
                    MainItem = mainItem,
                    CheckItem = checkItem,
                    CheckPlace = "",
                    ItemNo = "",
                    CheckMethod = "",
                    JudgmentStandard = ""
                });
            }
        }

        /// <summary>
        /// 解析月点检Sheet
        /// </summary>
        private void ParseMonthlySheet(ISheet sheet, string sheetName, string type, List<DeviceItemImportDto> result, string formNo, string deviceNo)
        {
            // 从第4行开始（跳过表头）
            for (int rowIndex = 3; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                // 月点检有6列
                var mainItem = GetCellValue(row.GetCell(0)); // A列：分類
                var checkPlace = GetCellValue(row.GetCell(1)); // B列：点检厂所
                var itemNo = GetCellValue(row.GetCell(2)); // C列：NO
                var checkItem = GetCellValue(row.GetCell(3)); // D列：点検項目
                var checkMethod = GetCellValue(row.GetCell(4)); // E列：点検方法
                var judgmentStandard = GetCellValue(row.GetCell(5)); // F列：判定基準

                if (string.IsNullOrEmpty(mainItem) && string.IsNullOrEmpty(checkItem))
                    continue;

                result.Add(new DeviceItemImportDto
                {
                    SheetName = sheetName,
                    FormNo = formNo,
                    DeviceNo = deviceNo,
                    Type = type,
                    MainItem = mainItem,
                    CheckPlace = checkPlace,
                    ItemNo = itemNo,
                    CheckItem = checkItem,
                    CheckMethod = checkMethod,
                    JudgmentStandard = judgmentStandard
                });
            }
        }

        /// <summary>
        /// 解析年点检Sheet
        /// </summary>
        private void ParseYearlySheet(ISheet sheet, string sheetName, string type, List<DeviceItemImportDto> result, string formNo, string deviceNo)
        {
            // 从第4行开始（跳过表头）
            for (int rowIndex = 3; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                // 年点检有6列
                var mainItem = GetCellValue(row.GetCell(0)); // A列：分類
                var checkPlace = GetCellValue(row.GetCell(1)); // B列：点検箇所
                var itemNo = GetCellValue(row.GetCell(2)); // C列：NO
                var checkItem = GetCellValue(row.GetCell(3)); // D列：点検項目
                var checkMethod = GetCellValue(row.GetCell(4)); // E列：点検方法
                var judgmentStandard = GetCellValue(row.GetCell(5)); // F列：判定基準

                if (string.IsNullOrEmpty(mainItem) && string.IsNullOrEmpty(checkItem))
                    continue;

                result.Add(new DeviceItemImportDto
                {
                    SheetName = sheetName,
                    FormNo = formNo,
                    DeviceNo = deviceNo,
                    Type = type,
                    MainItem = mainItem,
                    CheckPlace = checkPlace,
                    ItemNo = itemNo,
                    CheckItem = checkItem,
                    CheckMethod = checkMethod,
                    JudgmentStandard = judgmentStandard
                });
            }
        }


        /// <summary>
        /// 获取单元格的值
        /// </summary>
        private string GetCellValue(ICell cell)
        {
            if (cell == null) return string.Empty;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue?.Trim() ?? string.Empty;
                case CellType.Numeric:
                    return NPOI.SS.UserModel.DateUtil.IsCellDateFormatted(cell) ?
                        cell.StringCellValue.ToString() :
                        cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    try
                    {
                        return cell.StringCellValue?.Trim() ?? string.Empty;
                    }
                    catch
                    {
                        return cell.NumericCellValue.ToString();
                    }
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 根据sheet名称获取点检类型
        /// </summary>
        private string GetCheckTypeBySheetName(string sheetName)
        {
            if (sheetName.Contains("日次") || sheetName.Contains("日点检"))
                return "0";
            else if (sheetName.Contains("月次") || sheetName.Contains("月点检"))
                return "1";
            else if (sheetName.Contains("半年次") || sheetName.Contains("年点检") || sheetName.Contains("年"))
                return "2";
            else
                return "0"; // 默认日点检
        }

        /// <summary>
        /// 处理并保存数据（保持不变）
        /// </summary>
        private async Task ProcessAndSaveData(List<DeviceItemImportDto> importData, DeviceInfoDto deviceInfo, string formNo)
        {
            // 按Sheet名称分组处理
            var sheetGroups = importData.GroupBy(x => x.SheetName);

            foreach (var sheetGroup in sheetGroups)
            {
                var sheetData = sheetGroup.ToList();
                var firstItem = sheetData.First();
                string name = deviceInfo.DeviceTypeName + "_" + sheetGroup.Key;
                // 创建主表记录
                var mainEntity = new DeviceItemMainEntity
                {
                    ItemName = name, // Sheet名称作为项目名称
                    Type = firstItem.Type,
                    Remark = null,
                    DeviceId = deviceInfo.DeviceId, // 【新增】设备ID
                    DeviceTypeId = deviceInfo.DeviceTypeId, // 【新增】设备类型ID
                    Belongs = formNo // 【新增】表单编号
                };
                var isExist = await _deviceItemMainRepository.GetSingleAsync(m => m.ItemName == name && m.DeleteMark == null);
                if (isExist != null) continue;

                int result = await _deviceItemMainRepository.AsSugarClient().Insertable(mainEntity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();

                // 创建主表和设备关联表
                var deAllotEntity = new DeviceItemAllotEntity
                {
                    MainId = mainEntity.Id,
                    DeviceId = deviceInfo.DeviceId,
                };
                await _deviceItemAllotRepository.AsSugarClient().Insertable(deAllotEntity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();

                // 处理子表数据 - 按主项目分组（父级）
                var mainItemGroups = sheetData.Where(x => !string.IsNullOrEmpty(x.MainItem))
                                             .GroupBy(x => x.MainItem);

                foreach (var mainItemGroup in mainItemGroups)
                {
                    var mainItemData = mainItemGroup.ToList();
                    var mainItemName = mainItemGroup.Key;

                    // 创建父级子项（主项目）
                    var parentSubEntity = new DeviceItemSubEntity
                    {
                        MainId = mainEntity.Id,
                        ParentId = "0",
                        CheckItem = mainItemName,
                        CheckPlace = "",
                        ItemNo = "",
                        CheckMethod = "",
                        JudgmentStandard = "",
                        Note = ""
                    };
                    await _deviceItemSubRepository.AsSugarClient().Insertable(parentSubEntity)
                    .CallEntityMethod(m => m.Creator())
                    .ExecuteCommandAsync();

                    // 创建该父级下的所有子级项
                    foreach (var item in mainItemData)
                    {
                        if (string.IsNullOrEmpty(item.CheckItem) || item.CheckItem == mainItemName)
                            continue;

                        var subEntity = new DeviceItemSubEntity
                        {
                            MainId = mainEntity.Id,
                            ParentId = parentSubEntity.Id,
                            CheckItem = item.CheckItem,
                            CheckPlace = item.CheckPlace,
                            ItemNo = item.ItemNo,
                            CheckMethod = item.CheckMethod,
                            JudgmentStandard = item.JudgmentStandard,
                            Note = ""
                        };
                        await _deviceItemSubRepository.AsSugarClient().Insertable(subEntity)
                       .CallEntityMethod(m => m.Creator())
                       .ExecuteCommandAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 上传文件校验
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task<string> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw Oops.Oh(ErrorCode.D1800);
            }

            var webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot", "Upload");
            if (!Directory.Exists(webRootPath)) Directory.CreateDirectory(webRootPath);

            var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
            var allowedExtensions = new[] { ".xls", ".xlsx", ".csv" };
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                throw Oops.Oh(ErrorCode.D1800);
            }

            // 生成唯一文件名，避免重名
            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{fileExtension}";
            var filePath = Path.Combine(webRootPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                // 使用 await 确保文件完全复制
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        /// <summary>
        /// 动态设置实体类的值字段
        /// </summary>
        private void SetValueField(DeviceCheckEntity entity, string propDateName, string value)
        {
            if (string.IsNullOrEmpty(propDateName)) return;

            switch (propDateName.ToLower())
            {
                case "value1": entity.Value1 = value; break;
                case "value2": entity.Value2 = value; break;
                case "value3": entity.Value3 = value; break;
                case "value4": entity.Value4 = value; break;
                case "value5": entity.Value5 = value; break;
                case "value6": entity.Value6 = value; break;
                case "value7": entity.Value7 = value; break;
                case "value8": entity.Value8 = value; break;
                case "value9": entity.Value9 = value; break;
                case "value10": entity.Value10 = value; break;
                case "value11": entity.Value11 = value; break;
                case "value12": entity.Value12 = value; break;
                case "value13": entity.Value13 = value; break;
                case "value14": entity.Value14 = value; break;
                case "value15": entity.Value15 = value; break;
                case "value16": entity.Value16 = value; break;
                case "value17": entity.Value17 = value; break;
                case "value18": entity.Value18 = value; break;
                case "value19": entity.Value19 = value; break;
                case "value20": entity.Value20 = value; break;
                case "value21": entity.Value21 = value; break;
                case "value22": entity.Value22 = value; break;
                case "value23": entity.Value23 = value; break;
                case "value24": entity.Value24 = value; break;
                case "value25": entity.Value25 = value; break;
                case "value26": entity.Value26 = value; break;
                case "value27": entity.Value27 = value; break;
                case "value28": entity.Value28 = value; break;
                case "value29": entity.Value29 = value; break;
                case "value30": entity.Value30 = value; break;
                case "value31": entity.Value31 = value; break;
                default: break;
            }
        }

        /// <summary>
        /// 动态设置更新语句的值字段
        /// </summary>
        private IUpdateable<DeviceCheckEntity> SetValueFieldUpdate(IUpdateable<DeviceCheckEntity> update, string propDateName, string value)
        {
            if (string.IsNullOrEmpty(propDateName)) return update;

            switch (propDateName.ToLower())
            {
                case "value1": return update.SetColumns(m => m.Value1 == value);
                case "value2": return update.SetColumns(m => m.Value2 == value);
                case "value3": return update.SetColumns(m => m.Value3 == value);
                case "value4": return update.SetColumns(m => m.Value4 == value);
                case "value5": return update.SetColumns(m => m.Value5 == value);
                case "value6": return update.SetColumns(m => m.Value6 == value);
                case "value7": return update.SetColumns(m => m.Value7 == value);
                case "value8": return update.SetColumns(m => m.Value8 == value);
                case "value9": return update.SetColumns(m => m.Value9 == value);
                case "value10": return update.SetColumns(m => m.Value10 == value);
                case "value11": return update.SetColumns(m => m.Value11 == value);
                case "value12": return update.SetColumns(m => m.Value12 == value);
                case "value13": return update.SetColumns(m => m.Value13 == value);
                case "value14": return update.SetColumns(m => m.Value14 == value);
                case "value15": return update.SetColumns(m => m.Value15 == value);
                case "value16": return update.SetColumns(m => m.Value16 == value);
                case "value17": return update.SetColumns(m => m.Value17 == value);
                case "value18": return update.SetColumns(m => m.Value18 == value);
                case "value19": return update.SetColumns(m => m.Value19 == value);
                case "value20": return update.SetColumns(m => m.Value20 == value);
                case "value21": return update.SetColumns(m => m.Value21 == value);
                case "value22": return update.SetColumns(m => m.Value22 == value);
                case "value23": return update.SetColumns(m => m.Value23 == value);
                case "value24": return update.SetColumns(m => m.Value24 == value);
                case "value25": return update.SetColumns(m => m.Value25 == value);
                case "value26": return update.SetColumns(m => m.Value26 == value);
                case "value27": return update.SetColumns(m => m.Value27 == value);
                case "value28": return update.SetColumns(m => m.Value28 == value);
                case "value29": return update.SetColumns(m => m.Value29 == value);
                case "value30": return update.SetColumns(m => m.Value30 == value);
                case "value31": return update.SetColumns(m => m.Value31 == value);
                default: return update;
            }
        }

        #endregion
    }

}
