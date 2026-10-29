using JNPF.Common.Filter;
using System;
using System.Collections.Generic;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 生产订单管理

/// <summary>
/// 生产订单列表查询条件.
/// </summary>
public class OrderQuery : PageInputBase
{
    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 订单状态：1待排产 2已排产 3已发布 4生产中 5待入库 6已完工 7逾期，-1为全部.
    /// </summary>
    public int Status { get; set; } = -1;

    /// <summary>
    /// 发布状态：0未发布 1已发布，-1为全部.
    /// </summary>
    public int PublishStatus { get; set; } = -1;
}

/// <summary>
/// 生产订单新增或编辑入参（手工新增来源标记为2）.
/// </summary>
public class OrderCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 订单号（业务唯一,如MO202609001）.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 图番（图纸编号,如XC01）.
    /// </summary>
    public string DrawingNo { get; set; }

    /// <summary>
    /// 产品名称.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 计划数量.
    /// </summary>
    public decimal? PlanQty { get; set; }

    /// <summary>
    /// 单位（套/件）.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 负责班组.
    /// </summary>
    public string TeamName { get; set; }

    /// <summary>
    /// 计划完工日期.
    /// </summary>
    public DateTime? PlanFinishDate { get; set; }

    /// <summary>
    /// 客户.
    /// </summary>
    public string Customer { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 生产订单统计卡片输出.
/// </summary>
public class OrderStatisticsOutput
{
    /// <summary>
    /// 待排产数量.
    /// </summary>
    public int WaitSchedule { get; set; }

    /// <summary>
    /// 已排产数量.
    /// </summary>
    public int Scheduled { get; set; }

    /// <summary>
    /// 已发布/生产中数量.
    /// </summary>
    public int Published { get; set; }

    /// <summary>
    /// 已完工数量.
    /// </summary>
    public int Finished { get; set; }
}

/// <summary>
/// 用友同步拉取入参（模拟）.
/// </summary>
public class SyncPullInput
{
    /// <summary>
    /// 本批拉取订单条数（默认2条，模拟用友U8返回）.
    /// </summary>
    public int? Count { get; set; }
}

#endregion

#region 排产管理

/// <summary>
/// 排产保存入参（保存时强制校验流程卡必选）.
/// </summary>
public class ScheduleSaveInput
{
    /// <summary>
    /// 生产订单ID.
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// 流程卡ID（必填）.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 投产数量.
    /// </summary>
    public decimal? PlanQty { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 工序×设备分配明细（工序/设备/生产个数）.
    /// </summary>
    public List<ScheduleDetailInput> Details { get; set; } = new List<ScheduleDetailInput>();
}

/// <summary>
/// 排产明细行入参（工序 × 设备 × 生产个数）.
/// </summary>
public class ScheduleDetailInput
{
    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 设备ID.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// 生产个数（该设备分配数量）.
    /// </summary>
    public decimal? ProduceQty { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 订单排产方案输出（含工序设备分配明细）.
/// </summary>
public class ScheduleOutput
{
    /// <summary>
    /// 排产主表ID.
    /// </summary>
    public string ScheduleId { get; set; }

    /// <summary>
    /// 订单ID.
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 流程卡ID.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 投产数量.
    /// </summary>
    public decimal? PlanQty { get; set; }

    /// <summary>
    /// 状态：1已保存 2已发布.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 工序×设备分配明细.
    /// </summary>
    public List<ScheduleDetailOutput> Details { get; set; } = new List<ScheduleDetailOutput>();
}

/// <summary>
/// 排产明细行输出.
/// </summary>
public class ScheduleDetailOutput
{
    /// <summary>
    /// 明细主键.
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
    /// 工序顺序号.
    /// </summary>
    public int? SeqNo { get; set; }

    /// <summary>
    /// 设备ID.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// 设备编码.
    /// </summary>
    public string DeviceCode { get; set; }

    /// <summary>
    /// 设备名称.
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    /// 生产个数.
    /// </summary>
    public decimal? ProduceQty { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

#endregion

#region 生产进度查询

/// <summary>
/// 生产进度查询条件（与生产订单列表同查询条件）.
/// </summary>
public class ProgressQuery : PageInputBase
{
    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 订单状态，-1为全部.
    /// </summary>
    public int Status { get; set; } = -1;
}

/// <summary>
/// 生产进度列表行（关键信息）.
/// </summary>
public class ProgressListDto
{
    /// <summary>
    /// 订单ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 图番.
    /// </summary>
    public string DrawingNo { get; set; }

    /// <summary>
    /// 产品名称.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 计划数量.
    /// </summary>
    public decimal? PlanQty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 计划完工日期.
    /// </summary>
    public DateTime? PlanFinishDate { get; set; }

    /// <summary>
    /// 当前工序（最近报工工序）.
    /// </summary>
    public string CurrentProcess { get; set; }

    /// <summary>
    /// 进度百分比（Σ各工序报工良品 ÷ 任务总量）.
    /// </summary>
    public decimal ProgressPercent { get; set; }

    /// <summary>
    /// 订单状态.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 发布状态.
    /// </summary>
    public int? PublishStatus { get; set; }
}

/// <summary>
/// 生产进度详情输出（订单维度的工序流转）.
/// </summary>
public class ProgressDetailOutput
{
    /// <summary>
    /// 订单ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 图番.
    /// </summary>
    public string DrawingNo { get; set; }

    /// <summary>
    /// 产品名称.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 计划数量.
    /// </summary>
    public decimal? PlanQty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 计划完工日期.
    /// </summary>
    public DateTime? PlanFinishDate { get; set; }

    /// <summary>
    /// 客户.
    /// </summary>
    public string Customer { get; set; }

    /// <summary>
    /// 订单状态.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 发布状态.
    /// </summary>
    public int? PublishStatus { get; set; }

    /// <summary>
    /// 工序流转进度（各工序完成数量/设备/报工人/时间）.
    /// </summary>
    public List<ProgressFlowDto> Flows { get; set; } = new List<ProgressFlowDto>();
}

/// <summary>
/// 工序流转进度行.
/// </summary>
public class ProgressFlowDto
{
    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 工序顺序号.
    /// </summary>
    public int? SeqNo { get; set; }

    /// <summary>
    /// 状态：1未开始 2生产中 3已完成.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 目标数量（Σ流程卡定量）.
    /// </summary>
    public decimal TargetQty { get; set; }

    /// <summary>
    /// 完成数量（Σ报工良品）.
    /// </summary>
    public decimal CompleteQty { get; set; }

    /// <summary>
    /// 设备（逗号分隔）.
    /// </summary>
    public string DeviceNames { get; set; }

    /// <summary>
    /// 报工人（逗号分隔）.
    /// </summary>
    public string WorkerNames { get; set; }

    /// <summary>
    /// 最后报工时间.
    /// </summary>
    public DateTime? LastReportTime { get; set; }
}

#endregion
