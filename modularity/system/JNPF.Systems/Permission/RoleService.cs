using JNPF.Common.Const;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Role;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Data;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：角色信息.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Role", Order = 167)]
[Route("api/permission/[controller]")]
public class RoleService : IRoleService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<RoleEntity> _repository;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 机构表服务.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 初始化一个<see cref="RoleService"/>类型的新实例.
    /// </summary>
    public RoleService(
        ISqlSugarRepository<RoleEntity> repository,
        IOrganizeService organizeService,
        IBillRullService billRuleService)
    {
        _repository = repository;
        _organizeService = organizeService;
        _billRuleService = billRuleService;
    }

    #region GET

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] RoleListInput input)
    {
        var roleIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>()
            .WhereIF(input.organizeId.IsNotEmptyOrNull(), x => x.ObjectId.Equals(input.organizeId))
            .Select(x => x.RoleId).ToListAsync();

        var data = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(a => a.DeleteMark == null)
            .WhereIF(input.organizeId.IsNotEmptyOrNull(), a => roleIds.Contains(a.Id))
            .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Type.Equals(input.type))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .OrderBy(a => a.GlobalMark).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime)
            .Select(a => new RoleListOutput
            {
                id = a.Id,
                type = a.Type,
                isSystem = SqlFunc.IIF(a.GlobalMark.Equals(1), 1, 0),
                enCode = a.EnCode,
                fullName = a.FullName,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                sortCode = a.SortCode,
                description = a.Description
            }).ToListAsync();

        return new { list = data };
    }

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="isSystem"></param>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector(string type, int? isSystem)
    {
        var roleList = await _repository.AsQueryable()
            .Where(a => a.DeleteMark == null)
            .WhereIF(type.IsNotEmptyOrNull(), a => a.Type.Equals(type))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Asc)
            .Select(a => new RoleListTreeOutput
            {
                id = a.Id,
                fullName = a.FullName,
                isSystem = a.GlobalMark,
                globalMark = a.GlobalMark,
                icon = "icon-ym icon-ym-generator-role",
                sortCode = a.SortCode,
                type = a.Type,
            }).ToListAsync();

        if (isSystem.IsNotEmptyOrNull())
        {
            if (isSystem.Equals(1))
                roleList = roleList.FindAll(x => x.isSystem.Equals(1));
            else
                roleList = roleList.FindAll(x => x.isSystem.Equals(2));
        }

        return new { list = roleList };
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        RoleEntity? entity = await _repository.GetFirstAsync(r => r.Id == id);
        RoleInfoOutput? output = entity.Adapt<RoleInfoOutput>();

        var perList = await _repository.GetListAsync(x => x.IsCondition == 1 && x.ConditionJson.Contains(id));
        if (perList.Any())
        {
            foreach (var item in perList)
            {
                var cond = item.ConditionJson.ToObject<RoleConstraintModel>();
                if (cond.prerequisite != null && cond.prerequisite.Contains(id))
                {
                    output.isPrePosition = true;
                    continue;
                }
            }
        }
        return output;
    }

    #endregion

    #region POST

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpPost("SelectedList")]
    public async Task<dynamic> SelectedList([FromBody] UserSelectedInput input)
    {
        return await _organizeService.GetSelectedList(input);
    }

    /// <summary>
    /// 获取角色列表 根据组织Id集合.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("getListByOrgIds")]
    public async Task<dynamic> GetListByOrgIds([FromBody] RoleListInput input)
    {
        // 获取 角色
        List<RoleListTreeOutput>? roleList = await _repository.AsSugarClient().Queryable<RoleEntity>()
            .Where(a => a.DeleteMark == null && a.EnabledMark == 1).Where(a => a.GlobalMark != 1)
            .Select(a => new RoleListTreeOutput
            {
                id = a.Id,
                type = a.Type,
                isSystem = a.GlobalMark,
                fullName = a.FullName,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                sortCode = a.SortCode
            }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc).ToListAsync();

        return roleList;
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    public async Task Create([FromBody] RoleCrInput input)
    {
        if (await _repository.IsAnyAsync(r => r.FullName == input.fullName && r.Type == input.type && r.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D1601);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D1600);

        RoleEntity? entity = input.Adapt<RoleEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.GlobalMark = 2;

        #region 约束验证

        if (input.isCondition.Equals(1) && input.type.Equals(CommonConst.RelationObjectUser))
        {
            var constraint = input.conditionJson.ToObject<RoleConstraintModel>();
            if (constraint.constraintType.Contains(0) && constraint.constraintType.Contains(2) && constraint.mutualExclusion.Any(x => constraint.prerequisite.Contains(x))) throw Oops.Oh(ErrorCode.D6020);

            if (constraint.constraintType.Contains(0) && constraint.mutualExclusion.Any())
            {
                var mePosList = await _repository.AsQueryable().Where(x => constraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                foreach (var moPos in mePosList)
                {
                    if (moPos.ConditionJson.IsNullOrEmpty()) moPos.ConditionJson = new RoleConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>() }.ToJsonString();
                    var newCond = moPos.ConditionJson.ToObject<RoleConstraintModel>();
                    if (newCond.constraintType == null) newCond.constraintType = new List<int>();
                    if (newCond.mutualExclusion == null) newCond.mutualExclusion = new List<string>();
                    if (!newCond.constraintType.Contains(0)) newCond.constraintType.Insert(0, 0);
                    if (!newCond.mutualExclusion.Contains(entity.Id)) newCond.mutualExclusion.Add(entity.Id);
                    moPos.IsCondition = 1;
                    moPos.ConditionJson = newCond.ToJsonString();
                }

                await _repository.AsSugarClient().Updateable(mePosList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            }
            if (constraint.constraintType.Contains(2) && constraint.prerequisite.Any())
            {
                var preList = await _repository.AsQueryable().Where(x => constraint.prerequisite.Contains(x.Id)).ToListAsync();

                foreach (var pre in preList)
                {
                    if (pre.IsCondition.Equals(1) && pre.ConditionJson.IsNotEmptyOrNull())
                    {
                        var preConstraint = pre.ConditionJson.ToObject<RoleConstraintModel>();
                        if (preConstraint.constraintType != null && preConstraint.constraintType.Contains(2)) throw Oops.Oh(ErrorCode.D6029);
                    }
                }
            }
        }

        #endregion

        switch (entity.Type)
        {
            case CommonConst.RelationObjectUser:
                entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeUserRole) : entity.EnCode;
                break;
            case CommonConst.RelationObjectOrganize:
                entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrgRole) : entity.EnCode;
                break;
            case CommonConst.RelationObjectPosition:
                entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePosRole) : entity.EnCode;
                break;
        }

        await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [UnitOfWork]
    public async Task Delete(string id)
    {
        RoleEntity? entity = await _repository.GetFirstAsync(r => r.Id == id && r.DeleteMark == null);
        _ = entity ?? throw Oops.Oh(ErrorCode.D1608);

        // 角色下有数据权限不能删
        List<string>? items = await GetAuthorizeItemIds(entity.Id, CommonConst.AuthorizeItemResource);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D1603);

        // 角色下有表单不能删
        items = await GetAuthorizeItemIds(entity.Id, CommonConst.AuthorizeItemForm);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D1606);

        // 角色下有列不能删除
        items = await GetAuthorizeItemIds(entity.Id, CommonConst.AuthorizeItemColumn);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D1605);

        // 角色下有按钮不能删除
        items = await GetAuthorizeItemIds(entity.Id, CommonConst.AuthorizeItemButton);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D1604);

        // 角色下有菜单不能删
        items = await GetAuthorizeItemIds(entity.Id, CommonConst.AuthorizeItemModule);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D1606);

        await _repository.AsSugarClient().Deleteable<RoleRelationEntity>().Where(x => x.RoleId.Equals(id)).ExecuteCommandAsync();
        await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
    }
    /// <summary>
    /// 获取权限项ids.
    /// </summary>
    /// <param name="roleId">角色id.</param>
    /// <param name="itemType">项类型.</param>
    /// <returns></returns>
    private async Task<List<string>> GetAuthorizeItemIds(string roleId, string itemType)
    {
        var data = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(a => a.ObjectId == roleId && a.ItemType == itemType).GroupBy(it => new { it.ItemId }).Select(it => new { it.ItemId }).ToListAsync();
        return data.Select(it => it.ItemId).ToList();
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [UnitOfWork]
    public async Task Update(string id, [FromBody] RoleUpInput input)
    {
        RoleEntity? oldRole = await _repository.AsQueryable().Where(x => x.Id.Equals(input.id)).FirstAsync();

        if (await _repository.IsAnyAsync(r => r.FullName == input.fullName && r.Type == input.type && r.DeleteMark == null && r.Id != id))
            throw Oops.Oh(ErrorCode.D1601);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D1600);

        RoleEntity? entity = input.Adapt<RoleEntity>();
        entity.GlobalMark = oldRole.GlobalMark;
        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize) : entity.EnCode;

        #region 约束验证

        if (input.isCondition.Equals(1) && input.type.Equals(CommonConst.RelationObjectUser))
        {
            var constraint = input.conditionJson.ToObject<RoleConstraintModel>();
            if (constraint.constraintType.Contains(0) && constraint.mutualExclusion.Contains(id)) throw Oops.Oh(ErrorCode.D6015);
            if (constraint.constraintType.Contains(2) && constraint.prerequisite.Contains(id)) throw Oops.Oh(ErrorCode.D6016);
            if (constraint.constraintType.Contains(0) && constraint.constraintType.Contains(2) && constraint.mutualExclusion.Any(x => constraint.prerequisite.Contains(x))) throw Oops.Oh(ErrorCode.D6020);

            if (constraint.constraintType.Contains(0) && constraint.mutualExclusion.Any())
            {
                var mePosList = await _repository.AsQueryable().Where(x => constraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                foreach (var moPos in mePosList)
                {
                    if (moPos.ConditionJson.IsNullOrEmpty()) moPos.ConditionJson = new RoleConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>(), prerequisite = new List<string>() }.ToJsonString();
                    var newCond = moPos.ConditionJson.ToObject<RoleConstraintModel>();
                    if (newCond.mutualExclusion == null) newCond.mutualExclusion = new List<string>();
                    if (newCond.constraintType == null) newCond.constraintType = new List<int>();
                    if (!newCond.constraintType.Contains(0)) newCond.constraintType.Insert(0, 0);
                    if (!newCond.mutualExclusion.Contains(id)) newCond.mutualExclusion.Add(id);
                    moPos.IsCondition = 1;
                    moPos.ConditionJson = newCond.ToJsonString();
                    if (newCond.constraintType.Contains(0) && newCond.constraintType.Contains(2) && newCond.mutualExclusion.Any(x => newCond.prerequisite.Contains(x))) throw Oops.Oh(ErrorCode.D6020);
                }

                await _repository.AsSugarClient().Updateable(mePosList).ExecuteCommandAsync();
            }
            else
            {
                if (constraint.constraintType.Contains(2) && constraint.prerequisite.Any())
                {
                    var preList = await _repository.AsQueryable().Where(x => constraint.prerequisite.Contains(x.Id)).ToListAsync();

                    foreach (var pre in preList)
                    {
                        if (pre.IsCondition.Equals(1) && pre.ConditionJson.IsNotEmptyOrNull())
                        {
                            var preConstraint = pre.ConditionJson.ToObject<RoleConstraintModel>();
                            if (preConstraint.constraintType != null && preConstraint.constraintType.Contains(2)) throw Oops.Oh(ErrorCode.D6029);
                        }
                    }
                }
                if (oldRole.ConditionJson.IsNotEmptyOrNull())
                {
                    var oldConstraint = oldRole.ConditionJson.ToObject<RoleConstraintModel>();
                    var mePosList = await _repository.AsQueryable().Where(x => oldConstraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                    foreach (var moPos in mePosList)
                    {
                        if (moPos.ConditionJson.IsNullOrEmpty()) moPos.ConditionJson = new RoleConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>() }.ToJsonString();
                        var newCond = moPos.ConditionJson.ToObject<RoleConstraintModel>();
                        if (newCond.mutualExclusion == null) newCond.mutualExclusion = new List<string>();
                        if (newCond.constraintType == null) newCond.constraintType = new List<int>();
                        if (newCond.mutualExclusion.Contains(id)) newCond.mutualExclusion.Remove(id);
                        if (newCond.constraintType.Contains(0) && !newCond.mutualExclusion.Any()) newCond.constraintType.Remove(0);
                        if (newCond.constraintType.Any()) moPos.IsCondition = 1;
                        else moPos.IsCondition = 0;
                        moPos.ConditionJson = newCond.ToJsonString();
                    }

                    await _repository.AsSugarClient().Updateable(mePosList).ExecuteCommandAsync();
                }
                if (constraint.mutualExclusion != null) constraint.mutualExclusion.Clear();
                input.conditionJson = constraint.ToJsonString();
            }

            // 验证是否存在已有互斥角色用户
            if (constraint.constraintType.Contains(0))
            {
                var userIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => constraint.mutualExclusion.Contains(x.RoleId) || x.RoleId.Equals(id)).ToListAsync();
                if (userIds.Count(x => x.RoleId.Equals(id)) > 0)
                {
                    foreach (var userId in userIds.Where(x => x.RoleId.Equals(id)).Select(x => x.ObjectId).Distinct())
                    {
                        var objIds = userIds.Where(x => x.ObjectId.Equals(userId) && constraint.mutualExclusion.Contains(x.RoleId)).Select(x => x.RoleId).ToList();
                        if (objIds.Any()) throw Oops.Oh(ErrorCode.D6025);
                    }
                }
            }

            // 验证是否不存在已有先决角色用户
            if (constraint.constraintType.Contains(2))
            {
                var reIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => constraint.prerequisite.Contains(x.RoleId) || x.RoleId.Equals(id)).ToListAsync();
                if (reIds.Count(x => x.RoleId.Equals(id)) > 0)
                {
                    var userIds = new List<string>();

                    foreach (var userId in reIds.Where(x => x.RoleId.Equals(id)).Select(x => x.ObjectId).Distinct())
                    {
                        var objIds = reIds.Where(x => x.ObjectId.Equals(userId)).Select(x => x.RoleId).ToList();
                        if (!constraint.prerequisite.All(x => objIds.Contains(x))) userIds.Add(userId);
                    }

                    if (userIds.Any())
                    {
                        throw Oops.Oh(ErrorCode.D6026);
                    }
                }
            }
        }
        else
        {
            if (input.conditionJson.IsNotEmptyOrNull())
            {
                var constraint = input.conditionJson.ToObject<RoleConstraintModel>();
                if (constraint.mutualExclusion != null) constraint.mutualExclusion.Clear();
                input.conditionJson = constraint.ToJsonString();
            }
            if (oldRole.ConditionJson.IsNotEmptyOrNull())
            {
                var oldConstraint = oldRole.ConditionJson.ToObject<RoleConstraintModel>();
                var mePosList = await _repository.AsQueryable().Where(x => oldConstraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                foreach (var moPos in mePosList)
                {
                    var newCond = moPos.ConditionJson.ToObject<RoleConstraintModel>();
                    if (newCond.mutualExclusion == null) newCond.mutualExclusion = new List<string>();
                    if (newCond.mutualExclusion.Contains(id)) newCond.mutualExclusion.Remove(id);
                    if (newCond.constraintType.Contains(0) && !newCond.mutualExclusion.Any()) newCond.constraintType.Remove(0);
                    if (newCond.constraintType.Any()) moPos.IsCondition = 1;
                    else moPos.IsCondition = 0;
                    moPos.ConditionJson = newCond.ToJsonString();
                }

                await _repository.AsSugarClient().Updateable(mePosList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            }
        }

        #endregion

        await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 通过角色id获取角色列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("RoleCondition")]
    public async Task<dynamic> RoleCondition([FromBody] RoleConditionInput input)
    {
        var roleList = await _repository.AsQueryable()
            .Where(a => a.DeleteMark == null && a.EnabledMark == 1 && input.ids.Contains(a.Id))
            .Select(a => new RoleListTreeOutput
            {
                id = a.Id,
                type = a.Type,
                fullName = a.FullName,
                isSystem = a.GlobalMark,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                sortCode = a.SortCode,
                icon = "icon-ym icon-ym-global-role"
            }).MergeTable().OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc).ToListAsync();

        return roleList;
    }

    #endregion
}