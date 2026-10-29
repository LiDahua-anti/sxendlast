using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 流程卡打印（合并/拆分/重印）服务接口.
    /// </summary>
    public interface IFlowCardService
    {
        /// <summary>
        /// 获取流程卡列表（分页，含关联旧卡）.
        /// </summary>
        Task<dynamic> GetFlowCardList([FromBody] FlowCardQuery input);

        /// <summary>
        /// 打印流程卡（原卡打印/合并多旧→一新/拆分一旧→多新/重印关联原卡，写追溯关系）.
        /// </summary>
        Task<dynamic> PrintFlowCard([FromBody] FlowCardPrintInput input);

        /// <summary>
        /// 流程卡关联追溯（当前卡来源旧卡，可上溯/下钻）.
        /// </summary>
        Task<dynamic> GetFlowCardRelation(string id);

        /// <summary>
        /// 按流程卡号扫码查询（供PDA：卡信息+工序流转列表）.
        /// </summary>
        Task<dynamic> GetFlowCardByCardNo(string cardNo);

        /// <summary>
        /// 初始化流程卡工序流转（按订单排产明细生成各工序流转，PDA报工前调用）.
        /// </summary>
        Task<dynamic> InitFlowCardProcesses(string flowCardId);
    }
}
