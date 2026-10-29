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
    /// 班次表
    /// </summary>
    [SugarTable("sx_base_class")]
    public class BaseClassEntity : CLDSEntityBase
    {
        /// <summary>
        /// 班次名称
        /// </summary>
        [SugarColumn(ColumnName = "class_name", IsNullable = true, ColumnDescription = "/// 班次名称 ///")]
        [Description("班次名称")]
        public string ClassName { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注 ///")]
        [Description("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 班次类型.
        /// </summary>
        [SugarColumn(ColumnName = "class_type", ColumnDescription = "/// 班次类型. ///")]
        public string Class_type { get; set; }

        /// <summary>
        /// 时间.
        /// </summary>
        [SugarColumn(ColumnName = "class_time", ColumnDescription = "/// 时间. ///")]
        public string Class_time { get; set; }

        /// <summary>
        /// 结束时间.
        /// </summary>
        [SugarColumn(ColumnName = "end_time", ColumnDescription = "/// 结束时间. ///")]
        public string End_time { get; set; }

        /// <summary>
        /// 是否跨日：0否 1是（夜班跨越零点的班次归属次日）.
        /// </summary>
        [SugarColumn(ColumnName = "cross_day", IsNullable = true, ColumnDescription = "/// 是否跨日：0否 1是（夜班跨越零点的班次归属次日）. ///")]
        [Description("是否跨日:0否 1是")]
        public int? CrossDay { get; set; }

    }

}
