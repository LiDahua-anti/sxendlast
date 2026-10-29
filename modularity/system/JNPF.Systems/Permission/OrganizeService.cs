using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Data;
using System.Text.RegularExpressions;

namespace JNPF.Systems;

/// <summary>
/// 机构管理.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Organize", Order = 165)]
[Route("api/permission/[controller]")]
public class OrganizeService : IOrganizeService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<OrganizeEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 系统配置.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 第三方同步.
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
    /// 多租户事务.
    /// </summary>
    private readonly ITenant _db;

    /// <summary>
    /// 初始化一个<see cref="OrganizeService"/>类型的新实例.
    /// </summary>
    public OrganizeService(
        ISqlSugarRepository<OrganizeEntity> repository,
        IBillRullService billRuleService,
        ISysConfigService sysConfigService,
        ISynThirdInfoService synThirdInfoService,
        IUserManager userManager,
        ICacheManager cacheManager,
        IFileManager fileService,
        ISqlSugarClient context)
    {
        _repository = repository;
        _billRuleService = billRuleService;
        _cacheManager = cacheManager;
        _fileManager = fileService;
        _sysConfigService = sysConfigService;
        _synThirdInfoService = synThirdInfoService;
        _userManager = userManager;
        _db = context.AsTenant();
    }

    #region GET

    /// <summary>
    /// 获取机构列表.
    /// </summary>
    /// <param name="input">关键字参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] CommonInput input)
    {
        StructureOrganizeIdTree(); // 构造组织树 id.

        List<OrganizeListOutput>? data = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Category.Equals(input.type))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime)
            .Select(x => new OrganizeListOutput()
            {
                category = x.Category,
                creatorTime = x.CreatorTime,
                enCode = x.EnCode,
                enabledMark = x.EnabledMark,
                fullName = x.FullName,
                id = x.Id,
                organizeIdTree = x.OrganizeIdTree,
                parentId = x.ParentId,
                sortCode = x.SortCode,
                icon = SqlFunc.IIF(x.Category.Equals(CommonConst.OrganizeTypeGroup) || x.Category.Equals(CommonConst.OrganizeTypeInstitution), "icon-ym icon-ym-tree-organization3", "icon-ym icon-ym-tree-department1"),
                type = x.Category
            }).ToListAsync();

        if (!string.IsNullOrEmpty(input.keyword))
            data = data.TreeWhere(t => t.fullName.Contains(input.keyword) || t.enCode.Contains(input.keyword), t => t.id, t => t.parentId);
        data.ForEach(item =>
        {
            if (!data.Any(x => x.id.Equals(item.parentId)))
                item.parentId = item.parentId.Equals("-1") ? "-1" : "0";
        });

        // 获取组织树
        var orgTree = GetOrgListTreeName();

        data.ForEach(item =>
        {
            item.fullName = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });

        return data.Any(x => x.parentId.Equals("-1")) ? new { list = data.OrderBy(x => x.sortCode).ToList().ToTree("-1") } : new { list = data.OrderBy(x => x.sortCode).ToList().ToTree("0") };
    }

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector/{id}")]
    public async Task<dynamic> GetSelector(string id)
    {
        // 获取组织树
        var orgTree = GetOrgListTreeName();

        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(t => t.DeleteMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
        if (!"0".Equals(id))
        {
            OrganizeEntity? info = data.Find(it => it.Id == id);
            data.Remove(info);
        }

        List<OrganizeListOutput>? treeList = data.Adapt<List<OrganizeListOutput>>();
        treeList.ForEach(item =>
        {
            item.type = item.category;
            if (item != null && (item.category.Equals(CommonConst.OrganizeTypeGroup) || item.category.Equals(CommonConst.OrganizeTypeInstitution))) item.icon = "icon-ym icon-ym-tree-organization3";
            item.organize = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });
        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelectorByPid([FromQuery] OrganizeSelectorInput input)
    {
        if (input.parentId.IsNullOrEmpty() || input.parentId.Equals("0")) input.parentId = "-1";

        var data = new SqlSugarPagedList<OrganizeListOutput>();
        if (input.keyword.IsNotEmptyOrNull())
        {
            data = await _repository.AsQueryable()
               .Where(t => t.DeleteMark == null && (t.FullName.Contains(input.keyword) || t.EnCode.Contains(input.keyword)))
               .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime).Select(a => new OrganizeListOutput()
               {
                   id = a.Id,
                   category = a.Category,
                   enCode = a.EnCode,
                   fullName = a.FullName,
                   icon = SqlFunc.IIF(a.Category.Equals(CommonConst.OrganizeTypeGroup) || a.Category.Equals(CommonConst.OrganizeTypeInstitution), "icon-ym icon-ym-tree-organization3", "icon-ym icon-ym-tree-department1"),
                   orgNameTree = a.OrganizeNameTree,
                   organizeIdTree = a.OrganizeIdTree,
                   parentId = a.ParentId,
               }).ToPagedListAsync(input.currentPage, input.pageSize);
        }
        else
        {
            data.list = await _repository.AsQueryable()
               .Where(t => t.DeleteMark == null && t.ParentId.Equals(input.parentId))
               .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime).Select(a => new OrganizeListOutput()
               {
                   id = a.Id,
                   category = a.Category,
                   enCode = a.EnCode,
                   fullName = a.FullName,
                   icon = SqlFunc.IIF(a.Category.Equals(CommonConst.OrganizeTypeGroup) || a.Category.Equals(CommonConst.OrganizeTypeInstitution), "icon-ym icon-ym-tree-organization3", "icon-ym icon-ym-tree-department1"),
                   orgNameTree = a.OrganizeNameTree,
                   organizeIdTree = a.OrganizeIdTree,
                   parentId = a.ParentId,
               }).ToListAsync();
        }

        foreach (var item in data.list)
        {
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
            item.hasChildren = true;
        }

        return PageResult<OrganizeListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        OrganizeEntity? entity = await _repository.GetSingleAsync(p => p.Id == id);
        var res = entity.Adapt<OrganizeInfoOutput>();
        var pEntity = await _repository.GetSingleAsync(p => p.Id == entity.ParentId);
        if (pEntity != null)
        {
            res.parentName = pEntity.FullName;
            res.parentCategory = pEntity.Category;
            res.organizeIdTree = pEntity.OrganizeIdTree.Split(",").ToList();
        }
        else
        {
            res.organizeIdTree = new List<string>() { "-1" };
        }
        return res;
    }

    /// <summary>
    /// 获取下拉框异步.
    /// </summary>
    /// <returns></returns>
    [HttpGet("AsyncList/{id}")]
    public async Task<dynamic> GetAsyncList(string id, [FromQuery] PageInputBase input)
    {
        StructureOrganizeIdTree(); // 构造组织树 id.

        if (input.keyword.IsNullOrEmpty()) input.pageSize = 99999;

        if (input.keyword.IsNotEmptyOrNull())
        {
            var keywordList = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .Where(x => x.FullName.Contains(input.keyword) || x.EnCode.Contains(input.keyword))
            .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Asc).ToPagedListAsync(input.currentPage, input.pageSize);

            var resList = keywordList.list.Adapt<List<OrganizeSelectorOutput>>();

            foreach (var item in resList)
            {
                item.type = item.category;
                if (item.type != null && (item.category.Equals(CommonConst.OrganizeTypeGroup) || item.category.Equals(CommonConst.OrganizeTypeInstitution))) item.icon = "icon-ym icon-ym-tree-organization3";
                item.fullName = item.fullName;
                item.organizeIds = item.organizeIdTree.Split(",").ToList();
                item.isLeaf = null;
                item.children = null;
            }

            return PageResult<OrganizeSelectorOutput>.SqlSugarPageResult(new SqlSugarPagedList<OrganizeSelectorOutput>() { list = resList, pagination = keywordList.pagination });
        }
        else
        {
            var dataList = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
                .WhereIF(!id.Equals("0"), x => x.ParentId.Equals(id))
                .WhereIF(_userManager.IsAdministrator && id.Equals("0"), x => x.ParentId.Equals("-1"))
                .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Asc).ToListAsync();

            List<OrganizeSelectorOutput>? treeList = dataList.Adapt<List<OrganizeSelectorOutput>>();

            treeList.ForEach(item =>
            {
                item.type = item.category;
                if (item.type != null && (item.category.Equals(CommonConst.OrganizeTypeGroup) || item.category.Equals(CommonConst.OrganizeTypeInstitution))) item.icon = "icon-ym icon-ym-tree-organization3";
                item.organizeIds = item.organizeIdTree.Split(",").ToList();
            });

            // 组织断层处理
            treeList.Where(x => x.parentId != "-1").OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
            {
                if (!(input.keyword.IsNotEmptyOrNull() && id.Equals("0")))
                {
                    if (!treeList.Any(x => x.id.Equals(item.parentId)))
                    {
                        var pItem = treeList.Find(x => x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
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
                        var pItem = treeList.Find(x => x.id.Equals(item.parentId));
                        item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                    }
                }
            });
            if (id.IsNotEmptyOrNull() && !id.Equals("0")) treeList.RemoveAll(x => x.id.Equals(id));

            var res = new List<OrganizeSelectorOutput>();
            if (id.Equals("0"))
                res = treeList.Where(x => x.parentId.Equals("-1")).ToList();
            else
                res = treeList.ToList();
            return new { list = res };
        }
    }

    /// <summary>
    /// 获取下拉框权限异步.
    /// </summary>
    /// <returns></returns>
    [HttpGet("SelectAsyncByAuth/{id}")]
    public async Task<dynamic> SelectAsyncByAuth(string id, [FromQuery] OrganizeCurrInput input)
    {
        StructureOrganizeIdTree(); // 构造组织树 id.

        // 获取组织树
        var orgTree = GetOrgListTreeName();
        if (input.keyword.IsNullOrEmpty()) input.pageSize = 99999;

        var dataList = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .WhereIF(!id.Equals("0"), x => x.OrganizeIdTree.Contains(id))
            .WhereIF(id.Equals("0") && input.currOrgId.IsNotEmptyOrNull() && input.currOrgId != "0", x => !x.OrganizeIdTree.Contains(input.currOrgId))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword))
            .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime).ToPagedListAsync(input.currentPage, input.pageSize);

        List<OrganizeSelectorOutput>? treeList = dataList.list.Adapt<List<OrganizeSelectorOutput>>();

        treeList.ForEach(item =>
        {
            item.type = item.category;
            if (item.type != null && (item.category.Equals(CommonConst.OrganizeTypeGroup) || item.category.Equals(CommonConst.OrganizeTypeInstitution))) item.icon = "icon-ym icon-ym-tree-organization3";
            item.fullName = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organize = item.fullName;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });

        if (!await _repository.AsSugarClient().Queryable<OrganizeEntity>().AnyAsync(x => treeList.Select(xx => xx.id).Contains(x.ParentId) && x.Id.Equals(id)))
        {
            if (!id.Equals("0") && id.IsNotEmptyOrNull())
            {
                var entity = orgTree.FirstOrDefault(x => x.Id.Equals(id));
                var pItem = orgTree.FirstOrDefault(x => x.Id.Equals(entity.ParentId));
                if (pItem != null)
                {
                    var addItem = pItem.Adapt<OrganizeSelectorOutput>();

                    if (addItem.type != null && (addItem.category.Equals(CommonConst.OrganizeTypeGroup) || addItem.category.Equals(CommonConst.OrganizeTypeInstitution))) addItem.icon = "icon-ym icon-ym-tree-organization3";
                    addItem.fullName = orgTree.FirstOrDefault(x => x.Id.Equals(addItem.id))?.Description;
                    addItem.organize = addItem.fullName;
                    addItem.organizeIds = addItem.organizeIdTree.Split(",").ToList();
                    addItem.disabled = true;
                    addItem.sortCode = 0;
                    if (!treeList.Any(x => x.id.Equals(addItem.id))) treeList.Add(addItem);
                }
            }
        }

        // 组织断层处理
        treeList.Where(x => x.parentId != "-1").OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            if (!(input.keyword.IsNotEmptyOrNull() && id.Equals("0")))
            {
                if (!treeList.Any(x => x.id.Equals(item.parentId)))
                {
                    var pItem = treeList.Find(x => x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
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
                    var pItem = treeList.Find(x => x.id.Equals(item.parentId));
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
            }
        });

        if (input.keyword.IsNotEmptyOrNull() && id.Equals("0"))
        {
            return PageResult<OrganizeSelectorOutput>.SqlSugarPageResult(new SqlSugarPagedList<OrganizeSelectorOutput>() { list = treeList, pagination = dataList.pagination });
        }
        else
        {
            var res = new List<OrganizeSelectorOutput>();
            if (id.Equals("0"))
                res = treeList.Where(x => x.parentId.Equals("-1")).ToList();
            else
                res = treeList.Where(x => x.parentId.Equals(id)).ToList();
            return new { list = res };
        }
    }

    /// <summary>
    /// 获取组织和岗位异步.
    /// </summary>
    /// <returns></returns>
    [HttpGet("AsyncPosList")]
    public async Task<dynamic> GetAsyncPosList([FromQuery] OrganizeQueryInput input)
    {
        if (input.id.Equals("0")) input.id = "-1";

        var res = await _repository.AsSugarClient().Queryable<PositionEntity>()
            .Where(x => x.DeleteMark == null)
            .WhereIF(input.type.Equals(CommonConst.RelationObjectOrganize), x => x.OrganizeId.Equals(input.id) && (x.ParentId.Equals("-1") || x.ParentId == null))
            .WhereIF(input.type.Equals(CommonConst.RelationObjectPosition), x => x.ParentId.Equals(input.id))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime)
            .Select(x => new OrganizeListOutput()
            {
                id = x.Id,
                enCode = x.EnCode,
                fullName = x.FullName,
                icon = "icon-ym icon-ym-tree-position1",
                parentId = x.OrganizeId,
                hasChildren = true,
                orgNameTree = SqlFunc.MergeString(SqlFunc.Subqueryable<OrganizeEntity>().Where(xx => xx.Id.Equals(x.OrganizeId)).Select(xx => xx.OrganizeNameTree), "/", x.FullName),
                type = CommonConst.RelationObjectPosition
            }).ToListAsync();

        var orgList = await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null)
            .Where(x => x.ParentId.Equals(input.id)).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime)
            .Select(x => new OrganizeListOutput()
            {
                id = x.Id,
                category = x.Category,
                enCode = x.EnCode,
                fullName = x.FullName,
                icon = SqlFunc.IIF(x.Category.Equals(CommonConst.OrganizeTypeGroup) || x.Category.Equals(CommonConst.OrganizeTypeInstitution), "icon-ym icon-ym-tree-organization3", "icon-ym icon-ym-tree-department1"),
                orgNameTree = x.OrganizeNameTree,
                parentId = x.ParentId,
                hasChildren = true,
                type = CommonConst.RelationObjectOrganize
            }).ToListAsync();

        res.AddRange(orgList);
        return new { list = res };
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
        return await GetSelectedList(input);
    }

    /// <summary>
    /// 获取选中组织、岗位、角色、分组、用户基本信息.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("OrganizeCondition")]
    public async Task<dynamic> GetOrganizeCondition([FromBody] UserSelectedInput input)
    {
        // 获取所有组织
        List<OrganizeEntity>? allOrgList = GetOrgListTreeName();
        var oIdList = new List<string>();
        foreach (var x in input.ids)
        {
            if (x.Equals("@organizeId"))
            {
                oIdList.AddRange(_userManager.CurrentOrganizeList);
                continue;
            }

            if (x.Equals("@organizeAndSub"))
            {
                oIdList.AddRange(_userManager.CurrentOrganizationAndSubOrganizations);
                continue;
            }

            if (x.Equals("@organizeAndProgeny"))
            {
                oIdList.AddRange(_userManager.CurrentOrganizationTree);
                continue;
            }

            var oid = x.Split("--").First();
            if (x.Contains("--org"))
            {
                oIdList.Add(oid);
            }
            else if (x.Contains("--subOrg"))
            {
                var ids = allOrgList.Where(x => x.ParentId.Equals(oid)).Select(x => x.Id).ToList();
                oIdList.Add(oid);
                oIdList.AddRange(ids);
            }
            else if (x.Contains("--progenyOrg"))
            {
                var ids = allOrgList.Where(x => x.OrganizeIdTree.Contains(oid)).Select(x => x.Id).ToList();
                oIdList.AddRange(ids);
            }
            else
            {
                oIdList.Add(x);
            }
        }

        var data = allOrgList.Where(t => t.DeleteMark == null && oIdList.Contains(t.Id))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime).Select(a => new OrganizeListOutput()
            {
                id = a.Id,
                parentId = a.ParentId,
                category = a.Category,
                enCode = a.EnCode,
                fullName = a.FullName,
                icon = SqlFunc.IIF(a.Category.Equals(CommonConst.OrganizeTypeGroup) || a.Category.Equals(CommonConst.OrganizeTypeInstitution), "icon-ym icon-ym-tree-organization3", "icon-ym icon-ym-tree-department1"),
                orgNameTree = a.Description,
                organizeIdTree = a.OrganizeIdTree,
            }).ToList();

        data.ForEach(item =>
        {
            if (!data.Any(x => x.id.Equals(item.parentId))) item.parentId = "0";
        });
        return new { list = data.ToTree("0") };
    }

    /// <summary>
    /// 根据组织Id List 获取当前所属组织(部门).
    /// </summary>
    /// <returns></returns>
    //[HttpPost("getDefaultCurrentValueDepartmentId")]
    [NonAction]
    public async Task<dynamic> GetDefaultCurrentValueDepartmentId([FromBody] GetDefaultCurrentValueInput input)
    {
        var depId = _repository.AsSugarClient().Queryable<UserEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId))
            .Where((a, b) => a.Id.Equals(_userManager.UserId) && b.Category.Equals("department")).Select((a, b) => a.OrganizeId).First();

        if (input.DepartIds == null || !input.DepartIds.Any()) return new { departmentId = depId };
        var userRelationList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => input.DepartIds.Contains(x.ObjectId))
            .Select(x => x.UserId).ToList();

        if (userRelationList.Contains(_userManager.UserId)) return new { userId = depId };
        else return new { departmentId = string.Empty };
    }

    /// <summary>
    /// 通过部门id获取部门列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    //[HttpPost("OrganizeCondition")]
    [NonAction]
    public async Task<dynamic> OrganizeCondition([FromBody] OrganizeConditionInput input)
    {
        // 获取所有组织
        List<OrganizeEntity>? allOrgList = GetOrgListTreeName();

        if (input.departIds.Contains("@currentOrg"))
        {
            input.departIds.Add(_userManager.User.OrganizeId);
            input.departIds.Remove("@currentOrg");
        }
        if (input.departIds.Contains("@currentOrgAndSubOrg"))
        {
            input.departIds.AddRange(allOrgList.TreeChildNode(_userManager.User.OrganizeId, t => t.Id, t => t.ParentId).Select(it => it.Id).ToList());
            input.departIds.Remove("@currentOrgAndSubOrg");
        }
        if (input.departIds.Contains("@currentGradeOrg"))
        {
            if (_userManager.IsAdministrator)
            {
                input.departIds.AddRange(allOrgList.Select(it => it.Id).ToList());
            }
            input.departIds.Remove("@currentGradeOrg");
        }

        List<OrganizeListOutput>? data = await _repository.AsQueryable()
            .Where(a => a.DeleteMark == null && input.departIds.Contains(a.Id))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .Select(a => new OrganizeListOutput
            {
                id = a.Id,
                organizeIdTree = a.OrganizeIdTree,
                type = a.Category,
                parentId = a.ParentId,
                lastFullName = a.FullName,
                fullName = a.FullName,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                icon = a.Category.Equals(CommonConst.OrganizeTypeGroup) || a.Category.Equals(CommonConst.OrganizeTypeInstitution) ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                sortCode = a.SortCode
            }).ToListAsync();

        data.ForEach(item =>
        {
            var description = allOrgList.FirstOrDefault(x => x.Id == item.id)?.Description;
            if (!data.Any(x => x.id.Equals(item.parentId)))
            {
                item.fullName = description;
                item.parentId = "0";
            }
            var ids = new List<string>();
            ids.AddRange(item.organizeIdTree.Split(",").ToList());
            item.organizeIds = ids;
            item.organize = description;
        });

        // 组织断层处理
        data.OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            if (!data.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = data.Find(x => x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
                if (pItem != null)
                {
                    item.parentId = pItem.id;
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
                else
                {
                    item.parentId = "0";
                }
            }
            else
            {
                var pItem = data.Find(x => x.id.Equals(item.parentId));
                item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
            }
        });

        return new { list = data.ToTree("0") };
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] OrganizeCrInput input)
    {
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2008);
        if (await _repository.IsAnyAsync(o => o.ParentId == input.parentId && o.FullName == input.fullName && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2009);
        OrganizeEntity? entity = input.Adapt<OrganizeEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize) : entity.EnCode;
        //entity.PropertyJson = JSON.Serialize(input.propertyJson);

        #region 上级组织逻辑验证

        var parentEntity = await _repository.GetFirstAsync(x => x.Id.Equals(input.parentId));

        switch (input.category)
        {
            case CommonConst.OrganizeTypeBase:
                if (await _repository.IsAnyAsync(x => x.ParentId.Equals("-1"))) throw Oops.Oh(ErrorCode.COM1018, "已存在根组织");
                break;
            case CommonConst.OrganizeTypeGroup:
                if (!input.parentId.Equals("-1")) throw Oops.Oh(ErrorCode.COM1018, "集团必须是在根组织之下");
                break;
            case CommonConst.OrganizeTypeCompany:
                if (parentEntity != null && parentEntity.Category != CommonConst.OrganizeTypeGroup) throw Oops.Oh(ErrorCode.COM1018, "上级组织必须是根组织或者集团");
                break;
            case CommonConst.OrganizeTypeSubsidiary:
                if (parentEntity == null || parentEntity.Category != CommonConst.OrganizeTypeCompany) throw Oops.Oh(ErrorCode.COM1018, "上级组织必须是公司");
                break;
            case CommonConst.OrganizeTypeDepartment:
                if (parentEntity == null) throw Oops.Oh(ErrorCode.COM1018, "上级组织不能是根组织");
                break;
            case CommonConst.OrganizeTypeInstitution:
                if (!input.parentId.Equals("-1")) throw Oops.Oh(ErrorCode.COM1018, "机构必须是在根组织之下");
                break;
            case CommonConst.OrganizeTypeOrgan:
                if (parentEntity != null && parentEntity.Category != CommonConst.OrganizeTypeInstitution) throw Oops.Oh(ErrorCode.COM1018, "上级组织必须是机构");
                break;
            case CommonConst.OrganizeTypeUnit:
                if (parentEntity != null && (parentEntity.Category != CommonConst.OrganizeTypeInstitution && parentEntity.Category != CommonConst.OrganizeTypeOrgan)) throw Oops.Oh(ErrorCode.COM1018, "上级组织必须是根组织、机构、机关");
                break;
        }

        #endregion

        #region 处理 上级ID列表 存储

        List<string>? idList = new List<string>();
        List<string>? nameList = new List<string>();
        if (entity.ParentId != "-1")
        {
            var tree = _repository.AsSugarClient().Queryable<OrganizeEntity>().First(x => x.Id.Equals(entity.ParentId));
            List<string>? ids = tree.OrganizeIdTree.Split(",").ToList();
            List<string>? names = tree.OrganizeNameTree.Split("/").ToList();
            idList.AddRange(ids);
            nameList.AddRange(names);
            idList.Add(entity.Id);
            nameList.Add(entity.FullName);
        }
        else
        {
            idList.Add(entity.Id);
            nameList.Add(entity.FullName);
        }
        entity.OrganizeIdTree = string.Join(",", idList);
        entity.OrganizeNameTree = string.Join("/", nameList);

        var layer = await _userManager.GetOrganizeOrPositionLayer(CommonConst.RelationObjectOrganize);
        if (idList.Count > layer) throw Oops.Oh(ErrorCode.D2021, layer);
        #endregion

        var def = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("sysPositionName")).Select(x => x.Value).FirstAsync();
        var posEntity = new PositionEntity()
        {
            Id = SnowflakeIdHelper.NextId(),
            ParentId = null,
            EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePosition),
            FullName = def.IsNullOrEmpty() ? "默认岗位" : def,
            OrganizeId = entity.Id,
            SortCode = 0,
            DefaultMark = 1,
        };
        posEntity.PositionIdTree = posEntity.Id;
        entity.DutyPositionId = posEntity.Id;
        await _repository.AsSugarClient().Insertable(posEntity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        OrganizeEntity? isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        _ = isOk ?? throw Oops.Oh(ErrorCode.D2012);
        _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);

        #region 第三方同步

        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            var orgList = new List<OrganizeListOutput> { entity.Adapt<OrganizeListOutput>() };
            if (sysConfig.dingDepartment.IsNotEmptyOrNull() && entity.OrganizeIdTree.Contains(sysConfig.dingDepartment))
            {
                if (sysConfig.dingSynIsSynOrg)
                {
                    await _synThirdInfoService.SynDep(2, 1, sysConfig, orgList);
                }
                else
                {
                    var synEntity = new SynThirdInfoEntity() { ThirdType = 2, DataType = 1, SysObjId = entity.Id, EnabledMark = 0 };
                    await _repository.AsSugarClient().Insertable(synEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
            }

            if (sysConfig.qyhDepartment.IsNotEmptyOrNull() && entity.OrganizeIdTree.Contains(sysConfig.qyhDepartment))
            {
                if (sysConfig.qyhIsSynOrg)
                {
                    await _synThirdInfoService.SynDep(1, 1, sysConfig, orgList);
                }
                else
                {
                    var synEntity = new SynThirdInfoEntity() { ThirdType = 1, DataType = 1, SysObjId = entity.Id, EnabledMark = 0 };
                    await _repository.AsSugarClient().Insertable(synEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
            }
        }
        catch (Exception)
        {
        }

        #endregion
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] OrganizeUpInput input)
    {
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D2008);
        OrganizeEntity? oldEntity = await _repository.GetSingleAsync(it => it.Id == id);

        if (input.parentId.Equals(id))
            throw Oops.Oh(ErrorCode.D2001);
        if (input.parentId.Equals("-1") && !oldEntity.ParentId.Equals("-1") && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        // 父id不能为自己的子节点
        if (input.parentId.IsNotEmptyOrNull() && await _repository.IsAnyAsync(p => p.Id.Equals(input.parentId) && p.OrganizeIdTree.Contains(id) && p.Id != id))
            throw Oops.Oh(ErrorCode.D2001);

        if (await _repository.IsAnyAsync(o => o.ParentId == input.parentId && o.FullName == input.fullName && o.Id != id && o.Category == input.category && o.DeleteMark == null && o.Id != id))
            throw Oops.Oh(ErrorCode.D2009);

        #region 上级组织逻辑验证

        var parentEntity = await _repository.GetFirstAsync(x => x.Id.Equals(input.parentId));
        var chlidEntity = await _repository.AsQueryable().Where(x => x.ParentId.Equals(oldEntity.Id) && x.Id != oldEntity.Id && x.DeleteMark == null).OrderBy(x => x.CreatorTime).ToListAsync();
        var typeList1 = new List<string>() { CommonConst.OrganizeTypeGroup, CommonConst.OrganizeTypeCompany, CommonConst.OrganizeTypeSubsidiary };
        var typeList2 = new List<string>() { CommonConst.OrganizeTypeInstitution, CommonConst.OrganizeTypeOrgan, CommonConst.OrganizeTypeUnit };

        if (chlidEntity.Any())
        {
            foreach (var item in chlidEntity)
            {
                if ((typeList1.Contains(item.Category) && typeList2.Contains(input.category)) || typeList2.Contains(item.Category) && typeList1.Contains(input.category))
                    throw Oops.Oh(ErrorCode.COM1018, "下级组织不支持当前选中的组织类型");

                if ((input.category.Equals(CommonConst.OrganizeTypeGroup) && item.Category.Equals(CommonConst.OrganizeTypeSubsidiary))
                    || (input.category.Equals(CommonConst.OrganizeTypeCompany) && item.Category.Equals(CommonConst.OrganizeTypeCompany))
                    || (input.category.Equals(CommonConst.OrganizeTypeSubsidiary) && item.Category.Equals(CommonConst.OrganizeTypeCompany))
                    || (input.category.Equals(CommonConst.OrganizeTypeSubsidiary) && item.Category.Equals(CommonConst.OrganizeTypeSubsidiary))
                    || (input.category.Equals(CommonConst.OrganizeTypeDepartment) && item.Category.Equals(CommonConst.OrganizeTypeCompany))
                    || (input.category.Equals(CommonConst.OrganizeTypeDepartment) && item.Category.Equals(CommonConst.OrganizeTypeSubsidiary)))
                    throw Oops.Oh(ErrorCode.COM1018, "下级组织不支持当前选中的组织类型");

                if ((input.category.Equals(CommonConst.OrganizeTypeOrgan) && item.Category.Equals(CommonConst.OrganizeTypeOrgan))
                    || (input.category.Equals(CommonConst.OrganizeTypeUnit) && item.Category.Equals(CommonConst.OrganizeTypeOrgan))
                    || (input.category.Equals(CommonConst.OrganizeTypeUnit) && item.Category.Equals(CommonConst.OrganizeTypeUnit))
                    || (input.category.Equals(CommonConst.OrganizeTypeDepartment) && item.Category.Equals(CommonConst.OrganizeTypeOrgan))
                    || (input.category.Equals(CommonConst.OrganizeTypeDepartment) && item.Category.Equals(CommonConst.OrganizeTypeUnit))
                    || (input.category.Equals(CommonConst.OrganizeTypeInstitution) && item.Category.Equals(CommonConst.OrganizeTypeDepartment))
                    || (input.category.Equals(CommonConst.OrganizeTypeOrgan) && item.Category.Equals(CommonConst.OrganizeTypeDepartment)))
                    throw Oops.Oh(ErrorCode.COM1018, "下级组织不支持当前选中的组织类型");
            }
        }

        switch (input.category)
        {
            case CommonConst.OrganizeTypeGroup:
                if (!input.parentId.Equals("-1")) throw Oops.Oh(ErrorCode.COM1018, "集团必须是在根组织之下");
                break;
            case CommonConst.OrganizeTypeCompany:
                if (parentEntity != null && parentEntity.Category != CommonConst.OrganizeTypeGroup) throw Oops.Oh(ErrorCode.COM1018, "上级组织必须是根组织或者集团");
                break;
            case CommonConst.OrganizeTypeSubsidiary:
                if (parentEntity == null || parentEntity.Category != CommonConst.OrganizeTypeCompany) throw Oops.Oh(ErrorCode.COM1018, "上级组织必须是公司");
                break;
            case CommonConst.OrganizeTypeDepartment:
                if (parentEntity == null) throw Oops.Oh(ErrorCode.COM1018, "上级组织不能是根组织");
                break;
            case CommonConst.OrganizeTypeInstitution:
                if (!input.parentId.Equals("-1")) throw Oops.Oh(ErrorCode.COM1018, "机构必须是在根组织之下");
                break;
            case CommonConst.OrganizeTypeOrgan:
                if (parentEntity != null && parentEntity.Category != CommonConst.OrganizeTypeInstitution) throw Oops.Oh(ErrorCode.COM1018, "上级组织必须是机构");
                break;
            case CommonConst.OrganizeTypeUnit:
                if (parentEntity != null && (parentEntity.Category != CommonConst.OrganizeTypeInstitution && parentEntity.Category != CommonConst.OrganizeTypeOrgan)) throw Oops.Oh(ErrorCode.COM1018, "上级组织必须是根组织、机构、机关");
                break;
        }

        #endregion

        OrganizeEntity? entity = input.Adapt<OrganizeEntity>();
        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize) : entity.EnCode;
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;
        //entity.PropertyJson = JSON.Serialize(input.propertyJson);

        List<OrganizeEntity>? oldEntityList = null;

        #region 处理 上级ID列表 存储

        if (string.IsNullOrWhiteSpace(oldEntity.OrganizeIdTree) || entity.ParentId != oldEntity.ParentId || entity.FullName != oldEntity.FullName)
        {
            List<string>? idList = new List<string>();
            List<string>? nameList = new List<string>();
            if (entity.ParentId != "-1")
            {
                var tree = _repository.AsSugarClient().Queryable<OrganizeEntity>().First(x => x.Id.Equals(entity.ParentId));
                List<string>? ids = tree.OrganizeIdTree.Split(",").ToList();
                List<string>? names = tree.OrganizeNameTree.Split("/").ToList();
                idList.AddRange(ids);
                nameList.AddRange(names);
            }
            idList.Add(entity.Id);
            nameList.Add(entity.FullName);
            entity.OrganizeIdTree = string.Join(",", idList);
            entity.OrganizeNameTree = string.Join("/", nameList);

            var layer = await _userManager.GetOrganizeOrPositionLayer(CommonConst.RelationObjectOrganize);
            if (idList.Count > layer) throw Oops.Oh(ErrorCode.D2021, layer);

            // 如果上级结构 变动 ，需要更改所有包含 该组织的id 的结构
            if (entity.ParentId != oldEntity.ParentId)
            {
                // 验证涉及流程相关逻辑拦截
                if (await _repository.AsSugarClient().Queryable<WorkFlow.Entitys.Entity.WorkFlowLaunchUserEntity>().Where(x => x.OrganizeId.Contains(entity.Id) && x.Type.Equals(2)).AnyAsync())
                    throw Oops.Oh(ErrorCode.WF0080);
            }

            if (entity.ParentId != oldEntity.ParentId || entity.FullName != oldEntity.FullName)
            {
                oldEntityList = await _repository.AsQueryable().Where(x => x.OrganizeIdTree.Contains(oldEntity.OrganizeIdTree + ",") && x.Id != oldEntity.Id).ToListAsync();
                var allOrgList = await _repository.AsQueryable().Select(x => new OrganizeEntity() { Id = x.Id, FullName = x.FullName }).ToListAsync();
                allOrgList.Find(x => x.Id == id).FullName = entity.FullName;
                oldEntityList.Add(oldEntity);
                foreach (var item in oldEntityList)
                {
                    string? childList = item.OrganizeIdTree.Split(oldEntity.Id).LastOrDefault();
                    item.OrganizeIdTree = entity.OrganizeIdTree + childList;
                    var itNameList = new List<string>();
                    foreach (var it in item.OrganizeIdTree.Split(',')) itNameList.Add(allOrgList.Find(x => x.Id.Equals(it)).FullName);
                    item.OrganizeNameTree = string.Join("/", itNameList);
                }

            }
        }

        #endregion

        try
        {
            // 开启事务
            _db.BeginTran();

            if (oldEntityList != null)
                await _repository.AsSugarClient().Updateable(oldEntityList).UpdateColumns(x => new { x.OrganizeIdTree, x.OrganizeNameTree }).ExecuteCommandAsync(); // 批量修改 父级组织
            await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);
            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.D2010);
        }

        #region 第三方同步

        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            List<OrganizeListOutput>? orgList = new List<OrganizeListOutput>();
            entity.Category = input.category;
            if (entity.OrganizeIdTree.IsNullOrEmpty()) entity.OrganizeIdTree = oldEntity.OrganizeIdTree;
            orgList.Add(entity.Adapt<OrganizeListOutput>());
            if (sysConfig.dingSynIsSynOrg && sysConfig.dingDepartment.IsNotEmptyOrNull() && entity.OrganizeIdTree.Contains(sysConfig.dingDepartment))
                await _synThirdInfoService.SynDep(2, 1, sysConfig, orgList);
            if (sysConfig.qyhIsSynOrg && sysConfig.qyhDepartment.IsNotEmptyOrNull() && entity.OrganizeIdTree.Contains(sysConfig.qyhDepartment))
                await _synThirdInfoService.SynDep(1, 1, sysConfig, orgList);
        }
        catch (Exception)
        {
        }

        #endregion
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var orgList = await _repository.AsQueryable().Where(x => x.OrganizeIdTree.Contains(id) && x.DeleteMark == null).ToListAsync();
        var orgIds = orgList.Select(x => x.Id).ToList();
        if (orgIds == null) throw Oops.Oh(ErrorCode.D2002);

        // 该机构下有用户，则不能删
        if (await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType == CommonConst.RelationObjectOrganize && orgIds.Contains(x.ObjectId)).AnyAsync())
            throw Oops.Oh(ErrorCode.D2022);

        try
        {
            // 开启事务
            _db.BeginTran();

            await _repository.AsSugarClient().Updateable<OrganizeEntity>().SetColumns(it => new OrganizeEntity()
            {
                DeleteMark = 1,
                DeleteTime = SqlFunc.GetDate(),
                DeleteUserId = _userManager.UserId,
            }).Where(it => orgIds.Contains(it.Id) && it.DeleteMark == null).ExecuteCommandAsync();

            // 删除岗位
            await _repository.AsSugarClient().Updateable<PositionEntity>().SetColumns(it => new PositionEntity()
            {
                DeleteMark = 1,
                DeleteTime = SqlFunc.GetDate(),
                DeleteUserId = _userManager.UserId,
            }).Where(it => orgIds.Contains(it.OrganizeId) && it.DeleteMark == null).ExecuteCommandAsync();

            // 删除该组织和角色关联数据
            await _repository.AsSugarClient().Deleteable<RoleRelationEntity>().Where(x => orgIds.Contains(x.ObjectId) && x.ObjectType == CommonConst.RelationObjectOrganize).ExecuteCommandAsync();
            _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);
            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.D2012);
        }

        #region 第三方同步
        try
        {
            var entity = orgList.Find(x => x.Id.Equals(id));
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            if (sysConfig.dingSynIsSynOrg && entity.OrganizeIdTree.Contains(sysConfig.dingDepartment))
            {
                foreach (var org in orgList.OrderByDescending(e => e.OrganizeIdTree.Split(',').Length).ToList())
                    await _synThirdInfoService.DelSynData(2, 1, sysConfig, org.Id);
            }
            else
            {
                await _repository.AsSugarClient().Deleteable<SynThirdInfoEntity>().Where(x => x.ThirdType.Equals(2) && x.DataType.Equals(1) && x.SysObjId.Equals(id) && x.EnabledMark.Equals(0)).ExecuteCommandAsync();
            }

            if (sysConfig.qyhIsSynOrg && entity.OrganizeIdTree.Contains(sysConfig.qyhDepartment))
            {
                foreach (var org in orgList.OrderByDescending(e => e.OrganizeIdTree.Split(',').Length).ToList())
                    await _synThirdInfoService.DelSynData(1, 1, sysConfig, org.Id);
            }
            else
            {
                await _repository.AsSugarClient().Deleteable<SynThirdInfoEntity>().Where(x => x.ThirdType.Equals(1) && x.DataType.Equals(1) && x.SysObjId.Equals(id) && x.EnabledMark.Equals(0)).ExecuteCommandAsync();
            }
        }
        catch (Exception)
        {
        }
        #endregion
    }

    /// <summary>
    /// 更新状态.
    /// </summary>
    /// <param name="id">主键</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task UpdateState(string id)
    {
        if (!await _repository.IsAnyAsync(u => u.Id == id && u.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2002);

        int isOk = await _repository.AsSugarClient().Updateable<OrganizeEntity>().SetColumns(it => new OrganizeEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandAsync();
        _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D2011);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 是否机构主管.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<bool> GetIsManagerByUserId(string userId)
    {
        return await _repository.IsAnyAsync(o => o.EnabledMark == 1 && o.DeleteMark == null && o.ManagerId == userId);
    }

    /// <summary>
    /// 获取机构列表(其他服务使用).
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeEntity>> GetListAsync()
    {
        return await _repository.AsQueryable().Where(t => t.EnabledMark == 1 && t.DeleteMark == null).OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
    }

    /// <summary>
    /// 获取公司列表(其他服务使用).
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeEntity>> GetCompanyListAsync()
    {
        return await _repository.AsQueryable().Where(t => t.EnabledMark == 1 && t.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
    }

    /// <summary>
    /// 下属机构.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <param name="isAdmin">是否管理员.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<string[]> GetSubsidiary(string organizeId, bool isAdmin)
    {
        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(o => o.DeleteMark == null && o.EnabledMark == 1).OrderBy(o => o.SortCode).ToListAsync();
        if (!isAdmin)
            data = data.TreeChildNode(organizeId, t => t.Id, t => t.ParentId);
        return data.Select(m => m.Id).ToArray();
    }

    /// <summary>
    /// 下属机构.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<string>> GetSubsidiary(string organizeId)
    {
        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(o => o.DeleteMark == null && o.EnabledMark == 1).OrderBy(o => o.SortCode).ToListAsync();
        data = data.TreeChildNode(organizeId, t => t.Id, t => t.ParentId);
        return data.Select(m => m.Id).ToList();
    }

    /// <summary>
    /// 获取机构成员列表 .
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeMemberListOutput>> GetOrganizeMemberList(string organizeId)
    {
        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();
        if (organizeId.Equals("0"))
        {
            List<OrganizeEntity>? data = await _repository.AsQueryable()
                .Where(o => o.DeleteMark == null && o.EnabledMark == 1 && o.ParentId.Equals("-1")).OrderBy(o => o.SortCode).ToListAsync();
            data.ForEach(o =>
            {
                output.Add(new OrganizeMemberListOutput
                {
                    id = o.Id,
                    parentId = o.ParentId,
                    fullName = o.FullName,
                    enabledMark = o.EnabledMark,
                    type = o.Category,
                    organize = o.FullName,
                    icon = o.Category.Equals(CommonConst.OrganizeTypeGroup) || o.Category.Equals(CommonConst.OrganizeTypeInstitution) ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                    organizeIdTree = o.OrganizeIdTree,
                    hasChildren = true,
                    isLeaf = false
                });
            });
        }
        else
        {
            var relationList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && x.ObjectId.Equals(organizeId)).Select(x => x.UserId).ToListAsync();
            List<UserEntity>? userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(u => relationList.Contains(u.Id) && u.EnabledMark > 0 && u.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
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
                    isLeaf = true,
                    isAdministrator = u.IsAdministrator,
                });
            });
            List<OrganizeEntity>? departmentList = await _repository.AsQueryable()
                .Where(o => o.ParentId.Equals(organizeId) && o.DeleteMark == null && o.EnabledMark == 1).OrderBy(o => o.SortCode).ToListAsync();
            departmentList.ForEach(o =>
            {
                output.Add(new OrganizeMemberListOutput()
                {
                    id = o.Id,
                    parentId = o.ParentId,
                    fullName = o.FullName,
                    enabledMark = o.EnabledMark,
                    type = o.Category,
                    icon = o.Category.Equals(CommonConst.OrganizeTypeGroup) || o.Category.Equals(CommonConst.OrganizeTypeInstitution) ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                    hasChildren = true,
                    organizeIdTree = o.OrganizeIdTree,
                    isLeaf = false
                });
            });
        }

        // 获取组织树
        var orgTree = GetOrgListTreeName();

        // 组织断层处理
        output.Where(x => x.parentId != "-1" && x.organizeIdTree.IsNotEmptyOrNull()).OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            item.fullName = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organize = item.fullName;
            if (!output.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = output.Find(x => x.organizeIdTree.IsNotEmptyOrNull() && x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
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

        if (!organizeId.Equals("0"))
        {
            var pOrgTreeName = orgTree.Find(x => x.Id.Equals(organizeId)).Description;
            output.ForEach(item => item.fullName = item.fullName.Replace(pOrgTreeName + "/", string.Empty));
        }

        return output;
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<OrganizeEntity> GetInfoById(string Id)
    {
        return await _repository.GetSingleAsync(p => p.Id == Id);
    }

    /// <summary>
    /// 获取组织下所有子组织.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<List<string>> GetChildOrgId(string id)
    {
        return (await _repository.GetListAsync(x => x.OrganizeIdTree.Contains(id) && x.EnabledMark == 1 && x.DeleteMark == null)).Select(x => x.Id).ToList();
    }

    /// <summary>
    /// 处理组织树 名称.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public List<OrganizeEntity> GetOrgListTreeName()
    {
        var orgTreeNameList = _repository.AsSugarClient().Queryable<OrganizeEntity>()
            .Where(x => x.DeleteMark == null)
            .Select(x => new OrganizeEntity()
            {
                Id = x.Id,
                EnCode = x.EnCode,
                ParentId = x.ParentId,
                FullName = x.FullName,
                Category = x.Category,
                OrganizeIdTree = x.OrganizeIdTree,
                Description = x.OrganizeNameTree,
            }).ToList();

        return orgTreeNameList;
    }

    /// <summary>
    /// 构造 OrganizeIdTree.
    /// </summary>
    private void StructureOrganizeIdTree()
    {

        if (_repository.IsAny(x => SqlFunc.IsNullOrEmpty(SqlFunc.ToString(x.OrganizeIdTree))))
        {
            var orgList = _repository.GetList(x => SqlFunc.IsNullOrEmpty(x.OrganizeIdTree) || SqlFunc.IsNullOrEmpty(x.OrganizeNameTree));

            foreach (var item in orgList)
            {
                if (item.ParentId == "-1")
                {
                    item.OrganizeIdTree = item.Id;
                }
                else
                {
                    var plist = GetOrganizeIdTree(item.Id);
                    plist.Reverse();
                    item.OrganizeIdTree = string.Join(",", plist);
                    var itNameList = new List<string>();
                    foreach (var it in item.OrganizeIdTree.Split(',')) itNameList.Add(orgList.Find(x => x.Id.Equals(it)).FullName);
                    item.OrganizeNameTree = string.Join("/", itNameList);
                }
            }

            _repository.AsUpdateable(orgList).ExecuteCommand();
        }
    }
    private List<string> GetOrganizeIdTree(string id)
    {
        var res = new List<string>() { id };
        var entity = _repository.AsQueryable().First(x => x.Id.Equals(id));
        if (entity != null && !entity.ParentId.Equals("-1")) res.AddRange(GetOrganizeIdTree(entity.ParentId));
        return res;
    }

    /// <summary>
    /// 获取选中组织、岗位、角色、分组、用户基本信息.
    /// </summary>
    /// <param name="input">参数.</param>
    public async Task<dynamic> GetSelectedList(UserSelectedInput input)
    {
        var resList = new List<UserSelectedOutput>();
        if (input.ids == null) return resList;
        var objIds = new List<string>();
        input.ids.ForEach(it => objIds.Add(it.Split("--").First()));
        var orgInfoList = GetOrgListTreeName();
        var posInfoList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null && objIds.Contains(x.Id)).Select(x => new PositionEntity() { Id = x.Id, FullName = x.FullName, OrganizeId = x.OrganizeId }).ToListAsync();
        var roleInfoList = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null && objIds.Contains(x.Id)).Select(x => new RoleEntity() { Id = x.Id, FullName = x.FullName }).ToListAsync();
        var groupInfoList = await _repository.AsSugarClient().Queryable<GroupEntity>().Where(x => x.DeleteMark == null && objIds.Contains(x.Id)).Select(x => new GroupEntity() { Id = x.Id, FullName = x.FullName }).ToListAsync();
        var userInfoList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null && objIds.Contains(x.Id)).Select(x => new UserEntity() { Id = x.Id, Account = SqlFunc.MergeString(x.RealName, "/", x.Account), HeadIcon = x.HeadIcon }).ToListAsync();

        foreach (var oid in input.ids)
        {
            if (oid.Equals("@userId"))
            {
                var user = _userManager.User;
                resList.Add(new UserSelectedOutput()
                {
                    id = "@userId",
                    fullName = user.RealName + "/" + user.Account,
                    orgNameTree = "当前用户",
                    icon = user.HeadIcon,
                    type = "user",
                });
                continue;
            }

            if (oid.Equals("@organizeId"))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = "@organizeId",
                    fullName = "当前用户所属组织",
                    orgNameTree = "当前用户所属组织",
                    icon = "icon-ym icon-ym-tree-organization3",
                    type = "organizeId",
                });
                continue;
            }

            if (oid.Equals("@organizeAndSub"))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = "@organizeAndSub",
                    fullName = "当前用户所属组织及子组织",
                    orgNameTree = "当前用户所属组织及子组织",
                    icon = "icon-ym icon-ym-tree-organization3",
                    type = "organizeAndSub",
                });
                continue;
            }

            if (oid.Equals("@organizeAndProgeny"))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = "@organizeAndProgeny",
                    fullName = "当前用户所属组织及子孙组织",
                    orgNameTree = "当前用户所属组织及子孙组织",
                    icon = "icon-ym icon-ym-tree-organization3",
                    type = "organizeAndProgeny",
                });
                continue;
            }

            if (oid.Equals("@positionId"))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = "@positionId",
                    fullName = "当前用户所属岗位",
                    orgNameTree = "当前用户所属岗位",
                    icon = "icon-ym icon-ym-tree-position1",
                    type = "positionId",
                });
                continue;
            }

            if (oid.Equals("@positionAndSub"))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = "@positionAndSub",
                    fullName = "当前用户所属岗位及子岗位",
                    orgNameTree = "当前用户所属岗位及子岗位",
                    icon = "icon-ym icon-ym-tree-position1",
                    type = "positionAndSub",
                });
                continue;
            }

            if (oid.Equals("@positionAndProgeny"))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = "@positionAndProgeny",
                    fullName = "当前用户所属岗位及子孙岗位",
                    orgNameTree = "当前用户所属岗位及子孙岗位",
                    icon = "icon-ym icon-ym-tree-position1",
                    type = "positionAndProgeny",
                });
                continue;
            }

            if (!oid.Contains("--"))
            {
                if (orgInfoList.Any(x => x.Id.Equals(oid)))
                {
                    resList.Add(new UserSelectedOutput()
                    {
                        id = oid,
                        fullName = (orgInfoList.Where(x => x.Id.Equals(oid.Split("--").First())).Select(x => x.Description).First()),
                        orgNameTree = (orgInfoList.Where(x => x.Id.Equals(oid.Split("--").First())).Select(x => x.Description).First()),
                        icon = "icon-ym icon-ym-tree-organization3",
                    });
                }
                if (posInfoList.Any(x => x.Id.Equals(oid)) && orgInfoList.Any(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)))
                {
                    resList.Add(new UserSelectedOutput()
                    {
                        id = oid,
                        fullName = orgInfoList.Find(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)).Description + "/" + posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        orgNameTree = orgInfoList.Find(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)).Description + "/" + posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        icon = "icon-ym icon-ym-tree-organization3",
                    });
                }
                if (roleInfoList.Any(x => x.Id.Equals(oid)))
                {
                    resList.Add(new UserSelectedOutput()
                    {
                        id = oid,
                        fullName = roleInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        orgNameTree = roleInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        icon = "icon-ym icon-ym-generator-role",
                    });
                }
                if (groupInfoList.Any(x => x.Id.Equals(oid)))
                {
                    resList.Add(new UserSelectedOutput()
                    {
                        id = oid,
                        fullName = groupInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        orgNameTree = groupInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        icon = "icon-ym icon-ym-generator-group1",
                    });
                }
                if (userInfoList.Any(x => x.Id.Equals(oid)))
                {
                    resList.Add(new UserSelectedOutput()
                    {
                        id = oid,
                        fullName = userInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).Account,
                        orgNameTree = userInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).Account,
                        icon = userInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).HeadIcon,
                    });
                }
                continue;
            }

            if (oid.Contains("--org") && orgInfoList.Any(x => x.Id.Equals(oid.Split("--").First())))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = oid,
                    fullName = (orgInfoList.Where(x => x.Id.Equals(oid.Split("--").First())).Select(x => x.Description).First()),
                    orgNameTree = (orgInfoList.Where(x => x.Id.Equals(oid.Split("--").First())).Select(x => x.Description).First()),
                    icon = "icon-ym icon-ym-tree-organization3",
                });
            }

            if (oid.Contains("--subOrg") && orgInfoList.Any(x => x.Id.Equals(oid.Split("--").First())))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = oid,
                    fullName = (orgInfoList.Where(x => x.Id.Equals(oid.Split("--").First())).Select(x => x.Description + "及子组织").First()),
                    orgNameTree = (orgInfoList.Where(x => x.Id.Equals(oid.Split("--").First())).Select(x => x.Description + "及子组织").First()),
                    icon = "icon-ym icon-ym-tree-organization3",
                });
            }

            if (oid.Contains("--progenyOrg") && orgInfoList.Any(x => x.Id.Equals(oid.Split("--").First())))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = oid,
                    fullName = (orgInfoList.Where(x => x.Id.Equals(oid.Split("--").First())).Select(x => x.Description + "及子孙组织").First()),
                    orgNameTree = (orgInfoList.Where(x => x.Id.Equals(oid.Split("--").First())).Select(x => x.Description + "及子孙组织").First()),
                    icon = "icon-ym icon-ym-tree-organization3",
                });
            }

            if (oid.Contains("--pos") && posInfoList.Any(x => x.Id.Equals(oid.Split("--").First())) && orgInfoList.Any(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = oid,
                    fullName = orgInfoList.Find(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)).Description + "/" + posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                    orgNameTree = orgInfoList.Find(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)).Description + "/" + posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                    icon = "icon-ym icon-ym-tree-position1",
                });
            }

            if (oid.Contains("--subPos") && posInfoList.Any(x => x.Id.Equals(oid.Split("--").First())) && orgInfoList.Any(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)))
            {

                resList.Add(new UserSelectedOutput()
                {
                    id = oid,
                    fullName = orgInfoList.Find(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)).Description + "/" + posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName + "及子岗位",
                    orgNameTree = orgInfoList.Find(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)).Description + "/" + posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName + "及子岗位",
                    icon = "icon-ym icon-ym-tree-position1",
                });
            }

            if (oid.Contains("--progenyPos") && posInfoList.Any(x => x.Id.Equals(oid.Split("--").First())) && orgInfoList.Any(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)))
            {
                resList.Add(new UserSelectedOutput()
                {
                    id = oid,
                    fullName = orgInfoList.Find(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)).Description + "/" + posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName + "及子孙岗位",
                    orgNameTree = orgInfoList.Find(x => x.Id.Equals(posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).OrganizeId)).Description + "/" + posInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName + "及子孙岗位",
                    icon = "icon-ym icon-ym-tree-position1",
                });
            }

            if (oid.Contains("--user"))
            {
                if (userInfoList.Any(x => x.Id.Equals(oid.Split("--").First())))
                {
                    resList.Add(new UserSelectedOutput()
                    {
                        id = oid,
                        fullName = userInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).Account,
                        orgNameTree = userInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).Account,
                        icon = userInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).HeadIcon,
                    });
                }
            }

            if (oid.Contains("--role"))
            {
                if (roleInfoList.Any(x => x.Id.Equals(oid.Split("--").First())))
                {
                    resList.Add(new UserSelectedOutput()
                    {
                        id = oid,
                        fullName = roleInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        orgNameTree = roleInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        icon = "icon-ym icon-ym-generator-role",
                    });
                }
            }

            if (oid.Contains("--group"))
            {
                if (groupInfoList.Any(x => x.Id.Equals(oid.Split("--").First())))
                {
                    resList.Add(new UserSelectedOutput()
                    {
                        id = oid,
                        fullName = groupInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        orgNameTree = groupInfoList.Find(x => x.Id.Equals(oid.Split("--").First())).FullName,
                        icon = "icon-ym icon-ym-generator-group1",
                    });
                }
            }

        }

        return new { list = resList.OrderBy(x => objIds.IndexOf(x.id)) };
    }
    #endregion

    #region 导出和导入

    /// <summary>
    /// 导出Excel.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("ExportData")]
    public async Task<dynamic> ExportData([FromQuery] OrganizeExportDataInput input)
    {
        var orgList = GetOrgListTreeName();

        DictionaryTypeEntity? typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "9b542177a477488994ce09fff3c93901" || x.EnCode == "EnterpriseNature") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? _enterpriseNatureList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 公司性质

        typeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "d59a3cf65f9847dbb08be449e3feae16" || x.EnCode == "IndustryType") && x.DeleteMark == null).FirstAsync();
        List<DictionaryDataEntity>? _industryList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null).ToListAsync(); // 所属行业

        var dataList = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Asc).Select(x => new {
                id = x.Id,
                category = x.Category,
                parentId = x.ParentId,
                fullName = x.FullName,
                enCode = x.EnCode,
                sortCode = x.SortCode,
                description = x.Description,
            }).ToListAsync();

        var data = new List<OrganizeListImportDataInput>();
        foreach (var item in dataList)
        {
            var addItem = new OrganizeListImportDataInput();
            addItem.category = GetOrgCategoryName(item.category);
            addItem.fullName = item.fullName;
            if (item.parentId != "-1") addItem.parentId = orgList.Where(x => x.Id.Equals(item.parentId)).Select(x => x.FullName + "/" + x.EnCode).FirstOrDefault();
            else addItem.parentId = string.Empty;
            addItem.enCode = item.enCode;
            addItem.sortCode = item.sortCode.ToString();
            addItem.description = item.description;
            data.Add(addItem);
        }

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("{1}_{0:yyyyMMddhhmmss}.xls", DateTime.Now, "组织信息");
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        var columnList = new List<ExportImportHelperModel>();
        columnList.AddRange(GetComInfoFieldToTitle());

        foreach (var item in columnList)
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, Required = item.Required });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var fs = ExcelExportHelper<OrganizeListImportDataInput>.ExportMemoryStream(data, excelconfig);
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
        List<OrganizeListImportDataInput>? dataList = new List<OrganizeListImportDataInput>() {
            new OrganizeListImportDataInput(){ parentId = "上级组织名称/编码(为空时则该组织添加为一级组织)", fullName = "组织名称" }};

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = "组织信息导入模板.xls";
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        var infoFields = GetImportInfoFieldToTitle();
        infoFields.RemoveAll(x => x.ColumnKey.Equals("errorsInfo"));
        foreach (var item in infoFields)
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, Required = item.Required, SelectList = item.SelectList });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var stream = ExcelExportHelper<OrganizeListImportDataInput>.ToStream(dataList, excelconfig);
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
    public async Task<dynamic> ExportExceptionData([FromBody] OrganizeImportDataInput list)
    {
        // 错误数据
        List<OrganizeListImportDataInput>? errorlist = list.list;

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("组织信息导入模板错误报告_{0}.xls", DateTime.Now.ToString("yyyyMMddHHmmss"));
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        foreach (var item in GetImportInfoFieldToTitle(true))
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, Required = item.Required, FreezePane = item.FreezePane, SelectList = item.SelectList });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var stream = ExcelExportHelper<OrganizeListImportDataInput>.ToStream(errorlist, excelconfig);
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
    public async Task<dynamic> ImportData([FromBody] OrganizeImportDataInput list)
    {
        if (list.type)
        {
            var excelData = await GetImportDataList(list.fileName);
            list.list = excelData.ToObject<List<OrganizeListImportDataInput>>();
        }

        list.list.ForEach(x => x.errorsInfo = string.Empty);
        object[]? res = await DataImport(list.list);
        List<OrganizeEntity>? addlist = res.First() as List<OrganizeEntity>;
        List<OrganizeListImportDataInput>? errorlist = res.Last() as List<OrganizeListImportDataInput>;
        return new { snum = addlist.Count, fnum = errorlist.Count, failResult = errorlist, resultType = errorlist.Count < 1 ? 0 : 1 };
    }

    /// <summary>
    /// 获取导入数据.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private async Task<DataTable> GetImportDataList(string fileName)
    {
        DataTable? excelData = new DataTable();
        try
        {
            var FileEncode = GetImportInfoFieldToTitle();

            string? filePath = FileVariable.TemporaryFilePath;
            string? savePath = Path.Combine(filePath, fileName);

            // 得到数据
            var sr = await _fileManager.GetFileStream(savePath);
            excelData = ExcelImportHelper.ToDataTable(savePath, sr);
            foreach (var it in excelData.Columns)
            {
                var item = it as DataColumn;
                if (item.ColumnName.Equals("errorsInfo")) throw Oops.Oh(ErrorCode.D1807);
                if (!FileEncode.Any(x => x.ColumnKey == item.ToString() && x.ColumnValue.Equals(item.Caption.Replace("*", "")))) throw Oops.Oh(ErrorCode.D1807);
                excelData.Columns[item.ToString()].ColumnName = FileEncode.Where(x => x.ColumnKey == item.ToString()).FirstOrDefault().ColumnKey;
            }

            if (excelData.Rows.Count > 0) excelData.Rows.RemoveAt(0);
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1807);
        }

        if (excelData.Rows.Count < 1)
            throw Oops.Oh(ErrorCode.D5019);
        if (excelData.Rows.Count > 1000)
            throw Oops.Oh(ErrorCode.D5029);

        // 返回结果
        return excelData;
    }

    /// <summary>
    /// 导入数据函数.
    /// </summary>
    /// <param name="list">list.</param>
    /// <returns>[成功列表,失败列表].</returns>
    private async Task<object[]> DataImport(List<OrganizeListImportDataInput> inputList)
    {
        var orgList = GetOrgListTreeName();

        foreach (var it in inputList) it.id = SnowflakeIdHelper.NextId();
        var list = inputList.ToObject<List<OrganizeListImportDataInput>>();
        foreach (var it in list) if (it.parentId.IsNotEmptyOrNull()) it.parentId = it.parentId.Split("/").LastOrDefault();
        foreach (var it in list)
        {
            if (it.parentId.IsNotEmptyOrNull())
            {
                var pEntity = orgList.FirstOrDefault(x => x.EnCode.Equals(it.parentId));
                if (pEntity != null)
                {
                    it.parentId = pEntity.Id;
                }
                else
                {
                    var pItem = list.FirstOrDefault(x => x.enCode.IsNotEmptyOrNull() && x.enCode.Equals(it.parentId));
                    if (pItem != null) it.parentId = pItem.id;
                }
            }
        }
        foreach (var it in list) it.organizeIdTree = ImportInitIdTree(it, list);

        if (inputList == null || inputList.Count() < 1) throw Oops.Oh(ErrorCode.D5019);
        if (inputList.Count > 1000) throw Oops.Oh(ErrorCode.D5029);

        List<OrganizeEntity> addList = new List<OrganizeEntity>();
        List<PositionEntity> addPosList = new List<PositionEntity>();

        var layer = await _userManager.GetOrganizeOrPositionLayer(CommonConst.RelationObjectOrganize);
        var regexEnCode = @"^[a-zA-Z0-9.]*$";
        foreach (var item in list.OrderBy(x => x.organizeIdTree.Count()).ToList())
        {
            if (item.category.IsNullOrWhiteSpace())
            {
                item.errorsInfo += "类型不能为空,";
                continue;
            }

            if (item.category.IsNotEmptyOrNull() && GetOrgCategory(item.category) == null)
            {
                item.errorsInfo += "找不到该类型值,";
                continue;
            }
            item.category = GetOrgCategory(item.category);
            if (item.parentId.IsNotEmptyOrNull() && !list.Any(x => x.id.Equals(item.parentId) && x.errorsInfo.IsNullOrEmpty()) && !orgList.Any(x => x.Id.Equals(item.parentId)))
            {
                item.errorsInfo += "找不到该上级组织,";
                continue;
            }

            var parentEntity = orgList.Where(x => x.Id.Equals(item.parentId)).FirstOrDefault();
            if (parentEntity == null && list.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = list.Where(x => x.id.Equals(item.parentId)).FirstOrDefault();
                parentEntity = new OrganizeEntity();
                parentEntity.Category = pItem.category;
                parentEntity.OrganizeIdTree = pItem.organizeIdTree;
            }
            if (item.parentId.IsNullOrEmpty()) parentEntity = null;
            #region 上级组织逻辑验证
            switch (item.category)
            {
                case CommonConst.OrganizeTypeGroup:
                    if (parentEntity != null) item.errorsInfo += "集团必须是在根组织之下,";
                    break;
                case CommonConst.OrganizeTypeCompany:
                    if (parentEntity != null && parentEntity.Category != CommonConst.OrganizeTypeGroup) item.errorsInfo += "上级组织必须是根组织或者集团,";
                    break;
                case CommonConst.OrganizeTypeSubsidiary:
                    if (parentEntity == null || (parentEntity != null && parentEntity.Category != CommonConst.OrganizeTypeCompany)) item.errorsInfo += "上级组织必须是公司,";
                    break;
                case CommonConst.OrganizeTypeDepartment:
                    if (parentEntity == null)
                    {
                        item.errorsInfo += "上级组织不能是根组织,";
                    }
                    else
                    {
                        if (parentEntity.Category == CommonConst.OrganizeTypeInstitution || parentEntity.Category == CommonConst.OrganizeTypeOrgan)
                            item.errorsInfo += "上级组织不能是机构、机关,";
                    }

                    break;
                case CommonConst.OrganizeTypeInstitution:
                    if (parentEntity != null) item.errorsInfo += "机构必须是在根组织之下,";
                    break;
                case CommonConst.OrganizeTypeOrgan:
                    if (parentEntity != null && parentEntity.Category != CommonConst.OrganizeTypeInstitution) item.errorsInfo += "上级组织必须是机构,";
                    break;
                case CommonConst.OrganizeTypeUnit:
                    if (parentEntity != null && (parentEntity.Category != CommonConst.OrganizeTypeInstitution && parentEntity.Category != CommonConst.OrganizeTypeOrgan)) item.errorsInfo += "上级组织必须是根组织、机构、机关,";
                    break;
            }

            #endregion

            if (item.fullName.IsNullOrWhiteSpace()) item.errorsInfo += "组织名称不能为空,";
            else if (item.fullName.Length > 50) item.errorsInfo += "组织名称值超出最多输入字符限制,";
            if (item.enCode.IsNullOrWhiteSpace()) item.enCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize);
            else if (item.enCode.Length > 50) item.errorsInfo += "组织编码值超出最多输入字符限制,";
            if (item.enCode.IsNotEmptyOrNull() && (!Regex.IsMatch(item.enCode, regexEnCode) || item.enCode.First().Equals('.') || item.enCode.Last().Equals('.')))
                item.errorsInfo += "组织编码值只能输入英文、数字和小数点且小数点不能放在首尾,";

            if (item.sortCode.IsNotEmptyOrNull())
            {
                try
                {
                    var sortCode = long.Parse(item.sortCode);
                    if (sortCode < 0) item.errorsInfo += "排序值不能小于0,";
                    else if (sortCode > 999999) item.errorsInfo += "排序值不能大于999999,";
                }
                catch
                {
                    item.errorsInfo += "排序值不正确,";
                }
            }

            if (item.fullName.IsNotEmptyOrNull())
            {
                var errorItems = list.Where(x => x.fullName == item.fullName && x.category == item.category).ToList();
                for (var i = 1; i < errorItems.Count; i++) if (!errorItems[i].errorsInfo.Contains("组织名称值已存在")) errorItems[i].errorsInfo += "组织名称值已存在,";
                if (orgList.Any(x => x.FullName == item.fullName && x.Category == item.category) && !item.errorsInfo.Contains("组织名称值已存在")) item.errorsInfo += "组织名称值已存在,";
            }
            if (item.enCode.IsNotEmptyOrNull())
            {
                var errorItems = list.Where(x => x.enCode == item.enCode).ToList();
                for (var i = 1; i < errorItems.Count; i++) if (!errorItems[i].errorsInfo.Contains("组织编码值已存在")) errorItems[i].errorsInfo += "组织编码值已存在,";
                if (orgList.Any(x => x.EnCode == item.enCode) && !item.errorsInfo.Contains("组织编码值已存在")) item.errorsInfo += "组织编码值已存在,";
            }

            OrganizeEntity entity = new OrganizeEntity();
            entity.Id = item.id;
            entity.ParentId = item.parentId.IsNotEmptyOrNull() ? item.parentId : "-1";
            entity.Category = item.category;
            entity.FullName = item.fullName;
            entity.Description = item.description;
            entity.EnCode = item.enCode;
            entity.SortCode = item.sortCode.IsNotEmptyOrNull() ? item.sortCode.ParseToLong() : 0;

            // 层级处理
            List<string>? idList = new List<string>();
            List<string>? nameList = new List<string>();

            if (parentEntity != null)
            {
                List<string>? ids = parentEntity.OrganizeIdTree.Split(",").ToList();
                List<string>? names = new List<string>();
                foreach (var itemId in ids)
                {
                    if (orgList.Any(x => x.Id.Equals(itemId))) names.Add(orgList.Find(x => x.Id.Equals(itemId)).FullName);
                    else if (list.Any(x => x.id.Equals(itemId))) names.Add(list.Find(x => x.id.Equals(itemId)).fullName);
                }
                idList.AddRange(ids);
                nameList.AddRange(names);
                idList.Add(entity.Id);
                nameList.Add(entity.FullName);
            }
            else
            {
                idList.Add(entity.Id);
                nameList.Add(entity.FullName);
            }
            entity.OrganizeIdTree = string.Join(",", idList);
            entity.OrganizeNameTree = string.Join("/", nameList);
            if (idList.Count > layer) item.errorsInfo += string.Format("该组织限制{0}级,已达上限,", layer);

            if (item.errorsInfo.IsNullOrEmpty())
            {
                var def = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("sysPositionName")).Select(x => x.Value).FirstAsync();
                var posEntity = new PositionEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    ParentId = "-1",
                    EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePosition),
                    FullName = def.IsNullOrEmpty() ? "默认岗位" : def,
                    OrganizeId = entity.Id,
                    SortCode = 0,
                    DefaultMark = 1,
                };
                posEntity.PositionIdTree = posEntity.Id;
                entity.DutyPositionId = posEntity.Id;
                addPosList.Add(posEntity);
                addList.Add(entity);
            }
        }

        if (addList.Any())
        {
            try
            {
                // 新增记录
                await _repository.AsInsertable(addList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                await _repository.AsSugarClient().Insertable(addPosList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);
            }
            catch (Exception)
            {
                list.ForEach(item => item.errorsInfo = "系统异常");
                list = new List<OrganizeListImportDataInput>();
            }
        }

        var errorList = new List<OrganizeListImportDataInput>();
        foreach (var item in inputList)
        {
            var it = list.Find(x => x.id.Equals(item.id));
            if (it.errorsInfo.IsNotEmptyOrNull())
            {
                item.errorsInfo = it.errorsInfo.TrimEnd(',');
                errorList.Add(item);
            }
        }
        return new object[] { addList, errorList };
    }
    private string ImportInitIdTree(OrganizeListImportDataInput dataItem, List<OrganizeListImportDataInput> list)
    {
        var parentItem = list.FirstOrDefault(x => x.id.Equals(dataItem.parentId));
        if (parentItem != null)
        {
            dataItem.parentId = parentItem.id;
            return ImportInitIdTree(parentItem, list) + "," + dataItem.id;
        }
        else
        {
            return dataItem.id;
        }
    }

    /// <summary>
    /// 字段对应 列名称 导入.
    /// </summary>
    /// <param name="isErrorsInfo">是否错误信息列.</param>
    /// <returns></returns>
    private List<ExportImportHelperModel> GetImportInfoFieldToTitle(bool isErrorsInfo = false)
    {
        //// 公司性质
        //DictionaryTypeEntity? typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "9b542177a477488994ce09fff3c93901" || x.EnCode == "EnterpriseNature") && x.DeleteMark == null).First();
        //var _enterpriseNatureList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList();

        //// 所属行业
        //typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "d59a3cf65f9847dbb08be449e3feae16" || x.EnCode == "IndustryType") && x.DeleteMark == null).First();
        //var _industryList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList();

        var res = new List<ExportImportHelperModel>();
        if (isErrorsInfo) res.Add(new ExportImportHelperModel() { ColumnKey = "errorsInfo", ColumnValue = "异常原因", FreezePane = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "parentId", ColumnValue = "上级组织" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "category", ColumnValue = "组织类型", Required = true, SelectList = new List<string>() { "集团", "公司", "子公司", "部门", "机构", "机关", "单位" } });
        res.Add(new ExportImportHelperModel() { ColumnKey = "fullName", ColumnValue = "组织名称", Required = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "enCode", ColumnValue = "组织编码" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "shortName", ColumnValue = "公司简称" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "enterpriseNature", ColumnValue = "公司性质", SelectList = _enterpriseNatureList });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "industry", ColumnValue = "所属行业", SelectList = _industryList });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "foundedTime", ColumnValue = "成立时间" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "telePhone", ColumnValue = "公司电话" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "fax", ColumnValue = "公司传真" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "webSite", ColumnValue = "公司主页" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "address", ColumnValue = "公司地址" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "managerName", ColumnValue = "公司法人" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "managerTelePhone", ColumnValue = "联系电话" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "managerMobilePhone", ColumnValue = "联系手机" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "manageEmail", ColumnValue = "联系邮箱" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "bankName", ColumnValue = "开户银行" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "bankAccount", ColumnValue = "银行账户" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "businessscope", ColumnValue = "经营范围" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "managerId", ColumnValue = "部门主管" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "sortCode", ColumnValue = "排序" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "description", ColumnValue = "说明" });

        return res;
    }

    /// <summary>
    /// 导出字段对应 列名称 公司.
    /// </summary>
    /// <returns></returns>
    private List<ExportImportHelperModel> GetComInfoFieldToTitle()
    {
        //// 公司性质
        //DictionaryTypeEntity? typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "9b542177a477488994ce09fff3c93901" || x.EnCode == "EnterpriseNature") && x.DeleteMark == null).First();
        //var _enterpriseNatureList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList();

        //// 所属行业
        //typeEntity = _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().Where(x => (x.Id == "d59a3cf65f9847dbb08be449e3feae16" || x.EnCode == "IndustryType") && x.DeleteMark == null).First();
        //var _industryList = _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == typeEntity.Id && d.DeleteMark == null && d.EnabledMark.Equals(1)).Select(x => x.FullName).ToList();

        var res = new List<ExportImportHelperModel>();
        res.Add(new ExportImportHelperModel() { ColumnKey = "parentId", ColumnValue = "上级组织" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "category", ColumnValue = "组织类型", Required = true, SelectList = new List<string>() { "集团", "公司", "子公司", "部门", "机构", "机关", "单位" } });
        res.Add(new ExportImportHelperModel() { ColumnKey = "fullName", ColumnValue = "组织名称" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "enCode", ColumnValue = "组织编码" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "shortName", ColumnValue = "公司简称" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "enterpriseNature", ColumnValue = "公司性质", SelectList = _enterpriseNatureList });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "industry", ColumnValue = "所属行业", SelectList = _industryList });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "foundedTime", ColumnValue = "成立时间" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "telePhone", ColumnValue = "公司电话" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "fax", ColumnValue = "公司传真" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "webSite", ColumnValue = "公司主页" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "address", ColumnValue = "公司地址" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "managerName", ColumnValue = "公司法人" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "managerTelePhone", ColumnValue = "联系电话" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "managerMobilePhone", ColumnValue = "联系手机" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "manageEmail", ColumnValue = "联系邮箱" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "bankName", ColumnValue = "开户银行" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "bankAccount", ColumnValue = "银行账户" });
        //res.Add(new ExportImportHelperModel() { ColumnKey = "businessscope", ColumnValue = "经营范围" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "sortCode", ColumnValue = "排序" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "description", ColumnValue = "说明" });

        return res;
    }

    /// <summary>
    /// 获取组织类型名称.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    private string GetOrgCategoryName(string category)
    {
        switch (category)
        {
            case CommonConst.OrganizeTypeBase:
                return "根组织";
            case CommonConst.OrganizeTypeCompany:
                return "公司";
            case CommonConst.OrganizeTypeDepartment:
                return "部门";
            case CommonConst.OrganizeTypeGroup:
                return "集团";
            case CommonConst.OrganizeTypeInstitution:
                return "机构";
            case CommonConst.OrganizeTypeOrgan:
                return "机关";
            case CommonConst.OrganizeTypeSubsidiary:
                return "子公司";
            case CommonConst.OrganizeTypeUnit:
                return "单位";
        }

        return null;
    }

    /// <summary>
    /// 获取组织类型.
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    private string GetOrgCategory(string fullName)
    {
        switch (fullName)
        {
            case "公司":
                return CommonConst.OrganizeTypeCompany;
            case "部门":
                return CommonConst.OrganizeTypeDepartment;
            case "集团":
                return CommonConst.OrganizeTypeGroup;
            case "机构":
                return CommonConst.OrganizeTypeInstitution;
            case "机关":
                return CommonConst.OrganizeTypeOrgan;
            case "子公司":
                return CommonConst.OrganizeTypeSubsidiary;
            case "单位":
                return CommonConst.OrganizeTypeUnit;
        }

        return null;
    }

    #endregion

}