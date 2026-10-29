using JNPF.Common.Const;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.VisualData.Entity;

/// <summary>
/// 可视化地图配置表.
/// </summary>
[SugarTable("BLADE_VISUAL_MAP")]
[Tenant(ClaimConst.TENANTID)]
public class VisualMapEntity : ITenantFilter
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "ID", ColumnDescription = "主键", IsPrimaryKey = true)]
    public string Id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "Name", ColumnDescription = "名称")]
    public string Name { get; set; }

    /// <summary>
    /// 地图数据.
    /// </summary>
    [SugarColumn(ColumnName = "DATA", ColumnDescription = "地图数据")]
    public string Data { get; set; }

    /// <summary>
    /// 地图编码.
    /// </summary>
    [SugarColumn(ColumnName = "CODE", ColumnDescription = "地图编码")]
    public string Code { get; set; }

    /// <summary>
    /// 地图级别 0:国家 1:省份 2:城市 3:区县.
    /// </summary>
    [SugarColumn(ColumnName = "MAP_LEVEL", ColumnDescription = "地图级别 0:国家 1:省份 2:城市 3:区县")]
    public int Level { get; set; }

    /// <summary>
    /// 上级ID.
    /// </summary>
    [SugarColumn(ColumnName = "PARENT_ID", ColumnDescription = "上级ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 上级编码.
    /// </summary>
    [SugarColumn(ColumnName = "PARENT_CODE", ColumnDescription = "上级编码")]
    public string ParentCode { get; set; }

    /// <summary>
    /// 祖编码.
    /// </summary>
    [SugarColumn(ColumnName = "ANCESTORS", ColumnDescription = "祖编码")]
    public string Ancestors { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    public string TenantId { get; set; }
}
