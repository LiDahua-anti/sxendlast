using JNPF.Common.Filter;
using System;
using System.Collections.Generic;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 来料检验（IQC）

/// <summary>
/// 来料检验列表查询条件.
/// </summary>
public class IqcQuery : PageInputBase
{
    /// <summary>
    /// 检验单号（模糊）.
    /// </summary>
    public string CheckNo { get; set; }

    /// <summary>
    /// 到货单号（模糊）.
    /// </summary>
    public string ArrivalNo { get; set; }

    /// <summary>
    /// 检验结果：1合格 2不合格 3免检，-1为全部.
    /// </summary>
    public int CheckResult { get; set; } = -1;
}

/// <summary>
/// 来料检验列表输出.
/// </summary>
public class IqcListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 检验单号.
    /// </summary>
    public string CheckNo { get; set; }

    /// <summary>
    /// 到货单号.
    /// </summary>
    public string ArrivalNo { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 检验方式：1扫码检查 2免检.
    /// </summary>
    public int? CheckType { get; set; }

    /// <summary>
    /// 检查项目摘要（如：外观 OK / 高度 OK）.
    /// </summary>
    public string CheckItemsSummary { get; set; }

    /// <summary>
    /// 检验结果：1合格 2不合格 3免检.
    /// </summary>
    public int? CheckResult { get; set; }

    /// <summary>
    /// 检验员.
    /// </summary>
    public string CheckerName { get; set; }

    /// <summary>
    /// 检验时间.
    /// </summary>
    public DateTime? CheckTime { get; set; }

    /// <summary>
    /// 不良处理状态：0无需处理 1待处理 2处理中 3已处理.
    /// </summary>
    public int? BadHandleStatus { get; set; }
}

/// <summary>
/// 来料检验统计卡片（本月检验批次/合格率/免检批次/不合格批次）.
/// </summary>
public class IqcStatisticsOutput
{
    /// <summary>
    /// 本月检验批次.
    /// </summary>
    public int MonthCheckCount { get; set; }

    /// <summary>
    /// 合格率（%）.
    /// </summary>
    public decimal PassRate { get; set; }

    /// <summary>
    /// 免检批次.
    /// </summary>
    public int FreeCheckCount { get; set; }

    /// <summary>
    /// 不合格批次.
    /// </summary>
    public int BadCount { get; set; }
}

/// <summary>
/// 检验明细项入参（检查项目实测值与判定）.
/// </summary>
public class CheckDetailItemInput
{
    /// <summary>
    /// 检查项目.
    /// </summary>
    public string CheckItem { get; set; }

    /// <summary>
    /// 检查标准.
    /// </summary>
    public string CheckStandard { get; set; }

    /// <summary>
    /// 实测值.
    /// </summary>
    public string ActualValue { get; set; }

