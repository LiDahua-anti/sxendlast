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
using System.Text.Json;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 报工记录（三层级/PDA报工/检包检验）.
    /// 报工记录三层级：流程卡 → 工序流转（sx_flow_card_process）→ 报工明细（sx_work_report）；
    /// 同一流程卡同一工序多人报工按报工人数均分；清洗工序按清洗合并码配置人数均分（默认4人，历史报工保留原配置人员）；
    /// 检查为工序最后一步（末道检查工序报工须填写检包检验记录）；全检良品计入末道工序报工；
    /// Σ报工良品 ≤ 流程卡定量（超出拦截）；全部工序完成 → 流程卡已完工、订单「待入库」；结束生产时未完工流程卡保留次日继续。
    /// </summary>
    [ApiDescriptionSettings(Tag = "WorkReport", Name = "", Order = 209)]
    [Route("api/[controller]")]
    public class WorkReportService : IWorkReportService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 报工明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<WorkReportEntity> _workReportRepository;

        /// <summary>
        /// 流程卡仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardEntity> _flowCardRepository;

        /// <summary>
        /// 流程卡工序流转仓储.
        /// </summary>
        private readonly ISqlSugarRepository<FlowCardProcessEntity> _flowCardProcessRepository;

        /// <summary>
        /// 检包检验记录仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ReportCheckDetailEntity> _checkDetailRepository;

        /// <summary>
        /// 清洗合并码配置仓储.
        /// </summary>
        private readonly ISqlSugarRepository<CleanMergeConfigEntity> _cleanConfigRepository;

        /// <summary>
        /// 清洗合并码成员仓储.
        /// </summary>
        private readonly ISqlSugarRepository<CleanMergeMemberEntity> _cleanMemberRepository;

        /// <summary>
        /// 设备台账仓储.
        /// </summary>
        private readonly ISqlSugarRepository<DeviceMasterEntity> _deviceRepository;

        /// <summary>
        /// 生产订单仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionOrderEntity> _orderRepository;

        /// <summary>
        /// 流程卡服务（自动初始化工序流转）.
        /// </summary>
        private readonly IFlowCardService _flowCardService;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="WorkReportService"/>类型的新实例.
        /// </summary>
        public WorkReportService(
            ISqlSugarRepository<WorkReportEntity> workReportRepository,
            ISqlSugarRepository<FlowCardEntity> flowCardRepository,
            ISqlSugarRepository<FlowCardProcessEntity> flowCardProcessRepository,
            ISqlSugarRepository<ReportCheckDetailEntity> checkDetailRepository,
            ISqlSugarRepository<CleanMergeConfigEntity> cleanConfigRepository,
            ISqlSugarRepository<CleanMergeMemberEntity> cleanMemberRepository,
            ISqlSugarRepository<DeviceMasterEntity> deviceRepository,
            ISqlSugarRepository<ProductionOrderEntity> orderRepository,
            IFlowCardService flowCardService,
            IUserManager userManager)
        {
            _workReportRepository = workReportRepository;
            _flowCardRepository = flowCardRepository;
            _flowCardProcessRepository = flowCardProcessRepository;
            _checkDetailRepository = checkDetailRepository;
            _cleanConfigRepository = cleanConfigRepository;
            _cleanMemberRepository = cleanMemberRepository;
            _deviceRepository = deviceRepository;
            _orderRepository = orderRepository;
            _flowCardService = flowCardService;
            _userManager = userManager;
        }

        #region 报工记录查询（三层级）

        /// <summary>
        /// 报工记录列表（一级：流程卡层级，含当前工序/报工条数）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetWorkReportList")]
        public async Task<dynamic> GetWorkReportList([FromBody] WorkReportQuery input)
        {
            var whereLambda = LinqExpression.And<FlowCardEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);
            whereLambda = whereLambda.And(m => m.Status == null || m.Status != 5);

            if (!string.IsNullOrEmpty(input.FlowCardNo))
            {
                whereLambda = whereLambda.And(m => m.CardNo.Contains(input.FlowCardNo));
            }

            if (!string.IsNullOrEmpty(input.OrderNo))
            {
                whereLambda = whereLambda.And(m => m.OrderNo.Contains(input.OrderNo));
            }

            if (!string.IsNullOrEmpty(input.ProcessName))
            {
                var matchCardIds = await _flowCardProcessRepository.AsQueryable()
                    .Where(m => m.DeleteMark == null && m.ProcessName.Contains(input.ProcessName))
                    .Select(m => m.FlowCardId).ToListAsync();
                whereLambda = whereLambda.And(m => matchCardIds.Contains(m.Id));
            }

            var page = await _flowCardRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.PrintTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            // 当前页流程卡的工序流转与报工明细
            var cardIds = page.list.Select(m => m.Id).ToList();
            var processes = cardIds.Any()
                ? await _flowCardProcessRepository.AsQueryable()
                    .Where(m => cardIds.Contains(m.FlowCardId) && m.DeleteMark == null).ToListAsync()
                : new List<FlowCardProcessEntity>();
            var reports = cardIds.Any()
                ? await _workReportRepository.AsQueryable()
                    .Where(m => cardIds.Contains(m.FlowCardId) && m.DeleteMark == null && m.Status == 1).ToListAsync()
                : new List<WorkReportEntity>();

            var dtos = page.list.Select(m =>
            {
                var cardReports = reports.Where(r => r.FlowCardId == m.Id).ToList();
                var cardProcesses = processes.Where(p => p.FlowCardId == m.Id).ToList();
                var currentProcess = cardReports.Any()
                    ? cardReports.OrderByDescending(r => r.EndTime ?? r.StartTime).First().ProcessName
                    : cardProcesses.Where(p => p.Status == 2).OrderBy(p => p.SeqNo).FirstOrDefault()?.ProcessName;

                return new WorkReportCardListDto
                {
                    Id = m.Id,
                    CardNo = m.CardNo,
                    OrderNo = m.OrderNo,
                    ProductName = m.ProductName,
                    Qty = m.Qty,
                    Unit = m.Unit,
                    CurrentProcess = currentProcess,
                    ReportCount = cardReports.Count,
                    Status = m.Status
                };
            }).ToList();

            return new SqlSugarPagedList<WorkReportCardListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 报工记录流程卡明细（三级展开：流程卡→工序流转→报工明细）.
        /// </summary>
        /// <param name="flowCardId">流程卡主键.</param>
        /// <returns>三层级明细.</returns>
        [HttpGet("GetWorkReportCardDetail")]
        public async Task<dynamic> GetWorkReportCardDetail(string flowCardId)
        {
            if (flowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == flowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var processes = await _flowCardProcessRepository.AsQueryable()
                .Where(m => m.FlowCardId == flowCardId && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            var reports = await _workReportRepository.AsQueryable()
                .Where(m => m.FlowCardId == flowCardId && m.DeleteMark == null && m.Status == 1)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            // 已填写检包检验记录的报工（检查工序行显示「检查明细」）
            var reportIds = reports.Select(m => m.Id).ToList();
            var checkReportIds = reportIds.Any()
                ? await _checkDetailRepository.AsQueryable()
                    .Where(m => reportIds.Contains(m.ReportId) && m.DeleteMark == null)
                    .Select(m => m.ReportId).ToListAsync()
                : new List<string>();

            return new WorkReportCardDetailOutput
            {
                Id = card.Id,
                CardNo = card.CardNo,
                OrderId = card.OrderId,
                OrderNo = card.OrderNo,
                ProductName = card.ProductName,
                Qty = card.Qty,
                Unit = card.Unit,
                Status = card.Status,
                Processes = processes.Select(p => new WorkReportProcessDto
                {
                    CardProcessId = p.Id,
                    ProcessId = p.ProcessId,
                    ProcessCode = p.ProcessCode,
                    ProcessName = p.ProcessName,
                    SeqNo = p.SeqNo,
                    IsCheckProcess = p.IsCheckProcess,
                    Status = p.Status,
                    TargetQty = p.TargetQty,
                    CompleteQty = p.CompleteQty,
                    ReportCount = reports.Count(r => r.CardProcessId == p.Id),
                    Reports = reports.Where(r => r.CardProcessId == p.Id).Select(r => new WorkReportDetailDto
                    {
                        Id = r.Id,
                        CardProcessId = r.CardProcessId,
                        ProcessId = r.ProcessId,
                        ProcessName = r.ProcessName,
                        DeviceName = r.DeviceName,
                        WorkerName = r.WorkerName,
                        IsTempWorker = r.IsTempWorker,
                        GroupNo = r.GroupNo,
                        GroupQty = r.GroupQty,
                        MemberCount = r.MemberCount,
                        GoodQty = r.GoodQty,
                        BadQty = r.BadQty,
                        WorkHours = r.WorkHours,
                        Station = r.Station,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                        IsCleanMerge = r.IsCleanMerge,
                        HasCheckDetail = checkReportIds.Contains(r.Id)
                    }).ToList()
                }).ToList()
            };
        }

        #endregion

        #region PDA提交报工

        /// <summary>
        /// PDA提交报工（多人报工按人数均分；清洗工序按清洗合并码配置人数均分；末道检查工序写检包检验记录）.
        /// </summary>
        /// <param name="input">报工入参.</param>
        /// <returns>报工结果（批次号/均分结果）.</returns>
        [HttpPost("SubmitWorkReport")]
        public async Task<dynamic> SubmitWorkReport([FromBody] WorkReportSubmitInput input)
        {
            if (input.FlowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            if (input.GroupQty <= 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, "报工数量必须大于0");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == input.FlowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "流程卡不存在，请先扫码确认");
            }

            if (card.Status == 4 || card.Status == 5)
            {
                throw Oops.Oh(ErrorCode.COM1018, "该流程卡已入库或作废，不能报工");
            }

            // 匹配工序流转（优先流转ID，其次 流程卡+工序；缺失时尝试按排产自动初始化）
            var cardProcess = await MatchCardProcess(input);
            if (cardProcess == null)
            {
                await _flowCardService.InitFlowCardProcesses(input.FlowCardId);
                cardProcess = await MatchCardProcess(input);
            }

            if (cardProcess == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "该流程卡未找到对应工序流转，请确认工序或先完成排产发布");
            }

            // 勾稽校验：Σ各工序报工良品 ≤ 流程卡定量（超出拦截）
            var target = cardProcess.TargetQty ?? card.Qty ?? 0;
            var completed = cardProcess.CompleteQty ?? 0;
            if (target > 0 && completed + input.GroupQty > target)
            {
                throw Oops.Oh(ErrorCode.COM1018, $"超出流程卡定量已拦截：本工序已完成 {completed}，本次报工 {input.GroupQty}，流程卡定量 {target}");
            }

            // 检查工序：报工须填写检包检验记录（检查=工序最后一步）
            if (cardProcess.IsCheckProcess == 1 && input.CheckDetail == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "该工序为检查工序（最后一步），请填写检包检验记录后再报工");
            }

            // 均分口径：普通多人报工按报工人数均分；清洗合并报工按清洗合并码配置人数均分
            var members = new List<WorkReportWorkerInput>();
            var memberCount = 0;
            var isCleanMerge = 0;
            string mergeConfigId = null;
            if (input.IsCleanMerge == 1)
            {
                var config = await _cleanConfigRepository.AsQueryable()
                    .Where(m => m.ProcessId == cardProcess.ProcessId && m.Status == 1 && m.DeleteMark == null)
                    .OrderBy(m => m.EffectDate, OrderByType.Desc)
                    .FirstAsync();
                if (config != null)
                {
                    var configMembers = await _cleanMemberRepository.AsQueryable()
                        .Where(m => m.ConfigId == config.Id && m.DeleteMark == null)
                        .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                        .ToListAsync();
                    members = configMembers.Select(m => new WorkReportWorkerInput
                    {
                        WorkerNo = m.EmployeeNo,
                        WorkerName = m.EmployeeName
                    }).ToList();
                    memberCount = config.MemberCount ?? members.Count;
                    mergeConfigId = config.Id;
                    isCleanMerge = 1;
                }
                else
                {
                    // 无生效清洗配置：退化为按传入人员均分
                    members = input.Workers ?? new List<WorkReportWorkerInput>();
                    memberCount = members.Count;
                }
            }
            else
            {
                members = input.Workers ?? new List<WorkReportWorkerInput>();
                memberCount = members.Count;
            }

            if (!members.Any())
            {
                members = new List<WorkReportWorkerInput>
                {
                    new WorkReportWorkerInput
                    {
                        WorkerId = _userManager.UserId,
                        WorkerName = _userManager.RealName
                    }
                };
                memberCount = members.Count;
            }

            if (memberCount <= 0)
            {
                memberCount = members.Count;
            }

            // 人员快照（清洗合并码配置变更后历史报工仍按变更前人员存档，可追溯）
            var memberSnapshot = JsonSerializer.Serialize(members.Select(m => new
            {
                employeeNo = m.WorkerNo,
                employeeName = m.WorkerName
            }));

            // 工时（均分后计入各人）
            decimal totalHours = 0;
            if (input.WorkHours != null && input.WorkHours > 0)
            {
                totalHours = input.WorkHours.Value;
            }
            else if (input.StartTime != null && input.EndTime != null && input.EndTime > input.StartTime)
            {
                totalHours = Math.Round((decimal)(input.EndTime.Value - input.StartTime.Value).TotalHours, 2);
            }

            var perGood = Math.Round(input.GroupQty / memberCount, 4);
            var perBad = Math.Round(input.BadQty / memberCount, 4);
            var perHours = Math.Round(totalHours / memberCount, 2);

            // 设备冗余回填
            DeviceMasterEntity device = null;
            if (!input.DeviceId.IsNullOrEmpty())
            {
                device = await _deviceRepository.GetFirstAsync(m => m.Id == input.DeviceId && m.DeleteMark == null);
            }

            var now = DateTime.Now;
            var groupNo = "GRP" + now.ToString("yyyyMMddHHmmssfff");
            var rows = members.Select(m => new WorkReportEntity
            {
                CardProcessId = cardProcess.Id,
                FlowCardId = card.Id,
                FlowCardNo = card.CardNo,
                OrderId = card.OrderId,
                OrderNo = card.OrderNo,
                ProcessId = cardProcess.ProcessId,
                ProcessName = cardProcess.ProcessName,
                DeviceId = input.DeviceId,
                DeviceCode = device?.DeviceNo,
                DeviceName = device?.DeviceName,
                Station = input.Station,
                WorkerId = m.WorkerId,
                WorkerNo = m.WorkerNo,
                WorkerName = m.WorkerName,
                IsTempWorker = m.IsTempWorker,
                GroupNo = groupNo,
                GroupQty = input.GroupQty,
                MemberCount = memberCount,
                GoodQty = perGood,
                BadQty = perBad,
                StartTime = input.StartTime ?? now,
                EndTime = input.EndTime ?? now,
                WorkHours = perHours,
                IsCleanMerge = isCleanMerge,
                MergeConfigId = mergeConfigId,
                MemberSnapshot = memberSnapshot,
                Status = 1,
                Remark = input.Remark
            }).ToList();

            var insertOk = await _workReportRepository.AsSugarClient().Insertable(rows)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 检包检验记录（检查工序最后一步：材料不良4项/加工不良2项/返工5项；多人时按人数均分数量）
            if (input.CheckDetail != null && cardProcess.IsCheckProcess == 1)
            {
                var details = rows.Select(row => new ReportCheckDetailEntity
                {
                    ReportId = row.Id,
                    FlowCardId = card.Id,
                    FlowCardNo = card.CardNo,
                    CheckDate = input.CheckDetail.CheckDate ?? now.Date,
                    WorkHours = DivideOnAverage(input.CheckDetail.WorkHours, memberCount),
                    DrawingNo = !input.CheckDetail.DrawingNo.IsNullOrEmpty() ? input.CheckDetail.DrawingNo : card.DrawingNo,
                    MakeNo = input.CheckDetail.MakeNo,
                    CheckContent = input.CheckDetail.CheckContent,
                    CheckQty = DivideOnAverage(input.CheckDetail.CheckQty, memberCount),
                    GoodQty = DivideOnAverage(input.CheckDetail.GoodQty, memberCount),
                    MatBadPull = DivideOnAverage(input.CheckDetail.MatBadPull, memberCount),
                    MatBadBubble = DivideOnAverage(input.CheckDetail.MatBadBubble, memberCount),
                    MatBadJoint = DivideOnAverage(input.CheckDetail.MatBadJoint, memberCount),
                    MatBadThin = DivideOnAverage(input.CheckDetail.MatBadThin, memberCount),
                    ProcBadInjury = DivideOnAverage(input.CheckDetail.ProcBadInjury, memberCount),
                    ProcBadOther = DivideOnAverage(input.CheckDetail.ProcBadOther, memberCount),
                    ReworkClean = DivideOnAverage(input.CheckDetail.ReworkClean, memberCount),
                    ReworkMill = DivideOnAverage(input.CheckDetail.ReworkMill, memberCount),
                    ReworkRepair = DivideOnAverage(input.CheckDetail.ReworkRepair, memberCount),
                    ReworkFix = DivideOnAverage(input.CheckDetail.ReworkFix, memberCount),
                    ReworkDirty = DivideOnAverage(input.CheckDetail.ReworkDirty, memberCount),
                    DutyPerson = input.CheckDetail.DutyPerson,
                    IsTempWorker = input.CheckDetail.IsTempWorker,
                    OperatorName = input.CheckDetail.OperatorName,
                    Qty = DivideOnAverage(input.CheckDetail.Qty, memberCount),
                    Remark = input.CheckDetail.Remark
                }).ToList();

                await _checkDetailRepository.AsSugarClient().Insertable(details)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            // 更新工序流转：完成量累计 + 状态（达目标=已完成）
            cardProcess.CompleteQty = Math.Round(completed + input.GroupQty, 4);
            cardProcess.Status = target > 0 && cardProcess.CompleteQty >= target ? 3 : 2;
            await _flowCardProcessRepository.AsSugarClient().Updateable(cardProcess)
                .CallEntityMethod(m => m.LastModify())
                .UpdateColumns(it => new { it.CompleteQty, it.Status, it.LastModifyTime, it.LastModifyUserId })
                .ExecuteCommandAsync();

            // 流程卡：未使用 → 生产中
            if (card.Status == null || card.Status == 1)
            {
                card.Status = 2;
                await _flowCardRepository.AsSugarClient().Updateable(card)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();
            }

            // 全部工序完成 → 流程卡「已完工」+ 订单「待入库」
            var allProcesses = await _flowCardProcessRepository.AsQueryable()
                .Where(m => m.FlowCardId == card.Id && m.DeleteMark == null).ToListAsync();
            var allDone = allProcesses.Any() && allProcesses.All(p =>
                p.Id == cardProcess.Id ? cardProcess.Status == 3 : p.Status == 3);
            if (allDone)
            {
                card.Status = 3;
                await _flowCardRepository.AsSugarClient().Updateable(card)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();

                if (!card.OrderId.IsNullOrEmpty())
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
            }

            var mergeNote = isCleanMerge == 1 ? "（清洗合并码配置人数均分）" : "（按报工人数均分）";
            return new WorkReportSubmitOutput
            {
                GroupNo = groupNo,
                MemberCount = memberCount,
                PerGoodQty = perGood,
                PerBadQty = perBad,
                PerWorkHours = perHours,
                Message = $"报工成功：报工 {input.GroupQty} 件 {mergeNote}，每人计良品 {perGood} 件、工时 {perHours} 小时"
            };
        }

        /// <summary>
        /// 获取检包检验记录（检查明细弹窗，检查=工序最后一步）.
        /// </summary>
        /// <param name="reportId">报工明细主键.</param>
        /// <returns>检包检验记录（含流程卡/订单/产品信息）.</returns>
        [HttpGet("GetReportCheckDetail")]
        public async Task<dynamic> GetReportCheckDetail(string reportId)
        {
            if (reportId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var report = await _workReportRepository.GetFirstAsync(m => m.Id == reportId && m.DeleteMark == null);
            if (report == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var detail = await _checkDetailRepository.GetFirstAsync(m => m.ReportId == reportId && m.DeleteMark == null);
            if (detail == null)
            {
                throw Oops.Oh(ErrorCode.COM1018, "该报工无检包检验记录");
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == report.FlowCardId && m.DeleteMark == null);

            return new ReportCheckDetailOutput
            {
                Id = detail.Id,
                ReportId = detail.ReportId,
                FlowCardNo = report.FlowCardNo,
                OrderNo = report.OrderNo,
                ProductName = card?.ProductName,
                CheckDate = detail.CheckDate,
                WorkHours = detail.WorkHours,
                DrawingNo = detail.DrawingNo,
                MakeNo = detail.MakeNo,
                CheckContent = detail.CheckContent,
                CheckQty = detail.CheckQty,
                GoodQty = detail.GoodQty,
                MatBadPull = detail.MatBadPull,
                MatBadBubble = detail.MatBadBubble,
                MatBadJoint = detail.MatBadJoint,
                MatBadThin = detail.MatBadThin,
                ProcBadInjury = detail.ProcBadInjury,
                ProcBadOther = detail.ProcBadOther,
                ReworkClean = detail.ReworkClean,
                ReworkMill = detail.ReworkMill,
                ReworkRepair = detail.ReworkRepair,
                ReworkFix = detail.ReworkFix,
                ReworkDirty = detail.ReworkDirty,
                DutyPerson = detail.DutyPerson,
                IsTempWorker = detail.IsTempWorker,
                OperatorName = detail.OperatorName,
                Qty = detail.Qty,
                Remark = detail.Remark
            };
        }

        /// <summary>
        /// 结束生产（未完工流程卡保留，次日可继续报工）.
        /// </summary>
        /// <param name="flowCardId">流程卡主键.</param>
        /// <returns>未完工工序概况.</returns>
        [HttpGet("EndProduction")]
        public async Task<dynamic> EndProduction(string flowCardId)
        {
            if (flowCardId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var card = await _flowCardRepository.GetFirstAsync(m => m.Id == flowCardId && m.DeleteMark == null);
            if (card == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            // 未完工流程卡保留：不修改报工与流转数据，次日可继续扫码报工
            var processes = await _flowCardProcessRepository.AsQueryable()
                .Where(m => m.FlowCardId == flowCardId && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .ToListAsync();
            var unfinished = processes.Where(p => p.Status != 3)
                .Select(p => $"{p.ProcessName}（{p.CompleteQty ?? 0}/{p.TargetQty ?? 0}）").ToList();

            return new WorkReportEndOutput
            {
                CardNo = card.CardNo,
                UnfinishedProcesses = unfinished,
                Message = unfinished.Any()
                    ? $"结束生产：未完工流程卡已保留，{unfinished.Count} 道工序未完成，次日可继续报工"
                    : "结束生产：该流程卡全部工序已完成，可进入入库流程"
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 匹配流程卡工序流转（优先流转ID，其次 流程卡+工序）.
        /// </summary>
        /// <param name="input">报工入参.</param>
        /// <returns>工序流转实体（未匹配时为null）.</returns>
        private async Task<FlowCardProcessEntity> MatchCardProcess(WorkReportSubmitInput input)
        {
            if (!input.CardProcessId.IsNullOrEmpty())
            {
                var byId = await _flowCardProcessRepository.GetFirstAsync(m => m.Id == input.CardProcessId && m.DeleteMark == null);
                if (byId != null)
                {
                    return byId;
                }
            }

            if (!input.ProcessId.IsNullOrEmpty())
            {
                return await _flowCardProcessRepository.GetFirstAsync(m =>
                    m.FlowCardId == input.FlowCardId && m.ProcessId == input.ProcessId && m.DeleteMark == null);
            }

            return null;
        }

        /// <summary>
        /// 按人数均分（1人时原值；多人时四舍五入4位）.
        /// </summary>
        /// <param name="value">原始值.</param>
        /// <param name="memberCount">均分人数.</param>
        /// <returns>均分后值.</returns>
        private static decimal? DivideOnAverage(decimal? value, int memberCount)
        {
            if (value == null)
            {
                return null;
            }

            return memberCount > 1 ? Math.Round(value.Value / memberCount, 4) : value;
        }

        #endregion
    }
}
