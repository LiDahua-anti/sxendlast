using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 终端设备表（PDA手持终端，基础数据）.
    /// </summary>
    [SugarTable("sx_terminal_device")]
    public class TerminalDeviceEntity : CLDSEntityBase
    {
        /// <summary>
        /// 终端设备编码（业务唯一）.
        /// </summary>
        [SugarColumn(ColumnName = "terminal_code", IsNullable = false, ColumnDescription = "/// 终端设备编码（业务唯一）. ///")]
        [Description("终端设备编码")]
        public string TerminalCode { get; set; }

        /// <summary>
        /// 工位（如01/02）.
        /// </summary>
        [SugarColumn(ColumnName = "station", IsNullable = true, ColumnDescription = "/// 工位（如01/02）. ///")]
        [Description("工位")]
        public string Station { get; set; }

        /// <summary>
        /// 终端设备名称（如手持PDA）.
        /// </summary>
        [SugarColumn(ColumnName = "terminal_name", IsNullable = true, ColumnDescription = "/// 终端设备名称（如手持PDA）. ///")]
        [Description("终端设备名称")]
        public string TerminalName { get; set; }

        /// <summary>
        /// 终端设备IP.
        /// </summary>
        [SugarColumn(ColumnName = "terminal_ip", IsNullable = true, ColumnDescription = "/// 终端设备IP. ///")]
        [Description("终端设备IP")]
        public string TerminalIp { get; set; }

        /// <summary>
        /// 负责人.
        /// </summary>
        [SugarColumn(ColumnName = "charge_person", IsNullable = true, ColumnDescription = "/// 负责人. ///")]
        [Description("负责人")]
        public string ChargePerson { get; set; }

        /// <summary>
        /// 状态：1启用 2锁定 3禁用.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1启用 2锁定 3禁用. ///")]
        [Description("状态:1启用 2锁定 3禁用")]
        public int? Status { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
