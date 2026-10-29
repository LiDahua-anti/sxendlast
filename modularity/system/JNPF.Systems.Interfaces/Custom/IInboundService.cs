using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 入库服务接口.
    /// PDA采购入库（扫流程卡/到货单带出待入库明细逐项核对、扫库位码落位、入库数量≤待入数量、
    /// 到货单来源校验流程卡必填）、PDA产成品入库（扫流程卡带出完工数量与报工核对不一致拦截、
    /// 批次建档+货位归位、有无零头箱、免检直接确认）、PDA其它入库；入库单管理.
    /// </summary>
    public interface IInboundService
    {
        /// <summary>
        /// 入库记录列表（按类型/方式/到货单号/流程卡/日期筛选）.
        /// </summary>
        Task<dynamic> GetInRecordList([FromBody] InRecordQuery input);

        /// <summary>
        /// 入库记录详情.
        /// </summary>
        Task<dynamic> GetInRecordDetail(string id);

        /// <summary>
        /// 入库记录删除（软删）.
        /// </summary>
        Task DeleteInRecord(string id);

        /// <summary>
        /// 到货待入库明细（PDA采购入库扫到货单号/流程卡带出，逐项核对）.
        /// </summary>
        Task<dynamic> GetPendingArrivalItems(string arrivalNo);

        /// <summary>
        /// PDA采购入库确认（到货单号非空时流程卡必填；入库数量不可超过待入数量；扫库位码落位；IQC不合格拦截）.
        /// </summary>
        Task<dynamic> PurchaseIn([FromBody] PurchaseInInput input);

        /// <summary>
        /// PDA产成品入库确认（完工数量与报工核对不一致拦截；批次建档+货位归位；有无零头箱）.
        /// </summary>
        Task<dynamic> ProductIn([FromBody] ProductInInput input);

        /// <summary>
        /// PDA其它入库确认（盘盈/退料/其他，允许无流程卡）.
        /// </summary>
        Task<dynamic> OtherIn([FromBody] OtherInInput input);

        /// <summary>
        /// 入库单管理列表（分采购入库/产成品入库/其他入库）.
        /// </summary>
        Task<dynamic> GetInOrderList([FromBody] InOrderQuery input);

        /// <summary>
        /// 入库单明细（单据头+明细）.
        /// </summary>
        Task<dynamic> GetInOrderDetail(string id);
    }
}
