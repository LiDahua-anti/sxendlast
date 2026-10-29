using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 来料检验（IQC）主表（品质管理）.
    /// 检验方式：扫码检查 / 免检（免检自动跳过检查项并标记免检）；
    /// 不合格记录处理状态（待处理/处理中/已处理）；
    /// 发布校验②依据：流程卡对应来料检验存在「合格/免检」记录。
    /// </summary>
    [SugarTable("sx_iqc")]
    public class IqcEntity : CLDSEntityBase
    {
        /// <summary>
        /// 检验单号（业务唯一，如IQC20261010001）.
        /// </summary>
        [SugarColumn(ColumnName = "check_no", IsNullable = false, ColumnDescription = "/// 检验单号（业务唯一，如IQC20261010001）. ///")]
        [Description("检验单号")]
        public string CheckNo { get; set; }

        /// <summary>
        /// 到货单主表ID.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_id", IsNullable = true, ColumnDescription = "/// 到货单主表ID. ///")]
        [Description("到货单主表ID")]
        public string ArrivalId { get; set; }

        /// <summary>
        /// 到货单号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_no", IsNullable = true, ColumnDescription = "/// 到货单号（冗余）. ///")]
        [Description("到货单号(冗余)")]
        public string ArrivalNo { get; set; }

        /// <summary>
        /// 到货单明细ID.
        /// </summary>
        [SugarColumn(ColumnName = "arrival_detail_id", IsNullable = true, ColumnDescription = "/// 到货单明细ID. ///")]
        [Description("到货单明细ID")]
        public string ArrivalDetailId { get; set; }

        /// <summary>
        /// 供应商ID（sx_supplier.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "supplier_id", IsNullable = true, ColumnDescription = "/// 供应商ID（sx_supplier.F_ID）. ///")]
        [Description("供应商ID")]
        public string SupplierId { get; set; }

        /// <summary>
        /// 供应商名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "supplier_name", IsNullable = true, ColumnDescription = "/// 供应商名称（冗余）. ///")]
        [Description("供应商名称(冗余)")]
        public string SupplierName { get; set; }

        /// <summary>
        /// 批次号.
        /// </summary>
        [SugarColumn(ColumnName = "batch_no", IsNullable = true, ColumnDescription = "/// 批次号. ///")]
        [Description("批次号")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 存货ID（sx_inventory_master.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_id", IsNullable = true, ColumnDescription = "/// 存货ID（sx_inventory_master.F_ID）. ///")]
        [Description("存货ID")]
        public string InventoryId { get; set; }

        /// <summary>
        /// 存货编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_code", IsNullable = true, ColumnDescription = "/// 存货编码（冗余）. ///")]
        [Description("存货编码(冗余)")]
        public string InventoryCode { get; set; }

        /// <summary>
        /// 存货名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "inventory_name", IsNullable = true, ColumnDescription = "/// 存货名称（冗余）. ///")]
        [Description("存货名称(冗余)")]
        public string InventoryName { get; set; }

        /// <summary>
        /// 检验方式：1扫码检查 2免检.
        /// </summary>
        [SugarColumn(ColumnName = "check_type", IsNullable = true, ColumnDescription = "/// 检验方式：1扫码检查 2免检. ///")]
        [Description("检验方式:1扫码检查 2免检")]
        public int? CheckType { get; set; }

        /// <summary>
        /// 检查项目摘要（如：外观 OK / 高度 OK）.
        /// </summary>
        [SugarColumn(ColumnName = "check_items_summary", IsNullable = true, ColumnDescription = "/// 检查项目摘要（如：外观 OK / 高度 OK）. ///")]
        [Description("检查项目摘要")]
        public string CheckItemsSummary { get; set; }

        /// <summary>
        /// 检验数量.
        /// </summary>
        [SugarColumn(ColumnName = "check_qty", IsNullable = true, ColumnDescription = "/// 检验数量. ///")]
        [Description("检验数量")]
        public decimal? CheckQty { get; set; }

        /// <summary>
        /// 不良数.
        /// </summary>
        [SugarColumn(ColumnName = "bad_qty", IsNullable = true, ColumnDescription = "/// 不良数. ///")]
        [Description("不良数")]
        public decimal? BadQty { get; set; }

        /// <summary>
        /// 检验结果：1合格 2不合格 3免检.
        /// </summary>
        [SugarColumn(ColumnName = "check_result", IsNullable = true, ColumnDescription = "/// 检验结果：1合格 2不合格 3免检. ///")]
        [Description("检验结果:1合格 2不合格 3免检")]
        public int? CheckResult { get; set; }

        /// <summary>
        /// 检验员.
        /// </summary>
        [SugarColumn(ColumnName = "checker_name", IsNullable = true, ColumnDescription = "/// 检验员. ///")]
        [Description("检验员")]
        public string CheckerName { get; set; }

        /// <summary>
        /// 检验时间.
        /// </summary>
        [SugarColumn(ColumnName = "check_time", IsNullable = true, ColumnDescription = "/// 检验时间. ///")]
        [Description("检验时间")]
        public DateTime? CheckTime { get; set; }

        /// <summary>
        /// 不良处理状态：0无需处理 1待处理 2处理中 3已处理.
        /// </summary>
        [SugarColumn(ColumnName = "bad_handle_status", IsNullable = true, ColumnDescription = "/// 不良处理状态：0无需处理 1待处理 2处理中 3已处理. ///")]
        [Description("不良处理状态:0无需处理 1待处理 2处理中 3已处理")]
        public int? BadHandleStatus { get; set; }

        /// <summary>
        /// 不良处理说明.
        /// </summary>
        [SugarColumn(ColumnName = "bad_handle_remark", IsNullable = true, ColumnDescription = "/// 不良处理说明. ///")]
        [Description("不良处理说明")]
        public string BadHandleRemark { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
