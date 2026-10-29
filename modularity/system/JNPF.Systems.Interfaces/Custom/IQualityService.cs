using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 品质管理（来料检验IQC/质量检验OQC）服务接口.
    /// </summary>
    public interface IQualityService
    {
        /// <summary>
        /// 来料检验列表（分页）.
        /// </summary>
        Task<dynamic> GetIqcList([FromBody] IqcQuery input);

        /// <summary>
        /// 来料检验统计卡片（本月检验批次/合格率/免检批次/不合格批次）.
        /// </summary>
        Task<dynamic> GetIqcStatistics();

        /// <summary>
        /// 来料检验明细（主表+检查项目实测值与判定）.
        /// </summary>
        Task<dynamic> GetIqcDetail(string id);

        /// <summary>
        /// 来料检验登记（PDA采购入库扫码；免检自动跳过检查项并标记免检；不合格自动进入待处理）.
        /// </summary>
        Task<dynamic> RegisterIqc([FromBody] IqcRegisterInput input);

        /// <summary>
        /// 来料检验不良处理记录（待处理→处理中→已处理）.
        /// </summary>
        Task HandleIqcBad([FromBody] BadHandleInput input);

        /// <summary>
        /// 质量抽检列表（分页）.
        /// </summary>
        Task<dynamic> GetOqcList([FromBody] OqcQuery input);

        /// <summary>
        /// 质量抽检统计卡片（今日抽检批次/合格率/待检包检查/不良批次）.
        /// </summary>
        Task<dynamic> GetOqcStatistics();

        /// <summary>
        /// 质量抽检明细（主表+检查项目实测值与判定）.
        /// </summary>
        Task<dynamic> GetOqcDetail(string id);

        /// <summary>
        /// 质量抽检登记（PDA品质抽检扫码；按批抽样判定；合格后联动订单「待入库」；不合格自动进入待处理）.
        /// </summary>
        Task<dynamic> RegisterOqc([FromBody] OqcRegisterInput input);

        /// <summary>
        /// 质量抽检不良处理记录（待处理→处理中→已处理）.
        /// </summary>
        Task HandleOqcBad([FromBody] BadHandleInput input);

        /// <summary>
        /// 工序检查内容定义（PDA检查项目表按工序带出）.
        /// </summary>
        Task<dynamic> GetProcessCheckItems(string processId);
    }
}
