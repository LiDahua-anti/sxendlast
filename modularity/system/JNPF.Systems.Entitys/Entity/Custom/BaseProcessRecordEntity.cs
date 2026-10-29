using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 报工记录表
    /// </summary>
    [SugarTable("sx_base_process_record")]
    public class BaseProcessRecordEntity : CLDSEntityBase
    {
        /// <summary>
        /// 订单id
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 订单id ///")]
        [Description("订单id")]
        public string OrderId { get; set; }

        /// <summary>
        /// 工序id
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 工序id ///")]
        [Description("工序id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [SugarColumn(ColumnName = "start_time", IsNullable = true, ColumnDescription = "/// 开始时间 ///")]
        [Description("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        [SugarColumn(ColumnName = "end_time", IsNullable = true, ColumnDescription = "/// 结束时间 ///")]
        [Description("结束时间")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 加工周期
        /// </summary>
        [SugarColumn(ColumnName = "process_cycle", IsNullable = true, ColumnDescription = "/// 加工周期 ///")]
        [Description("加工周期")]
        public string ProcessCycle { get; set; }

        /// <summary>
        /// 报工内容
        /// </summary>
        [SugarColumn(ColumnName = "report_content", IsNullable = true, ColumnDescription = "/// 报工内容 ///")]
        [Description("报工内容")]
        public string ReportContent { get; set; }

        /// <summary>
        /// 报工数量
        /// </summary>
        [SugarColumn(ColumnName = "report_quantity", IsNullable = true, ColumnDescription = "/// 报工数量 ///")]
        [Description("报工数量")]
        public string ReportQuantity { get; set; }
    }
}