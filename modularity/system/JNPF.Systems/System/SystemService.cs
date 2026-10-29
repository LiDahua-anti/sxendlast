using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.System.System;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using NPOI.Util;
using SqlSugar;
using System.Text.RegularExpressions;

namespace JNPF.Systems.System;

/// <summary>
/// 系统功能.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "System", Order = 200)]
[Route("api/system/[controller]")]
public class SystemService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SystemEntity> _repository;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 初始化一个<see cref="SystemService"/>类型的新实例.
    /// </summary>
    public SystemService(
        ISqlSugarRepository<SystemEntity> repository,
        ICacheManager cacheManager,
        IMHandler imHandler,
        IUserManager userManager,
        IBillRullService billRuleService)
    {
        _repository = repository;
        _cacheManager = cacheManager;
        _imHandler = imHandler;
        _userManager = userManager;
        _billRuleService = billRuleService;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] KeywordInput input)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;

        var whereLambda = LinqExpression.And<SystemEntity>();
        whereLambda = whereLambda.And(x => x.DeleteMark == null && x.IsMain == 0);
        if (!_userManager.IsAdministrator)
        {
            if (_userManager.IsDevRole.Value)
                whereLambda = whereLambda.And(x => x.UserId == _userManager.UserId || x.AuthorizeId.Contains(CommonConst.SystemAuthCodeAllDevUser) || x.AuthorizeId.Contains(_userManager.UserId));
            else
                whereLambda = whereLambda.And(x => x.UserId == _userManager.UserId);
        }
        if (input.keyword.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.FullName.Contains(input.keyword) || x.EnCode.Contains(input.keyword));
        if (ignoreIds.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => !ignoreIds.Contains(x.Id));
        var entityList = await _repository.AsQueryable().Where(whereLambda).OrderBy(a => a.SortCode).OrderByDescending(a => a.CreatorTime).ToListAsync();

        var output = new List<SystemListOutput>();
        foreach (var item in entityList)
        {
            var newItem = item.Adapt<SystemListOutput>();
            var isUser = item.UserId.IsNotEmptyOrNull() && item.UserId.Equals(_userManager.UserId);
            var isDev = _userManager.IsDevRole.Value && item.AuthorizeId.IsNotEmptyOrNull() && (item.AuthorizeId.Equals(CommonConst.SystemAuthCodeAllDevUser) || item.AuthorizeId.Contains(_userManager.UserId));
            newItem.hasDelete = _userManager.IsAdministrator || isUser ? 1 : 0;
            newItem.hasUpdate = _userManager.IsAdministrator || isUser || isDev ? 1 : 0;
            output.Add(newItem);
        }

        return new { list = output };
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await GetEntityInfo(id);
        return data.Adapt<SystemCrInput>();
    }

    /// <summary>
    /// 下拉.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        return (await _repository.AsQueryable()
             .Where(x => x.DeleteMark == null && x.IsMain == 0)
             .OrderBy(x => x.SortCode).OrderByDescending(x => x.CreatorTime)
             .ToListAsync()).Adapt<List<SystemListOutput>>();
    }

    /// <summary>
    /// 获取开发人员列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("getDevUser")]
    public async Task<dynamic> GetDevUser([FromQuery] PageInputBase input)
    {
        var developerRoleId = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null && x.EnCode.Equals("developer")).Select(x => x.Id).FirstAsync();
        var developerUerIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.RoleId.Equals(developerRoleId)).Select(x => x.ObjectId).ToListAsync();
        var list = await _repository.AsSugarClient().Queryable<UserEntity>()
            .Where(x => x.DeleteMark == null && x.EnabledMark != 0 && developerUerIds.Contains(x.Id))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.Account.Contains(input.keyword) || x.RealName.Contains(input.keyword))
            .Select(x => new SystemDevUserOutput
            {
                id = x.Id,
                account = x.Account,
                realName = x.RealName,
                headIcon = x.HeadIcon,
                fullName = SqlFunc.MergeString(x.RealName, "/", x.Account)
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<SystemDevUserOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取应用权限.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("getAppAuthorization/{id}")]
    public async Task<dynamic> GetAppAuthorization(string id)
    {
        var data = await GetEntityInfo(id);

        var output = new SystemAuthorizationOutput();
        output.createUserId = data.UserId;
        output.fullName = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null && x.Id == data.UserId).Select(x => SqlFunc.MergeString(x.RealName, "/", x.Account)).FirstAsync();
        if (data.AuthorizeId.IsNotEmptyOrNull())
        {
            if (data.AuthorizeId.Equals(CommonConst.SystemAuthCodeAllDevUser))
                output.isAllDevUser = 1;
            else
                output.devUsers = data.AuthorizeId.Split(",").ToList();
        }

        return output;
    }

    /// <summary>
    /// 有权限的应用列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("userAuthList")]
    public async Task<dynamic> GetUserAuthList([FromQuery] KeywordInput input)
    {
        var output = (await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null && x.IsMain.Equals(0))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword) || x.EnCode.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderByDescending(a => a.CreatorTime)
            .ToListAsync()).Adapt<List<SystemListOutput>>();

        if (!_userManager.IsAdministrator)
        {
            var authorIds = await _userManager.GetUserAuthorizeItemIdList(CommonConst.AuthorizeItemSystem);
            output = output.FindAll(x => authorIds.Contains(x.id));
        }

        return output;
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    public async Task<dynamic> Create([FromBody] SystemCrInput input)
    {
        var regex = "^[a-zA-Z0-9]+$";
        if (input.enCode.IsNotEmptyOrNull() && !Regex.IsMatch(input.enCode, regex))
            throw Oops.Oh(ErrorCode.D3501);
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);

        var entity = input.Adapt<SystemEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.UserId = _userManager.UserId;
        entity.AuthorizeId = CommonConst.SystemAuthCodeAllDevUser;
        if (entity.EnCode.IsNullOrEmpty()) entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeSystem);

        // 添加流程菜单.
        await AddWorkFlowModule(entity.Id);

        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (isOk < 1) throw Oops.Oh(ErrorCode.COM1000);

        return entity.EnCode;
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [UnitOfWork]
    public async Task Update(string id, [FromBody] SystemCrInput input)
    {
        var regex = "^[a-zA-Z0-9]+$";
        if (input.enCode.IsNotEmptyOrNull() && !Regex.IsMatch(input.enCode, regex))
            throw Oops.Oh(ErrorCode.D3501);
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);

        var entity = input.Adapt<SystemEntity>();
        if (entity.EnCode.IsNullOrEmpty()) entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeSystem);
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null) ?? throw Oops.Oh(ErrorCode.COM1005);
        if (!_userManager.IsAdministrator && !entity.UserId.Equals(_userManager.UserId)) throw Oops.Oh(ErrorCode.D1045);

        await ForcedOffline(id);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 应用授权移交.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("changeAppAuthorization")]
    public async Task ChangeAppAuthorization([FromBody] SystemSaveAuthorizationInput input)
    {
        var data = await GetEntityInfo(input.systemId);
        data.UserId = input.createUserId;
        data.LastModifyTime = DateTime.Now;
        data.LastModifyUserId = _userManager.UserId;
        var isOk = await _repository.AsUpdateable(data)
            .UpdateColumns(it => new {
                it.UserId,
                it.LastModifyTime,
                it.LastModifyUserId
            }).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 应用授权保存应用开发者.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("saveAppAuthorization")]
    public async Task SaveAppAuthorization([FromBody] SystemSaveAuthorizationInput input)
    {
        var data = await GetEntityInfo(input.systemId);
        data.LastModifyTime = DateTime.Now;
        data.LastModifyUserId = _userManager.UserId;
        if (input.isAllDevUser == 1)
            data.AuthorizeId = CommonConst.SystemAuthCodeAllDevUser;
        else
            data.AuthorizeId = input.devUsers.IsNotEmptyOrNull() ? string.Join(",", input.devUsers) : null;

        var isOk = await _repository.AsUpdateable(data)
            .UpdateColumns(it => new {
                it.AuthorizeId,
                it.LastModifyTime,
                it.LastModifyUserId
            }).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 保存偏好配置.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("preferenceSave/{id}")]
    public async Task PreferenceSave(string id, [FromBody] SystemPreferenceSaveInput input)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => it.PreferenceJson == input.preferenceJson).Where(it => it.DeleteMark == null && it.Id.Equals(id)).ExecuteCommandAsync();
        if (isOk <= 0) throw Oops.Oh(ErrorCode.COM1001);
    }

    #endregion

    #region Private

    /// <summary>
    /// 实体信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<SystemEntity> GetEntityInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
    }

    /// <summary>
    /// 强制当前系统下的所有用户下线或切换应用.
    /// </summary>
    /// <param name="systemId"></param>
    private async Task ForcedOffline(string systemId)
    {
        var tenantId = _userManager.TenantId;
        var cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        var allUserOnlineList = await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);

        var saveUserOnlineList = allUserOnlineList.Copy();
        foreach (var item in allUserOnlineList)
        {
            if (item.systemId.IsNotEmptyOrNull() && item.systemId.Equals(systemId))
            {
                await _imHandler.SendMessageAsync(item.connectionId, new { method = "logout", msg = "应用已被禁用或删除" }.ToJsonString());
            }
        }
    }

    /// <summary>
    /// 添加流程菜单.
    /// </summary>
    /// <returns></returns>
    private async Task AddWorkFlowModule(string systemId)
    {
        var workFlowCodeList = new List<string> { "workFlow", "workFlow.flowQuickLaunch", "workFlow.flowLaunch", "workFlow.flowTodo", "workFlow.flowDone", "workFlow.flowCirculate", "workFlow.flowToSign", "workFlow.flowDoing" };
        var workFlowSystemId = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => x.DeleteMark == null && x.EnCode.Equals(CommonConst.SystemCodeWorkFlow)).Select(x => x.Id).FirstAsync();
        var workFlowModuleList = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => x.DeleteMark == null && x.SystemId.Equals(workFlowSystemId) && workFlowCodeList.Contains(x.EnCode)).ToListAsync();

        var newModuleList = new List<ModuleEntity>();
        var workFlow = workFlowModuleList.Find(x => x.EnCode.Equals("workFlow"));
        foreach (var category in new List<string>() { "Web", "App" })
        {
            var newWorkFlow = workFlow.Adapt<ModuleEntity>();
            newWorkFlow.Id = SnowflakeIdHelper.NextId();
            newWorkFlow.CreatorTime = DateTime.Now;
            newWorkFlow.CreatorUserId = _userManager.UserId;
            newWorkFlow.LastModifyTime = null;
            newWorkFlow.LastModifyUserId = null;
            newWorkFlow.Category = category;
            newWorkFlow.SystemId = systemId;
            newModuleList.Add(newWorkFlow);
            foreach (var moduele in workFlowModuleList)
            {
                if (!moduele.EnCode.Equals("workFlow"))
                {
                    var newModuele = moduele.Adapt<ModuleEntity>();
                    newModuele.Id = SnowflakeIdHelper.NextId();
                    newModuele.CreatorTime = DateTime.Now;
                    newModuele.CreatorUserId = _userManager.UserId;
                    newModuele.LastModifyTime = null;
                    newModuele.LastModifyUserId = null;
                    newModuele.Category = category;
                    newModuele.SystemId = systemId;
                    newModuele.ParentId = newWorkFlow.Id;
                    newModuleList.Add(newModuele);
                }
            }
        }

        await _repository.AsSugarClient().Insertable(newModuleList).ExecuteCommandAsync();
    }
    #endregion
}
