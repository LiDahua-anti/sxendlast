using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 工艺路线工序明细表（技术管理）.
    /// 按顺序记录路线的各道工序（工序/顺序/标准工时/是否检查工序），
    /// 工序检查项随路线流转至PDA报工.
    /// </summary>
    [SugarTable("sx_routing_process")]
    public class RoutingProcessEntity : CLDSEntityBase
    {
        /// <summary>
        /// 工艺路线ID（sx_routing.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "routing_id", IsNullable = false, ColumnDescription = "/// 工艺路线ID（sx_routing.F_ID）. ///")]
        [Description("工艺路线ID")]
        public string RoutingId { get; set; }

        /// <summary>
        /// 工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = false, ColumnDescription = "/// 工序ID（sx_base_process.F_ID）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序编码（冗余,如GX01）.
        /// </summary>
        [SugarColumn(ColumnName = "process_code", IsNullable = true, ColumnDescription = "/// 工序编码（冗余,如GX01）. ///")]
        [Description("工序编码")]
        public string ProcessCode { get; set; }

        /// <summary>
        /// 工序名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序名称（冗余）. ///")]
        [Description("工序名称")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 顺序号.
        /// </summary>
        [SugarColumn(ColumnName = "seq_no", IsNullable = true, ColumnDescription = "/// 顺序号. ///")]
        [Description("顺序号")]
        public int? SeqNo { get; set; }

        /// <summary>
        /// 标准工时（min）.
        /// </summary>
        [SugarColumn(ColumnName = "standard_hours", IsNullable = true, ColumnDescription = "/// 标准工时（min）. ///")]
        [Description("标准工时(min)")]
        public decimal? StandardHours { get; set; }

        /// <summary>
        /// 工序要求.
        /// </summary>
        [SugarColumn(ColumnName = "requirement", IsNullable = true, ColumnDescription = "/// 工序要求. ///")]
        [Description("工序要求")]
        public string Requirement { get; set; }

        /// <summary>
        /// 是否检查工序（最后一步检查）：0否 1是.
        /// </summary>
        [SugarColumn(ColumnName = "is_check_process", IsNullable = true, ColumnDescription = "/// 是否检查工序（最后一步检查）：0否 1是. ///")]
        [Description("是否检查工序:0否 1是")]
        public int? IsCheckProcess { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
