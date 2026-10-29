using Aop.Api.Domain;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Security;
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
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using SqlSugar;
using System.Reactive;
using System.Text.RegularExpressions;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 设备台账
    /// </summary>
    [ApiDescriptionSettings(Tag = "Device", Name = "", Order = 200)]
    [Route("api/[controller]")]
    public class DeviceService : IDeviceService, IDynamicApiController, ITransient
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
        public DeviceService(
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
            _maintenanceRecordSpareRepository = maintenanceRecordSpareRepository;
            _deviceRepairRecordRepository = deviceRepairRecordRepository;
            _cacheManager = cacheManager;
            _userManager = userManager;
            _fileManager = fileManager;
        }

        #region 设备类型

        /// <summary>
        /// 获取类型下拉框
        /// </summary>
        /// <returns></returns>
        [HttpGet("type/GetDeviceTypeDicts")]
        public async Task<dynamic> GetDeviceTypeDicts()
        {
            // 获取所有未删除的设备类型数据
            var allTypes = await _deviceTypeMasterRepository.AsQueryable()
                .Where(it => it.DeleteMark == null && it.EnabledMark == 1)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToListAsync();

            // 构建树形结构
            var tree = BuildDeviceTypeTree(allTypes, "0"); // "0"表示从最顶层开始构建
            return tree;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("type/GetDeviceTypes")]
        public async Task<dynamic> GetDeviceTypes([FromBody] DeviceTypeQuery input)
        {
            // 构建查询条件
            var whereLambda = LinqExpression.And<DeviceTypeMasterEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            // 关键词查询
            if (input.keyword.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m =>
                    m.DevicetypeName.Contains(input.keyword));
            }
            // 状态筛选
            if (input.status != -1)
            {
                whereLambda = whereLambda.And(m => m.EnabledMark == input.status.Value);
            }

            // 获取所有符合条件的设备类型数据
            var allTypes = await _deviceTypeMasterRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToListAsync();
            // 构建完整树形结构
            var fullTree = BuildDeviceTypeTree(allTypes, "0");

            // 对树形结构进行分页处理
            var pagedTree = PaginateTree(fullTree, input.currentPage, input.pageSize);

            // 返回分页结果
            return new {
                total = fullTree.Count, // 总节点数（注意：不是记录总数）
                items = pagedTree
            };
        }

        /// <summary>
        /// 新增父类型
        /// </summary>
        /// <returns></returns>
        [HttpPost("type/InsertParentType")]
        public async Task InsertParentType([FromBody] DeviceParentTypeInput input)
        {
            // 验证类型名称是否已存在
            if (await _deviceTypeMasterRepository.IsAnyAsync(p => p.DevicetypeName == input.DevicetypeName && p.DeleteMark == null))
                throw Oops.Oh(ErrorCode.D6005);

            // 获取当前最大编号
            var maxCode = await _deviceTypeMasterRepository.AsSugarClient()
           .Queryable<DeviceTypeMasterEntity>()
           .Where(p => p.DevicetypeCode.StartsWith("M_TYPE_") &&
                       p.DeleteMark == null &&
                       p.ParentId == "0")
           .Select(p => new {
               Code = p.DevicetypeCode,
               NumericPart = int.Parse(p.DevicetypeCode.Replace("M_TYPE_", ""))
           })
           .OrderByDescending(x => x.NumericPart)
           .FirstAsync();
            // 生成新编号
            int newNumber = 1;
            if (maxCode != null && !string.IsNullOrEmpty(maxCode.NumericPart.ToString()))
            {
                var lastNumberStr = maxCode.Code.Split('_').Last();
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    newNumber = lastNumber + 1;
                }
            }

            string newCode = $"M_TYPE_{newNumber}";

            // 创建实体
            DeviceTypeMasterEntity entity = input.Adapt<DeviceTypeMasterEntity>();
            entity.DevicetypeCode = newCode;
            entity.ParentId = "0"; // 主类型默认第一级，ParentId设为0
            entity.EnabledMark = input.F_ENABLED_MARK;

            // 插入数据库
            int isOk = await _deviceTypeMasterRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
        }

        /// <summary>
        /// 新增子类型
        /// </summary>
        /// <returns></returns>
        [HttpPost("type/InsertSonType")]
        public async Task InsertSonType([FromBody] DeviceSonTypeInput input)
        {
            if (input.CruType == 0) // 新增
            {
                await AddSonType(input);
            }
            else if (input.CruType == 1) // 编辑
            {
                await UpdateSonType(input);
            }
            else
            {
                throw Oops.Oh(ErrorCode.D1006); // 无效操作类型
            }
        }

        /// <summary>
        ///  删除设备类型
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("DeleteDeviceType")]
        public async Task DeleteDeviceType(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var list = await _deviceMasterRepository.GetListAsync(p => p.DeviceTypeId == id && p.DeleteMark == null);
            if (list.Any())
                throw Oops.Oh(ErrorCode.COM1034);

            var typeLists = await _deviceTypeMasterRepository.GetListAsync(m => m.ParentId == id && m.DeleteMark == null);
            if (typeLists.Any())
                throw Oops.Oh(ErrorCode.COM1033);

            var entity = await _deviceTypeMasterRepository.GetSingleAsync(m => m.Id == id && m.EnabledMark == 0);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1038);
            }

            int isOk = await _deviceTypeMasterRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
                throw Oops.Oh(ErrorCode.COM1002);
        }

        #endregion

        #region 设备

        /// <summary>
        /// 指定设备
        /// </summary>
        /// <returns></returns>
        [HttpGet("PointDevice")]
        public async Task PointDevice(string Id, int point)
        {
            // 直接获取实体
            var entity = await _deviceMasterRepository.GetSingleAsync(p => p.Id == Id && p.DeleteMark == null);

            // 判断实体是否存在
            if (entity == null)
                throw Oops.Oh(ErrorCode.D6005);

            // 这里 entity 肯定不为 null，可以继续使用
            if (point == 1)
            {
                entity.PointDevice = 0;  //0 否  1是
            }
            else
            {
                entity.PointDevice = 1;
            }

            // 插入数据库
            int isOk = await _deviceMasterRepository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
        }

        /// <summary>
        /// 获取多层级下拉
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpGet("SelectedMutiList")]
        public async Task<dynamic> SelectedMutiList(string propName)
        {
            if (propName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }
            var type = await _dictionaryTypeRepository.GetSingleAsync(m => m.FullName == propName);
            if (type == null)
            {
                throw Oops.Oh(ErrorCode.D1001);
            }

            // 构建树形结构
            var tree = await BuildTypeDataTree(type.Id);

            return tree;
        }

        /// <summary>
        /// 构建类型和数据的树形结构（不包含最高节点）
        /// </summary>
        private async Task<List<object>> BuildTypeDataTree(string typeId)
        {
            var currentType = await _dictionaryTypeRepository.GetSingleAsync(m => m.Id == typeId && m.DeleteMark == null);
            if (currentType == null) return new List<object>();

            var result = new List<object>();

            // 获取当前类型下的字典数据
            var dataList = await _dictionaryDataRepository.GetListAsync(m => m.DictionaryTypeId == typeId && m.DeleteMark == null);
            var dataNodes = dataList.Select(data => new {
                id = data.Id,
                name = data.FullName,
                isType = false,
                children = new List<object>()
            }).ToList();

            result.AddRange(dataNodes);

            // 获取子类型并递归构建
            var childTypes = await _dictionaryTypeRepository.GetListAsync(m => m.ParentId == typeId && m.DeleteMark == null);

            foreach (var childType in childTypes)
            {
                var childNode = new {
                    id = childType.Id,
                    name = childType.FullName,
                    isType = true,
                    children = await BuildTypeDataTree(childType.Id) // 递归获取子节点
                };
                result.Add(childNode);
            }

            return result;
        }

        /// <summary>
        /// 报修
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("Repairs")]
        public async Task Repairs([FromBody] DeviceRepariInput input)
        {
            if (input.DeviceId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }
            //添加报修
            var currentDate = DateTime.Today;
            var drList = await _deviceRepairRepository.AsQueryable().Where(m => m.DeleteMark == null && m.CreatorTime >= currentDate).Select(z => z.RepairNo).ToListAsync();
            var maxNumber = drList
            .Select(repairNo =>
            {
                // 按下滑线分割字符串
                var parts = repairNo.Split('_');
                if (parts.Length > 0)
                {
                    // 取最后一部分
                    var lastPart = parts[^1]; // 等同于 parts[parts.Length - 1]
                    if (int.TryParse(lastPart, out int number))
                    {
                        return number;
                    }
                }
                return 0;
            })
            .DefaultIfEmpty(0)
            .Max();

            var nextNumber = maxNumber + 1;
            var nextNumberFormatted = nextNumber.ToString("D3");
            var timeStr = DateTime.Now.ToString("yyyyMMddHHmmss");
            DeviceRepairEntity entity = input.Adapt<DeviceRepairEntity>();
            entity.RepairNo = "BX_" + timeStr + "_" + nextNumberFormatted;
            entity.EnabledMark = 0;
            await _deviceRepairRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            //添加维修记录
            DeviceRepairRecordEntity recordEntity = new DeviceRepairRecordEntity()
            {
                RepairId = entity.Id,
                RepairRecordNo = "WX_" + timeStr + "_" + nextNumberFormatted
            };
            await _deviceRepairRecordRepository.AsSugarClient().Insertable(recordEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            //添加文件
            if (input.fileContent.Any())
            {

                var mianDocId = string.Empty;
                var mainDoc = await _documentRepository.GetSingleAsync(p => p.FullName == "报修管理文件" && p.DeleteMark == null);
                if (mainDoc == null)
                {
                    //创建主分支
                    var mianDocEntity = new DocumentEntity() { FullName = "报修管理文件", Type = 0, ParentId = "0", EnabledMark = 1, TenantId = "0" };
                    await _documentRepository.AsSugarClient().Insertable(mianDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    mianDocId = mianDocEntity.Id;
                }
                else
                {
                    mianDocId = mainDoc.Id;
                }
                var deviceDocId = string.Empty;
                var deviceDoc = await _documentRepository.GetSingleAsync(p => p.Description == input.DeviceId && p.ParentId == mianDocId && p.DeleteMark == null);
                if (deviceDoc == null)
                {
                    //创建第二层级
                    var deviceMaster = await _deviceMasterRepository.GetSingleAsync(p => p.Id == input.DeviceId && p.DeleteMark == null);
                    var devDocEntity = new DocumentEntity() { FullName = deviceMaster.DeviceNo, Type = 0, ParentId = mianDocId, EnabledMark = 1, TenantId = "0", Description = input.DeviceId };
                    await _documentRepository.AsSugarClient().Insertable(devDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    deviceDocId = devDocEntity.Id;
                }
                else
                {
                    deviceDocId = deviceDoc.Id;
                }
                //直接新增一个新的报修号文件
                var repairDocEntity = new DocumentEntity() { FullName = entity.RepairNo, Type = 0, ParentId = deviceDocId, EnabledMark = 1, TenantId = "0", Description = entity.Id };
                await _documentRepository.AsSugarClient().Insertable(repairDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
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
                    }
                    var DocEntity = new DocumentEntity() { FullName = item.FileName, Type = 1, ParentId = repairDocEntity.Id, EnabledMark = 1, FileExtension = item.FileExtension, FileSize = item.FileSize.ToString(), TenantId = "0", UploadUrl = newUrl, FilePath = item.FileName };
                    await _documentRepository.AsSugarClient().Insertable(DocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
            }
        }

        /// <summary>
        ///  删除设备
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("DeleteDevice")]
        public async Task DeleteDevice(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            DeviceMasterEntity? entity = await _deviceMasterRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            int isOk = await _deviceMasterRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
                throw Oops.Oh(ErrorCode.COM1002);
        }

        /// <summary>
        /// 获取下拉框
        /// </summary>
        /// <returns></returns>
        [HttpGet("SelectedList")]
        public async Task<dynamic> SelectedList(string propName)
        {
            if (propName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            if (propName == "使用部门")
            {
                var list = await _organizeRepository.GetListAsync(m => m.DeleteMark == null && m.Category == "department");
                return list.Select(m => new {
                    id = m.Id,
                    name = m.FullName,
                }).ToList();
            }
            else
            {
                var type = await _dictionaryTypeRepository.GetSingleAsync(m => m.FullName == propName && m.DeleteMark == null);
                var list = await _dictionaryDataRepository.GetListAsync(m => m.DictionaryTypeId == type.Id && m.DeleteMark == null);
                return list.Select(m => new {
                    id = m.Id,
                    name = m.FullName,
                }).ToList();
            }
        }

        /// <summary>
        /// 新增/编辑设备
        /// </summary>
        /// <returns></returns>
        [HttpPost("CruDevice")]
        public async Task CruDevice([FromBody] DeviceCruInput input)
        {
            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                //编辑
                var existing = await _deviceMasterRepository.GetSingleAsync(m => m.Id == input.Id);
                existing = input.Adapt(existing);
                isOk = await _deviceTypeMasterRepository.AsSugarClient().Updateable(existing).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                // 创建实体
                DeviceMasterEntity entity = input.Adapt<DeviceMasterEntity>();
                // 插入数据库
                isOk = await _deviceMasterRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
        }

        /// <summary>
        /// 反填设备接口
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("GetDeviceById")]
        public async Task<dynamic> GetDeviceById(string id)
        {
            var data = await _deviceMasterRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            //var dataTypes = await _deviceTypeMasterRepository.GetSingleAsync(m => m.Id == data.DeviceTypeId && m.DeleteMark == null);
            //data.DeviceTypeId = dataTypes.DevicetypeName;
            return data.Adapt<DeviceCruInput>();

            // 获取设备类型及其所有父级
            //List<string> typeHierarchy = new List<string>();
            //string currentTypeId = data.DeviceTypeId;

            //// 递归查找所有父级（从当前级向上查找）
            //while (!string.IsNullOrEmpty(currentTypeId))
            //{
            //    var currentType = await _deviceTypeMasterRepository.GetFirstAsync(x => x.Id == currentTypeId && x.DeleteMark == null);
            //    if (currentType == null) break;
            //    typeHierarchy.Insert(0, currentTypeId); // 插入到列表开头，确保父级在前
            //    currentTypeId = currentType.ParentId;
            //}

            //// 如果找到了类型层级，则重新赋值DeviceTypeId为层级数组
            //if (typeHierarchy.Count > 0)
            //{
            //    data.DeviceTypeId = string.Join(",", typeHierarchy); // 或者直接赋值数组，取决于你的需求
            //    // 或者如果你想保持DeviceTypeId为数组类型（需要修改DTO）：
            //    // data.DeviceTypeIds = typeHierarchy.ToArray();
            //}
        }

        /// <summary>
        /// 获取设备列表
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("GetDeviceList")]
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

            var list = await _deviceMasterRepository.AsQueryable()
             .Where(whereLambda)
             .OrderBy(it => it.CreatorTime, OrderByType.Desc)
             .LeftJoin<DeviceTypeMasterEntity>((d, dt) => d.DeviceTypeId == dt.Id)
             .LeftJoin<DictionaryDataEntity>((d, dt, addr) => d.UseAddress == addr.Id)
             .LeftJoin<DictionaryDataEntity>((d, dt, addr, imp) => d.DeviceImportant == imp.Id)
             .LeftJoin<DictionaryDataEntity>((d, dt, addr, imp, stat) => d.DeviceStatus == stat.Id)
             .LeftJoin<OrganizeEntity>((d, dt, addr, imp, stat, dept) => d.UseDpt == dept.Id)
             .Select((d, dt, addr, imp, stat, dept) => new DeviceCruListDto
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
                 UseDptId = d.UseDpt,
                 UseAddressId = d.UseAddress,
                 PointDevice = d.PointDevice,
             })
             .ToPagedListAsync(input.currentPage, input.pageSize);
            return list;

        }

        /// <summary>
        /// 设备台账导出
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("DeviceExport")]
        public async Task<dynamic> DeviceExport([FromBody] DeviceInput input)
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

            var list = await _deviceMasterRepository.AsQueryable()
             .Where(whereLambda)
             .OrderBy(it => it.CreatorTime, OrderByType.Desc)
             .LeftJoin<DeviceTypeMasterEntity>((d, dt) => d.DeviceTypeId == dt.Id)
             .LeftJoin<DictionaryDataEntity>((d, dt, addr) => d.UseAddress == addr.Id)
             .LeftJoin<DictionaryDataEntity>((d, dt, addr, imp) => d.DeviceImportant == imp.Id)
             .LeftJoin<DictionaryDataEntity>((d, dt, addr, imp, stat) => d.DeviceStatus == stat.Id)
             .LeftJoin<OrganizeEntity>((d, dt, addr, imp, stat, dept) => d.UseDpt == dept.Id)
             .Select((d, dt, addr, imp, stat, dept) => new DeviceCruListDto
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
                 UseDptId = d.UseDpt,
                 UseAddressId = d.UseAddress,
             })
             .ToListAsync();

            // 准备导出文件路径
            var webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
            if (!Directory.Exists(webRootPath)) Directory.CreateDirectory(webRootPath);

            string exportFileName = $"设备台账_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            string templatePath = Path.Combine(webRootPath, "SoftTemplate", "设备台账.xlsx");
            string exportFilePath = Path.Combine(webRootPath, "ExportFile", exportFileName);

            // 确保导出目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(exportFilePath));
            File.Copy(templatePath, exportFilePath, overwrite: true);

            // 3. 读取模板文件到内存
            byte[] templateBytes;
            using (var templateStream = new FileStream(exportFilePath, FileMode.Open, FileAccess.Read))
            {
                templateBytes = new byte[templateStream.Length];
                templateStream.Read(templateBytes, 0, (int)templateStream.Length);
            }

            var memoryStream = new MemoryStream(templateBytes);
            var workbook = new XSSFWorkbook(memoryStream);
            var cellStyle = CreateCellStyle(workbook);

            // 处理点检记录表
            var checkSheet = workbook.GetSheet("设备台账列表");
            int checkStartRow = 1;
            foreach (var item in list)
            {
                checkSheet.DefaultRowHeightInPoints = 15;
                var row = checkSheet.CreateRow(checkStartRow++);
                int col = 0;
                // 填充数据
                CreateCell(row, col++, item.DeviceNo, cellStyle);
                CreateCell(row, col++, item.AssetsNumber, cellStyle);
                CreateCell(row, col++, item.DeviceName, cellStyle);
                CreateCell(row, col++, item.DeviceManufacturer, cellStyle);
                CreateCell(row, col++, item.DeviceModel, cellStyle);
                CreateCell(row, col++, item.DeviceSupplier, cellStyle);
                CreateCell(row, col++, item.DeviceTypeId, cellStyle);
                CreateCell(row, col++, item.BuyDate, cellStyle);
                CreateCell(row, col++, item.DeviceStatus, cellStyle);
                CreateCell(row, col++, item.UseAddress, cellStyle);
                CreateCell(row, col++, item.UseDpt, cellStyle);
            }

            using (var fileStream = new FileStream(exportFilePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fileStream);
            }

            workbook?.Close();
            memoryStream?.Dispose();

            return Path.Combine("ExportFile", exportFileName);

        }

        #endregion

        #region 报修联络

        /// <summary>
        /// 获取报修联络列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("repair/GetRepairs")]
        public async Task<dynamic> GetRepairs([FromBody] DeviceRepairQuery input)
        {
            //设备表  用户表   repair表  type表 
            var whereLambda = LinqExpression.And<DeviceRepairEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);
            if (input.EnabledMark != -1)
            {
                whereLambda = whereLambda.And(m => m.EnabledMark == input.EnabledMark);
            }
            if (!string.IsNullOrEmpty(input.ApplicationStartTime))
            {
                whereLambda = whereLambda.And(m => m.ApplicationTime.CompareTo(input.ApplicationStartTime) >= 0);
            }

            if (!string.IsNullOrEmpty(input.ApplicationEndTime))
            {
                whereLambda = whereLambda.And(m => m.ApplicationTime.CompareTo(input.ApplicationEndTime) <= 0);
            }

            if (!string.IsNullOrEmpty(input.FaultOccurStartTime))
            {
                whereLambda = whereLambda.And(m => m.FaultOccurTime.CompareTo(input.FaultOccurStartTime) >= 0);
            }

            if (!string.IsNullOrEmpty(input.FaultOccurEndTime))
            {
                whereLambda = whereLambda.And(m => m.FaultOccurTime.CompareTo(input.FaultOccurEndTime) <= 0);
            }

            var query = _deviceRepairRepository.AsQueryable()
            .Where(whereLambda);

            var joinedQuery = query
            .LeftJoin<DeviceMasterEntity>((dr, dm) => dr.DeviceId == dm.Id)
            .LeftJoin<DeviceTypeMasterEntity>((dr, dm, dt) => dm.DeviceTypeId == dt.Id)// 关联类型表
            .LeftJoin<UserEntity>((dr, dm, dt, user) => dr.ApplicantId == user.Id) // 关联用户表
            .LeftJoin<DictionaryDataEntity>((dr, dm, dt, user, d1) => dr.UsageLocationId == d1.Id)
            .LeftJoin<OrganizeEntity>((dr, dm, dt, user, d1, dept) => dm.UseDpt == dept.Id)
            .LeftJoin<DictionaryDataEntity>((dr, dm, dt, user, d1, dept, d2) => dr.FaultUrgencyLevelId == d2.Id);

            // 在关联后添加BomName查询条件
            if (input.DeviceNo.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((dr, dm) => dm.DeviceNo.Contains(input.DeviceNo));
            }
            if (input.DeviceName.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((dr, dm) => dm.DeviceName.Contains(input.DeviceName));
            }

            var list = await joinedQuery
            .Select((dr, dm, dt, user, d1, dept, d2) => new DeviceRepairOutput
            {
                Id = dr.Id,
                DeviceNo = dm.DeviceNo,
                AssetsNumber = dm.AssetsNumber,
                DeviceName = dm.DeviceName,
                DeviceManufacturer = dm.DeviceManufacturer,
                DeviceModel = dm.DeviceModel,
                DeviceSupplier = dm.DeviceSupplier,
                DeviceTypeName = dt.DevicetypeName,
                UseAddress = d1.FullName,
                UseDpt = dept.FullName,
                ApplicationTime = dr.ApplicationTime,
                EnabledMark = dr.EnabledMark,
                IsShutdownFault = dr.IsShutdownFault,
                FaultOccurTime = dr.FaultOccurTime,
                Images = dr.Images,
                FaultUrgencyLevelId = d2.FullName
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);
            return list;
        }

        /// <summary>
        ///  删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("repair/delete")]
        public async Task DeleteRepair(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }
            DeviceRepairRecordEntity? entityDrr = await _deviceRepairRecordRepository.GetSingleAsync(p => p.RepairId == id && p.DeleteMark == null);
            if (entityDrr.CompleteTime != null)
            {
                throw Oops.Oh(ErrorCode.COM1035);
            }
            await _deviceRepairRecordRepository.AsSugarClient().Updateable(entityDrr).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();

            DeviceRepairEntity? entity = await _deviceRepairRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            await _deviceRepairRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        }

        #endregion

        #region 维修记录

        /// <summary>
        /// 获取维修记录列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("record/GetRecords")]
        public async Task<dynamic> GetRecords([FromBody] DeviceRecordsQuery input)
        {
            //设备表  用户表   repair表   repairrecord表   type表   DeviceRecordsOutput
            var whereLambda = LinqExpression.And<DeviceRepairRecordEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);
            if (!string.IsNullOrEmpty(input.RepairStatusId))
            {
                whereLambda = whereLambda.And(m => m.RepairStatusId == input.RepairStatusId);
            }
            if (!string.IsNullOrEmpty(input.CompleteStartTime))
            {
                var startTime = DateTime.Parse(input.CompleteStartTime);
                whereLambda = whereLambda.And(m => m.CompleteTime >= startTime);
            }

            if (!string.IsNullOrEmpty(input.CompleteEndTime))
            {
                var endTime = DateTime.Parse(input.CompleteEndTime);
                whereLambda = whereLambda.And(m => m.CompleteTime <= endTime);
            }

            var query = _deviceRepairRecordRepository.AsQueryable()
            .Where(whereLambda);

            var joinedQuery = query
            .LeftJoin<DeviceRepairEntity>((drr, dr) => dr.Id == drr.RepairId)
            .LeftJoin<DeviceMasterEntity>((drr, dr, dm) => dr.DeviceId == dm.Id)
            .LeftJoin<DeviceTypeMasterEntity>((drr, dr, dm, dt) => dm.DeviceTypeId == dt.Id)// 关联类型表
            .LeftJoin<UserEntity>((drr, dr, dm, dt, user) => dr.ApplicantId == user.Id) // 关联用户表
            .LeftJoin<DictionaryDataEntity>((drr, dr, dm, dt, user, d1) => dr.FaultUrgencyLevelId == d1.Id)
            .LeftJoin<DictionaryDataEntity>((drr, dr, dm, dt, user, d1, d2) => drr.RepairStatusId == d2.Id)
            .LeftJoin<OrganizeEntity>((drr, dr, dm, dt, user, d1, d2, dept) => dm.UseDpt == dept.Id);
            // .LeftJoin<UserEntity>((drr, dr, dm, dt, user, d1, d2, dept, user2) => drr.RepairPersonId == user2.Id); // 关联用户表
            // 在关联后添加BomName查询条件
            if (input.DeviceNo.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((drr, dr, dm) => dm.DeviceNo.Contains(input.DeviceNo));
            }
            if (input.DeviceName.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((drr, dr, dm) => dm.DeviceName.Contains(input.DeviceName));
            }

            var list = await joinedQuery
            .Select((drr, dr, dm, dt, user, d1, d2, dept) => new DeviceRecordsOutput
            {
                Id = drr.Id,
                DeviceNo = dm.DeviceNo,
                DeviceName = dm.DeviceName,
                FaultOccurTime = dr.FaultOccurTime,
                ApplicantName = user.RealName,
                ApplicationTime = dr.ApplicationTime,
                RepairNo = dr.RepairNo,
                RepairRecordNo = drr.RepairRecordNo,
                FaultUrgencyLevelName = d1.FullName,
                IsShutdownFault = dr.IsShutdownFault,
                RepairStatusName = d2.FullName,
                RepairPersonName = drr.RepairPersonId,
                CompleteTime = drr.CompleteTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                RepairCost = drr.RepairCost,
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);
            return list;

        }

        /// <summary>
        /// 获取维修记录
        /// </summary>
        /// <returns></returns>
        [HttpGet("record/GetById")]
        public async Task<dynamic> GetRecordById(string Id)
        {
            var whereLambda = LinqExpression.And<DeviceRepairRecordEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null && x.Id == Id);

            var query = _deviceRepairRecordRepository.AsQueryable()
            .Where(whereLambda);

            var joinedQuery = query
            .LeftJoin<DeviceRepairEntity>((drr, dr) => dr.Id == drr.RepairId)
            .LeftJoin<DeviceMasterEntity>((drr, dr, dm) => dr.DeviceId == dm.Id)
            .LeftJoin<DeviceTypeMasterEntity>((drr, dr, dm, dt) => dm.DeviceTypeId == dt.Id)// 关联类型表
            .LeftJoin<UserEntity>((drr, dr, dm, dt, user) => dr.ApplicantId == user.Id) // 关联用户表
            .LeftJoin<OrganizeEntity>((drr, dr, dm, dt, user, dept) => dm.UseDpt == dept.Id)
            .LeftJoin<UserEntity>((drr, dr, dm, dt, user, dept, user2) => drr.RepairPersonId == user2.Id); // 关联用户表

            //获取备件信息
            var spareList = _maintenanceRecordSpareRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.MaintenanceRecordId == Id)
                .LeftJoin<WarehouseBomEntity>((s, bom) => s.BomId == bom.Id);
            //.LeftJoin<WarehouseLocationEntity>((s, bom, loc) => s.LocationId == loc.Id).OrderBy((s, bom, loc) => s.CreatorTime, OrderByType.Desc);

            var spareInfoList = spareList.Select((s, bom) => new SpareInfo
            {
                Id = s.Id,
                BomId = s.BomId,
                Quantity = s.Quantity ?? "0",
                Prices = s.Prices ?? "0",
                PartsManufacturer = bom.PartsManufacturer,
                //LocationId = s.LocationId,
                //LocationName = "",
                PartsModel = bom.PartsModel,
                PartsName = bom.PartsName,
            }).ToList();

            var result = await joinedQuery
            .Select((drr, dr, dm, dt, user, dept, user2) => new DeviceRecordsByIdOutput
            {
                Id = drr.Id,
                DeviceNo = dm.DeviceNo,
                DeviceName = dm.DeviceName,

                RepairNo = dr.RepairNo,
                FaultOccurTime = dr.FaultOccurTime,
                ApplicationTime = dr.ApplicationTime,
                ApplicationDepartmentId = dr.ApplicationDepartmentId,
                ApplicantId = dr.ApplicantId,
                FaultUrgencyLevelId = dr.FaultUrgencyLevelId,
                IsShutdownFault = dr.IsShutdownFault,
                FaultDescription = dr.FaultDescription,
                DeviceId = dr.DeviceId,

                RepairRecordNo = drr.RepairRecordNo,
                FaultCategoryId = drr.FaultCategoryId,
                FaultLocation = drr.FaultLocation,
                RepairStatusId = drr.RepairStatusId,
                FaultReason = drr.FaultReason,
                RepairPersonId = drr.RepairPersonId,
                AcceptTime = drr.AcceptTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                CompleteTime = drr.CompleteTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                WaitingDuration = drr.WaitingDuration,
                RepairDuration = drr.RepairDuration,
                RepairCost = drr.RepairCost,
                RepairDescription = drr.RepairDescription,
                DowntimeDuration = drr.DowntimeDuration,
                OutsourcingCost = drr.OutsourcingCost,
                GeneralAffairsCategoryId = drr.GeneralAffairsCategoryId,
                IsDowntimeRepair = drr.IsDowntimeRepair,
                GeneralAffairsDowntime = drr.GeneralAffairsDowntime,
                DowntimeDeviceName = drr.DowntimeDeviceName,
                DowntimeDeviceNo = drr.DowntimeDeviceNo,
            })
           .FirstAsync();
            result.SpareDatas = spareInfoList;
            return result;
        }

        /// <summary>
        /// 编辑维修记录
        /// </summary>
        /// <returns></returns>
        [HttpPost("record/Update")]
        public async Task UpdateRecord([FromBody] DeviceRecordsInput input)
        {
            //验证并添加备件
            List<MaintenanceRecordSpareEntity> spareEntity = input.SpareDatas.Adapt<List<MaintenanceRecordSpareEntity>>();
            List<WarehouseReceiveEntity> receiveEntities = new List<WarehouseReceiveEntity>();
            foreach (var item in spareEntity)
            {
                item.MaintenanceRecordId = input.Id;
                await _deviceRepairRecordRepository.AsSugarClient().Insertable(item).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                //添加领用
                WarehouseReceiveEntity warehouseReceiveEntity = new WarehouseReceiveEntity();
                warehouseReceiveEntity.ExternalId = item.Id;
                warehouseReceiveEntity.BomId = item.BomId;
                warehouseReceiveEntity.ReceiveQuantity = item.Quantity;
                warehouseReceiveEntity.Status = 1; // 未确认
                //warehouseReceiveEntity.LocationId = item.LocationId;
                warehouseReceiveEntity.ReceiveType = 2; // 维修
                warehouseReceiveEntity.DeviceId = input.DeviceId;
                await _warehouseReceiveRepository.AsSugarClient().Insertable(warehouseReceiveEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            //修改报修状态
            var existing = await _deviceRepairRecordRepository.GetSingleAsync(m => m.Id == input.Id);
            if (!string.IsNullOrEmpty(input.CompleteTime))
            {
                DeviceRepairEntity repairEntity = await _deviceRepairRepository.GetSingleAsync(m => m.Id == existing.RepairId);
                repairEntity.EnabledMark = 1;
                await _deviceRepairRepository.AsSugarClient().Updateable(repairEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            //修改记录
            existing = input.Adapt(existing);
            int isOk = await _deviceRepairRecordRepository.AsSugarClient().Updateable(existing).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);

            //处理上传文件
            if (input.fileContent.Any())
            {

                var mianDocId = string.Empty;
                var mainDoc = await _documentRepository.GetSingleAsync(p => p.FullName == "维修管理文件" && p.DeleteMark == null);
                if (mainDoc == null)
                {
                    //创建主分支
                    var mianDocEntity = new DocumentEntity() { FullName = "维修管理文件", Type = 0, ParentId = "0", EnabledMark = 1, TenantId = "0" };
                    await _documentRepository.AsSugarClient().Insertable(mianDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    mianDocId = mianDocEntity.Id;
                }
                else
                {
                    mianDocId = mainDoc.Id;
                }
                var deviceDocId = string.Empty;
                var deviceDoc = await _documentRepository.GetSingleAsync(p => p.Description == input.DeviceId && p.ParentId == mianDocId && p.DeleteMark == null);
                if (deviceDoc == null)
                {
                    //创建第二层级
                    var deviceMaster = await _deviceMasterRepository.GetSingleAsync(p => p.Id == input.DeviceId && p.DeleteMark == null);
                    var devDocEntity = new DocumentEntity() { FullName = deviceMaster.DeviceNo, Type = 0, ParentId = mianDocId, EnabledMark = 1, TenantId = "0", Description = input.DeviceId };
                    await _documentRepository.AsSugarClient().Insertable(devDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    deviceDocId = devDocEntity.Id;
                }
                else
                {
                    deviceDocId = deviceDoc.Id;
                }
                //直接新增一个新的报修号文件
                var repairDocEntity = new DocumentEntity() { FullName = existing.RepairRecordNo, Type = 0, ParentId = deviceDocId, EnabledMark = 1, TenantId = "0", Description = existing.Id };
                await _documentRepository.AsSugarClient().Insertable(repairDocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
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
                    }
                    var DocEntity = new DocumentEntity() { FullName = item.FileName, Type = 1, ParentId = repairDocEntity.Id, EnabledMark = 1, FileExtension = item.FileExtension, FileSize = item.FileSize.ToString(), TenantId = "0", UploadUrl = newUrl, FilePath = item.FileName };
                    await _documentRepository.AsSugarClient().Insertable(DocEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
            }

        }

        /// <summary>
        /// 报修联络导出
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("record/DeviceExport")]
        public async Task<dynamic> RepairExport([FromBody] DeviceRecordsQuery input)
        {
            var whereLambda = LinqExpression.And<DeviceRepairRecordEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);
            if (!string.IsNullOrEmpty(input.RepairStatusId))
            {
                whereLambda = whereLambda.And(m => m.RepairStatusId == input.RepairStatusId);
            }
            if (!string.IsNullOrEmpty(input.CompleteStartTime))
            {
                var startTime = DateTime.Parse(input.CompleteStartTime);
                whereLambda = whereLambda.And(m => m.CompleteTime >= startTime);
            }

            if (!string.IsNullOrEmpty(input.CompleteEndTime))
            {
                var endTime = DateTime.Parse(input.CompleteEndTime);
                whereLambda = whereLambda.And(m => m.CompleteTime <= endTime);
            }

            var query = _deviceRepairRecordRepository.AsQueryable()
            .Where(whereLambda);

            var joinedQuery = query
            .LeftJoin<DeviceRepairEntity>((drr, dr) => dr.Id == drr.RepairId)
            .LeftJoin<DeviceMasterEntity>((drr, dr, dm) => dr.DeviceId == dm.Id)
            .LeftJoin<DeviceTypeMasterEntity>((drr, dr, dm, dt) => dm.DeviceTypeId == dt.Id)// 关联类型表
            .LeftJoin<UserEntity>((drr, dr, dm, dt, user) => dr.ApplicantId == user.Id) // 关联用户表
            .LeftJoin<DictionaryTypeEntity>((drr, dr, dm, dt, user, d1) => dr.FaultUrgencyLevelId == d1.Id)
            .LeftJoin<DictionaryTypeEntity>((drr, dr, dm, dt, user, d1, d2) => drr.RepairStatusId == d2.Id)
            .LeftJoin<OrganizeEntity>((drr, dr, dm, dt, user, d1, d2, dept) => dm.UseDpt == dept.Id)
            .LeftJoin<UserEntity>((drr, dr, dm, dt, user, d1, d2, dept, user2) => drr.RepairPersonId == user2.Id); // 关联用户表

            if (input.DeviceNo.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((drr, dr, dm) => dm.DeviceNo.Contains(input.DeviceNo));
            }
            if (input.DeviceName.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((drr, dr, dm) => dm.DeviceName.Contains(input.DeviceName));
            }

            var list = await joinedQuery
            .Select((drr, dr, dm, dt, user, d1, d2, dept, user2) => new DeviceRepairExportOutput
            {
                Id = drr.Id,
                DeviceNo = dm.DeviceNo,
                AssetsNumber = dm.AssetsNumber,
                DeviceName = dm.DeviceName,
                DeviceManufacturer = dm.DeviceManufacturer,
                DeviceModel = dm.DeviceModel,
                DeviceSupplier = dm.DeviceSupplier,
                DeviceTypeName = dt.DevicetypeName,
                UseAddress = d1.FullName,
                UseDpt = dept.FullName,

                ApplicationTime = dr.ApplicationTime,
                EnabledMark = dr.EnabledMark,
                IsShutdownFault = dr.IsShutdownFault,
                FaultOccurTime = dr.FaultOccurTime,
                FaultUrgencyLevelId = d1.FullName,
                FaultDescription = dr.FaultDescription,
                RepairNo = dr.RepairNo,
                ApplicantId = user.RealName,

                RepairPersonId = user2.RealName,
                DowntimeDuration = drr.DowntimeDuration,

            })
            .ToListAsync();

            // 准备导出文件路径
            var webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
            if (!Directory.Exists(webRootPath)) Directory.CreateDirectory(webRootPath);

            string exportFileName = $"报修联络_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            string templatePath = Path.Combine(webRootPath, "SoftTemplate", "报修联络.xlsx");
            string exportFilePath = Path.Combine(webRootPath, "ExportFile", exportFileName);

            // 确保导出目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(exportFilePath));
            File.Copy(templatePath, exportFilePath, overwrite: true);

            // 3. 读取模板文件到内存
            byte[] templateBytes;
            using (var templateStream = new FileStream(exportFilePath, FileMode.Open, FileAccess.Read))
            {
                templateBytes = new byte[templateStream.Length];
                templateStream.Read(templateBytes, 0, (int)templateStream.Length);
            }

            var memoryStream = new MemoryStream(templateBytes);
            var workbook = new XSSFWorkbook(memoryStream);
            var cellStyle = CreateCellStyle(workbook);

            // 处理点检记录表
            var checkSheet = workbook.GetSheet("报修联络列表");
            int checkStartRow = 1;
            foreach (var item in list)
            {
                // 备件信息
                var spareList = _maintenanceRecordSpareRepository.AsQueryable()
                    .Where(m => m.DeleteMark == null && m.MaintenanceRecordId == item.Id)
                    .LeftJoin<WarehouseBomEntity>((s, bom) => s.BomId == bom.Id)
                    // .LeftJoin<WarehouseLocationEntity>((s, bom, loc) => s.LocationId == loc.Id)
                    .OrderBy((s, bom) => s.CreatorTime, OrderByType.Desc)
                    .Select((s, bom) => new {
                        BomName = bom.PartsName,
                        TotalAmount = int.Parse(s.Prices) * int.Parse(s.Quantity)
                    })
                    .ToList();

                // 获取逗号分割的字符串
                var bomNames = string.Join(",", spareList.Select(x => x.BomName));
                var totalAmounts = string.Join(",", spareList.Select(x => x.TotalAmount.ToString("F2"))); // 保留两位小数

                checkSheet.DefaultRowHeightInPoints = 15;
                var row = checkSheet.CreateRow(checkStartRow++);
                int col = 0;
                // 填充数据
                CreateCell(row, col++, item.DeviceNo, cellStyle);
                CreateCell(row, col++, item.AssetsNumber, cellStyle);
                CreateCell(row, col++, item.DeviceName, cellStyle);
                CreateCell(row, col++, item.DeviceManufacturer, cellStyle);
                CreateCell(row, col++, item.DeviceModel, cellStyle);
                CreateCell(row, col++, item.DeviceSupplier, cellStyle);
                CreateCell(row, col++, item.DeviceTypeName, cellStyle);
                CreateCell(row, col++, item.UseAddress, cellStyle);
                CreateCell(row, col++, item.UseDpt, cellStyle);

                CreateCell(row, col++, item.FaultOccurTime, cellStyle);  //保修时间
                CreateCell(row, col++, item.ApplicationTime, cellStyle);  //申请时间
                CreateCell(row, col++, item.FaultUrgencyLevelId, cellStyle);
                CreateCell(row, col++, item.RepairNo, cellStyle);
                CreateCell(row, col++, item.ApplicantId, cellStyle);
                CreateCell(row, col++, item.RepairPersonId, cellStyle);
                CreateCell(row, col++, item.FaultDescription, cellStyle);
                CreateCell(row, col++, item.EnabledMark == 0 ? "未处理" : "已处理", cellStyle);
                CreateCell(row, col++, item.DowntimeDuration, cellStyle);
                CreateCell(row, col++, bomNames, cellStyle);
                CreateCell(row, col++, totalAmounts, cellStyle);
                CreateCell(row, col++, item.IsShutdownFault == 0 ? "否" : "是", cellStyle);

            }

            using (var fileStream = new FileStream(exportFilePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fileStream);
            }

            workbook?.Close();
            memoryStream?.Dispose();

            return Path.Combine("ExportFile", exportFileName);
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 对树形结构进行分页处理
        /// </summary>
        private List<DeviceTypeTreeOutput> PaginateTree(List<DeviceTypeTreeOutput> fullTree, int currentPage, int pageSize)
        {
            // 展平树形结构（只包含顶级节点）
            var flatList = fullTree.ToList();

            // 计算分页范围
            int skipCount = (currentPage - 1) * pageSize;
            int takeCount = pageSize;

            // 分页处理
            return flatList
                .Skip(skipCount)
                .Take(takeCount)
                .ToList();
        }

        /// <summary>
        /// 新增设备类型
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task AddSonType(DeviceSonTypeInput input)
        {
            // 验证父类型是否存在
            if (input.ParentId == "0")
            {
                input.ParentId = input.Id;
            }

            // 生成子类型编码（格式：父类型编码_序号）
            var parentType = await _deviceTypeMasterRepository.GetSingleAsync(m => m.Id == input.ParentId);
            var maxCode = await _deviceTypeMasterRepository.AsSugarClient().Queryable<DeviceTypeMasterEntity>()
                .Where(p => p.DevicetypeCode.StartsWith(parentType.DevicetypeCode + "_") && p.DeleteMark == null && p.ParentId == input.ParentId)
                .MaxAsync(p => p.DevicetypeCode);

            int newNumber = 1;
            if (!string.IsNullOrEmpty(maxCode))
            {
                var lastNumberStr = maxCode.Split('_').Last();
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    newNumber = lastNumber + 1;
                }
            }
            string newCode = $"{parentType.DevicetypeCode}_{newNumber}";

            // 创建实体
            DeviceTypeMasterEntity entity = input.Adapt<DeviceTypeMasterEntity>();
            entity.DevicetypeCode = newCode;
            entity.FNote = input.FNote;
            entity.EnabledMark = input.F_ENABLED_MARK;
            entity.DevicetypeName = input.DevicetypeName;

            // 插入数据库
            int isOk = await _deviceTypeMasterRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
        }

        /// <summary>
        /// 变更设备类型
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task UpdateSonType(DeviceSonTypeInput input)
        {
            // 获取现有数据
            var existing = await _deviceTypeMasterRepository.GetSingleAsync(m => m.Id == input.Id);

            // 验证是否可以禁用(有子设备和挂载设备时不禁用)
            if (input.F_ENABLED_MARK == 0 && existing.EnabledMark != 0)
            {
                var list = await _deviceMasterRepository.GetListAsync(p => p.DeviceTypeId == input.Id && p.DeleteMark == null);
                if (list.Any())
                    throw Oops.Oh(ErrorCode.COM1033);
            }

            // 检查是否需要更新父类型
            if (existing.ParentId != input.ParentId)
            {
                // 生成新的设备类型编码（如果父类型变化，编码也需要变化）
                var newParentType = await _deviceTypeMasterRepository.GetSingleAsync(m => m.Id == input.ParentId);
                var maxCode = await _deviceTypeMasterRepository.AsSugarClient().Queryable<DeviceTypeMasterEntity>()
                    .Where(p => p.DevicetypeCode.StartsWith(newParentType.DevicetypeCode + "_") && p.DeleteMark == null && p.ParentId == input.ParentId)
                    .MaxAsync(p => p.DevicetypeCode);

                int newNumber = 1;
                if (!string.IsNullOrEmpty(maxCode))
                {
                    var lastNumberStr = maxCode.Split('_').Last();
                    if (int.TryParse(lastNumberStr, out int lastNumber))
                    {
                        newNumber = lastNumber + 1;
                    }
                }

                existing.DevicetypeCode = $"{newParentType.DevicetypeCode}_{newNumber}";
            }

            // 更新实体
            existing = input.Adapt(existing);

            // 更新数据库
            int isOk = await _deviceTypeMasterRepository.AsSugarClient().Updateable(existing).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6002);
        }

        /// <summary>
        /// 递归构建设备类型树
        /// </summary>
        /// <param name="allTypes">所有设备类型</param>
        /// <param name="parentId">父级ID</param>
        /// <returns></returns>
        private List<DeviceTypeTreeOutput> BuildDeviceTypeTree(List<DeviceTypeMasterEntity> allTypes, string parentId)
        {
            var tree = new List<DeviceTypeTreeOutput>();

            // 获取当前层级的设备类型
            var currentLevelTypes = allTypes.Where(t => t.ParentId == parentId).ToList();

            foreach (var type in currentLevelTypes)
            {
                var node = new DeviceTypeTreeOutput
                {
                    Id = type.Id,
                    ParentId = type.ParentId,
                    DevicetypeCode = type.DevicetypeCode,
                    DevicetypeName = type.DevicetypeName,
                    F_ENABLED_MARK = (int)type.EnabledMark,
                    FNote = type.FNote,
                    children = new List<DeviceTypeTreeOutput>()
                };

                // 递归获取子节点
                var children = BuildDeviceTypeTree(allTypes, type.Id);
                if (children.Any())
                {
                    node.children = children;
                }

                tree.Add(node);
            }

            return tree;
        }

        // 辅助方法：创建带样式的单元格
        private void CreateCell(IRow row, int col, object value, ICellStyle cellStyle)
        {
            var cell = row.CreateCell(col);
            cell.CellStyle = cellStyle;

            if (value is string strValue)
            {
                cell.SetCellValue(strValue);
                // 根据内容长度调整行高
                //if (!string.IsNullOrEmpty(strValue) && strValue.Length > 30)
                //{
                //    row.HeightInPoints = 20 * (int)Math.Ceiling(strValue.Length / 30.0);
                //}
            }
            else if (value is int intValue)
            {
                cell.SetCellValue(intValue);
            }
            else if (value is double doubleValue)
            {
                cell.SetCellValue(doubleValue);
            }
            else if (value is bool boolValue)
            {
                cell.SetCellValue(boolValue);
            }
            else
            {
                cell.SetCellValue(value?.ToString() ?? "");
            }
        }

        // 创建单元格样式
        private ICellStyle CreateCellStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();

            // 设置边框样式
            style.BorderTop = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;

            // 设置边框颜色
            style.TopBorderColor = IndexedColors.Black.Index;
            style.RightBorderColor = IndexedColors.Black.Index;
            style.BottomBorderColor = IndexedColors.Black.Index;
            style.LeftBorderColor = IndexedColors.Black.Index;

            return style;
        }
        #endregion
    }
}
