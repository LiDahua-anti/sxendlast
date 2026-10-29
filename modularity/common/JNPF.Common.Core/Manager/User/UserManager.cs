using JNPF.Common.Const;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.Authorize;
using JNPF.Common.Models.User;
using JNPF.Common.Models.VisualDev;
using JNPF.Common.Net;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Entity.Permission;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using NPOI.Util;
using SqlSugar;
using SqlSugar.Extensions;
using System.Security.Claims;

namespace JNPF.Common.Core.Manager;

/// <summary>
/// 当前登录用户.
/// </summary>
public class UserManager : IUserManager, IScoped
{
    /// <summary>
    /// 用户表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _repository;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 当前Http请求.
    /// </summary>
    private readonly HttpContext _httpContext;

    /// <summary>
    /// 用户Claim主体.
    /// </summary>
    private readonly ClaimsPrincipal _user;

    /// <summary>
    /// 初始化一个<see cref="UserManager"/>类型的新实例.
    /// </summary>
    /// <param name="repository">用户仓储.</param>
    /// <param name="cacheManager">缓存管理.</param>
    public UserManager(
        ISqlSugarRepository<UserEntity> repository,
        ICacheManager cacheManager)
    {
        _repository = repository;
        _cacheManager = cacheManager;
        _httpContext = App.HttpContext;
        _user = _httpContext?.User;
    }

    #region 请求属性
    /// <summary>
    /// 用户ID.
    /// </summary>
    public string UserId
    {
        get => _user.FindFirst(ClaimConst.CLAINMUSERID)?.Value;
    }

    /// <summary>
    /// 用户账号.
    /// </summary>
    public string Account
    {
        get => _user.FindFirst(ClaimConst.CLAINMACCOUNT)?.Value;
    }

    /// <summary>
    /// 用户昵称.
    /// </summary>
    public string RealName
    {
        get => _user.FindFirst(ClaimConst.CLAINMREALNAME)?.Value;
    }

    /// <summary>
    /// 租户ID.
    /// </summary>
    public string TenantId
    {
        get => _user.FindFirst(ClaimConst.TENANTID)?.Value;
    }

    /// <summary>
    /// 当前用户 token.
    /// </summary>
    public string ToKen
    {
        get => string.IsNullOrEmpty(App.HttpContext?.Request.Headers["Authorization"]) ? App.HttpContext?.Request.Query["token"] : App.HttpContext?.Request.Headers["Authorization"];
    }

    /// <summary>
    /// 获取请求端类型 pc 、 app.
    /// </summary>
    public string UserOrigin
    {
        get => _httpContext?.Request.Headers["jnpf-origin"];
    }

    /// <summary>
    /// 应用编码.
    /// </summary>
    public string AppCode
    {
        get => _httpContext?.Request?.Headers["App-Code"].ToString();
    }

    /// <summary>
    /// 当前pc/app身份.
    /// </summary>
    public string? Standing
    {
        get
        {
            if (_standing == null) _standing = !"pc".Equals(UserOrigin) ? User.AppStanding : User.Standing;
            return _standing;
        }
    }

    /// <summary>
    /// 获取请求vue版本
    /// 3-vue3,其他-vue2.
    /// </summary>
    public int VueVersion
    {

        get => (_httpContext?.Request.Headers["vue-version"].FirstOrDefault()).ParseToInt();
    }

    /// <summary>
    /// 当前是否主应用.
    /// </summary>
    public bool IsMainSystem
    {
        get => UserOrigin == "pc" && AppCode.IsNullOrEmpty();
    }
    #endregion

    #region 租户、缓存
    /// <summary>
    /// 租户数据库名称.
    /// </summary>
    public string TenantDbName
    {
        get
        {
            var tenant = GetGlobalTenantCache(TenantId);
            if (tenant == null) return null;
            else return tenant.connectionConfig.ConfigList.FirstOrDefault().ServiceName;
        }
    }

    /// <summary>
    /// 当前租户配置.
    /// </summary>
    public GlobalTenantCacheModel CurrentTenantInformation
    {
        get => GetGlobalTenantCache(TenantId);
    }

    /// <summary>
    /// 多租户场景忽略过滤的应用和菜单Ids.
    /// </summary>
    public List<string>? TenantIgnoreModuleIdList
    {
        get => GetGlobalTenantCache().Where(x => x.TenantId.Equals(TenantId)).FirstOrDefault()?.moduleIdList;
    }

    /// <summary>
    /// 多租户场景忽略过滤的应用和菜单urlList.
    /// </summary>
    public List<string>? TenantIgnoreUrlAddressList
    {
        get
        {
            var urlAddressList = GetGlobalTenantCache().Where(x => x.TenantId.Equals(TenantId)).FirstOrDefault()?.urlAddressList;
            if (TenantIgnoreModuleIdList.IsNotEmptyOrNull() && urlAddressList == null) return new List<string>();
            return urlAddressList;
        }
    }

    /// <summary>
    /// 获取公用菜单 编码 .
    /// </summary>
    /// <returns></returns>
    public List<string> CommonModuleEnCodeList
    {
        get
        {
            var list = new List<string> { "workFlow.flowQuickLaunch", "workFlow.addFlow", "workFlow.flowLaunch", "workFlow.entrust", "workFlow", "workFlow.flowTodo", "workFlow.flowDone", "workFlow.flowCirculate", "workFlow.flowToSign", "workFlow.flowDoing", "workFlow.schedule", "workFlow.document", "workFlow.printTemplate" };
            if (_repository.AsSugarClient().Queryable<SysConfigEntity>().Any(s => s.Category.Equals("SysConfig") && SqlFunc.ToString(s.Value) == "0" && s.Key.Equals("flowSign")))
            {
                list.Remove("workFlow.flowToSign");
            }
            if (_repository.AsSugarClient().Queryable<SysConfigEntity>().Any(s => s.Category.Equals("SysConfig") && SqlFunc.ToString(s.Value) == "0" && s.Key.Equals("flowTodo")))
            {
                list.Remove("workFlow.flowTodo");
            }
            return list;
        }
    }

    /// <summary>
    /// 获取全局租户缓存.
    /// </summary>
    /// <returns></returns>
    public List<GlobalTenantCacheModel> GetGlobalTenantCache()
    {
        string cacheKey = string.Format("{0}", CommonConst.GLOBALTENANT);
        var list = _cacheManager.Get<List<GlobalTenantCacheModel>>(cacheKey);
        return list != null ? list : new List<GlobalTenantCacheModel>();
    }
    #endregion

    #region 当前用户属性
    /// <summary>
    /// 用户信息.
    /// </summary>
    public UserEntity User
    {
        get
        {
            if (_userEntity == null) _userEntity = _repository.GetSingle(u => u.Id == UserId);
            return _userEntity;
        }
    }

    /// <summary>
    /// 是否是管理员.
    /// </summary>
    public bool IsAdministrator
    {
        get => User.IsAdministrator.Equals(1) && (Standing == null || Standing.Equals("1"));
    }

    /// <summary>
    /// 是否是管理员角色.
    /// </summary>
    public bool? IsManageRole
    {
        get
        {
            if (_isManageRole == null) _isManageRole = IsRole(CommonConst.RoleCodeTypeManager);
            return _isManageRole;
        }
    }

    /// <summary>
    /// 是否是开发者角色.
    /// </summary>
    public bool? IsDevRole
    {
        get
        {
            if (_isDevRole == null) _isDevRole = IsRole(CommonConst.RoleCodeTypeDeveloper);
            return _isDevRole;
        }
    }

    /// <summary>
    /// 是否所有身份包含开发者角色.
    /// </summary>
    public bool? IsAllStandingDevRole
    {
        get
        {
            if (_isAllStandingDevRole == null) _isAllStandingDevRole = IsRole(CommonConst.RoleCodeTypeDeveloper, false);
            return _isAllStandingDevRole;
        }
    }

    /// <summary>
    /// 是否是使用者角色.
    /// </summary>
    public bool? IsUserRole
    {
        get
        {
            if (_isUserRole == null) _isUserRole = IsRole(CommonConst.RoleCodeTypeUser);
            return _isUserRole;
        }
    }

    /// <summary>
    /// 是否是其他角色.
    /// </summary>
    public bool? IsOtherRole
    {
        get
        {
            if (_isOtherRole == null) _isOtherRole = IsRole(string.Empty);
            return _isOtherRole;
        }
    }

    /// <summary>
    /// 是否有后台权限.
    /// </summary>
    public bool? HasBackend
    {
        get
        {
            if (_hasBackend == null) _hasBackend = UserHasBackend();
            return _hasBackend;
        }
    }

