using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Models.User;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.Permission.User;
using JNPF.Systems.Entitys.Dto.Role;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Dto.UserRelation;
using JNPF.Systems.Entitys.Dto.UsersCurrent;
using JNPF.Systems.Entitys.Enum;
using JNPF.Systems.Entitys.Model.UsersCurrent;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Data;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace JNPF.Systems;

/// <summary>
///  业务实现：用户信息.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Users", Order = 163)]
[Route("api/permission/[controller]")]
public class UsersService : IUsersService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 配置文档.
    /// </summary>
    private readonly OauthOptions _oauthOptions = App.GetConfig<OauthOptions>("OAuth", true);

    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _repository;  // 用户表仓储

    private readonly IDictionaryDataService _dictionaryDataService;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 机构表服务.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 机构表服务.
    /// </summary>
    private readonly IDepartmentService _departmentService;

    /// <summary>
    /// 流程相关.
    /// </summary>
    private readonly IWorkFlowRepository _flowTaskRepository;

    /// <summary>
    /// 用户关系表服务.
    /// </summary>
    private readonly IUserRelationService _userRelationService;

    /// <summary>
    /// 系统配置服务.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 第三方同步服务.
    /// </summary>
    private readonly ISynThirdInfoService _synThirdInfoService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="UsersService"/>类型的新实例.
    /// </summary>
    public UsersService(
        ISqlSugarRepository<UserEntity> userRepository,
        IBillRullService billRuleService,
        IDictionaryDataService dictionaryDataService,
        IOrganizeService organizeService,
        IDepartmentService departmentService,
        IUserRelationService userRelationService,
        IWorkFlowRepository flowTaskRepository,
        ISysConfigService sysConfigService,
        ISynThirdInfoService synThirdInfoService,
        ICacheManager cacheManager,
        IFileManager fileService,
        ISqlSugarClient sqlSugarClient,
        IOptions<TenantOptions> tenantOptions,
        IUserManager userManager,
        ITenantManager tenantManager,
        IMHandler imHandler)
    {
        _repository = userRepository;
        _billRuleService = billRuleService;
        _dictionaryDataService = dictionaryDataService;
        _organizeService = organizeService;
        _departmentService = departmentService;
        _userRelationService = userRelationService;
        _flowTaskRepository = flowTaskRepository;
        _sysConfigService = sysConfigService;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _synThirdInfoService = synThirdInfoService;
        _fileManager = fileService;
        _tenant = tenantOptions.Value;
        _tenantManager = tenantManager;
        _imHandler = imHandler;
    }

    #region GET


    /// <summary>
    /// 获取全部用户.
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetUsers")]
    public async Task<dynamic> GetUsers(string departId)
    {
        var query = _repository.AsQueryable()
     .Where(p => p.EnabledMark == 1 && p.DeleteMark == null);

        // 使用条件判断添加组织ID条件
        if (!string.IsNullOrEmpty(departId))
        {
            query = query.Where(p => p.OrganizeId == departId);
        }

        return await query
            .OrderBy(p => p.SortCode)
            .Select(a => new UserListAllOutput
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", a.HeadIcon),
                gender = a.Gender,
                sortCode = a.SortCode,
                quickQuery = a.QuickQuery,
            })
            .ToListAsync();
    }

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<PageResult<UserListOutput>> GetList([FromQuery] UserListQuery input)
    {
        // 获取配置文件 账户锁定类型
        SysConfigEntity? config = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("lockType") && x.Category.Equals("SysConfig")).FirstAsync();
        ErrorStrategy configLockType = (ErrorStrategy)Enum.Parse(typeof(ErrorStrategy), config?.Value);

        // 性别字典类型
        var dictionaryTypeEntity2 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "sex" && x.DeleteMark == null && x.EnabledMark == 1);
        var dutyUserId = string.Empty;
        var userIds = new List<string>();
        if (input.groupId.IsNotEmptyOrNull()) input.groupId = input.groupId.Split("--").FirstOrDefault();
        if (input.organizeId.IsNotEmptyOrNull()) input.organizeId = input.organizeId.Split("--").FirstOrDefault();
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
        }
        var subOrganizeIds = new List<string>();
        if (input.showSubOrganize != null && input.showSubOrganize.Equals(1))
        {
            subOrganizeIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.OrganizeIdTree.Contains(input.organizeId)).Select(x => x.Id).ToListAsync();
            input.organizeId = null;
        }

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
            .OrderByIF(input.roleId.IsNotEmptyOrNull(), (a, b) => SqlFunc.CharIndexNew(string.Join("", userIds), a.Id))
            .OrderByIF(input.showSubOrganize.Equals(1), (a, b) => b.CreatorTime, OrderByType.Desc)
            .OrderByIF(input.groupId.IsNotEmptyOrNull(), (a, b) => b.CreatorTime, OrderByType.Desc)
            .OrderByIF(input.positionId.IsNotEmptyOrNull(), (a, b) => b.CreatorTime, OrderByType.Desc)
            .OrderByIF(input.organizeId.IsNotEmptyOrNull(), (a, b) => b.CreatorTime, OrderByType.Desc)
            .MergeTable()
            .GroupBy(a => new { a.Id, a.Account, a.RealName, a.CreatorTime, a.Gender, a.MobilePhone, a.Email, a.UnLockTime, a.SortCode, a.IsAdministrator, a.EnabledMark, a.HandoverUserId })
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
                email = a.Email,
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
            item.organize = string.Join(",", posList.Where(x => orgUserIdAll.Where(xx => xx.UserId.Equals(item.id)).Select(xx => xx.ObjectId).Contains(x.Id)).Select(x => x.FullName.Split("/").FirstOrDefault()).ToList());
        }

        #endregion

        return new PageResult<UserListOutput>() { list = data.list.ToList(), pagination = data.pagination.Adapt<PageResult>() };
    }

    /// <summary>
    /// 获取全部用户.
    /// </summary>
    /// <returns></returns>
    [HttpGet("All")]
    public async Task<dynamic> GetUserAllList()
    {
        return await _repository.AsSugarClient().Queryable<UserEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId))
            .Where(p => p.EnabledMark == 1 && p.DeleteMark == null).OrderBy(p => p.SortCode)
            .Select((a, b) => new UserListAllOutput
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", a.HeadIcon),
                gender = a.Gender,
                department = b.FullName,
                sortCode = a.SortCode,
                quickQuery = a.QuickQuery,
            }).ToListAsync();
    }

    /// <summary>
    /// 获取用户数据分页 根据角色Id.
    /// </summary>
    /// <returns></returns>
    [HttpGet("getUsersByRoleId")]
    public async Task<dynamic> GetUsersByRoleId([FromQuery] RoleListInput input)
    {
        // 查询角色
        List<string>? orgList = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectType == CommonConst.RelationObjectUser && x.RoleId == input.roleId).Select(x => x.ObjectId).ToListAsync();
        List<string>? userIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => orgList.Contains(x.ObjectId) || orgList.Contains(x.UserId)).Select(x => x.UserId).Distinct().ToListAsync();

        SqlSugarPagedList<UserListAllOutput>? list = await _repository.AsQueryable()
            .Where(a => userIdList.Contains(a.Id))
            .Where(p => p.EnabledMark == 1 && p.DeleteMark == null).OrderBy(p => p.SortCode)
            .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
            .Select(a => new UserListAllOutput
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                gender = a.Gender,
                sortCode = a.SortCode,
                quickQuery = a.QuickQuery,
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<UserListAllOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取用户数据 根据角色所属组织.
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetUsersByRoleOrgId")]
    public async Task<dynamic> GetUsersByRoleOrgId([FromQuery] RoleListInput input)
    {
        input.organizeId = input.organizeId == null ? "0" : input.organizeId;

        // 获取角色所属组织集合
        List<string>? orgList = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.RoleId == input.roleId).Select(x => x.ObjectId).ToListAsync();

        var orgTreeNameList = _organizeService.GetOrgListTreeName();

        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();
        if (input.organizeId.Equals("0"))
        {
            if (input.keyword.IsNotEmptyOrNull())
            {
                // 获取角色所属组织 成员id
                var res = await _repository.AsSugarClient().Queryable<UserEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.UserId == a.Id))
                .Where((a, b) => b.ObjectType == CommonConst.RelationObjectOrganize && orgList.Contains(b.ObjectId)).Where((a, b) => a.EnabledMark == 1 && a.DeleteMark == null)
                .Where((a, b) => a.RealName.Contains(input.keyword) || a.Account.Contains(input.keyword))
                .GroupBy((a, b) => new { a.Id, a.RealName, a.Account, a.EnabledMark })
                .Select((a, b) => new {
                    id = a.Id,
                    fullName = SqlFunc.MergeString(a.RealName, "/", a.Account),
                    enabledMark = a.EnabledMark,
                    type = "user",
                    icon = "icon-ym icon-ym-tree-user2",
                    hasChildren = false,
                    isLeaf = true
                }).ToListAsync();
                output.AddRange(res.Adapt<List<OrganizeMemberListOutput>>());
            }
            else
            {
                List<OrganizeEntity>? allOrg = _organizeService.GetOrgListTreeName();

                List<OrganizeEntity>? data = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                    .Where(o => orgList.Contains(o.Id) && o.DeleteMark == null && o.EnabledMark == 1)
                    .OrderBy(o => o.SortCode).ToListAsync();

                foreach (OrganizeEntity? o in data)
                {
                    if (o.OrganizeIdTree.IsNullOrEmpty()) o.OrganizeIdTree = o.Id;
                    if (!data.Where(x => x.Id != o.Id && o.OrganizeIdTree.Contains(x.OrganizeIdTree)).Any())
                    {
                        output.Add(new OrganizeMemberListOutput
                        {
                            id = o.Id,
                            fullName = allOrg.FirstOrDefault(x => x.Id.Equals(o.Id))?.Description,
                            enabledMark = o.EnabledMark,
                            type = o.Category,
                            icon = "icon-ym icon-ym-tree-organization3",
                            hasChildren = true,
                            isLeaf = false
                        });
                    }
                }
            }
        }
        else
        {
            List<OrganizeEntity>? allOrg = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.DeleteMark == null && o.EnabledMark == 1).OrderBy(o => o.ParentId).ToListAsync();

            var res = await _repository.AsSugarClient().Queryable<UserEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.UserId == a.Id))
                .Where((a, b) => b.ObjectType == CommonConst.RelationObjectOrganize && b.ObjectId == input.organizeId).Where((a, b) => a.EnabledMark == 1 && a.DeleteMark == null)
                .GroupBy((a, b) => new { a.Id, a.RealName, a.Account, a.EnabledMark })
                .Select((a, b) => new {
                    id = a.Id,
                    fullName = SqlFunc.MergeString(a.RealName, "/", a.Account),
                    enabledMark = a.EnabledMark,
                    type = "user",
                    icon = "icon-ym icon-ym-tree-user2",
                    hasChildren = false,
                    isLeaf = true
                }).ToListAsync();
            output.AddRange(res.Adapt<List<OrganizeMemberListOutput>>());
            var departmentList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.OrganizeIdTree.Contains(input.organizeId) && orgList.Contains(o.Id)).ToListAsync();

            departmentList.OrderBy(x => x.OrganizeIdTree.Length).ToList().ForEach(o =>
            {
                o.FullName = orgTreeNameList.FirstOrDefault(x => x.Id.Equals(o.Id)).Description;

                if (o.Id != input.organizeId && !output.Any(x => o.FullName.Contains(x.fullName)))
                {
                    var pName = string.Empty;
                    if (!departmentList.Any(x => x.Id == o.ParentId)) pName = orgTreeNameList.FirstOrDefault(x => x.Id.Equals(input.organizeId)).Description;
                    else pName = orgTreeNameList.FirstOrDefault(x => x.Id.Equals(o.ParentId)).Description;
                    output.Add(new OrganizeMemberListOutput()
                    {
                        id = o.Id,
                        fullName = o.FullName.Replace(pName + "/", string.Empty),
                        enabledMark = o.EnabledMark,
                        type = o.Category,
                        icon = o.Category.Equals(CommonConst.OrganizeTypeGroup) || o.Category.Equals(CommonConst.OrganizeTypeInstitution) ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                        hasChildren = true,
                        isLeaf = false
                    });
                }
            });
        }

        // 获取 所属组织的所有成员
        List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && output.Select(x => x.id).Contains(x.UserId)).ToListAsync();

        // 用户头像
        var usersHead = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => output.Select(xx => xx.id).Contains(x.Id)).Select(x => new { id = x.Id, headIcon = x.HeadIcon }).ToListAsync();

        // 处理组织树
        output.ForEach(item =>
        {
            if (item.type.Equals("user"))
            {
                var head = usersHead.Find(x => x.id.Equals(item.id)).headIcon;
                item.headIcon = head.IsNullOrEmpty() ? "/api/file/Image/userAvatar/001.png" : "/api/file/Image/userAvatar/" + head;
            }
            var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
            var oTree = orgTreeNameList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
            item.organize = string.Join(",", oTree);
        });
        return output;
    }

    /// <summary>
    /// 获取IM用户列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("ImUser")]
    public async Task<dynamic> GetImUserList([FromQuery] PageInputBase input)
    {
        SqlSugarPagedList<IMUserListOutput>? list = await _repository.AsSugarClient().Queryable<UserEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId))
            .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
            .Where(a => a.Id != _userManager.UserId && a.EnabledMark == 1 && a.DeleteMark == null).OrderBy(a => a.SortCode)
            .Select((a, b) => new IMUserListOutput
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", a.HeadIcon),
                department = b.FullName,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<IMUserListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取下拉框用户列表,根据用户同岗位Id.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}/SelectorByUserPosId")]
    public async Task<dynamic> GetSelectorByUserPosId(string id, [FromQuery] PageInputBase input)
    {
        var posIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId.Equals(id) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).Select(x => x.ObjectId).ToListAsync();
        var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => posIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
        var data = await _repository.AsQueryable()
            .Where(a => a.DeleteMark == null && a.EnabledMark != 0 && !a.Account.Equals("admin") && !a.Id.Equals(id) && userIds.Contains(a.Id))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select(a => new UserListOutput
            {
                id = a.Id,
                fullName = SqlFunc.MergeString(a.RealName, "/", a.Account),
                headIcon = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(e => e.Id == a.Id).Select(e => SqlFunc.MergeString("/api/File/Image/userAvatar/", e.HeadIcon)),

            }).ToPagedListAsync(input.currentPage, input.pageSize);

        return data;
    }

    /// <summary>
    /// 获取下拉框（公司+部门+用户）.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        List<OrganizeEntity>? organizeList = await _organizeService.GetListAsync();
        List<UserEntity>? userList = await _repository.AsQueryable().Where(t => t.EnabledMark == 1 && t.DeleteMark == null).OrderBy(u => u.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
        List<UserSelectorOutput>? organizeTreeList = organizeList.Adapt<List<UserSelectorOutput>>();
        List<UserSelectorOutput>? treeList = userList.Adapt<List<UserSelectorOutput>>();
        treeList = treeList.Concat(organizeTreeList).ToList();
        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        UserEntity? entity = await _repository.GetFirstAsync(u => u.Id == id);
        SysConfigEntity? config = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("lockType") && x.Category.Equals("SysConfig")).FirstAsync();
        string? configLockType = config?.Value;
        entity.EnabledMark = configLockType.IsNotEmptyOrNull() && configLockType == "2" && entity.EnabledMark == 2 && entity.UnLockTime < DateTime.Now ? 1 : entity.EnabledMark;
        UserInfoOutput? output = entity.Adapt<UserInfoOutput>();
        if (output.headIcon == "/api/File/Image/userAvatar/") output.headIcon = string.Empty;
        return output;
    }

    /// <summary>
    /// 获取当前用户所属机构下属成员.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("getOrganization")]
    public async Task<dynamic> GetOrganizeMember([FromQuery] UserListQuery input)
    {
        if (input.organizeId.IsNotEmptyOrNull() && input.organizeId != "0") input.organizeId = input.organizeId.Split(",").LastOrDefault();
        else input.organizeId = _userManager.User.OrganizeId;

        // 获取所属组织的所有成员
        List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize).ToListAsync();

        var res = await _repository.AsQueryable()
                .WhereIF(!input.keyword.IsNullOrEmpty(), u => u.Account.Contains(input.keyword) || u.RealName.Contains(input.keyword))
                .Where(u => u.EnabledMark == 1 && u.DeleteMark == null && userList.Where(x => x.ObjectId == input.organizeId).Select(x => x.UserId).Contains(u.Id)).OrderBy(o => o.SortCode)
                .Select(u => new OrganizeMemberListOutput
                {
                    id = u.Id,
                    fullName = SqlFunc.MergeString(u.RealName, "/", u.Account),
                    enabledMark = u.EnabledMark,
                    icon = "icon-ym icon-ym-tree-user2",
                    headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon),
                    isLeaf = true,
                    hasChildren = false,
                    type = "user",
                }).ToListAsync();

        if (res.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // 处理组织树
            res.ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return res;
    }

    /// <summary>
    /// 获取工作交接内容.
    /// </summary>
    /// <param name="fromId">移交人Id.</param>
    /// <returns></returns>
    [HttpGet("getWorkByUser")]
    public async Task<dynamic> GetWorkByUser([FromQuery] string fromId)
    {
        var res = new UserWorkHandoverModel();
        res.flowTask = _flowTaskRepository.GetWorkHandover(fromId, 1);
        res.flow = _flowTaskRepository.GetWorkHandover(fromId, 2);

        var whereLambda = LinqExpression.And<SystemEntity>();
        whereLambda = whereLambda.And(x => x.DeleteMark == null && x.IsMain == 0);
        var permissionIds = _userManager.GetPermissionIds(fromId, false);
        var isDevRole = await _repository.AsSugarClient().Queryable<RoleEntity>().AnyAsync(x => x.DeleteMark == null && x.EnCode.Equals(CommonConst.RoleCodeTypeDeveloper) && permissionIds.Contains(x.Id));
        if (isDevRole)
            whereLambda = whereLambda.And(x => x.UserId == fromId || x.AuthorizeId.Equals(CommonConst.SystemAuthCodeAllDevUser) || x.AuthorizeId.Contains(fromId));
        else
            whereLambda = whereLambda.And(x => x.UserId == fromId);

        res.app = await _repository.AsSugarClient().Queryable<SystemEntity>()
            .Where(whereLambda)
            .OrderBy(x => x.SortCode).OrderByDescending(x => x.CreatorTime)
            .Select(x => new SystemListSelector()
            {
                id = x.Id,
                fullName = x.FullName,
                enCode = x.EnCode,
                icon = x.Icon,
            }).ToListAsync();

        res.isAppShow = res.app.Any();
        return res;
    }

    /// <summary>
    /// 当前用户相关人员.
    /// </summary>
    /// <param name="type">2:同一部门、3:同一岗位、6:同一公司.</param>
    /// <returns></returns>
    [HttpGet("ReceiveUserList")]
    public async Task<dynamic> ReceiveUserList([FromQuery] string type, [FromQuery] UserListQuery input)
    {
        var userConditionInput = new UserConditionInput
        {
            type = type,
            pagination = new PageInputBase
            {
                keyword = input.keyword,
                pageSize = input.pageSize,
                currentPage = input.currentPage
            },
            userIds = (await GetList()).Select(x => x.Id).ToList()
        };
        return _userRelationService.GetUserPage(userConditionInput);
    }

    /// <summary>
    /// 获取用户权限类型.
    /// </summary>
    /// <param name="userId">主键.</param>
    /// <returns></returns>
    [HttpGet("GetAllPermission")]
    public async Task<dynamic> GetAllPermission([FromQuery] string userId)
    {
        if (userId.IsNullOrEmpty()) userId = _userManager.UserId;
        var res = new List<UserPermissionOutput>()
        {
            new UserPermissionOutput(){ id = "all" , fullName = "全部权限" , children = new List<UserPermissionOutput>() },
            new UserPermissionOutput(){ id = "position" , fullName = "所属岗位" , children = new List<UserPermissionOutput>() , hasChildren=true },
            new UserPermissionOutput(){ id = "role" , fullName = "所属角色" , children = new List<UserPermissionOutput>() , hasChildren=true },
        };

        var posIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)).Select(x => x.ObjectId).ToList();

        res.Find(x => x.id.Equals("position")).children = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => posIdList.Contains(x.Id))
            .Select(x => new UserPermissionOutput()
            {
                id = x.Id,
                fullName = SqlFunc.MergeString(SqlFunc.Subqueryable<OrganizeEntity>().EnableTableFilter().Where(xx => xx.Id.Equals(x.OrganizeId)).Select(xx => xx.OrganizeNameTree), "/", x.FullName),
                type = "position"
            }).ToListAsync();

        var roleIdList = _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectUser)).Select(x => x.RoleId).ToList();

        res.Find(x => x.id.Equals("role")).children = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null && roleIdList.Contains(x.Id))
            .Select(x => new UserPermissionOutput() { id = x.Id, fullName = x.FullName, type = "role" }).ToListAsync();

        if (!res.Find(x => x.id.Equals("position")).children.Any()) res.RemoveAll(x => x.id.Equals("position"));
        if (!res.Find(x => x.id.Equals("role")).children.Any()) res.RemoveAll(x => x.id.Equals("role"));

        return res;
    }

    /// <summary>
    /// 获取用户权限 .
    /// </summary>
    /// <returns></returns>
    [HttpGet("Authorize")]
    public async Task<dynamic> GetUserAuthorize([FromQuery] string userId, string objectId, string objectType)
    {
        if (userId.IsNullOrEmpty()) userId = _userManager.UserId;
        var IsAdministrator = await _repository.AsQueryable().Where(x => x.Id.Equals(userId)).Select(x => x.IsAdministrator).FirstAsync();
        var output = new UsersCurrentAuthorizeOutput()
        {
            module = new List<UsersCurrentAuthorizeMoldel>(),
            button = new List<UsersCurrentAuthorizeMoldel>(),
            column = new List<UsersCurrentAuthorizeMoldel>(),
            form = new List<UsersCurrentAuthorizeMoldel>(),
            resource = new List<UsersCurrentAuthorizeMoldel>(),
            flow = new List<UsersCurrentAuthorizeMoldel>(),
            print = new List<UsersCurrentAuthorizeMoldel>(),
        };

        // 根据用户Id 捞取所有权限
        var authorizeIdList = new List<string>();
        if (objectId.Equals("all"))
        {
            authorizeIdList = _userManager.GetPermissionIds(userId, false);
        }
        else
        {
            authorizeIdList.Add(objectId);
            if (objectType.Equals(CommonConst.RelationObjectPosition))
            {
                var posRoleIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectId.Equals(objectId)).Select(x => x.RoleId).ToListAsync();
                authorizeIdList.AddRange(posRoleIds);
            }
        }

        var mainSystemId = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => x.EnCode.Equals(CommonConst.SystemCodeMain)).Select(x => x.Id).FirstAsync();
        var itemList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().WhereIF(IsAdministrator != 1, x => authorizeIdList.Contains(x.ObjectId)).Select(x => new { x.ItemId, x.ItemType }).ToListAsync();
        if (itemList.Any(x => x.ItemId.Equals("XTCD"))) itemList.Add(new { ItemId = mainSystemId, ItemType = CommonConst.AuthorizeItemSystem });
        var system = await _repository.AsSugarClient().Queryable<SystemEntity>()
            .Where(x => !x.EnCode.Equals(CommonConst.SystemCodeTeamwork) && !x.EnCode.Equals(CommonConst.SystemCodeWorkFlow) && x.DeleteMark == null)
            .WhereIF(IsAdministrator != 1, x => itemList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemSystem)).Select(xx => xx.ItemId).Contains(x.Id))
            .Select(x => new UsersCurrentAuthorizeMoldel() { id = x.Id, parentId = "-1", moduleId = x.EnCode, fullName = x.FullName, icon = x.Icon, sortCode = x.SortCode, creatorTime = x.CreatorTime })
            .OrderBy(x => x.sortCode).OrderBy(x => x.creatorTime, OrderByType.Desc).ToListAsync();

        var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>()
            .WhereIF(IsAdministrator != 1, x => itemList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemModule)).Select(xx => xx.ItemId).Contains(x.Id) && x.DeleteMark == null)
            .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc).ToListAsync();

        var mainSystem = system.Find(x => x.moduleId.Equals(CommonConst.SystemCodeMain));
        if (mainSystem != null) mainSystem.fullName = "系统菜单";

        foreach (var item in system)
        {
            if (moduleList.Any(it => it.Category.Equals("Web") && it.SystemId != null && it.SystemId.Equals(item.id)))
            {
                moduleList.Where(it => it.Category.Equals("Web") && it.SystemId != null && it.SystemId.Equals(item.id) && it.ParentId.Equals("-1")).ToList().ForEach(it =>
                {
                    it.ParentId = item.id + ((!item.id.Equals(mainSystemId)) ? "--WEB" : string.Empty);
                });
                if (!item.id.Equals(mainSystemId))
                {
                    moduleList.Add(new ModuleEntity()
                    {
                        Id = item.id + "--WEB",
                        FullName = "WEB菜单",
                        Icon = "icon-ym icon-ym-pc",
                        ParentId = item.id,
                        Category = "Web",
                        Type = 1,
                        SortCode = 99998
                    });
                }
            }
            if (moduleList.Any(it => it.Category.Equals("App") && it.SystemId != null && it.SystemId.Equals(item.id)))
            {
                moduleList.Where(it => it.Category.Equals("App") && it.SystemId != null && it.SystemId.Equals(item.id) && it.ParentId.Equals("-1")).ToList().ForEach(it =>
                {
                    it.ParentId = item.id + "--APP";
                });
                moduleList.Add(new ModuleEntity()
                {
                    Id = item.id + "--APP",
                    FullName = "APP菜单",
                    Icon = "icon-ym icon-ym-mobile",
                    ParentId = item.id,
                    Category = "App",
                    Type = 1,
                    SortCode = 99999
                });
            }
        }
        foreach (var mItem in moduleList.Where(x => x.EnCode != null && (x.EnCode.Equals(CommonConst.SystemCodeTeamwork) || x.EnCode.Equals(CommonConst.SystemCodeWorkFlow)) && x.ParentId == "-1").ToList())
        {
            mItem.ParentId = mainSystemId;
        }

        var module = moduleList.Adapt<List<UsersCurrentAuthorizeMoldel>>();
        module.AddRange(system);
        output.module.AddRange(module);
        var allModule = output.module.ToObject<List<UsersCurrentAuthorizeMoldel>>();
        output.module = output.module.ToTree("-1");

        output.button = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>()
            .WhereIF(IsAdministrator != 1, x => itemList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemButton)).Select(xx => xx.ItemId).Contains(x.Id))
            .Select(x => new UsersCurrentAuthorizeMoldel() { id = x.Id, parentId = x.ModuleId, fullName = x.FullName, icon = x.Icon }).ToListAsync();
        allModule = allModule.ToObject<List<UsersCurrentAuthorizeMoldel>>();
        var fids = GetOutPutByChild(allModule, output.button.Select(x => x.parentId).ToList());
        output.button.AddRange(allModule.Where(x => fids.Contains(x.id)).ToList());
        output.button = output.button.ToTree("-1");

        output.column = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>()
            .WhereIF(IsAdministrator != 1, x => itemList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemColumn)).Select(xx => xx.ItemId).Contains(x.Id))
            .Select(x => new UsersCurrentAuthorizeMoldel() { id = x.Id, parentId = x.ModuleId, fullName = x.FullName }).ToListAsync();
        allModule = allModule.ToObject<List<UsersCurrentAuthorizeMoldel>>();
        fids = GetOutPutByChild(allModule, output.column.Select(x => x.parentId).ToList());
        output.column.AddRange(allModule.Where(x => fids.Contains(x.id)).ToList());
        output.column = output.column.ToTree("-1");

        output.form = await _repository.AsSugarClient().Queryable<ModuleFormEntity>()
            .WhereIF(IsAdministrator != 1, x => itemList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemForm)).Select(xx => xx.ItemId).Contains(x.Id))
            .Select(x => new UsersCurrentAuthorizeMoldel() { id = x.Id, parentId = x.ModuleId, fullName = x.FullName }).ToListAsync();
        allModule = allModule.ToObject<List<UsersCurrentAuthorizeMoldel>>();
        fids = GetOutPutByChild(allModule, output.form.Select(x => x.parentId).ToList());
        output.form.AddRange(allModule.Where(x => fids.Contains(x.id)).ToList());
        output.form = output.form.ToTree("-1");

        output.resource = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>()
            .WhereIF(IsAdministrator != 1, x => itemList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemResource)).Select(xx => xx.ItemId).Contains(x.Id))
            .Select(x => new UsersCurrentAuthorizeMoldel() { id = x.Id, parentId = x.ModuleId, fullName = x.FullName }).ToListAsync();
        allModule = allModule.ToObject<List<UsersCurrentAuthorizeMoldel>>();
        fids = GetOutPutByChild(allModule, output.resource.Select(x => x.parentId).ToList());
        output.resource.AddRange(allModule.Where(x => fids.Contains(x.id)).ToList());
        output.resource = output.resource.ToTree("-1");

        #region 流程、打印权限
        var isGetAll = await _repository.AsQueryable().AnyAsync(x => x.Id.Equals(userId) && x.IsAdministrator.Equals(1));
        if (authorizeIdList.Any() || isGetAll)
        {
            var items = new List<string>();
            var printItems = new List<string>();
            if (!isGetAll)
            {
                var authorize = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().In(a => a.ObjectId, authorizeIdList).ToListAsync();
                items = authorize.FindAll(x => x.ItemType == CommonConst.AuthorizeItemFlow).Select(x => x.ItemId).ToList();
                printItems = authorize.FindAll(x => x.ItemType == CommonConst.AuthorizeItemPrint).Select(x => x.ItemId).ToList();
            }

            //var VFID = await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().WhereIF(!isGetAll, x => items.Contains(x.Id)).Where(x => SqlFunc.ToInt32(SqlFunc.JsonField(x.FlowConfig, "visibleType")) == 2 && x.Status.Equals(1)).Select(x => x.Id).ToListAsync();
            var flows = await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>().Where(a => a.VisibleType == 2 && a.Status == 1).Where(a => a.EnabledMark == 1 && a.DeleteMark == null)
                .WhereIF(!isGetAll, a => items.Contains(a.Id))
                .Select(a => new UsersCurrentAuthorizeMoldel
                {
                    id = a.FlowId,
                    parentId = a.SystemId,
                    fullName = a.FullName,
                    icon = a.Icon,
                    sortCode = a.SortCode,
                    isLeaf = true,
                }).OrderBy(q => q.sortCode).ToListAsync();

            if (flows.Any())
            {
                var dicDataList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => flows.Select(xx => xx.parentId).Contains(x.Id))
                    .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime).ToListAsync();
                foreach (var item in dicDataList)
                {
                    flows.Add(new UsersCurrentAuthorizeMoldel()
                    {
                        fullName = item.FullName,
                        parentId = "0",
                        id = item.Id,
                    });
                }
            }

            output.flow.AddRange(flows.ToTree().Where(x => x.children != null && x.children.Any()).ToList());

            var prints = await _repository.AsSugarClient().Queryable<PrintDevEntity>()
                .Where(x => x.DeleteMark == null && x.CommonUse == 1 && x.VisibleType == 2)
                .WhereIF(!isGetAll, x => printItems.Contains(x.Id))
                .OrderBy(x => x.SortCode)
                .OrderBy(x => x.CreatorTime, OrderByType.Desc)
                .Select(x => new UsersCurrentAuthorizeMoldel
                {
                    id = x.Id,
                    parentId = x.SystemId,
                    fullName = x.FullName,
                    icon = x.Icon,
                    sortCode = x.SortCode,
                    isLeaf = true,
                }).ToListAsync();

            if (prints.Any())
            {
                var dicDataList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => prints.Select(xx => xx.parentId).Contains(x.Id))
                    .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime).ToListAsync();
                foreach (var item in dicDataList)
                {
                    prints.Add(new UsersCurrentAuthorizeMoldel()
                    {
                        fullName = item.FullName,
                        parentId = "0",
                        id = item.Id,
                    });
                }
            }

            output.print.AddRange(prints.ToTree().Where(x => x.children != null && x.children.Any()).ToList());
        }
        #endregion

        return output;
    }

    /// <summary>
    /// 获取用户岗位 .
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}/GetPosition")]
    public async Task<dynamic> GetPositionByUserId(string id)
    {
        var data = await _repository.AsSugarClient().Queryable<PositionEntity, UserRelationEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Inner, a.Id == b.ObjectId && a.DeleteMark == null && b.UserId.Equals(id) && b.ObjectType.Equals(CommonConst.RelationObjectPosition)))
            .Select((a, b) => new {
                id = a.Id
            }).ToListAsync();

        return data;
    }

    #endregion

    #region POST

    /// <summary>
    /// 根据用户Id List 获取当前用户Id.
    /// </summary>
    /// <returns></returns>
    [HttpPost("getDefaultCurrentValueUserId")]
    public async Task<dynamic> GetDefaultCurrentValueUserId([FromBody] GetDefaultCurrentValueInput input)
    {
        if ((input.UserIds == null || !input.UserIds.Any()) && (input.DepartIds == null || !input.DepartIds.Any()) && (input.PositionIds == null || !input.PositionIds.Any())
            && (input.RoleIds == null || !input.RoleIds.Any()) && (input.GroupIds == null || !input.GroupIds.Any())) return new { userId = _userManager.UserId };

        var userRelationList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Select(x => new UserRelationEntity() { UserId = x.UserId, ObjectId = x.ObjectId }).ToList();
        var userIdList = userRelationList.Where(x => input.UserIds.Contains(x.UserId) || input.DepartIds.Contains(x.ObjectId)
            || input.PositionIds.Contains(x.ObjectId) || input.RoleIds.Contains(x.ObjectId) || input.GroupIds.Contains(x.ObjectId)).Select(x => x.UserId).ToList();

        if (userIdList.Contains(_userManager.UserId)) return new { userId = _userManager.UserId };
        else return new { userId = string.Empty };
    }

    /// <summary>
    /// 获取.
    /// </summary>
    /// <returns></returns>
    [HttpPost("GetUserList")]
    public async Task<dynamic> GetUserList([FromBody] UserRelationInput input)
    {
        var data = await _repository.AsQueryable().Where(it => it.EnabledMark > 0 && it.DeleteMark == null)
            .Where(it => input.ids.Contains(it.Id))
            .Select(it => new OrganizeMemberListOutput()
            {
                id = it.Id,
                fullName = SqlFunc.MergeString(it.RealName, "/", it.Account),
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", it.HeadIcon),
                enabledMark = it.EnabledMark,
                isAdministrator = it.IsAdministrator,
            }).ToListAsync();

        data = data.OrderBy(x => input.ids.IndexOf(x.id)).ToList();
        if (data.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // 获取 所属组织的所有成员
            List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && data.Select(x => x.id).Contains(x.UserId)).ToListAsync();

            // 处理组织树
            data.ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return new { list = data };
    }

    /// <summary>
    /// 获取机构成员列表.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("ImUser/Selector/{organizeId}")]
    public async Task<dynamic> GetOrganizeMemberList(string organizeId, [FromBody] PageInputBase input)
    {
        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();
        var orgList = _organizeService.GetOrgListTreeName();
        if (!input.keyword.IsNullOrEmpty())
        {
            var outList = await _repository.AsQueryable()
                .WhereIF(!input.keyword.IsNullOrEmpty(), u => u.Account.Contains(input.keyword) || u.RealName.Contains(input.keyword))
                .Where(u => u.EnabledMark > 0 && u.DeleteMark == null).OrderBy(o => o.SortCode)
                .Select(u => new OrganizeMemberListOutput
                {
                    id = u.Id,
                    fullName = SqlFunc.MergeString(u.RealName, "/", u.Account),
                    enabledMark = SqlFunc.IIF(u.EnabledMark == 2 && u.UnLockTime < DateTime.Now, 1, u.EnabledMark),
                    icon = "icon-ym icon-ym-tree-user2",
                    headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon),
                    isLeaf = true,
                    hasChildren = false,
                    type = "user",
                    isAdministrator = u.IsAdministrator,
                }).ToPagedListAsync(input.currentPage, input.pageSize);

            if (outList.list.Any())
            {
                // 获取 所属组织的所有成员
                List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                    .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && outList.list.Select(x => x.id).Contains(x.UserId)).ToListAsync();

                // 处理组织树
                outList.list.Where(x => x.type.Equals("user")).ToList().ForEach(item =>
                {
                    var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                    var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                    item.organize = string.Join(",", oTree);
                });
            }

            return PageResult<OrganizeMemberListOutput>.SqlSugarPageResult(outList);
        }
        else
        {
            var pOrganize = orgList.FirstOrDefault(x => x.Id.Equals(organizeId));

            output = await _organizeService.GetOrganizeMemberList(organizeId);
            if (pOrganize != null) output.ForEach(item => item.fullName = item.fullName.Replace(pOrganize.FullName + "/", string.Empty));
        }

        if (output.Any())
        {
            // 获取 所属组织的所有成员
            List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && output.Select(x => x.id).Contains(x.UserId)).ToListAsync();

            // 处理组织树
            output.Where(x => x.type.Equals("user")).ToList().ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return new { list = output };
    }

    /// <summary>
    /// 获取下拉框 根据权限.
    /// </summary>
    /// <returns></returns>
    [HttpPost("GetListByAuthorize/{organizeId}")]
    public async Task<dynamic> GetListByAuthorize(string organizeId, [FromBody] KeywordInput input)
    {
        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();
        if (!input.keyword.IsNullOrEmpty())
        {
            var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Select(x => x.UserId).ToListAsync();

            output = await _repository.AsQueryable()
                .Where(u => u.Account.Contains(input.keyword) || u.RealName.Contains(input.keyword))
                .Where(u => u.EnabledMark > 0 && u.DeleteMark == null && userIds.Contains(u.Id)).OrderBy(o => o.SortCode)
                .Select(u => new OrganizeMemberListOutput
                {
                    id = u.Id,
                    fullName = SqlFunc.MergeString(u.RealName, "/", u.Account),
                    enabledMark = SqlFunc.IIF(u.EnabledMark == 2 && u.UnLockTime < DateTime.Now, 1, u.EnabledMark),
                    icon = "icon-ym icon-ym-tree-user2",
                    headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon),
                    isLeaf = true,
                    hasChildren = false,
                    type = "user",
                }).Take(50).ToListAsync();

        }
        else
        {
            output = await GetOrganizeMemberList(organizeId);
        }

        if (output.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();
            // 获取所属组织的所有成员
            List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && output.Select(x => x.id).Contains(x.UserId)).ToListAsync();

            // 处理组织树
            output.Where(x => x.type.Equals("user")).ToList().ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return new { list = output.DistinctBy(x => x.id).ToList() };
    }

    /// <summary>
    /// 获取当前用户下属成员.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("getSubordinates")]
    public async Task<dynamic> GetSubordinate([FromBody] KeywordInput input)
    {
        var res = await _repository.AsQueryable()
                   .WhereIF(!input.keyword.IsNullOrEmpty(), u => u.Account.Contains(input.keyword) || u.RealName.Contains(input.keyword))
                   .Where(u => u.EnabledMark == 1 && u.DeleteMark == null && u.ManagerId == _userManager.UserId).OrderBy(o => o.SortCode)
                   .Select(u => new OrganizeMemberListOutput
                   {
                       id = u.Id,
                       fullName = SqlFunc.MergeString(u.RealName, "/", u.Account),
                       enabledMark = u.EnabledMark,
                       icon = "icon-ym icon-ym-tree-user2",
                       headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon),
                       isLeaf = true,
                       hasChildren = false,
                       type = "user",
                   }).ToListAsync();

        // ��ȡ������֯�����г�Ա
        List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => res.Select(xx => xx.id).Contains(x.UserId)).ToListAsync();

        if (res.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // ������֯��
            res.ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return res;
    }

    /// <summary>
    /// 获取当前用户所属机构下属成员.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("GetUsersByPositionId")]
    public async Task<dynamic> GetUsersByPositionId([FromQuery] UserListQuery input)
    {
        List<OrganizeMemberListOutput>? outData = new List<OrganizeMemberListOutput>();
        UserEntity? user = _userManager.User;

        // 获取岗位所属组织信息
        OrganizeMemberListOutput? orgInfo = await _repository.AsSugarClient().Queryable<PositionEntity, OrganizeEntity>((a, b) =>
                new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId && b.EnabledMark == 1 && b.DeleteMark == null))
            .Where((a, b) => a.Id == input.positionId).Select((a, b) => new OrganizeMemberListOutput
            {
                id = b.Id,
                fullName = b.FullName,
                enabledMark = b.EnabledMark,
                type = b.Category,
                parentId = "0",
                organize = b.Id,
                icon = b.Category.Equals(CommonConst.OrganizeTypeGroup) || b.Category.Equals(CommonConst.OrganizeTypeInstitution) ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                hasChildren = true,
                isLeaf = false
            }).FirstAsync();

        var orgList = _organizeService.GetOrgListTreeName();

        // 处理组织树
        if (orgInfo.organize.IsNotEmptyOrNull())
        {
            orgInfo.fullName = orgList.FirstOrDefault(x => x.Id.Equals(orgInfo.organize))?.Description;
        }

        outData.Add(orgInfo);

        // 获取岗位所属组织的所有成员
        List<OrganizeMemberListOutput>? userData = await _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.UserId))
            .Where((a, b) => a.ObjectType == CommonConst.RelationObjectOrganize && a.ObjectId == orgInfo.id && b.EnabledMark == 1 && b.DeleteMark == null)
            .WhereIF(!input.keyword.IsNullOrEmpty(), (a, b) => b.Account.Contains(input.keyword) || b.RealName.Contains(input.keyword))
            .Select((a, b) => new OrganizeMemberListOutput
            {
                id = b.Id,
                parentId = orgInfo.id,
                fullName = SqlFunc.MergeString(b.RealName, "/", b.Account),
                enabledMark = b.EnabledMark,
                icon = "icon-ym icon-ym-tree-user2",
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", b.HeadIcon),
                isLeaf = true,
                hasChildren = false,
                type = "user"
            }).ToListAsync();

        // 获取 所属组织的所有成员
        List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && userData.Select(x => x.id).Contains(x.UserId)).ToListAsync();

        // 处理组织树
        userData.ForEach(item =>
        {
            var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
            var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
            item.organize = string.Join(",", oTree);
        });
        outData.AddRange(userData);

        return outData.ToTree("0");
    }

    /// <summary>
    /// 通过部门、岗位、用户、角色、分组id获取用户列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("UserCondition")]
    public async Task<dynamic> UserCondition([FromBody] UserConditionInput input)
    {
        SqlSugarPagedList<UserListOutput>? data = new SqlSugarPagedList<UserListOutput>();

        if (input.departIds == null) input.departIds = new List<string>();
        if (input.positionIds != null) input.departIds.AddRange(input.positionIds);
        if (input.roleIds != null) input.departIds.AddRange(input.roleIds);
        if (input.groupIds != null) input.departIds.AddRange(input.groupIds);
        if (data.list == null) data.list = new List<UserListOutput>();
        if (!input.departIds.Any()) return PageResult<UserListOutput>.SqlSugarPageResult(data);
        var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.UserId))
            .Where((a, b) => b.DeleteMark == null)
            .WhereIF(input.departIds.Any() || input.userIds.Any(), (a, b) => input.departIds.Contains(a.ObjectId) || input.userIds.Contains(b.Id))
            .WhereIF(input.pagination.keyword.IsNotEmptyOrNull(), (a, b) => b.Account.Contains(input.pagination.keyword) || b.RealName.Contains(input.pagination.keyword))
            .Select((a, b) => b.Id).Distinct().ToListAsync();
        data = await _repository.AsQueryable().Where(x => ids.Contains(x.Id)).Select(x => new UserListOutput()
        {
            id = x.Id,
            organizeId = x.OrganizeId,
            account = x.Account,
            fullName = SqlFunc.MergeString(x.RealName, "/", x.Account),
            headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", x.HeadIcon),
            gender = x.Gender,
            mobilePhone = x.MobilePhone
        }).ToPagedListAsync(input.pagination.currentPage, input.pagination.pageSize);
        if (data.list.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // 获取所属组织的所有成员
            List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && data.list.Select(x => x.id).Contains(x.UserId)).ToListAsync();

            // 处理组织树
            data.list.ToList().ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });

        }

        return PageResult<UserListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取选中组织、岗位、角色、分组、用户基本信息.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("GetSelectedList")]
    public async Task<dynamic> GetSelectedList([FromBody] UserSelectedInput input)
    {
        return await _organizeService.GetSelectedList(input);
    }

    /// <summary>
    /// 获取用户基本信息.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("GetSelectedUserList")]
    public async Task<dynamic> GetSelectedUserList([FromBody] UserSelectedInput input)
    {
        var oIdList = new List<string>();
        foreach (var id in input.ids)
        {
            if (id.Equals("@userId"))
            {
                oIdList.Add(_userManager.UserId);
                continue;
            }
            var oid = id.Split("--").First();
            if (id.Contains("--user"))
            {
                oIdList.Add(oid);
            }
            else if (id.Contains("--org"))
            {
                var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(oid)).Select(x => x.UserId).ToListAsync();
                oIdList.AddRange(ids);
            }
            else if (id.Contains("--subOrg"))
            {
                var oids = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(xx => xx.ParentId.Equals(oid) || xx.Id.Equals(oid)).Select(xx => xx.Id).ToListAsync();
                var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => oids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                oIdList.AddRange(ids);
            }
            else if (id.Contains("--progenyOrg"))
            {
                var oids = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(xx => xx.OrganizeIdTree.Contains(oid)).Select(xx => xx.Id).ToListAsync();
                var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => oids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                oIdList.AddRange(ids);
            }
            else if (id.Contains("--pos"))
            {
                var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(oid)).Select(x => x.UserId).ToListAsync();
                oIdList.AddRange(ids);
            }
            else if (id.Contains("--subPos"))
            {
                var pids = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(xx => xx.ParentId.Equals(oid)).Select(xx => xx.Id).ToListAsync();
                var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => pids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                oIdList.AddRange(ids);
            }
            else if (id.Contains("--progenyPos"))
            {
                var pids = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(xx => xx.PositionIdTree.Contains(oid)).Select(xx => xx.Id).ToListAsync();
                var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => pids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                oIdList.AddRange(ids);
            }
            else if (id.Contains("--role"))
            {
                var ids = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => oid.Equals(x.RoleId)).Select(x => x.ObjectId).ToListAsync();
                oIdList.AddRange(ids);
            }
            else if (id.Contains("--group"))
            {
                var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(oid)).Select(x => x.UserId).ToListAsync();
                oIdList.AddRange(ids);
            }
            else
            {
                oIdList.Add(id);
            }
        }

        var data = await _repository.AsQueryable().Where(x => x.DeleteMark == null && oIdList.Distinct().Contains(x.Id))
            .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword) || a.MobilePhone.Contains(input.keyword))
            .WhereIF(!_userManager.IsAdministrator, x => !x.Account.Equals("admin"))
            .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime).Select(x => new UserSelectedOutput()
            {
                id = x.Id,
                fullName = SqlFunc.MergeString(x.RealName, "/", x.Account),
                icon = "icon-ym icon-ym-tree-user2",
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", x.HeadIcon),
                type = "user",
                gender = x.Gender,
                mobilePhone = x.MobilePhone
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<UserSelectedOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] UserCrInput input)
    {
        if (_tenant.MultiTenancy)
        {
            var tenatInfo = await _tenantManager.GetTenant(_userManager.TenantId);

            // 是否超过租户账号额度
            if (tenatInfo.accountNum != 0 && tenatInfo.accountNum <= await _repository.AsQueryable().CountAsync(x => x.DeleteMark == null))
                throw Oops.Oh(ErrorCode.D1041);
        }

        if (await _repository.IsAnyAsync(u => u.Account == input.account && u.DeleteMark == null)) throw Oops.Oh(ErrorCode.D1003);
        UserEntity? entity = input.Adapt<UserEntity>();

        #region 用户表单
        entity.Id = SnowflakeIdHelper.NextId();
        entity.IsAdministrator = 0;
        entity.EntryDate = input.entryDate.IsNullOrEmpty() ? null : input.entryDate;
        entity.Birthday = input.birthday.IsNullOrEmpty() ? null : input.birthday;
        entity.QuickQuery = PinyinHelper.PinyinString(input.realName);
        entity.Secretkey = Guid.NewGuid().ToString();

        var defaultPassWord = await _repository.AsSugarClient().Queryable<SysConfigEntity>()
            .Where(it => it.Key.Equals("newUserDefaultPassword"))
            .Select(it => it.Value)
            .FirstAsync();
        entity.Password = MD5Encryption.Encrypt(MD5Encryption.Encrypt(defaultPassWord) + entity.Secretkey);
        string? headIcon = input.headIcon?.Split('/').ToList().Last();
        if (string.IsNullOrEmpty(headIcon))
            headIcon = "001.png";
        entity.HeadIcon = headIcon;

        #endregion

        try
        {
            // 用户组关联
            var userRelationEntity = new UserRelationEntity();
            userRelationEntity.ObjectType = CommonConst.RelationObjectGroup;
            userRelationEntity.ObjectId = input.groupId;
            userRelationEntity.SortCode = 0;
            userRelationEntity.UserId = entity.Id;
            await _repository.AsSugarClient().Insertable(userRelationEntity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

            // 角色关联(默认有使用者身份)
            var roleRelationEntity = new RoleRelationEntity();
            roleRelationEntity.ObjectType = CommonConst.RelationObjectUser;
            roleRelationEntity.ObjectId = entity.Id;
            roleRelationEntity.SortCode = 0;
            roleRelationEntity.RoleId = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.EnCode.Equals(CommonConst.RoleCodeTypeUser)).Select(x => x.Id).FirstAsync();
            await _repository.AsSugarClient().Insertable(roleRelationEntity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

            // 用户设定默认身份
            entity.Standing = await _repository.AsSugarClient().Queryable<StandingEntity>().Where(x => x.EnCode.Equals(CommonConst.RoleCodeTypeUser) && x.IsSystem.Equals(1)).Select(x => x.Id).FirstAsync();
            entity.AppStanding = entity.Standing;

            // 新增用户记录
            await _repository.AsInsertable(entity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

            // 将临时文件迁移至正式文件
            FileHelper.MoveFile(Path.Combine(FileVariable.TemporaryFilePath, headIcon), Path.Combine(FileVariable.UserAvatarFilePath, headIcon));

            // 单点登录同步
            entity.Password = defaultPassWord;
            await syncUserInfo(entity, "create", _userManager.TenantId);
        }
        catch (Exception)
        {
            throw Oops.Bah(ErrorCode.D5001);
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
        UserEntity? entity = await _repository.GetFirstAsync(u => u.Id == id && u.DeleteMark == null);

        _ = entity ?? throw Oops.Oh(ErrorCode.D5002);
        if (entity.IsAdministrator == (int)AccountType.Administrator)
            throw Oops.Oh(ErrorCode.D1014);
        if (entity.Id == _userManager.UserId)
            throw Oops.Oh(ErrorCode.D1001);
        entity.DeleteTime = DateTime.Now;
        entity.DeleteMark = 1;
        entity.DeleteUserId = _userManager.UserId;

        #region 第三方同步

        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            if (sysConfig.dingDepartment.IsNotEmptyOrNull() && sysConfig.dingSynIsSynUser)
            {
                var authIds = _userManager.GetPermissionIds(entity.Id, false);
                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.dingDepartment)).Select(x => x.Id).ToListAsync();
                if (await _repository.AsSugarClient().Queryable<PositionEntity>().AnyAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && authIds.Contains(x.Id)))
                    await _synThirdInfoService.DelSynData(2, 2, sysConfig, entity.Id);
            }

            if (sysConfig.qyhDepartment.IsNotEmptyOrNull() && sysConfig.qyhIsSynUser)
            {
                var authIds = _userManager.GetPermissionIds(entity.Id, false);
                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.qyhDepartment)).Select(x => x.Id).ToListAsync();
                if (await _repository.AsSugarClient().Queryable<PositionEntity>().AnyAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && authIds.Contains(x.Id)))
                    await _synThirdInfoService.DelSynData(1, 2, sysConfig, entity.Id);
            }
        }
        catch (Exception)
        {
        }

        #endregion

        // 用户软删除
        await _repository.AsUpdateable(entity).UpdateColumns(it => new { it.DeleteTime, it.DeleteMark, it.DeleteUserId }).ExecuteCommandAsync();

        // 变更涉及的岗位责任人
        var posList = await _repository.AsSugarClient().Updateable<PositionEntity>().SetColumns(it => it.DutyUserId == null).Where(x => x.DutyUserId.Equals(id)).ExecuteCommandAsync();

        // 直接删除用户关系表相关相关数据
        await _userRelationService.Delete(id);
        await _repository.AsSugarClient().Deleteable<RoleRelationEntity>().Where(u => u.ObjectId == id).ExecuteCommandAsync();

        await ForcedOffline(id, "logout", "当前账号已删除");

        // 单点登录同步
        await syncUserInfo(entity, "delete", _userManager.TenantId);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] UserUpInput input)
    {
        UserEntity? oldUserEntity = await _repository.GetFirstAsync(it => it.Id == id);

        // 超级管理员 只有 admin 账号才有变更权限
        if (_userManager.UserId != oldUserEntity.Id && oldUserEntity.IsAdministrator == 1 && _userManager.Account != "admin")
            throw Oops.Oh(ErrorCode.D1033);

        // 排除自己并且判断与其他是否相同
        if (await _repository.IsAnyAsync(u => u.Account == input.account && u.DeleteMark == null && u.Id != id)) throw Oops.Oh(ErrorCode.D1003);

        UserEntity? entity = input.Adapt<UserEntity>();
        entity.QuickQuery = PinyinHelper.PinyinString(input.realName);
        string? headIcon = input.headIcon.Split('/').ToList().Last();
        entity.HeadIcon = headIcon;
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;
        if (entity.EnabledMark == 2) entity.UnLockTime = null;
        if (entity.EnabledMark.Equals(0)) await ForcedOffline(id, "logout", "当前账号已禁用");
        if (entity.EnabledMark.Equals(2)) await ForcedOffline(id, "logout", "当前账号已锁定");

        try
        {
            // 更新用户记录
            int newEntity = await _repository.AsUpdateable(entity).UpdateColumns(it => new {
                it.Account,
                it.RealName,
                it.QuickQuery,
                it.Gender,
                it.Email,
                it.SortCode,
                it.EnabledMark,
                it.Description,
                it.HeadIcon,
                it.Nation,
                it.NativePlace,
                it.EntryDate,
                it.CertificatesType,
                it.CertificatesNumber,
                it.Education,
                it.UrgentContacts,
                it.UrgentTelePhone,
                it.PostalAddress,
                it.MobilePhone,
                it.Birthday,
                it.TelePhone,
                it.Landline,
                it.UnLockTime,
                it.GroupId,
                it.Ranks,
                it.LastModifyTime,
                it.LastModifyUserId
            }).ExecuteCommandAsync();

            // 将临时文件迁移至正式文件
            FileHelper.MoveFile(Path.Combine(FileVariable.TemporaryFilePath, headIcon), Path.Combine(FileVariable.UserAvatarFilePath, headIcon));

        }
        catch (Exception)
        {
            FileHelper.MoveFile(Path.Combine(FileVariable.UserAvatarFilePath, headIcon), Path.Combine(FileVariable.TemporaryFilePath, headIcon));
            throw Oops.Oh(ErrorCode.D5004);
        }

        #region 第三方同步

        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            var userList = new List<UserEntity>() { entity };
            if (sysConfig.dingDepartment.IsNotEmptyOrNull() && sysConfig.dingSynIsSynUser)
            {
                var authIds = _userManager.GetPermissionIds(entity.Id, false);
                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.dingDepartment)).Select(x => x.Id).ToListAsync();
                if (await _repository.AsSugarClient().Queryable<PositionEntity>().AnyAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && authIds.Contains(x.Id)))
                    await _synThirdInfoService.SynUser(2, 2, sysConfig, userList);
            }

            if (sysConfig.qyhDepartment.IsNotEmptyOrNull() && sysConfig.qyhIsSynUser)
            {
                var authIds = _userManager.GetPermissionIds(entity.Id, false);
                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.qyhDepartment)).Select(x => x.Id).ToListAsync();
                if (await _repository.AsSugarClient().Queryable<PositionEntity>().AnyAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && authIds.Contains(x.Id)))
                    await _synThirdInfoService.SynUser(1, 2, sysConfig, userList);
            }
        }
        catch (Exception)
        {
        }

        #endregion

        // 单点登录同步
        await syncUserInfo(entity, "update", _userManager.TenantId);
    }

    /// <summary>
    /// 批量更新状态.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("BatchUpdateState")]
    public async Task BatchUpdateState(UserBatchSelectedInput input)
    {
        int isOk = await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(it => new UserEntity()
        {
            EnabledMark = input.enabledMark,
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => input.ids.Contains(it.Id)).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D5005);
        if (input.enabledMark.Equals(0)) foreach (var id in input.ids) await ForcedOffline(id, "logout", "当前账号已禁用");
        if (input.enabledMark.Equals(2)) foreach (var id in input.ids) await ForcedOffline(id, "logout", "当前账号已锁定");
    }

    /// <summary>
    /// 批量删除.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("BatchDelete")]
    public async Task BatchDelete(UserBatchSelectedInput input)
    {
        var userList = await _repository.AsQueryable().Where(x => input.ids.Contains(x.Id)).ToListAsync();
        var delUserList = new List<UserEntity>();
        foreach (var entity in userList)
        {
            if (entity.IsAdministrator == (int)AccountType.Administrator)
                throw Oops.Oh(ErrorCode.D1014);
            if (entity.Id == _userManager.UserId)
                throw Oops.Oh(ErrorCode.D1001);
            entity.DeleteTime = DateTime.Now;
            entity.DeleteMark = 1;
            entity.DeleteUserId = _userManager.UserId;
            delUserList.Add(entity);
        }

        foreach (var entity in delUserList)
        {
            // 用户软删除
            await _repository.AsUpdateable(entity).UpdateColumns(it => new { it.DeleteTime, it.DeleteMark, it.DeleteUserId }).ExecuteCommandAsync();

            // 变更涉及的岗位责任人
            var posList = await _repository.AsSugarClient().Updateable<PositionEntity>().SetColumns(it => it.DutyUserId == null).Where(x => input.ids.Contains(x.DutyUserId)).ExecuteCommandAsync();

            // 直接删除用户关系表相关相关数据
            await _userRelationService.Delete(entity.Id);
            await _repository.AsSugarClient().Deleteable<RoleRelationEntity>().Where(u => u.ObjectId == entity.Id).ExecuteCommandAsync();

            await ForcedOffline(entity.Id, "logout", "当前账号已删除");

            #region 第三方同步

            try
            {
                var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
                if (sysConfig.dingDepartment.IsNotEmptyOrNull() && sysConfig.dingSynIsSynUser)
                {
                    var authIds = _userManager.GetPermissionIds(entity.Id, false);
                    var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.dingDepartment)).Select(x => x.Id).ToListAsync();
                    if (await _repository.AsSugarClient().Queryable<PositionEntity>().AnyAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && authIds.Contains(x.Id)))
                        await _synThirdInfoService.DelSynData(2, 2, sysConfig, entity.Id);
                }

                if (sysConfig.qyhDepartment.IsNotEmptyOrNull() && sysConfig.qyhIsSynUser)
                {
                    var authIds = _userManager.GetPermissionIds(entity.Id, false);
                    var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.qyhDepartment)).Select(x => x.Id).ToListAsync();
                    if (await _repository.AsSugarClient().Queryable<PositionEntity>().AnyAsync(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId) && authIds.Contains(x.Id)))
                        await _synThirdInfoService.DelSynData(1, 2, sysConfig, entity.Id);
                }
            }
            catch (Exception)
            {
            }

            #endregion

            // 单点登录同步
            await syncUserInfo(entity, "delete", _userManager.TenantId);
        }
    }

    /// <summary>
    /// 重置密码.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/ResetPassword")]
    public async Task ResetPassword(string id, [FromBody] UserResetPasswordInput input)
    {
        UserEntity? entity = await _repository.GetFirstAsync(u => u.Id == id && u.DeleteMark == null);

        if (!_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (!input.userPassword.Equals(input.validatePassword))
            throw Oops.Oh(ErrorCode.D5006);
        _ = entity ?? throw Oops.Oh(ErrorCode.D1002);

        string? password = MD5Encryption.Encrypt(input.userPassword + entity.Secretkey);

        int isOk = await _repository.AsUpdateable().SetColumns(it => new UserEntity()
        {
            Password = password,
            ChangePasswordDate = SqlFunc.GetDate(),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandAsync();

        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D5005);

        // 重置该用户密码，该用户会立即退出登录
        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);
        if (list != null)
        {
            var user = list.Find(it => it.tenantId == _userManager.TenantId && it.userId == id);
            if (user != null)
            {
                await _imHandler.SendMessageAsync(user.connectionId, new { method = "logout", msg = "密码已变更，请重新登录！" }.ToJsonString());

                // 删除在线用户ID
                list.RemoveAll((x) => x.connectionId == user.connectionId);
                await _cacheManager.SetAsync(onlineCacheKey, list);

                // 删除用户登录信息缓存
                var cacheKey = string.Format("{0}:{1}:{2}", _userManager.TenantId, CommonConst.CACHEKEYUSER, user.userId);
                await _cacheManager.DelAsync(cacheKey);
            }
        }

        // 单点登录同步
        entity.Password = input.userPassword;
        await syncUserInfo(entity, "modifyPassword", _userManager.TenantId);
    }

    /// <summary>
    /// 解除锁定.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/Unlock")]
    public async Task Unlock(string id)
    {
        UserEntity? entity = await _repository.GetFirstAsync(u => u.Id == id && u.DeleteMark == null);
        if (!_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);
        int isOk = await _repository.AsUpdateable().SetColumns(it => new UserEntity()
        {
            LockMark = 0, // 解锁
            LogErrorCount = 0, // 解锁
            EnabledMark = 1, // 解锁
            UnLockTime = DateTime.Now, // 取消解锁时间
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandAsync();

        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D5005);
    }

    /// <summary>
    /// 保存工作交接.
    /// </summary>
    /// <param name="input">主键.</param>
    /// <returns></returns>
    [HttpPost("workHandover")]
    public async Task SaveWorkHandover([FromBody] UserWorkHandoverInput input)
    {
        try
        {
            if (input.handoverUser.Equals(input.fromId)) throw Oops.Oh(ErrorCode.D5027);
            if (await _repository.IsAnyAsync(x => x.Id.Equals(input.handoverUser) && x.Account.Equals("admin"))) throw Oops.Oh(ErrorCode.D5028);

            // 记录 被交接人Id
            int isOk = await _repository.AsUpdateable().SetColumns(it => new UserEntity()
            {
                HandoverUserId = input.fromId,
                LastModifyUserId = _userManager.UserId,
                LastModifyTime = SqlFunc.GetDate()
            }).Where(it => it.Id == input.handoverUser).ExecuteCommandAsync();

            // 交接应用
            if (input.appList != null && input.appList.Any())
            {
                var systemList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => input.appList.Contains(x.Id)).ToListAsync();
                foreach (var item in systemList)
                {
                    if (item.UserId.Equals(input.fromId)) item.UserId = input.appHandoverUser;
                    if (item.AuthorizeId.IsNotEmptyOrNull()) item.AuthorizeId = item.AuthorizeId.Replace(input.fromId, input.appHandoverUser);
                    else item.AuthorizeId = input.appHandoverUser;
                }

                await _repository.AsSugarClient().Updateable(systemList).ExecuteCommandAsync();
            }

            // 待办
            if (input.flowTaskList != null && input.flowTaskList.Any()) _flowTaskRepository.SaveWorkHandover(input.handoverUser, input.flowTaskList, 1, input.fromId);

            // 负责流程
            if (input.flowList != null && input.flowList.Any()) _flowTaskRepository.SaveWorkHandover(input.handoverUser, input.flowList, 2, input.fromId);

        }
        catch (Exception e)
        {
            throw e;
        }
    }

    /// <summary>
    /// 设置用户岗位 .
    /// </summary>
    /// <returns></returns>
    [HttpPost("{id}/SetPosition")]
    public async Task SetPosition(string id, [FromBody] UserSelectedInput input)
    {
        // 选择的岗位列表
        var posList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => input.ids.Contains(x.Id)).ToListAsync();
        var addList = new List<UserRelationEntity>();

        foreach (var item in posList)
        {
            #region 岗位约束验证

            if (item.IsCondition.Equals(1) && item.ConditionJson.IsNotEmptyOrNull())
            {
                var condition = item.ConditionJson.ToObject<Entitys.Dto.Position.PositionConstraintModel>();
                if (condition.constraintType.Any())
                {
                    if (condition.constraintType.Contains(1))
                    {
                        var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(item.Id)).Select(x => x.UserId).ToListAsync();
                        userIds.Add(id);
                        if (userIds.Distinct().Count() > condition.userNum)
                            throw Oops.Oh(ErrorCode.D6017, "岗位");
                    }
                    if (condition.constraintType.Contains(0) && posList.Any(x => condition.mutualExclusion.Contains(x.Id)))
                    {
                        var resList = posList.Where(x => condition.mutualExclusion.Contains(x.Id)).Select(x => x.FullName).ToList();
                        resList.Insert(0, item.FullName);
                        throw Oops.Oh(ErrorCode.D6017, "岗位");
                    }
                    if (condition.constraintType.Contains(2) && !posList.Any(x => condition.prerequisite.Contains(x.Id)))
                    {
                        var resList = posList.Where(x => condition.prerequisite.Contains(x.Id)).Select(x => x.FullName).ToList();
                        resList.Insert(0, item.FullName);
                        throw Oops.Oh(ErrorCode.D6017, "岗位");
                    }
                }
            }

            #endregion

            addList.Add(new UserRelationEntity()
            {
                UserId = id,
                ObjectType = CommonConst.RelationObjectPosition,
                ObjectId = item.Id
            });

            if (!addList.Any(x => x.UserId.Equals(id) && x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && x.ObjectId.Equals(item.OrganizeId)))
            {
                addList.Add(new UserRelationEntity()
                {
                    UserId = id,
                    ObjectType = CommonConst.RelationObjectOrganize,
                    ObjectId = item.OrganizeId
                });
            }
        }

        var userEntity = await _repository.AsQueryable().Where(x => x.Id.Equals(id)).FirstAsync();
        if (posList.Any() && !input.ids.Contains(userEntity.PositionId))
        {
            userEntity.PositionId = posList.First().Id;
            userEntity.OrganizeId = posList.First().OrganizeId;
            await _repository.AsSugarClient().Updateable(userEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }

        // 删除旧的关联数据
        await _repository.AsSugarClient().Deleteable<UserRelationEntity>().Where(x => (x.ObjectType.Equals(CommonConst.RelationObjectPosition) || x.ObjectType.Equals(CommonConst.RelationObjectOrganize)) && x.UserId.Equals(id)).ExecuteCommandAsync();

        // 新增关联数据
        await _repository.AsSugarClient().Insertable(addList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

        #region 第三方同步

        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            if (sysConfig.dingDepartment.IsNotEmptyOrNull() && await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().AnyAsync(x => x.ThirdType == 2 && x.DataType == 1 && x.EnabledMark == 1))
            {
                var oldSynEntity = await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().FirstAsync(x => x.ThirdType == 2 && x.DataType == 2 && x.SysObjId == userEntity.Id);
                if (oldSynEntity.IsNotEmptyOrNull())
                {
                    if (oldSynEntity.ThirdObjId.IsNotEmptyOrNull()) await _synThirdInfoService.DelSynData(2, 2, sysConfig, oldSynEntity.SysObjId);
                    await _repository.AsSugarClient().Deleteable(oldSynEntity).ExecuteCommandAsync();
                }

                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.dingDepartment)).Select(x => x.Id).ToListAsync();
                var oPosList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId)).ToListAsync();
                foreach (var itemId in input.ids.Intersect(oPosList.Select(x => x.Id)))
                {
                    if (sysConfig.dingSynIsSynUser)
                    {
                        var newList = new List<UserEntity>() { userEntity };
                        await _synThirdInfoService.SynUser(2, 2, sysConfig, newList);
                    }
                    else
                    {
                        var synEntity = new SynThirdInfoEntity() { ThirdType = 2, DataType = 2, SysObjId = userEntity.Id, EnabledMark = 0 };
                        await _repository.AsSugarClient().Insertable(synEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    }
                }
            }

            if (sysConfig.qyhDepartment.IsNotEmptyOrNull() && await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().AnyAsync(x => x.ThirdType == 1 && x.DataType == 1 && x.EnabledMark == 1))
            {
                var oldSynEntity = await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().FirstAsync(x => x.ThirdType == 1 && x.DataType == 2 && x.SysObjId == userEntity.Id);
                if (oldSynEntity.IsNotEmptyOrNull())
                {
                    if (oldSynEntity.ThirdObjId.IsNotEmptyOrNull()) await _synThirdInfoService.DelSynData(1, 2, sysConfig, oldSynEntity.SysObjId);
                    await _repository.AsSugarClient().Deleteable(oldSynEntity).ExecuteCommandAsync();
                }

                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.qyhDepartment)).Select(x => x.Id).ToListAsync();
                var oPosList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId)).ToListAsync();
                foreach (var item in input.ids.Intersect(oPosList.Select(x => x.Id)))
                {
                    if (sysConfig.qyhIsSynUser)
                    {
                        var newList = new List<UserEntity>() { userEntity };
                        await _synThirdInfoService.SynUser(1, 2, sysConfig, newList);
                    }
                    else
                    {
                        var synEntity = new SynThirdInfoEntity() { ThirdType = 1, DataType = 2, SysObjId = userEntity.Id, EnabledMark = 0 };
                        await _repository.AsSugarClient().Insertable(synEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        #endregion

        await ForcedOffline(id);
    }

    /// <summary>
    /// 批量设置用户岗位 .
    /// </summary>
    /// <returns></returns>
    [HttpPost("SetPositionBatch")]
    [UnitOfWork]
    [NonUnify]
    public async Task<dynamic> BatchSetPosition([FromBody] UserSelectedInput input)
    {
        var posList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => input.ids.Contains(x.Id)).ToListAsync();

        // 选择的岗位列表
        var addList = new List<UserRelationEntity>();

        // 错误列表
        var errorList = new List<string>();
        foreach (var id in input.userIds)
        {
            foreach (var item in posList)
            {
                #region 岗位约束验证

                if (item.IsCondition.Equals(1) && item.ConditionJson.IsNotEmptyOrNull())
                {
                    var condition = item.ConditionJson.ToObject<Entitys.Dto.Position.PositionConstraintModel>();
                    if (condition.constraintType.Any())
                    {
                        if (condition.constraintType.Contains(1))
                        {
                            var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectPosition) && x.ObjectId.Equals(item.Id)).Select(x => x.UserId).ToListAsync();
                            userIds.AddRange(input.userIds);
                            userIds.Add(id);
                            if (userIds.Distinct().Count() > condition.userNum)
                            {
                                errorList.Add(id);
                                continue;
                            }
                        }
                        if (condition.constraintType.Contains(0))
                        {
                            if (posList.Any(x => condition.mutualExclusion.Contains(x.Id)))
                            {
                                errorList.Add(id);
                                continue;
                            }
                        }
                        if (condition.constraintType.Contains(2))
                        {
                            if (!posList.Any(x => x.Id.Equals(condition.prerequisite)))
                            {
                                errorList.Add(id);
                                continue;
                            }
                        }
                    }
                }

                #endregion

                addList.Add(new UserRelationEntity()
                {
                    UserId = id,
                    ObjectType = CommonConst.RelationObjectPosition,
                    ObjectId = item.Id
                });

                if (!addList.Any(x => x.UserId.Equals(id) && x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && x.ObjectId.Equals(item.OrganizeId)))
                {
                    addList.Add(new UserRelationEntity()
                    {
                        UserId = id,
                        ObjectType = CommonConst.RelationObjectOrganize,
                        ObjectId = item.OrganizeId
                    });
                }
            }
        }

        if (errorList.Any())
        {
            return new UnifyResult.RESTfulResult<object>()
            {
                code = 503,
                msg = "修改失败"
            };
        }

        if (addList.Any())
        {
            // 删除旧的关联数据
            await _repository.AsSugarClient().Deleteable<UserRelationEntity>()
                .Where(x => (x.ObjectType.Equals(CommonConst.RelationObjectPosition) || x.ObjectType.Equals(CommonConst.RelationObjectOrganize)) && addList.Select(xx => xx.UserId).Contains(x.UserId)).ExecuteCommandAsync();

            // 新增关联数据
            await _repository.AsSugarClient().Insertable(addList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

            var userList = await _repository.AsQueryable().Where(x => input.userIds.Contains(x.Id)).ToListAsync();
            foreach (var user in userList)
            {
                if (!addList.Any(x => x.ObjectType.Equals(CommonConst.RelationObjectPosition) && x.ObjectId.Equals(user.PositionId)))
                {
                    user.PositionId = posList.First().Id;
                    user.OrganizeId = posList.First().OrganizeId;
                }

                await ForcedOffline(user.Id);
            }
            await _repository.AsSugarClient().Updateable(userList).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }

        #region 第三方同步

        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            if (sysConfig.dingDepartment.IsNotEmptyOrNull() && await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().AnyAsync(x => x.ThirdType == 2 && x.DataType == 1 && x.EnabledMark == 1))
            {
                var userList = await _repository.AsQueryable().Where(x => input.userIds.Contains(x.Id)).ToListAsync();
                foreach (var user in userList)
                {
                    var oldSynEntity = await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().FirstAsync(x => x.ThirdType == 2 && x.DataType == 2 && x.SysObjId == user.Id);
                    if (oldSynEntity.IsNotEmptyOrNull())
                    {
                        if (oldSynEntity.ThirdObjId.IsNotEmptyOrNull()) await _synThirdInfoService.DelSynData(2, 2, sysConfig, oldSynEntity.SysObjId);
                        await _repository.AsSugarClient().Deleteable(oldSynEntity).ExecuteCommandAsync();
                    }
                }

                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.dingDepartment)).Select(x => x.Id).ToListAsync();
                var oPosList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId)).ToListAsync();
                foreach (var itemId in input.ids.Intersect(oPosList.Select(x => x.Id)))
                {
                    if (sysConfig.dingSynIsSynUser)
                    {
                        await _synThirdInfoService.SynUser(2, 2, sysConfig, userList);
                    }
                    else
                    {
                        var synList = new List<SynThirdInfoEntity>();
                        foreach (var user in userList) synList.Add(new SynThirdInfoEntity() { ThirdType = 2, DataType = 2, SysObjId = user.Id, EnabledMark = 0 });
                        await _repository.AsSugarClient().Insertable(synList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    }
                }
            }

            if (sysConfig.qyhDepartment.IsNotEmptyOrNull() && await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().AnyAsync(x => x.ThirdType == 1 && x.DataType == 1 && x.EnabledMark == 1))
            {
                var userList = await _repository.AsQueryable().Where(x => input.userIds.Contains(x.Id)).ToListAsync();
                foreach (var user in userList)
                {
                    var oldSynEntity = await _repository.AsSugarClient().Queryable<SynThirdInfoEntity>().FirstAsync(x => x.ThirdType == 1 && x.DataType == 2 && x.SysObjId == user.Id);
                    if (oldSynEntity.IsNotEmptyOrNull())
                    {
                        if (oldSynEntity.ThirdObjId.IsNotEmptyOrNull()) await _synThirdInfoService.DelSynData(1, 2, sysConfig, oldSynEntity.SysObjId);
                        await _repository.AsSugarClient().Deleteable(oldSynEntity).ExecuteCommandAsync();
                    }
                }

                var orgIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(sysConfig.qyhDepartment)).Select(x => x.Id).ToListAsync();
                var oPosList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null && orgIds.Contains(x.OrganizeId)).ToListAsync();
                foreach (var item in input.ids.Intersect(oPosList.Select(x => x.Id)))
                {
                    if (sysConfig.qyhIsSynUser)
                    {
                        await _synThirdInfoService.SynUser(1, 2, sysConfig, userList);
                    }
                    else
                    {
                        var synList = new List<SynThirdInfoEntity>();
                        foreach (var user in userList) synList.Add(new SynThirdInfoEntity() { ThirdType = 1, DataType = 2, SysObjId = user.Id, EnabledMark = 0 });
                        await _repository.AsSugarClient().Insertable(synList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        #endregion

        return new UnifyResult.RESTfulResult<object>()
        {
            code = 200,
            msg = "修改成功"
        };
    }

    #endregion

    #region 导入导出

    /// <summary>
    /// 导出Excel.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("ExportData")]
    public async Task<dynamic> ExportData([FromQuery] UserExportDataInput input)
    {
        // 处理组织树 名称
        List<OrganizeEntity>? orgTreeNameList = _organizeService.GetOrgListTreeName();

        #region 获取组织层级

        List<string>? childOrgIds = new List<string>();
        if (input.organizeId.IsNotEmptyOrNull())
        {
            childOrgIds.Add(input.organizeId);

            // 根据组织Id 获取所有子组织Id集合
            childOrgIds.AddRange(orgTreeNameList.Where(x => x.OrganizeIdTree.Contains(input.organizeId)).Select(x => x.Id).ToList());
            childOrgIds = childOrgIds.Distinct().ToList();
        }

        #endregion

        // 用户信息列表
        List<UserListImportDataInput>? userList = new List<UserListImportDataInput>();
        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "certificateType" && x.DeleteMark == null && x.EnabledMark == 1);
        var dictionaryTypeEntity1 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "Education" && x.DeleteMark == null && x.EnabledMark == 1);
        var dictionaryTypeEntity2 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "sex" && x.DeleteMark == null && x.EnabledMark == 1);
        var dictionaryTypeEntity3 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "Nation" && x.DeleteMark == null && x.EnabledMark == 1);
        var dictionaryTypeEntity4 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "Rank" && x.DeleteMark == null && x.EnabledMark == 1);
        var query = _repository.AsQueryable()
            .Where(a => a.DeleteMark == null && !a.Account.Equals("admin"))
            .WhereIF(input.enabledMark != null, a => a.EnabledMark.Equals(input.enabledMark))
            .WhereIF(input.gender != null, a => a.Gender.Equals(input.gender))
            .WhereIF(childOrgIds.Any(), a => SqlFunc.Subqueryable<UserRelationEntity>().EnableTableFilter().Where(x => childOrgIds.Contains(x.ObjectId) && x.UserId.Equals(a.Id)).Any())
            .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).OrderByIF(!input.keyword.IsNullOrEmpty(), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new UserListImportDataInput()
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                birthday = a.Birthday.Value.ToString("yyyy-MM-dd"),
                certificatesNumber = a.CertificatesNumber,
                positionId = a.PositionId, // 岗位
                certificatesType = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.DictionaryTypeId == dictionaryTypeEntity.Id && d.Id == a.CertificatesType && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                education = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.DictionaryTypeId == dictionaryTypeEntity1.Id && d.Id == a.Education && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                gender = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.DictionaryTypeId == dictionaryTypeEntity2.Id && d.EnCode == a.Gender && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                nation = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.DictionaryTypeId == dictionaryTypeEntity3.Id && d.Id == a.Nation && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                description = a.Description,
                entryDate = a.EntryDate.Value.ToString("yyyy-MM-dd"),
                email = a.Email,
                enabledMark = SqlFunc.IF(a.EnabledMark.Equals(0)).Return("禁用").ElseIF(a.EnabledMark.Equals(1)).Return("启用").End("锁定"),
                mobilePhone = a.MobilePhone,
                nativePlace = a.NativePlace,
                postalAddress = a.PostalAddress,
                telePhone = a.TelePhone,
                urgentContacts = a.UrgentContacts,
                urgentTelePhone = a.UrgentTelePhone,
                landline = a.Landline,
                ranks = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.DictionaryTypeId == dictionaryTypeEntity4.Id && d.Id == a.Ranks && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                sortCode = a.SortCode.ToString()
            });
        if (input.dataType.Equals("0"))
        {
            userList = (await query.ToPagedListAsync(input.currentPage, input.pageSize)).list.ToList();
        }
        else
        {
            userList = await query.ToListAsync();
        }

        userList.ForEach(item =>
        {
            if (item.birthday.IsNotEmptyOrNull()) item.birthday = Convert.ToDateTime(item.birthday).ToString("yyyy-MM-dd");
            else item.birthday = string.Empty;
            if (item.entryDate.IsNotEmptyOrNull()) item.entryDate = Convert.ToDateTime(item.entryDate).ToString("yyyy-MM-dd");
            else item.entryDate = string.Empty;
        });

        List<PositionEntity>? plist = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.EnabledMark == 1 && it.DeleteMark == null).ToListAsync(); // 获取所有岗位
        List<RoleEntity>? rlist = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(it => it.EnabledMark == 1 && it.DeleteMark == null).ToListAsync(); // 获取所有角色

        // 获取用户组织关联数据
        var userRelation = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType == CommonConst.RelationObjectOrganize || x.ObjectType == CommonConst.RelationObjectPosition).Where(x => userList.Select(xx => xx.id).Contains(x.UserId))
            .Select(x => new { x.ObjectId, x.ObjectType, x.UserId }).ToListAsync();

        // 获取用户角色关联数据
        var roleRelation = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectType == CommonConst.RelationObjectUser).Where(x => userList.Select(xx => xx.id).Contains(x.ObjectId))
            .Select(x => new { x.ObjectId, x.RoleId }).ToListAsync();

        // 转换 组织结构 和 岗位(多岗位)
        foreach (UserListImportDataInput? item in userList)
        {
            // 获取用户岗位关联
            List<string>? posRelList = userRelation.Where(x => x.ObjectType == CommonConst.RelationObjectPosition && x.UserId == item.id).Select(x => x.ObjectId).ToList();
            if (posRelList.Any())
                item.positionId = string.Join(",", plist.Where(x => posRelList.Contains(x.Id)).Select(x => x.FullName + "/" + x.EnCode).ToList());
            else
                item.positionId = string.Empty;

            // 获取用户角色关联
            List<string>? roleRelList = roleRelation.Where(x => x.ObjectId == item.id).Select(x => x.RoleId).ToList();
            if (roleRelList.Any())
                item.roleId = string.Join(",", rlist.Where(x => roleRelList.Contains(x.Id)).Select(x => x.FullName + "/" + x.EnCode).ToList());
        }

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("用户信息_{0:yyyyMMddhhmmss}.xls", DateTime.Now);
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        foreach (var item in GetUserInfoFieldToTitle(input.selectKey.Split(',').ToList()))
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, Required = item.Required });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var fs = ExcelExportHelper<UserListImportDataInput>.ExportMemoryStream(userList, excelconfig);
        var flag = await _fileManager.UploadFileByType(fs, FileVariable.TemporaryFilePath, excelconfig.FileName);
        if (flag)
        {
            fs.Flush();
            fs.Close();
        }

        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 模板下载.
    /// </summary>
    /// <returns></returns>
    [HttpGet("TemplateDownload")]
    public async Task<dynamic> TemplateDownload()
    {
        // 初始化 一条空数据
        List<UserListImportDataInput>? dataList = new List<UserListImportDataInput>() { new UserListImportDataInput() {
            positionId = "岗位名称/岗位编码,岗位名称1/岗位编码1",
            roleId = "角色名称,角色名称1", birthday = "yyyy-MM-dd", entryDate = "yyyy-MM-dd" } };

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = "用户信息导入模板.xls";
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        var userInfoFields = GetUserInfoFieldToTitle();
        userInfoFields.RemoveAll(x => x.ColumnKey.Equals("errorsInfo"));
        foreach (var item in userInfoFields)
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, Required = item.Required, SelectList = item.SelectList });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var stream = ExcelExportHelper<UserListImportDataInput>.ToStream(dataList, excelconfig);
        await _fileManager.UploadFileByType(stream, FileVariable.TemporaryFilePath, excelconfig.FileName);
        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 上传文件.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Uploader")]
    public async Task<dynamic> Uploader(IFormFile file)
    {
        var _filePath = _fileManager.GetPathByType(string.Empty);
        var _fileName = DateTime.Now.ToString("yyyyMMdd") + "_" + SnowflakeIdHelper.NextId() + Path.GetExtension(file.FileName);
        var stream = file.OpenReadStream();
        await _fileManager.UploadFileByType(stream, _filePath, _fileName);
        return new { name = _fileName, url = string.Format("/api/File/Image/{0}/{1}", string.Empty, _fileName) };
    }

    /// <summary>
    /// 导入预览.
    /// </summary>
    /// <returns></returns>
    [HttpGet("ImportPreview")]
    public async Task<dynamic> ImportPreview(string fileName)
    {
        var excelData = await GetImportDataList(fileName);
        return new { dataRow = excelData };
    }

    /// <summary>
    /// 导出错误报告.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("ExportExceptionData")]
    [UnitOfWork]
    public async Task<dynamic> ExportExceptionData([FromBody] UserImportDataInput list)
    {
        //list.list.ForEach(it => it.errorsInfo = string.Empty);
        //object[]? res = await ImportUserData(list.list);

        // 错误数据
        List<UserListImportDataInput>? errorlist = list.list;

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("用户信息导入模板错误报告_{0}.xls", DateTime.Now.ToString("yyyyMMddHHmmss"));
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        foreach (var item in GetUserInfoFieldToTitle())
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, FreezePane = item.FreezePane, Required = item.Required, SelectList = item.SelectList });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var stream = ExcelExportHelper<UserListImportDataInput>.ToStream(errorlist, excelconfig);
        var flag = await _fileManager.UploadFileByType(stream, FileVariable.TemporaryFilePath, excelconfig.FileName);
        if (flag)
        {
            stream.Flush();
            stream.Close();
        }

        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("ImportData")]
    [UnitOfWork]
    public async Task<dynamic> ImportData([FromBody] UserImportDataInput list)
    {
        if (list.type)
        {
            var excelData = await GetImportDataList(list.fileName);
            list.list = excelData.ToObject<List<UserListImportDataInput>>();
        }

        list.list.ForEach(x => x.errorsInfo = string.Empty);
        object[]? res = await ImportUserData(list.list);
        List<UserEntity>? addlist = res.First() as List<UserEntity>;
        List<UserListImportDataInput>? errorlist = res.Last() as List<UserListImportDataInput>;
        return new UserImportResultOutput() { snum = addlist.Count, fnum = errorlist.Count, failResult = errorlist, resultType = errorlist.Count < 1 ? 0 : 1 };
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取用户信息 根据用户ID.
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="isEnabledMark">是否过滤启用</param>
    /// <returns></returns>
    [NonAction]
    public UserEntity GetInfoByUserId(string userId, bool isEnabledMark = true)
    {
        return _repository.AsQueryable().WhereIF(isEnabledMark, u => u.EnabledMark == 1).First(u => u.Id == userId && u.DeleteMark == null);
    }

    /// <summary>
    /// 获取用户直属主管.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="isAll">所有主管.</param>
    /// <returns></returns>
    [NonAction]
    public List<string> GetUserManager(string posId)
    {
        var posUserList = new List<string>();
        var position = _repository.AsSugarClient().Queryable<PositionEntity>().First(x => x.Id == posId && x.DeleteMark == null);
        if (position.IsNotEmptyOrNull() && position.OrganizeId.IsNotEmptyOrNull())
        {
            var positionIds1 = _repository.AsSugarClient().Queryable<PositionEntity>().ToParentList(it => it.ParentId, position.Id).Where(x => x.Id != position.Id).Select(x => x.Id).ToList();
            var positionIds2 = _repository.AsSugarClient().Queryable<OrganizeEntity>().ToParentList(it => it.ParentId, position.OrganizeId).Where(x => x.Id != position.OrganizeId).Select(x => x.DutyPositionId).ToList();
            positionIds1.AddRange(positionIds2);
            foreach (var item in positionIds1)
            {
                var positionItem = _repository.AsSugarClient().Queryable<PositionEntity>().First(x => x.Id == item && x.DeleteMark == null);
                if (positionItem.IsNotEmptyOrNull() && positionItem.DutyUserId.IsNotEmptyOrNull())
                {
                    posUserList.Add(positionItem.DutyUserId);
                }
                else
                {
                    posUserList.Add(string.Empty);
                }
            }
        }
        return posUserList;
    }

    /// <summary>
    /// 获取岗位责任人.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetDutyUserId(string id)
    {
        var dutyUserId = string.Empty;
        var position = _repository.AsSugarClient().Queryable<PositionEntity>().First(x => x.Id == id && x.DeleteMark == null);
        if (position.IsNullOrEmpty())
        {
            var org = _repository.AsSugarClient().Queryable<OrganizeEntity>().First(x => x.Id == id && x.DeleteMark == null);
            if (org.IsNotEmptyOrNull())
            {
                position = _repository.AsSugarClient().Queryable<PositionEntity>().First(x => x.Id == org.DutyPositionId && x.DeleteMark == null);
            }
        }
        if (position.IsNotEmptyOrNull() && position.DutyUserId.IsNotEmptyOrNull())
        {
            var user = GetInfoByUserId(position.DutyUserId);
            if (user.IsNotEmptyOrNull())
            {
                dutyUserId = user.Id;
            }
        }
        return dutyUserId;
    }

    /// <summary>
    /// 获取用户列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<UserEntity>> GetList(List<string> ids = null)
    {
        return await _repository.AsQueryable().WhereIF(ids.IsNotEmptyOrNull() && ids.Any(), u => ids.Contains(u.Id)).Where(u => u.EnabledMark == 1 && u.DeleteMark == null).ToListAsync();
    }

    /// <summary>
    /// 获取当前用户岗位信息.
    /// </summary>
    /// <param name="PositionIds"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<PositionInfoModel>> GetPosition(string organizeId)
    {
        return await _repository.AsSugarClient().Queryable<PositionEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id.Equals(b.ObjectId) && b.ObjectType.Equals(CommonConst.RelationObjectPosition))).Where((a, b) => a.OrganizeId.Equals(organizeId) && b.UserId.Equals(_userManager.UserId)).Select(a => new PositionInfoModel { id = a.Id, name = a.FullName }).ToListAsync();
    }

    /// <summary>
    /// 表达式获取用户列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<UserEntity>> GetUserListByExp(Expression<Func<UserEntity, bool>> expression)
    {
        return await _repository.AsQueryable().Where(expression).ToListAsync();
    }

    /// <summary>
    /// 表达式获取指定字段的用户列表.
    /// </summary>
    /// <param name="expression">where 条件表达式.</param>
    /// <param name="select">select 选择字段表达式.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<UserEntity>> GetUserListByExp(Expression<Func<UserEntity, bool>> expression, Expression<Func<UserEntity, UserEntity>> select)
    {
        return await _repository.AsQueryable().Where(expression).Select(select).ToListAsync();
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取集合中的组织 树,根据上级ID.
    /// </summary>
    /// <param name="list">组织 集合.</param>
    /// <param name="parentId">上级ID.</param>
    /// <param name="addList">返回.</param>
    /// <returns></returns>
    private List<string> GetOrganizeParentName(List<OrganizeEntity> list, string parentId, List<string> addList)
    {
        OrganizeEntity? entity = list.Find(x => x.Id == parentId);

        if (entity.ParentId != "-1") GetOrganizeParentName(list, entity.ParentId, addList);
        else addList.Add(entity.FullName);

        return addList;
    }

    /// <summary>
    /// 是否我的下属.
    /// </summary>
    /// <param name="userId">当前用户.</param>
    /// <param name="managerId">主管ID.</param>
    /// <param name="tier">层级.</param>
    /// <returns></returns>
    private async Task<bool> GetIsMyStaff(string userId, string managerId, int tier)
    {
        bool isMyStaff = false;
        if (tier <= 0) return true;
        string? superiorUserId = (await _repository.GetFirstAsync(it => it.Id.Equals(managerId) && it.DeleteMark == null))?.ManagerId;
        if (superiorUserId == null)
        {
            isMyStaff = false;
        }
        else if (userId == superiorUserId)
        {
            isMyStaff = true;
        }
        else
        {
            tier--;
            isMyStaff = await GetIsMyStaff(userId, superiorUserId, tier);
        }

        return isMyStaff;
    }

    /// <summary>
    /// 用户信息 字段对应 列名称.
    /// </summary>
    /// <returns></returns>
    private List<ExportImportHelperModel> GetUserInfoFieldToTitle(List<string> fields = null)
    {
        var typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "963255a34ea64a2584c5d1ba269c1fe6" || x.EnCode == "sex") && x.DeleteMark == null).First();
        var _genderList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList(); // 性别

        typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "b6cd65a763fa45eb9fe98e5057693e40" || x.EnCode == "Nation") && x.DeleteMark == null).First();
        var _nationList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList(); // 民族

        typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "7866376d5f694d4d851c7164bd00ebfc" || x.EnCode == "certificateType") && x.DeleteMark == null).First();
        var certificateTypeList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList(); // 证件类型

        typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "6a6d6fb541b742fbae7e8888528baa16" || x.EnCode == "Education") && x.DeleteMark == null).First();
        var educationList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList(); // 文化程度

        typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "485719133245509" || x.EnCode == "Rank") && x.DeleteMark == null).First();
        var ranksList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList(); // 职级

        var userRoleList = _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null && x.Type.Equals(CommonConst.RelationObjectUser)).Select(x => x.FullName).ToList(); // 用户角色

        var res = new List<ExportImportHelperModel>();
        res.Add(new ExportImportHelperModel() { ColumnKey = "errorsInfo", ColumnValue = "异常原因", FreezePane = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "positionId", ColumnValue = "所属岗位" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "roleId", ColumnValue = "角色" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "account", ColumnValue = "账户", Required = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "realName", ColumnValue = "姓名", Required = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "gender", ColumnValue = "性别", Required = true, SelectList = _genderList });
        res.Add(new ExportImportHelperModel() { ColumnKey = "email", ColumnValue = "电子邮箱" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "ranks", ColumnValue = "职级", SelectList = ranksList });
        res.Add(new ExportImportHelperModel() { ColumnKey = "nation", ColumnValue = "民族", SelectList = _nationList });
        res.Add(new ExportImportHelperModel() { ColumnKey = "nativePlace", ColumnValue = "籍贯" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "entryDate", ColumnValue = "入职时间" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "certificatesType", ColumnValue = "证件类型", SelectList = certificateTypeList });
        res.Add(new ExportImportHelperModel() { ColumnKey = "certificatesNumber", ColumnValue = "证件号码" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "education", ColumnValue = "文化程度", SelectList = educationList });
        res.Add(new ExportImportHelperModel() { ColumnKey = "birthday", ColumnValue = "出生年月" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "telePhone", ColumnValue = "办公电话" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "landline", ColumnValue = "办公座机" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "mobilePhone", ColumnValue = "手机号码" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "urgentContacts", ColumnValue = "紧急联系" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "urgentTelePhone", ColumnValue = "紧急电话" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "postalAddress", ColumnValue = "通讯地址" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "enabledMark", ColumnValue = "状态", Required = true, SelectList = new List<string>() { "启用", "禁用", "锁定" } });
        res.Add(new ExportImportHelperModel() { ColumnKey = "sortCode", ColumnValue = "排序" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "description", ColumnValue = "说明" });

        if (fields == null || !fields.Any()) return res;

        var result = new List<ExportImportHelperModel>();

        foreach (var item in res)
        {
            if (fields.Contains(item.ColumnKey)) result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// 获取导入数据.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private async Task<List<UserListImportDataInput>> GetImportDataList(string fileName)
    {
        var resData = new List<UserListImportDataInput>();
        try
        {
            var fileEncode = GetUserInfoFieldToTitle();

            string? filePath = FileVariable.TemporaryFilePath;
            string? savePath = Path.Combine(filePath, fileName);

            // 得到数据
            var sr = await _fileManager.GetFileStream(savePath);
            var excelData = ExcelImportHelper.ToDataTable(savePath, sr);
            foreach (var it in excelData.Columns)
            {
                var item = it as DataColumn;
                if (item.ColumnName.Equals("errorsInfo")) throw Oops.Oh(ErrorCode.D1807);
                if (!fileEncode.Any(x => x.ColumnKey == item.ToString() && x.ColumnValue.Equals(item.Caption.Replace("*", "")))) throw Oops.Oh(ErrorCode.D1807);
                excelData.Columns[item.ToString()].ColumnName = fileEncode.Where(x => x.ColumnKey == item.ToString()).FirstOrDefault().ColumnKey;
            }

            if (excelData.Rows.Count > 0) excelData.Rows.RemoveAt(0);
            resData = excelData.ToJsonStringOld().ToObjectOld<List<UserListImportDataInput>>();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1807);
        }

        if (resData.Count < 1)
            throw Oops.Oh(ErrorCode.D5019);
        if (resData.Count > 1000)
            throw Oops.Oh(ErrorCode.D5029);

        return resData;

    }

    /// <summary>
    /// 导入用户数据函数.
    /// </summary>
    /// <param name="list">list.</param>
    /// <returns>[成功列表,失败列表].</returns>
    private async Task<object[]> ImportUserData(List<UserListImportDataInput> inputList)
    {
        foreach (var item in inputList) item.id = SnowflakeIdHelper.NextId();
        List<UserListImportDataInput> userInputList = inputList.ToObject<List<UserListImportDataInput>>();

        #region 初步排除错误数据

        if (userInputList == null || userInputList.Count() < 1)
            throw Oops.Oh(ErrorCode.D5019);
        if (userInputList.Count > 1000)
            throw Oops.Oh(ErrorCode.D5029);

        var regex = new Regex("^[a-z0-9A-Z\u4e00-\u9fa5]+$");

        // 上传重复的账号
        userInputList.ForEach(item =>
        {
            if (userInputList.Count(x => x.account == item.account) > 1)
            {
                var errorItems = userInputList.Where(x => x.account.IsNotEmptyOrNull() && x.account == item.account).ToList();
                for (var i = 1; i < errorItems.Count; i++) errorItems[i].errorsInfo = "账户值已存在,";
            }
        });

        // 用户账号 (匹配直属主管 和 验证重复账号)
        List<UserEntity>? _userRepositoryList = await _repository.AsQueryable().Where(it => it.DeleteMark == null).Select(it => new UserEntity() { Id = it.Id, Account = it.Account }).ToListAsync();

        // 已存在的账号
        List<UserEntity>? repeat = _userRepositoryList.Where(u => userInputList.Select(x => x.account).Contains(u.Account)).ToList();

        // 已存在的账号 列入 错误列表
        if (repeat.Any())
        {
            var addList = userInputList.Where(u => u.account.IsNotEmptyOrNull() && repeat.Select(x => x.Account).Contains(u.account)).ToList();
            addList.ForEach(item => item.errorsInfo = "账户值已存在,");
        }

        #endregion

        List<UserEntity>? userList = new List<UserEntity>();

        #region 预处理关联表数据

        // 组织机构
        var orgInfoList = _organizeService.GetOrgListTreeName();

        List<PositionEntity>? _positionRepositoryList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).ToListAsync(); // 岗位

        List<RoleEntity>? _roleRepositoryList = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null && x.Type.Equals(CommonConst.RelationObjectUser)).ToListAsync(); // 角色

        DictionaryTypeEntity? typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "963255a34ea64a2584c5d1ba269c1fe6" || x.EnCode == "sex") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? _genderList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 性别

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "b6cd65a763fa45eb9fe98e5057693e40" || x.EnCode == "Nation") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? _nationList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 民族

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "7866376d5f694d4d851c7164bd00ebfc" || x.EnCode == "certificateType") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? certificateTypeList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 证件类型

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "6a6d6fb541b742fbae7e8888528baa16" || x.EnCode == "Education") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? educationList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 文化程度

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "485719133245509" || x.EnCode == "Rank") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? ranksList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 职级

        #endregion

        // 处理多租户限定账户额度
        var maxAccountCount = 0;
        var addCount = 0;
        if (_tenant.MultiTenancy)
        {
            var tenatInfo = await _tenantManager.GetTenant(_userManager.TenantId);
            if (tenatInfo.accountNum != 0)
                maxAccountCount = (int)(tenatInfo.accountNum - await _repository.AsQueryable().CountAsync(x => x.DeleteMark == null));
        }

        var regexFullName = @"^([\u4e00-\u9fa5]|[a-zA-Z0-9])+$";

        // 用户关系数据
        List<UserRelationEntity>? userRelationList = new List<UserRelationEntity>();
        List<RoleRelationEntity>? roleRelationList = new List<RoleRelationEntity>();
        foreach (UserListImportDataInput? item in userInputList)
        {
            addCount++;

            if (item.account.IsNullOrWhiteSpace()) item.errorsInfo += "账户不能为空,";
            else if (item.account.Length > 50) item.errorsInfo += "账户值超出最多输入字符限制,";
            //if (item.account.IsNotEmptyOrNull() && !regex.IsMatch(item.account)) item.errorsInfo += "账户值不能含有特殊符号,";

            if (item.realName.IsNullOrWhiteSpace()) item.errorsInfo += "姓名不能为空,";
            else if (item.realName.Length > 50) item.errorsInfo += "姓名值超出最多输入字符限制,";
            //if (item.realName.IsNotEmptyOrNull() && !Regex.IsMatch(item.realName, regexFullName)) item.errorsInfo += "姓名值不能含有特殊符号,";

            if (item.gender.IsNullOrWhiteSpace()) item.errorsInfo += "性别不能为空,";
            if (item.enabledMark.IsNullOrWhiteSpace()) item.errorsInfo += "状态不能为空,";
            if (item.sortCode.IsNotEmptyOrNull())
            {
                try
                {
                    var sortCode = long.Parse(item.sortCode);
                    if (sortCode < 0) item.errorsInfo += "排序值不能小于0,";
                    else if (sortCode > 999999) item.errorsInfo += "排序值不能大于999999,";
                }
                catch (Exception)
                {
                    item.errorsInfo += "排序值不正确,";
                }
            }
            if (item.enabledMark.IsNotEmptyOrNull() && !item.enabledMark.Equals("禁用") && !item.enabledMark.Equals("启用") && !item.enabledMark.Equals("锁定")) item.errorsInfo += "找不到该状态值,";
            if (item.entryDate.IsNotEmptyOrNull())
            {
                try
                {
                    DateTime.Parse(item.entryDate);
                }
                catch (Exception)
                {
                    item.errorsInfo += "入职时间值不正确,";
                }
            }
            if (item.birthday.IsNotEmptyOrNull())
            {
                try
                {
                    DateTime.Parse(item.birthday);
                }
                catch (Exception)
                {
                    item.errorsInfo += "出生年月值不正确,";
                }
            }

            List<string>? orgIds = new List<string>(); // 多组织 , 号隔开
            List<string>? posIds = new List<string>(); // 多岗位 , 号隔开
            List<string>? roleIds = new List<string>(); // 角色 , 号隔开

            UserEntity? uentity = new UserEntity();
            uentity.Id = item.id;
            if (string.IsNullOrEmpty(uentity.HeadIcon)) uentity.HeadIcon = "001.png";
            uentity.Secretkey = Guid.NewGuid().ToString();

            var defaultPassWord = await _repository.AsSugarClient().Queryable<SysConfigEntity>()
                .Where(it => it.Key.Equals("newUserDefaultPassword"))
                .Select(it => it.Value)
                .FirstAsync();
            uentity.Password = MD5Encryption.Encrypt(MD5Encryption.Encrypt(defaultPassWord) + uentity.Secretkey); // 初始化密码

            // 寻找角色
            if (item.roleId.IsNotEmptyOrNull() && item.roleId.Split(",").Any())
            {
                var roleList = item.roleId.Split(",").ToList();
                var itemRoleIds = _roleRepositoryList.Where(r => roleList.Contains(r.FullName)).Select(x => new { Id = x.Id, FullName = x.FullName }).ToList();
                if (roleList.Count != itemRoleIds.Count)
                {
                    if (roleList.Count.Equals(1))
                    {
                        item.errorsInfo += "找不到该角色,";
                    }
                    else
                    {
                        var exList = roleList.Except(itemRoleIds.Select(x => x.FullName));
                        item.errorsInfo += string.Format("找不到该角色({0}),", string.Join("、", exList));
                    }
                }
                uentity.RoleId = string.Join(",", itemRoleIds.Select(x => x.Id));
            }

            // 性别
            if (item.gender.IsNotEmptyOrNull() && !_genderList.Any(x => x.FullName == item.gender))
            {
                item.errorsInfo += "找不到该性别值,";
            }

            // 寻找岗位
            if (item.positionId.IsNotEmptyOrNull() && item.positionId.Split(",").Any())
            {
                var itemPosList = item.positionId.Split(",").ToList();
                var exList = new List<string>();
                itemPosList.ForEach(it =>
                {
                    string[]? pinfo = it.Split("/");
                    var pEntity = _positionRepositoryList.Find(x => x.EnCode == pinfo.LastOrDefault());
                    if (pEntity.IsNotEmptyOrNull())
                    {
                        posIds.Add(pEntity.Id); // 多岗位
                        orgIds.Add(pEntity.OrganizeId); // 多组织
                    }
                    else exList.Add(it);
                });
                if (exList.Any())
                {
                    if (itemPosList.Count.Equals(1))
                    {
                        item.errorsInfo += "找不到该岗位,";
                    }
                    else
                    {
                        item.errorsInfo += string.Format("找不到该岗位({0}),", string.Join("、", exList));
                    }
                }
            }

            // 民族
            if (item.nation.IsNotEmptyOrNull() && !_nationList.Any(x => x.FullName == item.nation))
            {
                item.errorsInfo += "找不到该民族值,";
            }

            // 文化程度
            if (item.education.IsNotEmptyOrNull() && !educationList.Any(x => x.FullName == item.education))
            {
                item.errorsInfo += "找不到该文化程度值,";
            }

            // 证件类型
            if (item.certificatesType.IsNotEmptyOrNull() && !certificateTypeList.Any(x => x.FullName == item.certificatesType))
            {
                item.errorsInfo += "找不到该证件类型值,";
            }

            // 职级
            if (item.ranks.IsNotEmptyOrNull() && !ranksList.Any(x => x.FullName == item.ranks))
            {
                item.errorsInfo += "找不到该职级值,";
            }

            uentity.Gender = _genderList.Find(x => x.FullName == item.gender)?.EnCode;
            uentity.Nation = _nationList.Find(x => x.FullName == item.nation)?.Id; // 民族
            uentity.Education = educationList.Find(x => x.FullName == item.education)?.Id; // 文化程度
            uentity.CertificatesType = certificateTypeList.Find(x => x.FullName == item.certificatesType)?.Id; // 证件类型
            uentity.Ranks = ranksList.Find(x => x.FullName == item.ranks)?.Id; // 职级
            uentity.Account = item.account;
            uentity.Birthday = item.birthday.IsNotEmptyOrNull() ? item.birthday.ParseToDateTime() : null;
            uentity.CertificatesNumber = item.certificatesNumber;
            uentity.CreatorUserId = _userManager.UserId;
            uentity.CreatorTime = DateTime.Now;
            uentity.Description = item.description;
            uentity.Email = item.email;
            switch (item.enabledMark)
            {
                case "禁用":
                    uentity.EnabledMark = 0;
                    break;
                case "启用":
                    uentity.EnabledMark = 1;
                    break;
                case "锁定":
                    uentity.EnabledMark = 2;
                    break;
            }

            uentity.EntryDate = item.entryDate.IsNotEmptyOrNull() ? item.entryDate.ParseToDateTime() : null;
            uentity.Landline = item.landline;
            uentity.MobilePhone = item.mobilePhone;
            uentity.NativePlace = item.nativePlace;
            uentity.PostalAddress = item.postalAddress;
            uentity.RealName = item.realName;
            uentity.SortCode = item.sortCode.IsNotEmptyOrNull() ? item.sortCode.ParseToLong() : 0;
            uentity.TelePhone = item.telePhone;
            uentity.UrgentContacts = item.urgentContacts;
            uentity.UrgentTelePhone = item.urgentTelePhone;
            uentity.PositionId = posIds.FirstOrDefault();
            uentity.OrganizeId = orgIds.FirstOrDefault();

            if (uentity.PositionId.IsNotEmptyOrNull() && item.errorsInfo.IsNullOrEmpty())
            {
                List<UserRelationEntity>? positionList = _userRelationService.CreateUserRelation(uentity.Id, string.Join(",", posIds), CommonConst.RelationObjectPosition); // 岗位关系
                List<UserRelationEntity>? organizeList = _userRelationService.CreateUserRelation(uentity.Id, string.Join(",", orgIds), CommonConst.RelationObjectOrganize); // 组织关系
                userRelationList.AddRange(positionList);
                userRelationList.AddRange(organizeList);
            }

            if (uentity.RoleId.IsNotEmptyOrNull() && item.errorsInfo.IsNullOrEmpty())
            {
                // 角色关系
                List<string>? position = new List<string>(uentity.RoleId.Split(','));
                position.ForEach(item =>
                {
                    RoleRelationEntity? entity = new RoleRelationEntity();
                    entity.Id = SnowflakeIdHelper.NextId();
                    entity.ObjectType = CommonConst.RelationObjectUser;
                    entity.ObjectId = uentity.Id;
                    entity.SortCode = position.IndexOf(item);
                    entity.RoleId = item;
                    entity.CreatorTime = DateTime.Now;
                    entity.CreatorUserId = _userManager.UserId;
                    roleRelationList.Add(entity);
                });
            }

            if (_tenant.MultiTenancy && maxAccountCount != 0)
            {
                if (maxAccountCount >= addCount)
                {
                    if (item.errorsInfo.IsNotEmptyOrNull()) userList.Add(uentity);
                }
                else
                {
                    item.errorsInfo += "用户额度已达到上限";
                }
            }
            else
            {
                if (item.errorsInfo.IsNullOrEmpty()) userList.Add(uentity);
            }
        }

        foreach (var entity in userList)
        {
            #region 岗位约束验证
            var posIds = userRelationList.Where(x => x.ObjectType.Equals(CommonConst.RelationObjectPosition) && x.UserId.Equals(entity.Id)).Select(x => x.ObjectId).ToList();
            foreach (var posItem in _positionRepositoryList.Where(x => posIds.Contains(x.Id)))
            {
                if (posItem != null && posItem.IsCondition.Equals(1) && posItem.ConditionJson.IsNotEmptyOrNull())
                {
                    var condition = posItem.ConditionJson.ToObject<Entitys.Dto.Position.PositionConstraintModel>();
                    if (condition.constraintType.Any())
                    {
                        if (condition.constraintType.Contains(1))
                        {
                            var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => posItem.Id.Equals(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                            var addUserIds = userRelationList.Where(x => x.ObjectType.Equals(CommonConst.RelationObjectPosition) && x.ObjectId.Equals(posItem.Id)).Select(x => x.UserId);
                            userIds.AddRange(addUserIds);
                            userIds = userIds.Distinct().ToList();
                            if (userIds.Count > condition.userNum) userInputList.Find(x => x.id.Equals(entity.Id)).errorsInfo += string.Format("该{0}已达用户限制个数,", posItem.FullName);
                        }

                        if (condition.constraintType.Contains(0))
                        {
                            var userIds = userRelationList.Where(x => x.UserId.Equals(entity.Id) && condition.mutualExclusion.Contains(x.ObjectId));
                            if (userIds.Any())
                            {
                                var first = userInputList.Find(x => x.id.Equals(entity.Id));
                                if (!first.errorsInfo.Contains("存在互斥岗位")) first.errorsInfo += "存在互斥岗位,";
                            }
                        }

                        if (condition.constraintType.Contains(2))
                        {
                            var reIds = userRelationList.Where(x => x.UserId.Equals(entity.Id) && condition.prerequisite.Contains(x.ObjectId)).ToList();
                            var objIds = reIds.Where(x => x.UserId.Equals(entity.Id)).Select(x => x.ObjectId).ToList();
                            if (!condition.prerequisite.All(x => objIds.Contains(x))) userInputList.Find(x => x.id.Equals(entity.Id)).errorsInfo += "缺少先决岗位,";
                        }
                    }
                }
            }
            #endregion

            #region 角色约束验证
            var roleIds = roleRelationList.Where(x => x.ObjectType.Equals(CommonConst.RelationObjectUser) && x.ObjectId.Equals(entity.Id)).Select(x => x.RoleId).ToList();
            foreach (var roleItem in _roleRepositoryList.Where(x => roleIds.Contains(x.Id)))
            {
                if (roleItem.IsCondition.Equals(1) && roleItem.ConditionJson.IsNotEmptyOrNull())
                {
                    var condition = roleItem.ConditionJson.ToObject<RoleConstraintModel>();
                    if (condition.constraintType.Any())
                    {
                        if (condition.constraintType.Contains(1))
                        {
                            var userIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectUser) && x.RoleId.Equals(roleItem.Id)).Select(x => x.ObjectId).ToListAsync();
                            var addUserIds = roleRelationList.Where(x => x.ObjectType.Equals(CommonConst.RelationObjectUser) && x.RoleId.Equals(roleItem.Id)).Select(x => x.ObjectId);
                            userIds.AddRange(addUserIds);
                            userIds = userIds.Distinct().ToList();
                            if (userIds.Count > condition.userNum) userInputList.Find(x => x.id.Equals(entity.Id)).errorsInfo += string.Format("该{0}已达用户限制个数,", roleItem.FullName);
                        }

                        if (condition.constraintType.Contains(0))
                        {
                            var userIds = roleRelationList.Where(x => x.ObjectId.Equals(entity.Id) && condition.mutualExclusion.Contains(x.RoleId));
                            if (userIds.Any())
                            {
                                var first = userInputList.Find(x => x.id.Equals(entity.Id));
                                if (!first.errorsInfo.Contains("存在互斥角色")) first.errorsInfo += "存在互斥角色,";
                            }
                        }

                        if (condition.constraintType.Contains(2))
                        {
                            var reIds = roleRelationList.Where(x => x.ObjectId.Equals(entity.Id) && condition.prerequisite.Contains(x.RoleId)).ToList();
                            var objIds = reIds.Where(x => x.ObjectId.Equals(entity.Id)).Select(x => x.RoleId).ToList();
                            if (!condition.prerequisite.All(x => objIds.Contains(x))) userInputList.Find(x => x.id.Equals(entity.Id)).errorsInfo += "缺少先决角色,";
                        }
                    }
                }
            }
            #endregion
        }

        userList = userList.Where(x => userInputList.Where(xx => xx.errorsInfo.IsNullOrEmpty()).Select(xx => xx.id).Contains(x.Id)).ToList();

        if (userList.Any())
        {
            try
            {
                // 新增用户记录
                UserEntity? newEntity = await _repository.AsInsertable(userList).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();

                // 批量新增用户关系
                if (userRelationList.Count > 0) await _repository.AsSugarClient().Insertable(userRelationList).ExecuteCommandAsync();
                if (roleRelationList.Count > 0) await _repository.AsSugarClient().Insertable(roleRelationList).ExecuteCommandAsync();

            }
            catch (Exception)
            {
                userInputList.ForEach(item => item.errorsInfo = "系统异常");
            }
        }

        var errorList = new List<UserListImportDataInput>();
        foreach (var item in inputList)
        {
            var it = userInputList.Find(x => x.id.Equals(item.id));
            if (it.errorsInfo.IsNotEmptyOrNull())
            {
                item.errorsInfo = it.errorsInfo.TrimEnd(',');
                errorList.Add(item);
            }
        }

        return new object[] { userList, errorList };
    }

    /// <summary>
    /// 获取机构成员列表.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeMemberListOutput>> GetOrganizeMemberList(string organizeId)
    {
        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();

        if (organizeId.Equals("0"))
        {
            List<OrganizeEntity>? data = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.DeleteMark == null && o.EnabledMark == 1)
                .OrderBy(o => o.SortCode).ToListAsync();

            data.ForEach(o =>
            {
                output.Add(new OrganizeMemberListOutput
                {
                    id = o.Id,
                    fullName = o.FullName,
                    enabledMark = o.EnabledMark,
                    type = o.Category,
                    icon = o.Category.Equals(CommonConst.OrganizeTypeGroup) || o.Category.Equals(CommonConst.OrganizeTypeInstitution) ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                    organizeIdTree = o.OrganizeIdTree,
                    hasChildren = true,
                    isLeaf = false
                });
            });
        }
        else
        {
            var userRelationList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && x.ObjectId.Equals(organizeId)).Select(x => x.UserId).ToListAsync();
            List<UserEntity>? userList = await _repository.AsSugarClient().Queryable<UserEntity>()
                .Where(u => userRelationList.Contains(u.Id) && u.EnabledMark > 0 && u.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
            userList.ForEach(u =>
            {
                output.Add(new OrganizeMemberListOutput()
                {
                    id = u.Id,
                    fullName = u.RealName + "/" + u.Account,
                    enabledMark = u.EnabledMark,
                    type = "user",
                    icon = "icon-ym icon-ym-tree-user2",
                    headIcon = "/api/File/Image/userAvatar/" + u.HeadIcon,
                    hasChildren = false,
                    isLeaf = true
                });
            });

            List<OrganizeEntity>? departmentList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.DeleteMark == null && o.EnabledMark == 1)
                .Where(x => x.ParentId.Equals(organizeId))
                .OrderBy(o => o.SortCode).ToListAsync();

            departmentList.ForEach(o =>
            {
                output.Add(new OrganizeMemberListOutput()
                {
                    id = o.Id,
                    fullName = o.FullName,
                    enabledMark = o.EnabledMark,
                    type = o.Category,
                    icon = o.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                    hasChildren = true,
                    organizeIdTree = o.OrganizeIdTree,
                    isLeaf = false
                });
            });
        }

        if (!organizeId.Equals("0")) output.RemoveAll(x => x.id.Equals(organizeId));

        // 获取组织树
        var orgTree = _organizeService.GetOrgListTreeName();

        // 组织断层处理
        output.Where(x => x.parentId != "-1" && x.organizeIdTree.IsNotEmptyOrNull()).ToList().ForEach(item =>
        {
            item.fullName = orgTree.Find(x => x.Id.Equals(item.id)).Description;
            if (!output.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = output.Where(x => x.organizeIdTree.IsNotEmptyOrNull() && x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree)).FirstOrDefault();
                if (pItem != null)
                {
                    item.parentId = pItem.id;
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
                else
                {
                    item.parentId = "-1";
                }
            }
            else
            {
                var pItem = output.Find(x => x.id.Equals(item.parentId));
                item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
            }
        });

        output.RemoveAll(x => x.type != "user" && x.parentId != "-1");

        if (!organizeId.Equals("0"))
        {
            var pOrgTreeName = orgTree.Find(x => x.Id.Equals(organizeId)).Description;
            output.ForEach(item => item.fullName = item.fullName.Replace(pOrgTreeName + "/", string.Empty));
        }

        return output;
    }

    [NonAction]
    private List<string> GetOutPutByChild(List<UsersCurrentAuthorizeMoldel> list, List<string> pids)
    {
        var res = new List<string>();
        var pList = list.Where(x => pids.Contains(x.id)).Select(x => x.id).ToList();
        res.AddRange(pList);
        if (pList.Any()) res.AddRange(GetOutPutByChild(list, list.Where(x => pids.Contains(x.id)).Select(x => x.parentId).ToList()));
        return res;
    }

    /// <summary>
    /// 强制用户下线.
    /// </summary>
    /// <param name="userId"></param>
    private async Task ForcedOffline(string userId, string method = "refresh", string msg = "您的权限发生变更,请刷新界面！")
    {
        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);
        if (list != null && list.Any())
        {
            var userList = list.Where(it => it.tenantId == _userManager.TenantId && it.userId == userId).ToList();
            foreach (var user in userList)
            {
                if (user != null)
                {
                    await _imHandler.SendMessageAsync(user.connectionId, new { method = method, msg = msg }.ToJsonString());

                    if (!method.Equals("refresh"))
                    {
                        // 删除在线用户ID
                        list.RemoveAll((x) => x.connectionId == user.connectionId);
                        await _cacheManager.SetAsync(onlineCacheKey, list);

                        // 删除用户登录信息缓存
                        var cacheKey = string.Format("{0}:{1}:{2}", _userManager.TenantId, CommonConst.CACHEKEYUSER, user.userId);
                        await _cacheManager.DelAsync(cacheKey);
                    }
                }
            }
        }
    }
    #endregion

    #region 单点登录 数据同步

    /// <summary>
    /// 同步数据导maxkey.
    /// </summary>
    /// <param name="userEntity"></param>
    /// <param name="method"></param>
    /// <param name="tenantId"></param>
    [NonAction]
    public async Task syncUserInfo(UserEntity userEntity, string method, string tenantId)
    {
        try
        {
            if (_oauthOptions.Enabled)
            {
                var userName = string.Format("{0}:{1}", _oauthOptions.Pull.UserName, _oauthOptions.Pull.Password).ToBase64String();
                var map = parse(userEntity);
                tenantId = tenantId != null && tenantId.Length > 0 ? tenantId : "1";
                if (tenantId.Equals("default")) tenantId = "1";
                map.Add("instId", tenantId);
                var resString = string.Empty;
                var headers = new Dictionary<string, object>();
                headers.Add("Authorization", _oauthOptions.Pull.CredentialType + " " + userName);

                if (method.Equals("create"))
                {
                    resString = await (_oauthOptions.Pull.CreateRestAddress + "?appId=" + _oauthOptions.Pull.UserName).SetHeaders(headers).SetBody(map).PostAsStringAsync();
                }
                else if (method.Equals("update"))
                {
                    resString = await (_oauthOptions.Pull.ReplaceRestAddress + "?appId=" + _oauthOptions.Pull.UserName).SetHeaders(headers).SetBody(map).PutAsStringAsync();
                }
                else if (method.Equals("delete"))
                {
                    resString = await (_oauthOptions.Pull.DeleteRestAddress + "?appId=" + _oauthOptions.Pull.UserName).SetHeaders(headers).SetBody(map).DeleteAsStringAsync();
                }
                else if (method.Equals("modifyPassword"))
                {
                    resString = await (_oauthOptions.Pull.ChangePasswordRestAddress + "?appId=" + _oauthOptions.Pull.UserName).SetHeaders(headers).SetBody(map).PostAsStringAsync();
                }

                //            else if (method.Equals("modifyPassword")) {
                //                jsonObject = HttpUtil.httpRequest(_oauthOptions.Pull.getGetRestAddress() + username
                //                        , "GET"
                //                        , null, _oauthOptions.Pull.getCredentialType() + " " + _oauthOptions.Pull.getUserName() + "Og==" + _oauthOptions.Pull.getPassword()
                //                        , null);
                //            }
            }
        }
        catch (Exception)
        {

        }
    }

    private Dictionary<string, object> parse(UserEntity userEntity)
    {
        var map = new Dictionary<string, object>();

        // map.Add("id", userEntity.get("id"));
        map.Add("username", userEntity.Account);
        map.Add("password", userEntity.Password);
        map.Add("mobile", userEntity.MobilePhone);
        map.Add("email", userEntity.Email);
        map.Add("gender", userEntity.Gender);
        map.Add("createdBy", userEntity.CreatorUserId);
        map.Add("createdDate", userEntity.CreatorTime);
        map.Add("modifiedBy", userEntity.LastModifyUserId);
        map.Add("modifiedDate", userEntity.LastModifyTime);
        map.Add("displayName", userEntity.RealName);

        // map.Add("managerId", userEntity.get("managerId"));
        // map.Add("departmentId", userEntity.get("organizeId"));
        map.Add("loginCount", userEntity.LogSuccessCount);
        map.Add("badPasswordCount", userEntity.LogErrorCount);
        map.Add("lastLoginIp", userEntity.LastLogIP);
        map.Add("lastLoginTime", userEntity.LastLogTime);
        map.Add("status", userEntity.EnabledMark != null ? (userEntity.EnabledMark == 1 ? 1 : 4) : 4);
        return map;
    }

    #endregion
}