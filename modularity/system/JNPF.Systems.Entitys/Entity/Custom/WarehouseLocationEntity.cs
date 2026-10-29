using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 货位档案表（仓库管理）.
    /// 按「仓库 → 货位」两级结构；货位编码按 区-排-列-层 规则（如 A1-1-1）；
    /// 支持最大体积/重量校验与条形码绑定，PDA扫码入库时按货位码落位.
    /// </summary>
    [SugarTable("sx_warehouse_location")]
    public class WarehouseLocationEntity : CLDSEntityBase
    {
        /// <summary>
        /// 库位名称 
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_name", Length = 200, IsNullable = false, ColumnDescription = "/// 库位名称 ///")]
        [Description("库位名称 ")]
        public string WarehouseName { get; set; }

        /// <summary>
        /// 仓库ID（sx_warehouse.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_id", IsNullable = true, ColumnDescription = "/// 仓库ID（sx_warehouse.F_ID）. ///")]
        [Description("仓库ID")]
        public string WarehouseId { get; set; }

        /// <summary>
        /// 仓库编码（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "warehouse_code", IsNullable = true, ColumnDescription = "/// 仓库编码（冗余）. ///")]
        [Description("仓库编码")]
        public string WarehouseCode { get; set; }

        /// <summary>
        /// 货位编码（区-排-列-层，如A1-1-1）.
        /// </summary>
        [SugarColumn(ColumnName = "location_code", IsNullable = true, ColumnDescription = "/// 货位编码（区-排-列-层，如A1-1-1）. ///")]
        [Description("货位编码")]
        public string LocationCode { get; set; }

        /// <summary>
        /// 区号.
        /// </summary>
        [SugarColumn(ColumnName = "area_no", IsNullable = true, ColumnDescription = "/// 区号. ///")]
        [Description("区号")]
        public string AreaNo { get; set; }

        /// <summary>
        /// 排号.
        /// </summary>
        [SugarColumn(ColumnName = "row_no", IsNullable = true, ColumnDescription = "/// 排号. ///")]
        [Description("排号")]
        public string RowNo { get; set; }

        /// <summary>
        /// 列号.
        /// </summary>
        [SugarColumn(ColumnName = "column_no", IsNullable = true, ColumnDescription = "/// 列号. ///")]
        [Description("列号")]
        public string ColumnNo { get; set; }

        /// <summary>
        /// 层号.
        /// </summary>
        [SugarColumn(ColumnName = "layer_no", IsNullable = true, ColumnDescription = "/// 层号. ///")]
        [Description("层号")]
        public string LayerNo { get; set; }

        /// <summary>
        /// 最大体积（m³，校验用）.
        /// </summary>
        [SugarColumn(ColumnName = "max_volume", IsNullable = true, ColumnDescription = "/// 最大体积（m³，校验用）. ///")]
        [Description("最大体积")]
        public decimal? MaxVolume { get; set; }

        /// <summary>
        /// 最大重量（kg，校验用）.
        /// </summary>
        [SugarColumn(ColumnName = "max_weight", IsNullable = true, ColumnDescription = "/// 最大重量（kg，校验用）. ///")]
        [Description("最大重量")]
        public decimal? MaxWeight { get; set; }

        /// <summary>
        /// 条形码（PDA扫码识别）.
        /// </summary>
        [SugarColumn(ColumnName = "barcode", IsNullable = true, ColumnDescription = "/// 条形码（PDA扫码识别）. ///")]
        [Description("条形码")]
        public string Barcode { get; set; }

        /// <summary>
        /// 状态：1可用 0停用.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1可用 0停用. ///")]
        [Description("状态:1可用 0停用")]
        public int? Status { get; set; }

        /// <summary>
        /// 流程引擎Id
        /// </summary>
        [SugarColumn(ColumnName = "f_flow_id", Length = 200, IsNullable = true, ColumnDescription = "/// 流程引擎Id ///")]
        [StringLength(50)]
        public string FFlowId { get; set; }

        /// <summary>
        /// 流程任务Id
        /// </summary>
        [SugarColumn(ColumnName = "f_flow_task_id", Length = 200, IsNullable = true, ColumnDescription = "/// 流程任务Id ///")]
        [StringLength(50)]
        public string FFlowTaskId { get; set; }

        /// <summary>
        /// 集成助手数据标识
        /// </summary>
        [SugarColumn(ColumnName = "f_inte_assistant", IsNullable = true, ColumnDescription = "/// 集成助手数据标识 ///")]
        public int FInteAssistant { get; set; }
    }
}

