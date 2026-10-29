using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.System.Portal;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.Portal;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace JNPF.VisualDev;

/// <summary>
///  业务实现：门户设计.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "Portal", Order = 173)]
[Route("api/visualdev/[controller]")]
public class PortalService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<PortalEntity> _repository;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

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
    /// 初始化一个<see cref="PortalService"/>类型的新实例.
    /// </summary>
    public PortalService(
        ISqlSugarRepository<PortalEntity> repository,
        IUserManager userManager,
        IBillRullService billRuleService,
        IFileManager fileManager,
        ICacheManager cacheManager)
    {
        _repository = repository;
        _userManager = userManager;
        _billRuleService = billRuleService;
        _fileManager = fileManager;
        _cacheManager = cacheManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns>返回列表.</returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PortalListQueryInput input)
    {
        var data = await _repository.AsQueryable()
           .WhereIF(input.keyword.IsNotEmptyOrNull(), p => p.FullName.Contains(input.keyword) || p.EnCode.Contains(input.keyword))
           .WhereIF(input.category.IsNotEmptyOrNull(), p => p.Category == input.category)
           .WhereIF(input.type.IsNotEmptyOrNull(), p => p.Type == input.type)
           .WhereIF(input.enabledLock.IsNotEmptyOrNull(), p => p.EnabledLock == input.enabledLock)
           .WhereIF(input.isRelease.IsNotEmptyOrNull(), p => p.State == input.isRelease)
           .Where(p => p.DeleteMark == null && p.SystemId == _userManager.SystemId)
           .OrderBy(p => p.SortCode)
           .OrderBy(p => p.CreatorTime, OrderByType.Desc)
           .OrderByIF(!input.keyword.IsNullOrEmpty(), p => p.LastModifyTime, OrderByType.Desc)
           .Select(p => new PortalListOutput
           {
               id = p.Id,
               fullName = p.FullName,
               enCode = p.EnCode,
               category = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(it => it.Id.Equals(p.Category)).Select(it => it.FullName),
               creatorTime = p.CreatorTime,
               creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(it => it.Id.Equals(p.CreatorUserId)).Select(it => SqlFunc.MergeString(it.RealName, "/", it.Account)),
               lastModifyTime = p.LastModifyTime,
               state = p.State,
               isRelease = p.State,
               enabledLock = p.EnabledLock,
               type = p.Type,
               sortCode = p.SortCode,
               platformRelease = p.PlatformRelease,
           })
           .ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<PortalListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取门户下拉.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector([FromQuery] PortalSelectInput input)
    {
        var data = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.SystemId.Equals(_userManager.SystemId))
            .OrderBy(it => it.SortCode)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .OrderBy(it => it.LastModifyTime, OrderByType.Desc)
            .Select(it => new PortalSelectOutput
            {
                id = it.Id,
                fullName = it.FullName,
                parentId = it.Category
            })
            .ToListAsync();

        var parentIds = data.Select(it => it.parentId).Distinct().ToList();
        var treeList = new List<PortalSelectOutput>();
        if (parentIds.Any())
        {
            treeList = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().In(it => it.Id, parentIds.ToArray())
                .Where(d => d.DeleteMark == null && d.EnabledMark == 1)
                .OrderBy(o => o.SortCode)
                .OrderBy(o => o.CreatorTime, OrderByType.Desc)
                .Select(d => new PortalSelectOutput
                {
                    id = d.Id,
                    parentId = "0",
                    fullName = d.FullName
                }).ToListAsync();
        }

        return new { list = treeList.Union(data).ToList().ToTree("0") };
    }

    /// <summary>
    /// 获取门户侧边框列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector/Menu")]
    public async Task<dynamic> GetSelector()
    {
        var system = await _repository.AsSugarClient().Queryable<SystemEntity>().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(_userManager.SystemId));
        var isWeb = _userManager.UserOrigin.Equals("pc") ? true : false;
        var category = isWeb ? "Web" : "App";
        var portalIds = isWeb ? system.PortalId : system.AppPortalId;
        if (!_userManager.IsAdministrator)
        {
            var permissionIds = _userManager.PermissionIds;
            var items = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                .Where(x => x.ItemType == CommonConst.AuthorizeItemModule && permissionIds.Contains(x.ObjectId))
                .Select(x => x.ItemId)
                .ToListAsync();
            var ids = portalIds.Split(",").ToList().Where(x => items.Contains(x)).ToList();
            portalIds = string.Join(",", ids);
        }

        var list = await _repository.AsSugarClient().Queryable<ModuleEntity>()
            .Where(x => x.DeleteMark == null && x.Category == category && portalIds.Contains(x.Id))
            .Select(x => new PortalSelectMenuOutput
            {
                id = x.Id,
                fullName = x.FullName
            }).ToListAsync();

        return new { list = list };
    }

    /// <summary>
    /// 获取门户信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var systemList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.DeleteMark == null).ToListAsync();

        var data = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.Id == id)
            .Select(it => new PortalInfoOutput()
            {
                id = it.Id,
                category = it.Category,
                customUrl = it.CustomUrl,
                description = it.Description,
                enCode = it.EnCode,
                enabledLock = it.EnabledLock,
                fullName = it.FullName,
                linkType = it.LinkType,
                sortCode = it.SortCode,
                type = it.Type,
                formData = SqlFunc.Subqueryable<PortalDataEntity>().EnableTableFilter().Where(x => x.DeleteMark == null && x.Type.Equals("model") && x.PortalId.Equals(id)).Select(x => x.FormData),
                platformRelease = it.PlatformRelease,
                pcIsRelease = SqlFunc.Subqueryable<ModuleEntity>().EnableTableFilter().Where(x => x.DeleteMark == null && systemList.Select(s => s.Id).Contains(x.SystemId) && x.Category.Equals("Web") && x.PropertyJson.Contains(it.Id)).Any() ? 1 : 0,
                appIsRelease = SqlFunc.Subqueryable<ModuleEntity>().EnableTableFilter().Where(x => x.DeleteMark == null && systemList.Select(s => s.Id).Contains(x.SystemId) && x.Category.Equals("App") && x.PropertyJson.Contains(it.Id)).Any() ? 1 : 0,
            })
            .FirstAsync();

        var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null).ToListAsync();
        var pcModuleList = moduleList.Where(it => it.Category.Equals("Web")).ToList();
        var appModuleList = moduleList.Where(it => it.Category.Equals("App")).ToList();

        var pcList = new List<string>();
        foreach (var module in pcModuleList.Where(it => it.PropertyJson.Contains(data.id)))
        {
            GetReleaseName(pcList, pcModuleList, systemList, module, string.Empty);
        }
        data.pcReleaseName = string.Join("；", pcList);

        var appList = new List<string>();
        foreach (var module in appModuleList.Where(it => it.PropertyJson.Contains(data.id)))
        {
            GetReleaseName(appList, appModuleList, systemList, module, string.Empty);
        }
        data.appReleaseName = string.Join("；", appList);

        return data;
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("{id}/auth")]
    public async Task<dynamic> GetInfoAuth(string id, [FromQuery] PortalAuthInput input)
    {
        var entity = new PortalInfoAuthOutput();
        var portalId = string.Empty;
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(id)))
            portalId = id;
        else
            portalId = (await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => x.DeleteMark == null && x.Id.Equals(id)).Select(x => x.PropertyJson).FirstAsync())?.ToObject<JObject>()?["moduleId"]?.ToString();

        if (portalId.IsNotEmptyOrNull())
        {
            var systemId = _userManager.SystemId;

            // 判断子门户是否存在
            if (!await _repository.AsSugarClient().Queryable<PortalDataEntity>().AnyAsync(it => it.DeleteMark == null && it.Platform.Equals(input.platform) && it.Type.Equals("custom") && it.SystemId.Equals(systemId) && it.PortalId.Equals(portalId) && it.CreatorUserId.Equals(_userManager.UserId)))
            {
                var data = await _repository.AsSugarClient().Queryable<PortalDataEntity>()
                    .Where(it => it.DeleteMark == null && it.Type.Equals("release") && it.PortalId.Equals(portalId) && it.SystemId == null)
                    .FirstAsync();
                if (data.IsNotEmptyOrNull())
                {
                    data.Id = SnowflakeIdHelper.NextId();
                    data.Type = "custom";
                    data.Platform = input.platform;
                    data.SystemId = systemId;
                    data.CreatorTime = DateTime.Now;
                    data.CreatorUserId = _userManager.UserId;
                    await _repository.AsSugarClient().Insertable(data).ExecuteCommandAsync();
                }
            }

            entity = await _repository.AsQueryable()
                .Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.Id.Equals(portalId))
                .Select(it => new PortalInfoAuthOutput
                {
                    type = it.Type,
                    customUrl = it.CustomUrl,
                    linkType = it.LinkType,
                    enabledLock = it.EnabledLock,
                    formData = SqlFunc.Subqueryable<PortalDataEntity>().EnableTableFilter().Where(x => x.DeleteMark == null && x.CreatorUserId.Equals(_userManager.UserId) && x.Platform.Equals(input.platform) && x.Type.Equals("custom") && x.PortalId.Equals(portalId) && x.SystemId.Equals(systemId)).Select(x => x.FormData)
                })
                .FirstAsync();
        }
        else
        {
            throw Oops.Oh(ErrorCode.COM1007);
        }

        return entity;
    }

    #endregion

    #region Post

    /// <summary>
    /// 门户导出.
    /// </summary>
    /// <param name="modelId"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/Actions/Export")]
    public async Task<dynamic> ActionsExportData(string modelId)
    {
        // 模板实体
        var templateEntity = await _repository.AsQueryable()
            .Where(it => it.Id == modelId)
            .Select<PortalExportOutput>()
            .FirstAsync();
        templateEntity.formData = await _repository.AsSugarClient().Queryable<PortalDataEntity>()
            .Where(it => it.DeleteMark == null && it.PortalId.Equals(modelId) && it.Type.Equals("model"))
            .Select(it => it.FormData)
            .FirstAsync();

        string? jsonStr = templateEntity.ToJsonString();
        return await _fileManager.Export(jsonStr, templateEntity.fullName, ExportFileType.vp);
    }

    /// <summary>
    /// 门户导入.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type">识别重复（0：跳过，1：追加）.</param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    public async Task ActionsImportData(IFormFile file, int type)
    {
        string? fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.vp.ToString())) throw Oops.Oh(ErrorCode.D3006);
        string? josn = _fileManager.Import(file);
        PortalExportOutput? templateEntity;
        try
        {
            templateEntity = josn.ToObject<PortalExportOutput>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (templateEntity == null) throw Oops.Oh(ErrorCode.D3006);
        if (templateEntity != null && templateEntity.formData.IsNotEmptyOrNull() && templateEntity.formData.IndexOf("layoutId") <= 0)
            throw Oops.Oh(ErrorCode.D3006);

        if (templateEntity.systemId.IsNullOrEmpty() || !templateEntity.systemId.Equals(_userManager.SystemId))
        {
            templateEntity.id = SnowflakeIdHelper.NextId();
            templateEntity.enCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePortal);
        }

        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(templateEntity.id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(templateEntity.enCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.SystemId.Equals(_userManager.SystemId) && it.FullName.Equals(templateEntity.fullName))) errorList.Add("名称");

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
                templateEntity.id = SnowflakeIdHelper.NextId();
                templateEntity.fullName = string.Format("{0}.副本{1}", templateEntity.fullName, random);
                templateEntity.enCode += random;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        var portalEntity = templateEntity.Adapt<PortalEntity>();
        portalEntity.SystemId = _userManager.SystemId;
        portalEntity.State = 0;
        portalEntity.EnabledMark = 0;
        portalEntity.CreatorTime = DateTime.Now;
        portalEntity.CreatorUserId = _userManager.UserId;
        try
        {
            var storModuleModel = _repository.AsSugarClient().Storageable(portalEntity).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }

        // 门户数据
        var dataEntity = new PortalDataEntity()
        {
            Id = SnowflakeIdHelper.NextId(),
            PortalId = portalEntity.Id,
            FormData = templateEntity.formData,
            Type = "model",
            CreatorTime = DateTime.Now,
            CreatorUserId = _userManager.UserId
        };
        try
        {
            await _repository.AsSugarClient().Insertable(dataEntity).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }

    /// <summary>
    /// 新建门户信息.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task<string> Create([FromBody] PortalCrInput input)
    {
        if (string.IsNullOrEmpty(input.category)) throw Oops.Oh(ErrorCode.D1901);
        if (string.IsNullOrEmpty(input.fullName)) throw Oops.Oh(ErrorCode.D1902);
        if (await _repository.AsQueryable().Where(it => it.DeleteMark == null && it.SystemId.Equals(_userManager.SystemId) && it.FullName.Equals(input.fullName)).AnyAsync())
            throw Oops.Oh(ErrorCode.D1915);
        if (await _repository.AsQueryable().Where(it => it.DeleteMark == null && it.EnCode.Equals(input.enCode)).AnyAsync())
            throw Oops.Oh(ErrorCode.D1916);

        var entity = input.Adapt<PortalEntity>();
        entity.SystemId = _userManager.SystemId;
        entity.State = 0;
        entity.EnabledMark = 0;
        if (entity.Type.Equals(1)) entity.EnabledLock = null;
        if (entity.EnCode.IsNullOrEmpty()) entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePortal);
        await _repository.AsInsertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

        var dataEntity = new PortalDataEntity()
        {
            Id = SnowflakeIdHelper.NextId(),
            PortalId = entity.Id,
            FormData = input.formData,
            CreatorTime = DateTime.Now,
            CreatorUserId = _userManager.UserId,
            Type = "model"
        };
        var isOk = await _repository.AsSugarClient().Insertable(dataEntity).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.COM1000);

        // 确定并设计
        return entity.Id;
    }

    /// <summary>
    /// 修改接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] PortalUpInput input)
    {
        if (await _repository.AsQueryable().Where(it => it.DeleteMark == null && it.SystemId.Equals(_userManager.SystemId) && !it.Id.Equals(id) && it.FullName.Equals(input.fullName)).AnyAsync())
            throw Oops.Oh(ErrorCode.D1915);
        else if (await _repository.AsQueryable().Where(it => it.DeleteMark == null && !it.Id.Equals(id) && it.EnCode.Equals(input.enCode)).AnyAsync())
            throw Oops.Oh(ErrorCode.D1916);

        var entity = input.Adapt<PortalEntity>();
        if (entity.Type.Equals(1)) entity.EnabledLock = null;
        if (entity.EnCode.IsNullOrEmpty()) entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePortal);

        var state = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.Id.Equals(id))
            .Select(it => it.State)
            .FirstAsync();
        if (state.Equals(1)) entity.State = 2;

        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;

        int isOk = await _repository.AsSugarClient().Updateable(entity)
            .UpdateColumns(it => new {
                it.CustomUrl,
                it.EnCode,
                it.LinkType,
                it.SortCode,
                it.FullName,
                it.Description,
                it.Type,
                it.Category,
                it.State,
                it.EnabledLock,
                it.LastModifyTime,
                it.LastModifyUserId
            }).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();

        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.COM1001);

        if (input.formData.IsNotEmptyOrNull())
        {
            await _repository.AsSugarClient().Updateable<PortalDataEntity>()
                .Where(it => it.DeleteMark == null && it.PortalId.Equals(id) && it.Type.Equals("model"))
                .SetColumns(it => new PortalDataEntity()
                {
                    FormData = input.formData,
                    LastModifyTime = SqlFunc.GetDate(),
                    LastModifyUserId = _userManager.UserId
                })
                .ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 删除接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.AsQueryable()
            .Where(it => it.Id == id && it.DeleteMark == null)
            .FirstAsync();
        _ = entity ?? throw Oops.Oh(ErrorCode.COM1005);

        entity.DeleteMark = 1;
        entity.DeleteTime = DateTime.Now;
        entity.DeleteUserId = _userManager.UserId;
        await _repository.AsSugarClient().Updateable(entity).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();

        var isOk = await _repository.AsSugarClient().Updateable<PortalDataEntity>()
            .Where(it => it.DeleteMark == null && it.PortalId.Equals(id))
            .SetColumns(it => new PortalDataEntity()
            {
                DeleteMark = 1,
                DeleteTime = SqlFunc.GetDate(),
                DeleteUserId = _userManager.UserId
            })
            .ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        string? random = new Random().NextLetterAndNumberString(5);
        var entity = await _repository.AsQueryable()
            .Where(it => it.Id == id && it.DeleteMark == null)
            .FirstAsync();
        var newEntity = new PortalEntity()
        {
            Id = SnowflakeIdHelper.NextId(),
            CreatorTime = DateTime.Now,
            CreatorUserId = _userManager.UserId,
            FullName = entity.FullName + ".副本" + random,
            EnCode = entity.EnCode + random,
            Category = entity.Category,
            Description = entity.Description,
            EnabledMark = 0,
            EnabledLock = entity.EnabledLock,
            State = 0,
            SortCode = entity.SortCode,
            Type = entity.Type,
            LinkType = entity.LinkType,
            CustomUrl = entity.CustomUrl,
            SystemId = _userManager.SystemId
        };

        var dataEntity = await _repository.AsSugarClient().Queryable<PortalDataEntity>()
            .Where(it => it.DeleteMark == null && it.PortalId.Equals(id) && it.Type.Equals("model"))
            .FirstAsync();
        var newDataEntity = new PortalDataEntity()
        {
            Id = SnowflakeIdHelper.NextId(),
            CreatorTime = DateTime.Now,
            CreatorUserId = _userManager.UserId,
            PortalId = newEntity.Id,
            FormData = dataEntity.FormData,
            Type = "model"
        };

        try
        {
            await _repository.AsSugarClient().Insertable(newEntity).ExecuteCommandAsync();
            await _repository.AsSugarClient().Insertable(newDataEntity).ExecuteCommandAsync();
        }
        catch
        {
            if (entity.FullName.Length >= 100 || entity.EnCode.Length >= 50) throw Oops.Oh(ErrorCode.COM1009); // 数据长度超过 字段设定长度
            else throw;
        }
    }

    /// <summary>
    /// 设置默认门户.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="platform"></param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/SetDefault")]
    public async Task SetDefault(string id, string platform)
    {
        var portalDic = (await _repository.AsSugarClient().Queryable<UserEntity>()
            .Where(it => it.Id.Equals(_userManager.UserId))
            .Select(it => it.PortalId)
            .FirstAsync())
            .ToObject<Dictionary<string, string>>();

        var key = string.Format("{0}:{1}", platform, _userManager.SystemId);
        portalDic[key] = id;

        var portalId = portalDic.ToJsonString();
        var isOk = await _repository.AsSugarClient().Updateable<UserEntity>()
            .Where(it => it.Id.Equals(_userManager.UserId))
            .SetColumns(it => new UserEntity()
            {
                PortalId = portalId,
                LastModifyTime = SqlFunc.GetDate(),
                LastModifyUserId = _userManager.UserId
            })
            .ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D5014);
    }

    /// <summary>
    /// 实时保存门户.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("Custom/Save/{id}")]
    public async Task SavePortal(string id, [FromBody] PortalSaveInput input)
    {
        var portalId = string.Empty;
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(id)))
            portalId = id;
        else
            portalId = (await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => x.DeleteMark == null && x.Id.Equals(id)).Select(x => x.PropertyJson).FirstAsync())?.ToObject<JObject>()?["moduleId"]?.ToString();

        // 修改子门户的数据
        var isOk = await _repository.AsSugarClient().Updateable<PortalDataEntity>()
            .Where(it => it.DeleteMark == null && it.Platform == "Web" && it.Type.Equals("custom") && it.PortalId.Equals(portalId) && it.SystemId.Equals(_userManager.SystemId) && it.CreatorUserId.Equals(_userManager.UserId))
            .SetColumns(it => new PortalDataEntity()
            {
                FormData = input.formData,
                LastModifyTime = SqlFunc.GetDate(),
                LastModifyUserId = _userManager.UserId
            })
            .ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D1906);
    }

    /// <summary>
    /// 同步门户.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("Actions/release/{id}")]
    [UnitOfWork]
    public async Task SyncPortal(string id, [FromBody] PortalSyncInput input)
    {
        var entity = await _repository.AsQueryable().FirstAsync(it => it.DeleteMark == null && it.Id.Equals(id));
        var sysIdList = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.DeleteMark == null).Select(it => it.Id).ToListAsync();
        List<PortalDataEntity> portalDataList = new List<PortalDataEntity>();

        // 主门户数据
        var modelData = await _repository.AsSugarClient().Queryable<PortalDataEntity>()
            .Where(it => it.DeleteMark == null && it.Type.Equals("model") && it.PortalId.Equals(id))
            .FirstAsync();

        // 发布门户数据
        var releaseData = await _repository.AsSugarClient().Queryable<PortalDataEntity>()
            .Where(it => it.DeleteMark == null && it.Type.Equals("release") && it.PortalId.Equals(id))
            .FirstAsync();
        if (releaseData.IsNotEmptyOrNull())
        {
            releaseData.FormData = modelData.FormData;
            releaseData.LastModifyTime = DateTime.Now;
            releaseData.LastModifyUserId = _userManager.UserId;
            await _repository.AsSugarClient().Updateable(releaseData)
                .UpdateColumns(it => new { it.FormData, it.LastModifyTime, it.LastModifyUserId })
                .ExecuteCommandAsync();
        }
        else
        {
            var newReleaseData = modelData.Adapt<PortalDataEntity>();
            newReleaseData.Id = SnowflakeIdHelper.NextId();
            newReleaseData.Type = "release";
            await _repository.AsSugarClient().Insertable(newReleaseData).ExecuteCommandAsync();
        }

        #region 菜单门户

        var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null).ToListAsync();
        var allModuleList = moduleList.Where(it => it.PropertyJson.Contains(id)).ToList();
        var portalModuleList = new List<ModuleEntity>();
        if (input.pc == 1)
        {
            if (!input.pcModuleParentId.Any() && !await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && sysIdList.Contains(it.SystemId) && it.Category.Equals("Web") && it.PropertyJson.Contains(id)))
                throw Oops.Oh(ErrorCode.D4017);

            // 新发布的菜单
            var dic = new List<Dictionary<string, string>>();
            foreach (var item in input.pcModuleParentId)
            {
                if (sysIdList.Contains(item))
                {
                    dic.Add(new Dictionary<string, string> { { item, "-1" } });
                }
                else
                {
                    var module = moduleList.Find(it => it.Id.Equals(item));
                    if (module.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D4021);
                    dic.Add(new Dictionary<string, string> { { module.SystemId, module.Id } });
                }
            }

            foreach (var item in dic)
            {
                var data = item.First();
                var fullName = entity.FullName;
                var enCode = entity.EnCode + new Random().NextLetterAndNumberString(5);

                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.FullName == fullName && x.ParentId == data.Value && x.SystemId == data.Key && x.Category == "Web" && x.DeleteMark == null))
                    throw Oops.Oh(ErrorCode.COM1032);
                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.EnCode == enCode && x.Category == "Web" && x.DeleteMark == null))
                    throw Oops.Oh(ErrorCode.COM1031);

                var module = new ModuleEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    FullName = fullName,
                    EnCode = enCode,
                    UrlAddress = string.Format("portal/{0}", enCode),
                    ParentId = data.Value,
                    SystemId = data.Key,
                    ModuleId = id,
                    PropertyJson = new { moduleId = id, iconBackgroundColor = string.Empty, isTree = 0 }.ToJsonString(),
                    Icon = "icon-ym icon-ym-webForm",
                    Category = "Web",
                    IsButtonAuthorize = 0,
                    IsColumnAuthorize = 0,
                    IsDataAuthorize = 0,
                    IsFormAuthorize = 0,
                    EnabledMark = 1,
                    CreatorTime = DateTime.Now,
                    CreatorUserId = _userManager.UserId,
                    SortCode = 999,
                    Type = 8
                };

                await _repository.AsSugarClient().Insertable(module).ExecuteCommandAsync();
            }
        }
        if (input.app == 1)
        {
            if (!input.appModuleParentId.Any() && !await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && sysIdList.Contains(it.SystemId) && it.Category.Equals("App") && it.PropertyJson.Contains(id)))
                throw Oops.Oh(ErrorCode.D4017);

            // 新发布的菜单
            var dic = new List<Dictionary<string, string>>();
            foreach (var item in input.appModuleParentId)
            {
                if (sysIdList.Contains(item))
                {
                    dic.Add(new Dictionary<string, string> { { item, "-1" } });
                }
                else
                {
                    var module = moduleList.Find(it => it.Id.Equals(item));
                    if (module.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D4021);
                    dic.Add(new Dictionary<string, string> { { module.SystemId, module.Id } });
                }
            }

            foreach (var item in dic)
            {
                var data = item.First();
                var fullName = entity.FullName;
                var enCode = entity.EnCode + new Random().NextLetterAndNumberString(5);

                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.FullName == fullName && x.ParentId == data.Value && x.SystemId == data.Key && x.Category == "App" && x.DeleteMark == null))
                    throw Oops.Oh(ErrorCode.COM1032);
                if (await _repository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.EnCode == enCode && x.Category == "App" && x.DeleteMark == null))
                    throw Oops.Oh(ErrorCode.COM1031);

                var module = new ModuleEntity()
                {
                    Id = SnowflakeIdHelper.NextId(),
                    FullName = fullName,
                    EnCode = enCode,
                    UrlAddress = enCode,
                    ParentId = data.Value,
                    SystemId = data.Key,
                    ModuleId = id,
                    PropertyJson = new { moduleId = id, iconBackgroundColor = string.Empty, isTree = 0 }.ToJsonString(),
                    Icon = "icon-ym icon-ym-webForm",
                    Category = "App",
                    IsButtonAuthorize = 0,
                    IsColumnAuthorize = 0,
                    IsDataAuthorize = 0,
                    IsFormAuthorize = 0,
                    EnabledMark = 1,
                    CreatorTime = DateTime.Now,
                    CreatorUserId = _userManager.UserId,
                    SortCode = 999,
                    Type = 8
                };

                await _repository.AsSugarClient().Insertable(module).ExecuteCommandAsync();
            }
        }

        #endregion

        var updateList = new List<string>();
        if (input.pc.Equals(1)) updateList.Add("Web");
        if (input.app.Equals(1)) updateList.Add("App");

        foreach (var item in updateList)
        {
            var dataList = await _repository.AsSugarClient().Queryable<PortalDataEntity>()
                .Where(it => it.DeleteMark == null && it.PortalId.Equals(id) && it.Platform.Equals(item) && it.Type.Equals("custom"))
                .ToListAsync();

            foreach (var upData in dataList)
            {
                upData.FormData = modelData.FormData;
                upData.LastModifyTime = DateTime.Now;
                upData.LastModifyUserId = _userManager.UserId;

                portalDataList.Add(upData);
            }
        }

        // 更新门户数据
        await _repository.AsSugarClient().Updateable(portalDataList)
            .UpdateColumns(it => new {
                it.FormData,
                it.LastModifyTime,
                it.LastModifyUserId
            }).ExecuteCommandAsync();

        // 更新门户发布状态
        await _repository.AsUpdateable()
            .SetColumns(it => new PortalEntity
            {
                State = 1,
                EnabledMark = 1,
                PlatformRelease = input.platformRelease
            })
            .Where(it => it.DeleteMark == null && it.Id.Equals(id)).ExecuteCommandAsync();
    }

    #endregion

    #region Private

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
