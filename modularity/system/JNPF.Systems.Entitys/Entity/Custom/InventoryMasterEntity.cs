using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 存货档案主表（技术管理-存货管理，基础数据从用友U8同步）.
    /// </summary>
    [SugarTable("sx_inventory_master")]
    public class InventoryMasterEntity : CLDSEntityBase
    {
        /// <summary>
        /// 存货编码（用友U8同步，业务唯一）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_code", IsNullable = false, ColumnDescription = "/// 存货编码（用友U8同步，业务唯一）. ///")]
        [Description("存货编码")]
        public string InventoryCode { get; set; }

        /// <summary>
        /// 存货名称.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_name", IsNullable = true, ColumnDescription = "/// 存货名称. ///")]
        [Description("存货名称")]
        public string InventoryName { get; set; }

        /// <summary>
        /// 规格型号.
        /// </summary>
        [SugarColumn(ColumnName = "spec", IsNullable = true, ColumnDescription = "/// 规格型号. ///")]
        [Description("规格型号")]
        public string Spec { get; set; }

        /// <summary>
        /// 存货大类（产成品/主生产材料等）.
        /// </summary>
        [SugarColumn(ColumnName = "category", IsNullable = true, ColumnDescription = "/// 存货大类（产成品/主生产材料等）. ///")]
        [Description("存货大类")]
        public string Category { get; set; }

        /// <summary>
        /// 主计量单位（PCS/KG等）.
        /// </summary>
        [SugarColumn(ColumnName = "unit", IsNullable = true, ColumnDescription = "/// 主计量单位（PCS/KG等）. ///")]
        [Description("主计量单位")]
        public string Unit { get; set; }

        /// <summary>
        /// 生产方式：1自制 2委外 3采购.
        /// </summary>
        [SugarColumn(ColumnName = "make_type", IsNullable = true, ColumnDescription = "/// 生产方式：1自制 2委外 3采购. ///")]
        [Description("生产方式:1自制 2委外 3采购")]
        public int? MakeType { get; set; }

        /// <summary>
        /// 计价方式（如：全月平均法）.
        /// </summary>
        [SugarColumn(ColumnName = "pricing_method", IsNullable = true, ColumnDescription = "/// 计价方式（如：全月平均法）. ///")]
        [Description("计价方式")]
        public string PricingMethod { get; set; }

        /// <summary>
        /// 是否批次管理：0否 1是.
        /// </summary>
        [SugarColumn(ColumnName = "batch_manage", IsNullable = true, ColumnDescription = "/// 是否批次管理：0否 1是. ///")]
        [Description("是否批次管理:0否 1是")]
        public int? BatchManage { get; set; }

        /// <summary>
        /// 数据来源：1用友U8同步 2手工新增.
        /// </summary>
        [SugarColumn(ColumnName = "source", IsNullable = true, ColumnDescription = "/// 数据来源：1用友U8同步 2手工新增. ///")]
        [Description("数据来源:1用友U8同步 2手工新增")]
        public int? Source { get; set; }

        /// <summary>
        /// 最近一次从用友U8同步的时间.
        /// </summary>
        [SugarColumn(ColumnName = "sync_time", IsNullable = true, ColumnDescription = "/// 最近一次从用友U8同步的时间. ///")]
        [Description("同步时间")]
        public DateTime? SyncTime { get; set; }

        /// <summary>
        /// 用友U8存货内码（同步映射用）.
        /// </summary>
        [SugarColumn(ColumnName = "u8_inventory_id", IsNullable = true, ColumnDescription = "/// 用友U8存货内码（同步映射用）. ///")]
        [Description("用友U8存货内码")]
        public string U8InventoryId { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
