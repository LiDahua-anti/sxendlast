using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    [SugarTable("sx_device_master")]
    public class DeviceMasterEntity : CLDSEntityBase
    {
        /// <summary>
        /// 设备状态
        /// </summary>
        [SugarColumn(ColumnName = "device_state", ColumnDescription = "/// 设备状态 ///")]
        public string Device_State { get; set; }

        /// <summary>
        /// 执行设备
        /// </summary>
        [SugarColumn(ColumnName = "point_device", ColumnDescription = "/// 执行设备 ///")]
        public int PointDevice { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        [SugarColumn(ColumnName = "device_no", ColumnDescription = "/// 设备编号 ///")]
        public string DeviceNo { get; set; }

        /// <summary>
        /// 固定资产编号
        /// </summary>
        [SugarColumn(ColumnName = "assets_number", ColumnDescription = "/// 固定资产编号 ///")]
        public string AssetsNumber { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [SugarColumn(ColumnName = "device_name", ColumnDescription = "/// 设备名称 ///")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 设备厂家
        /// </summary>
        [SugarColumn(ColumnName = "device_manufacturer", ColumnDescription = "/// 设备厂家 ///")]
        public string DeviceManufacturer { get; set; }

        /// <summary>
        /// 设备型号
        /// </summary>
        [SugarColumn(ColumnName = "device_model", ColumnDescription = "/// 设备型号 ///")]
        public string DeviceModel { get; set; }

        /// <summary>
        /// 供应厂家
        /// </summary>
        [SugarColumn(ColumnName = "device_supplier", ColumnDescription = "/// 供应厂家 ///")]
        public string DeviceSupplier { get; set; }

        /// <summary>
        /// 购买日期
        /// </summary>
        [SugarColumn(ColumnName = "buy_date", ColumnDescription = "/// 购买日期 ///")]
        public string BuyDate { get; set; }

        /// <summary>
        /// 设备类型ID
        /// </summary>
        [SugarColumn(ColumnName = "devicetype_id", ColumnDescription = "/// 设备类型ID ///")]
        public string DeviceTypeId { get; set; }

        /// <summary>
        /// 使用场所
        /// </summary>
        [SugarColumn(ColumnName = "use_address", ColumnDescription = "/// 使用场所 ///")]
        public string UseAddress { get; set; }

        /// <summary>
        /// 使用部门
        /// </summary>
        [SugarColumn(ColumnName = "use_dpt", ColumnDescription = "/// 使用部门 ///")]
        public string UseDpt { get; set; }

        /// <summary>
        /// 设备重要性
        /// </summary>
        [SugarColumn(ColumnName = "device_important", ColumnDescription = "/// 设备重要性 ///")]
        public string? DeviceImportant { get; set; }

        /// <summary>
        /// 设备重要性
        /// </summary>
        [SugarColumn(ColumnName = "device_status", ColumnDescription = "/// 设备重要性 ///")]
        public string? DeviceStatus { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "f_note", ColumnDescription = "/// 备注 ///")]
        public string? FNote { get; set; }

        /// <summary>
        /// 二维码
        /// </summary>
        [SugarColumn(ColumnName = "two_dimensional_code", ColumnDescription = "/// 二维码 ///")]
        public string? TwoDimensionalCode { get; set; }

        /// <summary>
        /// 制作品外观
        /// </summary>
        [SugarColumn(ColumnName = "device_image", ColumnDescription = "/// 制作品外观 ///")]
        public string? DeviceImage { get; set; }

        /// <summary>
        /// 控制盘编号
        /// </summary>
        [SugarColumn(ColumnName = "control_panel_no", ColumnDescription = "/// 控制盘编号 ///")]
        public string? ControlPanelNo { get; set; }

        /// <summary>
        /// PLC型号
        /// </summary>
        [SugarColumn(ColumnName = "plc_model", ColumnDescription = "/// PLC型号 ///")]
        public string? PLCModel { get; set; }

        /// <summary>
        /// 设备IP地址
        /// </summary>
        [SugarColumn(ColumnName = "device_ip", ColumnDescription = "/// 设备IP地址 ///")]
        public string? DeviceIP { get; set; }

        /// <summary>
        /// 端口号
        /// </summary>
        [SugarColumn(ColumnName = "port_number", ColumnDescription = "/// 端口号 ///")]
        public int? PortNumber { get; set; }

        /// <summary>
        /// 通信协议
        /// </summary>
        [SugarColumn(ColumnName = "communication_protocol", ColumnDescription = "/// 通信协议 ///")]
        public string? CommunicationProtocol { get; set; }

        /// <summary>
        /// 网关IP地址
        /// </summary>
        [SugarColumn(ColumnName = "gateway_ip", ColumnDescription = "/// 网关IP地址 ///")]
        public string? GatewayIP { get; set; }

    }
}
