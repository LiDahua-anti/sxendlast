using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.Authorize;
using JNPF.Common.Models.Module;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.SensitiveDetection;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Engine;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDev;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Entitys.Model;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Senparc.Weixin.MP.AdvancedAPIs.GroupMessage;
using SqlSugar;
using System.Text.RegularExpressions;

namespace JNPF.VisualDev;

/// <summary>
/// 可视化开发基础 .
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "Base", Order = 171)]
[Route("api/visualdev/[controller]")]
public class VisualDevService : IVisualDevService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevEntity> _visualDevRepository;

    /// <summary>
    /// 切库.
    /// </summary>
    private readonly IDataBaseManager _changeDataBase;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 在线开发运行服务.
    /// </summary>
    private readonly RunService _runService;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 多租户事务.
    /// </summary>
    private readonly ITenant _db;

    private readonly ISensitiveDetectionProvider _sensitiveDetectionProvider;

    /// <summary>
    /// 初始化一个<see cref="VisualDevService"/>类型的新实例.
    /// </summary>
    public VisualDevService(
        ISqlSugarRepository<VisualDevEntity> visualDevRepository,
        IDataBaseManager changeDataBase,
        IUserManager userManager,
        RunService runService,
        IBillRullService billRuleService,
        ISqlSugarClient context,
        ISensitiveDetectionProvider sensitiveDetectionProvider)
    {
        _visualDevRepository = visualDevRepository;
        _userManager = userManager;
        _runService = runService;
        _billRuleService = billRuleService;
        _changeDataBase = changeDataBase;
        _db = context.AsTenant();
        _sensitiveDetectionProvider = sensitiveDetectionProvider;
    }

    #region Get

    /// <summary>
    /// 获取功能列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("List")]
    public async Task<dynamic> GetIntegrateList([FromQuery] VisualDevListQueryInput input)
    {
        var whereLambda = LinqExpression.And<VisualDevReleaseEntity>();
        if (input.webType.IsNotEmptyOrNull())
        {
            if (input.webType == 1)
                whereLambda = whereLambda.And(m => m.WebType == 2);
            else if (input.webType == 2)
                whereLambda = whereLambda.And(m => m.WebType == 1 || m.WebType == 2);
            else
                whereLambda = whereLambda.And(m => m.WebType == 1 || m.WebType == 2 || m.WebType == 4);
        }

        var data = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>()
           .WhereIF(!string.IsNullOrEmpty(input.keyword), it => it.FullName.Contains(input.keyword))
           .WhereIF(!string.IsNullOrEmpty(input.category), it => it.Category == input.category)
           .WhereIF(input.type.IsNotEmptyOrNull(), it => it.Type == input.type)
           .Where(whereLambda)
           .Where(it => it.DeleteMark == null && it.SystemId == _userManager.SystemId && (it.Type.Equals(1) || (it.Type.Equals(2) && it.EnableFlow.Equals(1))))
           .OrderBy(it => it.SortCode, OrderByType.Asc)
           .OrderBy(it => it.CreatorTime, OrderByType.Desc)
           .OrderByIF(!input.keyword.IsNullOrEmpty(), it => it.LastModifyTime, OrderByType.Desc)
           .Select(it => new VisualDevIntergrateListOutput
           {
               id = it.Id,
               fullName = it.FullName,
               enCode = it.EnCode,
               type = it.Type
           }).ToPagedListAsync(input.currentPage, input.pageSize);

        if (input.flowStart)
        {
            var flowIds = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(x => x.Status == 1 && x.DeleteMark == null).Select(x => x.Id).ToListAsync();
            var formIds = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => flowIds.Contains(x.FlowId) && x.NodeType == "start" && x.DeleteMark == null).Select(x => x.FormId).ToListAsync();
            if (formIds.Any())
            {
                foreach (var item in data.list)
                {
                    if (formIds.Contains(item.id) && item.type == 2) item.isQuote = 1;
                }
            }
        }

        return PageResult<VisualDevIntergrateListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取功能列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] VisualDevListQueryInput input)
    {
        var data = await _visualDevRepository.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(input.keyword), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .WhereIF(!string.IsNullOrEmpty(input.category), a => a.Category == input.category)
            .WhereIF(input.isRelease.IsNotEmptyOrNull(), a => input.isRelease.Contains(a.State.ToString()))
            .WhereIF(input.webType.IsNotEmptyOrNull() && input.webType.Equals(2), a => a.WebType == 1 || a.WebType == 2)
            .WhereIF(input.webType.IsNotEmptyOrNull() && input.webType.Equals(4), a => a.WebType == 4)
            .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Type == input.type)
            .Where(a => a.DeleteMark == null && a.SystemId == _userManager.SystemId)
            .OrderBy(a => a.SortCode, OrderByType.Asc)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!input.keyword.IsNullOrEmpty(), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new VisualDevListOutput
            {
                id = a.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                state = a.State,
                type = a.Type,
                webType = a.WebType,
                tables = a.Tables,
                creatorTime = a.CreatorTime,
                lastModifyTime = a.LastModifyTime,
                sortCode = a.SortCode,
                isRelease = a.State,
                category = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.Id == a.Category).Select(d => d.FullName),
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                lastModifyUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.LastModifyUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                platformRelease = a.PlatformRelease,
                urlAddress = a.UrlAddress,
                appUrlAddress = a.AppUrlAddress,
                interfaceUrl = a.InterfaceUrl,
                enableFlow = a.EnableFlow
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<VisualDevListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取功能列表下拉框.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector([FromQuery] VisualDevSelectorInput input)
    {
        var mainSystemId = await _visualDevRepository.AsSugarClient().Queryable<SystemEntity>().Where(x => x.EnCode.Equals(CommonConst.SystemCodeMain)).Select(x => x.Id).FirstAsync();
        var webType = input.webType.IsNotEmptyOrNull() ? input.webType.Split(',').ToObject<List<int?>>() : new List<int?>();
        var output = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>()
            .Where(v => v.Type == input.type && v.DeleteMark == null)
            .WhereIF(_userManager.SystemId != mainSystemId, v => v.SystemId == _userManager.SystemId)
            .WhereIF(webType.Any(), v => webType.Contains(v.WebType))
            .OrderBy(a => a.Category).OrderBy(a => a.SortCode)
            .Select(it => new VisualDevSelectorOutput
            {
                id = it.Id,
                fullName = it.FullName,
                SortCode = it.SortCode,
                parentId = it.Category,
                webAddress = it.WebAddress,
                appAddress = it.AppAddress,
                webType = it.WebType
            }).ToListAsync();
        IEnumerable<string>? parentIds = output.Select(x => x.parentId).ToList().Distinct();
        List<VisualDevSelectorOutput>? pList = new List<VisualDevSelectorOutput>();
        List<DictionaryDataEntity>? parentData = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => parentIds.Contains(d.Id) && d.DeleteMark == null).OrderBy(x => x.SortCode).ToListAsync();
        foreach (DictionaryDataEntity? item in parentData)
        {
            VisualDevSelectorOutput? pData = item.Adapt<VisualDevSelectorOutput>();
            pData.parentId = "-1";
            pList.Add(pData);
        }

        return new { list = output.Union(pList).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取功能信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        VisualDevEntity? data = await _visualDevRepository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        var output = data.Adapt<VisualDevInfoOutput>();
        output.interfaceName = await _visualDevRepository.AsSugarClient().Queryable<DataInterfaceEntity>()
            .Where(it => it.DeleteMark == null && it.Id.Equals(output.interfaceId))
            .Select(it => it.FullName)
            .FirstAsync();
        return output;
    }

    /// <summary>
    /// 获取表单主表属性下拉框.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="filterType">1：过滤指定控件.</param>
    /// <returns></returns>
    [HttpGet("{id}/FormDataFields")]
    public async Task<dynamic> GetFormDataFields(string id, [FromQuery] int filterType)
    {
        var templateEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        TemplateParsingBase? tInfo = new TemplateParsingBase(templateEntity.Adapt<VisualDevEntity>()); // 解析模板
        List<FieldsModel>? fieldsModels = tInfo.SingleFormData.FindAll(x => x.__vModel__.IsNotEmptyOrNull() && !JnpfKeyConst.RELATIONFORM.Equals(x.__config__.jnpfKey));
        if (filterType.Equals(1))
        {
            fieldsModels = fieldsModels.FindAll(x => !JnpfKeyConst.UPLOADIMG.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.UPLOADFZ.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.MODIFYUSER.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.MODIFYTIME.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.LINK.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.BUTTON.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.ALERT.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.JNPFTEXT.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.BARCODE.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.QRCODE.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.TABLE.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.CREATEUSER.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.CREATETIME.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.POPUPSELECT.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.CURRORGANIZE.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.CURRPOSITION.Equals(x.__config__.jnpfKey)
                        && !JnpfKeyConst.IFRAME.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.POPUPATTR.Equals(x.__config__.jnpfKey) && !JnpfKeyConst.RELATIONFORMATTR.Equals(x.__config__.jnpfKey));
        }

        List<VisualDevFormDataFieldsOutput>? output = fieldsModels.Select(x => new VisualDevFormDataFieldsOutput()
        {
            label = x.__config__.label,
            vmodel = x.__vModel__
        }).ToList();
        return new { list = output };
    }

    /// <summary>
    /// 获取表单主表属性列表.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("{id}/FieldDataSelect")]
    public async Task<dynamic> GetFieldDataSelect(string id, [FromQuery] VisualDevDataFieldDataListInput input)
    {
        VisualDevEntity? templateEntity = await GetInfoById(id, true); // 取数据
        TemplateParsingBase? tInfo = new TemplateParsingBase(templateEntity); // 解析模板

        Dictionary<string, object> queryDic = new Dictionary<string, object>();
        if (input.relationField.IsNullOrEmpty() && input.columnOptions.IsNotEmptyOrNull()) input.relationField = input.columnOptions.Split(',').First();
        if (!string.IsNullOrWhiteSpace(input.columnOptions) && !string.IsNullOrWhiteSpace(input.keyword))
        {
            foreach (var item in input.columnOptions.Split(','))
            {
                var model = tInfo.FieldsModelList.Find(x => x.__vModel__ == item);
                if (input.queryType.IsNullOrEmpty() || input.queryType == 1 || (input.queryType == 0 && (model.__config__.jnpfKey == JnpfKeyConst.COMINPUT || model.__config__.jnpfKey == JnpfKeyConst.TEXTAREA || model.__config__.jnpfKey == JnpfKeyConst.NUMINPUT || model.__config__.jnpfKey == JnpfKeyConst.AUTOCOMPLETE)))
                    queryDic.Add(item, input.keyword);
            }
        }

        // 获取值 无分页
        VisualDevModelListQueryInput listQueryInput = new VisualDevModelListQueryInput
        {
            queryJson = queryDic.Count > 0 ? queryDic.ToJsonString() : string.Empty,
            queryType = input.queryType,
            currentPage = input.currentPage > 0 ? input.currentPage : 1,
            pageSize = input.pageSize > 0 ? input.pageSize : 20,
            dataType = "1",
            sidx = tInfo.ColumnData.defaultSidx,
            sort = tInfo.ColumnData.sort
        };

        return await _runService.GetRelationFormList(templateEntity, listQueryInput, "List");
    }

    /// <summary>
    /// 回滚模板.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/RollbackTemplate")]
    public async Task RollbackTemplate(string id)
    {
        var vREntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id.Equals(id) && x.DeleteMark == null);
        if (vREntity == null) throw Oops.Oh(ErrorCode.D1415);
        VisualDevEntity? entity = vREntity.Adapt<VisualDevEntity>();
        entity.State = 1;
        await _visualDevRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取已发布菜单名称.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}/GetReleaseMenu")]
    public async Task<dynamic> GetReleaseMenu(string id)
    {
        var entity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id.Equals(id));
        if (entity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1005);
        var output = entity.Adapt<VisualDevReleaseMenuOutput>();
        output.isRelease = entity.PlatformRelease.IsNotEmptyOrNull() ? 1 : 0;
        var systemList = await _visualDevRepository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.DeleteMark == null).ToListAsync();
        var moduleList = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null).ToListAsync();
        var pcModuleList = moduleList.Where(it => it.Category.Equals("Web")).ToList();
        var appModuleList = moduleList.Where(it => it.Category.Equals("App")).ToList();

        var pcList = new List<string>();
        foreach (var module in pcModuleList.Where(it => it.PropertyJson.Contains(id)))
        {
            GetReleaseName(pcList, pcModuleList, systemList, module, string.Empty);
        }

        output.pcReleaseName = string.Join("；", pcList);
        if (output.pcReleaseName.IsNotEmptyOrNull()) output.pcIsRelease = 1;

        var appList = new List<string>();
        foreach (var module in appModuleList.Where(it => it.PropertyJson.Contains(id)))
        {
            GetReleaseName(appList, appModuleList, systemList, module, string.Empty);
        }

        output.appReleaseName = string.Join("；", appList);
        if (output.appReleaseName.IsNotEmptyOrNull()) output.appIsRelease = 1;

        return output;
    }

    /// <summary>
    /// 获取字段列表.
    /// </summary>
    /// <param name="menuId"></param>
    /// <returns></returns>
    [HttpGet("GetColumnList")]
    public async Task<dynamic> GetColumnList([FromQuery] string menuId)
    {
        var module = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(menuId));
        var moduleId = module.ModuleId;
        if (module.Type == 9)
        {
            var flowId = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowTemplateEntity>().Where(x => x.Id == moduleId && x.Status == 1).Select(it => it.FlowId).FirstAsync();
            if (flowId.IsNotEmptyOrNull())
            {
                var node = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowNodeEntity>().FirstAsync(x => x.FlowId == flowId && x.NodeType == "start");
                moduleId = node.FormId;
            }
        }

        var visualDev = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(moduleId));
        if (visualDev.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1005);

        var list = new List<VisualDevColumnListOutput>();
        if (visualDev.Type.Equals(1))
        {
            var tInfo = new TemplateParsingBase(visualDev.Adapt<VisualDevEntity>());
            foreach (var column in tInfo.ColumnData.defaultColumnList)
            {
                list.Add(new VisualDevColumnListOutput
                {
                    fieldId = column.id,
                    fieldName = column.fullName,
                    jnpfKey = column.__config__.jnpfKey,
                    tableName = column.__config__.tableName,
                    multiple = column.multiple,
                    required = column.__config__.required
                });
            }
        }
        else
        {
            var columnData = module.Category.Equals("Web") ? visualDev.ColumnData : visualDev.AppColumnData;
            list.AddRange(columnData.ToObject<List<VisualDevColumnListOutput>>());
        }

        return list;
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建功能信息.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task<dynamic> Create([FromBody] VisualDevCrInput input)
    {
        VisualDevEntity? entity = input.Adapt<VisualDevEntity>();

        try
        {
            var systemKeyList = CommonConst.SYSTEMKEY.Split(",").ToList();
            if (await _sensitiveDetectionProvider.VaildedDataBaseAsync(input.fullName.ToUpper()) || systemKeyList.Contains(input.fullName.ToUpper()))
                throw Oops.Oh(ErrorCode.D1523, string.Format("表单名称{0}", input.fullName));

            // 验证名称和编码是否重复
            if (await _visualDevRepository.IsAnyAsync(x => x.DeleteMark == null && x.Type == entity.Type && x.EnCode == entity.EnCode)) throw Oops.Oh(ErrorCode.COM1031);
            if (await _visualDevRepository.IsAnyAsync(x => x.DeleteMark == null && x.Type == entity.Type && x.SystemId.Equals(_userManager.SystemId) && x.FullName == entity.FullName)) throw Oops.Oh(ErrorCode.COM1032);

            entity.SystemId = _userManager.SystemId;
            entity.State = 0;
            if (entity.EnCode.IsNullOrEmpty())
            {
                if (entity.Type.Equals(1))
                    entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeVisualDev);
                else
                    entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeForm);
            }

            var tInfo = new TemplateParsingBase();
            if (input.formData.IsNotEmptyOrNull())
            {
                if (input.type == 1)
                {
                    tInfo = new TemplateParsingBase(entity); // 解析模板
                }
                else if (input.type == 2)
                {
                    tInfo = new TemplateParsingBase(entity.FormData, entity.Tables); // 解析模板
                }

                // 验证关键词
                var error = new List<string>();
                foreach (var fieldsModel in tInfo.FieldsModelList)
                {
                    if (await _sensitiveDetectionProvider.VaildedDataBaseAsync(fieldsModel.__vModel__.ToUpper()) || systemKeyList.Contains(fieldsModel.__vModel__.ToUpper()))
                        error.Add(string.Format("{0} — {1}", fieldsModel.__config__.label, fieldsModel.__vModel__));
                }
                if (error.Any()) throw Oops.Oh(ErrorCode.D1523, string.Join(",", error));

                if (!tInfo.VerifyTemplate()) throw Oops.Oh(ErrorCode.D1401); // 验证模板
                await VerifyPrimaryKeyPolicy(tInfo, entity.DbLinkId); // 验证雪花Id 和自增长Id 主键是否支持
            }

            _db.BeginTran(); // 开启事务

            // 添加功能
            entity = await _visualDevRepository.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();

            await SyncField(entity, tInfo);
            _db.CommitTran(); // 提交事务
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw;
        }

        return entity.Id;
    }

    /// <summary>
    /// 修改接口.
    /// </summary>
    /// <param name="id">主键id</param>
    /// <param name="input">参数</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [UnitOfWork]
    public async Task Update(string id, [FromBody] VisualDevUpInput input)
    {
        VisualDevEntity? entity = input.Adapt<VisualDevEntity>();

        try
        {
            var systemKeyList = CommonConst.SYSTEMKEY.Split(",").ToList();
            if (await _sensitiveDetectionProvider.VaildedDataBaseAsync(input.fullName.ToUpper()) || systemKeyList.Contains(input.fullName.ToUpper()))
                throw Oops.Oh(ErrorCode.D1523, string.Format("表单名称{0}", input.fullName));

            if (!input.webType.Equals(4) && await _visualDevRepository.AsQueryable().AnyAsync(x => x.Id.Equals(id) && x.State.Equals(1)) && (entity.Tables.IsNullOrEmpty() || entity.Tables.Equals("[]")))
                throw Oops.Oh(ErrorCode.D1416); // 已发布的模板  表不能为空.

            if (entity.EnCode.IsNullOrEmpty())
            {
                if (entity.Type.Equals(1))
                    entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeVisualDev);
                else
                    entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeForm);
            }

            // 验证名称和编码是否重复
            if (await _visualDevRepository.IsAnyAsync(x => x.DeleteMark == null && x.Id != entity.Id && x.Type == entity.Type && x.EnCode == entity.EnCode)) throw Oops.Oh(ErrorCode.COM1031);
            if (await _visualDevRepository.IsAnyAsync(x => x.DeleteMark == null && x.Id != entity.Id && x.Type == entity.Type && x.SystemId.Equals(_userManager.SystemId) && x.FullName == entity.FullName)) throw Oops.Oh(ErrorCode.COM1032);

            var state = await _visualDevRepository.AsQueryable()
                .Where(it => it.DeleteMark == null && it.Id.Equals(id))
                .Select(it => it.State).FirstAsync();
            if (state == 1)
                entity.State = 2;

            var tInfo = new TemplateParsingBase();
            if (input.formData.IsNotEmptyOrNull())
            {
                if (input.type == 1)
                {
                    tInfo = new TemplateParsingBase(entity); // 解析模板
                }
                else if (input.type == 2)
                {
                    tInfo = new TemplateParsingBase(entity.FormData, entity.Tables); // 解析模板
                }

                // 验证关键词
                var error = new List<string>();
                foreach (var fieldsModel in tInfo.FieldsModelList)
                {
                    if (await _sensitiveDetectionProvider.VaildedDataBaseAsync(fieldsModel.__vModel__.ToUpper()) || systemKeyList.Contains(fieldsModel.__vModel__.ToUpper()))
                        error.Add(string.Format("{0} — {1}", fieldsModel.__config__.label, fieldsModel.__vModel__));
                }
                if (error.Any()) throw Oops.Oh(ErrorCode.D1523, string.Join(",", error));

                if (!tInfo.VerifyTemplate()) throw Oops.Oh(ErrorCode.D1401); // 验证模板
                await VerifyPrimaryKeyPolicy(tInfo, entity.DbLinkId); // 验证雪花Id 和自增长Id 主键是否支持
            }

            _db.BeginTran(); // 开启事务

            // 修改功能
            await _visualDevRepository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

            await SyncField(entity, tInfo);
            _db.CommitTran(); // 提交事务
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw;
        }
    }

    /// <summary>
    /// 删除接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [UnitOfWork]
    public async Task Delete(string id)
    {
        var entity = await _visualDevRepository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);

        if (entity.Type.Equals(2) && await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(x => x.DeleteMark == null && x.ModuleId.Equals(id)))
            throw Oops.Oh(ErrorCode.D1426);

        await _visualDevRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();

        // 同步删除线上版本
        var rEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        if (rEntity != null) await _visualDevRepository.AsSugarClient().Updateable(rEntity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();

        if (await _visualDevRepository.AsSugarClient().Queryable<WorkFlowNodeEntity>().AnyAsync(it => it.DeleteMark == null && it.FormId == id))
            throw Oops.Oh(ErrorCode.COM1012);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        var entity = new VisualDevEntity();
        var vREntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        if (vREntity.IsNotEmptyOrNull())
            entity = vREntity.Adapt<VisualDevEntity>();
        else
            entity = await _visualDevRepository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);

        var random = new Random().NextLetterAndNumberString(5);
        entity.Id = SnowflakeIdHelper.NextId();
        entity.FullName = entity.FullName + ".副本" + random;
        entity.EnCode += random;
        entity.State = 0;
        entity.LastModifyUserId = null;
        entity.LastModifyTime = null;

        var aliasList = await _visualDevRepository.AsSugarClient().Queryable<VisualAliasEntity>().Where(v => v.VisualId == id && v.DeleteMark == null).ToListAsync();
        foreach (var alias in aliasList)
        {
            alias.VisualId = entity.Id;
            alias.LastModifyUserId = null;
            alias.LastModifyTime = null;
        }

        try
        {
            entity = await _visualDevRepository.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
            await _visualDevRepository.AsSugarClient().Insertable(aliasList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
        catch
        {
            if (entity.FullName.Length >= 100 || entity.EnCode.Length >= 50) throw Oops.Oh(ErrorCode.COM1009); // 数据长度超过 字段设定长度
            else throw;
        }
    }

    /// <summary>
    /// 功能发布.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Release")]
    public async Task Release(string id)
    {
        var entity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id == id);

        if (entity.Type == 1)
        {
            if (entity.FormData.IsNullOrEmpty() && !entity.WebType.Equals(4)) throw Oops.Oh(ErrorCode.COM1013);
            if ((entity.WebType.Equals(2) || entity.WebType.Equals(4)) && entity.ColumnData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1014);

            var tInfo = new TemplateParsingBase(entity); // 解析模板

            // 无表转有表
            if (!tInfo.IsHasTable && !entity.WebType.Equals(4))
            {
                VisualDevEntity? res = await NoTblToTable(entity);
                entity = res;
                if (res != null)
                {
                    tInfo = new TemplateParsingBase(res); // 解析模板
                    await _visualDevRepository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                }
                else throw Oops.Oh(ErrorCode.D1414);
            }
        }

        // 修改功能发布状态
        await _visualDevRepository.AsUpdateable().SetColumns(it => it.State == 1).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();

        // 线上版本
        var releaseEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id == id);
        if (releaseEntity.IsNullOrEmpty())
        {
            var vReleaseEntity = entity.Adapt<VisualDevReleaseEntity>();
            await _visualDevRepository.AsSugarClient().Insertable(vReleaseEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        }
        else
        {
            var vReleaseEntity = entity.Adapt<VisualDevReleaseEntity>();

            if (releaseEntity.DbLinkId != vReleaseEntity.DbLinkId ||
                releaseEntity.WebType != vReleaseEntity.WebType ||
                releaseEntity.Tables != vReleaseEntity.Tables ||
                releaseEntity.FormData != vReleaseEntity.FormData ||
                releaseEntity.ColumnData != vReleaseEntity.ColumnData ||
                releaseEntity.AppColumnData != vReleaseEntity.AppColumnData)
            {
                vReleaseEntity.OldContent = new Dictionary<string, object>
                {
                    { "DbLinkId", releaseEntity.DbLinkId },
                    { "WebType", releaseEntity.WebType },
                    { "Tables", releaseEntity.Tables },
                    { "FormData", releaseEntity.FormData },
                    { "ColumnData", releaseEntity.ColumnData },
                    { "AppColumnData", releaseEntity.AppColumnData }
                }.ToJsonString();
            }

            await _visualDevRepository.AsSugarClient().Updateable(vReleaseEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 功能生成菜单.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Module")]
    [UnitOfWork]
    public async Task GenerateMenu(string id, [FromBody] VisualDevToMenuInput input)
    {
        var entity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id == id);
        if (entity.State == 0) throw Oops.Oh(ErrorCode.D1424);

        var systemId = _userManager.SystemId;

        // 全部生成的菜单
        var release = new Dictionary<string, List<string>>();
        var moduleList = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null && it.SystemId.Equals(systemId)).ToListAsync();
        var allModuleList = moduleList.Where(it => it.PropertyJson.Contains(id)).ToList();
        if (input.pc == 1)
        {
            if (!input.pcModuleParentId.Any() && !await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && it.SystemId.Equals(systemId) && it.Category.Equals("Web") && it.PropertyJson.Contains(id)))
                throw Oops.Oh(ErrorCode.D4017);

            // 添加已经存在的菜单
            var list = new List<string>();
            list.AddRange(allModuleList.Where(it => it.Category.Equals("Web")).Select(x => x.ParentId).ToList());

            // 新发布的菜单
            foreach (var item in input.pcModuleParentId)
            {
                if (item.Equals(systemId))
                {
                    list.Add("-1");
                }
                else
                {
                    var module = moduleList.Find(it => it.Id.Equals(item));
                    if (module.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D4021);
                    list.Add(module.Id);
                }
            }

            release.Add("Web", list);
        }
        if (input.app == 1)
        {
            if (!input.appModuleParentId.Any() && !await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().AnyAsync(it => it.DeleteMark == null && it.SystemId.Equals(systemId) && it.Category.Equals("App") && it.PropertyJson.Contains(id)))
                throw Oops.Oh(ErrorCode.D4017);

            // 添加已经存在的菜单
            var list = new List<string>();
            list.AddRange(allModuleList.Where(it => it.Category.Equals("App")).Select(x => x.ParentId).ToList());

            // 新发布的菜单
            foreach (var item in input.appModuleParentId)
            {
                if (item.Equals(systemId))
                {
                    list.Add("-1");
                }
                else
                {
                    var module = moduleList.Find(it => it.Id.Equals(item));
                    if (module.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D4021);
                    list.Add(module.Id);
                }
            }

            release.Add("App", list);
        }

        if (entity.Type == 1)
        {
            var tInfo = new TemplateParsingBase(entity);
            foreach (var platform in release)
            {
                foreach (var data in platform.Value)
                {
                    #region 旧的菜单、权限数据

                    var oldWebModule = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Category == "Web" && x.DeleteMark == null && x.PropertyJson.Contains(id) && x.SystemId.Equals(systemId) && x.ParentId.Equals(data));
                    var oldWebModuleButtonEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldWebModule != null, x => x.ModuleId == oldWebModule.Id).WhereIF(oldWebModule == null, x => x.ModuleId == "0").ToListAsync();
                    var oldWebModuleColumnEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldWebModule != null, x => x.ModuleId == oldWebModule.Id).WhereIF(oldWebModule == null, x => x.ModuleId == "0").ToListAsync();
                    var oldWebModuleFormEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleFormEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldWebModule != null, x => x.ModuleId == oldWebModule.Id).WhereIF(oldWebModule == null, x => x.ModuleId == "0").ToListAsync();

                    var oldAppModule = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Category == "App" && x.DeleteMark == null && x.PropertyJson.Contains(id) && x.SystemId.Equals(systemId) && x.ParentId.Equals(data));
                    var oldAppModuleButtonEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldAppModule != null, x => x.ModuleId == oldAppModule.Id).WhereIF(oldAppModule == null, x => x.ModuleId == "0").ToListAsync();
                    var oldAppModuleColumnEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldAppModule != null, x => x.ModuleId == oldAppModule.Id).WhereIF(oldAppModule == null, x => x.ModuleId == "0").ToListAsync();
                    var oldAppModuleFormEntity = await _visualDevRepository.AsSugarClient().Queryable<ModuleFormEntity>().Where(x => x.DeleteMark == null)
                        .WhereIF(oldAppModule != null, x => x.ModuleId == oldAppModule.Id).WhereIF(oldAppModule == null, x => x.ModuleId == "0").ToListAsync();

                    #endregion

                    var fullName = entity.FullName;
                    var enCode = entity.EnCode + new Random().NextLetterAndNumberString(5);

                    if ((platform.Key == "Web" && (input.pcModuleParentId.Contains(data) || (data.Equals("-1") && input.pcModuleParentId.Contains(systemId))))
                        || (platform.Key == "App" && (input.appModuleParentId.Contains(data) || (data.Equals("-1") && input.appModuleParentId.Contains(systemId)))))
                    {
                        if (_visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Any(x => x.FullName == fullName && x.ParentId == data && x.SystemId == systemId && x.Category == platform.Key && x.DeleteMark == null))
                            throw Oops.Oh(ErrorCode.COM1032);
                        if (_visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Any(x => x.EnCode == enCode && x.Category == platform.Key && x.DeleteMark == null))
                            throw Oops.Oh(ErrorCode.COM1031);
                    }

                    var moduleModel = new ModuleEntity();

                    // 数据视图
                    if (entity.WebType.Equals(4))
                    {
                        #region 菜单组装

                        moduleModel.Id = oldWebModule != null ? oldWebModule.Id : SnowflakeIdHelper.NextId();
                        moduleModel.ModuleId = id;
                        moduleModel.ParentId = oldWebModule != null ? oldWebModule.ParentId : data; // 父级菜单节点
                        moduleModel.Category = "Web";
                        moduleModel.FullName = oldWebModule != null ? oldWebModule.FullName : fullName;
                        moduleModel.EnCode = oldWebModule != null ? oldWebModule.EnCode : enCode;
                        moduleModel.Icon = oldWebModule != null ? oldWebModule.Icon : "icon-ym icon-ym-webForm";
                        moduleModel.UrlAddress = oldWebModule != null ? oldWebModule.UrlAddress : "model/" + moduleModel.EnCode;
                        moduleModel.Type = 3;
                        moduleModel.EnabledMark = 1;
                        moduleModel.IsColumnAuthorize = 0;
                        moduleModel.IsButtonAuthorize = 0;
                        moduleModel.IsFormAuthorize = 0;
                        moduleModel.IsDataAuthorize = 0;
                        moduleModel.SortCode = oldWebModule != null ? oldWebModule.SortCode : 999;
                        moduleModel.CreatorTime = DateTime.Now;
                        moduleModel.PropertyJson = new ModulePropertyModel() { moduleId = id, webType = 4 }.ToJsonStringOld();
                        moduleModel.SystemId = oldWebModule != null ? oldWebModule.SystemId : systemId;

                        #endregion

                        // 添加PC菜单
                        if (platform.Key == "Web")
                        {
                            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(moduleModel).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                        }

                        // 添加App菜单
                        if (platform.Key == "App")
                        {
                            #region App菜单
                            moduleModel.Id = oldAppModule != null ? oldAppModule.Id : SnowflakeIdHelper.NextId();
                            moduleModel.ParentId = oldAppModule != null ? oldAppModule.ParentId : data; // 父级菜单节点
                            moduleModel.Category = "App";
                            moduleModel.FullName = oldAppModule != null ? oldAppModule.FullName : fullName;
                            moduleModel.EnCode = oldAppModule != null ? oldAppModule.EnCode : enCode;
                            moduleModel.UrlAddress = oldAppModule != null ? oldAppModule.UrlAddress : "/pages/apply/dynamicModel/index?id=" + moduleModel.EnCode;
                            moduleModel.SystemId = oldAppModule != null ? oldAppModule.SystemId : systemId;
                            moduleModel.SortCode = oldAppModule != null ? oldAppModule.SortCode : 999;
                            moduleModel.Icon = oldAppModule != null ? oldAppModule.Icon : "icon-ym icon-ym-webForm";

                            #endregion

                            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(moduleModel).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                        }
                    }
                    else
                    {
                        ColumnDesignModel? columnData = new ColumnDesignModel();

                        // 列配置模型
                        if (!string.IsNullOrWhiteSpace(entity.ColumnData))
                        {
                            columnData = entity.ColumnData.ToObject<ColumnDesignModel>();
                        }
                        else
                        {
                            columnData = new ColumnDesignModel()
                            {
                                btnsList = new List<ButtonConfigModel>(),
                                columnBtnsList = new List<ButtonConfigModel>(),
                                customBtnsList = new List<ButtonConfigModel>(),
                                columnList = new List<IndexGridFieldModel>(),
                                defaultColumnList = new List<IndexGridFieldModel>()
                            };
                        }

                        columnData.btnsList = columnData.btnsList.Union(columnData.columnBtnsList).ToList();
                        if (columnData.customBtnsList != null && columnData.customBtnsList.Any()) columnData.btnsList = columnData.btnsList.Union(columnData.customBtnsList).ToList();

                        ColumnDesignModel? appColumnData = new ColumnDesignModel();

                        // App列配置模型
                        if (!string.IsNullOrWhiteSpace(entity.AppColumnData))
                        {
                            appColumnData = tInfo.AppColumnData;
                        }
                        else
                        {
                            appColumnData = new ColumnDesignModel()
                            {
                                btnsList = new List<ButtonConfigModel>(),
                                columnBtnsList = new List<ButtonConfigModel>(),
                                customBtnsList = new List<ButtonConfigModel>(),
                                columnList = new List<IndexGridFieldModel>(),
                                defaultColumnList = new List<IndexGridFieldModel>()
                            };
                        }

                        appColumnData.btnsList = appColumnData.btnsList.Union(appColumnData.columnBtnsList).ToList();
                        if (appColumnData.customBtnsList != null && appColumnData.customBtnsList.Any()) appColumnData.btnsList = appColumnData.btnsList.Union(appColumnData.customBtnsList).ToList();

                        #region 菜单组装
                        moduleModel.Id = oldWebModule != null ? oldWebModule.Id : SnowflakeIdHelper.NextId();
                        moduleModel.ModuleId = id;
                        moduleModel.ParentId = oldWebModule != null ? oldWebModule.ParentId : data; // 父级菜单节点
                        moduleModel.Category = "Web";
                        moduleModel.FullName = oldWebModule != null ? oldWebModule.FullName : fullName;
                        moduleModel.EnCode = oldWebModule != null ? oldWebModule.EnCode : enCode;
                        moduleModel.Icon = oldWebModule != null ? oldWebModule.Icon : "icon-ym icon-ym-webForm";
                        moduleModel.UrlAddress = oldWebModule != null ? oldWebModule.UrlAddress : "model/" + moduleModel.EnCode;
                        moduleModel.Type = 3;
                        moduleModel.EnabledMark = 1;
                        moduleModel.IsColumnAuthorize = columnData.useColumnPermission ? 1 : 0;
                        moduleModel.IsButtonAuthorize = columnData.useBtnPermission ? 1 : 0;
                        moduleModel.IsFormAuthorize = columnData.useFormPermission ? 1 : 0;
                        moduleModel.IsDataAuthorize = columnData.useDataPermission ? 1 : 0;
                        moduleModel.SortCode = oldWebModule != null ? oldWebModule.SortCode : 999;
                        moduleModel.CreatorTime = DateTime.Now;
                        moduleModel.PropertyJson = new ModulePropertyModel() { moduleId = id, webType = entity.WebType }.ToJsonStringOld();
                        moduleModel.SystemId = oldWebModule != null ? oldWebModule.SystemId : systemId;
                        #endregion

                        #region 配置权限

                        // 按钮权限
                        var btnAuth = new List<ModuleButtonEntity>();
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_add", FullName = "新增", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_download", FullName = "导出", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_upload", FullName = "导入", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_batchRemove", FullName = "批量删除", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_edit", FullName = "编辑", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_remove", FullName = "删除", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_detail", FullName = "详情", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_batchPrint", FullName = "批量打印", ModuleId = moduleModel.Id });
                        columnData.customBtnsList.ForEach(item =>
                        {
                            btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 1, SortCode = 0, ParentId = "-1", EnCode = item.value, FullName = item.label, ModuleId = moduleModel.Id });
                        });

                        columnData.btnsList.ForEach(item =>
                        {
                            var aut = btnAuth.Find(x => x.EnCode == "btn_" + item.value);
                            if (aut != null && item.show) aut.EnabledMark = 1;
                        });

                        // 表单权限
                        var columnAuth = new List<ModuleColumnEntity>();
                        var fieldList = tInfo.AllFieldsModel;
                        var formAuth = new List<ModuleFormEntity>();

                        var ctList = tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey == JnpfKeyConst.TABLE).ToList();
                        var childTableIndex = new Dictionary<string, string>();
                        for (var i = 0; i < ctList.Count; i++) childTableIndex.Add(ctList[i].__vModel__, ctList[i].__config__.label + (i + 1));

                        fieldList = fieldList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.TABLE).ToList();
                        fieldList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item =>
                        {
                            var fRule = item.__vModel__.Contains("_jnpf_") ? 1 : 0;
                            fRule = item.__vModel__.ToLower().Contains("tablefield") && item.__vModel__.Contains("-") ? 2 : fRule;
                            var ctName = item.__vModel__.Split("-");
                            formAuth.Add(new ModuleFormEntity()
                            {
                                ParentId = "-1",
                                EnCode = item.__vModel__,
                                BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                                ChildTableKey = fRule.Equals(2) ? ctName.FirstOrDefault() : string.Empty,
                                FieldRule = fRule,
                                ModuleId = moduleModel.Id,
                                FullName = fRule.Equals(2) ? childTableIndex[item.__vModel__.Split('-').First()] + "-" + item.__config__.label : item.__config__.label,
                                EnabledMark = 1,
                                SortCode = 0
                            });
                        });
                        ctList.ForEach(item =>
                        {
                            formAuth.Add(new ModuleFormEntity()
                            {
                                ParentId = "-1",
                                EnCode = item.__vModel__,
                                BindTable = item.__config__.tableName,
                                ChildTableKey = item.__vModel__,
                                FieldRule = 0,
                                ModuleId = moduleModel.Id,
                                FullName = childTableIndex[item.__vModel__],
                                EnabledMark = 1,
                                SortCode = 0
                            });
                        });

                        // 列表权限
                        columnData.defaultColumnList.ForEach(item =>
                        {
                            var itemModel = fieldList.FirstOrDefault(x => x.__config__.jnpfKey == item.__config__.jnpfKey && x.__vModel__ == item.prop);
                            if (itemModel != null)
                            {
                                var fRule = itemModel.__vModel__.Contains("_jnpf_") ? 1 : 0;
                                fRule = itemModel.__vModel__.ToLower().Contains("tablefield") && itemModel.__vModel__.Contains("-") ? 2 : fRule;
                                var ctName = item.__vModel__.Split("-");
                                columnAuth.Add(new ModuleColumnEntity()
                                {
                                    ParentId = "-1",
                                    EnCode = itemModel.__vModel__,
                                    BindTable = fRule.Equals(2) ? itemModel.__config__.relationTable : itemModel.__config__.tableName,
                                    ChildTableKey = fRule.Equals(2) ? itemModel.__vModel__.Split("-").FirstOrDefault() : string.Empty,
                                    FieldRule = fRule,
                                    ModuleId = moduleModel.Id,
                                    FullName = fRule.Equals(2) ? childTableIndex[itemModel.__vModel__.Split('-').First()] + "-" + itemModel.__config__.label : itemModel.__config__.label,
                                    EnabledMark = 0,
                                    SortCode = 0
                                });
                            }
                        });

                        columnData.columnList.ForEach(item =>
                        {
                            var aut = columnAuth.Find(x => x.EnCode == item.prop);
                            if (aut != null) aut.EnabledMark = 1;
                        });

                        #endregion

                        // 添加PC菜单和权限
                        if (platform.Key == "Web")
                        {
                            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(moduleModel).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新

                            #region 表单权限
                            if (columnData.useFormPermission)
                            {
                                if (!oldWebModuleFormEntity.Any())
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(formAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else
                                {
                                    var formAuthAddList = new List<ModuleFormEntity>();
                                    formAuth.ForEach(item =>
                                    {
                                        if (!oldWebModuleFormEntity.Any(x => x.EnCode == item.EnCode)) formAuthAddList.Add(item);
                                    });
                                    if (formAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(formAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                    oldWebModuleFormEntity.ForEach(item =>
                                    {
                                        var it = formAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = 1; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldWebModuleFormEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 按钮权限
                            if (columnData.useBtnPermission)
                            {
                                if (!oldWebModuleButtonEntity.Any()) // 新增数据
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(btnAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else // 修改增加数据权限
                                {
                                    var btnAuthAddList = new List<ModuleButtonEntity>();
                                    btnAuth.ForEach(item =>
                                    {
                                        if (!oldWebModuleButtonEntity.Any(x => x.EnCode == item.EnCode)) btnAuthAddList.Add(item);
                                    });
                                    if (btnAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(btnAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                                    oldWebModuleButtonEntity.ForEach(item =>
                                    {
                                        var it = btnAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = it.EnabledMark; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldWebModuleButtonEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 列表权限
                            if (columnData.useColumnPermission)
                            {
                                if (!oldWebModuleColumnEntity.Any()) // 新增数据
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(columnAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else // 修改增加数据权限
                                {
                                    var columnAuthAddList = new List<ModuleColumnEntity>();
                                    columnAuth.ForEach(item =>
                                    {
                                        if (!oldWebModuleColumnEntity.Any(x => x.EnCode == item.EnCode)) columnAuthAddList.Add(item);
                                    });
                                    if (columnAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(columnAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                    oldWebModuleColumnEntity.ForEach(item =>
                                    {
                                        var it = columnAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = it.EnabledMark; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldWebModuleColumnEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 数据权限
                            if (columnData.useDataPermission)
                            {
                                if (!_visualDevRepository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(x => x.EnCode.Equals("jnpf_alldata") && x.ModuleId == moduleModel.Id && x.DeleteMark == null).Any())
                                {
                                    // 全部数据权限方案
                                    var AllDataAuthScheme = new ModuleDataAuthorizeSchemeEntity()
                                    {
                                        FullName = "全部数据",
                                        EnCode = "jnpf_alldata",
                                        AllData = 1,
                                        ConditionText = string.Empty,
                                        ConditionJson = string.Empty,
                                        ModuleId = moduleModel.Id
                                    };
                                    await _visualDevRepository.AsSugarClient().Insertable(AllDataAuthScheme).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                                }

                                // 创建用户和所属组织权限方案
                                // 只添加 主表控件的数据权限
                                var fList = fieldList.Where(x => !x.__vModel__.Contains("_jnpf_") && x.__vModel__.IsNotEmptyOrNull() && x.__config__.visibility.Contains("pc") && (x.__config__.jnpfKey == JnpfKeyConst.CREATEUSER || x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE || x.__config__.jnpfKey == JnpfKeyConst.CURRPOSITION)).ToList();
                                await MenuMergeDataAuthScheme(moduleModel.Id, fList);
                            }
                            #endregion
                        }

                        #region App菜单、 权限组装
                        moduleModel.Id = oldAppModule != null ? oldAppModule.Id : SnowflakeIdHelper.NextId();
                        moduleModel.ParentId = oldAppModule != null ? oldAppModule.ParentId : data; // 父级菜单节点
                        moduleModel.Category = "App";
                        moduleModel.FullName = oldAppModule != null ? oldAppModule.FullName : fullName;
                        moduleModel.EnCode = oldAppModule != null ? oldAppModule.EnCode : enCode;
                        moduleModel.UrlAddress = oldAppModule != null ? oldAppModule.UrlAddress : "/pages/apply/dynamicModel/index?id=" + moduleModel.EnCode;
                        moduleModel.SystemId = oldAppModule != null ? oldAppModule.SystemId : systemId;
                        moduleModel.SortCode = oldAppModule != null ? oldAppModule.SortCode : 999;
                        moduleModel.Icon = oldAppModule != null ? oldAppModule.Icon : "icon-ym icon-ym-webForm";
                        moduleModel.IsColumnAuthorize = appColumnData.useColumnPermission ? 1 : 0;
                        moduleModel.IsButtonAuthorize = appColumnData.useBtnPermission ? 1 : 0;
                        moduleModel.IsFormAuthorize = appColumnData.useFormPermission ? 1 : 0;
                        moduleModel.IsDataAuthorize = appColumnData.useDataPermission ? 1 : 0;

                        btnAuth = new List<ModuleButtonEntity>();
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_add", FullName = "新增", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_edit", FullName = "编辑", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_remove", FullName = "删除", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_detail", FullName = "详情", ModuleId = moduleModel.Id });
                        btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 0, SortCode = 0, ParentId = "-1", EnCode = "btn_batchRemove", FullName = "批量删除", ModuleId = moduleModel.Id });
                        appColumnData.customBtnsList.ForEach(item =>
                        {
                            btnAuth.Add(new ModuleButtonEntity() { EnabledMark = 1, SortCode = 0, ParentId = "-1", EnCode = item.value, FullName = item.label, ModuleId = moduleModel.Id });
                        });
                        appColumnData.btnsList.ForEach(item =>
                        {
                            var aut = btnAuth.Find(x => x.EnCode == "btn_" + item.value);
                            if (aut != null && item.show) aut.EnabledMark = 1;
                        });

                        formAuth.Clear();
                        fieldList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item =>
                        {
                            var fRule = item.__vModel__.Contains("_jnpf_") ? 1 : 0;
                            fRule = item.__vModel__.ToLower().Contains("tablefield") && item.__vModel__.Contains("-") ? 2 : fRule;
                            formAuth.Add(new ModuleFormEntity()
                            {
                                ParentId = "-1",
                                EnCode = item.__vModel__,
                                BindTable = fRule.Equals(2) ? item.__config__.relationTable : item.__config__.tableName,
                                ChildTableKey = fRule.Equals(2) ? item.__vModel__.Split("-").FirstOrDefault() : string.Empty,
                                FieldRule = fRule,
                                ModuleId = moduleModel.Id,
                                FullName = fRule.Equals(2) ? childTableIndex[item.__vModel__.Split('-').First()] + "-" + item.__config__.label : item.__config__.label,
                                EnabledMark = 1,
                                SortCode = 0
                            });
                        });
                        ctList.ForEach(item =>
                        {
                            formAuth.Add(new ModuleFormEntity()
                            {
                                ParentId = "-1",
                                EnCode = item.__vModel__,
                                BindTable = item.__config__.tableName,
                                ChildTableKey = item.__vModel__,
                                FieldRule = 0,
                                ModuleId = moduleModel.Id,
                                FullName = childTableIndex[item.__vModel__],
                                EnabledMark = 1,
                                SortCode = 0
                            });
                        });

                        columnAuth.Clear();
                        appColumnData.defaultColumnList.ForEach(item =>
                        {
                            var itemModel = fieldList.FirstOrDefault(x => x.__config__.jnpfKey == item.__config__.jnpfKey && x.__vModel__ == item.prop);
                            if (itemModel != null)
                            {
                                var fRule = itemModel.__vModel__.Contains("_jnpf_") ? 1 : 0;
                                fRule = itemModel.__vModel__.ToLower().Contains("tablefield") && itemModel.__vModel__.Contains("-") ? 2 : fRule;
                                columnAuth.Add(new ModuleColumnEntity()
                                {
                                    ParentId = "-1",
                                    EnCode = itemModel.__vModel__,
                                    BindTable = fRule.Equals(2) ? itemModel.__config__.relationTable : itemModel.__config__.tableName,
                                    ChildTableKey = fRule.Equals(2) ? itemModel.__vModel__.Split("-").FirstOrDefault() : string.Empty,
                                    FieldRule = fRule,
                                    ModuleId = moduleModel.Id,
                                    FullName = fRule.Equals(2) ? childTableIndex[itemModel.__vModel__.Split('-').First()] + "-" + itemModel.__config__.label : itemModel.__config__.label,
                                    EnabledMark = 0,
                                    SortCode = 0
                                });
                            }
                        });
                        appColumnData.columnList.ForEach(item =>
                        {
                            var aut = columnAuth.Find(x => x.EnCode == item.prop);
                            if (aut != null) aut.EnabledMark = 1;
                        });

                        columnAuth.ForEach(item => { item.ModuleId = moduleModel.Id; });
                        #endregion

                        // 添加App菜单和权限
                        if (platform.Key == "App")
                        {
                            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(moduleModel).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新

                            #region 表单权限
                            if (appColumnData.useFormPermission)
                            {
                                if (!oldAppModuleFormEntity.Any())
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(formAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else
                                {
                                    var formAuthAddList = new List<ModuleFormEntity>();
                                    formAuth.ForEach(item =>
                                    {
                                        if (!oldAppModuleFormEntity.Any(x => x.EnCode == item.EnCode)) formAuthAddList.Add(item);
                                    });
                                    if (formAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(formAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                    oldAppModuleFormEntity.ForEach(item =>
                                    {
                                        var it = formAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = 1; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldAppModuleFormEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 按钮权限
                            if (appColumnData.useBtnPermission)
                            {
                                if (!oldAppModuleButtonEntity.Any()) // 新增数据
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(btnAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else // 修改增加数据权限
                                {
                                    var btnAuthAddList = new List<ModuleButtonEntity>();
                                    btnAuth.ForEach(item =>
                                    {
                                        if (!oldAppModuleButtonEntity.Any(x => x.EnCode == item.EnCode)) btnAuthAddList.Add(item);
                                    });
                                    if (btnAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(btnAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                                    oldAppModuleButtonEntity.ForEach(item =>
                                    {
                                        var it = btnAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = it.EnabledMark; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldAppModuleButtonEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 列表权限
                            if (appColumnData.useColumnPermission)
                            {
                                if (!oldAppModuleColumnEntity.Any()) // 新增数据
                                {
                                    await _visualDevRepository.AsSugarClient().Insertable(columnAuth).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                                }
                                else // 修改增加数据权限
                                {
                                    var columnAuthAddList = new List<ModuleColumnEntity>();
                                    columnAuth.ForEach(item =>
                                    {
                                        if (!oldAppModuleColumnEntity.Any(x => x.EnCode == item.EnCode)) columnAuthAddList.Add(item);
                                    });
                                    if (columnAuthAddList.Any()) await _visualDevRepository.AsSugarClient().Insertable(columnAuthAddList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

                                    oldAppModuleColumnEntity.ForEach(item =>
                                    {
                                        var it = columnAuth.FirstOrDefault(x => x.EnCode == item.EnCode);
                                        if (it != null) item.EnabledMark = it.EnabledMark; // 显示标识
                                    });
                                    await _visualDevRepository.AsSugarClient().Updateable(oldAppModuleColumnEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                                }
                            }
                            #endregion

                            #region 数据权限
                            if (appColumnData.useDataPermission)
                            {
                                // 全部数据权限
                                if (!_visualDevRepository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(x => x.EnCode.Equals("jnpf_alldata") && x.ModuleId == moduleModel.Id && x.DeleteMark == null).Any())
                                {
                                    // 全部数据权限方案
                                    var AllDataAuthScheme = new ModuleDataAuthorizeSchemeEntity()
                                    {
                                        FullName = "全部数据",
                                        EnCode = "jnpf_alldata",
                                        AllData = 1,
                                        ConditionText = string.Empty,
                                        ConditionJson = string.Empty,
                                        ModuleId = moduleModel.Id
                                    };
                                    await _visualDevRepository.AsSugarClient().Insertable(AllDataAuthScheme).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                                }

                                // 创建用户和所属组织权限方案
                                // 只添加 主表控件的数据权限
                                var fList = fieldList.Where(x => !x.__vModel__.Contains("_jnpf_") && x.__vModel__.IsNotEmptyOrNull() && x.__config__.visibility.Contains("app") && (x.__config__.jnpfKey == JnpfKeyConst.CREATEUSER || x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE || x.__config__.jnpfKey == JnpfKeyConst.CURRPOSITION)).ToList();
                                await MenuMergeDataAuthScheme(moduleModel.Id, fList);
                            }
                            #endregion
                        }
                    }
                }
            }
        }
        else
        {
            var tInfo = new TemplateParsingBase(entity.FormData, entity.Tables);
            await GenerateHcMenu(entity, input, allModuleList, release);
        }

        await _visualDevRepository.AsUpdateable().SetColumns(it => it.PlatformRelease == input.platformRelease).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取功能信息.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="isGetRelease">是否获取发布版本.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<VisualDevEntity> GetInfoById(string id, bool isGetRelease = false)
    {
        if (isGetRelease)
        {
            var vREntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id == id && x.DeleteMark == null);
            return vREntity.Adapt<VisualDevEntity>();
        }
        else
        {
            var vEntity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.Id == id && x.DeleteMark == null);
            return vEntity.Adapt<VisualDevEntity>();
        }
    }

    /// <summary>
    /// 获取代码生成命名规范.
    /// </summary>
    /// <param name="modelId">主键ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<VisualAliasEntity>> GetAliasList(string modelId)
    {
        return await _visualDevRepository.AsSugarClient().Queryable<VisualAliasEntity>().Where(it => it.VisualId == modelId && it.DeleteMark == null).ToListAsync();
    }

    /// <summary>
    /// 新增导入数据.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [NonAction]
    public async Task CreateImportData(VisualDevExportOutput input, int type)
    {
        var errorMsgList = new List<string>();
        var errorList = new List<string>();

        if (input.systemId.IsNullOrEmpty() || !input.systemId.Equals(_userManager.SystemId))
        {
            input.id = SnowflakeIdHelper.NextId();
            if (input.type.Equals(1))
                input.enCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeVisualDev);
            else
                input.enCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeForm);
        }
        else
        {
            if (input.id.IsNotEmptyOrNull())
            {
                if (await _visualDevRepository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(input.id))) errorList.Add("ID");
            }
            else
            {
                input.id = SnowflakeIdHelper.NextId();
            }
        }

        if (await _visualDevRepository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Type.Equals(input.type) && it.EnCode.Equals(input.enCode))) errorList.Add("编码");
        if (await _visualDevRepository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Type.Equals(input.type) && it.SystemId.Equals(_userManager.SystemId) && it.FullName.Equals(input.fullName))) errorList.Add("名称");

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
                input.id = SnowflakeIdHelper.NextId();
                input.fullName = string.Format("{0}.副本{1}", input.fullName, random);
                input.enCode += random;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        input.enabledMark = 0;
        input.state = 0;
        input.creatorTime = DateTime.Now;
        input.creatorUserId = _userManager.UserId;
        input.lastModifyTime = null;
        input.lastModifyUserId = null;
        var entity = input.Adapt<VisualDevEntity>();
        entity.SystemId = _userManager.SystemId;

        foreach (var alias in input.aliasListJson)
        {
            alias.VisualId = entity.Id;
            alias.LastModifyTime = null;
            alias.LastModifyUserId = null;
        }

        try
        {
            var storModuleModel = _visualDevRepository.AsSugarClient().Storageable(entity).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新

            await _visualDevRepository.AsSugarClient().Insertable(input.aliasListJson).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }

    #endregion

    #region Private

    /// <summary>
    /// 功能模板 无表 转 有表.
    /// </summary>
    /// <param name="vEntity">功能实体.</param>
    /// <param name="mainTableName">主表名称.</param>
    /// <returns></returns>
    [NonAction]
    private async Task<VisualDevEntity> NoTblToTable(VisualDevEntity vEntity)
    {
        var link = await _runService.GetDbLink(vEntity.DbLinkId);
        var isUpper = link.DbType.ToLower().Equals("oracle") || link.DbType.ToLower().Equals("dm") || link.DbType.ToLower().Equals("dm8"); // 是否大写

        FormDataModel formModel = vEntity.FormData.ToObjectOld<FormDataModel>();
        List<FieldsModel>? fieldsModelList = TemplateAnalysis.AnalysisTemplateData(formModel.fields, true);

        #region oracle数据库字段长度限制
        if (link.DbType.ToLower().Equals("oracle"))
        {
            var fieldsDic = new Dictionary<string, string>() {
                { JnpfKeyConst.NUMINPUT, "NUMBER" }, { JnpfKeyConst.COMSELECT, "ORGSEL"},
                { JnpfKeyConst.POSSELECT, "POSSEL" }, { JnpfKeyConst.USERSELECT, "USERSEL"},
                { JnpfKeyConst.TREESELECT, "TREESEL"}, { JnpfKeyConst.ROLESELECT, "ROLESEL" },
                { JnpfKeyConst.GROUPSELECT, "GROUPSEL"}, { JnpfKeyConst.USERSSELECT, "USERSSEL" },
                { JnpfKeyConst.POPUPTABLESELECT, "POPTBSEL" }, { JnpfKeyConst.AUTOCOMPLETE, "AUTOCOMP"},
                { JnpfKeyConst.ADDRESS, "AREASEL" }, { JnpfKeyConst.RELATIONFORM, "RELFORM"},
                { JnpfKeyConst.POPUPSELECT, "POPSEL" }, { JnpfKeyConst.RELATIONFORMATTR, "RELFORMATR"},
                { JnpfKeyConst.POPUPATTR, "POPATR" }, { JnpfKeyConst.CURRORGANIZE, "CURRORG"}, { JnpfKeyConst.CURRPOSITION, "CURRPOS"}};

            var newEntity = vEntity.ToJsonStringOld();
            foreach (var item in fieldsModelList)
            {
                if (item.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                {
                    foreach (var ctItem in item.__config__.children)
                    {
                        if (ctItem.__vModel__.Length > 14 && ctItem.__vModel__.Contains("FIELD"))
                        {
                            var vModel = ctItem.__vModel__.Split("FIELD");
                            if (vModel.Last().Length > 1)
                            {
                                var newVModel = string.Empty;
                                if (fieldsDic.ContainsKey(ctItem.__config__.jnpfKey)) newVModel = fieldsDic[ctItem.__config__.jnpfKey] + vModel.Last();
                                else newVModel = vModel.First() + vModel.Last();
                                newEntity = newEntity.Replace(ctItem.__vModel__, newVModel);
                            }
                        }
                    }
                }
                else if(item.__vModel__.Length > 14 && item.__vModel__.Contains("FIELD"))
                {
                    var vModel = item.__vModel__.Split("FIELD");
                    if (vModel.Last().Length > 1)
                    {
                        var newVModel = string.Empty;
                        if (fieldsDic.ContainsKey(item.__config__.jnpfKey)) newVModel = fieldsDic[item.__config__.jnpfKey] + vModel.Last();
                        else newVModel = vModel.First() + vModel.Last();
                        newEntity = newEntity.Replace(item.__vModel__, newVModel);
                    }
                }
            }

            vEntity = newEntity.ToObjectOld<VisualDevEntity>();
            formModel = vEntity.FormData.ToObjectOld<FormDataModel>();
            fieldsModelList = TemplateAnalysis.AnalysisTemplateData(formModel.fields, true);
        }
        #endregion

        // 主表表名
        var regex = "^[a-zA-Z][a-zA-Z0-9._-]*$";
        var cusTableList = new List<string>();
        var mainTableName = string.Empty;
        if (formModel.tableName.IsNotEmptyOrNull())
        {
            if (!Regex.IsMatch(formModel.tableName, regex))
                throw Oops.Oh(ErrorCode.D1525);
            if (formModel.tableName.Length > 30)
                throw Oops.Oh(ErrorCode.D1526);
            if (await _sensitiveDetectionProvider.VaildedDataBaseAsync(formModel.tableName.ToUpper()))
                throw Oops.Oh(ErrorCode.D1523, formModel.tableName);
            if (_changeDataBase.IsAnyTable(link, formModel.tableName))
                throw Oops.Oh(ErrorCode.D1524, formModel.tableName);
            mainTableName = formModel.tableName;
            cusTableList.Add(formModel.tableName);
        }
        else
        {
            var id = link.DbType.ToLower().Equals("oracle") ? vEntity.Id.Substring(vEntity.Id.Length - 8) : vEntity.Id;
            mainTableName = "mt" + id;
        }

        if (isUpper) mainTableName = mainTableName.ToUpper();

        #region 创表信息组装

        List<DbTableAndFieldModel>? addTableList = new List<DbTableAndFieldModel>(); // 表集合

        // 主表信息
        DbTableAndFieldModel? mainInfo = new DbTableAndFieldModel();
        mainInfo.table = mainTableName;
        mainInfo.tableName = vEntity.FullName;
        mainInfo.FieldList = FieldsModelToTableFile(fieldsModelList, formModel.primaryKeyPolicy == 2, isUpper);

        var flowTaskId = isUpper ? "F_FLOW_TASK_ID" : "f_flow_task_id";
        var flowId = isUpper ? "F_FLOW_ID" : "f_flow_id";
        var flowState = isUpper ? "F_FLOW_STATE" : "f_flow_state";
        mainInfo.FieldList.Add(new DbTableFieldModel() { field = flowTaskId, fieldName = "流程任务Id", dataType = "varchar", dataLength = "50", allowNull = 1 });
        mainInfo.FieldList.Add(new DbTableFieldModel() { field = flowId, fieldName = "流程引擎Id", dataType = "varchar", dataLength = "50", allowNull = 1 });
        mainInfo.FieldList.Add(new DbTableFieldModel() { field = flowState, fieldName = "流程状态", dataType = "int", dataLength = "1", allowNull = 1 });

        if (formModel.logicalDelete)
        {
            var deleteMark = isUpper ? "F_DELETE_MARK" : "f_delete_mark";
            var deleteUserId = isUpper ? "F_DELETE_USER_ID" : "f_delete_user_id";
            var deleteTime = isUpper ? "F_DELETE_TIME" : "f_delete_time";

            mainInfo.FieldList.Add(new DbTableFieldModel() { field = deleteMark, fieldName = "删除标识", dataType = "int", dataLength = "1", allowNull = 1 });
            mainInfo.FieldList.Add(new DbTableFieldModel() { field = deleteUserId, fieldName = "删除用户", dataType = "varchar", dataLength = "50", allowNull = 1 });
            mainInfo.FieldList.Add(new DbTableFieldModel() { field = deleteTime, fieldName = "删除时间", dataType = "datetime", dataLength = "50", allowNull = 1 });
        }

        if (formModel.concurrencyLock)
        {
            var version = isUpper ? "F_VERSION" : "f_version";
            mainInfo.FieldList.Add(new DbTableFieldModel() { field = version, fieldName = "并发锁定字段", dataType = "bigint", dataLength = "20", allowNull = 1 });
        }

        // 子表信息
        Dictionary<string, string>? childTableDic = new Dictionary<string, string>();

        foreach (var item in fieldsModelList.Where(x => x.__config__.jnpfKey == JnpfKeyConst.TABLE).ToList())
        {
            DbTableAndFieldModel? childTInfo = new DbTableAndFieldModel();

            // 子表表名
            if (item.__config__.tableName.IsNotEmptyOrNull())
            {
                if (!Regex.IsMatch(item.__config__.tableName, regex))
                    throw Oops.Oh(ErrorCode.D1525);
                if (item.__config__.tableName.Length > 30)
                    throw Oops.Oh(ErrorCode.D1526);
                if (await _sensitiveDetectionProvider.VaildedDataBaseAsync(item.__config__.tableName.ToUpper()))
                    throw Oops.Oh(ErrorCode.D1523, item.__config__.tableName);
                if (cusTableList.Contains(item.__config__.tableName) || _changeDataBase.IsAnyTable(link, item.__config__.tableName))
                    throw Oops.Oh(ErrorCode.D1524, item.__config__.tableName);
                childTInfo.table = item.__config__.tableName;
                cusTableList.Add(item.__config__.tableName);
            }
            else
            {
                var childId = SnowflakeIdHelper.NextId();
                if (link.DbType.ToLower().Equals("oracle")) childId = childId.Substring(childId.Length - 8);
                childTInfo.table = "ct" + childId;
            }

            if (isUpper) childTInfo.table = childTInfo.table.ToUpper();
            childTableDic.Add(item.__vModel__, childTInfo.table);
            childTInfo.tableName = vEntity.FullName + "_子表";
            childTInfo.FieldList = FieldsModelToTableFile(item.__config__.children, formModel.primaryKeyPolicy == 2, isUpper);

            var foreignId = isUpper ? "F_FOREIGN_ID" : "f_foreign_id";
            childTInfo.FieldList.Add(new DbTableFieldModel() { dataLength = "50", allowNull = 1, dataType = "varchar", field = foreignId, fieldName = vEntity.FullName + "_关联外键" });

            addTableList.Add(childTInfo);
        }

        #endregion

        #region 修改功能模板 有表改无表
        List<TableModel>? modelTableList = new List<TableModel>();

        // 处理主表
        TableModel? mainTable = new TableModel();
        mainTable.fields = new List<EntityFieldModel>();
        mainTable.table = mainInfo.table;
        mainTable.tableName = mainInfo.tableName;
        mainTable.typeId = "1";
        mainInfo.FieldList.ForEach(item => // 表字段
        {
            EntityFieldModel? etFieldModel = new EntityFieldModel();
            etFieldModel.dataLength = item.dataLength;
            etFieldModel.primaryKey = item.primaryKey.ParseToInt();
            etFieldModel.dataType = item.dataType;
            etFieldModel.field = item.field;
            etFieldModel.fieldName = item.fieldName;
            etFieldModel.allowNull = item.allowNull;
            etFieldModel.identity = item.identity ? 1 : 0;
            mainTable.fields.Add(etFieldModel);
        });

        // 处理子表
        addTableList.ForEach(item =>
        {
            TableModel? childInfo = new TableModel();
            childInfo.fields = new List<EntityFieldModel>();
            childInfo.table = item.table;
            childInfo.tableName = item.tableName;
            childInfo.tableField = isUpper ? "F_FOREIGN_ID" : "f_foreign_id"; // 关联外键
            childInfo.relationField = isUpper ? "F_ID" : "f_id"; // 关联主键
            childInfo.typeId = "0";
            item.FieldList.ForEach(it => // 子表字段
            {
                EntityFieldModel? etFieldModel = new EntityFieldModel();
                etFieldModel.dataLength = it.dataLength;
                etFieldModel.primaryKey = it.primaryKey.ParseToInt();
                etFieldModel.dataType = it.dataType;
                etFieldModel.field = it.field;
                etFieldModel.fieldName = it.fieldName;
                etFieldModel.allowNull = it.allowNull;
                etFieldModel.identity = it.identity ? 1 : 0;
                childInfo.fields.Add(etFieldModel);
            });
            modelTableList.Add(childInfo);
        });
        modelTableList.Add(mainTable);

        #region 给控件绑定 tableName、relationTable 属性

        // 用字典反序列化， 避免多增加不必要的属性
        Dictionary<string, object>? dicFormModel = vEntity.FormData.ToObjectOld<Dictionary<string, object>>();
        List<Dictionary<string, object>>? dicFieldsModelList = dicFormModel.FirstOrDefault(x => x.Key == "fields").Value.ToJsonString().ToObjectOld<List<Dictionary<string, object>>>();

        // 给控件绑定 tableName
        FieldBindTable(dicFieldsModelList, childTableDic, mainTableName);

        #endregion

        dicFormModel["fields"] = dicFieldsModelList; // 修改表单控件
        vEntity.FormData = dicFormModel.ToJsonString(); // 修改模板
        vEntity.Tables = modelTableList.ToJsonString(); // 修改模板涉及表

        addTableList.Add(mainInfo);

        #endregion

        try
        {
            _db.BeginTran(); // 执行事务
            foreach (DbTableAndFieldModel? item in addTableList)
            {
                bool res = await _changeDataBase.Create(link, item, item.FieldList);
                if (!res) throw null;
            }

            if (await _visualDevRepository.IsAnyAsync(x => x.Id.Equals(vEntity.Id)))
                await _visualDevRepository.AsUpdateable(vEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            else
                await _visualDevRepository.AsInsertable(vEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();

            _db.CommitTran(); // 提交事务

            return vEntity;
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran(); // 回滚事务
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 组件转换表字段.
    /// </summary>
    /// <param name="fmList">表单列表.</param>
    /// <param name="isIdentity">主键是否自增长.</param>
    /// <param name="isUpper">是否大写.</param>
    /// <returns></returns>
    [NonAction]
    private List<DbTableFieldModel> FieldsModelToTableFile(List<FieldsModel> fmList, bool isIdentity, bool isUpper)
    {
        List<DbTableFieldModel>? fieldList = new List<DbTableFieldModel>(); // 表字段
        List<FieldsModel>? mList = fmList.Where(x => x.__config__.jnpfKey.IsNotEmptyOrNull())
            .Where(x => x.__config__.jnpfKey != JnpfKeyConst.QRCODE && x.__config__.jnpfKey != JnpfKeyConst.BARCODE && x.__config__.jnpfKey != JnpfKeyConst.TABLE).ToList(); // 非存储字段
        fieldList.Add(new DbTableFieldModel()
        {
            primaryKey = true,
            dataType = isIdentity ? "int" : "varchar",
            dataLength = "50",
            identity = isIdentity,
            field = isUpper ? "F_ID" : "f_id",
            fieldName = "主键"
        });
        fieldList.Add(new DbTableFieldModel()
        {
            primaryKey = true,
            dataType = "varchar",
            dataLength = "50",
            field = isUpper ? "F_TENANT_ID" : "f_tenant_id",
            fieldName = "租户主键"
        });

        foreach (var item in mList)
        {
            // 不生成数据库字段(控件类型为：展示数据)，关联表单、弹窗选择、计算公式.
            if ((item.__config__.jnpfKey == JnpfKeyConst.RELATIONFORMATTR || item.__config__.jnpfKey == JnpfKeyConst.POPUPATTR || item.__config__.jnpfKey == JnpfKeyConst.CALCULATE || item.__config__.jnpfKey == JnpfKeyConst.DATECALCULATE) && item.isStorage.Equals(0))
                continue;
            DbTableFieldModel? field = new DbTableFieldModel();
            field.field = item.__vModel__;
            field.fieldName = item.__config__.label;
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.NUMINPUT:
                case JnpfKeyConst.CALCULATE:
                    field.dataType = "decimal";
                    field.dataLength = "38";
                    field.decimalDigits = item.precision.IsNullOrEmpty() ? 15 : item.precision;
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.TIME:
                case JnpfKeyConst.SWITCH:
                case JnpfKeyConst.COLORPICKER:
                case JnpfKeyConst.CREATEUSER:
                case JnpfKeyConst.MODIFYUSER:
                    field.dataType = "varchar";
                    field.dataLength = "50";
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.DATE:
                case JnpfKeyConst.CREATETIME:
                case JnpfKeyConst.MODIFYTIME:
                case JnpfKeyConst.DATECALCULATE:
                    field.dataType = "DateTime";
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.EDITOR:
                case JnpfKeyConst.UPLOADFZ:
                case JnpfKeyConst.UPLOADIMG:
                case JnpfKeyConst.SIGN:
                    field.dataType = "longtext";
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.RATE:
                    field.dataType = "decimal";
                    field.dataLength = "38";
                    field.decimalDigits = 1;
                    field.allowNull = 1;
                    break;
                case JnpfKeyConst.SLIDER:
                    field.dataType = "decimal";
                    field.dataLength = "38";
                    field.decimalDigits = 15;
                    field.allowNull = 1;
                    break;
                default:
                    field.dataType = "varchar";
                    field.dataLength = "255";
                    field.allowNull = 1;
                    break;
            }

            if (field.field.IsNotEmptyOrNull()) fieldList.Add(field);
        }

        return fieldList;
    }

    /// <summary>
    /// 组装菜单 数据权限 方案管理数据.
    /// </summary>
    /// <param name="menuId">菜单ID.</param>
    /// <param name="authList">字段管理列表.</param>
    /// <param name="fields">功能模板控件集合.</param>
    /// <returns></returns>
    private async Task MenuMergeDataAuthScheme(string menuId, List<FieldsModel> fields)
    {
        // 旧的自动生成的 方案管理
        var oldDataAuthScheme = await _visualDevRepository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>()
            .Where(x => x.ModuleId == menuId && x.DeleteMark == null && x.SortCode.Equals(-9527))
            .ToListAsync();

        var authSchemeList = new List<ModuleDataAuthorizeSchemeEntity>();
        var condJson = new AuthorizeModuleResourceConditionModelTopInput()
        {
            matchLogic = "and",
            conditionList = new List<AuthorizeModuleResourceConditionModelInput>()
            {
                new AuthorizeModuleResourceConditionModelInput()
                {
                    logic = "and",
                    groups = new List<AuthorizeModuleResourceConditionItemModelInput>() { new AuthorizeModuleResourceConditionItemModelInput() }
                }
            }
        };

        // 当前用户
        var item = fields.Find(x => x.__config__.jnpfKey == JnpfKeyConst.CREATEUSER);
        if (item != null)
        {
            condJson.conditionList.First().groups.First().cellKey = SnowflakeIdHelper.NextId();
            condJson.conditionList.First().groups.First().id = item.__vModel__;
            condJson.conditionList.First().groups.First().field = item.__vModel__;
            condJson.conditionList.First().groups.First().fieldId = item.__vModel__;
            condJson.conditionList.First().groups.First().fullName = item.__config__.label;
            condJson.conditionList.First().groups.First().fieldName = item.__config__.label;
            condJson.conditionList.First().groups.First().jnpfKey = item.__config__.jnpfKey;
            condJson.conditionList.First().groups.First().tableName = item.__config__.tableName;
            condJson.conditionList.First().groups.First().symbol = "==";
            condJson.conditionList.First().groups.First().symbolName = "等于";
            condJson.conditionList.First().groups.First().fieldValue = CommonConst.SysParamUserId;
            condJson.conditionList.First().groups.First().__config__ = new configModel { jnpfKey = item.__config__.jnpfKey, label = item.__config__.label };

            // 新增
            if (!oldDataAuthScheme.Any(x => x.ConditionText == item.__config__.label + " 等于当前用户"))
            {
                authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                {
                    FullName = "当前用户",
                    EnCode = SnowflakeIdHelper.NextId(),
                    SortCode = -9527,
                    ConditionText = item.__config__.label + " 等于当前用户",
                    ConditionJson = condJson.ToJsonStringOld(),
                    ModuleId = menuId,
                });
            }

            condJson.conditionList.First().groups.First().cellKey = SnowflakeIdHelper.NextId();
            condJson.conditionList.First().groups.First().symbol = "in";
            condJson.conditionList.First().groups.First().symbolName = "包含任意一个";
            condJson.conditionList.First().groups.First().fieldValue = CommonConst.SysParamUserAndSub;
            if (!oldDataAuthScheme.Any(x => x.ConditionText == item.__config__.label + " 包含任意一个当前用户及下属"))
            {
                authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                {
                    FullName = "当前用户及下属",
                    EnCode = SnowflakeIdHelper.NextId(),
                    SortCode = -9527,
                    ConditionText = item.__config__.label + " 包含任意一个当前用户及下属",
                    ConditionJson = condJson.ToJsonStringOld(),
                    ModuleId = menuId,
                });
            }
        }

        // 当前组织
        item = fields.Find(x => x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE);
        if (item != null)
        {
            condJson.conditionList.First().groups.First().cellKey = SnowflakeIdHelper.NextId();
            condJson.conditionList.First().groups.First().id = item.__vModel__;
            condJson.conditionList.First().groups.First().field = item.__vModel__;
            condJson.conditionList.First().groups.First().fieldId = item.__vModel__;
            condJson.conditionList.First().groups.First().fullName = item.__config__.label;
            condJson.conditionList.First().groups.First().fieldName = item.__config__.label;
            condJson.conditionList.First().groups.First().jnpfKey = item.__config__.jnpfKey;
            condJson.conditionList.First().groups.First().tableName = item.__config__.tableName;
            condJson.conditionList.First().groups.First().fieldValue = CommonConst.SysParamOrganizeId;
            condJson.conditionList.First().groups.First().__config__ = new configModel { jnpfKey = item.__config__.jnpfKey, label = item.__config__.label };

            // 新增
            if (!oldDataAuthScheme.Any(x => x.ConditionText == item.__config__.label + " 包含任意一个当前组织"))
            {
                authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                {
                    FullName = "当前组织",
                    EnCode = SnowflakeIdHelper.NextId(),
                    SortCode = -9527,
                    ConditionText = item.__config__.label + " 包含任意一个当前组织",
                    ConditionJson = condJson.ToJsonStringOld(),
                    ModuleId = menuId,
                });
            }

            condJson.conditionList.First().groups.First().cellKey = SnowflakeIdHelper.NextId();
            condJson.conditionList.First().groups.First().fieldValue = CommonConst.SysParamOrganizeAndSub;
            if (!oldDataAuthScheme.Any(x => x.ConditionText == item.__config__.label + " 包含任意一个当前组织及子组织"))
            {
                authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                {
                    FullName = "当前组织及子组织",
                    EnCode = SnowflakeIdHelper.NextId(),
                    SortCode = -9527,
                    ConditionText = item.__config__.label + " 包含任意一个当前组织及子组织",
                    ConditionJson = condJson.ToJsonStringOld(),
                    ModuleId = menuId,
                });
            }
        }

        // 当前岗位
        item = fields.Find(x => x.__config__.jnpfKey == JnpfKeyConst.CURRPOSITION);
        if (item != null)
        {
            condJson.conditionList.First().groups.First().cellKey = SnowflakeIdHelper.NextId();
            condJson.conditionList.First().groups.First().id = item.__vModel__;
            condJson.conditionList.First().groups.First().field = item.__vModel__;
            condJson.conditionList.First().groups.First().fieldId = item.__vModel__;
            condJson.conditionList.First().groups.First().fullName = item.__config__.label;
            condJson.conditionList.First().groups.First().fieldName = item.__config__.label;
            condJson.conditionList.First().groups.First().jnpfKey = item.__config__.jnpfKey;
            condJson.conditionList.First().groups.First().tableName = item.__config__.tableName;
            condJson.conditionList.First().groups.First().fieldValue = CommonConst.SysParamPositionId;
            condJson.conditionList.First().groups.First().__config__ = new configModel { jnpfKey = item.__config__.jnpfKey, label = item.__config__.label };

            // 新增
            if (!oldDataAuthScheme.Any(x => x.ConditionText == item.__config__.label + " 包含任意一个当前岗位"))
            {
                authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                {
                    FullName = "当前岗位",
                    EnCode = SnowflakeIdHelper.NextId(),
                    SortCode = -9527,
                    ConditionText = item.__config__.label + " 包含任意一个当前岗位",
                    ConditionJson = condJson.ToJsonStringOld(),
                    ModuleId = menuId,
                });
            }

            condJson.conditionList.First().groups.First().cellKey = SnowflakeIdHelper.NextId();
            condJson.conditionList.First().groups.First().fieldValue = CommonConst.SysParamPositionAndSub;
            if (!oldDataAuthScheme.Any(x => x.ConditionText == item.__config__.label + " 包含任意一个当前岗位及子岗位"))
            {
                authSchemeList.Add(new ModuleDataAuthorizeSchemeEntity()
                {
                    FullName = "当前岗位及子岗位",
                    EnCode = SnowflakeIdHelper.NextId(),
                    SortCode = -9527,
                    ConditionText = item.__config__.label + " 包含任意一个当前岗位及子岗位",
                    ConditionJson = condJson.ToJsonStringOld(),
                    ModuleId = menuId,
                });
            }
        }

        if (authSchemeList.Any()) await _visualDevRepository.AsSugarClient().Insertable(authSchemeList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 无限递归 给控件绑定tableName (绕过 布局控件).
    /// </summary>
    private void FieldBindTable(List<Dictionary<string, object>> dicFieldsModelList, Dictionary<string, string> childTableDic, string tableName)
    {
        foreach (var item in dicFieldsModelList)
        {
            var obj = item["__config__"].ToObject<Dictionary<string, object>>();

            if (obj.ContainsKey("jnpfKey") && obj["jnpfKey"].Equals(JnpfKeyConst.TABLE)) obj["tableName"] = childTableDic[item["__vModel__"].ToString()];
            else if (obj.ContainsKey("tableName")) obj["tableName"] = tableName;

            // 关联表单属性和弹窗属性
            if (obj.ContainsKey("jnpfKey") && (obj["jnpfKey"].Equals(JnpfKeyConst.RELATIONFORMATTR) || obj["jnpfKey"].Equals(JnpfKeyConst.POPUPATTR)))
            {
                string relationField = Convert.ToString(item["relationField"]);
                string? rField = relationField.ReplaceRegex(@"_jnpfTable_(\w+)", string.Empty);
                item["relationField"] = string.Format("{0}{1}{2}{3}", rField, "_jnpfTable_", tableName, "1");
            }

            // 子表控件
            if (obj.ContainsKey("jnpfKey") && obj["jnpfKey"].Equals(JnpfKeyConst.TABLE))
            {
                var cList = obj["children"].ToObject<List<Dictionary<string, object>>>();
                foreach (var child in cList)
                {
                    var cObj = child["__config__"].ToObject<Dictionary<string, object>>();
                    if (cObj.ContainsKey("relationTable")) cObj["relationTable"] = childTableDic[item["__vModel__"].ToString()];
                    else cObj.Add("relationTable", childTableDic[item["__vModel__"].ToString()]);

                    if (cObj.ContainsKey("tableName")) cObj["tableName"] = obj["tableName"];

                    // 关联表单属性和弹窗属性
                    if (cObj.ContainsKey("jnpfKey") && (cObj["jnpfKey"].Equals(JnpfKeyConst.RELATIONFORMATTR) || cObj["jnpfKey"].Equals(JnpfKeyConst.POPUPATTR)))
                    {
                        string relationField = Convert.ToString(child["relationField"]);
                        string? rField = relationField.ReplaceRegex(@"_jnpfTable_(\w+)", string.Empty);
                        if (child.ContainsKey("relationField")) child["relationField"] = string.Format("{0}{1}{2}{3}", rField, "_jnpfTable_", cObj["tableName"], "0");
                        else child.Add("relationField", string.Format("{0}{1}{2}{3}", rField, "_jnpfTable_", cObj["tableName"], "0"));
                    }

                    child["__config__"] = cObj;
                }

                obj["children"] = cList;
            }

            // 递归
            if (obj.ContainsKey("children") && !obj["jnpfKey"].Equals(JnpfKeyConst.TABLE))
            {
                var fmList = obj["children"].ToObject<List<Dictionary<string, object>>>();
                FieldBindTable(fmList, childTableDic, tableName);
                obj["children"] = fmList;
            }

            item["__config__"] = obj;
        }
    }

    /// <summary>
    /// 验证主键策略 数据库表是否支持.
    /// </summary>
    /// <param name="tInfo">模板信息.</param>
    /// <param name="dbLinkId">数据库连接id.</param>
    private async Task VerifyPrimaryKeyPolicy(TemplateParsingBase tInfo, string dbLinkId)
    {
        if (tInfo.IsHasTable)
        {
            DbLinkEntity link = await _runService.GetDbLink(dbLinkId);
            tInfo.AllTable.ForEach(item =>
            {
                List<DbTableFieldModel>? tableList = _changeDataBase.GetFieldList(link, item.table); // 获取主表所有列
                var mainPrimary = tableList.Find(t => t.primaryKey && t.field.ToLower() != "f_tenant_id"); // 主表主键
                if (mainPrimary == null) throw Oops.Oh(ErrorCode.D1409, "主键为空", item.table);

                if (tInfo.FormModel.primaryKeyPolicy.Equals(2) && !mainPrimary.identity)
                {
                    throw Oops.Oh(ErrorCode.D1409, "自增长ID,没有自增标识", item.table);
                }
                //if (tInfo.FormModel.primaryKeyPolicy.Equals(1) && !(mainPrimary.dataType.ToLower().Equals("string") || mainPrimary.dataType.ToLower().Equals("varchar") || mainPrimary.dataType.ToLower().Equals("nvarchar")))
                //    throw Oops.Oh(ErrorCode.D1409, "雪花ID", item.table);
            });
        }
    }

    /// <summary>
    /// 同步业务字段.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="tInfo"></param>
    /// <returns></returns>
    private async Task SyncField(VisualDevEntity entity, TemplateParsingBase tInfo)
    {
        if (entity.Tables.IsNotEmptyOrNull() && !entity.Tables.Equals("[]") && entity.FormData.IsNotEmptyOrNull())
        {
            tInfo.DbLink = await _runService.GetDbLink(entity.DbLinkId);
            await _runService.SyncField(tInfo);
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

    /// <summary>
    /// 生成表单回传菜单.
    /// </summary>
    private async Task GenerateHcMenu(VisualDevEntity entity, VisualDevToMenuInput input, List<ModuleEntity> allModuleList, Dictionary<string, List<string>> release)
    {
        var module = new ModuleEntity();
        module.FullName = entity.FullName;
        module.ModuleId = entity.Id;
        module.Icon = "icon-ym icon-ym-webForm";
        module.SystemId = _userManager.SystemId;
        module.IsColumnAuthorize = 1;
        module.IsButtonAuthorize = 1;
        module.IsFormAuthorize = 1;
        module.IsDataAuthorize = 1;
        module.Type = 11;
        module.SortCode = 999;
        module.PropertyJson = new ModulePropertyModel() { moduleId = entity.Id }.ToJsonStringOld();

        var newModuleList = new List<ModuleEntity>();
        var newModuleButtonList = new List<ModuleButtonEntity>();
        var moduleButtonList = entity.ButtonData.IsNotEmptyOrNull() && !entity.ButtonData.Equals("[]") ? await _visualDevRepository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(x => x.DeleteMark == null).ToListAsync() : new List<ModuleButtonEntity>();
        var newModuleFormList = new List<ModuleFormEntity>();
        var moduleFormList = entity.FormData.IsNotEmptyOrNull() && !entity.FormData.Equals("[]") ? await _visualDevRepository.AsSugarClient().Queryable<ModuleFormEntity>().Where(x => x.DeleteMark == null).ToListAsync() : new List<ModuleFormEntity>();
        var newModuleColumnList = new List<ModuleColumnEntity>();
        var moduleColumnList = entity.ColumnData.IsNotEmptyOrNull() && !entity.ColumnData.Equals("[]") ? await _visualDevRepository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(x => x.DeleteMark == null).ToListAsync() : new List<ModuleColumnEntity>();

        if (input.pc == 1)
        {
            foreach (var wItem in release["Web"])
            {
                var wId = string.Empty;
                var pId = wItem.Equals("-1") ? _userManager.SystemId : wItem;
                if (input.pcModuleParentId.Contains(pId))
                {
                    var newModule = module.Adapt<ModuleEntity>();
                    newModule.Creator();
                    newModule.EnCode = entity.EnCode + new Random().NextLetterAndNumberString(5);
                    newModule.Category = "Web";
                    newModule.ParentId = wItem;
                    newModule.UrlAddress = "form/" + entity.EnCode;
                    newModule.PageAddress = entity.WebAddress;
                    newModuleList.Add(newModule);
                    wId = newModule.Id;
                }
                else
                {
                    wId = allModuleList.Find(x => x.ParentId.Equals(wItem))?.Id;
                }

                // 按钮设置
                if (entity.ButtonData.IsNotEmptyOrNull() && !entity.ButtonData.Equals("[]"))
                {
                    foreach (var buttonData in entity.ButtonData.ToObject<List<VisualHcButtonSetModel>>())
                    {
                        if (!moduleButtonList.Any(x => x.EnCode.Equals(buttonData.enCode) && x.ModuleId.Equals(wId)))
                        {
                            var button = buttonData.Adapt<ModuleButtonEntity>();
                            button.Creator();
                            button.EnabledMark = buttonData.show ? 1 : 0;
                            button.ParentId = "-1";
                            button.ModuleId = wId;
                            newModuleButtonList.Add(button);
                        }
                    }
                }

                // 表单字段
                if (entity.FormData.IsNotEmptyOrNull() && !entity.FormData.Equals("[]"))
                {
                    foreach (var formData in entity.FormData.ToObject<List<VisualHcFormSetModel>>())
                    {
                        if (!moduleFormList.Any(x => x.EnCode.Equals(formData.fieldId) && x.ModuleId.Equals(wId)))
                        {
                            var form = new ModuleFormEntity();
                            form.Creator();
                            form.FullName = formData.fieldName;
                            form.BindTable = formData.tableName;
                            form.ParentId = "-1";
                            form.ModuleId = wId;
                            form.FieldRule = 0;
                            form.EnCode = formData.fieldId;
                            if (formData.fieldId.ToLower().Contains("_jnpf_"))
                            {
                                form.FieldRule = 1;
                            }
                            else if (formData.fieldId.ToLower().Contains("tablefield"))
                            {
                                form.FieldRule = 2;
                                var field = formData.fieldId.Split("-");
                                form.ChildTableKey = field.First();
                            }

                            newModuleFormList.Add(form);
                        }
                    }
                }

                // 列表字段
                if (entity.ColumnData.IsNotEmptyOrNull() && !entity.ColumnData.Equals("[]"))
                {
                    foreach (var columnData in entity.ColumnData.ToObject<List<VisualHcFormSetModel>>())
                    {
                        if (!moduleColumnList.Any(x => x.EnCode.Equals(columnData.fieldId) && x.ModuleId.Equals(wId)))
                        {
                            var column = new ModuleColumnEntity();
                            column.Creator();
                            column.FullName = columnData.fieldName;
                            column.BindTable = columnData.tableName;
                            column.ParentId = "-1";
                            column.ModuleId = wId;
                            column.FieldRule = 0;
                            column.EnCode = columnData.fieldId;
                            if (columnData.fieldId.ToLower().Contains("_jnpf_"))
                            {
                                column.FieldRule = 1;
                            }
                            else if (columnData.fieldId.ToLower().Contains("tablefield"))
                            {
                                column.FieldRule = 2;
                                var field = columnData.fieldId.Split("-");
                                column.ChildTableKey = field.First();
                            }

                            newModuleColumnList.Add(column);
                        }
                    }
                }
            }
        }

        if (input.app == 1)
        {
            foreach (var aItem in release["App"])
            {
                var aId = string.Empty;
                var pId = aItem.Equals("-1") ? _userManager.SystemId : aItem;
                if (input.appModuleParentId.Contains(pId))
                {
                    var newModule = module.Adapt<ModuleEntity>();
                    newModule.Creator();
                    newModule.EnCode = entity.EnCode + new Random().NextLetterAndNumberString(5);
                    newModule.Category = "App";
                    newModule.ParentId = aItem;
                    newModule.UrlAddress = entity.AppAddress;
                    newModule.PageAddress = entity.AppAddress;
                    newModuleList.Add(newModule);
                    aId = newModule.Id;
                }
                else
                {
                    aId = allModuleList.Find(x => x.ParentId.Equals(aItem))?.Id;
                }

                // 按钮设置
                if (entity.AppButtonData.IsNotEmptyOrNull() && !entity.AppButtonData.Equals("[]"))
                {
                    foreach (var buttonData in entity.AppButtonData.ToObject<List<VisualHcButtonSetModel>>())
                    {
                        if (!moduleButtonList.Any(x => x.EnCode.Equals(buttonData.enCode) && x.ModuleId.Equals(aId)))
                        {
                            var button = buttonData.Adapt<ModuleButtonEntity>();
                            button.Creator();
                            button.EnabledMark = buttonData.show ? 1 : 0;
                            button.ParentId = "-1";
                            button.ModuleId = aId;
                            newModuleButtonList.Add(button);
                        }
                    }
                }

                // 表单字段
                if (entity.FormData.IsNotEmptyOrNull() && !entity.FormData.Equals("[]"))
                {
                    foreach (var formData in entity.FormData.ToObject<List<VisualHcFormSetModel>>())
                    {
                        if (!moduleFormList.Any(x => x.EnCode.Equals(formData.fieldId) && x.ModuleId.Equals(aId)))
                        {
                            var form = new ModuleFormEntity();
                            form.Creator();
                            form.FullName = formData.fieldName;
                            form.BindTable = formData.tableName;
                            form.ParentId = "-1";
                            form.ModuleId = aId;
                            form.FieldRule = 0;
                            form.EnCode = formData.fieldId;
                            if (formData.fieldId.ToLower().Contains("_jnpf_"))
                            {
                                form.FieldRule = 1;
                            }
                            else if (formData.fieldId.ToLower().Contains("tablefield"))
                            {
                                form.FieldRule = 2;
                                var field = formData.fieldId.Split("-");
                                form.ChildTableKey = field.First();
                            }

                            newModuleFormList.Add(form);
                        }
                    }
                }

                // 列表字段
                if (entity.AppColumnData.IsNotEmptyOrNull() && !entity.AppColumnData.Equals("[]"))
                {
                    foreach (var columnData in entity.AppColumnData.ToObject<List<VisualHcFormSetModel>>())
                    {
                        if (!moduleColumnList.Any(x => x.EnCode.Equals(columnData.fieldId) && x.ModuleId.Equals(aId)))
                        {
                            var column = new ModuleColumnEntity();
                            column.Creator();
                            column.FullName = columnData.fieldName;
                            column.BindTable = columnData.tableName;
                            column.ParentId = "-1";
                            column.ModuleId = aId;
                            column.FieldRule = 0;
                            column.EnCode = columnData.fieldId;
                            if (columnData.fieldId.ToLower().Contains("_jnpf_"))
                            {
                                column.FieldRule = 1;
                            }
                            else if (columnData.fieldId.ToLower().Contains("tablefield"))
                            {
                                column.FieldRule = 2;
                                var field = columnData.fieldId.Split("-");
                                column.ChildTableKey = field.First();
                            }

                            newModuleColumnList.Add(column);
                        }
                    }
                }
            }
        }

        await _visualDevRepository.AsSugarClient().Insertable(newModuleList).ExecuteCommandAsync();
        await _visualDevRepository.AsSugarClient().Insertable(newModuleButtonList).ExecuteCommandAsync();
        await _visualDevRepository.AsSugarClient().Insertable(newModuleFormList).ExecuteCommandAsync();
        await _visualDevRepository.AsSugarClient().Insertable(newModuleColumnList).ExecuteCommandAsync();
    }

    #endregion
}
