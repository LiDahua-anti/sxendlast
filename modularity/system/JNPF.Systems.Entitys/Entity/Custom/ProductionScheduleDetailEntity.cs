using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 排产明细表（工序 × 设备 × 生产个数，生产计划管理）.
    /// 每道工序可分配多台设备并填写生产个数，工序合计与订单数量对比。
    /// </summary>
    [SugarTable("sx_production_schedule_detail")]
    public class ProductionScheduleDetailEntity : CLDSEntityBase
    {
        /// <summary>
        /// 排产主表ID（sx_production_schedule.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "schedule_id", IsNullable = false, ColumnDescription = "/// 排产主表ID（sx_production_schedule.F_ID）. ///")]
        [Description("排产主表ID")]
        public string ScheduleId { get; set; }

        /// <summary>
        /// 订单ID（冗余，便于查询）.
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 订单ID（冗余，便于查询）. ///")]
        [Description("订单ID(冗余)")]
        public string OrderId { get; set; }

        /// <summary>
        /// 工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 工序ID（sx_base_process.F_ID）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序名称（冗余）. ///")]
        [Description("工序名称(冗余)")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 工序顺序号.
        /// </summary>
        [SugarColumn(ColumnName = "seq_no", IsNullable = true, ColumnDescription = "/// 工序顺序号. ///")]
        [Description("工序顺序号")]
        public int? SeqNo { get; set; }

        /// <summary>
        /// 设备ID（sx_device_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "device_id", IsNullable = true, ColumnDescription = "/// 设备ID（sx_device_master.F_ID）. ///")]
        [Description("设备ID")]
        public string DeviceId { get; set; }

        /// <summary>
        /// 设备编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "device_code", IsNullable = true, ColumnDescription = "/// 设备编码（冗余）. ///")]
        [Description("设备编码(冗余)")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 设备名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "device_name", IsNullable = true, ColumnDescription = "/// 设备名称（冗余）. ///")]
        [Description("设备名称(冗余)")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 生产个数（该设备分配数量）.
        /// </summary>
        [SugarColumn(ColumnName = "produce_qty", IsNullable = true, ColumnDescription = "/// 生产个数（该设备分配数量）. ///")]
        [Description("生产个数")]
        public decimal? ProduceQty { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
