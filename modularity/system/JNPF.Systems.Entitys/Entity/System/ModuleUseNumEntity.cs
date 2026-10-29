using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 功能使用次数.
/// </summary>
[SugarTable("BASE_MODULE_USE_NUM")]
[Tenant(ClaimConst.TENANTID)]
public class ModuleUseNumEntity : CLDSEntityBase
{
    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 功能id.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 使用次数.
    /// </summary>
    [SugarColumn(ColumnName = "F_USE_NUM")]
    public int UseNum { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 系统编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_SYSTEM_CODE")]
    public string SystemCode { get; set; }
}