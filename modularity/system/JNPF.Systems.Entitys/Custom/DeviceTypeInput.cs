using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using SqlSugar;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 设备

/// <summary>
/// 设备列表
/// </summary>
public class DeviceInput : PageInputBase
{
    /// <summary>
    /// 设备类型ID
    /// </summary>
    public string DeviceTypeId { get; set; }

    public string DeviceNo { get; set; }

    /// <summary>
    /// 供应厂家
    /// </summary>
    public string DeviceSupplier { get; set; }

    /// <summary>
    /// 设备厂家
    /// </summary>
    public string DeviceManufacturer { get; set; }

    /// <summary>
    /// 固定资产编号
    /// </summary>
    public string AssetsNumber { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName { get; set; }


    /// <summary>
    /// 购买开始日期
    /// </summary>
    public string BuyStartDate { get; set; }

    /// <summary>
    /// 购买结束日期
    /// </summary>
    public string BuyEndDate { get; set; }

    /// <summary>
    /// 使用场所
    /// </summary>
    public string UseAddress { get; set; }

    /// <summary>
    /// 使用部门
    /// </summary>
    public string UseDpt { get; set; }

    /// <summary>
    /// 设备状态
    /// </summary>
    public string? DeviceStatus { get; set; }

    /// <summary>
    /// 项目id
    /// </summary>
    public string Id { get; set; }

}

/// <summary>
/// 设备新增或编辑
/// </summary>
public class DeviceCruInput
{
    /// <summary>
    /// 主键
    /// </summary>
    public string Id { get; set; }

    public string DeviceNo { get; set; }

    /// <summary>
    /// 固定资产编号
    /// </summary>
    public string AssetsNumber { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    /// 设备厂家
    /// </summary>
    public string DeviceManufacturer { get; set; }

    /// <summary>
    /// 设备型号
    /// </summary>
    public string DeviceModel { get; set; }

    /// <summary>
    /// 供应厂家
    /// </summary>
    public string DeviceSupplier { get; set; }

    /// <summary>
    /// 购买日期
    /// </summary>
    public string BuyDate { get; set; }

    /// <summary>
    /// 设备类型ID
    /// </summary>
    public string? DeviceTypeId { get; set; }

    /// <summary>
    /// 使用场所
    /// </summary>
    public string UseAddress { get; set; }

    /// <summary>
    /// 使用部门
    /// </summary>
    public string UseDpt { get; set; }

    /// <summary>
    /// 设备重要性
    /// </summary>
    public string? DeviceImportant { get; set; }

    /// <summary>
    /// 设备状态
    /// </summary>
    public string? DeviceStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? FNote { get; set; }

    /// <summary>
    /// 二维码
    /// </summary>
    public string? TwoDimensionalCode { get; set; }

    /// <summary>
    /// 制作品外观
    /// </summary>
    public string? DeviceImage { get; set; }

    /// <summary>
    /// 产线名称
    /// </summary>
    public string? ProductionLineName { get; set; }

    /// <summary>
    /// 产线编号
    /// </summary>
    public string? ProductionLineNo { get; set; }

    /// <summary>
    /// 控制盘编号
    /// </summary>
    public string? ControlPanelNo { get; set; }

    /// <summary>
    /// PLC型号
    /// </summary>
    public string? PLCModel { get; set; }

    /// <summary>
    /// 设备IP地址
    /// </summary>
    public string? DeviceIP { get; set; }

    /// <summary>
    /// 端口号
    /// </summary>
    public int? PortNumber { get; set; }

    /// <summary>
    /// 通信协议
    /// </summary>
    public string? CommunicationProtocol { get; set; }

    /// <summary>
    /// 网关IP地址
    /// </summary>
    public string? GatewayIP { get; set; }

}

/// <summary>
/// 列表反填
/// </summary>
public class DeviceCruListDto
{
    /// <summary>
    /// 主键
    /// </summary>
    public string Id { get; set; }

