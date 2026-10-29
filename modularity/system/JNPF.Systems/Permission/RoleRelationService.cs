using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Dto.Role;
using JNPF.Systems.Entitys.Dto.RoleRelation;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using Microsoft.AspNetCore.Mvc;
using NPOI.Util;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：角色关系.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "RoleRelation", Order = 169)]
[Route("api/permission/[controller]")]
public class RoleRelationService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<RoleRelationEntity> _repository;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="RoleRelationService"/>类型的新实例.
    /// </summary>
    public RoleRelationService(
        ISqlSugarRepository<RoleRelationEntity> repository,
        IOrganizeService organizeService,
        ICacheManager cacheManager,
        IMHandler imHandler,
        IUserManager userManager)
    {
        _repository = repository;
        _organizeService = organizeService;
        _cacheManager = cacheManager;
        _imHandler = imHandler;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 获取角色成员列表.
    /// </summary>
    /// <param name="input">.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] RoleRelationListInput input)
    {
        switch (input.type)
        {
            case CommonConst.RelationObjectUser:
                {
                    var dictionaryTypeEntity2 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "sex" && x.DeleteMark == null && x.EnabledMark == 1);

                    var data = await _repository.AsSugarClient().Queryable<UserEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.ObjectId))
                        .Where((a, b) => a.DeleteMark == null && b.RoleId.Equals(input.roleId))
                        .WhereIF(input.keyword.IsNotEmptyOrNull(), (a, b) => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
                        .OrderByDescending((a, b) => b.CreatorTime)
                        .Select((a, b) => new RoleUserRelationListOutput()
                        {
                            id = a.Id,
                            account = a.Account,
                            realName = a.RealName,
                            enabledMark = a.EnabledMark,
                            gender = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.DictionaryTypeId == dictionaryTypeEntity2.Id && d.EnCode == a.Gender && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                            mobilePhone = a.MobilePhone,
                            position = a.PositionId
                        }).ToPagedListAsync(input.currentPage, input.pageSize);

                    #region 处理 用户 多岗位
                    List<UserRelationEntity>? orgUserIdAll = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                        .Where(x => data.list.Select(u => u.id).Contains(x.UserId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).ToListAsync();
                    var posList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => orgUserIdAll.Select(xx => xx.ObjectId).Contains(x.Id)).ToListAsync();
                    var orgList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => posList.Select(xx => xx.OrganizeId).Contains(x.Id)).ToListAsync();
                    foreach (var item in posList)
                    {
                        item.FullName = string.Format("{0}/{1}", orgList.FirstOrDefault(x => x.Id.Equals(item.OrganizeId))?.OrganizeNameTree, item.FullName);
                    }
                    foreach (var item in data.list)
                    {
                        // 获取用户岗位集合
                        item.position = string.Join(" ; ", posList.Where(x => orgUserIdAll.Where(xx => xx.UserId.Equals(item.id)).Select(xx => xx.ObjectId).Contains(x.Id)).Select(x => x.FullName).ToList());
                    }

                    #endregion

                    return new {
                        list = data.list,
                        pagination = new { currentPage = input.currentPage, pageSize = input.pageSize, total = data.pagination.Total }
                    };
                }
            case CommonConst.RelationObjectOrganize:
                {
                    var data = await _repository.AsSugarClient().Queryable<OrganizeEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.ObjectId))
                        .WhereIF(input.keyword.IsNotEmptyOrNull(), (a, b) => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
                        .Where((a, b) => a.DeleteMark == null && b.RoleId.Equals(input.roleId)).OrderByDescending((a, b) => b.CreatorTime)
                        .Select((a, b) => new RoleOrganizeRelationListOutput()
                        {
                            id = a.Id,
                            enCode = a.EnCode,
                            fullName = a.OrganizeNameTree,
                            orgNameTree = a.OrganizeNameTree,
                            description = a.Description
                        }).ToPagedListAsync(input.currentPage, input.pageSize);

                    return new {
                        list = data.list,
                        pagination = new { currentPage = input.currentPage, pageSize = input.pageSize, total = data.pagination.Total }
                    };
                }
            case CommonConst.RelationObjectPosition:
                {
                    var data = await _repository.AsSugarClient().Queryable<PositionEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.ObjectId))
                        .WhereIF(input.keyword.IsNotEmptyOrNull(), (a, b) => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
                        .Where((a, b) => a.DeleteMark == null && b.RoleId.Equals(input.roleId)).OrderByDescending((a, b) => b.CreatorTime)
                        .Select((a, b) => new RolePositionRelationListOutput()
                        {
                            id = a.Id,
                            enCode = a.EnCode,
                            fullName = SqlFunc.Subqueryable<OrganizeEntity>().EnableTableFilter().Where(o => o.Id == a.OrganizeId).Select(o => SqlFunc.MergeString(o.OrganizeNameTree, "/", a.FullName)),
                            description = a.Description
                        }).ToPagedListAsync(input.currentPage, input.pageSize);

                    var pageList = new {
                        list = data.list,
                        pagination = new { currentPage = input.currentPage, pageSize = input.pageSize, total = data.pagination.Total }
                    };
                    return pageList;
                }
        }

        return null;
    }

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("RoleList")]
    public async Task<dynamic> RoleList([FromQuery] RoleListInput input)
    {
        var subOrganizeIds = new List<string>() { input.organizeId };
        if (input.showSubOrganize != null && input.showSubOrganize.Equals(1))
            subOrganizeIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.OrganizeIdTree.Contains(input.organizeId)).Select(x => x.Id).ToListAsync();

        var roleIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>()
            .WhereIF(input.positionId.IsNullOrEmpty(), x => subOrganizeIds.Contains(x.ObjectId))
            .WhereIF(input.positionId.IsNotEmptyOrNull(), x => x.ObjectId.Equals(input.positionId))
            .Select(x => x.RoleId).ToListAsync();

        var data = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(a => a.DeleteMark == null)
            .WhereIF(input.organizeId.IsNotEmptyOrNull() || input.positionId.IsNotEmptyOrNull(), a => roleIds.Contains(a.Id))
            .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Type.Equals(input.type))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .OrderBy(a => a.GlobalMark).OrderBy(a => a.SortCode)
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
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<RoleListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取角色成员列表.
    /// </summary>
    /// <param name="objectId">角色id.</param>
    /// <returns></returns>
    [HttpGet("{objectId}")]
    public async Task<dynamic> GetListByObjectId(string objectId)
    {
        return new { ids = await _repository.AsQueryable().Where(u => u.ObjectId == objectId).Select(s => s.RoleId).ToListAsync() };
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建角色关系.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    [NonUnify]
    public async Task<dynamic> CreateRelation([FromBody] RoleRelationCrInput input)
    {
        var entity = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.Id.Equals(input.roleId)).FirstAsync();

        if (entity == null)
        {
            return new UnifyResult.RESTfulResult<object>()
            {
                code = 503,
                msg = "数据不存在"
            };
        }

        if (input.type.Equals(CommonConst.RelationObjectUser) && await _repository.AsSugarClient().Queryable<UserEntity>().AnyAsync(x => input.ids.Contains(x.Id) && x.IsAdministrator.Equals(1)))
        {
            return new UnifyResult.RESTfulResult<object>()
            {
                code = 503,
                msg = "添加失败"
            };
        }

        #region 角色约束验证

        var errorList = new List<string>();
        if (entity.IsCondition.Equals(1) && entity.ConditionJson.IsNotEmptyOrNull() && entity.Type.Equals(CommonConst.RelationObjectUser))
        {
            var condition = entity.ConditionJson.ToObject<RoleConstraintModel>();
            if (condition.constraintType.Any())
            {
                if (condition.constraintType.Contains(1))
                {
                    var currIds = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectUser) && x.RoleId.Equals(input.roleId)).Select(x => x.ObjectId).ToListAsync();
                    var userIds = currIds.Copy();
                    userIds.AddRange(input.ids);
                    userIds = userIds.Distinct().ToList();
                    if (userIds.Count() > condition.userNum)
                    {
                        if (condition.userNum < 1)
                        {
                            return new UnifyResult.RESTfulResult<object>()
                            {
                                code = 503,
                                msg = "该角色已达用户限制个数"
                            };
                        }
                        else
                        {
                            var eCount = (userIds.Count() - condition.userNum).ParseToInt();
                            if (eCount >= input.ids.Count)
                            {
                                return new UnifyResult.RESTfulResult<object>()
                                {
                                    code = 503,
                                    msg = "该角色已达用户限制个数"
                                };
                            }
                            else
                            {
                                input.ids = input.ids.Where(x => !currIds.Contains(x)).ToList();
                                errorList.AddRange(input.ids.Where(x => !input.ids.GetRange(0, input.ids.Count - eCount).Contains(x)));
                            }
                        }
                    }
                }
                if (condition.constraintType.Contains(0))
                {
                    var userIds = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectUser) && input.ids.Contains(x.ObjectId) && condition.mutualExclusion.Contains(x.RoleId)).Select(x => x.ObjectId).ToListAsync();
                    if (userIds.Any()) errorList.AddRange(userIds);
                }
                if (condition.constraintType.Contains(2))
                {
                    var exUserIds = new List<string>();
                    var reIds = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectUser) && input.ids.Contains(x.ObjectId) && condition.prerequisite.Contains(x.RoleId)).ToListAsync();
                    foreach (var userId in input.ids)
                    {
                        var objIds = reIds.Where(x => x.ObjectId.Equals(userId)).Select(x => x.RoleId).ToList();
                        if (!condition.prerequisite.All(x => objIds.Contains(x))) exUserIds.Add(userId);
                    }

                    errorList.AddRange(exUserIds);
                }
            }
        }

        if (errorList.Any()) input.ids = input.ids.Except(errorList).ToList();
        #endregion

        var oldRelation = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(input.type) && x.RoleId.Equals(input.roleId)).ToListAsync();
        var addList = new List<RoleRelationEntity>();
        addList.AddRange(oldRelation);
        foreach (var item in input.ids)
        {
            if (!oldRelation.Any(x => x.ObjectId.Equals(item) && x.ObjectType.Equals(input.type) && x.RoleId.Equals(input.roleId)))
            {
                addList.Add(new RoleRelationEntity()
                {
                    ObjectId = item,
                    ObjectType = input.type,
                    RoleId = input.roleId,
                    CreatorTime = DateTime.Now,
                    Id = SnowflakeIdHelper.NextId(),
                });
            }
        }

        if (addList.Any())
        {
            // 删除旧的关联数据
            await _repository.AsSugarClient().Deleteable<RoleRelationEntity>().Where(x => x.ObjectType.Equals(input.type) && x.RoleId.Equals(input.roleId)).ExecuteCommandAsync();

            await _repository.AsSugarClient().Insertable(addList).ExecuteCommandAsync();
            if (input.type.Equals(CommonConst.RelationObjectUser))
                await ForcedOffline(addList.Select(x => x.ObjectId).ToList());
            if (input.type.Equals(CommonConst.RelationObjectPosition))
            {
                var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => addList.Select(xx => xx.ObjectId).Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                await ForcedOffline(userIds);
            }
        }

        addList = addList.Where(x => input.ids.Contains(x.ObjectId)).ToList();

        return new UnifyResult.RESTfulResult<object>()
        {
            code = errorList.Any() && addList.Any() ? 200 : (!errorList.Any() ? 200 : 503),
            msg = errorList.Any() && addList.Any() ? "部分添加成功" : (!errorList.Any() ? "添加成功" : "添加失败")
        };
    }

    /// <summary>
    /// 删除角色关系.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("Delete")]
    public async Task DeleteRelation([FromBody] RoleRelationCrInput input)
    {
        await _repository.AsSugarClient().Deleteable<RoleRelationEntity>()
            .Where(x => x.RoleId.Equals(input.roleId) && input.ids.Contains(x.ObjectId) && x.ObjectType.Equals(input.type)).ExecuteCommandAsync();

        var userIdList = new List<string>();
        if (input.type.Equals(CommonConst.RelationObjectUser))
        {
            userIdList.AddRange(input.ids);
        }
        else if (input.type.Equals(CommonConst.RelationObjectPosition))
        {
            var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType.Equals(input.type) && input.ids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
            userIdList.AddRange(ids);
        }

        // 下线
        await ForcedOffline(userIdList);
    }

    /// <summary>
    /// 新建角色关系.
    /// </summary>
    /// <param name="input">input.</param>
    /// <returns></returns>
    [HttpPost("AddRoles")]
    public async Task AddRoles([FromBody] RoleRelationCrInput input)
    {
        var addList = new List<RoleRelationEntity>();
        foreach (var item in input.ids)
        {
            addList.Add(new RoleRelationEntity()
            {
                ObjectId = input.objectId,
                ObjectType = input.type,
                RoleId = item,
            });
        }

        await _repository.AsSugarClient().Insertable(addList).CallEntityMethod(x => x.Create()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 移除关联.
    /// </summary>
    /// <param name="input">input.</param>
    /// <returns></returns>
    [HttpPost("DeleteRoles")]
    public async Task DeleteRoles([FromBody] RoleRelationCrInput input)
    {
        await _repository.AsSugarClient().Deleteable<RoleRelationEntity>().Where(x => input.ids.Contains(x.RoleId) && x.ObjectId.Equals(input.objectId)).ExecuteCommandAsync();

        var userIdList = new List<string>();
        if (input.type.Equals(CommonConst.RelationObjectUser))
        {
            userIdList.AddRange(input.ids);
        }
        else if (input.type.Equals(CommonConst.RelationObjectPosition))
        {
            var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType.Equals(input.type) && input.ids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
            userIdList.AddRange(ids);
        }

        // 下线
        await ForcedOffline(userIdList);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 创建角色关系.
    /// </summary>
    /// <param name="roleId">角色ID.</param>
    /// <param name="ids">对象ID组.</param>
    /// <param name="relationType">关系类型(岗位-Position;角色-Role;组织-Organize;分组-Group;).</param>
    /// <returns></returns>
    [NonAction]
    public List<RoleRelationEntity> CreateUserRelation(string roleId, string ids, string relationType)
    {
        List<RoleRelationEntity> userRelationList = new List<RoleRelationEntity>();
        if (!ids.IsNullOrEmpty())
        {
            List<string>? position = new List<string>(ids.Split(','));
            position.ForEach(item =>
            {
                RoleRelationEntity? entity = new RoleRelationEntity();
                entity.Id = SnowflakeIdHelper.NextId();
                entity.ObjectType = relationType;
                entity.ObjectId = item;
                entity.SortCode = position.IndexOf(item);
                entity.RoleId = roleId;
                entity.CreatorTime = DateTime.Now;
                entity.CreatorUserId = _userManager.UserId;
                userRelationList.Add(entity);
            });
        }

        return userRelationList;
    }

    /// <summary>
    /// 创建角色关系.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [NonAction]
    public async Task Create(List<RoleRelationEntity> input)
    {
        await _repository.AsInsertable(input).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">角色ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task Delete(string id)
    {

        await _repository.AsDeleteable().Where(u => u.RoleId == id).ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据角色主键获取列表.
    /// </summary>
    /// <param name="roleId">角色主键.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<RoleRelationEntity>> GetListByUserId(string roleId)
    {
        return await _repository.AsQueryable().Where(m => m.RoleId == roleId).OrderBy(o => o.CreatorTime).ToListAsync();
    }

    /// <summary>
    /// 获取角色.
    /// </summary>
    /// <param name="type">关系类型.</param>
    /// <param name="objId">对象ID.</param>
    /// <returns></returns>
    [NonAction]
    public List<string> GetUserId(string type, string objId)
    {
        var adminId = _userManager.GetAdminUserId();
        return _repository.AsSugarClient().Queryable<RoleRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.RoleId == b.Id))
                .Where((a, b) => a.ObjectType == type && a.ObjectId == objId && b.DeleteMark == null && b.EnabledMark > 0).Select(a => a.RoleId).Distinct().ToList();
    }

    /// <summary>
    /// 获取角色.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="objId"></param>
    /// <returns></returns>
    [NonAction]
    public List<string> GetUserId(List<string> objId, string type = null)
    {
        var adminId = _userManager.GetAdminUserId();
        if (objId.Any())
        {
            var objIds = new List<string>();
            foreach (var item in objId)
            {
                var id = item.Replace("--user", string.Empty).Replace("--department", string.Empty).Replace("--company", string.Empty).Replace("--role", string.Empty).Replace("--position", string.Empty).Replace("--group", string.Empty);
                objIds.Add(id);
            }
            if (type.IsNotEmptyOrNull())
            {
                return _repository.AsSugarClient().Queryable<RoleRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.RoleId == b.Id))
                .Where((a, b) => a.ObjectType == type && b.DeleteMark == null && (objIds.Contains(a.ObjectId) || objIds.Contains(a.RoleId)) && b.EnabledMark > 0).Select(a => a.RoleId).Distinct().ToList();
            }
            else
            {
                return _repository.AsSugarClient().Queryable<RoleRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.RoleId == b.Id))
               .Where((a, b) => b.DeleteMark == null && (objIds.Contains(a.ObjectId) || objIds.Contains(a.RoleId)) && b.EnabledMark > 0).Select(a => a.RoleId).Distinct().ToList();
            }
        }
        else
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 新角色组件获取人员.
    /// </summary>
    /// <param name="Ids"></param>
    /// <returns></returns>
    [NonAction]
    public List<string> GetUserId(List<string> Ids)
    {
        var userIdList = new List<string>();
        var objIdList = new List<string>();
        var adminId = _userManager.GetAdminUserId();
        foreach (var item in Ids)
        {
            var strList = item.Split("--").ToList();
            if (strList.Count == 1)
            {
                if (_repository.AsSugarClient().Queryable<UserEntity>().Any(x => x.Id == strList[0] && x.EnabledMark == 1))
                {
                    userIdList.Add(strList[0]);
                }
            }
            else if (strList.Count >= 2 && strList[1].Equals("user"))
            {
                if (_repository.AsSugarClient().Queryable<UserEntity>().Any(x => x.Id == strList[0] && x.EnabledMark == 1))
                {
                    userIdList.Add(strList[0]);
                }
            }
            else if (strList.Count >= 2 && (strList[1].Equals("department") || strList[1].Equals("company")))
            {
                var orgIds = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.OrganizeIdTree.Contains(strList[0]) && x.EnabledMark == 1 && x.DeleteMark == null).Select(x => x.Id).ToList();
                objIdList = objIdList.Union(orgIds).ToList();
            }
            else
            {
                objIdList.Add(strList[0]);
            }
        }
        var otherUserIds = _repository.AsSugarClient().Queryable<RoleRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.RoleId == b.Id))
                .Where((a, b) => b.DeleteMark == null && objIdList.Contains(a.ObjectId) && b.EnabledMark == 1).Select(a => a.RoleId).Distinct().ToList();
        return otherUserIds.Union(userIdList).ToList();
    }

    /// <summary>
    /// 强制在线角色下线.
    /// </summary>
    /// <param name="userIds">角色Id.</param>
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
    #endregion
}