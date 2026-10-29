using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 工序检查内容定义表（技术管理-工序管理）.
    /// 定义检查项目/标准/数据类型（数值/文本/下拉）/下拉选项/默认值/必填，
    /// PDA报工时按此展示检查项并录入结果.
    /// </summary>
    [SugarTable("sx_process_check_item")]
    public class ProcessCheckItemEntity : CLDSEntityBase
    {
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
        /// 检查项目（如:基准/厚度/螺纹孔通止端）.
        /// </summary>
        [SugarColumn(ColumnName = "check_item", IsNullable = false, ColumnDescription = "/// 检查项目（如:基准/厚度/螺纹孔通止端）. ///")]
        [Description("检查项目")]
        public string CheckItem { get; set; }

        /// <summary>
        /// 检查标准（如:3.0±0.05mm）.
        /// </summary>
        [SugarColumn(ColumnName = "check_standard", IsNullable = true, ColumnDescription = "/// 检查标准（如:3.0±0.05mm）. ///")]
        [Description("检查标准")]
        public string CheckStandard { get; set; }

        /// <summary>
        /// 数据类型：1数值 2文本 3下拉.
        /// </summary>
        [SugarColumn(ColumnName = "data_type", IsNullable = true, ColumnDescription = "/// 数据类型：1数值 2文本 3下拉. ///")]
        [Description("数据类型:1数值 2文本 3下拉")]
        public int? DataType { get; set; }

        /// <summary>
        /// 下拉选项（数据类型为下拉时有效,如:OK,NG）.
        /// </summary>
        [SugarColumn(ColumnName = "option_items", IsNullable = true, ColumnDescription = "/// 下拉选项（数据类型为下拉时有效,如:OK,NG）. ///")]
        [Description("下拉选项")]
        public string OptionItems { get; set; }

        /// <summary>
        /// 默认值.
        /// </summary>
        [SugarColumn(ColumnName = "default_value", IsNullable = true, ColumnDescription = "/// 默认值. ///")]
        [Description("默认值")]
        public string DefaultValue { get; set; }

        /// <summary>
        /// 是否必填：0否 1是.
        /// </summary>
        [SugarColumn(ColumnName = "required", IsNullable = true, ColumnDescription = "/// 是否必填：0否 1是. ///")]
        [Description("是否必填:0否 1是")]
        public int? Required { get; set; }

        /// <summary>
        /// 顺序号.
        /// </summary>
        [SugarColumn(ColumnName = "seq_no", IsNullable = true, ColumnDescription = "/// 顺序号. ///")]
        [Description("顺序号")]
        public int? SeqNo { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
