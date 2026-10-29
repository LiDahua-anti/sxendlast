using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 出库服务接口.
    /// PDA销售出库/其他出库确认（先进先出自动锁定最早入库批次库位）；
    /// 「材料出库」由PDA领料记录自动生成关联，本服务不重复录入；出库单管理.
    /// </summary>
    public interface IOutboundService
    {
        /// <summary>
        /// 出库记录列表（按单号/类型/流程卡/日期筛选）.
        /// </summary>
        Task<dynamic> GetOutRecordList([FromBody] OutRecordQuery input);

        /// <summary>
        /// 出库记录详情.
        /// </summary>
        Task<dynamic> GetOutRecordDetail(string id);

        /// <summary>
        /// 出库记录删除（软删）.
        /// </summary>
        Task DeleteOutRecord(string id);

        /// <summary>
        /// PDA出库确认（销售出库/其他出库；先进先出自动锁定最早批次库位；销售出库勾稽校验不超流程卡定量）.
        /// </summary>
        Task<dynamic> OutConfirm([FromBody] OutConfirmInput input);

        /// <summary>
        /// 出库单管理列表（分销售出库/材料出库/其他出库）.
        /// </summary>
        Task<dynamic> GetOutOrderList([FromBody] OutOrderQuery input);

        /// <summary>
        /// 出库单明细（单据头+明细）.
        /// </summary>
        Task<dynamic> GetOutOrderDetail(string id);
    }
}
