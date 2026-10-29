using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Interfaces.Custom;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 齐套性检查（技术管理）.
    /// 计算规则：需用数量 = 单个用量 × 投产数量 × (1 + 损耗率)；
    /// 可领量 = 在库 − 占用；缺口数量 = max(0, 需用 − 可领)；
    /// 输出缺料清单与齐套率（满足项数 / 总项数）。
    /// 两种模式：①母件存货 + 投产数量；②生产订单（取订单产品与计划数量）。
    /// </summary>
    [ApiDescriptionSettings(Tag = "Kitting", Name = "", Order = 206)]
    [Route("api/[controller]")]
    public class KittingService : IKittingService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 存货主档仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryMasterEntity> _inventoryRepository;

        /// <summary>
        /// 存货BOM子件明细仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryBomEntity> _bomRepository;

        /// <summary>
        /// 库存台账仓储.
        /// </summary>
        private readonly ISqlSugarRepository<InventoryStockEntity> _stockRepository;

        /// <summary>
        /// 生产订单仓储（模式二）.
        /// </summary>
        private readonly ISqlSugarRepository<ProductionOrderEntity> _productionOrderRepository;

        /// <summary>
        /// 初始化一个<see cref="KittingService"/>类型的新实例.
        /// </summary>
        public KittingService(
            ISqlSugarRepository<InventoryMasterEntity> inventoryRepository,
            ISqlSugarRepository<InventoryBomEntity> bomRepository,
            ISqlSugarRepository<InventoryStockEntity> stockRepository,
            ISqlSugarRepository<ProductionOrderEntity> productionOrderRepository)
        {
            _inventoryRepository = inventoryRepository;
            _bomRepository = bomRepository;
            _stockRepository = stockRepository;
            _productionOrderRepository = productionOrderRepository;
        }

        /// <summary>
        /// 齐套性检查计算（需用数量=单个用量×投产数量×(1+损耗率)；可领量=在库-占用；输出缺料清单）.
        /// 两种模式：①母件存货+投产数量；②生产订单（取订单产品与计划数量）.
        /// </summary>
        /// <param name="input">齐套检查入参.</param>
        /// <returns>齐套检查结果（含缺料明细与齐套率）.</returns>
        [HttpPost("CheckKitting")]
        public async Task<dynamic> CheckKitting([FromBody] KittingCheckInput input)
        {
            InventoryMasterEntity parent;
            decimal planQty;

            if (!input.InventoryId.IsNullOrEmpty())
            {
                // 模式一：母件存货 + 投产数量
                parent = await _inventoryRepository.GetFirstAsync(m => m.Id == input.InventoryId && m.DeleteMark == null);
                if (parent == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                planQty = input.PlanQty ?? 0;
            }
            else if (!input.OrderId.IsNullOrEmpty())
            {
                // 模式二：生产订单 → 取订单产品与计划数量
                var order = await _productionOrderRepository.GetFirstAsync(m => m.Id == input.OrderId && m.DeleteMark == null);
                if (order == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                planQty = order.PlanQty ?? 0;
                parent = await _inventoryRepository.GetFirstAsync(m => m.InventoryName == order.ProductName && m.DeleteMark == null);
                if (parent == null)
                {
                    throw Oops.Oh(ErrorCode.COM1018, $"未找到与订单产品「{order.ProductName}」匹配的存货档案，无法齐套检查");
                }
            }
            else
            {
                throw Oops.Oh(ErrorCode.COM1018, "请选择母件存货或生产订单");
            }

            if (planQty <= 0)
            {
                throw Oops.Oh(ErrorCode.COM1018, "投产数量必须大于0");
            }

            // BOM 子件明细
            var bomItems = await _bomRepository.AsQueryable()
                .Where(m => m.ParentInventoryId == parent.Id && m.DeleteMark == null)
                .OrderBy(m => m.PositionNo, OrderByType.Asc)
                .ToListAsync();

            var childIds = bomItems.Select(m => m.ChildInventoryId).Distinct().ToList();
            var children = new List<InventoryMasterEntity>();
            var stockRows = new List<InventoryStockEntity>();
            if (childIds.Any())
            {
                children = await _inventoryRepository.AsQueryable()
                    .Where(m => childIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync();

                // 可用量 = 在库 − 占用（多批次汇总）
                stockRows = await _stockRepository.AsQueryable()
                    .Where(m => childIds.Contains(m.InventoryId) && m.DeleteMark == null)
                    .ToListAsync();
            }

            var output = new KittingCheckOutput
            {
                InventoryCode = parent.InventoryCode,
                InventoryName = parent.InventoryName,
                PlanQty = planQty
            };

            var seqNo = 1;
            var satisfiedCount = 0;
            foreach (var bom in bomItems)
            {
                var child = children.FirstOrDefault(m => m.Id == bom.ChildInventoryId);
                var quantity = bom.Quantity ?? 0;
                var lossRate = bom.LossRate ?? 0;

                // 需用数量 = 单个用量 × 投产数量 × (1 + 损耗率)
                var requireQty = Math.Round(quantity * planQty * (1 + lossRate / 100), 2);

                // 可领量 = 在库 − 占用（该子件全部批次汇总）
                var availableQty = stockRows.Where(m => m.InventoryId == bom.ChildInventoryId)
                    .Sum(m => (m.StockQty ?? 0) - (m.OccupiedQty ?? 0));
                if (availableQty < 0)
                {
                    availableQty = 0;
                }

                var lackQty = Math.Max(0, Math.Round(requireQty - availableQty, 2));
                var satisfied = lackQty > 0 ? 0 : 1;
                if (satisfied == 1)
                {
                    satisfiedCount++;
                }

                output.Items.Add(new KittingItemDto
                {
                    SeqNo = seqNo++,
                    ChildInventoryCode = child?.InventoryCode ?? bom.ChildInventoryCode,
                    ChildInventoryName = child?.InventoryName,
                    Spec = child?.Spec,
                    Unit = child?.Unit,
                    Quantity = quantity,
                    LossRate = lossRate,
                    RequireQty = requireQty,
                    AvailableQty = Math.Round(availableQty, 2),
                    LackQty = lackQty,
                    Satisfied = satisfied
                });
            }

            // 齐套率 = 满足项数 / 总项数 × 100
            output.LackCount = output.Items.Count(m => m.Satisfied == 0);
            output.KittingRate = output.Items.Any()
                ? Math.Round((decimal)satisfiedCount / output.Items.Count * 100, 2)
                : 100;

            return output;
        }
    }
}
