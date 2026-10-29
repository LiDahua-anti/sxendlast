using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 人效考核月汇总表（人工管理）.
    /// 由报工记录按月统计生成：合格产量=Σ报工良品；不良率=Σ不良/Σ报工；
    /// 工时利用率=有效工时/出勤工时；绩效得分按等级 A/B/C 归档.
    /// </summary>
    [SugarTable("sx_efficiency")]
    public class EfficiencyEntity : CLDSEntityBase
    {
        /// <summary>
        /// 工号.
        /// </summary>
        [SugarColumn(ColumnName = "employee_no", IsNullable = false, ColumnDescription = "/// 工号. ///")]
        [Description("工号")]
        public string EmployeeNo { get; set; }

        /// <summary>
        /// 姓名.
        /// </summary>
        [SugarColumn(ColumnName = "employee_name", IsNullable = true, ColumnDescription = "/// 姓名. ///")]
        [Description("姓名")]
        public string EmployeeName { get; set; }

        /// <summary>
        /// 班组（A班/B班等，取班次名称）.
        /// </summary>
        [SugarColumn(ColumnName = "team_name", IsNullable = true, ColumnDescription = "/// 班组（A班/B班等，取班次名称）. ///")]
        [Description("班组")]
        public string TeamName { get; set; }

        /// <summary>
        /// 月份（如2026-09）.
        /// </summary>
        [SugarColumn(ColumnName = "month", IsNullable = false, ColumnDescription = "/// 月份（如2026-09）. ///")]
        [Description("月份")]
        public string Month { get; set; }

        /// <summary>
        /// 出勤天数.
        /// </summary>
        [SugarColumn(ColumnName = "attend_days", IsNullable = true, ColumnDescription = "/// 出勤天数. ///")]
        [Description("出勤天数")]
        public int? AttendDays { get; set; }

        /// <summary>
        /// 合格产量（Σ报工良品数，均分后）.
        /// </summary>
        [SugarColumn(ColumnName = "qualified_qty", IsNullable = true, ColumnDescription = "/// 合格产量（Σ报工良品数，均分后）. ///")]
        [Description("合格产量")]
        public int? QualifiedQty { get; set; }

        /// <summary>
        /// 不良率（%）.
        /// </summary>
        [SugarColumn(ColumnName = "bad_rate", IsNullable = true, ColumnDescription = "/// 不良率（%）. ///")]
        [Description("不良率(%)")]
        public decimal? BadRate { get; set; }

        /// <summary>
        /// 工时利用率（%）.
        /// </summary>
        [SugarColumn(ColumnName = "work_hours_rate", IsNullable = true, ColumnDescription = "/// 工时利用率（%）. ///")]
        [Description("工时利用率(%)")]
        public decimal? WorkHoursRate { get; set; }

        /// <summary>
        /// 绩效得分等级：A/B/C.
        /// </summary>
        [SugarColumn(ColumnName = "score_level", IsNullable = true, ColumnDescription = "/// 绩效得分等级：A/B/C. ///")]
        [Description("绩效得分等级:A/B/C")]
        public string ScoreLevel { get; set; }

        /// <summary>
        /// 绩效得分（百分制）.
        /// </summary>
        [SugarColumn(ColumnName = "score", IsNullable = true, ColumnDescription = "/// 绩效得分（百分制）. ///")]
        [Description("绩效得分")]
        public decimal? Score { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
