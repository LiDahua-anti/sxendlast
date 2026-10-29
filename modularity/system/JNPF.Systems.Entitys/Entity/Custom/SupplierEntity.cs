using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 供应商档案表（基础数据）.
    /// </summary>
    [SugarTable("sx_supplier")]
    public class SupplierEntity : CLDSEntityBase
    {
        /// <summary>
        /// 供应商编码（业务唯一）.
        /// </summary>
        [SugarColumn(ColumnName = "supplier_code", IsNullable = false, ColumnDescription = "/// 供应商编码（业务唯一）. ///")]
        [Description("供应商编码")]
        public string SupplierCode { get; set; }

        /// <summary>
        /// 供应商代码（用友U8同步的外码）.
        /// </summary>
        [SugarColumn(ColumnName = "supplier_no", IsNullable = true, ColumnDescription = "/// 供应商代码（用友U8同步的外码）. ///")]
        [Description("供应商代码")]
        public string SupplierNo { get; set; }

        /// <summary>
        /// 供应商名称.
        /// </summary>
        [SugarColumn(ColumnName = "supplier_name", IsNullable = false, ColumnDescription = "/// 供应商名称. ///")]
        [Description("供应商名称")]
        public string SupplierName { get; set; }

        /// <summary>
        /// 供应商简称.
        /// </summary>
        [SugarColumn(ColumnName = "short_name", IsNullable = true, ColumnDescription = "/// 供应商简称. ///")]
        [Description("供应商简称")]
        public string ShortName { get; set; }

        /// <summary>
        /// 联系人.
        /// </summary>
        [SugarColumn(ColumnName = "contact_person", IsNullable = true, ColumnDescription = "/// 联系人. ///")]
        [Description("联系人")]
        public string ContactPerson { get; set; }

        /// <summary>
        /// 联系电话.
        /// </summary>
        [SugarColumn(ColumnName = "contact_phone", IsNullable = true, ColumnDescription = "/// 联系电话. ///")]
        [Description("联系电话")]
        public string ContactPhone { get; set; }

        /// <summary>
        /// 地址.
        /// </summary>
        [SugarColumn(ColumnName = "address", IsNullable = true, ColumnDescription = "/// 地址. ///")]
        [Description("地址")]
        public string Address { get; set; }

        /// <summary>
        /// 税号.
        /// </summary>
        [SugarColumn(ColumnName = "tax_number", IsNullable = true, ColumnDescription = "/// 税号. ///")]
        [Description("税号")]
        public string TaxNumber { get; set; }

        /// <summary>
        /// 开户行.
        /// </summary>
        [SugarColumn(ColumnName = "bank_name", IsNullable = true, ColumnDescription = "/// 开户行. ///")]
        [Description("开户行")]
        public string BankName { get; set; }

        /// <summary>
        /// 银行账号.
        /// </summary>
        [SugarColumn(ColumnName = "bank_account", IsNullable = true, ColumnDescription = "/// 银行账号. ///")]
        [Description("银行账号")]
        public string BankAccount { get; set; }

        /// <summary>
        /// 状态：0禁用 1启用.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：0禁用 1启用. ///")]
        [Description("状态:0禁用 1启用")]
        public int? Status { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
