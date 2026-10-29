using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 员工技能表（人工管理）.
    /// 记录员工技能等级、认证方式（内部/外部）、证书信息及关联工序.
    /// </summary>
    [SugarTable("sx_employee_skill")]
    public class EmployeeSkillEntity : CLDSEntityBase
    {
        /// <summary>
        /// 工号（关联平台用户/员工档案）.
        /// </summary>
        [SugarColumn(ColumnName = "employee_no", IsNullable = false, ColumnDescription = "/// 工号（关联平台用户/员工档案）. ///")]
        [Description("工号")]
        public string EmployeeNo { get; set; }

        /// <summary>
        /// 姓名（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "employee_name", IsNullable = true, ColumnDescription = "/// 姓名（冗余）. ///")]
        [Description("姓名")]
        public string EmployeeName { get; set; }

        /// <summary>
        /// 技能名称（如:机器人焊接操作）.
        /// </summary>
        [SugarColumn(ColumnName = "skill_name", IsNullable = false, ColumnDescription = "/// 技能名称（如:机器人焊接操作）. ///")]
        [Description("技能名称")]
        public string SkillName { get; set; }

        /// <summary>
        /// 技能等级：L1/L2/L3.
        /// </summary>
        [SugarColumn(ColumnName = "skill_level", IsNullable = true, ColumnDescription = "/// 技能等级：L1/L2/L3. ///")]
        [Description("技能等级:L1/L2/L3")]
        public string SkillLevel { get; set; }

        /// <summary>
        /// 认证方式：1内(内部认证) 2外(外部认证).
        /// </summary>
        [SugarColumn(ColumnName = "cert_type", IsNullable = true, ColumnDescription = "/// 认证方式：1内(内部认证) 2外(外部认证). ///")]
        [Description("认证方式:1内 2外")]
        public int? CertType { get; set; }

        /// <summary>
        /// 证书编号.
        /// </summary>
        [SugarColumn(ColumnName = "cert_no", IsNullable = true, ColumnDescription = "/// 证书编号. ///")]
        [Description("证书编号")]
        public string CertNo { get; set; }

        /// <summary>
        /// 发证单位.
        /// </summary>
        [SugarColumn(ColumnName = "cert_org", IsNullable = true, ColumnDescription = "/// 发证单位. ///")]
        [Description("发证单位")]
        public string CertOrg { get; set; }

        /// <summary>
        /// 状态：1有效 0失效.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1有效 0失效. ///")]
        [Description("状态:1有效 0失效")]
        public int? Status { get; set; }

        /// <summary>
        /// 关联工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = true, ColumnDescription = "/// 关联工序ID（sx_base_process.F_ID）. ///")]
        [Description("关联工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 关联工序名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 关联工序名称（冗余）. ///")]
        [Description("关联工序名称")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
