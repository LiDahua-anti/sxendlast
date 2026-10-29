using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 用友U8同步服务接口（模拟对接：日志查询/手动触发/失败重试 + 出入库审核后自动推送现有量变更）.
    /// MES→用友：采购/产成品入库单、材料/销售出库单、生产领料单、现有量推送；
    /// 用友→MES：生产订单、存货档案、仓库档案等基础数据。
    /// </summary>
    public interface IYonyouSyncService
    {
        /// <summary>
        /// 同步日志列表（按单据类型/单据号/状态/方向/触发方式/日期筛选）.
        /// </summary>
        Task<dynamic> GetSyncLogList([FromBody] YonyouSyncLogQuery input);

        /// <summary>
        /// 同步日志详情（含请求/响应报文）.
        /// </summary>
        Task<dynamic> GetSyncLogDetail(string id);

        /// <summary>
        /// 手动触发基础数据拉取（模拟：记录请求/响应日志，用友→MES）.
        /// </summary>
        Task<dynamic> PullFromYonyou([FromBody] YonyouSyncPullInput input);

        /// <summary>
        /// 手动触发现有量推送（模拟：汇总实时库存生成推送报文，MES→用友）.
        /// </summary>
        Task<dynamic> PushStockQty();

        /// <summary>
        /// 失败日志重试（重试次数+1，模拟重试成功并回填凭证号）.
        /// </summary>
        Task<dynamic> RetrySync([FromBody] YonyouRetryInput input);

        /// <summary>
        /// 出入库审核后自动推送现有量变更（模拟：写同步日志，失败不阻断业务）.
        /// </summary>
        [NonAction]
        Task WriteAutoPushLogAsync(string bizType, string bizNo, string remark = null);
    }
}
