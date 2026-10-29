using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Interfaces;
using Mapster;
using Newtonsoft.Json.Linq;
using NPOI.Util;
using SqlSugar;
using System.Text;

namespace JNPF.VisualDev.Engine.Core;

/// <summary>
/// 模板表单列表数据解析.
/// </summary>
public class FormDataParsing : ITransient
{
    #region 构造

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 切库.
    /// </summary>
    private readonly IDataBaseManager _databaseService;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevEntity> _db;

    /// <summary>
    /// 构造.
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="cacheManager"></param>
    /// <param name="databaseService"></param>
    /// <param name="dataInterfaceService"></param>
    /// <param name="context"></param>
    public FormDataParsing(
        IUserManager userManager,
        ICacheManager cacheManager,
        IDataBaseManager databaseService,
        IDataInterfaceService dataInterfaceService,
        ISqlSugarRepository<VisualDevEntity> context)
    {
        _userManager = userManager;
        _cacheManager = cacheManager;
        _databaseService = databaseService;
        _dataInterfaceService = dataInterfaceService;
        _db = context;
    }
    #endregion

    #region 解析模板数据

    /// <summary>
    /// 控制模板数据转换.
    /// </summary>
    /// <param name="data">数据.</param>
    /// <param name="fieldsModel">数据模板.</param>
    /// <param name="actionType">操作类型(List-列表值,create-创建值,update-更新值,detail-详情值,transition-过渡值,query-查询).</param>
    /// <returns>object.</returns>
    public object TemplateControlsDataConversion(object data, FieldsModel fieldsModel, string? actionType = null)
    {
        if (fieldsModel == null || data == null || data.Equals("[]") || data.ToString().Equals("[]") || string.IsNullOrEmpty(data.ToString())) return string.Empty;
        try
        {
            object conversionData = new object();
            switch (fieldsModel.__config__.jnpfKey)
            {
                case JnpfKeyConst.SWITCH: // 开关
                    conversionData = data.ParseToInt();
                    break;
                case JnpfKeyConst.RATE: // 评分
                    conversionData = data.ParseToFloat();
                    break;
                case JnpfKeyConst.SLIDER: // 滑块
                    if (fieldsModel.range) conversionData = data.ToString().ToObject<List<object>>();
                    else conversionData = data.ParseToFloat();
                    break;
                case JnpfKeyConst.NUMINPUT: // 数字输入
                case JnpfKeyConst.CALCULATE: // 计算公式
                    if (data.ToString().Contains("."))
                    {
                        var dataList = data.ToString().Split('.');
                        if (fieldsModel.precision == 0 || fieldsModel.roundType == 3 || fieldsModel.roundType == 4)
                        {
                            conversionData = dataList.First();
                        }
                        else if (fieldsModel.precision.IsNullOrEmpty())
                        {
                            if (dataList.Last().TrimEnd('0').IsNullOrEmpty())
                            {
                                conversionData = dataList.First();
                            }
                            else
                            {
                                conversionData = dataList.First() + "." + dataList.Last().TrimEnd('0');
                            }
                        }
                        else
                        {
                            if (fieldsModel.precision > dataList.Last().Length)
                            {
                                conversionData = dataList.First() + "." + dataList.Last().PadRight((int)fieldsModel.precision, '0');
                            }
                            else
                            {
                                conversionData = dataList.First() + "." + dataList.Last().Substring(0, (int)fieldsModel.precision);
                            }
                        }
                    }
                    //else if (fieldsModel.precision > 0) conversionData = data.ToString() + ".".PadRight((int)fieldsModel.precision + 1, '0');
                    else conversionData = data;
                    break;
                case JnpfKeyConst.JNPFAMOUNT:
                    conversionData = data.ParseToDecimal(); // 金额输入
                    break;
                case JnpfKeyConst.CHECKBOX: // 多选框组
                    {
                        switch (actionType)
                        {
                            case "update":
                            case "create":
                                if (data.GetType().Name.ToLower().Equals("string")) conversionData = data.ToString();
                                else if (data.ToString().Contains("[")) conversionData = data.ToJsonStringOld();
                                else conversionData = data;
                                break;
                            default:
                                if (data.ToString().Contains("[")) conversionData = data.ToString().ToObject<List<object>>();
                                else conversionData = data;
                                break;
                        }
                    }

                    break;
                case JnpfKeyConst.SELECT: // 下拉选择
                    {
                        switch (actionType)
                        {
                            case "transition":
                                conversionData = data;
                                break;
                            case "update":
                            case "create":
                                if (data.GetType().Name.ToLower().Equals("string")) conversionData = data.ToString();
                                else conversionData = data;
                                break;
                            default:
                                if (fieldsModel.multiple && actionType != "query")
                                {
                                    if (data.ToString().Contains("[")) conversionData = data.ToString().ToObject<List<object>>();
                                    else if (data.ToString().Contains(",")) conversionData = string.Join(",", data.ToString().Split(',').ToArray());
                                    else conversionData = data.ToString();
                                }
                                else
                                {
                                    conversionData = data;
                                }

                                break;
                        }
                    }

                    break;
                case JnpfKeyConst.TIME: // 时间选择
                    {
                        switch (actionType)
                        {
                            case "update":
                            case "create":
                                var value = data.ToString().ParseToDateTime();
                                if (value.Equals(DateTime.MinValue))
                                    conversionData = data;
                                else
                                    conversionData = string.Format("{0:" + fieldsModel.format + "}", value);
                                break;
                            default:
                                conversionData = data;
                                break;
                        }
                    }

                    break;
                case JnpfKeyConst.DATE: // 日期选择
                case JnpfKeyConst.DATECALCULATE: // 日期公式
                    {
                        switch (actionType)
                        {
                            case "List":
                                DateTime dtDate;
                                if (DateTime.TryParse(data.ToString(), out dtDate))
                                    conversionData = data.ToString();
                                else
                                    conversionData = string.Format("{0:yyyy-MM-dd HH:mm:ss}", data.ToString().TimeStampToDateTime());
                                break;
                            case "create":
                                if (data.ParseToLong() != 0)
                                    conversionData = data.ToString().TimeStampToDateTime();
                                else
                                    conversionData = data;
                                break;
                            case "detail":
                                var newData = data.ParseToLong();
                                conversionData = newData != 0 ? newData : data;
                                break;
                            default:
                                try
                                {
                                    DateTime.Parse(data.ToString());
                                    conversionData = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", data.ToString());
                                }
                                catch
                                {
                                    conversionData = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", data.ToString().TimeStampToDateTime());
                                }
                                break;
                        }
                    }

                    break;
                case JnpfKeyConst.CREATETIME: // 创建时间
                case JnpfKeyConst.MODIFYTIME: // 修改时间
                    {
                        switch (actionType)
                        {
                            case "create":
                                conversionData = data.ToString();
                                break;
                            default:
                                DateTime dtDate;
                                if (DateTime.TryParse(data.ToString(), out dtDate))
                                    conversionData = data.ToString();
                                else
                                    conversionData = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", data.ToString().TimeStampToDateTime());
                                break;
                        }
                    }

                    break;
                case JnpfKeyConst.UPLOADFZ: // 文件上传
                    switch (actionType)
                    {
                        case "update":
                        case "create":
                            if (data.GetType().Name.ToLower().Equals("string")) conversionData = data.ToString();
                            else if (data.ToString().Contains("[")) conversionData = data.ToJsonString();
                            else conversionData = data;
                            break;
                        default:
                            if (data.ToJsonString() != "[]")
                            {
                                if (data is List<FileControlsModel>) conversionData = data.ToJsonString().ToObject<List<FileControlsModel>>();
                                else conversionData = data.ToString().ToObject<List<FileControlsModel>>();
                            }
                            else
                            {
                                conversionData = null;
                            }

                            break;
                    }

                    break;
                case JnpfKeyConst.UPLOADIMG: // 图片上传
                    {
                        switch (actionType)
                        {
                            case "update":
                            case "create":
                                if (data.GetType().Name.ToLower().Equals("string")) conversionData = data.ToString();
                                else if (data.ToString().Contains("[")) conversionData = data.ToJsonStringOld();
                                else conversionData = data;
                                break;
                            default:
                                if (data.ToJsonString() != "[]") conversionData = data.ToString().ToObject<List<FileControlsModel>>();
                                else conversionData = null;
                                break;
                        }
                    }

                    break;
                case JnpfKeyConst.ADDRESS: // 省市区联动
                    {
                        switch (actionType)
                        {
                            case "transition":
                                conversionData = data;
                                break;
                            case "update":
                            case "create":
                                if (data.GetType().Name.ToLower().Equals("string")) conversionData = data.ToString();
                                else if (data.ToString().Contains("[")) conversionData = data.ToJsonStringOld();
                                else conversionData = data;
                                break;
                            default:
                                conversionData = data.ToString().ToObject<List<object>>();
                                break;
                        }
                    }

                    break;
                case JnpfKeyConst.TABLE: // 设计子表
                    break;
                case JnpfKeyConst.CASCADER: // 级联
                    switch (actionType)
                    {
                        case "transition":
                            conversionData = data;
                            break;
                        case "update":
                        case "create":
                            if (data.GetType().Name.ToLower().Equals("string")) conversionData = data.ToString();
                            else if (data.ToString().Contains("[")) conversionData = data.ToJsonStringOld();
                            else conversionData = data;
                            break;
                        default:
                            {
                                if (fieldsModel.multiple) conversionData = data.ToString().ToObject<List<object>>();
                                else if (data.ToString().Contains("[")) conversionData = data.ToString().ToObject<List<object>>();
                                else conversionData = data;
                            }

                            break;
                    }
                    break;
                case JnpfKeyConst.COMSELECT: // 公司组件
                case JnpfKeyConst.GROUPSELECT: // 分组
                case JnpfKeyConst.ROLESELECT: // 角色
                case JnpfKeyConst.POSSELECT: // 岗位组件
                case JnpfKeyConst.USERSELECT: // 用户组件
                case JnpfKeyConst.USERSSELECT: // 新用户组件
                case JnpfKeyConst.TREESELECT: // 树形选择
                    {
                        switch (actionType)
                        {
                            case "transition":
                                conversionData = data;
                                break;
                            case "update":
                            case "create":
                                if (data.GetType().Name.ToLower().Equals("string")) conversionData = data.ToString();
                                else if (data.ToString().Contains("[")) conversionData = data.ToJsonStringOld();
                                else conversionData = data;
                                break;
                            default:
                                if (data.ToString().Contains('['))
                                    conversionData = data.ToString().ToObject<List<object>>();
                                else
                                    conversionData = data;

                                break;
                        }
                    }
                    break;
                case JnpfKeyConst.POPUPTABLESELECT: // 下拉表格
                    {
                        switch (actionType)
                        {
                            case "transition":
                                conversionData = data;

                                break;
                            case "update":
                            case "create":
                                if (data.GetType().Name.ToLower().Equals("string")) conversionData = data;
                                else if (data.ToString().Contains("[")) conversionData = data.ToJsonStringOld();
                                else conversionData = data;
                                break;
                            default:
                                if (fieldsModel.multiple && data.ToString().Contains("["))
                                    conversionData = data.ToString().ToObject<List<object>>();
                                else
                                    conversionData = data;

                                break;
                        }
                    }

                    break;
                case JnpfKeyConst.RELATIONFORM:
                    conversionData = data.ToString();
                    break;
                default:
                    conversionData = data;
                    break;
            }

            return conversionData;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 获取有表单条数据.
    /// </summary>
    /// <param name="dataList"></param>
    /// <param name="fieldsModels"></param>
    /// <param name="actionType"></param>
    /// <returns></returns>
    public List<Dictionary<string, object>> GetTableDataInfo(List<Dictionary<string, object>> dataList, List<FieldsModel> fieldsModels, string actionType)
    {
        // 转换表字符串成数组
        foreach (var dataMap in dataList)
        {
            int dicCount = dataMap.Keys.Count;
            string[] strKey = new string[dicCount];
            dataMap.Keys.CopyTo(strKey, 0);
            for (int i = 0; i < strKey.Length; i++)
            {
                var dataValue = dataMap[strKey[i]];
                if (dataValue != null && !string.IsNullOrEmpty(dataValue.ToString()))
                {
                    var model = fieldsModels.Find(f => f.__vModel__.Equals(strKey[i]));
                    if (model != null)
                    {
                        dataMap[strKey[i]] = TemplateControlsDataConversion(dataMap[strKey[i]], model, actionType);
                    }
                }
                else if (JnpfKeyConst.UPLOADFZ.Equals(fieldsModels.Find(f => f.__vModel__.Equals(strKey[i]))?.__config__.jnpfKey) && dataValue.IsNullOrEmpty())
                {
                    dataMap[strKey[i]] = new List<object>();
                }
            }
        }
        return dataList;
    }

    /// <summary>
    /// 解析处理 Sql 插入数据的 value.
    /// </summary>
    /// <param name="dbType">数据库 类型.</param>
    /// <param name="_tableList">表数据.</param>
    /// <param name="field">前端字段名.</param>
    /// <param name="data">插入的数据.</param>
    /// <param name="_fieldsModelList">控件集合.</param>
    /// <param name="actionType">操作类型.</param>
    /// <param name="isShortLink">是否外链.</param>
    /// <returns>string.</returns>
    public object InsertValueHandle(string dbType, List<DbTableFieldModel> _tableList, string field, object data, List<FieldsModel> _fieldsModelList, string actionType = "create", bool isShortLink = false)
    {
        // 根据KEY查找模板
        FieldsModel? model = _fieldsModelList.Find(f => f.__vModel__ == field);

        // 单独处理 Oracle,Kdbndp 日期格式转换
        if (!actionType.Equals("create") && dbType == "Oracle" && _tableList.Find(x => x.dataType.ToLower() == "datetime" && x.field == field.ReplaceRegex(@"(\w+)_jnpf_", string.Empty)) != null)
        {
            return string.Format("to_date('{0}','yyyy-mm-dd HH24/MI/SS')", TemplateControlsDataConversion(data, model, actionType));
        }
        else if (!actionType.Equals("create") && dbType == "Oracle" && _tableList.Any(x => x.dataLength == "2147483647" && x.field == field.ReplaceRegex(@"(\w+)_jnpf_", string.Empty)) && data?.ToString().Length > 4000)
        {
            // Oracle 超过4000长度的长文本类型
            var res = string.Empty;
            for (int i = 0; i < data.ToString().Length / 4000; i++)
            {
                if (i == 0)
                    res = string.Format("to_clob('{0}')", data.ToString().Substring(i, 4000));
                else
                    res = string.Format("{0}||to_clob('{1}')", res, data.ToString().Substring(i * 4000, 4000));
            }
            return res;
        }
        else
        {
            var res = TemplateControlsDataConversion(data, model, actionType);
            if (actionType.Equals("create"))
            {
                if (isShortLink && model.__config__.jnpfKey.Equals(JnpfKeyConst.CREATETIME))
                    return null;
                else if (model.__config__.jnpfKey.Equals(JnpfKeyConst.CREATETIME) || model.__config__.jnpfKey.Equals(JnpfKeyConst.DATE) || model.__config__.jnpfKey.Equals(JnpfKeyConst.DATECALCULATE))
                    return res.ToString().ParseToDateTime();
                else if (model.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) || model.__config__.jnpfKey.Equals(JnpfKeyConst.RATE) || model.__config__.jnpfKey.Equals(JnpfKeyConst.SLIDER) || model.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE))
                    return res.ToString().ParseToDecimal();
                else return res.ToString();
            }
            else
            {
                res = res.IsNullOrEmpty() ? "null" : string.Format("'{0}'", res.ToString().Replace("'", "''"));
                return res;
            }
        }
    }
    #endregion

