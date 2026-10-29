using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 存货BOM子件明细表（母件存货 × 子件存货 绑定关系）.
    /// </summary>
    [SugarTable("sx_inventory_bom")]
    public class InventoryBomEntity : CLDSEntityBase
    {
        /// <summary>
        /// 母件存货ID（sx_inventory_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "parent_inventory_id", IsNullable = false, ColumnDescription = "/// 母件存货ID（sx_inventory_master.F_ID）. ///")]
        [Description("母件存货ID")]
        public string ParentInventoryId { get; set; }

        /// <summary>
        /// 母件存货编码（冗余，便于查询）.
        /// </summary>
        [SugarColumn(ColumnName = "parent_inventory_code", IsNullable = true, ColumnDescription = "/// 母件存货编码（冗余，便于查询）. ///")]
        [Description("母件存货编码")]
        public string ParentInventoryCode { get; set; }

        /// <summary>
        /// 子件存货ID（sx_inventory_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "child_inventory_id", IsNullable = false, ColumnDescription = "/// 子件存货ID（sx_inventory_master.F_ID）. ///")]
        [Description("子件存货ID")]
        public string ChildInventoryId { get; set; }

        /// <summary>
        /// 子件存货编码（冗余，便于查询）.
        /// </summary>
        [SugarColumn(ColumnName = "child_inventory_code", IsNullable = true, ColumnDescription = "/// 子件存货编码（冗余，便于查询）. ///")]
        [Description("子件存货编码")]
        public string ChildInventoryCode { get; set; }

        /// <summary>
        /// 使用数量（单件母件消耗子件数）.
        /// </summary>
        [SugarColumn(ColumnName = "quantity", IsNullable = true, ColumnDescription = "/// 使用数量（单件母件消耗子件数）. ///")]
        [Description("使用数量")]
        public decimal? Quantity { get; set; }

        /// <summary>
        /// 损耗率（%）.
        /// </summary>
        [SugarColumn(ColumnName = "loss_rate", IsNullable = true, ColumnDescription = "/// 损耗率（%）. ///")]
        [Description("损耗率(%)")]
        public decimal? LossRate { get; set; }

        /// <summary>
        /// 位置号/行号.
        /// </summary>
        [SugarColumn(ColumnName = "position_no", IsNullable = true, ColumnDescription = "/// 位置号/行号. ///")]
        [Description("位置号")]
        public string PositionNo { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
