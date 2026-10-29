using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 模具档案表（基础数据）.
    /// </summary>
    [SugarTable("sx_mold")]
    public class MoldEntity : CLDSEntityBase
    {
        /// <summary>
        /// 模具编号（业务唯一）.
        /// </summary>
        [SugarColumn(ColumnName = "mold_code", IsNullable = false, ColumnDescription = "/// 模具编号（业务唯一）. ///")]
        [Description("模具编号")]
        public string MoldCode { get; set; }

        /// <summary>
        /// 模具名称.
        /// </summary>
        [SugarColumn(ColumnName = "mold_name", IsNullable = true, ColumnDescription = "/// 模具名称. ///")]
        [Description("模具名称")]
        public string MoldName { get; set; }

        /// <summary>
        /// 对应产品（图番/产品名称）.
        /// </summary>
        [SugarColumn(ColumnName = "product_name", IsNullable = true, ColumnDescription = "/// 对应产品（图番/产品名称）. ///")]
        [Description("对应产品")]
        public string ProductName { get; set; }

        /// <summary>
        /// 模具寿命（模）.
        /// </summary>
        [SugarColumn(ColumnName = "mold_life", IsNullable = true, ColumnDescription = "/// 模具寿命（模）. ///")]
        [Description("模具寿命(模)")]
        public int? MoldLife { get; set; }

        /// <summary>
        /// 已用次数（模）.
        /// </summary>
        [SugarColumn(ColumnName = "used_count", IsNullable = true, ColumnDescription = "/// 已用次数（模）. ///")]
        [Description("已用次数(模)")]
        public int? UsedCount { get; set; }

        /// <summary>
        /// 状态：1在用 2维修 3报废.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1在用 2维修 3报废. ///")]
        [Description("状态:1在用 2维修 3报废")]
        public int? Status { get; set; }

        /// <summary>
        /// 上次保养日期.
        /// </summary>
        [SugarColumn(ColumnName = "last_maintain_date", IsNullable = true, ColumnDescription = "/// 上次保养日期. ///")]
        [Description("上次保养日期")]
        public DateTime? LastMaintainDate { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
