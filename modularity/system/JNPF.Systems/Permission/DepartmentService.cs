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
using JNPF.Systems.Entitys.Dto.Department;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Data;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：部门管理.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Organize", Order = 166)]
[Route("api/permission/[controller]")]
public class DepartmentService : IDepartmentService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 部门管理仓储.
    /// </summary>
    private readonly ISqlSugarRepository<OrganizeEntity> _repository;

    /// <summary>
    /// 系统配置.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 第三方同步.
    /// </summary>
    private readonly ISynThirdInfoService _synThirdInfoService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 初始化一个<see cref="DepartmentService"/>类型的新实例.
    /// </summary>
    public DepartmentService(
        ISqlSugarRepository<OrganizeEntity> repository,
        ISysConfigService sysConfigService,
        IOrganizeService organizeService,
        ICacheManager cacheManager,
        IFileManager fileService,
        ISynThirdInfoService synThirdInfoService,
        IBillRullService billRuleService,
        IUserManager userManager)
    {
        _repository = repository;
        _cacheManager = cacheManager;
        _fileManager = fileService;
        _sysConfigService = sysConfigService;
        _organizeService = organizeService;
        _synThirdInfoService = synThirdInfoService;
        _userManager = userManager;
        _billRuleService = billRuleService;
    }

    #region GET

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="companyId">公司主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("{companyId}/Department")]
    public async Task<dynamic> GetList(string companyId, [FromQuery] KeywordInput input)
    {
        List<DepartmentListOutput>? data = new List<DepartmentListOutput>();

        // 全部部门数据
        List<DepartmentListOutput>? departmentAllList = await _repository.AsSugarClient().Queryable<OrganizeEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.ManagerId))
            .Where(a => a.ParentId == companyId && a.Category.Equals("department") && a.DeleteMark == null)
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select((a, b) => new DepartmentListOutput
            {
                id = a.Id,
                parentId = a.ParentId,
                fullName = a.FullName,
                enCode = a.EnCode,
                description = a.Description,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                manager = SqlFunc.MergeString(b.RealName, "/", b.Account),
                sortCode = a.SortCode
            }).ToListAsync();

        // 当前公司部门
        List<OrganizeEntity>? departmentList = await _repository.AsQueryable().WhereIF(!string.IsNullOrEmpty(input.keyword), d => d.FullName.Contains(input.keyword) || d.EnCode.Contains(input.keyword))
            .Where(t => t.ParentId == companyId && t.Category.Equals("department") && t.DeleteMark == null)
            .OrderBy(a => a.SortCode)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .ToListAsync();
        departmentList.ForEach(item =>
        {
            item.ParentId = "0";
            data.AddRange(departmentAllList.TreeChildNode(item.Id, t => t.id, t => t.parentId));
        });
        return new { list = data.OrderBy(x => x.sortCode).ToList() };
    }

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Department/Selector/{id}")]
    public async Task<dynamic> GetSelector(string id)
    {
        // 获取组织树
        var orgTree = _organizeService.GetOrgListTreeName();

        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(t => t.DeleteMark == null).OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
        if (!"0".Equals(id)) data.RemoveAll(it => it.Id == id);

        List<DepartmentSelectorOutput>? treeList = data.Adapt<List<DepartmentSelectorOutput>>();
        treeList.ForEach(item =>
        {
            if (item.type != null && item.type.Equals("company")) item.icon = "icon-ym icon-ym-tree-organization3";
            item.organize = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });
        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取下拉框根据权限.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Department/SelectorByAuth/{id}")]
    public async Task<dynamic> GetSelectorByAuth(string id)
    {
        // 获取组织树
        var orgTree = _organizeService.GetOrgListTreeName();

        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();

        List<DepartmentSelectorOutput>? treeList = data.Adapt<List<DepartmentSelectorOutput>>();
        treeList.ForEach(item =>
        {
            if (item.type != null && item.type.Equals("company")) item.icon = "icon-ym icon-ym-tree-organization3";
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
                    var addItem = pItem.Adapt<DepartmentSelectorOutput>();

                    if (addItem.type != null && addItem.type.Equals("company")) addItem.icon = "icon-ym icon-ym-tree-organization3";
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
        });

        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取下拉框根据权限异步.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Department/SelectAsyncList/{id}")]
    public async Task<dynamic> GetSelectAsyncList(string id, [FromQuery] PageInputBase input)
    {
        if (id.IsNotEmptyOrNull() && id.Equals("0")) id = "-1";
        if (input.keyword.IsNotEmptyOrNull()) id = null;
        if (input.keyword.IsNullOrEmpty()) input.pageSize = 99999;

        // 获取组织树
        var orgTree = _organizeService.GetOrgListTreeName();

        var dataPage = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .WhereIF(id.IsNotEmptyOrNull(), x => x.ParentId.Equals(id))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword))
            .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        var data = dataPage.list;
        List<DepartmentSelectorOutput>? treeList = data.Adapt<List<DepartmentSelectorOutput>>();

        treeList.ForEach(item =>
        {
            item.type = item.category;
            if (item.type != null && item.type.Equals("company")) item.icon = "icon-ym icon-ym-tree-organization3";
            item.organize = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });

        if (input.keyword.IsNotEmptyOrNull())
        {
            return PageResult<DepartmentSelectorOutput>.SqlSugarPageResult(new SqlSugarPagedList<DepartmentSelectorOutput>() { list = treeList, pagination = dataPage.pagination });
        }
        else
        {
            return new { list = treeList };
        }
    }

    /// <summary>
    /// 获取下拉框根据权限异步.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Department/SelectAsyncByAuth/{id}")]
    public async Task<dynamic> SelectAsyncByAuth(string id, [FromQuery] OrganizeCurrInput input)
    {
        // 获取组织树
        var orgTree = _organizeService.GetOrgListTreeName();
        if (input.keyword.IsNullOrEmpty()) input.pageSize = 99999;

        var dataList = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .WhereIF(!id.Equals("0"), x => x.OrganizeIdTree.Contains(id))
            .WhereIF(id.Equals("0") && input.currOrgId.IsNotEmptyOrNull() && input.currOrgId != "0", x => !x.OrganizeIdTree.Contains(input.currOrgId))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword))
            .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);

        List<DepartmentSelectorOutput>? treeList = dataList.list.Adapt<List<DepartmentSelectorOutput>>();

        treeList.ForEach(item =>
        {
            item.type = item.category;
            if (item.type != null && item.type.Equals("company")) item.icon = "icon-ym icon-ym-tree-organization3";
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
                    var addItem = pItem.Adapt<DepartmentSelectorOutput>();

                    if (addItem.type != null && addItem.type.Equals("company")) addItem.icon = "icon-ym icon-ym-tree-organization3";
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
            return PageResult<DepartmentSelectorOutput>.SqlSugarPageResult(new SqlSugarPagedList<DepartmentSelectorOutput>() { list = treeList, pagination = dataList.pagination });
        }
        else
        {
            var res = new List<DepartmentSelectorOutput>();
            if (id.Equals("0"))
                res = treeList.Where(x => x.parentId.Equals("-1")).ToList();
            else
                res = treeList.Where(x => x.parentId.Equals(id)).ToList();
            return new { list = res };
        }
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("Department/{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        OrganizeEntity? entity = await _repository.GetSingleAsync(d => d.Id == id);
        var res = entity.Adapt<DepartmentInfoOutput>();
        if (entity.ParentId.Equals("-1")) res.organizeIdTree = new List<string>() { res.id };
        else res.organizeIdTree = (await _repository.GetSingleAsync(p => p.Id == entity.ParentId)).OrganizeIdTree.Split(",").ToList();
        return res;
    }

    #endregion

    #region POST

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Department/SelectedList")]
    public async Task<dynamic> SelectedList([FromBody] UserSelectedInput input)
    {
        return await _organizeService.GetSelectedList(input);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("Department")]
    public async Task Create([FromBody] DepartmentCrInput input)
    {
        if (await _repository.IsAnyAsync(o => o.EnCode.Equals(input.enCode) && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2014);
        if (await _repository.IsAnyAsync(o => o.ParentId == input.parentId && o.FullName == input.fullName && o.Category == "department" && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2019);
        OrganizeEntity? entity = input.Adapt<OrganizeEntity>();
        entity.Category = "department";
        entity.Id = SnowflakeIdHelper.NextId();
        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize) : entity.EnCode;
        entity.EnabledMark = 1;
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;

        #region  处理 上级ID列表 存储

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
        idList.Add(entity.FullName);
        entity.OrganizeIdTree = string.Join(",", idList);
        entity.OrganizeNameTree = string.Join("/", nameList);

        #endregion

        OrganizeEntity? newEntity = await _repository.AsInsertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        _ = newEntity ?? throw Oops.Oh(ErrorCode.D2015);
        _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);

        #region 第三方同步

        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            List<OrganizeListOutput>? orgList = new List<OrganizeListOutput>();
            orgList.Add(entity.Adapt<OrganizeListOutput>());
            if (sysConfig.dingSynIsSynOrg && sysConfig.dingDepartment.IsNotEmptyOrNull())
                await _synThirdInfoService.SynDep(2, 1, sysConfig, orgList);
            if (sysConfig.qyhIsSynOrg && sysConfig.qyhDepartment.IsNotEmptyOrNull())
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
    [HttpDelete("Department/{id}")]
    public async Task Delete(string id)
    {
        // 该机构下有机构，则不能删
        if (await _repository.IsAnyAsync(o => o.ParentId.Equals(id) && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2005);

        // 该机构下有岗位，则不能删
        if (await _repository.AsSugarClient().Queryable<PositionEntity>().AnyAsync(p => p.OrganizeId.Equals(id) && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2006);

        // 该机构下有用户，则不能删
        if (await _repository.AsSugarClient().Queryable<UserRelationEntity>().AnyAsync(x => x.ObjectType == CommonConst.RelationObjectOrganize && x.ObjectId == id))
            throw Oops.Oh(ErrorCode.D2022);

        OrganizeEntity? entity = await _repository.GetSingleAsync(o => o.Id == id && o.DeleteMark == null);
        _ = entity ?? throw Oops.Oh(ErrorCode.D2002);

        int isOK = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        if (!(isOK > 0))
        {
            throw Oops.Oh(ErrorCode.D2017);
        }
        else
        {
            // 删除该组织和角色关联数据
            await _repository.AsSugarClient().Deleteable<RoleRelationEntity>().Where(x => x.ObjectId == id && x.ObjectType == CommonConst.RelationObjectOrganize).ExecuteCommandAsync();
        }
        _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);

        #region 第三方数据删除
        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            if (sysConfig.dingSynIsSynOrg) await _synThirdInfoService.DelSynData(2, 1, sysConfig, id);
            if (sysConfig.qyhIsSynOrg) await _synThirdInfoService.DelSynData(1, 1, sysConfig, id);
        }
        catch (Exception)
        {
        }
        #endregion
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("Department/{id}")]
    public async Task Update(string id, [FromBody] DepartmentUpInput input)
    {
        OrganizeEntity? oldEntity = await _repository.GetSingleAsync(it => it.Id == id);
        if (input.parentId.Equals(id))
            throw Oops.Oh(ErrorCode.D2001);

        // 父id不能为自己的子节点
        if (input.parentId.IsNotEmptyOrNull() && await _repository.IsAnyAsync(p => p.OrganizeIdTree.Contains(input.parentId) && p.OrganizeIdTree.Contains(id) && p.Id != id))
            throw Oops.Oh(ErrorCode.D2001);
        if (await _repository.IsAnyAsync(o => o.EnCode == input.enCode && o.Id != id && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2014);
        if (await _repository.IsAnyAsync(o => o.ParentId == input.parentId && o.FullName == input.fullName && o.Id != id && o.Category == "department" && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2019);
        OrganizeEntity? entity = input.Adapt<OrganizeEntity>();
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;
        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize) : entity.EnCode;

        #region 处理 上级ID列表 存储
        if (string.IsNullOrWhiteSpace(oldEntity.OrganizeIdTree) || entity.ParentId != oldEntity.ParentId)
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
            idList.Add(entity.FullName);
            entity.OrganizeIdTree = string.Join(",", idList);
            entity.OrganizeNameTree = string.Join("/", nameList);

            // 如果上级结构 变动 ，需要更改所有包含 该组织的id 的结构
            if (entity.OrganizeIdTree != oldEntity.OrganizeIdTree)
            {
                var orgTree = _organizeService.GetOrgListTreeName();
                List<OrganizeEntity>? oldEntityList = await _repository.AsQueryable().Where(x => x.OrganizeIdTree.Contains(oldEntity.OrganizeIdTree + ",") && x.Id != oldEntity.Id).ToListAsync();
                var allOrgList = await _repository.AsQueryable().Select(x => new { x.Id, x.FullName }).ToListAsync();
                oldEntityList.Add(oldEntity);
                oldEntityList.ForEach(item =>
                {
                    string? childList = item.OrganizeIdTree.Split(oldEntity.Id).LastOrDefault();
                    item.OrganizeIdTree = entity.OrganizeIdTree + childList;
                    var itNameList = new List<string>();
                    foreach (var it in item.OrganizeIdTree.Split(',')) itNameList.Add(allOrgList.Find(x => x.Id.Equals(it)).FullName);
                    item.OrganizeNameTree = string.Join("/", itNameList);
                });

                await _repository.AsUpdateable(oldEntityList).UpdateColumns(x => new { x.OrganizeIdTree, x.OrganizeNameTree }).ExecuteCommandAsync(); // 批量修改 父级组织
            }
        }
        #endregion

        int isOK = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOK > 0)) throw Oops.Oh(ErrorCode.D2018);
        _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);

        #region 第三方同步
        try
        {
            var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
            List<OrganizeListOutput>? orgList = new List<OrganizeListOutput>();
            entity.Category = "department";
            orgList.Add(entity.Adapt<OrganizeListOutput>());
            if (sysConfig.dingSynIsSynOrg && sysConfig.dingDepartment.IsNotEmptyOrNull())
                await _synThirdInfoService.SynDep(2, 1, sysConfig, orgList);
            if (sysConfig.qyhIsSynOrg && sysConfig.qyhDepartment.IsNotEmptyOrNull())
                await _synThirdInfoService.SynDep(1, 1, sysConfig, orgList);
        }
        catch (Exception)
        {
        }
        #endregion
    }

    /// <summary>
    /// 更新状态.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpPut("Department/{id}/Actions/State")]
    public async Task UpdateState(string id)
    {
        OrganizeEntity? entity = await _repository.GetFirstAsync(o => o.Id == id);
        _ = entity.EnabledMark == 1 ? 0 : 1;
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;

        int isOk = await _repository.AsUpdateable(entity).UpdateColumns(o => new { o.EnabledMark, o.LastModifyTime, o.LastModifyUserId }).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D2016);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取部门列表(其他服务使用).
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeEntity>> GetListAsync()
    {
        return await _repository.AsQueryable().Where(t => t.Category.Equals("department") && t.EnabledMark == 1 && t.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
    }

    /// <summary>
    /// 部门名称.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public string GetDepName(string id)
    {
        OrganizeEntity? entity = _repository.GetFirst(x => x.Id == id && x.Category == "department" && x.EnabledMark == 1 && x.DeleteMark == null);
        return entity == null ? string.Empty : entity.FullName;
    }

    /// <summary>
    /// 公司名称.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public string GetComName(string id)
    {
        string? name = string.Empty;
        OrganizeEntity? entity = _repository.GetFirst(x => x.Id == id && x.EnabledMark == 1 && x.DeleteMark == null);
        if (entity == null)
        {
            return name;
        }
        else
        {
            if (entity.Category == "company")
            {
                return entity.FullName;
            }
            else
            {
                OrganizeEntity? pEntity = _repository.GetFirst(x => x.Id == entity.ParentId && x.EnabledMark == 1 && x.DeleteMark == null);
                return GetComName(pEntity.Id);
            }
        }
    }

    /// <summary>
    /// 公司结构名称树.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public string GetOrganizeNameTree(string id)
    {
        return _organizeService.GetOrgListTreeName().FirstOrDefault(x => x.Id.Equals(id))?.Description;
    }

    /// <summary>
    /// 获取部门直属分类上级id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public string GetCompanyId(string id)
    {
        OrganizeEntity? entity = _repository.GetFirst(x => x.Id == id && x.EnabledMark == 1 && x.DeleteMark == null);
        if (entity == null || entity.Category == CommonConst.OrganizeTypeBase || entity.Category == CommonConst.OrganizeTypeInstitution || entity.Category == CommonConst.OrganizeTypeOrgan)
        {
            return string.Empty;
        }
        else
        {
            if (entity.Category == CommonConst.OrganizeTypeDepartment)
            {
                OrganizeEntity? pEntity = _repository.GetFirst(x => x.Id == entity.ParentId && x.EnabledMark == 1 && x.DeleteMark == null);
                return GetCompanyId(pEntity.Id);
            }
            else
            {
                return entity.Id;
            }
        }
    }

    /// <summary>
    /// 获取公司下所有部门.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isSubordinate">是否直属部门（不包含子公司）</param>
    /// <returns></returns>
    public List<OrganizeEntity> GetCompanyAllDep(string id, bool isSubordinate = false)
    {
        if (isSubordinate)
        {
            var output = new List<OrganizeEntity>();
            var depIdlist = _repository.GetList(x => x.ParentId == id && x.Category == "department" && x.EnabledMark == 1 && x.DeleteMark == null);
            foreach (var item in depIdlist)
            {
                var list = _repository.GetList(x => x.OrganizeIdTree.Contains(item.Id) && x.Category == "department" && x.EnabledMark == 1 && x.DeleteMark == null);
                output.AddRange(list);
            }
            return output;
        }
        else
        {
            return _repository.GetList(x => x.OrganizeIdTree.Contains(id) && x.Category == "department" && x.EnabledMark == 1 && x.DeleteMark == null);
        }
    }
    #endregion

    #region 导出和导入

    /// <summary>
    /// 模板下载.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Department/TemplateDownload")]
    public async Task<dynamic> TemplateDownload()
    {
        // 初始化 一条空数据
        List<OrganizeListImportDataInput>? dataList = new List<OrganizeListImportDataInput>() { new OrganizeListImportDataInput()
            { fullName = "组织名称/组织名称1" } };

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = "组织信息导入模板.xls";
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        var infoFields = GetInfoFieldToTitle();
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
    [HttpPost("Department/Uploader")]
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
    [HttpGet("Department/ImportPreview")]
    public async Task<dynamic> ImportPreview(string fileName)
    {
        DataTable? excelData = new DataTable();
        try
        {
            var FileEncode = GetInfoFieldToTitle();

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
        return new { dataRow = excelData };
    }

    /// <summary>
    /// 导出错误报告.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("Department/ExportExceptionData")]
    [UnitOfWork]
    public async Task<dynamic> ExportExceptionData([FromBody] OrganizeImportDataInput list)
    {
        list.list.ForEach(it => it.errorsInfo = string.Empty);
        object[]? res = await DataImport(list.list);

        // 错误数据
        List<OrganizeListImportDataInput>? errorlist = res.Last() as List<OrganizeListImportDataInput>;

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("部门信息导入模板错误报告_{0}.xls", DateTime.Now.ToString("yyyyMMddHHmmss"));
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        foreach (var item in GetInfoFieldToTitle())
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
    [HttpPost("Department/ImportData")]
    [UnitOfWork]
    public async Task<dynamic> ImportData([FromBody] OrganizeImportDataInput list)
    {
        list.list.ForEach(x => x.errorsInfo = string.Empty);
        object[]? res = await DataImport(list.list);
        List<OrganizeEntity>? addlist = res.First() as List<OrganizeEntity>;
        List<OrganizeListImportDataInput>? errorlist = res.Last() as List<OrganizeListImportDataInput>;
        return new { snum = addlist.Count, fnum = errorlist.Count, failResult = errorlist, resultType = errorlist.Count < 1 ? 0 : 1 };
    }

    /// <summary>
    /// 导入数据函数.
    /// </summary>
    /// <param name="list">list.</param>
    /// <returns>[成功列表,失败列表].</returns>
    private async Task<object[]> DataImport(List<OrganizeListImportDataInput> list)
    {
        List<OrganizeListImportDataInput> inputList = list;

        if (inputList == null || inputList.Count() < 1)
            throw Oops.Oh(ErrorCode.D5019);
        if (inputList.Count > 1000)
            throw Oops.Oh(ErrorCode.D5029);

        var addList = new List<OrganizeEntity>();

        var orgList = _organizeService.GetOrgListTreeName();

        foreach (var item in inputList)
        {
            if (item.fullName.IsNullOrWhiteSpace()) item.errorsInfo += "部门名称不能为空,";
            if (item.enCode.IsNullOrWhiteSpace()) item.errorsInfo += "部门编码不能为空,";
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
            if (inputList.Count(x => x.fullName == item.fullName) > 1)
            {
                var errorItems = inputList.Where(x => x.fullName == item.fullName).ToList();
                for (var i = 1; i < errorItems.Count; i++) errorItems[i].errorsInfo += "部门名称已存在,";
            }
            if (inputList.Count(x => x.enCode == item.enCode) > 1)
            {
                var errorItems = inputList.Where(x => x.enCode == item.enCode).ToList();
                for (var i = 1; i < errorItems.Count; i++) errorItems[i].errorsInfo += "部门编码已存在,";
            }
            OrganizeEntity entity = new OrganizeEntity();
            entity.Id = SnowflakeIdHelper.NextId();
            entity.Category = "department";
            entity.Description = item.description;
            if (item.fullName.IsNotEmptyOrNull())
            {
                var pName = item.fullName.Split('/').ToList();
                entity.FullName = pName.Last();
                pName.Remove(pName.Last());
                entity.ParentId = orgList.FirstOrDefault(x => x.Description.Equals(string.Join("/", pName)))?.Id;
                if (entity.ParentId.IsNullOrEmpty())
                {
                    item.errorsInfo += "找不到该所属组织,";
                }
                else
                {
                    if (orgList.Any(o => o.ParentId == entity.ParentId && o.FullName == entity.FullName && o.Category == "department" && o.DeleteMark == null))
                    {
                        if (!item.errorsInfo.Contains("部门名称值已存在"))
                            item.errorsInfo += "部门名称值已存在,";
                    }
                }
            }

            if (item.enCode.IsNotEmptyOrNull() && orgList.Any(o => o.EnCode == item.enCode && o.DeleteMark == null))
            {
                if (!item.errorsInfo.Contains("部门编码值已存在"))
                    item.errorsInfo += "部门编码值已存在,";
            }
            entity.EnCode = item.enCode;
            entity.SortCode = item.sortCode.IsNotEmptyOrNull() ? item.sortCode.ParseToLong() : 0;

            if (item.errorsInfo.IsNullOrEmpty()) addList.Add(entity);
        }

        if (addList.Any())
        {
            try
            {
                // 新增记录
                var newEntity = await _repository.AsInsertable(addList).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
                _cacheManager.Del(CommonConst.CACHEKEYORGANIZE);
            }
            catch (Exception)
            {
                inputList.ForEach(item => item.errorsInfo = "系统异常");
                inputList = new List<OrganizeListImportDataInput>();
            }
        }

        var errorList = new List<OrganizeListImportDataInput>();
        foreach (var item in inputList)
        {
            if (item.errorsInfo.IsNotEmptyOrNull() && item.errorsInfo.Contains(","))
            {
                item.errorsInfo = item.errorsInfo.TrimEnd(',');
                errorList.Add(item);
            }
        }
        return new object[] { addList, errorList };
    }

    /// <summary>
    /// 字段对应 列名称.
    /// </summary>
    /// <returns></returns>
    private List<ExportImportHelperModel> GetInfoFieldToTitle(List<string> fields = null)
    {
        var res = new List<ExportImportHelperModel>();
        res.Add(new ExportImportHelperModel() { ColumnKey = "errorsInfo", ColumnValue = "异常原因", FreezePane = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "fullName", ColumnValue = "部门名称", Required = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "enCode", ColumnValue = "部门编码", Required = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "managerId", ColumnValue = "部门主管" });
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

    #endregion

}