using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.VisualData.Entity;

/// <summary>
/// 静态资源表.
/// </summary>
[SugarTable("BLADE_VISUAL_ASSETS")]
public class VisualAssetsEntity : ITenantFilter
{
    /// <summary>
    /// 主键.
    /// </summary>
    [SugarColumn(ColumnName = "ID", ColumnDescription = "主键", IsPrimaryKey = true)]
    public string Id { get; set; }

    /// <summary>
    /// 资源名称.
    /// </summary>
    [SugarColumn(ColumnName = "ASSETSNAME", ColumnDescription = "资源名称")]
    public string AssetsName { get; set; }

    /// <summary>
    /// 资源大小 1M.
    /// </summary>
    [SugarColumn(ColumnName = "ASSETSSIZE", ColumnDescription = "资源大小 1M")]
    public string AssetsSize { get; set; }

    /// <summary>
    /// 资源上传时间.
    /// </summary>
    [SugarColumn(ColumnName = "ASSETSTIME", ColumnDescription = "资源上传时间")]
    public DateTime? AssetsTime { get; set; }

    /// <summary>
    /// 资源后缀名.
    /// </summary>
    [SugarColumn(ColumnName = "ASSETSTYPE", ColumnDescription = "资源后缀名")]
    public string AssetsType { get; set; }

    /// <summary>
    /// 资源地址.
    /// </summary>
    [SugarColumn(ColumnName = "ASSETSURL", ColumnDescription = "资源地址")]
    public string AssetsUrl { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    [SugarColumn(ColumnName = "SYSTEM_ID", ColumnDescription = "系统id")]
    public string SystemId { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    public string TenantId { get; set; }
}
