using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 物料
    /// </summary>
    [SugarTable("sx_warehouse_bom")]
    public class WarehouseBomEntity : CLDSEntityBase
    {
        /// <summary>
        /// 零部件制造商
        /// </summary>
        [SugarColumn(ColumnName = "parts_manufacturer", Length = 200, IsNullable = true, ColumnDescription = "/// 零部件制造商 ///")]
        [Description("零部件制造商")]
        public string? PartsManufacturer { get; set; }

        /// <summary>
        /// 零部件品名
        /// </summary>
        [SugarColumn(ColumnName = "parts_name", Length = 200, IsNullable = false, ColumnDescription = "/// 零部件品名 ///")]
        [Description("零部件品名")]
        public string? PartsName { get; set; }

        /// <summary>
        /// 零部件型号
        /// </summary>
        [SugarColumn(ColumnName = "parts_model", Length = 200, IsNullable = true, ColumnDescription = "/// 零部件型号 ///")]
        [Description("零部件型号")]
        public string? PartsModel { get; set; }

        /// <summary>
        /// 安全在库数量
        /// </summary>
        [SugarColumn(ColumnName = "safe_inlibrary", Length = 100, IsNullable = true, ColumnDescription = "/// 安全在库数量 ///")]
        [Description("安全在库数量")]
        public string? SafeInlibrary { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "f_note", Length = 500, IsNullable = true, ColumnDescription = "/// 备注 ///")]
        [Description("备注")]
        public string? Note { get; set; }

        /// <summary>
        /// 规格
        /// </summary>
        [SugarColumn(ColumnName = "model_spec", Length = 200, IsNullable = true, ColumnDescription = "/// 规格 ///")]
        [Description("规格")]
        public string? ModelSpec { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        [SugarColumn(ColumnName = "unit", Length = 50, IsNullable = true, ColumnDescription = "/// 单位 ///")]
        [Description("单位")]
        public string? Unit { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [SugarColumn(ColumnName = "unit_price", IsNullable = true, ColumnDescription = "/// 单价 ///")]
        [Description("单价")]
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        [SugarColumn(ColumnName = "weight", IsNullable = true, ColumnDescription = "/// 重量 ///")]
        [Description("重量")]
        public decimal? Weight { get; set; }

        /// <summary>
        /// 流程引擎Id
        /// </summary>
        [SugarColumn(ColumnName = "f_flow_id", Length = 200, IsNullable = true, ColumnDescription = "/// 流程引擎Id ///")]
        [StringLength(50)]
        public string? FFlowId { get; set; }

        /// <summary>
        /// 流程任务Id
        /// </summary>
        [SugarColumn(ColumnName = "f_flow_task_id", Length = 200, IsNullable = true, ColumnDescription = "/// 流程任务Id ///")]
        [StringLength(50)]
        public string? FFlowTaskId { get; set; }

        /// <summary>
        /// 集成助手数据标识
        /// </summary>
        [SugarColumn(ColumnName = "f_inte_assistant", IsNullable = true, ColumnDescription = "/// 集成助手数据标识 ///")]
        public int FInteAssistant { get; set; }
    }
}
