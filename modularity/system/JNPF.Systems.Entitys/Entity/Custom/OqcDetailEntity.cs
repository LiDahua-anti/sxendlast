using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 质量检验（OQC抽检）明细表.
    /// 每个检查项目记录实测值与判定结果（OK/NG），检查项目按工序检查内容定义带出。
    /// </summary>
    [SugarTable("sx_oqc_detail")]
    public class OqcDetailEntity : CLDSEntityBase
    {
        /// <summary>
        /// OQC主表ID（sx_oqc.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "oqc_id", IsNullable = false, ColumnDescription = "/// OQC主表ID（sx_oqc.F_ID）. ///")]
        [Description("OQC主表ID")]
        public string OqcId { get; set; }

        /// <summary>
        /// 检查项目.
        /// </summary>
        [SugarColumn(ColumnName = "check_item", IsNullable = true, ColumnDescription = "/// 检查项目. ///")]
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
