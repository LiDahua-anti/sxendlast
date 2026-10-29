using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

/// <summary>
/// 点检设备分配表
/// </summary>
[SugarTable("sx_device_item_allot")]
public class DeviceItemAllotEntity : CLDSEntityBase
{
    /// <summary>
    /// 点检项目主表id
    /// </summary>
    [SugarColumn(ColumnName = "main_id", IsNullable = false, ColumnDescription = "/// 点检项目主表id ///")]
    [Description("点检项目主表id")]
    public string MainId { get; set; }

    /// <summary>
    /// 设备id
    /// </summary>
    [SugarColumn(ColumnName = "device_id", Length = 200, IsNullable = false, ColumnDescription = "/// 设备id ///")]
    [Description("设备id")]
    public string? DeviceId { get; set; }
}