    #region 缓存模板数据

    /// <summary>
    /// 获取可视化开发模板可缓存数据.
    /// </summary>
    /// <param name="moldelId">模板id.</param>
    /// <param name="formData">模板数据结构.</param>
    /// <returns>控件缓存数据.</returns>
    public async Task<Dictionary<string, object>> GetVisualDevCaCheData(string moldelId, List<FieldsModel> formData)
    {
        Dictionary<string, object> templateData = new Dictionary<string, object>();
        string? cacheKey = CommonConst.VISUALDEV + _userManager.TenantId + "_" + moldelId + "_";

        // 获取或设置控件缓存数据
        foreach (FieldsModel? model in formData)
        {
            if (model != null && model.__vModel__ != null)
            {
                ConfigModel configModel = model.__config__;
                string fieldCacheKey = cacheKey + configModel.renderKey + "_" + model.__vModel__;
                switch (configModel.jnpfKey)
                {
                    case JnpfKeyConst.RADIO: // 单选框
                    case JnpfKeyConst.SELECT: // 下拉框
                    case JnpfKeyConst.CHECKBOX: // 复选框
                        {
                            var list = new List<Dictionary<string, string>>();
                            switch (configModel.dataType)
                            {
                                case "dynamic":
                                    if (configModel.useCache)
                                    {
                                        if (!GetCacheValues(fieldCacheKey, templateData))
                                        {
                                            list = await GetDynamicList(model);
                                            templateData.Add(fieldCacheKey, list);
                                            _cacheManager.Set(fieldCacheKey, list, TimeSpan.FromMinutes(3));
                                        }
                                    }
                                    else
                                    {
                                        list = await GetDynamicList(model);
                                        templateData.Add(fieldCacheKey, list);
                                    }

                                    break;
                                case "dictionary":
                                    if (!GetCacheValues(fieldCacheKey, templateData))
                                    {
                                        list = await GetDictionaryList(configModel.dictionaryType);
                                        templateData.Add(fieldCacheKey, list);
                                        _cacheManager.Set(fieldCacheKey, list, TimeSpan.FromMinutes(3));
                                    }

                                    break;
                                case "static":
                                    if (!GetCacheValues(fieldCacheKey, templateData))
                                    {
                                        if (model.__slot__ != null && model.options != null && model.options.Any())
                                        {
                                            foreach (Dictionary<string, object>? item in model.options)
                                            {
                                                if (item[model.props.value].IsNotEmptyOrNull())
                                                {
                                                    Dictionary<string, string> option = new Dictionary<string, string>();
                                                    option.Add(item[model.props.value].ToString(), item[model.props.label].ToString());
                                                    list.Add(option);
                                                }
                                            }
                                        }
                                        else if (model.options != null && model.options.Any())
                                        {
                                            foreach (Dictionary<string, object>? item in model.options.ToObject<List<Dictionary<string, object>>>())
                                            {
                                                if (item[model.props.value].IsNotEmptyOrNull())
                                                {
                                                    Dictionary<string, string> option = new Dictionary<string, string>();
                                                    option.Add(item[model.props.value].ToString(), item[model.props.label].ToString());
                                                    list.Add(option);
                                                }
                                            }
                                        }
                                        templateData.Add(fieldCacheKey, list);
                                        _cacheManager.Set(fieldCacheKey, list, TimeSpan.FromMinutes(3));
                                    }

                                    break;
                            }
                        }

                        break;
                    case JnpfKeyConst.TREESELECT: // 树形选择
                    case JnpfKeyConst.CASCADER: // 级联选择
                        {
                            var list = new List<Dictionary<string, string>>();
                            switch (configModel.dataType)
                            {
                                case "dynamic":
                                    if (configModel.useCache)
                                    {
                                        if (!GetCacheValues(fieldCacheKey, templateData))
                                        {
                                            list = await GetDynamicList(model);
                                            templateData.Add(fieldCacheKey, list);
                                            _cacheManager.Set(fieldCacheKey, list, TimeSpan.FromMinutes(3));
                                        }
                                    }
                                    else
                                    {
                                        list = await GetDynamicList(model);
                                        templateData.Add(fieldCacheKey, list);
                                    }

                                    break;
                                case "dictionary":
                                    if (!GetCacheValues(fieldCacheKey, templateData))
                                    {
                                        list = await GetDictionaryList(configModel.dictionaryType);
                                        templateData.Add(fieldCacheKey, list);
                                        _cacheManager.Set(fieldCacheKey, list, TimeSpan.FromMinutes(3));
                                    }

                                    break;
                                case "static":
                                    if (!GetCacheValues(fieldCacheKey, templateData))
                                    {
                                        list = GetStaticList(model);
                                        templateData.Add(fieldCacheKey, list);
                                        _cacheManager.Set(fieldCacheKey, list, TimeSpan.FromMinutes(3));
                                    }

                                    break;
                            }
                        }

                        break;
                    case JnpfKeyConst.DICTIONARY: // 数据字典
                        if (!GetCacheValues(fieldCacheKey, templateData))
                        {
                            List<Dictionary<string, string>>? vlist = await GetDictionaryList();
                            templateData.Add(fieldCacheKey, vlist);
                            _cacheManager.Set(fieldCacheKey, vlist, TimeSpan.FromMinutes(3));
                        }

                        break;
                    case JnpfKeyConst.GROUPSELECT: // 分组
                        if (!GetCacheValues(fieldCacheKey, templateData))
                        {
                            List<GroupEntity>? positionEntityList = await _db.AsSugarClient().Queryable<GroupEntity>().ToListAsync();
                            List<Dictionary<string, string>> positionList = new List<Dictionary<string, string>>();
                            foreach (GroupEntity? item in positionEntityList)
                            {
                                Dictionary<string, string> position = new Dictionary<string, string>();
                                position.Add(item.Id, item.FullName);
                                positionList.Add(position);
                            }

                            templateData.Add(fieldCacheKey, positionList);
                            _cacheManager.Set(fieldCacheKey, positionList, TimeSpan.FromMinutes(3));
                        }

                        break;
                    case JnpfKeyConst.ROLESELECT: // 角色
                        if (!GetCacheValues(fieldCacheKey, templateData))
                        {
                            List<RoleEntity>? positionEntityList = await _db.AsSugarClient().Queryable<RoleEntity>().ToListAsync();
                            List<Dictionary<string, string>> positionList = new List<Dictionary<string, string>>();
                            foreach (RoleEntity? item in positionEntityList)
                            {
                                Dictionary<string, string> position = new Dictionary<string, string>();
                                position.Add(item.Id, item.FullName);
                                positionList.Add(position);
                            }

                            templateData.Add(fieldCacheKey, positionList);
                            _cacheManager.Set(fieldCacheKey, positionList, TimeSpan.FromMinutes(3));
                        }

                        break;
                }
            }
        }

        #region 省市区 单独处理

        if (formData.Where(x => x.__config__.jnpfKey == JnpfKeyConst.ADDRESS).Any())
        {
            var addCacheKey = CommonConst.VISUALDEV + "_address";
            if (_cacheManager.Exists(addCacheKey))
            {
                templateData.Add(addCacheKey, _cacheManager.Get(addCacheKey).ToObject<List<Dictionary<string, object>>>());
            }
            else
            {
                List<ProvinceEntity>? addressEntityList = await _db.AsSugarClient().Queryable<ProvinceEntity>().Select(x => new ProvinceEntity { Id = x.Id, ParentId = x.ParentId, Type = x.Type, FullName = x.FullName }).ToListAsync();

                // 处理省市区树
                addressEntityList.Where(x => x.Type == "1").ToList().ForEach(item => item.QuickQuery = item.FullName);
                addressEntityList.Where(x => x.Type == "2").ToList().ForEach(item => item.QuickQuery = addressEntityList.Find(x => x.Id == item.ParentId).QuickQuery + "/" + item.FullName);
                addressEntityList.Where(x => x.Type == "3").ToList().ForEach(item => item.QuickQuery = addressEntityList.Find(x => x.Id == item.ParentId).QuickQuery + "/" + item.FullName);
                addressEntityList.Where(x => x.Type == "4").ToList().ForEach(item =>
                {
                    ProvinceEntity? it = addressEntityList.Find(x => x.Id == item.ParentId);
                    if (it != null) item.QuickQuery = it.QuickQuery + "/" + item.FullName;
                });

                // 分开 省市区街道 数据
                List<Dictionary<string, string>> addressList = new List<Dictionary<string, string>>();
                foreach (ProvinceEntity? item in addressEntityList.Where(x => x.Type == "1").ToList())
                {
                    Dictionary<string, string> address = new Dictionary<string, string>();
                    address.Add(item.Id, item.QuickQuery);
                    addressList.Add(address);
                }

                foreach (ProvinceEntity? item in addressEntityList.Where(x => x.Type == "2").ToList())
                {
                    Dictionary<string, string> address = new Dictionary<string, string>();
                    address.Add(item.Id, item.QuickQuery);
                    addressList.Add(address);
                }

                foreach (ProvinceEntity? item in addressEntityList.Where(x => x.Type == "3").ToList())
                {
                    Dictionary<string, string> address = new Dictionary<string, string>();
                    address.Add(item.Id, item.QuickQuery);
                    addressList.Add(address);
                }

                foreach (ProvinceEntity? item in addressEntityList.Where(x => x.Type == "4").ToList())
                {
                    Dictionary<string, string> address = new Dictionary<string, string>();
                    address.Add(item.Id, item.QuickQuery);
                    addressList.Add(address);
                }

                var noTypeList = addressEntityList.Where(x => x.Type.IsNullOrWhiteSpace()).ToList();
                foreach (ProvinceEntity? item in noTypeList) item.QuickQuery = GetAddressByPList(noTypeList, item);
                foreach (ProvinceEntity? item in noTypeList)
                {
                    Dictionary<string, string> address = new Dictionary<string, string>();
                    address.Add(item.Id, item.QuickQuery);
                    addressList.Add(address);
                }

                // 缓存七天
                _cacheManager.Set(addCacheKey, addressList, TimeSpan.FromDays(7));
                templateData.Add(addCacheKey, addressList);
            }
        }
        #endregion

        #region 用户单独处理
        if (formData.Where(x => x.__config__.jnpfKey == JnpfKeyConst.USERSELECT).Any())
        {
            string? userCacheKey = CommonConst.VISUALDEV + "_userSelect";
            if (_cacheManager.Exists(userCacheKey))
            {
                templateData.Add(userCacheKey, _cacheManager.Get(userCacheKey).ToObject<List<Dictionary<string, object>>>());
            }
            else
            {
                List<UserEntity>? userEntityList = await _db.AsSugarClient().Queryable<UserEntity>().Select(x => new UserEntity() { Id = x.Id, RealName = x.RealName, Account = x.Account }).ToListAsync();

                List<Dictionary<string, string>> userList = new List<Dictionary<string, string>>();
                foreach (UserEntity? item in userEntityList)
                {
                    Dictionary<string, string> user = new Dictionary<string, string>();
                    user.Add(item.Id, item.RealName + "/" + item.Account);
                    userList.Add(user);
                }

                // 缓存30分钟
                _cacheManager.Set(userCacheKey, userList, TimeSpan.FromMinutes(30));
                templateData.Add(userCacheKey, userList);
            }
        }

        if (formData.Where(x => x.__config__.jnpfKey == JnpfKeyConst.USERSSELECT).Any())
        {
            string? userCacheKey = CommonConst.VISUALDEV + "_usersSelect";
            if (_cacheManager.Exists(userCacheKey))
            {
                templateData.Add(userCacheKey, _cacheManager.Get(userCacheKey).ToObject<List<Dictionary<string, object>>>());
            }
            else
            {
                var addList = new List<Dictionary<string, string>>();
                (await _db.AsSugarClient().Queryable<UserEntity>().Select(x => new { x.Id, x.RealName, x.Account }).ToListAsync()).ForEach(item =>
                {
                    Dictionary<string, string> user = new Dictionary<string, string>();
                    user.Add(item.Id + "--user", item.RealName + "/" + item.Account);
                    addList.Add(user);
                });
                (await _db.AsSugarClient().Queryable<OrganizeEntity>().Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item =>
                {
                    Dictionary<string, string> user = new Dictionary<string, string>();
                    user.Add(item.Id + "--company", item.FullName);
                    user.Add(item.Id + "--department", item.FullName);
                    addList.Add(user);
                });
                (await _db.AsSugarClient().Queryable<RoleEntity>().Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item =>
                {
                    Dictionary<string, string> user = new Dictionary<string, string>();
                    user.Add(item.Id + "--role", item.FullName);
                    addList.Add(user);
                });
                (await _db.AsSugarClient().Queryable<PositionEntity>().Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item =>
                {
                    Dictionary<string, string> user = new Dictionary<string, string>();
                    user.Add(item.Id + "--position", item.FullName);
                    addList.Add(user);
                });
                (await _db.AsSugarClient().Queryable<GroupEntity>().Select(x => new { x.Id, x.FullName }).ToListAsync()).ForEach(item =>
                {
                    Dictionary<string, string> user = new Dictionary<string, string>();
                    user.Add(item.Id + "--group", item.FullName);
                    addList.Add(user);
                });

                // 缓存30分钟
                _cacheManager.Set(userCacheKey, addList, TimeSpan.FromMinutes(30));
                templateData.Add(userCacheKey, addList);
            }
        }
        #endregion

        #region 组织单独处理
        if (formData.Where(x => x.__config__.jnpfKey == JnpfKeyConst.COMSELECT || x.__config__.jnpfKey == JnpfKeyConst.CURRORGANIZE).Any())
        {
            await GetOrgList(templateData);
        }
        #endregion

        #region 岗位单独处理
        if (formData.Where(x => x.__config__.jnpfKey == JnpfKeyConst.POSSELECT || x.__config__.jnpfKey == JnpfKeyConst.CURRPOSITION).Any())
        {
            var posCacheKey = CommonConst.VISUALDEV + "_position";
            if (!GetCacheValues(posCacheKey, templateData))
            {
                var orgList = new Dictionary<string, object>();
                await GetOrgList(orgList);
                var orgData = orgList.FirstOrDefault().Value.ToObject<List<Dictionary<string, string>>>();

                var positionList = await _db.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null).ToListAsync();
                var posDic = new List<Dictionary<string, string>>();
                foreach (var pos in positionList)
                {
                    var orgName = orgData.Find(x => x.ContainsKey(pos.OrganizeId))?.FirstOrDefault().Value;
                    if (orgName.IsNotEmptyOrNull())
                    {
                        var posName = string.Format("{0}/{1}", orgName, pos.FullName);
                        posDic.Add(new Dictionary<string, string>() { { pos.Id, posName } });
                    }
                }

                templateData.Add(posCacheKey, posDic);
                _cacheManager.Set(posCacheKey, posDic, TimeSpan.FromMinutes(30));
            }
        }
        #endregion

