using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 存货管理（技术管理）服务接口.
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// 获取存货档案列表（分页）.
        /// </summary>
        Task<dynamic> GetInventoryList([FromBody] InventoryQuery input);

        /// <summary>
        /// 获取存货档案详情（含工序检查项目配置）.
        /// </summary>
        Task<dynamic> GetInventoryById(string id);

        /// <summary>
        /// 新增/编辑存货档案（同时保存工序检查项目配置）.
        /// </summary>
        Task CruInventory([FromBody] InventoryCruInput input);

        /// <summary>
        /// 删除存货档案（级联软删除BOM子件与检查项目）.
        /// </summary>
        Task DeleteInventory(string id);

        /// <summary>
        /// 获取某母件的BOM子件明细.
        /// </summary>
        Task<dynamic> GetBomChildren(string inventoryId);

        /// <summary>
        /// 绑定/保存某母件的BOM子件清单（整体覆盖）.
        /// </summary>
        Task BindBom([FromBody] InventoryBomBindInput input);

        /// <summary>
        /// 删除单条BOM子件绑定.
        /// </summary>
        Task DeleteBom(string id);

        /// <summary>
        /// 获取某存货的工序检查项目配置.
        /// </summary>
        Task<dynamic> GetCheckItems(string inventoryId);

        /// <summary>
        /// 从用友U8同步存货档案.
        /// </summary>
        Task<dynamic> SyncFromYonyou([FromBody] SyncPullInput input);

        /// <summary>
        /// 存货下拉/检索（用于绑定子件时选择存货）.
        /// </summary>
        Task<dynamic> GetInventorySelected(string keyword);
    }
}
