using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 用户信息扩展基类.
/// </summary>
[SugarTable("BASE_USER_EXTRA")]
public class UserExtraEntity : CLDSEntityBase
{
    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 常用菜单.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_COMMON_MENU")]
    public string CommonMenu { get; set; }

    /// <summary>
    /// 扩展属性.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON")]
    public string PropertyJson { get; set; }

    /// <summary>
    /// 偏好配置.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_PREFERENCE_JSON")]
    public string PreferenceJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    /// <returns></returns>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}