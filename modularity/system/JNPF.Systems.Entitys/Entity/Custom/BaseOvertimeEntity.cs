using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    public class BaseOvertimeEntity
    {
        /// <summary>
        /// 班次id
        /// </summary>
        [SugarColumn(ColumnName = "class_id", ColumnDescription = "班次id", IsNullable = true)]
        [Description("班次id")]
        public string ClassId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "remark", ColumnDescription = "备注", IsNullable = true)]
        [Description("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 类型(0提前 1延后)
        /// </summary>
        [SugarColumn(ColumnName = "over_type", ColumnDescription = "类型(0提前 1延后)", IsNullable = true)]
        [Description("类型(0提前 1延后)")]
        public string OverType { get; set; }

        /// <summary>
        /// 指向时间 (0开始时间 1结束时间)
        /// </summary>
        [SugarColumn(ColumnName = "point_time", ColumnDescription = "指向时间 (0开始时间 1结束时间)", IsNullable = true)]
        [Description("指向时间 (0开始时间 1结束时间)")]
        public string PointTime { get; set; }

        /// <summary>
        /// 产线id
        /// </summary>
        [SugarColumn(ColumnName = "production_typeid", ColumnDescription = "产线id", IsNullable = true)]
        [Description("产线id")]
        public string ProductionTypeId { get; set; }

        /// <summary>
        /// 用时
        /// </summary>
        [SugarColumn(ColumnName = "used_time", ColumnDescription = "用时", IsNullable = true)]
        [Description("用时")]
        public string UsedTime { get; set; }

        /// <summary>
        /// 是否跨日（0否1是）
        /// </summary>
        [SugarColumn(ColumnName = "is_twoday", ColumnDescription = "是否跨日（0否1是）", IsNullable = true)]
        [Description("是否跨日（0否1是）")]
        public int? IsTwoDay { get; set; }

    }
}
