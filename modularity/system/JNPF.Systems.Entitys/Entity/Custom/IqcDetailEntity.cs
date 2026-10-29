using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 来料检验（IQC）明细表.
    /// 每个检查项目记录实测值与判定结果（OK/NG）；免检时无明细（自动跳过检查项）。
    /// </summary>
    [SugarTable("sx_iqc_detail")]
    public class IqcDetailEntity : CLDSEntityBase
    {
        /// <summary>
        /// IQC主表ID（sx_iqc.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "iqc_id", IsNullable = false, ColumnDescription = "/// IQC主表ID（sx_iqc.F_ID）. ///")]
        [Description("IQC主表ID")]
        public string IqcId { get; set; }

        /// <summary>
        /// 检查项目（如：外观/高度）.
        /// </summary>
        [SugarColumn(ColumnName = "check_item", IsNullable = true, ColumnDescription = "/// 检查项目（如：外观/高度）. ///")]
        [Description("检查项目")]
        public string CheckItem { get; set; }

        /// <summary>
        /// 检查标准.
        /// </summary>
        [SugarColumn(ColumnName = "check_standard", IsNullable = true, ColumnDescription = "/// 检查标准. ///")]
        [Description("检查标准")]
        public string CheckStandard { get; set; }

        /// <summary>
        /// 实测值.
        /// </summary>
        [SugarColumn(ColumnName = "actual_value", IsNullable = true, ColumnDescription = "/// 实测值. ///")]
        [Description("实测值")]
        public string ActualValue { get; set; }

        /// <summary>
        /// 判定结果：OK/NG.
        /// </summary>
        [SugarColumn(ColumnName = "check_result", IsNullable = true, ColumnDescription = "/// 判定结果：OK/NG. ///")]
        [Description("判定结果:OK/NG")]
        public string CheckResult { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
