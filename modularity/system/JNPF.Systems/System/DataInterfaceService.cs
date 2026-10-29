using JNPF.ClayObject;
using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Net;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.Thirdparty.JSEngine;
using JNPF.FriendlyException;
using JNPF.JsonSerialization;
using JNPF.LinqBuilder;
using JNPF.Logging.Attributes;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.DataInterFace;
using JNPF.Systems.Entitys.Entity.System;
using JNPF.Systems.Entitys.Model.DataInterFace;
using JNPF.Systems.Entitys.Model.System.DataInterFace;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace JNPF.Systems;

/// <summary>
/// 数据接口
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DataInterface", Order = 204)]
[Route("api/system/[controller]")]
public class DataInterfaceService : IDataInterfaceService, IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<DataInterfaceEntity> _repository;
    private readonly IDataBaseManager _dataBaseManager;
    private readonly IUserManager _userManager;
    private readonly IFileManager _fileManager;
    private readonly SqlSugarScope _sqlSugarClient;
    private readonly ICacheManager _cacheManager;
    private readonly ITenantManager _tenantManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private string _configId = App.GetOptions<ConnectionStringsOptions>().DefaultConnectionConfig.ConfigId.ToString();
    private string _dbName = App.GetOptions<ConnectionStringsOptions>().DefaultConnectionConfig.DBName;
    private int currentPage = 1;
    private int pageSize = 20;
    private string keyword = string.Empty;
    private string showKey = string.Empty;
    private string showValue = string.Empty;

    /// <summary>
    /// 初始化一个<see cref="DataInterfaceService"/>类型的新实例.
    /// </summary>
    public DataInterfaceService(
        ISqlSugarRepository<DataInterfaceEntity> repository,
        IDataBaseManager dataBaseManager,
        IUserManager userManager,
        ICacheManager cacheManager,
        IFileManager fileManager,
        ITenantManager tenantManager,
        IServiceScopeFactory serviceScopeFactory,
        ISqlSugarClient context)
    {
        _repository = repository;
        _fileManager = fileManager;
        _dataBaseManager = dataBaseManager;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _tenantManager = tenantManager;
        _serviceScopeFactory = serviceScopeFactory;
        _sqlSugarClient = (SqlSugarScope)context;
    }

    #region Get

    /// <summary>
    /// 获取接口列表(分页).
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] DataInterfaceListQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<DataInterfaceEntity>()
            .Where(a => a.DeleteMark == null)
            .WhereIF(!string.IsNullOrEmpty(input.category), a => a.Category == input.category)
            .WhereIF(!string.IsNullOrEmpty(input.type), a => input.type.Contains(a.Type.ToString()))
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark.Equals(input.enabledMark))
            .WhereIF(!string.IsNullOrEmpty(input.keyword), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select(a => new DataInterfaceListOutput
            {
                id = a.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                type = SqlFunc.IF(a.Type == 1).Return("SQL操作").ElseIF(a.Type == 2).Return("静态数据").End("API操作"),
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                sortCode = a.SortCode,
                enabledMark = a.EnabledMark,
                isPostPosition = a.IsPostposition,
                hasPage = a.HasPage,
                tenantId = _userManager.TenantId
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<DataInterfaceListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取接口列表(分页).
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("getList")]
    public async Task<dynamic> getList([FromQuery] DataInterfaceListQuery input)
    {
        var whereLambda = LinqExpression.And<DataInterfaceEntity>();
        switch (input.sourceType)
        {
            case 1:
                whereLambda = whereLambda.And(it => (!it.Type.Equals(1) || (it.Type.Equals(1) && it.Action.Equals(3))) && !it.HasPage.Equals(1));
                break;
            case 2:
                whereLambda = whereLambda.And(it => !it.Type.Equals(1) || (it.Type.Equals(1) && it.Action.Equals(3)));
                break;
            case 3:
                whereLambda = whereLambda.And(it => (!it.Type.Equals(1) || (it.Type.Equals(1) && !it.Action.Equals(3))) && !it.HasPage.Equals(1));
                break;
        }

        var list = await _repository.AsQueryable()
            .Where(a => a.DeleteMark == null && a.EnabledMark == 1 && a.IsPostposition == 0)
            .WhereIF(!string.IsNullOrEmpty(input.category), a => a.Category == input.category)
            .WhereIF(!string.IsNullOrEmpty(input.type), a => input.type.Contains(a.Type.ToString()))
            .WhereIF(!string.IsNullOrEmpty(input.keyword), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .Where(whereLambda)
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select(a => new DataInterfaceListOutput
            {
                id = a.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                type = SqlFunc.IF(a.Type == 1).Return("SQL操作").ElseIF(a.Type == 2).Return("静态数据").End("API操作"),
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                sortCode = a.SortCode,
                enabledMark = a.EnabledMark,
                tenantId = _userManager.TenantId,
                isPostPosition = a.IsPostposition,
                parameterJson = a.ParameterJson,
                hasPage = a.HasPage,
                fieldJson = a.FieldJson,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<DataInterfaceListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取接口列表下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        List<DataInterfaceSelectorOutput> tree = new List<DataInterfaceSelectorOutput>();
        var entity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "DataInterfaceType" && x.DeleteMark == null);
        var entityList = await _repository.AsQueryable().Where(x => x.DeleteMark == null && x.EnabledMark == 1 && x.IsPostposition == 0).OrderBy(x => x.SortCode)
           .Select(a => new DataInterfaceSelectorOutput
           {
               id = a.Id,
               categoryId = "1",
               fullName = a.FullName,
               parentId = a.Category,
           }).ToListAsync();
        var pidList = entityList.Select(it => it.parentId).ToList();
        tree = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(d => pidList.Contains(d.Id) && d.DictionaryTypeId == entity.Id && d.DeleteMark == null && d.EnabledMark == 1)
            .Select(a => new DataInterfaceSelectorOutput
            {
                id = a.Id,
                categoryId = "0",
                fullName = a.FullName,
                parentId = "0",
            }).ToListAsync();

        return tree.Union(entityList).ToList().ToTree();
    }

    /// <summary>
    /// 获取接口数据.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfoApi(string id)
    {
        return (await GetInfo(id)).Adapt<DataInterfaceInput>();
    }

    /// <summary>
    /// 获取预览参数.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("GetParam/{id}")]
    [UnitOfWork]
    public async Task<dynamic> GetParam(string id)
    {
        var info = await GetInfo(id);
        if (info.IsNotEmptyOrNull() && info.ParameterJson.IsNotEmptyOrNull())
        {
            return info.ParameterJson.ToList<DataInterfaceReqParameter>();
        }
        else
        {
            return new List<DataInterfaceReqParameter>();
        }
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var data = await GetInfo(id);
        var jsonStr = data.ToJsonString();
        return await _fileManager.Export(jsonStr, data.FullName, ExportFileType.bd);
    }

    #endregion

    #region Post

    /// <summary>
    /// 预览接口.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Preview")]
    [UnitOfWork]
    [UnifySerializerSetting("special")]
    public async Task<dynamic> Preview(string id, [FromBody] DataInterfacePreviewInput input)
    {
        _configId = _userManager.TenantId;
        _dbName = _userManager.TenantDbName;
        GetDatainterfaceParameter(input);
        return await GetDataInterfaceData(id, input, 3);
    }

    /// <summary>
    /// 访问接口 选中 回写.
    /// </summary>
    /// <returns></returns>
    [IgnoreLog]
    [HttpPost("{id}/Actions/InfoByIds")]
    [UnifySerializerSetting("special")]
    public async Task<dynamic> ActionsResponseInfo(string id, [FromBody] DataInterfacePreviewInput input)
    {
        _configId = _userManager.TenantId;
        _dbName = _userManager.TenantDbName;
        var isEcho = await _repository.IsAnyAsync(x => x.Id == id && x.HasPage == 1);
        GetDatainterfaceParameter(input);
        var output = new List<object>();
        keyword = input.keyword.IsNotEmptyOrNull() ? input.keyword : string.Empty;
        showKey = input.propsValue;
        input.dicParameters.Add("@showKey", input.propsValue);
        if (isEcho)
        {
            foreach (var item in input.ids)
            {
                showValue = item;
                input.dicParameters["@showValue"] = item;
                var data = await GetDataInterfaceData(id, input, 1, isEcho);
                if (data.IsNotEmptyOrNull())
                    output.Add(data);
            }
        }
        else
        {
            return await GetDataInterfaceData(id, input, 1, isEcho);
        }
        return output;
    }

    /// <summary>
    /// 访问接口 分页.
    /// </summary>
    /// <returns></returns>
    [IgnoreLog]
    [HttpPost("{id}/Actions/List")]
    [UnifySerializerSetting("special")]
    public async Task<dynamic> ActionsResponseList(string id, [FromBody] DataInterfacePreviewInput input)
    {
        _configId = _userManager.TenantId;
        _dbName = _userManager.TenantDbName;
        currentPage = input.currentPage;
        pageSize = input.pageSize;
        keyword = input.keyword;
        GetDatainterfaceParameter(input);
        return await GetDataInterfaceData(id, input, 0, false);
    }

    /// <summary>
    /// 外部访问接口.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dic"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("{id}/Actions/Response")]
    [UnitOfWork]
    [UnifySerializerSetting("special")]
    public async Task<dynamic> ActionsResponse(string id, [FromBody] Dictionary<string, string> dic)
    {
        return await InterfaceVerify(id, dic);
    }

    /// <summary>
    /// 添加接口.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] DataInterfaceInput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<DataInterfaceEntity>();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] DataInterfaceInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<DataInterfaceEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new DataInterfaceEntity()
        {
            DeleteMark = 1,
            DeleteTime = DateTime.Now,
            DeleteUserId = _userManager.UserId
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 更新接口状态.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task UpdateState(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new DataInterfaceEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyTime = DateTime.Now,
            LastModifyUserId = _userManager.UserId
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    public async Task ActionsImport(IFormFile file, int type)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.bd.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        DataInterfaceEntity? data;
        try
        {
            data = josn.ToObject<DataInterfaceEntity>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (data == null)
            throw Oops.Oh(ErrorCode.D3006);
        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(data.Id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(data.EnCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.FullName.Equals(data.FullName))) errorList.Add("名称");

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
                data.Id = SnowflakeIdHelper.NextId();
                data.FullName = string.Format("{0}.副本{1}", data.FullName, random);
                data.EnCode += random;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        data.Create();
        data.EnabledMark = 0;
        data.CreatorUserId = _userManager.UserId;
        data.LastModifyTime = null;
        data.LastModifyUserId = null;
        try
        {
            var storModuleModel = _repository.AsSugarClient().Storageable(data).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }

    }

    /// <summary>
    /// 外部接口授权码.
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="intefaceId"></param>
    /// <param name="dic"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [IgnoreLog]
    [HttpPost("Actions/GetAuth")]
    public async Task<dynamic> GetAuthorization([FromQuery] string appId, [FromQuery] string tenantId, [FromQuery] string intefaceId, [FromBody] Dictionary<string, string> dic)
    {
        await ChangeTenantDB(tenantId);
        var interfaceOauthEntity = await _sqlSugarClient.Queryable<InterfaceOauthEntity>().FirstAsync(x => x.AppId == appId && x.DeleteMark == null && x.EnabledMark == 1);
        if (interfaceOauthEntity == null) return null;
        var ymDate = DateTime.Now.ParseToUnixTime().ToString();
        var authorization = GetVerifySignature(interfaceOauthEntity, intefaceId, ymDate);
        return new {
            YmDate = ymDate,
            Authorization = authorization,
        };
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var random = RandomExtensions.NextLetterAndNumberString(new Random(), 5).ToLower();
        entity.FullName = string.Format("{0}.副本{1}", entity.FullName, random);
        entity.EnCode = string.Format("{0}{1}", entity.EnCode, random);
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);
        entity.EnabledMark = 0;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    #endregion

    #region Public

    /// <summary>
    /// 远端数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type"></param>
    /// <param name="tenantId"></param>
    /// <param name="input"></param>
    /// <param name="dicParameters"></param>
    /// <returns></returns>
    public async Task<object> GetResponseByType(string id, int type, DataInterfacePreviewInput input)
    {
        var isEcho = await _repository.IsAnyAsync(x => x.Id == id && x.HasPage == 1) && type == 1;
        if (input.tenantId.IsNotEmptyOrNull() && input.tenantId != "defualt")
        {
            await ChangeTenantDB(input.tenantId);
        }
        if (input.IsNotEmptyOrNull())
        {
            currentPage = input.currentPage;
            pageSize = input.pageSize;
            keyword = input.keyword.IsNotEmptyOrNull() ? input.keyword : string.Empty;
        }
        GetDatainterfaceParameter(input);
        return await GetDataInterfaceData(id, input, type, isEcho);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<DataInterfaceEntity> GetInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
    }

    /// <summary>
    /// 处理远端数据.
    /// </summary>
    /// <param name="cacheKey">缓存标识.</param>
    /// <param name="propsUrl">远端数据ID.</param>
    /// <param name="value">指定选项标签为选项对象的某个属性值.</param>
    /// <param name="label">指定选项的值为选项对象的某个属性值.</param>
    /// <param name="children">指定选项的子选项为选项对象的某个属性值.</param>
    /// <param name="linkageParameters">联动参数.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<StaticDataModel>> GetDynamicList(string cacheKey, string propsUrl, string value, string label, string children, List<DataInterfaceParameter> linkageParameters = null)
    {
        List<StaticDataModel> list = new List<StaticDataModel>();

        // 获取远端数据
        DataInterfaceEntity? dynamic = await _repository.AsQueryable().Where(x => x.Id == propsUrl && x.DeleteMark == null).FirstAsync();
        if (dynamic == null) return list;

        // 控件联动 不能缓存
        if (linkageParameters == null)
        {
            list = await GetDynamicDataCache(cacheKey, dynamic.Id);
        }

        if (list == null || list.Count == 0)
        {
            list = new List<StaticDataModel>();

            _configId = _userManager.TenantId;
            _dbName = _userManager.TenantDbName;
            var input = new DataInterfacePreviewInput { paramList = linkageParameters };
            if (linkageParameters == null) input.paramList = new List<DataInterfaceParameter>();
            GetDatainterfaceParameter(input);
            //var dicParameters = linkageParameters != null && linkageParameters.Any() ? linkageParameters.ToDictionary(x => x.ParameterName, y => y.FormFieldValues) : new Dictionary<string, string>();
            var interfaceData = await GetDataInterfaceData(propsUrl, input, 3);

            // 数据处理结果
            var dataProcessingResults = interfaceData.IsNullOrEmpty() ? string.Empty : interfaceData.ToJsonString();

            if (!dataProcessingResults.IsNullOrEmpty())
            {
                if (dynamic.HasPage.Equals(1) && dataProcessingResults.IsNotEmptyOrNull() && !dataProcessingResults.FirstOrDefault().Equals('['))
                {
                    var realList = dataProcessingResults.ToObject<PageResult<Dictionary<string, object>>>();
                    dataProcessingResults = realList.list.ToJsonString();
                }

                foreach (JToken? item in JToken.Parse(dataProcessingResults))
                {
                    StaticDataModel dynamicDic = new StaticDataModel()
                    {
                        id = item.Value<string>(value),
                        fullName = item.Value<string>(label)
                    };
                    list.Add(dynamicDic);

                    // 为避免子级有数据.
                    if (item.Value<object>(children) != null && item.Value<object>(children).ToString().IsNotEmptyOrNull())
                        list.AddRange(GetDynamicInfiniteData(item.Value<object>(children).ToString(), value, label, children));
                }
                await SetDynamicDataCache(cacheKey, dynamic.Id, list);
            }
        }
        return list;
    }

    /// <summary>
    /// 处理数据接口参数.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Dictionary<string, string> GetDatainterfaceParameter(DataInterfacePreviewInput input)
    {
        dynamic? clay = input.sourceData.IsNotEmptyOrNull() ? Clay.Parse(input.sourceData.ToJsonString()) : null;
        Dictionary<string, string> pairs = new Dictionary<string, string>();
        if (input.paramList == null) return pairs;
        foreach (var item in input.paramList)
        {
            switch (item.sourceType)
            {
                case 1:
                    if (item.relationField.IsNotEmptyOrNull() && clay != null)
                    {
                        if (item.isSubTable || item.relationField.ToString().IsMatch("^tableField\\d{3}"))
                        {
                            var fields = item.relationField.ToString().Split("-").ToList();
                            // 子表键值
                            var tableField = fields[0];
                            // 子表字段键值
                            var keyField = fields[1];
                            if (clay.IsDefined(tableField))
                            {
                                // 获取子表全部数据
                                var subTabelData = clay[tableField];

                                // 子表数据转粘土对象
                                var subTableClay = Clay.Parse(subTabelData.ToString());
                                List<Dictionary<string, object>> dataCount = subTableClay.Deserialize<List<Dictionary<string, object>>>();
                                if (dataCount.Count > 0)
                                {
                                    // 粘土对象转数组/集合
                                    IEnumerable<dynamic> subTableList = subTableClay.AsEnumerator<dynamic>();

                                    // 取子表第一条数据
                                    var subTable = subTableList.FirstOrDefault();

                                    // 子表行转换成 dictionary
                                    Dictionary<string, object> subTableDic = subTable.ToDictionary();

                                    item.defaultValue = subTableDic[keyField];
                                }
                            }
                        }
                        else
                        {
                            if (clay.IsDefined(item.relationField.ToString()))
                            {
                                item.defaultValue = clay[item.relationField.ToString()];
                            }
                        }
                    }
                    break;
                case 2:
                    if (item.relationField.IsNotEmptyOrNull())
                    {
                        item.defaultValue = item.relationField;
                    }
                    break;
                case 3:
                    item.defaultValue = string.Empty;
                    break;
                case 4:
                    if (input.systemParamter.Keys.Contains(item.relationField.ToString()))
                    {
                        item.defaultValue = input.systemParamter[item.relationField.ToString()];
                    }
                    else
                    {
                        switch (item.relationField.ToString())
                        {
                            case CommonConst.SysParamUserId:
                            case "@flowOperatorUserId":
                                item.defaultValue = _userManager.UserId;
                                break;
                            case "@flowOperatorUserName":
                                item.defaultValue = _userManager.User.RealName;
                                break;
                            case CommonConst.SysParamUserAndSub:
                                item.defaultValue = _userManager.CurrentUserAndSubordinates.ToJsonStringOld();
                                break;
                            case CommonConst.SysParamUserTree:
                                item.defaultValue = _userManager.CurrentUserTree.ToJsonStringOld();
                                break;
                            case CommonConst.SysParamPositionId:
                                item.defaultValue = _userManager.CurrentPositionList;
                                break;
                            case CommonConst.SysParamPositionAndSub:
                                item.defaultValue = _userManager.CurrentPositionAndSuborganization;
                                break;
                            case CommonConst.SysParamPositionTree:
                                item.defaultValue = _userManager.CurrentPositionTree;
                                break;
                            case CommonConst.SysParamOrganizeId:
                                item.defaultValue = _userManager.CurrentOrganizeList.ToJsonStringOld();
                                break;
                            case CommonConst.SysParamOrganizeAndSub:
                                item.defaultValue = _userManager.CurrentOrganizationAndSubOrganizations.ToJsonStringOld();
                                break;
                            case CommonConst.SysParamOrganizationTree:
                                item.defaultValue = _userManager.CurrentOrganizationTree.ToJsonStringOld();
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    if (item.relationField.IsNotEmptyOrNull())
                    {
                        item.defaultValue = item.relationField;
                    }
                    break;
            }

            if (item.defaultValue == null)
            {
                pairs[item.field] = null;
            }
            else
            {
                if (item.defaultValue.ToJsonString().FirstOrDefault().Equals('['))
                {
                    pairs[item.field] = item.defaultValue.ToJsonStringOld().ToJsonStringOld().Trim('"');
                }
                else if (item.defaultValue.ToString().FirstOrDefault().Equals('['))
                {
                    pairs[item.field] = item.defaultValue.ToJsonString().Trim('"');
                }
                else
                {
                    pairs[item.field] = item.defaultValue?.ToString();
                }
            }
        }
        if (pairs.Any())
        {
            input.dicParameters = pairs;
        }
        return pairs;
    }
    #endregion

    #region Private

    /// <summary>
    /// 获取数据接口数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <param name="type"></param>
    /// <param name="isEcho"></param>
    /// <returns></returns>
    public async Task<object> GetDataInterfaceData(string id, DataInterfacePreviewInput input, int type, bool isEcho = false)
    {
        object? output = null;
        var info = await GetInfo(id);
        if (info.IsNotEmptyOrNull())
        {
            ReplaceSqlParameter(info, input.dicParameters);
            VerifyRequired(info, input.dicParameters);
            if (info.Type == 1)
            {
                output = await GetSqlData(info, isEcho);
                // 预览更新变量值.
                if (info.IsPostposition == 1)
                {
                    var variateList = _repository.AsSugarClient().Queryable<DataInterfaceVariateEntity>().Where(x => x.InterfaceId == info.Id && x.DeleteMark == null).ToList();
                    foreach (var item in variateList)
                    {
                        string sheetData = Regex.Match(item.Expression, @"\{(.*)\}", RegexOptions.Singleline).Groups[1].Value;
                        var scriptStr = "var result = function(data){data = JSON.parse(data);" + sheetData + "}";
                        var value = JsEngineUtil.CallFunction(scriptStr, output.ToJsonString(CommonConst.options));//此处时间非时间戳
                        item.Value = value.ToJsonString().Trim('"');
                    }
                    _repository.AsSugarClient().Updateable(variateList).ExecuteCommand();
                }
            }
            else if (info.Type == 2)
            {
                output = info.DataConfigJson.ToObject<DataInterfaceProperJson>().staticData.ToObject<object>();
                // 预览更新变量值.
                if (info.IsPostposition == 1)
                {
                    var variateList = _repository.AsSugarClient().Queryable<DataInterfaceVariateEntity>().Where(x => x.InterfaceId == info.Id && x.DeleteMark == null).ToList();
                    foreach (var item in variateList)
                    {
                        string sheetData = Regex.Match(item.Expression, @"\{(.*)\}", RegexOptions.Singleline).Groups[1].Value;
                        var scriptStr = "var result = function(data){data = JSON.parse(data);" + sheetData + "}";
                        var value = JsEngineUtil.CallFunction(scriptStr, output.ToJsonString(CommonConst.options));//此处时间非时间戳
                        item.Value = value.ToJsonString().Trim('"');
                    }
                    _repository.AsSugarClient().Updateable(variateList).ExecuteCommand();
                }
            }
            else
            {
                output = await GetApiData(info, input.dicParameters, type, isEcho);
            }

            if (info.DataJsJson.IsNotEmptyOrNull() && output.IsNotEmptyOrNull())
            {
                string sheetData = Regex.Match(info.DataJsJson, @"\{(.*)\}", RegexOptions.Singleline).Groups[1].Value;
                var scriptStr = "var result = function(data){data = JSON.parse(data);" + sheetData + "}";
                output = JsEngineUtil.CallFunction(scriptStr, output.ToJsonString(CommonConst.options));//此处时间非时间戳
            }

            if (info.HasPage == 0)
            {
                // 假分页数据处理.
                output = InterfaceDataManage(output, type, input);
            }
        }

        return output;
    }

    /// <summary>
    /// 未开启分页数据处理.
    /// </summary>
    /// <param name="interfaceData"></param>
    /// <param name="type"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    private object InterfaceDataManage(object interfaceData, int type, DataInterfacePreviewInput input)
    {
        var output = new object();
        var result = new List<Dictionary<string, object>>();
        if (type != 3)
        {
            if (interfaceData.ToJsonString().FirstOrDefault().Equals('['))
            {
                result = interfaceData.ToObject<List<Dictionary<string, object>>>();
            }
            else
            {
                var dic = interfaceData.ToObject<Dictionary<string, object>>();
                result = dic.IsNotEmptyOrNull() && dic.ContainsKey("list") ? dic["list"].ToObject<List<Dictionary<string, object>>>() : new List<Dictionary<string, object>>();
            }
        }

        switch (type)
        {
            case 0:
                if (input.IsNotEmptyOrNull())
                {
                    // 模糊搜索.
                    if (input.keyword.IsNotEmptyOrNull())
                    {
                        result = result.FindAll(r => r.WhereIF(input.columnOptions.IsNotEmptyOrNull(), x => input.columnOptions.Contains(x.Key)).Where(x => x.Value != null && x.Value.ToString().Contains(input.keyword)).Any());
                    }

                    // 精准搜索.
                    if (input.queryJson.IsNotEmptyOrNull() || input.sidx.IsNotEmptyOrNull())
                    {
                        if (input.queryJson.IsNotEmptyOrNull())
                        {
                            foreach (var item in input.queryJson.ToObject<Dictionary<string, string>>())
                            {
                                if (item.Key.Contains("jnpf_searchType_equals_")) result = result.Where(x => x[item.Key.Replace("jnpf_searchType_equals_", "")].Equals(item.Value)).ToList();
                                else result = result.Where(x => x[item.Key].ToJsonString().Contains(item.Value)).ToList();
                            }
                        }

                        if (input.sidx.IsNotEmptyOrNull())
                        {
                            if (input.sort.Equals("desc")) result = result.OrderBy(x => x[input.sidx]).ToList();
                            else result = result.OrderByDescending(x => x[input.sidx]).ToList();
                        }
                    }
                }

                output = new {
                    pagination = new PageResult()
                    {
                        currentPage = input.currentPage,
                        pageSize = input.pageSize,
                        total = result.Count
                    },
                    list = result.Skip((input.currentPage - 1) * input.pageSize).Take(input.pageSize).ToList(),
                };
                break;
            case 1:
                if (input.id != null)
                {
                    output = result.FirstOrDefault(x => x[input.propsValue].ToString().Equals(input.id));
                }
                else if (input.id == null && input.ids.Count > 0)
                {
                    var res = new List<Dictionary<string, object>>();
                    foreach (var item in input.ids)
                    {
                        var firstData = result.FirstOrDefault(x => x[input.propsValue].ToString().Equals(item));
                        if (firstData != null)
                            res.Add(firstData);
                    }
                    output = res;
                }
                break;
            default:
                output = interfaceData;
                break;
        }

        return output;
    }

    /// <summary>
    /// 验证必填参数.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="dicParams"></param>
    private void VerifyRequired(DataInterfaceEntity entity, Dictionary<string, string> dicParams)
    {
        try
        {
            if (entity.IsNotEmptyOrNull() && entity.ParameterJson.IsNotEmptyOrNull() && !entity.ParameterJson.Equals("[]"))
            {
                var reqParams = entity.ParameterJson.ToList<DataInterfaceReqParameter>();
                if (reqParams.Count > 0)
                {
                    // 必填参数
                    var requiredParams = reqParams.Where(x => x.required == "1").ToList();
                    if (requiredParams.Any() && (dicParams.IsNullOrEmpty() || dicParams.Keys.Count == 0))
                        throw Oops.Oh(ErrorCode.xg1003);
                    foreach (var item in requiredParams)
                    {
                        if (dicParams.ContainsKey(item.field))
                        {
                            switch (item.dataType)
                            {
                                case "varchar":
                                    if (dicParams[item.field].IsNullOrEmpty())
                                    {
                                        throw Oops.Oh(ErrorCode.COM1018, item.field + "不能为空");
                                    }
                                    break;
                                case "int":
                                    int.Parse(dicParams[item.field]);
                                    break;
                                case "datetime":
                                    if (long.TryParse(dicParams[item.field], out var result))
                                        dicParams[item.field].TimeStampToDateTime();
                                    else
                                        DateTime.Parse(dicParams[item.field]);
                                    break;
                                case "decimal":
                                    decimal.Parse(dicParams[item.field]);
                                    break;
                            }
                        }
                        else
                        {
                            throw Oops.Oh(ErrorCode.COM1018, item.field + "不能为空");
                        }
                    }
                }
            }
        }
        catch (AppFriendlyException)
        {
            throw Oops.Oh(ErrorCode.xg1003);
        }
    }

    #region Sql

    /// <summary>
    /// 执行sql.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="isEcho"></param>
    /// <returns></returns>
    private async Task<object> GetSqlData(DataInterfaceEntity entity, bool isEcho = false)
    {
        var propJson = isEcho ? entity.DataEchoJson.ToObject<DataInterfaceProperJson>() : entity.DataConfigJson.ToObject<DataInterfaceProperJson>();
        var parameter = entity.ParameterJson.ToObject<List<DataInterfaceReqParameter>>().Select(x => new SugarParameter("@" + x.field, GetSugarParameterList(x))).ToList();
        var sqlData = await ExcuteSql(propJson, parameter, entity.Action.ParseToInt());
        if (isEcho)
        {
            return sqlData.ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
        }
        else
        {
            if (entity.HasPage == 1)
            {
                propJson = entity.DataCountJson.ToObject<DataInterfaceProperJson>();
                var count = await ExcuteSql(propJson, parameter, -1);
                return new { list = sqlData, pagination = new PageResult() { currentPage = currentPage, pageSize = pageSize, total = count.ParseToInt() } };
            }

            return sqlData;
        }
    }

    /// <summary>
    /// 替换sql参数默认值.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="dic"></param>
    [NonAction]
    public void ReplaceSqlParameter(DataInterfaceEntity entity, Dictionary<string, string> dic)
    {
        if (dic.IsNotEmptyOrNull() && entity.IsNotEmptyOrNull() && entity.ParameterJson.IsNotEmptyOrNull())
        {
            var parameterList = entity.ParameterJson.ToList<DataInterfaceReqParameter>();
            foreach (var item in parameterList)
            {
                if (dic.Keys.Contains(item.field))
                {
                    if (dic[item.field].IsNullOrEmpty()) dic[item.field] = item.defaultValue;
                    item.defaultValue = HttpUtility.UrlDecode(dic[item.field], Encoding.UTF8); // 对参数解码
                }
                if (entity.Type == 1)
                {
                    // 将sql语句参数替换成@field
                    entity.DataConfigJson = entity.DataConfigJson?.Replace("{" + item.field + "}", "@" + item.field);
                    entity.DataCountJson = entity.DataCountJson?.Replace("{" + item.field + "}", "@" + item.field);
                    entity.DataEchoJson = entity.DataEchoJson?.Replace("{" + item.field + "}", "@" + item.field);
                }
            }

            entity.ParameterJson = parameterList.ToJsonString();
        }
    }

    /// <summary>
    /// 执行sql.
    /// </summary>
    /// <param name="properJson"></param>
    /// <param name="parameter"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    private async Task<object> ExcuteSql(DataInterfaceProperJson properJson, List<SugarParameter> parameter, int action)
    {
        var dt = new DataTable();
        var link = new DbLinkEntity();
        if (!_sqlSugarClient.AsTenant().IsAnyConnection(_configId))
        {
            link = await _sqlSugarClient.CopyNew().Queryable<DbLinkEntity>().FirstAsync(x => x.Id == properJson.sqlData.dbLinkId && x.DeleteMark == null);
        }
        else
        {
            link = await _repository.AsSugarClient().CopyNew().Queryable<DbLinkEntity>().FirstAsync(x => x.Id == properJson.sqlData.dbLinkId && x.DeleteMark == null);
        }

        if (KeyVariable.MultiTenancy && _userManager.IsNotEmptyOrNull())
        {
            _configId = _userManager.TenantId;
            _dbName = _userManager.TenantDbName;
        }
        var tenantLink = link ?? _dataBaseManager.GetTenantDbLink(_configId, _dbName);
        var sql = GetSqlParameter(properJson.sqlData.sql, parameter);
        if (action == 3)
        {
            dt = _dataBaseManager.GetSqlData(tenantLink, sql, true, parameter.ToArray());
        }
        else if (action == -1)
        {
            return _dataBaseManager.GetCount(tenantLink, properJson.sqlData.sql, true, parameter.ToArray());
        }
        else
        {
            if (_sqlSugarClient.CurrentConnectionConfig.DbType == SqlSugar.DbType.Oracle && sql.Contains(";"))
            {
                foreach (var item in sql.Split(";"))
                {
                    if (item.IsNotEmptyOrNull() && !item.Equals("\r\n"))
                        await _dataBaseManager.ExecuteSql(tenantLink, sql, true, parameter.ToArray());
                }
            }
            else
            {
                await _dataBaseManager.ExecuteSql(tenantLink, sql, true, parameter.ToArray());
            }
        }
        return dt;
    }

    /// <summary>
    /// 获取sql系统变量参数.
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="sugarParameters"></param>
    [NonAction]
    public string GetSqlParameter(string sql, List<SugarParameter> sugarParameters)
    {
        if (_userManager.ToKen != null)
        {
            if (sql.Contains(CommonConst.SysParamUserId) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamUserId))
            {
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamUserId, _userManager.UserId));
            }
            if (sql.Contains("@snowFlakeID") && !sugarParameters.Any(x => x.ParameterName == "@snowFlakeID"))
            {
                sugarParameters.Add(new SugarParameter("@snowFlakeID", SnowflakeIdHelper.NextId()));
            }
            if (sql.Contains("@lotSnowID") && !sugarParameters.Any(x => x.ParameterName == "@lotSnowID"))
            {
                var splitCount = sql.Split("@lotSnowID").Count();
                Regex r = new Regex("@lotSnowID");
                for (int i = 0; i < splitCount - 1; i++)
                {
                    var newId = string.Format("@snowID{0}", i);
                    sql = r.Replace(sql, newId, 1);
                    sugarParameters.Add(new SugarParameter(string.Format("@snowID{0}", i), SnowflakeIdHelper.NextId()));
                }
            }
            if (sql.Contains(CommonConst.SysParamUserAndSub) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamUserAndSub))
            {
                var subordinates = _userManager.CurrentUserAndSubordinates;
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamUserAndSub, subordinates));
            }
            if (sql.Contains(CommonConst.SysParamUserTree) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamUserTree))
            {
                var subordinates = _userManager.CurrentUserTree;
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamUserTree, subordinates));
            }
            if (sql.Contains(CommonConst.SysParamOrganizeId) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamOrganizeId))
            {
                var subordinates = _userManager.CurrentOrganizeList;
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamOrganizeId, _userManager.CurrentOrganizeList));
            }
            if (sql.Contains(CommonConst.SysParamOrganizeAndSub) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamOrganizeAndSub))
            {
                var subsidiary = _userManager.CurrentOrganizationAndSubOrganizations;
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamOrganizeAndSub, subsidiary));
            }
            if (sql.Contains(CommonConst.SysParamOrganizationTree) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamOrganizationTree))
            {
                var subsidiary = _userManager.CurrentOrganizationTree;
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamOrganizationTree, subsidiary));
            }
            if (sql.Contains(CommonConst.SysParamPositionId) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamPositionId))
            {
                var subsidiary = _userManager.CurrentPositionList.ToObject<List<string>>();
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamPositionId, subsidiary));
            }
            if (sql.Contains(CommonConst.SysParamPositionAndSub) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamPositionAndSub))
            {
                var subsidiary = _userManager.CurrentPositionAndSuborganization.ToObject<List<string>>();
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamPositionAndSub, subsidiary));
            }
            if (sql.Contains(CommonConst.SysParamPositionTree) && !sugarParameters.Any(x => x.ParameterName == CommonConst.SysParamPositionTree))
            {
                var subsidiary = _userManager.CurrentPositionTree.ToObject<List<string>>();
                sugarParameters.Add(new SugarParameter(CommonConst.SysParamPositionTree, subsidiary));
            }
            if (sql.Contains("@offsetSize") && !sugarParameters.Any(x => x.ParameterName == "@offsetSize"))
            {
                sugarParameters.Add(new SugarParameter("@offsetSize", (currentPage - 1) * pageSize));
            }
            if (sql.Contains("@pageSize") && !sugarParameters.Any(x => x.ParameterName == "@pageSize"))
            {
                sugarParameters.Add(new SugarParameter("@pageSize", pageSize));
            }
            if (sql.Contains("@showValue") && !sugarParameters.Any(x => x.ParameterName == "@showValue"))
            {
                sugarParameters.Add(new SugarParameter("@showValue", showValue));
            }
            if (sql.Contains("@keyword") && !sugarParameters.Any(x => x.ParameterName == "@keyword"))
            {
                sugarParameters.Add(new SugarParameter("@keyword", keyword));
            }
            if (sql.Contains("@showKey"))
            {
                sql = sql.Replace("@showKey", showKey);
            }
        }
        return sql;
    }

    /// <summary>
    /// 切换租户数据库.
    /// </summary>
    /// <param name="tenantId">租户id.</param>
    /// <returns></returns>
    private async Task ChangeTenantDB(string tenantId)
    {
        if (KeyVariable.MultiTenancy)
        {
            tenantId = tenantId.IsNullOrEmpty() ? _userManager.TenantId : tenantId;
            var tenantInterFaceOutput = await _tenantManager.ChangTenant(_sqlSugarClient, tenantId);
            _configId = tenantId;
            if (tenantInterFaceOutput.dotnet.IsNotEmptyOrNull() && tenantInterFaceOutput.type != 1)
            {
                _dbName = tenantInterFaceOutput.dotnet;
            }
        }
    }

    /// <summary>
    /// 转换接口参数类型.
    /// </summary>
    /// <param name="dataInterfaceReqParameter"></param>
    /// <returns></returns>
    public object GetSugarParameterList(DataInterfaceReqParameter dataInterfaceReqParameter)
    {
        try
        {
            object? value = null;
            if (dataInterfaceReqParameter.defaultValue.IsNotEmptyOrNull())
            {
                switch (dataInterfaceReqParameter.dataType)
                {
                    case "int":
                        value = int.Parse(dataInterfaceReqParameter.defaultValue);
                        break;
                    case "datetime":
                        if (long.TryParse(dataInterfaceReqParameter.defaultValue, out var result))
                            value = string.Format("{0:yyyy-MM-dd HH:mm:ss}", dataInterfaceReqParameter.defaultValue.TimeStampToDateTime());
                        else
                            value = DateTime.Parse(dataInterfaceReqParameter.defaultValue);
                        break;
                    case "decimal":
                        value = decimal.Parse(dataInterfaceReqParameter.defaultValue);
                        break;
                }
            }

            if (dataInterfaceReqParameter.dataType == "varchar")
            {
                value = dataInterfaceReqParameter.defaultValue.IsNotEmptyOrNull() ? dataInterfaceReqParameter.defaultValue : string.Empty;
            }

            return value;
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.IO0010);
        }
    }
    #endregion

    #region Api
    /// <summary>
    /// 根据不同规则请求接口(预览).
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private async Task<JToken> GetApiData(DataInterfaceEntity entity, Dictionary<string, string> dicParameters, int type, bool isEcho = false)
    {
        try
        {
            var propJson = isEcho && entity.HasPage == 1 ? entity.DataEchoJson.ToObject<DataInterfaceProperJson>() : entity.DataConfigJson.ToObject<DataInterfaceProperJson>();
            GetApiParameter(propJson, entity.ParameterJson, dicParameters);
            var result = await RequestApi(entity, propJson, isEcho);
            // 异常验证
            if (entity.DataExceptionJson.IsNotEmptyOrNull())
            {
                string sheetData = Regex.Match(entity.DataExceptionJson, @"\{(.*)\}", RegexOptions.Singleline).Groups[1].Value;
                var scriptStr = "var result = function(data){data = JSON.parse(data);" + sheetData + "}";
                var flag = JsEngineUtil.CallFunction(scriptStr, result.ToJsonString(CommonConst.options));//此处时间非时间戳
                if (flag.ToJsonString().Equals("false"))
                {
                    if (entity.IsPostposition == 0)
                    {
                        if (propJson.variateIds.Any())
                        {
                            foreach (var item in propJson.variateIds)
                            {
                                await Preview(item, new DataInterfacePreviewInput());
                            }
                            GetApiParameter(propJson, entity.ParameterJson, dicParameters);
                            result = await RequestApi(entity, propJson, isEcho);
                        }
                        else
                        {
                            throw Oops.Oh(ErrorCode.IO0005);
                        }
                    }
                    else
                    {
                        throw Oops.Oh(ErrorCode.IO0006);
                    }
                }
            }

            // 预览更新变量值.
            if (entity.IsPostposition == 1)
            {
                var variateList = _repository.AsSugarClient().Queryable<DataInterfaceVariateEntity>().Where(x => x.InterfaceId == entity.Id && x.DeleteMark == null).ToList();
                foreach (var item in variateList)
                {
                    string sheetData = Regex.Match(item.Expression, @"\{(.*)\}", RegexOptions.Singleline).Groups[1].Value;
                    var scriptStr = "var result = function(data){data = JSON.parse(data);" + sheetData + "}";
                    var value = JsEngineUtil.CallFunction(scriptStr, result.ToJsonString(CommonConst.options));//此处时间非时间戳
                    item.Value = value.ToJsonString().Trim('"');
                }
                _repository.AsSugarClient().Updateable(variateList).ExecuteCommand();
            }
            if (result.ContainsKey("code") && result.ContainsKey("data") && result.ContainsKey("msg") && type != 4)
            {
                if (result["data"] is JObject)
                    result = result["data"].ToObject<JObject>();
                else if (result["data"] is JArray)
                    return JArray.Parse(result["data"].ToJsonString());
            }
            if (isEcho && result is JArray)
            {
                return JArray.Parse(result.ToJsonString()).FirstOrDefault().ToObject<JObject>();
            }
            return result;
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.IO0005);
        }
    }

    /// <summary>
    /// 获取api参数.
    /// </summary>
    /// <param name="json"></param>
    /// <param name="parameterList"></param>
    /// <returns></returns>
    private string GetApiParameter(DataInterfaceProperJson propJson, string parameterJson, Dictionary<string, string> dicParameter)
    {
        var parameterList = parameterJson.ToList<DataInterfaceReqParameter>();
        foreach (var item in parameterList)
        {
            if (dicParameter.Keys.Contains(item.field))
                item.defaultValue = HttpUtility.UrlDecode(dicParameter[item.field], Encoding.UTF8);
        }
        parameterList.Add(new DataInterfaceReqParameter() { field = "currentPage", defaultValue = currentPage.ToString() });
        parameterList.Add(new DataInterfaceReqParameter() { field = "pageSize", defaultValue = pageSize.ToString() });
        parameterList.Add(new DataInterfaceReqParameter() { field = "keyword", defaultValue = keyword });
        parameterList.Add(new DataInterfaceReqParameter() { field = "showKey", defaultValue = showKey });
        parameterList.Add(new DataInterfaceReqParameter() { field = "showValue", defaultValue = showValue });
        if (propJson.apiData.header.Any())
        {
            foreach (var item in propJson.apiData.header)
            {
                if (item.source == "1")
                {
                    if (dicParameter.ContainsKey(item.defaultValue))
                    {
                        item.defaultValue = dicParameter[item.defaultValue];
                    }
                    else
                    {
                        var parameter = parameterList.Find(x => x.field == item.defaultValue);
                        item.defaultValue = parameter.IsNullOrEmpty() ? item.defaultValue : parameter.defaultValue;
                    }
                }
                if (item.source == "2")
                {
                    var variateEntity = _repository.AsSugarClient().Queryable<DataInterfaceVariateEntity>().First(x => x.FullName == item.field);
                    item.defaultValue = variateEntity.Value.Trim('"');
                    propJson.variateIds.Add(variateEntity.InterfaceId);
                }
                if (item.source == "4" && parameterList.Any(it => it.field.Equals(item.field)))
                {
                    item.defaultValue = parameterList.Find(it => it.field.Equals(item.field)).defaultValue;
                }
            }
        }
        if (propJson.apiData.query.Any())
        {
            foreach (var item in propJson.apiData.query)
            {
                if (item.source == "1")
                {
                    if (dicParameter.ContainsKey(item.defaultValue))
                    {
                        item.defaultValue = dicParameter[item.defaultValue];
                    }
                    else
                    {
                        var parameter = parameterList.Find(x => x.field == item.defaultValue);
                        item.defaultValue = parameter.IsNullOrEmpty() ? item.defaultValue : parameter.defaultValue;
                    }
                }
                if (item.source == "2")
                {
                    var variateEntity = _repository.AsSugarClient().Queryable<DataInterfaceVariateEntity>().First(x => x.Id == item.defaultValue);
                    item.defaultValue = variateEntity.Value;
                    propJson.variateIds.Add(variateEntity.InterfaceId);
                }
                if (item.source == "4" && parameterList.Any(it => it.field.Equals(item.field)))
                {
                    item.defaultValue = parameterList.Find(it => it.field.Equals(item.field)).defaultValue;
                }
                item.defaultValue = HttpUtility.UrlDecode(item.defaultValue, Encoding.UTF8);
            }
        }

        if (propJson.apiData.body.IsNotEmptyOrNull())
        {
            if (propJson.apiData.bodyType == "1" || propJson.apiData.bodyType == "2")
            {
                var bodyList = propJson.apiData.body.ToObject<List<DataInterfaceReqParameter>>();
                foreach (var item in bodyList)
                {
                    if (item.source == "1")
                    {
                        if (dicParameter.ContainsKey(item.defaultValue))
                        {
                            item.defaultValue = dicParameter[item.defaultValue];
                        }
                        else
                        {
                            var parameter = parameterList.Find(x => x.field == item.defaultValue);
                            item.defaultValue = parameter.IsNullOrEmpty() ? item.defaultValue : parameter.defaultValue;
                        }
                    }
                    if (item.source == "2")
                    {
                        var variateEntity = _repository.AsSugarClient().Queryable<DataInterfaceVariateEntity>().First(x => x.FullName == item.field);
                        item.defaultValue = variateEntity.Value.Trim('"');
                        propJson.variateIds.Add(variateEntity.InterfaceId);
                    }
                    if (item.source == "4" && parameterList.Any(it => it.field.Equals(item.field)))
                    {
                        item.defaultValue = parameterList.Find(it => it.field.Equals(item.field)).defaultValue;
                    }
                }

            }
            else
            {
                foreach (var item in parameterList)
                {
                    propJson.apiData.body = propJson.apiData.body.Replace("{" + item.field + "}", item.defaultValue);
                }
                var variateList = _repository.AsSugarClient().Queryable<DataInterfaceVariateEntity>().Where(x => x.DeleteMark == null).ToList();
                foreach (var item in variateList)
                {
                    if (propJson.apiData.body.Contains("{@" + item.FullName + "}"))
                    {
                        propJson.apiData.body = propJson.apiData.body.Replace("{@" + item.FullName + "}", item.Value);
                        propJson.variateIds.Add(item.InterfaceId);
                    }
                }
            }
        }
        propJson.apiData.url = propJson.apiData.url.Replace("{showKey}", showKey).Replace("{showValue}", showValue);
        if (!propJson.apiData.url.StartsWith("http"))
        {
            propJson.apiData.url = GetLocalAddress() + propJson.apiData.url;
        }
        return propJson.ToJsonString();
    }

    private async Task<JObject> RequestApi(DataInterfaceEntity entity, DataInterfaceProperJson propJson, bool isEcho)
    {
        var result = new JObject();
        var heraderParameters = new Dictionary<string, object>(); // 头部参数.
        var queryParameters = new Dictionary<string, object>();// 请求参数.
        var bodyParameters = new Dictionary<string, object>();// 请求参数.
        var body = new object();
        //heraderParameters.Add("jnpf_api", true); // 接口返回结果是{"code":200,"msg":"操作成功","data":null}结构则直接取data数据
        if (_userManager.ToKen != null && !_userManager.ToKen.Contains("::"))
        {
            heraderParameters.Add("Authorization", _userManager.ToKen);
            heraderParameters.Add("Jnpf-Origin", _userManager.UserOrigin);
        }
        var path = propJson.apiData.url;
        var requestMethod = propJson.apiData.method;
        // contentType设置
        string contentType = null;
        if (propJson.apiData.bodyType == "2")
        {
            contentType = "application/x-www-form-urlencoded";
        }
        if (propJson.apiData.bodyType == "4")
        {
            contentType = "application/xml";
        }
        foreach (var key in propJson.apiData.query)
        {
            queryParameters.Add(key.field, key.defaultValue);
        }

        foreach (var key in propJson.apiData.header)
        {
            heraderParameters[key.field] = key.defaultValue;
        }

        if (propJson.apiData.body.IsNotEmptyOrNull())
        {
            if (propJson.apiData.bodyType == "1" || propJson.apiData.bodyType == "2")
            {
                foreach (var key in propJson.apiData.body.ToObject<List<DataInterfaceReqParameter>>())
                {
                    bodyParameters.Add(key.field, GetSugarParameterList(key));
                }
                body = bodyParameters;
            }
            else
            {
                if ('['.Equals(propJson.apiData.body.FirstOrDefault()))
                {
                    body = propJson.apiData.body.ToObject<List<Dictionary<string, object>>>();
                }
                else
                {
                    bodyParameters = propJson.apiData.body.ToObject<Dictionary<string, object>>();
                    body = bodyParameters;
                }
            }
        }

        switch (requestMethod)
        {
            case "1":
                result = (await path.SetHeaders(heraderParameters).SetQueries(queryParameters).GetAsStringAsync()).ToObject<JObject>();
                break;
            case "2":
                result = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetHeaders(heraderParameters).SetQueries(queryParameters).SetBody(body, contentType).PostAsStringAsync()).ToObject<JObject>();
                break;
        }
        return result;
    }
    #endregion

    #region 接口验证

    /// <summary>
    /// 外部接口验证并请求.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dic"></param>
    /// <returns></returns>
    private async Task<dynamic> InterfaceVerify(string id, Dictionary<string, string> dic)
    {
        object output = null;
        var input = new DataInterfacePreviewInput { dicParameters = dic };
        UserAgent userAgent = new UserAgent(App.HttpContext);
        var tenantId = dic.IsNotEmptyOrNull() && dic.ContainsKey("tenantId") ? dic["tenantId"] : "0";
        await ChangeTenantDB(tenantId);
        var userId = string.Empty;
        var appId = VerifyInterfaceOauth(id, ref userId);
        var sw = new Stopwatch();
        sw.Start();
        if (userId.IsNullOrEmpty())
        {
            output = await GetDataInterfaceData(id, input, 3);
            if (tenantId.IsNotEmptyOrNull() && _sqlSugarClient.IsAnyConnection(tenantId)) _sqlSugarClient.ChangeDatabase(tenantId);
        }
        else
        {
            var userEntity = _sqlSugarClient.Queryable<UserEntity>().First(x => x.Id == userId && x.DeleteMark == null && x.EnabledMark == 1);
            var token = NetHelper.GetToken(userEntity.Id, userEntity.Account, userEntity.RealName, userEntity.IsAdministrator, tenantId);
            var heraderDic = new Dictionary<string, object>();
            heraderDic.Add("Authorization", token);
            heraderDic.Add("jnpf_api", true);
            var scheduleTaskModel = new ScheduleTaskModel();
            scheduleTaskModel.taskParams.Add("id", id);
            scheduleTaskModel.taskParams.Add("input", input.ToJsonString());
            var path = string.Format("{0}/ScheduleTask/datainterface", GetLocalAddress());
            var result = await path.SetHeaders(heraderDic).SetBody(scheduleTaskModel).PostAsStringAsync();
            output = result.FirstOrDefault().Equals('[') ? result.ToObject<JArray>() : result.ToObject<JObject>();
        }
        sw.Stop();

        #region 插入日志

        if (App.HttpContext.IsNotEmptyOrNull())
        {
            var httpContext = App.HttpContext;
            var headers = httpContext.Request.Headers;
            var log = new DataInterfaceLogEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                OauthAppId = appId,
                InvokId = id,
                InvokTime = DateTime.Now,
                InvokIp = httpContext.GetLocalIpAddressToIPv4(),
                InvokDevice = string.Format("{0}-{1}", RuntimeInformation.OSDescription, userAgent.RawValue),
                InvokWasteTime = (int)sw.ElapsedMilliseconds,
                InvokType = httpContext.Request.Method
            };
            await _sqlSugarClient.Insertable(log).ExecuteCommandAsync();
        }
        #endregion

        return output;
    }

    /// <summary>
    /// HMACSHA256加密.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="interfaceId"></param>
    /// <param name="ymDate"></param>
    /// <returns></returns>
    private string GetVerifySignature(InterfaceOauthEntity entity, string interfaceId, string ymDate)
    {
        string secret = entity.AppSecret;
        string method = "POST";
        string urlPath = string.Format("/dev/api/system/DataInterface/{0}/Actions/Response", interfaceId);
        string YmDate = ymDate;
        string host = App.HttpContext.Request.Host.ToString();
        string source = new StringBuilder().Append(method).Append('\n').Append(urlPath).Append('\n')
                .Append(YmDate).Append('\n').Append(host).ToString();
        using (var hmac = new HMACSHA256(secret.ToBase64String().ToBytes()))
        {
            byte[] hashmessage = hmac.ComputeHash(source.ToBytes(Encoding.UTF8));
            var signature = hashmessage.ToHexString();
            return entity.AppId + "::" + signature;
        }
    }

    /// <summary>
    /// 外部接口验证.
    /// </summary>
    /// <param name="interfaceId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    private string VerifyInterfaceOauth(string interfaceId, ref string userId)
    {
        var authorization = App.HttpContext.Request.Headers["Authorization"].ToString();
        if (authorization.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.IO0001);
        var ymDate = App.HttpContext.Request.Headers["YmDate"].ToString();
        if (ymDate.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.IO0002);
        var appId = authorization.Split("::")[0];
        var appSecret = authorization.Split("::")[1];
        var isUserKey = App.HttpContext.Request.Headers.ContainsKey("UserKey");
        var userKey = isUserKey ? App.HttpContext.Request.Headers["UserKey"].ToString() : string.Empty;
        var interfaceEntity = _sqlSugarClient.Queryable<InterfaceOauthEntity>().First(x => x.AppId == appId && x.DeleteMark == null && x.EnabledMark == 1);
        if (interfaceEntity.IsNullOrEmpty() || interfaceEntity.DataInterfaceIds.IsNullOrEmpty() || !interfaceEntity.DataInterfaceIds.Contains(interfaceId))
            throw Oops.Oh(ErrorCode.IO0003);
        var dataInterfaceUserList = _sqlSugarClient.Queryable<DataInterfaceUserEntity>().Where(x => x.OauthId == interfaceEntity.Id).ToList();
        if (dataInterfaceUserList.Any())
        {
            if (isUserKey)
            {
                if (dataInterfaceUserList.Any(x => x.UserKey == userKey))
                {
                    userId = dataInterfaceUserList.Find(x => x.UserKey == userKey).UserId;
                }
                else
                {
                    throw Oops.Oh(ErrorCode.IO0009);
                }
            }
            else
            {
                throw Oops.Oh(ErrorCode.IO0008);
            }
        }
        if (interfaceEntity.WhiteList.IsNotEmptyOrNull())
        {
            var ipList = interfaceEntity.WhiteList.Split(",").ToList();
            if (!ipList.Contains(App.HttpContext.GetLocalIpAddressToIPv4()))
                throw Oops.Oh(ErrorCode.D9002);
        }
        if (interfaceEntity.UsefulLife.IsNotEmptyOrNull() && interfaceEntity.UsefulLife < DateTime.Now)
            throw Oops.Oh(ErrorCode.IO0004);
        if (interfaceEntity.VerifySignature == 1)
        {
            if (DateTime.Now > ymDate.TimeStampToDateTime().AddMinutes(1))
                throw Oops.Oh(ErrorCode.IO0004);
            var signature = GetVerifySignature(interfaceEntity, interfaceId, ymDate);
            if (authorization != signature)
                throw Oops.Oh(ErrorCode.IO0003);
        }
        else
        {
            if (interfaceEntity.AppSecret != appSecret)
                throw Oops.Oh(ErrorCode.IO0003);
        }
        return appId;
    }

    /// <summary>
    /// 本地地址.
    /// </summary>
    /// <returns></returns>
    private string GetLocalAddress()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var server = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        return addresses.FirstOrDefault().Replace("[::]", "localhost");
    }
    #endregion

    #region 代码生成

    /// <summary>
    /// 获取代码生成远端数据缓存.
    /// </summary>
    /// <param name="key">缓存标识.</param>
    /// <param name="dynamicId">远端数据ID.</param>
    /// <returns></returns>
    private async Task<List<StaticDataModel>> GetDynamicDataCache(string key, string dynamicId)
    {
        string cacheKey = string.Format("{0}{1}_{2}_{3}", CommonConst.CodeGenDynamic, _userManager.TenantId, key, dynamicId);
        return await _cacheManager.GetAsync<List<StaticDataModel>>(cacheKey);
    }

    /// <summary>
    /// 保存代码生成远端数据缓存.
    /// </summary>
    /// <param name="dynamicId">远端数据ID.</param>
    /// <param name="list">在线用户列表.</param>
    /// <param name="key">缓存标识.</param>
    /// <returns></returns>
    private async Task<bool> SetDynamicDataCache(string key, string dynamicId, List<StaticDataModel> list)
    {
        string cacheKey = string.Format("{0}{1}_{2}_{3}", CommonConst.CodeGenDynamic, _userManager.TenantId, key, dynamicId);
        return await _cacheManager.SetAsync(cacheKey, list, TimeSpan.FromMinutes(3));
    }

    /// <summary>
    /// 获取动态无限级数据.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="value">指定选项标签为选项对象的某个属性值.</param>
    /// <param name="label">指定选项的值为选项对象的某个属性值.</param>
    /// <param name="children">指定选项的子选项为选项对象的某个属性值.</param>
    /// <returns></returns>
    private List<StaticDataModel> GetDynamicInfiniteData(string data, string value, string label, string children)
    {
        List<StaticDataModel> list = new List<StaticDataModel>();
        foreach (JToken? info in JToken.Parse(data))
        {
            StaticDataModel dic = new StaticDataModel()
            {
                id = info.Value<string>(value),
                fullName = info.Value<string>(label)
            };
            list.Add(dic);
            if (info.Value<object>(children) != null && info.Value<object>(children).ToString() != string.Empty)
                list.AddRange(GetDynamicInfiniteData(info.Value<object>(children).ToString(), value, label, children));
        }

        return list;
    }
    #endregion
    #endregion
}