using JNPF.Common.Filter;
using System;
using System.Collections.Generic;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 库存台账/流水（StockService）

/// <summary>
/// 库存台账查询条件（MES实时库存）.
/// </summary>
public class StockQuery : PageInputBase
{
    /// <summary>
    /// 仓库ID.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 物料关键字（存货编码/名称）.
    /// </summary>
    public string InventoryKeyword { get; set; }

    /// <summary>
    /// 库存状态：1正常 2低于安全库存，-1为全部.
    /// </summary>
    public int StockStatus { get; set; } = -1;
}

/// <summary>
/// 库存台账列表输出.
/// </summary>
public class StockListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 库位.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 在库数量.
    /// </summary>
    public decimal? StockQty { get; set; }

    /// <summary>
    /// 占用数量.
    /// </summary>
    public decimal? OccupiedQty { get; set; }

    /// <summary>
    /// 可用数量（=在库-占用）.
    /// </summary>
    public decimal AvailableQty { get; set; }

    /// <summary>
    /// 安全库存.
    /// </summary>
    public decimal? SafeQty { get; set; }

    /// <summary>
    /// 库存状态：1正常 2低于安全库存.
    /// </summary>
    public int StockStatus { get; set; }

    /// <summary>
    /// 入库时间（先进先出排序依据）.
    /// </summary>
    public DateTime? InTime { get; set; }
}

/// <summary>
/// 库存查询统计卡片（在库存货种类/库存总量/可用库存合计/低于安全库存）.
/// </summary>
public class StockStatisticsOutput
{
    /// <summary>
    /// 在库存货种类.
    /// </summary>
    public int InventoryKindCount { get; set; }

    /// <summary>
    /// 库存总量.
    /// </summary>
    public decimal TotalQty { get; set; }

    /// <summary>
    /// 可用库存合计.
    /// </summary>
    public decimal AvailableQtyTotal { get; set; }

    /// <summary>
    /// 低于安全库存.
    /// </summary>
    public int BelowSafeCount { get; set; }
}

/// <summary>
/// 库存流水查询条件.
/// </summary>
public class StockFlowQuery : PageInputBase
{
    /// <summary>
    /// 存货编码（模糊）.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 业务类型：1采购入库 2产成品入库 3其它入库 4销售出库 5其他出库 6领料出库，-1为全部.
    /// </summary>
    public int BizType { get; set; } = -1;

    /// <summary>
    /// 方向：1入库 2出库，-1为全部.
    /// </summary>
    public int Direction { get; set; } = -1;

    /// <summary>
    /// 关联单号（模糊）.
    /// </summary>
    public string BizNo { get; set; }

    /// <summary>
    /// 操作日期-起.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 操作日期-止.
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 库存流水列表输出.
/// </summary>
public class StockFlowListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 货位编码.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 业务类型.
    /// </summary>
    public int? BizType { get; set; }

    /// <summary>
    /// 方向：1入库 2出库.
    /// </summary>
    public int? Direction { get; set; }

    /// <summary>
    /// 变动数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 变动前在库数量.
    /// </summary>
    public decimal? BeforeQty { get; set; }

    /// <summary>
    /// 变动后在库数量.
    /// </summary>
    public decimal? AfterQty { get; set; }

    /// <summary>
    /// 关联业务单号.
    /// </summary>
    public string BizNo { get; set; }

    /// <summary>
    /// 关联生产订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 关联流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 操作人.
    /// </summary>
    public string OperatorName { get; set; }

