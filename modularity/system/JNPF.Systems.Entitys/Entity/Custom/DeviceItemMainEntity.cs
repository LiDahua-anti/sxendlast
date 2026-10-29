using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

/// <summary>
/// 点检项目主表
/// </summary>
[SugarTable("sx_device_item_main")]
public class DeviceItemMainEntity : CLDSEntityBase
{
    /// <summary>
    /// 项目名称
    /// </summary>
    [SugarColumn(ColumnName = "item_name", Length = 200, IsNullable = false, ColumnDescription = "/// 项目名称 ///")]
    [Description("项目名称")]
    public string? ItemName { get; set; }

    /// <summary>
    /// 所属
    /// </summary>
    [SugarColumn(ColumnName = "belongs", Length = 200, IsNullable = true, ColumnDescription = "/// 所属 ///")]
    [Description("所属")]
    public string? Belongs { get; set; }

    /// <summary>
    /// 项目类型（0日 1月 2年）
    /// </summary>
    [SugarColumn(ColumnName = "type", Length = 200, IsNullable = false, ColumnDescription = "/// 项目类型（0日 1月 2年） ///")]
    [Description("项目类型（0日 1月 2年）")]
    public string? Type { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "/// 备注 ///")]
    [Description("备注")]
    public string? Remark { get; set; }

    /// <summary>
    /// 设备ID
    /// </summary>
    [SugarColumn(ColumnName = "deviceId", ColumnDescription = "/// 设备ID ///")]
    public string DeviceId { get; set; }

    /// <summary>
    /// 设备类型ID
    /// </summary>
    [SugarColumn(ColumnName = "deviceTypeId", ColumnDescription = "/// 设备类型ID ///")]
    public string DeviceTypeId { get; set; }


}
