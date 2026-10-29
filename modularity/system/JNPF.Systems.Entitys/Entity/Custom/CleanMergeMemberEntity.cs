using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 清洗合并码成员表.
    /// 配置变更生成新版本成员记录，历史版本成员永久保留可追溯；
    /// 历史报工记录仍按变更前的工位人员存档（member_snapshot），保证绩效工时与当时实际参与人员一致。
    /// </summary>
    [SugarTable("sx_clean_merge_member")]
    public class CleanMergeMemberEntity : CLDSEntityBase
    {
        /// <summary>
        /// 清洗合并码配置ID（sx_clean_merge_config.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "config_id", IsNullable = false, ColumnDescription = "/// 清洗合并码配置ID（sx_clean_merge_config.F_ID）. ///")]
        [Description("清洗合并码配置ID")]
        public string ConfigId { get; set; }

        /// <summary>
        /// 工号（如E001）.
        /// </summary>
        [SugarColumn(ColumnName = "employee_no", IsNullable = true, ColumnDescription = "/// 工号（如E001）. ///")]
        [Description("工号")]
        public string EmployeeNo { get; set; }

        /// <summary>
        /// 姓名（如张伟）.
        /// </summary>
        [SugarColumn(ColumnName = "employee_name", IsNullable = true, ColumnDescription = "/// 姓名（如张伟）. ///")]
        [Description("姓名")]
        public string EmployeeName { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
