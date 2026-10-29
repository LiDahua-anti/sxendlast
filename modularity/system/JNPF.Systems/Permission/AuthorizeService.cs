using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Dto.Authorize;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：操作权限.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Authority", Order = 170)]
[Route("api/permission/[controller]")]
public class AuthorizeService : IAuthorizeService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 权限操作表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<AuthorizeEntity> _repository;

    private readonly IDictionaryDataService _dictionaryDataService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="AuthorizeService"/>类型的新实例.
    /// </summary>
    public AuthorizeService(
        ISqlSugarRepository<AuthorizeEntity> repository,
        ICacheManager cacheManager,
        IUserManager userManager,
        IDictionaryDataService dictionaryDataService,
        IMHandler imHandler)
    {
        _repository = repository;
        _dictionaryDataService = dictionaryDataService;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _imHandler = imHandler;
    }

    #region Get

    /// <summary>
    /// 获取功能权限数据.
    /// </summary>
    /// <param name="itemId">模块ID.</param>
    /// <param name="objectType">对象类型.</param>
    /// <returns></returns>
    [HttpGet("Model/{itemId}/{objectType}")]
    public async Task<dynamic> GetModelList(string itemId, string objectType)
    {
        IEnumerable<string> ids = await _repository.AsQueryable().Where(a => a.ItemId == itemId && a.ObjectType == objectType).Select(s => s.ObjectId).ToListAsync();
        return new { ids };
    }

    /// <summary>
    /// 获取模块列表展示字段权限.
    /// </summary>
    /// <param name="moduleId">模块主键.</param>
    /// <returns></returns>
    [HttpGet("GetColumnsByModuleId/{moduleId}")]
    public async Task<dynamic> GetColumnsByModuleId(string moduleId)
    {
        string? data = await _repository.AsSugarClient().Queryable<ColumnsPurviewEntity>().Where(x => x.ModuleId == moduleId).Select(x => x.FieldList).FirstAsync();
        if (!string.IsNullOrEmpty(data)) return data.ToObject<List<ListDisplayFieldOutput>>();
        else return new List<ListDisplayFieldOutput>();
    }

    /// <summary>
    /// 获取打印模板权限数据.
    /// </summary>
    /// <param name="id">权限组id.</param>
    /// <returns></returns>
    [HttpGet("Print/{id}")]
    public async Task<dynamic> GetPrintAuthorizeList(string id)
    {
        // 捞取当前用户所有权限
        var pIds = _userManager.PermissionIds;

        List<string> currUserAuthorizeList = null;
        var isDevOrManagRole = false;
        if (!_userManager.IsAdministrator)
        {
            isDevOrManagRole = await _repository.AsSugarClient().Queryable<RoleEntity>().AnyAsync(x => x.DeleteMark == null && pIds.Contains(x.Id) && (x.EnCode.Equals(CommonConst.RoleCodeTypeManager) || x.EnCode.Equals(CommonConst.RoleCodeTypeDeveloper)));
            currUserAuthorizeList = await _repository.AsQueryable().Where(x => pIds.Contains(x.ObjectId) && x.ItemType.Equals(CommonConst.AuthorizeItemFlow)).Select(x => x.ItemId).ToListAsync();
        }

        var printAuthorizeList = await _repository.AsQueryable().Where(a => a.ObjectId == id && a.ItemType.Equals(CommonConst.AuthorizeItemPrint)).Select(m => m.ItemId).ToListAsync();
        var printList = await _repository.AsSugarClient().Queryable<PrintDevEntity>()
            .Where(a => a.DeleteMark == null && a.State != 0 && a.CommonUse == 1 && a.VisibleType == 2)
            .Select(a => new AuthorizeSystemDataModelOutput
            {
                id = a.Id,
                parentId = a.SystemId,
                fullName = a.FullName,
                icon = a.Icon,
                sortCode = a.SortCode,
                creatorUserId = a.CreatorUserId,
            }).ToListAsync();

        if (printList.Any())
        {
            if (currUserAuthorizeList != null && !isDevOrManagRole)
                foreach (var item in printList.Where(x => !currUserAuthorizeList.Contains(x.id) && x.creatorUserId != _userManager.UserId)) item.disabled = true;
            var dicDataList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => printList.Select(xx => xx.parentId).Contains(x.Id))
                .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime).ToListAsync();
            foreach (var item in dicDataList)
            {
                var disabled = printList.Any(x => item.Id.Equals(x.parentId) && x.disabled);
                printList.Add(new AuthorizeSystemDataModelOutput()
                {
                    fullName = item.FullName,
                    parentId = "0",
                    id = item.Id,
                    disabled = disabled,
                });
            }
        }
        var printAll = printList.Select(x => x.id).ToList();
        return new { list = printList.ToTree().Where(x => x.children != null && x.children.Any()).ToList(), all = printAll, ids = printAuthorizeList };
    }

    /// <summary>
    /// 获取流程权限数据.
    /// </summary>
    /// <param name="id">权限组id.</param>
    /// <returns></returns>
    [HttpGet("Flow/{id}")]
    public async Task<dynamic> GetFlowAuthorizeList(string id)
    {
        // 捞取当前用户所有权限
        var pIds = _userManager.PermissionIds;

        List<string> currUserAuthorizeList = null;
        var isDevOrManagRole = false;
        if (!_userManager.IsAdministrator)
        {
            isDevOrManagRole = await _repository.AsSugarClient().Queryable<RoleEntity>().AnyAsync(x => x.DeleteMark == null && pIds.Contains(x.Id) && (x.EnCode.Equals(CommonConst.RoleCodeTypeManager) || x.EnCode.Equals(CommonConst.RoleCodeTypeDeveloper)));
            currUserAuthorizeList = await _repository.AsQueryable().Where(x => pIds.Contains(x.ObjectId) && x.ItemType.Equals(CommonConst.AuthorizeItemFlow)).Select(x => x.ItemId).ToListAsync();
        }

        var checkFlowList = await _repository.AsQueryable().Where(a => a.ObjectId == id && a.ItemType.Equals(CommonConst.AuthorizeItemFlow)).Select(m => m.ItemId).ToListAsync();
        //var VFID = await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(x => x.VisibleType.Equals(2) && x.Status.Equals(1)).Select(x => x.Id).ToListAsync();
        var flowList = await _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>()
                    .Where(a => a.DeleteMark == null && a.Status == 1 && a.EnabledMark == 1 && a.VisibleType == 2)
                    .Select(a => new AuthorizeSystemDataModelOutput
                    {
                        id = a.Id,
                        parentId = a.SystemId,
                        fullName = a.FullName,
                        icon = a.Icon,
                        sortCode = a.SortCode,
                        creatorUserId = a.CreatorUserId,
                    }).ToListAsync();

        if (flowList.Any())
        {
            if (currUserAuthorizeList != null && !isDevOrManagRole)
                foreach (var item in flowList.Where(x => !currUserAuthorizeList.Contains(x.id) && x.creatorUserId != _userManager.UserId)) item.disabled = true;
            var dicDataList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => flowList.Select(xx => xx.parentId).Contains(x.Id))
                .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime).ToListAsync();
            foreach (var item in dicDataList)
            {
                var disabled = flowList.Any(x => item.Id.Equals(x.parentId) && x.disabled);
                flowList.Add(new AuthorizeSystemDataModelOutput()
                {
                    fullName = item.FullName,
                    parentId = "0",
                    id = item.Id,
                    disabled = disabled,
                });
            }
        }
        var flowAll = flowList.Select(x => x.id).ToList();
        return new { list = flowList.ToTree().Where(x => x.children != null && x.children.Any()).ToList(), all = flowAll, ids = checkFlowList };
    }

    /// <summary>
    /// 获取流程权限组.
    /// </summary>
    /// <param name="id">流程id.</param>
    /// <returns></returns>
    [HttpGet("GroupFlow/{id}")]
    public async Task<dynamic> GetGroupFlowList(string id)
    {
        var checkAuthorizeList = await _repository.AsQueryable().Where(x => x.ItemId.Equals(id) && x.ItemType.Equals(CommonConst.AuthorizeItemFlow)).Select(x => x.ObjectId).ToListAsync();
        return new { list = checkAuthorizeList };
    }

    /// <summary>
    /// 保存流程权限组.
    /// </summary>
    /// <param name="id">流程id.</param>
    /// <param name="input">权限组id.</param>
    /// <returns></returns>
    [HttpPost("GroupFlow/{id}")]
    public async Task SaveGroupFlowList(string id, [FromBody] AuthorizeSaveInput input)
    {
        await _repository.AsDeleteable().Where(x => x.ItemId.Equals(id) && x.ItemType.Equals(CommonConst.AuthorizeItemFlow)).ExecuteCommandAsync();
        var addList = new List<AuthorizeEntity>();
        foreach (string? item in input.ids)
        {
            AuthorizeEntity? entity = new AuthorizeEntity();
            entity.ItemId = id;
            entity.ItemType = CommonConst.AuthorizeItemFlow;
            entity.ObjectId = item;
            entity.ObjectType = input.objectType;
            addList.Add(entity);
        }

        // 数据不为空添加
        if (addList.Any())
        {
            // 新增权限
            int num = await _repository.AsSugarClient().Insertable(addList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
    }

    #endregion

    #region Post

    /// <summary>
    /// 权限数据.
    /// </summary>
    /// <param name="objectId">对象主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("Data/{objectId}/Values")]
    public async Task<dynamic> GetDataValues(string objectId, [FromBody] AuthorizeDataQuery input)
    {
        var objectIdList = new List<string>() { objectId };
        AuthorizeDataOutput? output = new AuthorizeDataOutput()
        {
            all = new List<string>(),
            ids = new List<string>(),
            list = new List<AuthorizeDataModelOutput>()
        };

        // 捞取当前用户所有权限
        var pIds = _userManager.PermissionIds;

        // 组织类型授权依赖上级组织权限
        List<AuthorizeEntity> orgObjList = null;
        if (input.objectType.Equals(CommonConst.RelationObjectOrganize))
        {
            var orgEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => objectId.Equals(x.Id)).FirstAsync();

            if (orgEntity.ParentId != "-1")
            {
                // 上级组织所有授权对象Id
                orgObjList = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(input.objectType) && x.ObjectId.Equals(orgEntity.ParentId))
                    .Select(x => new AuthorizeEntity() { ItemId = x.ItemId, ItemType = x.ItemType }).ToListAsync();

                // 上级组织角色授权对象Id
                var orgRoleIds = await _repository.AsSugarClient().Queryable<RoleEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.RoleId == a.Id && a.DeleteMark == null))
                    .Where((a, b) => b.ObjectId.Equals(orgEntity.ParentId)).Select((a, b) => a.Id).ToListAsync();
                var orgRoleList = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectRole) && orgRoleIds.Contains(x.ObjectId))
                    .Select(x => new AuthorizeEntity() { ItemId = x.ItemId, ItemType = x.ItemType }).ToListAsync();
                orgObjList.AddRange(orgRoleList);
            }
        }
        else if (input.objectType.Equals(CommonConst.RelationObjectPosition))
        {
            var posEntity = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => objectId.Equals(x.Id)).FirstAsync();

            if (posEntity.ParentId.IsNullOrEmpty() || posEntity.ParentId.Equals("-1"))
            {
                // 岗位所属组织所有授权对象Id
                orgObjList = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectOrganize) && x.ObjectId.Equals(posEntity.OrganizeId))
                    .Select(x => new AuthorizeEntity() { ItemId = x.ItemId, ItemType = x.ItemType }).ToListAsync();

                // 所属组织角色授权对象Id
                var posRoleIds = await _repository.AsSugarClient().Queryable<RoleEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.RoleId == a.Id && a.DeleteMark == null))
                    .Where((a, b) => b.ObjectId.Equals(posEntity.OrganizeId)).Select((a, b) => a.Id).ToListAsync();
                var posRoleList = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectRole) && posRoleIds.Contains(x.ObjectId))
                    .Select(x => new AuthorizeEntity() { ItemId = x.ItemId, ItemType = x.ItemType }).ToListAsync();
                orgObjList.AddRange(posRoleList);
            }
            else
            {
                // 上级岗位所有授权对象Id
                orgObjList = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectPosition) && x.ObjectId.Equals(posEntity.ParentId))
                    .Select(x => new AuthorizeEntity() { ItemId = x.ItemId, ItemType = x.ItemType }).ToListAsync();

                // 上级岗位角色授权对象Id
                var posRoleIds = await _repository.AsSugarClient().Queryable<RoleEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.RoleId == a.Id && a.DeleteMark == null))
                    .Where((a, b) => b.ObjectId.Equals(posEntity.ParentId)).Select((a, b) => a.Id).ToListAsync();
                var posRoleList = await _repository.AsQueryable().Where(x => x.ObjectType.Equals(CommonConst.RelationObjectRole) && posRoleIds.Contains(x.ObjectId))
                    .Select(x => new AuthorizeEntity() { ItemId = x.ItemId, ItemType = x.ItemType }).ToListAsync();
                orgObjList.AddRange(posRoleList);
            }

        }

        List<AuthorizeEntity> currUserAuthorizeList = null;
        var isDevOrManagRole = false;
        if (!_userManager.IsAdministrator)
        {
            if (!input.objectType.Equals(CommonConst.RelationObjectRole))
            {
                var objIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => pIds.Contains(x.ObjectId) && x.ObjectType != CommonConst.RelationObjectRole).Select(x => new RoleRelationEntity() { RoleId = x.RoleId, ObjectId = x.ObjectId }).ToListAsync();
                objectIdList.AddRange(objIds.Where(x => objectIdList.Contains(x.ObjectId)).Select(x => x.RoleId));
                pIds.AddRange(objIds.Select(x => x.RoleId));
            }

            currUserAuthorizeList = await _repository.AsQueryable().Where(x => pIds.Contains(x.ObjectId)).Select(x => new AuthorizeEntity() { ItemId = x.ItemId, ItemType = x.ItemType }).ToListAsync();
            isDevOrManagRole = await _repository.AsSugarClient().Queryable<RoleEntity>().AnyAsync(x => x.DeleteMark == null && pIds.Contains(x.Id) && (x.EnCode.Equals(CommonConst.RoleCodeTypeManager) || x.EnCode.Equals(CommonConst.RoleCodeTypeDeveloper)));
        }
        else
        {
            if (!input.objectType.Equals(CommonConst.RelationObjectRole))
            {
                var objIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.ObjectType != CommonConst.RelationObjectRole).Select(x => new RoleRelationEntity() { RoleId = x.RoleId, ObjectId = x.ObjectId }).ToListAsync();
                objectIdList.AddRange(objIds.Where(x => objectIdList.Contains(x.ObjectId)).Select(x => x.RoleId));
                pIds.AddRange(objIds.Select(x => x.RoleId));
            }
        }

        #region 所有应用、菜单

        // 已勾选的应用和菜单
        List<string> selectIds = null;
        if (input.moduleIds.IsNotEmptyOrNull()) selectIds = input.moduleIds.Split(',').ToList();

        var mainList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => x.IsMain.Equals(1)).Select(x => new ModuleEntity() { Id = x.Id, EnCode = x.EnCode, FullName = x.FullName }).ToListAsync();
        var mainSystemId = mainList.Where(x => x.EnCode.Equals(CommonConst.SystemCodeMain)).Select(x => x.Id).First();
        if (orgObjList != null) orgObjList.Add(new AuthorizeEntity() { ItemType = CommonConst.AuthorizeItemSystem, ItemId = mainSystemId });
        var allSystemList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => (x.DeleteMark == null && x.EnabledMark.Equals(1)) || (x.CreatorUserId.Equals(_userManager.UserId) && x.DeleteMark == null && x.EnabledMark.Equals(1)))
            .WhereIF(input.moduleIds.IsNotEmptyOrNull(), x => selectIds.Contains(x.Id))
            .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
            .Select(x => new ModuleEntity()
            {
                Id = x.Id,
                ParentId = "-1",
                FullName = x.FullName,
                EnCode = x.EnCode,
                Icon = x.Icon,
                SystemId = "-1",
                SortCode = x.SortCode,
                CreatorUserId = x.CreatorUserId,
            }).ToListAsync();

        var allModuleList = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => (x.DeleteMark == null && x.EnabledMark.Equals(1)) || (x.CreatorUserId.Equals(_userManager.UserId) && x.DeleteMark == null && x.EnabledMark.Equals(1)))
            .WhereIF(input.moduleIds.IsNotEmptyOrNull(), x => selectIds.Contains(x.Id))
            .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
            .Select(x => new ModuleEntity()
            {
                Id = x.Id,
                ParentId = x.ParentId,
                SystemId = x.SystemId,
                Category = x.Category,
                FullName = x.FullName,
                EnCode = x.EnCode,
                Icon = x.Icon,
                CreatorUserId = x.CreatorUserId,
            }).ToListAsync();

        if (orgObjList != null)
        {
            var selectItemIds = new List<string>();
            if (orgObjList != null) selectItemIds = orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemModule) || xx.ItemType.Equals(CommonConst.AuthorizeItemSystem)).Select(xx => xx.ItemId).ToList();
            if (selectItemIds.Contains("XTCD")) selectItemIds.Add(mainSystemId);
            allModuleList = allModuleList.Where(x => selectItemIds.Contains(x.Id)).ToList();
            allSystemList = allSystemList.Where(x => selectItemIds.Contains(x.Id)).ToList();
        }

        if (currUserAuthorizeList != null)
        {
            var selectItemIds = new List<string>();
            if (currUserAuthorizeList != null) selectItemIds = currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemModule) || xx.ItemType.Equals(CommonConst.AuthorizeItemSystem)).Select(xx => xx.ItemId).ToList();
            if (selectItemIds.Contains("XTCD")) selectItemIds.Add(mainSystemId);

            if (isDevOrManagRole)
            {
                foreach (var module in allModuleList.Where(x => !selectItemIds.Contains(x.Id) && x.SystemId.Equals(mainSystemId)))
                    module.PropertyJson = "disabled"; // disabled = true , 置灰
                foreach (var module in allSystemList.Where(x => !selectItemIds.Contains(x.Id) && x.Id.Equals(mainSystemId)))
                    module.PropertyJson = "disabled"; // disabled = true , 置灰
            }
            else
            {
                foreach (var module in allModuleList.Where(x => !selectItemIds.Contains(x.Id) && x.CreatorUserId != _userManager.UserId))
                    module.PropertyJson = "disabled"; // disabled = true , 置灰
                foreach (var module in allSystemList.Where(x => !selectItemIds.Contains(x.Id) && x.CreatorUserId != _userManager.UserId))
                    module.PropertyJson = "disabled"; // disabled = true , 置灰
            }

            foreach (var it in mainList.Where(x => x.Id != mainSystemId))
            {
                if (allModuleList.Any(x => selectItemIds.Contains(x.Id) && x.SystemId.Equals(it.Id))) selectItemIds.Add(it.Id);
            }
        }

        #endregion

        #region 系统菜单组装
        var mainModelList = new List<ModuleEntity>();
        if (allSystemList.Any(x => x.EnCode.Equals(CommonConst.SystemCodeMain)))
        {
            var mainSystemList = allSystemList.Where(x => x.EnCode.Equals(CommonConst.SystemCodeTeamwork) || x.EnCode.Equals(CommonConst.SystemCodeWorkFlow));
            if (mainSystemList.Any(x => x.ParentId == "-1"))
            {
                mainSystemList.Where(x => x.ParentId == "-1").ToList().ForEach(it => it.ParentId = "XTCD");
            }

            mainModelList = allModuleList.Where(x => x.SystemId.Equals(mainSystemId))
            .Where(x => !x.EnCode.Equals(CommonConst.ModuleCodeAppConfig) && !x.EnCode.Equals(CommonConst.ModuleCodeOnlineDev)).ToList();
            if (mainModelList.Any(x => x.ParentId == "-1"))
            {
                mainModelList.Where(x => x.ParentId == "-1").ToList().ForEach(it => it.ParentId = "XTCD");
            }

            if (mainModelList.Any() || mainSystemList.Any())
            {
                mainModelList.Add(new ModuleEntity() { Id = "XTCD", FullName = "系统菜单", Icon = "", });
            }
        }

        #endregion

        #region 应用菜单组装

        var moduleList = allModuleList.Where(x => !x.SystemId.Equals(mainSystemId)).ToList();
        var systemList = allSystemList.Where(x => !x.EnCode.Equals(CommonConst.SystemCodeTeamwork) && !x.EnCode.Equals(CommonConst.SystemCodeWorkFlow) && !x.EnCode.Equals(CommonConst.SystemCodeMain)).ToList();
        systemList = systemList.Where(x => moduleList.Select(xx => xx.SystemId).Contains(x.Id)).ToList();
        var addList = new List<ModuleEntity>();
        var sysIdIndex = 1;
        systemList.ForEach(it =>
        {
            it.ParentId = "YYCD";
            var addApp = new ModuleEntity() { Id = it.Id + sysIdIndex, ParentId = it.Id, FullName = "APP菜单" };
            sysIdIndex++;
            var addWeb = new ModuleEntity() { Id = it.Id + sysIdIndex, ParentId = it.Id, FullName = "WEB菜单" };
            sysIdIndex++;
            if (moduleList.Any(x => x.ParentId == "-1" && x.Category == "Web" && x.SystemId == it.Id))
            {
                addList.Add(addWeb);
                moduleList.Where(x => x.ParentId == "-1" && x.Category == "Web" && x.SystemId == it.Id).ToList().ForEach(x => x.ParentId = addWeb.Id);
            }
            if (moduleList.Any(x => x.ParentId == "-1" && x.Category == "App" && x.SystemId == it.Id))
            {
                addList.Add(addApp);
                moduleList.Where(x => x.ParentId == "-1" && x.Category == "App" && x.SystemId == it.Id).ToList().ForEach(x => x.ParentId = addApp.Id);
            }
        });
        var fIds = allSystemList.Where(x => x.EnCode.Equals(CommonConst.SystemCodeTeamwork) || x.EnCode.Equals(CommonConst.SystemCodeWorkFlow)).Select(x => x.Id).ToList();
        moduleList.Where(x => fIds.Contains(x.SystemId) && x.ParentId.Equals("-1")).ToList().ForEach(x => x.ParentId = "XTCD");
        moduleList.RemoveAll(x => x.ParentId == "-1");
        mainModelList.AddRange(systemList);
        mainModelList.AddRange(addList);
        mainModelList.AddRange(moduleList);
        if (systemList.Any()) mainModelList.Add(new ModuleEntity() { Id = "YYCD", FullName = "应用菜单", Icon = "", });

        #endregion

        #region 按钮、列表、表单、数据权限
        switch (input.type)
        {
            case CommonConst.AuthorizeItemButton:
                {
                    var allList = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(x => (x.DeleteMark == null && x.EnabledMark.Equals(1)) || (x.CreatorUserId.Equals(_userManager.UserId) && x.DeleteMark == null && x.EnabledMark.Equals(1)))
                        //.WhereIF(currUserAuthorizeList != null, x => currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemButton)).Select(xx => xx.ItemId).Contains(x.Id))
                        //.WhereIF(orgObjList != null, x => orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemButton)).Select(xx => xx.ItemId).Contains(x.Id))
                        .Where(x => allModuleList.Select(x => x.Id).Contains(x.ModuleId))
                        .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
                        .Select(x => new ModuleEntity()
                        {
                            Id = x.Id,
                            ParentId = x.ModuleId,
                            FullName = x.FullName,
                            EnCode = x.EnCode,
                            Icon = x.Icon,
                            SortCode = x.SortCode,
                            CreatorUserId = x.CreatorUserId,
                        }).ToListAsync();

                    if (currUserAuthorizeList != null || orgObjList != null)
                    {
                        var selectItemIds = new List<string>();
                        if (currUserAuthorizeList != null) selectItemIds = currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemButton)).Select(xx => xx.ItemId).ToList();
                        if (orgObjList != null) selectItemIds = orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemButton)).Select(xx => xx.ItemId).ToList();

                        foreach (var module in allList.Where(x => !selectItemIds.Contains(x.Id))) module.PropertyJson = "disabled"; // disabled = true , 置灰
                        if (input.objectType.Equals(CommonConst.RelationObjectPosition))
                            allList = allList.Where(x => selectItemIds.Contains(x.Id)).ToList();
                    }

                    var fids = GetOutPutByChild(mainModelList, allList.Select(x => x.ParentId).ToList());
                    mainModelList = mainModelList.Where(x => fids.Contains(x.Id)).ToList();
                    mainModelList.AddRange(allList);
                }

                break;
            case CommonConst.AuthorizeItemColumn:
                {
                    var allList = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(x => (x.DeleteMark == null && x.EnabledMark.Equals(1)) || (x.CreatorUserId.Equals(_userManager.UserId) && x.DeleteMark == null && x.EnabledMark.Equals(1)))
                        //.WhereIF(currUserAuthorizeList != null, x => currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemColumn)).Select(xx => xx.ItemId).Contains(x.Id))
                        //.WhereIF(orgObjList != null, x => orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemColumn)).Select(xx => xx.ItemId).Contains(x.Id))
                        .Where(x => allModuleList.Select(x => x.Id).Contains(x.ModuleId))
                        .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
                        .Select(x => new ModuleEntity()
                        {
                            Id = x.Id,
                            ParentId = x.ModuleId,
                            FullName = x.FullName,
                            EnCode = x.EnCode,
                            SortCode = x.SortCode,
                            CreatorUserId = x.CreatorUserId,
                        }).ToListAsync();

                    if (currUserAuthorizeList != null || orgObjList != null)
                    {
                        var selectItemIds = new List<string>();
                        if (currUserAuthorizeList != null) selectItemIds = currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemColumn)).Select(xx => xx.ItemId).ToList();
                        if (orgObjList != null) selectItemIds = orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemColumn)).Select(xx => xx.ItemId).ToList();
                        foreach (var module in allList.Where(x => !selectItemIds.Contains(x.Id))) module.PropertyJson = "disabled"; // disabled = true , 置灰
                        if (input.objectType.Equals(CommonConst.RelationObjectPosition))
                            allList = allList.Where(x => selectItemIds.Contains(x.Id)).ToList();
                    }

                    var fids = GetOutPutByChild(mainModelList, allList.Select(x => x.ParentId).ToList());
                    mainModelList = mainModelList.Where(x => fids.Contains(x.Id)).ToList();
                    mainModelList.AddRange(allList);
                }

                break;
            case CommonConst.AuthorizeItemForm:
                {
                    var allList = await _repository.AsSugarClient().Queryable<ModuleFormEntity>().Where(x => (x.DeleteMark == null && x.EnabledMark.Equals(1)) || (x.CreatorUserId.Equals(_userManager.UserId) && x.DeleteMark == null && x.EnabledMark.Equals(1)))
                        //.WhereIF(currUserAuthorizeList != null, x => currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemForm)).Select(xx => xx.ItemId).Contains(x.Id))
                        //.WhereIF(orgObjList != null, x => orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemForm)).Select(xx => xx.ItemId).Contains(x.Id))
                        .Where(x => allModuleList.Select(x => x.Id).Contains(x.ModuleId))
                        .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
                        .Select(x => new ModuleEntity()
                        {
                            Id = x.Id,
                            ParentId = x.ModuleId,
                            FullName = x.FullName,
                            EnCode = x.EnCode,
                            SortCode = x.SortCode,
                            CreatorUserId = x.CreatorUserId,
                        }).ToListAsync();

                    if (currUserAuthorizeList != null || orgObjList != null)
                    {
                        var selectItemIds = new List<string>();
                        if (currUserAuthorizeList != null) selectItemIds = currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemForm)).Select(xx => xx.ItemId).ToList();
                        if (orgObjList != null) selectItemIds = orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemForm)).Select(xx => xx.ItemId).ToList();
                        foreach (var module in allList.Where(x => !selectItemIds.Contains(x.Id))) module.PropertyJson = "disabled"; // disabled = true , 置灰
                        if (input.objectType.Equals(CommonConst.RelationObjectPosition))
                            allList = allList.Where(x => selectItemIds.Contains(x.Id)).ToList();
                    }

                    var fids = GetOutPutByChild(mainModelList, allList.Select(x => x.ParentId).ToList());
                    mainModelList = mainModelList.Where(x => fids.Contains(x.Id)).ToList();
                    mainModelList.AddRange(allList);
                }

                break;
            case CommonConst.AuthorizeItemResource:
                {
                    var allList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(x => (x.DeleteMark == null && x.EnabledMark.Equals(1)) || (x.CreatorUserId.Equals(_userManager.UserId) && x.DeleteMark == null && x.EnabledMark.Equals(1)))
                        //.WhereIF(currUserAuthorizeList != null, x => currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemResource)).Select(xx => xx.ItemId).Contains(x.Id))
                        //.WhereIF(orgObjList != null, x => orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemResource)).Select(xx => xx.ItemId).Contains(x.Id))
                        .Where(x => allModuleList.Select(x => x.Id).Contains(x.ModuleId))
                        .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
                        .Select(x => new ModuleEntity()
                        {
                            Id = x.Id,
                            ParentId = x.ModuleId,
                            FullName = x.FullName,
                            EnCode = x.EnCode,
                            SortCode = x.SortCode,
                            CreatorUserId = x.CreatorUserId,
                        }).ToListAsync();

                    if (currUserAuthorizeList != null || orgObjList != null)
                    {
                        var selectItemIds = new List<string>();
                        if (currUserAuthorizeList != null) selectItemIds = currUserAuthorizeList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemResource)).Select(xx => xx.ItemId).ToList();
                        if (orgObjList != null) selectItemIds = orgObjList.Where(xx => xx.ItemType.Equals(CommonConst.AuthorizeItemResource)).Select(xx => xx.ItemId).ToList();
                        foreach (var module in allList.Where(x => !selectItemIds.Contains(x.Id))) module.PropertyJson = "disabled"; // disabled = true , 置灰
                        if (input.objectType.Equals(CommonConst.RelationObjectPosition))
                            allList = allList.Where(x => selectItemIds.Contains(x.Id)).ToList();
                    }

                    var fids = GetOutPutByChild(mainModelList, allList.Select(x => x.ParentId).ToList());
                    mainModelList = mainModelList.Where(x => fids.Contains(x.Id)).ToList();
                    mainModelList.AddRange(allList);
                }

                break;
        }
        #endregion

        var modelOutputList = new List<AuthorizeDataModelOutput>();
        foreach (var mItem in mainModelList)
        {
            var addItem = mItem.Adapt<AuthorizeDataModelOutput>();
            if (mItem.PropertyJson != null && mItem.PropertyJson.Equals("disabled")) addItem.disabled = true;
            modelOutputList.Add(addItem);
        }

        // 已选中的权限Ids
        output.ids = await _repository.AsQueryable().Where(x => x.ObjectId.Equals(objectId)).Select(x => x.ItemId).ToListAsync();
        if (selectIds != null) output.ids.AddRange(selectIds);
        output.ids = output.ids.Distinct().ToList();
        GetOutPutResult(ref output, modelOutputList, output.ids);
        return output;
    }

    /// <summary>
    /// 设置或更新岗位/角色/用户权限.
    /// </summary>
    /// <param name="objectId">参数.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("Data/{objectId}")]
    [NonUnify]
    public async Task<dynamic> UpdateData(string objectId, [FromBody] AuthorizeDataUpInput input)
    {
        //input.module = input.module.Distinct().ToList();
        //input.button = input.button.Except(input.module).Distinct().ToList();
        //input.column = input.column.Except(input.module).Distinct().ToList();
        //input.form = input.form.Except(input.module).Distinct().ToList();
        //input.resource = input.resource.Except(input.module).Distinct().ToList();
        List<AuthorizeEntity>? authorizeList = new List<AuthorizeEntity>();
        var systemIds = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => input.module.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        if (input.module.Contains("XTCD"))
            systemIds.Add(await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => x.EnCode.Equals(CommonConst.SystemCodeMain)).Select(x => x.Id).FirstAsync());
        input.module = input.module.Except(systemIds).ToList();
        var inputModuleCount = input.module.Count();

        // 岗位、角色授权约束
        if (input.objectType.Equals(CommonConst.RelationObjectPosition) || input.objectType.Equals(CommonConst.RelationObjectRole))
        {
            var posEntity = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.Id.Equals(objectId)).FirstAsync();
            var roleEntity = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.Id.Equals(objectId) && x.Type.Equals(CommonConst.RelationObjectUser)).FirstAsync();
            var IsCondition = posEntity != null ? posEntity.IsCondition : (roleEntity != null ? roleEntity.IsCondition : 0);
            var ConditionJson = posEntity != null ? posEntity.ConditionJson : (roleEntity != null ? roleEntity.ConditionJson : string.Empty);
            if (IsCondition.Equals(1) && ConditionJson.IsNotEmptyOrNull())
            {
                var entityFullName = posEntity != null ? posEntity.FullName : roleEntity.FullName;
                var condition = ConditionJson.ToObject<Entitys.Dto.Role.RoleConstraintModel>();
                if (condition.constraintType.Any() && condition.constraintType.Contains(1))
                {
                    var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => input.module.Contains(x.Id)).ToListAsync();
                    var authorizeCount = await _repository.AsQueryable().Where(a => a.ObjectId == objectId && a.ItemType.Equals(CommonConst.AuthorizeItemModule) && moduleList.Where(xx => xx.Type != 1).Select(xx => xx.Id).Contains(a.ItemId)).CountAsync();
                    if (authorizeCount >= condition.permissionNum && moduleList.Count(x => x.Type != 1) > authorizeCount)
                    {
                        return new UnifyResult.RESTfulResult<object>() { code = 503, msg = "已达权限上限个数" };
                    }
                    else if (moduleList.Count(x => x.Type != 1) > condition.permissionNum)
                    {
                        input.module = moduleList.Where(x => x.Type == 1).Select(x => x.Id).ToList();
                        input.module.AddRange(moduleList.Where(x => x.Type != 1).Select(x => x.Id).ToList().GetRange(0, condition.permissionNum.ParseToInt()));
                        input.button = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(x => input.module.Contains(x.ModuleId) && input.button.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                        input.column = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(x => input.module.Contains(x.ModuleId) && input.column.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                        input.form = await _repository.AsSugarClient().Queryable<ModuleFormEntity>().Where(x => input.module.Contains(x.ModuleId) && input.form.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                        input.resource = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(x => input.module.Contains(x.ModuleId) && input.resource.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                    }
                }
            }
            else
            {
                var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => input.module.Contains(x.Id)).ToListAsync();
                input.module = moduleList.Select(x => x.Id).ToList();
                input.button = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(x => input.module.Contains(x.ModuleId) && input.button.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                input.column = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(x => input.module.Contains(x.ModuleId) && input.column.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                input.form = await _repository.AsSugarClient().Queryable<ModuleFormEntity>().Where(x => input.module.Contains(x.ModuleId) && input.form.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                input.resource = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(x => input.module.Contains(x.ModuleId) && input.resource.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                inputModuleCount = input.module.Count();
            }
        }

        AddAuthorizeEntity(ref authorizeList, systemIds, objectId, input.objectType, CommonConst.AuthorizeItemSystem);
        AddAuthorizeEntity(ref authorizeList, input.module, objectId, input.objectType, CommonConst.AuthorizeItemModule);
        AddAuthorizeEntity(ref authorizeList, input.button, objectId, input.objectType, CommonConst.AuthorizeItemButton);
        AddAuthorizeEntity(ref authorizeList, input.column, objectId, input.objectType, CommonConst.AuthorizeItemColumn);
        AddAuthorizeEntity(ref authorizeList, input.form, objectId, input.objectType, CommonConst.AuthorizeItemForm);
        AddAuthorizeEntity(ref authorizeList, input.resource, objectId, input.objectType, CommonConst.AuthorizeItemResource);

        // 联动变更子权限
        if (!input.objectType.Equals(CommonConst.RelationObjectRole))
        {
            if (input.objectType.Equals(CommonConst.RelationObjectOrganize))
            {
                var orgChildIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => !x.Id.Equals(objectId) && x.OrganizeIdTree.Contains(objectId)).Select(x => x.Id).ToListAsync();
                if (orgChildIds.Any())
                {
                    var orgRoleIds = await _repository.AsSugarClient().Queryable<RoleEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.RoleId == a.Id && a.DeleteMark == null))
                        .Where((a, b) => b.ObjectId.Equals(objectId)).Select((a, b) => a.Id).ToListAsync();
                    var orgChildIdsItemList = await _repository.AsQueryable().Where(x => orgChildIds.Contains(x.ObjectId)).Select(x => new { Id = x.Id, ItemId = x.ItemId }).ToListAsync();
                    var orgRoleIdsItemList = await _repository.AsQueryable().Where(x => orgRoleIds.Contains(x.ObjectId)).Select(x => new { Id = x.Id, ItemId = x.ItemId }).ToListAsync();

                    // 过滤删除授权id
                    var delIdList = orgChildIdsItemList
                        .Where(x => !systemIds.Contains(x.ItemId))
                        .Where(x => !input.module.Contains(x.ItemId))
                        .Where(x => !input.button.Contains(x.ItemId))
                        .Where(x => !input.column.Contains(x.ItemId))
                        .Where(x => !input.form.Contains(x.ItemId))
                        .Where(x => !input.resource.Contains(x.ItemId))
                        .Where(x => !orgRoleIdsItemList.Select(xx => xx.ItemId).Contains(x.ItemId))
                        .Select(x => x.Id).ToList();
                    await _repository.AsDeleteable().Where(x => delIdList.Contains(x.Id)).ExecuteCommandAsync();
                }
            }

            var posChildIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => !x.Id.Equals(objectId) && (x.OrganizeId.Equals(objectId) || x.PositionIdTree.Contains(objectId))).Select(x => x.Id).ToListAsync();

            if (posChildIds.Any())
            {
                var posRoleIds = await _repository.AsSugarClient().Queryable<RoleEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.RoleId == a.Id && a.DeleteMark == null))
                    .Where((a, b) => b.ObjectId.Equals(objectId)).Select((a, b) => a.Id).ToListAsync();
                var orgChildIdsItemList = await _repository.AsQueryable().Where(x => posChildIds.Contains(x.ObjectId)).Select(x => new { Id = x.Id, ItemId = x.ItemId }).ToListAsync();
                var orgRoleIdsItemList = await _repository.AsQueryable().Where(x => posRoleIds.Contains(x.ObjectId)).Select(x => new { Id = x.Id, ItemId = x.ItemId }).ToListAsync();

                // 过滤删除授权id
                var delIdList = orgChildIdsItemList
                    .Where(x => !systemIds.Contains(x.ItemId))
                    .Where(x => !input.module.Contains(x.ItemId))
                    .Where(x => !input.button.Contains(x.ItemId))
                    .Where(x => !input.column.Contains(x.ItemId))
                    .Where(x => !input.form.Contains(x.ItemId))
                    .Where(x => !input.resource.Contains(x.ItemId))
                    .Where(x => !orgRoleIdsItemList.Select(xx => xx.ItemId).Contains(x.ItemId))
                    .Select(x => x.Id).ToList();

                await _repository.AsDeleteable().Where(x => delIdList.Contains(x.Id)).ExecuteCommandAsync();
            }
        }
        else
        {
            var roleEntity = await _repository.AsSugarClient().Queryable<RoleEntity>().FirstAsync(x => x.Id.Equals(objectId));
            if (roleEntity != null && roleEntity.Type != CommonConst.RelationObjectUser)
            {
                var objectIdList = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => x.RoleId.Equals(objectId)).Select(x => x.ObjectId).ToListAsync();

                foreach(var objId in objectIdList)
                {
                    if (roleEntity.Type.Equals(CommonConst.RelationObjectOrganize))
                    {
                        var orgChildIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => !x.Id.Equals(objId) && x.OrganizeIdTree.Contains(objId)).Select(x => x.Id).ToListAsync();
                        if (orgChildIds.Any())
                        {
                            var orgRoleIds = await _repository.AsSugarClient().Queryable<RoleEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.RoleId == a.Id && a.DeleteMark == null))
                                .Where((a, b) => b.ObjectId.Equals(objId) && b.RoleId != objectId).Select((a, b) => a.Id).ToListAsync();
                            var orgChildIdsItemList = await _repository.AsQueryable().Where(x => orgChildIds.Contains(x.ObjectId)).Select(x => new { x.Id, x.ItemId }).ToListAsync();
                            var orgRoleIdsItemList = await _repository.AsQueryable().Where(x => orgRoleIds.Contains(x.ObjectId)).Select(x => new { x.Id, x.ItemId }).ToListAsync();

                            // 过滤删除授权id
                            var delIdList = orgChildIdsItemList
                                .Where(x => !systemIds.Contains(x.ItemId))
                                .Where(x => !input.module.Contains(x.ItemId))
                                .Where(x => !input.button.Contains(x.ItemId))
                                .Where(x => !input.column.Contains(x.ItemId))
                                .Where(x => !input.form.Contains(x.ItemId))
                                .Where(x => !input.resource.Contains(x.ItemId))
                                .Where(x => !orgRoleIdsItemList.Select(xx => xx.ItemId).Contains(x.ItemId))
                                .Select(x => x.Id).ToList();
                            await _repository.AsDeleteable().Where(x => delIdList.Contains(x.Id)).ExecuteCommandAsync();
                        }
                    }

                    var posChildIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => !x.Id.Equals(objId) && (x.OrganizeId.Equals(objId) || x.PositionIdTree.Contains(objId))).Select(x => x.Id).ToListAsync();

                    if (posChildIds.Any())
                    {
                        var posRoleIds = await _repository.AsSugarClient().Queryable<RoleEntity, RoleRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.RoleId == a.Id && a.DeleteMark == null))
                            .Where((a, b) => b.ObjectId.Equals(objId) && b.RoleId != objectId).Select((a, b) => a.Id).ToListAsync();
                        var orgChildIdsItemList = await _repository.AsQueryable().Where(x => posChildIds.Contains(x.ObjectId)).Select(x => new { Id = x.Id, ItemId = x.ItemId }).ToListAsync();
                        var orgRoleIdsItemList = await _repository.AsQueryable().Where(x => posRoleIds.Contains(x.ObjectId)).Select(x => new { Id = x.Id, ItemId = x.ItemId }).ToListAsync();

                        // 过滤删除授权id
                        var delIdList = orgChildIdsItemList
                            .Where(x => !systemIds.Contains(x.ItemId))
                            .Where(x => !input.module.Contains(x.ItemId))
                            .Where(x => !input.button.Contains(x.ItemId))
                            .Where(x => !input.column.Contains(x.ItemId))
                            .Where(x => !input.form.Contains(x.ItemId))
                            .Where(x => !input.resource.Contains(x.ItemId))
                            .Where(x => !orgRoleIdsItemList.Select(xx => xx.ItemId).Contains(x.ItemId))
                            .Select(x => x.Id).ToList();

                        await _repository.AsDeleteable().Where(x => delIdList.Contains(x.Id)).ExecuteCommandAsync();
                    }
                }
            }
        }

        // 删除相关权限
        await _repository.DeleteAsync(a => a.ObjectId == objectId && !a.ItemType.Equals(CommonConst.AuthorizeItemStanding) && !a.ItemType.Equals(CommonConst.AuthorizeItemFlow));

        if (authorizeList.Count > 0)
        {
            // 新增权限
            await _repository.AsSugarClient().Insertable(authorizeList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        // 刷新权限的在线用户
        await ForcedRefresh(new List<string>() { objectId });

        return new UnifyResult.RESTfulResult<object>()
        {
            code = 200,
            msg = input.module.Count == inputModuleCount ? "保存成功" : "部分添加成功,超出权限基数的未被保存"
        };
    }

    /// <summary>
    /// 批量设置权限.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("Data/Batch")]
    public async Task BatchData([FromBody] AuthorizeDataBatchInput input)
    {
        // 计算按钮、列表、资源三个集合内不包含菜单ID的差
        input.button = input.button.Except(input.module).ToList();
        input.column = input.column.Except(input.module).ToList();
        input.form = input.form.Except(input.module).ToList();
        input.resource = input.resource.Except(input.module).ToList();

        // 拼装权限集合
        List<AuthorizeEntity>? authorizeItemList = new List<AuthorizeEntity>();
        List<AuthorizeEntity>? authorizeObejctList = new List<AuthorizeEntity>();
        BatchAddAuthorizeEntity(ref authorizeItemList, input.systemIds, CommonConst.AuthorizeItemSystem, true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.module, CommonConst.AuthorizeItemModule, true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.button, CommonConst.AuthorizeItemButton, true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.column, CommonConst.AuthorizeItemColumn, true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.form, CommonConst.AuthorizeItemForm, true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.resource, CommonConst.AuthorizeItemResource, true);
        BatchAddAuthorizeEntity(ref authorizeObejctList, new List<string>() { input.permissionGroupId }, input.objectType, false);
        List<AuthorizeEntity>? data = new List<AuthorizeEntity>();
        SeveBatch(ref data, authorizeObejctList, authorizeItemList);

        // 获取已有权限集合
        List<AuthorizeEntity>? existingRoleData = await _repository.AsQueryable().Where(x => input.permissionGroupId.Equals(x.ObjectId) && x.ObjectType.Equals(input.objectType)).ToListAsync();

        // 计算新增菜单集合与已有权限集合差
        data = data.Except(existingRoleData).ToList();

        // 数据不为空添加
        if (data.Count > 0)
        {
            // 新增权限
            int num = await _repository.AsSugarClient().Insertable(data).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        // 刷新权限的在线用户
        await ForcedRefresh(new List<string>() { input.permissionGroupId });
    }

    /// <summary>
    /// 设置/更新功能权限.
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("Model/{itemId}")]
    public async Task UpdateModel(string itemId, [FromBody] AuthorizeModelInput input)
    {
        List<AuthorizeEntity>? authorizeList = new List<AuthorizeEntity>();

        try
        {
            _repository.AsSugarClient().Ado.BeginTran();

            // 权限组ID不为空
            if (input.objectId.Count > 0)
            {
                foreach (var item in input.objectId)
                {
                    AuthorizeEntity? entity = new AuthorizeEntity();
                    entity.ItemId = itemId;
                    entity.ItemType = input.itemType;
                    entity.ObjectId = item;
                    entity.ObjectType = input.objectType;
                    authorizeList.Add(entity);
                }

                // 删除除了门户外的相关权限
                await _repository.DeleteAsync(a => a.ItemId == itemId);

                // 新增权限
                await _repository.AsSugarClient().Insertable(authorizeList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
            else
            {
                // 删除除了门户外的相关权限
                await _repository.DeleteAsync(a => a.ItemId == itemId);
            }

            _repository.AsSugarClient().Ado.CommitTran();
        }
        catch
        {
            _repository.AsSugarClient().Ado.RollbackTran();
        }

        if (input.objectId.Any()) await ForcedRefresh(input.objectId); // 刷新权限的在线用户
    }

    /// <summary>
    /// 设置模块列表展示字段权限.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("SetColumnsByModuleId")]
    public async Task SetColumnsByModuleId([FromBody] ColumnsPurviewDataUpInput input)
    {
        ColumnsPurviewEntity? entity = await _repository.AsSugarClient().Queryable<ColumnsPurviewEntity>().Where(x => x.ModuleId == input.moduleId).FirstAsync();
        if (entity == null) entity = new ColumnsPurviewEntity();
        entity.FieldList = input.fieldList;
        entity.ModuleId = input.moduleId;

        if (entity.Id.IsNotEmptyOrNull())
        {
            // 更新
            int newEntity = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }
        else
        {
            entity.Id = SnowflakeIdHelper.NextId();
            entity.CreatorTime = DateTime.Now;
            entity.CreatorUserId = _userManager.UserId;
            await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 保存打印模板权限.
    /// </summary>
    /// <param name="id">权限组id.</param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Print/{id}")]
    [UnitOfWork]
    public async Task SavePrintAuthorize(string id, [FromBody] AuthorizeSaveInput input)
    {
        var printeIdList = await _repository.AsSugarClient().Queryable<PrintDevEntity>()
            .Where(it => it.DeleteMark == null && it.State == 1 && it.CommonUse == 1 && it.VisibleType == 2)
            .Select(it => it.Id)
            .ToListAsync();

        var authorizeList = new List<AuthorizeEntity>();
        foreach (var item in input.ids)
        {
            if (printeIdList.Contains(item))
            {
                authorizeList.Add(new AuthorizeEntity
                {
                    Id = SnowflakeIdHelper.NextId(),
                    ItemType = CommonConst.AuthorizeItemPrint,
                    ItemId = item,
                    ObjectType = input.objectType,
                    ObjectId = id,
                    SortCode = 0,
                    CreatorTime = DateTime.Now,
                    CreatorUserId = _userManager.UserId
                });
            }
        }

        await _repository.AsDeleteable().Where(it => it.ObjectType.Equals(input.objectType) && it.ObjectId.Equals(id) && it.ItemType.Equals(CommonConst.AuthorizeItemPrint)).ExecuteCommandAsync();
        await _repository.AsInsertable(authorizeList).ExecuteCommandAsync();
    }

    /// <summary>
    /// 保存流程权限.
    /// </summary>
    /// <param name="id">权限组id.</param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Flow/{id}")]
    [UnitOfWork]
    public async Task SaveFlowAuthorize(string id, [FromBody] AuthorizeSaveInput input)
    {
        var authorizeList = new List<AuthorizeEntity>();
        foreach (var item in input.ids)
        {
            authorizeList.Add(new AuthorizeEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                ItemType = CommonConst.AuthorizeItemFlow,
                ItemId = item,
                ObjectType = input.objectType,
                ObjectId = id,
                SortCode = 0,
                CreatorTime = DateTime.Now,
                CreatorUserId = _userManager.UserId
            });
        }

        await _repository.AsDeleteable().Where(it => it.ObjectType.Equals(input.objectType) && it.ObjectId.Equals(id) && it.ItemType.Equals(CommonConst.AuthorizeItemFlow)).ExecuteCommandAsync();
        await _repository.AsInsertable(authorizeList).ExecuteCommandAsync();
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 添加权限接口参数组装.
    /// </summary>
    /// <param name="list">返回参数.</param>
    /// <param name="itemIds">权限数据id.</param>
    /// <param name="objectId">对象ID.</param>
    /// <param name="objectType">分类.</param>
    /// <param name="itemType">权限分类.</param>
    private void AddAuthorizeEntity(ref List<AuthorizeEntity> list, List<string> itemIds, string objectId, string objectType, string itemType)
    {
        foreach (string? item in itemIds)
        {
            AuthorizeEntity? entity = new AuthorizeEntity();
            entity.Id = SnowflakeIdHelper.NextId();
            entity.CreatorTime = DateTime.Now;
            entity.CreatorUserId = _userManager.UserId;
            entity.ItemId = item;
            entity.ObjectId = objectId;
            entity.ItemType = itemType;
            entity.ObjectType = objectType;
            entity.SortCode = itemIds.IndexOf(item);
            list.Add(entity);
        }
    }

    /// <summary>
    /// 批量添加权限接口参数组装.
    /// </summary>
    /// <param name="list">返回参数.</param>
    /// <param name="ids">来源数据.</param>
    /// <param name="type">来源类型.</param>
    /// <param name="isData">是否是权限数据.</param>
    private void BatchAddAuthorizeEntity(ref List<AuthorizeEntity> list, List<string> ids, string type, bool isData)
    {
        if (ids != null && ids.Count != 0)
        {
            if (isData)
            {
                foreach (string? item in ids)
                {
                    AuthorizeEntity? entity = new AuthorizeEntity();
                    entity.ItemId = item;
                    entity.ItemType = type;
                    list.Add(entity);
                }
            }
            else
            {
                foreach (string? item in ids)
                {
                    AuthorizeEntity? entity = new AuthorizeEntity();
                    entity.ObjectId = item;
                    entity.ObjectType = type;
                    list.Add(entity);
                }
            }
        }
    }

    /// <summary>
    /// 保存批量权限.
    /// </summary>
    /// <param name="list">返回list.</param>
    /// <param name="objectList">对象数据.</param>
    /// <param name="authorizeList">权限数据.</param>
    private void SeveBatch(ref List<AuthorizeEntity> list, List<AuthorizeEntity> objectList, List<AuthorizeEntity> authorizeList)
    {
        foreach (AuthorizeEntity? objectItem in objectList)
        {
            foreach (AuthorizeEntity entityItem in authorizeList)
            {
                AuthorizeEntity? entity = new AuthorizeEntity();
                entity.Id = SnowflakeIdHelper.NextId();
                entity.CreatorTime = DateTime.Now;
                entity.CreatorUserId = _userManager.UserId;
                entity.ItemId = entityItem.ItemId;
                entity.ItemType = entityItem.ItemType;
                entity.ObjectId = objectItem.ObjectId;
                entity.ObjectType = objectItem.ObjectType;
                entity.SortCode = authorizeList.IndexOf(entityItem);
                list.Add(entity);
            }
        }
    }

    /// <summary>
    /// 返回参数处理.
    /// </summary>
    /// <param name="output">返回参数.</param>
    /// <param name="list">返回参数数据.</param>
    /// <param name="checkList">已勾选的id.</param>
    /// <param name="parentId"></param>
    private void GetOutPutResult(ref AuthorizeDataOutput output, List<AuthorizeDataModelOutput> list, List<string> checkList, string parentId = "-1")
    {
        output.all = list.Select(l => l.id).ToList();
        output.ids = checkList.Intersect(output.all).ToList();
        output.list = list.OrderBy(x => x.sortCode).ToList().ToTree(parentId);
        SetResultDisabled(output.list, output.ids);
    }
    private void SetResultDisabled(List<AuthorizeDataModelOutput> dataList, List<string> ids)
    {
        foreach (var it in dataList)
        {
            if (it.children != null && it.children.Any())
            {
                var list = it.children.Adapt<List<AuthorizeDataModelOutput>>();
                SetResultDisabled(list, ids);
                if (!list.Any(x => !x.disabled)) it.disabled = true;
                it.children = list.Adapt<List<object>>();
            }
        }
    }

    private List<string> GetOutPutByChild(List<ModuleEntity> list, List<string> pids)
    {
        var res = new List<string>();
        var pList = list.Where(x => pids.Contains(x.Id)).Select(x => x.Id).ToList();
        res.AddRange(pList);
        if (pList.Any()) res.AddRange(GetOutPutByChild(list, list.Where(x => pids.Contains(x.Id)).Select(x => x.ParentId).ToList()));
        return res;
    }

    /// <summary>
    /// 强制权限组下的所有用户刷新页面.
    /// </summary>
    /// <param name="permissionIds">权限 Ids.</param>
    /// <returns></returns>
    private async Task ForcedRefresh(List<string> permissionIds)
    {
        var userIds = await _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => permissionIds.Contains(x.RoleId) && !x.ObjectType.Equals(CommonConst.RelationObjectOrganize)).Select(x => x.ObjectId).ToListAsync();
        userIds.AddRange(await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => permissionIds.Contains(x.ObjectId) || userIds.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync());
        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);

        if (userIds.Any() && list != null)
        {
            var onlineUserList = list.Where(it => it.tenantId == _userManager.TenantId && userIds.Contains(it.userId)).ToList();
            if (onlineUserList != null && onlineUserList.Any())
            {
                foreach (var onlineUser in onlineUserList)
                {
                    await _imHandler.SendMessageAsync(onlineUser.connectionId, new { method = "refresh", msg = "您的权限发生变更,请刷新界面！" }.ToJsonString());
                }
            }
        }
    }

    #endregion

}