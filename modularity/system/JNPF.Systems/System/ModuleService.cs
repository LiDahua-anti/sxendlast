using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Module;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Entitys;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 菜单管理
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Menu", Order = 212)]
[Route("api/system/[controller]")]
public class ModuleService : IModuleService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ModuleEntity> _repository;

    /// <summary>
    /// 功能按钮服务.
    /// </summary>
    private readonly IModuleButtonService _moduleButtonService;

    /// <summary>
    /// 功能列表服务.
    /// </summary>
    private readonly IModuleColumnService _moduleColumnService;

    /// <summary>
    /// 功能数据资源服务.
    /// </summary>
    private readonly IModuleDataAuthorizeSchemeService _moduleDataAuthorizeSchemeService;

    /// <summary>
    /// 功能表单服务.
    /// </summary>
    private readonly IModuleFormService _moduleFormSerive;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ModuleService"/>类型的新实例.
    /// </summary>
    public ModuleService(
        ISqlSugarRepository<ModuleEntity> repository,
        IFileManager fileManager,
        ModuleButtonService moduleButtonService,
        IModuleColumnService moduleColumnService,
        IModuleFormService moduleFormSerive,
        IModuleDataAuthorizeSchemeService moduleDataAuthorizeSchemeService,
        IUserManager userManager)
    {
        _repository = repository;
        _fileManager = fileManager;
        _moduleButtonService = moduleButtonService;
        _moduleColumnService = moduleColumnService;
        _moduleFormSerive = moduleFormSerive;
        _moduleDataAuthorizeSchemeService = moduleDataAuthorizeSchemeService;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取菜单列表.
    /// </summary>
    /// <param name="systemId">模块ID.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("ModuleBySystem")]
    public async Task<dynamic> GetList([FromQuery] ModuleListQuery input)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var data = await GetList();
        if (!_userManager.IsAdministrator)
        {
            var authorIds = await _userManager.GetUserStandingAuthIdList(CommonConst.AuthorizeItemModule);
            data = data.FindAll(x => authorIds.Contains(x.Id));
        }

        if (!string.IsNullOrEmpty(input.category))
            data = data.FindAll(x => x.Category == input.category);
        if (ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any())
            data = data.FindAll(x => !ignoreIds.Contains(x.Id));
        if (ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any())
            data = data.FindAll(x => !ignoreUrls.Contains(x.UrlAddress));
        if (!string.IsNullOrEmpty(input.keyword))
            data = data.TreeWhere(t => t.FullName.Contains(input.keyword) || t.EnCode.Contains(input.keyword) || (t.UrlAddress.IsNotEmptyOrNull() && t.UrlAddress.Contains(input.keyword)), t => t.Id, t => t.ParentId);
        if (input.type.IsNotEmptyOrNull())
            data = data.TreeWhere(x => x.Type.Equals(input.type), t => t.Id, t => t.ParentId);
        if (input.enabledMark.IsNotEmptyOrNull())
            data = data.TreeWhere(x => x.EnabledMark.Equals(input.enabledMark), t => t.Id, t => t.ParentId);

        var treeList = data.OrderBy(o => o.SortCode).ThenByDescending(o => o.CreatorTime).Adapt<List<ModuleListOutput>>();
        foreach (var item in treeList)
        {
            if (item.isButtonAuthorize.Equals(1) || item.isColumnAuthorize.Equals(1) || item.isDataAuthorize.Equals(1) || item.isFormAuthorize.Equals(1))
                item.hasPermission = 1;
            if (item.type.Equals(3))
                item.webType = await _repository.AsSugarClient().Queryable<VisualDevReleaseEntity>().Where(x => x.DeleteMark == null && x.Id.Equals(item.moduleId)).Select(x => x.WebType).FirstAsync();
        }

        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取菜单下拉框.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="category">菜单分类（参数有Web,App），默认显示所有分类.</param>
    /// <returns></returns>
    [HttpGet("Selector/{id}")]
    public async Task<dynamic> GetSelector(string id, string category)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var data = await GetList();
        if (!_userManager.IsAdministrator)
        {
            var authorIds = await _userManager.GetUserAuthorizeItemIdList(CommonConst.AuthorizeItemModule);
            data = data.FindAll(x => authorIds.Contains(x.Id));
        }

        if (!string.IsNullOrEmpty(category))
            data = data.FindAll(x => x.Category == category && (x.Type == 1 || x.Type == 0));
        if (!id.Equals("0"))
            data.RemoveAll(x => x.Id == id);
        if (ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any())
            data = data.FindAll(x => !ignoreIds.Contains(x.Id));
        if (ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any())
            data = data.FindAll(x => !ignoreUrls.Contains(x.UrlAddress));
        var treeList = data.Where(x => x.EnabledMark.Equals(1) && !x.EnCode.Equals(CommonConst.ModuleCodeAppConfig) && !x.EnCode.Equals(CommonConst.ModuleCodeOnlineDev)).ToList().Adapt<List<ModuleSelectorOutput>>();
        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取菜单下拉框.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="systemId">模块ID.</param>
    /// <param name="category">菜单分类（参数有Web,App），默认显示所有分类.</param>
    /// <returns></returns>
    [HttpGet("Selector/{id}/{systemId}")]
    public async Task<dynamic> GetSelector(string id, string systemId, string category)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var data = await GetList();
        if (!string.IsNullOrEmpty(category))
            data = data.FindAll(x => x.Category == category && (x.Type == 1 || x.Type == 0));
        if (!id.Equals("0"))
            data.RemoveAll(x => x.Id == id);
        if (ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any())
            data = data.FindAll(x => !ignoreIds.Contains(x.Id));
        if (ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any())
            data = data.FindAll(x => !ignoreUrls.Contains(x.UrlAddress));
        var treeList = data.Adapt<List<ModuleSelectorOutput>>();
        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取菜单列表（下拉框）.
    /// </summary>
    /// <param name="category">菜单分类（参数有Web,App）.</param>
    /// <returns></returns>
    [HttpGet("Selector/All")]
    public async Task<dynamic> GetSelectorAll(string category)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var data = await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null && x.SystemId.Equals(_userManager.SystemId) && !x.EnCode.Equals("workFlow"))
            .WhereIF(category.IsNotEmptyOrNull(), x => x.Category == category)
            .WhereIF(ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any(), x => !ignoreIds.Contains(x.Id))
            .WhereIF(ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any(), x => !ignoreUrls.Contains(x.UrlAddress))
            .OrderBy(o => o.SortCode).OrderByDescending(o => o.CreatorTime)
            .ToListAsync();

        var system = await _repository.AsSugarClient().Queryable<SystemEntity>()
            .Where(x => x.DeleteMark == null && x.Id.Equals(_userManager.SystemId))
            .Select(x => new ModuleSelectorAllOutput
            {
                id = x.Id,
                parentId = "0",
                fullName = x.FullName,
                icon = x.Icon,
                type = "0",
                hasModule = false
            })
            .FirstAsync();

        var treeList = data.Adapt<List<ModuleSelectorAllOutput>>();
        foreach (var item in treeList)
        {
            if (item.type == "1") item.hasModule = false;
            if (item.parentId == "-1") item.parentId = item.systemId;
        }

        treeList.Add(system);
        return new { list = treeList.ToTree() };
    }

    /// <summary>
    /// 获取菜单的表单列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Selector/Form")]
    public async Task<dynamic> GetSelectorForm([FromQuery] ModuleSelectorFormInput input)
    {
        var systemList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.EnCode != CommonConst.SystemCodeMain).ToListAsync();

        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.Category == "Web")
            .Where(it => systemList.Select(x => x.Id).Contains(it.SystemId) && (it.Type == 3 || it.Type == 9))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), it => it.FullName.Contains(input.keyword) || it.EnCode.Contains(input.keyword))
            .WhereIF(input.systemId.IsNotEmptyOrNull(), it => it.SystemId == input.systemId)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new ModuleSelectorFormOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                category = it.Category,
                systemId = it.SystemId,
                type = it.Type,
                typeName = SqlFunc.IIF(it.Type == 3, "表单", "流程"),
                propertyJson = it.PropertyJson
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        foreach (var item in list.list)
        {
            item.systemName = systemList.Find(x => x.Id == item.systemId)?.FullName;

            var dic = item.propertyJson.ToObject<Dictionary<string, object>>();
            if (dic.ContainsKey("moduleId") && dic["moduleId"].IsNotEmptyOrNull())
            {
                var moduleId = dic["moduleId"].ToString();
                if (item.type == 9)
                {
                    var flowId = await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(x => x.TemplateId == moduleId && x.Status == 1 && x.DeleteMark == null).Select(x => x.Id).FirstAsync();
                    var formId = await _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == flowId && x.NodeType == "start" && x.DeleteMark == null).Select(x => x.FormId).FirstAsync();

                    item.flowId = moduleId;
                    item.formId = formId;
                }
                else
                {
                    item.formId = moduleId;
                }
            }
        }

        return PageResult<ModuleSelectorFormOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取指定类型的菜单列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Selector/Page")]
    public async Task<dynamic> GetSelectorPage([FromQuery] ModuleSelectorPageInput input)
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.SystemId == _userManager.SystemId)
            .WhereIF(input.category.IsNotEmptyOrNull(), it => it.Category.Equals(input.category))
            .WhereIF(input.type.IsNotEmptyOrNull(), it => it.Type.Equals(input.type))
            .WhereIF(input.type.IsNotEmptyOrNull() && input.type.Equals(3), it => it.PropertyJson.Contains("\"webType\":2"))
            .OrderBy(it => it.SortCode)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new ModuleSelectorPageOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                type = it.Type,
                propertyJson = it.PropertyJson
            })
            .ToListAsync();

        if (!_userManager.IsAdministrator)
        {
            var authorIds = await _userManager.GetUserStandingAuthIdList(CommonConst.AuthorizeItemModule);
            list = list.FindAll(x => authorIds.Contains(x.id));
        }

        return new { list = list };
    }

    /// <summary>
    /// 获取菜单发布下拉框（功能设计、门户设计）.
    /// </summary>
    /// <returns></returns>
    [HttpGet("SelectorFilter/{moduleId}")]
    public async Task<dynamic> GetSelectorFilter(string moduleId, string category)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var system = await _repository.AsSugarClient().Queryable<SystemEntity>()
            .Where(x => x.DeleteMark == null && x.Id == _userManager.SystemId)
            .Select(x => new ModuleEntity
            {
                Id = x.Id,
                ParentId = "-1",
                Type = 0,
                FullName = x.FullName,
                Icon = x.Icon,
                EnCode = x.EnCode,
                SystemId = x.Id
            }).FirstAsync();
        var data = await GetList();
        data = data.FindAll(x => x.EnabledMark.Equals(1) && x.Category == category && (x.Type == 1 || x.Type == 0));
        foreach (var item in data) if (item.ParentId.Equals("-1")) item.ParentId = system.Id;

        if (!_userManager.IsAdministrator)
        {
            var authorIds = await _userManager.GetUserAuthorizeItemIdList(CommonConst.AuthorizeItemModule);
            data = data.FindAll(x => authorIds.Contains(x.Id));
        }

        if (ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any())
            data = data.FindAll(x => !ignoreIds.Contains(x.Id));
        if (ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any())
            data = data.FindAll(x => !ignoreUrls.Contains(x.UrlAddress));

        data.Add(system);
        var treeList = data.Adapt<List<ModuleSelectorFilterOutput>>();

        if (category == "App")
        {
            foreach (var item in treeList.FindAll(it => it.parentId.Equals("-1")))
                item.disabled = true;
        }

        // 已发布的菜单
        var moduleList = await _repository.AsQueryable().Where(it => it.DeleteMark == null && it.Category.Equals(category) && it.PropertyJson.Contains(moduleId)).ToListAsync();
        foreach (var item in moduleList)
        {
            if (item.ParentId.Equals("-1"))
            {
                var sys = treeList.Find(it => it.id.Equals(item.SystemId));
                if (sys.IsNotEmptyOrNull()) sys.disabled = true;
            }
            else
            {
                var mod = treeList.Find(it => it.id.Equals(item.ParentId));
                if (mod.IsNotEmptyOrNull()) mod.disabled = true;
            }
        }

        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取菜单信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var data = await GetInfo(id);
        return data.Adapt<ModuleInfoOutput>();
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var data = (await GetInfo(id)).Adapt<ModuleExportInput>();
        data.buttonEntityList = (await _moduleButtonService.GetList(id)).Adapt<List<ButtonEntityListItem>>();
        data.columnEntityList = (await _moduleColumnService.GetList(id)).Adapt<List<ColumnEntityListItem>>();
        data.schemeEntityList = (await _moduleDataAuthorizeSchemeService.GetList(id)).Adapt<List<SchemeEntityListItem>>();
        data.formEntityList = (await _moduleFormSerive.GetList(id)).Adapt<List<FromEntityListItem>>();
        var jsonStr = data.ToJsonString();
        return await _fileManager.Export(jsonStr, data.fullName, ExportFileType.bm);
    }

    /// <summary>
    /// 获取菜单权限返回权限组.
    /// </summary>
    /// <param name="id">主键id</param>
    /// <returns></returns>
    [HttpGet("getPermissionGroup/{id}")]
    public async Task<dynamic> GetPermissionGroup(string id)
    {
        // 获取当前菜单开启了哪些权限
        var entity = await GetInfo(id);
        if (entity.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1005);

        // 权限：0-未开启、1-有权限、2-无权限
        var type = 0;
        var buttonAuthList = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .ToListAsync();
        var columnAuthList = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .ToListAsync();
        var fromAuthList = await _repository.AsSugarClient().Queryable<ModuleFormEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .ToListAsync();
        var dataAuthList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .ToListAsync();

        var list = new List<ModulePermissionGroupListOutput>();
        if (buttonAuthList.Count > 0 || columnAuthList.Count > 0 || fromAuthList.Count > 0 || dataAuthList.Count > 0)
        {
            var allAuthList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                .Where(it => it.ItemType.Equals(CommonConst.AuthorizeItemModule) && it.ItemId.Equals(id))
                .Select(it => it.ObjectId)
                .ToListAsync();

            list = await _repository.AsSugarClient().Queryable<StandingEntity>().Where(it => it.DeleteMark == null)
                .Where(it => SqlFunc.Subqueryable<AuthorizeEntity>().EnableTableFilter().Where(xx => allAuthList.Contains(xx.ObjectId) && xx.ItemType.Equals(CommonConst.AuthorizeItemStanding)).Select(xx => xx.ItemId).Contains(it.Id))
                .Select(it => new ModulePermissionGroupListOutput
                {
                    id = it.Id,
                    fullName = it.FullName,
                    enCode = it.EnCode,
                    icon = "icon-ym icon-ym-authGroup"
                }).ToListAsync();

            if (list.Count > 0)
                type = 1;
            else
                type = 2;
        }

        return new { list = list, type = type };
    }

    /// <summary>
    /// 获取菜单权限返回身份.
    /// </summary>
    /// <param name="id">主键id</param>
    /// <param name="permissionId">权限组id</param>
    /// <returns></returns>
    [HttpGet("getPermission/{id}/{permissionId}")]
    public async Task<dynamic> GetPermission(string id, string permissionId)
    {
        var modulePermission = new modulePermissionOutput();

        // 获取当前菜单开启了哪些权限
        var entity = await GetInfo(id);
        if (entity.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1005);

        var permissionGroup = await _repository.AsSugarClient().Queryable<StandingEntity>()
            .Where(it => SqlFunc.Subqueryable<AuthorizeEntity>().EnableTableFilter().Where(xx => permissionId.Equals(xx.ObjectId) && xx.ItemType.Equals(CommonConst.AuthorizeItemStanding)).Select(xx => xx.ItemId).Contains(it.Id))
            .Where(it => it.DeleteMark == null && it.Id.Equals(permissionId))
            .ToListAsync();
        if (!permissionGroup.Any())
            throw Oops.Oh(ErrorCode.COM1005);

        // 权限组的权限
        var allAuthList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(it => it.ObjectId.Equals(permissionId))
            .ToListAsync();

        var permissionMemberModel = new ModulePermissionModel();
        permissionMemberModel.fullName = "权限成员";
        permissionMemberModel.type = 1;
        if (!permissionGroup.Any())
        {
            var list = permissionGroup.Adapt<List<ModulePermissionBaseModel>>();
            permissionMemberModel.list = list;
        }
        modulePermission.permissionMember = permissionMemberModel;

        var moduleButtonPermissionModel = new ModulePermissionModel();
        moduleButtonPermissionModel.fullName = "按钮权限";
        var buttonAuthList = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .Select(it => new ModulePermissionBaseModel
            {
                id = it.Id,
                fullName = it.FullName
            })
            .ToListAsync();
        if (buttonAuthList.Count > 0)
        {
            moduleButtonPermissionModel.type = 2;
            moduleButtonPermissionModel.list = buttonAuthList.Where(it => allAuthList.Select(x => x.ItemId).Contains(it.id)).ToList();
            if (moduleButtonPermissionModel.list.Count > 0)
                moduleButtonPermissionModel.type = 1;
        }
        modulePermission.buttonAuthorize = moduleButtonPermissionModel;

        var moduleColumnPermissionModel = new ModulePermissionModel();
        moduleColumnPermissionModel.fullName = "列表权限";
        var columnAuthList = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .Select(it => new ModulePermissionBaseModel
            {
                id = it.Id,
                fullName = it.FullName
            })
            .ToListAsync();
        if (columnAuthList.Count > 0)
        {
            moduleColumnPermissionModel.type = 2;
            moduleColumnPermissionModel.list = columnAuthList.Where(it => allAuthList.Select(x => x.ItemId).Contains(it.id)).ToList();
            if (moduleColumnPermissionModel.list.Count > 0)
                moduleColumnPermissionModel.type = 1;
        }
        modulePermission.columnAuthorize = moduleColumnPermissionModel;

        var moduleFromPermissionModel = new ModulePermissionModel();
        moduleFromPermissionModel.fullName = "表单权限";
        var fromAuthList = await _repository.AsSugarClient().Queryable<ModuleFormEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .Select(it => new ModulePermissionBaseModel
            {
                id = it.Id,
                fullName = it.FullName
            })
            .ToListAsync();
        if (fromAuthList.Count > 0)
        {
            moduleFromPermissionModel.type = 2;
            moduleFromPermissionModel.list = fromAuthList.Where(it => allAuthList.Select(x => x.ItemId).Contains(it.id)).ToList();
            if (moduleFromPermissionModel.list.Count > 0)
                moduleFromPermissionModel.type = 1;
        }
        modulePermission.formAuthorize = moduleFromPermissionModel;

        var moduleDataPermissionModel = new ModulePermissionModel();
        moduleDataPermissionModel.fullName = "数据权限";
        var dataAuthList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .Select(it => new ModulePermissionBaseModel
            {
                id = it.Id,
                fullName = it.FullName
            })
            .ToListAsync();
        if (dataAuthList.Count > 0)
        {
            moduleDataPermissionModel.type = 2;
            moduleDataPermissionModel.list = dataAuthList.Where(it => allAuthList.Select(x => x.ItemId).Contains(it.id)).ToList();
            if (moduleDataPermissionModel.list.Count > 0)
                moduleDataPermissionModel.type = 1;
        }
        modulePermission.dataAuthorize = moduleDataPermissionModel;

        return modulePermission;
    }

    /// <summary>
    /// 获取已发布菜单名称.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("GetReportMenu")]
    public async Task<dynamic> GetReportMenu(string id)
    {
        var output = new ModuleReportMenuOutput();
        var systemList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.DeleteMark == null).ToListAsync();
        var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null).ToListAsync();
        var pcModuleList = moduleList.Where(it => it.Category.Equals("Web")).ToList();
        var appModuleList = moduleList.Where(it => it.Category.Equals("App")).ToList();

        var pcList = new List<string>();
        foreach (var module in pcModuleList.Where(it => it.PropertyJson.Contains(id)))
        {
            GetReleaseName(pcList, pcModuleList, systemList, module, string.Empty);
        }
        output.pcNames = string.Join("；", pcList);

        var appList = new List<string>();
        foreach (var module in appModuleList.Where(it => it.PropertyJson.Contains(id)))
        {
            GetReleaseName(appList, appModuleList, systemList, module, string.Empty);
        }
        output.appNames = string.Join("；", appList);

        return output;
    }

    /// <summary>
    /// 获取表单菜单列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetSystemMenu")]
    public async Task<dynamic> GetSystemMenu()
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var list = (await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null && x.SystemId.Equals(_userManager.SystemId) && x.Category.Equals("Web") && x.Type.Equals(3) && (x.PropertyJson.Contains("\"webType\":2") || x.PropertyJson.Contains("\"webType\":4")))
            .WhereIF(ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any(), x => !ignoreIds.Contains(x.Id))
            .WhereIF(ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any(), x => !ignoreUrls.Contains(x.UrlAddress))
            .OrderBy(o => o.SortCode).OrderByDescending(o => o.CreatorTime).ToListAsync()).Adapt<List<ModuleSelectorPageOutput>>();

        if (!_userManager.IsAdministrator)
        {
            var authIds = await _userManager.GetUserStandingAuthIdList(CommonConst.AuthorizeItemModule);
            list = list.FindAll(x => authIds.Contains(x.id));
        }

        return list;
    }

    #endregion

    #region Post

    /// <summary>
    /// 添加菜单.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    public async Task Creater([FromBody] ModuleCrInput input)
    {
        var systemId = _userManager.SystemId;
        if (input.type.Equals(1) || input.type.Equals(6)) input.urlAddress = string.Empty;
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null && x.Category == input.category))
            throw Oops.Oh(ErrorCode.COM1025);
        if (await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null && x.Category == input.category && input.parentId == x.ParentId && x.SystemId == systemId))
            throw Oops.Oh(ErrorCode.COM1024);
        if (input.urlAddress.IsNotEmptyOrNull() && await _repository.IsAnyAsync(x => x.UrlAddress == input.urlAddress && x.DeleteMark == null && x.SystemId == systemId))
            throw Oops.Oh(ErrorCode.D4027);
        var entity = input.Adapt<ModuleEntity>();
        entity.SystemId = systemId;

        // 添加字典菜单按钮
        if (entity.Type == 4)
        {
            foreach (var item in await _moduleButtonService.GetList())
            {
                if (item.ModuleId == "-1")
                {
                    item.ModuleId = entity.Id;
                    await _moduleButtonService.Create(item);
                }
            }
        }

        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改菜单.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ModuleUpInput input)
    {
        var systemId = _userManager.SystemId;
        var info = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (input.type.Equals(1) || input.type.Equals(6)) input.urlAddress = string.Empty;
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null && x.Category == input.category))
            throw Oops.Oh(ErrorCode.COM1004);
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.FullName == input.fullName) && x.DeleteMark == null && x.Category == input.category && input.parentId == x.ParentId && x.SystemId == systemId))
            throw Oops.Oh(ErrorCode.COM1004);
        if (input.urlAddress.IsNotEmptyOrNull() && await _repository.IsAnyAsync(x => x.Id != id && x.UrlAddress == input.urlAddress && x.DeleteMark == null && x.SystemId == systemId))
            throw Oops.Oh(ErrorCode.D4027);
        if (await _repository.IsAnyAsync(x => x.ParentId == id && x.DeleteMark == null && x.SystemId == systemId) && info.Type != input.type)
            throw Oops.Oh(ErrorCode.D4008);
        var entity = input.Adapt<ModuleEntity>();
        entity.SystemId = systemId;

        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除菜单.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [UnitOfWork]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null || await _repository.IsAnyAsync(x => x.ParentId == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D1039);

        try
        {
            // 删除app菜单的常用功能
            //if (entity.Category.Equals("App"))
            //{
            //    await _repository.AsSugarClient().Updateable<AppDataEntity>().SetColumns(it => new AppDataEntity()
            //    {
            //        DeleteMark = 1,
            //        DeleteUserId = _userManager.UserId,
            //        DeleteTime = SqlFunc.GetDate()
            //    }).Where(it => it.ObjectId == id && it.CreatorUserId == _userManager.UserId).ExecuteCommandAsync();
            //}

            await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();

            // 删除菜单相关权限
            await _repository.AsSugarClient().Updateable<ModuleButtonEntity>().SetColumns(it => new ModuleButtonEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
            await _repository.AsSugarClient().Updateable<ModuleColumnEntity>().SetColumns(it => new ModuleColumnEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
            await _repository.AsSugarClient().Updateable<ModuleFormEntity>().SetColumns(it => new ModuleFormEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
            await _repository.AsSugarClient().Updateable<ModuleDataAuthorizeSchemeEntity>().SetColumns(it => new ModuleDataAuthorizeSchemeEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.COM1002);
        }
    }

    /// <summary>
    /// 更新菜单状态.
    /// </summary>
    /// <param name="id">菜单id.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task ActionsState(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new ModuleEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="systemId"></param>
    /// <param name="file"></param>
    /// <param name="parentId"></param>
    /// <param name="category"></param>
    /// <param name="type">识别重复（0：跳过，1：追加）.</param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    [UnitOfWork]
    public async Task ActionsImport(IFormFile file, string parentId, string category, int type)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.bm.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        ModuleExportInput? moduleModel;
        try
        {
            moduleModel = josn.ToObject<ModuleExportInput>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }

        if (moduleModel == null || moduleModel.id == null) throw Oops.Oh(ErrorCode.D3006);

        var deffSystem = false;
        if (moduleModel.systemId.IsNullOrEmpty() || !moduleModel.systemId.Equals(_userManager.SystemId))
        {
            moduleModel.id = SnowflakeIdHelper.NextId();
            deffSystem = true;
        }

        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(moduleModel.id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Category.Equals(category) && it.EnCode.Equals(moduleModel.enCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Category.Equals(category) && it.SystemId.Equals(_userManager.SystemId) && it.FullName.Equals(moduleModel.fullName) && it.ParentId.Equals(parentId))) errorList.Add("名称");
        if (moduleModel.urlAddress.IsNotEmptyOrNull() && await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.SystemId.Equals(_userManager.SystemId) && it.UrlAddress.Equals(moduleModel.urlAddress))) errorList.Add("路由地址");
        moduleModel.moduleId = null;
        moduleModel.parentId = parentId;
        moduleModel.systemId = _userManager.SystemId;

        if (errorList.Any())
        {
            if (type.Equals(0))
            {
                var error = string.Join("、", errorList);
                errorMsgList.Add(string.Format("{0}重复", error));
            }
            else
            {
                var random = new Random().NextLetterAndNumberString(5);
                moduleModel.id = SnowflakeIdHelper.NextId();
                moduleModel.fullName = string.Format("{0}.副本{1}", moduleModel.fullName, random);

                var oldCode = moduleModel.enCode;
                moduleModel.enCode += random;
                if (moduleModel.type == 3)
                    moduleModel.urlAddress = moduleModel.urlAddress.Replace(oldCode, moduleModel.enCode);
            }
        }
        await ImportData(moduleModel, type, errorMsgList, deffSystem);

        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join("；", errorMsgList));
    }

    /// <summary>
    /// 获取已发布菜单名称.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("SaveReportMenu")]
    public async Task SaveReportMenu([FromBody] ModuleSaveReportMenuInput input)
    {
        var sysIdList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.DeleteMark == null).Select(it => it.Id).ToListAsync();
        var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null).ToListAsync();
        if (input.pc == 1)
        {
            if (!input.pcModuleParentId.Any() && !await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && sysIdList.Contains(it.SystemId) && it.Category.Equals("Web") && it.PropertyJson.Contains(input.id)))
                throw Oops.Oh(ErrorCode.D4017);

            // 新发布的菜单
            var webDic = new List<Dictionary<string, string>>();
            foreach (var item in input.pcModuleParentId)
            {
                if (sysIdList.Contains(item))
                {
                    webDic.Add(new Dictionary<string, string> { { item, "-1" } });
                }
                else
                {
                    var pcMod = moduleList.Find(it => it.Id.Equals(item));
                    if (pcMod.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D4021);
                    webDic.Add(new Dictionary<string, string> { { pcMod.SystemId, pcMod.Id } });
                }
            }

            foreach (var item in webDic)
            {
                var data = item.First();
                var fullName = input.fullName;
                var enCode = input.enCode + new Random().NextLetterAndNumberString(5);

                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.FullName == fullName && x.ParentId == data.Value && x.SystemId == data.Key && x.Category == "Web" && x.DeleteMark == null))
                    throw Oops.Oh(ErrorCode.COM1032);
                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.EnCode == enCode && x.Category == "Web" && x.DeleteMark == null))
                    throw Oops.Oh(ErrorCode.COM1031);

                var pcModule = new ModuleEntity()
                {
                    FullName = fullName,
                    EnCode = enCode,
                    UrlAddress = string.Format("report/{0}", enCode),
                    ParentId = data.Value,
                    SystemId = data.Key,
                    ModuleId = input.id,
                    PropertyJson = new { moduleId = input.id, iconBackgroundColor = string.Empty, isTree = 0 }.ToJsonString(),
                    Icon = "icon-ym icon-ym-webForm",
                    Category = "Web",
                    IsButtonAuthorize = 0,
                    IsColumnAuthorize = 0,
                    IsDataAuthorize = 0,
                    IsFormAuthorize = 0,
                    SortCode = 999,
                    Type = input.type
                };

                await _repository.AsSugarClient().Insertable(pcModule).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }

        if (input.app == 1)
        {
            if (!input.appModuleParentId.Any() && !await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && sysIdList.Contains(it.SystemId) && it.Category.Equals("App") && it.PropertyJson.Contains(input.id)))
                throw Oops.Oh(ErrorCode.D4017);

            // 新发布的菜单
            var appDic = new List<Dictionary<string, string>>();
            foreach (var item in input.appModuleParentId)
            {
                if (sysIdList.Contains(item))
                {
                    appDic.Add(new Dictionary<string, string> { { item, "-1" } });
                }
                else
                {
                    var appMod = moduleList.Find(it => it.Id.Equals(item));
                    if (appMod.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D4021);
                    appDic.Add(new Dictionary<string, string> { { appMod.SystemId, appMod.Id } });
                }
            }

            foreach (var item in appDic)
            {
                var data = item.First();
                var fullName = input.fullName;
                var enCode = input.enCode + new Random().NextLetterAndNumberString(5);

                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.FullName == fullName && x.ParentId == data.Value && x.SystemId == data.Key && x.Category == "App" && x.DeleteMark == null))
                    throw Oops.Oh(ErrorCode.COM1032);
                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.EnCode == enCode && x.Category == "App" && x.DeleteMark == null))
                    throw Oops.Oh(ErrorCode.COM1031);

                var appModule = new ModuleEntity()
                {
                    FullName = fullName,
                    EnCode = enCode,
                    UrlAddress = enCode,
                    ParentId = data.Value,
                    SystemId = data.Key,
                    ModuleId = input.id,
                    PropertyJson = new { moduleId = input.id, iconBackgroundColor = string.Empty, isTree = 0 }.ToJsonString(),
                    Icon = "icon-ym icon-ym-webForm",
                    Category = "App",
                    IsButtonAuthorize = 0,
                    IsColumnAuthorize = 0,
                    IsDataAuthorize = 0,
                    IsFormAuthorize = 0,
                    SortCode = 999,
                    Type = input.type
                };

                await _repository.AsSugarClient().Insertable(appModule).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<ModuleEntity> GetInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
    }

    /// <summary>
    /// 获取用户树形模块功能列表.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="systemId">SystemId.</param>
    /// <param name="isBackend">isBackend.</param>
    /// <param name="mIds">指定过滤Ids.</param>
    [NonAction]
    public async Task<List<ModuleNodeOutput>> GetUserModuleListByIds(string type, string systemId = "", int isBackend = 0, List<string> mIds = null, List<string> mUrls = null)
    {
        var result = await GetUserModuleList(type, systemId, isBackend);
        if (mIds == null) mIds = new List<string>();
        if (mUrls == null) mUrls = new List<string>();

        return result.Where(x => !mIds.Contains(x.id) && !mUrls.Contains(x.urlAddress)).ToList();
    }

    /// <summary>
    /// 获取用户树形模块功能列表.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="systemId">SystemId.</param>
    /// <param name="isBackend">isBackend.</param>
    /// <param name="keyword"></param>
    [NonAction]
    public async Task<List<ModuleNodeOutput>> GetUserModuleList(string type, string systemId = "", int isBackend = 0, string keyword = "")
    {
        var output = new List<ModuleNodeOutput>();
        if (systemId.IsNullOrEmpty()) return output;
        if (isBackend.Equals(1) && _userManager.HasBackend.Value)
        {
            var menus = await _repository.AsQueryable().OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).OrderBy(q => q.CreatorTime, OrderByType.Desc).ToListAsync();
            menus.RemoveAll(x => x.ParentId.Equals("-1") && (x.EnCode != CommonConst.ModuleCodeAppConfig && x.EnCode != CommonConst.ModuleCodeOnlineDev));
            output = menus.Adapt<List<ModuleNodeOutput>>();
        }
        else
        {
            if (!_userManager.IsAdministrator)
            {
                var pIds = _userManager.PermissionIds;
                if (pIds.Any() && systemId.IsNotEmptyOrNull())
                {
                    var stAuthIds = new List<string>();
                    if (_userManager.Standing.IsNotEmptyOrNull())
                    {
                        stAuthIds = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(x => pIds.Contains(x.ObjectId) && x.ItemType.Equals(CommonConst.AuthorizeItemStanding) && x.ItemId.Equals(_userManager.Standing)).Select(x => x.ObjectId).ToListAsync();
                        var relationList = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => stAuthIds.Contains(x.ObjectId) && !x.ObjectType.Equals(CommonConst.RelationObjectOrganize)).Select(x => x.RoleId).ToListAsync();
                        stAuthIds.AddRange(relationList);
                    }
                    var mIdList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                        .Where(a => a.ItemType == CommonConst.AuthorizeItemModule)
                        .WhereIF(_userManager.Standing.IsNotEmptyOrNull(), a => stAuthIds.Contains(a.ObjectId))
                        .WhereIF(_userManager.Standing.IsNullOrEmpty(), a => pIds.Contains(a.ObjectId))
                        .Select(a => a.ItemId).ToListAsync();

                    // 当前系统的有权限的菜单
                    var menus = await _repository.AsQueryable()
                        .Where(a => (a.SystemId.Equals(systemId) && mIdList.Contains(a.Id) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null))
                        .WhereIF(_userManager.IsMainSystem, a => a.EnCode != CommonConst.ModuleCodeAppConfig && a.EnCode != CommonConst.ModuleCodeOnlineDev && !a.EnCode.Equals(CommonConst.SystemCodeWorkFlow))
                        .WhereIF(!string.IsNullOrEmpty(keyword), x => x.FullName.Contains(keyword) || x.ParentId == "-1")
                        .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).OrderBy(q => q.CreatorTime, OrderByType.Desc).ToListAsync();
                    //if (!_userManager.IsMainSystem)
                    //{
                    //    var commonFlowList = await _repository.AsQueryable().Where(x => (x.EnCode.Equals(CommonConst.SystemCodeTeamwork) && mIdList.Contains(x.Id))
                    //    || SqlFunc.Subqueryable<ModuleEntity>().EnableTableFilter().Where(xx => (xx.EnCode.Equals(CommonConst.SystemCodeTeamwork))).Select(xx => xx.Id).Contains(x.ParentId))
                    //        .ToListAsync();
                    //    menus.AddRange(commonFlowList);
                    //}
                    output = menus.Adapt<List<ModuleNodeOutput>>();
                }
            }
            else
            {
                var menus = await _repository.AsQueryable()
                    .Where(a => (a.SystemId.Equals(systemId) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null))
                    .WhereIF(_userManager.IsMainSystem, a => a.EnCode != CommonConst.ModuleCodeAppConfig && a.EnCode != CommonConst.ModuleCodeOnlineDev && !a.EnCode.Equals(CommonConst.SystemCodeWorkFlow))
                    .WhereIF(!string.IsNullOrEmpty(keyword), x => x.FullName.Contains(keyword) || x.ParentId == "-1")
                    .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).OrderBy(q => q.CreatorTime, OrderByType.Desc).ToListAsync();
                //if (!_userManager.IsMainSystem)
                //{
                //    var commonFlowList = await _repository.AsQueryable().Where(x => (x.EnCode.Equals(CommonConst.SystemCodeTeamwork))
                //    || SqlFunc.Subqueryable<ModuleEntity>().EnableTableFilter().Where(xx => (xx.EnCode.Equals(CommonConst.SystemCodeTeamwork))).Select(xx => xx.Id).Contains(x.ParentId))
                //        .ToListAsync();
                //    menus.AddRange(commonFlowList);
                //}
                output = menus.Adapt<List<ModuleNodeOutput>>();
            }
        }


        return output;
    }

    /// <summary>
    /// 获取用户所有权限菜单.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="keyword"></param>
    /// <param name="mIds">指定过滤Ids.</param>
    /// <param name="mUrls">指定过滤Urls.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleEntity>> GetUserAllModuleList(string type, string keyword = "", List<string>? mIds = null, List<string>? mUrls = null)
    {
        var output = new List<ModuleEntity>();
        if (_userManager.SystemId.IsNullOrEmpty()) return output;
        if (!_userManager.IsAdministrator)
        {
            var pIds = _userManager.PermissionIds;
            if (pIds.Any())
            {
                var stAuthIds = new List<string>();
                if (_userManager.Standing.IsNotEmptyOrNull())
                {
                    stAuthIds = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(x => pIds.Contains(x.ObjectId) && x.ItemType.Equals(CommonConst.AuthorizeItemStanding) && x.ItemId.Equals(_userManager.Standing)).Select(x => x.ObjectId).ToListAsync();
                    var relationList = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => stAuthIds.Contains(x.ObjectId) && !x.ObjectType.Equals(CommonConst.RelationObjectOrganize)).Select(x => x.RoleId).ToListAsync();
                    stAuthIds.AddRange(relationList);
                }
                var mIdList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                    .Where(a => a.ItemType == CommonConst.AuthorizeItemModule)
                    .WhereIF(_userManager.Standing.IsNotEmptyOrNull(), a => stAuthIds.Contains(a.ObjectId))
                    .WhereIF(_userManager.Standing.IsNullOrEmpty(), a => pIds.Contains(a.ObjectId))
                    .Select(a => a.ItemId).ToListAsync();

                output = await _repository.AsQueryable()
                    .Where(a => mIdList.Contains(a.Id) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                    .WhereIF(_userManager.IsMainSystem, a => a.EnCode != CommonConst.ModuleCodeAppConfig && a.EnCode != CommonConst.ModuleCodeOnlineDev)
                    .WhereIF(type.Equals("App"), a => a.SystemId.Equals(_userManager.SystemId))
                    .WhereIF(keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(keyword) || a.Type == 1)
                    .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).OrderBy(q => q.CreatorTime, OrderByType.Desc)
                    .ToListAsync();
            }
        }
        else
        {
            output = await _repository.AsQueryable()
                .Where(a => a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                .WhereIF(_userManager.IsMainSystem, a => a.EnCode != CommonConst.ModuleCodeAppConfig && a.EnCode != CommonConst.ModuleCodeOnlineDev)
                .WhereIF(type.Equals("App"), a => a.SystemId.Equals(_userManager.SystemId))
                .WhereIF(keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(keyword) || a.Type == 1)
                .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).OrderBy(q => q.CreatorTime, OrderByType.Desc)
                .ToListAsync();
        }

        if (mIds == null) mIds = new List<string>();
        if (mUrls == null) mUrls = new List<string>();
        return output.Where(x => !mIds.Contains(x.Id) && !mUrls.Contains(x.UrlAddress)).ToList();
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    private async Task<List<ModuleEntity>> GetList()
    {
        return await _repository.AsQueryable().Where(x => x.DeleteMark == null && x.SystemId == _userManager.SystemId && !x.EnCode.Equals("workFlow")).OrderBy(o => o.SortCode).OrderBy(o => o.CreatorTime, OrderByType.Desc).ToListAsync();
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="errorMsg"></param>
    /// <param name="deffSystem"></param>
    /// <returns></returns>
    private async Task ImportData(ModuleExportInput data, int type, List<string> errorMsg, bool deffSystem)
    {
        try
        {
            if (data.buttonEntityList.Any())
            {
                var button = data.buttonEntityList.Adapt<List<ModuleButtonEntity>>();
                var buttonDic = new Dictionary<string, string>();
                foreach (var item in button)
                {
                    var isExist = false; // 是否出现重复
                    var random = new Random().NextLetterAndNumberString(5);
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (deffSystem)
                        {
                            item.Id = SnowflakeIdHelper.NextId();
                        }
                        else if (await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (buttonDic.ContainsKey("ID"))
                                buttonDic["ID"] = string.Format("{0}、{1}", buttonDic["ID"], item.Id);
                            else
                                buttonDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.EnCode.Equals(item.EnCode)))
                    {
                        if (buttonDic.ContainsKey("编码"))
                            buttonDic["编码"] = string.Format("{0}、{1}", buttonDic["编码"], item.EnCode);
                        else
                            buttonDic.Add("编码", item.EnCode);
                        isExist = true;
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.FullName.Equals(item.FullName)))
                    {
                        if (buttonDic.ContainsKey("名称"))
                            buttonDic["名称"] = string.Format("{0}、{1}", buttonDic["名称"], item.FullName);
                        else
                            buttonDic.Add("名称", item.FullName);
                        isExist = true;
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                        item.EnCode += random;
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && buttonDic.Any())
                {
                    var buttonMsg = new List<string>();
                    foreach (var item in buttonDic)
                        buttonMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("buttonEntityList：{0}重复", string.Join("、", buttonMsg)));
                }
            }
            if (data.columnEntityList.Any())
            {
                var colum = data.columnEntityList.Adapt<List<ModuleColumnEntity>>();
                var columDic = new Dictionary<string, string>();
                foreach (var item in colum)
                {
                    var isExist = false; // 是否出现重复
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (deffSystem)
                        {
                            item.Id = SnowflakeIdHelper.NextId();
                        }
                        else if (await _repository.AsSugarClient().Queryable<ModuleColumnEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (columDic.ContainsKey("ID"))
                                columDic["ID"] = string.Format("{0}、{1}", columDic["ID"], item.Id);
                            else
                                columDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && columDic.Any())
                {
                    var columMsg = new List<string>();
                    foreach (var item in columDic)
                        columMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("columnEntityList：{0}重复", string.Join("、", columMsg)));
                }
            }
            if (data.formEntityList.Any())
            {
                var form = data.formEntityList.Adapt<List<ModuleFormEntity>>();
                var formDic = new Dictionary<string, string>();
                foreach (var item in form)
                {
                    var isExist = false; // 是否出现重复
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (deffSystem)
                        {
                            item.Id = SnowflakeIdHelper.NextId();
                        }
                        else if (await _repository.AsSugarClient().Queryable<ModuleFormEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (formDic.ContainsKey("ID"))
                                formDic["ID"] = string.Format("{0}、{1}", formDic["ID"], item.Id);
                            else
                                formDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && formDic.Any())
                {
                    var formMsg = new List<string>();
                    foreach (var item in formDic)
                        formMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("formEntityList：{0}重复", string.Join("、", formMsg)));
                }
            }

            var dic = new Dictionary<string, string>();
            if (data.schemeEntityList.Any())
            {
                var dataAuthorizeScheme = data.schemeEntityList.Adapt<List<ModuleDataAuthorizeSchemeEntity>>();
                var dataAuthorizeSchemeDic = new Dictionary<string, string>();
                foreach (var item in dataAuthorizeScheme)
                {
                    var isExist = false; // 是否出现重复
                    var random = new Random().NextLetterAndNumberString(5);
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (deffSystem)
                        {
                            item.Id = SnowflakeIdHelper.NextId();
                        }
                        else if (await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (dataAuthorizeSchemeDic.ContainsKey("ID"))
                                dataAuthorizeSchemeDic["ID"] = string.Format("{0}、{1}", dataAuthorizeSchemeDic["ID"], item.Id);
                            else
                                dataAuthorizeSchemeDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.EnCode.Equals(item.EnCode)))
                    {
                        if (dataAuthorizeSchemeDic.ContainsKey("编码"))
                            dataAuthorizeSchemeDic["编码"] = string.Format("{0}、{1}", dataAuthorizeSchemeDic["编码"], item.EnCode);
                        else
                            dataAuthorizeSchemeDic.Add("编码", item.EnCode);
                        isExist = true;
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.FullName.Equals(item.FullName)))
                    {
                        if (dataAuthorizeSchemeDic.ContainsKey("名称"))
                            dataAuthorizeSchemeDic["名称"] = string.Format("{0}、{1}", dataAuthorizeSchemeDic["名称"], item.FullName);
                        else
                            dataAuthorizeSchemeDic.Add("名称", item.FullName);
                        isExist = true;
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                        item.EnCode += random;
                        if (item.ConditionJson.IsNotEmptyOrNull())
                        {
                            foreach (var key in dic.Keys)
                            {
                                item.ConditionJson = item.ConditionJson.Replace(key, dic[key]);
                            }
                        }
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && dataAuthorizeSchemeDic.Any())
                {
                    var dataAuthorizeSchemeMsg = new List<string>();
                    foreach (var item in dataAuthorizeSchemeDic)
                        dataAuthorizeSchemeMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("schemeEntityList：{0}重复", string.Join("、", dataAuthorizeSchemeMsg)));
                }
            }

            var module = data.Adapt<ModuleEntity>();
            module.Create();
            module.CreatorUserId = _userManager.UserId;
            module.LastModifyTime = null;
            module.LastModifyUserId = null;
            var stor = _repository.AsSugarClient().Storageable(module).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await stor.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await stor.AsUpdateable.ExecuteCommandAsync(); // 执行更新
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }

    /// <summary>
    /// 递归获取发布菜单名称.
    /// </summary>
    private void GetReleaseName(List<string> list, List<ModuleEntity> moduleList, List<SystemEntity> systemList, ModuleEntity module, string? url)
    {
        if (url.IsNullOrEmpty()) url = module.FullName;

        if (module.ParentId.Equals("-1"))
        {
            var sys = systemList.Find(it => it.Id.Equals(module.SystemId));
            if (sys.IsNotEmptyOrNull())
            {
                url = string.Format("{0}/{1}", sys.FullName, url);
                list.Add(url);
            }
        }
        else
        {
            var mod = moduleList.Find(it => it.Id.Equals(module.ParentId));
            if (mod.IsNotEmptyOrNull())
            {
                url = string.Format("{0}/{1}", mod.FullName, url);
                GetReleaseName(list, moduleList, systemList, mod, url);
            }
        }
    }

    #endregion
}