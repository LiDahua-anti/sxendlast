using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 汇率表（基础数据）.
    /// </summary>
    [SugarTable("sx_exchange_rate")]
    public class ExchangeRateEntity : CLDSEntityBase
    {
        /// <summary>
        /// 币种（人民币/美元/欧元/日元/韩元）.
        /// </summary>
        [SugarColumn(ColumnName = "currency", IsNullable = false, ColumnDescription = "/// 币种（人民币/美元/欧元/日元/韩元）. ///")]
        [Description("币种")]
        public string Currency { get; set; }

        /// <summary>
        /// 对人民币汇率（4位小数）.
        /// </summary>
        [SugarColumn(ColumnName = "rate_to_cny", IsNullable = true, ColumnDescription = "/// 对人民币汇率（4位小数）. ///")]
        [Description("对人民币汇率")]
        public string RateToCny { get; set; }

        /// <summary>
        /// 汇率日期.
        /// </summary>
        [SugarColumn(ColumnName = "rate_date", IsNullable = true, ColumnDescription = "/// 汇率日期. ///")]
        [Description("汇率日期")]
        public DateTime? RateDate { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
