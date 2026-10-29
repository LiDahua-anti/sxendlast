using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Core.Manager;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 生产订单管理（生产计划管理）.
    /// 订单来自用友U8同步（模拟）或手工新增；按订单图番从工艺线路带出工序明细与可用设备排产；
    /// 排产保存强制校验「流程卡必选」；发布校验①排产已绑卡②来料检验/前道工序完成；
    /// 删除订单清理排产、流程卡解绑保留、领料/报工标记作废保留、不回冲库存（审计日志记录影响范围）。
    /// </summary>
    [ApiDescriptionSettings(Tag = "ProductionOrder", Name = "", Order = 207)]
    [Route("api/[controller]")]
    public class ProductionOrderService : IProductionOrderService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 生产订单仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionOrderEntity> _orderRepository;

        /// <summary>
        /// 排产主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionScheduleEntity> _scheduleRepository;

        /// <summary>
        /// 排产明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionScheduleDetailEntity> _scheduleDetailRepository;

        /// <summary>
        /// 订单操作审计日志仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionScheduleLogEntity> _scheduleLogRepository;

        /// <summary>
        /// 流程卡仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardEntity> _flowCardRepository;

        /// <summary>
        /// 流程卡工序流转仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardProcessEntity> _flowCardProcessRepository;

        /// <summary>
        /// 来料检验仓储.
        /// </summary>
        private readonly ISqlSugarRepository<IqcEntity> _iqcRepository;

        /// <summary>
        /// 报工明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<WorkReportEntity> _workReportRepository;

        /// <summary>
        /// 领料记录仓储.
        /// </summary>
        private readonly ISqlSugarRepository<PickingRecordEntity> _pickingRepository;

        /// <summary>
        /// 基础工序仓储.
        /// </summary>
        private readonly ISqlSugarRepository<BaseProcessEntity> _baseProcessRepository;

        /// <summary>
        /// 设备台账仓储.
        /// </summary>
        private readonly ISqlSugarRepository<DeviceMasterEntity> _deviceRepository;

        /// <summary>
        /// 用友同步日志仓储.
        /// </summary>
        private readonly ISqlSugarRepository<YonyouSyncLogEntity> _yonyouLogRepository;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="ProductionOrderService"/>类型的新实例.
        /// </summary>
        public ProductionOrderService(
            ISqlSugarRepository<ProductionOrderEntity> orderRepository,
            ISqlSugarRepository<ProductionScheduleEntity> scheduleRepository,
            ISqlSugarRepository<ProductionScheduleDetailEntity> scheduleDetailRepository,
            ISqlSugarRepository<ProductionScheduleLogEntity> scheduleLogRepository,
            ISqlSugarRepository<FlowCardEntity> flowCardRepository,
            ISqlSugarRepository<FlowCardProcessEntity> flowCardProcessRepository,
            ISqlSugarRepository<IqcEntity> iqcRepository,
            ISqlSugarRepository<WorkReportEntity> workReportRepository,
            ISqlSugarRepository<PickingRecordEntity> pickingRepository,
            ISqlSugarRepository<BaseProcessEntity> baseProcessRepository,
            ISqlSugarRepository<DeviceMasterEntity> deviceRepository,
            ISqlSugarRepository<YonyouSyncLogEntity> yonyouLogRepository,
            IUserManager userManager)
        {
            _orderRepository = orderRepository;
            _scheduleRepository = scheduleRepository;
            _scheduleDetailRepository = scheduleDetailRepository;
            _scheduleLogRepository = scheduleLogRepository;
            _flowCardRepository = flowCardRepository;
            _flowCardProcessRepository = flowCardProcessRepository;
            _iqcRepository = iqcRepository;
            _workReportRepository = workReportRepository;
            _pickingRepository = pickingRepository;
            _baseProcessRepository = baseProcessRepository;
            _deviceRepository = deviceRepository;
            _yonyouLogRepository = yonyouLogRepository;
            _userManager = userManager;
        }

        #region 生产订单管理

        /// <summary>
        /// 获取生产订单列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetOrderList")]
        public async Task<dynamic> GetOrderList([FromBody] OrderQuery input)
        {
            var whereLambda = LinqExpression.And<ProductionOrderEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.OrderNo))
            {
                whereLambda = whereLambda.And(m => m.OrderNo.Contains(input.OrderNo)
                    || m.DrawingNo.Contains(input.OrderNo)
                    || m.ProductName.Contains(input.OrderNo));
            }

            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.Status == input.Status);
            }

            if (input.PublishStatus != -1)
            {
                whereLambda = whereLambda.And(m => m.PublishStatus == input.PublishStatus);
            }

            return await _orderRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 获取生产订单统计卡片（待排产/已排产/已发布生产中/已完工）.
        /// </summary>
        /// <returns>各状态计数.</returns>
        [HttpGet("GetOrderStatistics")]
        public async Task<dynamic> GetOrderStatistics()
        {
            return new OrderStatisticsOutput
            {
                WaitSchedule = await _orderRepository.AsQueryable().Where(m => m.DeleteMark == null && m.Status == 1).CountAsync(),
                Scheduled = await _orderRepository.AsQueryable().Where(m => m.DeleteMark == null && m.Status == 2).CountAsync(),
                Published = await _orderRepository.AsQueryable().Where(m => m.DeleteMark == null && (m.Status == 3 || m.Status == 4)).CountAsync(),
                Finished = await _orderRepository.AsQueryable().Where(m => m.DeleteMark == null && m.Status == 6).CountAsync()
            };
        }

        /// <summary>
        /// 获取生产订单详情.
        /// </summary>
        /// <param name="id">订单主键.</param>
        /// <returns>订单详情.</returns>
        [HttpGet("GetOrderById")]
        public async Task<dynamic> GetOrderById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _orderRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return entity;
        }

        /// <summary>
        /// 新增/编辑生产订单（手工新增来源标记2）.
        /// </summary>
        /// <param name="input">订单信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruOrder")]
        public async Task CruOrder([FromBody] OrderCruInput input)
        {
            if (input.OrderNo.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "订单号不能为空");
            }

            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _orderRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                // 订单号唯一性校验（排除自身）
                if (await _orderRepository.IsAnyAsync(p => p.OrderNo == input.OrderNo && p.Id != input.Id && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                existing = input.Adapt(existing);
                isOk = await _orderRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                // 订单号唯一性校验
                if (await _orderRepository.IsAnyAsync(p => p.OrderNo == input.OrderNo && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                var entity = input.Adapt<ProductionOrderEntity>();
                entity.Status = 1;
                entity.PublishStatus = 0;
                entity.Source = 2; // 手工新增
                isOk = await _orderRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除生产订单（清理排产、流程卡解绑保留、领料/报工标记作废保留、不回冲库存）.
        /// </summary>
        /// <param name="id">订单主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteOrder")]
        public async Task DeleteOrder(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var order = await _orderRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (order == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            // ① 清理排产方案（排产主表 + 明细软删除）
            var schedules = await _scheduleRepository.AsQueryable()
                .Where(m => m.OrderId == id && m.DeleteMark == null).ToListAsync();
            var scheduleIds = schedules.Select(m => m.Id).ToList();
            if (schedules.Any())
            {
                await _scheduleRepository.AsSugarClient().Updateable(schedules)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            var scheduleDetails = scheduleIds.Any()
                ? await _scheduleDetailRepository.AsQueryable().Where(m => scheduleIds.Contains(m.ScheduleId) && m.DeleteMark == null).ToListAsync()
                : new List<ProductionScheduleDetailEntity>();
            if (scheduleDetails.Any())
            {
                await _scheduleDetailRepository.AsSugarClient().Updateable(scheduleDetails)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            // ② 流程卡解绑保留（解绑订单关联，流程卡本身保留）
            var cards = await _flowCardRepository.AsQueryable()
                .Where(m => m.OrderId == id && m.DeleteMark == null).ToListAsync();
            if (cards.Any())
            {
                var now = DateTime.Now;
                foreach (var card in cards)
                {
                    card.OrderId = null;
                    card.OrderNo = null;
                    card.LastModifyTime = now;
                    card.LastModifyUserId = _userManager.UserId;
                }

                await _flowCardRepository.AsSugarClient().Updateable(cards)
                    .UpdateColumns(it => new { it.OrderId, it.OrderNo, it.LastModifyTime, it.LastModifyUserId }).ExecuteCommandAsync();
            }

            // ③ 报工记录标记作废保留历史（1有效→0作废）
            var reports = await _workReportRepository.AsQueryable()
                .Where(m => m.OrderId == id && m.DeleteMark == null && m.Status == 1).ToListAsync();
            if (reports.Any())
            {
                var now = DateTime.Now;
                foreach (var report in reports)
                {
                    report.Status = 0;
                    report.LastModifyTime = now;
                    report.LastModifyUserId = _userManager.UserId;
                }

                await _workReportRepository.AsSugarClient().Updateable(reports)
                    .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId }).ExecuteCommandAsync();
            }

            // ④ 领料记录标记作废保留历史（→3已作废）
            var pickings = await _pickingRepository.AsQueryable()
                .Where(m => m.OrderId == id && m.DeleteMark == null && (m.Status == null || m.Status != 3)).ToListAsync();
            if (pickings.Any())
            {
                var now = DateTime.Now;
                foreach (var picking in pickings)
                {
                    picking.Status = 3;
                    picking.LastModifyTime = now;
                    picking.LastModifyUserId = _userManager.UserId;
                }

                await _pickingRepository.AsSugarClient().Updateable(pickings)
                    .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId }).ExecuteCommandAsync();
            }

            // ⑤ 订单软删除（不自动回冲库存，需通过「其他出入库」单另作调整）
            int isOk = await _orderRepository.AsSugarClient().Updateable(order)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            // ⑥ 审计日志（操作类型3：删除清理，记录影响范围）
            await WriteScheduleLog(id, order.OrderNo, 3, new
            {
                orderNo = order.OrderNo,
                scheduleCount = schedules.Count,
                scheduleDetailCount = scheduleDetails.Count,
                unboundCardCount = cards.Count,
                voidReportCount = reports.Count,
                voidPickingCount = pickings.Count,
                stockRollback = false,
                note = "不自动回冲库存，需通过「其他出入库」单另作调整"
            });
        }

        /// <summary>
        /// 用友同步拉取生产订单（模拟：生成本批订单并写同步日志）.
        /// </summary>
        /// <param name="input">拉取入参.</param>
        /// <returns>导入结果.</returns>
        [HttpPost("SyncOrdersFromYonyou")]
        public async Task<dynamic> SyncOrdersFromYonyou([FromBody] SyncPullInput input)
        {
            var count = input?.Count ?? 2;
            if (count <= 0)
            {
                count = 2;
            }

            if (count > 20)
            {
                count = 20;
            }

            var now = DateTime.Now;
            var created = new List<ProductionOrderEntity>();
            for (var i = 1; i <= count; i++)
            {
                var suffix = now.ToString("yyyyMMddHHmmss") + i.ToString("D2");
                var orderNo = "MO" + suffix;
                var seq = 1;
                while (await _orderRepository.IsAnyAsync(m => m.OrderNo == orderNo))
                {
                    seq++;
                    orderNo = "MO" + suffix + "-" + seq;
                }

                created.Add(new ProductionOrderEntity
                {
                    OrderNo = orderNo,
                    DrawingNo = "U8-" + suffix,
                    ProductName = "U8同步物料" + i,
                    PlanQty = 100m * i,
                    Unit = "件",
                    PlanFinishDate = now.Date.AddDays(7 + i),
                    Status = 1,
                    PublishStatus = 0,
                    Source = 1, // 用友U8同步
                    SyncTime = now,
                    U8OrderId = "U8-" + suffix
                });
            }

            int isOk = await _orderRepository.AsSugarClient().Insertable(created)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 写用友同步日志（模拟对接：记录请求/响应报文）
            var bizNo = string.Join(",", created.Select(m => m.OrderNo));
            if (bizNo.Length > 50)
            {
                bizNo = bizNo.Substring(0, 50);
            }

            var log = new YonyouSyncLogEntity
            {
                BizType = "生产订单",
                BizNo = bizNo,
                Direction = 2, // 用友→MES
                Status = 1,
                SyncTime = now,
                CostMs = 120,
                TriggerType = 2, // 手动触发
                RetryCount = 0,
                OperatorName = _userManager.GetUserName(_userManager.UserId),
                RequestContent = JsonSerializer.Serialize(new { action = "pull_production_orders", count }),
                ResponseContent = JsonSerializer.Serialize(new
                {
                    success = true,
                    imported = created.Count,
                    orderNos = created.Select(m => m.OrderNo).ToList()
                })
            };
            await _yonyouLogRepository.AsSugarClient().Insertable(log)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

            return new
            {
                imported = created.Count,
                orderNos = created.Select(m => m.OrderNo).ToList(),
                message = $"已从用友U8获取 {created.Count} 张生产订单"
            };
        }

        #endregion

        #region 排产管理

        /// <summary>
        /// 保存排产（强制校验流程卡必选；工序×设备×生产个数整体覆盖；写审计日志）.
        /// </summary>
        /// <param name="input">排产信息.</param>
        /// <returns>无.</returns>
        [HttpPost("SaveSchedule")]
        public async Task SaveSchedule([FromBody] ScheduleSaveInput input)
        {
            if (input.OrderId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 强制校验：流程卡必选
            if (input.FlowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请先选择流程卡，才能保存排产");
            }

            var order = await _orderRepository.GetFirstAsync(m => m.Id == input.OrderId && m.DeleteMark == null);
            if (order == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "所选流程卡不存在，请重新选择");
            }

            if (input.Details == null)
            {
                input.Details = new List<ScheduleDetailInput>();
            }

            // 排产主表（订单维度覆盖式保存）
            var schedule = await _scheduleRepository.GetFirstAsync(m => m.OrderId == input.OrderId && m.DeleteMark == null);
            if (schedule == null)
            {
                schedule = new ProductionScheduleEntity
                {
                    OrderId = input.OrderId,
                    OrderNo = order.OrderNo,
                    FlowCardId = input.FlowCardId,
                    FlowCardNo = card.CardNo,
                    PlanQty = input.PlanQty ?? order.PlanQty,
                    Status = 1,
                    Remark = input.Remark
                };
                int insertOk = await _scheduleRepository.AsSugarClient().Insertable(schedule)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                if (!(insertOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1000);
                }
            }
            else
            {
                schedule.FlowCardId = input.FlowCardId;
                schedule.FlowCardNo = card.CardNo;
                schedule.PlanQty = input.PlanQty ?? order.PlanQty;
                schedule.Status = 1; // 重新保存后回到「已保存」状态
                if (input.Remark != null)
                {
                    schedule.Remark = input.Remark;
                }

                await _scheduleRepository.AsSugarClient().Updateable(schedule)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.FlowCardId, it.FlowCardNo, it.PlanQty, it.Status, it.Remark, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();
            }

            // 工序×设备分配明细整体覆盖保存
            await SaveScheduleDetails(schedule, input);

            // 订单状态：待排产 → 已排产
            if (order.Status == null || order.Status == 1)
            {
                order.Status = 2;
                await _orderRepository.AsSugarClient().Updateable(order)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId }).ExecuteCommandAsync();
            }

            // 审计日志（操作类型1：排产保存）
            await WriteScheduleLog(input.OrderId, order.OrderNo, 1, new
            {
                flowCardId = schedule.FlowCardId,
                flowCardNo = schedule.FlowCardNo,
                planQty = schedule.PlanQty,
                detailCount = input.Details.Count,
                details = input.Details.Select(m => new { m.ProcessId, m.DeviceId, m.ProduceQty }).ToList()
            });
        }

        /// <summary>
        /// 获取订单排产方案（含工序设备分配明细）.
        /// </summary>
        /// <param name="orderId">订单主键.</param>
        /// <returns>排产方案（未排产时返回 null）.</returns>
        [HttpGet("GetScheduleByOrder")]
        public async Task<dynamic> GetScheduleByOrder(string orderId)
        {
            if (orderId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var schedule = await _scheduleRepository.GetFirstAsync(m => m.OrderId == orderId && m.DeleteMark == null);
            if (schedule == null)
            {
                return null;
            }

            var output = schedule.Adapt<ScheduleOutput>();
            var details = await _scheduleDetailRepository.AsQueryable()
                .Where(m => m.ScheduleId == schedule.Id && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();
            output.Details = details.Adapt<List<ScheduleDetailOutput>>();
            return output;
        }

        #endregion

        #region 发布与生产进度

        /// <summary>
        /// 发布生产订单（校验①排产已绑流程卡 ②来料检验/前道工序完成，不通过拦截）.
        /// </summary>
        /// <param name="id">订单主键.</param>
        /// <returns>发布结果.</returns>
        [HttpGet("PublishOrder")]
        public async Task<dynamic> PublishOrder(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var order = await _orderRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (order == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 校验①：排产方案已保存并绑定流程卡
            var schedule = await _scheduleRepository.GetFirstAsync(m => m.OrderId == id && m.DeleteMark == null);
            if (schedule == null || schedule.FlowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "发布失败：该订单尚未保存排产方案或未绑定流程卡，请先完成排产后发布");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == schedule.FlowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "发布失败：排产绑定的流程卡不存在，请重新排产");
            }

            // 校验②：来料检验（合格/免检）或供应商预加工首道工序已完成
            var passCheck = false;
            var arrivalId = card.ArrivalId;
            var arrivalDetailId = card.ArrivalDetailId;
            if (!arrivalId.IsNullOrEmpty() || !arrivalDetailId.IsNullOrEmpty())
            {
                var iqcList = await _iqcRepository.AsQueryable()
                    .Where(m => m.DeleteMark == null && (m.CheckResult == 1 || m.CheckResult == 3))
                    .ToListAsync();
                passCheck = iqcList.Any(m =>
                    (!arrivalId.IsNullOrEmpty() && m.ArrivalId == arrivalId)
                    || (!arrivalDetailId.IsNullOrEmpty() && m.ArrivalDetailId == arrivalDetailId));
            }

            if (!passCheck)
            {
                var firstProcess = await _flowCardProcessRepository.AsQueryable()
                    .Where(m => m.FlowCardId == schedule.FlowCardId && m.DeleteMark == null)
                    .OrderBy(m => m.SeqNo, OrderByType.Asc)
                    .FirstAsync();
                if (firstProcess != null && firstProcess.Status == 3)
                {
                    passCheck = true;
                }
            }

            if (!passCheck)
            {
                throw Oops.Oh(ErrorCode.COM1018, "发布失败：该流程卡来料检验/前道工序未完成，请先完成后再发布");
            }

            // 发布：订单「已发布」+ 排产「已发布」
            order.Status = 3;
            order.PublishStatus = 1;
            await _orderRepository.AsSugarClient().Updateable(order)
                .CallEntityMethod(m => m.LastModify())
                .UpdateColumns(it => new { it.Status, it.PublishStatus, it.LastModifyTime, it.LastModifyUserId }).ExecuteCommandAsync();

            schedule.Status = 2;
            await _scheduleRepository.AsSugarClient().Updateable(schedule)
                .CallEntityMethod(m => m.LastModify())
                .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId }).ExecuteCommandAsync();

            // 审计日志（操作类型2：发布）
            await WriteScheduleLog(id, order.OrderNo, 2, new
            {
                flowCardId = schedule.FlowCardId,
                flowCardNo = schedule.FlowCardNo,
                checkItems = new[] { "排产方案已保存并绑定流程卡", "来料检验/前道工序已完成" }
            });

            return new { message = "发布成功：流程卡绑定+来料检验校验通过，PDA端可见" };
        }

        /// <summary>
        /// 生产进度列表（订单维度关键信息，含进度百分比与当前工序）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetProgressList")]
        public async Task<dynamic> GetProgressList([FromBody] ProgressQuery input)
        {
            var whereLambda = LinqExpression.And<ProductionOrderEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.OrderNo))
            {
                whereLambda = whereLambda.And(m => m.OrderNo.Contains(input.OrderNo)
                    || m.DrawingNo.Contains(input.OrderNo)
                    || m.ProductName.Contains(input.OrderNo));
            }

            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.Status == input.Status);
            }

            var page = await _orderRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            // 批量取当前页订单的流程卡/工序流转/报工（进度聚合）
            var orderIds = page.list.Select(x => x.Id).ToList();
            var cards = new List<FlowCardEntity>();
            var processes = new List<FlowCardProcessEntity>();
            var reports = new List<WorkReportEntity>();
            if (orderIds.Any())
            {
                cards = await _flowCardRepository.AsQueryable()
                    .Where(m => orderIds.Contains(m.OrderId) && m.DeleteMark == null).ToListAsync();
                var cardIds = cards.Select(m => m.Id).ToList();
                if (cardIds.Any())
                {
                    processes = await _flowCardProcessRepository.AsQueryable()
                        .Where(m => cardIds.Contains(m.FlowCardId) && m.DeleteMark == null).ToListAsync();
                }

                reports = await _workReportRepository.AsQueryable()
                    .Where(m => orderIds.Contains(m.OrderId) && m.DeleteMark == null && m.Status == 1).ToListAsync();
            }

            var dtos = page.list.Select(m =>
            {
                var orderCards = cards.Where(c => c.OrderId == m.Id).ToList();
                var orderCardIds = orderCards.Select(c => c.Id).ToList();
                var orderProcesses = processes.Where(p => orderCardIds.Contains(p.FlowCardId)).ToList();

                // 进度 = Σ各工序报工良品 ÷ 任务总量（Σ每张卡定量×卡工序数）
                decimal totalTarget = 0;
                decimal totalDone = 0;
                foreach (var cardItem in orderCards)
                {
                    var cardProcesses = orderProcesses.Where(p => p.FlowCardId == cardItem.Id).ToList();
                    if (!cardProcesses.Any())
                    {
                        continue;
                    }

                    totalTarget += (cardItem.Qty ?? 0) * cardProcesses.Count;
                    totalDone += cardProcesses.Sum(p => p.CompleteQty ?? 0);
                }

                var percent = totalTarget > 0 ? Math.Round(totalDone / totalTarget * 100, 1) : 0;
                if (percent > 100)
                {
                    percent = 100;
                }

                var lastReport = reports.Where(r => r.OrderId == m.Id)
                    .OrderByDescending(r => r.EndTime ?? r.StartTime).FirstOrDefault();

                return new ProgressListDto
                {
                    Id = m.Id,
                    OrderNo = m.OrderNo,
                    DrawingNo = m.DrawingNo,
                    ProductName = m.ProductName,
                    PlanQty = m.PlanQty,
                    Unit = m.Unit,
                    PlanFinishDate = m.PlanFinishDate,
                    CurrentProcess = lastReport?.ProcessName,
                    ProgressPercent = percent,
                    Status = m.Status,
                    PublishStatus = m.PublishStatus
                };
            }).ToList();

            return new SqlSugarPagedList<ProgressListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 生产进度详情（工序流转聚合：完成数量/设备/报工人/时间）.
        /// </summary>
        /// <param name="orderId">订单主键.</param>
        /// <returns>订单进度详情.</returns>
        [HttpGet("GetProgressDetail")]
        public async Task<dynamic> GetProgressDetail(string orderId)
        {
            if (orderId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var order = await _orderRepository.GetFirstAsync(m => m.Id == orderId && m.DeleteMark == null);
            if (order == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var output = order.Adapt<ProgressDetailOutput>();

            var cards = await _flowCardRepository.AsQueryable()
                .Where(m => m.OrderId == orderId && m.DeleteMark == null).ToListAsync();
            var cardIds = cards.Select(m => m.Id).ToList();
            var processes = cardIds.Any()
                ? await _flowCardProcessRepository.AsQueryable()
                    .Where(m => cardIds.Contains(m.FlowCardId) && m.DeleteMark == null).ToListAsync()
                : new List<FlowCardProcessEntity>();
            var reports = await _workReportRepository.AsQueryable()
                .Where(m => m.OrderId == orderId && m.DeleteMark == null && m.Status == 1).ToListAsync();

            // 按工序聚合（跨流程卡合并），输出完成数量/设备/报工人/时间
            var groups = processes.Where(m => !m.ProcessId.IsNullOrEmpty()).GroupBy(m => m.ProcessId).ToList();
            foreach (var group in groups)
            {
                var flow = new ProgressFlowDto
                {
                    ProcessId = group.Key,
                    ProcessName = group.First().ProcessName,
                    SeqNo = group.Min(x => x.SeqNo),
                    TargetQty = group.Sum(x => x.TargetQty ?? 0),
                    CompleteQty = group.Sum(x => x.CompleteQty ?? 0)
                };

                var processReports = reports.Where(r => r.ProcessId == group.Key).ToList();
                flow.DeviceNames = string.Join("、", processReports.Select(r => r.DeviceName).Where(x => !x.IsNullOrEmpty()).Distinct());
                flow.WorkerNames = string.Join("、", processReports.Select(r => r.WorkerName).Where(x => !x.IsNullOrEmpty()).Distinct());
                flow.LastReportTime = processReports.Any() ? processReports.Max(r => r.EndTime ?? r.StartTime) : null;

                if (group.All(x => x.Status == 3))
                {
                    flow.Status = 3;
                }
                else if (group.Any(x => x.Status == 2) || flow.CompleteQty > 0)
                {
                    flow.Status = 2;
                }
                else
                {
                    flow.Status = 1;
                }

                output.Flows.Add(flow);
            }

            output.Flows = output.Flows.OrderBy(m => m.SeqNo ?? 0).ToList();
            return output;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 保存排产明细（整体覆盖，工序序号按工序首次出现顺序重排，回填工序/设备冗余）.
        /// </summary>
        /// <param name="schedule">排产主表.</param>
        /// <param name="input">排产入参.</param>
        /// <returns>无.</returns>
        private async Task SaveScheduleDetails(ProductionScheduleEntity schedule, ScheduleSaveInput input)
        {
            // 旧明细整体软删除
            var oldDetails = await _scheduleDetailRepository.AsQueryable()
                .Where(m => m.ScheduleId == schedule.Id && m.DeleteMark == null).ToListAsync();
            if (oldDetails.Any())
            {
                await _scheduleDetailRepository.AsSugarClient().Updateable(oldDetails)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            if (!input.Details.Any())
            {
                return;
            }

            // 回填工序名称（SeqNo 按工序首次出现顺序）
            var processIds = input.Details.Select(m => m.ProcessId).Where(m => !m.IsNullOrEmpty()).Distinct().ToList();
            var baseProcesses = processIds.Any()
                ? await _baseProcessRepository.AsQueryable().Where(m => processIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync()
                : new List<BaseProcessEntity>();
            var seqDict = processIds.Select((pid, idx) => new { pid, seq = idx + 1 }).ToDictionary(x => x.pid, x => x.seq);

            // 回填设备编码/名称
            var deviceIds = input.Details.Select(m => m.DeviceId).Where(m => !m.IsNullOrEmpty()).Distinct().ToList();
            var devices = deviceIds.Any()
                ? await _deviceRepository.AsQueryable().Where(m => deviceIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync()
                : new List<DeviceMasterEntity>();

            var newDetails = new List<ProductionScheduleDetailEntity>();
            foreach (var item in input.Details)
            {
                var baseProcess = baseProcesses.FirstOrDefault(m => m.Id == item.ProcessId);
                var device = devices.FirstOrDefault(m => m.Id == item.DeviceId);
                int? seqNo = null;
                if (!item.ProcessId.IsNullOrEmpty() && seqDict.ContainsKey(item.ProcessId))
                {
                    seqNo = seqDict[item.ProcessId];
                }

                newDetails.Add(new ProductionScheduleDetailEntity
                {
                    ScheduleId = schedule.Id,
                    OrderId = schedule.OrderId,
                    ProcessId = item.ProcessId,
                    ProcessName = baseProcess?.ProcessName,
                    SeqNo = seqNo,
                    DeviceId = item.DeviceId,
                    DeviceCode = device?.DeviceNo,
                    DeviceName = device?.DeviceName,
                    ProduceQty = item.ProduceQty,
                    Remark = item.Remark
                });
            }

            await _scheduleDetailRepository.AsSugarClient().Insertable(newDetails)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        /// <summary>
        /// 写订单操作审计日志（排产保存/发布/删除清理的快照）.
        /// </summary>
        /// <param name="orderId">订单ID.</param>
        /// <param name="orderNo">订单号.</param>
        /// <param name="operateType">操作类型：1排产保存 2发布 3删除清理.</param>
        /// <param name="content">快照内容.</param>
        /// <returns>无.</returns>
        private async Task WriteScheduleLog(string orderId, string orderNo, int operateType, object content)
        {
            var log = new ProductionScheduleLogEntity
            {
                OrderId = orderId,
                OrderNo = orderNo,
                OperateType = operateType,
                Content = JsonSerializer.Serialize(content)
            };
            await _scheduleLogRepository.AsSugarClient().Insertable(log)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        #endregion
    }
}
