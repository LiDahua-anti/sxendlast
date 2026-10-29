using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 系统功能.
/// </summary>
[SugarTable("BASE_SYSTEM")]
public class SystemEntity : CLDSEntityBase
{
    /// <summary>
    /// 系统名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 系统编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 门户数组.
    /// </summary>
    [SugarColumn(ColumnName = "F_PORTAL_ID")]
    public string PortalId { get; set; } = string.Empty;

    /// <summary>
    /// app门户数组.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_PORTAL_ID")]
    public string AppPortalId { get; set; } = string.Empty;

    /// <summary>
    /// 应用图标.
    /// </summary>
    [SugarColumn(ColumnName = "F_ICON")]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 图标背景色.
    /// </summary>
    [SugarColumn(ColumnName = "F_BACKGROUND_COLOR")]
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>
    /// 是否是主系统（0-不是，1-是）.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_MAIN")]
    public int IsMain { get; set; }

    /// <summary>
    /// 偏好配置.
    /// </summary>
    [SugarColumn(ColumnName = "F_PREFERENCE_JSON")]
    public string PreferenceJson { get; set; }

    /// <summary>
    /// 扩展属性.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON")]
    public string PropertyJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 所属用户(应用创建者).
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 授权id列表（用户id--user、角色id--role）.
    /// </summary>
    [SugarColumn(ColumnName = "F_AUTHORIZE_ID")]
    public string AuthorizeId { get; set; }
}
