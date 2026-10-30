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
    /// 出库服务.
    /// PDA销售出库/其他出库确认（先进先出自动锁定最早入库批次库位）；
    /// 销售出库勾稽：Σ销售/其他出库 ≤ Σ报工良品 ≤ 流程卡定量；
    /// 出库扣减跨多批次时出库单明细按批次多行记录；
    /// 「材料出库」由PDA领料记录自动生成关联（picking_id），本服务不重复录入；出库单管理.
    /// </summary>
    [ApiDescriptionSettings(Tag = "Outbound", Name = "", Order = 216)]
    [Route("api/[controller]")]
    public class OutboundService : IOutboundService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 出库记录仓储.
        /// </summary>
        private readonly ISqlSugarRepository<OutStockEntity> _outRecordRepository;

        /// <summary>
        /// 出库单主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<OutOrderEntity> _outOrderRepository;

        /// <summary>
        /// 出库单明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<OutOrderDetailEntity> _outOrderDetailRepository;

        /// <summary>
        /// 存货主档仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryMasterEntity> _inventoryRepository;

        /// <summary>
        /// 流程卡仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardEntity> _flowCardRepository;

        /// <summary>
        /// 报工记录仓储.
        /// </summary>
        private readonly ISqlSugarRepository<WorkReportEntity> _workReportRepository;

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
        /// 库存流水仓储.
        /// </summary>
        private readonly ISqlSugarRepository<StockFlowEntity> _flowRepository;

        /// <summary>
        /// 库存核心服务（内部出库扣减方法）.
        /// </summary>
        private readonly IStockService _stockService;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 用友同步服务（出库审核后自动推送现有量，模拟）.
        /// </summary>
        private readonly IYonyouSyncService _yonyouSyncService;

        /// <summary>
        /// 初始化一个<see cref="OutboundService"/>类型的新实例.
        /// </summary>
        public OutboundService(
            ISqlSugarRepository<OutStockEntity> outRecordRepository,
            ISqlSugarRepository<OutOrderEntity> outOrderRepository,
            ISqlSugarRepository<OutOrderDetailEntity> outOrderDetailRepository,
            ISqlSugarRepository<InventoryMasterEntity> inventoryRepository,
            ISqlSugarRepository<FlowCardEntity> flowCardRepository,
            ISqlSugarRepository<WorkReportEntity> workReportRepository,
            ISqlSugarRepository<WarehouseEntity> warehouseRepository,
            ISqlSugarRepository<WarehouseLocationEntity> locationRepository,
            ISqlSugarRepository<InventoryStockEntity> stockRepository,
            ISqlSugarRepository<StockFlowEntity> flowRepository,
            IStockService stockService,
            IUserManager userManager,
            IYonyouSyncService yonyouSyncService)
        {
            _outRecordRepository = outRecordRepository;
            _outOrderRepository = outOrderRepository;
            _outOrderDetailRepository = outOrderDetailRepository;
            _inventoryRepository = inventoryRepository;
            _flowCardRepository = flowCardRepository;
            _workReportRepository = workReportRepository;
            _warehouseRepository = warehouseRepository;
            _locationRepository = locationRepository;
            _stockRepository = stockRepository;
            _flowRepository = flowRepository;
            _stockService = stockService;
            _userManager = userManager;
            _yonyouSyncService = yonyouSyncService;
        }

        /// <summary>
        /// 出库记录列表（按单号/类型/流程卡/日期筛选）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetOutRecordList")]
        public async Task<dynamic> GetOutRecordList([FromBody] OutRecordQuery input)
        {
            var whereLambda = LinqExpression.And<OutStockEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!input.OutNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.OutNo.Contains(input.OutNo));
            }

            if (input.OutType != -1)
            {
                whereLambda = whereLambda.And(m => m.OutType == input.OutType);
            }

            if (!input.FlowCardNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.FlowCardNo.Contains(input.FlowCardNo));
            }

            if (input.StartTime != null)
            {
                whereLambda = whereLambda.And(m => m.OutTime >= input.StartTime);
            }

            if (input.EndTime != null)
            {
                var endTime = input.EndTime.Value.Date.AddDays(1);
                whereLambda = whereLambda.And(m => m.OutTime < endTime);
            }

            var page = await _outRecordRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.OutTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            var dtos = page.list.Select(m => new OutRecordListDto
            {
                Id = m.Id,
                OutNo = m.OutNo,
                OutType = m.OutType,
                OrderNo = m.OrderNo,
                FlowCardNo = m.FlowCardNo,
                InventoryCode = m.InventoryCode,
                InventoryName = m.InventoryName,
                Qty = m.Qty,
                Unit = m.Unit,
                BatchNo = m.BatchNo,
                WarehouseName = m.WarehouseName,
                LocationCode = m.LocationCode,
                FifoCheck = m.FifoCheck,
                OperatorName = m.OperatorName,
                OutTime = m.OutTime,
                SyncStatus = m.SyncStatus
            }).ToList();

            return new SqlSugarPagedList<OutRecordListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 出库记录详情（含存货规格与扣减批次明细）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>详情.</returns>
        [HttpGet("GetOutRecordDetail")]
        public async Task<dynamic> GetOutRecordDetail(string id)
        {
            var entity = await _outRecordRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var inventory = await _inventoryRepository.GetFirstAsync(m => m.Id == entity.InventoryId && m.DeleteMark == null);

            // 扣减批次明细（库存流水中同业务单号的出库扣减）
            var flows = await _flowRepository.AsQueryable()
                .Where(m => m.BizId == entity.Id && m.Direction == 2 && m.DeleteMark == null)
                .ToListAsync();

            return new
            {
                entity.Id,
                entity.OutNo,
                entity.OutType,
                entity.OrderId,
                entity.OrderNo,
                entity.FlowCardId,
                entity.FlowCardNo,
                entity.PickingId,
                entity.InventoryId,
                entity.InventoryCode,
                entity.InventoryName,
                Spec = inventory?.Spec,
                entity.Qty,
                entity.Unit,
                entity.WarehouseId,
                entity.WarehouseName,
                entity.LocationId,
                entity.LocationCode,
                entity.BatchNo,
                entity.FifoCheck,
                entity.OperatorName,
                entity.OutTime,
                entity.Status,
                entity.SyncStatus,
                entity.Remark,
                DeductItems = flows.Select(m => new OutDeductItemDto
                {
                    BatchNo = m.BatchNo,
                    LocationCode = m.LocationCode,
                    Qty = m.Qty ?? 0,
                    BeforeQty = m.BeforeQty ?? 0,
                    AfterQty = m.AfterQty ?? 0
                }).ToList()
            };
        }

        /// <summary>
        /// 出库记录删除（软删；已出库记录按扣减流水逐批次回滚库存）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteOutRecord")]
        public async Task DeleteOutRecord(string id)
        {
            var entity = await _outRecordRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 已出库：按扣减流水逐批次回滚
            if (entity.Status == 2)
            {
                var flows = await _flowRepository.AsQueryable()
                    .Where(m => m.BizId == entity.Id && m.Direction == 2 && m.DeleteMark == null)
                    .ToListAsync();

                foreach (var flow in flows)
                {
                    var query = _stockRepository.AsQueryable()
                        .Where(m => m.InventoryId == flow.InventoryId && m.BatchNo == flow.BatchNo && m.DeleteMark == null);
                    query = query.WhereIF(!flow.WarehouseName.IsNullOrEmpty(), m => m.WarehouseName == flow.WarehouseName);
                    query = query.WhereIF(!flow.LocationCode.IsNullOrEmpty(), m => m.LocationCode == flow.LocationCode);
                    var stock = await query.FirstAsync();
                    if (stock == null)
                    {
                        throw Oops.Oh(ErrorCode.COM1018, $"批次 {flow.BatchNo} 对应台账行缺失，无法回滚出库记录");
                    }

                    var qty = flow.Qty ?? 0;
                    var beforeQty = stock.StockQty ?? 0;
                    stock.StockQty = beforeQty + qty;
                    await _stockRepository.AsSugarClient().Updateable(stock)
                        .CallEntityMethod(m => m.LastModify())
                        .UpdateColumns(it => new { it.StockQty, it.LastModifyTime, it.LastModifyUserId })
                        .ExecuteCommandAsync();

                    // 红字流水（冲销）
                    await _flowRepository.AsSugarClient().Insertable(new StockFlowEntity
                    {
                        InventoryId = flow.InventoryId,
                        InventoryCode = flow.InventoryCode,
                        InventoryName = flow.InventoryName,
                        WarehouseName = flow.WarehouseName,
                        LocationCode = flow.LocationCode,
                        BatchNo = flow.BatchNo,
                        BizType = flow.BizType,
                        Direction = 1,
                        Qty = qty,
                        BeforeQty = beforeQty,
                        AfterQty = beforeQty + qty,
                        BizId = entity.Id,
                        BizNo = entity.OutNo,
                        OrderNo = flow.OrderNo,
                        FlowCardNo = flow.FlowCardNo,
                        OperatorName = _userManager.RealName,
                        OperateTime = DateTime.Now,
                        Remark = "出库记录删除冲销"
                    }).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
            }

            int isOk = await _outRecordRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete())
                .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                .ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        #region PDA接口

        /// <summary>
        /// PDA出库确认（销售出库/其他出库；先进先出自动锁定最早批次库位；
        /// 销售出库勾稽：已出+本次 ≤ 报工良品 ≤ 流程卡定量；多批次扣减生成多行出库单明细）.
        /// </summary>
        /// <param name="input">出库确认入参.</param>
        /// <returns>出库结果.</returns>
        [HttpPost("pad/OutConfirm")]
        public async Task<dynamic> OutConfirm([FromBody] OutConfirmInput input)
        {
            if (input.Qty <= 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, "出库数量必须大于0");
            }

            if (input.OutType != 1 && input.OutType != 2)
            {
                throw Oops.Oh(ErrorCode.COM1018, "出库类型仅支持销售出库或其他出库（材料出库由领料自动生成）");
            }

            // 存货档案
            var inventory = !input.InventoryId.IsNullOrEmpty()
                ? await _inventoryRepository.GetFirstAsync(m => m.Id == input.InventoryId && m.DeleteMark == null)
                : await _inventoryRepository.GetFirstAsync(m => m.InventoryCode == input.InventoryCode && m.DeleteMark == null);
            if (inventory == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "存货档案不存在，请核对物料编码");
            }

            input.InventoryId = inventory.Id;
            input.InventoryCode = inventory.InventoryCode;
            input.InventoryName = input.InventoryName.IsNullOrEmpty() ? inventory.InventoryName : input.InventoryName;
            input.Unit = input.Unit.IsNullOrEmpty() ? inventory.Unit : input.Unit;

            // 流程卡（扫码带出；销售出库勾稽校验用）
            FlowCardEntity card = null;
            string orderNoRef = null;
            if (!input.FlowCardId.IsNullOrEmpty())
            {
                card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
                if (card == null)
                {
                    throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在或已作废");
                }

                orderNoRef = card.OrderNo;
            }

            // 勾稽校验（销售/其他出库：已出+本次 ≤ Σ报工良品 ≤ 流程卡定量）
            if (card != null)
            {
                var outQtySum = (await _outRecordRepository.AsQueryable()
                    .Where(m => m.FlowCardId == card.Id && m.Status == 2 && m.DeleteMark == null
                        && (m.OutType == 1 || m.OutType == 2))
                    .ToListAsync()).Sum(m => m.Qty ?? 0);

                var cardQty = card.Qty ?? 0;
                if (outQtySum + input.Qty > cardQty)
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"勾稽校验不通过：出库累计 {outQtySum + input.Qty} 不得超过流程卡定量 {cardQty}");
                }

                var goodQty = (await _workReportRepository.AsQueryable()
                    .Where(m => m.FlowCardId == card.Id && m.Status == 1 && m.DeleteMark == null)
                    .ToListAsync()).Sum(m => m.GoodQty ?? 0);
                if (outQtySum + input.Qty > goodQty)
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"勾稽校验不通过：出库累计 {outQtySum + input.Qty} 超过报工良品总数 {goodQty}");
                }
            }

            // 仓库（可空按存货所在仓库自动定位）
            WarehouseEntity warehouse = null;
            if (!input.WarehouseId.IsNullOrEmpty())
            {
                warehouse = await _warehouseRepository.GetFirstAsync(m => m.Id == input.WarehouseId && m.DeleteMark == null);
                if (warehouse == null)
                {
                    throw Oops.Oh(ErrorCode.COM1018, "仓库不存在");
                }
            }

            var now = DateTime.Now;
            var outNo = await BuildOutNo();
            var record = new OutStockEntity
            {
                OutNo = outNo,
                OutType = input.OutType,
                OrderId = card?.OrderId,
                OrderNo = orderNoRef,
                FlowCardId = input.FlowCardId,
                FlowCardNo = card?.CardNo,
                InventoryId = input.InventoryId,
                InventoryCode = input.InventoryCode,
                InventoryName = input.InventoryName,
                Qty = input.Qty,
                Unit = input.Unit,
                WarehouseId = warehouse?.Id,
                WarehouseName = warehouse?.RepoName,
                FifoCheck = 1,
                OperatorName = _userManager.RealName,
                OutTime = now,
                Status = 1,
                SyncStatus = 1,
                Remark = input.Remark
            };

            var insertOk = await _outRecordRepository.AsSugarClient().Insertable(record)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 先进先出扣减（可用量不足抛错拦截；写库存流水）
            var deductItems = await _stockService.StockOutAsync(
                input.InventoryId,
                input.WarehouseId,
                input.Qty,
                input.OutType == 1 ? 4 : 5,
                record.Id,
                outNo,
                orderNoRef,
                card?.CardNo,
                _userManager.RealName,
                input.Remark);

            // 主记录回填最早批次库位（先进先出锁定结果）
            var first = deductItems.First();
            var firstLocation = await _locationRepository.GetFirstAsync(m => m.LocationCode == first.LocationCode
                && m.DeleteMark == null);
            await _outRecordRepository.AsSugarClient().Updateable<OutStockEntity>()
                .SetColumns(it => new OutStockEntity
                {
                    Status = 2,
                    BatchNo = first.BatchNo,
                    LocationCode = first.LocationCode,
                    LocationId = firstLocation != null ? firstLocation.Id : null,
                    OutTime = DateTime.Now
                })
                .Where(it => it.Id == record.Id)
                .ExecuteCommandAsync();

            var totalBefore = deductItems.Sum(m => m.BeforeQty);
            var totalAfter = deductItems.Sum(m => m.AfterQty);
            record.BatchNo = first.BatchNo;
            record.LocationCode = first.LocationCode;

            // 自动生成出库单（多批次扣减 → 明细多行）
            await CreateOutOrder(record, input, inventory, deductItems);

            // 审核后自动推送现有量变更（模拟）
            await _yonyouSyncService.WriteAutoPushLogAsync(input.OutType == 1 ? "销售出库单" : "其他出库单", outNo, $"库位 {string.Join("、", deductItems.Select(m => m.LocationCode).Distinct())} 出库推送");

            return new OutConfirmOutput
            {
                OutNo = outNo,
                FifoPass = 1,
                DeductItems = deductItems,
                Message = $"出库成功，先进先出已锁定最早批次库位（{deductItems.Count} 个批次，库位 {string.Join("、", deductItems.Select(m => m.LocationCode).Distinct())}）"
            };
        }

        #endregion

        /// <summary>
        /// 出库单管理列表（分销售出库/材料出库/其他出库）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetOutOrderList")]
        public async Task<dynamic> GetOutOrderList([FromBody] OutOrderQuery input)
        {
            var whereLambda = LinqExpression.And<OutOrderEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!input.OrderNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.OrderNo.Contains(input.OrderNo));
            }

            if (input.OutType != -1)
            {
                whereLambda = whereLambda.And(m => m.OutType == input.OutType);
            }

            if (input.StartTime != null)
            {
                whereLambda = whereLambda.And(m => m.CreatorTime >= input.StartTime);
            }

            if (input.EndTime != null)
            {
                var endTime = input.EndTime.Value.Date.AddDays(1);
                whereLambda = whereLambda.And(m => m.CreatorTime < endTime);
            }

            return await _outOrderRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 出库单明细（单据头+明细）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>详情.</returns>
        [HttpGet("GetOutOrderDetail")]
        public async Task<dynamic> GetOutOrderDetail(string id)
        {
            var order = await _outOrderRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (order == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var details = await _outOrderDetailRepository.AsQueryable()
                .Where(m => m.OrderId == id && m.DeleteMark == null)
                .ToListAsync();

            return new OutOrderDetailOutput
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                OutType = order.OutType,
                Customer = order.Customer,
                OrderNoRef = order.OrderNoRef,
                FlowCardNo = order.FlowCardNo,
                WarehouseName = order.WarehouseName,
                Status = order.Status,
                AuditUser = order.AuditUser,
                AuditTime = order.AuditTime,
                Remark = order.Remark,
                Details = details.Select(m => new OutOrderDetailItemDto
                {
                    InventoryCode = m.InventoryCode,
                    InventoryName = m.InventoryName,
                    Spec = m.Spec,
                    Unit = m.Unit,
                    Qty = m.Qty,
                    BatchNo = m.BatchNo,
                    LocationCode = m.LocationCode,
                    Remark = m.Remark
                }).ToList()
            };
        }

        /// <summary>
        /// 生成出库单号（CK+yyyyMMdd+3位序号）.
        /// </summary>
        /// <returns>出库单号.</returns>
        private async Task<string> BuildOutNo()
        {
            var prefix = "CK" + DateTime.Now.ToString("yyyyMMdd");
            var seq = await _outRecordRepository.AsQueryable()
                .Where(m => m.OutNo.StartsWith(prefix)).CountAsync() + 1;
            var outNo = prefix + seq.ToString("D3");
            while (await _outRecordRepository.IsAnyAsync(m => m.OutNo == outNo))
            {
                seq++;
                outNo = prefix + seq.ToString("D3");
            }

            return outNo;
        }

        /// <summary>
        /// 自动生成出库单（单据头+明细；多批次扣减每批次一行；审核后模拟推送用友）.
        /// </summary>
        /// <param name="record">出库记录.</param>
        /// <param name="input">出库确认入参.</param>
        /// <param name="inventory">存货档案.</param>
        /// <param name="deductItems">扣减明细.</param>
        /// <returns>无.</returns>
        private async Task CreateOutOrder(OutStockEntity record, OutConfirmInput input, InventoryMasterEntity inventory, List<OutDeductItemDto> deductItems)
        {
            var orderNo = await BuildOutOrderNo();
            var order = new OutOrderEntity
            {
                OrderNo = orderNo,
                OutType = record.OutType,
                Customer = input.Customer,
                OrderId = record.OrderId,
                OrderNoRef = record.OrderNo,
                FlowCardNo = record.FlowCardNo,
                WarehouseId = record.WarehouseId,
                WarehouseName = record.WarehouseName,
                Status = 3,
                AuditUser = _userManager.RealName,
                AuditTime = DateTime.Now,
                SyncStatus = 1,
                Remark = input.Remark
            };

            var insertOk = await _outOrderRepository.AsSugarClient().Insertable(order)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            foreach (var item in deductItems)
            {
                await _outOrderDetailRepository.AsSugarClient().Insertable(new OutOrderDetailEntity
                {
                    OrderId = order.Id,
                    InventoryId = record.InventoryId,
                    InventoryCode = record.InventoryCode,
                    InventoryName = record.InventoryName,
                    Spec = inventory.Spec,
                    Unit = record.Unit,
                    Qty = item.Qty,
                    BatchNo = item.BatchNo,
                    LocationCode = item.LocationCode,
                    Remark = record.Remark
                }).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 生成出库单管理单号（CK+yyyyMMdd+3位序号，sx_out_order 独立计数）.
        /// </summary>
        /// <returns>出库单号.</returns>
        private async Task<string> BuildOutOrderNo()
        {
            var prefix = "CK" + DateTime.Now.ToString("yyyyMMdd");
            var seq = await _outOrderRepository.AsQueryable()
                .Where(m => m.OrderNo.StartsWith(prefix)).CountAsync() + 1;
            var orderNo = prefix + seq.ToString("D3");
            while (await _outOrderRepository.IsAnyAsync(m => m.OrderNo == orderNo))
            {
                seq++;
                orderNo = prefix + seq.ToString("D3");
            }

            return orderNo;
        }
    }
}
