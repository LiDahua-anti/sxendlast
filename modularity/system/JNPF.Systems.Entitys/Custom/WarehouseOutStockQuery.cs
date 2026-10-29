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
    /// 在库查询
    /// </summary>
    public class WarehouseOutStockQuery : PageInputBase
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
        /// 出库数量
        /// </summary>
        public string OutQuantity { get; set; }

        /// <summary>
        /// 出库开始时间
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 出库开始结束
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// 出库类型（0-预防维护，1-创建，2-维修，3-其他）
        /// </summary>
        public int ReceiveType { get; set; }


    }

    /// <summary>
    /// 列表出库
    /// </summary>
    public class WarehouseOutStockOutput
    {

        /// <summary>
        /// 出库主键
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 库位
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// 出库数量
        /// </summary>
        public string OutQuantity { get; set; }

        /// 创建时间
        /// </summary>
        public string? CreatorTime { get; set; }

        /// <summary>
        /// 出库人
        /// </summary>
        public string? CreatorUserName { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 外部id
        /// </summary>
        public string ExternalId { get; set; }

        /// <summary>
        /// bomid
        /// </summary>
        public string BomId { get; set; }

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

        /// <summary>
        /// 使用部门
        /// </summary>
        public string UseDeptName { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceNo { get; set; }

        /// <summary>
        /// 出库类型（0-预防维护，1-创建，2-维修，3-其他）
        /// </summary>
        public int ReceiveType { get; set; }

        /// <summary>
        /// 维修单号
        /// </summary>
        public string RepairRecordNo { get; set; }

        /// <summary>
        /// 领用时间
        /// </summary>
        public string? ReceiveTime { get; set; }

        /// <summary>
        /// 领用人
        /// </summary>
        public string? ReceivUserName { get; set; }

    }
}