    public string DeviceNo { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public int PointDevice { get; set; }

    /// <summary>
    /// 固定资产编号
    /// </summary>
    public string AssetsNumber { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    /// 设备厂家
    /// </summary>
    public string DeviceManufacturer { get; set; }

    /// <summary>
    /// 设备型号
    /// </summary>
    public string DeviceModel { get; set; }

    /// <summary>
    /// 供应厂家
    /// </summary>
    public string DeviceSupplier { get; set; }

    /// <summary>
    /// 购买日期
    /// </summary>
    public string BuyDate { get; set; }

    /// <summary>
    /// 设备类型ID
    /// </summary>
    public string? DeviceTypeId { get; set; }

    /// <summary>
    /// 使用场所
    /// </summary>
    public string UseAddress { get; set; }

    public string UseAddressId { get; set; }


    /// <summary>
    /// 使用部门
    /// </summary>
    public string UseDpt { get; set; }

    /// <summary>
    /// 使用部门
    /// </summary>
    public string UseDptId { get; set; }

    /// <summary>
    /// 设备重要性
    /// </summary>
    public string? DeviceImportant { get; set; }

    /// <summary>
    /// 设备状态
    /// </summary>
    public string? DeviceStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? FNote { get; set; }

    /// <summary>
    /// 二维码
    /// </summary>
    public string? TwoDimensionalCode { get; set; }

    /// <summary>
    /// 制作品外观
    /// </summary>
    public string? DeviceImage { get; set; }

}
/// <summary>
/// 分配设备列表
/// </summary>
public class DeviceCruInput2
{
    /// <summary>
    /// 主键
    /// </summary>
    public string Id { get; set; }

    public string DeviceNo { get; set; }

    /// <summary>
    /// 固定资产编号
    /// </summary>
    public string AssetsNumber { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    /// 设备厂家
    /// </summary>
    public string DeviceManufacturer { get; set; }

    /// <summary>
    /// 设备型号
    /// </summary>
    public string DeviceModel { get; set; }

    /// <summary>
    /// 供应厂家
    /// </summary>
    public string DeviceSupplier { get; set; }

    /// <summary>
    /// 购买日期
    /// </summary>
    public string BuyDate { get; set; }

    /// <summary>
    /// 设备类型ID
    /// </summary>
    public string? DeviceTypeId { get; set; }

    /// <summary>
    /// 使用场所
    /// </summary>
    public string UseAddress { get; set; }

    /// <summary>
    /// 使用部门
    /// </summary>
    public string UseDpt { get; set; }

    /// <summary>
    /// 设备重要性
    /// </summary>
    public string? DeviceImportant { get; set; }

    /// <summary>
    /// 设备状态
    /// </summary>
    public string? DeviceStatus { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? FNote { get; set; }

    /// <summary>
    /// 二维码
    /// </summary>
    public string? TwoDimensionalCode { get; set; }

    /// <summary>
    /// 制作品外观
    /// </summary>
    public string? DeviceImage { get; set; }

    /// <summary>
    /// 是否勾选(0:不勾选，1：勾选)
    /// </summary>
    public int IsCheck { get; set; }

}

/// <summary>
/// 设备下拉框
/// </summary>
public class DeviceDictInput
{
    /// <summary>
    /// 设备类型状态
    /// </summary>
    public string propName { get; set; }

}

#endregion


#region 设备类型

/// <summary>
/// 设备类型查询输入参数
/// </summary>
public class DeviceTypeQuery : PageInputBase
{
    /// <summary>
    /// 设备类型状态
    /// </summary>
    public int? status { get; set; }
}

/// <summary>
/// 单据规则修改输入.
/// </summary>
[SuppressSniffer]
public class DeviceParentTypeInput
{
    /// <summary>
    /// 设备类型名称
    /// </summary>
    public string DevicetypeName { get; set; }

    /// <summary>
    ///状态(0:未启用，1：启用)
    /// </summary>
    public int F_ENABLED_MARK { get; set; }

    /// <summary>
    ///备注
    /// </summary>
    public string FNote { get; set; }
}

/// <summary>
/// 单据规则修改输入.
/// </summary>
[SuppressSniffer]
public class DeviceSonTypeInput
{
    /// <summary>
    /// 当前id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 新增接口调用0   编辑接口调用1
    /// </summary>
    public int CruType { get; set; }

    /// <summary>
    /// 父级id  （最上级应该是0）
    /// </summary>
    public string ParentId { get; set; }

    /// <summary>
    /// 设备类型名称
    /// </summary>
    public string DevicetypeName { get; set; }

    /// <summary>
    ///状态(0:未启用，1：启用)
    /// </summary>
    public int F_ENABLED_MARK { get; set; }

    /// <summary>
    ///备注
    /// </summary>
    public string FNote { get; set; }
}


/// <summary>
/// 设备类型树形输出DTO
/// </summary>
public class DeviceTypeTreeOutput
{
    public string Id { get; set; }
    public string ParentId { get; set; }
    public string DevicetypeCode { get; set; }
    public string DevicetypeName { get; set; }
    public int F_ENABLED_MARK { get; set; }
    public string FNote { get; set; }

    public List<DeviceTypeTreeOutput> children { get; set; }
}

#endregion