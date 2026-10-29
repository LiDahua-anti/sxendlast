using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 常用功能.
/// </summary>
[SugarTable("BASE_MODULE_DATA")]
public class ModuleDataEntity : CLDSEntityBase
{
    /// <summary>
    /// 模块类型（Web，App）.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_TYPE")]
    public string ModuleType { get; set; }

    /// <summary>
    /// 模块主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>.
    [SugarColumn(ColumnName = "F_SYSTEM_ID")]
    public string SystemId { get; set; }
}