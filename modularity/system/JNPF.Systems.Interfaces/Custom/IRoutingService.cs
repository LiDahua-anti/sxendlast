using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 工艺路线管理（技术管理）服务接口.
    /// </summary>
    public interface IRoutingService
    {
        /// <summary>
        /// 获取工艺路线列表（分页）.
        /// </summary>
        Task<dynamic> GetRoutingList([FromBody] RoutingQuery input);

        /// <summary>
        /// 获取工艺路线详情（含工序顺序明细与检查项目）.
        /// </summary>
        Task<dynamic> GetRoutingById(string id);

        /// <summary>
        /// 新增/编辑工艺路线（工序顺序明细整体覆盖保存，自动汇总工序数与总标准工时）.
        /// </summary>
        Task CruRouting([FromBody] RoutingCruInput input);

        /// <summary>
        /// 删除工艺路线（级联软删除工序明细）.
        /// </summary>
        Task DeleteRouting(string id);

        /// <summary>
        /// 复制工艺路线（副本编号自动追加"-COPY"，工序明细一并复制）.
        /// </summary>
        Task CopyRouting(string id);

        /// <summary>
        /// 批量生效/停用工艺路线.
        /// </summary>
        Task BatchChangeRoutingStatus([FromBody] RoutingBatchStatusInput input);

        /// <summary>
        /// 工艺路线下拉（供排产选择，仅生效路线）.
        /// </summary>
        Task<dynamic> GetRoutingSelected(string keyword);
    }
}
