using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 齐套性检查（技术管理）服务接口.
    /// </summary>
    public interface IKittingService
    {
        /// <summary>
        /// 齐套性检查计算（需用数量=单个用量×投产数量×(1+损耗率)；可领量=在库-占用；输出缺料清单）.
        /// 两种模式：①母件存货+投产数量；②生产订单（取订单产品与计划数量）.
        /// </summary>
        Task<dynamic> CheckKitting([FromBody] KittingCheckInput input);
    }
}
