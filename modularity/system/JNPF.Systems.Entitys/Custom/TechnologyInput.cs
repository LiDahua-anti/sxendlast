using JNPF.Common.Filter;
using System;
using System.Collections.Generic;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 工艺路线管理

/// <summary>
/// 工艺路线列表查询条件.
/// </summary>
public class RoutingQuery : PageInputBase
{
    /// <summary>
    /// 路线编号.
    /// </summary>
    public string RoutingCode { get; set; }

    /// <summary>
    /// 路线名称.
    /// </summary>
    public string RoutingName { get; set; }

    /// <summary>
    /// 存货编码/名称关键字（按存货维度查询）.
    /// </summary>
    public string InventoryKeyword { get; set; }

    /// <summary>
    /// 状态：1生效 2停用 3草稿，-1为全部.
    /// </summary>
    public int Status { get; set; } = -1;
}

/// <summary>
/// 工艺路线新增或编辑入参（含工序顺序明细，整体覆盖保存）.
/// </summary>
public class RoutingCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 路线编号（如GY-EH01-09B-V1）.
    /// </summary>
    public string RoutingCode { get; set; }

    /// <summary>
    /// 路线名称.
    /// </summary>
    public string RoutingName { get; set; }

    /// <summary>
    /// 适用存货（母件）ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 状态：1生效 2停用 3草稿.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 工序顺序明细（从工序管理中选择，按顺序排布）.
    /// </summary>
    public List<RoutingProcessItem> Processes { get; set; } = new List<RoutingProcessItem>();
}

/// <summary>
/// 工艺路线工序明细行（保存入参）.
/// </summary>
public class RoutingProcessItem
{
    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 标准工时（min）.
    /// </summary>
    public decimal? StandardHours { get; set; }

    /// <summary>
    /// 工序要求.
    /// </summary>
    public string Requirement { get; set; }

    /// <summary>
    /// 是否检查工序（最后一步检查）：0否 1是.
    /// </summary>
    public int? IsCheckProcess { get; set; }
}

/// <summary>
/// 工艺路线工序明细输出（含检查项目/标准串）.
/// </summary>
public class RoutingProcessOutput
{
    /// <summary>
    /// 明细主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 顺序号.
    /// </summary>
    public int? SeqNo { get; set; }

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
    /// 标准工时（min）.
    /// </summary>
    public decimal? StandardHours { get; set; }

    /// <summary>
    /// 工序要求.
    /// </summary>
    public string Requirement { get; set; }

    /// <summary>
    /// 是否检查工序：0否 1是.
    /// </summary>
    public int? IsCheckProcess { get; set; }

    /// <summary>
    /// 检查项目（如：基准/厚度/螺纹孔通止端）.
    /// </summary>
    public string CheckItems { get; set; }

    /// <summary>
    /// 检查标准（如：按图纸±0.02 / 3.0±0.05 / 通规通过）.
    /// </summary>
    public string CheckStandards { get; set; }
}

