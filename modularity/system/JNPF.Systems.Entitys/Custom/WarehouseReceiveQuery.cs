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
    /// 领用列表查询
    /// </summary>
    public class WarehouseReceiveConfirmInput
    {
        /// <summary>
        /// id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

    }

    /// <summary>
    /// 领用列表查询
    /// </summary>
    public class WarehouseReceiveQuery : PageInputBase
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
        /// 状态（默认-1  0-已确认，1-未确认）
        /// </summary>
        public int Status { get; set; }


        /// <summary>
        /// 领用开始时间
        /// </summary>
        public string? StartTime { get; set; }

        /// <summary>
        /// 领用结束时间
        /// </summary>
        public string? EndTime { get; set; }

        /// <summary>
        /// 领用人id
        /// </summary>
        public string? RecUserName { get; set; }

    }

    /// <summary>
    /// 新增领用
    /// </summary>
    public class WarehouseReceiveInput
    {
        /// <summary>
        /// 库位
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// 申请领用数量
        /// </summary>
        public string ReceiveQuantity { get; set; }

        /// <summary>
        /// 状态（0-已确认，1-未确认）
        /// </summary>
        public int Status { get; set; } = 1;

        /// <summary>
        /// 领用类型（0-预防维护，1-创建，2-维修，3-其他）
        /// </summary>
        public int ReceiveType { get; set; } = 1;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 关联的物料ID
        /// </summary>
        public string BomId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }
    }

    /// <summary>
    /// 列表输出
    /// </summary>
    public class WarehouseReceiveOutput
    {
        public string Id { get; set; }

        public string? ReceiveQuantity { get; set; }

        /// <summary>
        /// 状态（0-已确认，1-未确认）
        /// </summary>
        public int Status { get; set; } = 1;

        /// <summary>
        /// 外部单号
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// 领用类型（0-预防维护，1-创建，2-维修，3-其他）
        /// </summary>
        public int ReceiveType { get; set; } = 3;

        /// <summary>
        /// 库位
        /// </summary>
        public string? LocationId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 关联的物料ID
        /// </summary>
        public string? BomId { get; set; }

        /// <summary>
        /// 使用部门
        /// </summary>
        public string UserDeptName { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// 零部件制造商
        /// </summary>
        public string? PartsManufacturer { get; set; }

        /// <summary>
        /// 零部件品名
        /// </summary>
        public string? PartsName { get; set; }

        /// <summary>
        /// 零部件型号
        /// </summary>
        public string? PartsModel { get; set; }

        /// <summary>
        /// 安全在库数量
        /// </summary>
        public string? SafeInlibrary { get; set; }

        /// <summary>
        /// 领用时间
        /// </summary>
        public string? CreatorTime { get; set; }

        /// <summary>
        /// 领用人
        /// </summary>
        public string? CreatorUserName { get; set; }

        /// <summary>
        /// 维修单号
        /// </summary>
        public string RepairRecordNo { get; set; }

    }
}