    /// <summary>
    /// 操作时间.
    /// </summary>
    public DateTime? OperateTime { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 先进先出批次锁定明细（最早入库批次库位）.
/// </summary>
public class FifoBatchLockDto
{
    /// <summary>
    /// 台账主键.
    /// </summary>
    public string StockId { get; set; }

    /// <summary>
    /// 存货ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 仓库ID.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 货位ID.
    /// </summary>
    public string LocationId { get; set; }

    /// <summary>
    /// 货位编码.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 入库时间.
    /// </summary>
    public DateTime? InTime { get; set; }

    /// <summary>
    /// 可用数量（在库-占用）.
    /// </summary>
    public decimal AvailableQty { get; set; }

    /// <summary>
    /// 本次锁定数量.
    /// </summary>
    public decimal LockQty { get; set; }
}

/// <summary>
/// 库存变动结果（入库上架/出库扣减）.
/// </summary>
public class StockChangeOutput
{
    /// <summary>
    /// 台账主键.
    /// </summary>
    public string StockId { get; set; }

    /// <summary>
    /// 变动前在库数量.
    /// </summary>
    public decimal BeforeQty { get; set; }

    /// <summary>
    /// 变动后在库数量.
    /// </summary>
    public decimal AfterQty { get; set; }
}

#endregion

#region 仓库/货位档案（WarehouseArchiveService）

/// <summary>
/// 仓库档案查询条件.
/// </summary>
public class WarehouseQuery : PageInputBase
{
    /// <summary>
    /// 仓库编码（模糊）.
    /// </summary>
    public string RepoCode { get; set; }

    /// <summary>
    /// 仓库名称（模糊）.
    /// </summary>
    public string RepoName { get; set; }

    /// <summary>
    /// 货位管理：0否 1是，-1为全部.
    /// </summary>
    public int IsLocationManage { get; set; } = -1;
}

/// <summary>
/// 仓库档案新增/编辑入参.
/// </summary>
public class WarehouseInput
{
    /// <summary>
    /// 主键（为空新增，非空编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 仓库编码.
    /// </summary>
    public string RepoCode { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string RepoName { get; set; }

    /// <summary>
    /// 部门名称.
    /// </summary>
    public string DeptName { get; set; }

    /// <summary>
    /// 仓库地址.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// 负责人.
    /// </summary>
    public string ChargePerson { get; set; }

    /// <summary>
    /// 计价方式（如：全月平均法）.
    /// </summary>
    public string PricingMethod { get; set; }

    /// <summary>
    /// 是否货位管理：0否 1是.
    /// </summary>
    public int? IsLocationManage { get; set; }

    /// <summary>
    /// 仓库属性：普通仓/现场仓.
    /// </summary>
    public string RepoAttr { get; set; }

    /// <summary>
    /// 对应条形码.
    /// </summary>
    public string Barcode { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 货位档案查询条件.
/// </summary>
public class WarehouseLocationQuery : PageInputBase
{
    /// <summary>
    /// 仓库ID.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 货位编码（模糊）.
    /// </summary>
    public string LocationCode { get; set; }
}

/// <summary>
/// 货位档案新增/编辑入参（编码区-排-列-层，如A1-1-1）.
/// </summary>
public class WarehouseLocationInput
{
    /// <summary>
    /// 主键（为空新增，非空编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 仓库ID.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 货位编码（区-排-列-层，如A1-1-1；或紧凑格式A10101）.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 货位名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 区号.
    /// </summary>
    public string AreaNo { get; set; }

    /// <summary>
    /// 排号.
    /// </summary>
    public string RowNo { get; set; }

    /// <summary>
    /// 列号.
    /// </summary>
    public string ColumnNo { get; set; }

    /// <summary>
    /// 层号.
    /// </summary>
    public string LayerNo { get; set; }

    /// <summary>
    /// 最大体积（m³，校验用）.
    /// </summary>
    public decimal? MaxVolume { get; set; }

    /// <summary>
    /// 最大重量（kg，校验用）.
    /// </summary>
    public decimal? MaxWeight { get; set; }

    /// <summary>
    /// 条形码.
    /// </summary>
    public string Barcode { get; set; }

    /// <summary>
    /// 状态：1可用 0停用.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 货位档案列表输出.
/// </summary>
public class WarehouseLocationListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 货位编码.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 货位名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 所属仓库名称.
    /// </summary>
    public string RepoName { get; set; }

    /// <summary>
    /// 最大体积.
    /// </summary>
    public decimal? MaxVolume { get; set; }

    /// <summary>
    /// 最大重量.
    /// </summary>
    public decimal? MaxWeight { get; set; }

    /// <summary>
    /// 对应条形码.
    /// </summary>
    public string Barcode { get; set; }

    /// <summary>
    /// 状态：1可用 0停用.
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>
/// 按库位码扫码查询输出（PDA扫码落位，兼容货位编码/条形码）.
/// </summary>
public class LocationScanOutput
{
    /// <summary>
    /// 货位主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 货位编码.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 仓库ID.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 仓库编码.
    /// </summary>
    public string WarehouseCode { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 条形码.
    /// </summary>
    public string Barcode { get; set; }

    /// <summary>
    /// 状态：1可用 0停用.
    /// </summary>
    public int? Status { get; set; }
}

#endregion

#region 入库（InboundService）

/// <summary>
/// 入库记录查询条件.
/// </summary>
public class InRecordQuery : PageInputBase
{
    /// <summary>
    /// 入库类型：1采购入库 2产成品入库 3其他入库，-1为全部.
    /// </summary>
    public int InType { get; set; } = -1;

    /// <summary>
    /// 入库方式：1扫码流程卡入库 2直接入库，-1为全部.
    /// </summary>
    public int InMode { get; set; } = -1;

    /// <summary>
    /// 到货单号（模糊）.
    /// </summary>
    public string ArrivalNo { get; set; }

    /// <summary>
    /// 流程卡号（模糊）.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 存货关键字（编码/名称）.
    /// </summary>
    public string InventoryKeyword { get; set; }

    /// <summary>
    /// 入库日期-起.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 入库日期-止.
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 入库记录列表输出.
/// </summary>
public class InRecordListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 入库单号.
    /// </summary>
    public string InNo { get; set; }

    /// <summary>
    /// 入库类型：1采购入库 2产成品入库 3其他入库.
    /// </summary>
    public int? InType { get; set; }

    /// <summary>
    /// 入库方式：1扫码流程卡入库 2直接入库.
    /// </summary>
    public int? InMode { get; set; }

    /// <summary>
    /// 到货单号.
    /// </summary>
    public string ArrivalNo { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 库位.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 有无零头箱：0无 1有.
    /// </summary>
    public int? HasOddBox { get; set; }

    /// <summary>
    /// 操作人.
    /// </summary>
    public string OperatorName { get; set; }

    /// <summary>
    /// 入库时间.
    /// </summary>
    public DateTime? InTime { get; set; }

    /// <summary>
    /// 状态：1待审核 2已入库.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 用友同步状态：0待同步 1已同步 2同步失败.
    /// </summary>
    public int? SyncStatus { get; set; }
}

/// <summary>
/// 到货待入库明细输出（PDA采购入库扫码带出，逐项核对）.
/// </summary>
public class ArrivalPendingItemDto
{
    /// <summary>
    /// 到货明细ID.
    /// </summary>
    public string ArrivalDetailId { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 批号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 建议仓库（到货单行仓库名称）.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 已入库数量.
    /// </summary>
    public decimal InQty { get; set; }
}

/// <summary>
/// PDA采购入库确认入参（扫流程卡/到货单带出待入明细，扫库位码落位）.
/// </summary>
public class PurchaseInInput
{
    /// <summary>
    /// 到货单ID.
    /// </summary>
    public string ArrivalId { get; set; }

    /// <summary>
    /// 到货单号（非空时流程卡必填）.
    /// </summary>
    public string ArrivalNo { get; set; }

    /// <summary>
    /// 流程卡ID（扫码带出）.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 存货ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 入库数量（不可超过待入数量）.
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// 仓库ID.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 货位ID（扫库位码落位）.
    /// </summary>
    public string LocationId { get; set; }

    /// <summary>
    /// 批次号（为空自动建档生成）.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// PDA产成品入库确认入参（扫流程卡带出完工数量与报工核对，扫库位码落位）.
/// </summary>
public class ProductInInput
{
    /// <summary>
    /// 流程卡ID（扫码带出）.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 入库数量（与报工核对，不一致拦截）.
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// 仓库ID.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 货位ID（扫库位码落位）.
    /// </summary>
    public string LocationId { get; set; }

    /// <summary>
    /// 批次号（为空自动建档生成）.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 有无零头箱：0无 1有（有零头箱将在确认时一并入库）.
    /// </summary>
    public int HasOddBox { get; set; } = 0;

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// PDA其它入库确认入参（盘盈/退料/其他，允许无流程卡）.
/// </summary>
public class OtherInInput
{
    /// <summary>
    /// 流程卡ID（可空，无到货单来源才允许无流程卡）.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 存货ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 入库数量.
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// 仓库ID.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 货位ID（扫库位码落位）.
    /// </summary>
    public string LocationId { get; set; }

    /// <summary>
    /// 批次号（为空自动建档生成）.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 入库确认输出.
/// </summary>
public class InConfirmOutput
{
    /// <summary>
    /// 入库单号.
    /// </summary>
    public string InNo { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 库位.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 入库前在库数量.
    /// </summary>
    public decimal BeforeQty { get; set; }

    /// <summary>
    /// 入库后在库数量.
    /// </summary>
    public decimal AfterQty { get; set; }

    /// <summary>
    /// 提示信息.
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// 入库单管理查询条件.
/// </summary>
public class InOrderQuery : PageInputBase
{
    /// <summary>
    /// 入库单号（模糊）.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 入库类别：1采购入库 2产成品入库 3其他入库，-1为全部.
    /// </summary>
    public int InType { get; set; } = -1;

    /// <summary>
    /// 入库日期-起.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 入库日期-止.
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 入库单明细输出（入库单管理详情）.
/// </summary>
public class InOrderDetailOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 入库单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 入库类型.
    /// </summary>
    public int? InType { get; set; }

    /// <summary>
    /// 入库方式.
    /// </summary>
    public int? InMode { get; set; }

    /// <summary>
    /// 到货单号.
    /// </summary>
    public string ArrivalNo { get; set; }

    /// <summary>
    /// 供应商.
    /// </summary>
    public string SupplierName { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 状态：1草稿 2待审核 3已入库 4已作废.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 审核人.
    /// </summary>
    public string AuditUser { get; set; }

    /// <summary>
    /// 审核时间.
    /// </summary>
    public DateTime? AuditTime { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 明细行.
    /// </summary>
    public List<InOrderDetailItemDto> Details { get; set; } = new List<InOrderDetailItemDto>();
}

/// <summary>
/// 入库单明细行输出.
/// </summary>
public class InOrderDetailItemDto
{
    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 入库数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 货位编码.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

#endregion

#region 出库（OutboundService）

/// <summary>
/// 出库记录查询条件.
/// </summary>
public class OutRecordQuery : PageInputBase
{
    /// <summary>
    /// 出库单号（模糊）.
    /// </summary>
    public string OutNo { get; set; }

    /// <summary>
    /// 出库类型：1销售出库 2其他出库 3材料出库，-1为全部.
    /// </summary>
    public int OutType { get; set; } = -1;

    /// <summary>
    /// 流程卡号（模糊）.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 出库日期-起.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 出库日期-止.
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 出库记录列表输出.
/// </summary>
public class OutRecordListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 出库单号.
    /// </summary>
    public string OutNo { get; set; }

    /// <summary>
    /// 出库类型：1销售出库 2其他出库 3材料出库.
    /// </summary>
    public int? OutType { get; set; }

    /// <summary>
    /// 关联订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 批次.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 仓库.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 库位.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 先进先出校验：1已通过.
    /// </summary>
    public int? FifoCheck { get; set; }

    /// <summary>
    /// 操作人.
    /// </summary>
    public string OperatorName { get; set; }

    /// <summary>
    /// 出库时间.
    /// </summary>
    public DateTime? OutTime { get; set; }

    /// <summary>
    /// 用友同步状态.
    /// </summary>
    public int? SyncStatus { get; set; }
}

/// <summary>
/// PDA出库确认入参（销售出库/其他出库，先进先出自动锁定最早入库批次库位）.
/// </summary>
public class OutConfirmInput
{
    /// <summary>
    /// 出库类型：1销售出库 2其他出库.
    /// </summary>
    public int OutType { get; set; } = 1;

    /// <summary>
    /// 流程卡ID（扫码带出，销售出库勾稽校验用）.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 存货ID.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 出库数量.
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// 仓库ID（为空按存货所在仓库自动定位）.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 客户（销售出库）.
    /// </summary>
    public string Customer { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 出库扣减明细行.
/// </summary>
public class OutDeductItemDto
{
    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 库位.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 扣减数量.
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// 扣减前在库数量.
    /// </summary>
    public decimal BeforeQty { get; set; }

    /// <summary>
    /// 扣减后在库数量.
    /// </summary>
    public decimal AfterQty { get; set; }
}

/// <summary>
/// 出库确认输出.
/// </summary>
public class OutConfirmOutput
{
    /// <summary>
    /// 出库单号.
    /// </summary>
    public string OutNo { get; set; }

    /// <summary>
    /// 先进先出校验：1已通过.
    /// </summary>
    public int FifoPass { get; set; } = 1;

    /// <summary>
    /// 扣减明细（先进先出锁定最早批次库位）.
    /// </summary>
    public List<OutDeductItemDto> DeductItems { get; set; } = new List<OutDeductItemDto>();

    /// <summary>
    /// 提示信息.
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// 出库单管理查询条件.
/// </summary>
public class OutOrderQuery : PageInputBase
{
    /// <summary>
    /// 出库单号（模糊）.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 出库类别：1销售出库 2其他出库 3材料出库，-1为全部.
    /// </summary>
    public int OutType { get; set; } = -1;

    /// <summary>
    /// 出库日期-起.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 出库日期-止.
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 出库单明细输出（出库单管理详情）.
/// </summary>
public class OutOrderDetailOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 出库单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 出库类型.
    /// </summary>
    public int? OutType { get; set; }

    /// <summary>
    /// 客户（销售出库）.
    /// </summary>
    public string Customer { get; set; }

    /// <summary>
    /// 关联生产订单号.
    /// </summary>
    public string OrderNoRef { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 状态：1草稿 2待审核 3已出库 4已作废.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 审核人.
    /// </summary>
    public string AuditUser { get; set; }

    /// <summary>
    /// 审核时间.
    /// </summary>
    public DateTime? AuditTime { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 明细行.
    /// </summary>
    public List<OutOrderDetailItemDto> Details { get; set; } = new List<OutOrderDetailItemDto>();
}

/// <summary>
/// 出库单明细行输出.
/// </summary>
public class OutOrderDetailItemDto
{
    /// <summary>
    /// 存货编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 存货名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 规格型号.
    /// </summary>
    public string Spec { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 出库数量.
    /// </summary>
    public decimal? Qty { get; set; }

    /// <summary>
    /// 批次号.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 库位.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

#endregion

#region 领料（PickingService）

/// <summary>
/// 领料记录查询条件.
/// </summary>
public class PickingQuery : PageInputBase
{
    /// <summary>
    /// 领料单号（模糊）.
    /// </summary>
    public string PickingNo { get; set; }

    /// <summary>
    /// 生产订单号（模糊）.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 流程卡号（模糊）.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 领料日期-起.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 领料日期-止.
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 领料记录列表输出.
/// </summary>
public class PickingListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 领料单号.
    /// </summary>
    public string PickingNo { get; set; }

    /// <summary>
    /// 生产订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 工序.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 物料编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 物料名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 应发数量.
    /// </summary>
    public decimal? ShouldQty { get; set; }

    /// <summary>
    /// 实发数量.
    /// </summary>
    public decimal? ActualQty { get; set; }

    /// <summary>
    /// 库位（先进先出取最早批次库位）.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 批次（最早入库批次）.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 领料人.
    /// </summary>
    public string PickerName { get; set; }

    /// <summary>
    /// 领料时间.
    /// </summary>
    public DateTime? PickTime { get; set; }

    /// <summary>
    /// 状态：1待审核 2已审核 3已作废.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 用友同步状态.
    /// </summary>
    public int? SyncStatus { get; set; }
}

/// <summary>
/// 领料记录详情输出（含自动生成的材料出库记录关联）.
/// </summary>
public class PickingDetailOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 领料单号.
    /// </summary>
    public string PickingNo { get; set; }

    /// <summary>
    /// 生产订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string FlowCardNo { get; set; }

    /// <summary>
    /// 工序.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 物料编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 物料名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 应发数量.
    /// </summary>
    public decimal? ShouldQty { get; set; }

    /// <summary>
    /// 实发数量.
    /// </summary>
    public decimal? ActualQty { get; set; }

    /// <summary>
    /// 仓库名称.
    /// </summary>
    public string WarehouseName { get; set; }

    /// <summary>
    /// 库位.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 批次.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 领料人.
    /// </summary>
    public string PickerName { get; set; }

    /// <summary>
    /// 领料时间.
    /// </summary>
    public DateTime? PickTime { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 关联材料出库记录ID（自动生成）.
    /// </summary>
    public string OutStockId { get; set; }

    /// <summary>
    /// 关联材料出库单号.
    /// </summary>
    public string OutStockNo { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 领料前齐套明细行（子件需领量/可领量）.
/// </summary>
public class PickKittingItemDto
{
    /// <summary>
    /// 子件存货编码.
    /// </summary>
    public string ChildInventoryCode { get; set; }

    /// <summary>
    /// 子件存货名称.
    /// </summary>
    public string ChildInventoryName { get; set; }

    /// <summary>
    /// 单位.
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// 需领数量.
    /// </summary>
    public decimal RequireQty { get; set; }

    /// <summary>
    /// 可领量（在库-占用）.
    /// </summary>
    public decimal AvailableQty { get; set; }

    /// <summary>
    /// 缺料数量.
    /// </summary>
    public decimal LackQty { get; set; }

    /// <summary>
    /// 是否满足：1是 0否.
    /// </summary>
    public int Satisfied { get; set; }
}

/// <summary>
/// PDA领料前校验上下文输出（前道工序完成校验 + 齐套校验 + FIFO提示）.
/// </summary>
public class PickContextOutput
{
    /// <summary>
    /// 流程卡号.
    /// </summary>
    public string CardNo { get; set; }

    /// <summary>
    /// 生产订单号.
    /// </summary>
    public string OrderNo { get; set; }

    /// <summary>
    /// 本工序.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 是否首道工序：1是 0否.
    /// </summary>
    public int IsFirstProcess { get; set; }

    /// <summary>
    /// 前道工序名称（首道工序时为「来料检验」）.
    /// </summary>
    public string PrevProcessName { get; set; }

    /// <summary>
    /// 前道校验结果文本（如：已完成 100% / 已通过来料检验）.
    /// </summary>
    public string PrevProcessText { get; set; }

    /// <summary>
    /// 前道校验是否通过：1是 0否.
    /// </summary>
    public int PrevPass { get; set; }

    /// <summary>
    /// 齐套是否通过：1是 0否.
    /// </summary>
    public int KittingPass { get; set; }

    /// <summary>
    /// 齐套明细.
    /// </summary>
    public List<PickKittingItemDto> Items { get; set; } = new List<PickKittingItemDto>();

    /// <summary>
    /// 提示信息.
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// PDA领料确认入参（扫员工码+流程卡，FIFO取最早批次库位）.
/// </summary>
public class PdaPickInput
{
    /// <summary>
    /// 流程卡ID（扫码带出）.
    /// </summary>
    public string FlowCardId { get; set; }

    /// <summary>
    /// 工序ID（本工序）.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 物料ID（子件存货ID）.
    /// </summary>
    public string InventoryId { get; set; }

    /// <summary>
    /// 物料编码.
    /// </summary>
    public string InventoryCode { get; set; }

    /// <summary>
    /// 物料名称.
    /// </summary>
    public string InventoryName { get; set; }

    /// <summary>
    /// 领料数量（实发）.
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// 仓库ID（为空按FIFO最早批次所在仓库）.
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    /// 领料人（为空取当前用户）.
    /// </summary>
    public string PickerName { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// PDA领料确认输出（领料记录+自动生成材料出库记录）.
/// </summary>
public class PdaPickOutput
{
    /// <summary>
    /// 领料单号.
    /// </summary>
    public string PickingNo { get; set; }

    /// <summary>
    /// 自动生成的材料出库单号.
    /// </summary>
    public string OutNo { get; set; }

    /// <summary>
    /// 批次号（最早入库批次）.
    /// </summary>
    public string BatchNo { get; set; }

    /// <summary>
    /// 库位（先进先出取最早批次库位）.
    /// </summary>
    public string LocationCode { get; set; }

    /// <summary>
    /// 领料前在库数量.
    /// </summary>
    public decimal BeforeQty { get; set; }

    /// <summary>
    /// 领料后在库数量.
    /// </summary>
    public decimal AfterQty { get; set; }

    /// <summary>
    /// 提示信息.
    /// </summary>
    public string Message { get; set; }
}

#endregion
