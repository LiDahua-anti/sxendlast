using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 清洗合并码（版本化配置）服务接口.
    /// </summary>
    public interface ICleanMergeService
    {
        /// <summary>
        /// 获取当前生效配置（工序：清洗，含清洗工位成员）.
        /// </summary>
        Task<dynamic> GetCurrentConfig(string processId);

        /// <summary>
        /// 保存配置（编辑即升版本：旧版本写失效日期并标记已失效，永久保留可存档追溯）.
        /// </summary>
        Task<dynamic> SaveConfig([FromBody] CleanMergeSaveInput input);

        /// <summary>
        /// 历史配置存档（版本/成员/生效失效日期/关联报工批次）.
        /// </summary>
        Task<dynamic> GetHistory([FromBody] CleanMergeHistoryQuery input);

        /// <summary>
        /// 查看指定版本配置成员（历史存档查看）.
        /// </summary>
        Task<dynamic> GetConfigMembers(string id);
    }
}
