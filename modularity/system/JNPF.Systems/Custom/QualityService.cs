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
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 品质管理（来料检验IQC/质量检验OQC）.
    /// IQC：PDA采购入库扫码时的来料检查记录（按到货单/批次关联）；免检规则自动跳过检查项并标记「免检」；
    /// 不合格批次自动触发不良处理流程（待处理→处理中→已处理）；
    /// OQC：PDA「品质抽检」扫码产生的抽检记录（按批抽样判定），与报工记录末道工序全检（检包检验记录）两套并行；
    /// 抽检合格后订单状态变为「待入库」。
    /// </summary>
    [ApiDescriptionSettings(Tag = "Quality", Name = "", Order = 211)]
    [Route("api/[controller]")]
    public class QualityService : IQualityService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 来料检验主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<IqcEntity> _iqcRepository;

        /// <summary>
        /// 来料检验明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<IqcDetailEntity> _iqcDetailRepository;

        /// <summary>
        /// 质量抽检主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<OqcEntity> _oqcRepository;

        /// <summary>
        /// 质量抽检明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<OqcDetailEntity> _oqcDetailRepository;

        /// <summary>
        /// 工序检查内容定义仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProcessCheckItemEntity> _processCheckItemRepository;

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
        private readonly ISqlSugarRepository<ProductionOrderEntity> _orderRepository;

        /// <summary>
        /// 存货主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryMasterEntity> _inventoryMasterRepository;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="QualityService"/>类型的新实例.
        /// </summary>
        public QualityService(
            ISqlSugarRepository<IqcEntity> iqcRepository,
            ISqlSugarRepository<IqcDetailEntity> iqcDetailRepository,
            ISqlSugarRepository<OqcEntity> oqcRepository,
            ISqlSugarRepository<OqcDetailEntity> oqcDetailRepository,
            ISqlSugarRepository<ProcessCheckItemEntity> processCheckItemRepository,
            ISqlSugarRepository<FlowCardEntity> flowCardRepository,
            ISqlSugarRepository<FlowCardProcessEntity> flowCardProcessRepository,
            ISqlSugarRepository<ProductionOrderEntity> orderRepository,
            ISqlSugarRepository<InventoryMasterEntity> inventoryMasterRepository,
            IUserManager userManager)
        {
            _iqcRepository = iqcRepository;
            _iqcDetailRepository = iqcDetailRepository;
            _oqcRepository = oqcRepository;
            _oqcDetailRepository = oqcDetailRepository;
            _processCheckItemRepository = processCheckItemRepository;
            _flowCardRepository = flowCardRepository;
            _flowCardProcessRepository = flowCardProcessRepository;
            _orderRepository = orderRepository;
            _inventoryMasterRepository = inventoryMasterRepository;
            _userManager = userManager;
        }

        #region 来料检验（IQC）

        /// <summary>
        /// 来料检验列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetIqcList")]
        public async Task<dynamic> GetIqcList([FromBody] IqcQuery input)
        {
            var whereLambda = LinqExpression.And<IqcEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.CheckNo))
            {
                whereLambda = whereLambda.And(m => m.CheckNo.Contains(input.CheckNo));
            }

            if (!string.IsNullOrEmpty(input.ArrivalNo))
            {
                whereLambda = whereLambda.And(m => m.ArrivalNo.Contains(input.ArrivalNo));
            }

            if (input.CheckResult != -1)
            {
                whereLambda = whereLambda.And(m => m.CheckResult == input.CheckResult);
            }

            var page = await _iqcRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            var dtos = page.list.Adapt<List<IqcListDto>>();
            return new SqlSugarPagedList<IqcListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 来料检验统计卡片（本月检验批次/合格率/免检批次/不合格批次）.
        /// </summary>
        /// <returns>统计结果.</returns>
        [HttpGet("GetIqcStatistics")]
        public async Task<dynamic> GetIqcStatistics()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var whereLambda = LinqExpression.And<IqcEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);
            whereLambda = whereLambda.And(m => (m.CheckTime != null && m.CheckTime >= monthStart)
                || (m.CheckTime == null && m.CreatorTime >= monthStart));

            var monthList = await _iqcRepository.AsQueryable().Where(whereLambda).ToListAsync();
            var passCount = monthList.Count(m => m.CheckResult == 1);
            var badCount = monthList.Count(m => m.CheckResult == 2);
            var freeCount = monthList.Count(m => m.CheckResult == 3);
            var judged = passCount + badCount;

            return new IqcStatisticsOutput
            {
                MonthCheckCount = monthList.Count,
                PassRate = judged > 0 ? Math.Round((decimal)passCount / judged * 100, 1) : 100,
                FreeCheckCount = freeCount,
                BadCount = badCount
            };
        }

        /// <summary>
        /// 来料检验明细（主表+检查项目实测值与判定）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>检验明细.</returns>
        [HttpGet("GetIqcDetail")]
        public async Task<dynamic> GetIqcDetail(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _iqcRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var items = await _iqcDetailRepository.AsQueryable()
                .Where(m => m.IqcId == id && m.DeleteMark == null)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            var output = entity.Adapt<IqcDetailOutput>();
            output.Items = items.Adapt<List<CheckDetailItemOutput>>();

            // 单位（取自存货主表）
            if (!entity.InventoryId.IsNullOrEmpty())
            {
                var inventory = await _inventoryMasterRepository.GetFirstAsync(m => m.Id == entity.InventoryId && m.DeleteMark == null);
                output.Unit = inventory?.Unit;
            }

            return output;
        }

        /// <summary>
        /// 来料检验登记（PDA采购入库扫码；免检自动跳过检查项并标记免检；不合格自动进入待处理）.
        /// </summary>
        /// <param name="input">登记入参.</param>
        /// <returns>登记结果.</returns>
        [HttpPost("RegisterIqc")]
        public async Task<dynamic> RegisterIqc([FromBody] IqcRegisterInput input)
        {
            var now = DateTime.Now;
            var details = input.CheckType == 2
                ? new List<CheckDetailItemInput>()
                : (input.Details ?? new List<CheckDetailItemInput>());

            int checkResult;
            int badHandleStatus;
            string summary;
            if (input.CheckType == 2)
            {
                // 免检规则：自动跳过检查项，检验方式标记「免检」
                checkResult = 3;
                badHandleStatus = 0;
                summary = "（供应商免检规则）";
            }
            else
            {
                if (!details.Any())
                {
                    throw Oops.Oh(ErrorCode.COM1018, "扫码检查至少录入一项检查项目");
                }

                var hasNg = details.Any(m => "NG".Equals((m.CheckResult ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
                checkResult = hasNg ? 2 : 1;
                badHandleStatus = hasNg ? 1 : 0; // 不合格自动进入「待处理」
                summary = string.Join(" / ", details.Select(m => $"{m.CheckItem} {m.CheckResult}"));
            }

            // 检验单号：IQC + 日期 + 3位序号
            var prefix = "IQC" + now.ToString("yyyyMMdd");
            var seq = await _iqcRepository.AsQueryable()
                .Where(m => m.CheckNo.StartsWith(prefix)).CountAsync() + 1;
            var checkNo = prefix + seq.ToString("D3");
            while (await _iqcRepository.IsAnyAsync(m => m.CheckNo == checkNo))
            {
                seq++;
                checkNo = prefix + seq.ToString("D3");
            }

            var entity = new IqcEntity
            {
                CheckNo = checkNo,
                ArrivalId = input.ArrivalId,
                ArrivalNo = input.ArrivalNo,
                ArrivalDetailId = input.ArrivalDetailId,
                SupplierId = input.SupplierId,
                SupplierName = input.SupplierName,
                BatchNo = input.BatchNo,
                InventoryId = input.InventoryId,
                InventoryCode = input.InventoryCode,
                InventoryName = input.InventoryName,
                CheckType = input.CheckType,
                CheckItemsSummary = summary,
                CheckQty = input.CheckQty ?? 0,
                BadQty = input.BadQty ?? 0,
                CheckResult = checkResult,
                CheckerName = input.CheckerName.IsNullOrEmpty() ? _userManager.RealName : input.CheckerName,
                CheckTime = input.CheckTime ?? now,
                BadHandleStatus = badHandleStatus,
                Remark = input.Remark
            };

            var insertOk = await _iqcRepository.AsSugarClient().Insertable(entity)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 检查项目明细（免检时跳过）
            if (details.Any())
            {
                var rows = details.Select(m => new IqcDetailEntity
                {
                    IqcId = entity.Id,
                    CheckItem = m.CheckItem,
                    CheckStandard = m.CheckStandard,
                    ActualValue = m.ActualValue,
                    CheckResult = m.CheckResult,
                    Remark = m.Remark
                }).ToList();
                await _iqcDetailRepository.AsSugarClient().Insertable(rows)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            var message = checkResult == 3
                ? "免检登记完成：已跳过检查项并标记「免检」，允许入库"
                : checkResult == 1
                    ? "检验合格：允许入库，记录推送「入库记录」并同步用友采购入库单"
                    : "检验不合格：已自动进入不良处理流程（待处理）";

            return new { checkNo, checkResult, badHandleStatus, message };
        }

        /// <summary>
        /// 来料检验不良处理记录（待处理→处理中→已处理）.
        /// </summary>
        /// <param name="input">处理入参.</param>
        /// <returns>无.</returns>
        [HttpPost("HandleIqcBad")]
        public async Task HandleIqcBad([FromBody] BadHandleInput input)
        {
            if (input.Id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _iqcRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            entity.BadHandleStatus = input.BadHandleStatus;
            entity.BadHandleRemark = input.BadHandleRemark;
            int isOk = await _iqcRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.LastModify())
                .UpdateColumns(it => new { it.BadHandleStatus, it.BadHandleRemark, it.LastModifyTime, it.LastModifyUserId })
                .ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1001);
            }
        }

        #endregion

        #region 质量检验（OQC抽检）

        /// <summary>
        /// 质量抽检列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetOqcList")]
        public async Task<dynamic> GetOqcList([FromBody] OqcQuery input)
        {
            var whereLambda = LinqExpression.And<OqcEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.CheckNo))
            {
                whereLambda = whereLambda.And(m => m.CheckNo.Contains(input.CheckNo));
            }

            if (!string.IsNullOrEmpty(input.FlowCardNo))
            {
                whereLambda = whereLambda.And(m => m.FlowCardNo.Contains(input.FlowCardNo));
            }

            if (!string.IsNullOrEmpty(input.ProcessName))
            {
                whereLambda = whereLambda.And(m => m.ProcessName.Contains(input.ProcessName));
            }

            if (input.CheckResult != -1)
            {
                whereLambda = whereLambda.And(m => m.CheckResult == input.CheckResult);
            }

            var page = await _oqcRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            var dtos = page.list.Adapt<List<OqcListDto>>();
            return new SqlSugarPagedList<OqcListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 质量抽检统计卡片（今日抽检批次/合格率/待检包检查/不良批次）.
        /// </summary>
        /// <returns>统计结果.</returns>
        [HttpGet("GetOqcStatistics")]
        public async Task<dynamic> GetOqcStatistics()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var today = now.Date;

            var whereLambda = LinqExpression.And<OqcEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);
            whereLambda = whereLambda.And(m => (m.CheckTime != null && m.CheckTime >= monthStart)
                || (m.CheckTime == null && m.CreatorTime >= monthStart));

            var monthList = await _oqcRepository.AsQueryable().Where(whereLambda).ToListAsync();
            var todayCount = monthList.Count(m => (m.CheckTime ?? m.CreatorTime)?.Date == today);
            var passCount = monthList.Count(m => m.CheckResult == 1);
            var badCount = monthList.Count(m => m.CheckResult == 2);
            var judged = passCount + badCount;

            // 待检包检查：生产中（状态2）且检查工序未完成的流程卡数
            var prodCardIds = await _flowCardRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.Status == 2)
                .Select(m => m.Id).ToListAsync();
            var checkRows = await _flowCardProcessRepository.AsQueryable()
                .Where(m => m.IsCheckProcess == 1 && m.DeleteMark == null).ToListAsync();
            var waitCheckCount = checkRows
                .Where(m => prodCardIds.Contains(m.FlowCardId) && m.Status != 3)
                .Select(m => m.FlowCardId).Distinct().Count();

            return new OqcStatisticsOutput
            {
                TodayCount = todayCount,
                PassRate = judged > 0 ? Math.Round((decimal)passCount / judged * 100, 1) : 100,
                WaitCheckCount = waitCheckCount,
                BadCount = badCount
            };
        }

        /// <summary>
        /// 质量抽检明细（主表+检查项目实测值与判定）.
        /// </summary>
        /// <param name="id">主键.</param>
        /// <returns>抽检明细.</returns>
        [HttpGet("GetOqcDetail")]
        public async Task<dynamic> GetOqcDetail(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _oqcRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var items = await _oqcDetailRepository.AsQueryable()
                .Where(m => m.OqcId == id && m.DeleteMark == null)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            var output = entity.Adapt<OqcDetailOutput>();
            output.Items = items.Adapt<List<CheckDetailItemOutput>>();

            // 产品（取自流程卡）
            if (!entity.FlowCardId.IsNullOrEmpty())
            {
                var card = await _flowCardRepository.GetFirstAsync(m => m.Id == entity.FlowCardId && m.DeleteMark == null);
                output.ProductName = card?.ProductName;
            }

            return output;
        }

        /// <summary>
        /// 质量抽检登记（PDA品质抽检扫码；按批抽样判定；合格后联动订单「待入库」；不合格自动进入待处理）.
        /// </summary>
        /// <param name="input">登记入参.</param>
        /// <returns>登记结果.</returns>
        [HttpPost("RegisterOqc")]
        public async Task<dynamic> RegisterOqc([FromBody] OqcRegisterInput input)
        {
            if (input.FlowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请扫码流程卡");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在，请先扫码确认");
            }

            var details = input.Details ?? new List<CheckDetailItemInput>();
            if (!details.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "至少录入一项检查项目");
            }

            var hasNg = details.Any(m => "NG".Equals((m.CheckResult ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
            var checkResult = hasNg ? 2 : 1;
            var badHandleStatus = hasNg ? 1 : 0; // 不合格自动进入「待处理」
            var summary = string.Join(" / ", details.Select(m => $"{m.CheckItem} {m.CheckResult}"));

            var now = DateTime.Now;
            var prefix = "OQC" + now.ToString("yyyyMMdd");
            var seq = await _oqcRepository.AsQueryable()
                .Where(m => m.CheckNo.StartsWith(prefix)).CountAsync() + 1;
            var checkNo = prefix + seq.ToString("D3");
            while (await _oqcRepository.IsAnyAsync(m => m.CheckNo == checkNo))
            {
                seq++;
                checkNo = prefix + seq.ToString("D3");
            }

            var entity = new OqcEntity
            {
                CheckNo = checkNo,
                FlowCardId = card.Id,
                FlowCardNo = card.CardNo,
                OrderId = card.OrderId,
                OrderNo = card.OrderNo,
                ProcessId = input.ProcessId,
                ProcessName = input.ProcessName,
                CheckType = 2, // 抽检（按批抽样）
                CheckItemsSummary = summary,
                SampleQty = input.SampleQty ?? 0,
                BadQty = input.BadQty ?? 0,
                CheckResult = checkResult,
                CheckerName = input.CheckerName.IsNullOrEmpty() ? _userManager.RealName : input.CheckerName,
                CheckTime = input.CheckTime ?? now,
                ReportId = input.ReportId,
                BadHandleStatus = badHandleStatus,
                Remark = input.Remark
            };

            var insertOk = await _oqcRepository.AsSugarClient().Insertable(entity)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            var rows = details.Select(m => new OqcDetailEntity
            {
                OqcId = entity.Id,
                CheckItem = m.CheckItem,
                CheckStandard = m.CheckStandard,
                ActualValue = m.ActualValue,
                CheckResult = m.CheckResult,
                Remark = m.Remark
            }).ToList();
            await _oqcDetailRepository.AsSugarClient().Insertable(rows)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

            // 判定合格 → 订单状态更新为「待入库」，可进行PDA产成品入库
            if (checkResult == 1 && !card.OrderId.IsNullOrEmpty())
            {
                var order = await _orderRepository.GetFirstAsync(m => m.Id == card.OrderId && m.DeleteMark == null);
                if (order != null && (order.Status == null || order.Status < 5))
                {
                    order.Status = 5; // 待入库
                    await _orderRepository.AsSugarClient().Updateable(order)
                        .CallEntityMethod(m => m.LastModify())
                        .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId })
                        .ExecuteCommandAsync();
                }
            }

            var message = checkResult == 1
                ? "抽检合格：订单状态已更新为「待入库」，可进行PDA产成品入库"
                : "抽检不合格：已自动进入不良处理流程（待处理）";

            return new { checkNo, checkResult, badHandleStatus, message };
        }

        /// <summary>
        /// 质量抽检不良处理记录（待处理→处理中→已处理）.
        /// </summary>
        /// <param name="input">处理入参.</param>
        /// <returns>无.</returns>
        [HttpPost("HandleOqcBad")]
        public async Task HandleOqcBad([FromBody] BadHandleInput input)
        {
            if (input.Id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _oqcRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            entity.BadHandleStatus = input.BadHandleStatus;
            entity.BadHandleRemark = input.BadHandleRemark;
            int isOk = await _oqcRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.LastModify())
                .UpdateColumns(it => new { it.BadHandleStatus, it.BadHandleRemark, it.LastModifyTime, it.LastModifyUserId })
                .ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1001);
            }
        }

        /// <summary>
        /// 工序检查内容定义（PDA检查项目表按工序带出）.
        /// </summary>
        /// <param name="processId">工序ID.</param>
        /// <returns>检查项目定义列表.</returns>
        [HttpGet("GetProcessCheckItems")]
        public async Task<dynamic> GetProcessCheckItems(string processId)
        {
            if (processId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请选择工序");
            }

            var items = await _processCheckItemRepository.AsQueryable()
                .Where(m => m.ProcessId == processId && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            return items.Select(m => new CheckItemDefineOutput
            {
                Id = m.Id,
                ProcessId = m.ProcessId,
                CheckItem = m.CheckItem,
                CheckStandard = m.CheckStandard,
                DataType = m.DataType,
                Options = m.OptionItems,
                DefaultValue = m.DefaultValue,
                IsRequired = m.Required,
                SeqNo = m.SeqNo
            }).ToList();
        }

        #endregion
    }
}
