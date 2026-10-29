using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 机型表
    /// </summary>
    [SugarTable("sx_base_model")]
    public class BaseModelEntity : CLDSEntityBase
    {
        /// <summary>
        /// 机型编号
        /// </summary>
        [SugarColumn(ColumnName = "device_code", IsNullable = true, ColumnDescription = "/// 机型编号 ///")]
        [Description("机型编号")]
        public string DeviceCode { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [SugarColumn(ColumnName = "device_name", IsNullable = true, ColumnDescription = "/// 名称 ///")]
        [Description("名称")]
        public string DeviceName { get; set; }

        /// <summary>
        /// 记性类型
        /// </summary>
        [SugarColumn(ColumnName = "device_type", IsNullable = true, ColumnDescription = "/// 记性类型 ///")]
        [Description("机型类型")]
        public string DeviceType { get; set; }

        /// <summary>
        /// 机型品牌
        /// </summary>
        [SugarColumn(ColumnName = "device_brand", IsNullable = true, ColumnDescription = "/// 机型品牌 ///")]
        [Description("机型品牌")]
        public string DeviceBrand { get; set; }

        /// <summary>
        /// 型号
        /// </summary>
        [SugarColumn(ColumnName = "device_model", IsNullable = true, ColumnDescription = "/// 型号 ///")]
        [Description("型号")]
        public string DeviceModel { get; set; }

        /// <summary>
        /// 规格
        /// </summary>
        [SugarColumn(ColumnName = "device_spec", IsNullable = true, ColumnDescription = "/// 规格 ///")]
        [Description("规格")]
        public string DeviceSpec { get; set; }

        /// <summary>
        /// 生产厂家
        /// </summary>
        [SugarColumn(ColumnName = "manufacturer", IsNullable = true, ColumnDescription = "/// 生产厂家 ///")]
        [Description("生产厂家")]
        public string Manufacturer { get; set; }

        /// <summary>
        /// 出厂编号
        /// </summary>
        [SugarColumn(ColumnName = "factory_number", IsNullable = true, ColumnDescription = "/// 出厂编号 ///")]
        [Description("出厂编号")]
        public string FactoryNumber { get; set; }

        /// <summary>
        /// 购买日期
        /// </summary>
        [SugarColumn(ColumnName = "purchase_date", IsNullable = true, ColumnDescription = "/// 购买日期 ///")]
        [Description("购买日期")]
        public DateTime? PurchaseDate { get; set; }

        /// <summary>
        /// 保修截止日期
        /// </summary>
        [SugarColumn(ColumnName = "warranty_end_date", IsNullable = true, ColumnDescription = "/// 保修截止日期 ///")]
        [Description("保修截止日期")]
        public DateTime? WarrantyEndDate { get; set; }

        /// <summary>
        /// 使用部门
        /// </summary>
        [SugarColumn(ColumnName = "use_department", IsNullable = true, ColumnDescription = "/// 使用部门 ///")]
        [Description("使用部门")]
        public string UseDepartment { get; set; }

        /// <summary>
        /// 使用状态
        /// </summary>
        [SugarColumn(ColumnName = "use_status", IsNullable = false, ColumnDescription = "/// 使用状态 ///")]
        [Description("使用状态")]
        public int UseStatus { get; set; }

        /// <summary>
        /// 存放位置
        /// </summary>
        [SugarColumn(ColumnName = "storage_location", IsNullable = true, ColumnDescription = "/// 存放位置 ///")]
        [Description("存放位置")]
        public string StorageLocation { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注 ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}