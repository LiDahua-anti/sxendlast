using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 报工记录（三层级/PDA报工/检包检验）服务接口.
    /// </summary>
    public interface IWorkReportService
    {
        /// <summary>
        /// 报工记录列表（一级：流程卡层级，含当前工序/报工条数）.
        /// </summary>
        Task<dynamic> GetWorkReportList([FromBody] WorkReportQuery input);

        /// <summary>
        /// 报工记录流程卡明细（三级展开：流程卡→工序流转→报工明细）.
        /// </summary>
        Task<dynamic> GetWorkReportCardDetail(string flowCardId);

        /// <summary>
        /// PDA提交报工（多人报工按人数均分；清洗工序按清洗合并码配置人数均分；末道检查工序写检包检验记录）.
        /// </summary>
        Task<dynamic> SubmitWorkReport([FromBody] WorkReportSubmitInput input);

        /// <summary>
        /// 获取检包检验记录（检查明细弹窗，检查=工序最后一步）.
        /// </summary>
        Task<dynamic> GetReportCheckDetail(string reportId);

        /// <summary>
        /// 结束生产（未完工流程卡保留，次日可继续报工）.
        /// </summary>
        Task<dynamic> EndProduction(string flowCardId);

        /// <summary>
        /// 修改报工结果（记录修改人和修改时间）.
        /// </summary>
        Task<dynamic> UpdateWorkReportResult([FromBody] WorkReportResultInput input);
    }
}
