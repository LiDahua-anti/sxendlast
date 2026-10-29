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
    /// 零部件查询
    /// </summary>
    public class WarehouseBomQuery : PageInputBase
    {


    }

    /// <summary>
    /// 入库记录列表查询
    /// </summary>
    public class WarehouseInStockQuery : PageInputBase
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
        /// 库位
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// 入库开始时间
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 入库结束时间
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// 在库数量
        /// </summary>
        public string InstockNum { get; set; }

    }

    /// <summary>
    /// 入库添加
    /// </summary>
    public class WarehouseInStockInput
    {
        /// <summary>
        /// bomid
        /// </summary>
        public string BomId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public string Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public string UnitPrice { get; set; }

        /// <summary>
        /// 总价
        /// </summary>
        public string TotalPrice { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 库位id
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

    }

    /// <summary>
    /// 列表
    /// </summary>
    public class WarehouseInStockOutput
    {
        /// <summary>
        /// 入库人
        /// </summary>
        public string? CreatorUserName { get; set; }

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
        /// 单价
        /// </summary>
        public string UnitPrice { get; set; }

        /// <summary>
        /// 总价
        /// </summary>
        public string TotalPrice { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 库位id
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string? CreatorTime { get; set; }
        public string PartsManufacturer { get; set; }

        /// <summary>
        /// 零部件品名
        /// </summary>
        public string PartsName { get; set; }

        /// <summary>
        /// 零部件型号
        /// </summary>
        public string PartsModel { get; set; }

        /// <summary>
        /// 安全在库数量
        /// </summary>
        public string SafeInlibrary { get; set; }

        public string InstockNum { get; set; }


    }

}
