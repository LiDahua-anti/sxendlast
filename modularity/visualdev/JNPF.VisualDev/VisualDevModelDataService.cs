using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.EventBus.Sources;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Dtos.VisualDev;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.EventBus;
using JNPF.FriendlyException;
using JNPF.JsonSerialization;
using JNPF.Logging.Attributes;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.UnifyResult;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDev;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.VisualDev.Entitys.Model;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Dto.Operator;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NPOI.Util;
using SqlSugar;
using System.Data;
using System.Text.RegularExpressions;

namespace JNPF.VisualDev;

/// <summary>
/// 可视化开发基础.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "OnlineDev", Order = 172)]
[Route("api/visualdev/[controller]")]
public class VisualDevModelDataService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevEntity> _visualDevRepository;  // 在线开发功能实体

    /// <summary>
    /// SqlSugarClient客户端.
    /// </summary>
    private SqlSugarScope _sqlSugarClient;

    /// <summary>
    /// 可视化开发基础.
    /// </summary>
    private readonly IVisualDevService _visualDevService;

    /// <summary>
    /// 在线开发运行服务.
    /// </summary>
    private readonly RunService _runService;

    /// <summary>
    /// 模板表单列表数据解析.
    /// </summary>
    private readonly FormDataParsing _formDataParsing;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 切库.
    /// </summary>
    private readonly IDataBaseManager _databaseService;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 多租户事务.
    /// </summary>
    private readonly ITenant _db;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 事件总线.
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceScope _serviceScope;

    /// <summary>
    /// 初始化一个<see cref="VisualDevModelDataService"/>类型的新实例.
    /// </summary>
    public VisualDevModelDataService(
        IServiceScopeFactory serviceScopeFactory,
        ISqlSugarRepository<VisualDevEntity> visualDevRepository,
        ISqlSugarClient sqlSugarClient,
        IVisualDevService visualDevService,
        RunService runService,
        FormDataParsing formDataParsing,
        IDbLinkService dbLinkService,
        IDataInterfaceService dataInterfaceService,
        IUserManager userManager,
        IDataBaseManager databaseService,
        IBillRullService billRuleService,
        ICacheManager cacheManager,
        IFileManager fileManager,
        ISqlSugarClient context,
        IOrganizeService organizeService,
        IEventPublisher eventPublisher)
    {
        _visualDevRepository = visualDevRepository;
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
        _visualDevService = visualDevService;
        _databaseService = databaseService;
        _dbLinkService = dbLinkService;
        _runService = runService;
        _formDataParsing = formDataParsing;
        _billRuleService = billRuleService;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _fileManager = fileManager;
        _dataInterfaceService = dataInterfaceService;
        _db = context.AsTenant();
        _organizeService = organizeService;
        _eventPublisher = eventPublisher;
        _serviceScope = serviceScopeFactory.CreateScope();
    }

    #region Get

    /// <summary>
    /// 获取表单菜单配置JSON.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Config")]
    public async Task<dynamic> MenuConfig([FromQuery] VisualDevModelConfigInput input)
    {
        if (!await IsMenuAuthorize(input.menuId)) throw Oops.Oh(ErrorCode.COM1007);

        VisualDevReleaseEntity? releaseEntity = null;
        var menu = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(x => x.Id.Equals(input.menuId));

        if (menu.IsNotEmptyOrNull() && menu.PropertyJson.IsNotEmptyOrNull())
        {
            var dic = menu.PropertyJson.ToString().ToObject<Dictionary<string, object>>();
            if (dic.ContainsKey("moduleId") && dic["moduleId"].IsNotEmptyOrNull())
                releaseEntity = await _visualDevRepository.AsSugarClient().Queryable<VisualDevReleaseEntity>().FirstAsync(x => x.Id.Equals(dic["moduleId"]));
        }

        if (releaseEntity.IsNullOrEmpty() || (input.systemId.IsNotEmptyOrNull() && !menu.SystemId.Equals(input.systemId)))
            throw Oops.Oh(ErrorCode.COM1007);

        return releaseEntity.Adapt<VisualDevModelDataConfigOutput>();
    }

    /// <summary>
    /// 获取列表表单配置JSON.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <param name="type">1 线上版本, 0 草稿版本.</param>
    /// <param name="menuId">菜单id.</param>
    /// <returns></returns>
    [HttpGet("{modelId}/Config")]
    public async Task<dynamic> GetData(string modelId, string type, string menuId)
    {
        if (type.IsNullOrEmpty()) type = "1";
        VisualDevEntity? data = await _visualDevService.GetInfoById(modelId, type.Equals("1"));
        if (data == null) throw Oops.Bah(ErrorCode.COM1018, "该表单已删除");
        if (data.Type.Equals(1))
        {
            if (data.WebType.Equals(1) && data.FormData.IsNullOrWhiteSpace()) throw Oops.Bah(ErrorCode.COM1018, "该模板内表单内容为空，无法预览!");
            else if (data.WebType.Equals(2) && data.ColumnData.IsNullOrWhiteSpace()) throw Oops.Bah(ErrorCode.COM1018, "该模板内列表内容为空，无法预览!");
        }

        return await GetVisualDevModelDataConfig(data, menuId);
    }

    /// <summary>
    /// 获取列表配置JSON.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <returns></returns>
    [HttpGet("{modelId}/ColumnData")]
    public async Task<dynamic> GetColumnData(string modelId)
    {
        VisualDevEntity? data = await _visualDevService.GetInfoById(modelId);
        return new { columnData = data.ColumnData };
    }

    /// <summary>
    /// 获取列表配置JSON.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <returns></returns>
    [HttpGet("{modelId}/FormData")]
    public async Task<dynamic> GetFormData(string modelId)
    {
        VisualDevEntity? data = await _visualDevService.GetInfoById(modelId);
        return new { formData = data.FormData };
    }

    /// <summary>
    /// 获取数据信息.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="modelId"></param>
    /// <param name="menuId"></param>
    /// <returns></returns>
    [HttpGet("{modelId}/{id}")]
    public async Task<dynamic> GetInfo(string id, string modelId, [FromQuery] string menuId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true); // 模板实体

        // 有表
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            return new { id = id, data = (await _runService.GetHaveTableInfo(id, templateEntity, null, false, menuId)).ToJsonString() };
        else
            return null;
    }

    /// <summary>
    /// 获取详情.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/DataChange")]
    public async Task<dynamic> GetDetails(string modelId, [FromBody] VisualDataChangeInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true); // 模板实体

        var id = string.Empty;
        if (input.id is string)
            id = input.id.ToString();
        else if (input.id is long)
            id = input.id.ToString()?.TimeStampToDateTime().ToString();
        else
            id = input.id.ToJsonStringOld();

        // 有表
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
        {
            var data = await _runService.GetHaveTableInfoDetails(templateEntity, id, input.propsValue, input.menuId);
            var newId = data.IsNotEmptyOrNull() ? data.ToString().ToObject<Dictionary<string, object>>()["id"].ToString() : string.Empty;
            return new { id = newId, data = data };
        }
        else
        {
            return null;
        }
    }

    #endregion

    #region Post

    /// <summary>
    /// 功能导出.
    /// </summary>
    /// <param name="modelId"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string modelId)
    {
        var entity = new VisualDevEntity();
        var vREntity = await _visualDevService.GetInfoById(modelId, true);
        if (vREntity.IsNotEmptyOrNull())
            entity = vREntity.Adapt<VisualDevEntity>();
        else
            entity = await _visualDevService.GetInfoById(modelId);

        var templateEntity = entity.ToObject<VisualDevExportOutput>();
        templateEntity.aliasListJson = await _visualDevService.GetAliasList(modelId);

        var jsonStr = templateEntity.ToJsonString();
        return await _fileManager.Export(jsonStr, templateEntity.fullName, ExportFileType.vdd);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    [UnitOfWork]
    public async Task ActionsImport(IFormFile file, int type)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.vdd.ToString())) throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        VisualDevExportOutput? templateEntity;
        try
        {
            templateEntity = josn.ToObject<VisualDevExportOutput>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }

        if (templateEntity == null || templateEntity.type.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D3006);
        await _visualDevService.CreateImportData(templateEntity, type);
    }

    /// <summary>
    /// 获取数据列表.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <param name="input">分页查询条件.</param>
    /// <returns></returns>
    [HttpPost("{modelId}/List")]
    [UnifySerializerSetting("special")]
    public async Task<dynamic> List(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (input.flowIds.IsNullOrEmpty() && input.menuId.IsNotEmptyOrNull() && !await IsMenuAuthorize(input.menuId)) throw Oops.Oh(ErrorCode.COM1008);
        return await _runService.GetListResult(templateEntity, input);
    }

    /// <summary>
    /// 外链获取数据列表.
    /// </summary>
    /// <param name="modelId">主键id.</param>
    /// <param name="input">分页查询条件.</param>
    /// <returns></returns>
    [HttpPost("{modelId}/ListLink")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> ListLink(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (templateEntity == null) throw Oops.Oh(ErrorCode.D7009);
        return await _runService.GetListResult(templateEntity, input);
    }

    /// <summary>
    /// 创建数据.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="visualdevModelDataCrForm"></param>
    /// <returns></returns>
    [HttpPost("{modelId}")]
    public async Task Create(string modelId, [FromBody] VisualDevModelDataCrInput visualdevModelDataCrForm)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!await IsMenuButtonAuthorize(templateEntity, "btn_add")) throw Oops.Oh(ErrorCode.COM1008);
        await _runService.Create(templateEntity, visualdevModelDataCrForm);
    }

    /// <summary>
    /// 修改数据.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="id"></param>
    /// <param name="visualdevModelDataUpForm"></param>
    /// <returns></returns>
    [HttpPut("{modelId}/{id}")]
    public async Task Update(string modelId, string id, [FromBody] VisualDevModelDataUpInput visualdevModelDataUpForm)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!await IsMenuButtonAuthorize(templateEntity, "btn_edit")) throw Oops.Oh(ErrorCode.COM1008);
        await _runService.Update(id, templateEntity, visualdevModelDataUpForm);
    }

    /// <summary>
    /// 删除数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="modelId"></param>
    /// <returns></returns>
    [HttpDelete("{modelId}/{id}")]
    public async Task Delete(string id, string modelId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!await IsMenuButtonAuthorize(templateEntity, "btn_remove")) throw Oops.Oh(ErrorCode.COM1008);
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables)) await _runService.DelHaveTableInfo(id, templateEntity);
    }

    /// <summary>
    /// 删除子表数据（任务流程）.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpDelete("DelChildTable/{modelId}")]
    public async Task DelChildTable(string modelId, [FromBody] VisualDevModelDelChildTableInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables)) await _runService.DelChildTable(templateEntity, input);
    }

    /// <summary>
    /// 批量删除.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("batchDelete/{modelId}")]
    [UnitOfWork]
    public async Task BatchDelete(string modelId, [FromBody] VisualDevModelDataBatchDelInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (!(await IsMenuButtonAuthorize(templateEntity, "btn_remove") || await IsMenuButtonAuthorize(templateEntity, "btn_batchRemove"))) throw Oops.Oh(ErrorCode.COM1008);
        if (!string.IsNullOrEmpty(templateEntity.Tables) && !"[]".Equals(templateEntity.Tables))
            await _runService.BatchDelHaveTableData(input.ids, templateEntity, input);
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/Actions/ExportData")]
    public async Task<dynamic> ExportData(string modelId, [FromBody] VisualDevModelListQueryInput input)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        if (input.dataType == "1")
        {
            input.pageSize = 99999999;
            input.currentPage = 1;
        }
        PageResult<Dictionary<string, object>>? pageList = await _runService.GetListResult(templateEntity, input);

        // 如果是 分组表格 模板
        ColumnDesignModel? columnData = templateEntity.ColumnData.ToObject<ColumnDesignModel>(); // 列配置模型
        if (columnData.type == 3)
        {
            List<Dictionary<string, object>>? newValueList = new List<Dictionary<string, object>>();
            pageList.list.ForEach(item =>
            {
                List<Dictionary<string, object>>? tt = item["children"].ToJsonString().ToObject<List<Dictionary<string, object>>>();
                newValueList.AddRange(tt);
            });
            pageList.list = newValueList;
        }

        // 导出当前选择数据
        var selectList = new List<Dictionary<string, object>>();
        if (input.dataType == "2" && input.selectIds.Any())
        {
            foreach (var item in pageList.list)
            {
                if (item.ContainsKey("id") && (input.selectIds.Contains(item["id"]) || (item["id"].IsNullOrEmpty() && input.selectIds.Contains(null))))
                    selectList.Add(item);
            }

            pageList.list = selectList;
            pageList.pagination.total = selectList.Count;
        }

        var templateInfo = new TemplateParsingBase(templateEntity);
        templateInfo.AllFieldsModel.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item => item.__config__.label = string.Format("{0}({1})", item.__config__.label, item.__vModel__));
        var res = GetCreateFirstColumnsHeader(input.selectKey, pageList.list, templateInfo.AllFieldsModel, templateInfo.ColumnData);
        var firstColumns = res.Item1;
        var resultList = res.Item2;
        var newResultList = new List<Dictionary<string, object>>();

        // 行内编辑
        if (templateInfo.ColumnData.type.Equals(4))
        {
            resultList.ForEach(row =>
            {
                foreach (var data in row) if (data.Key.Contains("_name") && row.ContainsKey(data.Key.Replace("_name", string.Empty))) row[data.Key.Replace("_name", string.Empty)] = data.Value;
            });
        }

        resultList.ForEach(row =>
        {
            foreach (var item in input.selectKey)
            {
                if (row[item].IsNotEmptyOrNull())
                {
                    newResultList.Add(row);
                    break;
                }
            }
        });

        if (!newResultList.Any())
        {
            var dic = new Dictionary<string, object>();
            dic.Add("id", "id");
            foreach (var item in input.selectKey) dic.Add(item, string.Empty);
            newResultList.Add(dic);
        }

        var fName = input.menuId.IsNotEmptyOrNull() ? await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.Id.Equals(input.menuId)).Select(it => it.FullName).FirstAsync() : templateEntity.FullName;
        fName = _fileManager.DetectionSpecialStr(fName);
        var excelName = string.Format("{0}_{1}", fName, DateTime.Now.ToString("yyyyMMddHHmmss"));
        _cacheManager.Set(excelName + ".xls", string.Empty);
        return firstColumns.Any() ? await ExcelCreateModel(templateInfo.AllFieldsModel, newResultList, input.selectKey, false, excelName, firstColumns)
            : await ExcelCreateModel(templateInfo.AllFieldsModel, newResultList, input.selectKey, false, excelName);
    }

    /// <summary>
    /// 模板下载.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{modelId}/TemplateDownload")]
    public async Task<dynamic> TemplateDownload(string modelId, string menuId)
    {
        var tInfo = await GetUploaderTemplateInfoAsync(modelId);

        if (tInfo.selectKey == null || !tInfo.selectKey.Any()) throw Oops.Oh(ErrorCode.D1411);

        // 初始化 一条空数据
        List<Dictionary<string, object>>? dataList = new List<Dictionary<string, object>>();

        // 赋予默认值
        var dicItem = new Dictionary<string, object>();
        tInfo.AllFieldsModel.Where(x => tInfo.selectKey.Contains(x.__vModel__)).ToList().ForEach(item =>
        {
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.COMSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "公司名称/部门名称,公司名称/部门名称" : "公司名称/部门名称");
                    break;
                case JnpfKeyConst.POSSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "组织名称/岗位名称,组织名称/岗位名称" : "组织名称/岗位名称");
                    break;
                case JnpfKeyConst.USERSSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "姓名/账号,公司名称,组织名称/岗位名称,角色名称/角色编码,分组名称/分组编码" : "姓名/账号");
                    break;
                case JnpfKeyConst.USERSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "姓名/账号,姓名/账号" : "姓名/账号");
                    break;
                case JnpfKeyConst.ROLESELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "角色名称/角色编码,角色名称/角色编码" : "角色名称/角色编码");
                    break;
                case JnpfKeyConst.GROUPSELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "分组名称/分组编码,分组名称/分组编码" : "分组名称/分组编码");
                    break;
                case JnpfKeyConst.DATE:
                case JnpfKeyConst.TIME:
                    dicItem.Add(item.__vModel__, string.Format("{0}", item.format));
                    break;
                case JnpfKeyConst.ADDRESS:
                    switch (item.level)
                    {
                        case 0:
                            dicItem.Add(item.__vModel__, item.multiple ? "省,省" : "省");
                            break;
                        case 1:
                            dicItem.Add(item.__vModel__, item.multiple ? "省/市,省/市" : "省/市");
                            break;
                        case 2:
                            dicItem.Add(item.__vModel__, item.multiple ? "省/市/区,省/市/区" : "省/市/区");
                            break;
                        case 3:
                            dicItem.Add(item.__vModel__, item.multiple ? "省/市/区/街道,省/市/区/街道" : "省/市/区/街道");
                            break;
                    }
                    break;
                case JnpfKeyConst.SELECT:
                    if (item.multiple) dicItem.Add(item.__vModel__, "选项一,选项二");
                    break;
                case JnpfKeyConst.CHECKBOX:
                    dicItem.Add(item.__vModel__, "选项一,选项二");
                    break;
                case JnpfKeyConst.CASCADER:
                    dicItem.Add(item.__vModel__, item.multiple ? "选项1/选项1-1,选项2/选项2-1" : "选项1/选项1-1");
                    break;
                case JnpfKeyConst.TREESELECT:
                    dicItem.Add(item.__vModel__, item.multiple ? "选项1,选项2" : "选项1");
                    break;
                default:
                    dicItem.Add(item.__vModel__, string.Empty);
                    break;
            }
        });
        dicItem.Add("id", "id");
        dataList.Add(dicItem);

        var fName = menuId.IsNotEmptyOrNull() ? await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.Id.Equals(menuId)).Select(it => it.FullName).FirstAsync() : tInfo.visualDevEntity.FullName;
        fName = _fileManager.DetectionSpecialStr(fName);
        var excelName = string.Format("{0}导入模板", fName);
        var res = GetCreateFirstColumnsHeader(tInfo.selectKey, dataList, tInfo.AllFieldsModel, tInfo.ColumnData);
        var firstColumns = res.Item1;
        var resultList = res.Item2;
        _cacheManager.Set(excelName + ".xls", string.Empty);
        return firstColumns.Any()
            ? await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, true, excelName, firstColumns)
            : await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, true, excelName);
    }

    /// <summary>
    /// 上传文件.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Uploader")]
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
    [HttpGet("{modelId}/ImportPreview")]
    public async Task<dynamic> ImportPreview(string modelId, string fileName)
    {
        var tInfo = await GetUploaderTemplateInfoAsync(modelId);
        return await GetImportDataPreview(tInfo, fileName);
    }

    /// <summary>
    /// 导入数据的错误报告.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/ImportExceptionData")]
    [UnitOfWork]
    public async Task<dynamic> ExportExceptionData(string modelId, [FromBody] VisualDevImportDataInput input)
    {
        var tInfo = await GetUploaderTemplateInfoAsync(modelId);
        //object[]? res = await ImportMenuData(tInfo, list.list, tInfo.visualDevEntity);

        // 错误数据
        tInfo.selectKey.Insert(0, "errorsInfo");
        tInfo.AllFieldsModel.Add(new FieldsModel() { __vModel__ = "errorsInfo", __config__ = new ConfigModel() { label = "异常原因" } });
        for (var i = 0; i < input.list.Count(); i++) input.list[i].Add("id", i);

        var result = GetCreateFirstColumnsHeader(tInfo.selectKey, input.list, tInfo.AllFieldsModel, tInfo.ColumnData);
        var firstColumns = result.Item1;
        var resultList = result.Item2;

        var menuName = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.Id.Equals(input.menuId)).Select(it => it.FullName).FirstAsync();
        var excelName = string.Format("{0}错误报告_{1}", menuName, DateTime.Now.ToString("yyyyMMddHHmmss"));
        _cacheManager.Set(excelName + ".xls", string.Empty);
        return firstColumns.Any()
            ? await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, true, excelName, firstColumns)
            : await ExcelCreateModel(tInfo.AllFieldsModel, resultList, tInfo.selectKey, true, excelName);
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/ImportData")]
    [UnitOfWork]
    public async Task<dynamic> ImportData(string modelId, [FromBody] VisualDevImportDataInput input)
    {
        var result = new VisualDevImportDataOutput();
        var tInfo = await GetUploaderTemplateInfoAsync(modelId);

        if (input.type)
        {
            var importDataPreview = await GetImportDataPreview(tInfo, input.fileName);
            input.list = importDataPreview.dataRow;
            result.headerRow = importDataPreview.headerRow;
        }

        if (input.flowId.IsNotEmptyOrNull())
        {
            var flowId = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(it => it.DeleteMark == null && it.TemplateId == input.flowId && it.Status == 1).Select(it => it.Id).FirstAsync();
            foreach (var item in input.list) item.Add("flowId", flowId);
        }

        object[]? res = await ImportMenuData(tInfo, input);
        var addlist = res.First() as List<Dictionary<string, object>>;
        var errorlist = res.Last() as List<Dictionary<string, object>>;

        result.snum = addlist.Count;
        result.fnum = errorlist.Count;
        result.failResult = errorlist;
        result.resultType = errorlist.Count < 1 ? 0 : 1;

        return result;
    }

    /// <summary>
    /// 自定义按钮发起流程.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/actionLaunchFlow")]
    [NonUnify]
    public async Task<dynamic> ActionLaunchFlow([FromBody] VisualDevModelDataFlowInput input)
    {
        if (input.dataList.IsNullOrEmpty() || (input.dataList.IsNotEmptyOrNull() && input.dataList.Count == 0))
            return new RESTfulResult<object> { code = 400, msg = "未选择记录，发起失败" };

        // 处理用户
        var initiator = new List<string>();
        if (input.customUser.Equals(1))
        {
            var objIds = new List<string>();
            foreach (var item in input.initiator)
            {
                var id = item.Replace("--user", string.Empty).Replace("--department", string.Empty).Replace("--company", string.Empty).Replace("--role", string.Empty).Replace("--position", string.Empty).Replace("--group", string.Empty);
                objIds.Add(id);
            }

            initiator.AddRange(await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.UserId == b.Id))
                .Where((a, b) => b.DeleteMark == null && (objIds.Contains(a.ObjectId) || objIds.Contains(a.UserId)) && b.EnabledMark > 0).Select(a => a.UserId).Distinct().ToListAsync());
        }
        if (input.currentUser.Equals(1) && !initiator.Contains(_userManager.UserId)) initiator.Add(_userManager.UserId);
        if (initiator.Count == 0) return new RESTfulResult<object> { code = 400, msg = "找不到发起人，发起失败" };

        var parameter = new { candidateType = 3, countersignOver = true, flowId = input.template, formData = new Dictionary<string, object>(), status = 1, hasPermission = input.hasPermission };
        var localAddress = GetLocalAddress();
        var path = string.Format("{0}/api/workflow/task", localAddress);
        var path1 = string.Format("{0}/api/workflow/Operator/CandidateNode/0", localAddress);

        var result = new RESTfulResult<object> { code = 200, msg = "操作成功" };
        foreach (var data in input.dataList)
        {
            parameter.formData.Clear();
            foreach (var item in data)
            {
                if (item.targetField.Contains("tableField"))
                {
                    var targetField = item.targetField.Split("-");
                    if (parameter.formData.ContainsKey(targetField[0]))
                    {
                        var cList = parameter.formData[targetField[0]].ToObject<List<Dictionary<string, object>>>();
                        if (item.sourceValue.Contains("tableField"))
                        {
                            var cData = item.defaultValue.ToObject<List<object>>();
                            for (var i = 0; i < cData.Count; i++)
                            {
                                if (cList[i].IsNotEmptyOrNull())
                                    cList[i][targetField[1]] = cData[i];
                                else
                                    cList.Add(new Dictionary<string, object>() { { targetField[1], cData[i] } });
                            }
                        }
                        else
                        {
                            cList[0][targetField[1]] = item.defaultValue;
                        }

                        parameter.formData[targetField[0]] = cList;
                    }
                    else
                    {
                        var cList = new List<Dictionary<string, object>>();
                        if (item.sourceValue.Contains("tableField"))
                        {
                            foreach (var cItem in item.defaultValue.ToObject<List<object>>())
                            {
                                cList.Add(new Dictionary<string, object>() { { targetField[1], cItem } });
                            }
                        }
                        else
                        {
                            cList.Add(new Dictionary<string, object>() { { targetField[1], item.defaultValue } });
                        }

                        parameter.formData.Add(targetField[0], cList);
                    }
                }
                else
                {
                    parameter.formData[item.targetField] = item.defaultValue;
                }
            }

            var skinCount = 0;
            foreach (var userId in initiator)
            {
                var heraderParameters = new Dictionary<string, object>(); // 头部参数.
                var user = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().FirstAsync(it => it.Id.Equals(userId));
                var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, _userManager.TenantId);
                heraderParameters.Add("Authorization", toKen);
                heraderParameters.Add("Jnpf-Origin", "pc");
                var bodyParameters = parameter.ToJsonString();

                result = (await path1.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(heraderParameters).SetBody(bodyParameters).PostAsStringAsync()).ToObject<RESTfulResult<object>>();
                if (result.code == 200)
                {
                    var output = result.data.ToObject<Dictionary<string, object>>();
                    if (output.IsNotEmptyOrNull() && output.ContainsKey("type") && output["countersignOver"].ToString() == "True")
                    {
                        var type = output.ContainsKey("type") ? output["type"].ToString() : string.Empty;
                        if (type == "1")
                        {
                            result = new RESTfulResult<object>
                            {
                                code = 400,
                                msg = "发起节点设置了选择分支，无法发起审批"
                            };
                            break;
                        }
                        else if (type == "2")
                        {
                            result = new RESTfulResult<object>
                            {
                                code = 400,
                                msg = "第一个审批节点设置候选人，无法发起审批"
                            };
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }

                result = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(heraderParameters).SetBody(bodyParameters).PostAsStringAsync()).ToObject<RESTfulResult<object>>();
                if (result.code == 200)
                {
                    var output = result.data.ToObject<OperatorOutput>();
                    if (output.IsNotEmptyOrNull() && output.errorCodeList.Any())
                    {
                        result = new RESTfulResult<object>
                        {
                            code = 400,
                            msg = "第一个审批节点异常，无法发起审批"
                        };
                        break;
                    }
                }
                else
                {
                    if (result.msg.Equals("[WF0052] 您没有发起该流程的权限"))
                    {
                        skinCount++;
                        result = new RESTfulResult<object> { code = 200, msg = "操作成功" };
                        continue;
                    }

                    break;
                }
            }

            if (skinCount == initiator.Count)
            {
                result = new RESTfulResult<object>
                {
                    code = 400,
                    msg = "找不到发起人，发起失败"
                };
            }
        }

        return result;
    }

    #endregion

    #region 任务流程
    /// <summary>
    /// 批量新增/修改.
    /// </summary>
    /// <param name="modelId"></param>
    /// <param name="visualdevModelDataCrForm"></param>
    /// <returns></returns>
    [HttpPost("{modelId}/Save")]
    public async Task Save(string modelId, [FromBody] VisualDevModelBatchInput input)
    {
        if (input.isCreate)
        {
            foreach (var item in input.dataList)
            {
                input.data = item.ToJsonStringOld();
                await Create(modelId, input);
            }
        }
        else
        {
            foreach (var item in input.dataList)
            {
                input.data = item.ToJsonStringOld();
                if (input.data.ToObject<Dictionary<string, object>>().TryGetValue("id", out object value))
                {
                    var id = value.ToString();
                    await Update(modelId, id, input);
                }
                else
                {
                    await Create(modelId, input);
                }
            }
        }
    }
    #endregion

    #region PublicMethod

    /// <summary>
    /// Excel 转输出 Model.
    /// </summary>
    /// <param name="fieldList">控件集合.</param>
    /// <param name="realList">数据列表.</param>
    /// <param name="keys"></param>
    /// <param name="isImport"></param>
    /// <param name="excelName">导出文件名称.</param>
    /// <param name="firstColumns">手动输入第一行（合并主表列和各个子表列）.</param>
    /// <returns>VisualDevModelDataExportOutput.</returns>
    public async Task<VisualDevModelDataExportOutput> ExcelCreateModel(List<FieldsModel> fieldList, List<Dictionary<string, object>> realList, List<string> keys, bool isImport, string excelName = null, Dictionary<string, int> firstColumns = null)
    {
        VisualDevModelDataExportOutput output = new VisualDevModelDataExportOutput();
        try
        {
            List<string> columnList = new List<string>();
            ExcelConfig excelconfig = new ExcelConfig();
            excelconfig.FileName = (excelName.IsNullOrEmpty() ? SnowflakeIdHelper.NextId() : excelName) + ".xls";
            excelconfig.HeadFont = "微软雅黑";
            excelconfig.HeadPoint = 10;
            excelconfig.IsAllSizeColumn = true;
            excelconfig.IsBold = true;
            excelconfig.IsAllBorder = true;
            excelconfig.IsImport = isImport;
            excelconfig.ColumnModel = new List<ExcelColumnModel>();
            foreach (string? item in keys)
            {
                FieldsModel? excelColumn = fieldList.Find(t => t.__vModel__ == item);
                if (excelColumn != null)
                {
                    // Excel下拉的数据
                    var selectList = new List<string>();
                    if (isImport)
                    {
                        switch (excelColumn.__config__.jnpfKey)
                        {
                            case JnpfKeyConst.RADIO:
                            case JnpfKeyConst.SELECT:
                                if (!excelColumn.multiple)
                                {
                                    var propsLabel = excelColumn.props != null ? excelColumn.props.label : string.Empty;

                                    if (excelColumn.__config__.dataType.Equals("static") && excelColumn.options != null)
                                    {
                                        foreach (var option in excelColumn.options) selectList.Add(option[propsLabel].ToString());
                                    }
                                    else if (excelColumn.__config__.dataType.Equals("dictionary"))
                                    {
                                        var dictionaryDataList = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity, DictionaryTypeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.DictionaryTypeId))
                                            .WhereIF(excelColumn.__config__.dictionaryType.IsNotEmptyOrNull(), (a, b) => b.Id == excelColumn.__config__.dictionaryType || b.EnCode == excelColumn.__config__.dictionaryType)
                                            .Where(a => a.DeleteMark == null && a.EnabledMark == 1).Select(a => new { a.Id, a.EnCode, a.FullName }).ToListAsync();
                                        foreach (var it in dictionaryDataList) selectList.Add(it.FullName);
                                    }
                                    else if (excelColumn.__config__.dataType.Equals("dynamic"))
                                    {
                                        var dataList = await _formDataParsing.GetDynamicList(excelColumn);
                                        foreach (var data in dataList) selectList.Add(data.FirstOrDefault().Value);
                                    }
                                }

                                break;
                            case JnpfKeyConst.SWITCH:
                                if (excelColumn.activeTxt.IsNotEmptyOrNull())
                                    selectList.Add(excelColumn.activeTxt);
                                else
                                    selectList.Add("开");

                                if (excelColumn.inactiveTxt.IsNotEmptyOrNull())
                                    selectList.Add(excelColumn.inactiveTxt);
                                else
                                    selectList.Add("关");

                                break;
                        }
                    }

                    // 数据类型
                    var type = string.Empty;
                    switch (excelColumn.__config__.jnpfKey)
                    {
                        case JnpfKeyConst.NUMINPUT:
                        case JnpfKeyConst.CALCULATE:
                            type = "decimal";
                            break;
                    }

                    excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item, ExcelColumn = excelColumn.__config__.label, Required = excelColumn.__config__.required, SelectList = selectList, Type = type });
                    columnList.Add(excelColumn.__config__.label);
                }
            }

            string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
            var fs = firstColumns == null ? ExcelExportHelper<Dictionary<string, object>>.ExportMemoryStream(realList, excelconfig, columnList) : ExcelExportHelper<Dictionary<string, object>>.ExportMemoryStream(realList, excelconfig, columnList, firstColumns);
            var flag = await _fileManager.UploadFileByType(fs, FileVariable.TemporaryFilePath, excelconfig.FileName);
            if (flag)
            {
                fs.Flush();
                fs.Close();
            }
            output.name = excelconfig.FileName;
            output.url = "/api/file/Download?encryption=" + DESEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF");
            return output;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 组装导出带子表得数据,返回 第一个合并行标头,第二个导出数据.
    /// </summary>
    /// <param name="selectKey">导出选择列.</param>
    /// <param name="realList">原数据集合.</param>
    /// <param name="fieldList">控件列表.</param>
    /// <param name="columnDesignModel"></param>
    /// <returns>第一行标头 , 导出数据.</returns>
    public (Dictionary<string, int>, List<Dictionary<string, object>>) GetCreateFirstColumnsHeader(List<string> selectKey, List<Dictionary<string, object>> realList, List<FieldsModel> fieldList, ColumnDesignModel columnDesignModel = null)
    {
        // 是否有复杂表头
        var isComplexHeader = false;
        if (!columnDesignModel.type.Equals(3) && !columnDesignModel.type.Equals(5) && columnDesignModel.complexHeaderList.Any())
        {
            foreach (var item in columnDesignModel.complexHeaderList)
            {
                foreach (var subItem in item.childColumns)
                {
                    if (selectKey.Contains(subItem))
                    {
                        isComplexHeader = true;
                        break;
                    }
                }
            }
        }

        selectKey.ForEach(item =>
        {
            realList.ForEach(it =>
            {
                if (!it.ContainsKey(item)) it.Add(item, string.Empty);
            });
        });

        var addItemList = new List<Dictionary<int, Dictionary<string, object>>>();
        var num = 0;
        realList.ForEach(items =>
        {
            var rowChildDatas = new Dictionary<string, List<Dictionary<string, object>>>();
            foreach (var item in items)
            {
                if (item.Value != null && item.Key.ToLower().Contains("tablefield") && (item.Value is List<Dictionary<string, object>> || item.Value.GetType().Name.Equals("JArray")))
                {
                    var ctList = item.Value.ToObject<List<Dictionary<string, object>>>();
                    rowChildDatas.Add(item.Key, ctList);
                }
            }

            var len = rowChildDatas.Select(x => x.Value.Count()).OrderByDescending(x => x).FirstOrDefault();

            if (len != null && len > 0)
            {
                for (int i = 0; i < len; i++)
                {
                    if (i == 0)
                    {
                        var newRealItem = realList.Find(x => x["id"].Equals(items["id"]));
                        foreach (var cData in rowChildDatas)
                        {
                            var itemData = cData.Value.FirstOrDefault();
                            if (itemData != null)
                            {
                                foreach (var key in itemData)
                                    if (newRealItem.ContainsKey(cData.Key + "-" + key.Key)) newRealItem[cData.Key + "-" + key.Key] = key.Value;
                            }
                        }
                    }
                    else
                    {
                        var newRealItem = new Dictionary<string, object>();
                        foreach (var it in items)
                        {
                            if (it.Key.Equals("id")) newRealItem.Add(it.Key, it.Value);
                            else newRealItem.Add(it.Key, string.Empty);
                        }
                        foreach (var cData in rowChildDatas)
                        {
                            if (cData.Value.Count > i)
                            {
                                foreach (var it in cData.Value[i])
                                    if (newRealItem.ContainsKey(cData.Key + "-" + it.Key)) newRealItem[cData.Key + "-" + it.Key] = it.Value;
                            }
                        }
                        var dicItem = new Dictionary<int, Dictionary<string, object>>();
                        dicItem.Add(num + 1, newRealItem);
                        addItemList.Add(dicItem);
                    }
                }
            }

            num++;
        });
        for (int i = 0; i < addItemList.Count; i++)
        {
            var dic = addItemList[i].FirstOrDefault();
            realList.Insert(dic.Key + i, dic.Value);
        }

        var firstColumns = new Dictionary<string, int>();
        if (selectKey.Any(x => x.Contains("-") && x.ToLower().Contains("tablefield")) || isComplexHeader)
        {
            var empty = string.Empty;
            var keyList = selectKey.Select(x => x.Split("-").First()).Distinct().ToList();

            var complexHeaderField = new List<string>();
            var lastName = "jnpf-singlefield";
            foreach (var item in keyList)
            {
                if (item.ToLower().Contains("tablefield"))
                {
                    var title = fieldList.FirstOrDefault(x => x.__vModel__.Equals(item))?.__config__.label;
                    firstColumns.Add(title + empty, selectKey.Count(x => x.Contains(item)));
                    empty += " ";
                }
                else if (!complexHeaderField.Contains(item))
                {
                    var flag = false;
                    foreach (var ch in columnDesignModel.complexHeaderList)
                    {
                        if (ch.childColumns.Contains(item))
                        {
                            var columns = new List<string>();
                            foreach (var sk in selectKey)
                            {
                                if (ch.childColumns.Contains(sk)) columns.Add(sk);
                            }

                            // 调整 selectKey 顺序
                            var index = selectKey.IndexOf(item);
                            foreach (var col in columns)
                            {
                                selectKey.Remove(col);
                                selectKey.Insert(index, col);
                                index++;
                            }

                            complexHeaderField.AddRange(columns);
                            flag = true;
                            lastName = ch.fullName;
                            firstColumns[ch.fullName] = columns.Count;

                            break;
                        }
                    }

                    // 字段没在复杂表头
                    if (!flag)
                    {
                        if (lastName.Contains("jnpf-singlefield"))
                        {
                            if (firstColumns.ContainsKey("jnpf-singlefield" + empty))
                                firstColumns[lastName]++;
                            else
                                firstColumns.Add("jnpf-singlefield" + empty, 1);
                        }
                        else
                        {
                            empty += " ";
                            lastName = "jnpf-singlefield" + empty;
                            firstColumns.Add("jnpf-singlefield" + empty, 1);
                        }
                    }
                }
            }
        }

        return (firstColumns, realList);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取导出模板信息.
    /// </summary>
    /// <param name="modelId"></param>
    /// <returns></returns>
    private async Task<TemplateParsingBase> GetUploaderTemplateInfoAsync(string modelId)
    {
        VisualDevEntity? templateEntity = await _visualDevService.GetInfoById(modelId, true);
        var tInfo = new TemplateParsingBase(templateEntity);
        tInfo.DbLink = await _dbLinkService.GetInfo(templateEntity.DbLinkId);
        if (tInfo.DbLink == null) tInfo.DbLink = _databaseService.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName); // 当前数据库连接
        var tableList = _databaseService.GetFieldList(tInfo.DbLink, tInfo.MainTableName); // 获取主表所有列
        var mainPrimary = tableList.Find(t => t.primaryKey); // 主表主键
        if (mainPrimary == null || mainPrimary.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D1402); // 主表未设置主键
        tInfo.MainPrimary = mainPrimary.field;
        tInfo.AllFieldsModel = tInfo.AllFieldsModel.Where(x => !x.__config__.jnpfKey.Equals(JnpfKeyConst.UPLOADFZ)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.UPLOADIMG)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.COLORPICKER)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPTABLESELECT)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORMATTR)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPATTR)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.QRCODE)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.BARCODE)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.SIGN)
        && !x.__config__.jnpfKey.Equals(JnpfKeyConst.LOCATION)).ToList();
        tInfo.AllFieldsModel.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(item => item.__config__.label = string.Format("{0}({1})", item.__config__.label, item.__vModel__));
        return tInfo;
    }

    /// <summary>
    /// 获取导入数据.
    /// </summary>
    /// <param name="tInfo"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private async Task<VisualDevModelDataPreviewOutput> GetImportDataPreview(TemplateParsingBase tInfo, string fileName)
    {
        var output = new VisualDevModelDataPreviewOutput();

        var isChildTable = tInfo.selectKey.Any(x => tInfo.ChildTableFields.ContainsKey(x));

        // 复杂表头
        if (!tInfo.ColumnData.type.Equals(3) && !tInfo.ColumnData.type.Equals(5) && tInfo.ColumnData.complexHeaderList.Any())
        {
            var complexHeaderField = new List<string>();
            foreach (var key in tInfo.selectKey.Select(x => x.Split("-").First()).Distinct().ToList())
            {
                if (!complexHeaderField.Contains(key))
                {
                    foreach (var ch in tInfo.ColumnData.complexHeaderList)
                    {
                        if (ch.childColumns.Contains(key))
                        {
                            var columns = new List<string>();
                            foreach (var sk in tInfo.selectKey)
                            {
                                if (ch.childColumns.Contains(sk)) columns.Add(sk);
                            }

                            // 调整 selectKey 顺序
                            var index = tInfo.selectKey.IndexOf(key);
                            foreach (var col in columns)
                            {
                                tInfo.selectKey.Remove(col);
                                tInfo.selectKey.Insert(index, col);
                                index++;
                                isChildTable = true;
                            }

                            complexHeaderField.AddRange(columns);
                        }
                    }
                }
            }
        }

        var fileEncode = new List<FieldsModel>();
        foreach (var key in tInfo.selectKey)
        {
            var model = tInfo.AllFieldsModel.Find(x => key.Equals(x.__vModel__));
            if (model.IsNotEmptyOrNull()) fileEncode.Add(model);
        }

        string? savePath = Path.Combine(FileVariable.TemporaryFilePath, fileName);

        // 得到数据
        var sr = await _fileManager.GetFileStream(savePath);
        var excelData = new DataTable();
        if (isChildTable) excelData = ExcelImportHelper.ToDataTable(savePath, sr, 0, 0, 2);
        else excelData = ExcelImportHelper.ToDataTable(savePath, sr);
        if (excelData.Columns.Count > tInfo.selectKey.Count) excelData.Columns.RemoveAt(tInfo.selectKey.Count);
        if (excelData.DefaultView.Count > 1000) throw Oops.Oh(ErrorCode.D1423);

        try
        {
            for (int i = 0; i < fileEncode.Count; i++)
            {
                if (i + 1 > excelData.Columns.Count) throw new Exception();
                DataColumn? column = excelData.Columns[i];
                if (!(fileEncode[i].__vModel__ == column.ColumnName && fileEncode[i].__config__.label.Split("(").First() == column.Caption.Replace("*", string.Empty)))
                    throw new Exception();
            }

            output.dataRow = excelData.ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>();
            if (output.dataRow.Any())
            {
                if (isChildTable)
                {
                    var hRow = output.dataRow[1].Copy();
                    var hRow2 = output.dataRow[0].Copy();
                    foreach (var it in hRow) if (it.Value.IsNullOrEmpty()) hRow[it.Key] = hRow2[it.Key];

                    foreach (var item in hRow)
                    {
                        if (item.Key.ToLower().Contains("tablefield") && item.Key.Contains('-'))
                        {
                            var childVModel = item.Key.Split("-").First();
                            if (!output.headerRow.Any(x => x.id.Equals(childVModel)))
                            {
                                var child = new List<dynamic>();
                                hRow.Where(x => x.Key.Contains(childVModel)).ToList().ForEach(x =>
                                {
                                    child.Add(new { id = x.Key.Replace(childVModel + "-", string.Empty), fullName = x.Value.ToString().Replace(string.Format("({0})", x.Key), string.Empty) });
                                });
                                output.headerRow.Add(new { id = childVModel, fullName = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(childVModel)).__config__.label.Replace(string.Format("({0})", childVModel), string.Empty), jnpfKey = "table", children = child });
                            }
                        }
                        else if (tInfo.ColumnData.complexHeaderList.Count > 0 && tInfo.ColumnData.complexHeaderList.Any(it => it.childColumns.Contains(item.Key)))
                        {
                            var complexHeaderModel = tInfo.ColumnData.complexHeaderList.Find(it => it.childColumns.Contains(item.Key));
                            if (!output.headerRow.Any(x => x.id.Equals(complexHeaderModel.id)))
                            {
                                var child = new List<dynamic>();
                                foreach (var key in tInfo.selectKey)
                                {
                                    if (complexHeaderModel.childColumns.Contains(key) && hRow.ContainsKey(key))
                                        child.Add(new { id = key, fullName = hRow[key].ToString() });
                                }
                                output.headerRow.Add(new { id = complexHeaderModel.id, fullName = complexHeaderModel.fullName, jnpfKey = "complexHeader", children = child });
                            }
                        }
                        else
                        {
                            output.headerRow.Add(new { id = item.Key, fullName = item.Value.ToString() });
                        }
                    }
                    output.dataRow.Remove(output.dataRow.First());
                    output.dataRow.Remove(output.dataRow.First());
                }
                else
                {
                    foreach (var item in output.dataRow.First().Copy()) output.headerRow.Add(new { id = item.Key, fullName = item.Value.ToString() });
                    output.dataRow.Remove(output.dataRow.First());
                }
            }
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1410);
        }

        try
        {
            // 带子表字段数据导入
            var newData = new List<Dictionary<string, object>>();
            if (isChildTable)
            {
                var singleForm = tInfo.selectKey.Where(x => !x.ToLower().Contains("tablefield")).ToList();

                var childTableVModel = tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).Select(x => x.__vModel__).ToList();

                output.dataRow.ForEach(dataItem =>
                {
                    var addItem = new Dictionary<string, object>();
                    var isNextRow = false;
                    foreach (var item in dataItem)
                    {
                        if (singleForm.Contains(item.Key) && item.Value.IsNotEmptyOrNull())
                            isNextRow = true;
                    }

                    // 单条数据 (多行子表数据合并)
                    if (isNextRow)
                    {
                        singleForm.ForEach(item => addItem.Add(item, dataItem[item]));

                        // 子表数据
                        childTableVModel.ForEach(item =>
                        {
                            var childAddItem = new Dictionary<string, object>();
                            tInfo.selectKey.Where(x => x.Contains(item)).ToList().ForEach(it =>
                            {
                                if (dataItem.ContainsKey(it))
                                    childAddItem.Add(it.Replace(item + "-", string.Empty), dataItem[it]);
                            });

                            if (childAddItem.Any()) addItem.Add(item, new List<Dictionary<string, object>> { childAddItem });
                        });

                        newData.Add(addItem);
                    }
                    else
                    {
                        var item = newData.LastOrDefault();
                        if (item != null)
                        {
                            // 子表数据
                            childTableVModel.ForEach(citem =>
                            {
                                var childAddItem = new Dictionary<string, object>();
                                tInfo.selectKey.Where(x => x.Contains(citem)).ToList().ForEach(it =>
                                {
                                    childAddItem.Add(it.Replace(citem + "-", string.Empty), dataItem[it]);
                                });

                                if (childAddItem.Any(x => x.Value.IsNotEmptyOrNull()))
                                {
                                    if (!item.ContainsKey(citem))
                                    {
                                        item.Add(citem, new List<Dictionary<string, object>> { childAddItem });
                                    }
                                    else
                                    {
                                        var childList = item[citem].ToJsonString().ToObjectOld<List<Dictionary<string, object>>>();
                                        childList.Add(childAddItem);
                                        item[citem] = childList;
                                    }
                                }
                            });

                            if (!childTableVModel.Any()) newData.Add(dataItem);
                        }
                        else
                        {
                            singleForm.ForEach(item => addItem.Add(item, dataItem[item]));

                            // 子表数据
                            childTableVModel.ForEach(item =>
                            {
                                var childAddItem = new Dictionary<string, object>();
                                tInfo.selectKey.Where(x => x.Contains(item)).ToList().ForEach(it =>
                                {
                                    if (dataItem.ContainsKey(it))
                                        childAddItem.Add(it.Replace(item + "-", string.Empty), dataItem[it]);
                                });

                                if (childAddItem.Any(x => x.Value.IsNotEmptyOrNull())) addItem.Add(item, new List<Dictionary<string, object>> { childAddItem });
                            });

                            if (addItem.Any(x => x.Value.IsNotEmptyOrNull())) newData.Add(addItem);
                        }
                    }
                });
            }
            else
            {
                foreach (var item in output.dataRow)
                {
                    if (item.Any(x => x.Value.IsNotEmptyOrNull())) newData.Add(item);
                }
            }

            output.dataRow = newData;
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D1412);
        }

        output.dataRow.ForEach(items =>
        {
            foreach (var item in items)
            {
                var vmodel = tInfo.AllFieldsModel.FirstOrDefault(x => x.__vModel__.Equals(item.Key));
                if (vmodel != null && vmodel.__config__.jnpfKey.Equals(JnpfKeyConst.DATE) && item.Value.IsNotEmptyOrNull())
                    items[item.Key] = string.Format("{0:" + vmodel.format + "} ", item.Value);
                else if (vmodel != null && vmodel.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && item.Value.IsNotEmptyOrNull())
                {
                    var ctList = item.Value.ToJsonString().ToObjectOld<List<Dictionary<string, object>>>();
                    ctList.ForEach(ctItems =>
                    {
                        foreach (var ctItem in ctItems)
                        {
                            var ctVmodel = tInfo.AllFieldsModel.FirstOrDefault(x => x.__vModel__.Equals(vmodel.__vModel__ + "-" + ctItem.Key));
                            if (ctVmodel != null && ctVmodel.__config__.jnpfKey.Equals(JnpfKeyConst.DATE) && ctItem.Value.IsNotEmptyOrNull())
                                ctItems[ctItem.Key] = string.Format("{0:" + vmodel.format + "} ", ctItem.Value);
                        }
                    });
                    items[item.Key] = ctList;
                }
            }
        });

        // 返回结果
        return output;
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="tInfo">模板信息.</param>
    /// <param name="input"></param>
    /// <returns>[成功列表,失败列表].</returns>
    private async Task<object[]> ImportMenuData(TemplateParsingBase tInfo, VisualDevImportDataInput input)
    {
        if (tInfo.ColumnData.complexHeaderList.Count > 0 && !tInfo.ColumnData.type.Equals(3) && !tInfo.ColumnData.type.Equals(5))
        {
            var complexHeaderIdList = tInfo.ColumnData.complexHeaderList.Select(it => it.id).ToList();
            foreach (var item in input.list)
            {
                var addValue = new Dictionary<string, object>();
                foreach (var subItem in item)
                {
                    if (complexHeaderIdList.Contains(subItem.Key))
                    {
                        foreach (var newItem in subItem.Value.ToObject<List<Dictionary<string, object>>>())
                        {
                            foreach (var dicItem in newItem)
                            {
                                addValue[dicItem.Key] = dicItem.Value;
                            }
                        }
                    }
                }

                if (addValue.Count > 0)
                {
                    foreach (var addItem in addValue)
                    {
                        item[addItem.Key] = addItem.Value;
                    }
                }
            }
        }

        var oldList = new List<Dictionary<string, object>>();
        foreach (var data in input.list) oldList.Add(data);
        var successList = new List<Dictionary<string, object>>();
        var errorsList = new List<Dictionary<string, object>>();

        // 捞取控件解析数据
        var cData = await GetCDataList(tInfo.AllFieldsModel, new Dictionary<string, List<Dictionary<string, string>>>());
        ImportFirstVerify(tInfo, input.list);
        var res = await ImportDataAssemble(tInfo, tInfo.AllFieldsModel, input.list, cData);
        await ImportAfterVerify(tInfo, res, input.flowId);

        // 处理错误信息
        var newRes = new List<Dictionary<string, object>>();
        for (int i = 0; i < res.Count; i++)
        {
            if (res[i].ContainsKey("errorsInfo"))
            {
                oldList[i]["errorsInfo"] = res[i]["errorsInfo"];
                newRes.Add(oldList[i]);
            }
            else if (res[i].Any(x => x.Key.Contains("|error")))
            {
                var errorInfo = new List<string>();
                for (int x = 0; x < tInfo.AllFieldsModel.Count; x++)
                {
                    var errorKey = string.Format("{0}|error", tInfo.AllFieldsModel[x].__vModel__);
                    if (res[i].ContainsKey(errorKey)) errorInfo.Add(res[i][errorKey].ToString());
                }

                if (res[i].ContainsKey("flow|error")) errorInfo.Add(res[i]["flow|error"].ToString());

                oldList[i]["errorsInfo"] = string.Join(",", errorInfo);
                newRes.Add(oldList[i]);
            }
            else
            {
                newRes.Add(res[i]);
            }
        }

        newRes.Where(x => x.ContainsKey("errorsInfo")).ToList().ForEach(item => errorsList.Add(item));
        newRes.Where(x => !x.ContainsKey("errorsInfo")).ToList().ForEach(item => successList.Add(item));

        // 唯一验证已处理，入库前去掉.
        tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique).ToList().ForEach(item => item.__config__.unique = false);

        var eventList = new List<object>();
        var model = new TaskFlowEventModel();
        model.TenantId = _userManager.TenantId;
        model.UserId = _userManager.UserId;
        model.ModelId = tInfo.visualDevEntity.Id;
        model.TriggerType = "eventTrigger";
        foreach (var item in successList)
        {
            var dataModel = item.Copy();
            if (item.ContainsKey("Update_MainTablePrimary_Id"))
            {
                string? mainId = item["Update_MainTablePrimary_Id"].ToString();
                var haveTableSql = await _runService.GetUpdateSqlByTemplate(tInfo, new VisualDevModelDataUpInput() { data = item.ToJsonString() }, mainId, true, null, null);
                foreach (var it in haveTableSql) await _databaseService.ExecuteSql(tInfo.DbLink, it); // 修改功能数据

                var eventData = item.Copy();
                eventData.Remove("Update_MainTablePrimary_Id");
                eventList.Add(new { id = mainId, data = eventData });
                model.ActionType = 2;
                dataModel["id"] = mainId;
            }
            else
            {
                string? mainId = SnowflakeIdHelper.NextId();
                var haveTableSql = await _runService.GetCreateSqlByTemplate(tInfo, new VisualDevModelDataCrInput() { data = item.ToJsonString() }, mainId, true, null);

                // 主表自增长Id.
                if (haveTableSql.ContainsKey("MainTableReturnIdentity")) haveTableSql.Remove("MainTableReturnIdentity");
                foreach (var it in haveTableSql)
                    await _databaseService.ExecuteSql(tInfo.DbLink, it.Key, it.Value); // 新增功能数据

                eventList.Add(new { id = mainId, data = item });
                model.ActionType = 1;
                dataModel["id"] = mainId;
            }
            model.taskFlowData = new List<Dictionary<string, object>>();
            model.taskFlowData.Add(dataModel);
            await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTask", model));
        }

        foreach (var item in errorsList)
        {
            if (item.ContainsKey("errorsInfo") && item["errorsInfo"].IsNotEmptyOrNull()) item["errorsInfo"] = item["errorsInfo"].ToString().TrimStart(',').TrimEnd(',');
        }
        return new object[] { successList, errorsList };
    }

    /// <summary>
    /// 导入功能数据初步验证.
    /// </summary>
    private void ImportFirstVerify(TemplateParsingBase tInfo, List<Dictionary<string, object>> list)
    {
        #region 验证必填

        var childTableList = tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).Select(x => x.__vModel__).ToList();
        var requiredList = tInfo.AllFieldsModel.Where(x => x.__config__.required).ToList();
        var rVModelList = requiredList.Select(x => x.__vModel__).ToList();
        if (rVModelList.Any())
        {
            foreach (var items in list)
            {
                var errorDic = new Dictionary<string, string>();
                foreach (var item in items)
                {
                    if (item.Value.IsNullOrEmpty() && rVModelList.Contains(item.Key))
                    {
                        var errorKey = string.Format("{0}|error", item.Key);
                        var errorInfo = requiredList.Find(x => x.__vModel__.Equals(item.Key)).__config__.label.Split("(").FirstOrDefault()?.TrimEnd(')') + "不能为空";
                        if (errorDic.ContainsKey(errorKey)) errorDic[errorKey] = errorDic[errorKey] + "," + errorInfo;
                        else errorDic.Add(errorKey, errorInfo);
                    }

                    // 子表
                    if (childTableList.Contains(item.Key))
                    {
                        foreach (var childItems in item.Value.ToObject<List<Dictionary<string, object>>>())
                        {
                            foreach (var childItem in childItems)
                            {
                                var childKey = item.Key + "-" + childItem.Key;
                                if (childItem.Value.IsNullOrEmpty() && rVModelList.Contains(childKey))
                                {
                                    var errorKey = string.Format("{0}|error", childKey);
                                    var errorInfo = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(item.Key)).__config__.children.Find(x => x.__vModel__.Equals(childKey)).__config__.label.Split("(").FirstOrDefault()?.TrimEnd(')') + "不能为空";
                                    if (errorDic.ContainsKey(errorKey)) errorDic[errorKey] = errorDic[errorKey] + "," + errorInfo;
                                    else errorDic.Add(errorKey, errorInfo);
                                }
                            }
                        }
                    }
                }

                foreach (var error in errorDic) items.Add(error.Key, error.Value);
            }
        }

        #endregion
    }

    /// <summary>
    /// 导入功能数据唯一和业务主键验证.
    /// </summary>
    private async Task ImportAfterVerify(TemplateParsingBase tInfo, List<Dictionary<string, object>> list, string flowId = "")
    {
        // 流程条件
        var versionIds = await _visualDevRepository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(it => it.DeleteMark == null && it.TemplateId == flowId).Select(it => it.Id).ToListAsync();
        var flowIds = string.Join(",", versionIds);

        #region 验证业务主键

        if (tInfo.FormModel.useBusinessKey)
        {
            var bkList = new List<string>();
            var link = await _runService.GetDbLink(tInfo.visualDevEntity.DbLinkId);
            var tableList = _databaseService.GetFieldList(link, tInfo.MainTableName); // 获取主表 表结构 信息
            var mainPrimary = tableList.Find(t => t.primaryKey && t.field.ToLower() != "f_tenant_id"); // 主表主键
            var dbType = link?.DbType != null ? link.DbType : _visualDevRepository.AsSugarClient().CurrentConnectionConfig.DbType.ToString();
            foreach (var items in list)
            {
                var bkDic = new Dictionary<string, object>();
                var fieldList = new List<string>();
                fieldList.Add(string.Format("{0}.{1}", tInfo.MainTableName, tInfo.MainPrimary));
                var whereList = new List<IConditionalModel>();
                foreach (var key in tInfo.FormModel.businessKeyList)
                {
                    var model = tInfo.SingleFormData.Find(x => x.__vModel__ == key);
                    var cSharpTypeName = string.Empty;

                    string? value = null;
                    if (items.ContainsKey(model.__vModel__) && items[model.__vModel__].IsNotEmptyOrNull())
                    {
                        var newValue = _formDataParsing.InsertValueHandle(dbType, tableList, model.__vModel__, items[model.__vModel__], tInfo.FieldsModelList);
                        if (newValue is DateTime)
                        {
                            value = string.Format("{0:yyyy-MM-dd HH:mm:ss}", newValue);
                            cSharpTypeName = "datetime";
                        }
                        else
                        {
                            value = newValue.ToString();
                        }
                    }

                    var type = items.ContainsKey(model.__vModel__) && items[model.__vModel__].IsNotEmptyOrNull() && !items[model.__vModel__].ToString().Equals("[]") ? ConditionalType.Equal : model.__config__.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) || model.__config__.jnpfKey.Equals(JnpfKeyConst.DATE) ? ConditionalType.EqualNull : ConditionalType.IsNullOrEmpty;

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

                    bkDic.Add(key, value);
                }

                // 验证excel内数据
                var bkJson = bkDic.ToJsonStringOld();
                if (bkList.Contains(bkJson))
                {
                    items["errorsInfo"] = tInfo.FormModel.businessKeyTip;
                    continue;
                }
                else
                {
                    bkList.Add(bkJson);
                }

                _sqlSugarClient = _databaseService.ChangeDataBase(tInfo.DbLink);
                var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(whereList).ToSqlString();
                _sqlSugarClient.AsTenant().ChangeDatabase("default");
                if (!itemWhere.Equals("@"))
                {
                    var relationKey = new List<string>();
                    var auxiliaryFieldList = tInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
                    foreach (var tName in auxiliaryFieldList)
                    {
                        var tableField = tInfo.AllTable.Find(tf => tf.table == tName)?.tableField;
                        relationKey.Add(tInfo.MainTableName + "." + tInfo.MainPrimary + "=" + tName + "." + tableField);
                    }

                    relationKey.Add("(" + itemWhere.Split("WHERE").Last() + ")");
                    var querStr = string.Format(
                        "select {0} from {1} where ({2}) ",
                        string.Join(",", fieldList),
                        auxiliaryFieldList.Any() ? tInfo.MainTableName + "," + string.Join(",", auxiliaryFieldList) : tInfo.MainTableName,
                        string.Join(" and ", relationKey)); // 多表， 联合查询
                    if (tInfo.FormModel.logicalDelete) querStr = string.Format(" {0} and {1}.{2} ", querStr, tInfo.MainTableName, "f_delete_mark is null");

                    if (items.ContainsKey("flowId") && items["flowId"].IsNotEmptyOrNull())
                        querStr = string.Format(" {0} and {1}.f_flow_id in ('{2}') ", querStr, tInfo.MainTableName, string.Join("','", versionIds));
                    else
                        querStr = string.Format(" {0} and {1}.f_flow_id is null", querStr, tInfo.MainTableName);

                    var res = _databaseService.GetSqlData(link, querStr).ToObject<List<Dictionary<string, string>>>();
                    if (res.Count > 0)
                    {
                        if (tInfo.dataType.Equals("1"))
                        {
                            items["errorsInfo"] = tInfo.FormModel.businessKeyTip;
                        }
                        else
                        {
                            var mainId = res[0][tInfo.MainPrimary];
                            items["Update_MainTablePrimary_Id"] = mainId;

                            if (items.ContainsKey("flowId"))
                            {
                                var taskFlowStatus = _visualDevRepository.AsSugarClient().Queryable<WorkFlowTaskEntity>().Where(it => it.Id.Equals(mainId)).Select(it => it.Status).First();
                                if (taskFlowStatus.IsNotEmptyOrNull() && !taskFlowStatus.Equals(0))
                                    items.Add("flow|error", "已发起流程，导入失败");
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region 验证唯一

        var vdic = new Dictionary<string, int>();
        var childTableList = tInfo.AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).Select(x => x.__vModel__).ToList();
        var uniqueList = tInfo.AllFieldsModel.Where(x => x.__config__.unique).ToList();
        var VModelList = uniqueList.Select(x => x.__vModel__).ToList();
        foreach (var items in list)
        {
            var newItems = items.Copy();
            foreach (var item in newItems)
            {
                // 子表
                var cdic = new Dictionary<string, int>();
                if (childTableList.Contains(item.Key))
                {
                    foreach (var childItems in item.Value.ToObject<List<Dictionary<string, object>>>())
                    {
                        foreach (var childItem in childItems)
                        {
                            var uniqueKey = item.Key + "-" + childItem.Key;
                            if (VModelList.Contains(uniqueKey) && childItem.Value != null)
                            {
                                var cformat = string.Format("{0}:{1}", uniqueKey, childItem.Value);
                                cdic[cformat] = cdic.ContainsKey(cformat) ? cdic[cformat] + 1 : 1;
                                if (cdic.ContainsKey(cformat) && cdic[cformat] > 1)
                                {
                                    var errorKey = string.Format("{0}|error", uniqueKey);
                                    var errorInfo = tInfo.AllFieldsModel.Find(x => x.__vModel__.Equals(uniqueKey)).__config__.label.Split("(").FirstOrDefault()?.TrimEnd(')') + "值已存在";
                                    if (items.ContainsKey(errorKey)) items[errorKey] = items[errorKey] + "," + errorInfo;
                                    else items.Add(errorKey, errorInfo);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var item in tInfo.SingleFormData.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) && x.__config__.unique))
            {
                var format = string.Format("{0}:{1}", item.__vModel__, items[item.__vModel__]);
                vdic[format] = vdic.ContainsKey(format) ? vdic[format] + 1 : 1;
                if (vdic.ContainsKey(format) && vdic[format] > 1)
                {
                    var errorKey = string.Format("{0}|error", item.__vModel__);
                    var errorInfo = item.__config__.label.Split("(").FirstOrDefault()?.TrimEnd(')') + "值已存在";
                    if (items.ContainsKey(errorKey)) items[errorKey] = items[errorKey] + "," + errorInfo;
                    else items.Add(errorKey, errorInfo);

                    continue;
                }

                var fieldList = new List<string>();
                fieldList.Add(string.Format("{0}.{1}", tInfo.MainTableName, tInfo.MainPrimary));
                fieldList.Add(string.Format("{0}.{1} {2}", item.__config__.tableName, item.__vModel__.Split("_jnpf_").Last(), item.__vModel__));

                var whereList = new List<IConditionalModel>
                {
                    new ConditionalModel
                    {
                        FieldName = string.Format("{0}.{1}", item.__config__.tableName, item.__vModel__.Split("_jnpf_").Last()),
                        ConditionalType = ConditionalType.Equal,
                        FieldValue = items[item.__vModel__]?.ToString()
                    }
                };

                // 流程条件
                if (flowId.IsNotEmptyOrNull())
                {
                    whereList.Add(new ConditionalModel
                    {
                        FieldName = string.Format("{0}.{1}", tInfo.MainTableName, "f_flow_id"),
                        ConditionalType = ConditionalType.In,
                        FieldValue = flowIds
                    });
                }
                else
                {
                    whereList.Add(new ConditionalModel
                    {
                        FieldName = string.Format("{0}.{1}", tInfo.MainTableName, "f_flow_id"),
                        ConditionalType = ConditionalType.EqualNull,
                        FieldValue = null
                    });
                }

                // 逻辑删除
                if (tInfo.FormModel.logicalDelete)
                {
                    whereList.Add(new ConditionalModel
                    {
                        FieldName = string.Format("{0}.{1}", tInfo.MainTableName, "f_delete_mark"),
                        ConditionalType = ConditionalType.EqualNull,
                        FieldValue = null
                    });
                }

                _sqlSugarClient = _databaseService.ChangeDataBase(tInfo.DbLink);
                var itemWhere = _sqlSugarClient.SqlQueryable<object>("@").Where(whereList).ToSqlString();
                _sqlSugarClient.AsTenant().ChangeDatabase("default");
                if (!itemWhere.Equals("@"))
                {
                    var relationKey = new List<string>();
                    var auxiliaryFieldList = tInfo.AuxiliaryTableFieldsModelList.Select(x => x.__config__.tableName).Distinct().ToList();
                    foreach (var tName in auxiliaryFieldList)
                    {
                        var tableField = tInfo.AllTable.Find(tf => tf.table == tName)?.tableField;
                        relationKey.Add(tInfo.MainTableName + "." + tInfo.MainPrimary + "=" + tName + "." + tableField);
                    }

                    var relationList = new List<string>();
                    relationList.AddRange(relationKey);
                    var whereStr = string.Empty;
                    if (relationList.Count > 0)
                    {
                        var whereRelation = string.Join(" and ", relationList);
                        whereStr = string.Format("({0}) and {1}", whereRelation, itemWhere.Split("WHERE").Last());
                    }
                    else
                    {
                        whereStr = itemWhere.Split("WHERE").Last();
                    }

                    var querStr = string.Format(
                        "select {0} from {1} where {2}",
                        string.Join(",", fieldList),
                        auxiliaryFieldList.Any() ? tInfo.MainTableName + "," + string.Join(",", auxiliaryFieldList) : tInfo.MainTableName,
                        whereStr); // 多表， 联合查询
                    var res = _databaseService.GetSqlData(tInfo.DbLink, querStr).ToObject<List<Dictionary<string, string>>>();

                    if (res.Count > 0)
                    {
                        var errorKey = string.Format("{0}|error", item.__vModel__);
                        var errorInfo = item.__config__.label.Split("(").FirstOrDefault()?.TrimEnd(')') + "值已存在";
                        if (items.ContainsKey("Update_MainTablePrimary_Id") && !items.Any(x => x.Key.Contains("|error")))
                        {
                            var id = res[0][tInfo.MainPrimary];
                            if (!id.Equals(items["Update_MainTablePrimary_Id"].ToString()))
                            {
                                if (items.ContainsKey(errorKey)) items[errorKey] = items[errorKey] + "," + errorInfo;
                                else items.Add(errorKey, errorInfo);
                            }
                        }
                        else
                        {
                            if (items.ContainsKey(errorKey)) items[errorKey] = items[errorKey] + "," + errorInfo;
                            else items.Add(errorKey, errorInfo);
                        }
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 获取模板控件解析数据.
    /// </summary>
    /// <param name="tInfo"></param>
    /// <param name="resData"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, List<Dictionary<string, string>>>> GetCDataList(List<FieldsModel> listFieldsModel, Dictionary<string, List<Dictionary<string, string>>> resData)
    {
        foreach (var item in listFieldsModel.Where(x => !x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).ToList())
        {
            var addItem = new List<Dictionary<string, string>>();
            switch (item.__config__.jnpfKey)
            {
                case JnpfKeyConst.COMSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var allDataList = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1)
                                .Select(x => new OrganizeEntity { Id = x.Id, ParentId = x.ParentId, OrganizeIdTree = x.OrganizeIdTree, OrganizeNameTree = x.OrganizeNameTree }).ToListAsync();
                            var dataList = new List<OrganizeEntity>();
                            if (item.selectType.Equals("custom"))
                            {
                                foreach (var orgId in item.ableIds)
                                {
                                    var oid = orgId.ToString().Split("--").First();
                                    if (oid.Equals("@organizeId"))
                                        dataList.AddRange(allDataList.FindAll(x => _userManager.CurrentOrganizeList.Contains(x.Id)));
                                    else if (oid.Equals("@organizeAndSub"))
                                        dataList.AddRange(allDataList.FindAll(x => _userManager.CurrentOrganizationAndSubOrganizations.Contains(x.Id)));
                                    else if (oid.Equals("@organizeAndProgeny"))
                                        dataList.AddRange(allDataList.FindAll(x => _userManager.CurrentOrganizationTree.Contains(x.Id)));
                                    else if (orgId.ToString().Contains("--org"))
                                        dataList.Add(allDataList.Find(x => x.Id.Equals(oid)));
                                    else if (orgId.ToString().Contains("--subOrg"))
                                        dataList.AddRange(allDataList.FindAll(x => x.Id.Equals(oid) || (x.ParentId.IsNotEmptyOrNull() && x.ParentId.Equals(oid))));
                                    else if (orgId.ToString().Contains("--progenyOrg"))
                                        dataList.AddRange(allDataList.FindAll(x => x.OrganizeIdTree.Contains(oid)));
                                    else
                                        dataList.Add(allDataList.Find(x => x.Id.Equals(orgId.ToString())));
                                }
                            }
                            else
                            {
                                dataList = allDataList;
                            }

                            dataList.ForEach(item =>
                            {
                                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                dictionary.Add(item.Id, item.OrganizeNameTree);
                                addItem.Add(dictionary);
                            });

                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.ADDRESS:
                    {
                        string? addCacheKey = "Import_Address";

                        if (!resData.ContainsKey(JnpfKeyConst.ADDRESS))
                        {
                            if (_cacheManager.Exists(addCacheKey))
                            {
                                addItem = _cacheManager.Get(addCacheKey).ToObject<List<Dictionary<string, string>>>();
                                resData.Add(JnpfKeyConst.ADDRESS, addItem);
                            }
                            else
                            {
                                var dataList = await _visualDevRepository.AsSugarClient().Queryable<ProvinceEntity>().Select(x => new ProvinceEntity { Id = x.Id, ParentId = x.ParentId, Type = x.Type, FullName = x.FullName }).ToListAsync();

                                // 处理省市区树
                                dataList.Where(x => x.Type == "1").ToList().ForEach(item =>
                                {
                                    item.QuickQuery = item.FullName;
                                    item.Description = item.Id;
                                    Dictionary<string, string> address = new Dictionary<string, string>();
                                    address.Add(item.Description, item.QuickQuery);
                                    addItem.Add(address);
                                });
                                dataList.Where(x => x.Type == "2").ToList().ForEach(item =>
                                {
                                    item.QuickQuery = dataList.Find(x => x.Id == item.ParentId).QuickQuery + "/" + item.FullName;
                                    item.Description = dataList.Find(x => x.Id == item.ParentId).Description + "," + item.Id;
                                    Dictionary<string, string> address = new Dictionary<string, string>();
                                    address.Add(item.Description, item.QuickQuery);
                                    addItem.Add(address);
                                });
                                dataList.Where(x => x.Type == "3").ToList().ForEach(item =>
                                {
                                    item.QuickQuery = dataList.Find(x => x.Id == item.ParentId).QuickQuery + "/" + item.FullName;
                                    item.Description = dataList.Find(x => x.Id == item.ParentId).Description + "," + item.Id;
                                    Dictionary<string, string> address = new Dictionary<string, string>();
                                    address.Add(item.Description, item.QuickQuery);
                                    addItem.Add(address);
                                });
                                dataList.Where(x => x.Type == "4").ToList().ForEach(item =>
                                {
                                    ProvinceEntity? it = dataList.Find(x => x.Id == item.ParentId);
                                    if (it != null)
                                    {
                                        item.QuickQuery = it.QuickQuery + "/" + item.FullName;
                                        item.Description = it.Description + "," + item.Id;
                                        Dictionary<string, string> address = new Dictionary<string, string>();
                                        address.Add(item.Description, item.QuickQuery);
                                        addItem.Add(address);
                                    }
                                });
                                dataList.ForEach(it =>
                                {
                                    if (it.Description.IsNotEmptyOrNull())
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(it.Description, it.QuickQuery);
                                        addItem.Add(dictionary);
                                    }
                                });

                                var noTypeList = dataList.Where(x => x.Type.IsNullOrWhiteSpace()).ToList();
                                foreach (var it in noTypeList)
                                {
                                    it.QuickQuery = GetAddressByPList(noTypeList, it);
                                    it.Description = GetAddressIdByPList(noTypeList, it);
                                }
                                foreach (var it in noTypeList)
                                {
                                    Dictionary<string, string> address = new Dictionary<string, string>();
                                    address.Add(it.Description, it.QuickQuery);
                                    addItem.Add(address);
                                }

                                _cacheManager.Set(addCacheKey, addItem, TimeSpan.FromDays(7)); // 缓存七天
                                resData.Add(JnpfKeyConst.ADDRESS, addItem);
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.GROUPSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var dataList = await _visualDevRepository.AsSugarClient().Queryable<GroupEntity>().Where(x => x.DeleteMark == null).Select(x => new GroupEntity() { Id = x.Id, FullName = x.FullName, EnCode = x.EnCode }).ToListAsync();
                            if (item.selectType.Equals("custom")) dataList = dataList.Where(it => item.ableIds.Contains(it.Id)).ToList();
                            dataList.ForEach(item =>
                            {
                                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                dictionary.Add(item.Id, item.FullName + "/" + item.EnCode);
                                addItem.Add(dictionary);
                            });
                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.ROLESELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var dataList = await _visualDevRepository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.DeleteMark == null && x.Type.Equals(CommonConst.RelationObjectUser)).Select(x => new RoleEntity() { Id = x.Id, FullName = x.FullName, EnCode = x.EnCode }).ToListAsync();
                            if (item.selectType.Equals("custom")) dataList = dataList.Where(it => item.ableIds.Contains(it.Id)).ToList();
                            dataList.ForEach(item =>
                            {
                                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                dictionary.Add(item.Id, item.FullName + "/" + item.EnCode);
                                addItem.Add(dictionary);
                            });
                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.SWITCH:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            Dictionary<string, string> dictionary = new Dictionary<string, string>();
                            dictionary.Add("1", item.activeTxt);
                            addItem.Add(dictionary);
                            Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                            dictionary2.Add("0", item.inactiveTxt);
                            addItem.Add(dictionary2);
                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.CHECKBOX:
                case JnpfKeyConst.SELECT:
                case JnpfKeyConst.RADIO:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var propsValue = string.Empty;
                            var propsLabel = string.Empty;
                            var children = string.Empty;
                            if (item.props != null)
                            {
                                propsValue = item.props.value;
                                propsLabel = item.props.label;
                                children = item.props.children;
                            }

                            if (item.__config__.dataType.Equals("static"))
                            {
                                if (item != null && item.options != null)
                                {
                                    item.options.ForEach(option =>
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(option[propsValue].ToString(), option[propsLabel].ToString());
                                        addItem.Add(dictionary);
                                    });
                                    resData.Add(item.__vModel__, addItem);
                                }
                            }
                            else if (item.__config__.dataType.Equals("dictionary"))
                            {
                                var dictionaryDataList = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity, DictionaryTypeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.DictionaryTypeId))
                                    .WhereIF(item.__config__.dictionaryType.IsNotEmptyOrNull(), (a, b) => b.Id == item.__config__.dictionaryType || b.EnCode == item.__config__.dictionaryType)
                                    .Where(a => a.DeleteMark == null).Select(a => new { a.Id, a.EnCode, a.FullName }).ToListAsync();

                                foreach (var it in dictionaryDataList)
                                {
                                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                    if (propsValue.Equals("id")) dictionary.Add(it.Id, it.FullName);
                                    if (propsValue.Equals("enCode")) dictionary.Add(it.EnCode, it.FullName);
                                    addItem.Add(dictionary);
                                }

                                resData.Add(item.__vModel__, addItem);
                            }
                            else if (item.__config__.dataType.Equals("dynamic"))
                            {
                                var popDataList = await _formDataParsing.GetDynamicList(item);
                                resData.Add(item.__vModel__, popDataList);
                            }
                        }
                    }
                    break;
                case JnpfKeyConst.TREESELECT:
                case JnpfKeyConst.CASCADER:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            if (item.__config__.dataType.Equals("static"))
                            {
                                if (item.options != null)
                                    resData.Add(item.__vModel__, GetStaticList(item));
                            }
                            else if (item.__config__.dataType.Equals("dictionary"))
                            {
                                var dictionaryDataList = await _visualDevRepository.AsSugarClient().Queryable<DictionaryDataEntity, DictionaryTypeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.DictionaryTypeId))
                                    .WhereIF(item.__config__.dictionaryType.IsNotEmptyOrNull(), (a, b) => b.Id == item.__config__.dictionaryType || b.EnCode == item.__config__.dictionaryType)
                                    .Where(a => a.DeleteMark == null).Select(a => new { a.Id, a.EnCode, a.FullName }).ToListAsync();
                                if (item.props.value.ToLower().Equals("encode"))
                                {
                                    foreach (var it in dictionaryDataList)
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(it.EnCode, it.FullName);
                                        addItem.Add(dictionary);
                                    }
                                }
                                else
                                {
                                    foreach (var it in dictionaryDataList)
                                    {
                                        Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                        dictionary.Add(it.Id, it.FullName);
                                        addItem.Add(dictionary);
                                    }
                                }

                                resData.Add(item.__vModel__, addItem);
                            }
                            else if (item.__config__.dataType.Equals("dynamic"))
                            {
                                var popDataList = await _formDataParsing.GetDynamicList(item);
                                resData.Add(item.__vModel__, popDataList);
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.POPUPTABLESELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var popDataList = await _formDataParsing.GetDynamicList(item);
                            resData.Add(item.__vModel__, popDataList);
                        }
                    }
                    break;

                case JnpfKeyConst.USERSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var dataList = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).Select(x => new UserEntity() { Id = x.Id, RealName = x.RealName, Account = x.Account }).ToListAsync();
                            var userList = new List<UserEntity>();
                            if (item.selectType.Equals("custom"))
                            {
                                foreach (var userId in item.ableIds)
                                {
                                    var oid = userId.ToString().Split("--").First();
                                    if (oid.Equals("@userId"))
                                    {
                                        userList.Add(dataList.Find(x => x.Id.Equals(_userManager.UserId)));
                                    }
                                    else if (userId.ToString().Contains("--user"))
                                    {
                                        userList.Add(dataList.Find(x => x.Id.Equals(oid)));
                                    }
                                    else if (userId.ToString().Contains("--org"))
                                    {
                                        var ids = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(oid)).Select(x => x.UserId).ToListAsync();
                                        userList.AddRange(dataList.FindAll(x => ids.Contains(x.Id)));
                                    }
                                    else if (userId.ToString().Contains("--subOrg"))
                                    {
                                        var oids = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(xx => xx.ParentId.Equals(oid) || xx.Id.Equals(oid)).Select(xx => xx.Id).ToListAsync();
                                        var ids = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => oids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                                        userList.AddRange(dataList.FindAll(x => ids.Contains(x.Id)));
                                    }
                                    else if (userId.ToString().Contains("--progenyOrg"))
                                    {
                                        var oids = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(xx => xx.OrganizeIdTree.Contains(oid)).Select(xx => xx.Id).ToListAsync();
                                        var ids = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => oids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                                        userList.AddRange(dataList.FindAll(x => ids.Contains(x.Id)));
                                    }
                                    else if (userId.ToString().Contains("--pos"))
                                    {
                                        var ids = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(oid)).Select(x => x.UserId).ToListAsync();
                                        userList.AddRange(dataList.FindAll(x => ids.Contains(x.Id)));
                                    }
                                    else if (userId.ToString().Contains("--subPos"))
                                    {
                                        var pids = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(xx => xx.ParentId.Equals(oid)).Select(xx => xx.Id).ToListAsync();
                                        var ids = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => pids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                                        userList.AddRange(dataList.FindAll(x => ids.Contains(x.Id)));
                                    }
                                    else if (userId.ToString().Contains("--progenyPos"))
                                    {
                                        var pids = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(xx => xx.PositionIdTree.Contains(oid)).Select(xx => xx.Id).ToListAsync();
                                        var ids = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => pids.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();
                                        userList.AddRange(dataList.FindAll(x => ids.Contains(x.Id)));
                                    }
                                    else if (userId.ToString().Contains("--role"))
                                    {
                                        var ids = await _visualDevRepository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => oid.Equals(x.RoleId)).Select(x => x.ObjectId).ToListAsync();
                                        userList.AddRange(dataList.FindAll(x => ids.Contains(x.Id)));
                                    }
                                    else if (userId.ToString().Contains("--group"))
                                    {
                                        var ids = await _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(oid)).Select(x => x.UserId).ToListAsync();
                                        userList.AddRange(dataList.FindAll(x => ids.Contains(x.Id)));
                                    }
                                    else
                                    {
                                        userList.Add(dataList.Find(x => x.Id.Equals(userId.ToString())));
                                    }
                                }
                            }
                            else
                            {
                                userList = dataList;
                            }

                            foreach (var user in userList)
                            {
                                var dictionary = new Dictionary<string, string>();
                                dictionary.Add(user.Id, user.RealName + "/" + user.Account);
                                if (!addItem.Any(x => x.ContainsKey(user.Id))) addItem.Add(dictionary);
                            }

                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
                case JnpfKeyConst.POSSELECT:
                    {
                        if (!resData.ContainsKey(item.__vModel__))
                        {
                            var allorgList = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1)
                            .Select(x => new OrganizeEntity { Id = x.Id, OrganizeNameTree = x.OrganizeNameTree }).ToListAsync();
                            var dataList = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.DeleteMark == null)
                                .Select(x => new PositionEntity() { Id = x.Id, FullName = x.FullName, OrganizeId = x.OrganizeId, ParentId = x.ParentId, PositionIdTree = x.PositionIdTree }).ToListAsync();
                            var posList = new List<PositionEntity>();
                            if (item.selectType.Equals("custom"))
                            {
                                foreach (var posId in item.ableIds)
                                {
                                    var pid = posId.ToString().Split("--").First();
                                    if (pid.Equals("@positionId"))
                                        posList.AddRange(dataList.FindAll(x => _userManager.CurrentPositionList.Contains(x.Id)));
                                    else if (pid.Equals("@positionAndSub"))
                                        posList.AddRange(dataList.FindAll(x => _userManager.CurrentPositionAndSuborganization.Contains(x.Id)));
                                    else if (pid.Equals("@positionAndProgeny"))
                                        posList.AddRange(dataList.FindAll(x => _userManager.CurrentPositionTree.Contains(x.Id)));
                                    else if (posId.ToString().Contains("--pos"))
                                        posList.Add(dataList.Find(x => x.Id.Equals(pid)));
                                    else if (posId.ToString().Contains("--subPos"))
                                        posList.AddRange(dataList.FindAll(x => x.Id.Equals(pid) || (x.ParentId.IsNotEmptyOrNull() && x.ParentId.Equals(pid))));
                                    else if (posId.ToString().Contains("--progenyPos"))
                                        posList.AddRange(dataList.FindAll(x => x.PositionIdTree.Contains(pid)));
                                    else
                                        posList.Add(dataList.Find(x => x.Id.Equals(posId.ToString())));
                                }
                            }
                            else
                            {
                                posList = dataList;
                            }

                            foreach (var pos in posList)
                            {
                                var dictionary = new Dictionary<string, string>();
                                dictionary.Add(pos.Id, allorgList.Find(x => x.Id.Equals(pos.OrganizeId))?.OrganizeNameTree + "/" + pos.FullName);
                                addItem.Add(dictionary);
                            }

                            resData.Add(item.__vModel__, addItem);
                        }
                    }

                    break;
            }
        }

        listFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).ToList().ForEach(async item =>
        {
            var res = await GetCDataList(item.__config__.children, resData);
            if (res.Any()) foreach (var it in res) if (!resData.ContainsKey(it.Key)) resData.Add(it.Key, it.Value);
        });

        return resData;
    }

    /// <summary>
    /// 导入数据组装.
    /// </summary>
    /// <param name="tInfo">模板配置.</param>
    /// <param name="fieldsModelList">控件列表.</param>
    /// <param name="dataList">导入数据列表.</param>
    /// <param name="cDataList">控件解析缓存数据.</param>
    /// <returns></returns>
    private async Task<List<Dictionary<string, object>>> ImportDataAssemble(TemplateParsingBase tInfo, List<FieldsModel> fieldsModelList, List<Dictionary<string, object>> dataList, Dictionary<string, List<Dictionary<string, string>>> cDataList)
    {
        var allorg = new List<OrganizeEntity>();
        if (fieldsModelList.Any(it => (it.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT) || it.__config__.jnpfKey.Equals(JnpfKeyConst.POSSELECT)) && it.selectType.Equals("custom")))
            allorg = await _visualDevRepository.AsSugarClient().Queryable<OrganizeEntity>().Where(it => it.DeleteMark == null).Select(it => new OrganizeEntity { Id = it.Id, OrganizeNameTree = it.OrganizeNameTree }).ToListAsync();
        var allpos = new List<string>();
        if (fieldsModelList.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.POSSELECT) && it.selectType.Equals("custom")))
        {
            var posList = await _visualDevRepository.AsSugarClient().Queryable<PositionEntity>().Where(it => it.DeleteMark == null).Select(it => new PositionEntity { FullName = it.FullName, OrganizeId = it.OrganizeId }).ToListAsync();
            foreach (var pos in posList) allpos.Add(allorg.Find(x => x.Id.Equals(pos.OrganizeId))?.OrganizeNameTree + "/" + pos.FullName);
        }
        var allrole = new List<string>();
        if (fieldsModelList.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.ROLESELECT) && it.selectType.Equals("custom")))
        {
            var roleList = await _visualDevRepository.AsSugarClient().Queryable<RoleEntity>().Where(it => it.DeleteMark == null && it.Type.Equals(CommonConst.RelationObjectUser)).Select(it => new RoleEntity { FullName = it.FullName, EnCode = it.EnCode }).ToListAsync();
            foreach (var role in roleList) allrole.Add(role.FullName + "/" + role.EnCode);
        }
        var allgroup = new List<string>();
        if (fieldsModelList.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.GROUPSELECT) && it.selectType.Equals("custom")))
        {
            var groupList = await _visualDevRepository.AsSugarClient().Queryable<GroupEntity>().Where(it => it.DeleteMark == null).Select(it => new GroupEntity { FullName = it.FullName, EnCode = it.EnCode }).ToListAsync();
            foreach (var group in groupList) allgroup.Add(group.FullName + "/" + group.EnCode);
        }
        var alluser = new List<string>();
        if (fieldsModelList.Any(it => (it.__config__.jnpfKey.Equals(JnpfKeyConst.USERSELECT) || it.__config__.jnpfKey.Equals(JnpfKeyConst.USERSSELECT)) && it.selectType.Equals("custom")))
        {
            var userList = await _visualDevRepository.AsSugarClient().Queryable<UserEntity>().Where(it => it.DeleteMark == null).Select(it => new UserEntity { RealName = it.RealName, Account = it.Account }).ToListAsync();
            foreach (var user in userList) alluser.Add(user.RealName + "/" + user.Account);
        }

        var resList = new List<Dictionary<string, object>>();
        foreach (var dataItems in dataList)
        {
            var newDataItems = dataItems.Copy();
            foreach (var item in dataItems)
            {
                var vModel = fieldsModelList.Find(x => x.__vModel__.Equals(item.Key));
                if (vModel == null) continue;
                var dicList = new List<Dictionary<string, string>>();
                if (cDataList.ContainsKey(vModel.__config__.jnpfKey)) dicList = cDataList[vModel.__config__.jnpfKey];
                if ((dicList == null || !dicList.Any()) && cDataList.ContainsKey(vModel.__vModel__)) dicList = cDataList[vModel.__vModel__];

                // 全部数据(组织、部门、岗位、角色、分组、用户)
                var allData = new List<string>();
                if (vModel.selectType.IsNotEmptyOrNull() && vModel.selectType.Equals("custom"))
                {
                    switch (vModel.__config__.jnpfKey)
                    {
                        case JnpfKeyConst.COMSELECT:
                            allData = allorg.Select(x => x.OrganizeNameTree).ToList();
                            break;
                        case JnpfKeyConst.POSSELECT:
                            allData = allpos;
                            break;
                        case JnpfKeyConst.GROUPSELECT:
                            allData = allgroup;
                            break;
                        case JnpfKeyConst.ROLESELECT:
                            allData = allrole;
                            break;
                        case JnpfKeyConst.USERSELECT:
                        case JnpfKeyConst.USERSSELECT:
                            allData = alluser;
                            break;
                    }
                }

                var errorKey = string.Format("{0}|error", item.Key);
                var fieldName = vModel.__config__.label.Split("(").FirstOrDefault()?.TrimEnd(')');
                switch (vModel.__config__.jnpfKey)
                {
                    case JnpfKeyConst.COMINPUT:
                    case JnpfKeyConst.TEXTAREA:
                        if (item.Value.IsNotEmptyOrNull())
                        {
                            if (item.Value.ToString().Length > vModel.maxlength)
                            {
                                var errorInfo = fieldName + "值超出最多输入字符限制";
                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                else newDataItems.Add(errorKey, errorInfo);
                            }

                            if (vModel.__config__.regList.IsNotEmptyOrNull())
                            {
                                foreach (var reg in vModel.__config__.regList)
                                {
                                    var pattern = reg.pattern.TrimStart('/').TrimEnd('/');
                                    if (!Regex.IsMatch(item.Value.ToString(), pattern))
                                    {
                                        var errorInfo = fieldName + reg.message;
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                        break;
                                    }
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.DATE:
                        try
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                // 判断格式是否正确
                                var value = DateTime.ParseExact(item.Value.ToString().TrimEnd(), vModel.format, System.Globalization.CultureInfo.CurrentCulture);

                                if (vModel.__config__.startTimeRule)
                                {
                                    var minDate = string.Format("{0:" + vModel.format + "}", DateTime.Now).ParseToDateTime();
                                    switch (vModel.__config__.startTimeType)
                                    {
                                        case 1:
                                            {
                                                if (vModel.__config__.startTimeValue.IsNotEmptyOrNull())
                                                    minDate = vModel.__config__.startTimeValue.TimeStampToDateTime();
                                            }

                                            break;
                                        case 2:
                                            {
                                                if (vModel.__config__.startRelationField.IsNotEmptyOrNull() && dataItems.ContainsKey(vModel.__config__.startRelationField))
                                                {
                                                    if (dataItems[vModel.__config__.startRelationField] == null)
                                                    {
                                                        minDate = DateTime.MinValue;
                                                    }
                                                    else
                                                    {
                                                        var data = dataItems[vModel.__config__.startRelationField].ToString();
                                                        minDate = data.TrimEnd().ParseToDateTime();
                                                    }
                                                }
                                            }

                                            break;
                                        case 3:
                                            break;
                                        case 4:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        minDate = minDate.AddYears(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        minDate = minDate.AddMonths(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        minDate = minDate.AddDays(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                        case 5:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        minDate = minDate.AddYears(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        minDate = minDate.AddMonths(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        minDate = minDate.AddDays(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                    }

                                    if (minDate > value && !minDate.Equals(DateTime.MinValue))
                                    {
                                        var errorInfo = fieldName + "值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }

                                if (vModel.__config__.endTimeRule)
                                {
                                    var maxDate = string.Format("{0:" + vModel.format + "}", DateTime.Now).ParseToDateTime();
                                    switch (vModel.__config__.endTimeType)
                                    {
                                        case 1:
                                            {
                                                if (vModel.__config__.endTimeValue.IsNotEmptyOrNull())
                                                    maxDate = vModel.__config__.endTimeValue.TimeStampToDateTime();
                                            }

                                            break;
                                        case 2:
                                            {
                                                if (vModel.__config__.endRelationField.IsNotEmptyOrNull() && dataItems.ContainsKey(vModel.__config__.endRelationField))
                                                {
                                                    if (dataItems[vModel.__config__.endRelationField] == null)
                                                    {
                                                        maxDate = DateTime.MinValue;
                                                    }
                                                    else
                                                    {
                                                        var data = dataItems[vModel.__config__.endRelationField].ToString();
                                                        maxDate = data.TrimEnd().ParseToDateTime();
                                                    }
                                                }
                                            }

                                            break;
                                        case 3:
                                            break;
                                        case 4:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        maxDate = maxDate.AddYears(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        maxDate = maxDate.AddMonths(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        maxDate = maxDate.AddDays(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                        case 5:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        maxDate = maxDate.AddYears(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        maxDate = maxDate.AddMonths(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        maxDate = maxDate.AddDays(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                    }

                                    if (maxDate < value && !maxDate.Equals(DateTime.MinValue))
                                    {
                                        var errorInfo = fieldName + "值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }

                                newDataItems[item.Key] = value.ParseToUnixTime();
                            }
                        }
                        catch
                        {
                            var errorInfo = fieldName + "值不正确";
                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                            else newDataItems.Add(errorKey, errorInfo);
                        }

                        break;
                    case JnpfKeyConst.TIME: // 时间选择
                        try
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                var value = DateTime.ParseExact(item.Value.ToString().TrimEnd(), vModel.format, System.Globalization.CultureInfo.CurrentCulture);

                                if (vModel.__config__.startTimeRule)
                                {
                                    var minTime = DateTime.Now;
                                    switch (vModel.__config__.startTimeType)
                                    {
                                        case 1:
                                            {
                                                if (vModel.__config__.startTimeValue.IsNotEmptyOrNull())
                                                    minTime = DateTime.Parse(vModel.__config__.startTimeValue);
                                            }

                                            break;
                                        case 2:
                                            {
                                                if (vModel.__config__.startRelationField.IsNotEmptyOrNull() && dataItems.ContainsKey(vModel.__config__.startRelationField))
                                                {
                                                    if (dataItems[vModel.__config__.startRelationField] == null)
                                                    {
                                                        minTime = DateTime.MinValue;
                                                    }
                                                    else
                                                    {
                                                        minTime = dataItems[vModel.__config__.startRelationField].ToString().ParseToDateTime();
                                                    }
                                                }
                                            }

                                            break;
                                        case 3:
                                            break;
                                        case 4:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        minTime = minTime.AddHours(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        minTime = minTime.AddMinutes(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        minTime = minTime.AddSeconds(-vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                        case 5:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        minTime = minTime.AddHours(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        minTime = minTime.AddMinutes(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        minTime = minTime.AddSeconds(vModel.__config__.startTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                    }

                                    if (minTime > value && !minTime.Equals(DateTime.MinValue))
                                    {
                                        var errorInfo = fieldName + "值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }

                                if (vModel.__config__.endTimeRule)
                                {
                                    var maxTime = DateTime.Now;
                                    switch (vModel.__config__.endTimeType)
                                    {
                                        case 1:
                                            {
                                                if (vModel.__config__.endTimeValue.IsNotEmptyOrNull())
                                                    maxTime = DateTime.Parse(vModel.__config__.endTimeValue);
                                            }

                                            break;
                                        case 2:
                                            {
                                                if (vModel.__config__.endRelationField.IsNotEmptyOrNull() && dataItems.ContainsKey(vModel.__config__.endRelationField))
                                                {
                                                    if (dataItems[vModel.__config__.endRelationField] == null)
                                                    {
                                                        maxTime = DateTime.MinValue;
                                                    }
                                                    else
                                                    {
                                                        maxTime = dataItems[vModel.__config__.endRelationField].ToString().ParseToDateTime();
                                                    }
                                                }
                                            }

                                            break;
                                        case 3:
                                            break;
                                        case 4:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        maxTime = maxTime.AddHours(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        maxTime = maxTime.AddMinutes(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        maxTime = maxTime.AddSeconds(-vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                        case 5:
                                            {
                                                switch (vModel.__config__.startTimeTarget)
                                                {
                                                    case 1:
                                                        maxTime = maxTime.AddHours(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 2:
                                                        maxTime = maxTime.AddMinutes(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                    case 3:
                                                        maxTime = maxTime.AddSeconds(vModel.__config__.endTimeValue.ParseToInt());
                                                        break;
                                                }
                                            }

                                            break;
                                    }

                                    if (maxTime < value && !maxTime.Equals(DateTime.MinValue))
                                    {
                                        var errorInfo = fieldName + "值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            var errorInfo = fieldName + "值不正确";
                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                            else newDataItems.Add(errorKey, errorInfo);
                        }

                        break;
                    case JnpfKeyConst.ADDRESS:
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                if (vModel.multiple)
                                {
                                    var addList = new List<object>();
                                    foreach (var it in item.Value.ToString().Split(","))
                                    {
                                        if (vModel.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT) || (it.Count(x => x == '/') == vModel.level))
                                        {
                                            if (dicList.Where(x => x.ContainsValue(it)).Any())
                                            {
                                                var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().FirstOrDefault();
                                                addList.Add(value.Key.Split(",").ToList());
                                            }
                                            else if (allData.Contains(it))
                                            {
                                                var errorInfo = fieldName + "值不在范围内";
                                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                                else newDataItems.Add(errorKey, errorInfo);
                                                break;
                                            }
                                            else
                                            {
                                                var errorInfo = fieldName + "值不正确";
                                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                                else newDataItems.Add(errorKey, errorInfo);
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            var errorInfo = fieldName + "值的格式不正确";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                            break;
                                        }
                                    }
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (vModel.__config__.jnpfKey.Equals(JnpfKeyConst.COMSELECT) || (item.Value?.ToString().Count(x => x == '/') == vModel.level))
                                    {
                                        if (dicList.Where(x => x.ContainsValue(item.Value?.ToString())).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().FirstOrDefault();
                                            newDataItems[item.Key] = value.Key.Split(",").ToList();
                                        }
                                        else if (allData.Contains(item.Value?.ToString()))
                                        {
                                            var errorInfo = fieldName + "值不在范围内";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                        else
                                        {
                                            var errorInfo = fieldName + "值不正确";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    }
                                    else
                                    {
                                        var errorInfo = fieldName + "值的格式不正确";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.SWITCH:
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.RADIO:
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                if (vModel.multiple || vModel.__config__.jnpfKey.Equals(JnpfKeyConst.CHECKBOX))
                                {
                                    var addList = new List<object>();
                                    foreach (var it in item.Value.ToString().Split(","))
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            var errorInfo = fieldName + "值不正确";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                            break;
                                        }
                                    }
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (dicList.Where(x => x.ContainsValue(item.Value.ToString())).Any())
                                    {
                                        var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().LastOrDefault();
                                        newDataItems[item.Key] = value.Key;
                                    }
                                    else
                                    {
                                        var errorInfo = fieldName + "值不正确";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.GROUPSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.USERSELECT:
                        {
                            if (item.Value.IsNotEmptyOrNull() && (vModel.selectType.IsNullOrEmpty() || vModel.selectType.Equals("all") || vModel.selectType.Equals("custom")))
                            {
                                if (vModel.multiple)
                                {
                                    var addList = new List<object>();
                                    foreach (var it in item.Value.ToString().Split(","))
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else if (allData.Contains(it))
                                        {
                                            var errorInfo = fieldName + "值不在范围内";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                            break;
                                        }
                                        else
                                        {
                                            var errorInfo = fieldName + "值不正确";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                            break;
                                        }
                                    }
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (dicList.Where(x => x.ContainsValue(item.Value.ToString())).Any())
                                    {
                                        var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().LastOrDefault();
                                        newDataItems[item.Key] = value.Key;
                                    }
                                    else if (allData.Contains(item.Value.ToString()))
                                    {
                                        var errorInfo = fieldName + "值不在范围内";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                    else
                                    {
                                        var errorInfo = fieldName + "值不正确";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                            else newDataItems[item.Key] = null;
                        }

                        break;
                    case JnpfKeyConst.USERSSELECT:
                        {
                            if (item.Value.IsNotEmptyOrNull() && (vModel.selectType.IsNullOrEmpty() || vModel.selectType.Equals("all") || vModel.selectType.Equals("custom")))
                            {
                                if (vModel.multiple)
                                {
                                    var addList = new List<object>();
                                    foreach (var it in item.Value.ToString().Split(","))
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            if (dicList.Where(x => x.ContainsValue(it.Split("/").Last())).Any())
                                            {
                                                var value = dicList.Where(x => x.ContainsValue(it.Split("/").Last())).FirstOrDefault().LastOrDefault();
                                                addList.Add(value.Key);
                                            }
                                            else if (allData.Contains(it.Split("/").Last()))
                                            {
                                                var errorInfo = fieldName + "值不在范围内";
                                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                                else newDataItems.Add(errorKey, errorInfo);
                                                break;
                                            }
                                            else
                                            {
                                                var errorInfo = fieldName + "值不正确";
                                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                                else newDataItems.Add(errorKey, errorInfo);
                                                break;
                                            }
                                        }
                                    }
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (dicList.Where(x => x.ContainsValue(item.Value.ToString())).Any())
                                    {
                                        var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().LastOrDefault();
                                        newDataItems[item.Key] = value.Key;
                                    }
                                    else
                                    {
                                        if (dicList.Where(x => x.ContainsValue(item.Value.ToString().Split("/").Last())).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString().Split("/").Last())).FirstOrDefault().LastOrDefault();
                                            newDataItems[item.Key] = value.Key;
                                        }
                                        else if (allData.Contains(item.Value.ToString().Split("/").Last()))
                                        {
                                            var errorInfo = fieldName + "值不在范围内";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                        else
                                        {
                                            var errorInfo = fieldName + "值不正确";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                        }
                                    }
                                }
                            }
                            else newDataItems[item.Key] = null;
                        }

                        break;
                    case JnpfKeyConst.TREESELECT:
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                if (vModel.multiple)
                                {
                                    var addList = new List<object>();
                                    foreach (var it in item.Value.ToString().Split(","))
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            var errorInfo = fieldName + "值不正确";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                            break;
                                        }
                                    }
                                    newDataItems[item.Key] = addList;
                                }
                                else
                                {
                                    if (dicList.Where(x => x.ContainsValue(item.Value.ToString())).Any())
                                    {
                                        var value = dicList.Where(x => x.ContainsValue(item.Value?.ToString())).FirstOrDefault().LastOrDefault();
                                        newDataItems[item.Key] = value.Key;
                                    }
                                    else
                                    {
                                        var errorInfo = fieldName + "值不正确";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.CASCADER:
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {
                                if (vModel.multiple)
                                {
                                    var addsList = new List<object>();
                                    foreach (var its in item.Value.ToString().Split(","))
                                    {
                                        var flag = false;
                                        var txtList = its.Split("/").ToList();

                                        var add = new List<object>();
                                        foreach (var it in txtList)
                                        {
                                            if (dicList.Where(x => x.ContainsValue(it)).Any())
                                            {
                                                var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                                add.Add(value.Key);
                                            }
                                            else
                                            {
                                                var errorInfo = fieldName + "值不正确";
                                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                                else newDataItems.Add(errorKey, errorInfo);
                                                flag = true;
                                                break;
                                            }
                                        }
                                        if (flag) break;
                                        addsList.Add(add);
                                    }
                                    newDataItems[item.Key] = addsList;
                                }
                                else
                                {
                                    var txtList = item.Value.ToString().Split("/").ToList();

                                    var addList = new List<object>();
                                    foreach (var it in txtList)
                                    {
                                        if (dicList.Where(x => x.ContainsValue(it)).Any())
                                        {
                                            var value = dicList.Where(x => x.ContainsValue(it)).FirstOrDefault().LastOrDefault();
                                            addList.Add(value.Key);
                                        }
                                        else
                                        {
                                            var errorInfo = fieldName + "值不正确";
                                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                            else newDataItems.Add(errorKey, errorInfo);
                                            break;
                                        }
                                    }
                                    newDataItems[item.Key] = addList;
                                }
                            }
                        }

                        break;
                    case JnpfKeyConst.TABLE:
                        {
                            if (item.Value != null)
                            {
                                var valueList = item.Value.ToObject<List<Dictionary<string, object>>>();

                                if (vModel.isNumLimit && valueList.Count > vModel.numLimit)
                                {
                                    var errorInfo = fieldName + "超出条数限制";
                                    if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                    else newDataItems.Add(errorKey, errorInfo);
                                    continue;
                                }

                                var newValueList = new List<Dictionary<string, object>>();
                                valueList.ForEach(it =>
                                {
                                    var addValue = new Dictionary<string, object>();
                                    foreach (var value in it) addValue.Add(vModel.__vModel__ + "-" + value.Key, value.Value);
                                    newValueList.Add(addValue);
                                });

                                var res = await ImportDataAssemble(tInfo, vModel.__config__.children, newValueList, cDataList);
                                var result = new List<Dictionary<string, object>>();
                                foreach (var it in res)
                                {
                                    var addValue = new Dictionary<string, object>();
                                    foreach (var value in it)
                                    {
                                        if (value.Key.Contains("|error"))
                                        {
                                            if (newDataItems.ContainsKey(value.Key)) newDataItems[value.Key] = newDataItems[value.Key] + "," + value.Value;
                                            else newDataItems.Add(value.Key, value.Value);
                                        }
                                        else
                                        {
                                            addValue.Add(value.Key.Replace(vModel.__vModel__ + "-", string.Empty), value.Value);
                                        }
                                    }

                                    result.Add(addValue);
                                }

                                newDataItems[item.Key] = result;
                            }
                        }
                        break;
                    case JnpfKeyConst.RATE:
                        try
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {

                                var value = double.Parse(item.Value.ToString());

                                if (value < 0) throw new Exception();

                                if (vModel.allowHalf)
                                {
                                    if (value % 0.5 != 0)
                                        throw new Exception();
                                }
                                else
                                {
                                    if (value % 1 != 0)
                                        throw new Exception();
                                }

                                if (vModel.count != null && vModel.count < value)
                                {
                                    var errorInfo = fieldName + "值不能大于最大值";
                                    if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                    else newDataItems.Add(errorKey, errorInfo);
                                }
                            }
                        }
                        catch
                        {
                            var errorInfo = fieldName + "值不正确";
                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                            else newDataItems.Add(errorKey, errorInfo);
                        }
                        break;
                    case JnpfKeyConst.SLIDER:
                        try
                        {
                            if (item.Value.IsNotEmptyOrNull())
                            {

                                var value = decimal.Parse(item.Value.ToString());
                                if (vModel.max != null)
                                {
                                    if (vModel.max < value)
                                    {
                                        var errorInfo = fieldName + "值不能大于最大值";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                                if (vModel.min != null)
                                {
                                    if (vModel.min > value)
                                    {
                                        var errorInfo = fieldName + "值不能小于最小值";
                                        if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                        else newDataItems.Add(errorKey, errorInfo);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            var errorInfo = fieldName + "值不正确";
                            if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                            else newDataItems.Add(errorKey, errorInfo);
                        }
                        break;
                    case JnpfKeyConst.NUMINPUT:
                        if (item.Value.IsNotEmptyOrNull())
                        {
                            try
                            {
                                var value = decimal.Parse(item.Value.ToString());

                                if (vModel.precision != null && value.ToString().Contains(".") && value.ToString().Split(".").LastOrDefault()?.Length > vModel.precision)
                                {
                                    var errorInfo = fieldName + "值的精度不正确";
                                    if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                    else newDataItems.Add(errorKey, errorInfo);
                                }
                                if (vModel.max != null && vModel.max < value)
                                {
                                    var errorInfo = fieldName + "值不能大于最大值";
                                    if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                    else newDataItems.Add(errorKey, errorInfo);
                                }
                                if (vModel.min != null && vModel.min > value)
                                {
                                    var errorInfo = fieldName + "值不能小于最小值";
                                    if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                    else newDataItems.Add(errorKey, errorInfo);
                                }
                            }
                            catch
                            {
                                var errorInfo = fieldName + "值不正确";
                                if (newDataItems.ContainsKey(errorKey)) newDataItems[errorKey] = newDataItems[errorKey] + "," + errorInfo;
                                else newDataItems.Add(errorKey, errorInfo);
                            }
                        }
                        break;
                }
            }

            resList.Add(newDataItems);
        }

        return resList;
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

    /// <summary>
    /// 递归获取手动添加的省市区,Id处理成树形结构.
    /// </summary>
    /// <param name="addressEntityList"></param>
    private string GetAddressIdByPList(List<ProvinceEntity> addressEntityList, ProvinceEntity pEntity)
    {
        if (pEntity.ParentId == null || pEntity.ParentId.Equals("-1"))
        {
            return pEntity.Id;
        }
        else
        {
            var pItem = addressEntityList.Find(x => x.Id == pEntity.ParentId);
            if (pItem != null) pEntity.Id = GetAddressIdByPList(addressEntityList, pItem) + "," + pEntity.Id;
            else pEntity.Id = pEntity.Id;
            return pEntity.Id;
        }
    }

    /// <summary>
    /// 处理模板默认值.
    /// 用户选择 , 部门选择 , 岗位选择 , 角色选择 , 分组选择 ， 用户组件.
    /// </summary>
    /// <param name="config">模板.</param>
    /// <param name="menuId">菜单id.</param>
    /// <returns></returns>
    private async Task<VisualDevModelDataConfigOutput> GetVisualDevModelDataConfig(VisualDevEntity config, string menuId)
    {
        if (config.WebType.Equals(4)) return config.Adapt<VisualDevModelDataConfigOutput>();

        var tInfo = new TemplateParsingBase();
        if (config.Type == 1)
        {
            tInfo = new TemplateParsingBase(config); // 解析模板
        }
        else if (config.Type == 2)
        {
            tInfo = new TemplateParsingBase(config.FormData, config.Tables); // 解析模板
        }

        if (tInfo.AllFieldsModel.Any(x => (x.__config__.defaultCurrent) && (x.__config__.jnpfKey.Equals(JnpfKeyConst.USERSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.POSSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.ROLESELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.GROUPSELECT) || x.__config__.jnpfKey.Equals(JnpfKeyConst.USERSSELECT))))
        {
            var userId = _userManager.UserId;
            var posIds = _userManager.CurrentPositionList.ToObject<List<string>>();
            var roleIds = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(it => it.UserId.Equals(_userManager.UserId) && it.ObjectType.Equals(CommonConst.RelationObjectRole)).Select(it => it.ObjectId).ToList();
            var groupIds = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(it => it.UserId.Equals(_userManager.UserId) && it.ObjectType.Equals(CommonConst.RelationObjectGroup)).Select(it => it.ObjectId).ToList();

            var allUserRelationList = _visualDevRepository.AsSugarClient().Queryable<UserRelationEntity>().Select(x => new UserRelationEntity() { UserId = x.UserId, ObjectId = x.ObjectId }).ToList();

            var configData = config.FormData.ToObject<Dictionary<string, object>>();
            var columnList = configData["fields"].ToObject<List<Dictionary<string, object>>>();
            _runService.FieldBindDefaultValue(ref columnList, userId, posIds, roleIds, groupIds, allUserRelationList);
            configData["fields"] = columnList;
            config.FormData = configData.ToJsonString();

            if (config.ColumnData.IsNotEmptyOrNull())
            {
                configData = config.ColumnData.ToObject<Dictionary<string, object>>();
                var searchList = configData["searchList"].ToObject<List<Dictionary<string, object>>>();
                columnList = configData["columnList"].ToObject<List<Dictionary<string, object>>>();
                _runService.FieldBindDefaultValue(ref searchList, userId, posIds, roleIds, groupIds, allUserRelationList);
                _runService.FieldBindDefaultValue(ref columnList, userId, posIds, roleIds, groupIds, allUserRelationList);
                configData["searchList"] = searchList;
                configData["columnList"] = columnList;
                config.ColumnData = configData.ToJsonString();
            }

            if (config.AppColumnData.IsNotEmptyOrNull())
            {
                configData = config.AppColumnData.ToObject<Dictionary<string, object>>();
                var searchList = configData["searchList"].ToObject<List<Dictionary<string, object>>>();
                columnList = configData["columnList"].ToObject<List<Dictionary<string, object>>>();
                _runService.FieldBindDefaultValue(ref searchList, userId, posIds, roleIds, groupIds, allUserRelationList);
                _runService.FieldBindDefaultValue(ref columnList, userId, posIds, roleIds, groupIds, allUserRelationList);
                configData["searchList"] = searchList;
                configData["columnList"] = columnList;
                config.AppColumnData = configData.ToJsonString();
            }
        }

        var output = config.Adapt<VisualDevModelDataConfigOutput>();

        if (config.Type == 1)
        {
            // 主表主键
            var primaryKey = tInfo.MainTable?.fields.Find(x => x.primaryKey.Equals(1) && !x.field.ToLower().Equals("f_tenant_id"))?.field;
            if (primaryKey.IsNullOrEmpty())
            {
                var link = await _runService.GetDbLink(config.DbLinkId, null);
                primaryKey = _runService.GetPrimary(link, tInfo.MainTableName);
            }

            output.propsValueList.Add(new PropsValueModel() { field = primaryKey, fieldName = "表单主键" });

            // 主表字段
            if (tInfo.MainTableFieldsModelList.IsNotEmptyOrNull())
            {
                foreach (var mainTableField in tInfo.MainTableFieldsModelList)
                {
                    if (mainTableField.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) || mainTableField.__config__.jnpfKey.Equals(JnpfKeyConst.BILLRULE))
                        output.propsValueList.Add(new PropsValueModel() { field = mainTableField.__vModel__ + "_jnpfId", fieldName = mainTableField.__config__.label });
                }
            }

            // 副表字段
            if (tInfo.AuxiliaryTableFieldsModelList.IsNotEmptyOrNull())
            {
                foreach (var auxiliaryTableField in tInfo.AuxiliaryTableFieldsModelList)
                {
                    if (auxiliaryTableField.__config__.jnpfKey.Equals(JnpfKeyConst.COMINPUT) || auxiliaryTableField.__config__.jnpfKey.Equals(JnpfKeyConst.BILLRULE))
                        output.propsValueList.Add(new PropsValueModel() { field = auxiliaryTableField.__vModel__ + "_jnpfId", fieldName = auxiliaryTableField.__config__.label });
                }
            }
        }

        if (_userManager.UserOrigin.Equals("app"))
        {
            var systemCode = _userManager.AppCode.IsNotEmptyOrNull() ? _userManager.AppCode : CommonConst.SystemCodeMain;
            var entity = await _visualDevRepository.AsSugarClient().Queryable<ModuleUseNumEntity>()
                .Where(x => x.DeleteMark == null && x.UserId.Equals(_userManager.UserId) && x.ModuleId.Equals(menuId) && x.SystemCode.Equals(systemCode))
                .FirstAsync();
            if (entity.IsNotEmptyOrNull())
            {
                entity.UseNum++;
                await _visualDevRepository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.LastModify()).UpdateColumns(x => new { x.UseNum, x.LastModifyTime, x.LastModifyUserId }).ExecuteCommandAsync();
            }
            else
            {
                var newEntity = new ModuleUseNumEntity();
                newEntity.Id = SnowflakeIdHelper.NextId();
                newEntity.UserId = _userManager.UserId;
                newEntity.ModuleId = menuId;
                newEntity.UseNum = 1;
                newEntity.SystemCode = systemCode;
                newEntity.CreatorUserId = _userManager.UserId;
                newEntity.CreatorTime = DateTime.Now;
                newEntity.LastModifyUserId = _userManager.UserId;
                newEntity.LastModifyTime = DateTime.Now;
                await _visualDevRepository.AsSugarClient().Insertable(newEntity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            }
        }

        return output;
    }

    /// <summary>
    /// 是否有菜单权限.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> IsMenuAuthorize(string menuId)
    {
        var flag = false;
        var menu = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().FirstAsync(it => it.DeleteMark == null && it.Id == menuId);
        if (menu.IsNotEmptyOrNull())
        {
            if (!_userManager.IsAdministrator)
            {
                var menuIds = await _userManager.GetUserStandingAuthIdList(CommonConst.AuthorizeItemModule);
                if (menuIds.Contains(menuId)) flag = true;
            }
            else
            {
                flag = true;
            }
        }

        return flag;
    }

    /// <summary>
    /// 是否有菜单按钮权限.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> IsMenuButtonAuthorize(VisualDevEntity templateEntity, string code)
    {
        var flag = false;
        var columnData = _userManager.UserOrigin == "pc" ? templateEntity.ColumnData?.ToObject<ColumnDesignModel>() : templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
        if (columnData.IsNotEmptyOrNull() && columnData.useBtnPermission)
        {
            var menuIds = await _visualDevRepository.AsSugarClient().Queryable<ModuleEntity>().Where(it => it.DeleteMark == null && it.PropertyJson.Contains(templateEntity.Id)).Select(x => x.Id).ToListAsync();
            var buttonIds = await _visualDevRepository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(it => it.DeleteMark == null && it.EnabledMark == 1 && it.EnCode == code && menuIds.Contains(it.ModuleId)).Select(x => x.Id).ToListAsync();
            if (buttonIds.IsNotEmptyOrNull() && buttonIds.Count > 0)
            {
                if (!_userManager.IsAdministrator)
                {
                    var bIds = await _userManager.GetUserStandingAuthIdList(CommonConst.AuthorizeItemButton);
                    flag = bIds.Any(x => buttonIds.Contains(x));
                }
                else
                {
                    flag = true;
                }
            }
        }
        else
        {
            flag = true;
        }

        return flag;
    }

    /// <summary>
    /// 获取当前进程地址.
    /// </summary>
    /// <returns></returns>
    private string GetLocalAddress()
    {
        var server = _serviceScope.ServiceProvider.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        return addresses?.FirstOrDefault()?.Replace("[::]", "localhost");
    }

    #endregion
}