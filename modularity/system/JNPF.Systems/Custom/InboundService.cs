using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Custom;
using JNPF.example.Entitys;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 入库服务.
    /// PDA采购入库（扫流程卡/到货单带出待入库明细逐项核对、扫库位码落位、入库数量≤待入数量、
    /// 到货单来源校验流程卡必填）、PDA产成品入库（扫流程卡带出完工数量与报工核对不一致拦截、
    /// 批次建档+货位归位、有无零头箱、免检直接确认）、PDA其它入库；
    /// 入库统一由 StockService.StockInAsync 完成「批次建档 + 货位归位」并写库存流水；入库单管理.
    /// </summary>
    [ApiDescriptionSettings(Tag = "Inbound", Name = "", Order = 215)]
    [Route("api/[controller]")]
    public class InboundService : IInboundService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 入库记录仓储.
        /// </summary>
        private readonly ISqlSugarRepository<StockInEntity> _inRecordRepository;

        /// <summary>
        /// 入库单主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InOrderEntity> _inOrderRepository;

        /// <summary>
        /// 入库单明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InOrderDetailEntity> _inOrderDetailRepository;

        /// <summary>
        /// 存货主档仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryMasterEntity> _inventoryRepository;

        /// <summary>
        /// 来料检验仓储.
        /// </summary>
        private readonly ISqlSugarRepository<IqcEntity> _iqcRepository;

        /// <summary>
        /// 数据库管理（切换到货单外部库）.
        /// </summary>
        private readonly IDataBaseManager _dataBaseManager;

        /// <summary>
        /// 流程卡仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardEntity> _flowCardRepository;

        /// <summary>
        /// 生产订单仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionOrderEntity> _productionOrderRepository;

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
        /// 库存核心服务（内部入库/批次建档方法）.
        /// </summary>
        private readonly IStockService _stockService;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 用友同步服务（出入库审核后自动推送现有量，模拟）.
        /// </summary>
        private readonly IYonyouSyncService _yonyouSyncService;

        /// <summary>
        /// 初始化一个<see cref="InboundService"/>类型的新实例.
        /// </summary>
        public InboundService(
            ISqlSugarRepository<StockInEntity> inRecordRepository,
            ISqlSugarRepository<InOrderEntity> inOrderRepository,
            ISqlSugarRepository<InOrderDetailEntity> inOrderDetailRepository,
            ISqlSugarRepository<InventoryMasterEntity> inventoryRepository,
            ISqlSugarRepository<IqcEntity> iqcRepository,
            ISqlSugarRepository<FlowCardEntity> flowCardRepository,
            ISqlSugarRepository<ProductionOrderEntity> productionOrderRepository,
            ISqlSugarRepository<WorkReportEntity> workReportRepository,
            ISqlSugarRepository<WarehouseEntity> warehouseRepository,
            ISqlSugarRepository<WarehouseLocationEntity> locationRepository,
            ISqlSugarRepository<InventoryStockEntity> stockRepository,
            ISqlSugarRepository<StockFlowEntity> flowRepository,
            IStockService stockService,
            IUserManager userManager,
            IDataBaseManager dataBaseManager,
            IYonyouSyncService yonyouSyncService)
        {
            _inRecordRepository = inRecordRepository;
            _inOrderRepository = inOrderRepository;
            _inOrderDetailRepository = inOrderDetailRepository;
            _inventoryRepository = inventoryRepository;
            _iqcRepository = iqcRepository;
            _flowCardRepository = flowCardRepository;
            _productionOrderRepository = productionOrderRepository;
            _workReportRepository = workReportRepository;
            _warehouseRepository = warehouseRepository;
            _locationRepository = locationRepository;
            _stockRepository = stockRepository;
            _flowRepository = flowRepository;
            _stockService = stockService;
            _userManager = userManager;
            _dataBaseManager = dataBaseManager;
            _yonyouSyncService = yonyouSyncService;
        }

        /// <summary>
        /// 入库记录列表（按类型/方式/到货单号/流程卡/日期筛选）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetInRecordList")]
        public async Task<dynamic> GetInRecordList([FromBody] InRecordQuery input)
        {
            var whereLambda = LinqExpression.And<StockInEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (input.InType != -1)
            {
                whereLambda = whereLambda.And(m => m.InType == input.InType);
            }

            if (input.InMode != -1)
            {
                whereLambda = whereLambda.And(m => m.InMode == input.InMode);
            }

            if (!input.ArrivalNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.ArrivalNo.Contains(input.ArrivalNo));
            }

            if (!input.FlowCardNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.FlowCardNo.Contains(input.FlowCardNo));
            }

            if (!input.InventoryKeyword.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.InventoryCode.Contains(input.InventoryKeyword)
                    || m.InventoryName.Contains(input.InventoryKeyword));
            }

            if (input.StartTime != null)
            {
                whereLambda = whereLambda.And(m => m.InTime >= input.StartTime);
            }

            if (input.EndTime != null)
            {
                var endTime = input.EndTime.Value.Date.AddDays(1);
                whereLambda = whereLambda.And(m => m.InTime < endTime);
            }

            var page = await _inRecordRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.InTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            var dtos = page.list.Select(m => new InRecordListDto
            {
                Id = m.Id,
                InNo = m.InNo,
                InType = m.InType,
                InMode = m.InMode,
                ArrivalNo = m.ArrivalNo,
                FlowCardNo = m.FlowCardNo,
                InventoryCode = m.InventoryCode,
                InventoryName = m.InventoryName,
                Qty = m.Qty,
                Unit = m.Unit,
                WarehouseName = m.WarehouseName,
                LocationCode = m.LocationCode,
                BatchNo = m.BatchNo,
                HasOddBox = m.HasOddBox,
                OperatorName = m.OperatorName,
                InTime = m.InTime,
                Status = m.Status,
                SyncStatus = m.SyncStatus
            }).ToList();

            return new SqlSugarPagedList<InRecordListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 入库记录详情（含存货规格）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>详情.</returns>
        [HttpGet("GetInRecordDetail")]
        public async Task<dynamic> GetInRecordDetail(string id)
        {
            var entity = await _inRecordRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var inventory = await _inventoryRepository.GetFirstAsync(m => m.Id == entity.InventoryId && m.DeleteMark == null);

            return new
            {
                entity.Id,
                entity.InNo,
                entity.InType,
                entity.InMode,
                entity.ArrivalId,
                entity.ArrivalNo,
                entity.FlowCardId,
                entity.FlowCardNo,
                entity.InventoryId,
                entity.InventoryCode,
                entity.InventoryName,
                Spec = inventory?.Spec,
                entity.Qty,
                entity.Unit,
                entity.WarehouseId,
                entity.WarehouseCode,
                entity.WarehouseName,
                entity.LocationId,
                entity.LocationCode,
                entity.BatchNo,
                entity.HasOddBox,
                entity.OperatorName,
                entity.InTime,
                entity.Status,
                entity.SyncStatus,
                entity.Remark
            };
        }

        /// <summary>
        /// 入库记录删除（软删；已入库记录同步回滚库存与流水，不足回滚时拦截）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteInRecord")]
        public async Task DeleteInRecord(string id)
        {
            var entity = await _inRecordRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 已入库：回滚「批次建档 + 货位归位」对应台账数量
            if (entity.Status == 2)
            {
                var qty = entity.Qty ?? 0;
                var stock = await _stockRepository.GetFirstAsync(m => m.InventoryId == entity.InventoryId
                    && m.WarehouseId == entity.WarehouseId
                    && m.LocationId == entity.LocationId
                    && m.BatchNo == entity.BatchNo
                    && m.DeleteMark == null);
                if (stock == null || (stock.StockQty ?? 0) < qty)
                {
                    throw Oops.Oh(ErrorCode.COM1018, "对应批次库存已被消耗，无法删除该入库记录");
                }

                var beforeQty = stock.StockQty ?? 0;
                stock.StockQty = beforeQty - qty;
                await _stockRepository.AsSugarClient().Updateable(stock)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.StockQty, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();

                // 红字流水（冲销）
                await _flowRepository.AsSugarClient().Insertable(new StockFlowEntity
                {
                    InventoryId = entity.InventoryId,
                    InventoryCode = entity.InventoryCode,
                    InventoryName = entity.InventoryName,
                    WarehouseName = entity.WarehouseName,
                    LocationCode = entity.LocationCode,
                    BatchNo = entity.BatchNo,
                    BizType = entity.InType ?? 1,
                    Direction = 2,
                    Qty = qty,
                    BeforeQty = beforeQty,
                    AfterQty = beforeQty - qty,
                    BizId = entity.Id,
                    BizNo = entity.InNo,
                    FlowCardNo = entity.FlowCardNo,
                    OperatorName = _userManager.RealName,
                    OperateTime = DateTime.Now,
                    Remark = "入库记录删除冲销"
                }).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            int isOk = await _inRecordRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete())
                .UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
                .ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 到货待入库明细（PDA采购入库扫到货单号带出，逐项核对）.
        /// </summary>
        /// <param name="arrivalNo">到货单号.</param>
        /// <returns>待入库明细列表.</returns>
        [HttpGet("GetPendingArrivalItems")]
        public async Task<dynamic> GetPendingArrivalItems(string arrivalNo)
        {
            if (arrivalNo.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "到货单号不能为空");
            }

            // 到货单在外部库（DbLink），切换后查询
            var dbLink = await _inRecordRepository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
            var arrivalClient = _dataBaseManager.ChangeDataBase(dbLink);
            var main = await arrivalClient.Queryable<SxPurchaseArrivalMainEntity>().FirstAsync(m => m.Bill_no == arrivalNo);
            if (main == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"到货单 {arrivalNo} 不存在");
            }

            var details = await arrivalClient.Queryable<SxPurchaseArrivalDetailEntity>()
                .Where(m => m.Arrival_main_id == main.Id)
                .ToListAsync();

            // 已入库数量汇总（同到货单+存货）
            var inRecords = await _inRecordRepository.AsQueryable()
                .Where(m => m.ArrivalNo == arrivalNo && m.Status == 2 && m.DeleteMark == null)
                .ToListAsync();

            var result = details.Select(m => new ArrivalPendingItemDto
            {
                ArrivalDetailId = m.Id,
                InventoryCode = m.Inventory_code,
                InventoryName = m.Inventory_name,
                Spec = m.Spec_model,
                Unit = m.Main_unit,
                BatchNo = m.Batch_no,
                WarehouseName = m.Warehouse_name,
                InQty = inRecords.Where(x => x.InventoryCode == m.Inventory_code).Sum(x => x.Qty ?? 0)
            }).ToList();

            return result;
        }

        /// <summary>
        /// PDA采购入库确认（到货单号非空时流程卡必填；IQC不合格拦截；货位管理仓库必须扫库位码落位；
        /// 批次自动建档；入库记录+流水+入库单一次完成；审核后模拟推送用友）.
        /// </summary>
        /// <param name="input">入库确认入参.</param>
        /// <returns>入库结果.</returns>
        [HttpPost("PurchaseIn")]
        public async Task<dynamic> PurchaseIn([FromBody] PurchaseInInput input)
        {
            if (input.Qty <= 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, "入库数量必须大于0");
            }

            // 规则：到货单号非空 → 流程卡必填（到货必须先打印流程卡）
            if (!input.ArrivalNo.IsNullOrEmpty() && input.FlowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "到货单来源的入库必须扫描流程卡（到货须先打印流程卡）");
            }

            // 存货档案（编码兜底查询并回填）
            var inventory = await ResolveInventory(input.InventoryId, input.InventoryCode);
            input.InventoryId = inventory.Id;
            input.InventoryCode = inventory.InventoryCode;
            input.InventoryName = input.InventoryName.IsNullOrEmpty() ? inventory.InventoryName : input.InventoryName;
            input.Spec = input.Spec.IsNullOrEmpty() ? inventory.Spec : input.Spec;
            input.Unit = input.Unit.IsNullOrEmpty() ? inventory.Unit : input.Unit;

            // 到货单校验：存在性 + 存货在明细中 + IQC不合格拦截
            string supplierName = null;
            if (!input.ArrivalNo.IsNullOrEmpty())
            {
                // 到货单在外部库（DbLink），切换后查询
                var dbLink = await _inRecordRepository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("1980101230294339584"));
                var arrivalClient = _dataBaseManager.ChangeDataBase(dbLink);
                var main = await arrivalClient.Queryable<SxPurchaseArrivalMainEntity>().FirstAsync(m => m.Bill_no == input.ArrivalNo);
                if (main == null)
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"到货单 {input.ArrivalNo} 不存在");
                }

                supplierName = main.Supplier_name;
                if (input.ArrivalId.IsNullOrEmpty())
                {
                    input.ArrivalId = main.Id;
                }

                // 逐项核对：存货必须在到货明细中（到货明细无数量字段，数量上限以到货单为准管控）
                var inArrival = await arrivalClient.Queryable<SxPurchaseArrivalDetailEntity>().AnyAsync(m => m.Arrival_main_id == main.Id
                    && m.Inventory_code == inventory.InventoryCode);
                if (!inArrival)
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"存货 {inventory.InventoryCode} 不在到货单 {input.ArrivalNo} 明细中");
                }

                // IQC不合格拦截（不合格物料禁止入库）
                var hasNg = await _iqcRepository.IsAnyAsync(m => m.ArrivalNo == input.ArrivalNo
                    && m.CheckResult == 2 && m.DeleteMark == null);
                if (hasNg)
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"到货单 {input.ArrivalNo} 存在来料检验（IQC）不合格记录，禁止入库");
                }
            }

            // 流程卡（扫码带出卡号）
            string flowCardNo = null;
            if (!input.FlowCardId.IsNullOrEmpty())
            {
                var card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
                if (card == null)
                {
                    throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在或已作废");
                }

                flowCardNo = card.CardNo;
            }

            // 仓库/货位校验（货位管理仓库必须扫库位码落位）
            var (warehouse, location) = await ResolveWarehouseLocation(input.WarehouseId, input.LocationId);

            // 批次建档（无批次时自动生成）
            if (input.BatchNo.IsNullOrEmpty())
            {
                input.BatchNo = await _stockService.BuildBatchNo(inventory.InventoryCode);
            }

            var now = DateTime.Now;
            var inNo = await BuildInNo();
            var record = new StockInEntity
            {
                InNo = inNo,
                InType = 1,
                InMode = input.FlowCardId.IsNullOrEmpty() ? 2 : 1,
                ArrivalId = input.ArrivalId,
                ArrivalNo = input.ArrivalNo,
                FlowCardId = input.FlowCardId,
                FlowCardNo = flowCardNo,
                InventoryId = input.InventoryId,
                InventoryCode = input.InventoryCode,
                InventoryName = input.InventoryName,
                Qty = input.Qty,
                Unit = input.Unit,
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.RepoCode,
                WarehouseName = warehouse.RepoName,
                LocationId = location?.Id,
                LocationCode = location?.LocationCode,
                BatchNo = input.BatchNo,
                HasOddBox = 0,
                OperatorName = _userManager.RealName,
                InTime = now,
                Status = 1,
                SyncStatus = 1,
                Remark = input.Remark
            };

            var insertOk = await _inRecordRepository.AsSugarClient().Insertable(record)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 批次建档 + 货位归位（写库存流水）
            var change = await _stockService.StockInAsync(record, input.Qty);

            // 入库成功：置已入库
            await _inRecordRepository.AsSugarClient().Updateable<StockInEntity>()
                .SetColumns(it => new StockInEntity { Status = 2, InTime = DateTime.Now })
                .Where(it => it.Id == record.Id)
                .ExecuteCommandAsync();

            // 自动生成入库单（审核后模拟推送用友）
            await CreateInOrder(record, supplierName, input.Remark);

            // 审核后自动推送现有量变更（模拟）
            await _yonyouSyncService.WriteAutoPushLogAsync("采购入库单", inNo, $"到货单 {input.ArrivalNo} 入库推送");

            return new InConfirmOutput
            {
                InNo = inNo,
                BatchNo = input.BatchNo,
                LocationCode = location?.LocationCode,
                BeforeQty = change.BeforeQty,
                AfterQty = change.AfterQty,
                Message = "采购入库成功，库存已更新并推送用友（模拟）"
            };
        }

        /// <summary>
        /// PDA产成品入库确认（完工数量与报工核对不一致拦截；勾稽：入库≤卡定量−已入 且 ≤报工良品−已入；
        /// 批次建档+货位归位；有无零头箱；免检直接确认）.
        /// </summary>
        /// <param name="input">入库确认入参.</param>
        /// <returns>入库结果.</returns>
        [HttpPost("ProductIn")]
        public async Task<dynamic> ProductIn([FromBody] ProductInInput input)
        {
            if (input.FlowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请扫描流程卡后再入库");
            }

            if (input.Qty <= 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, "入库数量必须大于0");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在或已作废");
            }

            // 订单须处于待入库（OQC合格联动置5）之后
            var order = await _productionOrderRepository.GetFirstAsync(m => m.Id == card.OrderId && m.DeleteMark == null);
            if (order != null && (order.Status == null || order.Status < 5))
            {
                throw Oops.Oh(ErrorCode.COM1018, $"订单 {order.OrderNo} 尚未完成出货检验（OQC），当前状态不满足入库条件");
            }

            // 成品存货（按订单产品名称匹配存货档案）
            var inventory = await _inventoryRepository.GetFirstAsync(m => m.InventoryName == order.ProductName && m.DeleteMark == null);
            if (inventory == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"未找到与订单产品「{order.ProductName}」匹配的存货档案，无法入库");
            }

            // 已入库数量（同流程卡）
            var arrivedQty = (await _inRecordRepository.AsQueryable()
                .Where(m => m.FlowCardId == input.FlowCardId && m.Status == 2 && m.DeleteMark == null)
                .ToListAsync()).Sum(m => m.Qty ?? 0);

            // 勾稽①：Σ产成品入库 ≤ 流程卡定量
            var cardQty = card.Qty ?? 0;
            if (arrivedQty + input.Qty > cardQty)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"勾稽校验不通过：入库累计 {arrivedQty + input.Qty} 不得超过流程卡定量 {cardQty}");
            }

            // 勾稽②：完工数量与报工核对（Σ报工良品），不一致拦截
            var goodQty = (await _workReportRepository.AsQueryable()
                .Where(m => m.FlowCardId == input.FlowCardId && m.Status == 1 && m.DeleteMark == null)
                .ToListAsync()).Sum(m => m.GoodQty ?? 0);
            if (arrivedQty + input.Qty > goodQty)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"完工数量与报工核对不一致：入库累计 {arrivedQty + input.Qty} 超过报工良品总数 {goodQty}");
            }

            // 仓库/货位校验（货位管理仓库必须扫库位码落位）
            var (warehouse, location) = await ResolveWarehouseLocation(input.WarehouseId, input.LocationId);

            // 批次建档
            if (input.BatchNo.IsNullOrEmpty())
            {
                input.BatchNo = await _stockService.BuildBatchNo(inventory.InventoryCode);
            }

            var now = DateTime.Now;
            var inNo = await BuildInNo();
            var record = new StockInEntity
            {
                InNo = inNo,
                InType = 2,
                InMode = 1,
                FlowCardId = input.FlowCardId,
                FlowCardNo = card.CardNo,
                InventoryId = inventory.Id,
                InventoryCode = inventory.InventoryCode,
                InventoryName = inventory.InventoryName,
                Qty = input.Qty,
                Unit = card.Unit ?? inventory.Unit,
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.RepoCode,
                WarehouseName = warehouse.RepoName,
                LocationId = location?.Id,
                LocationCode = location?.LocationCode,
                BatchNo = input.BatchNo,
                HasOddBox = input.HasOddBox,
                OperatorName = _userManager.RealName,
                InTime = now,
                Status = 1,
                SyncStatus = 1,
                Remark = input.Remark
            };

            var insertOk = await _inRecordRepository.AsSugarClient().Insertable(record)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 批次建档 + 货位归位（写库存流水）
            var change = await _stockService.StockInAsync(record, input.Qty);

            // 入库成功：置已入库
            await _inRecordRepository.AsSugarClient().Updateable<StockInEntity>()
                .SetColumns(it => new StockInEntity { Status = 2, InTime = DateTime.Now })
                .Where(it => it.Id == record.Id)
                .ExecuteCommandAsync();

            // 自动生成入库单
            await CreateInOrder(record, null, input.Remark);

            // 审核后自动推送现有量变更（模拟）
            await _yonyouSyncService.WriteAutoPushLogAsync("产成品入库单", inNo, $"流程卡 {card.CardNo} 产成品入库推送");

            var oddBoxText = input.HasOddBox == 1 ? "（含零头箱）" : string.Empty;
            return new InConfirmOutput
            {
                InNo = inNo,
                BatchNo = input.BatchNo,
                LocationCode = location?.LocationCode,
                BeforeQty = change.BeforeQty,
                AfterQty = change.AfterQty,
                Message = $"产成品入库成功{oddBoxText}，报工已自检，免检直接确认；批次已建档归位"
            };
        }

        /// <summary>
        /// PDA其它入库确认（盘盈/退料/其他，允许无流程卡）.
        /// </summary>
        /// <param name="input">入库确认入参.</param>
        /// <returns>入库结果.</returns>
        [HttpPost("OtherIn")]
        public async Task<dynamic> OtherIn([FromBody] OtherInInput input)
        {
            if (input.Qty <= 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, "入库数量必须大于0");
            }

            var inventory = await ResolveInventory(input.InventoryId, input.InventoryCode);
            input.InventoryId = inventory.Id;
            input.InventoryCode = inventory.InventoryCode;
            input.InventoryName = input.InventoryName.IsNullOrEmpty() ? inventory.InventoryName : input.InventoryName;
            input.Unit = input.Unit.IsNullOrEmpty() ? inventory.Unit : input.Unit;

            // 流程卡可空（无到货单来源才允许无卡）
            string flowCardNo = null;
            if (!input.FlowCardId.IsNullOrEmpty())
            {
                var card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
                flowCardNo = card?.CardNo;
            }

            // 仓库/货位校验
            var (warehouse, location) = await ResolveWarehouseLocation(input.WarehouseId, input.LocationId);

            // 批次建档
            if (input.BatchNo.IsNullOrEmpty())
            {
                input.BatchNo = await _stockService.BuildBatchNo(inventory.InventoryCode);
            }

            var now = DateTime.Now;
            var inNo = await BuildInNo();
            var record = new StockInEntity
            {
                InNo = inNo,
                InType = 3,
                InMode = input.FlowCardId.IsNullOrEmpty() ? 2 : 1,
                FlowCardId = input.FlowCardId,
                FlowCardNo = flowCardNo,
                InventoryId = input.InventoryId,
                InventoryCode = input.InventoryCode,
                InventoryName = input.InventoryName,
                Qty = input.Qty,
                Unit = input.Unit,
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.RepoCode,
                WarehouseName = warehouse.RepoName,
                LocationId = location?.Id,
                LocationCode = location?.LocationCode,
                BatchNo = input.BatchNo,
                HasOddBox = 0,
                OperatorName = _userManager.RealName,
                InTime = now,
                Status = 1,
                SyncStatus = 1,
                Remark = input.Remark
            };

            var insertOk = await _inRecordRepository.AsSugarClient().Insertable(record)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 批次建档 + 货位归位（写库存流水）
            var change = await _stockService.StockInAsync(record, input.Qty);

            await _inRecordRepository.AsSugarClient().Updateable<StockInEntity>()
                .SetColumns(it => new StockInEntity { Status = 2, InTime = DateTime.Now })
                .Where(it => it.Id == record.Id)
                .ExecuteCommandAsync();

            await CreateInOrder(record, null, input.Remark);

            // 审核后自动推送现有量变更（模拟）
            await _yonyouSyncService.WriteAutoPushLogAsync("其他入库单", inNo, "其它入库推送");

            return new InConfirmOutput
            {
                InNo = inNo,
                BatchNo = input.BatchNo,
                LocationCode = location?.LocationCode,
                BeforeQty = change.BeforeQty,
                AfterQty = change.AfterQty,
                Message = "其它入库成功，库存已更新并推送用友（模拟）"
            };
        }

        /// <summary>
        /// 入库单管理列表（分采购入库/产成品入库/其他入库）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetInOrderList")]
        public async Task<dynamic> GetInOrderList([FromBody] InOrderQuery input)
        {
            var whereLambda = LinqExpression.And<InOrderEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!input.OrderNo.IsNullOrEmpty())
            {
                whereLambda = whereLambda.And(m => m.OrderNo.Contains(input.OrderNo));
            }

            if (input.InType != -1)
            {
                whereLambda = whereLambda.And(m => m.InType == input.InType);
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

            return await _inOrderRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 入库单明细（单据头+明细）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>详情.</returns>
        [HttpGet("GetInOrderDetail")]
        public async Task<dynamic> GetInOrderDetail(string id)
        {
            var order = await _inOrderRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (order == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var details = await _inOrderDetailRepository.AsQueryable()
                .Where(m => m.OrderId == id && m.DeleteMark == null)
                .ToListAsync();

            return new InOrderDetailOutput
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                InType = order.InType,
                InMode = order.InMode,
                ArrivalNo = order.ArrivalNo,
                SupplierName = order.SupplierName,
                FlowCardNo = order.FlowCardNo,
                WarehouseName = order.WarehouseName,
                Status = order.Status,
                AuditUser = order.AuditUser,
                AuditTime = order.AuditTime,
                Remark = order.Remark,
                Details = details.Select(m => new InOrderDetailItemDto
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
        /// 存货解析（ID 或编码兜底查询）.
        /// </summary>
        /// <param name="inventoryId">存货ID（可空）.</param>
        /// <param name="inventoryCode">存货编码（可空）.</param>
        /// <returns>存货档案.</returns>
        private async Task<InventoryMasterEntity> ResolveInventory(string inventoryId, string inventoryCode)
        {
            var inventory = !inventoryId.IsNullOrEmpty()
                ? await _inventoryRepository.GetFirstAsync(m => m.Id == inventoryId && m.DeleteMark == null)
                : await _inventoryRepository.GetFirstAsync(m => m.InventoryCode == inventoryCode && m.DeleteMark == null);
            if (inventory == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "存货档案不存在，请核对物料编码");
            }

            return inventory;
        }

        /// <summary>
        /// 仓库/货位解析校验（货位管理仓库必须扫库位码落位；库位与仓库归属一致性校验）.
        /// </summary>
        /// <param name="warehouseId">仓库ID（可空，货位非空时以货位为准）.</param>
        /// <param name="locationId">货位ID（可空）.</param>
        /// <returns>仓库与货位.</returns>
        private async Task<(WarehouseEntity Warehouse, WarehouseLocationEntity Location)> ResolveWarehouseLocation(string warehouseId, string locationId)
        {
            WarehouseLocationEntity location = null;
            if (!locationId.IsNullOrEmpty())
            {
                location = await _locationRepository.GetFirstAsync(m => m.Id == locationId && m.DeleteMark == null);
                if (location == null)
                {
                    throw Oops.Oh(ErrorCode.COM1018, "货位不存在，请重新扫描库位码");
                }

                if (location.Status == 0)
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"货位 {location.LocationCode} 已停用，禁止落位");
                }

                // 以货位所属仓库为准
                warehouseId = location.WarehouseId;
            }

            var warehouse = await _warehouseRepository.GetFirstAsync(m => m.Id == warehouseId && m.DeleteMark == null);
            if (warehouse == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "仓库不存在，请先选择仓库或扫描库位码");
            }

            // 货位管理仓库必须扫库位码落位
            if (warehouse.IsLocationManage == 1 && location == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"仓库「{warehouse.RepoName}」启用货位管理，必须扫描库位码落位");
            }

            return (warehouse, location);
        }

        /// <summary>
        /// 生成入库单号（RK+yyyyMMdd+3位序号）.
        /// </summary>
        /// <returns>入库单号.</returns>
        private async Task<string> BuildInNo()
        {
            var prefix = "RK" + DateTime.Now.ToString("yyyyMMdd");
            var seq = await _inRecordRepository.AsQueryable()
                .Where(m => m.InNo.StartsWith(prefix)).CountAsync() + 1;
            var inNo = prefix + seq.ToString("D3");
            while (await _inRecordRepository.IsAnyAsync(m => m.InNo == inNo))
            {
                seq++;
                inNo = prefix + seq.ToString("D3");
            }

            return inNo;
        }

        /// <summary>
        /// 自动生成入库单（单据头+明细；审核后模拟推送用友）.
        /// </summary>
        /// <param name="record">入库记录.</param>
        /// <param name="supplierName">供应商（可空）.</param>
        /// <param name="remark">备注.</param>
        /// <returns>无.</returns>
        private async Task CreateInOrder(StockInEntity record, string supplierName, string remark)
        {
            var orderNo = await BuildInOrderNo();
            var spec = (await _inventoryRepository.GetFirstAsync(m => m.Id == record.InventoryId && m.DeleteMark == null))?.Spec;

            var order = new InOrderEntity
            {
                OrderNo = orderNo,
                InType = record.InType,
                InMode = record.InMode,
                ArrivalId = record.ArrivalId,
                ArrivalNo = record.ArrivalNo,
                SupplierName = supplierName,
                FlowCardNo = record.FlowCardNo,
                WarehouseId = record.WarehouseId,
                WarehouseName = record.WarehouseName,
                Status = 3,
                AuditUser = _userManager.RealName,
                AuditTime = DateTime.Now,
                SyncStatus = 1,
                Remark = remark
            };

            var insertOk = await _inOrderRepository.AsSugarClient().Insertable(order)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            await _inOrderDetailRepository.AsSugarClient().Insertable(new InOrderDetailEntity
            {
                OrderId = order.Id,
                InventoryId = record.InventoryId,
                InventoryCode = record.InventoryCode,
                InventoryName = record.InventoryName,
                Spec = spec,
                Unit = record.Unit,
                Qty = record.Qty,
                BatchNo = record.BatchNo,
                LocationCode = record.LocationCode,
                Remark = record.Remark
            }).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        /// <summary>
        /// 生成入库单管理单号（RK+yyyyMMdd+3位序号，sx_in_order 独立计数）.
        /// </summary>
        /// <returns>入库单号.</returns>
        private async Task<string> BuildInOrderNo()
        {
            var prefix = "RK" + DateTime.Now.ToString("yyyyMMdd");
            var seq = await _inOrderRepository.AsQueryable()
                .Where(m => m.OrderNo.StartsWith(prefix)).CountAsync() + 1;
            var orderNo = prefix + seq.ToString("D3");
            while (await _inOrderRepository.IsAnyAsync(m => m.OrderNo == orderNo))
            {
                seq++;
                orderNo = prefix + seq.ToString("D3");
            }

            return orderNo;
        }
    }
}
