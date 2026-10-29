using JNPF.Common.Filter;
using System;
using System.Collections.Generic;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 流程卡打印（合并/拆分/重印）

/// <summary>
/// 流程卡列表查询条件.
/// </summary>
public class FlowCardQuery : PageInputBase
{
    /// <summary>
    /// 流程卡号（模糊）.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 类型：1原卡 2合并卡 3拆分卡 4重印卡，-1为全部.
    /// </summary>
    public int CardType { get; set; } = -1;

    /// <summary>
    /// 订单号（模糊）.
    /// </summary>
    public string OrderNo { get; set; }
}

/// <summary>
/// 流程卡列表输出.
/// </summary>
public class FlowCardListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 类型：1原卡 2合并卡 3拆分卡 4重印卡.
    /// </summary>
    public int? CardType { get; set; }

    /// <summary>
    /// 关联旧流程卡（逗号分隔，如 LCK0002,LCK0004）.
    /// </summary>
    public string OldCardNos { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 产品.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 打印人.
    /// </summary>
    public string PrintUser { get; set; }

    /// <summary>
    /// 打印时间.
    /// </summary>
    public DateTime? PrintTime { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>
/// 打印流程卡入参（原卡打印/合并/拆分/重印）.
/// </summary>
public class FlowCardPrintInput
{
    /// <summary>
    /// 打印类型：1原卡（普通打印） 2合并（多旧→一新） 3拆分（一旧→多新） 4重印（关联原卡）.
    /// </summary>
    public int PrintType { get; set; } = 1;

    /// <summary>
    /// 关联旧流程卡ID集合（合并可多选；拆分/重印仅一张）.
    /// </summary>
    public List<string> OldCardIds { get; set; } = new List<string>();

    /// <summary>
    /// 订单ID（可空，合并/拆分/重印时未填则继承旧卡）.
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// 订单号（可空）.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 产品（可空）.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 图番（可空）.
    /// </summary>
    public string DrawingNo { get; set; }

    /// <summary>
    /// 单卡数量（可空，合并时默认=旧卡合计；拆分时默认=旧卡均分）.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位（可空）.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 新卡数量（默认1；合并通常为1，拆分为多张）.
    /// </summary>
    public int NewCardCount { get; set; } = 1;

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 打印流程卡输出.
/// </summary>
public class FlowCardPrintOutput
{
    /// <summary>
    /// 新流程卡ID集合.
    /// </summary>
    public List<string> NewCardIds { get; set; } = new List<string>();

    /// <summary>
    /// 新流程卡号集合.
    /// </summary>
    public List<string> NewCardNos { get; set; } = new List<string>();

    /// <summary>
    /// 提示消息.
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// 流程卡追溯关系项（来源旧卡）.
/// </summary>
public class FlowCardRelationItemDto
{
    /// <summary>
    /// 旧卡主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 旧卡卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 旧卡类型：1原卡 2合并卡 3拆分卡 4重印卡.
    /// </summary>
    public int? CardType { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 打印人.
    /// </summary>
    public string PrintUser { get; set; }

    /// <summary>
    /// 打印时间.
    /// </summary>
    public DateTime? PrintTime { get; set; }

    /// <summary>
    /// 旧卡自身来源（用于追溯链路展示）.
    /// </summary>
    public string OldCardNos { get; set; }
}

/// <summary>
/// 流程卡关联追溯输出.
/// </summary>
public class FlowCardRelationOutput
{
    /// <summary>
    /// 当前卡主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 当前卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 当前卡类型.
    /// </summary>
    public int? CardType { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 产品.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 打印人.
    /// </summary>
    public string PrintUser { get; set; }

    /// <summary>
    /// 打印时间.
    /// </summary>
    public DateTime? PrintTime { get; set; }

    /// <summary>
    /// 来源旧卡列表（可上溯/下钻追溯）.
    /// </summary>
    public List<FlowCardRelationItemDto> OldCards { get; set; } = new List<FlowCardRelationItemDto>();

    /// <summary>
    /// 追溯链路说明.
    /// </summary>
    public string TraceNote { get; set; }
}

/// <summary>
/// 流程卡工序流转项.
/// </summary>
public class FlowCardProcessItemDto
{
    /// <summary>
    /// 流转主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 工序编码.
    /// </summary>
    public string ProcessCode { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 工序顺序号.
    /// </summary>
    public int? SeqNo { get; set; }

    /// <summary>
    /// 是否检查工序：0否 1是.
    /// </summary>
    public int? IsCheckProcess { get; set; }

    /// <summary>
    /// 状态：1未开始 2生产中 3已完成.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 目标数量.
    /// </summary>
    public decimal? TargetQty { get; set; }

    /// <summary>
    /// 完成数量.
    /// </summary>
    public decimal? CompleteQty { get; set; }
}

/// <summary>
/// 流程卡扫码查询输出（供PDA）.
/// </summary>
public class FlowCardScanOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 类型：1原卡 2合并卡 3拆分卡 4重印卡.
    /// </summary>
    public int? CardType { get; set; }

    /// <summary>
    /// 订单ID.
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 产品.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 图番.
    /// </summary>
    public string DrawingNo { get; set; }

    /// <summary>
    /// 数量（流程卡定量）.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 状态：1未使用 2生产中 3已完工 4已入库 5作废.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 工序流转列表.
    /// </summary>
    public List<FlowCardProcessItemDto> Processes { get; set; } = new List<FlowCardProcessItemDto>();
}

#endregion

#region 报工记录（三层级/PDA报工/检包检验）

/// <summary>
/// 报工记录查询条件（流程卡层级）.
/// </summary>
public class WorkReportQuery : PageInputBase
{
    /// <summary>
    /// 订单号（模糊）.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 流程卡号（模糊）.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 工序名称（模糊，按工序过滤流程卡）.
    /// </summary>
    public string ProcessName { get; set; }
}

/// <summary>
/// 报工记录流程卡层级列表项（一级）.
/// </summary>
public class WorkReportCardListDto
{
    /// <summary>
    /// 流程卡主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 产品.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 当前工序（最近报工工序）.
    /// </summary>
    public string CurrentProcess { get; set; }

    /// <summary>
    /// 报工条数.
    /// </summary>
    public int ReportCount { get; set; }

    /// <summary>
    /// 流程卡状态：1未使用 2生产中 3已完工 4已入库 5作废.
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>
/// 报工明细项（三级：该工序下的多条报工）.
/// </summary>
public class WorkReportDetailDto
{
    /// <summary>
    /// 报工主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 流程卡工序流转ID.
    /// </summary>
    public string CardProcessId { get; set; }

    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 设备名称.
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    /// 报工人姓名.
    /// </summary>
    public string WorkerName { get; set; }

    /// <summary>
    /// 是否临时作业员：0否 1是.
    /// </summary>
    public int? IsTempWorker { get; set; }

    /// <summary>
    /// 报工批次号（同一次多人报工共享）.
    /// </summary>
    public string GroupNo { get; set; }

    /// <summary>
    /// 批次报工总数量（均分前）.
    /// </summary>
    public decimal? GroupQty { get; set; }

    /// <summary>
    /// 均分人数.
    /// </summary>
    public int? MemberCount { get; set; }

    /// <summary>
    /// 良品数（均分后）.
    /// </summary>
    public decimal? GoodQty { get; set; }

    /// <summary>
    /// 不良数（均分后）.
    /// </summary>
    public decimal? BadQty { get; set; }

    /// <summary>
    /// 工时（均分后）.
    /// </summary>
    public decimal? WorkHours { get; set; }

    /// <summary>
    /// 工位.
    /// </summary>
    public string Station { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 是否清洗合并报工：0否 1是.
    /// </summary>
    public int? IsCleanMerge { get; set; }

    /// <summary>
    /// 是否已填写检包检验记录（检查工序）.
    /// </summary>
    public bool HasCheckDetail { get; set; }
}

/// <summary>
/// 报工记录工序层级（二级：工序流转 + 该工序报工明细）.
/// </summary>
public class WorkReportProcessDto
{
    /// <summary>
    /// 流转主键.
    /// </summary>
    public string CardProcessId { get; set; }

    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 工序编码.
    /// </summary>
    public string ProcessCode { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 工序顺序号.
    /// </summary>
    public int? SeqNo { get; set; }

    /// <summary>
    /// 是否检查工序：0否 1是.
    /// </summary>
    public int? IsCheckProcess { get; set; }

    /// <summary>
    /// 状态：1未开始 2生产中 3已完成.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 目标数量.
    /// </summary>
    public decimal? TargetQty { get; set; }

    /// <summary>
    /// 完成数量（Σ报工良品）.
    /// </summary>
    public decimal? CompleteQty { get; set; }

    /// <summary>
    /// 报工条数.
    /// </summary>
    public int ReportCount { get; set; }

    /// <summary>
    /// 报工明细列表（三级）.
    /// </summary>
    public List<WorkReportDetailDto> Reports { get; set; } = new List<WorkReportDetailDto>();
}

/// <summary>
/// 报工记录流程卡明细输出（三层级展开）.
/// </summary>
public class WorkReportCardDetailOutput
{
    /// <summary>
    /// 流程卡主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 订单ID.
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 产品.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 工序层级列表（二级）.
    /// </summary>
    public List<WorkReportProcessDto> Processes { get; set; } = new List<WorkReportProcessDto>();
}

/// <summary>
/// 报工人员入参.
/// </summary>
public class WorkReportWorkerInput
{
    /// <summary>
    /// 报工人ID（平台用户ID，可空）.
    /// </summary>
    public string WorkerId { get; set; }

    /// <summary>
    /// 报人工号.
    /// </summary>
    public string WorkerNo { get; set; }

    /// <summary>
    /// 报工人姓名.
    /// </summary>
    public string WorkerName { get; set; }

    /// <summary>
    /// 是否临时作业员：0否 1是.
    /// </summary>
    public int IsTempWorker { get; set; } = 0;
}

/// <summary>
/// PDA提交报工入参（多人报工按人数均分；清洗工序按清洗合并码配置均分）.
/// </summary>
public class WorkReportSubmitInput
{
    /// <summary>
    /// 流程卡ID（扫码带出）.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 流程卡工序流转ID（优先；为空时按 流程卡+工序 匹配）.
    /// </summary>
    public string CardProcessId { get; set; }

    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 设备ID（扫码带出，可空）.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// 工位.
    /// </summary>
    public string Station { get; set; }

    /// <summary>
    /// 批次报工总数量（均分前；良品数口径）.
    /// </summary>
    public decimal GroupQty { get; set; }

    /// <summary>
    /// 不良数（批次总量，均分后计入每条记录）.
    /// </summary>
    public decimal BadQty { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 总工时（小时，均分）；为空时由开始/结束时间计算.
    /// </summary>
    public decimal? WorkHours { get; set; }

    /// <summary>
    /// 是否清洗合并报工：按清洗合并码配置的人数均分（0否 1是）.
    /// </summary>
    public int IsCleanMerge { get; set; } = 0;

    /// <summary>
    /// 报工人员列表（多人并行报工）.
    /// </summary>
    public List<WorkReportWorkerInput> Workers { get; set; } = new List<WorkReportWorkerInput>();

    /// <summary>
    /// 检包检验记录（检查工序最后一步填写）.
    /// </summary>
    public ReportCheckDetailInput CheckDetail { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// PDA提交报工输出.
/// </summary>
public class WorkReportSubmitOutput
{
    /// <summary>
    /// 报工批次号（同一次多人报工共享）.
    /// </summary>
    public string GroupNo { get; set; }

    /// <summary>
    /// 均分人数.
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// 每人良品数（均分后）.
    /// </summary>
    public decimal PerGoodQty { get; set; }

    /// <summary>
    /// 每人不良数（均分后）.
    /// </summary>
    public decimal PerBadQty { get; set; }

    /// <summary>
    /// 每人工时（均分后）.
    /// </summary>
    public decimal PerWorkHours { get; set; }

    /// <summary>
    /// 提示消息.
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// 检包检验记录入参（检查工序最后一步：材料不良4项/加工不良2项/返工5项）.
/// </summary>
public class ReportCheckDetailInput
{
    /// <summary>
    /// 检验日期.
    /// </summary>
    public DateTime? CheckDate { get; set; }

    /// <summary>
    /// 工时.
    /// </summary>
    public decimal? WorkHours { get; set; }

    /// <summary>
    /// 图番.
    /// </summary>
    public string DrawingNo { get; set; }

    /// <summary>
    /// 制造编号.
    /// </summary>
    public string MakeNo { get; set; }

    /// <summary>
    /// 检验内容.
    /// </summary>
    public string CheckContent { get; set; }

    /// <summary>
    /// 检验数量.
    /// </summary>
    public decimal? CheckQty { get; set; }

    /// <summary>
    /// 良品数.
    /// </summary>
    public decimal? GoodQty { get; set; }

    /// <summary>
    /// 材料不良-材料拉伤.
    /// </summary>
    public decimal? MatBadPull { get; set; }

    /// <summary>
    /// 材料不良-气泡.
    /// </summary>
    public decimal? MatBadBubble { get; set; }

    /// <summary>
    /// 材料不良-接头.
    /// </summary>
    public decimal? MatBadJoint { get; set; }

    /// <summary>
    /// 材料不良-厚度薄.
    /// </summary>
    public decimal? MatBadThin { get; set; }

    /// <summary>
    /// 加工不良-伤.
    /// </summary>
    public decimal? ProcBadInjury { get; set; }

    /// <summary>
    /// 加工不良-其它.
    /// </summary>
    public decimal? ProcBadOther { get; set; }

    /// <summary>
    /// 返工-返清洗.
    /// </summary>
    public decimal? ReworkClean { get; set; }

    /// <summary>
    /// 返工-返铣面.
    /// </summary>
    public decimal? ReworkMill { get; set; }

    /// <summary>
    /// 返工-返修品.
    /// </summary>
    public decimal? ReworkRepair { get; set; }

    /// <summary>
    /// 返工-修伤.
    /// </summary>
    public decimal? ReworkFix { get; set; }

    /// <summary>
    /// 返工-脏污.
    /// </summary>
    public decimal? ReworkDirty { get; set; }

    /// <summary>
    /// 责任人.
    /// </summary>
    public string DutyPerson { get; set; }

    /// <summary>
    /// 是否临时作业员：0否 1是.
    /// </summary>
    public int IsTempWorker { get; set; } = 0;

    /// <summary>
    /// 操作员.
    /// </summary>
    public string OperatorName { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 检包检验记录输出（检查明细弹窗）.
/// </summary>
public class ReportCheckDetailOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 报工明细ID.
    /// </summary>
    public string ReportId { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 产品.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 检验日期.
    /// </summary>
    public DateTime? CheckDate { get; set; }

    /// <summary>
    /// 工时.
    /// </summary>
    public decimal? WorkHours { get; set; }

    /// <summary>
    /// 图番.
    /// </summary>
    public string DrawingNo { get; set; }

    /// <summary>
    /// 制造编号.
    /// </summary>
    public string MakeNo { get; set; }

    /// <summary>
    /// 检验内容.
    /// </summary>
    public string CheckContent { get; set; }

    /// <summary>
    /// 检验数量.
    /// </summary>
    public decimal? CheckQty { get; set; }

    /// <summary>
    /// 良品数.
    /// </summary>
    public decimal? GoodQty { get; set; }

    /// <summary>
    /// 材料不良-材料拉伤.
    /// </summary>
    public decimal? MatBadPull { get; set; }

    /// <summary>
    /// 材料不良-气泡.
    /// </summary>
    public decimal? MatBadBubble { get; set; }

    /// <summary>
    /// 材料不良-接头.
    /// </summary>
    public decimal? MatBadJoint { get; set; }

    /// <summary>
    /// 材料不良-厚度薄.
    /// </summary>
    public decimal? MatBadThin { get; set; }

    /// <summary>
    /// 加工不良-伤.
    /// </summary>
    public decimal? ProcBadInjury { get; set; }

    /// <summary>
    /// 加工不良-其它.
    /// </summary>
    public decimal? ProcBadOther { get; set; }

    /// <summary>
    /// 返工-返清洗.
    /// </summary>
    public decimal? ReworkClean { get; set; }

    /// <summary>
    /// 返工-返铣面.
    /// </summary>
    public decimal? ReworkMill { get; set; }

    /// <summary>
    /// 返工-返修品.
    /// </summary>
    public decimal? ReworkRepair { get; set; }

    /// <summary>
    /// 返工-修伤.
    /// </summary>
    public decimal? ReworkFix { get; set; }

    /// <summary>
    /// 返工-脏污.
    /// </summary>
    public decimal? ReworkDirty { get; set; }

    /// <summary>
    /// 责任人.
    /// </summary>
    public string DutyPerson { get; set; }

    /// <summary>
    /// 是否临时作业员：0否 1是.
    /// </summary>
    public int? IsTempWorker { get; set; }

    /// <summary>
    /// 操作员.
    /// </summary>
    public string OperatorName { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 结束生产输出（未完工流程卡保留，次日可继续报工）.
/// </summary>
public class WorkReportEndOutput
{
    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 未完工工序列表.
    /// </summary>
    public List<string> UnfinishedProcesses { get; set; } = new List<string>();

    /// <summary>
    /// 提示消息.
    /// </summary>
    public string Message { get; set; }
}

#endregion

#region 清洗合并码（版本化配置）

/// <summary>
/// 清洗合并码成员入参.
/// </summary>
public class CleanMergeMemberInput
{
    /// <summary>
    /// 工号.
    /// </summary>
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string EmployeeName { get; set; }
}

/// <summary>
/// 清洗合并码配置保存入参（编辑即升版本：旧版本写失效日期并标记已失效，永久保留）.
/// </summary>
public class CleanMergeSaveInput
{
    /// <summary>
    /// 工序ID（默认清洗工序）.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 报工人数（绩效口径，默认4人）.
    /// </summary>
    public int MemberCount { get; set; } = 4;

    /// <summary>
    /// 生效日期（默认当天）.
    /// </summary>
    public DateTime? EffectDate { get; set; }

    /// <summary>
    /// 清洗工位成员列表.
    /// </summary>
    public List<CleanMergeMemberInput> Members { get; set; } = new List<CleanMergeMemberInput>();

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 清洗合并码成员输出.
/// </summary>
public class CleanMergeMemberOutput
{
    /// <summary>
    /// 工号.
    /// </summary>
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string EmployeeName { get; set; }
}

/// <summary>
/// 清洗合并码当前生效配置输出.
/// </summary>
public class CleanMergeCurrentOutput
{
    /// <summary>
    /// 配置主键.
    /// </summary>
    public string ConfigId { get; set; }

    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 版本号（V1/V2/V3）.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 报工人数（绩效口径）.
    /// </summary>
    public int? MemberCount { get; set; }

    /// <summary>
    /// 生效日期.
    /// </summary>
    public DateTime? EffectDate { get; set; }

    /// <summary>
    /// 状态：1生效中 0已失效.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 成员列表.
    /// </summary>
    public List<CleanMergeMemberOutput> Members { get; set; } = new List<CleanMergeMemberOutput>();
}

/// <summary>
/// 清洗合并码历史存档查询条件.
/// </summary>
public class CleanMergeHistoryQuery : PageInputBase
{
    /// <summary>
    /// 工序ID（可空，空=全部）.
    /// </summary>
    public string ProcessId { get; set; }
}

/// <summary>
/// 清洗合并码历史存档列表项.
/// </summary>
public class CleanMergeHistoryDto
{
    /// <summary>
    /// 配置主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 版本号.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 报工人数.
    /// </summary>
    public int? MemberCount { get; set; }

    /// <summary>
    /// 清洗工位成员（如 张伟/李芳/王强/刘敏）.
    /// </summary>
    public string MemberNames { get; set; }

    /// <summary>
    /// 生效日期.
    /// </summary>
    public DateTime? EffectDate { get; set; }

    /// <summary>
    /// 失效日期（生效中为空）.
    /// </summary>
    public DateTime? ExpireDate { get; set; }

    /// <summary>
    /// 状态：1生效中 0已失效.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 关联报工批次（按该版本配置产生的清洗报工批次数）.
    /// </summary>
    public int ReportBatchCount { get; set; }
}

#endregion
