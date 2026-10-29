using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 工序设备绑定表（技术管理-工序管理）.
    /// 绑定该工序可用的生产设备，排产时按工序明细自动带出可选设备.
    /// </summary>
    [SugarTable("sx_process_device")]
    public class ProcessDeviceEntity : CLDSEntityBase
    {
        /// <summary>
        /// 工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = false, ColumnDescription = "/// 工序ID（sx_base_process.F_ID）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_code", IsNullable = true, ColumnDescription = "/// 工序编码（冗余）. ///")]
        [Description("工序编码")]
        public string ProcessCode { get; set; }

        /// <summary>
        /// 工序名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序名称（冗余）. ///")]
        [Description("工序名称")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 设备ID（sx_device_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "device_id", IsNullable = false, ColumnDescription = "/// 设备ID（sx_device_master.F_ID）. ///")]
        [Description("设备ID")]
        public string DeviceId { get; set; }

        /// <summary>
        /// 设备编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "device_code", IsNullable = true, ColumnDescription = "/// 设备编码（冗余）. ///")]
        [Description("设备编码")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 设备名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "device_name", IsNullable = true, ColumnDescription = "/// 设备名称（冗余）. ///")]
        [Description("设备名称")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
