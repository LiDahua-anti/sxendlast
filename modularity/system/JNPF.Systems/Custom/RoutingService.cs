using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Custom;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 工艺路线管理（技术管理）.
    /// 工艺路线 = 存货（母件）× 工序顺序明细 的绑定关系；
    /// 是排产的数据来源：工艺线路→工序明细→工序可用设备级联带出；
    /// 工序检查项随路线流转至 PDA 报工展示。
    /// </summary>
    [ApiDescriptionSettings(Tag = "Routing", Name = "", Order = 204)]
    [Route("api/[controller]")]
    public class RoutingService : IRoutingService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 工艺路线主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<RoutingEntity> _routingRepository;

        /// <summary>
        /// 工艺路线工序明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<RoutingProcessEntity> _routingProcessRepository;

        /// <summary>
        /// 基础工序仓储（sx_base_process）.
        /// </summary>
        private readonly ISqlSugarRepository<BaseProcessEntity> _baseProcessRepository;

        /// <summary>
        /// 工序检查内容定义仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProcessCheckItemEntity> _checkItemRepository;

        /// <summary>
        /// 存货主档仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryMasterEntity> _inventoryRepository;

        /// <summary>
        /// 用户仓储（用于反查创建人名称）.
        /// </summary>
        private readonly ISqlSugarRepository<UserEntity> _userRepository;

        /// <summary>
        /// 初始化一个<see cref="RoutingService"/>类型的新实例.
        /// </summary>
        public RoutingService(
            ISqlSugarRepository<RoutingEntity> routingRepository,
            ISqlSugarRepository<RoutingProcessEntity> routingProcessRepository,
            ISqlSugarRepository<BaseProcessEntity> baseProcessRepository,
            ISqlSugarRepository<ProcessCheckItemEntity> checkItemRepository,
            ISqlSugarRepository<InventoryMasterEntity> inventoryRepository,
            ISqlSugarRepository<UserEntity> userRepository)
        {
            _routingRepository = routingRepository;
            _routingProcessRepository = routingProcessRepository;
            _baseProcessRepository = baseProcessRepository;
            _checkItemRepository = checkItemRepository;
            _inventoryRepository = inventoryRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// 获取工艺路线列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表（含创建人名称、状态名称、格式化时间）.</returns>
        [HttpPost("GetRoutingList")]
        public async Task<dynamic> GetRoutingList([FromBody] RoutingQuery input)
        {
            var whereLambda = LinqExpression.And<RoutingEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.RoutingCode))
            {
                whereLambda = whereLambda.And(m => m.RoutingCode.Contains(input.RoutingCode));
            }

            if (!string.IsNullOrEmpty(input.RoutingName))
            {
                whereLambda = whereLambda.And(m => m.RoutingName.Contains(input.RoutingName));
            }

            if (!string.IsNullOrEmpty(input.InventoryKeyword))
            {
                whereLambda = whereLambda.And(m => m.InventoryCode.Contains(input.InventoryKeyword)
                    || m.InventoryName.Contains(input.InventoryKeyword));
            }

            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.Status == input.Status);
            }

            var page = await _routingRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            // 反查创建人名称
            var userIds = page.list.Select(m => m.CreatorUserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var userDict = new Dictionary<string, string>();
            if (userIds.Any())
            {
                var users = await _userRepository.AsQueryable()
                    .Where(m => userIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.RealName })
                    .ToListAsync();
                userDict = users.ToDictionary(m => m.Id, m => m.RealName ?? "");
            }

            var dtos = page.list.Select(m => new RoutingListOutput
            {
                Id = m.Id,
                RoutingCode = m.RoutingCode,
                RoutingName = m.RoutingName,
                InventoryId = m.InventoryId,
                InventoryCode = m.InventoryCode,
                InventoryName = m.InventoryName,
                ProcessCount = m.ProcessCount,
                TotalHours = m.TotalHours,
                Version = m.Version,
                Status = m.Status,
                StatusName = m.Status == 1 ? "生效" : (m.Status == 2 ? "停用" : "草稿"),
                Remark = m.Remark,
                CreatorUserId = m.CreatorUserId,
                CreatorUserName = userDict.TryGetValue(m.CreatorUserId ?? "", out var name) ? name : "",
                CreatorTime = m.CreatorTime,
                CreatorTimeText = m.CreatorTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
            }).ToList();

            return new SqlSugarPagedList<RoutingListOutput>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 获取工艺路线详情（含工序顺序明细与检查项目）.
        /// </summary>
        /// <param name="id">路线主键.</param>
        /// <returns>路线详情.</returns>
        [HttpGet("GetRoutingById")]
        public async Task<dynamic> GetRoutingById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _routingRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var output = entity.Adapt<RoutingDetailOutput>();

            // 工序顺序明细
            var details = await _routingProcessRepository.AsQueryable()
                .Where(m => m.RoutingId == id && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .ToListAsync();

            var processIds = details.Select(m => m.ProcessId).Distinct().ToList();
            var checkItems = new List<ProcessCheckItemEntity>();
            if (processIds.Any())
            {
                checkItems = await _checkItemRepository.AsQueryable()
                    .Where(m => processIds.Contains(m.ProcessId) && m.DeleteMark == null)
                    .OrderBy(m => m.SeqNo, OrderByType.Asc)
                    .ToListAsync();
            }

            output.Processes = details.Select(m =>
            {
                var item = m.Adapt<RoutingProcessOutput>();
                var items = checkItems.Where(c => c.ProcessId == m.ProcessId).ToList();
                item.CheckItems = string.Join(" / ", items.Select(c => c.CheckItem));
                item.CheckStandards = string.Join(" / ", items.Select(c => c.CheckStandard).Where(c => !c.IsNullOrEmpty()));
                return item;
            }).ToList();

            return output;
        }

        // ──────────────────────────────────────────────────────────
        // CruRouting = Create + Update（新增/编辑合一接口）
        // 判断逻辑：input.Id 有值 = 编辑；input.Id 为空 = 新增
        // ──────────────────────────────────────────────────────────
        /// <summary>
        /// 新增/编辑工艺路线（Cru = Create + Update 合一接口）.
        /// 判断逻辑：input.Id 有值 = 编辑；input.Id 为空 = 新增.
        /// 工序顺序明细整体覆盖保存，自动汇总工序数与总标准工时.
        /// </summary>
        /// <param name="input">路线信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruRouting")]
        public async Task CruRouting([FromBody] RoutingCruInput input)
        {
            if (input.RoutingCode.IsNullOrEmpty() || input.RoutingName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "路线编号和路线名称不能为空");
            }

            if (input.Processes == null)
            {
                input.Processes = new List<RoutingProcessItem>();
            }

            // 工序去重校验
            var repeatProcess = input.Processes.GroupBy(m => m.ProcessId).Any(g => g.Count() > 1);
            if (repeatProcess)
            {
                throw Oops.Oh(ErrorCode.COM1018, "工序明细存在重复工序，请检查");
            }

            // 汇总：工序数与总标准工时
            var processCount = input.Processes.Count;
            var totalHours = input.Processes.Sum(m => m.StandardHours ?? 0);

            // 存货信息回填
            InventoryMasterEntity inventory = null;
            if (!input.InventoryId.IsNullOrEmpty())
            {
                inventory = await _inventoryRepository.GetFirstAsync(m => m.Id == input.InventoryId && m.DeleteMark == null);
            }

            string routingId;
            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _routingRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                // 路线编号唯一性校验（排除自身）
                if (await _routingRepository.IsAnyAsync(p => p.RoutingCode == input.RoutingCode && p.Id != input.Id && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                existing = input.Adapt(existing);
                existing.InventoryCode = inventory?.InventoryCode;
                existing.InventoryName = inventory?.InventoryName;
                existing.ProcessCount = processCount;
                existing.TotalHours = totalHours;
                isOk = await _routingRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                if (!(isOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1001);
                }

                routingId = existing.Id;
            }
            else
            {
                // 路线编号唯一性校验
                if (await _routingRepository.IsAnyAsync(p => p.RoutingCode == input.RoutingCode && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                var entity = input.Adapt<RoutingEntity>();
                entity.InventoryCode = inventory?.InventoryCode;
                entity.InventoryName = inventory?.InventoryName;
                entity.ProcessCount = processCount;
                entity.TotalHours = totalHours;
                entity.Status = input.Status ?? 3;
                isOk = await _routingRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(isOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1000);
                }

                routingId = entity.Id;
            }

            // 工序顺序明细整体覆盖保存
            await SaveRoutingProcesses(routingId, input.Processes);
        }

        /// <summary>
        /// 删除工艺路线（级联软删除工序明细）.
        /// </summary>
        /// <param name="id">路线主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteRouting")]
        public async Task DeleteRouting(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _routingRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _routingRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            // 级联软删除工序明细
            var details = await _routingProcessRepository.AsQueryable()
                .Where(m => m.RoutingId == id && m.DeleteMark == null).ToListAsync();
            if (details.Any())
            {
                await _routingProcessRepository.AsSugarClient().Updateable(details)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 复制工艺路线（副本编号自动追加"-COPY"，工序明细一并复制）.
        /// </summary>
        /// <param name="id">源路线主键.</param>
        /// <returns>无.</returns>
        [HttpGet("CopyRouting")]
        public async Task CopyRouting(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var source = await _routingRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (source == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 生成副本编号（-COPY / -COPY2 / -COPY3 ...）
            var newCode = source.RoutingCode + "-COPY";
            var index = 1;
            while (await _routingRepository.IsAnyAsync(m => m.RoutingCode == newCode && m.DeleteMark == null))
            {
                index++;
                newCode = source.RoutingCode + "-COPY" + index;
            }

            var entity = source.Adapt<RoutingEntity>();
            entity.Id = null;
            entity.RoutingCode = newCode;
            entity.RoutingName = source.RoutingName + "（副本）";
            entity.Status = 3; // 副本默认草稿状态
            var isOk = await _routingRepository.AsSugarClient().Insertable(entity)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 复制工序明细
            var details = await _routingProcessRepository.AsQueryable()
                .Where(m => m.RoutingId == id && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .ToListAsync();
            if (details.Any())
            {
                var newDetails = details.Select(m =>
                {
                    var item = m.Adapt<RoutingProcessEntity>();
                    item.Id = null;
                    item.RoutingId = entity.Id;
                    return item;
                }).ToList();
                await _routingProcessRepository.AsSugarClient().Insertable(newDetails)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 批量生效/停用工艺路线.
        /// </summary>
        /// <param name="input">批量状态变更入参.</param>
        /// <returns>无.</returns>
        [HttpPost("BatchChangeRoutingStatus")]
        public async Task BatchChangeRoutingStatus([FromBody] RoutingBatchStatusInput input)
        {
            if (input.Ids == null || !input.Ids.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请选择需要操作的工艺路线");
            }

            var entities = await _routingRepository.AsQueryable()
                .Where(m => input.Ids.Contains(m.Id) && m.DeleteMark == null).ToListAsync();
            if (!entities.Any())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            foreach (var entity in entities)
            {
                entity.Status = input.Status;
            }

            int isOk = await _routingRepository.AsSugarClient().Updateable(entities)
                .CallEntityMethod(m => m.LastModify()).UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1003);
            }
        }

        /// <summary>
        /// 工艺路线下拉（供排产选择，仅生效路线）.
        /// </summary>
        /// <param name="keyword">编号/名称关键字.</param>
        /// <returns>路线选择项.</returns>
        [HttpGet("GetRoutingSelected")]
        public async Task<dynamic> GetRoutingSelected(string keyword)
        {
            var query = _routingRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.Status == 1);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(m => m.RoutingCode.Contains(keyword)
                    || m.RoutingName.Contains(keyword)
                    || m.InventoryName.Contains(keyword));
            }

            return await query
                .OrderBy(m => m.CreatorTime, OrderByType.Desc)
                .Take(50)
                .Select(m => new RoutingSelectedOutput
                {
                    id = m.Id,
                    code = m.RoutingCode,
                    name = m.RoutingName,
                    inventoryId = m.InventoryId,
                    inventoryName = m.InventoryName,
                    version = m.Version,
                    processCount = m.ProcessCount
                })
                .ToListAsync();
        }

        /// <summary>
        /// 保存工艺路线的工序顺序明细（整体覆盖，顺序按数组下标重排）.
        /// </summary>
        /// <param name="routingId">路线主键.</param>
        /// <param name="processes">工序明细行.</param>
        /// <returns>无.</returns>
        private async Task SaveRoutingProcesses(string routingId, List<RoutingProcessItem> processes)
        {
            // 旧明细整体软删除
            var oldItems = await _routingProcessRepository.AsQueryable()
                .Where(m => m.RoutingId == routingId && m.DeleteMark == null).ToListAsync();
            if (oldItems.Any())
            {
                await _routingProcessRepository.AsSugarClient().Updateable(oldItems)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            if (processes == null || !processes.Any())
            {
                return;
            }

            // 回填工序编码/名称
            var processIds = processes.Select(m => m.ProcessId).Distinct().ToList();
            var baseProcesses = await _baseProcessRepository.AsQueryable()
                .Where(m => processIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync();

            var newItems = new List<RoutingProcessEntity>();
            var seqNo = 1;
            foreach (var item in processes)
            {
                var baseProcess = baseProcesses.FirstOrDefault(m => m.Id == item.ProcessId);
                newItems.Add(new RoutingProcessEntity
                {
                    RoutingId = routingId,
                    ProcessId = item.ProcessId,
                    ProcessCode = baseProcess?.ProcessCode,
                    ProcessName = baseProcess?.ProcessName,
                    SeqNo = seqNo++,
                    StandardHours = item.StandardHours,
                    Requirement = item.Requirement,
                    IsCheckProcess = item.IsCheckProcess ?? 0
                });
            }

            await _routingProcessRepository.AsSugarClient().Insertable(newItems)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
    }
}
