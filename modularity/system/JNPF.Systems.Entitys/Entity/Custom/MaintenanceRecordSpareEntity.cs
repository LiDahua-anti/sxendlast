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
    [SugarTable("sx_maintenance_record_spare")]
    public class MaintenanceRecordSpareEntity : CLDSEntityBase
    {
        /// <summary>
        /// 维护记录id
        /// </summary>
        [SugarColumn(ColumnName = "maintenance_record_id", IsNullable = false, ColumnDescription = "/// 维护记录id ///")]
        [Description("维护记录id")]
        public string MaintenanceRecordId { get; set; }

        /// <summary>
        /// Bomid
        /// </summary>
        [SugarColumn(ColumnName = "Bom_id", IsNullable = false, ColumnDescription = "/// Bomid ///")]
        [Description("备件bom_id")]
        public string BomId { get; set; }

        /// <summary>
        /// 使用数量
        /// </summary>
        [SugarColumn(ColumnName = "quantity", IsNullable = false, ColumnDescription = "/// 使用数量 ///")]
        [Description("使用数量")]
        public string Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [SugarColumn(ColumnName = "prices", IsNullable = false, ColumnDescription = "/// 单价 ///")]
        [Description("单价")]
        public string Prices { get; set; }

        /// <summary>
        /// 库位id
        /// </summary>
        [SugarColumn(ColumnName = "location_id", IsNullable = false, ColumnDescription = "/// 库位id ///")]
        [Description("库位")]
        public string LocationId { get; set; }


    }
}
