using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 工艺路线主表（技术管理）.
    /// 工艺路线 = 存货（母件）× 工序 的绑定关系；排产数据来源，
    /// 由工艺线路→工序明细→工序可用设备级联带出.
    /// </summary>
    [SugarTable("sx_routing")]
    public class RoutingEntity : CLDSEntityBase
    {
        /// <summary>
        /// 路线编号（如GY-EH01-09B-V1）.
        /// </summary>
        [SugarColumn(ColumnName = "routing_code", IsNullable = false, ColumnDescription = "/// 路线编号（如GY-EH01-09B-V1）. ///")]
        [Description("路线编号")]
        public string RoutingCode { get; set; }

        /// <summary>
        /// 路线名称.
        /// </summary>
        [SugarColumn(ColumnName = "routing_name", IsNullable = false, ColumnDescription = "/// 路线名称. ///")]
        [Description("路线名称")]
        public string RoutingName { get; set; }

        /// <summary>
        /// 适用存货（母件）ID（sx_inventory_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_id", IsNullable = true, ColumnDescription = "/// 适用存货（母件）ID（sx_inventory_master.F_ID）. ///")]
        [Description("适用存货(母件)ID")]
        public string InventoryId { get; set; }

        /// <summary>
        /// 母件编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_code", IsNullable = true, ColumnDescription = "/// 母件编码（冗余）. ///")]
        [Description("母件编码")]
        public string InventoryCode { get; set; }

        /// <summary>
        /// 母件名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_name", IsNullable = true, ColumnDescription = "/// 母件名称（冗余）. ///")]
        [Description("母件名称")]
        public string InventoryName { get; set; }

        /// <summary>
        /// 工序数.
        /// </summary>
        [SugarColumn(ColumnName = "process_count", IsNullable = true, ColumnDescription = "/// 工序数. ///")]
        [Description("工序数")]
        public int? ProcessCount { get; set; }

        /// <summary>
        /// 总标准工时（min）.
        /// </summary>
        [SugarColumn(ColumnName = "total_hours", IsNullable = true, ColumnDescription = "/// 总标准工时（min）. ///")]
        [Description("总标准工时(min)")]
        public decimal? TotalHours { get; set; }

        /// <summary>
        /// 版本.
        /// </summary>
        [SugarColumn(ColumnName = "version", IsNullable = true, ColumnDescription = "/// 版本. ///")]
        [Description("版本")]
        public string Version { get; set; }

        /// <summary>
        /// 状态：1生效 2停用 3草稿.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1生效 2停用 3草稿. ///")]
        [Description("状态:1生效 2停用 3草稿")]
        public int? Status { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
