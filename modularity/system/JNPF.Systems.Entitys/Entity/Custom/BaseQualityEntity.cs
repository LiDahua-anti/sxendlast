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
    [SugarTable("sx_base_quality")]
    public class BaseQualityEntity : CLDSEntityBase
    {
        /// <summary>
        /// 工序id
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 工序id ///")]
        [Description("工序id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 订单id
        /// </summary>
        [SugarColumn(ColumnName = "order_id", IsNullable = true, ColumnDescription = "/// 订单id ///")]
        [Description("订单id")]
        public string OrderId { get; set; }

        /// 设备编号
        /// </summary>
        [SugarColumn(ColumnName = "device_code", IsNullable = true)]
        [Description("设备编号")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 类型（0随机检查 1报工检查 -1其他）
        /// </summary>
        [SugarColumn(ColumnName = "check_type", IsNullable = true, ColumnDescription = "/// 类型（0随机检查 1报工检查 -1其他） ///")]
        [Description("类型（0随机检查 1报工检查 -1其他）")]
        public int CheckType { get; set; }

        /// <summary>
        /// 质检结果（0合格 1不合格）
        /// </summary>
        [SugarColumn(ColumnName = "quality_result", IsNullable = true, ColumnDescription = "/// 质检结果（0合格 1不合格） ///")]
        [Description("质检结果（0合格 1不合格）")]
        public string QualityResult { get; set; }

        /// <summary>
        /// 质检说明
        /// </summary>
        [SugarColumn(ColumnName = "quality_description", IsNullable = true, ColumnDescription = "/// 质检说明 ///")]
        [Description("质检说明")]
        public string QualityDescription { get; set; }

        /// <summary>
        /// 质检数
        /// </summary>
        [SugarColumn(ColumnName = "check_quantity", IsNullable = true, ColumnDescription = "/// 质检数 ///")]
        [Description("质检数")]
        public string CheckQuantity { get; set; }

        /// <summary>
        /// 合格数
        /// </summary>
        [SugarColumn(ColumnName = "qualified_quantity", IsNullable = true, ColumnDescription = "/// 合格数 ///")]
        [Description("合格数")]
        public string QualifiedQuantity { get; set; }

        /// <summary>
        /// 不合格数
        /// </summary>
        [SugarColumn(ColumnName = "unqualified_quantity", IsNullable = true, ColumnDescription = "/// 不合格数 ///")]
        [Description("不合格数")]
        public string UnqualifiedQuantity { get; set; }

    }
}
