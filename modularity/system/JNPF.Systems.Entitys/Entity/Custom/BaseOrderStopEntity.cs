using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 订单暂停记录表
    /// </summary>
    [SugarTable("sx_base_order_stop")]
    [Description("订单暂停记录")]
    public class BaseOrderStopEntity : CLDSEntityBase
    {
        /// <summary>
        /// order表id
        /// </summary>
        [SugarColumn(ColumnName = "order_id", ColumnDescription = "order表id", IsNullable = true)]
        [Description("order表id")]
        public string OrderId { get; set; }

        /// <summary>
        /// 暂停开始时间
        /// </summary>
        [SugarColumn(ColumnName = "stop_start_time", ColumnDescription = "暂停开始时间", IsNullable = true)]
        [Description("暂停开始时间")]
        public DateTime? StopStartTime { get; set; }

        /// <summary>
        /// 暂停结束时间
        /// </summary>
        [SugarColumn(ColumnName = "stop_end_time", ColumnDescription = "暂停结束时间", IsNullable = true)]
        [Description("暂停结束时间")]
        public DateTime? StopEndTime { get; set; }

        /// <summary>
        /// 产线id
        /// </summary>
        [SugarColumn(ColumnName = "production_type", ColumnDescription = "产线id", IsNullable = true)]
        [Description("产线id")]
        public string ProductionType { get; set; }
    }
}
