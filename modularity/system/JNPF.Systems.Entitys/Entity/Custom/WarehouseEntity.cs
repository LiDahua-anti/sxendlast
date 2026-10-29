using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 仓库档案表（仓库管理，用友U8同步）.
    /// 仓库基础数据从用友U8同步（编码/名称/计价方式/是否货位管理/仓库属性/条形码）；
    /// 不良品仓（91/92）、备货仓（93）等专用仓在此维护.
    /// </summary>
    [SugarTable("sx_warehouse")]
    public class WarehouseEntity : CLDSEntityBase
    {
        /// <summary>
        /// 仓库编码（如01/02/03/09/91/92/93）.
        /// </summary>
        [SugarColumn(ColumnName = "repo_code", IsNullable = false, ColumnDescription = "/// 仓库编码（如01/02/03/09/91/92/93）. ///")]
        [Description("仓库编码")]
        public string RepoCode { get; set; }

        /// <summary>
        /// 仓库名称（如成品仓库）.
        /// </summary>
        [SugarColumn(ColumnName = "repo_name", IsNullable = false, ColumnDescription = "/// 仓库名称（如成品仓库）. ///")]
        [Description("仓库名称")]
        public string RepoName { get; set; }

        /// <summary>
        /// 部门名称.
        /// </summary>
        [SugarColumn(ColumnName = "dept_name", IsNullable = true, ColumnDescription = "/// 部门名称. ///")]
        [Description("部门名称")]
        public string DeptName { get; set; }

        /// <summary>
        /// 仓库地址.
        /// </summary>
        [SugarColumn(ColumnName = "address", IsNullable = true, ColumnDescription = "/// 仓库地址. ///")]
        [Description("仓库地址")]
        public string Address { get; set; }

        /// <summary>
        /// 负责人.
        /// </summary>
        [SugarColumn(ColumnName = "charge_person", IsNullable = true, ColumnDescription = "/// 负责人. ///")]
        [Description("负责人")]
        public string ChargePerson { get; set; }

        /// <summary>
        /// 计价方式（如：全月平均法）.
        /// </summary>
        [SugarColumn(ColumnName = "pricing_method", IsNullable = true, ColumnDescription = "/// 计价方式（如：全月平均法）. ///")]
        [Description("计价方式")]
        public string PricingMethod { get; set; }

        /// <summary>
        /// 是否货位管理：0否 1是.
        /// </summary>
        [SugarColumn(ColumnName = "is_location_manage", IsNullable = true, ColumnDescription = "/// 是否货位管理：0否 1是. ///")]
        [Description("是否货位管理:0否 1是")]
        public int? IsLocationManage { get; set; }

        /// <summary>
        /// 仓库属性：普通仓/现场仓.
        /// </summary>
        [SugarColumn(ColumnName = "repo_attr", IsNullable = true, ColumnDescription = "/// 仓库属性：普通仓/现场仓. ///")]
        [Description("仓库属性")]
        public string RepoAttr { get; set; }

        /// <summary>
        /// 对应条形码.
        /// </summary>
        [SugarColumn(ColumnName = "barcode", IsNullable = true, ColumnDescription = "/// 对应条形码. ///")]
        [Description("对应条形码")]
        public string Barcode { get; set; }

        /// <summary>
        /// 货位数量.
        /// </summary>
        [SugarColumn(ColumnName = "location_count", IsNullable = true, ColumnDescription = "/// 货位数量. ///")]
        [Description("货位数量")]
        public int? LocationCount { get; set; }

        /// <summary>
        /// 来源：1用友U8同步 2手工新增.
        /// </summary>
        [SugarColumn(ColumnName = "source", IsNullable = true, ColumnDescription = "/// 来源：1用友U8同步 2手工新增. ///")]
        [Description("来源:1用友U8同步 2手工新增")]
        public int? Source { get; set; }

        /// <summary>
        /// 最近同步时间.
        /// </summary>
        [SugarColumn(ColumnName = "sync_time", IsNullable = true, ColumnDescription = "/// 最近同步时间. ///")]
        [Description("最近同步时间")]
        public DateTime? SyncTime { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
