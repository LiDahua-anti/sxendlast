using JNPF.Common.Filter;
using System.Collections.Generic;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 存货档案

/// <summary>
/// 存货档案列表查询条件.
/// </summary>
public class InventoryQuery : PageInputBase
{
    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 存货大类.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// 生产方式：1自制 2委外 3采购，-1或空为全部.
    /// </summary>
    public int MakeType { get; set; } = -1;

    /// <summary>
    /// 有无子件：1有子件 0无子件，-1为全部.
    /// </summary>
    public int HasChild { get; set; } = -1;

    /// <summary>
    /// 状态：1启用 0禁用，-1为全部.
    /// </summary>
    public int Status { get; set; } = -1;
}

/// <summary>
/// 存货档案新增或编辑入参（含工序检查项目配置）.
/// </summary>
public class InventoryCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 存货大类.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// 主计量单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 生产方式：1自制 2委外 3采购.
    /// </summary>
    public int? MakeType { get; set; }

    /// <summary>
    /// 计价方式.
    /// </summary>
    public string PricingMethod { get; set; }

    /// <summary>
    /// 是否批次管理：0否 1是.
    /// </summary>
    public int? BatchManage { get; set; }

    /// <summary>
    /// 启用标识：0禁用 1启用.
    /// </summary>
    public int? EnabledMark { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 工序检查项目配置集合.
    /// </summary>
    public List<InventoryCheckItemDto> CheckItems { get; set; } = new List<InventoryCheckItemDto>();
}

/// <summary>
/// 存货档案列表输出.
/// </summary>
public class InventoryListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 存货大类.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// 主计量单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 生产方式：1自制 2委外 3采购.
    /// </summary>
    public int? MakeType { get; set; }

    /// <summary>
    /// 生产方式名称.
    /// </summary>
    public string MakeTypeName { get; set; }

    /// <summary>
    /// 子件条数（BOM绑定数量）.
    /// </summary>
    public int BomCount { get; set; }

    /// <summary>
    /// 工序数（已配置检查项目的工序数）.
    /// </summary>
    public int ProcessCount { get; set; }

    /// <summary>
    /// 数据来源：1用友U8同步 2手工新增.
    /// </summary>
    public int? Source { get; set; }

    /// <summary>
    /// 同步时间.
    /// </summary>
    public string SyncTime { get; set; }

    /// <summary>
    /// 启用标识：0禁用 1启用.
    /// </summary>
    public int? EnabledMark { get; set; }
}

#endregion

#region 工序检查项目

/// <summary>
/// 存货工序检查项目配置（编辑/绑定入参与输出通用）.
/// </summary>
public class InventoryCheckItemDto
{
    /// <summary>
    /// 主键（新增检查项时为空）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 工序ID（sx_base_process.F_ID）.
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
    /// 检查项目名称.
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
    /// 下拉选项（数据类型为下拉时有效，如：OK,NG）.
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
    /// 排序.
    /// </summary>
    public int Sort { get; set; }
}

/// <summary>
/// 保存某存货的工序检查项目配置入参（整体覆盖式保存）.
/// </summary>
public class InventoryCheckItemInput
{
    /// <summary>
    /// 存货ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 检查项目集合.
    /// </summary>
    public List<InventoryCheckItemDto> CheckItems { get; set; } = new List<InventoryCheckItemDto>();
}

#endregion

#region BOM子件绑定

/// <summary>
/// 绑定子件（BOM）入参（整体覆盖式保存某母件的子件清单）.
/// </summary>
public class InventoryBomBindInput
{
    /// <summary>
    /// 母件存货ID.
    /// </summary>
    public string ParentInventoryId { get; set; }

    /// <summary>
    /// 子件明细集合.
    /// </summary>
    public List<InventoryBomItemDto> Items { get; set; } = new List<InventoryBomItemDto>();
}

/// <summary>
/// BOM子件明细项.
/// </summary>
public class InventoryBomItemDto
{
    /// <summary>
    /// 主键（新增子件时为空）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 子件存货ID.
    /// </summary>
    public string ChildInventoryId { get; set; }

    /// <summary>
    /// 子件存货编码.
    /// </summary>
    public string ChildInventoryCode { get; set; }

    /// <summary>
    /// 子件存货名称.
    /// </summary>
    public string ChildInventoryName { get; set; }

    /// <summary>
    /// 计量单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 使用数量.
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// 损耗率（%）.
    /// </summary>
    public decimal? LossRate { get; set; }

    /// <summary>
    /// 位置号/行号.
    /// </summary>
    public string PositionNo { get; set; }
}

/// <summary>
/// BOM子件明细列表输出.
/// </summary>
public class InventoryBomListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 母件存货ID.
    /// </summary>
    public string ParentInventoryId { get; set; }

    /// <summary>
    /// 子件存货ID.
    /// </summary>
    public string ChildInventoryId { get; set; }

    /// <summary>
    /// 子件存货编码.
    /// </summary>
    public string ChildInventoryCode { get; set; }

    /// <summary>
    /// 子件存货名称.
    /// </summary>
    public string ChildInventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 计量单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 使用数量.
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// 损耗率（%）.
    /// </summary>
    public decimal? LossRate { get; set; }

    /// <summary>
    /// 位置号/行号.
    /// </summary>
    public string PositionNo { get; set; }
}

#endregion

#region 下拉/选择

/// <summary>
/// 存货下拉/选择项（用于绑定子件时的存货检索）.
/// </summary>
public class InventorySelectedOutput
{
    /// <summary>
    /// 存货ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 计量单位.
    /// </summary>
    public string unit { get; set; }
}

#endregion
