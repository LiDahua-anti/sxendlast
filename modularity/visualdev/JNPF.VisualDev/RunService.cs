using JNPF.Common.Const;
using JNPF.Common.Core.EventBus.Sources;
using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Dtos.VisualDev;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.VisualDev;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model;
using JNPF.EventBus;
using JNPF.FriendlyException;
using JNPF.JsonSerialization;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.UnifyResult;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Entitys.Model;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Data;

namespace JNPF.VisualDev;

/// <summary>
/// 在线开发运行服务 .
/// </summary>
public class RunService : IRunService, ITransient, IDisposable
{
    #region 构造

    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceScope _serviceScope;

    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevEntity> _visualDevRepository;  // 在线开发功能实体

    /// <summary>
    /// SqlSugarClient客户端.
    /// </summary>
    private SqlSugarScope _sqlSugarClient;

    /// <summary>
    /// 表单数据解析.
    /// </summary>
    private readonly FormDataParsing _formDataParsing;

    /// <summary>
    /// 切库.
    /// </summary>
    private readonly IDataBaseManager _databaseService;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 事件总线.
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 事务.
    /// </summary>
    private readonly ITenant _db;

    /// <summary>
    /// 流程数据.
    /// </summary>
    private readonly IWorkFlowRepository _flowTaskRepository;

