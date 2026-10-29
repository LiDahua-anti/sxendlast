using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 仓库/货位档案服务接口.
    /// 仓库层引用用友U8仓库档案；货位层按 区-排-列-层 编码规则（如 A1-1-1），
    /// 支持最大体积/重量校验和条形码绑定，PDA扫码入库时按货位码落位.
    /// </summary>
    public interface IWarehouseArchiveService
    {
        /// <summary>
        /// 仓库档案列表（按编码/名称/货位管理筛选）.
        /// </summary>
        Task<dynamic> GetWarehouseList([FromBody] WarehouseQuery input);

        /// <summary>
        /// 仓库档案新增/编辑（编码唯一校验）.
        /// </summary>
        Task CruWarehouse([FromBody] WarehouseInput input);

        /// <summary>
        /// 仓库档案删除（存在货位时拦截）.
        /// </summary>
        Task DeleteWarehouse(string id);

        /// <summary>
        /// 货位档案列表（按仓库/货位编码筛选）.
        /// </summary>
        Task<dynamic> GetLocationList([FromBody] WarehouseLocationQuery input);

        /// <summary>
        /// 货位档案新增/编辑（编码区-排-列-层解析与唯一校验、体积重量、条码）.
        /// </summary>
        Task CruLocation([FromBody] WarehouseLocationInput input);

        /// <summary>
        /// 货位档案删除.
        /// </summary>
        Task DeleteLocation(string id);

        /// <summary>
        /// 按库位码扫码查询（兼容货位编码/条形码；PDA扫码落位）.
        /// </summary>
        Task<dynamic> GetLocationByCode(string code);

        /// <summary>
        /// 从用友U8同步仓库档案（模拟实现）.
        /// </summary>
        Task SyncWarehouseFromYonyou();
    }
}
