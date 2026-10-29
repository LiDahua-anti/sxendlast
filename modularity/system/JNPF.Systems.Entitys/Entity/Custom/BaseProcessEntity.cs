using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    [SugarTable("sx_base_process")]
    public class BaseProcessEntity : CLDSEntityBase
    {
        //工艺名称   工序编号  工序类型  标准周期  作业场所  工序内容  备注
        /// <summary>
        /// 工艺名称
        /// </summary>
        [SugarColumn(ColumnName = "process_name", IsNullable = true, ColumnDescription = "/// 工艺名称 ///")]
        [Description("工艺名称")]
        public string ProcessName { get; set; }

        /// <summary>
        /// 工序编号
        /// </summary>
        [SugarColumn(ColumnName = "process_code", IsNullable = true, ColumnDescription = "/// 工序编号 ///")]
        [Description("工序编号")]
        public string ProcessCode { get; set; }

        /// <summary>
        /// 工序类型
        /// </summary>
        [SugarColumn(ColumnName = "process_type", IsNullable = true, ColumnDescription = "/// 工序类型 ///")]
        [Description("工序类型")]
        public string ProcessType { get; set; }

        /// <summary>
        /// 标准周期
        /// </summary>
        [SugarColumn(ColumnName = "standard_cycle", IsNullable = true, ColumnDescription = "/// 标准周期 ///")]
        [Description("标准周期")]
        public string StandardCycle { get; set; }

        /// <summary>
        /// 作业场所
        /// </summary>
        [SugarColumn(ColumnName = "work_location", IsNullable = true, ColumnDescription = "/// 作业场所 ///")]
        [Description("作业场所")]
        public string WorkLocation { get; set; }

        /// <summary>
        /// 工序内容
        /// </summary>
        [SugarColumn(ColumnName = "process_content", IsNullable = true, ColumnDescription = "/// 工序内容 ///")]
        [Description("工序内容")]
        public string ProcessContent { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注 ///")]
        [Description("备注")]
        public string Remark { get; set; }

    }
}
