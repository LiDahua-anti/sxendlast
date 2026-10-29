using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.Authorize;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.CodeAnalysis;
using SqlSugar;

namespace JNPF.Common.CodeGen.DataParsing;

public class ControlParsing : ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _repository;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 模板表单列表数据解析.
    /// </summary>
    private readonly FormDataParsing _formDataParsing;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public ControlParsing(
        IUserManager userManager,
        FormDataParsing formDataParsing,
        ISqlSugarRepository<UserEntity> repositoryRepository,
        IDataInterfaceService dataInterfaceService,
        IBillRullService billRuleService,
        ICacheManager cacheManager)
    {
        _userManager = userManager;
        _repository = repositoryRepository;
        _cacheManager = cacheManager;
        _formDataParsing = formDataParsing;
        _billRuleService = billRuleService;
        _dataInterfaceService = dataInterfaceService;
    }

    /// <summary>
    /// 解析控件数据.
    /// </summary>
    /// <param name="oldDatas">原数据集合.</param>
    /// <param name="vModelStr">解析的字段集合 多个需以 ,号隔开.</param>
    /// <param name="jnpfKeyConst">控件类型 (JnpfKeyConst).</param>
    /// <param name="tenantId">租户Id.</param>
    /// <param name="vModelAttr">控件属性 (vModel ,属性字符串).</param>
    /// <param name="isInlineEditor">是否行内编辑.</param>
    /// <param name="userOrigin">请求端类型 pc , app.</param>
    /// <returns></returns>
    public async Task<List<Dictionary<string, object>>> GetParsDataList(
        List<Dictionary<string, object>> oldDatas,
        string vModelStr,
        string jnpfKeyConst,
        string tenantId,
        List<FieldsModel>? vModelAttr = null,
        bool isInlineEditor = false,
        string userOrigin = "pc")
    {
        var vModels = new Dictionary<string, object>();
        var vModelList = vModelStr.Split(',');
        oldDatas.ForEach(items =>
        {
            foreach (var item in items)
            {
                if (vModelList.Contains(item.Key) && !vModels.ContainsKey(item.Key)) vModels.Add(item.Key, jnpfKeyConst);

                // 子表
                if (item.Value != null && item.Key.ToLower().Contains("tablefield") && (item.Value is List<Dictionary<string, object>> || item.Value.GetType().Name.Equals("JArray")))
                {
                    var ctOldDatas = item.Value.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
                    ctOldDatas.ForEach(ctItems =>
                    {
                        foreach (var ctItem in ctItems)
                        {
                            if ((vModelList.Contains(item.Key + "-" + ctItem.Key) || vModelList.Contains(item.Key + "-" + ctItem.Key + "_name")) && !vModels.ContainsKey(item.Key + "-" + ctItem.Key))
                                vModels.Add(item.Key + "-" + ctItem.Key, jnpfKeyConst);
                        }
                    });
                }
            }
        });

        return await GetParsDataByList(vModelAttr, oldDatas, vModels, tenantId, isInlineEditor, userOrigin);
    }

    /// <summary>
    /// 获取解析数据.
    /// </summary>
    /// <param name="fields">模板集合.</param>
    /// <param name="oldDatas">原数据集合.</param>
    /// <param name="vModels">需解析的字段 (字段名,JnpfKeyConst/子表dictionary).</param>
    /// <param name="tenantId">租户Id.</param>
    /// <param name="isInlineEditor">是否行内编辑.</param>
    /// <param name="userOrigin">请求端类型 pc , app.</param>
    /// <param name="isChildTable">是否子表控件.</param>
    /// <returns></returns>
    private async Task<List<Dictionary<string, object>>> GetParsDataByList(
        List<FieldsModel> fields,
        List<Dictionary<string, object>> oldDatas,
        Dictionary<string, object> vModels,
        string tenantId,
        bool isInlineEditor,
        string userOrigin,
        bool isChildTable = false)
    {
        var cacheData = await GetCaCheData(vModels, tenantId);
        var usersselectDatas = cacheData.Where(t => t.Key.Equals(CommonConst.CodeGenDynamic + "_usersSelect_" + tenantId)).FirstOrDefault().Value.ToObject<Dictionary<string, string>>(); // 用户组件

        foreach (var items in oldDatas)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items.ToList()[i];

                if (vModels.Any(x => x.Key.Equals(item.Key)) && items[item.Key] != null)
                {
                    FieldsModel model = fields.Any(x => x.__vModel__.Equals(item.Key)) ? fields.Find(x => x.__vModel__.Equals(item.Key)) : (fields.Any(x => x.__vModel__.Equals(item.Key.Replace("_name", string.Empty))) ? fields.Find(x => x.__vModel__.Equals(item.Key.Replace("_name", string.Empty))) : new FieldsModel());
                    model.separator = ",";
                    var jnpfKey = vModels.FirstOrDefault(x => x.Key.Equals(item.Key)).Value;
                    switch (jnpfKey)
                    {
                        case JnpfKeyConst.USERSSELECT:
                            {
                                if (userOrigin.Equals("pc") && isInlineEditor && isChildTable) continue;
                                var itemValue = item.Value;
                                if (itemValue == null && items.ContainsKey(item.Key.Replace("_name", string.Empty))) itemValue = items[item.Key.Replace("_name", string.Empty)];
                                if (itemValue != null)
                                {
                                    var vList = new List<string>();
                                    if (itemValue.ToString().Contains("[")) vList = itemValue.ToString().ToObject<List<string>>();
                                    else vList.Add(itemValue.ToString());
                                    var itemValues = new List<string>();
                                    vList.ForEach(it =>
                                    {
                                        if (usersselectDatas.Any(x => x.Key.Contains(it))) itemValues.Add(usersselectDatas.First(x => x.Key.Contains(it)).Value);
                                    });
                                    if (itemValues.Any()) items[item.Key] = string.Join(",", itemValues);
                                }
                            }

                            break;
                        case JnpfKeyConst.POPUPSELECT: // 弹窗选择
                            {
                                if (model.interfaceId.IsNullOrEmpty()) continue;
                                if (userOrigin.Equals("pc") && isInlineEditor && isChildTable) continue;
                                List<Dictionary<string, string>> popupselectDataList = new List<Dictionary<string, string>>();

                                // 获取远端数据
                                var dynamic = await _dataInterfaceService.GetInfo(model.interfaceId);
                                var redisName = CommonConst.CodeGenDynamic + "_" + model.interfaceId + "_" + tenantId;
                                if (_cacheManager.Exists(redisName))
                                {
                                    popupselectDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
                                }
                                else
                                {
                                    popupselectDataList = await _formDataParsing.GetDynamicList(model);
                                    _cacheManager.Set(redisName, popupselectDataList.ToList(), TimeSpan.FromMinutes(10)); // 缓存10分钟
                                    popupselectDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
                                }

                                switch (dynamic.Type)
                                {
                                    case 1: // SQL数据
                                        {
                                            var specificData = popupselectDataList.Where(it => it.ContainsKey(model.propsValue) && it.ContainsValue(items[item.Key] == null ? model.interfaceId : items[item.Key].ToString())).FirstOrDefault();
                                            if (specificData != null)
                                            {
                                                // 要用模板的 “显示字段 - relationField”来展示数据
                                                items[model.__vModel__ + "_id"] = items[item.Key];
                                                items[item.Key] = specificData[model.relationField];

                                                // 弹窗选择属性
                                                if (model.relational.IsNotEmptyOrNull())
                                                    foreach (var fItem in model.relational.Split(",")) items[model.__vModel__ + "_" + fItem] = specificData[fItem];
                                            }
                                        }
                                        break;
                                    case 2: // 静态数据
                                        {
                                            var vara = popupselectDataList.Where(a => a.ContainsValue(items[item.Key] == null ? model.interfaceId : items[item.Key].ToString())).FirstOrDefault();
                                            if (vara != null)
                                            {
                                                items[model.__vModel__ + "_id"] = items[item.Key];
                                                items[item.Key] = vara[items[item.Key].ToString()];

                                                // 弹窗选择属性
                                                if (model.relational.IsNotEmptyOrNull())
                                                    foreach (var fItem in model.relational.Split(",")) items[model.__vModel__ + "_" + fItem] = vara[fItem];
                                            }
                                        }
                                        break;
                                    case 3: // Api数据
                                        {
                                            var specificData = popupselectDataList.Where(it => it.ContainsKey(model.propsValue) && it.ContainsValue(items[item.Key] == null ? model.interfaceId : items[item.Key].ToString())).FirstOrDefault();
                                            if (specificData != null)
                                            {
                                                // 要用模板的 “显示字段 - relationField”来展示数据
                                                items[model.__vModel__ + "_id"] = items[item.Key];
                                                items[item.Key] = specificData[model.relationField];

                                                // 弹窗选择属性
                                                if (model.relational.IsNotEmptyOrNull())
                                                    foreach (var fItem in model.relational.Split(",")) items[model.__vModel__ + "_" + fItem] = specificData[fItem];
                                            }
                                        }
                                        break;
                                }
                            }

                            break;

                        case JnpfKeyConst.RELATIONFORM: // 关联表单
                            {
                                if (model.modelId.IsNullOrEmpty()) continue;
                                if (userOrigin.Equals("pc") && isInlineEditor && isChildTable) continue;
                                List<Dictionary<string, object>> relationFormDataList = new List<Dictionary<string, object>>();

                                var redisName = CommonConst.CodeGenDynamic + "_" + model.modelId + "_" + tenantId;
                                if (_cacheManager.Exists(redisName))
                                {
                                    relationFormDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, object>>>();
                                }
                                else
                                {
                                    // 根据可视化功能ID获取该模板全部数据
                                    var relationFormModel = await _repository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == model.modelId);
                                    VisualDevModelListQueryInput listQueryInput = new VisualDevModelListQueryInput
                                    {
                                        dataType = "1",
                                        pageSize = 999999
                                    };

                                    Scoped.Create((_, scope) =>
                                    {
                                        var services = scope.ServiceProvider;
                                        var _runService = App.GetService<IRunService>(services);
                                        var res = _runService.GetRelationFormList(relationFormModel.Adapt<VisualDevEntity>(), listQueryInput).WaitAsync(TimeSpan.FromMinutes(2)).Result;
                                        _cacheManager.Set(redisName, res.list.ToList(), TimeSpan.FromMinutes(10)); // 缓存10分钟
                                    });
                                    var cacheStr = _cacheManager.Get(redisName);
                                    if (cacheStr.IsNotEmptyOrNull()) relationFormDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, object>>>();
                                }

                                if (model.propsValue.IsNullOrEmpty()) model.propsValue = "id";
                                var relationFormRealData = relationFormDataList.Where(it => it[model.propsValue].ToString().TrimEnd().Equals(items[item.Key].ToString())).FirstOrDefault();
                                if (relationFormRealData != null && relationFormRealData.Count > 0)
                                {
                                    items[model.__vModel__ + "_id"] = relationFormRealData[model.propsValue];
                                    items[item.Key] = relationFormRealData.ContainsKey(model.relationField) ? relationFormRealData[model.relationField] : string.Empty;

                                    // 关联表单属性
                                    if (model.relational.IsNotEmptyOrNull())
                                        foreach (var fItem in model.relational.Split(",")) if (relationFormRealData.ContainsKey(fItem)) items[model.__vModel__ + "_" + fItem] = relationFormRealData[fItem];
                                }
                                else
                                {
                                    items[item.Key] = string.Empty;
                                }
                            }

                            break;
                    }
                }

                // 子表
                if (item.Value != null && item.Key.ToLower().Contains("tablefield") && (item.Value is List<Dictionary<string, object>> || item.Value.GetType().Name.Equals("JArray")))
                {
                    var ctList = item.Value.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
                    foreach (var ctItem in vModels.Where(x => x.Key.Contains(item.Key)).ToList())
                    {
                        var ctVModels = new Dictionary<string, object>();
                        ctVModels.Add(ctItem.Key.Split("-").LastOrDefault(), ctItem.Value);
                        var ctFields = fields.Find(x => x.__vModel__.Equals(ctItem.Key.Split("-").FirstOrDefault())).__config__.children;
                        if (ctList.Any()) items[item.Key] = await GetParsDataByList(ctFields, ctList, ctVModels, tenantId, isInlineEditor, userOrigin, true);
                    }
                }
            }
        }

        return oldDatas;
    }

    /// <summary>
    /// 获取解析数据缓存.
    /// </summary>
    /// <param name="vModels"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, object>> GetCaCheData(Dictionary<string, object> vModels, string tenantId)
    {
        var res = new Dictionary<string, object>();

        if (vModels.Where(x => x.Value.Equals(JnpfKeyConst.USERSSELECT)).Any())
        {
            string? userCacheKey = CommonConst.CodeGenDynamic + "_usersSelect_" + tenantId;
            if (_cacheManager.Exists(userCacheKey))
            {
                res.Add(userCacheKey, _cacheManager.Get(userCacheKey).ToObject<Dictionary<string, object>>());
            }
            else
            {
                var addList = new Dictionary<string, string>();
                (await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.RealName, x.Account }).ToListAsync()).ForEach(item => addList.Add(item.Id + "--user", item.RealName + "/" + item.Account));
                (await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item =>
                {
                    addList.Add(item.Id + "--company", item.FullName);
                    addList.Add(item.Id + "--department", item.FullName);
                });
                (await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item => addList.Add(item.Id + "--role", item.FullName));
                (await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item => addList.Add(item.Id + "--position", item.FullName));
                (await _repository.AsSugarClient().Queryable<GroupEntity>().Where(x => x.DeleteMark == null).Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item => addList.Add(item.Id + "--group", item.FullName));

                // 缓存5分钟
                _cacheManager.Set(userCacheKey, addList, TimeSpan.FromMinutes(5));
                res.Add(userCacheKey, addList);
            }
        }

        return res;
    }

    /// <summary>
    /// 获取用户组件查询条件组装.
    /// </summary>
    /// <param name="key">字段名.</param>
    /// <param name="values">查询值.</param>
    /// <returns></returns>
    public List<IConditionalModel> GetUsersSelectQueryWhere(string key, object values)
    {
        var valueList = new List<string>();
        if (values != null)
        {
            valueList = values.ToString().Contains("[") && values.ToString().Contains("]") ? values.ToObject<List<string>>() : new List<string>() { values.ToString() };
        }

        if (values.IsNotEmptyOrNull()) return GetUsersSelectQueryWhere(key, valueList);
        else return new List<IConditionalModel>();
    }

    /// <summary>
    /// 获取用户组件查询条件组装.
    /// </summary>
    /// <param name="key">字段名.</param>
    /// <param name="values">查询值.</param>
    /// <returns></returns>
    public List<IConditionalModel> GetUsersSelectQueryWhere(string key, object values, bool isMultiple = false)
    {
        return GetUsersSelectQueryWhere(key, values);
    }

    /// <summary>
    /// 获取用户组件查询条件组装.
    /// </summary>
    /// <param name="key">字段名.</param>
    /// <param name="values">查询值.</param>
    /// <returns></returns>
    public List<IConditionalModel> GetUsersSelectQueryWhere(string key, List<string> values)
    {
        var conModels = new List<IConditionalModel>();
        if (values.IsNullOrEmpty() || !values.Any()) return conModels;
        values = values.Where(x => x != null).ToList();
        if (values.IsNullOrEmpty() || !values.Any()) return conModels;
        var userIds = values.Select(x => x.Replace("--user", string.Empty)).ToList();
        var rIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => userIds.Contains(x.UserId)).Select(x => new { x.ObjectId, x.ObjectType }).ToList();
        var objIdList = values;
        rIdList.ForEach(x =>
        {
            if (x.ObjectType.Equals("organize"))
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
                    FieldName = key,
                    ConditionalType = ConditionalType.Like,
                    FieldValue = objIdList[i]
                }));
            }
            else
            {
                whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.Or, new ConditionalModel
                {
                    FieldName = key,
                    ConditionalType = ConditionalType.Like,
                    FieldValue = objIdList[i]
                }));
            }
        }

        whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
        {
            FieldName = key,
            ConditionalType = ConditionalType.IsNot,
            FieldValue = null
        }));
        whereList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, new ConditionalModel
        {
            FieldName = key,
            ConditionalType = ConditionalType.IsNot,
            FieldValue = string.Empty
        }));
        conModels.Add(new ConditionalCollections() { ConditionalList = whereList });
        return conModels;
    }

    /// <summary>
    /// 生成查询多选条件.
    /// </summary>
    /// <param name="key">数据库列名称.</param>
    /// <param name="list"></param>
    /// <returns></returns>
    public List<IConditionalModel> GenerateMultipleSelectionCriteriaForQuerying(string key, object? obj)
    {
        var conModels = new List<IConditionalModel>();
        var addItems = new List<KeyValuePair<WhereType, ConditionalModel>>();
        var list = new List<string>();
        if (obj != null && obj.ToString().IsNotEmptyOrNull())
        {
            list = obj.ToString().Contains("[") && obj.ToString().Contains("]") ? obj.ToObject<List<string>>() : new List<string>() { obj.ToString() };
        }

        for (int i = 0; i < list?.Count; i++)
        {
            var add = new KeyValuePair<WhereType, ConditionalModel>(i == 0 ? WhereType.And : WhereType.Or, new ConditionalModel
            {
                FieldName = key,
                ConditionalType = ConditionalType.Like,
                FieldValue = list[i]
            });
            addItems.Add(add);
        }
        if (addItems?.Count > 0)
            conModels.Add(new ConditionalCollections() { ConditionalList = addItems });
        return conModels;
    }

    #region 解析组织、岗位数据

    /// <summary>
    /// 所有组织.
    /// </summary>
    public List<OrganizeEntity> AllOrganizeList
    {
        get
        {
            if (_allOrganizeList == null) _allOrganizeList = GetOrgListTreeName();
            return _allOrganizeList;
        }
    }
    private List<OrganizeEntity> _allOrganizeList { get; set; }

    /// <summary>
    /// 处理组织树 名称.
    /// </summary>
    /// <returns></returns>
    private List<OrganizeEntity> GetOrgListTreeName()
    {
        List<OrganizeEntity>? orgTreeNameList = new List<OrganizeEntity>();

        string? userCacheKey = CommonConst.CodeGenDynamic + "_AllOrganize";
        if (_cacheManager.Exists(userCacheKey))
        {
            orgTreeNameList = _cacheManager.Get(userCacheKey).ToObject<List<OrganizeEntity>>();
        }
        else
        {
            orgTreeNameList = new List<OrganizeEntity>();
            List<OrganizeEntity>? orgList = _repository.AsSugarClient().Queryable<OrganizeEntity>().ToList();
            orgList.ForEach(item =>
            {
                if (item.OrganizeIdTree.IsNullOrEmpty()) item.OrganizeIdTree = item.Id;
                OrganizeEntity? newItem = item.Adapt<OrganizeEntity>();
                newItem.Id = item.Id;
                var orgNameList = new List<string>();
                item.OrganizeIdTree.Split(",").ToList().ForEach(it =>
                {
                    var org = orgList.Find(x => x.Id == it);
                    if (org != null) orgNameList.Add(org.FullName);
                });
                newItem.Description = string.Join("/", orgNameList);
                orgTreeNameList.Add(newItem);
            });

            // 缓存5分钟
            _cacheManager.Set(userCacheKey, orgTreeNameList, TimeSpan.FromMinutes(5));
        }

        return orgTreeNameList;
    }

    /// <summary>
    /// 所有岗位.
    /// </summary>
    public List<PositionEntity> AllPositionList
    {
        get
        {
            if (_allPositionList == null) _allPositionList = GetPosListTreeName();
            return _allPositionList;
        }
    }
    private List<PositionEntity> _allPositionList { get; set; }

    /// <summary>
    /// 处理岗位 名称.
    /// </summary>
    /// <returns></returns>
    private List<PositionEntity> GetPosListTreeName()
    {
        List<PositionEntity>? orgTreeNameList = new List<PositionEntity>();

        string? userCacheKey = CommonConst.CodeGenDynamic + "_AllPosition";
        if (_cacheManager.Exists(userCacheKey))
        {
            orgTreeNameList = _cacheManager.Get(userCacheKey).ToObject<List<PositionEntity>>();
        }
        else
        {
            orgTreeNameList = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null)
                .Select(x => new PositionEntity()
                {
                    Id = x.Id,
                    FullName = SqlFunc.MergeString(SqlFunc.Subqueryable<OrganizeEntity>().Where(xx => xx.Id.Equals(x.OrganizeId)).Select(xx => xx.FullName), "/", x.FullName)
                }).ToList();

            // 缓存5分钟
            _cacheManager.Set(userCacheKey, orgTreeNameList, TimeSpan.FromMinutes(5));
        }

        return orgTreeNameList;
    }

    /// <summary>
    /// 解析所属组织.
    /// </summary>
    /// <param name="oIds">组织Id json.</param>
    /// <returns></returns>
    public string GetCurrOrganizeName(string oIds)
    {
        var res = string.Empty;
        if (oIds.IsNotEmptyOrNull())
        {
            var idList = new List<string>();
            try
            {
                idList = oIds.ToObject<List<string>>().ToList();
            }
            catch
            {
                idList = oIds.ToString().ToObject<List<string>>().ToList();
            }
            var oList = AllOrganizeList.Where(x => idList.Contains(x.Id) || idList.Contains(x.EnCode)).Select(x => x.OrganizeNameTree).ToList();
            res = string.Join(";", oList);
        }

        return res;
    }

    /// <summary>
    /// 解析所属岗位.
    /// </summary>
    /// <param name="pIds">岗位Id json.</param>
    /// <returns></returns>
    public string GetCurrPositionName(string pIds)
    {
        var res = string.Empty;
        if (pIds.IsNotEmptyOrNull())
        {
            var idList = new List<string>();
            try
            {
                idList = pIds.ToObject<List<string>>().ToList();
            }
            catch
            {
                idList = pIds.ToString().ToObject<List<string>>().ToList();
            }

            var pList = new List<string>();
            foreach (var item in idList) pList.Add(AllPositionList.Where(x => x.Id == item).Select(x => x.FullName).FirstOrDefault());
            res = string.Join(";", pList);
        }

        return res;
    }

    /// <summary>
    /// 解析岗位.
    /// </summary>
    /// <param name="pIds">岗位Id json.</param>
    /// <returns></returns>
    public string GetPositionName(string pIds)
    {
        var res = string.Empty;
        if (pIds.IsNotEmptyOrNull())
        {
            var idList = new List<string>() { pIds };
            if (pIds.Contains("[") && pIds.Contains("]"))
            {
                try
                {
                    idList = pIds.ToObject<List<string>>().ToList();
                }
                catch
                {
                    idList = pIds.ToString().ToObject<List<string>>().ToList();
                }
            }

            var pList = new List<string>();
            foreach (var item in idList) pList.Add(AllPositionList.Where(x => x.Id == item).Select(x => x.FullName).FirstOrDefault());
            res = string.Join(";", pList);
        }

        return res;
    }

    /// <summary>
    /// 解析组织.
    /// </summary>
    /// <param name="isMultiple">是否多选.</param>
    /// <param name="oIds">组织Id.</param>
    /// <returns></returns>
    public string GetOrganizeName(bool isMultiple, object oIds)
    {
        var res = string.Empty;
        if (oIds.IsNotEmptyOrNull())
        {
            if (isMultiple)
            {
                var idList = new List<string>();
                try
                {
                    idList = oIds.ToObject<List<string>>();
                }
                catch
                {
                    idList = oIds.ToString().ToObject<List<string>>();
                }
                var resList = new List<string>();
                foreach (var item in idList) resList.Add(AllOrganizeList.Where(x => x.Id == item || x.EnCode == item).Select(x => x.Description).FirstOrDefault());
                res = string.Join(",", resList);
            }
            else
            {
                res = AllOrganizeList.Where(x => x.Id == oIds.ToString() || x.EnCode == oIds.ToString()).Select(x => x.Description).FirstOrDefault();
            }
        }

        return res;
    }

    /// <summary>
    /// 解析部门.
    /// </summary>
    /// <param name="isMultiple">是否多选.</param>
    /// <param name="oIds">组织Id.</param>
    /// <param name="singleParsing">单选也解析.</param>
    /// <returns></returns>
    public string GetDepartmentName(bool isMultiple, object oIds, bool singleParsing = false)
    {
        var res = string.Empty;
        if (oIds.IsNotEmptyOrNull())
        {
            if (isMultiple)
            {
                var idList = new List<string>();
                try
                {
                    idList = oIds.ToObject<List<string>>();
                }
                catch
                {
                    idList = oIds.ToString().ToObject<List<string>>();
                }

                var resList = new List<string>();
                foreach (var item in idList) resList.Add(AllOrganizeList.Where(x => x.Id == item || x.EnCode == item).Select(x => x.FullName).FirstOrDefault());
                res = string.Join(",", resList);
            }
            else
            {
                res = AllOrganizeList.Where(x => x.Id == oIds.ToString() || x.EnCode == oIds.ToString()).Select(x => x.FullName).FirstOrDefault();
            }
        }

        return res;
    }

    #endregion

    #region 流程相关

    /// <summary>
    /// 根据流程模板Id 获取所有FlowId.
    /// </summary>
    /// <param name="templateId">.</param>
    /// <returns></returns>
    public async Task<List<string>> GetFlowIdListByTemplateId(string templateId)
    {
        return await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(x => x.TemplateId.Equals(templateId) && x.DeleteMark == null).Select(x => x.Id).ToListAsync();
    }
    #endregion

    #region 获取单据规则

    /// <summary>
    /// 获取单据规则.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="moduleId"></param>
    /// <param name="fieldList"></param>
    /// <param name="allDataMap"></param>
    /// <param name="isChild"></param>
    /// <returns></returns>
    public async Task<string> GetBillRule(string key, string value, string moduleId, List<FieldsModel> fieldList, object allDataMap, bool isChild = false)
    {
        if (value.IsNullOrEmpty())
        {
            var config = fieldList.FirstOrDefault(x => x.__vModel__.Equals(key))?.__config__;
            if (isChild)
            {
                var childTable = key.Split('-');
                config = fieldList.FirstOrDefault(x => x.__vModel__.Equals(childTable.First())).__config__.children.FirstOrDefault(x => x.__vModel__.Equals(childTable.LastOrDefault()))?.__config__;
            }
            var allData = allDataMap.ToObject<Dictionary<string, object>>();
            var flowId = allData.ContainsKey("flowId") && allData["flowId"].IsNotEmptyOrNull() ? allData["flowId"].ToString() : null;
            if (config.ruleType != null && config.ruleType == 2) return await _formDataParsing.GetBillRule(moduleId, config, allDataMap.ToObject<Dictionary<string, object>>(), flowId, isChild);
            else return await _billRuleService.GetBillNumber(config.rule);
        }
        else
        {
            return value;
        }
    }

    #endregion

    #region 查询条件动态参数解析

    public async Task<T> ReplaceDynamicQuery<T>(T queryObj)
    {
        if (queryObj == null) return queryObj;
        var dynamicQueryKey = CommonConst.DYNAMICQUERYKEY.Split(',');

        #region 高级查询
        if (queryObj is ConvertSuper)
        {
            var querList = queryObj as ConvertSuper;
            foreach (var item in querList.convertGroups)
            {
                var copyQuery = new List<ConvertSuperQuery>();
                foreach (var quer in item.convertSuperQuery)
                {
                    if (dynamicQueryKey.Contains(quer.fieldValue))
                    {
                        var queryKey = quer.fieldValue;
                        quer.fieldValue = await GetQueryItemValue(quer.fieldValue);
                        if (quer.conditionalType.Equals(ConditionalType.In) || quer.conditionalType.Equals(ConditionalType.NotIn))
                        {
                            if (quer.fieldValue.Contains("[") && quer.fieldValue.Contains("]"))
                            {
                                var ids = quer.fieldValue.ToObject<List<string>>();

                                var convertSuperQuery = new ConvertSuperQuery();
                                var childConvertSuperQuery = new List<ConvertSuperQuery>();
                                for (var i = 0; i < ids.Count; i++)
                                {
                                    var it = ids[i];
                                    var conditionWhereType = WhereType.And;
                                    if (quer.conditionalType.Equals(ConditionalType.In)) conditionWhereType = i.Equals(0) && quer.whereType.Equals(WhereType.And) ? WhereType.And : WhereType.Or;
                                    else conditionWhereType = i.Equals(0) && quer.whereType.Equals(WhereType.Or) ? WhereType.Or : WhereType.And;

                                    childConvertSuperQuery.Add(new ConvertSuperQuery
                                    {
                                        whereType = conditionWhereType,
                                        jnpfKey = quer.jnpfKey,
                                        field = quer.field,
                                        fieldValue = it,
                                        conditionalType = quer.conditionalType.Equals(ConditionalType.In) ? ConditionalType.Like : ConditionalType.NoLike,
                                        mainWhere = false,
                                        symbol = ""
                                    });
                                }

                                convertSuperQuery.whereType = WhereType.And;
                                convertSuperQuery.childConvertSuperQuery = childConvertSuperQuery;
                                copyQuery.Add(convertSuperQuery);

                                if (quer.conditionalType.Equals(ConditionalType.NotIn))
                                {
                                    copyQuery.Add(new ConvertSuperQuery
                                    {
                                        whereType = WhereType.And,
                                        jnpfKey = quer.jnpfKey,
                                        field = quer.field,
                                        fieldValue = null,
                                        conditionalType = ConditionalType.IsNot,
                                        mainWhere = false,
                                        symbol = ""
                                    });
                                    copyQuery.Add(new ConvertSuperQuery
                                    {
                                        whereType = WhereType.And,
                                        jnpfKey = quer.jnpfKey,
                                        field = quer.field,
                                        fieldValue = string.Empty,
                                        conditionalType = ConditionalType.IsNot,
                                        mainWhere = false,
                                        symbol = ""
                                    });
                                }
                            }
                            else
                            {
                                if (quer.fieldValue.IsNullOrEmpty() || quer.fieldValue.Equals("[]"))
                                {
                                    if (quer.conditionalType.Equals(ConditionalType.In)) quer.conditionalType = ConditionalType.IsNullOrEmpty;
                                    else if (quer.conditionalType.Equals(ConditionalType.NotIn)) quer.conditionalType = ConditionalType.IsNot;

                                    copyQuery.Add(quer);
                                }
                                else
                                {
                                    copyQuery.Add(quer);
                                }
                            }
                        }
                        else
                        {
                            if (quer.fieldValue.IsNullOrEmpty() || quer.fieldValue.Equals("[]"))
                            {
                                if (quer.conditionalType.Equals(ConditionalType.Equal)) quer.conditionalType = ConditionalType.IsNullOrEmpty;
                                else if (quer.conditionalType.Equals(ConditionalType.NoEqual)) quer.conditionalType = ConditionalType.IsNot;

                                copyQuery.Add(quer);
                            }
                            else
                            {
                                copyQuery.Add(quer);
                            }
                        }
                    }
                    else
                    {
                        copyQuery.Add(quer);
                    }
                }
                item.convertSuperQuery = copyQuery;
            }

            return querList.ToObject<T>();
        }
        #endregion

        #region 数据过滤
        else if (queryObj is List<CodeGenDataRuleModuleResourceModel>)
        {
            var querList = queryObj as List<CodeGenDataRuleModuleResourceModel>;

            foreach (var quer in querList)
            {
                if (dynamicQueryKey.Any(x => quer.conditionalModelJson.Contains(x)))
                {
                    var newConditionalList = new List<IConditionalModel>();
                    foreach (var condItem in _repository.AsSugarClient().Utilities.JsonToConditionalModels(quer.conditionalModelJson))
                    {
                        var cond = condItem as ConditionalTree;

                        for (int i = 0; i < cond.ConditionalList.Count; i++)
                        {
                            var iValues = cond.ConditionalList[i].Value as ConditionalTree;
                            for (var j = 0; j < iValues.ConditionalList.Count; j++)
                            {
                                var jValues = iValues.ConditionalList[j].Value as ConditionalTree;
                                var newValues = new ConditionalTree() { ConditionalList = new List<KeyValuePair<WhereType, IConditionalModel>>() };
                                for (var k = 0; k < jValues.ConditionalList.Count; k++)
                                {
                                    var item = jValues.ConditionalList[k].Value as ConditionalModel;

                                    if (dynamicQueryKey.Contains(item.FieldValue))
                                    {
                                        var queryKey = item.FieldValue;
                                        item.FieldValue = await GetQueryItemValue(item.FieldValue);
                                        if ((item.ConditionalType.Equals(ConditionalType.In) || item.ConditionalType.Equals(ConditionalType.NotIn)))
                                        {
                                            if (item.FieldValue.Contains("[") && item.FieldValue.Contains("]"))
                                            {
                                                var ids = item.FieldValue.ToObject<List<string>>();

                                                for (var l = 0; l < ids.Count; l++)
                                                {
                                                    var it = ids[l];
                                                    var whereType = WhereType.And;
                                                    if (item.ConditionalType.Equals(ConditionalType.In)) whereType = l.Equals(0) && jValues.ConditionalList[k].Key.ToString().ToUpper().Equals("AND") ? WhereType.And : WhereType.Or;
                                                    else whereType = l.Equals(0) && jValues.ConditionalList[k].Key.ToString().ToUpper().Equals("OR") ? WhereType.Or : WhereType.And;
                                                    newValues.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(whereType, new ConditionalModel
                                                    {
                                                        FieldName = item.FieldName,
                                                        ConditionalType = item.ConditionalType.Equals(ConditionalType.In) ? ConditionalType.Like : ConditionalType.NoLike,
                                                        FieldValue = it
                                                    }));
                                                }

                                                if (item.ConditionalType.Equals(ConditionalType.NotIn))
                                                {
                                                    newValues.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(WhereType.And, new ConditionalModel
                                                    {
                                                        FieldName = item.FieldName,
                                                        ConditionalType = ConditionalType.IsNot,
                                                        FieldValue = string.Empty
                                                    }));
                                                }
                                            }
                                            else
                                            {
                                                if (item.FieldValue.IsNullOrEmpty() || item.FieldValue.Equals("[]"))
                                                {
                                                    if (item.ConditionalType.Equals(ConditionalType.In)) item.ConditionalType = ConditionalType.IsNullOrEmpty;
                                                    else if (item.ConditionalType.Equals(ConditionalType.NotIn)) item.ConditionalType = ConditionalType.IsNot;

                                                    newValues.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(jValues.ConditionalList[k].Key, item));
                                                }
                                                else
                                                {
                                                    item.ConditionalType = item.ConditionalType.Equals(ConditionalType.In) ? ConditionalType.Like : ConditionalType.NoLike;
                                                    newValues.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(jValues.ConditionalList[k].Key, item));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if ((item.FieldValue.IsNullOrEmpty() && !(item.ConditionalType != ConditionalType.NoEqual)))
                                                item.ConditionalType = ConditionalType.IsNullOrEmpty;
                                            else if (((item.FieldValue.IsNullOrEmpty() || item.FieldValue.Equals("[]")) && item.ConditionalType != ConditionalType.Equal))
                                                item.ConditionalType = ConditionalType.EqualNull;
                                            newValues.ConditionalList.Add(new KeyValuePair<WhereType, IConditionalModel>(jValues.ConditionalList[k].Key, item));
                                        }
                                    }
                                    else
                                    {
                                        newValues.ConditionalList.Add(jValues.ConditionalList[k]);
                                    }
                                }

                                iValues.ConditionalList[j] = new KeyValuePair<WhereType, IConditionalModel>(iValues.ConditionalList[j].Key, newValues);
                            }

                            cond.ConditionalList[i] = new KeyValuePair<WhereType, IConditionalModel>(cond.ConditionalList[i].Key, iValues);
                        }

                        newConditionalList.Add(cond);
                    }

                    quer.conditionalModelJson = newConditionalList.ToJsonStringOld();
                }
            }

            return querList.ToObject<T>();
        }
        #endregion

        return queryObj;
    }

    private async Task<string> GetQueryItemValue(string value)
    {
        switch (value)
        {
            case CommonConst.SysParamCurrentTime:
                value = DateTime.Now.ToString();
                break;
            case CommonConst.SysParamUserId:
                value = _userManager.UserId;
                break;
            case CommonConst.SysParamUserAndSub:
                value = _userManager.CurrentUserAndSubordinates.ToJsonStringOld();
                break;
            case CommonConst.SysParamUserTree:
                value = _userManager.CurrentUserTree.ToJsonStringOld();
                break;
            case CommonConst.SysParamPositionId:
                if (_userManager.CurrentPositionList.IsNullOrEmpty() || _userManager.CurrentPositionList.Equals("[]")) value = "jnpfNull";
                else value = _userManager.CurrentPositionList;
                break;
            case CommonConst.SysParamPositionAndSub:
                if (_userManager.CurrentPositionAndSuborganization.IsNullOrEmpty() || _userManager.CurrentPositionAndSuborganization.Equals("[]")) value = "jnpfNull";
                else value = _userManager.CurrentPositionAndSuborganization;
                break;
            case CommonConst.SysParamPositionTree:
                if (_userManager.CurrentPositionTree.IsNullOrEmpty() || _userManager.CurrentPositionTree.Equals("[]")) value = "jnpfNull";
                else value = _userManager.CurrentPositionTree;
                break;
            case CommonConst.SysParamOrganizeId:
                {
                    if (_userManager.CurrentOrganizeList.Any()) value = _userManager.CurrentOrganizeList.ToJsonString();
                    else value = "jnpfNull";
                }
                break;
            case CommonConst.SysParamOrganizeAndSub:
                {
                    if (_userManager.CurrentOrganizationAndSubOrganizations.Any()) value = _userManager.CurrentOrganizationAndSubOrganizations.ToJsonString();
                    else value = "jnpfNull";
                }
                break;
            case CommonConst.SysParamOrganizationTree:
                {
                    if (_userManager.CurrentOrganizationTree.Any()) value = _userManager.CurrentOrganizationTree.ToJsonString();
                    else value = "jnpfNull";
                }
                break;
        }

        return value;
    }

    #endregion

    /// <summary>
    /// 获取菜单名称,根据菜单Id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetModuleNameById(string id)
    {
        var res = _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => x.Id.Equals(id)).Select(x => x.FullName).First();
        if (res.IsNotEmptyOrNull()) return res.Replace("/", "_").Replace("\\", "_").Replace("|", "_").Replace(":", "_").Replace("?", "_");
        else return string.Empty;
    }

}