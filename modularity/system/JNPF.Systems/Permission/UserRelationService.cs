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
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Dto.UserRelation;
using JNPF.Systems.Entitys.Enum;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Linq.Expressions;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：用户关系.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "UserRelation", Order = 169)]
[Route("api/permission/[controller]")]
public class UserRelationService : IUserRelationService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserRelationEntity> _repository;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IDepartmentService _departmentService;

    /// <summary>
    /// 系统配置服务.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 第三方同步服务.
    /// </summary>
    private readonly ISynThirdInfoService _synThirdInfoService;

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
    /// 初始化一个<see cref="UserRelationService"/>类型的新实例.
    /// </summary>
    public UserRelationService(
        ISqlSugarRepository<UserRelationEntity> userRelationRepository,
        IOrganizeService organizeService,
        IDepartmentService departmentService,
        ISysConfigService sysConfigService,
        ISynThirdInfoService synThirdInfoService,
        ICacheManager cacheManager,
        IMHandler imHandler,
        IUserManager userManager)
    {
        _repository = userRelationRepository;
        _organizeService = organizeService;
        _departmentService = departmentService;
        _sysConfigService = sysConfigService;
        _synThirdInfoService = synThirdInfoService;
        _cacheManager = cacheManager;
        _imHandler = imHandler;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 获取用户列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("UserList")]
    public async Task<PageResult<UserListOutput>> GetUserList([FromQuery] UserListQuery input)
    {
        // 获取配置文件 账户锁定类型
        SysConfigEntity? config = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("lockType") && x.Category.Equals("SysConfig")).FirstAsync();
        ErrorStrategy configLockType = (ErrorStrategy)Enum.Parse(typeof(ErrorStrategy), config?.Value);

        // 性别字典类型
        var dictionaryTypeEntity2 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "sex" && x.DeleteMark == null && x.EnabledMark == 1);
        var dutyUserId = string.Empty;
        var userIds = new List<string>();
        if (input.organizeId.IsNotEmptyOrNull())
        {
            input.organizeId = input.organizeId.Split("--").FirstOrDefault();
            userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(input.organizeId)).OrderByDescending(x => x.CreatorTime).Select(x => x.UserId).ToListAsync();
        }
        if (input.groupId.IsNotEmptyOrNull())
        {
            input.groupId = input.groupId.Split("--").FirstOrDefault();
            userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(input.groupId)).OrderByDescending(x => x.CreatorTime).Select(x => x.UserId).ToListAsync();
        }
        if (input.roleId.IsNotEmptyOrNull())
        {
            input.roleId = input.roleId.Split("--").FirstOrDefault();
            userIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.RoleId.Equals(input.roleId)).OrderByDescending(x => x.CreatorTime).Select(x => x.ObjectId).ToListAsync();
        }
        if (input.positionId.IsNotEmptyOrNull())
        {
            input.positionId = input.positionId.Split("--").FirstOrDefault();
            dutyUserId = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.Id.Equals(input.positionId)).Select(x => x.DutyUserId).FirstAsync();
            input.organizeId = null;
            userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(input.positionId)).OrderByDescending(x => x.CreatorTime).Select(x => x.UserId).ToListAsync();
        }
        var subOrganizeIds = new List<string>();
        if (input.showSubOrganize != null && input.showSubOrganize.Equals(1))
        {
            subOrganizeIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.OrganizeIdTree.Contains(input.organizeId)).Select(x => x.Id).ToListAsync();
            input.organizeId = null;
            userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => subOrganizeIds.Contains(x.ObjectId)).OrderByDescending(x => x.CreatorTime).Select(x => x.UserId).ToListAsync();
        }
        userIds = userIds.Distinct().ToList();

        var isJoinQuery = input.groupId.IsNullOrEmpty() && input.organizeId.IsNullOrEmpty() && input.roleId.IsNullOrEmpty() && input.positionId.IsNullOrEmpty() && input.showSubOrganize.IsNullOrEmpty() ? true : false;

        var data = await _repository.AsSugarClient().Queryable<UserEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.UserId))
            .Where((a, b) => a.DeleteMark == null && !a.Account.Equals("admin"))
            .WhereIF(input.enabledMark != null, (a, b) => a.EnabledMark.Equals(input.enabledMark))
            .WhereIF(input.gender != null, (a, b) => a.Gender.Equals(input.gender))
            .WhereIF(input.showSubOrganize.Equals(1), (a, b) => subOrganizeIds.Contains(b.ObjectId))
            .WhereIF(input.groupId.IsNotEmptyOrNull(), (a, b) => b.ObjectId.Equals(input.groupId))
            .WhereIF(input.roleId.IsNotEmptyOrNull(), (a, b) => userIds.Contains(a.Id))
            .WhereIF(input.positionId.IsNotEmptyOrNull(), (a, b) => b.ObjectId.Equals(input.positionId))
            .WhereIF(input.organizeId.IsNotEmptyOrNull(), (a, b) => b.ObjectId.Equals(input.organizeId))
            .WhereIF(!input.keyword.IsNullOrEmpty(), (a, b) => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword) || a.MobilePhone.Contains(input.keyword))
            .GroupBy(a => new { a.Id, a.Account, a.RealName, a.CreatorTime, a.Gender, a.MobilePhone, a.SortCode, a.UnLockTime, a.IsAdministrator, a.EnabledMark, a.HandoverUserId })
            .OrderByIF(input.roleId.IsNotEmptyOrNull() || input.showSubOrganize.Equals(1) || input.groupId.IsNotEmptyOrNull() || input.positionId.IsNotEmptyOrNull() || input.organizeId.IsNotEmptyOrNull(), (a, b) => SqlFunc.CharIndexNew(string.Join("", userIds), a.Id))
            .OrderByIF(isJoinQuery, a => a.SortCode)
            .OrderByIF(isJoinQuery, a => a.CreatorTime, OrderByType.Desc)
            .Select(a => new UserListOutput
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                fullName = SqlFunc.MergeString(a.RealName, "/", a.Account),
                headIcon = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(e => e.Id == a.Id).Select(e => SqlFunc.MergeString("/api/File/Image/userAvatar/", e.HeadIcon)),
                creatorTime = a.CreatorTime,
                gender = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.DictionaryTypeId == dictionaryTypeEntity2.Id && d.EnCode == a.Gender && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                mobilePhone = a.MobilePhone,
                sortCode = a.SortCode,
                isAdministrator = a.IsAdministrator,
                isDutyUser = a.Id.Equals(dutyUserId) ? 1 : 0,
                enabledMark = SqlFunc.IIF(configLockType == ErrorStrategy.Delay && a.EnabledMark == 2 && a.UnLockTime < DateTime.Now, 1, a.EnabledMark),
                handoverMark = SqlFunc.IIF(SqlFunc.IsNullOrEmpty(a.HandoverUserId), 0, 1)
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        #region 处理 用户 多岗位
        List<UserRelationEntity>? orgUserIdAll = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => data.list.Select(u => u.id).Contains(x.UserId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).ToListAsync();
        var posList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => orgUserIdAll.Select(xx => xx.ObjectId).Contains(x.Id)).ToListAsync();
        var orgList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => posList.Select(xx => xx.OrganizeId).Contains(x.Id)).ToListAsync();
        foreach (var item in posList)
        {
            item.FullName = string.Format("{0}/{1}", orgList.Find(x => x.Id.Equals(item.OrganizeId)).OrganizeNameTree, item.FullName);
        }
        foreach (UserListOutput? item in data.list)
        {
            // 获取用户岗位集合
            item.position = string.Join(" ; ", posList.Where(x => orgUserIdAll.Where(xx => xx.UserId.Equals(item.id)).Select(xx => xx.ObjectId).Contains(x.Id)).Select(x => x.FullName).ToList());
        }

        #endregion

        return new PageResult<UserListOutput>() { list = data.list.ToList(), pagination = data.pagination.Adapt<PageResult>() };
    }

    /// <summary>
    /// 获取岗位/角色成员列表.
    /// </summary>
    /// <param name="objectId">岗位id或角色id.</param>
    /// <returns></returns>
    [HttpGet("{objectId}")]
    public async Task<dynamic> GetListByObjectId(string objectId)
    {
        return new { ids = await _repository.AsQueryable().Where(u => u.ObjectId == objectId).Select(s => s.UserId).ToListAsync() };
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建用户关系.
    /// </summary>
    /// <param name="objectId">功能主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    [NonUnify]
    public async Task<dynamic> Create([FromBody] UserRelationCrInput input)
    {
        var entity = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.Id.Equals(input.objectId)).FirstAsync();

        if (entity == null)
        {
            return new UnifyResult.RESTfulResult<object>()
            {
                code = 503,
                msg = "数据不存在"
            };
        }

        if (input.objectType.Equals(CommonConst.RelationObjectPosition) && await _repository.AsSugarClient().Queryable<UserEntity>().AnyAsync(x => input.userIds.Contains(x.Id) && x.IsAdministrator.Equals(1)))
        {
            return new UnifyResult.RESTfulResult<object>()
            {
                code = 503,
                msg = "添加失败"
            };
        }

        // 已存在关系的数据
        var addList = await _repository.AsQueryable().Where(x => input.userIds.Contains(x.UserId) && x.ObjectId != null).ToListAsync();

        #region 岗位约束验证
        var errorList = new List<string>();
        if (entity != null && entity.IsCondition.Equals(1) && entity.ConditionJson.IsNotEmptyOrNull())
        {
            var condition = entity.ConditionJson.ToObject<Entitys.Dto.Position.PositionConstraintModel>();
            if (condition.constraintType.Any())
            {
                if (condition.constraintType.Contains(1))
                {
                    var userIds = await _repository.AsSugarClient().Queryable<UserEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.UserId)).Where((a, b) => input.objectId.Equals(b.ObjectId) && b.ObjectType.Equals(CommonConst.RelationObjectPosition)).Select((a, b) => b.UserId).ToListAsync();
                    userIds.AddRange(input.userIds);
                    userIds = userIds.Distinct().ToList();
                    if (userIds.Count() > condition.userNum)
                    {
                        if (condition.userNum < 1)
                        {
                            return new UnifyResult.RESTfulResult<object>()
                            {
                                code = 503,
                                msg = "该岗位已达用户限制个数"
                            };
                        }
                        else
                        {
                            var eCount = (userIds.Count() - condition.userNum).ParseToInt();
                            if (eCount >= input.userIds.Count)
                            {
                                return new UnifyResult.RESTfulResult<object>()
                                {
                                    code = 503,
                                    msg = "该岗位已达用户限制个数"
                                };
                            }
                            else
                            {
                                input.userIds = input.userIds.Where(x => !addList.Where(x => x.ObjectId.Equals(input.objectId)).Select(x => x.UserId).Contains(x)).ToList();
                                errorList.AddRange(input.userIds.Where(x => !input.userIds.GetRange(0, input.userIds.Count - eCount).Contains(x)));
                            }
                        }
                    }
                }
                if (condition.constraintType.Contains(0))
                {
                    var userIds = await _repository.AsQueryable().Where(x => input.userIds.Contains(x.UserId) && condition.mutualExclusion.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                    if (userIds.Any()) errorList.AddRange(userIds);
                }
                if (condition.constraintType.Contains(2))
                {
                    var exUserIds = new List<string>();
                    var reIds = await _repository.AsQueryable().Where(x => input.userIds.Contains(x.UserId) && condition.prerequisite.Contains(x.ObjectId)).ToListAsync();
                    foreach (var userId in input.userIds)
                    {
                        var objIds = reIds.Where(x => x.UserId.Equals(userId)).Select(x => x.ObjectId).ToList();
                        if (!condition.prerequisite.All(x => objIds.Contains(x))) exUserIds.Add(userId);
                    }

                    errorList.AddRange(exUserIds);
                }
            }
        }

        if (errorList.Any()) input.userIds = input.userIds.Except(errorList).ToList();
        #endregion

        input.userIds.ForEach(item =>
        {
            if (!addList.Any(x => x.UserId.Equals(item) && x.ObjectType.Equals(input.objectType) && x.ObjectId.Equals(input.objectId)))
            {
                addList.Add(new UserRelationEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    UserId = item,
                    ObjectType = input.objectType,
                    ObjectId = input.objectId,
                    CreatorTime = DateTime.Now,
                    SortCode = input.userIds.IndexOf(item)
                });

                // 新增用户岗位关系,联动添加组织关系
                if (entity != null && !addList.Any(x => x.UserId.Equals(item) && x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && x.ObjectId.Equals(entity.OrganizeId)))
                {
                    addList.Add(new UserRelationEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        UserId = item,
                        ObjectType = CommonConst.RelationObjectOrganize,
                        ObjectId = entity.OrganizeId,
                        CreatorTime = DateTime.Now,
                        SortCode = input.userIds.IndexOf(item)
                    });
                }
            }
        });

        if (addList.Any())
        {
            // 删除旧的关联数据
            await _repository.AsSugarClient().Deleteable<UserRelationEntity>().Where(x => addList.Select(xx => xx.UserId).Contains(x.UserId)).ExecuteCommandAsync();

            await _repository.AsInsertable(addList).ExecuteCommandAsync();

            // 当前所属组织、岗位为空 默认赋值
            var pList = addList.Where(x => x.ObjectType.Equals(CommonConst.RelationObjectPosition)).ToList();
            if (pList.Any())
            {
                var userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.PositionId == null && pList.Select(xx => xx.UserId).Contains(x.Id)).ToListAsync();
                var posList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => pList.Select(xx => xx.ObjectId).Contains(x.Id)).ToListAsync();
                foreach (var user in userList)
                {
                    var pEntity = posList.Find(x => x.Id.Equals(pList.Find(x => x.UserId.Equals(user.Id)).ObjectId));
                    user.PositionId = pEntity.Id;
                    user.OrganizeId = pEntity.OrganizeId;
                }

                await _repository.AsSugarClient().Updateable(userList).UpdateColumns(it => new { it.PositionId, it.OrganizeId }).ExecuteCommandAsync();
            }

            if (input.objectType.Equals(CommonConst.RelationObjectPosition))
                await ForcedOffline(addList.Select(x => x.UserId).ToList());
        }

        #region 第三方同步

        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            if (sysConfig.dingDepartment.IsNotEmptyOrNull() && await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().AnyAsync(x => x.ThirdType == 2 && x.DataType == 1 && x.EnabledMark == 1))
            {
                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.dingDepartment)).Select(x => x.Id).ToListAsync();
                var pos = await _repository.AsSugarClient().Queryable<PositionEntity>().FirstAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && x.Id.Equals(input.objectId));
                if (pos.IsNotEmptyOrNull())
                {
                    var userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => input.userIds.Contains(x.Id)).ToListAsync();
                    foreach (var user in userList)
                    {
                        if (sysConfig.dingSynIsSynUser)
                        {
                            user.OrganizeId = pos.OrganizeId;
                            var newList = new List<UserEntity>() { user };
                            await _synThirdInfoService.SynUser(2, 2, sysConfig, newList);
                        }
                        else
                        {
                            var synEntity = new SynThirdInfoEntity() { ThirdType = 2, DataType = 2, SysObjId = user.Id, EnabledMark = 0 };
                            await _repository.AsSugarClient().Insertable(synEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                        }
                    }
                }
            }

            if (sysConfig.qyhDepartment.IsNotEmptyOrNull() && await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().AnyAsync(x => x.ThirdType == 1 && x.DataType == 1 && x.EnabledMark == 1))
            {
                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.qyhDepartment)).Select(x => x.Id).ToListAsync();
                var pos = await _repository.AsSugarClient().Queryable<PositionEntity>().FirstAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && x.Id.Equals(input.objectId));
                if (pos.IsNotEmptyOrNull())
                {
                    var userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => input.userIds.Contains(x.Id)).ToListAsync();
                    foreach (var user in userList)
                    {
                        if (sysConfig.qyhIsSynUser)
                        {
                            user.OrganizeId = pos.OrganizeId;
                            var newList = new List<UserEntity>() { user };
                            await _synThirdInfoService.SynUser(1, 2, sysConfig, newList);
                        }
                        else
                        {
                            var synEntity = new SynThirdInfoEntity() { ThirdType = 1, DataType = 2, SysObjId = user.Id, EnabledMark = 0 };
                            await _repository.AsSugarClient().Insertable(synEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        #endregion

        addList = addList.Where(x => input.userIds.Contains(x.UserId)).ToList();

        return new UnifyResult.RESTfulResult<object>()
        {
            code = errorList.Any() && addList.Any() ? 200 : (!errorList.Any() ? 200 : 503),
            msg = errorList.Any() && addList.Any() ? "部分添加成功" : (!errorList.Any() ? "添加成功" : "添加失败")
        };
    }

    /// <summary>
    /// 移除关联用户.
    /// </summary>
    /// <param name="input">input.</param>
    /// <returns></returns>
    [HttpPost("Delete")]
    public async Task DeleteUser([FromBody] UserRelationCrInput input)
    {
        if (input.objectType.Equals(CommonConst.RelationObjectPosition))
        {
            var posEntity = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.Id.Equals(input.objectId)).FirstAsync();
            var posIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.OrganizeId.Equals(posEntity.OrganizeId)).Select(x => x.Id).ToListAsync();
            var reList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => input.userIds.Contains(x.UserId) && !x.ObjectType.Equals(CommonConst.RelationObjectGroup)).ToListAsync();
            var delIds = new List<string>();
            foreach (var userId in input.userIds)
            {
                if (reList.Count(x => x.UserId.Equals(userId) && posIds.Contains(x.ObjectId)) == 1)
                {
                    var del = reList.Where(x => x.UserId.Equals(userId) && x.ObjectId.Equals(posEntity.OrganizeId)).ToList();
                    delIds.AddRange(del.Select(x => x.Id));
                }
            }

            await _repository.AsSugarClient().Deleteable<UserRelationEntity>().Where(x => delIds.Contains(x.Id)).ExecuteCommandAsync();

            if (input.userIds.Contains(posEntity.DutyUserId))
            {
                posEntity.DutyUserId = null;
                await _repository.AsSugarClient().Updateable(posEntity).UpdateColumns(it => new { it.DutyUserId }).ExecuteCommandAsync();
            }
        }
        await _repository.AsSugarClient().Deleteable<UserRelationEntity>().Where(x => input.userIds.Contains(x.UserId) && x.ObjectId.Equals(input.objectId)).ExecuteCommandAsync();

        await ForcedOffline(input.userIds);

        if (input.objectType.Equals(CommonConst.RelationObjectPosition))
        {
            var userReList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => input.userIds.Contains(x.UserId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).ToListAsync();

            // 用户默认岗位处理
            var userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => input.userIds.Contains(x.Id)).ToListAsync();
            foreach (var item in userList)
            {
                if (!userReList.Any(x => x.UserId.Equals(item.Id) && x.ObjectId.Equals(item.PositionId)))
                {
                    item.PositionId = userReList.FirstOrDefault(x => x.UserId.Equals(item.Id))?.ObjectId;
                    item.OrganizeId = item.PositionId;
                    if (item.PositionId.IsNotEmptyOrNull())
                    {
                        item.OrganizeId = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.Id.Equals(item.PositionId)).Select(x => x.OrganizeId).FirstAsync();
                    }
                }
            }

            await _repository.AsSugarClient().Updateable(userList).UpdateColumns(it => new { it.PositionId, it.OrganizeId }).ExecuteCommandAsync();

            #region 第三方同步

            try
            {
                var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
                if (sysConfig.dingDepartment.IsNotEmptyOrNull())
                {
                    var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.dingDepartment)).Select(x => x.Id).ToListAsync();
                    var pos = await _repository.AsSugarClient().Queryable<PositionEntity>().FirstAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && x.Id.Equals(input.objectId));
                    if (pos.IsNotEmptyOrNull())
                    {
                        if (sysConfig.dingSynIsSynUser)
                            await _synThirdInfoService.SynUser(2, 2, sysConfig, userList);
                    }
                }

                if (sysConfig.qyhDepartment.IsNotEmptyOrNull())
                {
                    var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.qyhDepartment)).Select(x => x.Id).ToListAsync();
                    var pos = await _repository.AsSugarClient().Queryable<PositionEntity>().FirstAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && x.Id.Equals(input.objectId));
                    if (pos.IsNotEmptyOrNull())
                    {
                        if (sysConfig.qyhIsSynUser)
                            await _synThirdInfoService.SynUser(1, 2, sysConfig, userList);
                    }
                }
            }
            catch (Exception)
            {
            }

            #endregion
        }
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 创建用户关系.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="ids">对象ID组.</param>
    /// <param name="relationType">关系类型(岗位-Position;角色-Role;组织-Organize;分组-Group;).</param>
    /// <returns></returns>
    [NonAction]
    public List<UserRelationEntity> CreateUserRelation(string userId, string ids, string relationType)
    {
        List<UserRelationEntity> userRelationList = new List<UserRelationEntity>();
        if (!ids.IsNullOrEmpty())
        {
            List<string>? position = new List<string>(ids.Split(','));
            position.ForEach(item =>
            {
                UserRelationEntity? entity = new UserRelationEntity();
                entity.Id = SnowflakeIdHelper.NextId();
                entity.ObjectType = relationType;
                entity.ObjectId = item;
                entity.SortCode = position.IndexOf(item);
                entity.UserId = userId;
                entity.CreatorTime = DateTime.Now;
                entity.CreatorUserId = _userManager.UserId;
                userRelationList.Add(entity);
            });
        }

        return userRelationList;
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">用户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task Delete(string id)
    {
        await _repository.AsDeleteable().Where(u => u.UserId == id).ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据表达式获取列表.
    /// </summary>
    /// <param name="userId">用户主键.</param>
    /// <returns></returns>
    [NonAction]
    public List<UserRelationEntity> GetListByExp(Expression<Func<UserRelationEntity, bool>> expression)
    {
        return _repository.AsQueryable().Where(expression).ToList();
    }

    /// <summary>
    /// 获取用户(分页).
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [NonAction]
    public dynamic GetUserPage(UserConditionInput input)
    {
        SqlSugarPagedList<UserListOutput>? data = GetUserIdList(input).ToPagedList(input.pagination.currentPage, input.pagination.pageSize);
        if (data.list.Any())
        {
            var userIds = data.list.Select(x => x.id).ToList();
            #region 处理 用户 多岗位
            List<UserRelationEntity>? orgUserIdAll = _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => userIds.Contains(x.UserId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).ToList();
            var posList = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => orgUserIdAll.Select(xx => xx.ObjectId).Contains(x.Id)).ToList();
            var orgList = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => posList.Select(xx => xx.OrganizeId).Contains(x.Id)).ToList();
            foreach (var item in posList)
            {
                item.FullName = string.Format("{0}/{1}", orgList.Find(x => x.Id.Equals(item.OrganizeId)).OrganizeNameTree, item.FullName);
            }
            foreach (UserListOutput? item in data.list)
            {
                // 获取用户岗位集合
                item.organize = string.Join(" ; ", posList.Where(x => orgUserIdAll.Where(xx => xx.UserId.Equals(item.id)).Select(xx => xx.ObjectId).Contains(x.Id)).Select(x => x.FullName).ToList());
            }
            #endregion
        }
        return PageResult<UserListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取用户(分页,userIds纯用户id).
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public dynamic GetUserListPage(UserConditionInput input)
    {
        SqlSugarPagedList<UserListOutput>? data = new SqlSugarPagedList<UserListOutput>();
        data = _repository.AsSugarClient().Queryable<UserEntity>()
       .Where(a => a.DeleteMark == null && a.EnabledMark == 1 && input.userIds.Contains(a.Id))
       .WhereIF(input.pagination.keyword.IsNotEmptyOrNull(), a => a.Account.Contains(input.pagination.keyword) || a.RealName.Contains(input.pagination.keyword))
       .Select(a => new UserListOutput()
       {
           id = a.Id,
           organizeId = a.OrganizeId,
           account = a.Account,
           fullName = SqlFunc.MergeString(a.RealName, "/", a.Account),
           headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", a.HeadIcon),
           gender = a.Gender,
           mobilePhone = a.MobilePhone
       }).ToPagedList(input.pagination.currentPage, input.pagination.pageSize);

        if (data.list.Any())
        {
            var userIds = data.list.Select(x => x.id).ToList();
            #region 处理 用户 多岗位
            List<UserRelationEntity>? orgUserIdAll = _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => userIds.Contains(x.UserId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).ToList();
            var posList = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => orgUserIdAll.Select(xx => xx.ObjectId).Contains(x.Id)).ToList();
            var orgList = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => posList.Select(xx => xx.OrganizeId).Contains(x.Id)).ToList();
            foreach (var item in posList)
            {
                item.FullName = string.Format("{0}/{1}", orgList.Find(x => x.Id.Equals(item.OrganizeId)).OrganizeNameTree, item.FullName);
            }
            foreach (UserListOutput? item in data.list)
            {
                // 获取用户岗位集合
                item.organize = string.Join(" ; ", posList.Where(x => orgUserIdAll.Where(xx => xx.UserId.Equals(item.id)).Select(xx => xx.ObjectId).Contains(x.Id)).Select(x => x.FullName).ToList());
            }
            #endregion
        }

        return PageResult<UserListOutput>.SqlSugarPageResult(data);
    }

    [NonAction]
    public List<UserListOutput> GetUserList(UserConditionInput input)
    {
        List<UserListOutput>? data = GetUserIdList(input).ToList();
        if (data.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // 获取所属组织的所有成员
            List<UserRelationEntity>? userList = _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && data.Select(x => x.id).Contains(x.UserId)).ToList();

            // 处理组织树
            data.ToList().ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });

        }

        return data;
    }

    /// <summary>
    /// 新用户组件获取人员.
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
            if (item.Contains("--"))
            {
                var strList = item.Split("--").ToList();
                var objId = strList[0];
                var type = strList[1];
                if (type == "subOrg")
                {
                    var ids = _repository.AsSugarClient().Queryable<OrganizeEntity>()
                       .Where(x => x.ParentId.Equals(objId))
                       .Select(x => x.Id).ToList();
                    objIdList.AddRange(ids);
                }
                if (type == "progenyOrg")
                {
                    var idTreeList = _repository.AsSugarClient().Queryable<OrganizeEntity>()
                        .Where(x => x.OrganizeIdTree.Contains(objId))
                        .Select(x => x.Id).ToList();
                    objIdList.AddRange(idTreeList);
                }
                if (type == "subPos")
                {
                    var ids = _repository.AsSugarClient().Queryable<PositionEntity>()
                       .Where(x => x.ParentId.Equals(objId))
                       .Select(x => x.Id).ToList();
                    objIdList.AddRange(ids);
                }
                if (type == "progenyPos")
                {
                    var idTreeList = _repository.AsSugarClient().Queryable<PositionEntity>()
                        .Where(x => x.PositionIdTree.Contains(objId))
                        .Select(x => x.Id).ToList();
                    objIdList.AddRange(idTreeList);
                }
                if (type == "role")
                {
                    var ids = _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.RoleId == objId).Select(x => x.ObjectId).ToList();
                    objIdList.AddRange(ids);
                }
                objIdList.Add(objId);
            }
            else
            {
                if (item == "@userId")
                {
                    objIdList.Add(_userManager.UserId);
                }
                else
                {
                    var ids = _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.RoleId == item).Select(x => x.ObjectId).ToList();
                    objIdList.AddRange(ids);
                    objIdList.Add(item);
                }
            }
        }
        var userIds = _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.UserId == b.Id))
              .Where((a, b) => (objIdList.Contains(a.ObjectId) || objIdList.Contains(a.UserId)) && b.DeleteMark == null && b.EnabledMark == 1).Select(a => a.UserId).Distinct().ToList();
        if (objIdList.Contains(adminId))
        {
            userIds.Add(adminId);
        }
        return userIds;
    }

    /// <summary>
    /// 获取控件用户。
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private ISugarQueryable<UserListOutput> GetUserIdList(UserConditionInput input)
    {
        input.userIds = GetUserId(input.userIds);
        var objIds = new List<string>();
        var filterUserId = input.userId.IsNotEmptyOrNull() ? input.userId : _userManager.UserId; // 过滤基准人员.
        switch (input.type)
        {
            case "2":
                objIds = _repository.AsSugarClient().Queryable<UserRelationEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.ObjectId == b.Id))
               .Where((a, b) => a.UserId == filterUserId && a.ObjectType == CommonConst.RelationObjectOrganize && b.Category == CommonConst.OrganizeTypeDepartment && b.EnabledMark == 1 && b.DeleteMark == null).Select((a, b) => b.Id).ToList();
                break;
            case "3":
                objIds = _userManager.GetUserPositionList(filterUserId);
                break;
            case "4":
                input.userIds = _userManager.GetSupervisor(filterUserId).Intersect(input.userIds).ToList();
                objIds = null;
                break;
            case "5":
                input.userIds = _userManager.GetSubordinatesAllPos(filterUserId, true).Intersect(input.userIds).ToList();
                objIds = null;
                break;
            case "6":
                var orgEntitList = _repository.AsSugarClient().Queryable<UserRelationEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.ObjectId == b.Id))
              .Where((a, b) => a.UserId == filterUserId && a.ObjectType == CommonConst.RelationObjectOrganize && !(b.Category == CommonConst.OrganizeTypeBase || b.Category == CommonConst.OrganizeTypeInstitution || b.Category == CommonConst.OrganizeTypeOrgan) && b.EnabledMark == 1 && b.DeleteMark == null).Select((a, b) => b).ToList();
                foreach (var item in orgEntitList)
                {
                    var companyId = item.Category == CommonConst.OrganizeTypeDepartment ? _departmentService.GetCompanyId(item.Id) : item.Id;
                    var orgIds = _departmentService.GetCompanyAllDep(companyId, true).Select(x => x.Id).Distinct().ToList();
                    orgIds.Add(companyId);
                    objIds.AddRange(orgIds);
                }
                if (!objIds.Any())
                    objIds = null;
                break;
            case "7":
                var roleIds = _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectType == CommonConst.RoleCodeTypeUser && x.ObjectId == filterUserId).Select(x => x.RoleId).ToList();
                input.userIds = _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectType == CommonConst.RoleCodeTypeUser && roleIds.Contains(x.RoleId)).Select(x => x.ObjectId).ToList().Intersect(input.userIds).ToList();
                objIds = null;
                break;
            case "8":
                objIds = _repository.AsQueryable().Where(a => a.UserId == filterUserId && a.ObjectType == CommonConst.RelationObjectGroup).Select(a => a.ObjectId).ToList();
                break;
            default:
                objIds = null;
                break;
        }
        return _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.UserId))
        .Where((a, b) => b.DeleteMark == null && b.EnabledMark == 1)
        .Where((a, b) => input.userIds.Contains(a.UserId))
        .WhereIF(objIds.IsNotEmptyOrNull(), (a, b) => objIds.Contains(a.ObjectId))
        .WhereIF(input.pagination.keyword.IsNotEmptyOrNull(), (a, b) => b.Account.Contains(input.pagination.keyword) || b.RealName.Contains(input.pagination.keyword))
        .GroupBy((a, b) => new { Id = b.Id, Account = b.Account, RealName = b.RealName, Gender = b.Gender, MobilePhone = b.MobilePhone })
        .Select((a, b) => new UserListOutput()
        {
            id = b.Id,
            organizeId = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(e => e.Id == b.Id).Select(u => u.OrganizeId),
            account = b.Account,
            fullName = SqlFunc.MergeString(b.RealName, "/", b.Account),
            headIcon = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(e => e.Id == b.Id).Select(u => SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon)),
            gender = b.Gender,
            mobilePhone = b.MobilePhone
        });
    }

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
    #endregion
}