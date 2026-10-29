using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 领料服务接口.
    /// PDA领料：①前道工序完成校验（首道工序校验来料检验完成，后续工序校验上一工序报工完成）；
    /// ②先进先出取最早批次库位；③齐套校验（按绑定子件×数量检查可领量=在库-占用）；
    /// 领料成功自动生成「领料记录」（主数据）与「材料出库记录」（关联不重复录入）.
    /// </summary>
    public interface IPickingService
    {
        /// <summary>
        /// 领料记录列表（按领料单号/订单/流程卡/日期筛选）.
        /// </summary>
        Task<dynamic> GetPickingList([FromBody] PickingQuery input);

        /// <summary>
        /// 领料记录详情（含自动生成的材料出库记录关联）.
        /// </summary>
        Task<dynamic> GetPickingDetail(string id);

        /// <summary>
        /// PDA领料前校验上下文（前道工序完成校验 + 齐套校验 + 提示）.
        /// </summary>
        Task<dynamic> GetPickContext(string flowCardId, string processId);

        /// <summary>
        /// PDA领料确认（校验通过后FIFO取最早批次库位扣减；自动生成领料记录+材料出库记录）.
        /// </summary>
        Task<dynamic> PdaPick([FromBody] PdaPickInput input);
    }
}
