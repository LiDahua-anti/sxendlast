using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{

    [SugarTable("sx_base_model_bom")]
    public class BaseModelBomEntity : CLDSEntityBase
    {
        /// <summary>
        /// 物料id
        /// </summary>
        [SugarColumn(ColumnName = "bom_id", IsNullable = true, ColumnDescription = "/// 物料id ///")]
        [Description("物料id")]
        public string BomId { get; set; }

        /// <summary>
        /// 机型id
        /// </summary>
        [SugarColumn(ColumnName = "model_id", IsNullable = false, ColumnDescription = "/// 机型id ///")]
        [Description("机型id")]
        public string ModelId { get; set; }
    }
}
