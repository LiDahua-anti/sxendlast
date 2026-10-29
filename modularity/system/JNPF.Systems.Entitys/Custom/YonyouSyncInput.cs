using JNPF.Common.Filter;
using System;
using System.Collections.Generic;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 用友同步（YonyouSyncService）

/// <summary>
/// 同步日志查询条件.
/// </summary>
public class YonyouSyncLogQuery : PageInputBase
{
    /// <summary>
    /// 单据类型（采购入库单/产成品入库单/材料出库单/销售出库单/生产领料单/生产订单/存货档案/仓库档案/现有量推送）.
    /// </summary>
    public string BizType { get; set; }

    /// <summary>
    /// 单据号（模糊）.
    /// </summary>
    public string BizNo { get; set; }

    /// <summary>
    /// 状态：1成功 2失败（null为全部）.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 同步方向：1 MES→用友 2 用友→MES（null为全部）.
    /// </summary>
    public int? Direction { get; set; }

    /// <summary>
    /// 触发方式：1自动 2手动触发（null为全部）.
    /// </summary>
    public int? TriggerType { get; set; }

    /// <summary>
    /// 同步时间范围.
    /// </summary>
    public List<DateTime> SyncTime { get; set; }
}

/// <summary>
/// 同步日志列表输出.
/// </summary>
public class YonyouSyncLogListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 单据类型.
    /// </summary>
    public string BizType { get; set; }

    /// <summary>
    /// 单据号.
    /// </summary>
    public string BizNo { get; set; }

    /// <summary>
    /// 同步方向：1 MES→用友 2 用友→MES.
    /// </summary>
    public int? Direction { get; set; }

    /// <summary>
    /// 同步方向名称.
    /// </summary>
    public string DirectionText { get; set; }

    /// <summary>
    /// 用友凭证号.
    /// </summary>
    public string VoucherNo { get; set; }

    /// <summary>
    /// 状态：1成功 2失败.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 状态名称.
    /// </summary>
    public string StatusText { get; set; }

    /// <summary>
    /// 失败原因.
    /// </summary>
    public string FailReason { get; set; }

    /// <summary>
    /// 同步时间.
    /// </summary>
    public DateTime? SyncTime { get; set; }

    /// <summary>
    /// 耗时（毫秒）.
    /// </summary>
    public int? CostMs { get; set; }

    /// <summary>
    /// 触发方式：1自动 2手动触发.
    /// </summary>
    public int? TriggerType { get; set; }

    /// <summary>
    /// 触发方式名称.
    /// </summary>
    public string TriggerTypeText { get; set; }

    /// <summary>
    /// 重试次数.
    /// </summary>
    public int? RetryCount { get; set; }

    /// <summary>
    /// 操作人.
    /// </summary>
    public string OperatorName { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 同步日志详情输出（含请求/响应报文）.
/// </summary>
public class YonyouSyncLogDetailDto : YonyouSyncLogListDto
{
    /// <summary>
    /// 请求内容（模拟对接，JSON）.
    /// </summary>
    public string RequestContent { get; set; }

    /// <summary>
    /// 响应内容（模拟对接，JSON）.
    /// </summary>
    public string ResponseContent { get; set; }
}

/// <summary>
/// 手动触发同步输入（拉取方向：用友→MES）.
/// </summary>
public class YonyouSyncPullInput
{
    /// <summary>
    /// 数据类型：生产订单/存货档案/仓库档案.
    /// </summary>
    public string BizType { get; set; }
}

/// <summary>
/// 失败重试输入.
/// </summary>
public class YonyouRetryInput
{
    /// <summary>
    /// 同步日志ID.
    /// </summary>
    public string LogId { get; set; }
}

/// <summary>
/// 手动触发同步输出.
/// </summary>
public class YonyouSyncTriggerOutput
{
    /// <summary>
    /// 日志ID.
    /// </summary>
    public string LogId { get; set; }

    /// <summary>
    /// 提示信息.
    /// </summary>
    public string Message { get; set; }
}

#endregion
