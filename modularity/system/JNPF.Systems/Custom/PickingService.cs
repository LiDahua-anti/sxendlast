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
    /// 领料服务.
    /// PDA领料：①前道工序完成校验（首道工序校验来料检验完成，后续工序校验上一工序报工完成）；
    /// ②先进先出取最早批次库位；③齐套校验（按绑定子件×数量检查可领量=在库-占用）；
    /// 领料成功自动生成「领料记录」（主数据）与「材料出库记录」（picking_id 关联，同步用友）.
    /// </summary>
    [ApiDescriptionSettings(Tag = "Picking", Name = "", Order = 217)]
    [Route("api/[controller]")]
    public class PickingService : IPickingService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 领料记录仓储.
        /// </summary>
        private readonly ISqlSugarRepository<PickingRecordEntity> _pickingRepository;

        /// <summary>
        /// 出库记录仓储（材料出库）.
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
        /// 存货BOM仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryBomEntity> _bomRepository;

        /// <summary>
        /// 流程卡仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardEntity> _flowCardRepository;

        /// <summary>
        /// 流程卡工序流转仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardProcessEntity> _flowCardProcessRepository;

        /// <summary>
        /// 生产订单仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionOrderEntity> _productionOrderRepository;

        /// <summary>
        /// 来料检验仓储.
        /// </summary>
        private readonly ISqlSugarRepository<IqcEntity> _iqcRepository;

        /// <summary>
        /// 库存台账仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryStockEntity> _stockRepository;

        /// <summary>
        /// 库存核心服务（FIFO锁定/出库扣减）.
        /// </summary>
        private readonly IStockService _stockService;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 用友同步服务（领料审核后自动推送现有量，模拟）.
        /// </summary>
        private readonly IYonyouSyncService _yonyouSyncService;

        /// <summary>
        /// 初始化一个<see cref="PickingService"/>类型的新实例.
        /// </summary>
        public PickingService(
            ISqlSugarRepository<PickingRecordEntity> pickingRepository,
            ISqlSugarRepository<OutStockEntity> outRecordRepository,
            ISqlSugarRepository<OutOrderEntity> outOrderRepository,
            ISqlSugarRepository<OutOrderDetailEntity> outOrderDetailRepository,
            ISqlSugarRepository<InventoryMasterEntity> inventoryRepository,
            ISqlSugarRepository<InventoryBomEntity> bomRepository,
            ISqlSugarRepository<FlowCardEntity> flowCardRepository,
            ISqlSugarRepository<FlowCardProcessEntity> flowCardProcessRepository,
            ISqlSugarRepository<ProductionOrderEntity> productionOrderRepository,
            ISqlSugarRepository<IqcEntity> iqcRepository,
            ISqlSugarRepository<InventoryStockEntity> stockRepository,
            IStockService stockService,
            IUserManager userManager,
            IYonyouSyncService yonyouSyncService)
        {
            _pickingRepository = pickingRepository;
            _outRecordRepository = outRecordRepository;
            _outOrderRepository = outOrderRepository;
            _outOrderDetailRepository = outOrderDetailRepository;
            _inventoryRepository = inventoryRepository;
            _bomRepository = bomRepository;
            _flowCardRepository = flowCardRepository;
            _flowCardProcessRepository = flowCardProcessRepository;
            _productionOrderRepository = productionOrderRepository;
            _iqcRepository = iqcRepository;
            _stockRepository = stockRepository;
            _stockService = stockService;
            _userManager = userManager;
            _yonyouSyncService = yonyouSyncService;
        }

        /// <summary>
        /// 领料记录列表（按领料单号/订单/流程卡/日期筛选）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetPickingList")]
        public async Task<dynamic> GetPickingList([FromBody] PickingQuery input)
        {
            var whereLambda = LinqExpression.And<PickingRecordEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!input.PickingNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.PickingNo.Contains(input.PickingNo));
            }

            if (!input.OrderNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.OrderNo.Contains(input.OrderNo));
            }

            if (!input.FlowCardNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.FlowCardNo.Contains(input.FlowCardNo));
            }

            if (input.StartTime != null)
            {
                whereLambda = whereLambda.And(m => m.PickTime >= input.StartTime);
            }

            if (input.EndTime != null)
            {
                var endTime = input.EndTime.Value.Date.AddDays(1);
                whereLambda = whereLambda.And(m => m.PickTime < endTime);
            }

            var page = await _pickingRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.PickTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            var dtos = page.list.Select(m => new PickingListDto
            {
                Id = m.Id,
                PickingNo = m.PickingNo,
                OrderNo = m.OrderNo,
                FlowCardNo = m.FlowCardNo,
                ProcessName = m.ProcessName,
                InventoryCode = m.InventoryCode,
                InventoryName = m.InventoryName,
                ShouldQty = m.ShouldQty,
                ActualQty = m.ActualQty,
                LocationCode = m.LocationCode,
                BatchNo = m.BatchNo,
                PickerName = m.PickerName,
                PickTime = m.PickTime,
                Status = m.Status,
                SyncStatus = m.SyncStatus
            }).ToList();

            return new SqlSugarPagedList<PickingListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 领料记录详情（含自动生成的材料出库记录关联）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>详情.</returns>
        [HttpGet("GetPickingDetail")]
        public async Task<dynamic> GetPickingDetail(string id)
        {
            var entity = await _pickingRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 自动生成的材料出库记录（picking_id 关联）
            var outStock = !entity.OutStockId.IsNullOrEmpty()
                ? await _outRecordRepository.GetFirstAsync(m => m.Id == entity.OutStockId && m.DeleteMark == null)
                : null;

            return new PickingDetailOutput
            {
                Id = entity.Id,
                PickingNo = entity.PickingNo,
                OrderNo = entity.OrderNo,
                FlowCardNo = entity.FlowCardNo,
                ProcessName = entity.ProcessName,
                InventoryCode = entity.InventoryCode,
                InventoryName = entity.InventoryName,
                ShouldQty = entity.ShouldQty,
                ActualQty = entity.ActualQty,
                WarehouseName = entity.WarehouseName,
                LocationCode = entity.LocationCode,
                BatchNo = entity.BatchNo,
                PickerName = entity.PickerName,
                PickTime = entity.PickTime,
                Status = entity.Status,
                OutStockId = entity.OutStockId,
                OutStockNo = outStock?.OutNo,
                Remark = entity.Remark
            };
        }

        /// <summary>
        /// PDA领料前校验上下文（前道工序完成校验 + 齐套校验 + FIFO提示）.
        /// </summary>
        /// <param name="flowCardId">流程卡ID.</param>
        /// <param name="processId">工序ID（可空取首道工序）.</param>
        /// <returns>校验上下文.</returns>
        [HttpGet("GetPickContext")]
        public async Task<dynamic> GetPickContext(string flowCardId, string processId)
        {
            if (flowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请扫描流程卡");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == flowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在或已作废");
            }

            var order = await GetOrder(card);
            var processes = await GetCardProcesses(flowCardId);
            if (!processes.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "该流程卡未配置工序，无法领料");
            }

            var current = !processId.IsNullOrEmpty()
                ? processes.FirstOrDefault(m => m.ProcessId == processId)
                : processes.First();
            if (current == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "该流程卡未绑定所选工序");
            }

            var isFirst = IsFirstProcess(current, processes);
            var output = new PickContextOutput
            {
                CardNo = card.CardNo,
                OrderNo = card.OrderNo,
                ProcessName = current.ProcessName,
                IsFirstProcess = isFirst ? 1 : 0
            };

            // ①前道工序完成校验
            string prevText;
            if (isFirst)
            {
                // 首道工序：校验来料检验（IQC）完成
                output.PrevProcessName = "来料检验";
                var (pass, text) = await CheckIqcByCard(card);
                output.PrevPass = pass ? 1 : 0;
                prevText = text;
            }
            else
            {
                // 后续工序：校验上一工序报工完成
                var prev = processes
                    .Where(m => (m.SeqNo ?? int.MaxValue) < (current.SeqNo ?? int.MaxValue))
                    .OrderByDescending(m => m.SeqNo).First();
                output.PrevProcessName = prev.ProcessName;
                var pass = prev.Status == 3;
                output.PrevPass = pass ? 1 : 0;
                prevText = pass
                    ? $"已完成（{prev.CompleteQty ?? 0}/{prev.TargetQty ?? 0}）"
                    : $"进行中（{prev.CompleteQty ?? 0}/{prev.TargetQty ?? 0}），请等待上一工序报工完成";
            }

            output.PrevProcessText = prevText;

            // ③齐套校验（需用=单个用量×投产数量×(1+损耗率)；可领量=在库-占用）
            var (items, kittingPass) = await BuildKittingItems(card, order);
            output.Items = items;
            output.KittingPass = kittingPass ? 1 : 0;

            // 提示组装
            var messages = new List<string> { prevText };
            if (!kittingPass)
            {
                var lackNames = items.Where(m => m.Satisfied == 0)
                    .Select(m => $"{m.ChildInventoryName ?? m.ChildInventoryCode}(缺{m.LackQty})").ToList();
                messages.Add($"齐套缺料：{string.Join("、", lackNames)}");
            }
            else
            {
                messages.Add("齐套校验通过");
            }

            output.Message = string.Join("；", messages);
            return output;
        }

        /// <summary>
        /// PDA领料确认（校验通过后FIFO取最早批次库位扣减；自动生成领料记录+材料出库记录）.
        /// </summary>
        /// <param name="input">领料确认入参.</param>
        /// <returns>领料结果.</returns>
        [HttpPost("PdaPick")]
        public async Task<dynamic> PdaPick([FromBody] PdaPickInput input)
        {
            if (input.FlowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请扫描流程卡");
            }

            if (input.ProcessId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请选择当前工序");
            }

            if (input.Qty <= 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, "领料数量必须大于0");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在或已作废");
            }

            var order = await GetOrder(card);
            var processes = await GetCardProcesses(input.FlowCardId);
            var current = processes.FirstOrDefault(m => m.ProcessId == input.ProcessId);
            if (current == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "该流程卡未绑定所选工序");
            }

            // 存货档案
            var inventory = !input.InventoryId.IsNullOrEmpty()
                ? await _inventoryRepository.GetFirstAsync(m => m.Id == input.InventoryId && m.DeleteMark == null)
                : await _inventoryRepository.GetFirstAsync(m => m.InventoryCode == input.InventoryCode && m.DeleteMark == null);
            if (inventory == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "物料档案不存在，请核对物料编码");
            }

            input.InventoryId = inventory.Id;
            input.InventoryCode = inventory.InventoryCode;
            input.InventoryName = input.InventoryName.IsNullOrEmpty() ? inventory.InventoryName : input.InventoryName;

            // ①前道工序完成校验
            if (IsFirstProcess(current, processes))
            {
                // 首道工序：本次物料属于BOM子件时校验来料检验合格/免检
                var parent = await _inventoryRepository.GetFirstAsync(m => m.InventoryName == card.ProductName && m.DeleteMark == null);
                if (parent != null)
                {
                    var isBomChild = await _bomRepository.IsAnyAsync(m => m.ParentInventoryId == parent.Id
                        && m.ChildInventoryId == inventory.Id && m.DeleteMark == null);
                    if (isBomChild)
                    {
                        var iqcOk = await _iqcRepository.IsAnyAsync(m => m.InventoryId == inventory.Id
                            && (m.CheckResult == 1 || m.CheckResult == 3) && m.DeleteMark == null);
                        if (!iqcOk)
                        {
                            throw Oops.Oh(ErrorCode.COM1018, $"物料 {inventory.InventoryCode} 尚未完成来料检验（IQC），禁止领料");
                        }
                    }
                }
            }
            else
            {
                var prev = processes
                    .Where(m => (m.SeqNo ?? int.MaxValue) < (current.SeqNo ?? int.MaxValue))
                    .OrderByDescending(m => m.SeqNo).First();
                if (prev.Status != 3)
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"上一工序「{prev.ProcessName}」尚未完成报工（当前 {prev.CompleteQty ?? 0}/{prev.TargetQty ?? 0}），禁止领料");
                }
            }

            // ②先进先出取最早批次库位（可用量不足拦截）
            var locks = await _stockService.FifoLock(inventory.Id, input.WarehouseId, input.Qty);
            if (!locks.Any())
            {
                var stockRows = await _stockRepository.AsQueryable()
                    .Where(m => m.InventoryId == inventory.Id && m.DeleteMark == null).ToListAsync();
                var available = stockRows.Sum(m => Math.Max(0, (m.StockQty ?? 0) - (m.OccupiedQty ?? 0)));
                throw Oops.Oh(ErrorCode.COM1018, $"缺料拦截：需领 {input.Qty}，可领 {available}（在库-占用），请补充库存后再领料");
            }

            var firstLock = locks.First();

            // ③齐套校验（整体提示；本次物料不足已在FIFO环节拦截）
            var (kittingItems, kittingPass) = await BuildKittingItems(card, order);
            var kittingMsg = kittingPass
                ? "齐套校验通过"
                : $"注意：存在缺料子件（{string.Join("、", kittingItems.Where(m => m.Satisfied == 0).Select(m => m.ChildInventoryName ?? m.ChildInventoryCode).Distinct())}）";

            var pickerName = input.PickerName.IsNullOrEmpty() ? _userManager.RealName : input.PickerName;
            var now = DateTime.Now;

            // 领料记录（主数据）
            var pickingNo = await BuildPickingNo();
            var shouldQty = kittingItems.FirstOrDefault(m => m.ChildInventoryCode == inventory.InventoryCode)?.RequireQty;
            var picking = new PickingRecordEntity
            {
                PickingNo = pickingNo,
                OrderId = card.OrderId,
                OrderNo = card.OrderNo,
                FlowCardId = card.Id,
                FlowCardNo = card.CardNo,
                ProcessId = current.ProcessId,
                ProcessName = current.ProcessName,
                InventoryId = inventory.Id,
                InventoryCode = inventory.InventoryCode,
                InventoryName = inventory.InventoryName,
                ShouldQty = shouldQty,
                ActualQty = input.Qty,
                WarehouseId = firstLock.WarehouseId,
                WarehouseName = firstLock.WarehouseName,
                LocationId = firstLock.LocationId,
                LocationCode = firstLock.LocationCode,
                BatchNo = firstLock.BatchNo,
                PickerName = pickerName,
                PickTime = now,
                Status = 1,
                SyncStatus = 1,
                Remark = input.Remark
            };

            var insertOk = await _pickingRepository.AsSugarClient().Insertable(picking)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 材料出库记录（领料自动生成，不重复录入）
            var outNo = await BuildOutStockNo();
            var outRecord = new OutStockEntity
            {
                OutNo = outNo,
                OutType = 3,
                OrderId = card.OrderId,
                OrderNo = card.OrderNo,
                FlowCardId = card.Id,
                FlowCardNo = card.CardNo,
                PickingId = picking.Id,
                InventoryId = inventory.Id,
                InventoryCode = inventory.InventoryCode,
                InventoryName = inventory.InventoryName,
                Qty = input.Qty,
                Unit = inventory.Unit,
                WarehouseId = firstLock.WarehouseId,
                WarehouseName = firstLock.WarehouseName,
                LocationId = firstLock.LocationId,
                LocationCode = firstLock.LocationCode,
                BatchNo = firstLock.BatchNo,
                FifoCheck = 1,
                OperatorName = pickerName,
                OutTime = now,
                Status = 1,
                SyncStatus = 1,
                Remark = "领料自动生成"
            };

            insertOk = await _outRecordRepository.AsSugarClient().Insertable(outRecord)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 先进先出扣减（写库存流水 BizType=6领料）
            var deductItems = await _stockService.StockOutAsync(
                inventory.Id,
                input.WarehouseId,
                input.Qty,
                6,
                outRecord.Id,
                outNo,
                card.OrderNo,
                card.CardNo,
                pickerName,
                input.Remark);

            // 材料出库记录回填FIFO最早批次 + 置已出库
            var firstDeduct = deductItems.First();
            var firstLocation = await _stockRepository.AsSugarClient().Queryable<WarehouseLocationEntity>()
                .FirstAsync(m => m.LocationCode == firstDeduct.LocationCode && m.DeleteMark == null);
            await _outRecordRepository.AsSugarClient().Updateable<OutStockEntity>()
                .SetColumns(it => new OutStockEntity
                {
                    Status = 2,
                    BatchNo = firstDeduct.BatchNo,
                    LocationCode = firstDeduct.LocationCode,
                    LocationId = firstLocation != null ? firstLocation.Id : null,
                    OutTime = DateTime.Now
                })
                .Where(it => it.Id == outRecord.Id)
                .ExecuteCommandAsync();

            // 领料记录回写关联材料出库ID
            await _pickingRepository.AsSugarClient().Updateable<PickingRecordEntity>()
                .SetColumns(it => new PickingRecordEntity { OutStockId = outRecord.Id })
                .Where(it => it.Id == picking.Id)
                .ExecuteCommandAsync();

            // 材料出库单（多批次扣减 → 明细多行；审核后模拟推送用友）
            await CreateOutOrderForPicking(outRecord, inventory, deductItems, pickerName);

            // 审核后自动推送现有量变更（模拟）
            await _yonyouSyncService.WriteAutoPushLogAsync("生产领料单", pickingNo, $"材料出库 {outNo}，库位 {firstDeduct.LocationCode}");

            return new PdaPickOutput
            {
                PickingNo = pickingNo,
                OutNo = outNo,
                BatchNo = firstDeduct.BatchNo,
                LocationCode = firstDeduct.LocationCode,
                BeforeQty = firstDeduct.BeforeQty,
                AfterQty = firstDeduct.AfterQty,
                Message = $"领料成功（FIFO最早批次 {firstDeduct.BatchNo}，库位 {firstDeduct.LocationCode}）；已自动生成材料出库记录 {outNo}；{kittingMsg}"
            };
        }

        /// <summary>
        /// 获取流程卡订单（可空）.
        /// </summary>
        /// <param name="card">流程卡.</param>
        /// <returns>生产订单.</returns>
        private async Task<ProductionOrderEntity> GetOrder(FlowCardEntity card)
        {
            return !card.OrderId.IsNullOrEmpty()
                ? await _productionOrderRepository.GetFirstAsync(m => m.Id == card.OrderId && m.DeleteMark == null)
                : null;
        }

        /// <summary>
        /// 获取流程卡工序流转列表（按顺序号升序）.
        /// </summary>
        /// <param name="flowCardId">流程卡ID.</param>
        /// <returns>工序列表.</returns>
        private async Task<List<FlowCardProcessEntity>> GetCardProcesses(string flowCardId)
        {
            return await _flowCardProcessRepository.AsQueryable()
                .Where(m => m.FlowCardId == flowCardId && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .ToListAsync();
        }

        /// <summary>
        /// 是否首道工序（顺序号最小）.
        /// </summary>
        /// <param name="current">当前工序.</param>
        /// <param name="processes">工序列表.</param>
        /// <returns>是否首道.</returns>
        private static bool IsFirstProcess(FlowCardProcessEntity current, List<FlowCardProcessEntity> processes)
        {
            var minSeq = processes.Min(m => m.SeqNo ?? int.MaxValue);
            return (current.SeqNo ?? int.MaxValue) == minSeq;
        }

        /// <summary>
        /// 按流程卡产品BOM子件校验来料检验完成情况（首道工序前道校验）.
        /// </summary>
        /// <param name="card">流程卡.</param>
        /// <returns>是否通过 + 结果文本.</returns>
        private async Task<(bool Pass, string Text)> CheckIqcByCard(FlowCardEntity card)
        {
            var parent = await _inventoryRepository.GetFirstAsync(m => m.InventoryName == card.ProductName && m.DeleteMark == null);
            if (parent == null)
            {
                return (true, "未找到与产品匹配的存货档案，跳过来料检验校验");
            }

            var bomItems = await _bomRepository.AsQueryable()
                .Where(m => m.ParentInventoryId == parent.Id && m.DeleteMark == null)
                .ToListAsync();
            if (!bomItems.Any())
            {
                return (true, "无绑定子件，跳过来料检验校验");
            }

            var childIds = bomItems.Select(m => m.ChildInventoryId).Distinct().ToList();
            var iqcRows = await _iqcRepository.AsQueryable()
                .Where(m => childIds.Contains(m.InventoryId) && (m.CheckResult == 1 || m.CheckResult == 3) && m.DeleteMark == null)
                .Select(m => m.InventoryId)
                .ToListAsync();

            var missing = bomItems.Where(m => !iqcRows.Contains(m.ChildInventoryId)).ToList();
            if (missing.Any())
            {
                return (false, $"存在未完成来料检验的子件：{string.Join("、", missing.Select(m => m.ChildInventoryCode).Distinct())}");
            }

            return (true, "已通过来料检验");
        }

        /// <summary>
        /// 齐套校验明细（需用数量=单个用量×投产数量×(1+损耗率)；可领量=在库-占用）.
        /// 投产数量取流程卡定量（为空回退订单计划数量）.
        /// </summary>
        /// <param name="card">流程卡.</param>
        /// <param name="order">生产订单（可空）.</param>
        /// <returns>齐套明细 + 是否齐套.</returns>
        private async Task<(List<PickKittingItemDto> Items, bool Pass)> BuildKittingItems(FlowCardEntity card, ProductionOrderEntity order)
        {
            var items = new List<PickKittingItemDto>();
            var planQty = card.Qty ?? order?.PlanQty ?? 0;
            var parent = await _inventoryRepository.GetFirstAsync(m => m.InventoryName == card.ProductName && m.DeleteMark == null);
            if (parent == null || planQty <= 0)
            {
                return (items, true);
            }

            var bomItems = await _bomRepository.AsQueryable()
                .Where(m => m.ParentInventoryId == parent.Id && m.DeleteMark == null)
                .OrderBy(m => m.PositionNo, OrderByType.Asc)
                .ToListAsync();
            if (!bomItems.Any())
            {
                return (items, true);
            }

            var childIds = bomItems.Select(m => m.ChildInventoryId).Distinct().ToList();
            var children = await _inventoryRepository.AsQueryable()
                .Where(m => childIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync();
            var stockRows = await _stockRepository.AsQueryable()
                .Where(m => childIds.Contains(m.InventoryId) && m.DeleteMark == null).ToListAsync();

            foreach (var bom in bomItems)
            {
                var child = children.FirstOrDefault(m => m.Id == bom.ChildInventoryId);
                var quantity = bom.Quantity ?? 0;
                var lossRate = bom.LossRate ?? 0;

                // 需用数量 = 单个用量 × 投产数量 × (1 + 损耗率)
                var requireQty = Math.Round(quantity * planQty * (1 + lossRate / 100), 2);

                // 可领量 = 在库 − 占用（该子件全部批次汇总）
                var availableQty = stockRows.Where(m => m.InventoryId == bom.ChildInventoryId)
                    .Sum(m => (m.StockQty ?? 0) - (m.OccupiedQty ?? 0));
                if (availableQty < 0)
                {
                    availableQty = 0;
                }

                var lackQty = Math.Max(0, Math.Round(requireQty - availableQty, 2));
                items.Add(new PickKittingItemDto
                {
                    ChildInventoryCode = child?.InventoryCode ?? bom.ChildInventoryCode,
                    ChildInventoryName = child?.InventoryName,
                    Unit = child?.Unit,
                    RequireQty = requireQty,
                    AvailableQty = Math.Round(availableQty, 2),
                    LackQty = lackQty,
                    Satisfied = lackQty > 0 ? 0 : 1
                });
            }

            return (items, items.All(m => m.Satisfied == 1));
        }

        /// <summary>
        /// 生成领料单号（LL+yyyyMMdd+3位序号）.
        /// </summary>
        /// <returns>领料单号.</returns>
        private async Task<string> BuildPickingNo()
        {
            var prefix = "LL" + DateTime.Now.ToString("yyyyMMdd");
            var seq = await _pickingRepository.AsQueryable()
                .Where(m => m.PickingNo.StartsWith(prefix)).CountAsync() + 1;
            var pickingNo = prefix + seq.ToString("D3");
            while (await _pickingRepository.IsAnyAsync(m => m.PickingNo == pickingNo))
            {
                seq++;
                pickingNo = prefix + seq.ToString("D3");
            }

            return pickingNo;
        }

        /// <summary>
        /// 生成材料出库记录单号（CK+yyyyMMdd+3位序号，sx_out_stock 独立计数）.
        /// </summary>
        /// <returns>出库单号.</returns>
        private async Task<string> BuildOutStockNo()
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
        /// 材料出库单自动生成（单据头+明细；多批次扣减每批次一行；审核后模拟推送用友）.
        /// </summary>
        /// <param name="outRecord">材料出库记录.</param>
        /// <param name="inventory">物料档案.</param>
        /// <param name="deductItems">扣减明细.</param>
        /// <param name="pickerName">领料人.</param>
        /// <returns>无.</returns>
        private async Task CreateOutOrderForPicking(OutStockEntity outRecord, InventoryMasterEntity inventory, List<OutDeductItemDto> deductItems, string pickerName)
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

            var order = new OutOrderEntity
            {
                OrderNo = orderNo,
                OutType = 3,
                OrderId = outRecord.OrderId,
                OrderNoRef = outRecord.OrderNo,
                FlowCardNo = outRecord.FlowCardNo,
                WarehouseId = outRecord.WarehouseId,
                WarehouseName = outRecord.WarehouseName,
                Status = 3,
                AuditUser = pickerName,
                AuditTime = DateTime.Now,
                SyncStatus = 1,
                Remark = "领料自动生成材料出库"
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
                    InventoryId = outRecord.InventoryId,
                    InventoryCode = outRecord.InventoryCode,
                    InventoryName = outRecord.InventoryName,
                    Spec = inventory.Spec,
                    Unit = outRecord.Unit,
                    Qty = item.Qty,
                    BatchNo = item.BatchNo,
                    LocationCode = item.LocationCode,
                    Remark = outRecord.Remark
                }).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }
    }
}
