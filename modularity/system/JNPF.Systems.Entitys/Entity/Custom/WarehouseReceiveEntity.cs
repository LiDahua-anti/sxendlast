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
    [SugarTable("sx_warehouse_receive")]
    public class WarehouseReceiveEntity : CLDSEntityBase
    {

        /// <summary>
        /// 库位
        /// </summary>
        [SugarColumn(ColumnName = "location_id", IsNullable = false, ColumnDescription = "/// 库位 ///")]
        [Description("库位")]
        public string LocationId { get; set; }

        /// <summary>
        /// 申请领用数量
        /// </summary>
        [SugarColumn(ColumnName = "receive_quantity", IsNullable = false, ColumnDescription = "/// 申请领用数量 ///")]
        [Description("申请领用数量")]
        public string ReceiveQuantity { get; set; }

        /// <summary>
        /// 状态（0-已确认，1-未确认）
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = false, DefaultValue = "1", ColumnDescription = "/// 状态（0-已确认，1-未确认） ///")]
        [Description("状态（0-已确认，1-未确认）")]
        public int Status { get; set; } = 1;

        /// <summary>
        /// 外部单号
        /// </summary>
        [SugarColumn(ColumnName = "external_id", Length = 100, IsNullable = true, ColumnDescription = "/// 外部单号 ///")]
        [Description("外部Id（维修单号）")]
        public string ExternalId { get; set; }

        /// <summary>
        /// 领用类型（0-预防维护，1-创建，2-维修，3-其他）
        /// </summary>
        [SugarColumn(ColumnName = "receive_type", IsNullable = false, DefaultValue = "3", ColumnDescription = "/// 领用类型（0-预防维护，1-创建，2-维修，3-其他） ///")]
        [Description("领用类型（0-预防维护，1-创建，2-维修，3-其他）")]
        public int ReceiveType { get; set; } = 3;

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "/// 备注 ///")]
        [Description("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 关联的物料ID
        /// </summary>
        [SugarColumn(ColumnName = "bom_id", Length = 200, IsNullable = false, ColumnDescription = "/// 关联的物料ID ///")]
        [Description("关联的物料ID")]
        public string BomId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        [SugarColumn(ColumnName = "device_id", Length = 200, IsNullable = false, ColumnDescription = "/// 设备id ///")]
        [Description("设备id")]
        public string DeviceId { get; set; }

    }
}
