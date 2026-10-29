using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 库存台账服务接口（核心：库存查询/流水 + 供其余服务注入的库存变动方法）.
    /// 可用量=在库-占用；出库先进先出（FIFO）按 in_time 排序锁定最早批次库位；
    /// 入库统一「批次建档 + 货位归位」；出库扣减超量拦截.
    /// </summary>
    public interface IStockService
    {
        /// <summary>
        /// 库存台账列表（MES实时库存，按仓库/物料/库存状态筛选）.
        /// </summary>
        Task<dynamic> GetStockList([FromBody] StockQuery input);

        /// <summary>
        /// 库存查询统计卡片（在库存货种类/库存总量/可用库存合计/低于安全库存）.
        /// </summary>
        Task<dynamic> GetStockStatistics();

        /// <summary>
        /// 库存流水列表（按存货/业务类型/方向/日期筛选）.
        /// </summary>
        Task<dynamic> GetStockFlowList([FromBody] StockFlowQuery input);

        /// <summary>
        /// 先进先出锁定最早入库批次库位（同存货多批次按 in_time 升序；可用量不足返回空列表）.
        /// </summary>
        [NonAction]
        Task<List<FifoBatchLockDto>> FifoLock(string inventoryId, string warehouseId, decimal qty);

        /// <summary>
        /// 入库上架（批次建档+货位归位）：同存货同批次同批次库位累加，否则新建台账行；写库存流水.
        /// </summary>
        [NonAction]
        Task<StockChangeOutput> StockInAsync(StockInEntity data, decimal qty);

        /// <summary>
        /// 出库扣减（先进先出按批次扣减；可用量不足抛错拦截）；写库存流水；返回扣减明细.
        /// </summary>
        [NonAction]
        Task<List<OutDeductItemDto>> StockOutAsync(string inventoryId, string warehouseId, decimal qty, int bizType, string bizId, string bizNo, string orderNo, string flowCardNo, string operatorName, string remark);

        /// <summary>
        /// 批次建档号生成（同存货多批次独立建档，如 20261014-001）.
        /// </summary>
        [NonAction]
        Task<string> BuildBatchNo(string inventoryCode);

        /// <summary>
        /// 占用（按先进先出批次增加占用数量，可用量不足返回false）.
        /// </summary>
        [NonAction]
        Task<bool> OccupyAsync(string inventoryId, decimal qty);

        /// <summary>
        /// 释放占用（按批次减少占用数量）.
        /// </summary>
        [NonAction]
        Task<bool> ReleaseAsync(string inventoryId, decimal qty);
    }
}