        return templateData;
    }

    /// <summary>
    /// 获取缓存 根据控件key.
    /// </summary>
    /// <param name="fieldCacheKey"></param>
    /// <param name="templateData"></param>
    /// <returns></returns>
    private bool GetCacheValues(string fieldCacheKey, Dictionary<string, object> templateData)
    {
        if (_cacheManager.Exists(fieldCacheKey))
        {
            List<Dictionary<string, object>>? list = _cacheManager.Get(fieldCacheKey).ToObject<List<Dictionary<string, object>>>();
            templateData.Add(fieldCacheKey, list);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 处理组织信息.
    /// </summary>
    /// <param name="templateData"></param>
    /// <returns></returns>
    private async Task GetOrgList(Dictionary<string, object> templateData)
    {
        var cacheKey = CommonConst.VISUALDEV + "_organize";
        if (!GetCacheValues(cacheKey, templateData))
        {
            var orgEntityList = await _db.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null).ToListAsync();
            var vlist = new List<Dictionary<string, string>>();
            foreach (var item in orgEntityList)
            {
                var organize = new Dictionary<string, string>() { { item.Id, item.OrganizeNameTree } };
                vlist.Add(organize);
            }

            templateData.Add(cacheKey, vlist);
            _cacheManager.Set(cacheKey, vlist, TimeSpan.FromMinutes(3));
        }
    }

    /// <summary>
    /// 处理远端数据.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<List<Dictionary<string, string>>> GetDynamicList(FieldsModel model)
    {
        List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();

        // 获取远端数据
        DataInterfaceEntity? dynamic = await _dataInterfaceService.GetInfo(model.interfaceId.IsNotEmptyOrNull() ? model.interfaceId : model.__config__.propsUrl);
        if (dynamic == null) return list;

        var propsValue = string.Empty;
        var propsLabel = string.Empty;
        var children = string.Empty;
        if (model.props != null)
        {
            propsValue = model.props.value;
            propsLabel = model.props.label;
            children = model.props.children;
        }

        var input = new DataInterfacePreviewInput()
        {
            pageSize = 999999,
            currentPage = 1,
            keyword = string.Empty,
            tenantId = _userManager.TenantId,
            paramList = model.__config__.templateJson != null ? model.__config__.templateJson.Adapt<List<DataInterfaceParameter>>() : null,
        };

        var result = new object();
        try
        {
            result = await _dataInterfaceService.GetResponseByType(model.interfaceId.IsNotEmptyOrNull() ? model.interfaceId : model.__config__.propsUrl, 0, input);
        }
        catch
        {

        }

        if (result.IsNotEmptyOrNull())
        {
            var resList = new List<Dictionary<string, object>>();
            if (result.ToJsonString().Trim().First().Equals('['))
            {
                resList = result.ToObject<List<Dictionary<string, object>>>();
            }
            else
            {
                var data = result.ToObject<Dictionary<string, object>>();
                resList = data.ContainsKey("list") ? data["list"].ToObject<List<Dictionary<string, object>>>() : new List<Dictionary<string, object>>();
            }
            if (model.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT) || model.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT))
            {
                foreach (Dictionary<string, object>? item in resList)
                {
                    Dictionary<string, string> dynamicDic = new Dictionary<string, string>();
                    foreach (var it in item) dynamicDic.Add(it.Key, it.Value?.ToString());
                    list.Add(dynamicDic);
                }

                return list;
            }
            foreach (Dictionary<string, object>? item in resList)
            {
                Dictionary<string, string> dynamicDic = new Dictionary<string, string>();
                dynamicDic.Add(item[propsValue]?.ToString(), item[propsLabel]?.ToString());
                list.Add(dynamicDic);
                if (children != null && item.ContainsKey(children) && item[children].IsNotEmptyOrNull())
                    list.AddRange(GetDynamicInfiniteData(item[children].ToJsonString(), model.props));
            }
        }

        return list;
    }

    /// <summary>
    /// 处理静态数据.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private List<Dictionary<string, string>> GetStaticList(FieldsModel model)
    {
        PropsBeanModel? props = model.props;
        List<OptionsModel>? optionList = GetTreeOptions(model.options, props);
        List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
        foreach (OptionsModel? item in optionList)
        {
            Dictionary<string, string> option = new Dictionary<string, string>();
            option.Add(item.value, item.label);
            list.Add(option);
        }

        return list;
    }

    /// <summary>
    /// 获取数据字典数据 根据 类型Id.
    /// </summary>
    /// <param name="dictionaryTypeId"></param>
    /// <returns>List.</returns>
    private async Task<List<Dictionary<string, string>>> GetDictionaryList(string? dictionaryTypeId = null)
    {
        List<DictionaryDataEntity> dictionaryDataEntityList = await _db.AsSugarClient().Queryable<DictionaryDataEntity, DictionaryTypeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.DictionaryTypeId))
            .WhereIF(dictionaryTypeId.IsNotEmptyOrNull(), (a, b) => b.Id == dictionaryTypeId || b.EnCode == dictionaryTypeId).ToListAsync();

        List<Dictionary<string, string>> dictionaryDataList = new List<Dictionary<string, string>>();
        foreach (DictionaryDataEntity? item in dictionaryDataEntityList)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add(item.Id, item.FullName);
            dictionary.Add(item.EnCode, item.FullName);
            dictionaryDataList.Add(dictionary);
        }

        return dictionaryDataList;
    }

    /// <summary>
    /// options无限级.
    /// </summary>
    /// <returns></returns>
    private List<OptionsModel> GetTreeOptions(List<Dictionary<string, object>> model, PropsBeanModel props)
    {
        List<OptionsModel> options = new List<OptionsModel>();
        foreach (object? item in model)
        {
            OptionsModel option = new OptionsModel();
            Dictionary<string, object>? dicObject = item.ToJsonString().ToObject<Dictionary<string, object>>();
            option.label = dicObject[props.label].ToString();
            option.value = dicObject[props.value].ToString();
            if (dicObject.ContainsKey(props.children))
            {
                List<Dictionary<string, object>>? children = dicObject[props.children].ToJsonString().ToObject<List<Dictionary<string, object>>>();
                options.AddRange(GetTreeOptions(children, props));
            }

            options.Add(option);
        }

        return options;
    }

    /// <summary>
    /// 递归获取手动添加的省市区,名称处理成树形结构.
    /// </summary>
    /// <param name="addressEntityList"></param>
    private string GetAddressByPList(List<ProvinceEntity> addressEntityList, ProvinceEntity pEntity)
    {
        if (pEntity.ParentId == null || pEntity.ParentId.Equals("-1"))
        {
            return pEntity.FullName;
        }
        else
        {
            var pItem = addressEntityList.Find(x => x.Id == pEntity.ParentId);
            if (pItem != null) pEntity.QuickQuery = GetAddressByPList(addressEntityList, pItem) + "/" + pEntity.FullName;
            else pEntity.QuickQuery = pEntity.FullName;
            return pEntity.QuickQuery;
        }
    }

    #endregion

    #region 系统组件生成与解析

    /// <summary>
    /// 将系统组件生成的数据转换为数据.
    /// </summary>
    /// <param name="modelId">模板id.</param>
    /// <param name="formData">表单模板.</param>
    /// <param name="modelData">真实数据.</param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetSystemComponentsData(string modelId, List<FieldsModel> formData, string modelData)
    {
        // 获取控件缓存数据
        Dictionary<string, object> templateData = await GetVisualDevCaCheData(modelId, formData);

        Dictionary<string, object> dataMap = modelData.ToObject<Dictionary<string, object>>(); // 数据库保存的F_Data

        // 序列化后时间戳转换处理
        List<FieldsModel>? timeList = formData.Where(x => x.__config__.jnpfKey == JnpfKeyConst.CREATETIME || x.__config__.jnpfKey == JnpfKeyConst.MODIFYTIME).ToList();
        if (timeList.Any())
        {
            timeList.ForEach(item =>
            {
                if (dataMap.ContainsKey(item.__vModel__) && dataMap[item.__vModel__] != null)
                {
                    string value = dataMap[item.__vModel__].ToString();
                    if (value.IsNotEmptyOrNull())
                    {
                        dataMap.Remove(item.__vModel__);
                        DateTime dtDate;
                        if (!DateTime.TryParse(value, out dtDate)) value = string.Format("{0:yyyy-MM-dd HH:mm:ss}", value.TimeStampToDateTime());
                        else value = string.Format("{0:yyyy-MM-dd HH:mm:ss}", value.ParseToDateTime());
                        dataMap.Add(item.__vModel__, value);
                    }
                }
            });
        }

        int dicCount = dataMap.Keys.Count;
        string[] strKey = new string[dicCount];
        dataMap.Keys.CopyTo(strKey, 0);

        // 自动生成的数据不在模板数据内
        foreach (string? key in dataMap.Keys.ToList())
        {
            FieldsModel? model = formData.Where(f => f.__vModel__ == key).FirstOrDefault();
            if (model == null) continue;
            if (dataMap[key].IsNotEmptyOrNull() && model != null)
            {
                ConfigModel configModel = model.__config__;
                if (string.IsNullOrWhiteSpace(model.separator)) model.separator = ",";
                switch (configModel.jnpfKey)
                {
                    case JnpfKeyConst.UPLOADFZ: // 文件上传
                    case JnpfKeyConst.UPLOADIMG: // 图片上传
                        dataMap[key] = dataMap[key] == null ? new List<object>().ToJsonString() : dataMap[key];
                        break;
                    case JnpfKeyConst.CREATEUSER:
                    case JnpfKeyConst.MODIFYUSER:
                        dataMap[key] = await _db.AsSugarClient().Queryable<UserEntity>().Where(x => x.Id.Equals(dataMap[key])).Select(x => SqlFunc.MergeString(x.RealName, "/", x.Account)).FirstAsync();
                        break;
                    case JnpfKeyConst.CURRPOSITION:
                        {
                            var currPositionTemplateValue = templateData.FirstOrDefault(t => t.Key.Equals(CommonConst.VISUALDEV + "_position")).Value?.ToJsonString().ToObject<List<Dictionary<string, string>>>();
                            dataMap[key] = ConversionName(currPositionTemplateValue, dataMap[key]);
                        }

                        break;
                    case JnpfKeyConst.CURRORGANIZE:
                        {
                            var currOrganizeTemplateValue = templateData.FirstOrDefault(t => t.Key.Equals(CommonConst.VISUALDEV + "_organize")).Value?.ToJsonString().ToObject<List<Dictionary<string, string>>>();
                            dataMap[key] = ConversionName(currOrganizeTemplateValue, dataMap[key]);
                        }

                        break;
                    case JnpfKeyConst.TABLE:
                        var dic = new List<Dictionary<string, object>>();
                        foreach (var childList in dataMap[key].ToObject<List<Dictionary<string, object>>>())
                        {
                            foreach (var child in childList)
                            {
                                var cModel = model.__config__.children.Find(it => it.__vModel__ == child.Key);
                                if (cModel.IsNotEmptyOrNull() && child.Value.IsNotEmptyOrNull())
                                {
                                    switch (cModel.__config__.jnpfKey)
                                    {
                                        case JnpfKeyConst.CREATEUSER:
                                        case JnpfKeyConst.MODIFYUSER:
                                            childList[child.Key] = await _db.AsSugarClient().Queryable<UserEntity>().Where(x => x.Id.Equals(childList[child.Key])).Select(x => SqlFunc.MergeString(x.RealName, "/", x.Account)).FirstAsync();
                                            break;
                                        case JnpfKeyConst.CURRPOSITION:
                                            {
                                                var currPositionTemplateValue = templateData.FirstOrDefault(t => t.Key.Equals(CommonConst.VISUALDEV + "_position")).Value?.ToJsonString().ToObject<List<Dictionary<string, string>>>();
                                                childList[child.Key] = ConversionName(currPositionTemplateValue, child.Value);
                                            }

                                            break;
                                        case JnpfKeyConst.CURRORGANIZE:
                                            {
                                                var currOrganizeTemplateValue = templateData.FirstOrDefault(t => t.Key.Equals(CommonConst.VISUALDEV + "_organize")).Value?.ToJsonString().ToObject<List<Dictionary<string, string>>>();
                                                childList[child.Key] = ConversionName(currOrganizeTemplateValue, child.Value);
                                            }

                                            break;
                                    }
                                }
                            }

                            dic.Add(childList);
                        }

                        dataMap[key] = dic;

                        break;
                }
            }
            else
            {
                // 前端空数组提交 转换 .
                switch (model.__config__.jnpfKey)
                {
                    case JnpfKeyConst.CASCADER:
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.UPLOADFZ: // 文件上传
                    case JnpfKeyConst.UPLOADIMG: // 图片上传
                        dataMap[key] = dataMap[key] == null ? new List<object>() : dataMap[key];
                        break;

                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.ADDRESS:
                    case JnpfKeyConst.GROUPSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.TREESELECT:
                    case JnpfKeyConst.USERSELECT:
                    case JnpfKeyConst.POPUPTABLESELECT:
                        if (model.multiple) dataMap[key] = dataMap[key] == null ? new List<object>() : dataMap[key];
                        break;
                }
            }
        }

        return dataMap;
    }

    #endregion

    #region 列表转换数据(Id 转 Name)

    /// <summary>
    /// 将关键字key查询传输的id转换成名称，还有动态数据id成名称.
    /// </summary>
    /// <param name="modelId">模板id.</param>
    /// <param name="formData">数据库模板数据.</param>
    /// <param name="list">真实数据.</param>
    /// <param name="columnDesign"></param>
    /// <param name="actionType"></param>
    /// <param name="webType">表单类型1-纯表单、2-普通表单、3-工作流表单.</param>
    /// <param name="primaryKey">数据主键.</param>
    /// <param name="isShortLink">是否外链.</param>
    /// <param name="isConvertData">是否转换数据0-转换、1-不转换（有用于定位来区分 列表、详情）.</param>
    /// <returns></returns>
    public async Task<List<Dictionary<string, object>>> GetKeyData(
        string modelId,
        List<FieldsModel> formData,
        List<Dictionary<string, object>> list,
        ColumnDesignModel? columnDesign = null,
        string actionType = "List",
        int webType = 2,
        string primaryKey = "f_id",
        bool isShortLink = false,
        int? isConvertData = null)
    {
        if (isShortLink)
        {
            formData = formData.Where(x => x.__config__.jnpfKey == JnpfKeyConst.COMINPUT || x.__config__.jnpfKey == JnpfKeyConst.TEXTAREA
                || x.__config__.jnpfKey == JnpfKeyConst.NUMINPUT
                || x.__config__.jnpfKey == JnpfKeyConst.SWITCH
                || (x.__config__.jnpfKey == JnpfKeyConst.RADIO && x.__config__.dataType.Equals("static"))
                || (x.__config__.jnpfKey == JnpfKeyConst.CHECKBOX && x.__config__.dataType.Equals("static"))
                || (x.__config__.jnpfKey == JnpfKeyConst.SELECT && x.__config__.dataType.Equals("static"))
                || (x.__config__.jnpfKey == JnpfKeyConst.CASCADER && x.__config__.dataType.Equals("static"))
                || (x.__config__.jnpfKey == JnpfKeyConst.TREESELECT && x.__config__.dataType.Equals("static"))
                || x.__config__.jnpfKey == JnpfKeyConst.DATE || x.__config__.jnpfKey == JnpfKeyConst.TIME || x.__config__.jnpfKey == JnpfKeyConst.COLORPICKER
                || x.__config__.jnpfKey == JnpfKeyConst.RATE || x.__config__.jnpfKey == JnpfKeyConst.SLIDER || x.__config__.jnpfKey == JnpfKeyConst.EDITOR
                || x.__config__.jnpfKey == JnpfKeyConst.LINK || x.__config__.jnpfKey == JnpfKeyConst.JNPFTEXT || x.__config__.jnpfKey == JnpfKeyConst.ALERT
                || x.__config__.jnpfKey == JnpfKeyConst.LOCATION)
                .Where(x => !x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT)).ToList();
        }

        // 获取控件缓存数据
        var templateData = new Dictionary<string, object>();
        if (list.IsNotEmptyOrNull() && list.Any()) templateData = await GetVisualDevCaCheData(modelId, formData);

        // 转换数据
        Dictionary<string, object>? convData = new Dictionary<string, object>();

        // 存放 预缓存数据的控件的缓存数据 ， 避免循环时重复序列化 耗资源
        List<Dictionary<string, string>>? addressTemplateValue = new List<Dictionary<string, string>>(); // 地址 省市区 缓存
        List<Dictionary<string, string>>? comselectTemplateValue = new List<Dictionary<string, string>>(); // 组织
        List<Dictionary<string, string>>? userselectTemplateValue = new List<Dictionary<string, string>>(); // 用户
        List<Dictionary<string, string>>? usersselectTemplateValue = new List<Dictionary<string, string>>(); // 用户组件
        List<Dictionary<string, string>>? posselectTemplateValue = new List<Dictionary<string, string>>(); // 岗位
        List<Dictionary<string, string>>? radioTemplateValue = new List<Dictionary<string, string>>(); // 单选框
        List<Dictionary<string, string>>? checkboxTemplateValue = new List<Dictionary<string, string>>(); // 复选框
        List<Dictionary<string, string>>? selectTemplateValue = new List<Dictionary<string, string>>(); // 下拉框
        List<Dictionary<string, string>>? treeSelectTemplateValue = new List<Dictionary<string, string>>(); // 树
        List<Dictionary<string, string>>? cascaderTemplateValue = new List<Dictionary<string, string>>(); // 级联选择
        List<Dictionary<string, string>>? groupTemplateValue = new List<Dictionary<string, string>>(); // 分组
        List<Dictionary<string, string>>? roleTemplateValue = new List<Dictionary<string, string>>(); // 角色
        Dictionary<string, List<Dictionary<string, string>>>? templateValues = new Dictionary<string, List<Dictionary<string, string>>>(); // 其他

        // 处理流程数据状态
        foreach (var data in list)
        {
            if (data.ContainsKey("f_flow_id")) data["flowId"] = data["f_flow_id"];
            if (data.ContainsKey("F_FLOW_ID")) data["flowId"] = data["F_FLOW_ID"];
            if (data.ContainsKey("f_flow_task_id")) data["flowTaskId"] = data["f_flow_task_id"];
            if (data.ContainsKey("F_FLOW_task_ID")) data["flowTaskId"] = data["F_FLOW_task_ID"];
            if (data.ContainsKey("f_flow_state")) data["flowState"] = data["f_flow_state"].ParseToInt();
            if (data.ContainsKey("F_FLOW_STATE")) data["flowState"] = data["F_FLOW_STATE"].ParseToInt();
        }

        // 转换列表数据
        foreach (Dictionary<string, object>? dataMap in list)
        {
            var oldDataMap = dataMap.Copy();
            if (dataMap.ContainsKey(primaryKey)) dataMap["id"] = dataMap[primaryKey].ToString(); // 主键

            if (!isConvertData.Equals(1))
            {
                int dicCount = dataMap.Keys.Count;
                string[] strKey = new string[dicCount];
                dataMap.Keys.CopyTo(strKey, 0);

                // 处理有缓存
                for (int i = 0; i < strKey.Length; i++)
                {
                    var form = formData.FirstOrDefault(f => f.__vModel__ == strKey[i]);
                    if (form != null)
                    {
                        var oldKey = string.Format("{0}_jnpfId", strKey[i]);
                        if (dataMap[strKey[i]].IsNotEmptyOrNull())
                        {
                            string? jnpfKey = form.__config__.jnpfKey;
                            dataMap[strKey[i]] = TemplateControlsDataConversion(dataMap[strKey[i]], form, actionType);

                            // 转换前的数据
                            var oldValue = dataMap[strKey[i]];
                            if (oldValue is string)
                            {
                                if (oldValue.ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty).StartsWith("[["))
                                {
                                    oldValue = oldValue.ToString().ToObject<List<List<object>>>();
                                }
                                else if (oldValue.ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty).StartsWith('['))
                                {
                                    oldValue = oldValue.ToString().ToObject<List<object>>();
                                }

                                if (jnpfKey.Equals(JnpfKeyConst.DATE) || jnpfKey.Equals(JnpfKeyConst.MODIFYTIME) || jnpfKey.Equals(JnpfKeyConst.CREATETIME) || jnpfKey.Equals(JnpfKeyConst.DATECALCULATE))
                                    oldValue = oldValue.ToString().ParseToDateTime().GetTotalMilliseconds();
                                else if (jnpfKey.Equals(JnpfKeyConst.NUMINPUT) || jnpfKey.Equals(JnpfKeyConst.CALCULATE))
                                    oldValue = oldValue.ToString().ParseToDecimal();
                            }
                            dataMap[oldKey] = oldValue;

                            KeyValuePair<string, object> templateValue = templateData.Where(t => t.Key.Equals(CommonConst.VISUALDEV + _userManager.TenantId + "_" + modelId + "_" + form.__config__.renderKey + "_" + strKey[i])).FirstOrDefault();

                            #region 处理 单独存储缓存
                            if (jnpfKey == JnpfKeyConst.USERSELECT)
                            {
                                templateValue = templateData.Where(t => t.Key.Equals(CommonConst.VISUALDEV + "_userSelect")).FirstOrDefault();
                                if (!templateData.ContainsKey(form.__config__.renderKey + "_" + strKey[i])) templateData.Add(form.__config__.renderKey + "_" + strKey[i], string.Empty);
                            }

                            if (jnpfKey == JnpfKeyConst.USERSSELECT)
                            {
                                templateValue = templateData.Where(t => t.Key.Equals(CommonConst.VISUALDEV + "_usersSelect")).FirstOrDefault();
                                if (!templateData.ContainsKey(form.__config__.renderKey + "_" + strKey[i])) templateData.Add(form.__config__.renderKey + "_" + strKey[i], string.Empty);
                            }

                            if (jnpfKey == JnpfKeyConst.ADDRESS)
                            {
                                templateValue = templateData.Where(t => t.Key.Equals(CommonConst.VISUALDEV + "_address")).FirstOrDefault();
                                if (!templateData.ContainsKey(form.__config__.renderKey + "_" + strKey[i])) templateData.Add(form.__config__.renderKey + "_" + strKey[i], string.Empty);
                            }

                            if (jnpfKey == JnpfKeyConst.COMSELECT || jnpfKey == JnpfKeyConst.CURRORGANIZE)
                            {
                                templateValue = templateData.Where(t => t.Key.Equals(CommonConst.VISUALDEV + "_organize")).FirstOrDefault();
                                if (!templateData.ContainsKey(form.__config__.renderKey + "_" + strKey[i])) templateData.Add(form.__config__.renderKey + "_" + strKey[i], string.Empty);
                            }

                            if (jnpfKey == JnpfKeyConst.POSSELECT || jnpfKey == JnpfKeyConst.CURRPOSITION)
                            {
                                templateValue = templateData.Where(t => t.Key.Equals(CommonConst.VISUALDEV + "_position")).FirstOrDefault();
                                if (!templateData.ContainsKey(form.__config__.renderKey + "_" + strKey[i])) templateData.Add(form.__config__.renderKey + "_" + strKey[i], string.Empty);
                            }
                            #endregion

                            // 转换后的数据值
                            object? dataDicValue = dataMap[strKey[i]];
                            if (templateValue.Key != null && !(dataDicValue is null) && dataDicValue.ToString() != "[]")
                            {
                                IEnumerable<object>? moreValue = dataDicValue as IEnumerable<object>;
                                if (dataDicValue.IsNullOrEmpty()) jnpfKey = string.Empty; // 空数据直接赋值

                                // 不是List数据直接赋值 用逗号切割成 List
                                if (moreValue == null && !dataDicValue.ToString().Contains("[")) moreValue = dataDicValue.ToString().Split(",");
                                if (moreValue == null && dataDicValue.ToString().Contains("[[")) moreValue = dataDicValue.ToString().ToObject<List<List<object>>>();
                                if (moreValue == null && dataDicValue.ToString().Contains("[")) moreValue = dataDicValue.ToString().ToObject<List<string>>();

                                if (string.IsNullOrWhiteSpace(form.separator))
                                {
                                    if (jnpfKey.Equals(JnpfKeyConst.CASCADER) && !form.multiple)
                                        form.separator = "/";
                                    else
                                        form.separator = ",";
                                }

                                switch (jnpfKey)
                                {
                                    case JnpfKeyConst.COMSELECT:
                                    case JnpfKeyConst.CURRORGANIZE:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(comselectTemplateValue, templateValue, moreValue, form);
                                        break;
                                    case JnpfKeyConst.ADDRESS:
                                        {
                                            var addressValues = new List<List<object>>();
                                            if (form.multiple) addressValues = dataDicValue.ToJsonString().ToObject<List<List<object>>>();
                                            else addressValues.Add(dataDicValue.ToJsonString().ToObject<List<object>>());

                                            var addNames = new List<string>();

                                            if (addressTemplateValue.Count < 1) addressTemplateValue = templateValue.Value.ToObject<List<Dictionary<string, string>>>();

                                            foreach (var item in addressValues)
                                            {
                                                if (item.Any())
                                                {
                                                    var value = addressTemplateValue.Where(a => a.ContainsKey(item.LastOrDefault().ToString())).FirstOrDefault();
                                                    if (value != null) addNames.Add(value[item.LastOrDefault().ToString()]);
                                                }
                                            }
                                            if (addNames.Count != 0) dataMap[strKey[i]] = string.Join(form.separator, addNames);
                                        }
                                        break;
                                    case JnpfKeyConst.USERSELECT:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(userselectTemplateValue, templateValue, moreValue, form);
                                        break;
                                    case JnpfKeyConst.USERSSELECT:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(usersselectTemplateValue, templateValue, moreValue, form);
                                        break;
                                    case JnpfKeyConst.POSSELECT:
                                    case JnpfKeyConst.CURRPOSITION:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(posselectTemplateValue, templateValue, moreValue, form);
                                        break;
                                    case JnpfKeyConst.RADIO:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(radioTemplateValue, templateValue, moreValue, form, formData, oldDataMap);
                                        break;
                                    case JnpfKeyConst.CHECKBOX:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(checkboxTemplateValue, templateValue, moreValue, form, formData, oldDataMap);
                                        break;
                                    case JnpfKeyConst.SELECT:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(selectTemplateValue, templateValue, moreValue, form, formData, oldDataMap);
                                        break;
                                    case JnpfKeyConst.TREESELECT:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(treeSelectTemplateValue, templateValue, moreValue, form, formData, oldDataMap);
                                        break;
                                    case JnpfKeyConst.CASCADER:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(cascaderTemplateValue, templateValue, moreValue, form, formData, oldDataMap);
                                        break;
                                    case JnpfKeyConst.GROUPSELECT:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(groupTemplateValue, templateValue, moreValue, form);
                                        break;
                                    case JnpfKeyConst.ROLESELECT:
                                        dataMap[strKey[i]] = await GetTemplateDataValueByKey(roleTemplateValue, templateValue, moreValue, form);
                                        break;
                                    default:
                                        {
                                            if (jnpfKey.IsNotEmptyOrNull())
                                            {
                                                if (!templateValues.ContainsKey(strKey[i])) templateValues.Add(strKey[i], templateValue.Value.ToJsonString().ToObject<List<Dictionary<string, string>>>());

                                                var convertData = templateValues[strKey[i]].Where(t => t.ContainsKey(dataMap[strKey[i]].ToString())).FirstOrDefault();
                                                if (convertData != null) dataMap[strKey[i]] = convertData.Values.FirstOrDefault().ToString();
                                            }
                                            else
                                            {
                                                dataMap[strKey[i]] = string.Empty;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        else
                        {
                            dataMap[oldKey] = null;
                        }
                    }
                }

                // 剪掉有缓存的，获取无缓存的 继续解析
                List<string>? record = dataMap.Keys.Except(templateData.Keys.Select(x => x = x.Substring(14, x.Length - 14))).ToList();

                // 针对有表的模板去除"rownum,主键"
                record.RemoveAll(r => r == "ROWNUM");
                record.RemoveAll(r => r == primaryKey);

                foreach (string? key in record)
                {
                    if (!(dataMap[key] is null) && dataMap[key].ToString() != string.Empty)
                    {
                        var dataValue = dataMap[key];
                        var model = formData.Where(f => f.__vModel__ == key).FirstOrDefault();
                        if (model != null)
                        {
                            var dataValueStr = dataValue.ToString();
                            ConfigModel configModel = model.__config__;
                            string type = configModel.jnpfKey;
                            if (string.IsNullOrWhiteSpace(model.separator)) model.separator = ",";
                            switch (type)
                            {
                                case JnpfKeyConst.SWITCH: // switch开关
                                    dataMap[key] = dataMap[key].ParseToInt() == 0 ? model.inactiveTxt : model.activeTxt;
                                    break;
                                case JnpfKeyConst.DATERANGE: // 日期范围
                                case JnpfKeyConst.DATE: // 日期选择
                                case JnpfKeyConst.DATECALCULATE: // 日期公式
                                    {
                                        string value = string.Empty;
                                        var keyValue = dataMap[key].ToString();
                                        DateTime dtDate;
                                        if (DateTime.TryParse(keyValue, out dtDate)) value = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", keyValue);
                                        else value = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", keyValue.ParseToDateTime());

                                        if (!string.IsNullOrEmpty(model.format))
                                        {
                                            value = string.Format("{0:" + model.format + "} ", Convert.ToDateTime(value));
                                        }
                                        else
                                        {
                                            switch (model.type)
                                            {
                                                case "date":
                                                    value = string.Format("{0:yyyy-MM-dd}", Convert.ToDateTime(value));
                                                    break;
                                                default:
                                                    value = string.Format("{0:yyyy-MM-dd HH:mm:ss}", Convert.ToDateTime(value));
                                                    break;
                                            }
                                        }
                                        dataMap[key] = value;
                                    }
                                    break;
                                case JnpfKeyConst.TIME: // 日期选择
                                    {
                                        string value = string.Empty;
                                        var keyValue = dataMap[key].ToString();
                                        DateTime dtDate;
                                        if (DateTime.TryParse(keyValue, out dtDate)) value = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", keyValue);
                                        else value = string.Format("{0:yyyy-MM-dd HH:mm:ss} ", keyValue.TimeStampToDateTime());
                                        if (!string.IsNullOrEmpty(model.format)) value = string.Format("{0:" + model.format + "}", Convert.ToDateTime(value));
                                        else value = dataMap[key].ToString();
                                        dataMap[key] = value;
                                    }
                                    break;
                                case JnpfKeyConst.CALCULATE: // 计算公式
                                case JnpfKeyConst.NUMINPUT: // 数字输入
                                    dataMap[key] = dataMap[key].ToString();
                                    break;
                                case JnpfKeyConst.MODIFYUSER:
                                case JnpfKeyConst.CREATEUSER:
                                    var user = await _db.AsSugarClient().Queryable<UserEntity>().Where(x => x.Id == dataValueStr).Select(x => SqlFunc.MergeString(x.RealName, "/", x.Account)).FirstAsync();
                                    if (user.IsNotEmptyOrNull()) dataMap[key] = user;
                                    break;
                                case JnpfKeyConst.MODIFYTIME:
                                case JnpfKeyConst.CREATETIME:
                                    dataMap[key] = string.Format("{0:yyyy-MM-dd HH:mm:ss}", dataMap[key].ToString().ParseToDateTime());
                                    break;
                                case JnpfKeyConst.POPUPTABLESELECT:
                                case JnpfKeyConst.POPUPSELECT:
                                    {
                                        if (model.templateJson != null && model.templateJson.Any())
                                        {
                                            await ControlParsingLinkage(dataMap, model, formData);
                                            break;
                                        }

                                        // 获取远端数据
                                        var dynamic = await _dataInterfaceService.GetInfo(model.interfaceId);
                                        if (dynamic == null) break;
                                        List<Dictionary<string, string>> popupselectDataList = new List<Dictionary<string, string>>();
                                        var redisName = CommonConst.VISUALDEV + _userManager.TenantId + "_" + model.__vModel__ + "_" + model.interfaceId;

                                        if (model.__config__.useCache)
                                        {
                                            if (_cacheManager.Exists(redisName))
                                            {
                                                popupselectDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
                                            }
                                            else
                                            {
                                                popupselectDataList = await GetDynamicList(model);
                                                _cacheManager.Set(redisName, popupselectDataList.ToList(), TimeSpan.FromMinutes(10)); // 缓存10分钟
                                                popupselectDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
                                            }
                                        }
                                        else
                                        {
                                            popupselectDataList = await GetDynamicList(model);
                                        }

                                        switch (dynamic.Type)
                                        {
                                            case 1: // SQL数据
                                                {
                                                    // 关联属性
                                                    var attrList = formData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPATTR) && x.relationField.Contains(model.__vModel__) && x.isStorage.Equals(0)).ToList();
                                                    Dictionary<string, object> attrDataList = new Dictionary<string, object>();

                                                    var specificData = popupselectDataList.Where(it => it.ContainsKey(model.propsValue) && it.ContainsValue(dataMap[key].ToString())).FirstOrDefault();
                                                    if (specificData != null)
                                                    {
                                                        // 要用模板的 “显示字段 - relationField”来展示数据
                                                        if (model.relationField.IsNullOrEmpty())
                                                        {
                                                            var showField = model.columnOptions.First();
                                                            dataMap[key] = specificData[showField.value];
                                                        }
                                                        else
                                                        {
                                                            foreach (var item in attrList)
                                                            {
                                                                if (specificData.ContainsKey(item.showField))
                                                                    dataMap[key + "_" + item.showField] = specificData[item.showField];
                                                            }
                                                            dataMap[key + "_id"] = dataValue;
                                                            dataMap[key] = specificData[model.relationField];
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (model.multiple)
                                                        {
                                                            var nameList = new List<string>();
                                                            var strItList = dataMap[key].ToString().Contains("[") ? dataMap[key].ToObject<List<string>>() : new List<string>() { dataMap[key].ToString() };
                                                            strItList.ForEach(strIt =>
                                                            {
                                                                var specificData = popupselectDataList.Where(it => it.ContainsKey(model.propsValue) && it.ContainsValue(strIt)).FirstOrDefault();
                                                                if (specificData != null)
                                                                {
                                                                    // 要用模板的 “显示字段 - relationField”来展示数据
                                                                    if (model.relationField.IsNullOrEmpty())
                                                                    {
                                                                        var showField = model.columnOptions.First();
                                                                        nameList.Add(specificData[showField.value]);
                                                                    }
                                                                    else
                                                                    {
                                                                        nameList.Add(specificData[model.relationField]);
                                                                    }
                                                                }
                                                            });
                                                            if (nameList.Any())
                                                            {
                                                                dataMap[key] = string.Join(model.separator, nameList);
                                                                dataMap[key + "_id"] = dataValue;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            dataMap[key] = string.Empty;
                                                        }
                                                    }
                                                }
                                                break;
                                            case 2: // 静态数据
                                                {
                                                    // 关联属性
                                                    var attrList = formData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPATTR) && x.relationField.Contains(model.__vModel__) && x.isStorage.Equals(0)).ToList();
                                                    Dictionary<string, object> attrDataList = new Dictionary<string, object>();

                                                    List<string> cascaderList = new List<string>();
                                                    if (dataMap[key].ToJsonString().Contains("["))
                                                    {
                                                        var dataList = dataMap[key].ToJsonString().ToObject<List<string>>();
                                                        foreach (var items in dataList)
                                                        {
                                                            var vara = popupselectDataList.Where(a => a.ContainsValue(items)).FirstOrDefault();
                                                            if (vara != null) cascaderList.Add(vara[model.props.label]);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var vara = popupselectDataList.Where(a => a.ContainsValue(dataMap[key].ToString())).FirstOrDefault();
                                                        if (vara != null)
                                                        {
                                                            cascaderList.Add(vara[model.relationField]);
                                                            foreach (var item in attrList)
                                                            {
                                                                if (vara.ContainsKey(item.showField))
                                                                    attrDataList[string.Format("{0}_{1}", model.__vModel__, item.showField)] = vara[item.showField];
                                                            }
                                                        }
                                                    }

                                                    // 关联属性
                                                    foreach (var item in attrDataList) dataMap[item.Key] = item.Value;

                                                    dataMap[key + "_id"] = dataValue;
                                                    if (actionType == "List") dataMap[key] = string.Join(model.separator, cascaderList);
                                                    else dataMap[key] = cascaderList;
                                                }
                                                break;
                                            case 3: // Api数据
                                                {
                                                    // 关联属性
                                                    var attrList = formData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPATTR) && x.relationField.Contains(model.__vModel__) && x.isStorage.Equals(0)).ToList();
                                                    Dictionary<string, object> attrDataList = new Dictionary<string, object>();

                                                    List<object> cascaderList = new List<object>();
                                                    if (model.multiple && popupselectDataList != null)
                                                    {
                                                        popupselectDataList.ForEach(obj =>
                                                        {
                                                            dataValue.ToObject<List<string>>().ForEach(str => { if (obj[model.propsValue] == str) cascaderList.Add(obj[model.relationField]); });
                                                        });
                                                    }
                                                    else if (popupselectDataList != null)
                                                    {
                                                        popupselectDataList.ForEach(obj =>
                                                        {
                                                            if (obj[model.propsValue] == dataValue.ToString())
                                                            {
                                                                cascaderList.Add(obj[model.relationField]);
                                                                foreach (var item in attrList)
                                                                {
                                                                    if (obj.ContainsKey(item.showField))
                                                                        attrDataList[string.Format("{0}_{1}", model.__vModel__, item.showField)] = obj[item.showField];
                                                                }
                                                            }
                                                        });
                                                    }

                                                    // 关联属性
                                                    foreach (var item in attrDataList) dataMap[item.Key] = item.Value;

                                                    dataMap[key + "_id"] = dataValue;
                                                    dataMap[key] = string.Join(model.separator, cascaderList);
                                                }
                                                break;
                                        }
                                    }
                                    break;
                                case JnpfKeyConst.RELATIONFORM: // 关联表单
                                    {
                                        List<Dictionary<string, object>> relationFormDataList = new List<Dictionary<string, object>>();

                                        var redisName = CommonConst.VISUALDEV + _userManager.TenantId + "_" + modelId + "_" + model.__config__.renderKey + "_" + model.__vModel__;
                                        if (_cacheManager.Exists(redisName))
                                        {
                                            relationFormDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, object>>>();
                                        }
                                        else
                                        {
                                            // 根据可视化功能ID获取该模板全部数据
                                            var relationFormReleaseModel = await _db.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(v => v.Id == model.modelId);
                                            var relationFormModel = relationFormReleaseModel.Adapt<VisualDevEntity>();

                                            VisualDevModelListQueryInput listQueryInput = new VisualDevModelListQueryInput
                                            {
                                                dataType = "1",
                                                sidx = columnDesign.defaultSidx,
                                                sort = columnDesign.sort,
                                                pageSize = 999999
                                            };

                                            Scoped.Create((_, scope) =>
                                            {
                                                var services = scope.ServiceProvider;
                                                var _runService = App.GetService<IRunService>(services);
                                                var res = _runService.GetRelationFormList(relationFormModel, listQueryInput).WaitAsync(TimeSpan.FromMinutes(2)).Result;
                                                _cacheManager.Set(redisName, res.list.ToList(), TimeSpan.FromMinutes(10)); // 缓存10分钟
                                            });
                                            var cacheStr = _cacheManager.Get(redisName);
                                            if (cacheStr.IsNotEmptyOrNull()) relationFormDataList = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, object>>>();
                                        }

                                        var relationFormRealData = relationFormDataList.Where(it => it.ContainsKey(model.propsValue) && it[model.propsValue].IsNotEmptyOrNull() && it[model.propsValue].Equals(dataMap[key].ToString())).FirstOrDefault();
                                        if (relationFormRealData != null && relationFormRealData.Count > 0)
                                        {
                                            // 关联属性
                                            foreach (var item in formData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORMATTR) && x.relationField.Contains(model.__vModel__) && x.isStorage.Equals(0)).ToList())
                                            {
                                                if (relationFormRealData.ContainsKey(item.showField))
                                                    dataMap[key + "_" + item.showField] = relationFormRealData[item.showField];
                                            }

                                            dataMap[key + "_id"] = relationFormRealData[model.propsValue];
                                            dataMap[key] = relationFormRealData.ContainsKey(model.relationField) ? relationFormRealData[model.relationField] : string.Empty;
                                        }
                                        else
                                        {
                                            dataMap[key] = string.Empty;
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.LOCATION: // 定位
                                    switch (actionType)
                                    {
                                        case "List":
                                            if (isConvertData.IsNotEmptyOrNull()) // 列表有值，详情没值
                                            {
                                                var value = dataMap[key]?.ToString().ToObject<Dictionary<string, object>>();
                                                if (value.IsNotEmptyOrNull() && value.ContainsKey("fullAddress"))
                                                    dataMap[key] = value["fullAddress"];
                                            }
                                            break;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        return list;
    }

    /// <summary>
    /// 从缓存读取数据,根据Key.
    /// </summary>
    /// <param name="keyTData">控件对应的缓存</param>
    /// <param name="tValue">所有的缓存</param>
    /// <param name="mValue">要转换的key</param>
    /// <param name="form">组件</param>
    /// <param name="fieldList">所有组件(控件联动会用到)</param>
    /// <param name="dataMap">当前行数据(控件联动会用到)</param>
    /// <returns></returns>
    private async Task<string> GetTemplateDataValueByKey(List<Dictionary<string, string>> keyTData, KeyValuePair<string, object> tValue, IEnumerable<object> mValue, FieldsModel? form, List<FieldsModel>? fieldList = null, Dictionary<string, object>? dataMap = null)
    {
        List<string>? data = new List<string>();
        if (keyTData.Count < 1) keyTData = tValue.Value.ToObject<List<Dictionary<string, string>>>();

        if (form != null && form.props != null && form.multiple)
        {
            foreach (object? item in mValue)
            {
                List<string>? sb = new List<string>();
                var itemList = new List<string>();
                if (item.ToJsonString().Contains('[')) itemList = item.ToJsonString().ToObject<List<string>>();
                else itemList.Add(item.ToString());
                itemList.ForEach(items =>
                {
                    var cascaderData = keyTData.Where(c => c.ContainsKey(items)).FirstOrDefault();
                    if (cascaderData != null) sb.Add(cascaderData[items]);
                });
                if (sb.Count != 0) data.Add(string.Join("/", sb));
                else data.Add(item.ToString());
            }
        }
        else if (form != null && form.props != null && form.props.value.IsNotEmptyOrNull() && form.props.label.IsNotEmptyOrNull())
        {
            foreach (object? item in mValue)
            {
                var itemData = keyTData.FirstOrDefault(x => (x.ContainsKey(form.props.value) && x.ContainsValue(item.ToString())) || x.ContainsKey(item.ToString()));
                if (itemData != null) data.Add(itemData.First().Value);
                else data.Add(item.ToString());
            }
        }
        else
        {
            foreach (object? item in mValue)
            {
                Dictionary<string, string>? comData = keyTData.Where(a => a.ContainsKey(item.ToString())).FirstOrDefault();
                if (comData != null) data.Add(comData[item.ToString()]);
                else data.Add(item.ToString());
            }
        }

        // 控件联动
        if (data.ToJsonString().Equals(mValue.ToJsonString()) && (form.__config__.templateJson != null && form.__config__.templateJson.Any()))
        {
            data.Clear();
            form.__config__.templateJson.ForEach(x =>
            {
                if (x.relationField != null)
                {
                    if (x.relationField.ToLower().Contains("tablefield") && x.relationField.Contains("-"))
                    {
                        var rField = x.relationField.Split("-").Last();
                        if (dataMap.ContainsKey(rField)) x.defaultValue = dataMap[rField] != null ? dataMap[rField]?.ToString() : x.defaultValue;
                    }
                    else
                    {
                        if (dataMap.ContainsKey(x.relationField))
                        {
                            x.defaultValue = dataMap[x.relationField] != null ? dataMap[x.relationField]?.ToString() : x.defaultValue;
                        }
                        else if (dataMap.ContainsKey("JnpfKeyConst_MainData"))
                        {
                            var mainData = dataMap["JnpfKeyConst_MainData"].ToString().ToObject<Dictionary<string, object>>();
                            if (mainData.ContainsKey(x.relationField)) x.defaultValue = mainData[x.relationField] != null ? mainData[x.relationField]?.ToString() : x.defaultValue;
                        }
                    }
                }
            });
            _databaseService.ChangeDataBase(_databaseService.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName));
            var res = await _dataInterfaceService.GetResponseByType(form.__config__.propsUrl, 0, new DataInterfacePreviewInput() { paramList = form.__config__.templateJson.Adapt<List<DataInterfaceParameter>>(), pageSize = 500, currentPage = 1 });
            var resList = res.ToObject<PageResult<Dictionary<string, object>>>();
            if (resList != null && resList.list.Any())
            {
                foreach (object? item in mValue)
                {
                    var comData = resList.list.Where(a => a.Any(xx => xx.Value.ToString().Equals(item.ToString()))).FirstOrDefault();
                    if (comData != null) data.Add(comData[form.props.label].ToString());
                    else data.Add(item.ToString());
                }
            }
        }

        return string.Join(form.separator, data);
    }

    #endregion

    #region 公用方法

    /// <summary>
    /// 解析 处理 条形码和二维码.
    /// </summary>
    /// <param name="fieldsModels"></param>
    /// <param name="_newDataMap"></param>
    /// <param name="_dataMap"></param>
    public void GetBARAndQR(List<FieldsModel> fieldsModels, Dictionary<string, object> _newDataMap, Dictionary<string, object> _dataMap)
    {
        fieldsModels.Where(x => x.__config__.jnpfKey == JnpfKeyConst.BARCODE || x.__config__.jnpfKey == JnpfKeyConst.QRCODE).Where(x => !string.IsNullOrWhiteSpace(x.relationField)).ToList().ForEach(item =>
        {
            if (!_newDataMap.ContainsKey(item.relationField + "_id") && _dataMap.ContainsKey(item.relationField))
                _newDataMap.Add(item.relationField + "_id", _dataMap[item.relationField]);
        });
    }

    /// <summary>
    /// 获取弹窗选择 数据列表.
    /// </summary>
    /// <param name="interfaceId"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<List<Dictionary<string, string>>?> GetPopupSelectDataList(string interfaceId, FieldsModel model)
    {
        List<Dictionary<string, string>>? result = new List<Dictionary<string, string>>();
        var redisName = CommonConst.VISUALDEV + _userManager.TenantId + "_" + model.__config__.jnpfKey + "_" + model.interfaceId;
        if (_cacheManager.Exists(redisName))
        {
            result = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
        }
        else
        {
            result = await GetDynamicList(model);
            _cacheManager.Set(redisName, result.ToList(), TimeSpan.FromMinutes(10)); // 缓存10分钟
            result = _cacheManager.Get(redisName).ToObject<List<Dictionary<string, string>>>();
        }

        return result;
    }

    /// <summary>
    /// 获取单据规则.
    /// </summary>
    /// <param name="moduleId"></param>
    /// <param name="modelConfig"></param>
    /// <param name="allDataMap"></param>
    /// <param name="flowId"></param>
    /// <param name="isChild"></param>
    /// <returns></returns>
    public async Task<string> GetBillRule(string moduleId, ConfigModel modelConfig, Dictionary<string, object> allDataMap, string flowId, bool isChild = false)
    {
        var ruleConfig = modelConfig.ruleConfig;
        var strNumber = new StringBuilder();

        // 拼接前缀
        foreach (var prefix in ruleConfig.prefixList)
        {
            var value = string.Empty;
            switch (prefix.sourceType)
            {
                case 1:
                    var key = isChild ? prefix.relationField.Split("-").Last() : prefix.relationField;
                    if (allDataMap.ContainsKey(key) && allDataMap[key].IsNotEmptyOrNull())
                        value = allDataMap[key].ToString();
                    break;
                case 2:
                    if (prefix.relationField.IsNotEmptyOrNull()) value = prefix.relationField;
                    break;
            }

            if (value?.Length > 100) throw Oops.Oh(ErrorCode.BR0002, modelConfig.label);
            strNumber.Append(value);
        }

        switch (ruleConfig.type)
        {
            case 1: // 时间格式
                ruleConfig.randomDigit = null;
                ruleConfig.randomType = null;
                var config = ruleConfig.ToJsonStringOld();

                var entity = await _db.AsSugarClient().Queryable<BillNumEntity>().FirstAsync(x => x.DeleteMark == null && x.RuleId == modelConfig.formId && x.VisualId == moduleId && x.FlowId == flowId);
                if (entity.IsNullOrEmpty())
                {
                    entity = new BillNumEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        RuleId = modelConfig.formId,
                        VisualId = moduleId,
                        FlowId = flowId,
                        RuleConfig = config,
                        DateValue = "jnpf",
                        Num = 0
                    };
                    await _db.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                }

                var thisDate = ruleConfig.dateFormat == "no" ? string.Empty : DateTime.Now.ToString(ruleConfig.dateFormat.Replace("YYYY", "yyyy").Replace("DD", "dd").Replace("SSS", "fff"));
                if (!entity.RuleConfig.Equals(config)) // 修改配置序号归0
                {
                    entity.RuleConfig = config;
                    entity.DateValue = thisDate;
                    entity.Num = 0;
                }
                else
                {
                    var value = entity.DateValue.IsNotEmptyOrNull() ? entity.DateValue : string.Empty;
                    if (!value.Equals(thisDate)) // 处理时间变化序号归0
                    {
                        entity.DateValue = thisDate;
                        entity.Num = 0;
                    }
                    else
                    {
                        entity.Num++;
                    }
                }

                strNumber.Append(thisDate);
                var number = int.Parse(ruleConfig.startNumber) + entity.Num;

                // 超过最大值
                var max = (int)Math.Pow(10, ruleConfig.digit.Value) - 1;
                if (number > max)
                {
                    entity.Num = 0;
                    number = int.Parse(ruleConfig.startNumber);
                }

                strNumber.Append(number.ToString()?.PadLeft((int)ruleConfig.digit, '0')); // 流水号

                await _db.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

                break;
            case 2: // 随机数编号
                var random = ruleConfig.randomType.Equals(1) ? new Random().NextNumberString(ruleConfig.randomDigit.Value) : new Random().NextLetterAndNumberString(ruleConfig.randomDigit.Value);
                strNumber.Append(random);

                await _db.AsSugarClient().Updateable<BillNumEntity>()
                    .SetColumns(it => new BillNumEntity()
                    {
                        DeleteMark = 1,
                        DeleteTime = SqlFunc.GetDate(),
                        DeleteUserId = _userManager.UserId
                    }).Where(it => it.VisualId == moduleId && it.RuleId == modelConfig.formId).ExecuteCommandAsync();
                break;
            case 3: // UUID
                strNumber.Append(Guid.NewGuid().ToString());

                await _db.AsSugarClient().Updateable<BillNumEntity>()
                    .SetColumns(it => new BillNumEntity()
                    {
                        DeleteMark = 1,
                        DeleteTime = SqlFunc.GetDate(),
                        DeleteUserId = _userManager.UserId
                    }).Where(it => it.VisualId == moduleId && it.RuleId == modelConfig.formId).ExecuteCommandAsync();
                break;
        }

        // 拼接后缀
        foreach (var suffix in ruleConfig.suffixList)
        {
            var value = string.Empty;
            switch (suffix.sourceType)
            {
                case 1:
                    var key = isChild ? suffix.relationField.Split("-").Last() : suffix.relationField;
                    if (allDataMap.ContainsKey(key) && allDataMap[key].IsNotEmptyOrNull())
                        value = allDataMap[key].ToString();
                    break;
                case 2:
                    if (suffix.relationField.IsNotEmptyOrNull()) value = suffix.relationField;
                    break;
            }

            if (value?.Length > 100) throw Oops.Oh(ErrorCode.BR0002, modelConfig.label);
            strNumber.Append(value);
        }

        return strNumber.ToString();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取动态无限级数据.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    private List<Dictionary<string, string>> GetDynamicInfiniteData(string data, PropsBeanModel props)
    {
        List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
        string? value = props.value;
        string? label = props.label;
        string? children = props.children;
        foreach (JToken? info in JToken.Parse(data))
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic[info.Value<string>(value)] = info.Value<string>(label);
            list.Add(dic);
            if (info.Value<object>(children) != null && info.Value<object>(children).ToString() != string.Empty)
                list.AddRange(GetDynamicInfiniteData(info.Value<object>(children).ToString(), props));
        }

        return list;
    }

    /// <summary>
    /// 控件联动解析.
    /// </summary>
    /// <param name="dataMap">单条数据.</param>
    /// <param name="model">控件模型.</param>
    private async Task<string> ControlParsingLinkage(Dictionary<string, object> dataMap, FieldsModel model, List<FieldsModel> formData)
    {
        var result = dataMap[model.__vModel__];
        var templateJson = (model.templateJson != null && model.templateJson.Any()) ? model.templateJson : model.__config__.templateJson;
        if (templateJson != null && templateJson.Any())
        {
            templateJson.ForEach(x =>
            {
                if (x.relationField != null)
                {
                    if (x.relationField.ToLower().Contains("tablefield") && x.relationField.Contains("-"))
                    {
                        var rField = x.relationField.Split("-").Last();
                        if (dataMap.ContainsKey(rField)) x.defaultValue = dataMap[rField] != null ? dataMap[rField]?.ToString() : x.defaultValue;
                    }
                    else
                    {
                        if (dataMap.ContainsKey(x.relationField + "_jnpfId"))
                        {
                            x.defaultValue = dataMap[x.relationField + "_jnpfId"] != null ? dataMap[x.relationField + "_jnpfId"]?.ToString() : x.defaultValue;
                        }
                        else if (dataMap.ContainsKey("JnpfKeyConst_MainData"))
                        {
                            var mainData = dataMap["JnpfKeyConst_MainData"].ToString().ToObject<Dictionary<string, object>>();
                            if (mainData.ContainsKey(x.relationField)) x.defaultValue = mainData[x.relationField] != null ? mainData[x.relationField]?.ToString() : x.defaultValue;
                        }
                    }
                }
            });
            var interfaceId = model.interfaceId.IsNotEmptyOrNull() ? model.interfaceId : model.__config__.propsUrl;
            var res = await _dataInterfaceService.GetResponseByType(interfaceId, 0, new DataInterfacePreviewInput() { paramList = templateJson.Adapt<List<DataInterfaceParameter>>(), pageSize = 500, currentPage = 1 });
            var resList = res.ToObject<PageResult<Dictionary<string, object>>>();

            if (resList != null && resList.list.Any())
            {
                // 关联属性
                var attrList = formData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPATTR) && x.relationField.Contains(model.__vModel__) && x.isStorage.Equals(0)).ToList();
                Dictionary<string, object> attrDataList = new Dictionary<string, object>();

                List<object> cascaderList = new List<object>();
                if (model.multiple && resList != null && resList.list != null)
                {
                    var dataValue = dataMap[model.__vModel__].ToObject<List<string>>();
                    dataValue.ForEach(str =>
                    {
                        var obj = resList.list.Find(x => x.ContainsKey(model.propsValue) && x[model.propsValue].ToString().Equals(str));
                        if (obj != null) cascaderList.Add(obj[model.relationField]);
                    });
                }
                else if (resList != null)
                {
                    resList.list.ForEach(obj =>
                    {
                        if (obj[model.propsValue].IsNotEmptyOrNull() && obj[model.propsValue].ToString().Equals(dataMap[model.__vModel__]))
                        {
                            cascaderList.Add(obj[model.relationField]);
                            foreach (var item in attrList)
                            {
                                if (obj.ContainsKey(item.showField))
                                    attrDataList[string.Format("{0}_{1}", model.__vModel__, item.showField)] = obj[item.showField];
                            }
                        }
                    });
                }

                // 关联属性
                foreach (var item in attrDataList) dataMap[item.Key] = item.Value;

                dataMap[model.__vModel__ + "_id"] = dataMap[model.__vModel__];
                dataMap[model.__vModel__] = string.Join(model.separator, cascaderList);
                result = dataMap[model.__vModel__];
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 转换名称.
    /// </summary>
    /// <param name="dic"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    private string ConversionName(List<Dictionary<string, string>> dic, object data)
    {
        var res = string.Empty;
        if (data.ToString().StartsWith("["))
        {
            var ids = data.ToString().ToObject<List<string>>();
            if (dic.IsNotEmptyOrNull())
            {
                var fullName = new List<string>();
                foreach (var id in ids)
                {
                    var name = dic.Find(x => x.ContainsKey(id))?.FirstOrDefault().Value;
                    if (name.IsNotEmptyOrNull()) fullName.Add(name);
                }

                res = string.Join(",", fullName);
            }
        }
        else
        {
            var name = dic.Find(x => x.ContainsKey(data.ToString()))?.FirstOrDefault().Value;
            if (name.IsNotEmptyOrNull()) res = name;
        }

        return res;
    }

    #endregion
}