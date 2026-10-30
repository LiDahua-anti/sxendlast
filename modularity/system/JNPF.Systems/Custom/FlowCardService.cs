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
    /// 流程卡打印（合并/拆分/重印）.
    /// 生产过程中存在流程卡合并、拆分与重印：多张旧卡合并为一张新卡、一张旧卡拆分为多张新卡，
    /// 或损坏/变更后重印；新流程卡与旧流程卡建立追溯关系（可上溯/下钻追溯来源）。
    /// </summary>
    [ApiDescriptionSettings(Tag = "FlowCard", Name = "", Order = 208)]
    [Route("api/[controller]")]
    public class FlowCardService : IFlowCardService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 流程卡仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardEntity> _flowCardRepository;

        /// <summary>
        /// 流程卡追溯关系仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardRelationEntity> _flowCardRelationRepository;

        /// <summary>
        /// 流程卡工序流转仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardProcessEntity> _flowCardProcessRepository;

        /// <summary>
        /// 排产主表仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionScheduleEntity> _scheduleRepository;

        /// <summary>
        /// 排产明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionScheduleDetailEntity> _scheduleDetailRepository;

        /// <summary>
        /// 基础工序仓储.
        /// </summary>
        private readonly ISqlSugarRepository<BaseProcessEntity> _baseProcessRepository;

        /// <summary>
        /// 流程卡不良记录仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardDefectEntity> _defectRepository;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="FlowCardService"/>类型的新实例.
        /// </summary>
        public FlowCardService(
            ISqlSugarRepository<FlowCardEntity> flowCardRepository,
            ISqlSugarRepository<FlowCardRelationEntity> flowCardRelationRepository,
            ISqlSugarRepository<FlowCardProcessEntity> flowCardProcessRepository,
            ISqlSugarRepository<ProductionScheduleEntity> scheduleRepository,
            ISqlSugarRepository<ProductionScheduleDetailEntity> scheduleDetailRepository,
            ISqlSugarRepository<BaseProcessEntity> baseProcessRepository,
            ISqlSugarRepository<FlowCardDefectEntity> defectRepository,
            IUserManager userManager)
        {
            _flowCardRepository = flowCardRepository;
            _flowCardRelationRepository = flowCardRelationRepository;
            _flowCardProcessRepository = flowCardProcessRepository;
            _scheduleRepository = scheduleRepository;
            _scheduleDetailRepository = scheduleDetailRepository;
            _baseProcessRepository = baseProcessRepository;
            _defectRepository = defectRepository;
            _userManager = userManager;
        }

        #region 流程卡列表与打印

        /// <summary>
        /// 获取流程卡列表（分页，含关联旧流程卡）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetFlowCardList")]
        public async Task<dynamic> GetFlowCardList([FromBody] FlowCardQuery input)
        {
            var whereLambda = LinqExpression.And<FlowCardEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.CardNo))
            {
                whereLambda = whereLambda.And(m => m.CardNo.Contains(input.CardNo));
            }

            if (input.CardType != -1)
            {
                whereLambda = whereLambda.And(m => m.CardType == input.CardType);
            }

            if (!string.IsNullOrEmpty(input.OrderNo))
            {
                whereLambda = whereLambda.And(m => m.OrderNo.Contains(input.OrderNo));
            }

            var page = await _flowCardRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            // 当前页流程卡的关联旧卡（合并/拆分/重印追溯）
            var cardIds = page.list.Select(m => m.Id).ToList();
            var relations = cardIds.Any()
                ? await _flowCardRelationRepository.AsQueryable()
                    .Where(m => cardIds.Contains(m.NewCardId) && m.DeleteMark == null).ToListAsync()
                : new List<FlowCardRelationEntity>();

            var dtos = page.list.Select(m => new FlowCardListDto
            {
                Id = m.Id,
                CardNo = m.CardNo,
                CardType = m.CardType,
                OldCardNos = string.Join(",", relations.Where(r => r.NewCardId == m.Id).Select(r => r.OldCardNo).Where(x => !x.IsNullOrEmpty())),
                OrderNo = m.OrderNo,
                ProductName = m.ProductName,
                Qty = m.Qty,
                Unit = m.Unit,
                PrintUser = m.PrintUser,
                PrintTime = m.PrintTime,
                Status = m.Status
            }).ToList();

            return new SqlSugarPagedList<FlowCardListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 打印流程卡（原卡打印/合并多旧→一新/拆分一旧→多新/重印关联原卡，写追溯关系）.
        /// </summary>
        /// <param name="input">打印入参.</param>
        /// <returns>打印结果.</returns>
        [HttpPost("PrintFlowCard")]
        public async Task<dynamic> PrintFlowCard([FromBody] FlowCardPrintInput input)
        {
            var printType = input.PrintType;
            if (printType < 1 || printType > 4)
            {
                throw Oops.Oh(ErrorCode.COM1018, "打印类型不正确");
            }

            if (input.NewCardCount <= 0)
            {
                input.NewCardCount = 1;
            }

            if (input.NewCardCount > 20)
            {
                throw Oops.Oh(ErrorCode.COM1018, "新卡数量不能超过20张");
            }

            // 旧卡校验（合并可多选；拆分/重印仅一张）
            var oldCards = new List<FlowCardEntity>();
            if (input.OldCardIds != null && input.OldCardIds.Any())
            {
                var ids = input.OldCardIds.Where(x => !x.IsNullOrEmpty()).Distinct().ToList();
                if (ids.Any())
                {
                    oldCards = await _flowCardRepository.AsQueryable()
                        .Where(m => ids.Contains(m.Id) && m.DeleteMark == null).ToListAsync();
                    if (oldCards.Count != ids.Count)
                    {
                        throw Oops.Oh(ErrorCode.COM1018, "所选旧流程卡不存在或已删除，请重新选择");
                    }

                    // 保持用户选择顺序
                    oldCards = ids.Select(id => oldCards.First(m => m.Id == id)).ToList();
                }
            }

            if (printType == 2 && !oldCards.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "合并流程卡至少选择一张旧卡");
            }

            if (printType == 3 && oldCards.Count != 1)
            {
                throw Oops.Oh(ErrorCode.COM1018, "拆分流程卡必须选择一张旧卡");
            }

            if (printType == 4)
            {
                if (oldCards.Count != 1)
                {
                    throw Oops.Oh(ErrorCode.COM1018, "重印流程卡必须关联一张原卡");
                }

                input.NewCardCount = 1;
            }

            // 继承字段（未填时取旧卡）
            var first = oldCards.FirstOrDefault();
            var orderId = !input.OrderId.IsNullOrEmpty() ? input.OrderId : first?.OrderId;
            var orderNo = !input.OrderNo.IsNullOrEmpty() ? input.OrderNo : first?.OrderNo;
            var productName = !input.ProductName.IsNullOrEmpty() ? input.ProductName : first?.ProductName;
            var drawingNo = !input.DrawingNo.IsNullOrEmpty() ? input.DrawingNo : first?.DrawingNo;
            var unit = !input.Unit.IsNullOrEmpty() ? input.Unit : first?.Unit;

            // 单卡数量口径：合并默认=旧卡合计；拆分默认=旧卡均分；重印默认=原卡
            decimal? qty = input.Qty;
            var oldSum = oldCards.Sum(m => m.Qty ?? 0);
            if (qty == null || qty <= 0)
            {
                if (printType == 2)
                {
                    qty = oldSum;
                }
                else if (printType == 3)
                {
                    qty = Math.Round(oldSum / input.NewCardCount, 4);
                }
                else
                {
                    qty = first?.Qty;
                }
            }

            // 生成新卡
            var printUser = _userManager.GetUserName(_userManager.UserId);
            var now = DateTime.Now;
            var baseNo = BuildCardNoBase(printType);
            var newCards = new List<FlowCardEntity>();
            for (var i = 0; i < input.NewCardCount; i++)
            {
                var itemBase = input.NewCardCount == 1 ? baseNo : baseNo + "-" + (i + 1).ToString("D2");
                var cardNo = itemBase;
                var seq = 1;
                while (await _flowCardRepository.IsAnyAsync(m => m.CardNo == cardNo))
                {
                    seq++;
                    cardNo = itemBase + "-" + seq;
                }

                newCards.Add(new FlowCardEntity
                {
                    CardNo = cardNo,
                    CardType = printType,
                    OrderId = orderId,
                    OrderNo = orderNo,
                    ProductName = productName,
                    DrawingNo = drawingNo,
                    Qty = qty,
                    Unit = unit,
                    PrintUser = printUser,
                    PrintTime = now,
                    Status = printType == 4 ? (first?.Status ?? 1) : 1,
                    Remark = input.Remark
                });
            }

            var insertOk = await _flowCardRepository.AsSugarClient().Insertable(newCards)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 合并/拆分/重印：写追溯关系（新卡→旧卡，类型：1合并 2拆分 3重印）
            if (printType >= 2 && oldCards.Any())
            {
                var relations = new List<FlowCardRelationEntity>();
                foreach (var newCard in newCards)
                {
                    foreach (var old in oldCards)
                    {
                        relations.Add(new FlowCardRelationEntity
                        {
                            NewCardId = newCard.Id,
                            NewCardNo = newCard.CardNo,
                            OldCardId = old.Id,
                            OldCardNo = old.CardNo,
                            RelationType = printType - 1,
                            Remark = GetCardTypeName(printType) + "打印建立追溯关系"
                        });
                    }
                }

                await _flowCardRelationRepository.AsSugarClient().Insertable(relations)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            var message = printType == 1
                ? $"流程卡已打印：{string.Join("、", newCards.Select(m => m.CardNo))}"
                : printType == 2
                    ? $"流程卡已打印，已与旧卡建立关联（来源旧卡合计 {oldSum}{unit}）"
                    : printType == 3
                        ? "流程卡已打印（拆分），已与旧卡建立关联"
                        : "流程卡已重印，已关联原卡";

            return new FlowCardPrintOutput
            {
                NewCardIds = newCards.Select(m => m.Id).ToList(),
                NewCardNos = newCards.Select(m => m.CardNo).ToList(),
                Message = message
            };
        }

        #endregion

        #region 追溯与扫码查询

        /// <summary>
        /// 流程卡关联追溯（当前卡来源旧卡，可上溯/下钻）.
        /// </summary>
        /// <param name="id">流程卡主键.</param>
        /// <returns>追溯关系.</returns>
        [HttpGet("GetFlowCardRelation")]
        public async Task<dynamic> GetFlowCardRelation(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var relations = await _flowCardRelationRepository.AsQueryable()
                .Where(m => m.NewCardId == id && m.DeleteMark == null).ToListAsync();
            var oldIds = relations.Select(m => m.OldCardId).Where(m => !m.IsNullOrEmpty()).Distinct().ToList();
            var oldCards = oldIds.Any()
                ? await _flowCardRepository.AsQueryable().Where(m => oldIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync()
                : new List<FlowCardEntity>();

            // 旧卡自身的来源（追溯链路上一级）
            var parentRelations = oldIds.Any()
                ? await _flowCardRelationRepository.AsQueryable().Where(m => oldIds.Contains(m.NewCardId) && m.DeleteMark == null).ToListAsync()
                : new List<FlowCardRelationEntity>();

            var output = new FlowCardRelationOutput
            {
                Id = card.Id,
                CardNo = card.CardNo,
                CardType = card.CardType,
                Qty = card.Qty,
                Unit = card.Unit,
                OrderNo = card.OrderNo,
                ProductName = card.ProductName,
                PrintUser = card.PrintUser,
                PrintTime = card.PrintTime,
                OldCards = oldCards.Select(m => new FlowCardRelationItemDto
                {
                    Id = m.Id,
                    CardNo = m.CardNo,
                    CardType = m.CardType,
                    Qty = m.Qty,
                    Unit = m.Unit,
                    PrintUser = m.PrintUser,
                    PrintTime = m.PrintTime,
                    OldCardNos = string.Join(" + ", parentRelations.Where(r => r.NewCardId == m.Id).Select(r => r.OldCardNo).Where(x => !x.IsNullOrEmpty()))
                }).ToList()
            };

            output.TraceNote = output.OldCards.Any()
                ? $"追溯链路：{string.Join(" + ", output.OldCards.Select(m => $"{m.CardNo}（{GetCardTypeName(m.CardType)} {m.Qty}{m.Unit}）"))} → {card.CardNo}（{GetCardTypeName(card.CardType)} {card.Qty}{card.Unit}）"
                : $"追溯链路：{card.CardNo}（{GetCardTypeName(card.CardType)}），无来源旧卡";

            return output;
        }

        /// <summary>
        /// 按流程卡号扫码查询（供PDA：卡信息+工序流转列表；未初始化时自动按排产初始化工序流转）.
        /// </summary>
        /// <param name="cardNo">流程卡号.</param>
        /// <returns>卡信息与工序流转.</returns>
        [HttpGet("pad/GetFlowCardByCardNo")]
        public async Task<dynamic> GetFlowCardByCardNo(string cardNo)
        {
            if (cardNo.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请扫码或输入流程卡号");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.CardNo == cardNo && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在，请确认卡号");
            }

            var processes = await _flowCardProcessRepository.AsQueryable()
                .Where(m => m.FlowCardId == card.Id && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            // 已绑订单但未初始化工序流转时自动初始化（扫码即用）
            if (!processes.Any() && !card.OrderId.IsNullOrEmpty())
            {
                try
                {
                    await InitFlowCardProcesses(card.Id);
                    processes = await _flowCardProcessRepository.AsQueryable()
                        .Where(m => m.FlowCardId == card.Id && m.DeleteMark == null)
                        .OrderBy(m => m.SeqNo, OrderByType.Asc)
                        .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                        .ToListAsync();
                }
                catch
                {
                    // 未完成排产时忽略自动初始化，扫码仍返回卡信息
                }
            }

            return new FlowCardScanOutput
            {
                Id = card.Id,
                CardNo = card.CardNo,
                CardType = card.CardType,
                OrderId = card.OrderId,
                OrderNo = card.OrderNo,
                ProductName = card.ProductName,
                DrawingNo = card.DrawingNo,
                Qty = card.Qty,
                Unit = card.Unit,
                Status = card.Status,
                Processes = processes.Select(m => new FlowCardProcessItemDto
                {
                    Id = m.Id,
                    ProcessId = m.ProcessId,
                    ProcessCode = m.ProcessCode,
                    ProcessName = m.ProcessName,
                    SeqNo = m.SeqNo,
                    IsCheckProcess = m.IsCheckProcess,
                    Status = m.Status,
                    TargetQty = m.TargetQty,
                    CompleteQty = m.CompleteQty
                }).ToList()
            };
        }

        /// <summary>
        /// 初始化流程卡工序流转（按订单排产明细生成各工序流转，PDA报工前调用）.
        /// </summary>
        /// <param name="flowCardId">流程卡主键.</param>
        /// <returns>初始化结果.</returns>
        [HttpGet("pad/InitFlowCardProcesses")]
        public async Task<dynamic> InitFlowCardProcesses(string flowCardId)
        {
            if (flowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == flowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在");
            }

            var existing = await _flowCardProcessRepository.AsQueryable()
                .Where(m => m.FlowCardId == flowCardId && m.DeleteMark == null).CountAsync();
            if (existing > 0)
            {
                return new { initialized = false, count = existing, message = "该流程卡工序流转已存在，无需重复初始化" };
            }

            if (card.OrderId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "该流程卡未绑定生产订单，无法初始化工序流转");
            }

            var schedule = await _scheduleRepository.GetFirstAsync(m => m.OrderId == card.OrderId && m.DeleteMark == null);
            if (schedule == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "该订单尚未保存排产方案，请先完成排产");
            }

            var details = await _scheduleDetailRepository.AsQueryable()
                .Where(m => m.ScheduleId == schedule.Id && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();
            var processIds = details.Select(m => m.ProcessId).Where(m => !m.IsNullOrEmpty()).Distinct().ToList();
            if (!processIds.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "排产方案无工序明细，请先保存排产");
            }

            var baseProcesses = await _baseProcessRepository.AsQueryable()
                .Where(m => processIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync();

            // 按工序顺序生成流转行；「检查」类型工序标记为检查工序（最后一步检查，报工需填检包检验记录）
            var rows = new List<FlowCardProcessEntity>();
            for (var i = 0; i < processIds.Count; i++)
            {
                var pid = processIds[i];
                var bp = baseProcesses.FirstOrDefault(m => m.Id == pid);
                var isCheck = (bp != null && !bp.ProcessType.IsNullOrEmpty() && bp.ProcessType.Contains("检查"))
                    || (bp != null && !bp.ProcessName.IsNullOrEmpty() && (bp.ProcessName.Contains("检查") || bp.ProcessName.Contains("检包")));

                rows.Add(new FlowCardProcessEntity
                {
                    FlowCardId = card.Id,
                    FlowCardNo = card.CardNo,
                    ProcessId = pid,
                    ProcessCode = bp?.ProcessCode,
                    ProcessName = bp?.ProcessName,
                    SeqNo = i + 1,
                    IsCheckProcess = isCheck ? 1 : 0,
                    Status = 1,
                    TargetQty = card.Qty ?? 0,
                    CompleteQty = 0
                });
            }

            var insertOk = await _flowCardProcessRepository.AsSugarClient().Insertable(rows)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            return new { initialized = true, count = rows.Count, message = $"已初始化 {rows.Count} 道工序流转，PDA可开始报工" };
        }

        #endregion

        #region PDA接口

        /// <summary>
        /// 流程卡不良处理（PDA生产过程中不良记录与处理；报废数量从流程卡可报工上限中扣减；记录修改人和时间）.
        /// </summary>
        /// <param name="input">不良处理入参.</param>
        /// <returns>不良处理结果.</returns>
        [HttpPost("pad/HandleFlowCardDefect")]
        public async Task<dynamic> HandleFlowCardDefect([FromBody] FlowCardDefectInput input)
        {
            if (input.FlowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请扫描流程卡");
            }

            if (input.DefectQty <= 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, "不良数量必须大于0");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在");
            }

            // 工序流转匹配
            FlowCardProcessEntity cardProcess = null;
            if (!input.CardProcessId.IsNullOrEmpty())
            {
                cardProcess = await _flowCardProcessRepository.GetFirstAsync(m => m.Id == input.CardProcessId && m.DeleteMark == null);
            }

            if (cardProcess == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "未找到对应工序流转，请确认工序");
            }

            var now = DateTime.Now;
            var handlerName = _userManager.RealName;
            var dispositionName = input.Disposition == 1 ? "报废" : input.Disposition == 2 ? "返工" : "让步接收";

            // 不良记录
            var defect = new FlowCardDefectEntity
            {
                FlowCardId = card.Id,
                FlowCardNo = card.CardNo,
                CardProcessId = cardProcess.Id,
                ProcessId = cardProcess.ProcessId,
                ProcessName = cardProcess.ProcessName,
                DefectType = input.DefectType,
                DefectQty = input.DefectQty,
                DefectDescription = input.DefectDescription,
                Disposition = input.Disposition,
                HandleRemark = input.HandleRemark,
                HandlerName = handlerName,
                HandleTime = now,
                OrderId = card.OrderId,
                OrderNo = card.OrderNo
            };

            var insertOk = await _defectRepository.AsSugarClient().Insertable(defect)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 报废处理：从流程卡可报工上限中扣减（目标数量减少）
            if (input.Disposition == 1)
            {
                cardProcess.TargetQty = Math.Max(0, (cardProcess.TargetQty ?? 0) - input.DefectQty);
                await _flowCardProcessRepository.AsSugarClient().Updateable(cardProcess)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.TargetQty, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();
            }

            return new FlowCardDefectOutput
            {
                DefectId = defect.Id,
                CardNo = card.CardNo,
                ProcessName = cardProcess.ProcessName,
                DefectQty = input.DefectQty,
                DispositionName = dispositionName,
                Message = $"不良处理成功：{cardProcess.ProcessName} 不良 {input.DefectQty} 件，处理方式：{dispositionName}"
            };
        }

        /// <summary>
        /// 流程卡不良记录列表（PDA查看历史不良记录）.
        /// </summary>
        /// <param name="flowCardId">流程卡主键.</param>
        /// <returns>不良记录列表.</returns>
        [HttpGet("pad/GetFlowCardDefectList")]
        public async Task<dynamic> GetFlowCardDefectList(string flowCardId)
        {
            if (flowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var defects = await _defectRepository.AsQueryable()
                .Where(m => m.FlowCardId == flowCardId && m.DeleteMark == null)
                .OrderBy(m => m.HandleTime, OrderByType.Desc)
                .ToListAsync();

            return defects.Select(m => new
            {
                m.Id,
                m.FlowCardId,
                m.FlowCardNo,
                m.CardProcessId,
                m.ProcessId,
                m.ProcessName,
                m.DefectType,
                m.DefectQty,
                m.DefectDescription,
                m.Disposition,
                DispositionName = m.Disposition == 1 ? "报废" : m.Disposition == 2 ? "返工" : "让步接收",
                m.HandleRemark,
                m.HandlerName,
                m.HandleTime,
                m.CreatorTime
            }).ToList();
        }

        /// <summary>
        /// 流程卡作废（PDA/WEB均可调用；仅「未使用」或「生产中」状态可作废；已入库/已作废不可操作）.
        /// </summary>
        /// <param name="id">流程卡主键.</param>
        /// <returns>作废结果.</returns>
        [HttpGet("pad/CancelFlowCard")]
        public async Task<dynamic> CancelFlowCard(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在");
            }

            if (card.Status == 4)
            {
                throw Oops.Oh(ErrorCode.COM1018, "已入库的流程卡不可作废");
            }

            if (card.Status == 5)
            {
                throw Oops.Oh(ErrorCode.COM1018, "该流程卡已作废，无需重复操作");
            }

            var oldStatus = card.Status;
            card.Status = 5;
            var updateOk = await _flowCardRepository.AsSugarClient().Updateable(card)
                .CallEntityMethod(m => m.LastModify())
                .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId })
                .ExecuteCommandAsync();

            if (!(updateOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            var statusName = oldStatus == 1 ? "未使用" : oldStatus == 2 ? "生产中" : oldStatus == 3 ? "已完工" : "未知";
            return new
            {
                id = card.Id,
                cardNo = card.CardNo,
                oldStatus,
                message = $"流程卡 {card.CardNo} 已作废（原状态：{statusName}）"
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 生成流程卡号前缀（按类型区分：原卡 LCK-、合并 LCK-M-、拆分 LCK-S-、重印 LCK-R-）.
        /// </summary>
        /// <param name="printType">打印类型.</param>
        /// <returns>卡号前缀.</returns>
        private static string BuildCardNoBase(int printType)
        {
            var prefix = printType == 2 ? "LCK-M-" : printType == 3 ? "LCK-S-" : printType == 4 ? "LCK-R-" : "LCK-";
            return prefix + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }

        /// <summary>
        /// 流程卡类型名称.
        /// </summary>
        /// <param name="cardType">类型编码.</param>
        /// <returns>类型名称.</returns>
        private static string GetCardTypeName(int? cardType)
        {
            return cardType == 2 ? "合并卡" : cardType == 3 ? "拆分卡" : cardType == 4 ? "重印卡" : "原卡";
        }

        #endregion
    }
}