    /// <summary>
    /// 获取请求端应用 id.
    /// </summary>
    public string SystemId
    {
        get
        {
            if (_systemId == null) _systemId = GetSystemIdByCode();
            return _systemId;
        }
    }

    /// <summary>
    /// 当前用户权限Ids (用户id、角色ids、岗位ids).
    /// </summary>
    public List<string> PermissionIds
    {
        get
        {
            if (_permissionIds == null) _permissionIds = GetPermissionIds(UserId);
            return _permissionIds;
        }
    }

    /// <summary>
    /// 当前用户及下属.
    /// </summary>
    public List<string> CurrentUserAndSubordinates
    {
        get
        {
            if (_currentUserAndSubordinates == null)
            {
                _currentUserAndSubordinates = new List<string> { UserId };
                _currentUserAndSubordinates.AddRange(GetSubordinates(UserId).ToList());
            }
            return _currentUserAndSubordinates;
        }
    }

    /// <summary>
    /// 当前用户及子孙下属.
    /// </summary>
    public List<string> CurrentUserTree
    {
        get
        {
            if (_currentUserTree == null)
            {
                _currentUserTree = new List<string> { UserId };
                _currentUserTree.AddRange(GetSubordinates(UserId, true).ToList());
            }
            return _currentUserTree;
        }
    }

    /// <summary>
    /// 当前用户所属组织(所有).
    /// </summary>
    public string CurrentUserOrganizeList
    {
        get
        {
            if (_currentOrganizeList == null) _currentOrganizeList = GetUserOrganizeList(UserId);
            return _currentOrganizeList.ToJsonStringOld();
        }
    }

    /// <summary>
    /// 当前用户所属组织(所有).
    /// </summary>
    public List<string> CurrentOrganizeList
    {
        get
        {
            if (_currentOrganizeList == null) _currentOrganizeList = GetUserOrganizeList(UserId);
            return _currentOrganizeList;
        }
    }

    /// <summary>
    /// 当前组织及子组织(所有).
    /// </summary>
    public List<string> CurrentOrganizationAndSubOrganizations
    {
        get
        {
            if (_currentOrganizationAndSubOrganizations == null) _currentOrganizationAndSubOrganizations = GetUserOrganizeAndSuborganization(UserId);
            return _currentOrganizationAndSubOrganizations;
        }
    }

    /// <summary>
    /// 当前组织及子孙组织(所有).
    /// </summary>
    public List<string> CurrentOrganizationTree
    {
        get
        {
            if (_currentOrganizationTree == null) _currentOrganizationTree = GetUserOrganizeTree(UserId);
            return _currentOrganizationTree;
        }
    }

    /// <summary>
    /// 当前用户所属岗位(所有).
    /// </summary>
    public string CurrentPositionList
    {
        get
        {
            if (_currentPositionList == null) _currentPositionList = GetUserPositionList(UserId).ToJsonStringOld();
            return _currentPositionList;
        }
    }

    /// <summary>
    /// 当前所属岗位及子岗位(所有).
    /// </summary>
    public string CurrentPositionAndSuborganization
    {
        get
        {
            if (_currentPositionAndSuborganization == null) _currentPositionAndSuborganization = GetUserPositionAndSuborganization(UserId).ToJsonStringOld();
            return _currentPositionAndSuborganization;
        }
    }

    /// <summary>
    /// 当前所属岗位及子孙岗位(所有).
    /// </summary>
    public string CurrentPositionTree
    {
        get
        {
            if (_currentPositionTree == null) _currentPositionTree = GetUserPositionTree(UserId).ToJsonStringOld();
            return _currentPositionTree;
        }
    }

