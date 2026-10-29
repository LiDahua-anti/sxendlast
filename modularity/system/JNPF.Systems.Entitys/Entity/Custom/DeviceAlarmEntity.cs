using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 设备报警履历表（设备管理）.
    /// 记录设备报警编号/类型/内容/触发时间/恢复时间/处理人；
    /// 按设备/时间/处理状态筛选查询。
    /// </summary>
    [SugarTable("sx_device_alarm")]
    public class DeviceAlarmEntity : CLDSEntityBase
    {
        /// <summary>
        /// 报警编号（如ALM20261010-001）.
        /// </summary>
        [SugarColumn(ColumnName = "alarm_no", IsNullable = false, ColumnDescription = "/// 报警编号（如ALM20261010-001）. ///")]
        [Description("报警编号")]
        public string AlarmNo { get; set; }

        /// <summary>
        /// 设备ID（sx_device_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "device_id", IsNullable = true, ColumnDescription = "/// 设备ID（sx_device_master.F_ID）. ///")]
        [Description("设备ID")]
        public string DeviceId { get; set; }

        /// <summary>
        /// 设备编号（如CNC-01）.
        /// </summary>
        [SugarColumn(ColumnName = "device_code", IsNullable = true, ColumnDescription = "/// 设备编号（如CNC-01）. ///")]
        [Description("设备编号")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 设备名称（如数控车床1号）.
        /// </summary>
        [SugarColumn(ColumnName = "device_name", IsNullable = true, ColumnDescription = "/// 设备名称（如数控车床1号）. ///")]
        [Description("设备名称")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 报警类型：温度异常/压力异常/通讯中断/急停触发.
        /// </summary>
        [SugarColumn(ColumnName = "alarm_type", IsNullable = true, ColumnDescription = "/// 报警类型：温度异常/压力异常/通讯中断/急停触发. ///")]
        [Description("报警类型")]
        public string AlarmType { get; set; }

        /// <summary>
        /// 报警内容（如：主轴温度超过85℃）.
        /// </summary>
        [SugarColumn(ColumnName = "alarm_content", IsNullable = true, ColumnDescription = "/// 报警内容（如：主轴温度超过85℃）. ///")]
        [Description("报警内容")]
        public string AlarmContent { get; set; }

        /// <summary>
        /// 触发时间.
        /// </summary>
        [SugarColumn(ColumnName = "trigger_time", IsNullable = true, ColumnDescription = "/// 触发时间. ///")]
        [Description("触发时间")]
        public DateTime? TriggerTime { get; set; }

        /// <summary>
        /// 恢复时间.
        /// </summary>
        [SugarColumn(ColumnName = "recover_time", IsNullable = true, ColumnDescription = "/// 恢复时间. ///")]
        [Description("恢复时间")]
        public DateTime? RecoverTime { get; set; }

        /// <summary>
        /// 处理人（如：维修-陈工）.
        /// </summary>
        [SugarColumn(ColumnName = "handler_name", IsNullable = true, ColumnDescription = "/// 处理人（如：维修-陈工）. ///")]
        [Description("处理人")]
        public string HandlerName { get; set; }

        /// <summary>
        /// 处理状态：0未恢复 1已恢复 2已处理.
        /// </summary>
        [SugarColumn(ColumnName = "handle_status", IsNullable = true, ColumnDescription = "/// 处理状态：0未恢复 1已恢复 2已处理. ///")]
        [Description("处理状态:0未恢复 1已恢复 2已处理")]
        public int? HandleStatus { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
