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
    /// 盘点修正入参
    /// </summary>
    public class WarehouseTakeCorrectInput
    {

        /// <summary>
        /// 差异id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 修正数
        /// </summary>
        public string CorretStock { get; set; }

        /// <summary>
        /// 差异原因
        /// </summary>
        public string CorretReason { get; set; }
    }

    /// <summary>
    /// 盘点列表查询
    /// </summary>
    public class WarehouseTakeQuery : PageInputBase
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
        /// 盘点开始时间
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 盘点结束时间
        /// </summary>
        public string EndTime { get; set; }
    }

    /// <summary>
    /// 盘点列表返回参数
    /// </summary>
    public class WarehouseTakeStockOutput
    {
        public string Id { get; set; }

        public string LocationId { get; set; }

        /// <summary>
        /// bomid
        /// </summary>
        public string BomId { get; set; }


        /// <summary>
        /// 当前在库数
        /// </summary>
        public string CurrentStock { get; set; }

        /// <summary>
        /// 实际在库数
        /// </summary>
        public string ActualStock { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }

        public string? CreatorUserName { get; set; }

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

    }

    /// <summary>
    /// 盘点差异列表返回参数
    /// </summary>
    public class WarehouseTakeDiffStockOutput
    {
        public string Id { get; set; }

        public string LocationId { get; set; }

        /// <summary>
        /// bomid
        /// </summary>
        public string BomId { get; set; }


        /// <summary>
        /// 当前在库数
        /// </summary>
        public string CurrentStock { get; set; }

        /// <summary>
        /// 实际在库数
        /// </summary>
        public string ActualStock { get; set; }

        /// <summary>
        /// 修正数
        /// </summary>
        public string CorretStock { get; set; }

        /// <summary>
        /// 差异原因
        /// </summary>
        public string CorretReason { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }

        public string? CreatorUserName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string? CreatorTime { get; set; }

        public string PartsManufacturer { get; set; }

        /// <summary>
        /// 是否确认（0 否  1是）
        /// </summary>
        public int IsConfirm { get; set; }

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

    }
}
