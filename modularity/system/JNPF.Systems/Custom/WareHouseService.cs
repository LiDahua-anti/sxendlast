using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Custom;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JNPF.Common.Manager;
using SqlSugar;
using JNPF.Common.Extension;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using Mapster;
using JNPF.Common.Enums;
using JNPF.FriendlyException;
using System.Collections;
using BaiduBce.Services.Bos.Model;
using Aop.Api.Domain;
using NPOI.SS.Formula;
using Microsoft.AspNetCore.Hosting;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 备品库管理
    /// </summary>
    [ApiDescriptionSettings(Tag = "WareHouse", Name = "", Order = 200)]
    [Route("api/[controller]")]
    public class WareHouseService : IWareHouseService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 服务基础仓储.
        /// </summary>
        private readonly ISqlSugarRepository<WarehouseBomEntity> _warehouseBomRepository;
        private readonly ISqlSugarRepository<WarehouseLocationEntity> _warehouseLocationRepository;
        private readonly ISqlSugarRepository<WarehouseReceiveEntity> _warehouseReceiveRepository;
        private readonly ISqlSugarRepository<OrganizeEntity> _organizeRepository;
        private readonly ISqlSugarRepository<DeviceMasterEntity> _deviceMasterRepository;
        private readonly ISqlSugarRepository<DictionaryTypeEntity> _dictionaryTypeRepository;
        private readonly ISqlSugarRepository<DictionaryDataEntity> _dictionaryDataRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISqlSugarRepository<UserEntity> _repository;  // 用户表仓储

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
        public WareHouseService(
            ISqlSugarRepository<WarehouseBomEntity> warehouseBomRepository,
            ISqlSugarRepository<WarehouseLocationEntity> warehouseLocationRepository,
            ISqlSugarRepository<WarehouseReceiveEntity> warehouseReceiveRepository,
            ISqlSugarRepository<DeviceMasterEntity> deviceMasterRepository,
            ISqlSugarRepository<OrganizeEntity> organizeRepository,
            IWebHostEnvironment webHostEnvironment,
            ISqlSugarRepository<DictionaryTypeEntity> dictionaryTypeRepository,
            ISqlSugarRepository<DictionaryDataEntity> dictionaryDataRepository,
            ICacheManager cacheManager,
            ISqlSugarRepository<UserEntity> userRepository,
            IUserManager userManager,
            IFileManager fileManager)
        {
            _warehouseBomRepository = warehouseBomRepository;
            _warehouseLocationRepository = warehouseLocationRepository;
            _warehouseReceiveRepository = warehouseReceiveRepository;
            _webHostEnvironment = webHostEnvironment;
            _deviceMasterRepository = deviceMasterRepository;
            _dictionaryTypeRepository = dictionaryTypeRepository;
            _dictionaryDataRepository = dictionaryDataRepository;
            _organizeRepository = organizeRepository;
            _cacheManager = cacheManager;
            _repository = userRepository;
            _userManager = userManager;
            _fileManager = fileManager;
        }

        /// <summary>
        /// 获取所有零部件
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("GetBoms")]
        public async Task<dynamic> GetBoms(WarehouseBomQuery input)
        {
            var whereLambda = LinqExpression.And<WarehouseBomEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (input.keyword.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m =>
                    m.PartsName.Contains(input.keyword));
            }
            var list = await _warehouseBomRepository.AsQueryable()
                .Where(whereLambda)
                .Select(p => new WarehouseBomEntity
                {
                    Id = p.Id,
                    PartsManufacturer = p.PartsManufacturer,
                    PartsName = p.PartsName,
                    PartsModel = p.PartsModel,
                    SafeInlibrary = p.SafeInlibrary,
                    Note = p.Note,
                    Unit = p.Unit,
                    UnitPrice = p.UnitPrice,
                    ModelSpec = p.ModelSpec,
                    CreatorTime = p.CreatorTime,
                })
                .ToPagedListAsync(input.currentPage, input.pageSize);
            return list;
        }


        /// <summary>
        /// 获取所有库位
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetLocations")]
        public async Task<dynamic> GetLocations()
        {
            var list = await _warehouseLocationRepository.GetListAsync(m => m.DeleteMark == null);
            return list.Select(m => new {
                Id = m.Id,
                name = m.WarehouseName,
            }).ToList();
        }

        #region 领用管理

        /// <summary>
        /// 领用列表
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("receive/GetReceives")]
        public async Task<dynamic> GetReceives(WarehouseReceiveQuery input)
        {
            // 构建查询条件
            var whereLambda = LinqExpression.And<WarehouseReceiveEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);
            if (input.LocationId.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m => m.LocationId.Contains(input.LocationId));
            }
            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.Status.Equals(input.Status));
            }
            if (input.RecUserName.IsNotEmptyOrNull())
            {
                whereLambda = whereLambda.And(m => m.CreatorUserId.Contains(input.RecUserName));
            }
            if (input.StartTime.IsNotEmptyOrNull())
            {
                DateTime startTime = DateTime.Parse(input.StartTime);
                whereLambda = whereLambda.And(m => m.CreatorTime >= startTime);
            }
            if (input.EndTime.IsNotEmptyOrNull())
            {
                DateTime endTime = DateTime.Parse(input.EndTime).AddDays(1).AddSeconds(-1);
                whereLambda = whereLambda.And(m => m.CreatorTime <= endTime);
            }

            var query = _warehouseReceiveRepository.AsQueryable().Where(whereLambda);

            // 关联表
            var joinedQuery = query
                .LeftJoin<WarehouseBomEntity>((re, bom) => re.BomId == bom.Id)
                .LeftJoin<DeviceMasterEntity>((re, bom, dm) => dm.Id == re.DeviceId)
                .LeftJoin<OrganizeEntity>((re, bom, dm, d1) => d1.Id == dm.UseDpt)
                .LeftJoin<DeviceRepairRecordEntity>((re, bom, dm, d1, drr) => re.ExternalId == drr.Id)
                //.LeftJoin<WarehouseLocationEntity>((re, bom, dm, d1, drr, loc) => re.LocationId == loc.Id)
                .LeftJoin<UserEntity>((re, bom, dm, d1, drr, user) => re.CreatorUserId == user.Id);

            // 在关联后添加BomName查询条件
            if (input.BomName.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((re, bom, user) => bom.PartsName.Contains(input.BomName));
            }
            if (input.PartsManufacturer.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((re, bom, user) => bom.PartsManufacturer.Contains(input.PartsManufacturer));
            }
            if (input.PartsModel.IsNotEmptyOrNull())
            {
                joinedQuery = joinedQuery.Where((re, bom, user) => bom.PartsModel.Contains(input.PartsModel));
            }

            var list = await joinedQuery
                .Select((re, bom, dm, d1, drr, user) => new WarehouseReceiveOutput
                {
                    Id = re.Id,
                    BomId = re.BomId,
                    ReceiveQuantity = re.ReceiveQuantity,
                    //LocationId = loc.WarehouseName,
                    Status = re.Status,
                    Remark = re.Remark,
                    ExternalId = drr.RepairRecordNo,
                    UserDeptName = d1.FullName,
                    DeviceName = dm.DeviceName,
                    ReceiveType = re.ReceiveType,
                    RepairRecordNo = drr.RepairRecordNo,
                    //物料
                    PartsManufacturer = bom.PartsManufacturer,
                    PartsName = bom.PartsName,
                    PartsModel = bom.PartsModel,
                    SafeInlibrary = bom.SafeInlibrary,
                    // 用户信息
                    CreatorTime = re.CreatorTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatorUserName = user.RealName,
                })
                .ToPagedListAsync(input.currentPage, input.pageSize);

            return list;
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("receive/Insert")]
        public async Task InsertPlan([FromBody] WarehouseReceiveInput input)
        {
            WarehouseReceiveEntity entity = input.Adapt<WarehouseReceiveEntity>();
            entity.ReceiveType = 1;
            int isOk = await _warehouseReceiveRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
        }

        /// <summary>
        ///  删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("receive/Delete")]
        public async Task Delete(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }
            WarehouseReceiveEntity? entity = await _warehouseReceiveRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            int isOk = await _warehouseReceiveRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
                throw Oops.Oh(ErrorCode.COM1002);
        }

        #endregion

        #region 私有函数

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