/// <summary>
/// 工艺路线列表输出（含创建人名称、格式化时间）.
/// </summary>
public class RoutingListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 路线编号.
    /// </summary>
    public string RoutingCode { get; set; }

    /// <summary>
    /// 路线名称.
    /// </summary>
    public string RoutingName { get; set; }

    /// <summary>
    /// 适用存货（母件）ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 母件编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 母件名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 工序数.
    /// </summary>
    public int? ProcessCount { get; set; }

    /// <summary>
    /// 总标准工时（min）.
    /// </summary>
    public decimal? TotalHours { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 状态：1生效 2停用 3草稿.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 状态名称.
    /// </summary>
    public string StatusName { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 创建人ID.
    /// </summary>
    public string CreatorUserId { get; set; }

    /// <summary>
    /// 创建人名称.
    /// </summary>
    public string CreatorUserName { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? CreatorTime { get; set; }

    /// <summary>
    /// 创建时间（格式化）.
    /// </summary>
    public string CreatorTimeText { get; set; }
}

/// <summary>
/// 工艺路线详情输出.
/// </summary>
public class RoutingDetailOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 路线编号.
    /// </summary>
    public string RoutingCode { get; set; }

    /// <summary>
    /// 路线名称.
    /// </summary>
    public string RoutingName { get; set; }

    /// <summary>
    /// 适用存货（母件）ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 母件编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 母件名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 工序数.
    /// </summary>
    public int? ProcessCount { get; set; }

    /// <summary>
    /// 总标准工时（min）.
    /// </summary>
    public decimal? TotalHours { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 状态：1生效 2停用 3草稿.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 工序顺序明细.
    /// </summary>
    public List<RoutingProcessOutput> Processes { get; set; } = new List<RoutingProcessOutput>();
}

/// <summary>
/// 工艺路线批量生效/停用入参.
/// </summary>
public class RoutingBatchStatusInput
{
    /// <summary>
    /// 路线ID集合.
    /// </summary>
    public List<string> Ids { get; set; } = new List<string>();

    /// <summary>
    /// 目标状态：1生效 2停用.
    /// </summary>
    public int Status { get; set; }
}

/// <summary>
/// 工艺路线下拉输出（供排产选择）.
/// </summary>
public class RoutingSelectedOutput
{
    /// <summary>
    /// 路线ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 路线编号.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 路线名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 适用存货ID.
    /// </summary>
    public string inventoryId { get; set; }

    /// <summary>
    /// 适用存货名称.
    /// </summary>
    public string inventoryName { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public string version { get; set; }

    /// <summary>
    /// 工序数.
    /// </summary>
    public int? processCount { get; set; }
}

#endregion

#region 工序管理

/// <summary>
/// 工序列表查询条件（复用 sx_base_process）.
/// </summary>
public class ProcessQuery : PageInputBase
{
    /// <summary>
    /// 工序编码.
    /// </summary>
    public string ProcessCode { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 有无检查项：1有 0无，-1为全部.
    /// </summary>
    public int HasCheckItem { get; set; } = -1;
}

/// <summary>
/// 工序新增或编辑入参（复用 sx_base_process）.
/// </summary>
public class ProcessCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 工序编码.
    /// </summary>
    public string ProcessCode { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 工序类型（机加工/钣金/表面处理/检验包装/仓储）.
    /// </summary>
    public string ProcessType { get; set; }

    /// <summary>
    /// 标准周期.
    /// </summary>
    public string StandardCycle { get; set; }

    /// <summary>
    /// 作业场所.
    /// </summary>
    public string WorkLocation { get; set; }

    /// <summary>
    /// 工序说明（工序内容）.
    /// </summary>
    public string ProcessContent { get; set; }

    /// <summary>
    /// 启用标识：0禁用 1启用.
    /// </summary>
    public int? EnabledMark { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 工序列表输出（含检查项目数/关联设备数/是否有作业指导）.
/// </summary>
public class ProcessListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 工序编码.
    /// </summary>
    public string ProcessCode { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 工序类型.
    /// </summary>
    public string ProcessType { get; set; }

    /// <summary>
    /// 检查项目数.
    /// </summary>
    public int CheckItemCount { get; set; }

    /// <summary>
    /// 关联设备数.
    /// </summary>
    public int DeviceCount { get; set; }

    /// <summary>
    /// 是否有作业指导：1有 0无.
    /// </summary>
    public int HasSop { get; set; }

    /// <summary>
    /// 启用标识：0禁用 1启用.
    /// </summary>
    public int? EnabledMark { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? SortCode { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? CreatorTime { get; set; }
}

/// <summary>
/// 工序检查内容定义行（保存入参）.
/// </summary>
public class ProcessCheckItemDto
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
    /// 数据类型：1数值 2文本 3下拉.
    /// </summary>
    public int? DataType { get; set; }

    /// <summary>
    /// 下拉选项（如:OK,NG）.
    /// </summary>
    public string OptionItems { get; set; }

    /// <summary>
    /// 默认值.
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// 是否必填：0否 1是.
    /// </summary>
    public int? Required { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 工序检查内容整体覆盖保存入参.
/// </summary>
public class ProcessCheckItemSaveInput
{
    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 检查项目行集合.
    /// </summary>
    public List<ProcessCheckItemDto> Items { get; set; } = new List<ProcessCheckItemDto>();
}

/// <summary>
/// 工序设备绑定整体覆盖保存入参.
/// </summary>
public class ProcessDeviceSaveInput
{
    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 设备ID集合（整体覆盖）.
    /// </summary>
    public List<string> DeviceIds { get; set; } = new List<string>();
}

/// <summary>
/// 工序SOP文件上传记录入参.
/// </summary>
public class ProcessSopAddInput
{
    /// <summary>
    /// 工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 文件名.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 文件类型（PDF/图片）.
    /// </summary>
    public string FileType { get; set; }

    /// <summary>
    /// 文件大小（展示用,如2.4MB）.
    /// </summary>
    public string FileSize { get; set; }

    /// <summary>
    /// 文件存储路径.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// 版本.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 工序下拉输出（供工艺路线选择工序）.
/// </summary>
public class ProcessSelectedOutput
{
    /// <summary>
    /// 工序ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 工序编号.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 工序名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 工序类型.
    /// </summary>
    public string processType { get; set; }

    /// <summary>
    /// 作业场所.
    /// </summary>
    public string workLocation { get; set; }
}

#endregion

#region 齐套性检查

/// <summary>
/// 齐套检查入参（两种模式：①母件存货+投产数量；②生产订单）.
/// </summary>
public class KittingCheckInput
{
    /// <summary>
    /// 模式一：母件存货ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 模式一：投产数量.
    /// </summary>
    public decimal? PlanQty { get; set; }

    /// <summary>
    /// 模式二：生产订单ID（取订单产品与计划数量）.
    /// </summary>
    public string OrderId { get; set; }
}

/// <summary>
/// 齐套检查明细行输出.
/// </summary>
public class KittingItemDto
{
    /// <summary>
    /// 序号.
    /// </summary>
    public int SeqNo { get; set; }

    /// <summary>
    /// 子件编码.
    /// </summary>
    public string ChildInventoryCode { get; set; }

    /// <summary>
    /// 子件名称.
    /// </summary>
    public string ChildInventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 单个用量.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 损耗率（%）.
    /// </summary>
    public decimal LossRate { get; set; }

    /// <summary>
    /// 需用数量 = 单个用量 × 投产数量 × (1 + 损耗率/100).
    /// </summary>
    public decimal RequireQty { get; set; }

    /// <summary>
    /// 现有库存（可用量=在库-占用）.
    /// </summary>
    public decimal AvailableQty { get; set; }

    /// <summary>
    /// 缺口数量.
    /// </summary>
    public decimal LackQty { get; set; }

    /// <summary>
    /// 状态：1满足 0缺料.
    /// </summary>
    public int Satisfied { get; set; }
}

/// <summary>
/// 齐套检查结果输出.
/// </summary>
public class KittingCheckOutput
{
    /// <summary>
    /// 母件编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 母件名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 投产数量.
    /// </summary>
    public decimal PlanQty { get; set; }

    /// <summary>
    /// 齐套率（%，满足项数/总项数）.
    /// </summary>
    public decimal KittingRate { get; set; }

    /// <summary>
    /// 缺料项数.
    /// </summary>
    public int LackCount { get; set; }

    /// <summary>
    /// 缺料明细.
    /// </summary>
    public List<KittingItemDto> Items { get; set; } = new List<KittingItemDto>();
}

#endregion
