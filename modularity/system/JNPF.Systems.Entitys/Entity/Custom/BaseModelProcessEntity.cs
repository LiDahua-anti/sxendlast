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
    /// 机型工艺表
    /// </summary>
    [SugarTable("sx_base_model_process")]
    public class BaseModelProcessEntity : CLDSEntityBase
    {
        /// <summary>
        /// 机型modelid
        /// </summary>
        [SugarColumn(ColumnName = "model_id", IsNullable = true, ColumnDescription = "/// 机型modelid ///")]
        [Description("机型modelid")]
        public string ModelId { get; set; }

        /// <summary>
        /// 设备code
        /// </summary>
        [SugarColumn(ColumnName = "device_code", IsNullable = true, ColumnDescription = "/// 设备code ///")]
        [Description("设备code")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 工艺id
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 工艺id ///")]
        [Description("工艺id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 标准工时
        /// </summary>
        [SugarColumn(ColumnName = "standard_hours", IsNullable = true, ColumnDescription = "/// 标准工时 ///")]
        [Description("标准工时")]
        public string? StandardHours { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注 ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
