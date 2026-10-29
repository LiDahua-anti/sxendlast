using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 生产订单管理（生产计划管理）服务接口.
    /// </summary>
    public interface IProductionOrderService
    {
        /// <summary>
        /// 获取生产订单列表（分页）.
        /// </summary>
        Task<dynamic> GetOrderList([FromBody] OrderQuery input);

        /// <summary>
        /// 获取生产订单统计卡片（待排产/已排产/已发布生产中/已完工）.
        /// </summary>
        Task<dynamic> GetOrderStatistics();

        /// <summary>
        /// 获取生产订单详情.
        /// </summary>
        Task<dynamic> GetOrderById(string id);

        /// <summary>
        /// 新增/编辑生产订单（手工新增来源标记2）.
        /// </summary>
        Task CruOrder([FromBody] OrderCruInput input);

        /// <summary>
        /// 删除生产订单（清理排产、流程卡解绑保留、领料/报工标记作废保留、不回冲库存）.
        /// </summary>
        Task DeleteOrder(string id);

        /// <summary>
        /// 用友同步拉取生产订单（模拟：生成本批订单并写同步日志）.
        /// </summary>
        Task<dynamic> SyncOrdersFromYonyou([FromBody] SyncPullInput input);

        /// <summary>
        /// 保存排产（强制校验流程卡必选；工序×设备×生产个数整体覆盖；写审计日志）.
        /// </summary>
        Task SaveSchedule([FromBody] ScheduleSaveInput input);

        /// <summary>
        /// 获取订单排产方案（含工序设备分配明细）.
        /// </summary>
        Task<dynamic> GetScheduleByOrder(string orderId);

        /// <summary>
        /// 发布生产订单（校验①排产已绑流程卡 ②来料检验/前道工序完成，不通过拦截）.
        /// </summary>
        Task<dynamic> PublishOrder(string id);

        /// <summary>
        /// 生产进度列表（订单维度关键信息，含进度百分比与当前工序）.
        /// </summary>
        Task<dynamic> GetProgressList([FromBody] ProgressQuery input);

        /// <summary>
        /// 生产进度详情（工序流转聚合：完成数量/设备/报工人/时间）.
        /// </summary>
        Task<dynamic> GetProgressDetail(string orderId);
    }
}