    /// <summary>
    /// 判定结果：OK/NG.
    /// </summary>
    public string CheckResult { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 检验明细项输出.
/// </summary>
public class CheckDetailItemOutput
{
    /// <summary>
    /// 检查项目.
    /// </summary>
    public string CheckItem { get; set; }

    /// <summary>
    /// 检查标准.
    /// </summary>
    public string CheckStandard { get; set; }

    /// <summary>
    /// 实测值.
    /// </summary>
    public string ActualValue { get; set; }

    /// <summary>
    /// 判定结果：OK/NG.
    /// </summary>
    public string CheckResult { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 来料检验登记入参（PDA采购入库扫码登记；免检规则自动跳过检查项）.
/// </summary>
public class IqcRegisterInput
{
    /// <summary>
    /// 到货单主表ID.
    /// </summary>
    public string ArrivalId { get; set; }

    /// <summary>
    /// 到货单号.
    /// </summary>
    public string ArrivalNo { get; set; }

    /// <summary>
    /// 到货单明细ID.
    /// </summary>
    public string ArrivalDetailId { get; set; }

    /// <summary>
    /// 供应商ID.
    /// </summary>
    public string SupplierId { get; set; }

    /// <summary>
    /// 供应商名称.
    /// </summary>
    public string SupplierName { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 存货ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 检验方式：1扫码检查 2免检（免检自动跳过检查项并标记免检）.
    /// </summary>
    public int CheckType { get; set; } = 1;

    /// <summary>
    /// 检验数量.
    /// </summary>
    public decimal? CheckQty { get; set; }

    /// <summary>
    /// 不良数.
    /// </summary>
    public decimal? BadQty { get; set; }

    /// <summary>
    /// 检验员（为空取当前用户）.
    /// </summary>
    public string CheckerName { get; set; }

    /// <summary>
    /// 检验时间（为空取当前时间）.
    /// </summary>
    public DateTime? CheckTime { get; set; }

    /// <summary>
    /// 检查项目明细（免检时忽略）.
    /// </summary>
    public List<CheckDetailItemInput> Details { get; set; } = new List<CheckDetailItemInput>();

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 来料检验明细输出（检验明细弹窗）.
/// </summary>
public class IqcDetailOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 检验单号.
    /// </summary>
    public string CheckNo { get; set; }

    /// <summary>
    /// 到货单号.
    /// </summary>
    public string ArrivalNo { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 检验数量.
    /// </summary>
    public decimal? CheckQty { get; set; }

    /// <summary>
    /// 不良数.
    /// </summary>
    public decimal? BadQty { get; set; }

    /// <summary>
    /// 检验方式：1扫码检查 2免检.
    /// </summary>
    public int? CheckType { get; set; }

    /// <summary>
    /// 检验结果：1合格 2不合格 3免检.
    /// </summary>
    public int? CheckResult { get; set; }

    /// <summary>
    /// 检查项目摘要.
    /// </summary>
    public string CheckItemsSummary { get; set; }

    /// <summary>
    /// 检验员.
    /// </summary>
    public string CheckerName { get; set; }

    /// <summary>
    /// 检验时间.
    /// </summary>
    public DateTime? CheckTime { get; set; }

    /// <summary>
    /// 不良处理状态.
    /// </summary>
    public int? BadHandleStatus { get; set; }

    /// <summary>
    /// 不良处理说明.
    /// </summary>
    public string BadHandleRemark { get; set; }

    /// <summary>
    /// 检查项目明细.
    /// </summary>
    public List<CheckDetailItemOutput> Items { get; set; } = new List<CheckDetailItemOutput>();
}

#endregion

#region 质量检验（OQC抽检）

/// <summary>
/// 质量抽检列表查询条件.
/// </summary>
public class OqcQuery : PageInputBase
{
    /// <summary>
    /// 抽检单号（模糊）.
    /// </summary>
    public string CheckNo { get; set; }

    /// <summary>
    /// 流程卡号（模糊）.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 工序（CNC/清洗/检包/入库）.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 判定：1合格 2不合格，-1为全部.
    /// </summary>
    public int CheckResult { get; set; } = -1;
}

/// <summary>
/// 质量抽检列表输出.
/// </summary>
public class OqcListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 抽检单号.
    /// </summary>
    public string CheckNo { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 工序（CNC/清洗/检包/入库）.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 检查项目摘要（如：基准 OK / 厚度 OK）.
    /// </summary>
    public string CheckItemsSummary { get; set; }

    /// <summary>
    /// 抽检数（按批抽样）.
    /// </summary>
    public decimal? SampleQty { get; set; }

    /// <summary>
    /// 不良数.
    /// </summary>
    public decimal? BadQty { get; set; }

    /// <summary>
    /// 判定：1合格 2不合格.
    /// </summary>
    public int? CheckResult { get; set; }

    /// <summary>
    /// 质检员.
    /// </summary>
    public string CheckerName { get; set; }

    /// <summary>
    /// 抽检时间.
    /// </summary>
    public DateTime? CheckTime { get; set; }

    /// <summary>
    /// 不良处理状态.
    /// </summary>
    public int? BadHandleStatus { get; set; }
}

/// <summary>
/// 质量抽检统计卡片（今日抽检批次/合格率/待检包检查/不良批次）.
/// </summary>
public class OqcStatisticsOutput
{
    /// <summary>
    /// 今日抽检批次.
    /// </summary>
    public int TodayCount { get; set; }

    /// <summary>
    /// 合格率（%）.
    /// </summary>
    public decimal PassRate { get; set; }

    /// <summary>
    /// 待检包检查（生产中存在检查工序未完成的流程卡数）.
    /// </summary>
    public int WaitCheckCount { get; set; }

    /// <summary>
    /// 不良批次（本月不合格）.
    /// </summary>
    public int BadCount { get; set; }
}

/// <summary>
/// 质量抽检登记入参（PDA品质抽检扫码登记，按批抽样判定）.
/// </summary>
public class OqcRegisterInput
{
    /// <summary>
    /// 流程卡ID（扫码带出）.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 工序ID（工序选项卡：CNC/清洗/检包/入库）.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 关联报工记录ID（可空）.
    /// </summary>
    public string ReportId { get; set; }

    /// <summary>
    /// 抽检数（按批抽样）.
    /// </summary>
    public decimal? SampleQty { get; set; }

    /// <summary>
    /// 不良数.
    /// </summary>
    public decimal? BadQty { get; set; }

    /// <summary>
    /// 质检员（为空取当前用户）.
    /// </summary>
    public string CheckerName { get; set; }

    /// <summary>
    /// 抽检时间（为空取当前时间）.
    /// </summary>
    public DateTime? CheckTime { get; set; }

    /// <summary>
    /// 检查项目明细（按工序检查内容定义带出）.
    /// </summary>
    public List<CheckDetailItemInput> Details { get; set; } = new List<CheckDetailItemInput>();

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 质量抽检明细输出（抽检明细弹窗）.
/// </summary>
public class OqcDetailOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 抽检单号.
    /// </summary>
    public string CheckNo { get; set; }

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
    /// 工序.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 抽检数.
    /// </summary>
    public decimal? SampleQty { get; set; }

    /// <summary>
    /// 不良数.
    /// </summary>
    public decimal? BadQty { get; set; }

    /// <summary>
    /// 判定：1合格 2不合格.
    /// </summary>
    public int? CheckResult { get; set; }

    /// <summary>
    /// 检查项目摘要.
    /// </summary>
    public string CheckItemsSummary { get; set; }

    /// <summary>
    /// 质检员.
    /// </summary>
    public string CheckerName { get; set; }

    /// <summary>
    /// 抽检时间.
    /// </summary>
    public DateTime? CheckTime { get; set; }

    /// <summary>
    /// 不良处理状态.
    /// </summary>
    public int? BadHandleStatus { get; set; }

    /// <summary>
    /// 不良处理说明.
    /// </summary>
    public string BadHandleRemark { get; set; }

    /// <summary>
    /// 检查项目明细.
    /// </summary>
    public List<CheckDetailItemOutput> Items { get; set; } = new List<CheckDetailItemOutput>();
}

/// <summary>
/// 不良处理入参（IQC/OQC通用：记录不良处理状态与说明）.
/// </summary>
public class BadHandleInput
{
    /// <summary>
    /// 主键（IQC或OQC记录ID）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 不良处理状态：1待处理 2处理中 3已处理.
    /// </summary>
    public int BadHandleStatus { get; set; } = 1;

    /// <summary>
    /// 不良处理说明.
    /// </summary>
    public string BadHandleRemark { get; set; }
}

/// <summary>
/// 工序检查内容定义输出（PDA检查项目表按工序带出）.
/// </summary>
public class CheckItemDefineOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 检查项目.
    /// </summary>
    public string CheckItem { get; set; }

    /// <summary>
    /// 检查标准.
    /// </summary>
    public string CheckStandard { get; set; }

    /// <summary>
    /// 数据类型：1数值 2文本 3下拉.
    /// </summary>
    public int? DataType { get; set; }

    /// <summary>
    /// 下拉选项.
    /// </summary>
    public string Options { get; set; }

    /// <summary>
    /// 默认值.
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// 是否必填：0否 1是.
    /// </summary>
    public int? IsRequired { get; set; }

    /// <summary>
    /// 顺序号.
    /// </summary>
    public int? SeqNo { get; set; }
}

#endregion

#region 设备报警履历

/// <summary>
/// 设备报警履历查询条件.
/// </summary>
public class DeviceAlarmQuery : PageInputBase
{
    /// <summary>
    /// 设备编号（模糊）.
    /// </summary>
    public string DeviceCode { get; set; }

    /// <summary>
    /// 设备名称（模糊）.
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    /// 报警类型（温度异常/压力异常/通讯中断/急停触发）.
    /// </summary>
    public string AlarmType { get; set; }

    /// <summary>
    /// 处理状态：0未恢复 1已恢复 2已处理，-1为全部.
    /// </summary>
    public int HandleStatus { get; set; } = -1;

    /// <summary>
    /// 触发日期-起.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 触发日期-止.
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 设备报警登记与编辑入参.
/// </summary>
public class DeviceAlarmInput
{
    /// <summary>
    /// 主键（为空新增，非空编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 设备ID（可空，填了则回填设备编号/名称）.
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    /// 设备编号.
    /// </summary>
    public string DeviceCode { get; set; }

    /// <summary>
    /// 设备名称.
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    /// 报警类型（温度异常/压力异常/通讯中断/急停触发）.
    /// </summary>
    public string AlarmType { get; set; }

    /// <summary>
    /// 报警内容.
    /// </summary>
    public string AlarmContent { get; set; }

    /// <summary>
    /// 触发时间（为空取当前时间）.
    /// </summary>
    public DateTime? TriggerTime { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 设备报警处理入参（恢复/处理登记）.
/// </summary>
public class AlarmHandleInput
{
    /// <summary>
    /// 报警主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 处理状态：0未恢复 1已恢复 2已处理.
    /// </summary>
    public int HandleStatus { get; set; }

    /// <summary>
    /// 处理人（为空取当前用户）.
    /// </summary>
    public string HandlerName { get; set; }

    /// <summary>
    /// 恢复时间（为空取当前时间）.
    /// </summary>
    public DateTime? RecoverTime { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

#endregion
