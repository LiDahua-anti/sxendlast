using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Interfaces.Custom;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 基础数据管理（供应商/终端设备/模具/汇率）.
    /// 供应商供到货单、来料检验引用；终端设备为 PDA 手持终端档案；
    /// 模具供生产领用与寿命跟踪；汇率按币种+日期维护。
    /// </summary>
    [ApiDescriptionSettings(Tag = "BasicData", Name = "", Order = 202)]
    [Route("api/[controller]")]
    public class BasicDataService : IBasicDataService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 供应商档案仓储.
        /// </summary>
        private readonly ISqlSugarRepository<SupplierEntity> _supplierRepository;

        /// <summary>
        /// 终端设备仓储.
        /// </summary>
        private readonly ISqlSugarRepository<TerminalDeviceEntity> _terminalRepository;

        /// <summary>
        /// 模具档案仓储.
        /// </summary>
        private readonly ISqlSugarRepository<MoldEntity> _moldRepository;

        /// <summary>
        /// 汇率仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ExchangeRateEntity> _exchangeRateRepository;

        /// <summary>
        /// 初始化一个<see cref="BasicDataService"/>类型的新实例.
        /// </summary>
        public BasicDataService(
            ISqlSugarRepository<SupplierEntity> supplierRepository,
            ISqlSugarRepository<TerminalDeviceEntity> terminalRepository,
            ISqlSugarRepository<MoldEntity> moldRepository,
            ISqlSugarRepository<ExchangeRateEntity> exchangeRateRepository)
        {
            _supplierRepository = supplierRepository;
            _terminalRepository = terminalRepository;
            _moldRepository = moldRepository;
            _exchangeRateRepository = exchangeRateRepository;
        }

        #region 供应商管理

        /// <summary>
        /// 获取供应商列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetSupplierList")]
        public async Task<dynamic> GetSupplierList([FromBody] SupplierQuery input)
        {
            var whereLambda = LinqExpression.And<SupplierEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.Keyword))
            {
                whereLambda = whereLambda.And(m => m.SupplierCode.Contains(input.Keyword)
                    || m.SupplierName.Contains(input.Keyword)
                    || m.ShortName.Contains(input.Keyword));
            }

            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.Status == input.Status);
            }

            return await _supplierRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 获取供应商详情.
        /// </summary>
        /// <param name="id">供应商主键.</param>
        /// <returns>供应商详情.</returns>
        [HttpGet("GetSupplierById")]
        public async Task<dynamic> GetSupplierById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _supplierRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return entity;
        }

        /// <summary>
        /// 新增/编辑供应商.
        /// </summary>
        /// <param name="input">供应商信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruSupplier")]
        public async Task CruSupplier([FromBody] SupplierCruInput input)
        {
            if (input.SupplierCode.IsNullOrEmpty() || input.SupplierName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _supplierRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                // 供应商编码唯一性校验（排除自身）
                if (await _supplierRepository.IsAnyAsync(p => p.SupplierCode == input.SupplierCode && p.Id != input.Id && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                existing = input.Adapt(existing);
                isOk = await _supplierRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                // 供应商编码唯一性校验
                if (await _supplierRepository.IsAnyAsync(p => p.SupplierCode == input.SupplierCode && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                var entity = input.Adapt<SupplierEntity>();
                entity.Status = input.Status ?? 1;
                isOk = await _supplierRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除供应商（软删除）.
        /// </summary>
        /// <param name="id">供应商主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteSupplier")]
        public async Task DeleteSupplier(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _supplierRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _supplierRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 供应商下拉/检索（供到货单、来料检验引用）.
        /// </summary>
        /// <param name="keyword">编码/名称/简称关键字.</param>
        /// <returns>供应商选择项.</returns>
        [HttpGet("GetSupplierSelected")]
        public async Task<dynamic> GetSupplierSelected(string keyword)
        {
            var query = _supplierRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.Status == 1);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(m => m.SupplierCode.Contains(keyword)
                    || m.SupplierName.Contains(keyword)
                    || m.ShortName.Contains(keyword));
            }

            return await query
                .OrderBy(m => m.CreatorTime, OrderByType.Desc)
                .Take(50)
                .Select(m => new SupplierSelectedOutput
                {
                    id = m.Id,
                    code = m.SupplierCode,
                    name = m.SupplierName,
                    contactPerson = m.ContactPerson,
                    contactPhone = m.ContactPhone
                })
                .ToListAsync();
        }

        #endregion

        #region 终端设备管理

        /// <summary>
        /// 获取终端设备列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetTerminalList")]
        public async Task<dynamic> GetTerminalList([FromBody] TerminalQuery input)
        {
            var whereLambda = LinqExpression.And<TerminalDeviceEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.Keyword))
            {
                whereLambda = whereLambda.And(m => m.TerminalCode.Contains(input.Keyword)
                    || m.TerminalName.Contains(input.Keyword)
                    || m.Station.Contains(input.Keyword)
                    || m.TerminalIp.Contains(input.Keyword));
            }

            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.Status == input.Status);
            }

            return await _terminalRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 获取终端设备详情.
        /// </summary>
        /// <param name="id">终端主键.</param>
        /// <returns>终端详情.</returns>
        [HttpGet("GetTerminalById")]
        public async Task<dynamic> GetTerminalById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _terminalRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return entity;
        }

        /// <summary>
        /// 新增/编辑终端设备.
        /// </summary>
        /// <param name="input">终端信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruTerminal")]
        public async Task CruTerminal([FromBody] TerminalCruInput input)
        {
            if (input.TerminalCode.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _terminalRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                // 终端编码唯一性校验（排除自身）
                if (await _terminalRepository.IsAnyAsync(p => p.TerminalCode == input.TerminalCode && p.Id != input.Id && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                existing = input.Adapt(existing);
                isOk = await _terminalRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                // 终端编码唯一性校验
                if (await _terminalRepository.IsAnyAsync(p => p.TerminalCode == input.TerminalCode && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                var entity = input.Adapt<TerminalDeviceEntity>();
                entity.Status = input.Status ?? 1;
                isOk = await _terminalRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除终端设备（软删除）.
        /// </summary>
        /// <param name="id">终端主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteTerminal")]
        public async Task DeleteTerminal(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _terminalRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _terminalRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 终端状态切换（启用/锁定/禁用）.
        /// </summary>
        /// <param name="id">终端主键.</param>
        /// <param name="status">状态：1启用 2锁定 3禁用.</param>
        /// <returns>无.</returns>
        [HttpGet("ChangeTerminalStatus")]
        public async Task ChangeTerminalStatus(string id, int status)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _terminalRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            entity.Status = status;
            int isOk = await _terminalRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.LastModify())
                .UpdateColumns(it => new { it.Status, it.LastModifyTime, it.LastModifyUserId })
                .ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1003);
            }
        }

        #endregion

        #region 模具管理

        /// <summary>
        /// 获取模具列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetMoldList")]
        public async Task<dynamic> GetMoldList([FromBody] MoldQuery input)
        {
            var whereLambda = LinqExpression.And<MoldEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.Keyword))
            {
                whereLambda = whereLambda.And(m => m.MoldCode.Contains(input.Keyword)
                    || m.MoldName.Contains(input.Keyword)
                    || m.ProductName.Contains(input.Keyword));
            }

            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.Status == input.Status);
            }

            return await _moldRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 获取模具详情.
        /// </summary>
        /// <param name="id">模具主键.</param>
        /// <returns>模具详情.</returns>
        [HttpGet("GetMoldById")]
        public async Task<dynamic> GetMoldById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _moldRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return entity;
        }

        /// <summary>
        /// 新增/编辑模具.
        /// </summary>
        /// <param name="input">模具信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruMold")]
        public async Task CruMold([FromBody] MoldCruInput input)
        {
            if (input.MoldCode.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _moldRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                // 模具编号唯一性校验（排除自身）
                if (await _moldRepository.IsAnyAsync(p => p.MoldCode == input.MoldCode && p.Id != input.Id && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                existing = input.Adapt(existing);
                isOk = await _moldRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                // 模具编号唯一性校验
                if (await _moldRepository.IsAnyAsync(p => p.MoldCode == input.MoldCode && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                var entity = input.Adapt<MoldEntity>();
                entity.Status = input.Status ?? 1;
                entity.UsedCount = input.UsedCount ?? 0;
                isOk = await _moldRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除模具（软删除）.
        /// </summary>
        /// <param name="id">模具主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteMold")]
        public async Task DeleteMold(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _moldRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _moldRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        #endregion

        #region 汇率管理

        /// <summary>
        /// 获取汇率列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetExchangeRateList")]
        public async Task<dynamic> GetExchangeRateList([FromBody] ExchangeRateQuery input)
        {
            var whereLambda = LinqExpression.And<ExchangeRateEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.Currency))
            {
                whereLambda = whereLambda.And(m => m.Currency.Contains(input.Currency));
            }

            if (!string.IsNullOrEmpty(input.StartDate))
            {
                var start = DateTime.Parse(input.StartDate);
                whereLambda = whereLambda.And(m => m.RateDate >= start);
            }

            if (!string.IsNullOrEmpty(input.EndDate))
            {
                var end = DateTime.Parse(input.EndDate).AddDays(1);
                whereLambda = whereLambda.And(m => m.RateDate < end);
            }

            return await _exchangeRateRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(m => m.RateDate, OrderByType.Desc)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 获取汇率详情.
        /// </summary>
        /// <param name="id">汇率主键.</param>
        /// <returns>汇率详情.</returns>
        [HttpGet("GetExchangeRateById")]
        public async Task<dynamic> GetExchangeRateById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _exchangeRateRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return entity;
        }

        /// <summary>
        /// 新增/编辑汇率.
        /// </summary>
        /// <param name="input">汇率信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruExchangeRate")]
        public async Task CruExchangeRate([FromBody] ExchangeRateCruInput input)
        {
            if (input.Currency.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _exchangeRateRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                existing = input.Adapt(existing);
                isOk = await _exchangeRateRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                var entity = input.Adapt<ExchangeRateEntity>();
                entity.RateToCny = input.RateToCny;
                entity.RateDate = input.RateDate ?? DateTime.Now;
                isOk = await _exchangeRateRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除汇率（软删除）.
        /// </summary>
        /// <param name="id">汇率主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteExchangeRate")]
        public async Task DeleteExchangeRate(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _exchangeRateRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _exchangeRateRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 按币种+日期查询汇率（取该日期当日或之前的最新一条）.
        /// </summary>
        /// <param name="currency">币种.</param>
        /// <param name="date">查询日期（yyyy-MM-dd，可空取最新）.</param>
        /// <returns>汇率记录.</returns>
        [HttpGet("GetRateByCurrency")]
        public async Task<dynamic> GetRateByCurrency(string currency, string date)
        {
            if (currency.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var query = _exchangeRateRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.Currency == currency);

            if (!string.IsNullOrEmpty(date))
            {
                var queryDate = DateTime.Parse(date);
                query = query.Where(m => m.RateDate <= queryDate);
            }

            var entity = await query
                .OrderBy(m => m.RateDate, OrderByType.Desc)
                .OrderBy(m => m.CreatorTime, OrderByType.Desc)
                .FirstAsync();

            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return entity;
        }

        #endregion
    }
}
