using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 打印模板配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_PRINT_TEMPLATE")]
[Tenant(ClaimConst.TENANTID)]
public class PrintDevEntity : CLDEntityBase
{
    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 状态：0-未发布，1-已发布，2-已修改.
    /// </summary>
    [SugarColumn(ColumnName = "F_STATE")]
    public int? State { get; set; }

    /// <summary>
    /// 通用-将该模板设为通用(0-表单用，1-业务打印模板用).
    /// </summary>
    [SugarColumn(ColumnName = "F_COMMON_USE")]
    public int? CommonUse { get; set; }

    /// <summary>
    /// 发布范围：1-公开，2-权限设置.
    /// </summary>
    [SugarColumn(ColumnName = "F_VISIBLE_TYPE")]
    public int? VisibleType { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string Icon { get; set; }

    /// <summary>
    /// 图标颜色.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON_BACKGROUND")]
    public string IconBackground { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SYSTEM_ID")]
    public string SystemId { get; set; }
}