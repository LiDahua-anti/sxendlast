using BasicSQLFormatter;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.DataSet;
using JNPF.Systems.Entitys.Dto.System.DataSet;
using JNPF.Systems.Entitys.Model.DataSet;
using JNPF.Systems.Entitys.Model.PrintDev;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Data;

namespace JNPF.Systems;

/// <summary>
/// 数据集.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DataSet", Order = 200)]
[Route("api/system/[controller]")]
public class DataSetService : IDataSetService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<DataSetEntity> _repository;

    /// <summary>
    /// SqlSugarClient客户端.
    /// </summary>
    private SqlSugarScope _sqlSugarClient;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 切库.
    /// </summary>
    private readonly IDataBaseManager _databaseService;

    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 数据库管理.
    /// </summary>
    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 初始化一个<see cref="DataSetService"/>类型的新实例.
    /// </summary>
    public DataSetService(
        ISqlSugarRepository<DataSetEntity> repository,
        ISqlSugarClient sqlSugarClient,
        IOptions<TenantOptions> tenantOptions,
        ICacheManager cacheManager,
        IDataBaseManager databaseService,
        IDataBaseManager dataBaseManager,
        IUserManager userManager,
        IDbLinkService dbLinkService,
        IDataInterfaceService dataInterfaceService)
    {
        _repository = repository;
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
        _tenant = tenantOptions.Value;
        _cacheManager = cacheManager;
        _databaseService = databaseService;
        _dbLinkService = dbLinkService;
        _dataBaseManager = dataBaseManager;
        _userManager = userManager;
        _dataInterfaceService = dataInterfaceService;
    }

    private static string _printSensitive = "CREATE,UNIQUE,CHECK,DEFAULT,DROP,INDEX,ALTER,TABLE,VIEW,INSERT,DELETE,UPDATE";

    #region Get

    /// <summary>
    /// 列表(分页).
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PageInputBase input)
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), it => it.FullName.Contains(input.keyword))
            .OrderBy(it => it.SortCode).OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new DataSetListOutput
            {
                id = it.Id,
                fullName = it.FullName,
                objectId = it.ObjectId,
                objectType = it.ObjectType,
                dbLinkId = it.DbLinkId,
                dataConfigJson = it.DataConfigJson,
                fieldJson = it.FieldJson,
                parameterJson = it.ParameterJson,
                description = it.Description
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<DataSetListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await GetEntityInfo(id)).Adapt<DataSetInfoOutput>();
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="input">主键值.</param>
    /// <returns></returns>
    [HttpGet("GetList")]
    public async Task<dynamic> GetDataList([FromQuery] DataSetDataListInput input)
    {
        var list = (await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.ObjectId == input.objectId && it.ObjectType == input.objectType)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .ToListAsync()).Adapt<List<DataSetDataListOutput>>();

        var parameter = new List<SugarParameter>() { new("@formId", null) };
        foreach (var item in list)
        {
            switch (item.type)
            {
                case 1:
                    {
                        var sql = item.dataConfigJson;
                        item.children = await GetFieldModels(item.dbLinkId, sql, parameter);
                    }

                    break;
                case 2:
                    {
                        var filterConfig = item.filterConfigJson.ToObject<FilterConfigModel>();
                        var sql = await GetVisualConfigSql(item.dbLinkId, item.visualConfigJson, filterConfig);
                        item.children = await GetFieldModels(item.dbLinkId, sql, parameter);
                    }

                    break;
                case 3:
                    {
                        var iEntity = await _repository.AsSugarClient().Queryable<DataInterfaceEntity>().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(item.interfaceId));
                        if (iEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1007);
                        item.treePropsName = iEntity.FullName;
                        foreach (var fieldModel in iEntity.FieldJson.ToObject<List<DataInterfaceFieldModel>>())
                        {
                            item.children.Add(new DataSetFieldModel { id = fieldModel.defaultValue, fullName = fieldModel.defaultValue });
                        }
                    }

                    break;
            }
        }

        return list;
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] DataSetCrInput input)
    {
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.ObjectId == input.objectId && it.FullName == input.fullName))
            throw Oops.Oh(ErrorCode.D2801);

        var entity = input.Adapt<DataSetEntity>();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable().SetColumns(it => new DataSetEntity()
        {
            DeleteMark = 1,
            DeleteUserId = _userManager.UserId,
            DeleteTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] DataSetUpInput input)
    {
        if (await _repository.AsQueryable().AnyAsync(it => it.Id != id && it.DeleteMark == null && it.ObjectId == input.objectId && it.FullName == input.fullName))
            throw Oops.Oh(ErrorCode.D2801);

        var entity = input.Adapt<DataSetEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 获取字段.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Fields")]
    [UnitOfWork]
    public async Task<dynamic> GetFields([FromBody] DataSetFieldsInput input)
    {
        var res = new List<DataSetFieldModel>();
        switch (input.type)
        {
            case 1:
                {
                    var sql = string.Empty;
                    if (input.dataConfigJson.IsNotEmptyOrNull())
                    {
                        foreach (var item in _printSensitive.Split(","))
                        {
                            if (input.dataConfigJson.ToUpper().Contains(item + " ") || input.dataConfigJson.ToUpper().Contains(item + "-"))
                                throw Oops.Oh(ErrorCode.xg1005);
                        }

                        sql = input.dataConfigJson;
                    }
                    var parameter = new List<SugarParameter>() { new("@formId", null) };
                    res = await GetFieldModels(input.dbLinkId, sql, parameter);
                }

                break;
            case 2:
                {
                    var filterConfig = input.filterConfigJson.ToObject<FilterConfigModel>();
                    var sql = await GetVisualConfigSql(input.dbLinkId, input.visualConfigJson, filterConfig);
                    var parameter = new List<SugarParameter>() { new("@formId", null) };
                    res = await GetFieldModels(input.dbLinkId, sql, parameter);
                }

                break;
            case 3:
                {
                    var iEntity = await _repository.AsSugarClient().Queryable<DataInterfaceEntity>().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(input.interfaceId));
                    if (iEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1007);
                    if (iEntity.FieldJson.IsNotEmptyOrNull() && iEntity.FieldJson.Equals("[]")) throw Oops.Oh(ErrorCode.D2805);
                    foreach (var fieldModel in iEntity.FieldJson.ToObject<List<DataInterfaceFieldModel>>())
                    {
                        res.Add(new DataSetFieldModel { id = fieldModel.defaultValue, fullName = fieldModel.defaultValue });
                    }
                }

                break;
        }

        if (res.Count == 0) throw Oops.Oh(ErrorCode.D2804);
        return res;
    }

    /// <summary>
    /// 获取数据.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Data")]
    public async Task<dynamic> GetData([FromBody] DataSetDataInput input)
    {
        var res = new Dictionary<string, object>();
        var dataSetList = await _repository.AsQueryable().Where(it => it.DeleteMark == null && it.ObjectType == input.type && it.ObjectId == input.id).ToListAsync();
        foreach (var dataSet in dataSetList)
        {
            var list = new List<Dictionary<string, object>>();
            var filterConfig = dataSet.FilterConfigJson.ToObject<FilterConfigModel>();
            switch (dataSet.Type)
            {
                case 1:
                    {
                        var sql = dataSet.DataConfigJson;
                        list = await GetDataSetData(dataSet.DbLinkId, sql, new List<SugarParameter>());
                    }

                    break;
                case 2:
                    {
                        var sql = await GetVisualConfigSql(dataSet.DbLinkId, dataSet.VisualConfigJson, filterConfig);
                        list = await GetDataSetData(dataSet.DbLinkId, sql, new List<SugarParameter>());
                    }

                    break;
                case 3:
                    {
                        var par = new DataInterfacePreviewInput() { paramList = dataSet.ParameterJson?.ToObject<List<DataInterfaceParameter>>(), pageSize = 999999 };
                        list = (await _dataInterfaceService.GetResponseByType(dataSet.InterfaceId, 0, par)).ToJsonStringOld().ToObject<PageResult<Dictionary<string, object>>>().list;
                    }

                    break;
            }

            // 查询条件
            if (input.queryList.IsNotEmptyOrNull())
            {
                var queryModelList = input.queryList.ToObject<List<DataSetDataQueryModel>>();
                foreach (var par in input.mapStr.ToObject<Dictionary<string, object>>())
                {
                    var name = par.Key.Split("-").ToList();
                    var dataSetName = name.First();
                    var key = name.Last();
                    if (dataSet.FullName.Equals(dataSetName))
                    {
                        var queryModel = queryModelList.Find(x => x.vModel.Equals(dataSetName + "." + key));
                        if (queryModel.IsNotEmptyOrNull())
                        {
                            switch (queryModel.searchType)
                            {
                                case 1:
                                    {
                                        var newList = new List<Dictionary<string, object>>();
                                        if (queryModel.searchMultiple)
                                        {
                                            foreach (var data in par.Value.ToObject<List<object>>())
                                            {
                                                switch (queryModel.selectRange)
                                                {
                                                    case 2:
                                                        switch (queryModel.type)
                                                        {
                                                            case JnpfKeyConst.COMSELECT:
                                                                {
                                                                    var orgId = data.ToString();
                                                                    var orgChildIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.ParentId.Equals(orgId)).Select(it => it.Id).ToListAsync();
                                                                    foreach (var child in orgChildIds)
                                                                    {
                                                                        newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                                                    }
                                                                }

                                                                break;
                                                            case JnpfKeyConst.POSSELECT:
                                                                {
                                                                    var posId = data.ToString();
                                                                    var posChildIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.ParentId.Equals(posId)).Select(it => it.Id).ToListAsync();
                                                                    foreach (var child in posChildIds)
                                                                    {
                                                                        newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
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
                                                                        newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                                                    }
                                                                }

                                                                break;
                                                        }

                                                        break;
                                                    case 3:
                                                        switch (queryModel.type)
                                                        {
                                                            case JnpfKeyConst.COMSELECT:
                                                                var orgId = data.ToString();
                                                                var orgChildIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.OrganizeIdTree.Contains(orgId)).Select(it => it.Id).ToListAsync();
                                                                foreach (var child in orgChildIds)
                                                                {
                                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                                                }

                                                                break;
                                                            case JnpfKeyConst.POSSELECT:
                                                                var posId = data.ToString();
                                                                var posChildIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.PositionIdTree.Contains(posId)).Select(it => it.Id).ToListAsync();
                                                                foreach (var child in posChildIds)
                                                                {
                                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                                                }

                                                                break;
                                                            case JnpfKeyConst.USERSELECT:
                                                                var userId = data.ToString();
                                                                var userChildIds = new List<string>() { userId };
                                                                userChildIds.AddRange(_userManager.GetSubordinates(userId, true).ToList());
                                                                foreach (var child in userChildIds)
                                                                {
                                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(child))).ToList());
                                                                }

                                                                break;
                                                        }

                                                        break;
                                                }

                                                if (data.IsNotEmptyOrNull())
                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(data.ToString()))).ToList());
                                                else
                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNullOrEmpty())).ToList());
                                            }
                                        }
                                        else
                                        {
                                            switch (queryModel.selectRange)
                                            {
                                                case 2:
                                                    switch (queryModel.type)
                                                    {
                                                        case JnpfKeyConst.COMSELECT:
                                                            {
                                                                var orgId = par.Value.ToString();
                                                                var orgChildIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.ParentId.Equals(orgId)).Select(it => it.Id).ToListAsync();
                                                                foreach (var child in orgChildIds)
                                                                {
                                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                                                }
                                                            }

                                                            break;
                                                        case JnpfKeyConst.POSSELECT:
                                                            {
                                                                var posId = par.Value.ToString();
                                                                var posChildIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.ParentId.Equals(posId)).Select(it => it.Id).ToListAsync();
                                                                foreach (var child in posChildIds)
                                                                {
                                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                                                }
                                                            }

                                                            break;
                                                        case JnpfKeyConst.USERSELECT:
                                                            {
                                                                var userId = par.Value.ToString();
                                                                var userChildIds = new List<string>() { userId };
                                                                userChildIds.AddRange(_userManager.GetSubordinates(userId).ToList());
                                                                foreach (var child in userChildIds)
                                                                {
                                                                    newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                                                }
                                                            }

                                                            break;
                                                    }

                                                    break;
                                                case 3:
                                                    switch (queryModel.type)
                                                    {
                                                        case JnpfKeyConst.COMSELECT:
                                                            var orgId = par.Value.ToString();
                                                            var orgChildIds = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.OrganizeIdTree.Contains(orgId)).Select(it => it.Id).ToListAsync();
                                                            foreach (var child in orgChildIds)
                                                            {
                                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                                            }

                                                            break;
                                                        case JnpfKeyConst.POSSELECT:
                                                            var posId = par.Value.ToString();
                                                            var posChildIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.PositionIdTree.Contains(posId)).Select(it => it.Id).ToListAsync();
                                                            foreach (var child in posChildIds)
                                                            {
                                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                                            }

                                                            break;
                                                        case JnpfKeyConst.USERSELECT:
                                                            var userId = par.Value.ToString();
                                                            var userChildIds = new List<string>() { userId };
                                                            userChildIds.AddRange(_userManager.GetSubordinates(userId, true).ToList());
                                                            foreach (var child in userChildIds)
                                                            {
                                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(child))).ToList());
                                                            }

                                                            break;
                                                    }

                                                    break;
                                            }

                                            if (par.Value.IsNotEmptyOrNull())
                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(par.Value.ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty)))).ToList());
                                            else
                                                newList.AddRange(list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNullOrEmpty())).ToList());
                                        }

                                        list = newList.Distinct().ToList();
                                    }

                                    break;
                                case 2:
                                    {
                                        list = list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Contains(par.Value.ToString()))).ToList();
                                    }

                                    break;
                                case 3:
                                    {
                                        var between = par.Value.ToObject<List<object>>();
                                        switch (queryModel.type)
                                        {
                                            case "inputNumber":
                                                {
                                                    var start = between.First().ParseToDecimal();
                                                    var end = between.Last().ParseToDecimal();
                                                    list = list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ParseToDecimal() >= start && xx.Value.ParseToDecimal() <= end)).ToList();
                                                }

                                                break;
                                            case "date":
                                                {
                                                    var start = between.First().ToString().TimeStampToDateTime();
                                                    var end = between.Last().ToString().TimeStampToDateTime();
                                                    list = list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().ParseToDateTime() >= start && xx.Value.ToString().ParseToDateTime() <= end)).ToList();
                                                }

                                                break;
                                            case "time":
                                                {
                                                    var start = Convert.ToDateTime(between.First());
                                                    var end = Convert.ToDateTime(between.Last());
                                                    list = list.Where(x => x.Any(xx => xx.Key.Equals(key) && xx.Value.IsNotEmptyOrNull() && Convert.ToDateTime(xx.Value) >= start && Convert.ToDateTime(xx.Value) <= end)).ToList();
                                                }

                                                break;
                                        }
                                    }

                                    break;
                            }
                        }
                    }
                }
            }

            await ConvertData(list, dataSet, input.convertConfig);
            if (dataSet.Type.Equals(2) && list.Count > 0) list = ResultFiltering(list, filterConfig);
            res.Add(dataSet.FullName, list);
        }

        return res;
    }

    /// <summary>
    /// 参数转换.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("ParameterData")]
    public async Task<dynamic> GetParameterData([FromBody] DataSetDataInput input)
    {
        var res = new Dictionary<string, Dictionary<string, object>>();

        // 系统参数
        var configList = new List<PrintDevConvertConfigModel>();
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamUserId, type = "user" });
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamUserAndSub, type = "user" });
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamUserTree, type = "user" });
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamOrganizeId, type = "organize" });
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamOrganizeAndSub, type = "organize" });
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamOrganizationTree, type = "organize" });
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamPositionId, type = "position" });
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamPositionAndSub, type = "position" });
        configList.Add(new PrintDevConvertConfigModel() { field = CommonConst.SysParamPositionTree, type = "position" });

        var dic = new Dictionary<string, object>();
        dic.Add("@snowFlakeID", input.snowFlakeId.IsNotEmptyOrNull() ? input.snowFlakeId : SnowflakeIdHelper.NextId());
        dic.Add(CommonConst.SysParamUserId, _userManager.UserId);
        dic.Add(CommonConst.SysParamUserAndSub, _userManager.CurrentUserAndSubordinates.ToJsonStringOld());
        dic.Add(CommonConst.SysParamUserTree, _userManager.CurrentUserTree.ToJsonStringOld());
        dic.Add(CommonConst.SysParamOrganizeId, _userManager.CurrentOrganizeList.ToJsonStringOld());
        dic.Add(CommonConst.SysParamOrganizeAndSub, _userManager.CurrentOrganizationAndSubOrganizations.ToJsonStringOld());
        dic.Add(CommonConst.SysParamOrganizationTree, _userManager.CurrentOrganizationTree.ToJsonStringOld());
        dic.Add(CommonConst.SysParamPositionId, _userManager.CurrentPositionList);
        dic.Add(CommonConst.SysParamPositionAndSub, _userManager.CurrentPositionAndSuborganization);
        dic.Add(CommonConst.SysParamPositionTree, _userManager.CurrentPositionTree);
        foreach (var par in input.mapStr.ToObject<Dictionary<string, object>>()) dic.Add(par.Key, par.Value);

        // 查询参数
        foreach (var query in input.queryList.ToObject<List<DataSetDataQueryModel>>())
        {
            var field = query.vModel.Replace(".", "-");
            if (dic.ContainsKey(field))
                configList.Add(new PrintDevConvertConfigModel() { field = field, type = query.type, config = query.config });
        }

        await FieldConversion(configList, dic);

        foreach (var item in dic)
        {
            if (item.Key.Contains('@'))
            {
                res.Add(item.Key, new Dictionary<string, object> { { item.Key, item.Value } });
            }
            else
            {
                var name = item.Key.Split("-").ToList();
                var dataSetName = name.First();
                var key = name.Last();
                if (res.ContainsKey(dataSetName))
                    res[dataSetName].Add(key, item.Value);
                else
                    res.Add(dataSetName, new Dictionary<string, object> { { key, item.Value } });
            }
        }

        return res;
    }

    /// <summary>
    /// 批量保存.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Save")]
    [UnitOfWork]
    public async Task Save([FromBody] DataSetSaveInput input)
    {
        await SaveDataSetList(input.objectId, input.objectType, input.list);
    }

    /// <summary>
    /// 数据预览.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("GetPreviewData")]
    public async Task<dynamic> GetPreviewData([FromBody] DataSetFieldsInput input)
    {
        var output = new DataSetPreviewDataOutput();
        var sql = string.Empty;
        switch (input.type)
        {
            case 1:
            case 2:
                try
                {
                    if (input.visualConfigJson.IsNotEmptyOrNull())
                    {
                        var link = await _dbLinkService.GetInfo(input.dbLinkId);
                        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

                        // 列
                        var visualConfig = input.visualConfigJson.ToObject<List<VisualConfigModel>>().First();
                        if (visualConfig.children.Any())
                        {
                            var isUpper = tenantLink.DbType.ToLower().Equals("oracle") ? true : false;

                            var fieldAliasDic = new Dictionary<string, string>();
                            GetAllAlias(visualConfig, fieldAliasDic);
                            foreach (var field in fieldAliasDic)
                            {
                                var value = isUpper ? field.Value.ToUpper() : field.Value;
                                var dic = new Dictionary<string, string>();
                                dic.Add("label", value);
                                dic.Add("title", value.Split("(").First());
                                output.previewColumns.Add(dic);
                            }
                        }
                        else
                        {
                            foreach (var field in visualConfig.fieldList)
                            {
                                var dic = new Dictionary<string, string>();
                                dic.Add("label", field.fieldName);
                                dic.Add("title", field.field);
                                output.previewColumns.Add(dic);
                            }
                        }

                        var filterConfig = input.filterConfigJson.ToObject<FilterConfigModel>();
                        sql = await GetVisualConfigSql(input.dbLinkId, input.visualConfigJson, filterConfig);
                        output.previewSqlText = new SQLFormatter(sql).Format();

                        // 数据
                        _sqlSugarClient = _databaseService.ChangeDataBase(tenantLink);
                        DataTable dt = _sqlSugarClient.SqlQueryable<object>(sql).ToDataTable();
                        _sqlSugarClient.AsTenant().ChangeDatabase("default");

                        var list = dt.ToJsonStringOld().ToObject<List<Dictionary<string, object>>>();
                        if (input.type.Equals(2) && list.Count > 0) list = ResultFiltering(list, filterConfig);

                        output.previewData = list.Take(20).ToList();
                    }
                }
                catch (Exception)
                {
                    throw Oops.Oh(ErrorCode.D1511);
                }

                break;
            case 3:
                var iEntity = await _repository.AsSugarClient().Queryable<DataInterfaceEntity>().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(input.interfaceId));
                if (iEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1007);
                if (iEntity.FieldJson.IsNotEmptyOrNull() && iEntity.FieldJson.Equals("[]")) throw Oops.Oh(ErrorCode.D2805);

                // 列
                foreach (var fieldModel in iEntity.FieldJson.ToObject<List<DataInterfaceFieldModel>>())
                {
                    var dic = new Dictionary<string, string>();
                    dic.Add("label", fieldModel.field);
                    dic.Add("title", fieldModel.defaultValue);
                    output.previewColumns.Add(dic);
                }

                var systemParamter = new Dictionary<string, object> { { "@formId", "@formId" } };
                var par = new DataInterfacePreviewInput()
                {
                    paramList = input.parameterJson.ToObject<List<DataInterfaceParameter>>(),
                    systemParamter = systemParamter,
                    currentPage = 1,
                    pageSize = 20
                };

                var data = (await _dataInterfaceService.GetResponseByType(input.interfaceId, 0, par)).ToObject<PageResult<Dictionary<string, object>>>();
                output.previewData = data.list;
                break;
        }

        return output;
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取字段模型.
    /// </summary>
    /// <param name="dbLinkId"></param>
    /// <param name="sql"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<DataSetFieldModel>> GetFieldModels(string dbLinkId, string sql, List<SugarParameter> parameter)
    {
        var link = await _dbLinkService.GetInfo(dbLinkId);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        if (!_dataBaseManager.IsConnection(tenantLink)) throw Oops.Oh(ErrorCode.D1507);
        var dt = _dataBaseManager.GetSqlData(tenantLink, sql, false, parameter.ToArray());

        var models = new List<DataSetFieldModel>();
        foreach (var item in dt.Columns)
        {
            models.Add(new DataSetFieldModel()
            {
                id = item.ToString(),
                fullName = item.ToString()
            });
        }

        return models;
    }

    /// <summary>
    /// 保存数据集列表.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectType"></param>
    /// <param name="modelList"></param>
    /// <returns></returns>
    [NonAction]
    public async Task SaveDataSetList(string objectId, string objectType, List<PrintDevDataSetModel> modelList)
    {
        var ids = modelList.Where(it => it.id.IsNotEmptyOrNull()).Select(x => x.id).ToList();
        await _repository.AsUpdateable().SetColumns(it => new DataSetEntity
        {
            DeleteMark = 1,
            DeleteTime = SqlFunc.GetDate(),
            DeleteUserId = _userManager.UserId
        }).Where(it => it.DeleteMark == null && !ids.Contains(it.Id) && it.ObjectId == objectId).ExecuteCommandAsync();

        foreach (var model in modelList)
        {
            var entity = model.Adapt<DataSetEntity>();
            entity.ObjectId = objectId;
            entity.ObjectType = objectType;
            if (entity.Id.IsNullOrEmpty())
                await _repository.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            else
                await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 获取配置式sql.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<string> GetVisualConfigSql(string dbLinkId, string visualConfigJson, FilterConfigModel filterConfig)
    {
        var link = await _dbLinkService.GetInfo(dbLinkId);
        var dbLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

        // 租户字段
        var tenantId = string.Empty;
        var tenantCache = new GlobalTenantCacheModel();
        if (_tenant.MultiTenancy) tenantCache = _cacheManager.Get<List<GlobalTenantCacheModel>>(CommonConst.GLOBALTENANT).Find(it => it.TenantId.Equals(dbLink.Id));
        if (tenantCache.IsNotEmptyOrNull() && tenantCache.type.Equals(1)) tenantId = tenantCache.connectionConfig.IsolationField;

        var res = string.Empty;
        var tableAliasDic = new Dictionary<string, string>();
        var fieldAliasDic = new Dictionary<string, string>();
        var visualConfig = visualConfigJson.ToObject<List<VisualConfigModel>>().First();
        if (visualConfig.children.Any())
        {
            // 获取表别名和字段别名
            GetAllAlias(visualConfig, fieldAliasDic, tableAliasDic);

            // 主表sql
            var mainSql = await GetTableAliasSql(dbLink, visualConfig, fieldAliasDic, tableAliasDic, tenantId);

            // 子表sql
            var allChildSql = string.Empty;
            var childWhereSql = new List<string>();
            foreach (var child in visualConfig.children)
            {
                var childSql = string.Empty;
                var subChildWhereSql = new List<string>();
                if (child.children.Any())
                {
                    var curAndSubChildSql = string.Empty;
                    var childCurAndSubFieldAlias = new List<string>();
                    GetCurAndSubFieldAlias(child, fieldAliasDic, childCurAndSubFieldAlias);

                    var curChildSql = await GetTableAliasSql(dbLink, child, fieldAliasDic, tableAliasDic, tenantId);
                    curAndSubChildSql = string.Format(BaseSqlConst.QUERY, string.Join(",", childCurAndSubFieldAlias), curChildSql);

                    foreach (var subChild in child.children)
                    {
                        var subChildSql = await GetTableAliasSql(dbLink, subChild, fieldAliasDic, tableAliasDic, tenantId);
                        var childConnect = GetConnectSql(dbLink, subChild.relationConfig.type);

                        // 关联条件
                        var subRelationList = new List<string>();
                        foreach (var subRelation in subChild.relationConfig.relationList)
                        {
                            var field = fieldAliasDic[subChild.table + "." + subRelation.field].Split("(").First();
                            var pField = fieldAliasDic[subChild.parentTable + "." + subRelation.pField].Split("(").First();
                            subRelationList.Add(field + " = " + pField);
                        }

                        if (subChild.relationConfig.ruleList.Any())
                            subChildWhereSql.Add(await GetRuleSql(dbLink, subChild.relationConfig.matchLogic, subChild.relationConfig.ruleList, true, fieldAliasDic, false));

                        curAndSubChildSql += string.Format(childConnect, subChildSql, string.Join(" AND ", subRelationList));
                    }

                    // 子表的子表连接配置条件
                    if (subChildWhereSql.Any()) curAndSubChildSql += string.Format("WHERE {0}", string.Join(" AND ", subChildWhereSql));

                    childSql = string.Format("({0}) {1} ", curAndSubChildSql, tableAliasDic[child.table]);
                }
                else
                {
                    childSql = await GetTableAliasSql(dbLink, child, fieldAliasDic, tableAliasDic, tenantId);
                }

                // 关联条件
                var relationList = new List<string>();
                foreach (var relation in child.relationConfig.relationList)
                {
                    var field = fieldAliasDic[child.table + "." + relation.field].Split("(").First();
                    var pField = fieldAliasDic[child.parentTable + "." + relation.pField].Split("(").First();
                    relationList.Add(field + " = " + pField);
                }

                if (child.relationConfig.ruleList.Any())
                    childWhereSql.Add(await GetRuleSql(dbLink, child.relationConfig.matchLogic, child.relationConfig.ruleList, true, fieldAliasDic, false));

                var connect = GetConnectSql(dbLink, child.relationConfig.type);
                allChildSql += string.Format(connect, childSql, string.Join(" AND ", relationList));
            }

            var fields = fieldAliasDic.Select(x => x.Value.Split("(").First()).ToList();
            res = string.Format(BaseSqlConst.QUERY, string.Join(",", fields), mainSql + allChildSql);

            // 子表连接配置条件
            if (childWhereSql.Any()) res += string.Format("WHERE {0}", string.Join(" AND ", childWhereSql));
        }
        else
        {
            var fields = visualConfig.fieldList.ConvertAll(it => it.field);
            res = string.Format(BaseSqlConst.QUERY, string.Join(",", fields), visualConfig.table);

            if (tenantId.IsNotEmptyOrNull())
                res = string.Format("{0} WHERE f_tenant_id = '{1}'", res, tenantId);

            if (visualConfig.ruleList.Any())
            {
                if (tenantId.IsNotEmptyOrNull())
                {
                    res += " AND ";
                    res += await GetRuleSql(dbLink, visualConfig.matchLogic, visualConfig.ruleList, false, null, false);
                }
                else
                {
                    res += await GetRuleSql(dbLink, visualConfig.matchLogic, visualConfig.ruleList);
                }
            }
        }

        res = string.Format(BaseSqlConst.QUERY, "*", "(" + res + ") A");

        if (filterConfig.ruleList.Any())
        {
            res = string.Format(BaseSqlConst.QUERY, "*", "(" + res + ") A");
            res += await GetRuleSql(dbLink, filterConfig.matchLogic, filterConfig.ruleList, visualConfig.children.Any() ? true : false, fieldAliasDic);
        }

        return res;
    }

    /// <summary>
    /// 获取数据集数据.
    /// </summary>
    /// <param name="dbLinkId"></param>
    /// <param name="sql"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<Dictionary<string, object>>> GetDataSetData(string dbLinkId, string sql, List<SugarParameter> parameter)
    {
        var res = new List<Dictionary<string, object>>();

        var link = await _dbLinkService.GetInfo(dbLinkId);
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
        var dataTable = _dataBaseManager.GetSqlData(tenantLink, sql, false, parameter.ToArray());
        if (dataTable.Rows.Count > 0) res = DictionaryExtensions.DataTableToDicList(dataTable);

        return res;
    }

    /// <summary>
    /// 转换数据.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="dataSet"></param>
    /// <param name="convertConfigJson"></param>
    /// <returns></returns>
    [NonAction]
    public async Task ConvertData(List<Dictionary<string, object>> list, DataSetEntity dataSet, string convertConfigJson)
    {
        if (list.Count > 0)
        {
            var convertConfig = convertConfigJson.IsNotEmptyOrNull() ? convertConfigJson.ToObject<List<PrintDevConvertConfigModel>>() : new List<PrintDevConvertConfigModel>();
            var config = convertConfig?.FindAll(it => it.field.Contains(dataSet.FullName + "."));
            if (config.IsNotEmptyOrNull() && config.Count > 0)
            {
                var newList = new List<Dictionary<string, object>>();
                foreach (var data in list)
                {
                    await FieldConversion(config, data);
                    newList.Add(data);
                }

                list = newList;
            }
        }
    }

    /// <summary>
    /// 结果集筛选.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="filterConfig"></param>
    /// <returns></returns>
    [NonAction]
    public List<Dictionary<string, object>> ResultFiltering(List<Dictionary<string, object>> list, FilterConfigModel filterConfig)
    {
        var newList = new List<Dictionary<string, object>>();
        switch (filterConfig.resultFilter)
        {
            case 1:
                return list;
            case 2:
                newList = list.Take(filterConfig.resultNum).ToList();
                break;
            case 3:
                newList = list.Skip(list.Count - filterConfig.resultNum).ToList();
                break;
            case 4:
                for (var i = 0; i < list.Count; i++)
                {
                    if (i % 2 == 0)
                        newList.Add(list[i]);
                }

                break;
            case 5:
                for (var i = 0; i < list.Count; i++)
                {
                    if (i % 2 == 1)
                        newList.Add(list[i]);
                }

                break;
            case 6:
                foreach (var item in filterConfig.specifyData.Split(","))
                {
                    if (item.Contains('-'))
                    {
                        var items = item.Split("-").ToObject<List<int>>();
                        if (list.Count >= items[1])
                            newList.AddRange(list.GetRange(items[0] - 1, items[1] - items[0] + 1));
                    }
                    else
                    {
                        var i = item.ParseToInt();
                        if (list.Count >= i)
                            newList.Add(list[i - 1]);
                    }
                }

                break;
        }

        return newList;
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<DataSetEntity> GetEntityInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
    }

    /// <summary>
    /// 获取所有别名.
    /// </summary>
    private int GetAllAlias(VisualConfigModel configModel, Dictionary<string, string> fieldAliasDic, Dictionary<string, string>? tableAliasDic = null, int num = 97)
    {
        var letter = ((char)num).ToString();

        // 表
        if (tableAliasDic.IsNotEmptyOrNull()) tableAliasDic.Add(configModel.table, string.Format("{0}_{1}", letter, configModel.table));

        // 字段
        foreach (var field in configModel.fieldList)
        {
            fieldAliasDic.Add(string.Format("{0}.{1}", configModel.table, field.field), string.Format("{0}_{1}", letter, field.fieldName));
        }

        num++;
        foreach (var child in configModel.children)
        {
            num = GetAllAlias(child, fieldAliasDic, tableAliasDic, num);
        }

        return num;
    }

    /// <summary>
    /// 获取当前及下级的字段别名.
    /// </summary>
    private void GetCurAndSubFieldAlias(VisualConfigModel configModel, Dictionary<string, string> fieldAliasDic, List<string> fieldAliasList)
    {
        foreach (var field in configModel.fieldList)
        {
            var key = string.Format("{0}.{1}", configModel.table, field.field);
            fieldAliasList.Add(fieldAliasDic[key].Split("(").First());
        }

        foreach (var child in configModel.children)
        {
            GetCurAndSubFieldAlias(child, fieldAliasDic, fieldAliasList);
        }
    }

    /// <summary>
    /// 获取表别名sql语句.
    /// </summary>
    private async Task<string> GetTableAliasSql(DbLinkEntity dbLink, VisualConfigModel configModel, Dictionary<string, string> fieldAliasDic, Dictionary<string, string> tableAliasDic, string? tenantId = null)
    {
        var fields = new List<string>();
        foreach (var field in configModel.fieldList)
        {
            var newField = fieldAliasDic[configModel.table + "." + field.field].Split("(").First();
            fields.Add(string.Format("{0} AS {1}", field.field, newField));
        }

        // 条件
        var conditionSql = string.Empty;
        if (tenantId.IsNotEmptyOrNull()) conditionSql = string.Format(" WHERE f_tenant_id = '{0}'", tenantId);
        if (configModel.ruleList.Any())
        {
            if (tenantId.IsNotEmptyOrNull())
                conditionSql += " AND " + await GetRuleSql(dbLink, configModel.matchLogic, configModel.ruleList, false, null, false);
            else
                conditionSql = await GetRuleSql(dbLink, configModel.matchLogic, configModel.ruleList);
        }

        return string.Format(BaseSqlConst.QUERY_ALIAS, string.Join(",", fields), configModel.table + conditionSql, tableAliasDic[configModel.table]);
    }

    /// <summary>
    /// 获取连接sql.
    /// </summary>
    private string GetConnectSql(DbLinkEntity dbLink, int type)
    {
        switch (type)
        {
            case 1:
                return BaseSqlConst.LEFT_CONNECTION;
            case 2:
                return BaseSqlConst.RIGHT_CONNECTION;
            case 3:
                return BaseSqlConst.INNER_CONNECTION;
            case 4:
                if (dbLink.DbType.ToLower().Equals("mysql")) throw Oops.Oh(ErrorCode.D2803);
                return BaseSqlConst.FULL_CONNECTION;
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// 获取条件sql.
    /// </summary>
    private async Task<string> GetRuleSql(DbLinkEntity dbLink, string matchLogic, List<RuleModel> ruleList, bool isAlias = false, Dictionary<string, string>? fieldAliasDic = null, bool isWhere = true)
    {
        var res = string.Empty;
        var resultList = new List<object>();
        var whereType = matchLogic.ToUpper().Equals("AND") ? (int)WhereType.And : (int)WhereType.Or;
        foreach (var conditionItem in ruleList)
        {
            var groupList = new List<object>();
            var groupWhere = conditionItem.logic.Equals("or") ? (int)WhereType.Or : (int)WhereType.And;
            foreach (var model in conditionItem.groups)
            {
                var itemValue = new object();
                switch (model.fieldValue)
                {
                    case CommonConst.SysParamUserId:
                        itemValue = _userManager.UserId;
                        break;
                    case CommonConst.SysParamUserAndSub:
                        itemValue = _userManager.CurrentUserAndSubordinates.ToJsonStringOld();
                        break;
                    case CommonConst.SysParamUserTree:
                        itemValue = _userManager.CurrentUserTree.ToJsonStringOld();
                        break;
                    case CommonConst.SysParamPositionId:
                        itemValue = _userManager.CurrentPositionList;
                        break;
                    case CommonConst.SysParamPositionAndSub:
                        itemValue = _userManager.CurrentPositionAndSuborganization;
                        break;
                    case CommonConst.SysParamPositionTree:
                        itemValue = _userManager.CurrentPositionTree;
                        break;
                    case CommonConst.SysParamOrganizeId:
                        itemValue = _userManager.CurrentOrganizeList.ToJsonStringOld();
                        break;
                    case CommonConst.SysParamOrganizeAndSub:
                        itemValue = _userManager.CurrentOrganizationAndSubOrganizations.ToJsonStringOld();
                        break;
                    case CommonConst.SysParamOrganizationTree:
                        itemValue = _userManager.CurrentOrganizationTree.ToJsonStringOld();
                        break;
                    default:
                        itemValue = model.fieldValue;
                        break;
                }

                model.symbol = model.symbol.ReplaceOp();
                var itemMethod = (QueryType)Enum.Parse(typeof(QueryType), model.symbol);

                var between = new List<object>();
                string? cSharpTypeName = null;
                if (itemValue.IsNotEmptyOrNull())
                {
                    switch (model.dataType)
                    {
                        case "double":
                        case "bigint":
                            if (itemMethod.Equals(QueryType.Between))
                            {
                                var list = itemValue.ToString().ToObject<List<decimal>>();
                                between.Add(list[0]);
                                between.Add(list[1]);
                            }

                            cSharpTypeName = "decimal";
                            break;
                        case "date":
                        case "time":
                            if (itemMethod.Equals(QueryType.Between))
                            {
                                var list = itemValue.ToString().ToObject<List<string>>();
                                between.Add(list[0].TimeStampToDateTime().ToString());
                                between.Add(list[1].TimeStampToDateTime().ToString());
                            }
                            else
                            {
                                itemValue = itemValue.ToString().TimeStampToDateTime().ToString();
                            }

                            cSharpTypeName = "datetime";
                            break;
                    }
                }

                var conditionalList = new List<object>();
                var fieldName = isAlias && fieldAliasDic.IsNotEmptyOrNull() ? fieldAliasDic[model.field.Replace("-", ".")].Split("(").First() : model.field.Split("-").Last();
                switch (itemMethod)
                {
                    case QueryType.Equal:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.Equal } });
                        break;
                    case QueryType.NotEqual:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.NoEqual } });
                        break;
                    case QueryType.Included:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.Like } });
                        break;
                    case QueryType.NotIncluded:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.NoLike } });
                        break;
                    case QueryType.GreaterThan:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.GreaterThan } });
                        break;
                    case QueryType.GreaterThanOrEqual:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.GreaterThanOrEqual } });
                        break;
                    case QueryType.LessThan:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.LessThan } });
                        break;
                    case QueryType.LessThanOrEqual:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.LessThanOrEqual } });
                        break;
                    case QueryType.Between:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = between[0], CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.GreaterThanOrEqual } });
                        conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = fieldName, FieldValue = between[1], CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.LessThanOrEqual } });
                        break;
                    case QueryType.Null:
                        if (model.dataType.Equals("double") || model.dataType.Equals("bigint"))
                            conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.EqualNull } });
                        else
                            conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.IsNullOrEmpty } });
                        break;
                    case QueryType.NotNull:
                        conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue, CSharpTypeName = cSharpTypeName, ConditionalType = ConditionalType.IsNot } });
                        break;
                    case QueryType.In:
                    case QueryType.NotIn:
                        if (itemValue.IsNotEmptyOrNull())
                        {
                            if (itemValue.ToString().StartsWith('['))
                            {
                                var ids = itemValue.ToString().ToObject<List<string>>();
                                for (int i = 0; i < ids.Count; i++)
                                {
                                    var type = i == 0 ? groupWhere : itemMethod.Equals(QueryType.In) ? (int)WhereType.Or : (int)WhereType.And;
                                    conditionalList.Add(new { Key = type, Value = new { FieldName = fieldName, FieldValue = ids[i], ConditionalType = itemMethod.Equals(QueryType.In) ? ConditionalType.Like : ConditionalType.NoLike, CSharpTypeName = cSharpTypeName } });
                                }
                            }
                            else
                            {
                                conditionalList.Add(new { Key = groupWhere, Value = new { FieldName = fieldName, FieldValue = itemValue.ToString(), ConditionalType = itemMethod.Equals(QueryType.In) ? ConditionalType.Like : ConditionalType.NoLike, CSharpTypeName = cSharpTypeName } });
                            }

                            if (itemMethod.Equals(QueryType.NotIn))
                            {
                                conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = fieldName, FieldValue = (string)null, ConditionalType = ConditionalType.IsNot, CSharpTypeName = cSharpTypeName } });
                                conditionalList.Add(new { Key = (int)WhereType.And, Value = new { FieldName = fieldName, FieldValue = string.Empty, ConditionalType = ConditionalType.IsNot, CSharpTypeName = cSharpTypeName } });
                            }
                        }

                        break;
                }

                if (conditionalList.Any())
                {
                    if (groupList.Any())
                        groupList.Add(new { Key = groupWhere, Value = new { ConditionalList = conditionalList } });
                    else
                        groupList.Add(new { Key = whereType, Value = new { ConditionalList = conditionalList } });
                }
            }

            if (groupList.Any())
                resultList.Add(new { Key = whereType, Value = new { ConditionalList = groupList } });
        }

        if (resultList.Any())
        {
            var con = new List<object> { new { ConditionalList = resultList } };
            var conModels = _repository.AsSugarClient().Utilities.JsonToConditionalModels(con.ToJsonStringOld());

            _sqlSugarClient = _databaseService.ChangeDataBase(dbLink);
            var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(conModels).ToSqlString();
            _sqlSugarClient.AsTenant().ChangeDatabase("default");
            res = itemWhere.Split("WHERE").Last();

            if (isWhere) res = string.Format(" WHERE {0}", res);
        }

        return res;
    }

    /// <summary>
    /// 字段转换.
    /// </summary>
    /// <param name="modelList"></param>
    /// <param name="dic"></param>
    /// <returns></returns>
    private async Task FieldConversion(List<PrintDevConvertConfigModel> modelList, Dictionary<string, object> dic)
    {
        var allorg = new List<OrganizeEntity>();
        if (modelList.Any(it => it.type == "organize") || modelList.Any(it => it.type == "position") || modelList.Any(it => it.type == "users")) allorg = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null).Select(it => new OrganizeEntity { Id = it.Id, OrganizeNameTree = it.OrganizeNameTree }).ToListAsync();
        var allpos = new List<PositionEntity>();
        if (modelList.Any(it => it.type == "position") || modelList.Any(it => it.type == "users")) allpos = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null).Select(it => new PositionEntity { Id = it.Id, FullName = it.FullName, OrganizeId = it.OrganizeId }).ToListAsync();
        var allrole = new List<RoleEntity>();
        if (modelList.Any(it => it.type == "role") || modelList.Any(it => it.type == "users")) allrole = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(it => it.DeleteMark == null).Select(it => new RoleEntity { Id = it.Id, FullName = it.FullName }).ToListAsync();
        var allgroup = new List<GroupEntity>();
        if (modelList.Any(it => it.type == "group") || modelList.Any(it => it.type == "users")) allgroup = await _repository.AsSugarClient().Queryable<GroupEntity>().Where(it => it.DeleteMark == null).Select(it => new GroupEntity { Id = it.Id, FullName = it.FullName }).ToListAsync();
        var alluser = new List<UserEntity>();
        if (modelList.Any(it => it.type == "user") || modelList.Any(it => it.type == "users")) alluser = await _repository.AsSugarClient().Queryable<UserEntity>().Where(it => it.DeleteMark == null).Select(it => new UserEntity { Id = it.Id, Account = it.Account, RealName = it.RealName }).ToListAsync();
        var alladdress = new List<ProvinceEntity>();
        if (modelList.Any(it => it.type == "address")) alladdress = await _repository.AsSugarClient().Queryable<ProvinceEntity>().Where(it => it.DeleteMark == null).Select(it => new ProvinceEntity { Id = it.Id, FullName = it.FullName }).ToListAsync();

        foreach (var model in modelList)
        {
            var modelName = model.field.Split(".").Last();
            if (dic.ContainsKey(modelName) && dic[modelName].IsNotEmptyOrNull())
            {
                switch (model.type)
                {
                    case "select":
                        {
                            if (dic[modelName].ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty).StartsWith("[["))
                            {
                                var list = dic[modelName].ToString().ToObject<List<List<string>>>();
                                var nameList = new List<string>();
                                foreach (var item in list)
                                {
                                    var cNameList = new List<string>();
                                    foreach (var citem in item)
                                    {
                                        cNameList.Add(await GetSelectField(model, citem));
                                    }

                                    nameList.Add(string.Join("/", cNameList));
                                }

                                dic[modelName] = string.Join(",", nameList);
                            }
                            else if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                var nameList = new List<string>();
                                foreach (var item in list)
                                {
                                    nameList.Add(await GetSelectField(model, item));
                                }

                                dic[modelName] = string.Join(",", nameList);
                            }
                            else
                            {
                                dic[modelName] = await GetSelectField(model, dic[modelName].ToString());
                            }
                        }

                        break;
                    case "date":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                var dateList = new List<string>();
                                foreach (var item in list)
                                    dateList.Add(string.Format("{0:" + model.config.format + "}", item.TimeStampToDateTime()));

                                dic[modelName] = string.Join("～", dateList);
                            }
                            else
                            {
                                if (dic[modelName] is long) dic[modelName] = dic[modelName].ToString().TimeStampToDateTime();
                                dic[modelName] = string.Format("{0:" + model.config.format + "} ", dic[modelName]);
                            }
                        }

                        break;
                    case "time":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                dic[modelName] = string.Join("～", list);
                            }
                        }

                        break;
                    case "number":
                    case "inputNumber":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                dic[modelName] = string.Join("～", list);
                            }
                            else
                            {
                                if (dic[modelName].ToString().Contains('.'))
                                {
                                    var dataList = dic[modelName].ToString().Split('.').ToList();

                                    if (model.config.thousands) dataList[0] = string.Format("{0:N0}", dataList[0].ParseToLong());

                                    if (model.config.precision == 0)
                                    {
                                        dic[modelName] = dataList[0];
                                    }
                                    else
                                    {
                                        if (model.config.precision > dataList.Last().Length)
                                        {
                                            dic[modelName] = dataList[0] + "." + dataList.Last().PadRight(model.config.precision, '0');
                                        }
                                        else
                                        {
                                            dic[modelName] = dataList[0] + "." + dataList.Last().Substring(0, model.config.precision);
                                        }
                                    }
                                }
                                else
                                {
                                    if (model.config.thousands) dic[modelName] = string.Format("{0:N0}", dic[modelName].ParseToLong());

                                    if (model.config.precision > 0) dic[modelName] += ".".PadRight(model.config.precision + 1, '0');
                                }
                            }
                        }

                        break;
                    case "address":
                        {
                            if (dic[modelName].ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty).StartsWith("[["))
                            {
                                var treeList = dic[modelName].ToString().ToObject<List<List<string>>>();
                                var treeNameList = new List<string>();
                                foreach (var list in treeList)
                                {
                                    var nameList = new List<string>();
                                    foreach (var item in list)
                                    {
                                        var address = alladdress.Find(it => it.Id == item);
                                        if (address.IsNotEmptyOrNull()) nameList.Add(address.FullName);
                                    }

                                    treeNameList.Add(string.Join("/", nameList));
                                }

                                dic[modelName] = string.Join(",", treeNameList);
                            }
                            else if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                var nameList = new List<string>();
                                foreach (var item in list)
                                {
                                    var address = alladdress.Find(it => it.Id == item);
                                    if (address.IsNotEmptyOrNull()) nameList.Add(address.FullName);
                                }

                                dic[modelName] = string.Join("/", nameList);
                            }
                        }

                        break;
                    case "location":
                        {
                            var value = dic[modelName]?.ToString().ToObject<Dictionary<string, object>>();
                            if (value.IsNotEmptyOrNull() && value.ContainsKey("fullAddress"))
                                dic[modelName] = value["fullAddress"];
                        }

                        break;
                    case "organize":
                    case "organizeSelect":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var dataList = dic[modelName].ToString().ToObject<List<string>>();
                                var dataNameList = new List<string>();
                                foreach (var data in dataList)
                                {
                                    var org = allorg.Find(it => it.Id == data);
                                    if (org.IsNotEmptyOrNull()) dataNameList.Add(org.OrganizeNameTree);
                                }

                                dic[modelName] = string.Join(",", dataNameList);
                            }
                            else
                            {
                                var data = dic[modelName].ToString();
                                var org = allorg.Find(it => it.Id == data);
                                if (org.IsNotEmptyOrNull()) dic[modelName] = org.OrganizeNameTree;
                            }
                        }

                        break;
                    case "position":
                    case "posSelect":
                    case "positionSelect":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                var nameList = new List<string>();
                                foreach (var item in list)
                                {
                                    var pos = allpos.Find(it => it.Id == item);
                                    if (pos.IsNotEmptyOrNull())
                                    {
                                        var org = allorg.Find(x => x.Id == pos.OrganizeId);
                                        if (org.IsNotEmptyOrNull())
                                        {
                                            var name = string.Format("{0}/{1}", org.OrganizeNameTree, pos.FullName);
                                            nameList.Add(name);
                                        }
                                    }
                                }

                                dic[modelName] = string.Join(",", nameList);
                            }
                            else
                            {
                                var pos = allpos.Find(it => it.Id == dic[modelName].ToString());
                                if (pos.IsNotEmptyOrNull())
                                {
                                    var org = allorg.Find(x => x.Id == pos.OrganizeId);
                                    if (org.IsNotEmptyOrNull()) dic[modelName] = string.Format("{0}/{1}", org.OrganizeNameTree, pos.FullName);
                                }
                            }
                        }

                        break;
                    case "role":
                    case "roleSelect":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                var nameList = new List<string>();
                                foreach (var item in list)
                                {
                                    var role = allrole.Find(it => it.Id == item);
                                    if (role.IsNotEmptyOrNull()) nameList.Add(role.FullName);
                                }

                                dic[modelName] = string.Join(",", nameList);
                            }
                            else
                            {
                                var role = allrole.Find(it => it.Id == dic[modelName].ToString());
                                if (role.IsNotEmptyOrNull()) dic[modelName] = role.FullName;
                            }
                        }

                        break;
                    case "group":
                    case "groupSelect":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                var nameList = new List<string>();
                                foreach (var item in list)
                                {
                                    var group = allgroup.Find(it => it.Id == item);
                                    if (group.IsNotEmptyOrNull()) nameList.Add(group.FullName);
                                }

                                dic[modelName] = string.Join(",", nameList);
                            }
                            else
                            {
                                var group = allgroup.Find(it => it.Id == dic[modelName].ToString());
                                if (group.IsNotEmptyOrNull()) dic[modelName] = group.FullName;
                            }
                        }

                        break;
                    case "user":
                    case "userSelect":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                var nameList = new List<string>();
                                foreach (var item in list)
                                {
                                    var user = alluser.Find(it => it.Id == item);
                                    if (user.IsNotEmptyOrNull()) nameList.Add(string.Format("{0}/{1}", user.RealName, user.Account));
                                }

                                dic[modelName] = string.Join(",", nameList);
                            }
                            else
                            {
                                var user = alluser.Find(it => it.Id == dic[modelName].ToString());
                                if (user.IsNotEmptyOrNull()) dic[modelName] = string.Format("{0}/{1}", user.RealName, user.Account);
                            }
                        }

                        break;
                    case "users":
                        {
                            if (dic[modelName].ToString().StartsWith('['))
                            {
                                var list = dic[modelName].ToString().ToObject<List<string>>();
                                var nameList = new List<string>();
                                foreach (var item in list)
                                {
                                    var value = item.Split("--").ToList();
                                    if (value.Last() == "company" || value.Last() == "department")
                                    {
                                        var org = allorg.Find(it => it.Id == value.First());
                                        if (org.IsNotEmptyOrNull()) nameList.Add(org.FullName);
                                    }
                                    else if (value.Last() == "position")
                                    {
                                        var pos = allpos.Find(it => it.Id == value.First());
                                        if (pos.IsNotEmptyOrNull()) nameList.Add(pos.FullName);
                                    }
                                    else if (value.Last() == "role")
                                    {
                                        var role = allrole.Find(it => it.Id == value.First());
                                        if (role.IsNotEmptyOrNull()) nameList.Add(role.FullName);
                                    }
                                    else if (value.Last() == "user")
                                    {
                                        var user = alluser.Find(it => it.Id == value.First());
                                        if (user.IsNotEmptyOrNull()) nameList.Add(string.Format("{0}/{1}", user.RealName, user.Account));
                                    }
                                    else if (value.Last() == "group")
                                    {
                                        var group = allgroup.Find(it => it.Id == value.First());
                                        if (group.IsNotEmptyOrNull()) nameList.Add(group.FullName);
                                    }
                                }

                                dic[modelName] = string.Join(",", nameList);
                            }
                            else
                            {
                                var user = alluser.Find(it => dic[modelName].ToString().Contains(it.Id));
                                if (user.IsNotEmptyOrNull()) dic[modelName] = string.Format("{0}/{1}", user.RealName, user.Account);
                            }
                        }

                        break;
                }
            }
        }
    }

    /// <summary>
    /// 枚举字段.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="oldData"></param>
    /// <returns></returns>
    private async Task<string> GetSelectField(PrintDevConvertConfigModel model, string oldData)
    {
        var newData = oldData;
        switch (model.config.dataType)
        {
            case "static":
                {
                    var option = model.config.options.Find(it => it.id == oldData);
                    if (option.IsNotEmptyOrNull()) newData = option.fullName;
                }

                break;
            case "dictionary":
                {
                    var dicData = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(it => it.DeleteMark == null && it.DictionaryTypeId == model.config.dictionaryType).ToListAsync();
                    if (dicData.IsNotEmptyOrNull())
                    {
                        var data = dicData.WhereIF(model.config.propsValue == "id", it => it.Id == oldData).WhereIF(model.config.propsValue == "enCode", it => it.EnCode == oldData).FirstOrDefault();
                        if (data.IsNotEmptyOrNull()) newData = data.FullName;
                    }
                }

                break;
            case "dynamic":
                {
                    var par = new DataInterfacePreviewInput() { pageSize = 999999 };
                    var list = (await _dataInterfaceService.GetResponseByType(model.config.propsUrl, 0, par)).ToJsonStringOld().ToObject<PageResult<Dictionary<string, object>>>().list;
                    var data = list.Find(x => x.Any(xx => xx.Key.Equals(model.config.propsValue) && xx.Value.IsNotEmptyOrNull() && xx.Value.ToString().Equals(oldData)));
                    if (data.IsNotEmptyOrNull() && data.ContainsKey(model.config.propsLabel) && data[model.config.propsLabel].IsNotEmptyOrNull())
                        newData = data[model.config.propsLabel].ToString();
                }

                break;
        }

        return newData;
    }

    #endregion
}