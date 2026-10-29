using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Interfaces.Custom;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 仓库/货位档案服务.
    /// 仓库层引用用友U8仓库档案（编码/名称/计价方式/是否货位管理/仓库属性/条形码，U8变更后手动点击同步）；
    /// 货位层按 区-排-列-层 编码规则（如 A1-1-1），支持最大体积/重量校验和条形码绑定，
    /// PDA扫码入库时按货位码落位；货位编码按仓库内唯一，条形码全局唯一.
    /// </summary>
    [ApiDescriptionSettings(Tag = "WarehouseArchive", Name = "", Order = 214)]
    [Route("api/[controller]")]
    public class WarehouseArchiveService : IWarehouseArchiveService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 仓库档案仓储.
        /// </summary>
        private readonly ISqlSugarRepository<WarehouseEntity> _warehouseRepository;

        /// <summary>
        /// 货位档案仓储.
        /// </summary>
        private readonly ISqlSugarRepository<WarehouseLocationEntity> _locationRepository;

        /// <summary>
        /// 库存台账仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryStockEntity> _stockRepository;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="WarehouseArchiveService"/>类型的新实例.
        /// </summary>
        public WarehouseArchiveService(
            ISqlSugarRepository<WarehouseEntity> warehouseRepository,
            ISqlSugarRepository<WarehouseLocationEntity> locationRepository,
            ISqlSugarRepository<InventoryStockEntity> stockRepository,
            IUserManager userManager)
        {
            _warehouseRepository = warehouseRepository;
            _locationRepository = locationRepository;
            _stockRepository = stockRepository;
            _userManager = userManager;
        }

        /// <summary>
        /// 仓库档案列表（按编码/名称/货位管理筛选）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetWarehouseList")]
        public async Task<dynamic> GetWarehouseList([FromBody] WarehouseQuery input)
        {
            var whereLambda = LinqExpression.And<WarehouseEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!input.RepoCode.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.RepoCode.Contains(input.RepoCode));
            }

            if (!input.RepoName.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.RepoName.Contains(input.RepoName));
            }

            if (input.IsLocationManage != -1)
            {
                whereLambda = whereLambda.And(m => m.IsLocationManage == input.IsLocationManage);
            }

            var page = await _warehouseRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.RepoCode, OrderByType.Asc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            // 货位数量实时统计（仓库列表展示用）
            var repoIds = page.list.Select(m => m.Id).ToList();
            var locations = await _locationRepository.AsQueryable()
                .Where(m => repoIds.Contains(m.WarehouseId) && m.DeleteMark == null && m.Status != 0)
                .Select(m => m.WarehouseId).ToListAsync();
            var locationCounts = locations.GroupBy(m => m).ToDictionary(m => m.Key, m => m.Count());

            var dtos = page.list.Select(m => new
            {
                m.Id,
                m.RepoCode,
                m.RepoName,
                m.DeptName,
                m.Address,
                m.ChargePerson,
                m.PricingMethod,
                m.IsLocationManage,
                m.RepoAttr,
                m.Barcode,
                LocationCount = locationCounts.ContainsKey(m.Id) ? locationCounts[m.Id] : 0,
                m.Source,
                m.SyncTime,
                m.Remark
            }).ToList();

            return new SqlSugarPagedList<dynamic>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 仓库档案新增/编辑（编码唯一校验；手工新增来源=2）.
        /// </summary>
        /// <param name="input">表单数据.</param>
        /// <returns>无.</returns>
        [HttpPost("CruWarehouse")]
        public async Task CruWarehouse([FromBody] WarehouseInput input)
        {
            if (input.RepoCode.IsNullOrEmpty() || input.RepoName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "仓库编码与仓库名称不能为空");
            }

            int isOk;
            if (input.Id.IsNullOrEmpty())
            {
                // 新增：仓库编码唯一
                if (await _warehouseRepository.IsAnyAsync(m => m.RepoCode == input.RepoCode && m.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                var entity = input.Adapt<WarehouseEntity>();
                entity.Source = 2;
                entity.LocationCount = 0;
                isOk = await _warehouseRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(isOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1000);
                }
            }
            else
            {
                // 编辑
                var existing = await _warehouseRepository.GetFirstAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                if (await _warehouseRepository.IsAnyAsync(m => m.RepoCode == input.RepoCode && m.Id != input.Id && m.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                existing = input.Adapt(existing);
                isOk = await _warehouseRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true)
                    .CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                if (!(isOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1001);
                }
            }
        }

        /// <summary>
        /// 仓库档案删除（存在货位或库存时拦截）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteWarehouse")]
        public async Task DeleteWarehouse(string id)
        {
            var entity = await _warehouseRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 存在货位禁止删除
            if (await _locationRepository.IsAnyAsync(m => m.WarehouseId == id && m.DeleteMark == null))
            {
                throw Oops.Oh(ErrorCode.COM1018, "该仓库下存在货位档案，请先删除货位后再删除仓库");
            }

            // 存在库存禁止删除
            if (await _stockRepository.IsAnyAsync(m => m.WarehouseId == id && m.StockQty > 0 && m.DeleteMark == null))
            {
                throw Oops.Oh(ErrorCode.COM1018, "该仓库下存在在库库存，禁止删除");
            }

            int isOk = await _warehouseRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete())
                .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                .ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 货位档案列表（按仓库/货位编码筛选，含所属仓库名称）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetLocationList")]
        public async Task<dynamic> GetLocationList([FromBody] WarehouseLocationQuery input)
        {
            var whereLambda = LinqExpression.And<WarehouseLocationEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!input.WarehouseId.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.WarehouseId == input.WarehouseId);
            }

            if (!input.LocationCode.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.LocationCode.Contains(input.LocationCode));
            }

            var page = await _locationRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.LocationCode, OrderByType.Asc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            // 所属仓库名称（货位表 warehouse_name 为库位名称，仓库名从仓库档案取）
            var repoIds = page.list.Select(m => m.WarehouseId).Where(m => !m.IsNullOrEmpty()).Distinct().ToList();
            var warehouses = await _warehouseRepository.AsQueryable()
                .Where(m => repoIds.Contains(m.Id) && m.DeleteMark == null)
                .ToListAsync();

            var dtos = page.list.Select(m =>
            {
                var repo = warehouses.FirstOrDefault(w => w.Id == m.WarehouseId);
                return new WarehouseLocationListDto
                {
                    Id = m.Id,
                    LocationCode = m.LocationCode,
                    WarehouseName = m.WarehouseName,
                    RepoName = repo?.RepoName,
                    MaxVolume = m.MaxVolume,
                    MaxWeight = m.MaxWeight,
                    Barcode = m.Barcode,
                    Status = m.Status
                };
            }).ToList();

            return new SqlSugarPagedList<WarehouseLocationListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 货位档案新增/编辑（编码 区-排-列-层 解析与唯一校验、体积重量、条码）.
        /// </summary>
        /// <param name="input">表单数据.</param>
        /// <returns>无.</returns>
        [HttpPost("CruLocation")]
        public async Task CruLocation([FromBody] WarehouseLocationInput input)
        {
            if (input.LocationCode.IsNullOrEmpty() || input.WarehouseId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "货位编码与所属仓库不能为空");
            }

            var warehouse = await _warehouseRepository.GetFirstAsync(m => m.Id == input.WarehouseId && m.DeleteMark == null);
            if (warehouse == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "所属仓库不存在");
            }

            // 编码解析：区-排-列-层（如 A1-1-1 或紧凑格式 A10101）
            ParseLocationCode(input);

            // 条形码全局唯一
            if (!input.Barcode.IsNullOrEmpty()
                && await _locationRepository.IsAnyAsync(m => m.Barcode == input.Barcode && m.Id != input.Id && m.DeleteMark == null))
            {
                throw Oops.Oh(ErrorCode.COM1018, $"条形码 {input.Barcode} 已被其他货位绑定");
            }

            int isOk;
            if (input.Id.IsNullOrEmpty())
            {
                // 新增：同仓库内货位编码唯一
                if (await _locationRepository.IsAnyAsync(m => m.WarehouseId == input.WarehouseId
                    && m.LocationCode == input.LocationCode && m.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"仓库内货位编码 {input.LocationCode} 已存在");
                }

                var entity = input.Adapt<WarehouseLocationEntity>();
                entity.WarehouseCode = warehouse.RepoCode;
                entity.Status = input.Status ?? 1;
                isOk = await _locationRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(isOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1000);
                }
            }
            else
            {
                // 编辑
                var existing = await _locationRepository.GetFirstAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                if (await _locationRepository.IsAnyAsync(m => m.WarehouseId == input.WarehouseId
                    && m.LocationCode == input.LocationCode && m.Id != input.Id && m.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"仓库内货位编码 {input.LocationCode} 已存在");
                }

                existing = input.Adapt(existing);
                existing.WarehouseCode = warehouse.RepoCode;
                existing.WarehouseId = input.WarehouseId;
                isOk = await _locationRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true)
                    .CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                if (!(isOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1001);
                }
            }

            // 维护仓库货位数量
            await RefreshLocationCount(input.WarehouseId);
        }

        /// <summary>
        /// 货位档案删除（存在在库库存时拦截）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteLocation")]
        public async Task DeleteLocation(string id)
        {
            var entity = await _locationRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 货位上存在在库库存禁止删除
            if (await _stockRepository.IsAnyAsync(m => m.LocationId == id && m.StockQty > 0 && m.DeleteMark == null))
            {
                throw Oops.Oh(ErrorCode.COM1018, "该货位上存在在库库存，禁止删除");
            }

            int isOk = await _locationRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete())
                .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                .ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            // 维护仓库货位数量
            if (!entity.WarehouseId.IsNullOrEmpty())
            {
                await RefreshLocationCount(entity.WarehouseId);
            }
        }

        /// <summary>
        /// 按库位码扫码查询（兼容货位编码/条形码；PDA扫码落位）.
        /// </summary>
        /// <param name="code">扫码内容（货位编码或条形码）.</param>
        /// <returns>货位信息.</returns>
        [HttpGet("GetLocationByCode")]
        public async Task<dynamic> GetLocationByCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "库位码不能为空");
            }

            // 兼容货位编码 / 条形码
            var entity = await _locationRepository.GetFirstAsync(m => m.DeleteMark == null
                && (m.LocationCode == code || m.Barcode == code));
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"库位码 {code} 无效，请核对后重新扫码");
            }

            if (entity.Status == 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"货位 {entity.LocationCode} 已停用，禁止落位");
            }

            var warehouse = await _warehouseRepository.GetFirstAsync(m => m.Id == entity.WarehouseId && m.DeleteMark == null);

            return new LocationScanOutput
            {
                Id = entity.Id,
                LocationCode = entity.LocationCode,
                WarehouseId = entity.WarehouseId,
                WarehouseCode = entity.WarehouseCode ?? warehouse?.RepoCode,
                WarehouseName = warehouse?.RepoName,
                Barcode = entity.Barcode,
                Status = entity.Status
            };
        }

        /// <summary>
        /// 从用友U8同步仓库档案（模拟实现：U8档案变更后手动点击同步，仅刷新同步时间）.
        /// </summary>
        /// <returns>无.</returns>
        [HttpPost("SyncWarehouseFromYonyou")]
        public async Task SyncWarehouseFromYonyou()
        {
            // 模拟：从用友U8拉取仓库档案（编码/名称/计价方式/是否货位管理/仓库属性/条形码）
            var warehouses = await _warehouseRepository.AsQueryable()
                .Where(m => m.DeleteMark == null).ToListAsync();
            if (!warehouses.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "暂无仓库档案，无法从用友U8同步");
            }

            var now = DateTime.Now;
            foreach (var item in warehouses)
            {
                if (item.Source == null)
                {
                    item.Source = 1;
                }

                item.SyncTime = now;
            }

            await _warehouseRepository.AsSugarClient().Updateable(warehouses)
                .UpdateColumns(it => new { it.Source, it.SyncTime })
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 解析货位编码 区-排-列-层（输入已填时优先使用输入值）.
        /// </summary>
        /// <param name="input">货位表单数据.</param>
        private static void ParseLocationCode(WarehouseLocationInput input)
        {
            if (input.LocationCode.IsNullOrEmpty()
                || (!input.AreaNo.IsNullOrEmpty() && !input.RowNo.IsNullOrEmpty()))
            {
                return;
            }

            var code = input.LocationCode.Trim();
            if (code.Contains("-"))
            {
                // 格式：区-排-列-层，如 A1-1-1 或 A1-1-1-1
                var parts = code.Split('-');
                if (parts.Length > 0 && input.AreaNo.IsNullOrEmpty())
                {
                    input.AreaNo = parts[0];
                }

                if (parts.Length > 1 && input.RowNo.IsNullOrEmpty())
                {
                    input.RowNo = parts[1];
                }

                if (parts.Length > 2 && input.ColumnNo.IsNullOrEmpty())
                {
                    input.ColumnNo = parts[2];
                }

                if (parts.Length > 3 && input.LayerNo.IsNullOrEmpty())
                {
                    input.LayerNo = parts[3];
                }
            }
            else
            {
                // 紧凑格式：区A1+排01+列01，如 A10101
                var match = Regex.Match(code, @"^([A-Za-z]+\d+)(\d{2})(\d{2})$");
                if (match.Success)
                {
                    if (input.AreaNo.IsNullOrEmpty())
                    {
                        input.AreaNo = match.Groups[1].Value;
                    }

                    if (input.RowNo.IsNullOrEmpty())
                    {
                        input.RowNo = match.Groups[2].Value;
                    }

                    if (input.ColumnNo.IsNullOrEmpty())
                    {
                        input.ColumnNo = match.Groups[3].Value;
                    }
                }
                else if (input.AreaNo.IsNullOrEmpty())
                {
                    input.AreaNo = code;
                }
            }
        }

        /// <summary>
        /// 刷新仓库货位数量.
        /// </summary>
        /// <param name="warehouseId">仓库ID.</param>
        /// <returns>无.</returns>
        private async Task RefreshLocationCount(string warehouseId)
        {
            var count = await _locationRepository.CountAsync(m => m.WarehouseId == warehouseId && m.DeleteMark == null);
            await _warehouseRepository.AsSugarClient().Updateable<WarehouseEntity>()
                .SetColumns(it => it.LocationCount == count)
                .Where(it => it.Id == warehouseId)
                .ExecuteCommandAsync();
        }
    }
}
