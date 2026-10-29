using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Interfaces.Custom;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 用友U8同步服务（系统对接，模拟实现）.
    /// 不直连U8 API：MES/WMS 出入库/领料审核后自动写「现有量推送」同步日志；
    /// 支持手动触发基础数据拉取（用友→MES）、现有量推送（MES→用友）、失败重试（重试次数+1）。
    /// </summary>
    [ApiDescriptionSettings(Tag = "YonyouSync", Name = "", Order = 218)]
    [Route("api/[controller]")]
    public class YonyouSyncService : IYonyouSyncService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 同步日志仓储.
        /// </summary>
        private readonly ISqlSugarRepository<YonyouSyncLogEntity> _repository;

        /// <summary>
        /// 用户管理器.
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="YonyouSyncService"/>类型的新实例.
        /// </summary>
        public YonyouSyncService(
            ISqlSugarRepository<YonyouSyncLogEntity> repository,
            IUserManager userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

        /// <summary>
        /// 同步日志列表（按单据类型/单据号/状态/方向/触发方式/日期筛选）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetSyncLogList")]
        public async Task<dynamic> GetSyncLogList([FromBody] YonyouSyncLogQuery input)
        {
            var whereLambda = LinqExpression.And<YonyouSyncLogEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);
            if (!input.BizType.IsNullOrEmpty()) whereLambda = whereLambda.And(x => x.BizType == input.BizType);
            if (!input.BizNo.IsNullOrEmpty()) whereLambda = whereLambda.And(x => x.BizNo.Contains(input.BizNo));
            if (input.Status != null) whereLambda = whereLambda.And(x => x.Status == input.Status);
            if (input.Direction != null) whereLambda = whereLambda.And(x => x.Direction == input.Direction);
            if (input.TriggerType != null) whereLambda = whereLambda.And(x => x.TriggerType == input.TriggerType);
            if (input.SyncTime != null && input.SyncTime.Any())
            {
                var start = input.SyncTime.First();
                var end = input.SyncTime.Last();
                whereLambda = whereLambda.And(x => x.SyncTime >= start && x.SyncTime <= end);
            }

            var list = await _repository.AsQueryable()
                .Where(whereLambda)
                .OrderByDescending(x => x.SyncTime)
                .Select(x => new YonyouSyncLogListDto
                {
                    Id = x.Id,
                    BizType = x.BizType,
                    BizNo = x.BizNo,
                    Direction = x.Direction,
                    VoucherNo = x.VoucherNo,
                    Status = x.Status,
                    FailReason = x.FailReason,
                    SyncTime = x.SyncTime,
                    CostMs = x.CostMs,
                    TriggerType = x.TriggerType,
                    RetryCount = x.RetryCount,
                    OperatorName = x.OperatorName,
                    Remark = x.Remark
                })
                .ToPagedListAsync(input.currentPage, input.pageSize);

            if (list != null && list.list != null)
            {
                foreach (var item in list.list)
                {
                    item.DirectionText = item.Direction == 2 ? "用友→MES" : "MES→用友";
                    item.StatusText = item.Status == 2 ? "失败" : "成功";
                    item.TriggerTypeText = item.TriggerType == 2 ? "手动" : "自动";
                }
            }

            return PageResult<YonyouSyncLogListDto>.SqlSugarPageResult(list);
        }

        /// <summary>
        /// 同步日志详情（含请求/响应报文）.
        /// </summary>
        /// <param name="id">日志ID.</param>
        /// <returns>日志详情.</returns>
        [HttpGet("GetSyncLogDetail/{id}")]
        public async Task<dynamic> GetSyncLogDetail(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "日志ID不能为空");
            }

            var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var dto = entity.Adapt<YonyouSyncLogDetailDto>();
            dto.DirectionText = dto.Direction == 2 ? "用友→MES" : "MES→用友";
            dto.StatusText = dto.Status == 2 ? "失败" : "成功";
            dto.TriggerTypeText = dto.TriggerType == 2 ? "手动" : "自动";
            return dto;
        }

        /// <summary>
        /// 手动触发基础数据拉取（模拟：记录请求/响应日志，用友→MES）.
        /// </summary>
        /// <param name="input">拉取入参（数据类型：生产订单/存货档案/仓库档案）.</param>
        /// <returns>触发结果.</returns>
        [HttpPost("PullFromYonyou")]
        public async Task<dynamic> PullFromYonyou([FromBody] YonyouSyncPullInput input)
        {
            var bizType = input?.BizType;
            if (bizType.IsNullOrEmpty())
            {
                bizType = "生产订单";
            }

            if (bizType != "生产订单" && bizType != "存货档案" && bizType != "仓库档案")
            {
                throw Oops.Oh(ErrorCode.COM1018, "仅支持拉取：生产订单/存货档案/仓库档案");
            }

            var sw = Stopwatch.StartNew();

            // 模拟响应数据：取本地对应表最近数据作为「用友拉取结果」
            var rows = new List<object>();
            if (bizType == "生产订单")
            {
                var list = await _repository.AsSugarClient().Queryable<ProductionOrderEntity>()
                    .Where(m => m.DeleteMark == null).OrderBy(m => m.CreatorTime, OrderByType.Desc).Take(10).ToListAsync();
                rows = list.Select(m => (object)new { orderNo = m.OrderNo, productName = m.ProductName, planQty = m.PlanQty, status = m.Status }).ToList();
            }
            else if (bizType == "存货档案")
            {
                var list = await _repository.AsSugarClient().Queryable<InventoryMasterEntity>()
                    .Where(m => m.DeleteMark == null).OrderBy(m => m.CreatorTime, OrderByType.Desc).Take(10).ToListAsync();
                rows = list.Select(m => (object)new { inventoryCode = m.InventoryCode, inventoryName = m.InventoryName, spec = m.Spec, unit = m.Unit }).ToList();
            }
            else
            {
                var list = await _repository.AsSugarClient().Queryable<WarehouseEntity>()
                    .Where(m => m.DeleteMark == null).OrderBy(m => m.CreatorTime, OrderByType.Desc).Take(10).ToListAsync();
                rows = list.Select(m => (object)new { repoCode = m.RepoCode, repoName = m.RepoName, locationCount = m.LocationCount }).ToList();
            }

            sw.Stop();
            var costMs = (int)Math.Max(sw.ElapsedMilliseconds, 86);

            var operatorName = _userManager.GetUserName(_userManager.UserId);
            var request = new { api = "/u8/api/baseData/pull", bizType, pageSize = 10, operatorName }.ToJsonString();
            var response = new { code = "0", message = "success", count = rows.Count, data = rows }.ToJsonString();

            var logId = await WriteLogAsync(bizType, null, 2, null, 1, null, costMs, 2, 0, request, response, $"手动拉取{bizType}");

            return new YonyouSyncTriggerOutput
            {
                LogId = logId,
                Message = $"已从用友拉取「{bizType}」{rows.Count} 条（模拟），同步日志已记录"
            };
        }

        /// <summary>
        /// 手动触发现有量推送（模拟：汇总实时库存生成推送报文，MES→用友）.
        /// </summary>
        /// <returns>触发结果.</returns>
        [HttpPost("PushStockQty")]
        public async Task<dynamic> PushStockQty()
        {
            var sw = Stopwatch.StartNew();

            var stocks = await _repository.AsSugarClient().Queryable<InventoryStockEntity>()
                .Where(m => m.DeleteMark == null)
                .ToListAsync();
            var items = stocks
                .GroupBy(m => new { m.InventoryCode, m.InventoryName })
                .Select(g => new
                {
                    inventoryCode = g.Key.InventoryCode,
                    inventoryName = g.Key.InventoryName,
                    qty = g.Sum(m => m.StockQty ?? 0)
                })
                .ToList();

            sw.Stop();
            var costMs = (int)Math.Max(sw.ElapsedMilliseconds, 95);

            var voucherNo = "PZ-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var request = new { api = "/u8/api/stockQty/push", itemCount = items.Count, items }.ToJsonString();
            var response = new { code = "0", message = "success", voucherNo }.ToJsonString();

            var logId = await WriteLogAsync("现有量推送", null, 1, voucherNo, 1, null, costMs, 2, 0, request, response, "手动触发库存现有量推送");

            return new YonyouSyncTriggerOutput
            {
                LogId = logId,
                Message = $"现有量推送成功（模拟），凭证号 {voucherNo}，共 {items.Count} 项存货"
            };
        }

        /// <summary>
        /// 失败日志重试（重试次数+1，模拟重试成功并回填凭证号）.
        /// </summary>
        /// <param name="input">重试入参.</param>
        /// <returns>重试结果.</returns>
        [HttpPost("RetrySync")]
        public async Task<dynamic> RetrySync([FromBody] YonyouRetryInput input)
        {
            if (input == null || input.LogId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "日志ID不能为空");
            }

            var log = await _repository.GetFirstAsync(x => x.Id == input.LogId && x.DeleteMark == null);
            if (log == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            if (log.Status != 2)
            {
                throw Oops.Oh(ErrorCode.COM1018, "仅失败的同步记录可重试");
            }

            var voucherNo = "PZ-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            log.RetryCount = (log.RetryCount ?? 0) + 1;
            log.Status = 1;
            log.VoucherNo = voucherNo;
            log.SyncTime = DateTime.Now;
            log.CostMs = 88;
            log.ResponseContent = new { code = "0", message = "success", voucherNo, retry = log.RetryCount }.ToJsonString();
            log.Remark = (log.Remark ?? string.Empty) + $"；第{log.RetryCount}次重试成功(模拟)";

            await _repository.AsSugarClient().Updateable(log)
                .UpdateColumns(it => new { it.RetryCount, it.Status, it.VoucherNo, it.SyncTime, it.CostMs, it.ResponseContent, it.Remark })
                .CallEntityMethod(m => m.LastModify())
                .ExecuteCommandAsync();

            return new YonyouSyncTriggerOutput
            {
                LogId = log.Id,
                Message = $"重试成功（模拟），凭证号 {voucherNo}，累计重试 {log.RetryCount} 次"
            };
        }

        /// <summary>
        /// 出入库审核后自动推送现有量变更（模拟：写同步日志，失败不阻断业务）.
        /// </summary>
        /// <param name="bizType">单据类型（采购入库单/产成品入库单/材料出库单/销售出库单/生产领料单等）.</param>
        /// <param name="bizNo">单据号.</param>
        /// <param name="remark">备注.</param>
        /// <returns></returns>
        [NonAction]
        public async Task WriteAutoPushLogAsync(string bizType, string bizNo, string remark = null)
        {
            try
            {
                var voucherNo = "PZ-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var request = new { api = "/u8/api/inventory/push", bizType, bizNo }.ToJsonString();
                var response = new { code = "0", message = "success", voucherNo }.ToJsonString();
                await WriteLogAsync(bizType, bizNo, 1, voucherNo, 1, null, 120, 1, 0, request, response, remark);
            }
            catch
            {
                // 模拟对接失败不阻断业务
            }
        }

        /// <summary>
        /// 写同步日志.
        /// </summary>
        private async Task<string> WriteLogAsync(string bizType, string bizNo, int direction, string voucherNo, int status, string failReason,
            int costMs, int triggerType, int retryCount, string requestContent, string responseContent, string remark)
        {
            var entity = new YonyouSyncLogEntity
            {
                BizType = bizType,
                BizNo = bizNo,
                Direction = direction,
                VoucherNo = voucherNo,
                Status = status,
                FailReason = failReason,
                SyncTime = DateTime.Now,
                CostMs = costMs,
                TriggerType = triggerType,
                RetryCount = retryCount,
                RequestContent = requestContent,
                ResponseContent = responseContent,
                OperatorName = _userManager.GetUserName(_userManager.UserId),
                Remark = remark
            };

            await _repository.AsSugarClient().Insertable(entity)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            return entity.Id;
        }
    }
}
