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
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 库存台账（核心服务）.
    /// 库存=MES实时库存：可用量=在库-占用；库存状态=低于安全库存时预警；
    /// 出库先进先出（FIFO）按 in_time 排序自动锁定最早批次库位；
    /// 入库统一「批次建档 + 货位归位」（同存货多批次独立建档）；出库扣减超量拦截；
    /// 提供内部服务方法（FifoLock/StockInAsync/StockOutAsync/占用释放）供其余服务注入调用.
    /// </summary>
    [ApiDescriptionSettings(Tag = "Stock", Name = "", Order = 213)]
    [Route("api/[controller]")]
    public class StockService : IStockService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 库存台账仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryStockEntity> _stockRepository;

        /// <summary>
        /// 库存流水仓储.
        /// </summary>
        private readonly ISqlSugarRepository<StockFlowEntity> _flowRepository;

        /// <summary>
        /// 存货主档仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryMasterEntity> _inventoryRepository;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="StockService"/>类型的新实例.
        /// </summary>
        public StockService(
            ISqlSugarRepository<InventoryStockEntity> stockRepository,
            ISqlSugarRepository<StockFlowEntity> flowRepository,
            ISqlSugarRepository<InventoryMasterEntity> inventoryRepository,
            IUserManager userManager)
        {
            _stockRepository = stockRepository;
            _flowRepository = flowRepository;
            _inventoryRepository = inventoryRepository;
            _userManager = userManager;
        }

        /// <summary>
        /// 库存台账列表（MES实时库存，按仓库/物料/库存状态筛选）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetStockList")]
        public async Task<dynamic> GetStockList([FromBody] StockQuery input)
        {
            var whereLambda = LinqExpression.And<InventoryStockEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!input.WarehouseId.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.WarehouseId == input.WarehouseId);
            }

            if (!input.InventoryKeyword.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.InventoryCode.Contains(input.InventoryKeyword)
                    || m.InventoryName.Contains(input.InventoryKeyword));
            }

            // 库存状态：2低于安全库存（安全库存>0 且 可用量<安全库存）；1正常
            if (input.StockStatus == 2)
            {
                whereLambda = whereLambda.And(m => m.SafeQty > 0
                    && ((m.StockQty ?? 0) - (m.OccupiedQty ?? 0)) < m.SafeQty);
            }
            else if (input.StockStatus == 1)
            {
                whereLambda = whereLambda.And(m => (m.SafeQty ?? 0) <= 0
                    || ((m.StockQty ?? 0) - (m.OccupiedQty ?? 0)) >= m.SafeQty);
            }

            var page = await _stockRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.InventoryCode, OrderByType.Asc)
                .OrderBy(it => it.InTime, OrderByType.Asc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            var dtos = page.list.Select(m =>
            {
                var available = (m.StockQty ?? 0) - (m.OccupiedQty ?? 0);
                return new StockListDto
                {
                    Id = m.Id,
                    InventoryCode = m.InventoryCode,
                    InventoryName = m.InventoryName,
                    Spec = m.Spec,
                    Unit = m.Unit,
                    WarehouseName = m.WarehouseName,
                    LocationCode = m.LocationCode,
                    BatchNo = m.BatchNo,
                    StockQty = m.StockQty,
                    OccupiedQty = m.OccupiedQty,
                    AvailableQty = available,
                    SafeQty = m.SafeQty,
                    StockStatus = (m.SafeQty > 0 && available < m.SafeQty) ? 2 : 1,
                    InTime = m.InTime
                };
            }).ToList();

            return new SqlSugarPagedList<StockListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 库存查询统计卡片（在库存货种类/库存总量/可用库存合计/低于安全库存）.
        /// </summary>
        /// <returns>统计结果.</returns>
        [HttpGet("GetStockStatistics")]
        public async Task<dynamic> GetStockStatistics()
        {
            var rows = await _stockRepository.AsQueryable()
                .Where(m => m.DeleteMark == null).ToListAsync();

            return new StockStatisticsOutput
            {
                InventoryKindCount = rows.Select(m => m.InventoryId).Distinct().Count(),
                TotalQty = rows.Sum(m => m.StockQty ?? 0),
                AvailableQtyTotal = rows.Sum(m => Math.Max(0, (m.StockQty ?? 0) - (m.OccupiedQty ?? 0))),
                BelowSafeCount = rows.Count(m => m.SafeQty > 0
                    && ((m.StockQty ?? 0) - (m.OccupiedQty ?? 0)) < m.SafeQty)
            };
        }

        /// <summary>
        /// 库存流水列表（按存货/业务类型/方向/日期筛选）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetStockFlowList")]
        public async Task<dynamic> GetStockFlowList([FromBody] StockFlowQuery input)
        {
            var whereLambda = LinqExpression.And<StockFlowEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!input.InventoryCode.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.InventoryCode.Contains(input.InventoryCode));
            }

            if (input.BizType != -1)
            {
                whereLambda = whereLambda.And(m => m.BizType == input.BizType);
            }

            if (input.Direction != -1)
            {
                whereLambda = whereLambda.And(m => m.Direction == input.Direction);
            }

            if (!input.BizNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.BizNo.Contains(input.BizNo));
            }

            if (input.StartTime != null)
            {
                whereLambda = whereLambda.And(m => m.OperateTime >= input.StartTime);
            }

            if (input.EndTime != null)
            {
                var endTime = input.EndTime.Value.Date.AddDays(1);
                whereLambda = whereLambda.And(m => m.OperateTime < endTime);
            }

            return await _flowRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.OperateTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 先进先出锁定最早入库批次库位（同存货多批次按 in_time 升序；可用量不足返回空列表）.
        /// </summary>
        /// <param name="inventoryId">存货ID.</param>
        /// <param name="warehouseId">仓库ID（可空）. </param>
        /// <param name="qty">需锁定数量.</param>
        /// <returns>批次锁定明细（可用量不足时为空列表）.</returns>
        [NonAction]
        public async Task<List<FifoBatchLockDto>> FifoLock(string inventoryId, string warehouseId, decimal qty)
        {
            var result = new List<FifoBatchLockDto>();
            if (inventoryId.IsNullOrEmpty() || qty <= 0)
            {
                return result;
            }

            var rows = await _stockRepository.AsQueryable()
                .Where(m => m.InventoryId == inventoryId && m.DeleteMark == null && m.Status == 1)
                .WhereIF(!warehouseId.IsNullOrEmpty(), m => m.WarehouseId == warehouseId)
                .ToListAsync();

            // 先进先出：按入库时间升序（null 置后）
            rows = rows.OrderBy(m => m.InTime ?? DateTime.MaxValue)
                .ThenBy(m => m.CreatorTime ?? DateTime.MaxValue).ToList();

            var totalAvailable = rows.Sum(m => Math.Max(0, (m.StockQty ?? 0) - (m.OccupiedQty ?? 0)));
            if (totalAvailable < qty)
            {
                // 可用量不足，先进先出锁定失败
                return new List<FifoBatchLockDto>();
            }

            var remain = qty;
            foreach (var row in rows)
            {
                if (remain <= 0)
                {
                    break;
                }

                var available = Math.Max(0, (row.StockQty ?? 0) - (row.OccupiedQty ?? 0));
                if (available <= 0)
                {
                    continue;
                }

                var lockQty = Math.Min(available, remain);
                result.Add(new FifoBatchLockDto
                {
                    StockId = row.Id,
                    InventoryId = row.InventoryId,
                    InventoryCode = row.InventoryCode,
                    InventoryName = row.InventoryName,
                    Spec = row.Spec,
                    Unit = row.Unit,
                    WarehouseId = row.WarehouseId,
                    WarehouseName = row.WarehouseName,
                    LocationId = row.LocationId,
                    LocationCode = row.LocationCode,
                    BatchNo = row.BatchNo,
                    InTime = row.InTime,
                    AvailableQty = available,
                    LockQty = lockQty
                });
                remain -= lockQty;
            }

            return result;
        }

        /// <summary>
        /// 入库上架（批次建档+货位归位）：同存货+仓库+货位+批次累加，否则新建台账行；写库存流水.
        /// </summary>
        /// <param name="data">入库记录（须含存货/仓库/货位/批次信息，Id 与 InNo 用于流水关联）.</param>
        /// <param name="qty">入库数量.</param>
        /// <returns>变动前后数量.</returns>
        [NonAction]
        public async Task<StockChangeOutput> StockInAsync(StockInEntity data, decimal qty)
        {
            var now = DateTime.Now;
            var rows = await _stockRepository.AsQueryable()
                .Where(m => m.InventoryId == data.InventoryId && m.DeleteMark == null)
                .ToListAsync();

            // 批次建档 + 货位归位：同存货同仓库同货位同批次累加
            var existing = rows.FirstOrDefault(m => m.WarehouseId == data.WarehouseId
                && m.LocationId == data.LocationId
                && m.BatchNo == data.BatchNo
                && m.Status == 1);

            decimal beforeQty;
            string stockId;
            if (existing != null)
            {
                beforeQty = existing.StockQty ?? 0;
                existing.StockQty = beforeQty + qty;
                if (existing.InTime == null)
                {
                    existing.InTime = now;
                }

                await _stockRepository.AsSugarClient().Updateable(existing)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.StockQty, it.InTime, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();
                stockId = existing.Id;
            }
            else
            {
                var inventory = await _inventoryRepository.GetFirstAsync(m => m.Id == data.InventoryId && m.DeleteMark == null);
                var entity = new InventoryStockEntity
                {
                    InventoryId = data.InventoryId,
                    InventoryCode = data.InventoryCode,
                    InventoryName = data.InventoryName,
                    Spec = inventory?.Spec,
                    Unit = data.Unit ?? inventory?.Unit,
                    WarehouseId = data.WarehouseId,
                    WarehouseCode = data.WarehouseCode,
                    WarehouseName = data.WarehouseName,
                    LocationId = data.LocationId,
                    LocationCode = data.LocationCode,
                    BatchNo = data.BatchNo,
                    StockQty = qty,
                    OccupiedQty = 0,
                    SafeQty = 0,
                    InTime = now,
                    Status = 1
                };

                var insertOk = await _stockRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(insertOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1000);
                }

                beforeQty = 0;
                stockId = entity.Id;
            }

            // 库存流水（审核后自动推送用友现有量，见 YonyouSyncService 模拟）
            await _flowRepository.AsSugarClient().Insertable(new StockFlowEntity
            {
                InventoryId = data.InventoryId,
                InventoryCode = data.InventoryCode,
                InventoryName = data.InventoryName,
                WarehouseName = data.WarehouseName,
                LocationCode = data.LocationCode,
                BatchNo = data.BatchNo,
                BizType = data.InType ?? 1,
                Direction = 1,
                Qty = qty,
                BeforeQty = beforeQty,
                AfterQty = beforeQty + qty,
                BizId = data.Id,
                BizNo = data.InNo,
                FlowCardNo = data.FlowCardNo,
                OperatorName = data.OperatorName ?? _userManager.RealName,
                OperateTime = now
            }).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

            return new StockChangeOutput
            {
                StockId = stockId,
                BeforeQty = beforeQty,
                AfterQty = beforeQty + qty
            };
        }

        /// <summary>
        /// 出库扣减（先进先出按批次扣减；可用量不足抛错拦截）；写库存流水；返回扣减明细.
        /// </summary>
        /// <param name="inventoryId">存货ID.</param>
        /// <param name="warehouseId">仓库ID（可空）. </param>
        /// <param name="qty">出库数量.</param>
        /// <param name="bizType">业务类型：4销售出库 5其他出库 6领料出库.</param>
        /// <param name="bizId">关联业务单据ID.</param>
        /// <param name="bizNo">关联业务单号.</param>
        /// <param name="orderNo">关联生产订单号（可空）.</param>
        /// <param name="flowCardNo">关联流程卡号（可空）.</param>
        /// <param name="operatorName">操作人（可空取当前用户）.</param>
        /// <param name="remark">备注.</param>
        /// <returns>各批次扣减明细.</returns>
        [NonAction]
        public async Task<List<OutDeductItemDto>> StockOutAsync(string inventoryId, string warehouseId, decimal qty, int bizType, string bizId, string bizNo, string orderNo, string flowCardNo, string operatorName, string remark)
        {
            var now = DateTime.Now;
            var rows = await _stockRepository.AsQueryable()
                .Where(m => m.InventoryId == inventoryId && m.DeleteMark == null && m.Status == 1)
                .WhereIF(!warehouseId.IsNullOrEmpty(), m => m.WarehouseId == warehouseId)
                .ToListAsync();

            rows = rows.OrderBy(m => m.InTime ?? DateTime.MaxValue)
                .ThenBy(m => m.CreatorTime ?? DateTime.MaxValue).ToList();

            var totalAvailable = rows.Sum(m => Math.Max(0, (m.StockQty ?? 0) - (m.OccupiedQty ?? 0)));
            if (totalAvailable < qty)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"库存不足，可出数量 {totalAvailable}，本次出库 {qty}，已拦截");
            }

            var deductItems = new List<OutDeductItemDto>();
            var remain = qty;
            foreach (var row in rows)
            {
                if (remain <= 0)
                {
                    break;
                }

                var available = Math.Max(0, (row.StockQty ?? 0) - (row.OccupiedQty ?? 0));
                if (available <= 0)
                {
                    continue;
                }

                var deductQty = Math.Min(available, remain);
                var beforeQty = row.StockQty ?? 0;
                row.StockQty = beforeQty - deductQty;
                await _stockRepository.AsSugarClient().Updateable(row)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.StockQty, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();

                await _flowRepository.AsSugarClient().Insertable(new StockFlowEntity
                {
                    InventoryId = row.InventoryId,
                    InventoryCode = row.InventoryCode,
                    InventoryName = row.InventoryName,
                    WarehouseName = row.WarehouseName,
                    LocationCode = row.LocationCode,
                    BatchNo = row.BatchNo,
                    BizType = bizType,
                    Direction = 2,
                    Qty = deductQty,
                    BeforeQty = beforeQty,
                    AfterQty = beforeQty - deductQty,
                    BizId = bizId,
                    BizNo = bizNo,
                    OrderNo = orderNo,
                    FlowCardNo = flowCardNo,
                    OperatorName = operatorName ?? _userManager.RealName,
                    OperateTime = now,
                    Remark = remark
                }).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                deductItems.Add(new OutDeductItemDto
                {
                    BatchNo = row.BatchNo,
                    LocationCode = row.LocationCode,
                    Qty = deductQty,
                    BeforeQty = beforeQty,
                    AfterQty = beforeQty - deductQty
                });
                remain -= deductQty;
            }

            return deductItems;
        }

        /// <summary>
        /// 批次建档号生成（同存货多批次独立建档，如 20261014-001）.
        /// </summary>
        /// <param name="inventoryCode">存货编码（可空）.</param>
        /// <returns>批次号.</returns>
        [NonAction]
        public async Task<string> BuildBatchNo(string inventoryCode)
        {
            var prefix = DateTime.Now.ToString("yyyyMMdd");
            var batchNos = await _stockRepository.AsQueryable()
                .Where(m => m.BatchNo != null && m.BatchNo.StartsWith(prefix))
                .Select(m => m.BatchNo).ToListAsync();

            var seq = batchNos.Count + 1;
            var batchNo = $"{prefix}-{seq:D3}";
            while (batchNos.Contains(batchNo))
            {
                seq++;
                batchNo = $"{prefix}-{seq:D3}";
            }

            return batchNo;
        }

        /// <summary>
        /// 占用（按先进先出批次增加占用数量，可用量不足返回false）.
        /// </summary>
        /// <param name="inventoryId">存货ID.</param>
        /// <param name="qty">占用数量.</param>
        /// <returns>是否成功.</returns>
        [NonAction]
        public async Task<bool> OccupyAsync(string inventoryId, decimal qty)
        {
            var locks = await FifoLock(inventoryId, null, qty);
            if (!locks.Any())
            {
                return false;
            }

            foreach (var item in locks)
            {
                var row = await _stockRepository.GetFirstAsync(m => m.Id == item.StockId && m.DeleteMark == null);
                if (row == null)
                {
                    continue;
                }

                row.OccupiedQty = (row.OccupiedQty ?? 0) + item.LockQty;
                await _stockRepository.AsSugarClient().Updateable(row)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.OccupiedQty, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();
            }

            return true;
        }

        /// <summary>
        /// 释放占用（按最近占用批次减少占用数量）.
        /// </summary>
        /// <param name="inventoryId">存货ID.</param>
        /// <param name="qty">释放数量.</param>
        /// <returns>是否成功.</returns>
        [NonAction]
        public async Task<bool> ReleaseAsync(string inventoryId, decimal qty)
        {
            var rows = await _stockRepository.AsQueryable()
                .Where(m => m.InventoryId == inventoryId && m.DeleteMark == null && m.OccupiedQty > 0)
                .ToListAsync();

            var totalOccupied = rows.Sum(m => m.OccupiedQty ?? 0);
            if (totalOccupied < qty)
            {
                return false;
            }

            // 释放优先冲减最近批次占用
            rows = rows.OrderByDescending(m => m.InTime ?? DateTime.MaxValue).ToList();
            var remain = qty;
            foreach (var row in rows)
            {
                if (remain <= 0)
                {
                    break;
                }

                var occupied = row.OccupiedQty ?? 0;
                var releaseQty = Math.Min(occupied, remain);
                row.OccupiedQty = occupied - releaseQty;
                await _stockRepository.AsSugarClient().Updateable(row)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.OccupiedQty, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();
                remain -= releaseQty;
            }

            return true;
        }
    }
}
