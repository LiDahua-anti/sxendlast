using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 基础数据（供应商/终端设备/模具/汇率）服务接口.
    /// </summary>
    public interface IBasicDataService
    {
        #region 供应商管理

        /// <summary>
        /// 获取供应商列表（分页）.
        /// </summary>
        Task<dynamic> GetSupplierList([FromBody] SupplierQuery input);

        /// <summary>
        /// 获取供应商详情.
        /// </summary>
        Task<dynamic> GetSupplierById(string id);

        /// <summary>
        /// 新增/编辑供应商.
        /// </summary>
        Task CruSupplier([FromBody] SupplierCruInput input);

        /// <summary>
        /// 删除供应商（软删除）.
        /// </summary>
        Task DeleteSupplier(string id);

        /// <summary>
        /// 供应商下拉/检索（供到货单、来料检验引用）.
        /// </summary>
        Task<dynamic> GetSupplierSelected(string keyword);

        #endregion

        #region 终端设备管理

        /// <summary>
        /// 获取终端设备列表（分页）.
        /// </summary>
        Task<dynamic> GetTerminalList([FromBody] TerminalQuery input);

        /// <summary>
        /// 获取终端设备详情.
        /// </summary>
        Task<dynamic> GetTerminalById(string id);

        /// <summary>
        /// 新增/编辑终端设备.
        /// </summary>
        Task CruTerminal([FromBody] TerminalCruInput input);

        /// <summary>
        /// 删除终端设备（软删除）.
        /// </summary>
        Task DeleteTerminal(string id);

        /// <summary>
        /// 终端状态切换（启用/锁定/禁用）.
        /// </summary>
        Task ChangeTerminalStatus(string id, int status);

        #endregion

        #region 模具管理

        /// <summary>
        /// 获取模具列表（分页）.
        /// </summary>
        Task<dynamic> GetMoldList([FromBody] MoldQuery input);

        /// <summary>
        /// 获取模具详情.
        /// </summary>
        Task<dynamic> GetMoldById(string id);

        /// <summary>
        /// 新增/编辑模具.
        /// </summary>
        Task CruMold([FromBody] MoldCruInput input);

        /// <summary>
        /// 删除模具（软删除）.
        /// </summary>
        Task DeleteMold(string id);

        #endregion

        #region 汇率管理

        /// <summary>
        /// 获取汇率列表（分页）.
        /// </summary>
        Task<dynamic> GetExchangeRateList([FromBody] ExchangeRateQuery input);

        /// <summary>
        /// 获取汇率详情.
        /// </summary>
        Task<dynamic> GetExchangeRateById(string id);

        /// <summary>
        /// 新增/编辑汇率.
        /// </summary>
        Task CruExchangeRate([FromBody] ExchangeRateCruInput input);

        /// <summary>
        /// 删除汇率（软删除）.
        /// </summary>
        Task DeleteExchangeRate(string id);

        /// <summary>
        /// 按币种+日期查询汇率（取该日期当日或之前的最新一条）.
        /// </summary>
        Task<dynamic> GetRateByCurrency(string currency, string date);

        #endregion
    }
}