    /// <summary>
    /// 直属主管.
    /// </summary>
    public List<string> CurrentUserSupervisor
    {
        get
        {
            if (_currentUserSupervisor == null) _currentUserSupervisor = GetSupervisor(UserId);
            return _currentUserSupervisor;
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取用户登录信息.
    /// </summary>
    /// <returns></returns>
    public async Task<UserInfoModel> GetUserInfo()
    {
        UserAgent userAgent = new UserAgent(_httpContext);
        var data = new UserInfoModel();
        var userCache = string.Format("{0}:{1}:{2}", TenantId, CommonConst.CACHEKEYUSER, UserId);

        var ipAddress = NetHelper.Ip;
        var ipAddressName = await NetHelper.GetLocation(ipAddress);
        var sysConfigInfo = await _repository.AsSugarClient().Queryable<SysConfigEntity>().FirstAsync(s => s.Category.Equals("SysConfig") && s.Key.ToLower().Equals("tokentimeout"));
        _userEntity = null;
        _standing = null;
        _systemId = null;
        _permissionIds = null;
        data = await _repository.AsQueryable().Where(it => it.Id == UserId)
           .Select(a => new UserInfoModel
           {
               userId = a.Id,
               headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", a.HeadIcon),
               userAccount = a.Account,
               userName = a.RealName,
               gender = a.Gender,
               organizeId = a.OrganizeId,
               departmentId = a.OrganizeId,
               departmentName = SqlFunc.Subqueryable<OrganizeEntity>().EnableTableFilter().Where(o => o.Id == a.OrganizeId && o.Category.Equals("department")).Select(o => o.FullName),
               organizeName = SqlFunc.Subqueryable<OrganizeEntity>().EnableTableFilter().Where(o => o.Id == a.OrganizeId).Select(o => o.OrganizeNameTree),
               managerId = a.ManagerId,
               isAdministrator = SqlFunc.IIF(a.IsAdministrator == 1, true, false),
               positionId = a.PositionId,
               prevLoginTime = a.PrevLogTime,
               prevLoginIPAddress = SqlFunc.IIF(a.PrevLogIP != null, a.PrevLogIP, "127.0.0.1"),
               landline = a.Landline,
               telePhone = a.TelePhone,
               manager = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.ManagerId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
               mobilePhone = a.MobilePhone,
               email = a.Email,
               birthday = a.Birthday,
               signImg = SqlFunc.Subqueryable<SignImgEntity>().EnableTableFilter().Where(a => a.CreatorUserId == UserId && a.IsDefault == 1).Select(a => a.SignImg),
               signId = SqlFunc.Subqueryable<SignImgEntity>().EnableTableFilter().Where(a => a.CreatorUserId == UserId && a.IsDefault == 1).Select(a => a.Id),
               changePasswordDate = a.ChangePasswordDate,
               loginTime = DateTime.Now,
               standing = a.Standing,
               appStanding = a.AppStanding,
               portalId = a.PortalId
           }).FirstAsync();

        if (data != null)
        {
            var orgList = await _repository.AsSugarClient().Queryable<OrganizeEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id.Equals(b.ObjectId) && b.ObjectType.Equals(CommonConst.RelationObjectOrganize)))
                .Where((a, b) => b.UserId.Equals(UserId)).Select(a => new {
                    id = a.Id,
                    fullName = a.FullName,
                    treeName = a.OrganizeNameTree,
                    treeId = a.OrganizeIdTree,
                    icon = SqlFunc.IIF(a.Category.Equals(CommonConst.OrganizeTypeGroup) || a.Category.Equals(CommonConst.OrganizeTypeInstitution), "icon-ym icon-ym-tree-organization3", "icon-ym icon-ym-tree-department1"),
                }).ToListAsync();
            if (data.organizeId != null && orgList.Select(it => it.id).Contains(data.organizeId))
            {
                var mainPosition = orgList.Find(it => it.id.Equals(data.organizeId));
                orgList.Remove(mainPosition);
                orgList.Insert(0, mainPosition);
            }
            data.organizeList = orgList;
            data.organizeIds = orgList.Select(x => x.id).ToList();
        }
        else
        {
            data.organizeIds = new List<string>();
            data.organizeList = new List<object>();
        }
        data.prevLogin = (await _repository.AsSugarClient().Queryable<SysConfigEntity>().FirstAsync(x => x.Category.Equals("SysConfig") && x.Key.ToLower().Equals("lastlogintimeswitch"))).Value.ParseToInt();
        data.loginIPAddress = ipAddress;
        data.loginIPAddressName = ipAddressName;
        data.prevLoginIPAddressName = await NetHelper.GetLocation(data.prevLoginIPAddress);
        data.loginPlatForm = userAgent.RawValue;
        data.preferenceJson = await _repository.AsSugarClient().Queryable<UserExtraEntity>().Where(x => x.UserId.Equals(UserId)).Select(x => x.PreferenceJson).FirstAsync();

        if (data != null)
        {
            var positionIdList = await _repository.AsSugarClient().Queryable<PositionEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id.Equals(b.ObjectId) && b.ObjectType.Equals(CommonConst.RelationObjectPosition)))
                .Where((a, b) => b.UserId.Equals(UserId)).Select(a => new {
                    id = a.Id,
                    fullName = a.FullName,
                    treeName = SqlFunc.MergeString(SqlFunc.Subqueryable<OrganizeEntity>().Where(o => o.Id.Equals(a.OrganizeId)).Select(x => x.OrganizeNameTree), "/", a.FullName),
                    icon = "icon-ym icon-ym-tree-position1",
                }).ToListAsync();
            if (data.positionId != null && positionIdList.Select(it => it.id).Contains(data.positionId))
            {
                var mainPosition = positionIdList.Find(it => it.id.Equals(data.positionId));
                positionIdList.Remove(mainPosition);
                positionIdList.Insert(0, mainPosition);
            }
            if (positionIdList.Any()) data.positionName = positionIdList.FirstOrDefault().fullName;
            data.positionList = positionIdList;
            data.positionIds = positionIdList.Select(x => x.id).ToList();
        }
        else
        {
            data.positionIds = new List<string>();
            data.positionList = new List<object>();
        }

        data.groupIds = await _repository.AsSugarClient().Queryable<GroupEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id.Equals(b.ObjectId) && b.ObjectType.Equals(CommonConst.RelationObjectGroup))).Where((a, b) => a.EnabledMark == 1 && a.DeleteMark == null && b.UserId.Equals(data.userId)).Select((a, b) => b.ObjectId).ToListAsync();
        data.groupNames = await _repository.AsSugarClient().Queryable<GroupEntity>().Where(it => data.groupIds.Contains(it.Id)).Select(x => x.FullName).ToListAsync();
        data.overdueTime = TimeSpan.FromMinutes(sysConfigInfo.Value.ParseToDouble());
        if (data.isAdministrator)
        {
            var addItem = new UserStanding() { id = "1", name = "超级管理员", icon = "icon-ym icon-ym-id-admin" };
            if (data.standing == null)
            {
                addItem.currentStanding = true;
                await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => new UserEntity() { Standing = "1" }).Where(x => x.Id.Equals(UserId)).ExecuteCommandAsync();
                _userEntity = _repository.GetSingle(u => u.Id == UserId);
                data.standing = "1";
            }

            if (data.appStanding == null)
            {
                addItem.currentStanding = true;
                await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => new UserEntity() { AppStanding = "1" }).Where(x => x.Id.Equals(UserId)).ExecuteCommandAsync();
                _userEntity = _repository.GetSingle(u => u.Id == UserId);
                data.appStanding = "1";
            }

            addItem.currentStanding = UserOrigin.Equals("pc") ? data.standing.Equals("1") : data.appStanding.Equals("1");
            data.standingList.Add(addItem);
        }
        else
        {
            var roleIds = GetPermissionIds(UserId, false);
            var standingIds = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(x => x.ItemType.Equals(CommonConst.AuthorizeItemStanding) && roleIds.Contains(x.ObjectId)).Select(x => x.ItemId).Distinct().ToListAsync();
            var standingList = await _repository.AsSugarClient().Queryable<StandingEntity>().Where(x => standingIds.Contains(x.Id)).Select(x => new UserStanding()
            {
                id = x.Id,
                name = x.FullName
            }).ToListAsync();

            if (standingList.Any())
            {
                if (data.standing == null || !standingList.Any(x => x.id.Equals(data.standing)))
                {
                    standingList.First().currentStanding = true;
                    await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => new UserEntity() { Standing = standingList.First().id }).Where(x => x.Id.Equals(UserId)).ExecuteCommandAsync();
                    _userEntity = _repository.GetSingle(u => u.Id == UserId);
                    data.standing = standingList.First().id;
                }

                if (data.appStanding == null || !standingList.Any(x => x.id.Equals(data.appStanding)))
                {
                    standingList.First().currentStanding = true;
                    await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => new UserEntity() { AppStanding = standingList.First().id }).Where(x => x.Id.Equals(UserId)).ExecuteCommandAsync();
                    _userEntity = _repository.GetSingle(u => u.Id == UserId);
                    data.appStanding = standingList.First().id;
                }

                foreach (var item in standingList)
                {
                    if (UserOrigin.Equals("pc") && item.id.Equals(data.standing))
                    {
                        item.currentStanding = true;
                        break;
                    }
                    if (!UserOrigin.Equals("pc") && item.id.Equals(data.appStanding))
                    {
                        item.currentStanding = true;
                        break;
                    }
                }

                data.standingList.AddRange(standingList);
            }
            else
            {
                if (UserOrigin.Equals("pc") && data.standing != null)
                {
                    await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => new UserEntity() { Standing = null }).Where(x => x.Id.Equals(UserId)).ExecuteCommandAsync();
                    _userEntity = _repository.GetSingle(u => u.Id == UserId);
                }
                if (!UserOrigin.Equals("pc") && data.appStanding != null)
                {
                    await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => new UserEntity() { AppStanding = null }).Where(x => x.Id.Equals(UserId)).ExecuteCommandAsync();
                    _userEntity = _repository.GetSingle(u => u.Id == UserId);
                }
            }

            if (UserOrigin == "app")
            {
                var mIds = await GetUserAuthorizeItemIdList(CommonConst.AuthorizeItemModule);
                var sIds = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => mIds.Contains(x.Id) && x.Type != 2).Select(x => x.SystemId).ToListAsync();
                if (SystemId.IsNullOrEmpty() || !sIds.Any(x => x.Equals(SystemId)))
                {
                    await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => x.AppSystemId == sIds.FirstOrDefault()).Where(x => x.Id.Equals(UserId)).ExecuteCommandAsync();
                    _userEntity = _repository.GetSingle(u => u.Id == UserId);
                }
            }
        }

        // 门户
        if (SystemId.IsNotEmptyOrNull())
        {
            var oldPortalId = data.portalId;
            var wKey = string.Format("Web:{0}", SystemId);
            var aKey = string.Format("App:{0}", SystemId);
            var newPortalIdDic = await GetPortalIdDic(oldPortalId, wKey, aKey);
            data.portalId = newPortalIdDic.ContainsKey(wKey) ? newPortalIdDic[wKey] : string.Empty;
            data.appPortalId = newPortalIdDic.ContainsKey(aKey) ? newPortalIdDic[aKey] : string.Empty;
            var newPortalId = newPortalIdDic.ToJsonString();
            if (!newPortalId.Equals(oldPortalId))
                await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(x => x.PortalId == newPortalId).Where(x => x.Id.Equals(UserId)).ExecuteCommandAsync();
        }

        data.tenantId = TenantId;
        data.systemId = SystemId;
        if (data.systemId.IsNotEmptyOrNull())
        {
            var sysInfo = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => x.Id.Equals(data.systemId)).FirstAsync();
            data.systemCode = sysInfo.EnCode;
            data.systemName = sysInfo.FullName;
            data.systemColor = sysInfo.BackgroundColor;
            data.systemIcon = sysInfo.Icon;
        }

        data.isManageRole = IsManageRole.Value;
        data.isDevRole = IsDevRole.Value;
        data.isUserRole = IsUserRole.Value;
        data.isOtherRole = IsOtherRole.Value;
        data.hasBackend = HasBackend.Value;

        // 根据系统配置过期时间自动过期
        await SetUserInfo(userCache, data, TimeSpan.FromMinutes(sysConfigInfo.Value.ParseToDouble()));

        return data;
    }

    /// <summary>
    /// 获取数据条件.
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <param name="primaryKeyPolicy">是否自增长Id.</param>
    /// <returns></returns>
    public async Task<List<IConditionalModel>> GetConditionAsync<T>(string moduleId, string primaryKey = "f_id", bool isDataPermissions = true, int primaryKeyPolicy = 1)
        where T : new()
    {
        var resourceList = await GetCodeGenAuthorizeModuleResource<T>(moduleId, primaryKey, primaryKeyPolicy, isDataPermissions);
        var res = new List<IConditionalModel>();
        foreach (var item in resourceList) res.AddRange(item.conditionalModel);
        return res;
    }

    /// <summary>
    /// 获取代码生成数据条件 .
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="primaryKeyPolicy">是否自增长Id.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <returns></returns>
    public async Task<List<CodeGenAuthorizeModuleResourceModel>> GetCodeGenAuthorizeModuleResource<T>(string moduleId, string primaryKey, int primaryKeyPolicy, bool isDataPermissions = true)
        where T : new()
    {
        var codeGenConditional = new List<CodeGenAuthorizeModuleResourceModel>();

        // 获取所有数据权限的 表名
        var resourceList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(it => it.ModuleId == moduleId && it.DeleteMark == null).ToListAsync();

        var allTableName = new Dictionary<string, string>();
        foreach (var resourceItem in resourceList)
        {
            if (resourceItem != null && resourceItem.ConditionJson != null && resourceItem.ConditionJson.Any())
            {
                var items = resourceItem.ConditionJson.ToObject<AuthorizeModuleResourceConditionModel>();
                items.conditionList.ForEach(it =>
                {
                    it.groups.ForEach(fItem =>
                    {
                        if (fItem.field.Contains(string.Format("jnpf_{0}_jnpf_", fItem.tableName))) allTableName[fItem.tableName] = string.Format("jnpf_{0}_jnpf_", fItem.tableName);
                        else if (fItem.field.ToLower().Contains("tablefield") && fItem.field.Contains("-")) allTableName[fItem.tableName] = fItem.field.Split("-").First() + "-";
                        else allTableName[fItem.tableName] = null;
                    });
                });
            }
        }

        var condList = await GetDataPermission(primaryKey, moduleId, isDataPermissions, primaryKeyPolicy.Equals(2));

        foreach (var tName in allTableName)
        {
            var tNameConditional = GetIConditionalModelListByTableName(condList.Copy(), tName.Value);
            if (tNameConditional.Any())
            {
                if (tName.Value.IsNotEmptyOrNull())
                    tNameConditional = _repository.AsSugarClient().Utilities.JsonToConditionalModels(tNameConditional.ToJsonString().Replace(tName.Value, ""));
                codeGenConditional.Add(new CodeGenAuthorizeModuleResourceModel()
                {
                    conditionalModel = tNameConditional,
                    TableName = tName.Key,
                    FieldRule = -1
                });
            }
        }

        return codeGenConditional;
    }

    /// <summary>
    /// 所属机构(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    public List<string> GetUserOrganizeList(string userId)
    {
        return _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectOrganize)).Select(x => x.ObjectId).ToList();
    }

    /// <summary>
    /// 所属组织及子组织(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    public List<string> GetUserOrganizeAndSuborganization(string userId)
    {
        var pIds = _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectOrganize)).Select(x => x.ObjectId).ToList();
        var res = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => pIds.Contains(x.ParentId)).Select(x => x.Id).ToList();
        pIds.AddRange(res);

        return pIds;
    }

    /// <summary>
    /// 所属组织及子孙组织(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    public List<string> GetUserOrganizeTree(string userId)
    {
        var pIds = _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectOrganize)).Select(x => x.ObjectId).ToList();
        var querList = LinqExpression.Or<OrganizeEntity>();
        pIds.ForEach(item => querList = querList.Or(x => x.OrganizeIdTree.Contains(item)));
        var res = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(querList).Select(x => x.Id).ToList();
        return res;
    }

    /// <summary>
    /// 所属岗位(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    public List<string> GetUserPositionList(string userId)
    {
        return _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).Select(x => x.ObjectId).ToList();
    }

    /// <summary>
    /// 所属岗位及子岗位(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    public List<string> GetUserPositionAndSuborganization(string userId)
    {
        var pIds = _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).Select(x => x.ObjectId).ToList();
        var res = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => pIds.Contains(x.ParentId)).Select(x => x.Id).ToList();
        pIds.AddRange(res);
        return pIds;
    }

    /// <summary>
    /// 所属岗位及子孙岗位(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    public List<string> GetUserPositionTree(string userId)
    {
        var pIds = _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).Select(x => x.ObjectId).ToList();
        var querList = LinqExpression.Or<PositionEntity>();
        pIds.ForEach(item => querList = querList.Or(x => x.PositionIdTree.Contains(item)));
        var res = _repository.AsSugarClient().Queryable<PositionEntity>().Where(querList).Select(x => x.Id).ToList();
        return res;
    }

    /// <summary>
    /// 获取组织、岗位配置层级.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public async Task<int> GetOrganizeOrPositionLayer(string type)
    {
        var sysConfigData = string.Empty;
        if (type.Equals(CommonConst.RelationObjectOrganize)) sysConfigData = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("orgLevel")).Select(x => x.Value).FirstAsync();
        else if (type.Equals(CommonConst.RelationObjectPosition)) sysConfigData = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("positionLevel")).Select(x => x.Value).FirstAsync();

        if (sysConfigData == null || sysConfigData.IsNullOrEmpty()) return 9527;
        else return sysConfigData.ObjToInt();
    }

    /// <summary>
    /// 获取下属.
    /// </summary>
    /// <param name="managerId">主管Id.</param>
    /// <param name="isAllSub">是否子孙级全捞.</param>
    /// <returns></returns>
    public string[] GetSubordinates(string managerId, bool isAllSub = false)
    {
        var posId = User.PositionId;
        if (posId.IsNullOrEmpty()) return Array.Empty<string>();
        if (!managerId.Equals(UserId)) posId = _repository.AsQueryable().Where(x => x.Id.Equals(managerId)).Select(x => x.PositionId).First();

        // 捞下级岗位的所有用户
        var subIdList = _repository.AsSugarClient().Queryable<PositionEntity>()
            .WhereIF(!isAllSub, x => x.ParentId.Equals(posId))
            .WhereIF(isAllSub, x => x.PositionIdTree.Contains(posId))
            .Select(x => x.Id).ToList();
        subIdList.Remove(posId);
        var userIds = _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType.Equals(CommonConst.RelationObjectPosition) && subIdList.Contains(x.ObjectId)).Select(x => x.UserId).ToList();
        return userIds.ToArray();
    }

    /// <summary>
    /// 获取所有岗位下属.
    /// </summary>
    /// <param name="managerId">主管Id.</param>
    /// <param name="isAllSub">是否子孙级全捞.</param>
    /// <returns></returns>
    public List<string> GetSubordinatesAllPos(string managerId, bool isAllSub = false)
    {
        var posIds = GetUserPositionList(managerId);
        // 捞下级岗位的所有用户
        var subIdList = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => posIds.Contains(x.ParentId)).Select(x => x.Id).ToList();
        if (isAllSub)
        {
            subIdList = new List<string>();
            foreach (var item in posIds)
            {
                var ids = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.PositionIdTree.Contains(item)).Select(x => x.Id).ToList();
                subIdList.AddRange(ids);
            }
        }
        subIdList = subIdList.Except(posIds).ToList();
        var userIds = _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType.Equals(CommonConst.RelationObjectPosition) && subIdList.Contains(x.ObjectId)).Select(x => x.UserId).ToList();
        return userIds;
    }

    /// <summary>
    /// 用户权限Ids(角色ids、组织ids) 根据用户id.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isCurrStanding">是否身份过滤.</param>
    /// <returns></returns>
    public List<string> GetPermissionIds(string userId, bool isCurrStanding = true)
    {
        var objIds = new List<string>();
        if (isCurrStanding && Standing.IsNotEmptyOrNull())
        {
            objIds = _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                .Where(x => x.ItemType.Equals(CommonConst.AuthorizeItemStanding) && x.ItemId.Equals(Standing))
                .Where(x => x.ObjectType.Equals(CommonConst.RelationObjectRole) || x.ObjectType.Equals(CommonConst.RelationObjectPosition))
                .Select(x => x.ObjectId).ToList();
        }

        if (User == null || Standing.IsNullOrEmpty()) isCurrStanding = false;

        var posIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .WhereIF(isCurrStanding, x => objIds.Contains(x.ObjectId))
            .WhereIF(!isCurrStanding, x => x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition))
            .Select(x => x.ObjectId).ToList();

        var relationList = _repository.AsSugarClient().Queryable<RoleRelationEntity>()
            .WhereIF(isCurrStanding, x => (objIds.Contains(x.RoleId) || posIdList.Contains(x.ObjectId)) && !x.ObjectType.Equals(CommonConst.RelationObjectOrganize))
            .WhereIF(!isCurrStanding, x => (x.ObjectId.Equals(userId) || posIdList.Contains(x.ObjectId)) && !x.ObjectType.Equals(CommonConst.RelationObjectOrganize))
            .Select(x => x.RoleId).ToList();

        relationList.AddRange(posIdList);
        return relationList.Distinct().ToList();
    }

    /// <summary>
    /// 当前用户权限Ids (ItemIds).
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <returns></returns>
    public Dictionary<string, object> GetAuthorizeItemIds(string userId)
    {
        var pIds = PermissionIds;
        var mIdList = _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(a => a.ItemType == CommonConst.AuthorizeItemModule)
            .WhereIF(Standing.IsNullOrEmpty(), a => pIds.Contains(a.ObjectId))
            .Select(a => new { a.ItemId, a.ItemType }).ToList();
        var res = new Dictionary<string, object>();
        foreach (var item in mIdList) res[item.ItemId] = item.ItemType;
        return res;
    }

    /// <summary>
    /// 获取用户权限项目id,根据项目类型.
    /// </summary>
    /// <param name="objectType">system、module、button、column、resource、standing ...</param>
    /// <param name="userId">指定用户id.(为空则为当前用户id).</param>
    /// <returns></returns>
    public async Task<List<string>> GetUserAuthorizeItemIdList(string objectType, string userId = "")
    {
        var pIds = new List<string>();
        var isAdamin = IsAdministrator;
        if (userId.IsNullOrEmpty())
        {
            pIds = PermissionIds;
        }
        else
        {
            pIds = GetPermissionIds(userId, false);
            isAdamin = await _repository.AsSugarClient().Queryable<UserEntity>().AnyAsync(x => x.Id == userId && x.IsAdministrator == 1 && x.DeleteMark == null);
        }
        var res = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .WhereIF(!isAdamin, x => pIds.Contains(x.ObjectId))
            .WhereIF(objectType.IsNotEmptyOrNull(), x => x.ItemType.Equals(objectType)).Select(x => x.ItemId).ToListAsync();
        return res;
    }

    /// <summary>
    /// 根据项目类型获取用户身份的权限id.
    /// <param name="objectType"></param>
    /// </summary>
    /// <returns></returns>
    public async Task<List<string>> GetUserStandingAuthIdList(string objectType)
    {
        var res = new List<string>();
        if (Standing.IsNotEmptyOrNull())
        {
            var objIds = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                .Where(x => x.ItemType.Equals(CommonConst.AuthorizeItemStanding) && x.ItemId.Equals(Standing))
                .Select(x => x.ObjectId).ToListAsync();
            res = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                .Where(a => a.ItemType == objectType && objIds.Contains(a.ObjectId))
                .Select(a => a.ItemId).ToListAsync();
        }
        else
        {
            res = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                .Where(a => a.ItemType == objectType && PermissionIds.Contains(a.ObjectId))
                .Select(a => a.ItemId).ToListAsync();
        }

        return res;
    }

    /// <summary>
    /// 获取用户名称.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isAccount">是否带账号.</param>
    /// <returns></returns>
    public string GetUserName(string userId, bool isAccount = true)
    {
        UserEntity? entity = _repository.GetFirst(x => x.Id == userId && x.DeleteMark == null);
        if (entity.IsNullOrEmpty()) return string.Empty;
        return isAccount ? entity.RealName + "/" + entity.Account : entity.RealName;
    }

    /// <summary>
    /// 获取管理员用户id.
    /// </summary>
    public string GetAdminUserId()
    {
        var user = _repository.AsSugarClient().Queryable<UserEntity>().First(x => x.Account == "admin" && x.DeleteMark == null);
        if (user.IsNotEmptyOrNull()) return user.Id;
        return string.Empty;
    }

    /// <summary>
    /// 获取系统参数值.
    /// </summary>
    /// <param name="value">系统参数.</param>
    /// <param name="userId">用户id.</param>
    /// <param name="type">模块类型 0-默认 1-代码生成.</param>
    /// <returns></returns>
    public object GetSysParamValue(string value, string userId = null, int type = 0)
    {
        var userEntity = _repository.GetFirst(x => x.Id == userId);
        if (userEntity.IsNullOrEmpty())
        {
            userEntity = User;
        }
        switch (value)
        {
            case CommonConst.SysParamCurrentTime:
                return DateTime.Now.ToString();
            case CommonConst.SysParamUserId:
                return userEntity.IsNotEmptyOrNull() ? userEntity.Id : UserId;
            case CommonConst.SysParamUserAndSub:
                if (userEntity.IsNotEmptyOrNull())
                {
                    var userIdlist = new List<string> { userId };
                    userIdlist.AddRange(GetSubordinates(userId).ToList());
                    return type == 1 ? userIdlist.ToJsonStringOld() : userIdlist;
                }
                else
                {
                    return type == 1 ? CurrentUserAndSubordinates.ToJsonStringOld() : CurrentUserAndSubordinates;
                }
            case CommonConst.SysParamUserTree:
                if (userEntity.IsNotEmptyOrNull())
                {
                    var userIdlist = new List<string> { UserId };
                    userIdlist.AddRange(GetSubordinates(userId).ToList());
                    return type == 1 ? userIdlist.ToJsonStringOld() : userIdlist;
                }
                else
                {
                    return type == 1 ? CurrentUserTree.ToJsonStringOld() : CurrentUserTree;
                }
            case CommonConst.SysParamPositionId:
                {
                    var oList = GetUserPositionList(userEntity.Id);
                    return type == 1 ? oList.ToJsonStringOld() : oList;
                }
            case CommonConst.SysParamPositionAndSub:
                {
                    var oList = GetUserPositionAndSuborganization(userEntity.Id);
                    return type == 1 ? oList.ToJsonStringOld() : oList;
                }
            case CommonConst.SysParamPositionTree:
                {
                    var oList = GetUserPositionTree(userEntity.Id);
                    return type == 1 ? oList.ToJsonStringOld() : oList;
                }
            case CommonConst.SysParamOrganizeId:
                {
                    var oList = GetUserOrganizeList(userEntity.Id);
                    return type == 1 ? oList.ToJsonStringOld() : oList;
                }
            case CommonConst.SysParamOrganizeAndSub:
                {
                    var oList = GetUserOrganizeAndSuborganization(userEntity.Id);
                    return type == 1 ? oList.ToJsonStringOld() : oList;
                }
            case CommonConst.SysParamOrganizationTree:
                {
                    var oList = GetUserOrganizeTree(userEntity.Id);
                    return type == 1 ? oList.ToJsonStringOld() : oList;
                }
        }
        return value;
    }

    /// <summary>
    /// 直属主管.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public List<string> GetSupervisor(string userId)
    {
        var managerIds = new List<string>();
        var posEntityList = _repository.AsSugarClient().Queryable<UserRelationEntity, PositionEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.ObjectId == b.Id))
               .Where((a, b) => a.UserId == userId && a.ObjectType == CommonConst.RelationObjectPosition && b.EnabledMark == 1 && b.DeleteMark == null).Select((a, b) => b).ToList();
        foreach (var item in posEntityList)
        {
            var posEntity = item;
            if (posEntity.ParentId.IsNullOrEmpty()) // 顶级岗位
            {
                var org = _repository.AsSugarClient().Queryable<OrganizeEntity>().First(x => x.Id == posEntity.OrganizeId && x.DeleteMark == null);
                if (org.IsNotEmptyOrNull() && org.ParentId != "-1")
                {
                    org = _repository.AsSugarClient().Queryable<OrganizeEntity>().First(x => x.Id == org.ParentId && x.DeleteMark == null);
                    if (org.IsNotEmptyOrNull() && org.DutyPositionId.IsNotEmptyOrNull())
                    {
                        posEntity = _repository.AsSugarClient().Queryable<PositionEntity>().First(x => x.Id == org.DutyPositionId && x.DeleteMark == null);
                        if (posEntity.IsNotEmptyOrNull() && posEntity.DutyUserId.IsNotEmptyOrNull())
                        {
                            managerIds.Add(posEntity.DutyUserId);
                        }
                    }
                }
            }
            else
            {
                var dutyUserId = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.Id == posEntity.ParentId).Select(x => x.DutyUserId).First();
                managerIds.Add(dutyUserId);
            }

        }
        return managerIds;
    }

    /// <summary>
    /// 用户组件所有id.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public List<string> GetUserRelationAll(string userId)
    {
        var userList = new List<string>();
        var userRelationList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId == userId).ToList();
        var roleRelationList = _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectId == userId && x.ObjectType == CommonConst.RelationObjectUser).ToList();
        userList.AddRange(userRelationList.Where(x => x.ObjectType != CommonConst.RelationObjectOrganize || x.ObjectType != CommonConst.RelationObjectPosition).Select(x => string.Format("{0}--{1}", x.ObjectId, x.ObjectType)));
        userList.AddRange(roleRelationList.Select(x => string.Format("{0}--{1}", x.RoleId, CommonConst.RelationObjectRole)));
        var posIdList = userRelationList.Where(x => x.ObjectType == CommonConst.RelationObjectPosition).Select(x => x.ObjectId);
        var posList = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => posIdList.Contains(x.Id) && x.DeleteMark == null).ToList();
        foreach (var item in posList)
        {
            userList.Add(string.Format("{0}--{1}", item.Id, "pos"));
            userList.Add(string.Format("{0}--{1}", item.Id, "subPos"));
            userList.Add(string.Format("{0}--{1}", item.Id, "progenyPos"));
            if (item.ParentId.IsNotEmptyOrNull() && item.ParentId != "-1" && item.ParentId != "0")
            {
                userList.Add(string.Format("{0}--{1}", item.ParentId, "subPos"));
            }
            var progenyPos = _repository.AsSugarClient().Queryable<PositionEntity>().ToParentList(it => it.ParentId, item.Id).Where(x => x.Id != item.Id).Select(x => string.Format("{0}--{1}", x.Id, "progenyPos")).ToList();
            userList.AddRange(progenyPos);
        }
        var orgIdList = userRelationList.Where(x => x.ObjectType == CommonConst.RelationObjectOrganize).Select(x => x.ObjectId);
        var orgList = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => orgIdList.Contains(x.Id) && x.DeleteMark == null).ToList();
        foreach (var item in orgList)
        {
            userList.Add(string.Format("{0}--{1}", item.Id, "org"));
            userList.Add(string.Format("{0}--{1}", item.Id, "subOrg"));
            userList.Add(string.Format("{0}--{1}", item.Id, "progenyOrg"));
            if (item.ParentId.IsNotEmptyOrNull() && item.ParentId != "-1" && item.ParentId != "0")
            {
                userList.Add(string.Format("{0}--{1}", item.ParentId, "subOrg"));
            }
            var progenyPos = _repository.AsSugarClient().Queryable<OrganizeEntity>().ToParentList(it => it.ParentId, item.Id).Where(x => x.Id != item.Id).Select(x => string.Format("{0}--{1}", x.Id, "progenyOrg")).ToList();
            userList.AddRange(progenyPos);
        }
        userList.Add(string.Format("{0}--{1}", userId, CommonConst.RelationObjectUser));
        return userList.Distinct().ToList();
    }
    #endregion

    #region 私密方法

    private List<IConditionalModel> GetIConditionalModelListByTableName(List<IConditionalModel> cList, string tableName)
    {
        for (int i = 0; i < cList.Count; i++)
        {
            if (cList[i] is ConditionalTree)
            {
                var newItem = (ConditionalTree)cList[i];
                for (int j = 0; j < newItem.ConditionalList.Count; j++)
                {
                    var value = GetIConditionalModelListByTableName(new List<IConditionalModel>() { newItem.ConditionalList[j].Value }, tableName);
                    if (value != null && value.Any())
                    {
                        if (newItem.ConditionalList[j].Equals(newItem.ConditionalList.FirstOrDefault()))
                            newItem.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(newItem.ConditionalList[j].Key, value.First());
                        else
                            newItem.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(newItem.ConditionalList[j].Key, value.First());
                    }
                    else
                    {
                        newItem.ConditionalList.RemoveAt(j);
                        j--;
                    }
                }

                if (newItem.ConditionalList.Any())
                {
                    cList[i] = newItem;
                }
                else
                {
                    cList.RemoveAt(i);
                    i--;
                }
            }
            else if (cList[i] is ConditionalCollections)
            {
                var newItemList = (ConditionalCollections)cList[i];

                for (int j = 0; j < newItemList.ConditionalList.Count; j++)
                {
                    var fieldName = newItemList.ConditionalList[j].Value.FieldName;
                    if (tableName.IsNullOrEmpty() && (fieldName.Contains("_jnpf_") || (fieldName.ToLower().Contains("tablefield") && fieldName.Contains("-"))))
                    {
                        newItemList.ConditionalList.RemoveAt(j);
                    }
                    else if (tableName.IsNotEmptyOrNull() && !fieldName.Contains(tableName))
                    {
                        newItemList.ConditionalList.RemoveAt(j);
                    }
                }
                if (newItemList.ConditionalList.Any()) cList[i] = newItemList;
                else cList.RemoveAt(i);
            }
            else if (cList[i] is ConditionalModel)
            {
                var newItem = (ConditionalModel)cList[i];
                if (tableName.IsNullOrEmpty() && (newItem.FieldName.Contains("_jnpf_") || (newItem.FieldName.ToLower().Contains("tablefield") && newItem.FieldName.Contains("-"))))
                {
                    cList.RemoveAt(i);
                }
                else if (tableName.IsNotEmptyOrNull() && !newItem.FieldName.Contains(tableName))
                {
                    cList.RemoveAt(i);
                }
            }
        }

        return cList;
    }

    private string GetSystemIdByCode()
    {
        var sysCode = _httpContext?.Request?.Headers["App-Code"].ToString();
        if (sysCode.IsNullOrEmpty())
        {
            if (UserOrigin.Equals("pc"))
                sysCode = CommonConst.SystemCodeMain;
            else
                return User.AppSystemId;
        }
        return _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => x.EnCode.Equals(sysCode)).Select(x => x.Id).First();
    }

    /// <summary>
    /// 获取条件模型.
    /// </summary>
    /// <returns></returns>
    private ConditionalModel GetConditionalModel(QueryType expressType, string fieldName, string fieldValue, string dataType = "string")
    {
        switch (expressType)
        {
            // 模糊
            case QueryType.Contains:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Like, FieldValue = fieldValue };

            // 等于
            case QueryType.Equal:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Equal, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Equal, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Equal, FieldValue = fieldValue };
                }

            // 不等于
            case QueryType.NotEqual:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NoEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NoEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NoEqual, FieldValue = fieldValue };
                }

            // 小于
            case QueryType.LessThan:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThan, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThan, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThan, FieldValue = fieldValue };
                }

            // 小于等于
            case QueryType.LessThanOrEqual:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThanOrEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThanOrEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.LessThanOrEqual, FieldValue = fieldValue };
                }

            // 大于
            case QueryType.GreaterThan:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThan, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThan, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThan, FieldValue = fieldValue };
                }

            // 大于等于
            case QueryType.GreaterThanOrEqual:
                switch (dataType)
                {
                    case "Double":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThanOrEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(double)) };
                    case "Int32":
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThanOrEqual, FieldValue = fieldValue, FieldValueConvertFunc = it => SqlSugar.UtilMethods.ChangeType2(it, typeof(int)) };
                    default:
                        return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.GreaterThanOrEqual, FieldValue = fieldValue };
                }

            // 包含
            case QueryType.In:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.In, FieldValue = fieldValue };
            case QueryType.Included:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.Like, FieldValue = fieldValue };
            // 不包含
            case QueryType.NotIn:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NotIn, FieldValue = fieldValue };
            case QueryType.NotIncluded:
                return new ConditionalModel() { FieldName = fieldName, ConditionalType = ConditionalType.NoLike, FieldValue = fieldValue };
        }

        return new ConditionalModel();
    }

    /// <summary>
    /// 会否存在用户缓存.
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <returns></returns>
    private async Task<bool> ExistsUserInfo(string cacheKey)
    {
        return await _cacheManager.ExistsAsync(cacheKey);
    }

    /// <summary>
    /// 保存用户登录信息.
    /// </summary>
    /// <param name="cacheKey">key.</param>
    /// <param name="userInfo">用户信息.</param>
    /// <param name="timeSpan">过期时间.</param>
    /// <returns></returns>
    private async Task<bool> SetUserInfo(string cacheKey, UserInfoModel userInfo, TimeSpan timeSpan)
    {
        return await _cacheManager.SetAsync(cacheKey, userInfo, timeSpan);
    }

    /// <summary>
    /// 获取全局租户缓存.
    /// </summary>
    /// <returns></returns>
    private GlobalTenantCacheModel GetGlobalTenantCache(string tenantId)
    {
        string cacheKey = string.Format("{0}", CommonConst.GLOBALTENANT);
        return _cacheManager.Get<List<GlobalTenantCacheModel>>(cacheKey).Find(it => it.TenantId.Equals(tenantId));
    }

    /// <summary>
    /// 获取用户登录信息.
    /// </summary>
    /// <param name="cacheKey">key.</param>
    /// <returns></returns>
    private async Task<UserInfoModel> GetUserInfo(string cacheKey)
    {
        return (await _cacheManager.GetAsync(cacheKey)).Adapt<UserInfoModel>();
    }

    /// <summary>
    /// 获取门户id.
    /// </summary>
    /// <param name="portalId"></param>
    /// <param name="wKey"></param>
    /// <param name="aKey"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, string>> GetPortalIdDic(string portalId, string wKey, string aKey)
    {
        var portalDic = new Dictionary<string, string>();
        if (portalId.IsNotEmptyOrNull() && portalId.FirstOrDefault().Equals('{'))
            portalDic = portalId.ToObject<Dictionary<string, string>>();

        var sysEntity = await _repository.AsSugarClient().Queryable<SystemEntity>().FirstAsync(x => x.Id.Equals(SystemId));
        var wPortalIds = sysEntity.PortalId.Split(",").ToList();
        wPortalIds = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => x.DeleteMark == null && wPortalIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        var aPortalIds = sysEntity.AppPortalId.Split(",").ToList();
        aPortalIds = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => x.DeleteMark == null && aPortalIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        if (!IsAdministrator)
        {
            var authIds = await GetUserAuthorizeItemIdList(CommonConst.AuthorizeItemModule);
            wPortalIds = wPortalIds.FindAll(x => authIds.Contains(x));
            aPortalIds = aPortalIds.FindAll(x => authIds.Contains(x));
        }

        if (wPortalIds.IsNotEmptyOrNull() && wPortalIds.Count > 0)
        {
            if (!portalDic.ContainsKey(wKey) || (portalDic.ContainsKey(wKey) && !wPortalIds.Contains(portalDic[wKey])))
                portalDic[wKey] = wPortalIds[0];
        }
        else
        {
            portalDic.Remove(wKey);
        }

        if (aPortalIds.IsNotEmptyOrNull() && aPortalIds.Count > 0)
        {
            if (!portalDic.ContainsKey(aKey) || (portalDic.ContainsKey(aKey) && !aPortalIds.Contains(portalDic[aKey])))
                portalDic[aKey] = aPortalIds[0];
        }
        else
        {
            portalDic.Remove(aKey);
        }

        return portalDic;
    }

    /// <summary>
    /// 是否包含当前编码的角色.
    /// </summary>
    private bool IsRole(string enCode, bool isCurrStanding = true)
    {
        var permissionIds = GetPermissionIds(UserId, isCurrStanding);
        if (enCode.IsNotEmptyOrNull())
        {
            return _repository.AsSugarClient().Queryable<RoleEntity>().Any(x => x.DeleteMark == null && x.EnCode.Equals(enCode) && permissionIds.Contains(x.Id));
        }
        else
        {
            return _repository.AsSugarClient().Queryable<RoleEntity>().Any(x => x.DeleteMark == null && x.GlobalMark.Equals(2))
                || _repository.AsSugarClient().Queryable<PositionEntity>().Any(x => x.DeleteMark == null && permissionIds.Contains(x.Id));
        }
    }

    /// <summary>
    /// 是否有后台权限.
    /// </summary>
    private bool UserHasBackend()
    {
        if (IsAdministrator) return true;
        if (IsDevRole.Value)
            return _repository.AsSugarClient().Queryable<SystemEntity>().Any(x => x.Id.Equals(SystemId) && (x.UserId.Equals(UserId) || x.AuthorizeId.Contains(CommonConst.SystemAuthCodeAllDevUser) || x.AuthorizeId.Contains(UserId)));
        else
            return _repository.AsSugarClient().Queryable<SystemEntity>().Any(x => x.Id.Equals(SystemId) && x.UserId.Equals(UserId));
    }

    #endregion

    #region 在线开发

    /// <summary>
    /// 获取数据权限.
    /// </summary>
    /// <returns></returns>
    public async Task<List<IConditionalModel>> GetDataPermission(string primaryKey, string menuId, bool isDataPermissions, bool primaryKeyPolicy)
    {
        var primaryWhere = new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => UtilMethods.ChangeType2(it, typeof(string)) });
        if (primaryKeyPolicy) primaryWhere = new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel() { FieldName = primaryKey, ConditionalType = ConditionalType.NoEqual, FieldValue = "0", FieldValueConvertFunc = it => UtilMethods.ChangeType2(it, typeof(int)) });

        var conModels = new List<IConditionalModel>();
        if (IsAdministrator) return conModels; // 管理员全部放开

        var roles = PermissionIds;
        var roleAuthorizeList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(x => roles.Contains(x.ObjectId) && x.ItemType == CommonConst.AuthorizeItemResource).Select(a => new { a.ItemId, a.ObjectId }).ToListAsync();

        if (!isDataPermissions)
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                {
                    primaryWhere
                }
            });
            return conModels;
        }
        else if (roleAuthorizeList.Count == 0 && isDataPermissions)
        {
            primaryWhere.Value.ConditionalType = ConditionalType.Equal;
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                {
                    primaryWhere
                }
            });
            return conModels;
        }

        var resourceList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().In(it => it.Id, roleAuthorizeList.Select(x => x.ItemId).ToList()).Where(it => it.ModuleId == menuId && it.DeleteMark == null).ToListAsync();
        if (resourceList.Any(x => x.AllData == 1 || x.EnCode.Equals("jnpf_alldata")))
        {
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                {
                    primaryWhere
                }
            });
        }
        else
        {
            var allList = new List<object>(); // 构造任何层级的条件
            var resultList = new List<object>();
            foreach (var roleId in PermissionIds)
            {
                var roleList = new List<object>();
                foreach (var item in resourceList.Where(x => roleAuthorizeList.Where(xx => xx.ObjectId.Equals(roleId)).Select(x => x.ItemId).Contains(x.Id)).ToList())
                {
                    var condion = AssembleQuery(item.ConditionJson, true);
                    roleList.AddRange(condion.ToObject<List<object>>());
                }

                if (roleList.Any()) allList.Add(new { Key = (int)WhereType.Or, Value = new { ConditionalList = roleList } });
            }

            if (allList.Any()) resultList.Add(new { ConditionalList = allList });

            if (resultList.Any()) conModels.AddRange(_repository.AsSugarClient().Utilities.JsonToConditionalModels(resultList.ToJsonString()));
        }

        if (resourceList.Count == 0)
        {
            primaryWhere.Value.ConditionalType = ConditionalType.Equal;
            conModels.Add(new ConditionalCollections()
            {
                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                {
                    primaryWhere
                }
            });
        }

        return conModels;
    }

    /// <summary>
    /// 组装查询条件.
    /// </summary>
    /// <returns></returns>
    public string AssembleQuery(string superQuery, bool isDataPermissions = false)
    {
        var res = new List<object>();
        var conModels = new List<object>();

        if (superQuery.IsNotEmptyOrNull())
        {
            var queryModel = superQuery.ToObject<QueryModel>();
            var whereType = queryModel.matchLogic.ToUpper().Equals("AND") ? (int)WhereType.And : (int)WhereType.Or;
            foreach (var item in queryModel.conditionList)
            {
                var groupList = new List<object>();
                var groupWhere = item.logic.ToUpper().Equals("AND") ? (int)WhereType.And : (int)WhereType.Or;
                foreach (var con in item.groups)
                {
                    var conList = new List<object>();
                    var itemRule = GetQuery(con);
                    foreach (var model in itemRule)
                    {
                        var where = model.Equals(itemRule[0]) ? groupWhere : (int)model.Key;
                        var cSharpTypeName = model.Value.CSharpTypeName == null ? string.Empty : model.Value.CSharpTypeName;
                        conList.Add(new { Key = where, Value = new { FieldName = model.Value.FieldName, FieldValue = model.Value.FieldValue, ConditionalType = model.Value.ConditionalType, CSharpTypeName = cSharpTypeName } });
                    }

                    if (conList.Any())
                    {
                        if (groupList.Any())
                            groupList.Add(new { Key = groupWhere, Value = new { ConditionalList = conList } });
                        else
                            groupList.Add(new { Key = whereType, Value = new { ConditionalList = conList } });
                    }
                }

                if (groupList.Any()) conModels.Add(new { Key = whereType, Value = new { ConditionalList = groupList } });
            }
        }

        if (isDataPermissions) return conModels.ToJsonStringOld();
        if (conModels.Any()) res.Add(new { ConditionalList = conModels });

        return res.ToJsonStringOld();
    }

    /// <summary>
    /// 获取查询条件.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private List<KeyValuePair<WhereType, ConditionalModel>> GetQuery(QueryConditionGroupsListModel model)
    {
        var conModels = new List<KeyValuePair<WhereType, ConditionalModel>>();
        var between = new List<string>();
        var cSharpTypeName = string.Empty;
        if (model.fieldValue.IsNotEmptyOrNull())
        {
            switch (model.fieldValue)
            {
                case CommonConst.SysParamCurrentTime:
                    model.fieldValue = DateTime.Now;
                    break;
                case CommonConst.SysParamUserId:
                    model.fieldValue = UserId;
                    break;
                case CommonConst.SysParamUserAndSub:
                    model.fieldValue = CurrentUserAndSubordinates.ToJsonStringOld();
                    break;
                case CommonConst.SysParamUserTree:
                    model.fieldValue = CurrentUserTree.ToJsonStringOld();
                    break;
                case CommonConst.SysParamPositionId:
                    model.fieldValue = CurrentPositionList;
                    break;
                case CommonConst.SysParamPositionAndSub:
                    model.fieldValue = CurrentPositionAndSuborganization;
                    break;
                case CommonConst.SysParamPositionTree:
                    model.fieldValue = CurrentPositionTree;
                    break;
                case CommonConst.SysParamOrganizeId:
                    model.fieldValue = CurrentOrganizeList.ToJsonStringOld();
                    break;
                case CommonConst.SysParamOrganizeAndSub:
                    model.fieldValue = CurrentOrganizationAndSubOrganizations.ToJsonStringOld();
                    break;
                case CommonConst.SysParamOrganizationTree:
                    model.fieldValue = CurrentOrganizationTree.ToJsonStringOld();
                    break;
                default:
                    if (model.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "").StartsWith("["))
                        model.fieldValue = model.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "");
                    break;
            }

            if (model.symbol.Equals("between")) between = model.fieldValue.ToString()?.ToObject<List<string>>();
            switch (model.jnpfKey)
            {
                case JnpfKeyConst.CREATETIME:
                case JnpfKeyConst.MODIFYTIME:
                case JnpfKeyConst.DATE:
                case JnpfKeyConst.DATECALCULATE:
                    {
                        cSharpTypeName = "datetime";
                        if (model.symbol.Equals("between"))
                        {
                            var startTime = between.First().TimeStampToDateTime();
                            var endTime = between.Last().TimeStampToDateTime();
                            between[0] = startTime.ToString();
                            between[1] = endTime.ToString();
                        }
                        else
                        {
                            if (model.fieldValue is DateTime)
                                model.fieldValue = model.fieldValue.ToString();
                            else
                                model.fieldValue = model.fieldValue.ToString().TimeStampToDateTime();
                        }
                    }

                    break;
                case JnpfKeyConst.RATE:
                case JnpfKeyConst.SLIDER:
                case JnpfKeyConst.NUMINPUT:
                case JnpfKeyConst.CALCULATE:
                    cSharpTypeName = "decimal";
                    break;
            }
        }

        var conditionalType = ConditionalType.Equal;
        switch (model.symbol)
        {
            case ">=":
                conditionalType = ConditionalType.GreaterThanOrEqual;
                break;
            case ">":
                conditionalType = ConditionalType.GreaterThan;
                break;
            case "==":
                conditionalType = ConditionalType.Equal;
                break;
            case "<=":
                conditionalType = ConditionalType.LessThanOrEqual;
                break;
            case "<":
                conditionalType = ConditionalType.LessThan;
                break;
            case "<>":
                conditionalType = ConditionalType.NoEqual;
                if (model.fieldValue.IsNullOrEmpty())
                    model.fieldValue = string.Empty;
                break;
            case "like":
                if (model.fieldValue != null && model.fieldValue.ToString().Contains("[")) model.fieldValue = model.fieldValue.ToString().Replace("[", string.Empty).Replace("]", string.Empty);
                conditionalType = ConditionalType.Like;
                break;
            case "notLike":
                if (model.fieldValue != null && model.fieldValue.ToString().Contains("[")) model.fieldValue = model.fieldValue.ToString().Replace("[", string.Empty).Replace("]", string.Empty);
                conditionalType = ConditionalType.NoLike;
                break;
            case "in":
            case "notIn":
                if (model.fieldValue.IsNotEmptyOrNull())
                {
                    if (!model.fieldValue.ToString().Contains('[')) model.fieldValue = new List<string>() { model.fieldValue.ToString() }.ToJsonStringOld();
                    else if (model.jnpfKey.Equals(JnpfKeyConst.CASCADER) && !model.fieldValue.ToString().Equals("[]") && !model.fieldValue.ToString().StartsWith("[[")) model.fieldValue = new List<string>() { model.fieldValue.ToString()?.ToObject<List<string>>().Last() + "\"]" }.ToJsonStringOld();

                    var ids = new List<string>();
                    if (model.fieldValue.ToString().StartsWith("[["))
                        ids = model.fieldValue.ToString().ToObject<List<List<string>>>().Select(x => x.Last()).ToList();
                    else
                        ids = model.fieldValue.ToString().ToObject<List<string>>();
                    if (ids.Count == 0)
                    {
                        conModels.Add(new KeyValuePair<WhereType, ConditionalModel>(
                            WhereType.And, new ConditionalModel()
                            {
                                FieldName = model.field,
                                ConditionalType = ConditionalType.Equal,
                                FieldValue = "jnpfNull"
                            }));
                        return conModels;
                    }

                    if (model.symbol.Equals("notIn"))
                    {
                        conModels.Add(new KeyValuePair<WhereType, ConditionalModel>(
                            WhereType.And, new ConditionalModel()
                            {
                                FieldName = model.field,
                                ConditionalType = ConditionalType.IsNot,
                                FieldValue = null
                            }));
                        conModels.Add(new KeyValuePair<WhereType, ConditionalModel>(
                            WhereType.And, new ConditionalModel()
                            {
                                FieldName = model.field,
                                ConditionalType = ConditionalType.IsNot,
                                FieldValue = string.Empty
                            }));
                    }

                    var whereType = model.symbol.Equals("in") ? WhereType.Or : WhereType.And;
                    for (var i = 0; i < ids.Count; i++)
                    {
                        var it = ids[i];
                        conModels.Add(new KeyValuePair<WhereType, ConditionalModel>(
                            whereType, new ConditionalModel()
                            {
                                FieldName = model.field,
                                ConditionalType = model.symbol.Equals("in") ? ConditionalType.Like : ConditionalType.NoLike,
                                FieldValue = it
                            }));
                    }
                }

                return conModels;
            case "null":
                if (model.jnpfKey.Equals(JnpfKeyConst.CALCULATE) || model.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) || model.jnpfKey.Equals(JnpfKeyConst.RATE) || model.jnpfKey.Equals(JnpfKeyConst.SLIDER))
                    conditionalType = ConditionalType.EqualNull;
                else
                    conditionalType = ConditionalType.IsNullOrEmpty;
                break;
            case "notNull":
                conditionalType = ConditionalType.IsNot;
                if (model.fieldValue.IsNullOrEmpty())
                    model.fieldValue = null;
                break;
            case "between":
                conModels.Add(new KeyValuePair<WhereType, ConditionalModel>(
                    WhereType.And, new ConditionalModel()
                    {
                        FieldName = model.field,
                        ConditionalType = ConditionalType.GreaterThanOrEqual,
                        FieldValue = between.First(),
                        FieldValueConvertFunc = it => Convert.ToDateTime(it),
                        CSharpTypeName = cSharpTypeName.IsNotEmptyOrNull() ? cSharpTypeName : null
                    }));
                conModels.Add(new KeyValuePair<WhereType, ConditionalModel>(
                    WhereType.And, new ConditionalModel()
                    {
                        FieldName = model.field,
                        ConditionalType = ConditionalType.LessThanOrEqual,
                        FieldValue = between.Last(),
                        FieldValueConvertFunc = it => Convert.ToDateTime(it),
                        CSharpTypeName = cSharpTypeName.IsNotEmptyOrNull() ? cSharpTypeName : null
                    }));
                return conModels;
        }

        conModels.Add(new KeyValuePair<WhereType, ConditionalModel>(
            WhereType.And, new ConditionalModel()
            {
                FieldName = model.field,
                ConditionalType = conditionalType,
                FieldValue = model.fieldValue == null ? null : model.fieldValue.ToString(),
                CSharpTypeName = cSharpTypeName.IsNotEmptyOrNull() ? cSharpTypeName : null
            }));

        return conModels;
    }

    #endregion

    #region 当前用户信息
    private UserEntity _userEntity { get; set; }
    private List<string> _permissionIds { get; set; }
    private bool? _isManageRole { get; set; }
    private bool? _isDevRole { get; set; }
    private bool? _isAllStandingDevRole { get; set; }
    private bool? _isUserRole { get; set; }
    private bool? _isOtherRole { get; set; }
    private bool? _hasBackend { get; set; }
    private List<string> _currentUserAndSubordinates { get; set; }
    private List<string> _currentUserTree { get; set; }
    private List<string> _currentOrganizationAndSubOrganizations { get; set; }
    private List<string> _currentOrganizationTree { get; set; }
    private string _currentPositionList { get; set; }
    private string _currentPositionAndSuborganization { get; set; }

    private string _currentPositionTree { get; set; }

    private List<string> _currentOrganizeList { get; set; }

    private string? _standing { get; set; }

    private string _systemId { get; set; }

    private string _roleCode { get; set; }

    private List<string> _currentUserSupervisor { get; set; }

    #endregion
}