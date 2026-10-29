using JNPF.Common.Filter;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Dto.Custom
{
    /// <summary>
    /// 盘点入参
    /// </summary>
    public class WarehouseTakeStockInput
    {
        /// <summary>
        /// 在库id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 盘点数量
        /// </summary>
        public string TakeQuantity { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

    }

    /// <summary>
    /// 在库查询
    /// </summary>
    public class WarehouseStockQuery : PageInputBase
    {
        /// <summary>
        /// 零部件制造商
        /// </summary>
        public string? PartsManufacturer { get; set; }

        /// <summary>
        /// 零部件型号
        /// </summary>
        public string? PartsModel { get; set; }
        /// <summary>
        /// 零部件品名
        /// </summary>
        public string BomName { get; set; }

        /// <summary>
        /// 安全在库数量
        /// </summary>
        public string? SafeInlibrary { get; set; }

        /// <summary>
        /// 库位
        /// </summary>
        public string LocationId { get; set; }

    }

    /// <summary>
    /// 列表在库
    /// </summary>
    public class WarehouseStockOutput
    {
        /// <summary>
        /// id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// bomid
        /// </summary>
        public string BomId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public string Quantity { get; set; }

        /// <summary>
        /// 库位id
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public string? LastModifyTime { get; set; }

        public string PartsManufacturer { get; set; }

        /// <summary>
        /// 零部件品名
        /// </summary>
        public string PartsName { get; set; }

        /// <summary>
        /// 库位名称
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// 零部件型号
        /// </summary>
        public string PartsModel { get; set; }

        /// <summary>
        /// 安全在库数量
        /// </summary>
        public string SafeInlibrary { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 最后金额
        /// </summary>
        public string LastAmount { get; set; }
    }
}
