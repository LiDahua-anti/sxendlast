using JNPF.Common.Filter;
using System;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 供应商管理

/// <summary>
/// 供应商列表查询条件.
/// </summary>
public class SupplierQuery : PageInputBase
{
    /// <summary>
    /// 编码/名称/简称关键字.
    /// </summary>
    public string Keyword { get; set; }

    /// <summary>
    /// 状态：0禁用 1启用，-1为全部.
    /// </summary>
    public int Status { get; set; } = -1;
}

/// <summary>
/// 供应商新增或编辑入参.
/// </summary>
public class SupplierCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 供应商编码.
    /// </summary>
    public string SupplierCode { get; set; }

    /// <summary>
    /// 供应商代码（用友U8同步的外码）.
    /// </summary>
    public string SupplierNo { get; set; }

    /// <summary>
    /// 供应商名称.
    /// </summary>
    public string SupplierName { get; set; }

    /// <summary>
    /// 供应商简称.
    /// </summary>
    public string ShortName { get; set; }

    /// <summary>
    /// 联系人.
    /// </summary>
    public string ContactPerson { get; set; }

    /// <summary>
    /// 联系电话.
    /// </summary>
    public string ContactPhone { get; set; }

    /// <summary>
    /// 地址.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// 税号.
    /// </summary>
    public string TaxNumber { get; set; }

    /// <summary>
    /// 开户行.
    /// </summary>
    public string BankName { get; set; }

    /// <summary>
    /// 银行账号.
    /// </summary>
    public string BankAccount { get; set; }

    /// <summary>
    /// 状态：0禁用 1启用.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 供应商下拉选项输出.
/// </summary>
public class SupplierSelectedOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 供应商编码.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 供应商名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 联系人.
    /// </summary>
    public string contactPerson { get; set; }

    /// <summary>
    /// 联系电话.
    /// </summary>
    public string contactPhone { get; set; }
}

#endregion

#region 终端设备管理

/// <summary>
/// 终端设备列表查询条件.
/// </summary>
public class TerminalQuery : PageInputBase
{
    /// <summary>
    /// 编码/名称/工位/IP关键字.
    /// </summary>
    public string Keyword { get; set; }

    /// <summary>
    /// 状态：1启用 2锁定 3禁用，-1为全部.
    /// </summary>
    public int Status { get; set; } = -1;
}

/// <summary>
/// 终端设备新增或编辑入参.
/// </summary>
public class TerminalCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 终端设备编码.
    /// </summary>
    public string TerminalCode { get; set; }

    /// <summary>
    /// 工位（如01/02）.
    /// </summary>
    public string Station { get; set; }

    /// <summary>
    /// 终端设备名称（如手持PDA）.
    /// </summary>
    public string TerminalName { get; set; }

    /// <summary>
    /// 终端设备IP.
    /// </summary>
    public string TerminalIp { get; set; }

    /// <summary>
    /// 负责人.
    /// </summary>
    public string ChargePerson { get; set; }

    /// <summary>
    /// 状态：1启用 2锁定 3禁用.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

#endregion

#region 模具管理

/// <summary>
/// 模具列表查询条件.
/// </summary>
public class MoldQuery : PageInputBase
{
    /// <summary>
    /// 模具编号/名称/产品关键字.
    /// </summary>
    public string Keyword { get; set; }

    /// <summary>
    /// 状态：1在用 2维修 3报废，-1为全部.
    /// </summary>
    public int Status { get; set; } = -1;
}

/// <summary>
/// 模具新增或编辑入参.
/// </summary>
public class MoldCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 模具编号.
    /// </summary>
    public string MoldCode { get; set; }

    /// <summary>
    /// 模具名称.
    /// </summary>
    public string MoldName { get; set; }

    /// <summary>
    /// 对应产品（图番/产品名称）.
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// 模具寿命（模）.
    /// </summary>
    public int? MoldLife { get; set; }

    /// <summary>
    /// 已用次数（模）.
    /// </summary>
    public int? UsedCount { get; set; }

    /// <summary>
    /// 状态：1在用 2维修 3报废.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 上次保养日期.
    /// </summary>
    public DateTime? LastMaintainDate { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

#endregion

#region 汇率管理

/// <summary>
/// 汇率列表查询条件.
/// </summary>
public class ExchangeRateQuery : PageInputBase
{
    /// <summary>
    /// 币种.
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// 汇率日期起（yyyy-MM-dd）.
    /// </summary>
    public string StartDate { get; set; }

    /// <summary>
    /// 汇率日期止（yyyy-MM-dd）.
    /// </summary>
    public string EndDate { get; set; }
}

/// <summary>
/// 汇率新增或编辑入参.
/// </summary>
public class ExchangeRateCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 币种.
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// 对人民币汇率（4位小数）.
    /// </summary>
    public string RateToCny { get; set; }

    /// <summary>
    /// 汇率日期.
    /// </summary>
    public DateTime? RateDate { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

#endregion
