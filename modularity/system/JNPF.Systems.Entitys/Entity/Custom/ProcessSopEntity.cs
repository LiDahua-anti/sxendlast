using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 工序作业指导（SOP）文件表（技术管理-工序管理）.
    /// 存放本工序的作业指导标准和作业图片，PDA报工时可查看.
    /// </summary>
    [SugarTable("sx_process_sop")]
    public class ProcessSopEntity : CLDSEntityBase
    {
        /// <summary>
        /// 工序ID（sx_base_process.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "process_id", IsNullable = false, ColumnDescription = "/// 工序ID（sx_base_process.F_ID）. ///")]
        [Description("工序ID")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 工序编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_code", IsNullable = true, ColumnDescription = "/// 工序编码（冗余）. ///")]
        [Description("工序编码")]
        public string ProcessCode { get; set; }

        /// <summary>
        /// 工序名称（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工序名称（冗余）. ///")]
        [Description("工序名称")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 文件名（如:CNC粗加工-作业指导书V3.pdf）.
        /// </summary>
        [SugarColumn(ColumnName = "file_name", IsNullable = false, ColumnDescription = "/// 文件名（如:CNC粗加工-作业指导书V3.pdf）. ///")]
        [Description("文件名")]
        public string FileName { get; set; }

        /// <summary>
        /// 文件类型（PDF/图片）.
        /// </summary>
        [SugarColumn(ColumnName = "file_type", IsNullable = true, ColumnDescription = "/// 文件类型（PDF/图片）. ///")]
        [Description("文件类型")]
        public string FileType { get; set; }

        /// <summary>
        /// 文件大小（展示用,如2.4MB）.
        /// </summary>
        [SugarColumn(ColumnName = "file_size", IsNullable = true, ColumnDescription = "/// 文件大小（展示用,如2.4MB）. ///")]
        [Description("文件大小")]
        public string FileSize { get; set; }

        /// <summary>
        /// 文件存储路径.
        /// </summary>
        [SugarColumn(ColumnName = "file_path", IsNullable = true, ColumnDescription = "/// 文件存储路径. ///")]
        [Description("文件存储路径")]
        public string FilePath { get; set; }

        /// <summary>
        /// 版本.
        /// </summary>
        [SugarColumn(ColumnName = "version", IsNullable = true, ColumnDescription = "/// 版本. ///")]
        [Description("版本")]
        public string Version { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
