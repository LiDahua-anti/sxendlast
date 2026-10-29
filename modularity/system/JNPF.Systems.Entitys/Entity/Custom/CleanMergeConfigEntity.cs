using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 清洗合并码配置表（版本化，历史保留）.
    /// 工序「清洗」由多名员工在同一清洗工位同时工作、合并为一次报工；
    /// 配置变更即升版本（V1→V2→V3），旧版本写失效日期并标记已失效，永久保留可存档追溯。
    /// </summary>
    [SugarTable("sx_clean_merge_config")]
    public class CleanMergeConfigEntity : CLDSEntityBase
    {
        /// <summary>
        /// 工序ID（sx_base_process.F_ID，工序：清洗）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = false, ColumnDescription = "/// 工序ID（sx_base_process.F_ID，工序：清洗）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序名称（冗余）. ///")]
        [Description("工序名称(冗余)")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 版本号（如V1/V2/V3）.
        /// </summary>
        [SugarColumn(ColumnName = "version", IsNullable = false, ColumnDescription = "/// 版本号（如V1/V2/V3）. ///")]
        [Description("版本号")]
        public string Version { get; set; }

        /// <summary>
        /// 报工人数（绩效均分口径，默认4人）.
        /// </summary>
        [SugarColumn(ColumnName = "member_count", IsNullable = true, ColumnDescription = "/// 报工人数（绩效均分口径，默认4人）. ///")]
        [Description("报工人数(绩效均分口径)")]
        public int? MemberCount { get; set; }

        /// <summary>
        /// 生效日期.
        /// </summary>
        [SugarColumn(ColumnName = "effect_date", IsNullable = true, ColumnDescription = "/// 生效日期. ///")]
        [Description("生效日期")]
        public DateTime? EffectDate { get; set; }

        /// <summary>
        /// 失效日期（变更后旧版本写失效日期，永久保留）.
        /// </summary>
        [SugarColumn(ColumnName = "expire_date", IsNullable = true, ColumnDescription = "/// 失效日期（变更后旧版本写失效日期，永久保留）. ///")]
        [Description("失效日期")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>
        /// 状态：1生效中 0已失效.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1生效中 0已失效. ///")]
        [Description("状态:1生效中 0已失效")]
        public int? Status { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
