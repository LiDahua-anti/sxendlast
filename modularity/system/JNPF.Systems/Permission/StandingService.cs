using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Permission.Standing;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.Permission;

/// <summary>
/// 业务实现：身份管理.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Standing", Order = 171)]
[Route("api/Permission/[controller]")]
public class StandingService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<StandingEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="StandingService"/>类型的新实例.
    /// </summary>
    public StandingService(
        ISqlSugarRepository<StandingEntity> repository,
        IUserManager userManager,
        ICacheManager cacheManager,
        IMHandler imHandler,
        IBillRullService billRuleService)
    {
        _repository = repository;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _imHandler = imHandler;
        _billRuleService = billRuleService;
    }

    #region Get

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PageInputBase input)
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), it => it.FullName.Contains(input.keyword) || it.EnCode.Contains(input.keyword))
            .OrderBy(it => it.IsSystem).OrderBy(it => it.SortCode).OrderBy(it => it.CreatorTime)
            .Select(it => new StandingListOutput()
            {
                id = it.Id,
                fullName = it.FullName,
                isSystem = SqlFunc.IIF(it.IsSystem.Equals(1), it.IsSystem, 0)
            }).ToListAsync();
        return new { list = list };
    }

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("List")]
    public async Task<dynamic> GetList([FromQuery] StandingListInput input)
    {
        var objIds = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(x => x.ItemType.Equals(CommonConst.AuthorizeItemStanding) && x.ItemId.Equals(input.id) && x.ObjectType.Equals(input.type))
            .OrderByDescending(x => x.CreatorTime).Select(x => x.ObjectId).ToListAsync();
        var objIdsStr = string.Join(",", objIds);
        if (input.type.Equals(CommonConst.RelationObjectRole))
        {
            var sEntity = await _repository.AsQueryable().Where(x => x.Id.Equals(input.id)).FirstAsync();
            var data = await _repository.AsSugarClient().Queryable<RoleEntity>()
                .Where(x => x.DeleteMark == null && objIds.Contains(x.Id))
                .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword) || x.EnCode.Contains(input.keyword))
                .Select(x => new Entitys.Dto.Role.RoleListOutput()
                {
                    id = x.Id,
                    fullName = x.FullName,
                    enCode = x.EnCode,
                    description = x.Description,
                    disable = SqlFunc.IIF(x.EnCode.Equals(sEntity.EnCode), 1, 0)
                }).MergeTable().OrderByDescending(x => x.disable).OrderBy(x => SqlFunc.CharIndexNew(objIdsStr, x.id)).ToPagedListAsync(input.currentPage, input.pageSize);

            return data;
        }
        else if (input.type.Equals(CommonConst.RelationObjectPosition))
        {
            var data = await _repository.AsSugarClient().Queryable<PositionEntity>()
                .Where(x => x.DeleteMark == null && objIds.Contains(x.Id))
                .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword) || x.EnCode.Contains(input.keyword))
                .OrderBy(x => SqlFunc.CharIndexNew(objIdsStr, x.Id))
                .Select(x => new Entitys.Dto.Position.PositionListOutput()
                {
                    id = x.Id,
                    fullName = x.FullName,
                    enCode = x.EnCode,
                    description = x.Description,
                }).ToPagedListAsync(input.currentPage, input.pageSize);
            return data;
        }

        return null;
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var entity = await _repository.GetFirstAsync(r => r.Id == id);
        return entity.Adapt<StandingInfoOutput>();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] StandingCrInput input)
    {
        if (await _repository.IsAnyAsync(r => r.FullName == input.fullName && r.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3401);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3403);

        StandingEntity? entity = input.Adapt<StandingEntity>();
        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeStanding) : entity.EnCode;
        entity.IsSystem = 2;

        await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] StandingUpInput input)
    {
        if (await _repository.IsAnyAsync(r => r.FullName == input.fullName && r.DeleteMark == null && r.Id != id))
            throw Oops.Oh(ErrorCode.D3401);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D3403);

        var oldEntity = await _repository.AsQueryable().Where(x => x.Id.Equals(input.id)).FirstAsync();
        var entity = input.Adapt<StandingEntity>();
        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize) : entity.EnCode;
        entity.IsSystem = oldEntity.IsSystem;

        await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 新建身份关系.
    /// </summary>
    /// <param name="id">身份id.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/AddObject")]
    public async Task AddUser(string id, [FromBody] StandingConditionInput input)
    {
        var addList = new List<AuthorizeEntity>();
        foreach (var item in input.ids)
        {
            addList.Add(new AuthorizeEntity()
            {
                ObjectId = item,
                ObjectType = input.type,
                ItemType = CommonConst.AuthorizeItemStanding,
                ItemId = id,
            });
        }

        await _repository.AsSugarClient().Insertable(addList).CallEntityMethod(x => x.Create()).ExecuteCommandAsync();

        if (!input.type.Equals(CommonConst.RelationObjectOrganize))
        {
            var userIds = new List<string>();
            userIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => !x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && input.ids.Contains(x.RoleId)).Select(x => x.ObjectId).ToListAsync();
            userIds.AddRange(await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => input.ids.Contains(x.ObjectId) || userIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync());
            await ForcedOffline(userIds);
        }
    }

    /// <summary>
    /// 删除身份关系.
    /// </summary>
    /// <param name="id">身份id.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/DeleteObject")]
    public async Task DeleteObject(string id, [FromBody] StandingConditionInput input)
    {
        await _repository.AsSugarClient().Deleteable<AuthorizeEntity>()
            .Where(x => input.ids.Contains(x.ObjectId))
            .Where(x => x.ObjectType.Equals(input.type))
            .Where(x => x.ItemType.Equals(CommonConst.AuthorizeItemStanding))
            .Where(x => x.ItemId.Equals(id)).ExecuteCommandAsync();

        if (!input.type.Equals(CommonConst.RelationObjectOrganize))
        {
            var userIds = new List<string>();
            userIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => !x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && input.ids.Contains(x.RoleId)).Select(x => x.ObjectId).ToListAsync();
            userIds.AddRange(await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => input.ids.Contains(x.ObjectId) || userIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync());
            await ForcedOffline(userIds);
        }
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
        if (entity == null) throw Oops.Oh(ErrorCode.COM1005);

        var objIds = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(x => x.ItemType.Equals(CommonConst.AuthorizeItemStanding) && x.ItemId.Equals(id) && !x.ObjectType.Equals(CommonConst.RelationObjectOrganize))
            .Select(x => x.ObjectId).ToListAsync();
        if (objIds.Any())
        {
            var userIds = new List<string>();
            userIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => !x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && objIds.Contains(x.RoleId)).Select(x => x.ObjectId).ToListAsync();
            userIds.AddRange(await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => objIds.Contains(x.ObjectId) || userIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync());
            await ForcedOffline(userIds);
        }
        await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<AuthorizeEntity>().Where(x => x.ItemType.Equals(CommonConst.AuthorizeItemStanding) && x.ItemId.Equals(id)).ExecuteCommandAsync();
    }

    #endregion

    /// <summary>
    /// 强制在线用户下线.
    /// </summary>
    /// <param name="userIds">用户Id.</param>
    /// <returns></returns>
    [NonAction]
    public async Task ForcedOffline(List<string> userIds)
    {
        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);
        if (list != null && list.Any())
        {
            var fList = list.Where(it => it.tenantId == _userManager.TenantId && userIds.Contains(it.userId)).ToList();
            foreach (var user in fList)
            {
                await _imHandler.SendMessageAsync(user.connectionId, new { method = "refresh", msg = "您的权限发生变更,请刷新界面！" }.ToJsonString());
            }
        }
    }
}