    /// <summary>
    /// 构造.
    /// </summary>
    public RunService(
        IServiceScopeFactory serviceScopeFactory,
        ISqlSugarRepository<VisualDevEntity> visualDevRepository,
        ISqlSugarClient sqlSugarClient,
        FormDataParsing formDataParsing,
        IOptions<TenantOptions> tenantOptions,
        IUserManager userManager,
        IDbLinkService dbLinkService,
        IDataBaseManager databaseService,
        IDataInterfaceService dataInterfaceService,
        ISqlSugarClient context,
        IBillRullService billRuleService,
        IEventPublisher eventPublisher,
        ICacheManager cacheManager,
        IWorkFlowRepository flowTaskRepository)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _visualDevRepository = visualDevRepository;
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
        _dataInterfaceService = dataInterfaceService;
        _formDataParsing = formDataParsing;
        _userManager = userManager;
        _tenant = tenantOptions.Value;
        _databaseService = databaseService;
        _dbLinkService = dbLinkService;
        _billRuleService = billRuleService;
        _eventPublisher = eventPublisher;
        _db = context.AsTenant();
        _cacheManager = cacheManager;
        _flowTaskRepository = flowTaskRepository;
    }
    #endregion

    private static readonly List<string> LinkAllowKey = new() { "alert", "card", "collapse", "collapseItem", "colorPicker", "datePicker", "divider", "editor",
            "groupTitle", "iframe", "input", "inputNumber", "link", "location", "rate", "row", "slider", "stepItem", "steps", "switch", "tab", "tabItem", "table", "tableGrid",
            "tableGridTd", "tableGridTr", "text", "textarea", "timePicker" };

    #region Get

    /// <summary>
    /// 列表数据处理.
    /// </summary>
    /// <param name="entity">功能实体.</param>
    /// <param name="input">查询参数.</param>
    /// <param name="actionType"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    public async Task<PageResult<Dictionary<string, object>>> GetListResult(VisualDevEntity entity, VisualDevModelListQueryInput input, string actionType = "List", string? tenantId = null)
    {
        PageResult<Dictionary<string, object>>? realList = new PageResult<Dictionary<string, object>>() { list = new List<Dictionary<string, object>>() }; // 返回结果集
        TemplateParsingBase templateInfo = new TemplateParsingBase(entity); // 解析模板控件
        if (entity.WebType.Equals(4)) return await GetDataViewResults(templateInfo, input); // 数据视图

        // 处理查询
        Dictionary<string, object> queryJson = string.IsNullOrEmpty(input.queryJson) ? null : input.queryJson.ToObject<Dictionary<string, object>>();
        if (queryJson != null)
        {
            foreach (KeyValuePair<string, object> item in queryJson)
            {
                if (!item.Key.Equals("jnpfFlowState"))
                {
                    if (!templateInfo.ColumnData.searchList.Any(it => it.id.Equals(item.Key)) && !item.Key.Equals(JnpfKeyConst.JNPFKEYWORD))
                    {
                        var vmodel = templateInfo.AllFieldsModel.Find(it => it.__vModel__.Equals(item.Key));
                        if (templateInfo.ColumnData.searchList.Any(it => it.id.Equals(item.Key)))
                        {
                            vmodel.searchMultiple = templateInfo.ColumnData.searchList.Find(it => it.id.Equals(item.Key)).searchMultiple;
                        }
                        var searchModel = vmodel.ToObject<IndexSearchFieldModel>();
                        searchModel.id = item.Key;
                        templateInfo.ColumnData.searchList.Add(searchModel);
                    }
                    if (!templateInfo.AppColumnData.searchList.Any(it => it.id.Equals(item.Key)) && !item.Key.Equals(JnpfKeyConst.JNPFKEYWORD))
                    {
                        var vmodel = templateInfo.AllFieldsModel.Find(it => it.__vModel__.Equals(item.Key));
                        if (templateInfo.AppColumnData.searchList.Any(it => it.id.Equals(item.Key)))
                        {
                            vmodel.searchMultiple = templateInfo.AppColumnData.searchList.Find(it => it.id.Equals(item.Key)).searchMultiple;
                        }
                        var searchModel = vmodel.ToObject<IndexSearchFieldModel>();
                        searchModel.id = item.Key;
                        templateInfo.AppColumnData.searchList.Add(searchModel);
                    }
                }
            }
        }

        if (input.extraQueryJson.IsNotEmptyOrNull())
        {
            foreach (var extraQuery in input.extraQueryJson.ToObject<Dictionary<string, object>>())
            {
                if (!templateInfo.ColumnData.searchList.Any(it => it.id.Equals(extraQuery.Key)))
                {
                    var vmodel = templateInfo.AllFieldsModel.Find(it => it.__vModel__.Equals(extraQuery.Key));
                    var searchModel = vmodel.ToObject<IndexSearchFieldModel>();
                    searchModel.id = extraQuery.Key;
                    searchModel.searchType = 1;
                    searchModel.multiple = false;
                    templateInfo.ColumnData.searchList.Add(searchModel);
                }

                if (!templateInfo.AppColumnData.searchList.Any(it => it.id.Equals(extraQuery.Key)))
                {
                    var vmodel = templateInfo.AllFieldsModel.Find(it => it.__vModel__.Equals(extraQuery.Key));
                    var searchModel = vmodel.ToObject<IndexSearchFieldModel>();
                    searchModel.id = extraQuery.Key;
                    searchModel.searchType = 1;
                    searchModel.multiple = false;
                    templateInfo.AppColumnData.searchList.Add(searchModel);
                }
            }
        }

        // 获取请求端类型，并对应获取 数据权限
        DbLinkEntity link = await GetDbLink(entity.DbLinkId, tenantId);
        templateInfo.DbLink = link;
        await SyncField(templateInfo); // 同步业务字段
        var primaryKey = templateInfo.MainTable?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
        if (primaryKey.IsNullOrEmpty()) primaryKey = GetPrimary(link, templateInfo.MainTableName);
        templateInfo.ColumnData = _userManager.UserOrigin == "pc" ? templateInfo.ColumnData : templateInfo.AppColumnData;

        // 数据权限
        bool udp = _userManager.UserOrigin == "pc" ? templateInfo.ColumnData.useDataPermission : templateInfo.AppColumnData.useDataPermission;
        var pvalue = new List<IConditionalModel>();
        if (input.flowIds.IsNullOrEmpty() && (_userManager.User != null || _userManager.UserId.IsNotEmptyOrNull())) pvalue = await _userManager.GetDataPermission(primaryKey, input.menuId, udp, templateInfo.FormModel.primaryKeyPolicy.Equals(2));
        if (templateInfo.ColumnData.type.Equals(5) && input.extraQueryJson.IsNullOrEmpty())
        {
            pvalue.Clear(); // 树形表格 去掉数据权限.
            input.pageSize = 999999;
        }
        input.dataPermissionJson = pvalue.ToJsonStringOld();

        // 所有查询条件
        var dataRuleWhere = new List<IConditionalModel>();
        var superQueryWhere = new List<IConditionalModel>();
        var dataRule = _userManager.UserOrigin == "pc" ? templateInfo.ColumnData.ruleList.ToJsonString() : templateInfo.AppColumnData.ruleListApp.ToJsonString();
        input.dataRuleJson = _userManager.AssembleQuery(dataRule);
        input.superQueryJson = _userManager.AssembleQuery(input.superQueryJson);
        if (input.superQueryJson.IsNotEmptyOrNull()) superQueryWhere = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(input.superQueryJson);
        if (input.dataRuleJson.IsNotEmptyOrNull()) dataRuleWhere = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(input.dataRuleJson);
        var queryWhere = new List<IConditionalModel>();
        if (input.flowIds.IsNotEmptyOrNull())
            input.flowIds = input.flowIds != "jnpf" ? input.flowIds : string.Empty;
        else
            input.flowIds = await GetFlowIds(input.menuId);
        queryWhere = GetQueryJson(input.queryJson, templateInfo.ColumnData, 1, input.flowIds);
        input.queryJson = queryWhere.ToJsonStringOld();
        if (input.extraQueryJson.IsNotEmptyOrNull())
        {
            var extraQueryWhere = GetQueryJson(input.extraQueryJson, templateInfo.ColumnData, 0);
            input.extraQueryJson = extraQueryWhere.ToJsonStringOld();
        }

        if (templateInfo.ColumnData.type == 4 && input.extraQueryJson.IsNullOrEmpty()) await OptimisticLocking(link, templateInfo); // 开启行编辑 处理 开启并发锁定
        Dictionary<string, string>? tableFieldKeyValue = new Dictionary<string, string>(); // 联表查询 表字段名称 对应 前端字段名称 (应对oracle 查询字段长度不能超过30个)
        string? sql = GetListQuerySql(primaryKey, templateInfo, ref input, ref tableFieldKeyValue); // 查询sql

        // 未开启分页
        if (!templateInfo.ColumnData.hasPage) input.pageSize = 999999;

        realList = _databaseService.GetInterFaceData(link, sql, input, templateInfo.ColumnData.Adapt<MainBeltViceQueryModel>(), tableFieldKeyValue);

        // 显示列有子表字段
        if ((entity.isShortLink || (templateInfo.ColumnData.type != 4 && templateInfo.ColumnData.columnList.Any(x => templateInfo.ChildTableFields.ContainsKey(x.prop)))) && realList.list.Any())
            realList = await GetListChildTable(templateInfo, primaryKey, queryWhere, dataRuleWhere, superQueryWhere, realList, pvalue, input.isConvertData);

        if (input.sidx.IsNullOrEmpty()) input.sidx = primaryKey;

        // 列表权限
        if (input.menuId.IsNotEmptyOrNull() && templateInfo.ColumnData.useColumnPermission && !_userManager.IsAdministrator)
        {
            var roles = _userManager.PermissionIds;
            if (roles.Any())
            {
                var items = await _visualDevRepository.AsSugarClient().Queryable<AuthorizeEntity>().In(a => a.ObjectId, roles).Where(a => a.ItemType == "column").GroupBy(it => new { it.ItemId }).Select(it => it.ItemId).ToListAsync();
                var columns = await _visualDevRepository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(a => items.Contains(a.Id) && a.ModuleId.Equals(input.menuId)).Where(a => a.EnabledMark == 1 && a.DeleteMark == null).Select(x => x.EnCode).ToListAsync();

                var newList = new List<Dictionary<string, object>>();
                foreach (var data in realList.list) newList.Add(GetFormAuthData(templateInfo, data, columns));
                realList.list = newList;
            }
        }

        // 增加前端回显字段 : key_name
        var roweditId = SnowflakeIdHelper.NextId();
        if (templateInfo.ColumnData.type.Equals(4) && _userManager.UserOrigin.Equals("pc") && input.extraQueryJson.IsNullOrEmpty())
        {
            realList.list.ForEach(items =>
            {
                var addItem = new Dictionary<string, object>();
                foreach (var item in items) if (item.Key != "RowIndex") addItem.Add(item.Key + roweditId, item.Value);
                foreach (var item in addItem) items.Add(item.Key, item.Value);
            });
        }

        if (realList.list.Any())
        {
            // 树形表格
            if (templateInfo.ColumnData.type.Equals(5) && input.extraQueryJson.IsNullOrEmpty())
                realList.list.ForEach(item => item[templateInfo.ColumnData.parentField + "_pid"] = item[templateInfo.ColumnData.parentField]);

            // 数据解析
            if (templateInfo.SingleFormData.Any(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()))
                realList.list = await _formDataParsing.GetKeyData(entity.Id, templateInfo.SingleFormData.Where(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()).ToList(), realList.list, templateInfo.ColumnData, actionType, templateInfo.WebType, primaryKey, entity.isShortLink, input.isConvertData);
            realList.list = await _formDataParsing.GetKeyData(entity.Id, templateInfo.SingleFormData.Where(x => x.__config__.templateJson == null || !x.__config__.templateJson.Any()).ToList(), realList.list, templateInfo.ColumnData, actionType, templateInfo.WebType, primaryKey, entity.isShortLink, input.isConvertData);

            // 如果是无表数据并且排序字段不为空，再进行数据排序
            if (!templateInfo.IsHasTable && input.sidx.IsNotEmptyOrNull())
            {
                if (input.sort == "desc")
                {
                    realList.list = realList.list.OrderByDescending(x =>
                    {
                        var dic = x as IDictionary<string, object>;
                        dic.GetOrAdd(input.sidx, () => null);
                        return dic[input.sidx];
                    }).ToList();
                }
                else
                {
                    realList.list = realList.list.OrderBy(x =>
                    {
                        var dic = x as IDictionary<string, object>;
                        dic.GetOrAdd(input.sidx, () => null);
                        return dic[input.sidx];
                    }).ToList();
                }
            }
        }

        if (input.dataType == "0" || input.dataType == "2")
        {
            if (string.IsNullOrEmpty(entity.Tables) || "[]".Equals(entity.Tables))
            {
                realList.pagination = new PageResult();
                realList.pagination.total = realList.list.Count;
                realList.pagination.pageSize = input.pageSize;
                realList.pagination.currentPage = input.currentPage;
                realList.list = realList.list.Skip(input.pageSize * (input.currentPage - 1)).Take(input.pageSize).ToList();
            }

            // 分组表格
            if (templateInfo.ColumnData.type == 3 && _userManager.UserOrigin == "pc" && !entity.isShortLink && input.extraQueryJson.IsNullOrEmpty())
            {
                var showFieldList = templateInfo.ColumnData.columnList.FindAll(x => x.__vModel__.ToLower() != templateInfo.ColumnData.groupField.ToLower());
                var groupShowField = showFieldList.Any(it => it.@fixed.Equals("left")) ? showFieldList.First(it => it.@fixed.Equals("left")).__vModel__ : showFieldList.First().__vModel__;
                realList.list = CodeGenHelper.GetGroupList(realList.list, templateInfo.ColumnData.groupField, groupShowField);
            }

            // 树形表格
            if (templateInfo.ColumnData.type.Equals(5) && input.extraQueryJson.IsNullOrEmpty())
                realList.list = CodeGenHelper.GetTreeList(realList.list, templateInfo.ColumnData.parentField + "_pid", templateInfo.ColumnData.columnList.Find(x => x.__vModel__.ToLower() != templateInfo.ColumnData.parentField.ToLower()).__vModel__);
        }
        else
        {
            if (string.IsNullOrEmpty(entity.Tables) || "[]".Equals(entity.Tables))
            {
                realList.pagination = new PageResult();
                realList.pagination.total = realList.list.Count;
                realList.pagination.pageSize = input.pageSize;
                realList.pagination.currentPage = input.currentPage;
                realList.list = realList.list.ToList();
            }

            // 分组表格
            if (templateInfo.ColumnData.type == 3 && _userManager.UserOrigin == "pc" && input.extraQueryJson.IsNullOrEmpty())
            {
                var showFieldList = templateInfo.ColumnData.columnList.FindAll(x => x.__vModel__.ToLower() != templateInfo.ColumnData.groupField.ToLower());
                var groupShowField = showFieldList.Any(it => it.@fixed.Equals("left")) ? showFieldList.First(it => it.@fixed.Equals("left")).__vModel__ : showFieldList.First().__vModel__;
                realList.list = CodeGenHelper.GetGroupList(realList.list, templateInfo.ColumnData.groupField, groupShowField);
            }
        }

        // 增加前端回显字段 : key_name
        if (!entity.isShortLink && templateInfo.ColumnData.type.Equals(4) && _userManager.UserOrigin.Equals("pc") && input.extraQueryJson.IsNullOrEmpty())
        {
            var newList = new List<Dictionary<string, object>>();
            realList.list.ForEach(items =>
            {
                var newItem = new Dictionary<string, object>();
                foreach (var item in items)
                {
                    if (item.Key.Contains(roweditId))
                    {
                        if (item.Value.IsNotEmptyOrNull())
                        {
                            var obj = item.Value;
                            if (obj.ToString().Contains("[[")) obj = item.Value.ToString().ToObject<List<List<object>>>();
                            else if (obj.ToString().Contains("[")) obj = item.Value.ToString().ToObject<List<object>>();

                            var value = items.FirstOrDefault(x => x.Key == item.Key.Replace(roweditId, string.Empty)).Value;
                            if (value.IsNullOrEmpty()) obj = null;
                            if (!newItem.ContainsKey(item.Key.Replace(roweditId, string.Empty))) newItem.Add(item.Key.Replace(roweditId, string.Empty), obj);
                            if (!newItem.ContainsKey(item.Key.Replace(roweditId, string.Empty) + "_name")) newItem.Add(item.Key.Replace(roweditId, string.Empty) + "_name", value);
                        }
                        else
                        {
                            if (!newItem.ContainsKey(item.Key.Replace(roweditId, string.Empty))) newItem.Add(item.Key.Replace(roweditId, string.Empty), null);
                            if (!newItem.ContainsKey(item.Key.Replace(roweditId, string.Empty) + "_name")) newItem.Add(item.Key.Replace(roweditId, string.Empty) + "_name", null);
                        }
                    }
                    if (item.Key.Equals("flowState") || item.Key.Equals("flowState_name") || item.Key.Equals("flowId") || item.Key.Equals("flowTaskId")) newItem.Add(item.Key, item.Value);
                    if (item.Key.Equals("id") && !newItem.ContainsKey(item.Key)) newItem.Add(item.Key, item.Value);
                    if (item.Key.Contains("_jnpfId")) newItem.Add(item.Key, item.Value);

                    var model = templateInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Key));
                    if (model.IsNotEmptyOrNull())
                    {
                        if (model.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE) || model.__config__.jnpfKey.Equals(JnpfKeyConst.TIME) || model.__config__.jnpfKey.Equals(JnpfKeyConst.CREATEUSER) || model.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER) || model.__config__.jnpfKey.Equals(JnpfKeyConst.CURRDEPT) || model.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE) || model.__config__.jnpfKey.Equals(JnpfKeyConst.CURRPOSITION))
                        {
                            newItem[item.Key] = items[item.Key];
                        }
                        else if (model.__config__.jnpfKey.Equals(JnpfKeyConst.DATE) || model.__config__.jnpfKey.Equals(JnpfKeyConst.CREATETIME) || model.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME))
                        {
                            var data = items[item.Key]?.ToString().TrimEnd();
                            if (data.IsNotEmptyOrNull())
                            {
                                var date = data.ParseToDateTime();
                                if (date != DateTime.MinValue)
                                    newItem[item.Key] = date.ParseToUnixTime();
                                else
                                    newItem[item.Key] = data;
                            }
                            else
                            {
                                newItem[item.Key] = data;
                            }
                        }
                    }
                }

                newList.Add(newItem);
            });
            realList.list = newList;
        }

        // 集成助手所需流程表单已审核通过的数据
        var processReviewCompletedDic = new List<Dictionary<string, object>>();
        if (input.isProcessReviewCompleted.Equals(1))
        {
            foreach (var item in realList.list)
            {
                if (item.ContainsKey("flowIsEnd") && item["flowIsEnd"].Equals(1))
                    processReviewCompletedDic.Add(item);
            }
            realList.list = processReviewCompletedDic;
        }

        // 集成助手所需只有 id 的数据
        var onlyIdDic = new List<Dictionary<string, object>>();
        if (input.isOnlyId.Equals(1))
        {
            foreach (var item in realList.list)
            {
                var idDic = new Dictionary<string, object>();
                if (item.ContainsKey("id"))
                {
                    idDic["id"] = item["id"];
                    onlyIdDic.Add(idDic);
                }
            }
            realList.list = onlyIdDic;
        }

        return realList;
    }

    /// <summary>
    /// 关联表单列表数据处理.
    /// </summary>
    /// <param name="entity">功能实体.</param>
    /// <param name="input">查询参数.</param>
    /// <param name="actionType"></param>
    /// <returns></returns>
    public async Task<PageResult<Dictionary<string, object>>> GetRelationFormList(VisualDevEntity entity, VisualDevModelListQueryInput input, string actionType = "List")
    {
        PageResult<Dictionary<string, object>>? realList = new PageResult<Dictionary<string, object>>() { list = new List<Dictionary<string, object>>() }; // 返回结果集
        TemplateParsingBase? templateInfo = new TemplateParsingBase(entity); // 解析模板控件
        if (entity.WebType.Equals(4)) return await GetDataViewResults(templateInfo, input); // 数据视图

        DbLinkEntity link = await GetDbLink(entity.DbLinkId);
        templateInfo.DbLink = link;
        await SyncField(templateInfo); // 同步业务字段
        var primaryKey = templateInfo.MainTable?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
        if (primaryKey.IsNullOrEmpty()) primaryKey = GetPrimary(link, templateInfo.MainTableName);
        Dictionary<string, string>? tableFieldKeyValue = new Dictionary<string, string>(); // 联表查询 表字段名称 对应 前端字段名称 (应对oracle 查询字段长度不能超过30个)

        var dataRule = _userManager.UserOrigin == "pc" ? templateInfo.ColumnData.ruleList.ToJsonString() : templateInfo.AppColumnData.ruleListApp.ToJsonString();
        input.dataRuleJson = _userManager.AssembleQuery(dataRule);
        var pageSize = input.pageSize;
        var currentPage = input.currentPage;
        var queryDic = string.IsNullOrEmpty(input.queryJson) ? null : input.queryJson.ToObject<Dictionary<string, string>>();
        if (input.queryType.IsNotEmptyOrNull())
        {
            templateInfo.ChildTableFields = new Dictionary<string, string>();
            if (queryDic.IsNotEmptyOrNull() && queryDic.Count > 0)
            {
                if (input.queryType == 1)
                {
                    input.queryJson = string.Empty;
                    input.pageSize = 999999;
                    input.currentPage = 1;
                }
                else
                {
                    foreach (var item in queryDic)
                    {
                        if (!templateInfo.ColumnData.searchList.Any(it => it.id.Equals(item.Key)))
                        {
                            var vmodel = templateInfo.AllFieldsModel.Find(it => it.__vModel__.Equals(item.Key));
                            if (templateInfo.ColumnData.searchList.Any(it => it.id.Equals(item.Key)))
                            {
                                vmodel.searchMultiple = templateInfo.ColumnData.searchList.Find(it => it.id.Equals(item.Key)).searchMultiple;
                            }
                            var searchModel = vmodel.ToObject<IndexSearchFieldModel>();
                            searchModel.id = item.Key;
                            templateInfo.ColumnData.searchList.Add(searchModel);
                        }
                    }

                    input.queryJson = GetQueryJson(input.queryJson, templateInfo.ColumnData, 1).ToJsonStringOld();
                }
            }
            else
            {
                input.queryJson = string.Empty;
            }
        }
        else
        {
            input.queryJson = string.Empty;
            input.pageSize = 999999;
            input.currentPage = 1;
        }

        string? sql = GetListQuerySql(primaryKey, templateInfo, ref input, ref tableFieldKeyValue); // 查询sql
        realList = _databaseService.GetInterFaceData(link, sql, input, templateInfo.ColumnData.Adapt<MainBeltViceQueryModel>(), tableFieldKeyValue);

        input.pageSize = pageSize;
        input.currentPage = currentPage;
        if (input.sidx.IsNullOrEmpty()) input.sidx = primaryKey;

        if (realList.list.Any())
        {
            if (templateInfo.SingleFormData.Any(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()))
                realList.list = await _formDataParsing.GetKeyData(entity.Id, templateInfo.SingleFormData.Where(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()).ToList(), realList.list, templateInfo.ColumnData, actionType, templateInfo.WebType, primaryKey);
            realList.list = await _formDataParsing.GetKeyData(entity.Id, templateInfo.SingleFormData.Where(x => !x.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM) && (x.__config__.templateJson == null || !x.__config__.templateJson.Any())).ToList(), realList.list, templateInfo.ColumnData, actionType, templateInfo.WebType.ParseToInt(), primaryKey);

            if ((input.queryType.IsNullOrEmpty() || input.queryType == 1) && queryDic.IsNotEmptyOrNull() && queryDic.Count > 0)
            {
                if (queryDic.FirstOrDefault().Value.IsNotEmptyOrNull())
                {
                    var keyWord = queryDic.FirstOrDefault().Value;
                    var keyWordList = queryDic.Select(it => it.Key).ToList();
                    List<Dictionary<string, object>>? newList = new List<Dictionary<string, object>>();
                    List<string>? columnName = templateInfo.ColumnData.columnList.Select(x => x.prop).ToList();
                    realList.list.ForEach(item =>
                    {
                        if (item.Any(x => columnName.Contains(x.Key) && keyWordList.Contains(x.Key) && x.Value != null && x.Value.ToString().Contains(keyWord)))
                            newList.Add(item);
                    });

                    realList.list = newList;
                }
            }

            // 排序
            if (input.sidx.IsNotEmptyOrNull())
            {
                var sidx = input.sidx.Split(",").ToList();

                realList.list.Sort((Dictionary<string, object> x, Dictionary<string, object> y) =>
                {
                    foreach (var item in sidx)
                    {
                        if (item[0].ToString().Equals("-"))
                        {
                            var itemName = item.Remove(0, 1);
                            if (!x[itemName].Equals(y[itemName]))
                                return y[itemName].ToString().CompareTo(x[itemName].ToString());
                        }
                        else
                        {
                            if (!x[item].Equals(y[item]))
                                return x[item].ToString().CompareTo(y[item].ToString());
                        }
                    }

                    return 0;
                });
            }
        }

        realList.pagination.total = realList.list.Count;
        realList.pagination.pageSize = input.pageSize;
        realList.pagination.currentPage = input.currentPage;

        return realList;
    }

    /// <summary>
    /// 获取有表详情.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="flowId">流程id.</param>
    /// <param name="isConvert">是否转换数据.</param>
    /// <param name="menuId">菜单id.</param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetHaveTableInfo(string id, VisualDevEntity templateEntity, string? flowId = null, bool isConvert = false, string? menuId = null)
    {
        TemplateParsingBase templateInfo = new TemplateParsingBase(templateEntity); // 解析模板控件
        DbLinkEntity link = await GetDbLink(templateEntity.DbLinkId);
        var mainPrimary = templateInfo.MainTable?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
        if (mainPrimary.IsNullOrEmpty()) mainPrimary = GetPrimary(link, templateInfo.MainTableName);
        await OptimisticLocking(link, templateInfo); // 处理 开启 并发锁定
        if (id.Equals("0") || id.IsNullOrWhiteSpace()) return new Dictionary<string, object>();

        // 表单权限
        List<string>? formAuthList = null;
        if (menuId.IsNotEmptyOrNull() && templateInfo.ColumnData.useFormPermission) formAuthList = await GetMenuFormAuthorize(menuId);

        Dictionary<string, string>? tableFieldKeyValue = new Dictionary<string, string>(); // 联表查询 表字段 别名
        tableFieldKeyValue[mainPrimary.ToUpper()] = mainPrimary;
        tableFieldKeyValue["f_flow_id".ToUpper()] = "f_flow_id";
        tableFieldKeyValue["f_flow_task_id".ToUpper()] = "f_flow_task_id";
        var sql = GetInfoQuerySql(id, mainPrimary, templateInfo, ref tableFieldKeyValue, flowId.IsNotEmptyOrNull(), null, formAuthList); // 获取查询Sql
        var data = _databaseService.GetSqlData(link, sql).ToObject<List<Dictionary<string, string>>>().ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
        if (data == null) return null;
        if (data.ContainsKey("f_flow_id") && data["f_flow_id"].IsNullOrEmpty()) data["f_flow_id"] = flowId;

        // 记录全部数据
        Dictionary<string, object> dataMap = new Dictionary<string, object>();

        // 查询别名转换
        if (templateInfo.AuxiliaryTableFieldsModelList.Any()) foreach (KeyValuePair<string, object> item in data) dataMap.Add(tableFieldKeyValue[item.Key.ToUpper()], item.Value);
        else dataMap = data;

        Dictionary<string, object> newDataMap = new Dictionary<string, object>();

        dataMap = _formDataParsing.GetTableDataInfo(new List<Dictionary<string, object>>() { dataMap }, templateInfo.FieldsModelList, "detail").FirstOrDefault();

        // 处理子表数据
        newDataMap = GetChildTableData(templateInfo, link, dataMap, newDataMap, false, formAuthList);

        int dicCount = newDataMap.Keys.Count;
        string[] strKey = new string[dicCount];
        newDataMap.Keys.CopyTo(strKey, 0);
        for (int i = 0; i < strKey.Length; i++)
        {
            FieldsModel? model = templateInfo.FieldsModelList.Where(m => m.__vModel__ == strKey[i]).FirstOrDefault();
            if (model != null)
            {
                List<Dictionary<string, object>> tables = newDataMap[strKey[i]].ToObject<List<Dictionary<string, object>>>();
                List<Dictionary<string, object>> newTables = new List<Dictionary<string, object>>();
                foreach (Dictionary<string, object>? item in tables)
                {
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, object> value in item)
                    {
                        FieldsModel? child = model.__config__.children.Find(c => c.__vModel__ == value.Key);
                        if (child != null) dic.Add(value.Key, value.Value);
                        if (value.Key.ToLower().Equals("id")) dic.Add("id", value.Value);
                    }

                    newTables.Add(dic);
                }

                if (newTables.Count > 0) newDataMap[strKey[i]] = newTables;
            }
        }

        foreach (KeyValuePair<string, object> entryMap in dataMap)
        {
            FieldsModel? model = templateInfo.FieldsModelList.Where(m => m.__vModel__.ToLower() == entryMap.Key.ToLower()).FirstOrDefault();
            if (model != null && entryMap.Key.ToLower().Equals(model.__vModel__.ToLower())) newDataMap[entryMap.Key] = entryMap.Value;
            if (entryMap.Key.Equals(mainPrimary)) newDataMap[entryMap.Key] = entryMap.Value;
        }

        if (!newDataMap.ContainsKey("id")) newDataMap.Add("id", data[mainPrimary]);
        _formDataParsing.GetBARAndQR(templateInfo.FieldsModelList, newDataMap, dataMap); // 处理 条形码 、 二维码 控件
        if (dataMap.ContainsKey("f_flow_id")) newDataMap["flowId"] = dataMap["f_flow_id"];
        if (dataMap.ContainsKey("F_FLOW_ID")) newDataMap["flowId"] = dataMap["F_FLOW_ID"];
        if (dataMap.ContainsKey("f_flow_task_id")) newDataMap["flowTaskId"] = dataMap["f_flow_task_id"];
        if (dataMap.ContainsKey("F_FLOW_TASK_ID")) newDataMap["flowTaskId"] = dataMap["F_FLOW_TASK_ID"];

        // 不转换数据
        if (isConvert) return newDataMap;

        return await _formDataParsing.GetSystemComponentsData(templateEntity.Id, templateInfo.FieldsModelList, newDataMap.ToJsonString());
    }

    /// <summary>
    /// 获取有表详情转换.
    /// </summary>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="id">主键.</param>
    /// <param name="propsValue">存储字段（关联表单）.</param>
    /// <param name="menuId">菜单id.</param>
    /// <param name="tenantId">租户id.</param>
    /// <returns></returns>
    public async Task<string> GetHaveTableInfoDetails(VisualDevEntity templateEntity, string id, string? propsValue = null, string? menuId = null, string? tenantId = null)
    {
        TemplateParsingBase? templateInfo = new TemplateParsingBase(templateEntity); // 解析模板控件
        DbLinkEntity link = await GetDbLink(templateEntity.DbLinkId, tenantId);
        var mainPrimary = templateInfo.MainTable?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
        if (mainPrimary.IsNullOrEmpty()) mainPrimary = GetPrimary(link, templateInfo.MainTableName);

        // 表单权限
        List<string>? formAuthList = null;
        if (menuId.IsNotEmptyOrNull() && templateInfo.ColumnData.useFormPermission) formAuthList = await GetMenuFormAuthorize(menuId);

        Dictionary<string, string>? tableFieldKeyValue = new Dictionary<string, string>(); // 联表查询 表字段 别名
        tableFieldKeyValue[mainPrimary.ToUpper()] = mainPrimary;
        tableFieldKeyValue["f_flow_id".ToUpper()] = "f_flow_id";
        tableFieldKeyValue["f_flow_task_id".ToUpper()] = "f_flow_task_id";
        var sql = GetInfoQuerySql(id, mainPrimary, templateInfo, ref tableFieldKeyValue, false, propsValue, formAuthList); // 获取查询Sql

        var data = _databaseService.GetSqlData(link, sql).ToObject<List<Dictionary<string, string>>>().ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
        if (data == null) return null;

        // 记录全部数据
        Dictionary<string, object> dataMap = new Dictionary<string, object>();

        // 查询别名转换
        if (templateInfo.AuxiliaryTableFieldsModelList.Any()) foreach (KeyValuePair<string, object> item in data) dataMap.Add(tableFieldKeyValue[item.Key.ToUpper()], item.Value);
        else dataMap = data;

        Dictionary<string, object> newDataMap = new Dictionary<string, object>();

        // 处理子表数据
        newDataMap = GetChildTableData(templateInfo, link, dataMap, newDataMap, true, formAuthList);

        int dicCount = newDataMap.Keys.Count;
        string[] strKey = new string[dicCount];
        newDataMap.Keys.CopyTo(strKey, 0);
        for (int i = 0; i < strKey.Length; i++)
        {
            FieldsModel? model = templateInfo.FieldsModelList.Find(m => m.__vModel__ == strKey[i]);
            if (model != null)
            {
                List<Dictionary<string, object>> childModelData = new List<Dictionary<string, object>>();
                foreach (Dictionary<string, object>? item in newDataMap[strKey[i]].ToObject<List<Dictionary<string, object>>>())
                {
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, object> value in item)
                    {
                        FieldsModel? child = model.__config__.children.Find(c => c.__vModel__ == value.Key);
                        if (child != null && value.Value != null)
                        {
                            if (child.__config__.jnpfKey.Equals(JnpfKeyConst.DATE))
                            {
                                var keyValue = value.Value.ToString();
                                DateTime dtDate;
                                if (DateTime.TryParse(keyValue, out dtDate)) dic.Add(value.Key, keyValue.ParseToDateTime().ParseToUnixTime());
                                else dic.Add(value.Key, value.Value.ToString().TimeStampToDateTime().ParseToUnixTime());
                            }
                            else dic.Add(value.Key, value.Value);
                        }
                        else if (value.Key.ToLower().Equals("id"))
                        {
                            dic.Add("id", value.Value);
                        }
                        else
                        {
                            dic.Add(value.Key, value.Value);
                        }
                    }

                    dic["JnpfKeyConst_MainData"] = data.ToJsonString();
                    childModelData.Add(dic);
                }

                if (childModelData.Count > 0)
                {
                    // 将关键字查询传输的id转换成名称
                    if (model.__config__.children.Any(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()))
                        newDataMap[strKey[i]] = await _formDataParsing.GetKeyData(templateEntity.Id, model.__config__.children.Where(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()).ToList(), childModelData, templateInfo.ColumnData, "List", templateInfo.WebType, mainPrimary, templateEntity.isShortLink);
                    newDataMap[strKey[i]] = await _formDataParsing.GetKeyData(templateEntity.Id, model.__config__.children.Where(x => x.__config__.templateJson == null || !x.__config__.templateJson.Any()).ToList(), childModelData, templateInfo.ColumnData.ToObject<ColumnDesignModel>(), "List", templateInfo.WebType, mainPrimary, templateEntity.isShortLink);
                }
            }
        }

        List<Dictionary<string, object>> listEntity = new List<Dictionary<string, object>>() { dataMap };

        // 控件联动
        var tempDataMap = new Dictionary<string, object>();
        if (templateInfo.SingleFormData.Any(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()))
            tempDataMap = (await _formDataParsing.GetKeyData(templateEntity.Id, templateInfo.SingleFormData.Where(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()).ToList(), listEntity, templateInfo.ColumnData, "List", templateInfo.WebType, mainPrimary, templateEntity.isShortLink)).FirstOrDefault();
        tempDataMap = (await _formDataParsing.GetKeyData(templateEntity.Id, templateInfo.SingleFormData.Where(x => x.__config__.templateJson == null || !x.__config__.templateJson.Any()).ToList(), listEntity, templateInfo.ColumnData, "List", templateInfo.WebType, mainPrimary, templateEntity.isShortLink)).FirstOrDefault();
        foreach (var entryMap in tempDataMap) newDataMap[entryMap.Key] = entryMap.Value;

        _formDataParsing.GetBARAndQR(templateInfo.FieldsModelList, newDataMap, dataMap); // 处理 条形码 、 二维码 控件

        if (!newDataMap.ContainsKey("id")) newDataMap.Add("id", dataMap[mainPrimary]);
        return newDataMap.ToJsonString();
    }

    #endregion

    #region Post

    /// <summary>
    /// 创建在线开发功能.
    /// </summary>
    /// <param name="templateEntity">功能模板实体.</param>
    /// <param name="dataInput">数据输入.</param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    public async Task Create(VisualDevEntity templateEntity, VisualDevModelDataCrInput dataInput, string? tenantId = null)
    {
        string? mainId = SnowflakeIdHelper.NextId();
        DbLinkEntity link = await GetDbLink(templateEntity.DbLinkId, tenantId);
        var haveTableSql = await CreateHaveTableSql(templateEntity, dataInput, mainId, tenantId);

        // 主表自增长Id.
        if (haveTableSql.ContainsKey("MainTableReturnIdentity"))
        {
            mainId = haveTableSql["MainTableReturnIdentity"].FirstOrDefault()?["ReturnIdentity"].ToString();
            haveTableSql.Remove("MainTableReturnIdentity");
        }

        try
        {
            _db.BeginTran();
            foreach (var item in haveTableSql) await _databaseService.ExecuteSql(link, item.Key, item.Value); // 新增功能数据

            // 添加任务流程`事件触发`新增事件
            if (dataInput.isInteAssis)
            {
                var model = new TaskFlowEventModel();
                model.TenantId = _userManager.TenantId;
                model.UserId = _userManager.UserId;
                model.ModelId = templateEntity.Id;
                model.TriggerType = "eventTrigger";
                model.ActionType = 1;
                var dataModel = new Dictionary<string, object>();
                dataModel["id"] = mainId;
                model.taskFlowData = new List<Dictionary<string, object>>();
                model.taskFlowData.Add(dataModel);
                await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTask", model));
            }

            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.COM1000);
        }
    }

    /// <summary>
    /// 创建有表SQL.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="dataInput"></param>
    /// <param name="mainId"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    public async Task<Dictionary<string, List<Dictionary<string, object>>>> CreateHaveTableSql(VisualDevEntity templateEntity, VisualDevModelDataCrInput dataInput, string mainId, string? tenantId = null)
    {
        TemplateParsingBase templateInfo = new TemplateParsingBase(templateEntity); // 解析模板控件
        templateInfo.DbLink = await GetDbLink(templateEntity.DbLinkId, tenantId);
        return await GetCreateSqlByTemplate(templateInfo, dataInput, mainId);
    }

    public async Task<Dictionary<string, List<Dictionary<string, object>>>> GetCreateSqlByTemplate(TemplateParsingBase templateInfo, VisualDevModelDataCrInput dataInput, string mainId, bool isImport = false, List<string>? systemControlList = null)
    {
        await SyncField(templateInfo); // 同步业务字段
        Dictionary<string, object>? allDataMap = dataInput.data.ToObject<Dictionary<string, object>>();
        if (!templateInfo.VerifyTemplate()) throw Oops.Oh(ErrorCode.D1401); // 验证模板

        // 表单权限
        if (dataInput.menuId.IsNotEmptyOrNull() && templateInfo.ColumnData.useFormPermission)
        {
            var formAuthList = await GetMenuFormAuthorize(dataInput.menuId);
            if (formAuthList != null) allDataMap = GetFormAuthData(templateInfo, allDataMap, formAuthList, true);
        }

        // 处理系统控件(模板开启行编辑)
        if (templateInfo.ColumnData.type.Equals(4) && _userManager.UserOrigin.Equals("pc"))
        {
            templateInfo.GenerateFields.ForEach(item =>
            {
                if (!allDataMap.ContainsKey(item.__vModel__)) allDataMap.Add(item.__vModel__, string.Empty);
                if (item.__config__.jnpfKey.Equals(JnpfKeyConst.CREATETIME) && allDataMap.ContainsKey(item.__vModel__))
                {
                    var value = allDataMap[item.__vModel__].ToString();
                    allDataMap.Remove(item.__vModel__);
                    allDataMap.Add(item.__vModel__, DateTime.Now.ToString());
                }
            });
        }

        DbLinkEntity link = templateInfo.DbLink;
        List<DbTableFieldModel>? tableList = _databaseService.GetFieldList(link, templateInfo.MainTableName); // 获取主表 表结构 信息
        var primaryKey = tableList.Find(t => t.primaryKey && t.field.ToLower() != "f_tenant_id")?.field; // 主表主键
        if (primaryKey.IsNullOrEmpty()) primaryKey = templateInfo.MainTable.fields.Find(x => x.primaryKey.Equals(1) && x.field.ToLower() != "f_tenant_id")?.field;
        string? dbType = link?.DbType != null ? link.DbType : _visualDevRepository.AsSugarClient().CurrentConnectionConfig.DbType.ToString();
        var isUpper = link.DbType.ToLower().Equals("oracle") || link.DbType.ToLower().Equals("dm") || link.DbType.ToLower().Equals("dm8") ? true : false;

        // 验证必填（行内编辑不验证）
        if (!isImport && systemControlList == null && !templateInfo.ColumnData.type.Equals(4)) RequiredVerify(templateInfo, allDataMap);

        // 验证唯一值
        if (!isImport) await UniqueVerify(link, tableList, templateInfo, allDataMap, primaryKey, mainId, false);

        // 生成系统自动生成字段
        if (templateInfo.visualDevEntity != null && !templateInfo.visualDevEntity.isShortLink)
            allDataMap = await GenerateFeilds(templateInfo.visualDevEntity.Id, templateInfo.FieldsModelList.ToJsonString(), allDataMap, true, systemControlList);

        // 新增SQL
        Dictionary<string, List<Dictionary<string, object>>> dictionarySql = new Dictionary<string, List<Dictionary<string, object>>>();
        var tableField = new Dictionary<string, object>(); // 字段和值
        templateInfo?.MainTableFieldsModelList.ForEach(item =>
        {
            if (allDataMap.ContainsKey(item.__vModel__) && item.__config__.jnpfKey != JnpfKeyConst.MODIFYTIME && item.__config__.jnpfKey != JnpfKeyConst.MODIFYUSER)
            {
                object? itemData = allDataMap[item.__vModel__];
                if (item.__vModel__.IsNotEmptyOrNull() && itemData != null && !string.IsNullOrEmpty(itemData.ToString()) && itemData.ToString() != "[]")
                {
                    var value = _formDataParsing.InsertValueHandle(dbType, tableList, item.__vModel__, itemData, templateInfo.MainTableFieldsModelList, "create", templateInfo.visualDevEntity != null ? templateInfo.visualDevEntity.isShortLink : false);
                    tableField.Add(item.__vModel__, value);
                }
            }
        });

        // 流程字段
        if (allDataMap.ContainsKey("flowId") && allDataMap["flowId"].IsNotEmptyOrNull())
        {
            var flowTaskIdKey = isUpper ? "F_FLOW_TASK_ID" : "f_flow_task_id";
            var flowIdKey = isUpper ? "F_FLOW_ID" : "f_flow_id";
            var flowStateKey = isUpper ? "F_FLOW_STATE" : "f_flow_state";
            tableField.Add(flowTaskIdKey, mainId);
            tableField.Add(flowIdKey, allDataMap["flowId"]);
            tableField.Add(flowStateKey, 0);
        }

        var tenantCache = new GlobalTenantCacheModel();
        if (_tenant.MultiTenancy) tenantCache = _cacheManager.Get<List<GlobalTenantCacheModel>>(CommonConst.GLOBALTENANT).Find(it => it.TenantId.Equals(link.Id));

        // 租户字段
        var tenantIdKey = isUpper ? "F_TENANT_ID" : "f_tenant_id";
        if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1))
            tableField.Add(tenantIdKey, tenantCache.connectionConfig.IsolationField); // 多租户
        else
            tableField.Add(tenantIdKey, "0");

        // 乐观锁
        if (templateInfo.FormModel.concurrencyLock)
        {
            var versionKey = isUpper ? "F_VERSION" : "f_version";
            tableField.Add(versionKey, 0);
        }

        // 集成助手数据标识
        var assistantKey = isUpper ? "F_INTE_ASSISTANT" : "f_inte_assistant";
        if (allDataMap.ContainsKey(assistantKey) && allDataMap[assistantKey].IsNotEmptyOrNull())
        {
            tableField.Add(assistantKey, allDataMap[assistantKey]);
        }

        // 主键策略(雪花Id)
        if (templateInfo.FormModel.primaryKeyPolicy.Equals(1)) tableField.Add(primaryKey, mainId);

        // 前端空提交
        if (!tableField.Any()) tableField.Add(tableList.Where(x => !x.primaryKey).First().field, null);

        // 拼接主表 sql
        dictionarySql.Add(templateInfo.MainTableName, new List<Dictionary<string, object>>() { tableField });

        // 自增长主键 需要返回的自增id
        if (templateInfo.FormModel.primaryKeyPolicy.Equals(2))
        {
            var mainSql = dictionarySql.First();
            mainId = _databaseService.ExecuteReturnIdentity(link, mainSql.Key, mainSql.Value).ToString();
            if (mainId.Equals("0")) throw Oops.Oh(ErrorCode.D1402);
            tableField.Clear();
            dictionarySql.Clear();
            tableField.Add("ReturnIdentity", mainId);
            dictionarySql.Add("MainTableReturnIdentity", new List<Dictionary<string, object>>() { tableField });
        }

        // 拼接副表 sql
        if (templateInfo.AuxiliaryTableFieldsModelList.Any())
        {
            templateInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList().ForEach(tbname =>
            {
                var tb = templateInfo.AllTable.Find(t => t.table == tbname);
                tableField = new Dictionary<string, object>();

                // 主键策略(雪花Id)
                if (templateInfo.FormModel.primaryKeyPolicy.Equals(1))
                {
                    var tbMianKey = tb.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
                    if (tbMianKey.IsNullOrEmpty()) tbMianKey = GetPrimary(link, tbname);
                    tableField.Add(tbMianKey, SnowflakeIdHelper.NextId());
                }

                // 租户字段
                if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1))
                    tableField.Add("f_tenant_id", tenantCache.connectionConfig.IsolationField); // 多租户
                else
                    tableField.Add("f_tenant_id", "0");

                // 字段
                templateInfo.AuxiliaryTableFieldsModelList.Where(x => x.__vModel__.Contains("jnpf_" + tbname + "_jnpf_")).ToList().ForEach(item =>
                {
                    object? itemData = allDataMap.Where(x => x.Key == item.__vModel__).Count() > 0 ? allDataMap[item.__vModel__] : null;
                    if (item.IsNotEmptyOrNull() && itemData != null && !string.IsNullOrEmpty(itemData.ToString()) && itemData.ToString() != "[]" && item.__config__.jnpfKey != JnpfKeyConst.MODIFYTIME && item.__config__.jnpfKey != JnpfKeyConst.MODIFYUSER)
                    {
                        var value = _formDataParsing.InsertValueHandle(dbType, tableList, item.__vModel__, allDataMap[item.__vModel__], templateInfo.FieldsModelList, "create", templateInfo.visualDevEntity != null ? templateInfo.visualDevEntity.isShortLink : false);
                        tableField.Add(item.__vModel__.ReplaceRegex(@"(\w+)_jnpf_", string.Empty), value);
                    }
                });

                // 外键
                if (tb.relationField.Equals(primaryKey))
                    tableField[tb.tableField] = mainId;
                else if (allDataMap.ContainsKey(tb.relationField) && allDataMap[tb.relationField].IsNotEmptyOrNull())
                    tableField[tb.tableField] = allDataMap[tb.relationField];

                dictionarySql.Add(tbname, new List<Dictionary<string, object>>() { tableField });
            });
        }

        // 拼接子表 sql
        foreach (string? item in allDataMap.Where(d => d.Key.ToLower().Contains("tablefield")).Select(d => d.Key).ToList())
        {
            if (!templateInfo.AllFieldsModel.Any(x => x.__vModel__.Equals(item)) || !templateInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item)).__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) continue;

            // 查找到该控件数据
            object? objectData = allDataMap[item];
            List<Dictionary<string, object>>? model = objectData.ToObject<List<Dictionary<string, object>>>();
            if (model != null && model.Count > 0)
            {
                // 利用key去找模板
                FieldsModel? fieldsModel = templateInfo.FieldsModelList.Find(f => f.__vModel__ == item);
                if (fieldsModel.isNumLimit && model.Count > fieldsModel.numLimit) throw Oops.Oh(ErrorCode.D1425, fieldsModel.__config__.label);

                TableModel? childTable = templateInfo.AllTable.Find(t => t.table == fieldsModel.__config__.tableName);
                var childTableList = _databaseService.GetFieldList(link, childTable?.table);
                var childPrimaryKey = childTableList.Find(t => t.primaryKey && t.field.ToLower() != "f_tenant_id")?.field;
                if (childPrimaryKey.IsNullOrEmpty()) childPrimaryKey = childTable.fields.Find(x => x.primaryKey.Equals(1) && x.field.ToLower() != "f_tenant_id")?.field;
                foreach (Dictionary<string, object>? data in model)
                {
                    tableField = new Dictionary<string, object>();

                    // 主键策略(雪花Id)
                    if (templateInfo.FormModel.primaryKeyPolicy.Equals(1))
                        tableField.Add(childPrimaryKey, SnowflakeIdHelper.NextId());

                    // 租户字段
                    if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1))
                        tableField.Add("f_tenant_id", tenantCache.connectionConfig.IsolationField); // 多租户
                    else
                        tableField.Add("f_tenant_id", "0");

                    // 字段
                    foreach (KeyValuePair<string, object> child in data)
                    {
                        if (child.Key.Equals("id") && child.Value.IsNotEmptyOrNull())
                        {
                            tableField[childPrimaryKey] = child.Value;
                        }
                        else if (child.Key.IsNotEmptyOrNull() && !child.Key.Equals("jnpfId") && child.Value.IsNotEmptyOrNull() && child.Value.ToString() != "[]")
                        {
                            var value = _formDataParsing.InsertValueHandle(dbType, childTableList, child.Key, child.Value, fieldsModel?.__config__.children, "create", templateInfo.visualDevEntity != null ? templateInfo.visualDevEntity.isShortLink : false);
                            tableField.Add(child.Key, value);
                        }
                    }

                    // 外键
                    if (childTable.relationField.Equals(primaryKey))
                        tableField[childTable.tableField] = mainId;
                    else if (allDataMap.ContainsKey(childTable.relationField) && allDataMap[childTable.relationField].IsNotEmptyOrNull())
                        tableField[childTable.tableField] = allDataMap[childTable.relationField];

                    if (dictionarySql.ContainsKey(fieldsModel.__config__.tableName))
                        dictionarySql[fieldsModel.__config__.tableName].Add(tableField);
                    else
                        dictionarySql.Add(fieldsModel.__config__.tableName, new List<Dictionary<string, object>>() { tableField });
                }
            }
        }

        // 处理 开启 并发锁定
        await OptimisticLocking(link, templateInfo);

        // 数据日志
        if (!isImport && templateInfo.FormModel.dataLog)
        {
            var log = new VisualLogEntity()
            {
                ModuleId = templateInfo.visualDevEntity.Id,
                DataId = mainId,
                Type = 0
            };
            await _visualDevRepository.AsSugarClient().Insertable(log).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        return dictionarySql;
    }

    /// <summary>
    /// 修改在线开发功能.
    /// </summary>
    /// <param name="id">修改ID.</param>
    /// <param name="templateEntity"></param>
    /// <param name="visualdevModelDataUpForm"></param>
    /// <returns></returns>
    public async Task Update(string id, VisualDevEntity templateEntity, VisualDevModelDataUpInput visualdevModelDataUpForm)
    {
        TemplateParsingBase templateInfo = new TemplateParsingBase(templateEntity); // 解析模板控件
        if (templateInfo.ColumnData.type.Equals(4) && _userManager.UserOrigin.Equals("pc"))
        {
            // 剔除 [增加前端回显字段 : key_name]
            Dictionary<string, object> oldDataMap = visualdevModelDataUpForm.data.ToObject<Dictionary<string, object>>();
            Dictionary<string, object> newDataMap = new Dictionary<string, object>();
            foreach (var item in oldDataMap)
            {
                var key = item.Key.Substring(0, item.Key.LastIndexOf("_name") != -1 ? item.Key.LastIndexOf("_name") : item.Key.Length);
                if (!newDataMap.ContainsKey(key) && oldDataMap.ContainsKey(key)) newDataMap.Add(key, oldDataMap[key]);
            }

            if (newDataMap.Any()) visualdevModelDataUpForm.data = newDataMap.ToJsonString();
        }

        var logList = new List<VisualLogModel>();
        DbLinkEntity link = await GetDbLink(templateEntity.DbLinkId);
        var haveTableSql = await UpdateHaveTableSql(templateEntity, visualdevModelDataUpForm, id, logList);

        try
        {
            _db.BeginTran();
            foreach (var item in haveTableSql) await _databaseService.ExecuteSql(link, item); // 修改功能数据

            // 添加任务流程`事件触发`修改事件
            if (visualdevModelDataUpForm.isInteAssis)
            {
                var model = new TaskFlowEventModel();
                model.TenantId = _userManager.TenantId;
                model.UserId = _userManager.UserId;
                model.ModelId = templateEntity.Id;
                model.TriggerType = "eventTrigger";
                model.ActionType = 2;
                var dataModel = new Dictionary<string, object>();
                dataModel["id"] = visualdevModelDataUpForm.id;
                model.taskFlowData = new List<Dictionary<string, object>>();
                model.taskFlowData.Add(dataModel);
                model.upDateFieldList = logList.Select(x => x.field).ToList();
                await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTask", model));
            }
            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.COM1001);
        }
    }

    /// <summary>
    /// 修改有表SQL.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="visualdevModelDataUpForm"></param>
    /// <param name="id"></param>
    /// <param name="logList"></param>
    /// <returns></returns>
    public async Task<List<string>> UpdateHaveTableSql(VisualDevEntity templateEntity, VisualDevModelDataUpInput visualdevModelDataUpForm, string id, List<VisualLogModel>? logList = null)
    {
        TemplateParsingBase templateInfo = new TemplateParsingBase(templateEntity); // 解析模板控件
        templateInfo.DbLink = await GetDbLink(templateEntity.DbLinkId);
        return await GetUpdateSqlByTemplate(templateInfo, visualdevModelDataUpForm, id, false, logList);
    }

    public async Task<List<string>> GetUpdateSqlByTemplate(TemplateParsingBase templateInfo, VisualDevModelDataUpInput visualdevModelDataUpForm, string id, bool isImport = false, List<VisualLogModel>? logList = null, List<string>? systemControlList = null)
    {
        await SyncField(templateInfo); // 同步业务字段
        Dictionary<string, object>? allDataMap = visualdevModelDataUpForm.data.ToObject<Dictionary<string, object>>();
        if (!templateInfo.VerifyTemplate()) throw Oops.Oh(ErrorCode.D1401); // 验证模板

        // 表单权限
        if (visualdevModelDataUpForm.menuId.IsNotEmptyOrNull() && templateInfo.ColumnData.useFormPermission)
        {
            var formAuthList = await GetMenuFormAuthorize(visualdevModelDataUpForm.menuId);
            if (formAuthList != null) allDataMap = GetFormAuthData(templateInfo, allDataMap, formAuthList, true);
        }

        // 处理系统控件(模板开启行编辑)
        if (templateInfo.ColumnData.type.Equals(4) && _userManager.UserOrigin.Equals("pc"))
        {
            // 处理显示列和提交的表单数据匹配(行编辑空数据 前端会过滤该控件)
            templateInfo.ColumnData.columnList.Where(x => !allDataMap.ContainsKey(x.prop) && x.__config__.visibility.Equals("pc")).ToList()
                .ForEach(item => allDataMap.Add(item.prop, string.Empty));

            templateInfo.GenerateFields.ForEach(item =>
            {
                if (!allDataMap.ContainsKey(item.__vModel__)) allDataMap.Add(item.__vModel__, string.Empty);
                if (item.__config__.jnpfKey.Equals(JnpfKeyConst.CREATETIME) && allDataMap.ContainsKey(item.__vModel__) && allDataMap[item.__vModel__].IsNotEmptyOrNull())
                {
                    var value = allDataMap[item.__vModel__].ToString();
                    allDataMap.Remove(item.__vModel__);
                    DateTime dtDate;
                    if (DateTime.TryParse(value, out dtDate)) value = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", value);
                    else value = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", value.TimeStampToDateTime());
                    allDataMap.Add(item.__vModel__, value);
                }
            });
        }

        DbLinkEntity link = templateInfo.DbLink;
        List<DbTableFieldModel>? tableList = _databaseService.GetFieldList(link, templateInfo.MainTableName); // 获取主表 表结构 信息
        var primaryKey = tableList.Find(t => t.primaryKey && t.field.ToLower() != "f_tenant_id")?.field; // 主表主键
        if (primaryKey.IsNullOrEmpty()) primaryKey = templateInfo.MainTable.fields.Find(x => x.primaryKey.Equals(1) && x.field.ToLower() != "f_tenant_id")?.field;
        string? dbType = link?.DbType != null ? link.DbType : _visualDevRepository.AsSugarClient().CurrentConnectionConfig.DbType.ToString();
        var isUpper = link.DbType.ToLower().Equals("oracle") || link.DbType.ToLower().Equals("dm") || link.DbType.ToLower().Equals("dm8") ? true : false;
        if (!allDataMap.ContainsKey(primaryKey) && allDataMap.ContainsKey("Update_MainTablePrimary_Id")) allDataMap.Add(primaryKey, allDataMap["Update_MainTablePrimary_Id"]);

        // 验证必填（行内编辑不验证）
        if (!isImport && systemControlList == null && !templateInfo.ColumnData.type.Equals(4)) RequiredVerify(templateInfo, allDataMap);

        // 验证唯一值
        if (!isImport) await UniqueVerify(link, tableList, templateInfo, allDataMap, primaryKey, id, true);

        // 生成系统自动生成字段
        allDataMap = await GenerateFeilds(templateInfo.visualDevEntity.Id, templateInfo.FieldsModelList.ToJsonString(), allDataMap, false, systemControlList);

        // 主表查询语句
        List<string> mainSql = new List<string>();
        var fieldSql = new List<string>(); // key 字段名, value 修改值

        // 拼接主表 sql
        var fields = new List<string>() { primaryKey, "f_flow_id", "f_flow_task_id" };
        templateInfo?.MainTableFieldsModelList.ForEach(item =>
        {
            if (item.__vModel__.IsNotEmptyOrNull())
            {
                if (allDataMap.ContainsKey(item.__vModel__))
                {
                    var jnpfKey = item.__config__.jnpfKey;
                    if (jnpfKey != JnpfKeyConst.CREATEUSER && jnpfKey != JnpfKeyConst.CREATETIME && jnpfKey != JnpfKeyConst.CURRORGANIZE && jnpfKey != JnpfKeyConst.CURRPOSITION)
                        fieldSql.Add(string.Format("{0}={1}", item.__vModel__, _formDataParsing.InsertValueHandle(dbType, tableList, item.__vModel__, allDataMap[item.__vModel__], templateInfo.MainTableFieldsModelList, "update")));
                }

                fields.Add(item.__vModel__);
            }
        });

        var flowId = allDataMap.ContainsKey("flowId") && allDataMap["flowId"].IsNotEmptyOrNull() ? allDataMap["flowId"].ToString() : string.Empty;
        if (fieldSql.Any())
        {
            if (flowId.IsNotEmptyOrNull())
            {
                var flowTaskIdKey = isUpper ? "F_FLOW_TASK_ID" : "f_flow_task_id";
                var flowIdKey = isUpper ? "F_FLOW_ID" : "f_flow_id";
                fieldSql.Add(string.Format("{0}='{1}'", flowIdKey, flowId));
                mainSql.Add(string.Format("update {0} set {1} where {2}='{3}';", templateInfo?.MainTableName, string.Join(",", fieldSql), flowTaskIdKey, id));
            }
            else
            {
                mainSql.Add(string.Format("update {0} set {1} where {2}='{3}';", templateInfo?.MainTableName, string.Join(",", fieldSql), primaryKey, id));
            }
        }

        // 旧主表数据
        var oldMainSql = string.Empty;
        if (flowId.IsNotEmptyOrNull())
            oldMainSql = string.Format("select {0} from {1} where f_flow_task_id='{2}'", string.Join(",", fields), templateInfo.MainTableName, id);
        else
            oldMainSql = string.Format("select {0} from {1} where {2}='{3}'", string.Join(",", fields), templateInfo.MainTableName, primaryKey, id);
        var oldMainData = _databaseService.GetSqlData(link, oldMainSql).ToObject<List<Dictionary<string, string>>>().ToObject<List<Dictionary<string, object>>>().FirstOrDefault();

        // 拼接副表 sql
        if (templateInfo.AuxiliaryTableFieldsModelList.Any())
        {
            templateInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList().ForEach(tbname =>
            {
                List<string>? tableFieldList = templateInfo.AuxiliaryTableFieldsModelList.Where(x => x.__config__.tableName.Equals(tbname)).Select(x => x.__vModel__).ToList();

                // 外键
                var auxiliaryTable = templateInfo.AllTable.Find(t => t.table.Equals(tbname));
                var relationFieldData = allDataMap.ContainsKey(auxiliaryTable.relationField) && allDataMap[auxiliaryTable.relationField].IsNotEmptyOrNull() ? allDataMap[auxiliaryTable.relationField].ToString() : null;

                fieldSql.Clear(); // key 字段名, value 修改值
                List<DbTableFieldModel>? auxiliaryTableList = _databaseService.GetFieldList(link, tbname);
                templateInfo.AuxiliaryTableFieldsModelList.Where(x => x.__config__.tableName.Equals(tbname)).ToList().ForEach(item =>
                {
                    // 前端未填写数据的字段，默认会找不到字段名，需要验证
                    if (item.__vModel__.IsNotEmptyOrNull() && allDataMap.Where(x => x.Key == item.__vModel__).Count() > 0 && item.__config__.jnpfKey != JnpfKeyConst.CREATEUSER && item.__config__.jnpfKey != JnpfKeyConst.CREATETIME && item.__config__.jnpfKey != JnpfKeyConst.CURRORGANIZE && item.__config__.jnpfKey != JnpfKeyConst.CURRPOSITION)
                    {
                        var vModel = item.__vModel__.ReplaceRegex(@"(\w+)_jnpf_", string.Empty);
                        if (auxiliaryTable.tableField.Equals(vModel))
                            fieldSql.Add(string.Format("{0}={1}", vModel, relationFieldData.IsNotEmptyOrNull() ? relationFieldData : "null"));
                        else
                            fieldSql.Add(string.Format("{0}={1}", vModel, _formDataParsing.InsertValueHandle(dbType, auxiliaryTableList, item.__vModel__, allDataMap[item.__vModel__], templateInfo.FieldsModelList, "update")));
                    }
                });

                var oldRelationFieldData = oldMainData.IsNotEmptyOrNull() && oldMainData.ContainsKey(auxiliaryTable.relationField) && oldMainData[auxiliaryTable.relationField].IsNotEmptyOrNull() ? oldMainData[auxiliaryTable.relationField].ToString() : string.Empty;
                if (fieldSql.Any())
                {
                    if (oldRelationFieldData.IsNotEmptyOrNull())
                        mainSql.Add(string.Format("update {0} set {1} where {2}='{3}';", tbname, string.Join(",", fieldSql), auxiliaryTable.tableField, oldRelationFieldData));
                    else
                        mainSql.Add(string.Format("update {0} set {1} where {2} is null;", tbname, string.Join(",", fieldSql), auxiliaryTable.tableField));
                }
            });
        }

        // 非行编辑
        if (!templateInfo.ColumnData.type.Equals(4) || !_userManager.UserOrigin.Equals("pc"))
        {
            // 删除子表数据
            if (templateInfo.AllTable.Any(x => x.typeId.Equals("0")))
            {
                // 拼接子表 sql
                foreach (string? item in allDataMap.Where(d => d.Key.ToLower().Contains("tablefield")).Select(d => d.Key).ToList())
                {
                    if (!templateInfo.AllFieldsModel.Any(x => x.__vModel__.Equals(item)) || !templateInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item)).__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) continue;

                    // 查找到该控件数据
                    List<Dictionary<string, object>>? modelData = allDataMap[item].ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();

                    // 利用key去找模板
                    FieldsModel? fieldsModel = templateInfo.FieldsModelList.Find(f => f.__vModel__ == item);
                    if (fieldsModel.isNumLimit && modelData.Count > fieldsModel.numLimit) throw Oops.Oh(ErrorCode.D1425, fieldsModel.__config__.label);

                    ConfigModel? fieldsConfig = fieldsModel?.__config__;
                    List<string>? childColumn = new List<string>();
                    List<object>? childValues = new List<object>();
                    List<string>? updateFieldSql = new List<string>();
                    TableModel? childTable = templateInfo.AllTable.Find(t => t.table == fieldsModel.__config__.tableName && t.table != templateInfo.MainTableName);
                    var oldRelationFieldData = oldMainData.ContainsKey(childTable.relationField) && oldMainData[childTable.relationField].IsNotEmptyOrNull() ? oldMainData[childTable.relationField].ToString() : string.Empty;
                    var relationFieldData = allDataMap.ContainsKey(childTable.relationField) && allDataMap[childTable.relationField].IsNotEmptyOrNull() ? allDataMap[childTable.relationField].ToString() : string.Empty;
                    if (childTable != null)
                    {
                        var delUserId = _userManager.UserId;
                        var delTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        if (modelData != null && modelData.Count > 0)
                        {
                            var childTableList = _databaseService.GetFieldList(link, childTable?.table);
                            var childPrimaryKey = childTableList.Find(t => t.primaryKey && t.field.ToLower() != "f_tenant_id")?.field;
                            if (childPrimaryKey.IsNullOrEmpty()) childPrimaryKey = childTable.fields.Find(x => x.primaryKey.Equals(1) && x.field.ToLower() != "f_tenant_id")?.field;

                            if (visualdevModelDataUpForm.isDelSubTableData)
                            {
                                if (!modelData.Any(x => x.ContainsKey("id")))
                                {
                                    if (templateInfo.FormModel.logicalDelete)
                                    {
                                        if (dbType.Equals("Oracle"))
                                        {
                                            if (oldRelationFieldData.IsNotEmptyOrNull())
                                                mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3}='{4}';", childTable?.table, delUserId, delTime, childTable.tableField, oldRelationFieldData));
                                            else
                                                mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3} is null;", childTable?.table, delUserId, delTime, childTable.tableField));
                                        }
                                        else
                                        {
                                            if (oldRelationFieldData.IsNotEmptyOrNull())
                                                mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3}='{4}';", childTable?.table, delUserId, delTime, childTable.tableField, oldRelationFieldData));
                                            else
                                                mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3} is null;", childTable?.table, delUserId, delTime, childTable.tableField));
                                        }
                                    }
                                    else
                                    {
                                        if (oldRelationFieldData.IsNotEmptyOrNull())
                                            mainSql.Add(string.Format("delete from {0} where {1}='{2}';", childTable?.table, childTable.tableField, oldRelationFieldData));
                                        else
                                            mainSql.Add(string.Format("delete from {0} where {1} is null;", childTable?.table, childTable.tableField));
                                    }
                                }
                                else
                                {
                                    var ctIdList = modelData.Where(x => x.ContainsKey("id")).Select(x => x["id"]).ToObject<List<string>>();
                                    var querStr = string.Format("select {0} id from {1} where {0} in('{2}') ", childPrimaryKey, childTable?.table, string.Join("','", ctIdList));
                                    var res = _databaseService.GetSqlData(link, querStr).ToObject<List<Dictionary<string, string>>>();
                                    foreach (var data in res) if (!data.ContainsKey("id") && data.ContainsKey("ID")) data.Add("id", data["ID"]);
                                    foreach (var it in modelData.Where(x => x.ContainsKey("id"))) if (!res.Any(x => x["id"].Equals(it["id"].ToString()))) it.Remove("id");

                                    var sIds = string.Join("','", modelData.Where(x => x.ContainsKey("id")).Select(x => x["id"]).ToList());
                                    if (templateInfo.FormModel.logicalDelete)
                                    {
                                        if (dbType.Equals("Oracle"))
                                        {
                                            if (oldRelationFieldData.IsNotEmptyOrNull())
                                                mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3} not in ('{4}') and {5}='{6}';", childTable?.table, delUserId, delTime, childPrimaryKey, sIds, childTable.tableField, oldRelationFieldData));
                                            else
                                                mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3} not in ('{4}') and {5} is null;", childTable?.table, delUserId, delTime, childPrimaryKey, sIds, childTable.tableField));
                                        }
                                        else
                                        {
                                            if (oldRelationFieldData.IsNotEmptyOrNull())
                                                mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3} not in ('{4}') and {5}='{6}';", childTable?.table, delUserId, delTime, childPrimaryKey, sIds, childTable.tableField, oldRelationFieldData));
                                            else
                                                mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3} not in ('{4}') and {5} is null;", childTable?.table, delUserId, delTime, childPrimaryKey, sIds, childTable.tableField));
                                        }
                                    }
                                    else
                                    {
                                        if (oldRelationFieldData.IsNotEmptyOrNull())
                                            mainSql.Add(string.Format("delete from {0} where {1} not in ('{2}') and {3}='{4}';", childTable?.table, childPrimaryKey, sIds, childTable.tableField, oldRelationFieldData));
                                        else
                                            mainSql.Add(string.Format("delete from {0} where {1} not in ('{2}') and {3} is null;", childTable?.table, childPrimaryKey, sIds, childTable.tableField));
                                    }
                                }
                            }

                            foreach (Dictionary<string, object>? data in modelData)
                            {
                                if (data.Count > 0)
                                {
                                    foreach (KeyValuePair<string, object> child in data)
                                    {
                                        if (child.Key.IsNotEmptyOrNull() && child.Key != "id" && child.Key != "jnpfId" && child.Key != childTable.tableField)
                                        {
                                            childColumn.Add(child.Key); // Column部分
                                            var value = _formDataParsing.InsertValueHandle(dbType, childTableList, child.Key, child.Value, fieldsConfig.children, "update");
                                            childValues.Add(value); // Values部分

                                            var childModel = templateInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item + "-" + child.Key));
                                            if (childModel.IsNotEmptyOrNull())
                                            {
                                                var childJnpfKey = childModel.__config__.jnpfKey;
                                                if (childJnpfKey == JnpfKeyConst.CREATEUSER || childJnpfKey == JnpfKeyConst.CREATETIME || childJnpfKey == JnpfKeyConst.CURRORGANIZE || childJnpfKey == JnpfKeyConst.CURRPOSITION)
                                                    continue;
                                            }

                                            updateFieldSql.Add(string.Format("{0}={1}", child.Key, value));
                                        }
                                    }

                                    if (data.ContainsKey("id"))
                                    {
                                        updateFieldSql.Add(string.Format("{0}={1}", childTable.tableField, relationFieldData.IsNotEmptyOrNull() ? relationFieldData : "null"));

                                        if (updateFieldSql.Any())
                                        {
                                            var relationFieldSql = oldRelationFieldData.IsNotEmptyOrNull() ? string.Format("='{0}'", oldRelationFieldData) : "is null";
                                            mainSql.Add(string.Format("update {0} set {1} where {2}='{3}' and {4} {5};", fieldsModel.__config__.tableName, string.Join(',', updateFieldSql), childPrimaryKey, data["id"], childTable.tableField, relationFieldSql));
                                        }
                                    }
                                    else
                                    {
                                        // 租户字段
                                        var tenantId = "0";
                                        var tenantCache = _cacheManager.Get<List<GlobalTenantCacheModel>>(CommonConst.GLOBALTENANT).Find(it => it.TenantId.Equals(link.Id));
                                        if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1))
                                            tenantId = tenantCache.connectionConfig.IsolationField; // 多租户

                                        // 主键策略(雪花Id)
                                        if (templateInfo.FormModel.primaryKeyPolicy.Equals(1))
                                        {
                                            mainSql.Add(string.Format(
                                            "insert into {0}({6},{4}{1},{7}) values('{3}',{5}{2},'{8}');",
                                            fieldsModel.__config__.tableName,
                                            childColumn.Any() ? "," + string.Join(",", childColumn) : string.Empty,
                                            childColumn.Any() ? "," + string.Join(",", childValues) : string.Empty,
                                            SnowflakeIdHelper.NextId(),
                                            childTable.tableField,
                                            relationFieldData.IsNotEmptyOrNull() ? relationFieldData : "null",
                                            childPrimaryKey,
                                            "f_tenant_id",
                                            tenantId));
                                        }
                                        else
                                        {
                                            mainSql.Add(string.Format(
                                            "insert into {0}({1}{2},{5}) values({3}{4},'{6}');",
                                            fieldsModel.__config__.tableName,
                                            childTable.tableField,
                                            childColumn.Any() ? "," + string.Join(",", childColumn) : string.Empty,
                                            relationFieldData.IsNotEmptyOrNull() ? relationFieldData : "null",
                                            childColumn.Any() ? "," + string.Join(",", childValues) : string.Empty,
                                            "f_tenant_id",
                                            tenantId));
                                        }
                                    }

                                    childColumn.Clear();
                                    childValues.Clear();
                                    updateFieldSql.Clear();
                                }
                            }
                        }
                        else
                        {
                            if (templateInfo.FormModel.logicalDelete)
                            {
                                if (dbType.Equals("Oracle"))
                                {
                                    if (oldRelationFieldData.IsNotEmptyOrNull())
                                        mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3}='{4}';", childTable?.table, delUserId, delTime, childTable.tableField, oldRelationFieldData));
                                    else
                                        mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3} is null;", childTable?.table, delUserId, delTime, childTable.tableField));
                                }
                                else
                                {
                                    if (oldRelationFieldData.IsNotEmptyOrNull())
                                        mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3}='{4}';", childTable?.table, delUserId, delTime, childTable.tableField, oldRelationFieldData));
                                    else
                                        mainSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3} is null;", childTable?.table, delUserId, delTime, childTable.tableField));
                                }
                            }
                            else
                            {
                                if (oldRelationFieldData.IsNotEmptyOrNull())
                                    mainSql.Add(string.Format("delete from {0} where {1}='{2}';", childTable?.table, childTable.tableField, oldRelationFieldData));
                                else
                                    mainSql.Add(string.Format("delete from {0} where {1} is null;", childTable?.table, childTable.tableField));
                            }
                        }
                    }
                }
            }
        }

        // 处理 开启 并发锁定
        await OptimisticLocking(link, templateInfo, mainSql, allDataMap, primaryKey);

        // 数据日志
        if (!isImport)
        {
            if (logList.IsNullOrEmpty()) logList = new List<VisualLogModel>();
            var oldDataDic = (await GetHaveTableInfoDetails(templateInfo.visualDevEntity, allDataMap["id"].ToString()))?.ToObject<Dictionary<string, object>>();
            var newDataDic = new List<Dictionary<string, object>>() { allDataMap };
            if (templateInfo.SingleFormData.Any(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()))
                newDataDic = await _formDataParsing.GetKeyData(templateInfo.visualDevEntity.Id, templateInfo.SingleFormData.Where(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()).ToList(), newDataDic, templateInfo.ColumnData, "List", templateInfo.WebType, primaryKey, templateInfo.visualDevEntity.isShortLink);
            newDataDic = await _formDataParsing.GetKeyData(templateInfo.visualDevEntity.Id, templateInfo.SingleFormData.Where(x => x.__config__.templateJson == null || !x.__config__.templateJson.Any()).ToList(), newDataDic, templateInfo.ColumnData, "List", templateInfo.WebType, primaryKey, templateInfo.visualDevEntity.isShortLink);
            foreach (var item in newDataDic[0])
            {
                var model = templateInfo.FieldsModelList.Find(x => x.__vModel__ == item.Key);
                if (model.IsNotEmptyOrNull())
                {
                    var jnpfKey = model.__config__.jnpfKey;
                    if (jnpfKey == JnpfKeyConst.TABLE)
                    {
                        var childData = new List<Dictionary<string, object>>();
                        var chidField = new List<ChildFieldModel>();
                        var oldChildDataList = oldDataDic.ContainsKey(item.Key) ? oldDataDic[item.Key].ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>() : new List<Dictionary<string, object>>();
                        var newChildDataList = item.Value.ToObject<List<Dictionary<string, object>>>();
                        newChildDataList = await _formDataParsing.GetKeyData(templateInfo.visualDevEntity.Id, model.__config__.children.Where(x => x.__config__.templateJson == null || !x.__config__.templateJson.Any()).ToList(), newChildDataList, templateInfo.ColumnData, "List", templateInfo.WebType, primaryKey, templateInfo.visualDevEntity.isShortLink);

                        // 子表的删除
                        var deleteDataList = oldChildDataList.Where(it => !newChildDataList.Any(x => x.ContainsKey("id") && x["id"].Equals(it["id"]))).ToList();
                        foreach (var dateletData in deleteDataList)
                        {
                            var dic = new Dictionary<string, object>();
                            foreach (var data in dateletData)
                            {
                                var childModel = templateInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Key + "-" + data.Key));
                                if (childModel.IsNotEmptyOrNull())
                                {
                                    var childJnpfKey = childModel.__config__.jnpfKey;
                                    if (childJnpfKey == JnpfKeyConst.CREATETIME || childJnpfKey == JnpfKeyConst.CREATEUSER || childJnpfKey == JnpfKeyConst.MODIFYTIME || childJnpfKey == JnpfKeyConst.MODIFYUSER || childJnpfKey == JnpfKeyConst.CURRORGANIZE || childJnpfKey == JnpfKeyConst.CURRPOSITION || childJnpfKey == JnpfKeyConst.BILLRULE || childJnpfKey == JnpfKeyConst.RELATIONFORMATTR || childJnpfKey == JnpfKeyConst.POPUPATTR)
                                        break;

                                    // 子表数据
                                    var newChildValue = data.Value.IsNotEmptyOrNull() ? LogConvertData(data.Value, childJnpfKey) : string.Empty;
                                    dic.Add("jnpf_old_" + data.Key, newChildValue);
                                    dic.Add(data.Key, null);
                                    dic["jnpf_type"] = 2;
                                }
                            }

                            childData.Add(dic);
                        }

                        // 子表的新增、修改
                        foreach (var newChildData in newChildDataList)
                        {
                            var dic = new Dictionary<string, object>();
                            var flag = false;
                            foreach (var data in newChildData)
                            {
                                var childModel = templateInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Key + "-" + data.Key));
                                if (childModel.IsNotEmptyOrNull())
                                {
                                    var childJnpfKey = childModel.__config__.jnpfKey;
                                    if (childJnpfKey == JnpfKeyConst.CREATETIME || childJnpfKey == JnpfKeyConst.CREATEUSER || childJnpfKey == JnpfKeyConst.MODIFYTIME || childJnpfKey == JnpfKeyConst.MODIFYUSER || childJnpfKey == JnpfKeyConst.CURRORGANIZE || childJnpfKey == JnpfKeyConst.CURRPOSITION || childJnpfKey == JnpfKeyConst.BILLRULE || childJnpfKey == JnpfKeyConst.RELATIONFORMATTR || childJnpfKey == JnpfKeyConst.POPUPATTR)
                                        break;

                                    // 子表数据
                                    var newChildValue = data.Value.IsNotEmptyOrNull() ? LogConvertData(data.Value, childJnpfKey) : string.Empty;
                                    dic.Add(data.Key, newChildValue);
                                    if (newChildData.ContainsKey("id"))
                                    {
                                        var oldChildData = oldChildDataList.Find(x => x.ContainsKey("id") && x["id"].Equals(newChildData["id"]));
                                        var oldChildDataValue = oldChildData.ContainsKey(data.Key) && oldChildData[data.Key].IsNotEmptyOrNull() ? LogConvertData(oldChildData[data.Key], childJnpfKey) : string.Empty;

                                        if (oldChildDataValue != newChildValue) flag = true;

                                        dic.Add("jnpf_old_" + data.Key, oldChildDataValue);
                                        dic["jnpf_type"] = 1;
                                    }
                                    else
                                    {
                                        flag = true;
                                        dic.Add("jnpf_old_" + data.Key, null);
                                        dic["jnpf_type"] = 0;
                                    }
                                }
                            }

                            if (flag) childData.Add(dic);
                        }

                        if (childData.Any())
                        {
                            // 子表字段
                            foreach (var childModel in templateInfo.AllFieldsModel.FindAll(x => x.__vModel__.Contains(model.__vModel__ + "-")).ToList())
                            {
                                AddLogChidField(chidField, childModel);
                            }

                            var logModel = new VisualLogModel();
                            logModel.chidData = childData;
                            logModel.chidField = chidField;
                            logModel.field = model.__vModel__;
                            logModel.fieldName = model.__config__.label;
                            logModel.jnpfKey = jnpfKey;
                            logModel.type = 1;
                            logList.Add(logModel);
                        }
                    }
                    else if (jnpfKey == JnpfKeyConst.CREATETIME || jnpfKey == JnpfKeyConst.CREATEUSER || jnpfKey == JnpfKeyConst.MODIFYTIME || jnpfKey == JnpfKeyConst.MODIFYUSER || jnpfKey == JnpfKeyConst.CURRORGANIZE || jnpfKey == JnpfKeyConst.CURRPOSITION || jnpfKey == JnpfKeyConst.BILLRULE || jnpfKey == JnpfKeyConst.RELATIONFORMATTR || jnpfKey == JnpfKeyConst.POPUPATTR)
                    {
                    }
                    else
                    {
                        var oldValue = oldDataDic.IsNotEmptyOrNull() && oldDataDic.ContainsKey(item.Key) && oldDataDic[item.Key].IsNotEmptyOrNull() ? LogConvertData(oldDataDic[item.Key], jnpfKey) : string.Empty;
                        var newValue = item.Value.IsNotEmptyOrNull() ? LogConvertData(item.Value, jnpfKey) : string.Empty;
                        if (oldValue != newValue)
                        {
                            var logModel = new VisualLogModel();
                            logModel.field = model.__vModel__;
                            logModel.fieldName = model.__config__.label;
                            logModel.jnpfKey = jnpfKey;
                            logModel.type = 1;
                            logModel.oldData = oldValue;
                            logModel.newData = newValue;

                            switch (jnpfKey)
                            {
                                case JnpfKeyConst.RADIO:
                                case JnpfKeyConst.CHECKBOX:
                                case JnpfKeyConst.SELECT:
                                case JnpfKeyConst.CASCADER:
                                case JnpfKeyConst.TREESELECT:
                                    if (model.__config__.dataType == "dynamic") logModel.nameModified = true;

                                    break;
                                case JnpfKeyConst.UPLOADIMG:
                                case JnpfKeyConst.EDITOR:
                                case JnpfKeyConst.POPUPTABLESELECT:
                                case JnpfKeyConst.RELATIONFORM:
                                case JnpfKeyConst.POPUPSELECT:
                                    logModel.nameModified = true;

                                    break;
                            }

                            logList.Add(logModel);
                        }
                    }
                }
            }

            // 写入数据日志
            if (templateInfo.FormModel.dataLog && logList.Any())
            {
                var log = new VisualLogEntity()
                {
                    ModuleId = templateInfo.visualDevEntity.Id,
                    DataId = id,
                    DataLog = logList.ToJsonStringOld(),
                    Type = 1
                };
                await _visualDevRepository.AsSugarClient().Insertable(log).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
        }

        return mainSql;
    }

    #endregion

    #region 流程表单模块

    /// <summary>
    /// 添加、修改 流程表单数据.
    /// </summary>
    /// <param name="entity">表单模板.</param>
    /// <param name="formData">表单数据json.</param>
    /// <param name="dataId">主键Id.</param>
    /// <param name="flowId">flowId.</param>
    /// <param name="isUpdate">是否修改.</param>
    /// <param name="systemControlList">不赋值的系统控件Key.</param>
    /// <returns></returns>
    public async Task<List<VisualLogModel>> SaveFlowFormData(VisualDevReleaseEntity entity, string formData, string dataId, string flowId, bool isUpdate = false, List<string>? systemControlList = null)
    {
        var logList = new List<VisualLogModel>();
        if (entity != null)
        {
            // 自定义表单
            if (entity.Type.Equals(1))
            {
                var newEntity = entity.Adapt<VisualDevEntity>();
                var tInfo = new TemplateParsingBase(newEntity);
                tInfo.DbLink = await GetDbLink(newEntity.DbLinkId);
                var dic = formData.ToObject<Dictionary<string, object>>();
                dic["flowId"] = flowId;
                formData = dic.ToJsonString();
                if (isUpdate)
                {
                    var sqlList = await GetUpdateSqlByTemplate(tInfo, new VisualDevModelDataUpInput() { data = formData }, dataId, false, logList, systemControlList);
                    foreach (var item in sqlList) await _databaseService.ExecuteSql(tInfo.DbLink, item); // 修改功能数据
                }
                else
                {
                    var sqlList = await GetCreateSqlByTemplate(tInfo, new VisualDevModelDataUpInput() { data = formData }, dataId, false, systemControlList);

                    // 主表自增长Id.
                    if (sqlList.ContainsKey("MainTableReturnIdentity")) sqlList.Remove("MainTableReturnIdentity");
                    foreach (var item in sqlList) await _databaseService.ExecuteSql(tInfo.DbLink, item.Key, item.Value); // 新增功能数据
                }
            }
            else if (entity.Type.Equals(2))
            {
                // 新增,修改
                var dic = formData.ToObject<Dictionary<string, object>>();
                dic["flowId"] = flowId;
                var dicHerader = new Dictionary<string, object>();
                //dicHerader.Add("jnpf_api", true);
                if (_userManager.ToKen != null && !_userManager.ToKen.Contains("::"))
                    dicHerader.Add("Authorization", _userManager.ToKen);

                // 本地url地址
                // var localAddress = App.Configuration["Kestrel:Endpoints:Http:Url"];
                var localAddress = GetLocalAddress();

                // 多语言
                string language = App.HttpContext.Request.Query["culture"];

                // 请求地址拼接
                if (entity.InterfaceUrl.First().Equals('/')) entity.InterfaceUrl = entity.InterfaceUrl.Substring(1, entity.InterfaceUrl.Length - 1);
                var id = dic.ContainsKey("Update_MainTablePrimary_Id") && dic["Update_MainTablePrimary_Id"].IsNotEmptyOrNull() ? dic["Update_MainTablePrimary_Id"].ToString() : dataId;
                var path = string.Format("{0}/{1}/{2}?culture={3}", localAddress, entity.InterfaceUrl, id, language);

                var result = new RESTfulResult<object>();
                try
                {
                    result = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(dic).PostAsStringAsync()).ToObjectOld<RESTfulResult<object>>();
                }
                catch (Exception)
                {
                    throw Oops.Oh(ErrorCode.IO0005);
                }

                if (!result.code.Equals(StatusCodes.Status200OK))
                {
                    result.msg = result.msg.ToString().Split("] ")[1];
                    throw Oops.Oh(ErrorCode.COM1018, result.msg);
                }
            }
        }

        return logList;
    }

    /// <summary>
    /// 获取或删除流程表单数据.
    /// </summary>
    /// <param name="id">表单模板id.</param>
    /// <param name="dataId">主键Id.</param>
    /// <param name="isDelete">是否删除（0:详情，1:删除）.</param>
    /// <param name="flowId">流程id.</param>
    /// <param name="isConvert">是否转换数据.</param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>?> GetOrDelFlowFormData(string id, string dataId, int isDelete, string flowId = "", bool isConvert = false)
    {
        var entity = (await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id.Equals(id))).Adapt<VisualDevEntity>();
        if (entity.Type.Equals(2))
        {
            var res = new Dictionary<string, object>();
            // 获取详情
            var dicHerader = new Dictionary<string, object>();
            dicHerader.Add("jnpf_api", true);
            if (_userManager.ToKen != null && !_userManager.ToKen.Contains("::"))
                dicHerader.Add("Authorization", _userManager.ToKen);

            // 本地url地址
            // var localAddress = App.Configuration["Kestrel:Endpoints:Http:Url"];
            var localAddress = GetLocalAddress();

            // 多语言
            string language = App.HttpContext.Request.Query["culture"];

            // 请求地址拼接
            if (entity.InterfaceUrl.First().Equals('/')) entity.InterfaceUrl = entity.InterfaceUrl.Substring(1, entity.InterfaceUrl.Length - 1);
            var path = string.Format("{0}/{1}/{2}?culture={3}", localAddress, entity.InterfaceUrl, dataId, language);
            try
            {
                if (isDelete == 1)
                {
                    await path.SetHeaders(dicHerader).DeleteAsync();
                    return null;
                }

                var dataStr = await path.SetHeaders(dicHerader).GetAsStringAsync();
                return dataStr.ToObjectOld<Dictionary<string, object>>();
            }
            catch (Exception)
            {
                throw Oops.Oh(ErrorCode.IO0005);
            }
        }
        else
        {
            if (isDelete == 1)
            {
                await DelHaveTableInfo(dataId, entity, flowId);
                return null;
            }

            return await GetHaveTableInfo(dataId, entity, flowId, isConvert);
        }
    }

    /// <summary>
    /// 流程表单数据传递.
    /// </summary>
    /// <param name="startFId">起始表单模板Id.</param>
    /// <param name="lastFId">上节点表单模板Id.</param>
    /// <param name="newFId">传递表单模板Id.</param>
    /// <param name="mapRule">映射规则字段 : Key 原字段, Value 映射字段.</param>
    /// <param name="allFormData">所有表单数据.</param>
    /// <param name="isSubFlow">是否子流程.</param>
    public async Task<Dictionary<string, object>> SaveDataToDataByFId(string startFId, string lastFId, string newFId, List<Dictionary<string, string>> mapRule, Dictionary<string, object> allFormData, bool isSubFlow = false)
    {
        var res = new Dictionary<string, object>();
        if (mapRule.IsNullOrEmpty()) mapRule = new List<Dictionary<string, string>>();
        var startFEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id.Equals(startFId));
        var lastFEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id.Equals(lastFId));
        var newFEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id.Equals(newFId));
        if (startFEntity == null || lastFEntity == null || newFEntity == null) throw Oops.Oh(ErrorCode.WF0039); // 未找到流程表单模板
        var startTInfo = new TemplateParsingBase(startFEntity.FormData, startFEntity.Tables, startFEntity.Type.ParseToInt()); // 起始模板
        var lastTInfo = new TemplateParsingBase(lastFEntity.FormData, lastFEntity.Tables, lastFEntity.Type.ParseToInt()); // 上节点模板
        var newTInfo = new TemplateParsingBase(newFEntity.FormData, newFEntity.Tables, newFEntity.Type.ParseToInt()); // 新模板

        if (startFEntity.Type.Equals(2) || lastFEntity.Type.Equals(2) || newFEntity.Type.Equals(2))
        {
            startTInfo.AllFieldsModel.ForEach(it =>
            {
                if (!it.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME) && !it.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER) && !it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                    it.__config__.jnpfKey = JnpfKeyConst.COMINPUT;
            });
            lastTInfo.AllFieldsModel.ForEach(it =>
            {
                if (!it.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME) && !it.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER) && !it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                    it.__config__.jnpfKey = JnpfKeyConst.COMINPUT;
            });
            newTInfo.AllFieldsModel.ForEach(it =>
            {
                if (!it.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME) && !it.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER) && !it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                    it.__config__.jnpfKey = JnpfKeyConst.COMINPUT;
            });
        }

        var childTableSplitKey = "tablefield";

        // 三个特殊的系统表单 (请假申请、销售订单、订单示例)
        if (startFEntity.EnCode.Equals("leaveApply") || startFEntity.EnCode.Equals("salesOrder") || startFEntity.EnCode.Equals("crmOrder") ||
            lastFEntity.EnCode.Equals("leaveApply") || lastFEntity.EnCode.Equals("salesOrder") || lastFEntity.EnCode.Equals("crmOrder") ||
            newFEntity.EnCode.Equals("leaveApply") || newFEntity.EnCode.Equals("salesOrder") || newFEntity.EnCode.Equals("crmOrder"))
        {
            childTableSplitKey = "-";
        }

        var oldTInfo = new TemplateParsingBase();
        oldTInfo.AllFieldsModel = new List<FieldsModel>();
        foreach (var items in mapRule)
        {
            var item = items.First();
            var newModel = newTInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Value));
            var oldModel = new FieldsModel();
            var vmodel = item.Key.Split("|").First();
            var fId = item.Key.Split("|").Last();
            if (fId == startFId)
            {
                oldModel = startTInfo.AllFieldsModel.Find(x => x.__vModel__ == vmodel);
                if (oldModel.IsNotEmptyOrNull())
                {
                    oldModel.__vModel__ = item.Key;
                    oldTInfo.AllFieldsModel.Add(oldModel);
                }
            }
            else if (fId == lastFId)
            {
                oldModel = lastTInfo.AllFieldsModel.Find(x => x.__vModel__ == vmodel);
                if (oldModel.IsNotEmptyOrNull())
                {
                    oldModel.__vModel__ = item.Key;
                    oldTInfo.AllFieldsModel.Add(oldModel);
                }
            }

            if (newModel.__vModel__.ToLower().Contains(childTableSplitKey))
            {
                var newCTable = newModel.__vModel__.Split("-").First();
                var newCField = newModel.__vModel__.Split("-").Last();
                if (vmodel.ToLower().Contains(childTableSplitKey))
                {
                    var formData = new Dictionary<string, object>();
                    if (item.Key.Contains(startFId))
                    {
                        formData = allFormData[startFId].ToObject<Dictionary<string, object>>();
                    }
                    else if (item.Key.Contains(lastFId))
                    {
                        formData = allFormData[lastFId].ToObject<Dictionary<string, object>>();
                    }

                    var oldCTable = vmodel.Split("-").First();
                    var oldCTData = formData[oldCTable].ToObject<List<Dictionary<string, object>>>();
                    var newCTData = res.ContainsKey(newCTable) ? res[newCTable].ToObject<List<Dictionary<string, object>>>() : new List<Dictionary<string, object>>();
                    if (oldCTData.Count > newCTData.Count)
                    {
                        var oldCount = newCTData.Count;
                        for (var i = 0; i < oldCTData.Count - oldCount; i++)
                        {
                            newCTData.Add(new Dictionary<string, object>());
                        }
                    }

                    res[newCTable] = newCTData;
                }
                else
                {
                    if (!res.ContainsKey(newCTable)) res.Add(newCTable, new List<Dictionary<string, object>>() { new Dictionary<string, object>() });
                }
            }
        }

        foreach (var items in mapRule)
        {
            var item = items.First();
            var oldModel = oldTInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Key));
            var newModel = newTInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Value));

            var formData = new Dictionary<string, object>();
            if (item.Key.Contains("|globalParameter"))
            {
                oldModel = new FieldsModel
                {
                    __vModel__ = item.Key.Split("|").First(),
                    __config__ = new ConfigModel()
                    {
                        jnpfKey = "globalParameter"
                    }
                };

                formData = allFormData["globalParameter"].ToObject<Dictionary<string, object>>();
            }
            else if (item.Key.Contains("|outsideParameter"))
            {
                oldModel = new FieldsModel
                {
                    __vModel__ = item.Key.Split("|").First(),
                    __config__ = new ConfigModel()
                    {
                        jnpfKey = "outsideParameter"
                    }
                };

                formData = allFormData["outsideParameter"].ToObject<Dictionary<string, object>>();
            }
            else if (item.Key.Contains(startFId) || item.Key == "@startNodeFormId")
            {
                formData = allFormData[startFId].ToObject<Dictionary<string, object>>();
            }
            else if (item.Key.Contains(lastFId) || item.Key == "@prevNodeFormId")
            {
                formData = allFormData[lastFId].ToObject<Dictionary<string, object>>();
            }

            // 上节点的表单id
            if (item.Key.Equals("@startNodeFormId") || item.Key.Equals("@prevNodeFormId"))
            {
                var formId = formData["id"];
                if (newModel.__vModel__.ToLower().Contains(childTableSplitKey))
                {
                    var childKey = newModel.__vModel__.Split("-");
                    var childTableKey = childKey.First();
                    var childFieldKey = childKey.Last();

                    var childItems = res[childTableKey].ToObject<List<Dictionary<string, object>>>();
                    foreach (var child in childItems)
                    {
                        child[childFieldKey] = formId;
                    }

                    res[childTableKey] = childItems;
                }
                else
                {
                    res[item.Value] = formId;
                }
            }
            else
            {
                if (oldModel == null || newModel == null || oldModel.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME) || oldModel.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER))
                {
                    res[item.Value] = string.Empty; // 找不到 默认赋予 空字符串
                    continue;
                }
                if (oldModel.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) || newModel.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                    continue;

                // 子表字段 - 子表字段
                if (oldModel.__vModel__.ToLower().Contains(childTableSplitKey) && newModel.__vModel__.ToLower().Contains(childTableSplitKey))
                {
                    if (DataTransferVerify(oldModel, newModel))
                    {
                        var oldCTable = oldModel.__vModel__.Split("-").First();
                        var oldCField = oldModel.__vModel__.Split("-").Last().Split("|").First();
                        var newCTable = newModel.__vModel__.Split("-").First();
                        var newCField = newModel.__vModel__.Split("-").Last();
                        if (oldCField.IsNullOrWhiteSpace() || newCField.IsNullOrWhiteSpace()) continue;

                        if (formData.ContainsKey(oldCTable) && formData[oldCTable] != null && formData[oldCTable].ToString() != "[]")
                        {
                            var oldCTData = formData[oldCTable].ToObject<List<Dictionary<string, object>>>();
                            var newCTData = res[newCTable].ToObject<List<Dictionary<string, object>>>();

                            for (var i = 0; i < oldCTData.Count; i++)
                            {
                                if (oldCTData[i].ContainsKey(oldCField))
                                    newCTData[i][newCField] = oldCTData[i][oldCField];
                            }

                            res[newCTable] = newCTData;
                        }
                    }
                }
                else if (oldModel.__vModel__.ToLower().Contains(childTableSplitKey) || newModel.__vModel__.ToLower().Contains(childTableSplitKey))
                {
                    if (DataTransferVerify(oldModel, newModel) || oldModel.__config__.jnpfKey == "globalParameter" || oldModel.__config__.jnpfKey == "outsideParameter")
                    {
                        // 子表字段 - 非子表字段
                        // 传递规则：默认选用上节点的第一条子表数据赋值到下节点的非子表字段内
                        if (oldModel.__vModel__.ToLower().Contains(childTableSplitKey) && !newModel.__vModel__.ToLower().Contains(childTableSplitKey))
                        {
                            var childTable = oldModel.__vModel__.Split("-").First();
                            var childField = oldModel.__vModel__.Split("-").Last().Split("|").First();
                            var childTableData = formData[childTable].ToObject<List<Dictionary<string, object>>>();
                            if (childTableData.Any() && childTableData.Any(x => x.ContainsKey(childField)))
                                res[newModel.__vModel__] = childTableData.First()[childField];
                        }

                        // 非子表字段 - 子表字段
                        // 传递规则：下节点子表新增一行将上节点字段赋值进去
                        if (!oldModel.__vModel__.ToLower().Contains(childTableSplitKey) && newModel.__vModel__.ToLower().Contains(childTableSplitKey))
                        {
                            var oldModelKey = oldModel.__vModel__.Split("|").First();
                            var childKey = newModel.__vModel__.Split("-");
                            var childTableKey = childKey.First();
                            var childFieldKey = childKey.Last();
                            if (formData.ContainsKey(oldModelKey))
                            {
                                var childFieldValue = formData[oldModelKey];
                                var childItems = res[childTableKey].ToObject<List<Dictionary<string, object>>>();
                                foreach (var child in childItems)
                                {
                                    child[childFieldKey] = childFieldValue;
                                }

                                res[childTableKey] = childItems;
                            }
                        }
                    }
                }
                else
                {
                    // 三个特殊的系统表单，不做验证规则
                    if (!childTableSplitKey.Equals("-") && !DataTransferVerify(oldModel, newModel)) res[oldModel.__vModel__] = string.Empty;

                    // 主表字段 - 主表字段
                    if (DataTransferVerify(oldModel, newModel) || oldModel.__config__.jnpfKey == "globalParameter" || oldModel.__config__.jnpfKey == "outsideParameter")
                    {
                        var oldModelKey = oldModel.__vModel__.Split("|").First();
                        res[newModel.__vModel__] = formData.ContainsKey(oldModelKey) ? formData[oldModelKey] : string.Empty;
                    }
                }
            }
        }

        // 系统表单 直接请求接口.
        if (newFEntity.Type.Equals(2) && !isSubFlow)
        {
            // 新增,修改
            var dic = allFormData[lastFId].ToObject<Dictionary<string, object>>();
            var dicHerader = new Dictionary<string, object>();
            dicHerader.Add("jnpf_api", true);
            if (_userManager.ToKen != null && !_userManager.ToKen.Contains("::"))
                dicHerader.Add("Authorization", _userManager.ToKen);

            // 本地url地址
            // var localAddress = App.Configuration["Kestrel:Endpoints:Http:Url"];
            var localAddress = GetLocalAddress();

            // 多语言
            string language = App.HttpContext.Request.Query["culture"];

            // 请求地址拼接
            if (newFEntity.InterfaceUrl.First().Equals('/')) newFEntity.InterfaceUrl = newFEntity.InterfaceUrl.Substring(1, newFEntity.InterfaceUrl.Length - 1);
            var path = string.Format("{0}/{1}/{2}?culture={3}", localAddress, newFEntity.InterfaceUrl, dic["id"].ToString(), language);
            try
            {
                await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(dic).PostAsStringAsync();
            }
            catch (Exception)
            {
            }

            res["id"] = dic["id"];
            return res;
        }

        // 获取请求端类型，并对应获取 数据权限
        DbLinkEntity link = await GetDbLink(newFEntity.DbLinkId);
        newTInfo.DbLink = link;
        List<DbTableFieldModel>? tableList = _databaseService.GetFieldList(link, newTInfo.MainTableName); // 获取主表 表结构 信息
        newTInfo.MainPrimary = tableList.Find(t => t.primaryKey && t.field.ToLower() != "f_tenant_id").field;

        var startFormData = allFormData[startFId].ToObject<Dictionary<string, object>>();
        var flowId = startFormData["flowId"].ToString();
        var flowTaskId = startFormData["flowTaskId"].ToString();

        var isUpdate = false;
        var oldData = await GetHaveTableInfo(flowTaskId, newFEntity.Adapt<VisualDevEntity>(), flowId);
        if (oldData.IsNotEmptyOrNull() && oldData.Any())
        {
            if (newTInfo.FormModel.concurrencyLock)
            {
                if (oldData.ContainsKey("f_version")) res.Add("f_version", oldData["f_version"]);
                if (oldData.ContainsKey("F_VERSION")) res.Add("f_version", oldData["F_VERSION"]);
            }

            if (oldData.ContainsKey("id")) res.Add("id", oldData["id"]);
            foreach (var data in oldData)
            {
                if (!res.ContainsKey(data.Key) && newTInfo.FieldsModelList.Any(x => x.__vModel__ == data.Key)) res.Add(data.Key, data.Value);
            }

            isUpdate = true;
        }
        if (!isSubFlow)
        {
            await SaveFlowFormData(newFEntity, res.ToJsonString(), flowTaskId, flowId, isUpdate, new List<string>());
        }
        return res;
    }

    /// <summary>
    /// 更新流程数据状态.
    /// </summary>
    /// <param name="formIdList"></param>
    /// <param name="flowTaskId"></param>
    /// <param name="flowState"></param>
    /// <returns></returns>
    public async Task UpdateFlowState(List<string> formIdList, string flowTaskId, int flowState)
    {
        foreach (var formId in formIdList)
        {
            var entity = await _visualDevRepository.AsQueryable().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(formId));
            if (entity.IsNotEmptyOrNull() && entity.Type.Equals(1))
            {
                var tInfo = new TemplateParsingBase(entity);
                tInfo.DbLink = await GetDbLink(entity.DbLinkId);
                var updateSql = string.Format("update {0} set f_flow_state={1} where f_flow_task_id='{2}'", tInfo.MainTableName, flowState, flowTaskId);
                await _databaseService.ExecuteSql(tInfo.DbLink, updateSql);
            }
        }
    }

    private string GetLocalAddress()
    {
        var res = string.Empty;
        var cacheKey = "localAdress";
        if (_cacheManager.Exists(cacheKey))
        {
            res = _cacheManager.Get(cacheKey);
        }
        else
        {
            var server = _serviceScope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
            var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            var addresses = addressesFeature?.Addresses;
            res = addresses.FirstOrDefault().Replace("[::]", "localhost");
            _cacheManager.Set(cacheKey, res, TimeSpan.FromMinutes(30));
        }

        return res;
    }

    #endregion

    #region 公用方法

    /// <summary>
    /// 删除有表信息.
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    public async Task DelHaveTableInfo(string id, VisualDevEntity templateEntity, string flowId = "")
    {
        if (id.IsNotEmptyOrNull())
        {
            TemplateParsingBase templateInfo = new TemplateParsingBase(templateEntity); // 解析模板控件
            DbLinkEntity link = await GetDbLink(templateEntity.DbLinkId);
            templateInfo.DbLink = link;
            var mainPrimary = templateInfo.MainTable?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
            if (mainPrimary.IsNullOrEmpty()) mainPrimary = GetPrimary(link, templateInfo.MainTableName);
            var data = await GetHaveTableInfo(id, templateEntity, flowId, true);
            if (flowId.IsNotEmptyOrNull() && data.IsNotEmptyOrNull() && data.ContainsKey("id")) id = data["id"].ToString();

            // 树形表格 删除父节点时同时删除子节点数据
            if (templateInfo.ColumnData.type.Equals(5))
            {
                var delIdDic = new Dictionary<string, string>();
                var dataList = _databaseService.GetData(link, templateInfo.MainTableName).ToObject<List<Dictionary<string, string>>>();
                dataList.ForEach(item => delIdDic.Add(item[mainPrimary], item[templateInfo.ColumnData.parentField]));
                var delIds = new List<string>();
                CodeGenHelper.GetChildIdList(delIdDic, id, delIds);
                await BatchDelHaveTableData(delIds.Distinct().ToList(), templateEntity);
            }
            else
            {
                if (templateInfo.FormModel.logicalDelete)
                {
                    var dbType = link?.DbType != null ? link.DbType : _visualDevRepository.AsSugarClient().CurrentConnectionConfig.DbType.ToString();
                    var sql = string.Empty;
                    if (dbType.Equals("Oracle"))
                        sql = string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3}='{4}'", templateInfo.MainTableName, _userManager.UserId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), mainPrimary, id);
                    else
                        sql = string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3}='{4}'", templateInfo.MainTableName, _userManager.UserId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), mainPrimary, id);

                    await _databaseService.ExecuteSql(link, sql); // 删除标识
                }
                else
                {
                    List<string>? allDelSql = new List<string>(); // 拼接语句
                    allDelSql.Add(string.Format("delete from {0} where {1} = '{2}';", templateInfo.MainTable.table, mainPrimary, id));
                    if (templateInfo.AllTable.Any(x => x.typeId.Equals("0")))
                    {
                        templateInfo.AllTable.Where(x => x.typeId.Equals("0")).ToList()
                            .ForEach(item => allDelSql.Add(string.Format("delete from {0} where {1}='{2}';", item.table, item.tableField, id))); // 删除所有涉及表数据 sql
                    }

                    foreach (string? item in allDelSql) await _databaseService.ExecuteSql(link, item); // 删除有表数据
                }
            }
        }
    }

    /// <summary>
    /// 删除子表数据.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task DelChildTable(VisualDevEntity templateEntity, VisualDevModelDelChildTableInput input)
    {
        TemplateParsingBase templateInfo = new TemplateParsingBase(templateEntity); // 解析模板控件
        DbLinkEntity link = await GetDbLink(templateEntity.DbLinkId);
        var model = templateInfo.ChildTableFieldsModelList.Find(x => x.__vModel__.Equals(input.table));
        if (model.IsNotEmptyOrNull())
        {
            var tableName = model.__config__.tableName;
            var sql = string.Format("delete from {0}", tableName);

            var queryWhere = new List<IConditionalModel>();
            var query = _userManager.AssembleQuery(input.queryConfig);
            if (query.IsNotEmptyOrNull()) queryWhere = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(query);

            _sqlSugarClient = _databaseService.ChangeDataBase(templateInfo.DbLink);
            var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(queryWhere).ToSqlString();
            _sqlSugarClient.AsTenant().ChangeDatabase("default");
            if (itemWhere.Contains("WHERE"))
            {
                var where = string.Format("  where {0}", itemWhere.Split("WHERE").Last().Replace(input.table + "-", string.Empty));
                sql += where;
            }

            _db.BeginTran();
            await _databaseService.ExecuteSql(link, sql); // 执行修改Sql
            _db.CommitTran();
        }
    }

    /// <summary>
    /// 批量删除有表数据.
    /// </summary>
    /// <param name="ids">id数组</param>
    /// <param name="templateEntity">模板实体</param>
    /// <param name="visualdevModelDataBatchDeForm"></param>
    /// <returns></returns>
    public async Task BatchDelHaveTableData(List<string>? ids, VisualDevEntity templateEntity, VisualDevModelDataBatchDelInput? visualdevModelDataBatchDeForm = null)
    {
        TemplateParsingBase templateInfo = new TemplateParsingBase(templateEntity); // 解析模板控件
        DbLinkEntity link = await GetDbLink(templateEntity.DbLinkId);

        if (ids.IsNotEmptyOrNull() && ids.Count > 0)
        {
            var flowTaskIds = new List<string>();
            var mainPrimary = templateInfo.MainTable?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
            if (mainPrimary.IsNullOrEmpty()) mainPrimary = GetPrimary(link, templateInfo.MainTableName);
            var cTableList = templateInfo.AllTable.Where(x => x.typeId.Equals("0")).ToList();

            var delNodeList = new List<string>();
            var nodeList = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowNodeEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.FlowId == b.FlowId))
                .Where((a, b) => a.FormId == templateEntity.Id && a.NodeType == "eventTrigger" && a.DeleteMark == null && b.EnabledMark == 1 && b.Type == 2 && b.DeleteMark == null)
                .Select((a, b) => a).ToListAsync();
            foreach (var item in nodeList)
            {
                var triggerPro = item.NodeJson.ToObject<TriggerProperties>();
                if (triggerPro.triggerFormEvent != 3) continue;
                var input = new VisualDevModelListQueryInput();
                input.superQueryJson = new { matchLogic = triggerPro.ruleMatchLogic, conditionList = triggerPro.ruleList }.ToJsonString();
                input.currentPage = 1;
                input.pageSize = 9999;
                input.isOnlyId = 0;
                input.flowIds = "jnpf";
                var result = await GetListResult(templateEntity, input);
                if (result.IsNotEmptyOrNull() && result.list.IsNotEmptyOrNull() && result.list.Any())
                {
                    var idList = result.list.Where(x => x.ContainsKey("id")).Select(x => x["id"]).ToList();
                    if (ids.Intersect(idList).Any())
                    {
                        delNodeList.Add(item.Id);
                    }
                }
            }

            var taskFlowData = new List<Dictionary<string, object>>();
            var allData = new List<TaskFlowDataModel>();
            var tableFieldList = new Dictionary<string, List<string>>();
            foreach (var id in ids)
            {
                var data = await GetHaveTableInfo(id, templateEntity, null, true);
                taskFlowData.Add(data);
                allData.Add(new TaskFlowDataModel { DataId = id, Data = data.ToJsonString() });

                // 关联外键值
                foreach (var cTable in cTableList)
                {
                    if (!tableFieldList.ContainsKey(cTable.table)) tableFieldList.Add(cTable.table, new List<string>());
                    if (data.ContainsKey(cTable.relationField) && data[cTable.relationField].IsNotEmptyOrNull())
                        tableFieldList[cTable.table].Add(data[cTable.relationField].ToString());
                    else
                        tableFieldList[cTable.table].Add(string.Empty);
                }

                if (data.IsNotEmptyOrNull() && data.ContainsKey("flowTaskId") && data["flowTaskId"].IsNotEmptyOrNull())
                    flowTaskIds.Add(data["flowTaskId"].ToString());
            }

            // 流程
            if (flowTaskIds.Any())
            {
                ids = await _flowTaskRepository.DeleteLaunchTask(flowTaskIds);
                mainPrimary = "f_flow_task_id";
            }

            var allDelSql = new List<string>();
            var dbType = link?.DbType != null ? link.DbType : _visualDevRepository.AsSugarClient().CurrentConnectionConfig.DbType.ToString();
            if (templateInfo.FormModel.logicalDelete)
            {
                var delUserId = _userManager.UserId;
                var delTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (visualdevModelDataBatchDeForm.IsNotEmptyOrNull() && visualdevModelDataBatchDeForm.deleteRule.Equals(0))
                {
                    if (dbType.Equals("Oracle"))
                        allDelSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3} not in ('{4}')", templateInfo.MainTableName, delUserId, delTime, mainPrimary, string.Join("','", ids)));
                    else
                        allDelSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3} not in ('{4}')", templateInfo.MainTableName, delUserId, delTime, mainPrimary, string.Join("','", ids)));
                }
                else
                {
                    if (dbType.Equals("Oracle"))
                        allDelSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3} in ('{4}')", templateInfo.MainTableName, delUserId, delTime, mainPrimary, string.Join("','", ids)));
                    else
                        allDelSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3} in ('{4}')", templateInfo.MainTableName, delUserId, delTime, mainPrimary, string.Join("','", ids)));
                }

                foreach (var item in cTableList)
                {
                    var cIsNull = false;
                    var cIds = string.Empty;
                    foreach (var cId in tableFieldList[item.table])
                    {
                        if (cId.IsNotEmptyOrNull())
                            cIds += cIds.IsNotEmptyOrNull() ? "','" + cId : cId;
                        else
                            cIsNull = true;
                    }

                    if (visualdevModelDataBatchDeForm.IsNotEmptyOrNull() && visualdevModelDataBatchDeForm.deleteRule.Equals(0))
                    {
                        if (dbType.Equals("Oracle"))
                            allDelSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3} not in ('{4}')", item.table, delUserId, delTime, item.tableField, string.Join("','", tableFieldList[item.table])));
                        else
                            allDelSql.Add(string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3} not in ('{4}')", item.table, delUserId, delTime, item.tableField, string.Join("','", tableFieldList[item.table])));
                    }
                    else
                    {
                        var cSql = string.Empty;
                        if (dbType.Equals("Oracle"))
                            cSql = string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time=to_date('{2}','yyyy-mm-dd HH24/MI/SS') where {3} in ('{4}')", item.table, delUserId, delTime, item.tableField, cIds);
                        else
                            cSql = string.Format("update {0} set f_delete_mark=1,f_delete_user_id='{1}',f_delete_time='{2}' where {3} in ('{4}')", item.table, delUserId, delTime, item.tableField, cIds);
                        if (cIsNull) cSql += string.Format(" or {0} is null", item.tableField);
                        allDelSql.Add(cSql);
                    }
                }
            }
            else
            {
                if (visualdevModelDataBatchDeForm.IsNotEmptyOrNull() && visualdevModelDataBatchDeForm.deleteRule.Equals(0))
                    allDelSql.Add(string.Format("delete from {0} where {1} not in ('{2}')", templateInfo.MainTable.table, mainPrimary, string.Join("','", ids))); // 主表数据
                else
                    allDelSql.Add(string.Format("delete from {0} where {1} in ('{2}')", templateInfo.MainTable.table, mainPrimary, string.Join("','", ids))); // 主表数据

                foreach (var item in cTableList)
                {
                    var cIsNull = false;
                    var cIds = string.Empty;
                    foreach (var cId in tableFieldList[item.table])
                    {
                        if (cId.IsNotEmptyOrNull())
                            cIds += cIds.IsNotEmptyOrNull() ? "','" + cId : cId;
                        else
                            cIsNull = true;
                    }

                    if (visualdevModelDataBatchDeForm.IsNotEmptyOrNull() && visualdevModelDataBatchDeForm.deleteRule.Equals(0))
                    {
                        allDelSql.Add(string.Format("delete from {0} where {1} not in ('{2}')", item.table, item.tableField, string.Join("','", tableFieldList[item.table])));
                    }
                    else
                    {
                        var cSql = string.Format("delete from {0} where {1} in ('{2}')", item.table, item.tableField, cIds);
                        if (cIsNull) cSql += string.Format(" or {0} is null", item.tableField);
                        allDelSql.Add(cSql);
                    }
                }
            }

            // 删除有表数据
            foreach (string? item in allDelSql) await _databaseService.ExecuteSql(link, item);

            // 添加任务流程`事件触发`批量删除事件
            if (visualdevModelDataBatchDeForm.IsNotEmptyOrNull() && visualdevModelDataBatchDeForm.isInteAssis)
            {
                var model = new TaskFlowEventModel();
                model.TenantId = _userManager.TenantId;
                model.UserId = _userManager.UserId;
                model.ModelId = templateEntity.Id;
                model.TriggerType = "eventTrigger";
                model.ActionType = 3;
                model.taskFlowData = taskFlowData;
                model.delNodeIdList = delNodeList;
                await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTask", model));
            }
        }
        else if (visualdevModelDataBatchDeForm.deleteRule.Equals(0))
        {
            var deleteSql = new List<string>();
            foreach (var item in templateInfo.AllTable)
            {
                var tableSql = string.Format("delete from {0}", item.table);
                deleteSql.Add(tableSql);
            }

            _db.BeginTran();
            foreach (var item in deleteSql) await _databaseService.ExecuteSql(link, item); // 执行修改Sql
            _db.CommitTran();
        }
    }

    /// <summary>
    /// 生成系统自动生成字段.
    /// </summary>
    /// <param name="moduleId">模板id.</param>
    /// <param name="fieldsModelListJson">模板数据.</param>
    /// <param name="allDataMap">真实数据.</param>
    /// <param name="IsCreate">创建与修改标识 true创建 false 修改.</param>
    /// <param name="systemControlList">不赋值的系统控件Key.</param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GenerateFeilds(string moduleId, string fieldsModelListJson, Dictionary<string, object> allDataMap, bool IsCreate, List<string>? systemControlList = null)
    {
        List<FieldsModel> fieldsModelList = fieldsModelListJson.ToList<FieldsModel>();
        UserEntity? userInfo = _userManager.User;
        int dicCount = allDataMap.Keys.Count;
        string[] strKey = new string[dicCount];

        var flowId = allDataMap.ContainsKey("flowId") && allDataMap["flowId"].IsNotEmptyOrNull() ? allDataMap["flowId"].ToString() : null;
        foreach (var model in fieldsModelList)
        {
            if (model != null && model.__vModel__.IsNotEmptyOrNull())
            {
                // 如果模板jnpfKey为table为子表数据
                if (model.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__] != null)
                {
                    List<FieldsModel> childFieldsModelList = model.__config__.children;
                    object? objectData = allDataMap[model.__vModel__];
                    List<Dictionary<string, object>> childAllDataMapList = objectData.ToJsonString().ToObject<List<Dictionary<string, object>>>();
                    if (childAllDataMapList != null && childAllDataMapList.Count > 0)
                    {
                        List<Dictionary<string, object>> newChildAllDataMapList = new List<Dictionary<string, object>>();
                        foreach (Dictionary<string, object>? childmap in childAllDataMapList)
                        {
                            var newChildData = new Dictionary<string, object>();
                            foreach (var item in model.__config__.children)
                            {
                                if (childmap.ContainsKey("id")) newChildData["id"] = childmap["id"];
                                if (childmap.ContainsKey("ID")) newChildData["id"] = childmap["ID"];
                                if (childmap.ContainsKey("jnpfId")) newChildData["jnpfId"] = childmap["jnpfId"];

                                var vModel = string.Format("{0}-{1}", model.__vModel__, item.__vModel__);
                                if (systemControlList.IsNullOrEmpty() || (systemControlList.IsNotEmptyOrNull() && !systemControlList.Contains(vModel)))
                                {
                                    switch (item.__config__.jnpfKey)
                                    {
                                        case JnpfKeyConst.BILLRULE:
                                            if (!childmap.ContainsKey(item.__vModel__) || (childmap.ContainsKey(item.__vModel__) && childmap[item.__vModel__].IsNullOrEmpty()))
                                            {
                                                var billNumber = string.Empty;
                                                if (item.__config__.ruleType.IsNotEmptyOrNull() && item.__config__.ruleType == 2)
                                                {
                                                    billNumber = await _formDataParsing.GetBillRule(moduleId, item.__config__, newChildData, flowId, true);
                                                    newChildData[item.__vModel__] = billNumber;
                                                }
                                                else
                                                {
                                                    billNumber = await _billRuleService.GetBillNumber(item.__config__.rule);
                                                    if (!"单据规则不存在".Equals(billNumber)) newChildData[item.__vModel__] = billNumber;
                                                    else newChildData[item.__vModel__] = string.Empty;
                                                }
                                            }
                                            else
                                            {
                                                newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            }

                                            break;
                                        case JnpfKeyConst.CREATEUSER:
                                            if (!childmap.ContainsKey("id"))
                                                newChildData[item.__vModel__] = userInfo.Id;
                                            else if (childmap.ContainsKey(item.__vModel__))
                                                newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            break;
                                        case JnpfKeyConst.MODIFYUSER:
                                            if (childmap.ContainsKey("id"))
                                                newChildData[item.__vModel__] = userInfo.Id;
                                            else if (childmap.ContainsKey(item.__vModel__))
                                                newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            break;
                                        case JnpfKeyConst.CREATETIME:
                                            if (!childmap.ContainsKey("id"))
                                                newChildData[item.__vModel__] = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                                            else if (childmap.ContainsKey(item.__vModel__))
                                                newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            break;
                                        case JnpfKeyConst.MODIFYTIME:
                                            if (childmap.ContainsKey("id"))
                                                newChildData[item.__vModel__] = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                                            else if (childmap.ContainsKey(item.__vModel__))
                                                newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            break;
                                        case JnpfKeyConst.CURRPOSITION:
                                            if (!childmap.ContainsKey("id"))
                                            {
                                                if (allDataMap.ContainsKey("Jnpf_FlowDelegate_CurrPosition")) // 流程委托 需要指定所属岗位
                                                    newChildData[item.__vModel__] = allDataMap["Jnpf_FlowDelegate_CurrPosition"];
                                                else
                                                    newChildData[item.__vModel__] = _userManager.CurrentPositionList;
                                            }
                                            else if (childmap.ContainsKey(item.__vModel__))
                                            {
                                                newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            }

                                            break;
                                        case JnpfKeyConst.CURRORGANIZE:
                                            if (!childmap.ContainsKey("id"))
                                            {
                                                if (allDataMap.ContainsKey("Jnpf_FlowDelegate_CurrOrganize")) // 流程委托 需要指定所属组织
                                                    newChildData[item.__vModel__] = allDataMap["Jnpf_FlowDelegate_CurrOrganize"];
                                                else
                                                    newChildData[item.__vModel__] = _userManager.CurrentOrganizeList.ToJsonStringOld();
                                            }
                                            else if (childmap.ContainsKey(item.__vModel__))
                                            {
                                                newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            }

                                            break;
                                        case JnpfKeyConst.UPLOADFZ: // 文件上传
                                            if (!childmap.ContainsKey(item.__vModel__) || childmap[item.__vModel__].IsNullOrEmpty()) newChildData[item.__vModel__] = new string[] { };
                                            else newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            break;
                                        default:
                                            if (childmap.ContainsKey(item.__vModel__)) newChildData[item.__vModel__] = childmap[item.__vModel__];
                                            break;
                                    }
                                }
                            }

                            newChildAllDataMapList.Add(newChildData);
                            allDataMap[model.__vModel__] = newChildAllDataMapList;
                        }
                    }
                }
                else
                {
                    if (systemControlList.IsNotEmptyOrNull() && systemControlList.Contains(model.__vModel__))
                    {
                        allDataMap.Remove(model.__vModel__);
                    }
                    else
                    {
                        switch (model.__config__.jnpfKey)
                        {
                            case JnpfKeyConst.BILLRULE:
                                if (!allDataMap.ContainsKey(model.__vModel__) || (allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__].IsNullOrEmpty()))
                                {
                                    var billNumber = string.Empty;
                                    if (model.__config__.ruleType.IsNotEmptyOrNull() && model.__config__.ruleType == 2)
                                    {
                                        billNumber = await _formDataParsing.GetBillRule(moduleId, model.__config__, allDataMap, flowId);
                                        allDataMap[model.__vModel__] = billNumber;
                                    }
                                    else
                                    {
                                        billNumber = await _billRuleService.GetBillNumber(model.__config__.rule);
                                        if (!"单据规则不存在".Equals(billNumber)) allDataMap[model.__vModel__] = billNumber;
                                        else allDataMap[model.__vModel__] = string.Empty;
                                    }
                                }

                                break;
                            case JnpfKeyConst.CREATEUSER:
                                if (IsCreate)
                                {
                                    allDataMap[model.__vModel__] = userInfo.Id;
                                }
                                else if (allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__].IsNotEmptyOrNull())
                                {
                                    var accout = allDataMap[model.__vModel__].ToString()?.Split("/").Last();
                                    allDataMap[model.__vModel__] = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(x => x.Account == accout).Select(x => x.Id).FirstAsync();
                                }

                                break;
                            case JnpfKeyConst.CREATETIME:
                                if (IsCreate)
                                    allDataMap[model.__vModel__] = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                                break;
                            case JnpfKeyConst.MODIFYUSER:
                                if (!IsCreate)
                                {
                                    allDataMap[model.__vModel__] = userInfo.Id;
                                }
                                else if (allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__].IsNotEmptyOrNull())
                                {
                                    var accout = allDataMap[model.__vModel__].ToString()?.Split("/").Last();
                                    allDataMap[model.__vModel__] = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(x => x.Account == accout).Select(x => x.Id).FirstAsync();
                                }

                                break;
                            case JnpfKeyConst.MODIFYTIME:
                                if (!IsCreate)
                                    allDataMap[model.__vModel__] = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                                break;
                            case JnpfKeyConst.CURRPOSITION:
                                if (IsCreate)
                                {
                                    if (allDataMap.ContainsKey("Jnpf_FlowDelegate_CurrPosition")) // 流程委托 需要指定所属岗位
                                        allDataMap[model.__vModel__] = allDataMap["Jnpf_FlowDelegate_CurrPosition"];
                                    else
                                        allDataMap[model.__vModel__] = _userManager.CurrentPositionList;
                                }
                                else if (allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__].IsNotEmptyOrNull())
                                {
                                    var posList = new List<string>();
                                    foreach (var item in allDataMap[model.__vModel__].ToString().Split(","))
                                    {
                                        var posName = item.Split("/").Last();
                                        var pos = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.FullName == posName).Select(x => x.Id).FirstAsync();
                                        posList.Add(pos);
                                    }

                                    if (posList.Count > 0) allDataMap[model.__vModel__] = posList.ToJsonStringOld();
                                }

                                break;
                            case JnpfKeyConst.CURRORGANIZE:
                                if (IsCreate)
                                {
                                    if (allDataMap.ContainsKey("Jnpf_FlowDelegate_CurrOrganize")) // 流程委托 需要指定所属组织
                                        allDataMap[model.__vModel__] = allDataMap["Jnpf_FlowDelegate_CurrOrganize"];
                                    else
                                        allDataMap[model.__vModel__] = _userManager.CurrentOrganizeList.ToJsonStringOld();
                                }
                                else if (allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__].IsNotEmptyOrNull())
                                {
                                    var orgList = new List<string>();
                                    foreach (var item in allDataMap[model.__vModel__].ToString().Split(","))
                                    {
                                        var orgName = item.Split("/").Last();
                                        var org = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.FullName == orgName).Select(x => x.Id).FirstAsync();
                                        orgList.Add(org); orgList.Add(org);
                                    }

                                    if (orgList.Count > 0) allDataMap[model.__vModel__] = orgList.ToJsonStringOld();
                                }

                                break;
                            case JnpfKeyConst.UPLOADFZ: // 文件上传
                                if (!allDataMap.ContainsKey(model.__vModel__) || allDataMap[model.__vModel__].IsNullOrEmpty()) allDataMap[model.__vModel__] = new string[] { };
                                break;
                        }
                    }
                }
            }
        }

        return allDataMap;
    }

    /// <summary>
    /// 获取数据连接, 根据连接Id.
    /// </summary>
    /// <param name="linkId"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    public async Task<DbLinkEntity> GetDbLink(string linkId, string? tenantId = null)
    {
        DbLinkEntity link = await _dbLinkService.GetInfo(linkId);
        if (link == null)
        {
            if (tenantId.IsNotEmptyOrNull())
            {
                var tenantCache = _cacheManager.Get<List<GlobalTenantCacheModel>>(CommonConst.GLOBALTENANT).Find(it => it.TenantId.Equals(tenantId));
                if (tenantCache.type.Equals(1))
                    link = _databaseService.GetTenantDbLink(tenantCache.TenantId, tenantCache.connectionConfig.IsolationField);
                else
                    link = _databaseService.GetTenantDbLink(tenantCache.TenantId, tenantCache.connectionConfig.ConfigList.First().ServiceName);
            }
            else
            {
                link = _databaseService.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
            }
        }
        return link;
    }

    /// <summary>
    /// 无限递归 给控件绑定默认值 (绕过 布局控件).
    /// </summary>
    public void FieldBindDefaultValue(ref List<Dictionary<string, object>> dicFieldsModelList, string defaultUserId, List<string> defaultPosIds, List<string> defaultRoleIds, List<string> defaultGroupIds, List<UserRelationEntity> userRelationList)
    {
        foreach (var item in dicFieldsModelList)
        {
            var obj = item["__config__"].ToObject<Dictionary<string, object>>();

            if (obj.ContainsKey("jnpfKey") && (obj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.POSSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.ROLESELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.GROUPSELECT) || obj["jnpfKey"].Equals(JnpfKeyConst.USERSSELECT)) && obj.ContainsKey("defaultCurrent") && obj["defaultCurrent"].Equals(true))
            {
                switch (obj["jnpfKey"])
                {
                    case JnpfKeyConst.USERSSELECT:
                    case JnpfKeyConst.USERSELECT:
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var ableDepIds = item["ableDepIds"].ToObject<List<string>>();
                            if (ableDepIds == null) ableDepIds = new List<string>();
                            var ablePosIds = item["ablePosIds"].ToObject<List<string>>();
                            if (ablePosIds == null) ablePosIds = new List<string>();
                            var ableUserIds = item["ableUserIds"].ToObject<List<string>>();
                            if (ableUserIds == null) ableUserIds = new List<string>();
                            var ableRoleIds = item["ableRoleIds"].ToObject<List<string>>();
                            if (ableRoleIds == null) ableRoleIds = new List<string>();
                            var ableGroupIds = item["ableGroupIds"].ToObject<List<string>>();
                            if (ableGroupIds == null) ableGroupIds = new List<string>();
                            var userIdList = userRelationList.Where(x => ableUserIds.Contains(x.UserId) || ableDepIds.Contains(x.ObjectId)
                                || ablePosIds.Contains(x.ObjectId) || ableRoleIds.Contains(x.ObjectId) || ableGroupIds.Contains(x.ObjectId)).Select(x => x.UserId).ToList();
                            if (!userIdList.Contains(defaultUserId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }
                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                        {
                            if (obj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT))
                            {
                                obj["defaultValue"] = new List<string>() { defaultUserId };
                            }
                            else
                            {
                                obj["defaultValue"] = new List<string>() { string.Format("{0}--user", defaultUserId) };
                            }
                        }
                        else
                        {
                            obj["defaultValue"] = defaultUserId;
                        }
                        break;
                    case JnpfKeyConst.POSSELECT:
                        var defaultPosId = defaultPosIds.FirstOrDefault();
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var defValue = item["ablePosIds"].ToObject<List<string>>();
                            if (!defValue.Contains(defaultPosId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }
                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                        {
                            obj["defaultValue"] = defaultPosIds;
                        }
                        else
                        {
                            if (defaultPosIds.Contains(_userManager.User.PositionId))
                            {
                                obj["defaultValue"] = _userManager.User.PositionId;
                            }
                            else
                            {
                                obj["defaultValue"] = defaultPosId;
                            }
                        }
                        break;
                    case JnpfKeyConst.ROLESELECT:
                        var defaultRoleId = defaultRoleIds.FirstOrDefault();
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var defValue = item["ableRoleIds"].ToObject<List<string>>();
                            if (!defValue.Contains(defaultRoleId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }
                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true)) obj["defaultValue"] = defaultRoleIds;
                        else obj["defaultValue"] = defaultRoleId;
                        break;
                    case JnpfKeyConst.GROUPSELECT:
                        var defaultGroupId = defaultGroupIds.FirstOrDefault();
                        if (item.ContainsKey("selectType") && item["selectType"].Equals("custom"))
                        {
                            var defValue = item["ableGroupIds"].ToObject<List<string>>();
                            if (!defValue.Contains(defaultGroupId))
                            {
                                obj["defaultValue"] = null;
                                break;
                            }
                        }
                        if (item.ContainsKey("multiple") && item["multiple"].Equals(true)) obj["defaultValue"] = defaultGroupIds;
                        else obj["defaultValue"] = defaultGroupId;
                        break;
                }
            }

            // 子表控件
            if (obj.ContainsKey("jnpfKey") && obj["jnpfKey"].Equals(JnpfKeyConst.TABLE))
            {
                var cList = obj["children"].ToObject<List<Dictionary<string, object>>>();
                foreach (var child in cList)
                {
                    var cObj = child["__config__"].ToObject<Dictionary<string, object>>();
                    if (cObj.ContainsKey("jnpfKey") && (cObj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT) || cObj["jnpfKey"].Equals(JnpfKeyConst.POSSELECT) || cObj["jnpfKey"].Equals(JnpfKeyConst.ROLESELECT) || cObj["jnpfKey"].Equals(JnpfKeyConst.GROUPSELECT) || cObj["jnpfKey"].Equals(JnpfKeyConst.USERSSELECT)) && cObj.ContainsKey("defaultCurrent") && cObj["defaultCurrent"].Equals(true))
                    {
                        switch (cObj["jnpfKey"])
                        {
                            case JnpfKeyConst.USERSSELECT:
                            case JnpfKeyConst.USERSELECT:
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                                {
                                    if (cObj["jnpfKey"].Equals(JnpfKeyConst.USERSELECT))
                                    {
                                        cObj["defaultValue"] = new List<string>() { defaultUserId };
                                    }
                                    else
                                    {
                                        cObj["defaultValue"] = new List<string>() { string.Format("{0}--user", defaultUserId) };
                                    }
                                }
                                else
                                {
                                    cObj["defaultValue"] = defaultUserId;
                                }
                                break;
                            case JnpfKeyConst.POSSELECT:
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true))
                                {
                                    cObj["defaultValue"] = defaultPosIds;
                                }
                                else
                                {
                                    if (defaultPosIds.Contains(_userManager.User.PositionId))
                                    {
                                        cObj["defaultValue"] = _userManager.User.PositionId;
                                    }
                                    else
                                    {
                                        cObj["defaultValue"] = defaultPosIds.FirstOrDefault();
                                    }
                                }
                                break;
                            case JnpfKeyConst.ROLESELECT:
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true)) cObj["defaultValue"] = defaultRoleIds;
                                else cObj["defaultValue"] = defaultRoleIds.FirstOrDefault();
                                break;
                            case JnpfKeyConst.GROUPSELECT:
                                if (item.ContainsKey("multiple") && item["multiple"].Equals(true)) cObj["defaultValue"] = defaultGroupIds;
                                else cObj["defaultValue"] = defaultGroupIds.FirstOrDefault();
                                break;
                        }
                    }

                    child["__config__"] = cObj;
                }

                obj["children"] = cList;
            }

            // 递归布局控件
            if (obj.ContainsKey("children"))
            {
                var fmList = obj["children"].ToObject<List<Dictionary<string, object>>>();
                FieldBindDefaultValue(ref fmList, defaultUserId, defaultPosIds, defaultRoleIds, defaultGroupIds, userRelationList);
                obj["children"] = fmList;
            }

            item["__config__"] = obj;
        }
    }

    /// <summary>
    /// 处理模板默认值 (针对流程表单).
    /// 用户选择 , 岗位选择 , 角色选择 , 分组选择.
    /// </summary>
    /// <param name="propertyJson">表单json.</param>
    /// <param name="tableJson">关联表单.</param>
    /// <param name="formType">表单类型（1：系统表单 2：自定义表单）.</param>
    /// <returns></returns>
    public string GetVisualDevModelDataConfig(string propertyJson, string tableJson, int formType)
    {
        var tInfo = new TemplateParsingBase(propertyJson, tableJson, formType);
        if (tInfo.AllFieldsModel.Any(x => (x.__config__.defaultCurrent) && (x.__config__.jnpfKey.Equals(JnpfKeyConst.USERSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.POSSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.ROLESELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.GROUPSELECT))))
        {
            var userId = _userManager.UserId;
            var posIds = _visualDevRepository.AsSugarClient().Queryable<PositionEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.ObjectId && b.ObjectType.Equals(CommonConst.RelationObjectPosition)))
                .Where((a, b) => b.UserId.Equals(_userManager.UserId) && a.OrganizeId.Equals(_userManager.User.OrganizeId)).Select(a => a.Id).ToList();
            var roleIds = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(it => it.UserId.Equals(_userManager.UserId) && it.ObjectType.Equals(CommonConst.RelationObjectRole)).Select(it => it.ObjectId).ToList();
            var groupIds = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(it => it.UserId.Equals(_userManager.UserId) && it.ObjectType.Equals(CommonConst.RelationObjectGroup)).Select(it => it.ObjectId).ToList();

            var allUserRelationList = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Select(x => new UserRelationEntity() { UserId = x.UserId, ObjectId = x.ObjectId }).ToList();

            var configData = propertyJson.ToObject<Dictionary<string, object>>();
            var columnList = configData["fields"].ToObject<List<Dictionary<string, object>>>();
            FieldBindDefaultValue(ref columnList, userId, posIds, roleIds, groupIds, allUserRelationList);
            configData["fields"] = columnList;
            propertyJson = configData.ToJsonString();
        }

        return propertyJson;
    }

    /// <summary>
    /// 同步业务需要的字段.
    /// </summary>
    /// <param name="tInfo"></param>
    /// <returns></returns>
    public async Task SyncField(TemplateParsingBase tInfo)
    {
        if (tInfo.IsHasTable && !tInfo.visualDevEntity.WebType.Equals(4))
        {
            var isUpper = tInfo.DbLink.DbType.ToLower().Equals("oracle") || tInfo.DbLink.DbType.ToLower().Equals("dm") || tInfo.DbLink.DbType.ToLower().Equals("dm8") ? true : false;
            var mainTableFields = tInfo.MainTable.fields;
            var updateFields = new List<EntityFieldModel>();
            var addFieldList = new List<DbTableFieldModel>();

            // 乐观锁
            if (tInfo.FormModel.concurrencyLock)
            {
                var version = isUpper ? "F_VERSION" : "f_version";
                if (!mainTableFields.Any(x => x.field.Equals(version)))
                {
                    var field = new DbTableFieldModel() { field = version, fieldName = "并发锁定字段", dataType = "bigint", dataLength = "20", allowNull = 1, defaults = "0" };
                    if (!_databaseService.IsAnyColumn(tInfo.DbLink, tInfo.MainTableName, version))
                    {
                        var fieldList = new List<DbTableFieldModel>() { field };
                        await _databaseService.AddTableColumn(tInfo.DbLink, tInfo.MainTableName, fieldList);

                        // f_version 赋予默认值 0
                        var sql = string.Format("update {0} set f_version=0 where f_version is null;", tInfo.MainTableName);
                        await _databaseService.ExecuteSql(tInfo.DbLink, sql);
                    }

                    var uField = field.Adapt<EntityFieldModel>();
                    updateFields.Add(uField);
                }
            }

            // 流程字段
            var flowId = isUpper ? "F_FLOW_ID" : "f_flow_id";
            var flowTaskId = isUpper ? "F_FLOW_TASK_ID" : "f_flow_task_id";
            var flowState = isUpper ? "F_FLOW_STATE" : "f_flow_state";
            if (!mainTableFields.Any(x => x.field.Equals(flowId)))
            {
                var field = new DbTableFieldModel() { field = flowId, fieldName = "流程引擎Id", dataType = "varchar", dataLength = "50", allowNull = 1 };
                if (!_databaseService.IsAnyColumn(tInfo.DbLink, tInfo.MainTableName, flowId))
                    addFieldList.Add(field);

                var uField = field.Adapt<EntityFieldModel>();
                updateFields.Add(uField);
            }
            if (!mainTableFields.Any(x => x.field.Equals(flowTaskId)))
            {
                var field = new DbTableFieldModel() { field = flowTaskId, fieldName = "流程任务Id", dataType = "varchar", dataLength = "50", allowNull = 1 };
                if (!_databaseService.IsAnyColumn(tInfo.DbLink, tInfo.MainTableName, flowTaskId))
                    addFieldList.Add(field);

                var uField = field.Adapt<EntityFieldModel>();
                updateFields.Add(uField);
            }
            if (!mainTableFields.Any(x => x.field.Equals(flowState)))
            {
                var field = new DbTableFieldModel() { field = flowState, fieldName = "流程状态", dataType = "int", dataLength = "1", allowNull = 1 };
                if (!_databaseService.IsAnyColumn(tInfo.DbLink, tInfo.MainTableName, flowState))
                {
                    var fieldList = new List<DbTableFieldModel>() { field };
                    await _databaseService.AddTableColumn(tInfo.DbLink, tInfo.MainTableName, fieldList);

                    var flowTaskSql = string.Format("select f_flow_task_id from {0} where f_flow_task_id is not null", tInfo.MainTableName);
                    if (tInfo.FormModel.logicalDelete) flowTaskSql += " and f_delete_mark is null";
                    var ftList = _databaseService.GetSqlData(tInfo.DbLink, flowTaskSql).ToObject<List<Dictionary<string, string>>>();
                    var flowTaskIds = ftList.Select(a => a.First().Value).ToList();
                    var flowTaskList = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowTaskEntity>()
                        .Where(x => x.DeleteMark == null && flowTaskIds.Contains(x.Id))
                        .Select(x => new WorkFlowTaskEntity { Id = x.Id, Status = x.Status })
                        .ToListAsync();
                    foreach (var ftId in flowTaskIds)
                    {
                        var ft = flowTaskList.Find(x => x.Id.Equals(ftId));
                        var state = ft.IsNotEmptyOrNull() ? ft.Status : 0;
                        var ftSql = string.Format("update {0} set f_flow_state={1} where f_flow_task_id='{2}';", tInfo.MainTableName, state, ftId);
                        await _databaseService.ExecuteSql(tInfo.DbLink, ftSql);
                    }
                }

                var uField = field.Adapt<EntityFieldModel>();
                updateFields.Add(uField);
            }

            if (addFieldList.Any()) await _databaseService.AddTableColumn(tInfo.DbLink, tInfo.MainTableName, addFieldList);

            var isUpdate = false;
            var tableData = tInfo.visualDevEntity.Tables.ToObject<List<TableModel>>();
            foreach (var upField in updateFields)
            {
                tableData.Find(x => x.typeId.Equals("1"))?.fields.Add(upField);
                isUpdate = true;
            }

            var tenantId = isUpper ? "F_TENANT_ID" : "f_tenant_id";
            var deleteMark = isUpper ? "F_DELETE_MARK" : "f_delete_mark";
            var deleteUserId = isUpper ? "F_DELETE_USER_ID" : "f_delete_user_id";
            var deleteTime = isUpper ? "F_DELETE_TIME" : "f_delete_time";
            foreach (var table in tInfo.AllTable)
            {
                // 租户字段
                if (!table.fields.Any(x => x.field.Equals(tenantId)))
                {
                    var field = new DbTableFieldModel() { field = tenantId, fieldName = "租户Id", dataType = "varchar", dataLength = "50", primaryKey = true, allowNull = 0 };
                    if (!_databaseService.IsAnyColumn(tInfo.DbLink, table.table, tenantId))
                    {
                        var fieldList = new List<DbTableFieldModel>() { field };
                        await _databaseService.AddTableColumn(tInfo.DbLink, table.table, fieldList);
                    }

                    var uField = field.Adapt<EntityFieldModel>();
                    tableData.Find(x => x.table.Equals(table.table))?.fields.Add(uField);
                    isUpdate = true;
                }

                // 逻辑删除
                if (tInfo.FormModel.logicalDelete)
                {
                    if (!table.fields.Any(x => x.field.Equals(deleteMark)))
                    {
                        var field = new DbTableFieldModel() { field = deleteMark, fieldName = "删除标识", dataType = "int", dataLength = "1", allowNull = 1 };
                        if (!_databaseService.IsAnyColumn(tInfo.DbLink, table.table, deleteMark))
                        {
                            var fieldList = new List<DbTableFieldModel>() { field };
                            await _databaseService.AddTableColumn(tInfo.DbLink, table.table, fieldList);
                        }

                        var uField = field.Adapt<EntityFieldModel>();
                        tableData.Find(x => x.table.Equals(table.table))?.fields.Add(uField);
                        isUpdate = true;
                    }
                    if (!table.fields.Any(x => x.field.Equals(deleteUserId)))
                    {
                        var field = new DbTableFieldModel() { field = deleteUserId, fieldName = "删除用户", dataType = "varchar", dataLength = "50", allowNull = 1 };
                        if (!_databaseService.IsAnyColumn(tInfo.DbLink, table.table, deleteUserId))
                        {
                            var fieldList = new List<DbTableFieldModel>() { field };
                            await _databaseService.AddTableColumn(tInfo.DbLink, table.table, fieldList);
                        }

                        var uField = field.Adapt<EntityFieldModel>();
                        tableData.Find(x => x.table.Equals(table.table))?.fields.Add(uField);
                        isUpdate = true;
                    }
                    if (!table.fields.Any(x => x.field.Equals(deleteTime)))
                    {
                        var field = new DbTableFieldModel() { field = deleteTime, fieldName = "删除时间", dataType = "datetime", dataLength = "50", allowNull = 1 };
                        if (!_databaseService.IsAnyColumn(tInfo.DbLink, table.table, deleteTime))
                        {
                            var fieldList = new List<DbTableFieldModel>() { field };
                            await _databaseService.AddTableColumn(tInfo.DbLink, table.table, fieldList);
                        }

                        var uField = field.Adapt<EntityFieldModel>();
                        tableData.Find(x => x.table.Equals(table.table))?.fields.Add(uField);
                        isUpdate = true;
                    }
                }
            }

            if (isUpdate)
            {
                var moduleId = tInfo.visualDevEntity.Id;
                var tables = tableData.ToJsonString();
                await _visualDevRepository.AsUpdateable().SetColumns(x => x.Tables == tables).Where(x => x.Id == moduleId).ExecuteCommandAsync();
                await _visualDevRepository.AsSugarClient().Updateable<VisualDevReleaseEntity>().SetColumns(x => x.Tables == tables).Where(x => x.Id == moduleId).ExecuteCommandAsync();
            }
        }
    }
    #endregion

    #region 私有方法

    /// <summary>
    /// 获取数据表主键.
    /// </summary>
    /// <param name="link"></param>
    /// <param name="MainTableName"></param>
    /// <returns></returns>
    public string GetPrimary(DbLinkEntity link, string MainTableName)
    {
        List<DbTableFieldModel>? tableList = _databaseService.GetFieldList(link, MainTableName); // 获取主表所有列
        DbTableFieldModel? mainPrimary = tableList.Find(t => t.primaryKey && t.field.ToLower() != "f_tenant_id"); // 主表主键
        if (mainPrimary == null || mainPrimary.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D1402); // 主表未设置主键
        return mainPrimary.field;
    }

    /// <summary>
    /// 数据唯一 验证.
    /// </summary>
    /// <param name="link">DbLinkEntity.</param>
    /// <param name="tableList"></param>
    /// <param name="templateInfo">模板信息.</param>
    /// <param name="allDataMap">数据.</param>
    /// <param name="mainPrimary">主键名.</param>
    /// <param name="mainId">主键Id.</param>
    /// <param name="isUpdate">是否修改.</param>
    private async Task UniqueVerify(DbLinkEntity link, List<DbTableFieldModel> tableList, TemplateParsingBase templateInfo, Dictionary<string, object> allDataMap, string mainPrimary, string mainId, bool isUpdate = false)
    {
        // 业务主键 唯一验证
        if (templateInfo.FormModel.useBusinessKey && templateInfo.FormModel.businessKeyList.IsNotEmptyOrNull() && templateInfo.FormModel.businessKeyList.Count > 0)
        {
            var relationKey = new List<string>();
            var auxiliaryFieldList = templateInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
            auxiliaryFieldList.ForEach(tName =>
            {
                string? tableField = templateInfo.AllTable.Find(tf => tf.table == tName)?.tableField;
                relationKey.Add(templateInfo.MainTableName + "." + mainPrimary + "=" + tName + "." + tableField);
            });

            var dbType = link?.DbType != null ? link.DbType : _visualDevRepository.AsSugarClient().CurrentConnectionConfig.DbType.ToString();
            var fieldList = new List<string>();
            var whereList = new List<IConditionalModel>();
            foreach (var key in templateInfo.FormModel.businessKeyList)
            {
                var model = templateInfo.SingleFormData.Find(x => x.__vModel__ == key);
                var type = allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__].IsNotEmptyOrNull() && !allDataMap[model.__vModel__].ToString().Equals("[]") ? ConditionalType.Equal : model.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) || model.__config__.jnpfKey.Equals(JnpfKeyConst.DATE) ? ConditionalType.EqualNull : ConditionalType.IsNullOrEmpty;

                var cSharpTypeName = string.Empty;
                string? value = null;
                if (allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__].IsNotEmptyOrNull())
                {
                    var newValue = _formDataParsing.InsertValueHandle(dbType, tableList, model.__vModel__, allDataMap[model.__vModel__], templateInfo.FieldsModelList);
                    if (newValue is DateTime) cSharpTypeName = "datetime";
                    value = newValue.ToString();
                }

                if (model.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT)) cSharpTypeName = "decimal";

                fieldList.Add(string.Format("{0}.{1}", model.__config__.tableName, model.__vModel__.Split("_jnpf_").Last()));
                whereList.Add(new ConditionalCollections()
                {
                    ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                    {
                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                        {
                            FieldName = string.Format("{0}.{1}", model.__config__.tableName, model.__vModel__.Split("_jnpf_").Last()),
                            ConditionalType = type,
                            FieldValue = value,
                            CSharpTypeName = cSharpTypeName
                        })
                    }
                });
            }

            _sqlSugarClient = _databaseService.ChangeDataBase(templateInfo.DbLink);
            var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(whereList).ToSqlString();
            _sqlSugarClient.AsTenant().ChangeDatabase("default");
            if (!itemWhere.Equals("@"))
            {
                relationKey.Add("(" + itemWhere.Split("WHERE").Last() + ")");
                var querStr = string.Format(
                    "select {0} from {1} where ({2}) ",
                    string.Join(",", fieldList),
                    auxiliaryFieldList.Any() ? templateInfo.MainTableName + "," + string.Join(",", auxiliaryFieldList) : templateInfo.MainTableName,
                    string.Join(" and ", relationKey)); // 多表， 联合查询
                if (templateInfo.FormModel.logicalDelete) querStr = string.Format(" {0} and {1}.{2} ", querStr, templateInfo.MainTableName, "f_delete_mark is null");

                if (allDataMap.ContainsKey("flowId") && allDataMap["flowId"].IsNotEmptyOrNull())
                {
                    var tempId = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(it => it.DeleteMark == null && it.Id.Equals(allDataMap["flowId"])).Select(it => it.TemplateId).FirstAsync();
                    var versionIds = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(it => it.DeleteMark == null && it.TemplateId == tempId).Select(it => it.Id).ToListAsync();
                    querStr = string.Format(" {0} and {1}.f_flow_id in ('{2}') ", querStr, templateInfo.MainTableName, string.Join("','", versionIds));

                    if (isUpdate) querStr = string.Format("{0} and {1} <> '{2}'", querStr, templateInfo.MainTableName + ".f_flow_task_id", mainId);
                }
                else
                {
                    querStr = string.Format(" {0} and {1}.f_flow_id is null", querStr, templateInfo.MainTableName);

                    if (isUpdate) querStr = string.Format("{0} and {1} <> '{2}'", querStr, templateInfo.MainTableName + "." + mainPrimary, mainId);
                }

                var res = _databaseService.GetSqlData(link, querStr).ToObject<List<Dictionary<string, string>>>();
                if (res.Count > 0) throw Oops.Oh(ErrorCode.COM1018, templateInfo.FormModel.businessKeyTip);
            }
        }

        // 单行输入 唯一验证
        if (templateInfo.AllFieldsModel.Any(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique))
        {
            var relationKey = new List<string>();
            var auxiliaryFieldList = templateInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
            auxiliaryFieldList.ForEach(tName =>
            {
                string? tableField = templateInfo.AllTable.Find(tf => tf.table == tName)?.tableField;
                relationKey.Add(templateInfo.MainTableName + "." + mainPrimary + "=" + tName + "." + tableField);
            });

            var fieldList = new List<string>();
            var whereList = new List<IConditionalModel>();
            templateInfo.SingleFormData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique).ToList().ForEach(item =>
            {
                if (allDataMap.ContainsKey(item.__vModel__) && allDataMap[item.__vModel__].IsNotEmptyOrNull())
                {
                    allDataMap[item.__vModel__] = allDataMap[item.__vModel__].ToString().Trim();
                    fieldList.Add(string.Format("{0}.{1}", item.__config__.tableName, item.__vModel__.Split("_jnpf_").Last()));
                    whereList.Add(new ConditionalCollections()
                    {
                        ConditionalList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>()
                        {
                            new KeyValuePair<WhereType, ConditionalModel>(WhereType.Or, new ConditionalModel
                            {
                                FieldName = string.Format("{0}.{1}", item.__config__.tableName, item.__vModel__.Split("_jnpf_").Last()),
                                ConditionalType =allDataMap.ContainsKey(item.__vModel__) ? ConditionalType.Equal: ConditionalType.IsNullOrEmpty,
                                FieldValue = allDataMap.ContainsKey(item.__vModel__) ? allDataMap[item.__vModel__].ToString() : string.Empty,
                            })
                        }
                    });
                }
            });

            _sqlSugarClient = _databaseService.ChangeDataBase(templateInfo.DbLink);
            var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(whereList).ToSqlString();
            _sqlSugarClient.AsTenant().ChangeDatabase("default");
            if (!itemWhere.Equals("@"))
            {
                relationKey.Add("(" + itemWhere.Split("WHERE").Last() + ")");
                var querStr = string.Format(
                    "select {0} from {1} where ({2}) ",
                    string.Join(",", fieldList),
                    auxiliaryFieldList.Any() ? templateInfo.MainTableName + "," + string.Join(",", auxiliaryFieldList) : templateInfo.MainTableName,
                    string.Join(" and ", relationKey)); // 多表， 联合查询
                if (templateInfo.FormModel.logicalDelete) querStr = string.Format(" {0} and {1}.{2} ", querStr, templateInfo.MainTableName, "f_delete_mark is null");

                if (allDataMap.ContainsKey("flowId") && allDataMap["flowId"].IsNotEmptyOrNull())
                {
                    var tempId = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(it => it.DeleteMark == null && it.Id == allDataMap["flowId"].ToString()).Select(it => it.TemplateId).FirstAsync();
                    var versionIds = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(it => it.DeleteMark == null && it.TemplateId == tempId).Select(it => it.Id).ToListAsync();
                    querStr = string.Format(" {0} and {1}.f_flow_id in ('{2}') ", querStr, templateInfo.MainTableName, string.Join("','", versionIds));

                    if (isUpdate) querStr = string.Format("{0} and {1} <> '{2}'", querStr, templateInfo.MainTableName + ".f_flow_task_id", mainId);
                }
                else
                {
                    querStr = string.Format(" {0} and {1}.f_flow_id is null", querStr, templateInfo.MainTableName);

                    if (isUpdate) querStr = string.Format("{0} and {1} <> '{2}'", querStr, templateInfo.MainTableName + "." + mainPrimary, mainId);
                }

                var res = _databaseService.GetSqlData(link, querStr).ToObject<List<Dictionary<string, string>>>();

                if (res.Any())
                {
                    var errorList = new List<string>();

                    res.ForEach(items =>
                    {
                        foreach (var item in items)
                        {
                            // 主副表有相同唯一值字段
                            if (item.Key.Last().ToString().Equals("1") && items.ContainsKey(item.Key.Substring(0, item.Key.Length - 1)))
                            {
                                var key = item.Key.Substring(0, item.Key.Length - 1);
                                errorList.Add(templateInfo.SingleFormData.LastOrDefault(x => x.__vModel__.Equals(key) || x.__vModel__.Contains("_jnpf_" + key))?.__config__.label);
                            }
                            else
                            {
                                errorList.Add(templateInfo.SingleFormData.FirstOrDefault(x => x.__vModel__.Equals(item.Key) || x.__vModel__.Contains("_jnpf_" + item.Key))?.__config__.label);
                            }
                        }
                    });

                    throw Oops.Oh(ErrorCode.D1407, string.Join(",", errorList.Distinct()));
                }
            }

            foreach (var citem in templateInfo.ChildTableFieldsModelList)
            {
                if (allDataMap.ContainsKey(citem.__vModel__))
                {
                    var childrenValues = allDataMap[citem.__vModel__].ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
                    if (childrenValues.Any())
                    {
                        citem.__config__.children.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique).ToList().ForEach(item =>
                        {
                            var vList = childrenValues.Where(xx => xx.ContainsKey(item.__vModel__)).ToList();
                            vList.ForEach(vitem =>
                            {
                                if (vitem[item.__vModel__] != null)
                                {
                                    vitem[item.__vModel__] = vitem[item.__vModel__].ToString().Trim();
                                    if (childrenValues.Where(x => x.ContainsKey(item.__vModel__) && x.ContainsValue(vitem[item.__vModel__])).Count() > 1)
                                        throw Oops.Oh(ErrorCode.D1407, item.__config__.label);
                                }
                            });
                        });
                    }
                    allDataMap[citem.__vModel__] = childrenValues;
                }
            }
        }
    }

    /// <summary>
    /// 数据必填 验证.
    /// </summary>
    /// <param name="templateInfo">模板信息.</param>
    /// <param name="allDataMap">数据.</param>
    private void RequiredVerify(TemplateParsingBase templateInfo, Dictionary<string, object> allDataMap)
    {
        var childTableList = templateInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).Select(x => x.__vModel__).ToList();
        var requiredList = templateInfo.AllFieldsModel.Where(x => x.__config__.required).ToList();
        var error = new List<string>();

        // 主副表数据
        foreach (var model in requiredList.Where(x => !x.__vModel__.ToLower().Contains("tablefield")))
        {
            if (templateInfo.visualDevEntity.isShortLink && !LinkAllowKey.Contains(model.__config__.jnpfKey)) continue;

            if (!allDataMap.ContainsKey(model.__vModel__) || (allDataMap.ContainsKey(model.__vModel__) && allDataMap[model.__vModel__].IsNullOrEmpty()))
                error.Add(model.__config__.label + "不能为空");
        }

        // 子表数据
        foreach (var item in allDataMap.Where(x => childTableList.Contains(x.Key)))
        {
            foreach (var childItems in item.Value.ToObject<List<Dictionary<string, object>>>())
            {
                foreach (var cModel in requiredList.Where(x => x.__vModel__.Contains(item.Key)))
                {
                    if (templateInfo.visualDevEntity.isShortLink && !LinkAllowKey.Contains(cModel.__config__.jnpfKey)) continue;

                    var cKey = cModel.__vModel__.Split("-").Last();
                    if (!childItems.ContainsKey(cKey) || (childItems.ContainsKey(cKey) && childItems[cKey].IsNullOrEmpty()))
                        error.Add(cModel.__config__.label + "不能为空");
                }
            }
        }

        if (error.Any()) throw Oops.Oh(ErrorCode.COM1018, string.Join(",", error));
    }

    /// <summary>
    /// 组装列表查询sql.
    /// </summary>
    /// <param name="primaryKey">主键.</param>
    /// <param name="templateInfo">模板.</param>
    /// <param name="input">查询输入.</param>
    /// <param name="tableFieldKeyValue">联表查询 表字段名称 对应 前端字段名称 (应对oracle 查询字段长度不能超过30个).</param>
    /// <returns></returns>
    private string GetListQuerySql(string primaryKey, TemplateParsingBase templateInfo, ref VisualDevModelListQueryInput input, ref Dictionary<string, string> tableFieldKeyValue)
    {
        List<string> fields = new List<string>();

        string? sql = string.Empty; // 查询sql

        // 显示列和搜索列有子表字段
        if (templateInfo.ChildTableFields.Count > 0 && (templateInfo.ColumnData.columnList.Any(x => templateInfo.ChildTableFields.ContainsKey(x.prop)) || templateInfo.ColumnData.searchList.Any(xx => templateInfo.ChildTableFields.ContainsKey(xx.prop))))
        {
            foreach (var item in templateInfo.AllTableFields)
            {
                if (input.dataRuleJson.IsNotEmptyOrNull() && input.dataRuleJson.Contains(string.Format("\"{0}\"", item.Key)))
                    input.dataRuleJson = input.dataRuleJson.Replace(string.Format("\"{0}\"", item.Key), string.Format("\"{0}\"", item.Value));

                if (input.superQueryJson.IsNotEmptyOrNull() && input.superQueryJson.Contains(string.Format("\"{0}\"", item.Key)))
                    input.superQueryJson = input.superQueryJson.Replace(string.Format("\"{0}\"", item.Key), string.Format("\"{0}\"", item.Value));

                if (input.queryJson.IsNotEmptyOrNull() && input.queryJson.Contains(string.Format("\"{0}\"", item.Key)))
                    input.queryJson = input.queryJson.Replace(string.Format("\"{0}\"", item.Key), string.Format("\"{0}\"", item.Value));

                if (input.extraQueryJson.IsNotEmptyOrNull() && input.extraQueryJson.Contains(string.Format("\"{0}\"", item.Key)))
                    input.extraQueryJson = input.extraQueryJson.Replace(string.Format("\"{0}\"", item.Key), string.Format("\"{0}\"", item.Value));

                if (input.dataPermissionJson.IsNotEmptyOrNull() && input.dataPermissionJson.Contains(string.Format("\"{0}\"", item.Key)))
                    input.dataPermissionJson = input.dataPermissionJson.Replace(string.Format("\"{0}\"", item.Key), string.Format("\"{0}\"", item.Value));
            }

            input.queryJson = input.queryJson.Replace(string.Format("\"{0}\"", "f_flow_id"), string.Format("\"{0}\"", templateInfo.MainTableName + ".f_flow_id"));
            input.queryJson = input.queryJson.Replace(string.Format("\"{0}\"", "f_inte_assistant"), string.Format("\"{0}\"", templateInfo.MainTableName + ".f_inte_assistant"));
            input.queryJson = input.queryJson.Replace(string.Format("\"{0}\"", "f_flow_state"), string.Format("\"{0}\"", templateInfo.MainTableName + ".f_flow_state"));

            var dataRuleQuerDic = new List<IConditionalModel>();
            if (input.dataRuleJson.IsNotEmptyOrNull()) dataRuleQuerDic = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(input.dataRuleJson);
            var superQuerDic = new List<IConditionalModel>();
            if (input.superQueryJson.IsNotEmptyOrNull()) superQuerDic = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(input.superQueryJson);
            var querDic = new List<IConditionalModel>();
            if (input.queryJson.IsNotEmptyOrNull()) querDic = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(input.queryJson);
            var extraQuerDic = new List<IConditionalModel>();
            if (input.extraQueryJson.IsNotEmptyOrNull()) extraQuerDic = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(input.extraQueryJson);
            var dataPermissionsQuerDic = new List<IConditionalModel>();
            if (input.dataPermissionJson.IsNotEmptyOrNull()) dataPermissionsQuerDic = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(input.dataPermissionJson);

            var ctFields = templateInfo.ChildTableFields;
            templateInfo.ChildTableFields = new Dictionary<string, string>();
            var strSql = GetListQuerySql(primaryKey, templateInfo, ref input, ref tableFieldKeyValue);
            input.dataRuleJson = string.Empty;
            input.queryJson = string.Empty;
            input.superQueryJson = string.Empty;
            input.extraQueryJson = string.Empty;
            input.dataPermissionJson = string.Empty;
            templateInfo.ChildTableFields = ctFields;

            var sqlStr = "select {0} from {1} ";

            // 查询
            var querySqlList = new List<string>();
            if (querDic != null && querDic.Any())
            {
                GetQuerySqlList(templateInfo, primaryKey, sqlStr, querDic, querySqlList);
            }

            // 页签查询
            var extraQuerySqlList = new List<string>();
            if (extraQuerDic != null && extraQuerDic.Any())
            {
                GetQuerySqlList(templateInfo, primaryKey, sqlStr, extraQuerDic, extraQuerySqlList);
                extraQuerySqlList[0] = string.Format(" and {0}", extraQuerySqlList[0]);
            }

            // 高级查询
            var superQuerySqlCondition = string.Empty;
            if (superQuerDic != null && superQuerDic.Any())
            {
                superQuerySqlCondition = AssembleQueryCondition(templateInfo, primaryKey, sqlStr, superQuerDic, tableFieldKeyValue);
            }

            // 数据过滤
            var dataRuleSqlCondition = string.Empty;
            if (dataRuleQuerDic != null && dataRuleQuerDic.Any())
            {
                dataRuleSqlCondition = AssembleQueryCondition(templateInfo, primaryKey, sqlStr, dataRuleQuerDic, tableFieldKeyValue);
            }

            // 拼接数据权限
            var dataPermissionsSqlCondition = string.Empty;
            if (dataPermissionsQuerDic != null && dataPermissionsQuerDic.Any())
            {
                var allCondition = (ConditionalTree)dataPermissionsQuerDic.FirstOrDefault();
                foreach (var roleCondition in allCondition.ConditionalList)
                {
                    // 拼接多个权限组sql条件
                    if (dataPermissionsSqlCondition.IsNotEmptyOrNull())
                        dataPermissionsSqlCondition = string.Format("(" + dataPermissionsSqlCondition + ")" + roleCondition.Key);

                    var roleConditionSql = string.Empty;
                    if (roleCondition.Value.GetType().Name.Equals("ConditionalModel"))
                    {
                        var where = new List<IConditionalModel> { new ConditionalTree() { ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>> { roleCondition } } };

                        _sqlSugarClient = _databaseService.ChangeDataBase(templateInfo.DbLink);
                        var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(where).ToSqlString();
                        _sqlSugarClient.AsTenant().ChangeDatabase("default");

                        roleConditionSql = itemWhere.Split("WHERE").Last();
                    }
                    else
                    {
                        var condition = (ConditionalTree)roleCondition.Value;
                        var resultList = new List<object>() { new { ConditionalList = condition.ConditionalList } };
                        var conModels = new List<IConditionalModel>();
                        conModels.AddRange(_visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(resultList.ToJsonString()));
                        roleConditionSql = AssembleQueryCondition(templateInfo, primaryKey, sqlStr, conModels, tableFieldKeyValue, true);
                    }

                    // 拼接多个权限sql
                    dataPermissionsSqlCondition += "(" + roleConditionSql + ")";
                    roleConditionSql = string.Empty;
                }

                dataPermissionsSqlCondition = string.Format("and ({0})", dataPermissionsSqlCondition);
            }

            if (!querySqlList.Any()) querySqlList.Add(string.Format(" ( {0} in ({1}) ) ", primaryKey, string.Format(sqlStr, primaryKey, templateInfo.MainTableName)));

            sql = string.Format("select * from ({0}) mt where {1} {2} {3} {4} {5}", strSql, string.Join(" and ", querySqlList), string.Join(" and ", extraQuerySqlList), superQuerySqlCondition, dataRuleSqlCondition, dataPermissionsSqlCondition);
        }
        else if (!templateInfo.AuxiliaryTableFieldsModelList.Any())
        {
            fields.Add(primaryKey); // 主键
            fields.Add("f_flow_id");
            fields.Add("f_flow_task_id");
            fields.Add("f_flow_state");
            tableFieldKeyValue.Add(primaryKey.ToUpper(), primaryKey);
            tableFieldKeyValue.Add("f_flow_id".ToUpper(), "f_flow_id");
            tableFieldKeyValue.Add("f_flow_task_id".ToUpper(), "f_flow_task_id");
            tableFieldKeyValue.Add("f_flow_state".ToUpper(), "f_flow_state");

            for (int i = 0; i < templateInfo.MainTableFieldsModelList.Count; i++)
            {
                var vmodel = templateInfo.MainTableFieldsModelList[i].__vModel__.ReplaceRegex(@"(\w+)_jnpf_", string.Empty); // Field
                if (vmodel.IsNotEmptyOrNull())
                {
                    fields.Add(templateInfo.MainTableFieldsModelList[i].__config__.tableName + "." + vmodel + " FIELD_" + i); // TableName.Field_0
                    tableFieldKeyValue.Add("FIELD_" + i, templateInfo.MainTableFieldsModelList[i].__vModel__);

                    // 查询字段替换
                    if (input.queryJson.IsNotEmptyOrNull())
                        input.queryJson = input.queryJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.MainTableFieldsModelList[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));
                    if (input.superQueryJson.IsNotEmptyOrNull())
                        input.superQueryJson = input.superQueryJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.MainTableFieldsModelList[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));
                    if (input.dataRuleJson.IsNotEmptyOrNull())
                        input.dataRuleJson = input.dataRuleJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.MainTableFieldsModelList[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));
                    if (input.extraQueryJson.IsNotEmptyOrNull())
                        input.extraQueryJson = input.extraQueryJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.MainTableFieldsModelList[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));
                    if (input.dataPermissionJson.IsNotEmptyOrNull())
                        input.dataPermissionJson = input.dataPermissionJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.MainTableFieldsModelList[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));

                    templateInfo.ColumnData.searchList?.Where(x => x.__vModel__ == templateInfo.MainTableFieldsModelList[i].__vModel__).ToList().ForEach(item =>
                    {
                        item.__vModel__ = item.__vModel__.Replace(templateInfo.MainTableFieldsModelList[i].__vModel__, "FIELD_" + i);
                    });

                    // 排序字段替换
                    if (input.sidx.IsNotEmptyOrNull()) input.sidx = input.sidx.Replace(templateInfo.MainTableFieldsModelList[i].__vModel__, "FIELD_" + i);
                }
            }

            sql = string.Format("select {0} from {1}", string.Join(",", fields), templateInfo.MainTableName);
            if (templateInfo.FormModel.logicalDelete) sql += " where f_delete_mark is null "; // 处理软删除

            // 多租户字段隔离
            if (_tenant.MultiTenancy)
            {
                var tenantCache = _cacheManager.Get<List<GlobalTenantCacheModel>>(CommonConst.GLOBALTENANT).Find(it => it.TenantId.Equals(templateInfo.DbLink.Id));
                if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1) && _databaseService.IsAnyColumn(templateInfo.DbLink, templateInfo.MainTableName, "f_tenant_id"))
                    sql += string.Format(" {0} f_tenant_id='{1}' ", sql.Contains("where") ? "and" : "where", tenantCache.connectionConfig.IsolationField);
            }
            else
            {
                sql += string.Format(" {0} f_tenant_id='0' ", sql.Contains("where") ? "and" : "where");
            }
        }
        else
        {
            #region 所有主、副表 字段名 和 处理查询、排序字段

            // 所有主、副表 字段名
            fields.Add(templateInfo.MainTableName + "." + primaryKey);
            fields.Add(templateInfo.MainTableName + ".f_flow_id");
            fields.Add(templateInfo.MainTableName + ".f_flow_task_id");
            fields.Add(templateInfo.MainTableName + ".f_flow_state");
            tableFieldKeyValue.Add(primaryKey.ToUpper(), primaryKey);
            tableFieldKeyValue.Add("f_flow_id".ToUpper(), "f_flow_id");
            tableFieldKeyValue.Add("f_flow_task_id".ToUpper(), "f_flow_task_id");
            tableFieldKeyValue.Add("f_flow_state".ToUpper(), "f_flow_state");

            for (int i = 0; i < templateInfo.SingleFormData.Count; i++)
            {
                string? vmodel = templateInfo.SingleFormData[i].__vModel__.ReplaceRegex(@"(\w+)_jnpf_", string.Empty); // Field
                if (vmodel.IsNotEmptyOrNull())
                {
                    fields.Add(templateInfo.SingleFormData[i].__config__.tableName + "." + vmodel + " FIELD_" + i); // TableName.Field_0
                    tableFieldKeyValue.Add("FIELD_" + i, templateInfo.SingleFormData[i].__vModel__);

                    // 查询字段替换
                    if (input.queryJson.IsNotEmptyOrNull())
                        input.queryJson = input.queryJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.SingleFormData[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));
                    if (input.superQueryJson.IsNotEmptyOrNull())
                        input.superQueryJson = input.superQueryJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.SingleFormData[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));
                    if (input.dataRuleJson.IsNotEmptyOrNull())
                        input.dataRuleJson = input.dataRuleJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.SingleFormData[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));
                    if (input.extraQueryJson.IsNotEmptyOrNull())
                        input.extraQueryJson = input.extraQueryJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.SingleFormData[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));
                    if (input.dataPermissionJson.IsNotEmptyOrNull())
                        input.dataPermissionJson = input.dataPermissionJson.Replace(string.Format("\"FieldName\":\"{0}\"", templateInfo.SingleFormData[i].__vModel__), string.Format("\"FieldName\":\"{0}\"", "FIELD_" + i));

                    templateInfo.ColumnData.searchList.Where(x => x.__vModel__ == templateInfo.SingleFormData[i].__vModel__).ToList().ForEach(item =>
                    {
                        item.id = item.id.Replace(templateInfo.SingleFormData[i].__vModel__, "FIELD_" + i);
                        item.__vModel__ = item.__vModel__.Replace(templateInfo.SingleFormData[i].__vModel__, "FIELD_" + i);
                    });

                    // 排序字段替换
                    if (input.sidx.IsNotEmptyOrNull()) input.sidx = input.sidx.Replace(templateInfo.SingleFormData[i].__vModel__, "FIELD_" + i);
                }
            }

            #endregion

            #region 关联字段

            List<string>? relationKey = new List<string>();
            List<string>? auxiliaryFieldList = templateInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
            foreach (var tName in auxiliaryFieldList)
            {
                var tableField = templateInfo.AllTable.Find(tf => tf.table == tName);
                var relationSql = string.Format("(({0}.{1}={2}.{3}) or ({0}.{1} is null and {2}.{3} is null))", templateInfo.MainTableName, tableField.relationField, tName, tableField.tableField);
                relationKey.Add(relationSql);

                if (templateInfo.FormModel.logicalDelete) relationKey.Add(tName + ".f_delete_mark is null ");
            }

            if (templateInfo.FormModel.logicalDelete)
                relationKey.Add(templateInfo.MainTableName + ".f_delete_mark is null "); // 处理软删除

            // 多租户字段隔离
            if (_tenant.MultiTenancy)
            {
                var tenantCache = _cacheManager.Get<List<GlobalTenantCacheModel>>(CommonConst.GLOBALTENANT).Find(it => it.TenantId.Equals(templateInfo.DbLink.Id));
                if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1) && _databaseService.IsAnyColumn(templateInfo.DbLink, templateInfo.MainTableName, "f_tenant_id"))
                    relationKey.Add(string.Format(" {0}.f_tenant_id='{1}' ", templateInfo.MainTableName, tenantCache.connectionConfig.IsolationField));
            }
            else
            {
                relationKey.Add(string.Format(" {0}.f_tenant_id='0' ", templateInfo.MainTableName));
            }

            string? whereStr = string.Join(" and ", relationKey);

            #endregion

            sql = string.Format("select {0} from {1} where {2}", string.Join(",", fields), templateInfo.MainTableName + "," + string.Join(",", auxiliaryFieldList), whereStr); // 多表， 联合查询
        }

        return sql;
    }
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
                            newItem.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(WhereType.Null, value.First());
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
            else if (cList[i] is ConditionalModel)
            {
                var newItem = (ConditionalModel)cList[i];
                if (!newItem.FieldName.Contains(tableName)) cList.RemoveAt(i);
            }
        }

        return cList;
    }

    /// <summary>
    /// 组装单条信息查询sql.
    /// </summary>
    /// <param name="id">id.</param>
    /// <param name="mainPrimary">主键.</param>
    /// <param name="templateInfo">模板.</param>
    /// <param name="tableFieldKeyValue">联表查询 表字段名称 对应 前端字段名称 (应对oracle 查询字段长度不能超过30个).</param>
    /// <param name="isFlow">是否为流程.</param>
    /// <param name="propsValue"></param>
    /// <param name="formAuthList"></param>
    /// <returns></returns>
    private string GetInfoQuerySql(string id, string mainPrimary, TemplateParsingBase templateInfo, ref Dictionary<string, string> tableFieldKeyValue, bool isFlow = false, string? propsValue = null, List<string>? formAuthList = null)
    {
        if (propsValue.IsNotEmptyOrNull()) propsValue = propsValue.Replace("_jnpfId", string.Empty);
        List<string> fields = new List<string>();
        string? sql = string.Empty; // 查询sql

        // 没有副表,只查询主表
        if (!templateInfo.AuxiliaryTableFieldsModelList.Any())
        {
            fields.Add(mainPrimary); // 主表主键
            fields.Add("f_flow_id");
            fields.Add("f_flow_task_id");
            templateInfo.MainTableFieldsModelList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item =>
            {
                if (formAuthList == null || formAuthList.Contains(item.__vModel__))
                    fields.Add(item.__vModel__);
            }); // 主表列名

            if (isFlow)
                sql = string.Format("select {0} from {1} where f_flow_task_id='{2}'", string.Join(",", fields), templateInfo.MainTableName, id);
            else
                sql = string.Format("select {0} from {1} where {2}='{3}'", string.Join(",", fields), templateInfo.MainTableName, propsValue.IsNotEmptyOrNull() ? propsValue : mainPrimary, id);
        }
        else
        {
            #region 所有主表、副表 字段名
            fields.Add(templateInfo.MainTableName + "." + mainPrimary); // 主表主键
            fields.Add(templateInfo.MainTableName + ".f_flow_id");
            fields.Add(templateInfo.MainTableName + ".f_flow_task_id");
            for (int i = 0; i < templateInfo.SingleFormData.Count; i++)
            {
                if (formAuthList != null && !formAuthList.Contains(templateInfo.SingleFormData[i].__vModel__))
                    continue;

                string? vmodel = templateInfo.SingleFormData[i].__vModel__.ReplaceRegex(@"(\w+)_jnpf_", ""); // Field
                if (vmodel.IsNotEmptyOrNull())
                {
                    fields.Add(templateInfo.SingleFormData[i].__config__.tableName + "." + vmodel + " FIELD" + i); // TableName.Field_0
                    tableFieldKeyValue.Add("FIELD" + i, templateInfo.SingleFormData[i].__vModel__);
                }
            }
            #endregion

            #region 所有副表 关联字段
            List<string>? ctNameList = templateInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
            List<string>? relationKey = new List<string>();
            if (isFlow)
            {
                relationKey.Add(string.Format("{0}.f_flow_task_id='{1}'", templateInfo.MainTableName, id));
            }
            else if (propsValue.IsNotEmptyOrNull())
            {
                var field = propsValue.Contains("_jnpf_") ? ctNameList[0] + "." + propsValue.ReplaceRegex(@"(\w+)_jnpf_", string.Empty) : templateInfo.MainTableName + "." + propsValue;
                relationKey.Add(string.Format("{0}='{1}'", field, id));
            }
            else
            {
                relationKey.Add(string.Format("{0}.{1}='{2}'", templateInfo.MainTableName, mainPrimary, id));
            }

            foreach (var tName in ctNameList)
            {
                var table = templateInfo.AllTable.Find(tf => tf.table == tName);
                var relationSql = string.Format("(({0}.{1}={2}.{3}) or ({0}.{1} is null and {2}.{3} is null))", templateInfo.MainTableName, table.relationField, tName, table.tableField);
                relationKey.Add(relationSql);
            }

            string? whereStr = string.Join(" and ", relationKey);
            #endregion

            sql = string.Format("select {0} from {1} where {2}", string.Join(",", fields), templateInfo.MainTableName + "," + string.Join(",", ctNameList), whereStr); // 多表， 联合查询
        }

        return sql;
    }

    /// <summary>
    /// 组装 查询 json.
    /// </summary>
    /// <param name="queryJson"></param>
    /// <param name="columnDesign"></param>
    /// <param name="isQuery"></param>
    /// <param name="flowId"></param>
    /// <param name="isInteAssisData">是否为集成助手数据.</param>
    /// <returns></returns>
    private List<IConditionalModel> GetQueryJson(string queryJson, ColumnDesignModel columnDesign, int isQuery, string flowId = "")
    {
        // 将查询的关键字json转成Dictionary
        Dictionary<string, object> keywordJsonDic = string.IsNullOrEmpty(queryJson) ? null : queryJson.ToObject<Dictionary<string, object>>();
        var conModels = new List<IConditionalModel>();
        if (keywordJsonDic != null)
        {
            foreach (KeyValuePair<string, object> item in keywordJsonDic)
            {
                if (item.Value.IsNotEmptyOrNull())
                {
                    if (item.Key.Equals(JnpfKeyConst.JNPFKEYWORD) && columnDesign.searchList.Any(it => it.isKeyword))
                    {
                        var con = new ConditionalCollections() { ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>() };
                        foreach (var model in columnDesign.searchList.FindAll(it => it.isKeyword))
                        {
                            var conditional = new KeyValuePair<WhereType, ConditionalModel>(WhereType.Or, new ConditionalModel
                            {
                                FieldName = model.id,
                                ConditionalType = ConditionalType.Like,
                                FieldValue = item.Value.ToString()
                            });
                            con.ConditionalList.Add(conditional);
                        }

                        conModels.Add(con);
                    }
                    else if (item.Key.Equals("jnpfFlowState"))
                    {
                        conModels.Add(new ConditionalCollections()
                        {
                            ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                            {
                                new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                {
                                    FieldName = "f_flow_state",
                                    ConditionalType = ConditionalType.Equal,
                                    FieldValue = item.Value.ToString(),
                                    CSharpTypeName = "decimal"
                                })
                            }
                        });
                    }
                    else
                    {
                        var model = columnDesign.searchList.Find(it => it.id.Equals(item.Key));
                        if (model.IsNullOrEmpty())
                            model = columnDesign.searchList.Find(it => it.__vModel__.Equals(item.Key));
                        if (model.IsNotEmptyOrNull())
                        {
                            switch (model.__config__.jnpfKey)
                            {
                                case JnpfKeyConst.DATE:
                                case JnpfKeyConst.CREATETIME:
                                case JnpfKeyConst.MODIFYTIME:
                                case JnpfKeyConst.DATECALCULATE:
                                    {
                                        if (item.Value.ToString().Contains('['))
                                        {
                                            var timeRange = item.Value.ToObject<List<string>>();
                                            var startTime = timeRange.First().TimeStampToDateTime();
                                            var endTime = timeRange.Last().TimeStampToDateTime();

                                            var conditional = new ConditionalCollections() { ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>() };
                                            if (!startTime.Equals("0".TimeStampToDateTime()))
                                            {
                                                conditional.ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                {
                                                    FieldName = item.Key,
                                                    ConditionalType = ConditionalType.GreaterThanOrEqual,
                                                    FieldValue = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, startTime.Second, 0).ToString(),
                                                    CSharpTypeName = "datetime",
                                                    FieldValueConvertFunc = it => Convert.ToDateTime(it)
                                                }));
                                            }

                                            if (!endTime.Equals("0".TimeStampToDateTime()))
                                            {
                                                conditional.ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                {
                                                    FieldName = item.Key,
                                                    ConditionalType = ConditionalType.LessThanOrEqual,
                                                    FieldValue = new DateTime(endTime.Year, endTime.Month, endTime.Day, endTime.Hour, endTime.Minute, endTime.Second, 999).ToString(),
                                                    CSharpTypeName = "datetime",
                                                    FieldValueConvertFunc = it => Convert.ToDateTime(it)
                                                }));
                                            }

                                            conModels.Add(conditional);
                                        }
                                        else
                                        {
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Equal,
                                                        FieldValue = item.Value.ToString().TimeStampToDateTime().ToString(),
                                                        CSharpTypeName = "datetime",
                                                        FieldValueConvertFunc = it => Convert.ToDateTime(it)
                                                    })
                                                }
                                            });
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.TIME:
                                    {
                                        if (item.Value.ToString().Contains('['))
                                        {
                                            var timeRange = item.Value.ToObject<List<string>>();
                                            var startTime = string.Format("{0:" + model.format + "}", Convert.ToDateTime(timeRange.First()));
                                            var endTime = string.Format("{0:" + model.format + "}", Convert.ToDateTime(timeRange.Last()));

                                            var conditional = new ConditionalCollections() { ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>() };
                                            if (!startTime.Equals("00:00:00"))
                                            {
                                                conditional.ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                {
                                                    FieldName = item.Key,
                                                    ConditionalType = ConditionalType.GreaterThanOrEqual,
                                                    FieldValue = startTime
                                                }));
                                            }

                                            if (!endTime.Equals("00:00:00"))
                                            {
                                                conditional.ConditionalList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                {
                                                    FieldName = item.Key,
                                                    ConditionalType = ConditionalType.LessThanOrEqual,
                                                    FieldValue = endTime
                                                }));
                                            }

                                            conModels.Add(conditional);
                                        }
                                        else
                                        {
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Equal,
                                                        FieldValue = item.Value.ToString()
                                                    })
                                                }
                                            });
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.NUMINPUT:
                                case JnpfKeyConst.CALCULATE:
                                case JnpfKeyConst.RATE:
                                case JnpfKeyConst.SLIDER:
                                    {
                                        if (item.Value.ToString().Contains('['))
                                        {
                                            var numArray = item.Value.ToObject<List<string>>();
                                            var startNum = numArray.First() == null ? 0 : numArray.First().ParseToDecimal();
                                            var endNum = numArray.Last() == null ? decimal.MaxValue : numArray.Last().ParseToDecimal();
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        CSharpTypeName = "decimal",
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.GreaterThanOrEqual,
                                                        FieldValue = startNum.ToString()
                                                    }),
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        CSharpTypeName = "decimal",
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.LessThanOrEqual,
                                                        FieldValue = endNum.ToString()
                                                    })
                                                }
                                            });
                                        }
                                        else
                                        {
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        CSharpTypeName = "decimal",
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Equal,
                                                        FieldValue = item.Value.ToString()
                                                    })
                                                }
                                            });
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.CHECKBOX:
                                    {
                                        if (isQuery.Equals(1))
                                        {
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Like,
                                                        FieldValue = item.Value.ToJsonStringOld()
                                                    })
                                                }
                                            });
                                        }
                                        else
                                        {
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Equal,
                                                        FieldValue = item.Value.ToJsonStringOld()
                                                    })
                                                }
                                            });
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.ROLESELECT:
                                case JnpfKeyConst.GROUPSELECT:
                                case JnpfKeyConst.POSSELECT:
                                case JnpfKeyConst.USERSELECT:
                                case JnpfKeyConst.COMSELECT:
                                    {
                                        // 多选时为模糊查询
                                        if (model.multiple || model.searchMultiple)
                                        {
                                            var value = item.Value.ToString().Contains("[") ? item.Value.ToObject<List<object>>() : new List<object>() { item.Value.ToString() };
                                            var addItems = new List<KeyValuePair<WhereType, ConditionalModel>>();
                                            for (int i = 0; i < value.Count; i++)
                                            {
                                                var add = new KeyValuePair<WhereType, ConditionalModel>(i == 0 ? WhereType.And : WhereType.Or, new ConditionalModel
                                                {
                                                    FieldName = item.Key,
                                                    ConditionalType = model.multiple ? ConditionalType.Like : ConditionalType.Equal,
                                                    FieldValue = model.multiple ? value[i].ToJsonString() : value[i].ToString()
                                                });
                                                addItems.Add(add);
                                            }

                                            conModels.Add(new ConditionalCollections() { ConditionalList = addItems });
                                        }
                                        else
                                        {
                                            var value = item.Value.ToString().Contains("[") ? item.Value.ToJsonStringOld() : item.Value.ToString();
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Equal,
                                                        FieldValue = value
                                                    })
                                                }
                                            });
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.CURRORGANIZE:
                                case JnpfKeyConst.CURRPOSITION:
                                    {
                                        var value = item.Value.ToString().Contains("[") ? item.Value.ToObject<List<object>>() : new List<object>() { item.Value.ToString() };
                                        var addItems = new List<KeyValuePair<WhereType, ConditionalModel>>();
                                        for (int i = 0; i < value.Count; i++)
                                        {
                                            var add = new KeyValuePair<WhereType, ConditionalModel>(i == 0 ? WhereType.And : WhereType.Or, new ConditionalModel
                                            {
                                                FieldName = item.Key,
                                                ConditionalType = ConditionalType.Like,
                                                FieldValue = value[i].ToString()
                                            });
                                            addItems.Add(add);
                                        }

                                        conModels.Add(new ConditionalCollections() { ConditionalList = addItems });
                                    }
                                    break;
                                case JnpfKeyConst.USERSSELECT:
                                    {
                                        if (model.multiple || model.searchMultiple)
                                        {
                                            var objIdList = new List<string>();
                                            if (item.Value.ToString().Contains("[")) objIdList = item.Value.ToObject<List<string>>();
                                            else objIdList.Add(item.Value.ToString());
                                            var rIdList = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => objIdList.Select(xx => xx.Replace("--user", string.Empty)).Contains(x.UserId)).Select(x => new { x.ObjectId, x.ObjectType }).ToList();
                                            rIdList.ForEach(x =>
                                            {
                                                if (x.ObjectType.Equals(CommonConst.RelationObjectOrganize))
                                                {
                                                    objIdList.Add(x.ObjectId + "--company");
                                                    objIdList.Add(x.ObjectId + "--department");
                                                }
                                                else
                                                {
                                                    objIdList.Add(x.ObjectId + "--" + x.ObjectType.ToLower());
                                                }
                                            });

                                            var whereList = new List<KeyValuePair<WhereType, ConditionalModel>>();
                                            for (var i = 0; i < objIdList.Count(); i++)
                                            {
                                                if (i == 0)
                                                {
                                                    whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Like,
                                                        FieldValue = objIdList[i]
                                                    }));
                                                }
                                                else
                                                {
                                                    whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.Or, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Like,
                                                        FieldValue = objIdList[i]
                                                    }));
                                                }
                                            }

                                            conModels.Add(new ConditionalCollections() { ConditionalList = whereList });
                                        }
                                        else
                                        {
                                            var itemValue = item.Value.ToString().Contains("[") ? item.Value.ToJsonStringOld() : item.Value.ToString();
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                    {
                                                      new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                      {
                                                          FieldName = item.Key,
                                                          ConditionalType = ConditionalType.Equal,
                                                          FieldValue = itemValue
                                                      })
                                                    }
                                            });
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.TREESELECT:
                                    {
                                        if (item.Value.IsNotEmptyOrNull() && item.Value.ToString().Contains("["))
                                        {
                                            var value = item.Value.ToObject<List<string>>();

                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Like,
                                                        FieldValue = value.LastOrDefault()
                                                    })
                                                }
                                            });
                                        }
                                        else
                                        {
                                            // 多选时为模糊查询
                                            if (model.multiple)
                                            {
                                                conModels.Add(new ConditionalCollections()
                                                {
                                                    ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                    {
                                                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                        {
                                                            FieldName = item.Key,
                                                            ConditionalType = ConditionalType.Like,
                                                            FieldValue = item.Value.ToString()
                                                        })
                                                    }
                                                });
                                            }
                                            else
                                            {
                                                conModels.Add(new ConditionalCollections()
                                                {
                                                    ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                    {
                                                        new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                        {
                                                            FieldName = item.Key,
                                                            ConditionalType = ConditionalType.Equal,
                                                            FieldValue = item.Value.ToString()
                                                        })
                                                    }
                                                });
                                            }
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.ADDRESS:
                                    {
                                        // 多选时为模糊查询
                                        if (model.multiple || model.searchMultiple)
                                        {
                                            var value = item.Value?.ToString().ToObject<List<object>>();
                                            if (value.Any())
                                            {
                                                var addItems = new List<KeyValuePair<WhereType, ConditionalModel>>();
                                                for (int i = 0; i < value.Count; i++)
                                                {
                                                    var add = new KeyValuePair<WhereType, ConditionalModel>(i == 0 ? WhereType.And : WhereType.Or, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Like,
                                                        FieldValue = value[i].ToJsonStringOld().Contains('[') ? value[i].ToJsonStringOld().Replace("[", string.Empty) : item.Value?.ToString().Replace("[", string.Empty).Replace("\r\n", string.Empty).Replace(" ", string.Empty),
                                                    });
                                                    addItems.Add(add);
                                                }
                                                conModels.Add(new ConditionalCollections() { ConditionalList = addItems });
                                            }
                                        }
                                        else
                                        {
                                            var itemValue = item.Value.ToString().Contains('[') ? item.Value.ToJsonStringOld() : item.Value.ToString();
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Equal,
                                                        FieldValue = itemValue
                                                    })
                                                }
                                            });
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.SELECT:
                                    {
                                        // 多选时为模糊查询
                                        if (model.multiple || model.searchMultiple)
                                        {
                                            var value = item.Value.ToString().Contains("[") ? item.Value.ToObject<List<object>>() : new List<object>() { item.Value.ToString() };
                                            var addItems = new List<KeyValuePair<WhereType, ConditionalModel>>();
                                            for (int i = 0; i < value.Count; i++)
                                            {
                                                var add = new KeyValuePair<WhereType, ConditionalModel>(i == 0 ? WhereType.And : WhereType.Or, new ConditionalModel
                                                {
                                                    FieldName = item.Key,
                                                    ConditionalType = model.multiple ? ConditionalType.Like : ConditionalType.Equal,
                                                    FieldValue = model.multiple ? value[i].ToJsonString() : value[i].ToString()
                                                });
                                                addItems.Add(add);
                                            }

                                            conModels.Add(new ConditionalCollections() { ConditionalList = addItems });
                                        }
                                        else
                                        {
                                            var itemValue = item.Value.ToString().Contains("[") ? item.Value.ToJsonStringOld() : item.Value.ToString();
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Equal,
                                                        FieldValue = itemValue
                                                    })
                                                }
                                            });
                                        }
                                    }

                                    break;
                                default:
                                    {
                                        var itemValue = item.Value.ToString().Contains("[") ? item.Value.ToJsonStringOld() : item.Value.ToString();
                                        if (model.searchType == 1)
                                        {
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Equal,
                                                        FieldValue = itemValue
                                                    })
                                                }
                                            });
                                        }
                                        else
                                        {
                                            conModels.Add(new ConditionalCollections()
                                            {
                                                ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                                                {
                                                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.Key,
                                                        ConditionalType = ConditionalType.Like,
                                                        FieldValue = itemValue
                                                    })
                                                }
                                            });
                                        }
                                    }

                                    break;
                            }
                        }
                    }
                }
                else if (isQuery.Equals(0))
                {
                    conModels.Add(new ConditionalCollections()
                    {
                        ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                        {
                            new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                            {
                                FieldName = item.Key,
                                ConditionalType = ConditionalType.EqualNull,
                                FieldValue = null
                            })
                        }
                    });
                }
            }
        }

        if (isQuery.Equals(1))
        {
            if (flowId.IsNotEmptyOrNull())
            {
                conModels.Add(new ConditionalCollections()
                {
                    ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                {
                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                    {
                        FieldName = "f_flow_id",
                        ConditionalType = ConditionalType.In,
                        FieldValue = flowId
                    })
                }
                });
            }
            else
            {
                conModels.Add(new ConditionalCollections()
                {
                    ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                {
                    new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
                    {
                        FieldName = "f_flow_id",
                        ConditionalType = ConditionalType.EqualNull
                    })
                }
                });
            }
        }

        return conModels;
    }

    /// <summary>
    /// 组装 查询 sql.
    /// </summary>
    private void GetQuerySqlList(TemplateParsingBase templateInfo, string primaryKey, string sqlStr, List<IConditionalModel> querDic, List<string> querySqlList)
    {
        foreach (ConditionalTree quer in querDic)
        {
            if (quer.ConditionalList.Count > 1)
            {
                var conditionalSql = string.Empty;
                foreach (var condition in quer.ConditionalList)
                {
                    var fieldName = ((ConditionalModel)condition.Value).FieldName.Split(".").FirstOrDefault();
                    var idField = templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField;
                    var itemSql = string.Format(sqlStr, idField.IsNullOrEmpty() ? primaryKey : idField, fieldName);

                    var where = new List<IConditionalModel> { new ConditionalTree() { ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>> { condition } } };
                    _sqlSugarClient = _databaseService.ChangeDataBase(templateInfo.DbLink);
                    var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(where).ToSqlString();
                    _sqlSugarClient.AsTenant().ChangeDatabase("default");
                    if (itemWhere.Contains("WHERE"))
                    {
                        if (!fieldName.Equals("jnpfFlowState"))
                            itemSql = string.Format("({0} in ({1}WHERE{2}))", primaryKey, itemSql, itemWhere.Split("WHERE").LastOrDefault());
                        else
                            itemSql = itemWhere.Split("WHERE").LastOrDefault();
                        if (conditionalSql.IsNotEmptyOrNull())
                            conditionalSql = conditionalSql + condition.Key.ToString() + itemSql;
                        else
                            conditionalSql = itemSql;
                    }
                }

                if (conditionalSql.IsNotEmptyOrNull()) querySqlList.Add("(" + conditionalSql + ")");
            }
            else
            {
                var field = quer.ConditionalList.First();
                var fieldName = ((ConditionalModel)field.Value).FieldName.Split(".").FirstOrDefault();
                var idField = templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField;
                var itemSql = string.Format(sqlStr, idField.IsNullOrEmpty() ? primaryKey : idField, fieldName);

                var where = new List<IConditionalModel> { quer };
                _sqlSugarClient = _databaseService.ChangeDataBase(templateInfo.DbLink);
                var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(where).ToSqlString();
                _sqlSugarClient.AsTenant().ChangeDatabase("default");
                if (itemWhere.Contains("WHERE"))
                {
                    itemSql = string.Format("({0} in ({1}where))", primaryKey, itemSql);
                    if (querySqlList.Any(it => it.Contains(itemSql.TrimEnd(')'))))
                    {
                        var oldSql = querySqlList.Find(it => it.Contains(itemSql.TrimEnd(')')));
                        querySqlList.Remove(oldSql);
                        var newSql = string.Format("{0}where{1}and{2}", oldSql.Split("where").FirstOrDefault(), itemWhere.Split("WHERE").LastOrDefault(), oldSql.Split("where").LastOrDefault());
                        querySqlList.Add(newSql);
                    }
                    else
                    {
                        if (!fieldName.Equals("jnpfFlowState"))
                            itemSql = string.Format("{0}where{1}{2}", itemSql.Split("where").FirstOrDefault(), itemWhere.Split("WHERE").LastOrDefault(), itemSql.Split("where").LastOrDefault());
                        else
                            itemSql = itemWhere.Split("WHERE").LastOrDefault();
                        querySqlList.Add(itemSql);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 显示列有子表字段,根据主键查询所有子表.
    /// </summary>
    /// <param name="templateInfo"></param>
    /// <param name="primaryKey"></param>
    /// <param name="querList"></param>
    /// <param name="dataRuleList"></param>
    /// <param name="superQuerList"></param>
    /// <param name="result"></param>
    /// <param name="dataPermissions"></param>
    /// <param name="isConvertData">是否转换数据0-转换、1-不转换（有用于定位来区分 列表、详情）.</param>
    /// <returns></returns>
    private async Task<PageResult<Dictionary<string, object>>> GetListChildTable(
        TemplateParsingBase templateInfo,
        string primaryKey,
        List<IConditionalModel> querList,
        List<IConditionalModel> dataRuleList,
        List<IConditionalModel> superQuerList,
        PageResult<Dictionary<string, object>> result,
        List<IConditionalModel> dataPermissions,
        int? isConvertData = null)
    {
        var childTableList = new Dictionary<string, List<string>>();

        templateInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).ToList().ForEach(ctitem =>
        {
            templateInfo.AllFieldsModel.Where(x => x.__vModel__.Contains(ctitem.__vModel__ + "-")).ToList().ForEach(item =>
            {
                var value = item.__vModel__.Split("-").Last();

                if (!templateInfo.ColumnData.columnList.Any(it => it.id == item.__vModel__))
                    value = string.Empty;

                if (value.IsNotEmptyOrNull())
                {
                    if (childTableList.ContainsKey(ctitem.__config__.tableName)) childTableList[ctitem.__config__.tableName].Add(value);
                    else childTableList.Add(ctitem.__config__.tableName, new List<string>() { value });
                }
            });
        });

        var relationDic = new Dictionary<string, string>();
        templateInfo.ChildTableFieldsModelList.ForEach(item =>
        {
            var table = templateInfo.AllTable.Find(tf => tf.table == item.__config__.tableName);
            if (!relationDic.ContainsKey(item.__config__.tableName)) relationDic.Add(item.__config__.tableName, table.tableField + "||" + table.relationField);
        });

        var dataRuleJson = dataRuleList.ToJsonStringOld();
        foreach (var item in templateInfo.AllTableFields)
        {
            if (dataRuleJson.IsNotEmptyOrNull() && dataRuleJson.Contains(string.Format("\"{0}\"", item.Key)))
                dataRuleJson = dataRuleJson.Replace(string.Format("\"{0}\"", item.Key), string.Format("\"{0}\"", item.Value));
        }
        var superQuerJson = superQuerList.ToJsonStringOld();
        foreach (var item in templateInfo.AllTableFields)
        {
            if (superQuerJson.IsNotEmptyOrNull() && superQuerJson.Contains(string.Format("\"{0}\"", item.Key)))
                superQuerJson = superQuerJson.Replace(string.Format("\"{0}\"", item.Key), string.Format("\"{0}\"", item.Value));
        }

        // 捞取 所有子表查询条件 <tableName , where>
        var childTableQuery = new Dictionary<string, List<IConditionalModel>>();
        var dataRule = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(dataRuleJson);
        var superQuer = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(superQuerJson);
        var query = querList.ToObject<List<ConditionalCollections>>();
        foreach (var item in templateInfo.ChildTableFields)
        {
            var tableName = item.Value.Split(".").FirstOrDefault();
            var dataRuleConList = GetIConditionalModelListByTableName(dataRuleList, tableName);
            if (dataRuleConList.Any())
            {
                //foreach (var it in dataRuleConList) it.ConditionalList.ForEach(x => x.Value.FieldName = item.Value);
                if (!childTableQuery.ContainsKey(tableName)) childTableQuery.Add(tableName, new List<IConditionalModel>());
                childTableQuery[tableName].AddRange(dataRuleConList);
            }
            var conList = query.Where(x => x.ConditionalList.Any(xx => xx.Value.FieldName.Equals(item.Key))).ToList();
            if (conList.Any())
            {
                foreach (var it in conList)
                {
                    it.ConditionalList.ForEach(x =>
                    {
                        if (templateInfo.ChildTableFields.ContainsKey(x.Value.FieldName))
                        {
                            x.Value.FieldName = templateInfo.ChildTableFields[x.Value.FieldName];
                        }
                    });
                }

                if (!childTableQuery.ContainsKey(tableName)) childTableQuery.Add(tableName, new List<IConditionalModel>());
                childTableQuery[tableName].AddRange(conList);
            }
        }

        foreach (var childTable in childTableList)
        {
            var tableField = relationDic[childTable.Key].Split("||")[0];
            var relationField = relationDic[childTable.Key].Split("||")[1];
            var isNullId = false;
            var ids = new List<object>();
            foreach (var item in result.list)
            {
                if (item[relationField].IsNotEmptyOrNull())
                    ids.Add(item[relationField]);
                else
                    isNullId = true;
            }

            var ctPrimaryKey = templateInfo.AllTable.Find(x => x.table.Equals(childTable.Key))?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
            if (ctPrimaryKey.IsNullOrEmpty()) ctPrimaryKey = GetPrimary(templateInfo.DbLink, childTable.Key);
            childTable.Value.Add(ctPrimaryKey);
            if (!childTable.Value.Contains(tableField)) childTable.Value.Add(tableField);

            // 关联条件
            var conditionSql = string.Empty;
            if (isNullId)
            {
                if (ids.Count > 0)
                    conditionSql = string.Format("({0} in ('{1}') or {0} is null)", tableField, string.Join("','", ids));
                else
                    conditionSql = string.Format("{0} is null", tableField);
            }
            else
            {
                conditionSql = string.Format("{0} in ('{1}')", tableField, string.Join("','", ids));
            }

            var sql = string.Format("select {0} from {1} where {2}", string.Join(",", childTable.Value), childTable.Key, conditionSql);
            if (templateInfo.FormModel.logicalDelete) sql += " and f_delete_mark is null";
            if (childTableQuery.ContainsKey(childTable.Key)) // 子表查询条件
            {
                var itemWhere = _visualDevRepository.AsSugarClient().SqlQueryable<dynamic>("@").Where(childTableQuery[childTable.Key]).ToSqlString();
                if (itemWhere.Contains("WHERE")) sql = string.Format(" {0} and {1} ", sql, itemWhere.Split("WHERE").Last());
            }

            // 拼接高级查询
            var superQueryConList = new List<IConditionalModel>();
            if (superQuerList != null && superQuerList.Any())
            {
                var sList = new List<object>();
                var allPersissions = superQuer.ToJsonStringOld().ToObject<List<object>>();
                allPersissions.ForEach(it =>
                {
                    if (it.ToJsonString().Contains(childTable.Key + ".")) sList.Add(it);
                });
                if (sList.Any())
                {
                    superQueryConList = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(sList.ToJsonString());
                    superQueryConList = GetIConditionalModelListByTableName(superQueryConList, childTable.Key);
                    var json = superQueryConList.ToJsonStringOld().Replace(childTable.Key + ".", string.Empty);
                    superQueryConList = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(json);
                }
            }

            // 拼接数据权限
            var dataPermissionsList = new List<IConditionalModel>();
            if (dataPermissions != null && dataPermissions.Any())
            {
                var pList = new List<object>();
                var allPersissions = dataPermissions.ToJsonStringOld().ToObject<List<object>>();
                allPersissions.ForEach(it =>
                {
                    if (it.ToJsonString().Contains(childTable.Key + ".")) pList.Add(it);
                });
                if (pList.Any())
                {
                    dataPermissionsList = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(pList.ToJsonString());
                    dataPermissionsList = GetIConditionalModelListByTableName(dataPermissionsList, childTable.Key);
                    var json = dataPermissionsList.ToJsonString().Replace(childTable.Key + ".", string.Empty);
                    dataPermissionsList = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(json);
                }
            }

            // 数据过滤
            var dataRuleConditionalList = new List<IConditionalModel>();
            if (dataRule != null && dataRule.Any())
            {
                var pList = new List<object>();
                var allPersissions = dataRule.ToJsonStringOld().ToObject<List<object>>();
                allPersissions.ForEach(it =>
                {
                    if (it.ToJsonString().Contains(childTable.Key + ".")) pList.Add(it);
                });
                if (pList.Any())
                {
                    dataRuleConditionalList = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(pList.ToJsonString());
                    dataRuleConditionalList = GetIConditionalModelListByTableName(dataRuleConditionalList, childTable.Key);
                    var json = dataRuleConditionalList.ToJsonStringOld().Replace(childTable.Key + ".", string.Empty);
                    dataRuleConditionalList = _visualDevRepository.AsSugarClient().Utilities.JsonToConditionalModels(json);
                }
            }

            _sqlSugarClient = _databaseService.ChangeDataBase(templateInfo.DbLink);
            sql = _sqlSugarClient.SqlQueryable<object>(sql).Where(superQueryConList).Where(dataPermissionsList).Where(dataRuleConditionalList).ToSqlString();
            _sqlSugarClient.AsTenant().ChangeDatabase("default");

            var dt = _databaseService.GetSqlData(templateInfo.DbLink, sql).ToObject<List<Dictionary<string, string>>>().ToObject<List<Dictionary<string, object>>>();
            var vModel = templateInfo.AllFieldsModel.Find(x => x.__config__.tableName == childTable.Key)?.__vModel__;

            if (vModel.IsNotEmptyOrNull())
            {
                foreach (var it in result.list)
                {
                    var rows = it[relationField].IsNotEmptyOrNull() ? dt.Where(x => x[tableField].IsNotEmptyOrNull() && x[tableField].Equals(it[relationField])).ToList() : dt.Where(x => x[tableField].IsNullOrEmpty()).ToList();
                    foreach (var row in rows) row["JnpfKeyConst_MainData"] = it.ToJsonString();
                    var childTableModel = templateInfo.ChildTableFieldsModelList.First(x => x.__vModel__.Equals(vModel));

                    var datas = new List<Dictionary<string, object>>();
                    if (childTableModel.__config__.children.Any(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()))
                        datas = await _formDataParsing.GetKeyData(templateInfo.visualDevEntity.Id, childTableModel.__config__.children.Where(x => x.__config__.templateJson != null && x.__config__.templateJson.Any()).ToList(), rows, templateInfo.ColumnData, "List", templateInfo.WebType, primaryKey, templateInfo.visualDevEntity.isShortLink, isConvertData);
                    datas = await _formDataParsing.GetKeyData(templateInfo.visualDevEntity.Id, childTableModel.__config__.children.Where(x => x.__config__.templateJson == null || !x.__config__.templateJson.Any()).ToList(), rows, templateInfo.ColumnData, "List", templateInfo.WebType, primaryKey, templateInfo.visualDevEntity.isShortLink, isConvertData);

                    foreach (var data in datas) { data.Remove("JnpfKeyConst_MainData"); }
                    it.Add(vModel, datas);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取处理子表数据.
    /// </summary>
    /// <param name="templateInfo">模板信息.</param>
    /// <param name="link">数据库连接.</param>
    /// <param name="dataMap">全部数据.</param>
    /// <param name="newDataMap">新数据.</param>
    /// <param name="isDetail">是否详情转换.</param>
    /// <param name="formAuthList">表单权限.</param>
    /// <returns></returns>
    private Dictionary<string, object> GetChildTableData(TemplateParsingBase templateInfo, DbLinkEntity? link, Dictionary<string, object> dataMap, Dictionary<string, object> newDataMap, bool isDetail = false, List<string>? formAuthList = null)
    {
        foreach (var model in templateInfo.ChildTableFieldsModelList)
        {
            if (model.__vModel__.IsNotEmptyOrNull() && model.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
            {
                if (formAuthList != null && !formAuthList.Contains(model.__vModel__)) continue;

                List<string> feilds = new List<string>();
                var ct = templateInfo.AllTable.Find(x => x.table.Equals(model.__config__.tableName));
                var ctPrimaryKey = templateInfo.AllTable.Find(x => x.table.Equals(ct.table))?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
                if (ctPrimaryKey.IsNullOrEmpty()) ctPrimaryKey = GetPrimary(link, ct.table);
                feilds.Add(ctPrimaryKey + " id "); // 子表主键

                // 拼接查询字段
                foreach (FieldsModel? childModel in model.__config__.children)
                {
                    if (!string.IsNullOrEmpty(childModel.__vModel__))
                    {
                        if (formAuthList != null)
                        {
                            var formField = string.Format("{0}-{1}", model.__vModel__, childModel.__vModel__);
                            if (!formAuthList.Contains(formField)) continue;
                        }

                        feilds.Add(childModel.__vModel__);
                    }
                }

                string relationMainFeildValue = string.Empty;
                string childSql = string.Format("select {0} from {1} where 1=1 ", string.Join(",", feilds), model.__config__.tableName); // 查询子表数据
                foreach (TableModel? tableMap in templateInfo.AllTable.Where(x => !x.table.Equals(templateInfo.MainTableName)).ToList())
                {
                    if (tableMap.table.Equals(model.__config__.tableName))
                    {
                        // 外键
                        if (dataMap.ContainsKey(tableMap.relationField) && dataMap[tableMap.relationField].IsNotEmptyOrNull())
                            childSql += string.Format(" and {0}='{1}'", tableMap.tableField, dataMap[tableMap.relationField]);
                        else
                            childSql += string.Format(" and {0} is null", tableMap.tableField, dataMap[tableMap.relationField]);

                        // 逻辑删除
                        if (templateInfo.FormModel.logicalDelete)
                            childSql += " and f_delete_mark is null";

                        var childTableData = _databaseService.GetSqlData(link, childSql).ToJsonString().ToObject<List<Dictionary<string, object>>>();
                        if (!isDetail) childTableData = _formDataParsing.GetTableDataInfo(childTableData, model.__config__.children, "detail");

                        if (childTableData.Count > 0) newDataMap[model.__vModel__] = childTableData;
                        else newDataMap[model.__vModel__] = new List<Dictionary<string, object>>();
                    }
                }
            }
        }

        return newDataMap;
    }

    /// <summary>
    /// 处理并发锁定(乐观锁).
    /// </summary>
    /// <param name="link">数据库连接.</param>
    /// <param name="templateInfo">模板信息.</param>
    /// <param name="updateSqlList">修改Sql集合(提交修改时接入).</param>
    /// <param name="allDataMap">前端提交的数据(提交修改时接入).</param>
    /// <param name="mainPrimary">主表主键.</param>
    private async Task OptimisticLocking(DbLinkEntity? link, TemplateParsingBase templateInfo, List<string>? updateSqlList = null, Dictionary<string, object>? allDataMap = null, string? mainPrimary = null)
    {
        if (templateInfo.FormModel.concurrencyLock)
        {
            try
            {
                // 主表修改语句, 如果有修改语句 获取执行结果.
                // 不是修改模式, 增加并发锁定字段 f_version.
                if (updateSqlList != null && updateSqlList.Any())
                {
                    var versoin = (allDataMap.ContainsKey("f_version") && allDataMap["f_version"] != null) ? allDataMap["f_version"] : "-1";

                    // 并发乐观锁 字段 拼接条件
                    var versoinSql = string.Format("select * from {0} where {1}='{2}' and f_version={3};", templateInfo.MainTableName, mainPrimary, allDataMap["id"], versoin);
                    var res = _databaseService.GetSqlData(link, versoinSql).ToObject<List<Dictionary<string, object>>>();
                    if (res.Count.Equals(0) && !allDataMap.ContainsKey("jnpf_resurgence")) throw Oops.Oh(ErrorCode.D1408); // 该条数据已经被修改过

                    // f_version +1
                    string? sql = string.Format("update {0} set f_version={1} where {2}='{3}';", templateInfo.MainTableName, versoin.ParseToInt() + 1, mainPrimary, allDataMap["id"]);
                    await _databaseService.ExecuteSql(link, sql);
                }
                else
                {
                    var newVModel = new FieldsModel() { __vModel__ = "f_version", __config__ = new ConfigModel() { jnpfKey = JnpfKeyConst.COMINPUT, relationTable = templateInfo.MainTableName, tableName = templateInfo.MainTableName } };
                    templateInfo.SingleFormData.Add(newVModel);
                    templateInfo.MainTableFieldsModelList.Add(newVModel);
                    templateInfo.FieldsModelList.Add(newVModel);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("[D1408]")) throw Oops.Oh(ErrorCode.D1408);
                else throw Oops.Oh(ErrorCode.COM1008);
            }
        }
    }

    /// <summary>
    /// 数据是否可以传递.
    /// </summary>
    /// <param name="oldModel">原控件模型.</param>
    /// <param name="newModel">新控件模型.</param>
    /// <returns>true 可以传递, false 不可以</returns>
    private bool DataTransferVerify(FieldsModel oldModel, FieldsModel newModel)
    {
        switch (oldModel.__config__.jnpfKey)
        {
            case JnpfKeyConst.COMINPUT:
            case JnpfKeyConst.TEXTAREA:
            case JnpfKeyConst.RADIO:
            case JnpfKeyConst.EDITOR:
                if (!(newModel.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) ||
                    newModel.__config__.jnpfKey.Equals(JnpfKeyConst.TEXTAREA) ||
                    newModel.__config__.jnpfKey.Equals(JnpfKeyConst.RADIO) ||
                    (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && !newModel.multiple) ||
                    newModel.__config__.jnpfKey.Equals(JnpfKeyConst.EDITOR)))
                    return false;
                break;
            case JnpfKeyConst.CHECKBOX:
                if (!((newModel.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && newModel.multiple) ||
                    (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && newModel.multiple) ||
                    (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) && newModel.multiple) ||
                    newModel.__config__.jnpfKey.Equals(JnpfKeyConst.CHECKBOX) ||
                    newModel.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER)))
                    return false;
                break;
            case JnpfKeyConst.NUMINPUT:
            case JnpfKeyConst.DATE:
            case JnpfKeyConst.TIME:
            case JnpfKeyConst.UPLOADFZ:
            case JnpfKeyConst.UPLOADIMG:
            case JnpfKeyConst.COLORPICKER:
            case JnpfKeyConst.RATE:
            case JnpfKeyConst.SLIDER:
                if (!(oldModel.__config__.jnpfKey.Equals(newModel.__config__.jnpfKey)))
                    return false;
                break;
            case JnpfKeyConst.COMSELECT:
            case JnpfKeyConst.POSSELECT:
            case JnpfKeyConst.USERSELECT:
            case JnpfKeyConst.ROLESELECT:
            case JnpfKeyConst.GROUPSELECT:
            case JnpfKeyConst.ADDRESS:
                if (!(oldModel.__config__.jnpfKey.Equals(newModel.__config__.jnpfKey) && oldModel.multiple.Equals(newModel.multiple)))
                    return false;
                break;
            case JnpfKeyConst.TREESELECT:
                if (oldModel.multiple)
                {
                    if (!((newModel.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && newModel.multiple) ||
                        (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && newModel.multiple) ||
                        (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) && newModel.multiple) ||
                        newModel.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER)))
                        return false;
                }
                else
                {
                    if (!(newModel.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) ||
                        newModel.__config__.jnpfKey.Equals(JnpfKeyConst.TEXTAREA) ||
                        newModel.__config__.jnpfKey.Equals(JnpfKeyConst.RADIO) ||
                        (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && !newModel.multiple) ||
                        (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) && !newModel.multiple) ||
                        newModel.__config__.jnpfKey.Equals(JnpfKeyConst.EDITOR)))
                        return false;
                }

                break;
            case JnpfKeyConst.POPUPTABLESELECT:
                if (oldModel.multiple)
                {
                    if (!((newModel.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && newModel.multiple) ||
                        (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.SELECT) && newModel.multiple) ||
                        (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.TREESELECT) && newModel.multiple) ||
                        newModel.__config__.jnpfKey.Equals(JnpfKeyConst.CASCADER)))
                        return false;
                }
                else
                {
                    if (!((newModel.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && !newModel.multiple) ||
                        (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM)) ||
                        (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT))))
                        return false;
                }

                break;
            case JnpfKeyConst.POPUPSELECT:
            case JnpfKeyConst.RELATIONFORM:
                if (!((newModel.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM)) ||
                    (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT)) ||
                    (newModel.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT) && !newModel.multiple)))
                    return false;
                break;
        }

        return true;
    }

    /// <summary>
    /// 处理数据视图.
    /// </summary>
    /// <param name="templateInfo"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<PageResult<Dictionary<string, object>>> GetDataViewResults(TemplateParsingBase templateInfo, VisualDevModelListQueryInput input)
    {
        var realList = new PageResult<Dictionary<string, object>>() { list = new List<Dictionary<string, object>>() }; // 返回结果集

        var searchList = new List<IndexSearchFieldModel>();
        var hasPage = false;
        var viewKey = string.Empty;
        //if (_userManager.UserOrigin.Equals("pc"))
        if (true)
        {
            searchList = templateInfo.ColumnData.searchList;
            hasPage = templateInfo.ColumnData.hasPage;
            viewKey = templateInfo.ColumnData.viewKey;
        }
        else
        {
            searchList = templateInfo.AppColumnData.searchList;
            hasPage = templateInfo.AppColumnData.hasPage;
            viewKey = templateInfo.AppColumnData.viewKey;
        }

        // 处理参数
        var par = input.Adapt<DataInterfacePreviewInput>();
        par.tenantId = _userManager.TenantId;
        par.paramList = templateInfo.visualDevEntity.InterfaceParam.ToObject<List<DataInterfaceParameter>>();
        if (par.queryJson.IsNotEmptyOrNull())
        {
            var newList = new Dictionary<string, object>();
            foreach (var item in par.queryJson.ToObject<Dictionary<string, object>>())
            {
                if (par.paramList.Any(it => it.field.Equals(item.Key)))
                    par.paramList.Find(it => it.field.Equals(item.Key)).defaultValue = item.Value;
                else
                    newList.Add(item.Key, item.Value);
            }

            input.queryJson = newList.ToJsonString();
        }

        if (input.extraQueryJson.IsNotEmptyOrNull())
        {
            var newList = new Dictionary<string, object>();
            foreach (var item in input.extraQueryJson.ToObject<Dictionary<string, object>>())
            {
                if (par.paramList.Any(it => it.field.Equals(item.Key)))
                    par.paramList.Find(it => it.field.Equals(item.Key)).defaultValue = item.Value;
                else
                    newList.Add(item.Key, item.Value);
            }

            input.extraQueryJson = newList.ToJsonString();
        }

        // 数据
        var dataInterface = await _visualDevRepository.AsSugarClient().Queryable<DataInterfaceEntity>().FirstAsync(x => x.Id == templateInfo.visualDevEntity.InterfaceId && x.DeleteMark == null);
        var res = await _dataInterfaceService.GetResponseByType(templateInfo.visualDevEntity.InterfaceId, 2, par);
        if (dataInterface.HasPage.Equals(1))
        {
            if (!res.ToJsonString().Equals("[]") && res.ToJsonString() != string.Empty)
                realList = res.ToJsonStringOld().ToObjectOld<PageResult<Dictionary<string, object>>>();
        }
        else
        {
            if (res.ToJsonString().Contains('['))
                realList.list = res.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
            else
                realList.list.Add(res.ToJsonStringOld().ToObjectOld<Dictionary<string, object>>());

            // 页签查询
            if (input.extraQueryJson.IsNotEmptyOrNull())
            {
                foreach (var item in input.extraQueryJson.ToObject<Dictionary<string, object>>())
                {
                    var extraSearchList = new List<IndexSearchFieldModel>
                    {
                        new IndexSearchFieldModel()
                        {
                            __vModel__ = item.Key,
                            searchType = 1,
                        }
                    };
                    realList.list = await GetDataViewQuery(realList.list, extraSearchList, item);
                }
            }

            // 查询
            if (input.queryJson.IsNotEmptyOrNull())
            {
                foreach (var item in input.queryJson.ToObject<Dictionary<string, object>>())
                {
                    realList.list = await GetDataViewQuery(realList.list, searchList, item);
                }
            }

            // 排序
            DataViewSort(realList.list, input.sidx);

            // 假分页
            var total = realList.list.Count;
            if (hasPage)
            {
                var dt = GetPageToDataTable(realList.list, input.currentPage, input.pageSize);
                realList.list = dt.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
            }

            realList.pagination = new PageResult() { currentPage = input.currentPage, pageSize = input.pageSize, total = total };
        }

        // 递归给数据添加id
        AddDataViewId(realList.list, viewKey);

        // 分组表格
        if (templateInfo.ColumnData.type == 3 && _userManager.UserOrigin == "pc" && input.extraQueryJson.IsNullOrEmpty())
            realList.list = CodeGenHelper.GetGroupList(realList.list, templateInfo.ColumnData.groupField, templateInfo.ColumnData.columnList.Find(x => x.__vModel__.ToLower() != templateInfo.ColumnData.groupField.ToLower()).__vModel__);

        return realList;
    }


    public async Task<PageResult<Dictionary<string, object>>> GetDataViewResultOrders(TemplateParsingBase templateInfo, VisualDevModelListQueryInput input)
    {
        var realList = new PageResult<Dictionary<string, object>>() { list = new List<Dictionary<string, object>>() }; // 返回结果集

        var searchList = new List<IndexSearchFieldModel>();
        var hasPage = false;
        var viewKey = string.Empty;

        // 根据用户来源选择配置
        if (true) // 原判断 _userManager.UserOrigin.Equals("pc")
        {
            searchList = templateInfo.ColumnData.searchList;
            hasPage = templateInfo.ColumnData.hasPage;
            viewKey = templateInfo.ColumnData.viewKey;
        }
        else
        {
            searchList = templateInfo.AppColumnData.searchList;
            hasPage = templateInfo.AppColumnData.hasPage;
            viewKey = templateInfo.AppColumnData.viewKey;
        }

        // 处理参数
        var par = input.Adapt<DataInterfacePreviewInput>();
        par.tenantId = _userManager.TenantId;
        par.paramList = templateInfo.visualDevEntity.InterfaceParam.ToObject<List<DataInterfaceParameter>>();

        if (par.queryJson.IsNotEmptyOrNull())
        {
            var newList = new Dictionary<string, object>();
            foreach (var item in par.queryJson.ToObject<Dictionary<string, object>>())
            {
                if (par.paramList.Any(it => it.field.Equals(item.Key)))
                    par.paramList.Find(it => it.field.Equals(item.Key)).defaultValue = item.Value;
                else
                    newList.Add(item.Key, item.Value);
            }

            input.queryJson = newList.ToJsonString();
        }

        if (input.extraQueryJson.IsNotEmptyOrNull())
        {
            var newList = new Dictionary<string, object>();
            foreach (var item in input.extraQueryJson.ToObject<Dictionary<string, object>>())
            {
                if (par.paramList.Any(it => it.field.Equals(item.Key)))
                    par.paramList.Find(it => it.field.Equals(item.Key)).defaultValue = item.Value;
                else
                    newList.Add(item.Key, item.Value);
            }

            input.extraQueryJson = newList.ToJsonString();
        }

        // 获取数据接口
        var dataInterface = await _visualDevRepository.AsSugarClient().Queryable<DataInterfaceEntity>().FirstAsync(x => x.Id == templateInfo.visualDevEntity.InterfaceId && x.DeleteMark == null);
        var res = await _dataInterfaceService.GetResponseByType(templateInfo.visualDevEntity.InterfaceId, 2, par);

        // 临时列表，用于存储处理后的数据（未分页）
        var tempList = new List<Dictionary<string, object>>();

        if (dataInterface.HasPage.Equals(1))
        {
            if (!res.ToJsonString().Equals("[]") && res.ToJsonString() != string.Empty)
            {
                var pageResult = res.ToJsonStringOld().ToObjectOld<PageResult<Dictionary<string, object>>>();
                tempList = pageResult.list;
            }
        }
        else
        {
            if (res.ToJsonString().Contains('['))
                tempList = res.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
            else
                tempList.Add(res.ToJsonStringOld().ToObjectOld<Dictionary<string, object>>());

            // 页签查询
            if (input.extraQueryJson.IsNotEmptyOrNull())
            {
                foreach (var item in input.extraQueryJson.ToObject<Dictionary<string, object>>())
                {
                    var extraSearchList = new List<IndexSearchFieldModel>
                {
                    new IndexSearchFieldModel()
                    {
                        __vModel__ = item.Key,
                        searchType = 1,
                    }
                };
                    tempList = await GetDataViewQuery(tempList, extraSearchList, item);
                }
            }

            // 查询
            if (input.queryJson.IsNotEmptyOrNull())
            {
                foreach (var item in input.queryJson.ToObject<Dictionary<string, object>>())
                {
                    tempList = await GetDataViewQuery(tempList, searchList, item);
                }
            }

            // 排序
            DataViewSort(tempList, input.sidx);
        }

        // 递归给数据添加id
        AddDataViewId(tempList, viewKey);

        // 按 sidx 字段分组（在分页前进行）
        var groupedData = new List<List<Dictionary<string, object>>>();
        if (input.sidx.IsNotEmptyOrNull())
        {
            // 按 sidx 字段分组
            groupedData = tempList
                .GroupBy(x => x.ContainsKey(viewKey) ? x[viewKey] : null)
                .Where(g => g.Key != null)
                .Select(g => g.ToList())
                .ToList();
        }
        else
        {
            // 如果没有 sidx，每个单独的数据作为一个分组
            groupedData = tempList.Select(x => new List<Dictionary<string, object>> { x }).ToList();
        }

        // 分组总数（分组条数）
        var total = groupedData.Count;

        // 假分页（对分组进行分页，返回整个分组的数据）
        if (hasPage)
        {
            var pagedGroups = groupedData
                .Skip((input.currentPage - 1) * input.pageSize)
                .Take(input.pageSize)
                .ToList();

            // 将分页后的分组数据合并为一个列表
            realList.list = pagedGroups.SelectMany(g => g).ToList();
        }
        else
        {
            realList.list = tempList;
        }

        realList.pagination = new PageResult() { currentPage = input.currentPage, pageSize = input.pageSize, total = total };

        // 递归给数据添加id
        //AddDataViewId(realList.list, viewKey);

        // 分组表格
        if (templateInfo.ColumnData.type == 3 && input.extraQueryJson.IsNullOrEmpty()) // && _userManager.UserOrigin == "pc" 
            realList.list = CodeGenHelper.GetGroupList(realList.list, templateInfo.ColumnData.groupField, templateInfo.ColumnData.columnList.Find(x => x.__vModel__.ToLower() != templateInfo.ColumnData.groupField.ToLower()).__vModel__);


        return realList;
    }

    /// <summary>
    /// 数据视图列表排序.
    /// </summary>
    private void DataViewSort(List<Dictionary<string, object>> list, string allSidx)
    {
        if (allSidx.IsNotEmptyOrNull())
        {
            var sidx = allSidx.Split(",").ToList();

            list.Sort((Dictionary<string, object> x, Dictionary<string, object> y) =>
            {
                foreach (var item in sidx)
                {
                    if (item[0].ToString().Equals("-"))
                    {
                        var itemName = item.Remove(0, 1);
                        var xItem = x.ContainsKey(itemName) && x[itemName].IsNotEmptyOrNull() ? x[itemName].ToString() : string.Empty;
                        var yItem = y.ContainsKey(itemName) && y[itemName].IsNotEmptyOrNull() ? y[itemName].ToString() : string.Empty;
                        if (!xItem.Equals(yItem))
                            return yItem.CompareTo(xItem);
                    }
                    else
                    {
                        var xItem = x.ContainsKey(item) && x[item].IsNotEmptyOrNull() ? x[item].ToString() : string.Empty;
                        var yItem = y.ContainsKey(item) && y[item].IsNotEmptyOrNull() ? y[item].ToString() : string.Empty;
                        if (!xItem.Equals(yItem))
                            return xItem.CompareTo(yItem);
                    }
                }

                return 0;
            });
        }
    }

    /// <summary>
    /// 静态数据分页.
    /// </summary>
    /// <param name="dt">数据源.</param>
    /// <param name="PageIndex">第几页.</param>
    /// <param name="PageSize">每页多少条.</param>
    /// <returns></returns>
    private List<Dictionary<string, object>> GetPageToDataTable(List<Dictionary<string, object>> dt, int PageIndex, int PageSize)
    {
        if (PageIndex == 0) return dt; // 0页代表每页数据，直接返回
        if (dt == null) return new List<Dictionary<string, object>>();
        var newdt = new List<Dictionary<string, object>>();
        int rowbegin = (PageIndex - 1) * PageSize;
        int rowend = PageIndex * PageSize; // 要展示的数据条数
        if (rowbegin >= dt.Count) return dt; // 源数据记录数小于等于要显示的记录，直接返回dt
        if (rowend > dt.Count) rowend = dt.Count;
        for (int i = rowbegin; i <= rowend - 1; i++) newdt.Add(dt[i]);
        return newdt;
    }

    /// <summary>
    /// 数据视图列表递归添加id.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="viewKey"></param>
    private void AddDataViewId(List<Dictionary<string, object>> list, string viewKey)
    {
        foreach (var item in list)
        {
            if (!item.ContainsKey("id"))
            {
                var id = viewKey.IsNotEmptyOrNull() ? item[viewKey].IsNotEmptyOrNull() ? item[viewKey]?.ToString() : string.Empty : SnowflakeIdHelper.NextId();
                item.Add("id", id);
            }

            if (item.ContainsKey("children") && item["children"].IsNotEmptyOrNull())
            {
                var fmList = item["children"].ToObject<List<Dictionary<string, object>>>();
                AddDataViewId(fmList, viewKey);
                item["children"] = fmList;
            }
        }
    }

    /// <summary>
    /// 处理数据视图查询.
    /// </summary>
    /// <param name="list">数据.</param>
    /// <param name="searchList">查询列.</param>
    /// <param name="item">查询值</param>
    /// <returns></returns>
    private async Task<List<Dictionary<string, object>>> GetDataViewQuery(List<Dictionary<string, object>> list, List<IndexSearchFieldModel> searchList, KeyValuePair<string, object> item)
    {
        var searchInfo = searchList.Find(x => x.__vModel__.Equals(item.Key));
        if (searchInfo.IsNotEmptyOrNull())
        {
            switch (searchInfo.searchType)
            {
                case 1: // 等于查询
                    var newList = new List<Dictionary<string, object>>();
                    if (searchInfo.searchMultiple)
                    {
                        foreach (var data in item.Value.ToObject<List<object>>())
                        {
                            switch (searchInfo.selectRange)
                            {
                                case 2:
                                    switch (searchInfo.jnpfKey)
                                    {
                                        case JnpfKeyConst.COMSELECT:
                                            {
                                                var orgId = data.ToString();
                                                var orgChildIds = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.ParentId.Equals(orgId)).Select(it => it.Id).ToListAsync();
                                                foreach (var child in orgChildIds)
                                                {
                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                                }
                                            }

                                            break;
                                        case JnpfKeyConst.POSSELECT:
                                            {
                                                var posId = data.ToString();
                                                var posChildIds = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.ParentId.Equals(posId)).Select(it => it.Id).ToListAsync();
                                                foreach (var child in posChildIds)
                                                {
                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                                }
                                            }

                                            break;
                                        case JnpfKeyConst.USERSELECT:
                                            {
                                                var userId = data.ToString();
                                                var userChildIds = new List<string>() { userId };
                                                userChildIds.AddRange(_userManager.GetSubordinates(userId).ToList());
                                                foreach (var child in userChildIds)
                                                {
                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                                }
                                            }

                                            break;
                                    }

                                    break;
                                case 3:
                                    switch (searchInfo.jnpfKey)
                                    {
                                        case JnpfKeyConst.COMSELECT:
                                            var orgId = data.ToString();
                                            var orgChildIds = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.OrganizeIdTree.Contains(orgId)).Select(it => it.Id).ToListAsync();
                                            foreach (var child in orgChildIds)
                                            {
                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                            }

                                            break;
                                        case JnpfKeyConst.POSSELECT:
                                            var posId = data.ToString();
                                            var posChildIds = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.PositionIdTree.Contains(posId)).Select(it => it.Id).ToListAsync();
                                            foreach (var child in posChildIds)
                                            {
                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                            }

                                            break;
                                        case JnpfKeyConst.USERSELECT:
                                            var userId = data.ToString();
                                            var userChildIds = new List<string>() { userId };
                                            userChildIds.AddRange(_userManager.GetSubordinates(userId, true).ToList());
                                            foreach (var child in userChildIds)
                                            {
                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                            }

                                            break;
                                    }

                                    break;
                            }

                            if (item.Value.IsNotEmptyOrNull())
                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(data.ToString()))).ToList());
                            else
                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNullOrEmpty())).ToList());
                        }
                    }
                    else
                    {
                        switch (searchInfo.selectRange)
                        {
                            case 2:
                                switch (searchInfo.jnpfKey)
                                {
                                    case JnpfKeyConst.COMSELECT:
                                        {
                                            var orgId = item.Value.ToString();
                                            var orgChildIds = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.ParentId.Equals(orgId)).Select(it => it.Id).ToListAsync();
                                            foreach (var child in orgChildIds)
                                            {
                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                            }
                                        }

                                        break;
                                    case JnpfKeyConst.POSSELECT:
                                        {
                                            var posId = item.Value.ToString();
                                            var posChildIds = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.ParentId.Equals(posId)).Select(it => it.Id).ToListAsync();
                                            foreach (var child in posChildIds)
                                            {
                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                            }
                                        }

                                        break;
                                    case JnpfKeyConst.USERSELECT:
                                        {
                                            var userId = item.Value.ToString();
                                            var userChildIds = new List<string>() { userId };
                                            userChildIds.AddRange(_userManager.GetSubordinates(userId).ToList());
                                            foreach (var child in userChildIds)
                                            {
                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                            }
                                        }

                                        break;
                                }

                                break;
                            case 3:
                                switch (searchInfo.jnpfKey)
                                {
                                    case JnpfKeyConst.COMSELECT:
                                        var orgId = item.Value.ToString();
                                        var orgChildIds = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.OrganizeIdTree.Contains(orgId)).Select(it => it.Id).ToListAsync();
                                        foreach (var child in orgChildIds)
                                        {
                                            newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                        }

                                        break;
                                    case JnpfKeyConst.POSSELECT:
                                        var posId = item.Value.ToString();
                                        var posChildIds = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.PositionIdTree.Contains(posId)).Select(it => it.Id).ToListAsync();
                                        foreach (var child in posChildIds)
                                        {
                                            newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                        }

                                        break;
                                    case JnpfKeyConst.USERSELECT:
                                        var userId = item.Value.ToString();
                                        var userChildIds = new List<string>() { userId };
                                        userChildIds.AddRange(_userManager.GetSubordinates(userId, true).ToList());
                                        foreach (var child in userChildIds)
                                        {
                                            newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                        }

                                        break;
                                }

                                break;
                        }

                        if (item.Value.IsNotEmptyOrNull())
                            newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(item.Value.ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty)))).ToList());
                        else
                            newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNullOrEmpty())).ToList());
                    }

                    list = newList.Distinct().ToList();
                    break;
                case 2: // 模糊查询
                    list = list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(item.Value.ToString()))).ToList();
                    break;
                case 3: // 范围查询
                    var between = item.Value.ToObject<List<object>>();
                    var item1 = between[0];
                    var item2 = between[1];
                    switch (searchInfo.jnpfKey)
                    {
                        case JnpfKeyConst.NUMINPUT:
                            {
                                var sNum = item1.IsNotEmptyOrNull() ? item1.ParseToDecimal() : decimal.MinValue;
                                var eNum = item2.IsNotEmptyOrNull() ? item2.ParseToDecimal() : decimal.MaxValue;
                                list = list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ParseToDecimal() >= sNum && xx.Value.ParseToDecimal() <= eNum)).ToList();
                            }

                            break;
                        case JnpfKeyConst.DATE:
                            {
                                var sDate = item1.IsNotEmptyOrNull() ? item1.ToString().TimeStampToDateTime() : DateTime.MinValue;
                                var eDate = item2.IsNotEmptyOrNull() ? item2.ToString().TimeStampToDateTime() : DateTime.MaxValue;
                                list = list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().ParseToDateTime() >= sDate && xx.Value.ToString().ParseToDateTime() <= eDate)).ToList();
                            }

                            break;
                        case JnpfKeyConst.TIME:
                            {
                                var sTime = item1.IsNotEmptyOrNull() ? Convert.ToDateTime(item1) : DateTime.MinValue;
                                var eTime = item2.IsNotEmptyOrNull() ? Convert.ToDateTime(item2) : DateTime.MaxValue;
                                list = list.Where(x => x.Any(xx => xx.Key.Equals(item.Key) && xx.Value.IsNotEmptyOrNull() && Convert.ToDateTime(xx.Value) >= sTime && Convert.ToDateTime(xx.Value) <= eTime)).ToList();
                            }

                            break;
                    }

                    break;
            }
        }

        return list;
    }

    /// <summary>
    /// 组装查询条件.
    /// </summary>
    /// <param name="templateInfo"></param>
    /// <param name="primaryKey"></param>
    /// <param name="sqlStr"></param>
    /// <param name="queryList"></param>
    /// <param name="dataPermission"></param>
    /// <returns></returns>
    private string AssembleQueryCondition(TemplateParsingBase templateInfo, string primaryKey, string sqlStr, List<IConditionalModel> queryList, Dictionary<string, string> tableFieldKeyValue, bool dataPermission = false)
    {
        var sqlCondition = string.Empty;
        var query = (ConditionalTree)queryList.FirstOrDefault();
        foreach (var item in query.ConditionalList)
        {
            // 拼接分组sql条件
            if (sqlCondition.IsNotEmptyOrNull())
                sqlCondition = string.Format(sqlCondition + item.Key);

            // 分组内的sql
            var groupDataSql = string.Empty;

            var groupDataValue = (ConditionalTree)item.Value;
            foreach (var subItem in groupDataValue.ConditionalList)
            {
                if (subItem.Value.IsNotEmptyOrNull())
                {
                    var field = ((ConditionalTree)subItem.Value).ConditionalList.FirstOrDefault();
                    var fieldName = ((ConditionalModel)field.Value).FieldName.Split(".").FirstOrDefault();
                    var table = templateInfo.AllTable.Find(x => x.table.Equals(fieldName));
                    var itemSql = string.Format(sqlStr, table.tableField.IsNullOrEmpty() ? primaryKey : table.tableField, fieldName);
                    var relationField = !table.typeId.Equals("1") && !table.relationField.Equals(primaryKey) ? tableFieldKeyValue.First(x => x.Value.IsNotEmptyOrNull() && x.Value.Equals(table.relationField)).Key : primaryKey;

                    var where = new List<IConditionalModel> { new ConditionalTree() { ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>> { subItem } } };
                    _sqlSugarClient = _databaseService.ChangeDataBase(templateInfo.DbLink);
                    var itemWhere = _sqlSugarClient.SqlQueryable<object>("@")
                        .Where(where).ToSqlString();
                    _sqlSugarClient.AsTenant().ChangeDatabase("default");

                    if (itemWhere.Contains("WHERE"))
                    {
                        // 分组内的sql条件
                        var groupDataSqlCondition = subItem.Key.ToString();

                        if (groupDataValue.ConditionalList.FirstOrDefault().Equals(subItem))
                        {
                            groupDataSql = string.Format("( " + groupDataSql);
                            groupDataSqlCondition = string.Empty;
                        }
                        var splitWhere = itemSql + " where";
                        itemSql = splitWhere + itemWhere.Split("WHERE").Last();

                        // 子表字段为空 查询 处理.
                        if (templateInfo.ChildTableFields.Any(x => x.Value.Contains(fieldName + ".")) && (subItem.ToJsonStringOld().Contains("\"ConditionalType\":11") || subItem.ToJsonStringOld().Contains("\"ConditionalType\":14")))
                        {
                            groupDataSql = string.Format(groupDataSql + groupDataSqlCondition + " ({0} in ({1}) OR {0} NOT IN ( SELECT {2} FROM {3} ))", relationField, itemSql, templateInfo.AllTable.Where(x => x.table.Equals(fieldName)).First().tableField, fieldName);
                        }
                        else
                        {
                            groupDataSql = string.Format(groupDataSql + groupDataSqlCondition + " ({0} in ({1}))", relationField, itemSql);
                        }

                        if (groupDataValue.ConditionalList.LastOrDefault().Equals(subItem))
                            groupDataSql = string.Format(groupDataSql + ")");
                    }
                }
            }

            // 拼接分组sql
            sqlCondition = string.Format(sqlCondition + groupDataSql);
            groupDataSql = string.Empty;
        }

        if (sqlCondition.IsNotEmptyOrNull() && !dataPermission) sqlCondition = string.Format("and ({0})", sqlCondition);

        return sqlCondition;
    }

    /// <summary>
    /// 获取菜单的流程ids.
    /// </summary>
    /// <param name="menuId"></param>
    /// <returns></returns>
    private async Task<string> GetFlowIds(string menuId)
    {
        var flowIds = new List<string>();
        var module = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(it => it.Id == menuId);
        if (module.IsNotEmptyOrNull() && module.Type == 9)
        {
            flowIds = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(it => it.DeleteMark == null && module.PropertyJson.Contains(it.TemplateId)).Select(it => it.Id).ToListAsync();
        }

        return string.Join(",", flowIds);
    }

    /// <summary>
    /// 添加数据日志子表字段.
    /// </summary>
    /// <param name="chidField"></param>
    /// <param name="childModel"></param>
    private void AddLogChidField(List<ChildFieldModel> chidField, FieldsModel childModel)
    {
        if (childModel.IsNotEmptyOrNull() && !chidField.Any(x => x.prop == childModel.__vModel__.Split("-").Last()))
        {
            var childFieldModel = new ChildFieldModel()
            {
                jnpfKey = childModel.__config__.jnpfKey,
                prop = childModel.__vModel__.Split("-").Last(),
                label = childModel.__config__.label
            };

            switch (childModel.__config__.jnpfKey)
            {
                case JnpfKeyConst.RADIO:
                case JnpfKeyConst.CHECKBOX:
                case JnpfKeyConst.SELECT:
                case JnpfKeyConst.CASCADER:
                case JnpfKeyConst.TREESELECT:
                    if (childModel.__config__.dataType == "dynamic") childFieldModel.nameModified = true;

                    break;
                case JnpfKeyConst.UPLOADIMG:
                case JnpfKeyConst.EDITOR:
                case JnpfKeyConst.POPUPTABLESELECT:
                case JnpfKeyConst.RELATIONFORM:
                case JnpfKeyConst.POPUPSELECT:
                    childFieldModel.nameModified = true;

                    break;
            }

            chidField.Add(childFieldModel);
        }
    }

    /// <summary>
    /// 数据日志转换数据.
    /// </summary>
    /// <param name="oldData"></param>
    /// <param name="jnpfKey"></param>
    /// <returns></returns>
    private string LogConvertData(object oldData, string jnpfKey)
    {
        var output = string.Empty;
        switch (jnpfKey)
        {
            case JnpfKeyConst.NUMINPUT:
            case JnpfKeyConst.CALCULATE:
                output = oldData.ParseToDouble().ToString();

                break;
            case JnpfKeyConst.DATE:
            case JnpfKeyConst.CREATETIME:
            case JnpfKeyConst.MODIFYTIME:
            case JnpfKeyConst.DATECALCULATE:
                if (oldData is DateTime)
                    output = string.Format("{0:yyyy-MM-dd HH:mm:ss}", oldData);
                else
                    output = oldData.ToString();

                break;
            case JnpfKeyConst.UPLOADFZ:
                var res = new List<string>();
                foreach (var file in oldData.ToObject<List<Dictionary<string, object>>>())
                {
                    if (file.ContainsKey("name") && file["name"].IsNotEmptyOrNull())
                        res.Add(file["name"].ToString());
                }

                output = string.Join(",", res);
                break;
            case JnpfKeyConst.UPLOADIMG:
                if (oldData is string)
                    output = oldData.ToString();
                else
                    output = oldData.ToJsonString();

                break;
            case JnpfKeyConst.LOCATION:
                var location = oldData.ToString()?.ToObject<Dictionary<string, object>>();
                if (location.ContainsKey("fullAddress") && location["fullAddress"].IsNotEmptyOrNull())
                    output = location["fullAddress"].ToString();

                break;
            default:
                output = oldData.ToString();
                break;
        }

        return output;
    }

    /// <summary>
    /// 获取菜单的表单权限.
    /// </summary>
    /// <param name="menuId"></param>
    /// <returns></returns>
    private async Task<List<string>> GetMenuFormAuthorize(string menuId)
    {
        List<string>? res = null;
        if (!_userManager.IsAdministrator)
        {
            var roles = _userManager.PermissionIds;
            if (roles.Any())
            {
                var items = await _visualDevRepository.AsSugarClient().Queryable<AuthorizeEntity>().In(a => a.ObjectId, roles).Where(a => a.ItemType == "form").GroupBy(it => new { it.ItemId }).Select(it => it.ItemId).ToListAsync();
                res = await _visualDevRepository.AsSugarClient().Queryable<ModuleFormEntity>().Where(a => items.Contains(a.Id) && a.ModuleId.Equals(menuId)).Where(a => a.EnabledMark == 1 && a.DeleteMark == null).Select(x => x.EnCode).ToListAsync();
            }
        }

        return res;
    }

    /// <summary>
    /// 处理表单权限数据.
    /// </summary>
    /// <param name="templateInfo"></param>
    /// <param name="data"></param>
    /// <param name="authList"></param>
    /// <param name="isForm"></param>
    /// <returns></returns>
    private Dictionary<string, object> GetFormAuthData(TemplateParsingBase templateInfo, Dictionary<string, object> data, List<string> authList, bool isForm = false)
    {
        var res = new Dictionary<string, object>();
        foreach (var item in data)
        {
            var model = templateInfo.AllFieldsModel.Find(x => x.__vModel__ == item.Key);
            if (model.IsNotEmptyOrNull())
            {
                if (model.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                {
                    if (isForm && !authList.Contains(model.__vModel__))
                    {
                        res.Add(item.Key, new List<Dictionary<string, object>>());
                        continue;
                    }

                    var cDataList = item.Value.ToObject<List<Dictionary<string, object>>>();
                    var ncDataList = new List<Dictionary<string, object>>();
                    foreach (var cData in cDataList)
                    {
                        var ncData = new Dictionary<string, object>();
                        foreach (var ccData in cData)
                        {
                            if (ccData.Key.Contains("_jnpfId")) continue;

                            var cKey = string.Format("{0}-{1}", model.__vModel__, ccData.Key);
                            var cModel = templateInfo.AllFieldsModel.Find(x => x.__vModel__ == cKey);
                            if (cModel.IsNotEmptyOrNull())
                            {
                                if (authList.Contains(cKey))
                                {
                                    ncData.Add(ccData.Key, ccData.Value);
                                    var jnpfData = string.Format("{0}_jnpfId", ccData.Key);
                                    if (cData.ContainsKey(jnpfData)) ncData.Add(jnpfData, cData[jnpfData]);
                                }
                            }
                            else
                            {
                                ncData.Add(ccData.Key, ccData.Value);
                            }
                        }

                        ncDataList.Add(ncData);
                    }

                    res.Add(item.Key, ncDataList);
                }
                else
                {
                    if (authList.Contains(item.Key)) res.Add(item.Key, item.Value);
                }
            }
            else
            {
                res.Add(item.Key, item.Value);
            }
        }

        return res;
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }

    #endregion
}