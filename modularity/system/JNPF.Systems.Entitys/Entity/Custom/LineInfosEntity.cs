using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 产线信息实体
    /// </summary>
    [SugarTable("sx_line_infos")]
    public class LineInfosEntity
    {
        /// <summary>
        /// 获取或设置 编号.
        /// </summary>
        [SugarColumn(ColumnName = "f_id", ColumnDescription = "主键", IsPrimaryKey = true)]
        public string Id { get; set; }

        /// <summary>
        /// 产线名称ID（关联设备类型表）
        /// </summary>
        [SugarColumn(ColumnName = "devicetype_id", Length = 50, IsNullable = true, ColumnDescription = "/// 产线名称ID（关联设备类型表） ///")]
        [Description("产线名称ID")]
        public string DevicetypeId { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        [SugarColumn(ColumnName = "device_no", Length = 50, IsNullable = true, ColumnDescription = "/// 设备编号 ///")]
        [Description("设备编号")]
        public string DeviceNo { get; set; }

        /// <summary>
        /// 品番/产品型号
        /// </summary>
        [SugarColumn(ColumnName = "model", Length = 50, IsNullable = true, ColumnDescription = "/// 品番/产品型号 ///")]
        [Description("品番")]
        public string Model { get; set; }

        /// <summary>
        /// CT（周期时间）
        /// </summary>
        [SugarColumn(ColumnName = "ct", Length = 255, IsNullable = true, ColumnDescription = "/// CT（周期时间） ///")]
        [Description("CT")]
        public string Ct { get; set; }

        /// <summary>
        /// 产量
        /// </summary>
        [SugarColumn(ColumnName = "count", IsNullable = true, ColumnDescription = "/// 产量 ///")]
        [Description("产量")]
        public int? Count { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        [SugarColumn(ColumnName = "startdate", IsNullable = true, ColumnDescription = "/// 开始日期 ///")]
        [Description("开始日期")]
        public DateTime? Startdate { get; set; }

    }
}