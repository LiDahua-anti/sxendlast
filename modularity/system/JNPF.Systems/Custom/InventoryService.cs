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
using JNPF.Systems.Interfaces.Custom;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 存货管理（技术管理）.
    /// 存货档案基础数据从用友U8同步，本页维护存货与子件的BOM绑定关系（子件数量、损耗率），
    /// 并在编辑中绑定该存货在各工序的检查项目（PDA报工时展示）.
    /// </summary>
    [ApiDescriptionSettings(Tag = "Inventory", Name = "", Order = 201)]
    [Route("api/[controller]")]
    public class InventoryService : IInventoryService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 存货档案主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryMasterEntity> _inventoryMasterRepository;

        /// <summary>
        /// 存货BOM子件明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryBomEntity> _inventoryBomRepository;

        /// <summary>
        /// 存货工序检查项目配置仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryCheckItemEntity> _inventoryCheckItemRepository;

        /// <summary>
        /// 工序主数据仓储.
        /// </summary>
        private readonly ISqlSugarRepository<BaseProcessEntity> _baseProcessRepository;

        /// <summary>
        /// 用友同步服务.
        /// </summary>
        private readonly IYonyouSyncService _yonyouSyncService;

        /// <summary>
        /// 初始化一个<see cref="InventoryService"/>类型的新实例.
        /// </summary>
        public InventoryService(
            ISqlSugarRepository<InventoryMasterEntity> inventoryMasterRepository,
            ISqlSugarRepository<InventoryBomEntity> inventoryBomRepository,
            ISqlSugarRepository<InventoryCheckItemEntity> inventoryCheckItemRepository,
            ISqlSugarRepository<BaseProcessEntity> baseProcessRepository,
            IYonyouSyncService yonyouSyncService)
        {
            _inventoryMasterRepository = inventoryMasterRepository;
            _inventoryBomRepository = inventoryBomRepository;
            _inventoryCheckItemRepository = inventoryCheckItemRepository;
            _baseProcessRepository = baseProcessRepository;
            _yonyouSyncService = yonyouSyncService;
        }

        #region 存货档案

        /// <summary>
        /// 获取存货档案列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetInventoryList")]
        public async Task<dynamic> GetInventoryList([FromBody] InventoryQuery input)
        {
            // 构建查询条件
            var whereLambda = LinqExpression.And<InventoryMasterEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.InventoryCode))
            {
                whereLambda = whereLambda.And(m => m.InventoryCode.Contains(input.InventoryCode));
            }

            if (!string.IsNullOrEmpty(input.InventoryName))
            {
                whereLambda = whereLambda.And(m => m.InventoryName.Contains(input.InventoryName));
            }

            if (!string.IsNullOrEmpty(input.Category))
            {
                whereLambda = whereLambda.And(m => m.Category == input.Category);
            }

            if (input.MakeType != -1)
            {
                whereLambda = whereLambda.And(m => m.MakeType == input.MakeType);
            }

            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.EnabledMark == input.Status);
            }

            // 有无子件筛选
            if (input.HasChild != -1)
            {
                var parentIds = await _inventoryBomRepository.AsQueryable()
                    .Where(b => b.DeleteMark == null)
                    .Select(b => b.ParentInventoryId)
                    .Distinct()
                    .ToListAsync();
                if (input.HasChild == 1)
                {
                    whereLambda = whereLambda.And(m => parentIds.Contains(m.Id));
                }
                else
                {
                    whereLambda = whereLambda.And(m => !parentIds.Contains(m.Id));
                }
            }

            var page = await _inventoryMasterRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            // 统计当前页存货的子件条数与工序数
            var ids = page.list.Select(x => x.Id).ToList();
            var bomCountDict = new Dictionary<string, int>();
            var processCountDict = new Dictionary<string, int>();
            if (ids.Any())
            {
                var bomParents = await _inventoryBomRepository.AsQueryable()
                    .Where(b => b.DeleteMark == null && ids.Contains(b.ParentInventoryId))
                    .Select(b => b.ParentInventoryId)
                    .ToListAsync();
                bomCountDict = bomParents.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                var checkRows = await _inventoryCheckItemRepository.AsQueryable()
                    .Where(c => c.DeleteMark == null && ids.Contains(c.InventoryId))
                    .Select(c => new { c.InventoryId, c.ProcessId })
                    .ToListAsync();
                processCountDict = checkRows.GroupBy(x => x.InventoryId)
                    .ToDictionary(g => g.Key, g => g.Select(z => z.ProcessId).Distinct().Count());
            }

            var dtos = page.list.Select(m => new InventoryListDto
            {
                Id = m.Id,
                InventoryCode = m.InventoryCode,
                InventoryName = m.InventoryName,
                Spec = m.Spec,
                Category = m.Category,
                Unit = m.Unit,
                MakeType = m.MakeType,
                MakeTypeName = GetMakeTypeName(m.MakeType),
                BomCount = bomCountDict.TryGetValue(m.Id, out var bc) ? bc : 0,
                ProcessCount = processCountDict.TryGetValue(m.Id, out var pc) ? pc : 0,
                Source = m.Source,
                SyncTime = m.SyncTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                EnabledMark = m.EnabledMark
            }).ToList();

            return new SqlSugarPagedList<InventoryListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 获取存货档案详情（含工序检查项目配置）.
        /// </summary>
        /// <param name="id">存货主键.</param>
        /// <returns>存货详情.</returns>
        [HttpGet("GetInventoryById")]
        public async Task<dynamic> GetInventoryById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _inventoryMasterRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.D6005);
            }

            var result = entity.Adapt<InventoryCruInput>();
            result.CheckItems = await BuildCheckItemDtos(id);
            return result;
        }

        /// <summary>
        /// 新增/编辑存货档案（同时保存工序检查项目配置）.
        /// </summary>
        /// <param name="input">存货信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruInventory")]
        public async Task CruInventory([FromBody] InventoryCruInput input)
        {
            if (input.InventoryCode.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            int isOk;
            string inventoryId;
            if (!string.IsNullOrEmpty(input.Id))
            {
                // 编辑
                var existing = await _inventoryMasterRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.D6005);
                }

                // 存货编码唯一性校验（排除自身）
                if (await _inventoryMasterRepository.IsAnyAsync(p => p.InventoryCode == input.InventoryCode && p.Id != input.Id && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.D6005);
                }

                existing = input.Adapt(existing);
                isOk = await _inventoryMasterRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                inventoryId = existing.Id;
            }
            else
            {
                // 存货编码唯一性校验
                if (await _inventoryMasterRepository.IsAnyAsync(p => p.InventoryCode == input.InventoryCode && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.D6005);
                }

                var entity = input.Adapt<InventoryMasterEntity>();
                entity.Source = 2; // 手工新增
                isOk = await _inventoryMasterRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                inventoryId = entity.Id;
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.D6001);
            }

            // 覆盖式保存工序检查项目配置
            await SaveCheckItemsInternal(inventoryId, input.CheckItems);
        }

        /// <summary>
        /// 删除存货档案（级联软删除BOM子件与检查项目）.
        /// </summary>
        /// <param name="id">存货主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteInventory")]
        public async Task DeleteInventory(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _inventoryMasterRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _inventoryMasterRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            // 级联软删除：作为母件的BOM子件绑定
            var boms = await _inventoryBomRepository.GetListAsync(b => b.ParentInventoryId == id && b.DeleteMark == null);
            if (boms.Any())
            {
                await _inventoryBomRepository.AsSugarClient().Updateable(boms)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            // 级联软删除：工序检查项目配置
            var items = await _inventoryCheckItemRepository.GetListAsync(c => c.InventoryId == id && c.DeleteMark == null);
            if (items.Any())
            {
                await _inventoryCheckItemRepository.AsSugarClient().Updateable(items)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 存货下拉/检索（用于绑定子件时选择存货）.
        /// </summary>
        /// <param name="keyword">存货编码/名称关键字.</param>
        /// <returns>存货选择项.</returns>
        [HttpGet("GetInventorySelected")]
        public async Task<dynamic> GetInventorySelected(string keyword)
        {
            var query = _inventoryMasterRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.EnabledMark == 1);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(m => m.InventoryCode.Contains(keyword) || m.InventoryName.Contains(keyword));
            }

            var list = await query
                .OrderBy(m => m.CreatorTime, OrderByType.Desc)
                .Take(50)
                .Select(m => new InventorySelectedOutput
                {
                    id = m.Id,
                    code = m.InventoryCode,
                    name = m.InventoryName,
                    unit = m.Unit
                })
                .ToListAsync();
            return list;
        }

        #endregion

        #region BOM子件绑定

        /// <summary>
        /// 获取某母件的BOM子件明细.
        /// </summary>
        /// <param name="inventoryId">母件存货ID.</param>
        /// <returns>子件明细列表.</returns>
        [HttpGet("GetBomChildren")]
        public async Task<dynamic> GetBomChildren(string inventoryId)
        {
            if (inventoryId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var list = await _inventoryBomRepository.AsQueryable()
                .Where(b => b.DeleteMark == null && b.ParentInventoryId == inventoryId)
                .OrderBy(b => b.SortCode)
                .LeftJoin<InventoryMasterEntity>((b, ci) => b.ChildInventoryId == ci.Id)
                .Select((b, ci) => new InventoryBomListDto
                {
                    Id = b.Id,
                    ParentInventoryId = b.ParentInventoryId,
                    ChildInventoryId = b.ChildInventoryId,
                    ChildInventoryCode = b.ChildInventoryCode,
                    ChildInventoryName = ci.InventoryName,
                    Spec = ci.Spec,
                    Unit = ci.Unit,
                    Quantity = b.Quantity,
                    LossRate = b.LossRate,
                    PositionNo = b.PositionNo
                })
                .ToListAsync();
            return list;
        }

        /// <summary>
        /// 绑定/保存某母件的BOM子件清单（整体覆盖）.
        /// </summary>
        /// <param name="input">母件及子件明细.</param>
        /// <returns>无.</returns>
        [HttpPost("BindBom")]
        public async Task BindBom([FromBody] InventoryBomBindInput input)
        {
            if (input.ParentInventoryId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var master = await _inventoryMasterRepository.GetSingleAsync(m => m.Id == input.ParentInventoryId && m.DeleteMark == null);
            if (master == null)
            {
                throw Oops.Oh(ErrorCode.D6005);
            }

            // 软删除旧绑定关系
            var oldList = await _inventoryBomRepository.GetListAsync(b => b.ParentInventoryId == input.ParentInventoryId && b.DeleteMark == null);
            if (oldList.Any())
            {
                await _inventoryBomRepository.AsSugarClient().Updateable(oldList)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            if (input.Items == null || !input.Items.Any())
            {
                return;
            }

            // 加载子件存货主数据，回填编码/校验有效性
            var childIds = input.Items.Where(x => !string.IsNullOrEmpty(x.ChildInventoryId))
                .Select(x => x.ChildInventoryId).Distinct().ToList();
            var childDict = new Dictionary<string, InventoryMasterEntity>();
            if (childIds.Any())
            {
                var children = await _inventoryMasterRepository.GetListAsync(m => childIds.Contains(m.Id) && m.DeleteMark == null);
                childDict = children.ToDictionary(m => m.Id, m => m);
            }

            int sort = 1;
            var entities = new List<InventoryBomEntity>();
            foreach (var item in input.Items)
            {
                // 过滤空子件、以及绑定自身的情况
                if (string.IsNullOrEmpty(item.ChildInventoryId) || item.ChildInventoryId == input.ParentInventoryId)
                {
                    continue;
                }

                childDict.TryGetValue(item.ChildInventoryId, out var child);
                var entity = new InventoryBomEntity
                {
                    ParentInventoryId = input.ParentInventoryId,
                    ParentInventoryCode = master.InventoryCode,
                    ChildInventoryId = item.ChildInventoryId,
                    ChildInventoryCode = child?.InventoryCode ?? item.ChildInventoryCode,
                    Quantity = item.Quantity,
                    LossRate = item.LossRate,
                    PositionNo = item.PositionNo,
                    SortCode = sort++
                };
                entities.Add(entity);
            }

            if (entities.Any())
            {
                int isOk = await _inventoryBomRepository.AsSugarClient().Insertable(entities)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(isOk > 0))
                {
                    throw Oops.Oh(ErrorCode.D6001);
                }
            }
        }

        /// <summary>
        /// 删除单条BOM子件绑定.
        /// </summary>
        /// <param name="id">BOM明细主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteBom")]
        public async Task DeleteBom(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _inventoryBomRepository.GetSingleAsync(b => b.Id == id && b.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _inventoryBomRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 获取某存货的工序检查项目配置.
        /// </summary>
        /// <param name="inventoryId">存货ID.</param>
        /// <returns>检查项目集合.</returns>
        [HttpGet("GetCheckItems")]
        public async Task<dynamic> GetCheckItems(string inventoryId)
        {
            if (inventoryId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return await BuildCheckItemDtos(inventoryId);
        }

        #endregion

        #region 用友同步

        /// <summary>
        /// 从用友U8同步存货档案（模拟实现：生成存货并写同步日志）.
        /// </summary>
        /// <param name="input">拉取入参（可选 count 控制生成条数）.</param>
        /// <returns>同步结果.</returns>
        [HttpPost("SyncFromYonyou")]
        public async Task<dynamic> SyncFromYonyou([FromBody] SyncPullInput input)
        {
            var count = input?.Count ?? 5;
            if (count <= 0) count = 5;
            if (count > 50) count = 50;

            var now = DateTime.Now;
            var created = new List<InventoryMasterEntity>();
            var categories = new[] { "产成品", "主生产材料", "辅助材料", "半成品" };
            var units = new[] { "PCS", "KG", "M", "套" };
            var rng = new Random();

            for (var i = 1; i <= count; i++)
            {
                var code = "CH-" + now.ToString("yyyyMMdd") + "-" + (i + DateTime.Now.Hour).ToString("D4");
                var entity = new InventoryMasterEntity
                {
                    Id = SnowflakeIdHelper.NextId(),
                    InventoryCode = code,
                    InventoryName = $"用友同步存货{i}",
                    Spec = $"规格{rng.Next(100, 999)}",
                    Category = categories[rng.Next(categories.Length)],
                    Unit = units[rng.Next(units.Length)],
                    MakeType = rng.Next(1, 4),
                    Source = 1,
                    SyncTime = now,
                    EnabledMark = 1
                };
                created.Add(entity);
            }

            await _inventoryMasterRepository.AsSugarClient().Insertable(created).ExecuteCommandAsync();

            // 写用友同步日志
            var bizNo = string.Join(",", created.Select(m => m.InventoryCode));
            if (bizNo.Length > 50) bizNo = bizNo.Substring(0, 50);
            await _yonyouSyncService.WriteAutoPushLogAsync("存货档案", bizNo, $"用友同步存货 {count} 条");

            return new
            {
                success = true,
                imported = created.Count,
                codes = created.Select(m => m.InventoryCode).ToList()
            };
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 覆盖式保存某存货的工序检查项目配置.
        /// </summary>
        /// <param name="inventoryId">存货ID.</param>
        /// <param name="items">检查项目集合.</param>
        private async Task SaveCheckItemsInternal(string inventoryId, List<InventoryCheckItemDto> items)
        {
            // 软删除旧配置
            var oldList = await _inventoryCheckItemRepository.GetListAsync(c => c.InventoryId == inventoryId && c.DeleteMark == null);
            if (oldList.Any())
            {
                await _inventoryCheckItemRepository.AsSugarClient().Updateable(oldList)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            if (items == null || !items.Any())
            {
                return;
            }

            // 加载工序主数据，回填工序编码/名称
            var processIds = items.Where(x => !string.IsNullOrEmpty(x.ProcessId))
                .Select(x => x.ProcessId).Distinct().ToList();
            var processDict = new Dictionary<string, BaseProcessEntity>();
            if (processIds.Any())
            {
                var processes = await _baseProcessRepository.GetListAsync(p => processIds.Contains(p.Id) && p.DeleteMark == null);
                processDict = processes.ToDictionary(p => p.Id, p => p);
            }

            int sort = 1;
            var entities = new List<InventoryCheckItemEntity>();
            foreach (var item in items)
            {
                var entity = item.Adapt<InventoryCheckItemEntity>();
                entity.InventoryId = inventoryId;
                entity.SortCode = sort++;
                if (!string.IsNullOrEmpty(item.ProcessId) && processDict.TryGetValue(item.ProcessId, out var process))
                {
                    entity.ProcessCode = process.ProcessCode;
                    entity.ProcessName = process.ProcessName;
                }

                entities.Add(entity);
            }

            int isOk = await _inventoryCheckItemRepository.AsSugarClient().Insertable(entities)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.D6001);
            }
        }

        /// <summary>
        /// 构建某存货的检查项目DTO集合.
        /// </summary>
        /// <param name="inventoryId">存货ID.</param>
        private async Task<List<InventoryCheckItemDto>> BuildCheckItemDtos(string inventoryId)
        {
            var list = await _inventoryCheckItemRepository.AsQueryable()
                .Where(c => c.DeleteMark == null && c.InventoryId == inventoryId)
                .OrderBy(c => c.SortCode)
                .ToListAsync();

            return list.Select(c => new InventoryCheckItemDto
            {
                Id = c.Id,
                ProcessId = c.ProcessId,
                ProcessCode = c.ProcessCode,
                ProcessName = c.ProcessName,
                CheckItem = c.CheckItem,
                CheckStandard = c.CheckStandard,
                DataType = c.DataType,
                OptionItems = c.OptionItems,
                DefaultValue = c.DefaultValue,
                Required = c.Required,
                Sort = (int)(c.SortCode ?? 0)
            }).ToList();
        }

        /// <summary>
        /// 生产方式名称转换：1自制 2委外 3采购.
        /// </summary>
        /// <param name="makeType">生产方式值.</param>
        private static string GetMakeTypeName(int? makeType)
        {
            return makeType switch
            {
                1 => "自制",
                2 => "委外",
                3 => "采购",
                _ => string.Empty
            };
        }

        #endregion
    }
}
