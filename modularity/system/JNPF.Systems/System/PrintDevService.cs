using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.PrintDev;
using JNPF.Systems.Entitys.Dto.System.PrintDev;
using JNPF.Systems.Entitys.Model.DataSet;
using JNPF.Systems.Entitys.Model.PrintDev;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Data;

namespace JNPF.Systems;

/// <summary>
/// 打印模板配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "PrintDev", Order = 200)]
[Route("api/system/[controller]")]
public class PrintDevService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<PrintDevEntity> _repository;

    /// <summary>
    /// 数据字典服务.
    /// </summary>
    private readonly IDictionaryDataService _dictionaryDataService;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 数据接口.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 数据集.
    /// </summary>
    private readonly IDataSetService _dataSetService;

    /// <summary>
    /// 初始化一个<see cref="PrintDevService"/>类型的新实例.
    /// </summary>
    public PrintDevService(
        ISqlSugarRepository<PrintDevEntity> repository,
        IDictionaryDataService dictionaryDataService,
        IBillRullService billRuleService,
        IFileManager fileManager,
        IUserManager userManager,
        IDataInterfaceService dataInterfaceService,
        IDataSetService dataSetService)
    {
        _repository = repository;
        _dictionaryDataService = dictionaryDataService;
        _billRuleService = billRuleService;
        _fileManager = fileManager;
        _userManager = userManager;
        _dataInterfaceService = dataInterfaceService;
        _dataSetService = dataSetService;
    }

    #region Get

    /// <summary>
    /// 列表(分页).
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PrintDevListInput input)
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.SystemId.Equals(_userManager.SystemId))
            .WhereIF(input.category.IsNotEmptyOrNull(), it => it.Category == input.category)
            .WhereIF(input.state.IsNotEmptyOrNull(), it => it.State == input.state)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), it => it.FullName.Contains(input.keyword) || it.EnCode.Contains(input.keyword))
            .OrderBy(it => it.SortCode).OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new PrintDevListOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                category = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(x => x.DeleteMark == null && x.Id == it.Category).Select(x => x.FullName),
                state = it.State,
                sortCode = it.SortCode,
                creatorTime = it.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(x => x.DeleteMark == null && x.Id == it.CreatorUserId).Select(x => SqlFunc.MergeString(x.RealName, "/", x.Account)),
                lastModifyTime = it.LastModifyTime,
                lastModifyUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(x => x.DeleteMark == null && x.Id == it.LastModifyUserId).Select(x => SqlFunc.MergeString(x.RealName, "/", x.Account)),
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<PrintDevListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 打印模板下拉列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "businessType" && x.DeleteMark == null);
        var list = await _repository.AsSugarClient().Queryable<PrintDevEntity, UserEntity, UserEntity, DictionaryDataEntity>((a, b, c, d) => new JoinQueryInfos(JoinType.Left, b.Id == a.CreatorUserId, JoinType.Left, c.Id == a.LastModifyUserId, JoinType.Left, a.Category == d.Id))
            .Where((a, b, c, d) => a.DeleteMark == null && a.State == 1 && d.DictionaryTypeId == dictionaryTypeEntity.Id && a.SystemId.Equals(_userManager.SystemId))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select((a, b, c, d) => new PrintDevListTreeOutput
            {
                category = a.Category,
                id = a.Id,
                fullName = a.FullName,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                enCode = a.EnCode,
                lastModifyTime = a.LastModifyTime,
                lastModifyUser = SqlFunc.MergeString(c.RealName, "/", c.Account),
                sortCode = a.SortCode,
                parentId = d.Id,
            }).ToListAsync();

        // 数据库分类
        var dbTypeList = (await _dictionaryDataService.GetList("businessType")).FindAll(x => x.EnabledMark == 1);
        var result = new List<PrintDevListTreeOutput>();
        foreach (var item in dbTypeList)
        {
            var index = list.FindAll(x => x.category.Equals(item.Id)).Count;
            if (index > 0)
            {
                result.Add(new PrintDevListTreeOutput()
                {
                    id = item.Id,
                    parentId = "0",
                    fullName = item.FullName,
                    num = index
                });
            }
        }

        return new { list = result.OrderBy(x => x.sortCode).Union(list).ToList().ToTree() };
    }

    /// <summary>
    /// 打印模板业务列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("WorkSelector")]
    public async Task<dynamic> GetWorkSelector([FromQuery] PrintDevWorkSelectorInput input)
    {
        var whereLambda = LinqExpression.And<PrintDevEntity>();
        if (!_userManager.IsAdministrator)
        {
            var roles = _userManager.PermissionIds;
            var items = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(a => roles.Contains(a.ObjectId) && a.ItemType == CommonConst.AuthorizeItemPrint).GroupBy(it => it.ItemId).Select(it => it.ItemId).ToListAsync();
            whereLambda = whereLambda.And(x => x.VisibleType == 1 || (x.VisibleType == 2 && items.Contains(x.Id)));
        }

        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.State == 1 && it.CommonUse == 1)
            .Where(whereLambda)
            .WhereIF(input.systemId.IsNotEmptyOrNull(), it => it.SystemId == input.systemId)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), it => it.FullName.Contains(input.keyword) || it.EnCode.Contains(input.keyword))
            .OrderBy(it => it.SortCode).OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new PrintDevWorkSelectorOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                category = it.Category,
                commonUse = it.CommonUse,
                visibleType = it.VisibleType,
                icon = it.Icon,
                iconBackground = it.IconBackground
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<PrintDevWorkSelectorOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 模板预览.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Preview")]
    public async Task<dynamic> Preview(string id)
    {
        var version = await GetVersionEntityInfo(id);
        return await _repository.AsSugarClient().Queryable<PrintVersionEntity>()
            .Where(it => it.DeleteMark == null && it.Id == version.Id)
            .Select(it => new PrintVersionInfoOutput
            {
                id = it.TemplateId,
                versionId = it.Id,
                version = it.Version,
                state = it.State,
                printTemplate = it.PrintTemplate,
                convertConfig = it.ConvertConfig
            })
            .FirstAsync();
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await GetEntityInfo(id)).Adapt<PrintDevInfoOutput>();
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var entity = await GetEntityInfo(id);
        var version = await GetVersionEntityInfo(entity.Id);
        var dataSetList = await GetDataSetList(version.Id);

        var importModel = version.Adapt<PrintVersionInfoOutput>();
        importModel.id = entity.Id;
        importModel.fullName = entity.FullName;
        importModel.enCode = entity.EnCode;
        importModel.category = entity.Category;
        importModel.description = entity.Description;
        importModel.sortCode = entity.SortCode;
        importModel.versionId = version.Id;
        importModel.systemId = entity.SystemId;
        importModel.dataSetList = dataSetList.Adapt<List<PrintDevDataSetModel>>();

        var jsonStr = importModel.ToJsonString();
        return await _fileManager.Export(jsonStr, importModel.fullName, ExportFileType.bp);
    }

    /// <summary>
    /// 版本列表.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("Version/{id}")]
    public async Task<dynamic> GetVersionList(string id)
    {
        return await _repository.AsSugarClient().Queryable<PrintVersionEntity>()
            .Where(it => it.DeleteMark == null && it.TemplateId == id)
            .OrderBy(it => it.SortCode, OrderByType.Desc).OrderBy(it => it.State).OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new PrintVersionListOutput
            {
                id = it.Id,
                templateId = it.TemplateId,
                fullName = SqlFunc.MergeString("打印版本V", it.Version.ToString()),
                version = it.Version,
                state = it.State,
                printTemplate = it.PrintTemplate,
            })
            .ToListAsync();
    }

    /// <summary>
    /// 版本详情.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("Info/{id}")]
    public async Task<dynamic> GetVersionInfo(string id)
    {
        var data = await _repository.AsSugarClient().Queryable<PrintVersionEntity>()
            .Where(it => it.DeleteMark == null && it.Id == id)
            .Select(it => new PrintVersionInfoOutput
            {
                id = it.TemplateId,
                versionId = it.Id,
                version = it.Version,
                state = it.State,
                printTemplate = it.PrintTemplate,
                convertConfig = it.ConvertConfig,
                globalConfig = it.GlobalConfig,
            })
            .FirstAsync();
        data.dataSetList = (await _repository.AsSugarClient().Queryable<DataSetEntity>()
            .Where(it => it.DeleteMark == null && it.ObjectId == id)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .ToListAsync()).Adapt<List<PrintDevDataSetModel>>();

        var parameter = new List<SugarParameter>() { new("@formId", null) };
        foreach (var item in data.dataSetList)
        {
            switch (item.type)
            {
                case 1:
                    {
                        var sql = item.dataConfigJson;
                        item.children = await _dataSetService.GetFieldModels(item.dbLinkId, sql, parameter);
                    }

                    break;
                case 2:
                    {
                        var filterConfig = item.filterConfigJson.ToObject<FilterConfigModel>();
                        var sql = await _dataSetService.GetVisualConfigSql(item.dbLinkId, item.visualConfigJson, filterConfig);
                        item.children = await _dataSetService.GetFieldModels(item.dbLinkId, sql, parameter);
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

        return data;
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task<dynamic> Create([FromBody] PrintDevCrInput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || (x.FullName == input.fullName && x.SystemId.Equals(_userManager.SystemId))) && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1004);

        var entity = input.Adapt<PrintDevEntity>();
        entity.SystemId = _userManager.SystemId;
        if (entity.EnCode.IsNullOrEmpty()) entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePrint);
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1) throw Oops.Oh(ErrorCode.COM1000);

        var versionEntity = new PrintVersionEntity()
        {
            TemplateId = entity.Id,
            Version = 1,
        };
        await _repository.AsSugarClient().Insertable(versionEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

        return entity.Id;
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
        var isOk = await _repository.AsUpdateable().SetColumns(it => new PrintDevEntity()
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
    public async Task Update(string id, [FromBody] PrintDevUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.DeleteMark == null && (x.EnCode == input.enCode || (x.FullName == input.fullName && x.SystemId.Equals(_userManager.SystemId)))))
            throw Oops.Oh(ErrorCode.COM1004);

        var entity = input.Adapt<PrintDevEntity>();
        if (entity.EnCode.IsNullOrEmpty()) entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePrint);
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 保存或发布.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("Save")]
    [UnitOfWork]
    public async Task Save([FromBody] PrintDevSaveInput input)
    {
        var entity = input.Adapt<PrintVersionEntity>();
        if (input.type == 1)
        {
            await _repository.AsSugarClient().Updateable<PrintVersionEntity>().SetColumns(it => it.State == 2).SetColumns(it => it.SortCode == 0).Where(it => it.DeleteMark == null && it.TemplateId == input.id && it.State == 1).ExecuteCommandAsync();
            entity.State = 1;
            entity.SortCode = 1;

            await _repository.AsUpdateable().SetColumns(it => new PrintDevEntity
            {
                State = 1,
                LastModifyTime = SqlFunc.GetDate(),
                LastModifyUserId = _userManager.UserId
            }).Where(it => it.Id == input.id).ExecuteCommandAsync();
        }

        await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

        // 数据集
        await _dataSetService.SaveDataSetList(input.versionId, "printVersion", input.dataSetList);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    [UnitOfWork]
    public async Task ActionsCopy(string id)
    {
        var entity = await GetEntityInfo(id);
        var version = await GetVersionEntityInfo(id);
        var dataSetList = await GetDataSetList(version.Id);
        var random = new Random().NextLetterAndNumberString(5).ToLower();

        // 打印模板
        entity.Id = SnowflakeIdHelper.NextId();
        entity.FullName = entity.FullName + ".副本" + random;
        entity.EnCode += random;
        entity.State = 0;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);

        // 打印模板版本
        version.Id = SnowflakeIdHelper.NextId();
        version.TemplateId = entity.Id;
        version.State = 0;
        version.Version = 1;
        version.LastModifyTime = null;
        version.LastModifyUserId = null;

        // 打印模板版本的数据集
        foreach (var item in dataSetList)
        {
            item.ObjectId = version.Id;
            item.LastModifyTime = null;
            item.LastModifyUserId = null;
        }

        try
        {
            await _repository.AsInsertable(entity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            await _repository.AsSugarClient().Insertable(version).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            await _repository.AsSugarClient().Insertable(dataSetList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.COM1008);
        }
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
        if (!fileType.ToLower().Equals(ExportFileType.bp.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        PrintVersionInfoOutput? model;
        PrintDevEntity? entity;
        try
        {
            model = josn.ToObject<PrintVersionInfoOutput>();
            entity = model.Adapt<PrintDevEntity>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }

        if (entity.SystemId.IsNullOrEmpty() || !entity.SystemId.Equals(_userManager.SystemId))
        {
            entity.Id = SnowflakeIdHelper.NextId();
            entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePrint);
        }

        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(entity.Id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(entity.EnCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.SystemId.Equals(_userManager.SystemId) && it.FullName.Equals(entity.FullName))) errorList.Add("名称");

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
                entity.Id = SnowflakeIdHelper.NextId();
                entity.FullName = string.Format("{0}.副本{1}", entity.FullName, random);
                entity.EnCode += random;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        entity.SystemId = _userManager.SystemId;
        entity.State = 0;
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;

        var version = model.Adapt<PrintVersionEntity>();
        version.Id = SnowflakeIdHelper.NextId();
        version.TemplateId = entity.Id;
        version.State = 0;
        version.Version = 1;
        version.CreatorTime = DateTime.Now;
        version.CreatorUserId = _userManager.UserId;
        version.LastModifyTime = null;
        version.LastModifyUserId = null;

        var dataSetList = new List<DataSetEntity>();
        foreach (var item in model.dataSetList)
        {
            var dataSet = item.Adapt<DataSetEntity>();
            dataSet.ObjectId = version.Id;
            dataSet.ObjectType = "printVersion";
            dataSet.LastModifyTime = null;
            dataSet.LastModifyUserId = null;
            dataSetList.Add(dataSet);
        }

        try
        {
            var storModuleModel = _repository.AsSugarClient().Storageable(entity).WhereColumns(it => it.Id).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新

            await _repository.AsSugarClient().Insertable(version).ExecuteCommandAsync();
            await _repository.AsSugarClient().Insertable(dataSetList).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }

    /// <summary>
    /// 模板列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("getListOptions")]
    public async Task<dynamic> GetListOptions([FromBody] PrintDevSqlDataQuery input)
    {
        return await _repository.AsQueryable().Where(x => input.ids.Contains(x.Id)).Select(x => new { id = x.Id, fullName = x.FullName }).ToListAsync();
    }

    /// <summary>
    /// 模板数据.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("BatchData")]
    public async Task<dynamic> GetBatchData([FromBody] PrintDevSqlDataQuery input)
    {
        if (!await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id == input.id)) throw Oops.Oh(ErrorCode.D9010);
        var fullName = (await GetEntityInfo(input.id)).FullName;
        var version = await GetVersionEntityInfo(input.id);
        var globalConfig = version.GlobalConfig?.ToObject<PrintDevGlobalConfigModel>();

        var output = new List<PrintDevDataOutput>();
        foreach (var info in input.formInfo)
        {
            if (globalConfig.IsNotEmptyOrNull() && globalConfig.sliceConfig.Count > 0)
            {
                var dataList = await GetGlobalConfigOutput(fullName, version, info.formId, info.flowTaskId);
                output.AddRange(dataList);
            }
            else
            {
                var data = await GetPrintDevDataOutput(fullName, version, info.formId, info.flowTaskId);
                output.Add(data);
            }
        }

        return output;
    }

    /// <summary>
    /// 版本新增.
    /// </summary>
    /// <param name="versionId"></param>
    /// <returns></returns>
    [HttpPost("Info/{versionId}")]
    [UnitOfWork]
    public async Task<dynamic> CreateVersion(string versionId)
    {
        var version = await _repository.AsSugarClient().Queryable<PrintVersionEntity>().Where(it => it.DeleteMark == null && it.Id == versionId).FirstAsync();
        var versionDataSetList = await _repository.AsSugarClient().Queryable<DataSetEntity>().Where(it => it.DeleteMark == null && it.ObjectId == version.Id).ToListAsync();
        var maxVersion = await _repository.AsSugarClient().Queryable<PrintVersionEntity>().Where(it => it.DeleteMark == null && it.TemplateId == version.TemplateId).MaxAsync(it => it.Version);

        // 版本
        var newVersionEntity = new PrintVersionEntity()
        {
            Id = SnowflakeIdHelper.NextId(),
            PrintTemplate = version.PrintTemplate,
            TemplateId = version.TemplateId,
            Version = maxVersion + 1,
            ConvertConfig = version.ConvertConfig,
            GlobalConfig = version.GlobalConfig,
            SortCode = 0,
            CreatorTime = DateTime.Now,
            CreatorUserId = _userManager.UserId
        };

        // 版本数据源
        var newDataSetList = new List<DataSetEntity>();
        foreach (var item in versionDataSetList)
        {
            var newDataSet = item.Adapt<DataSetEntity>();
            newDataSet.Id = SnowflakeIdHelper.NextId();
            newDataSet.ObjectId = newVersionEntity.Id;
            newDataSet.CreatorTime = DateTime.Now;
            newDataSet.CreatorUserId = _userManager.UserId;
            newDataSet.LastModifyTime = null;
            newDataSet.LastModifyUserId = null;
            newDataSetList.Add(newDataSet);
        }

        await _repository.AsSugarClient().Insertable(newVersionEntity).ExecuteCommandAsync();
        await _repository.AsSugarClient().Insertable(newDataSetList).ExecuteCommandAsync();

        return newVersionEntity.Id;
    }

    /// <summary>
    /// 版本删除.
    /// </summary>
    /// <param name="versionId">主键值.</param>
    /// <returns></returns>
    [HttpDelete("Info/{versionId}")]
    public async Task DeleteVersion(string versionId)
    {
        if (!await _repository.AsSugarClient().Queryable<PrintVersionEntity>().AnyAsync(x => x.Id == versionId && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsSugarClient().Updateable<PrintVersionEntity>().SetColumns(it => new PrintVersionEntity()
        {
            DeleteMark = 1,
            DeleteUserId = _userManager.UserId,
            DeleteTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(versionId)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<PrintDevEntity> GetEntityInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
    }

    /// <summary>
    /// 启用的版本信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<PrintVersionEntity> GetVersionEntityInfo(string id)
    {
        var list = await _repository.AsSugarClient().Queryable<PrintVersionEntity>().Where(it => it.DeleteMark == null && it.TemplateId == id).ToListAsync();
        return list.Any(x => x.State == 1) ? list.Find(x => x.State == 1) : list.First();
    }

    /// <summary>
    /// 版本的数据集.
    /// </summary>
    /// <param name="versionId"></param>
    /// <returns></returns>
    private async Task<List<DataSetEntity>> GetDataSetList(string versionId)
    {
        return await _repository.AsSugarClient().Queryable<DataSetEntity>().Where(it => it.DeleteMark == null && it.ObjectId == versionId).ToListAsync();
    }

    /// <summary>
    /// 获取版本数据集的数据.
    /// </summary>
    /// <param name="version"></param>
    /// <param name="formId"></param>
    /// <returns></returns>
    private async Task<Dictionary<string, object>> GetDataSetVersionDataList(PrintVersionEntity version, string formId)
    {
        var dic = new Dictionary<string, object>();
        var dataSetList = await GetDataSetList(version.Id);
        foreach (var dataSet in dataSetList)
        {

            var list = new List<Dictionary<string, object>>();
            switch (dataSet.Type)
            {
                case 1:
                    {
                        var parameter = new List<SugarParameter>() { new("@formId", formId) };
                        var sql = dataSet.DataConfigJson.Replace("N'@formId'", "@formId");
                        list = await _dataSetService.GetDataSetData(dataSet.DbLinkId, sql, parameter);
                    }

                    break;
                case 2:
                    {
                        var filterConfig = dataSet.FilterConfigJson.ToObject<FilterConfigModel>();
                        var sql = await _dataSetService.GetVisualConfigSql(dataSet.DbLinkId, dataSet.VisualConfigJson, filterConfig);
                        sql = sql.Replace("N'@formId'", "@formId");
                        var parameter = new List<SugarParameter>() { new("@formId", formId) };
                        list = await _dataSetService.GetDataSetData(dataSet.DbLinkId, sql, parameter);
                        list = _dataSetService.ResultFiltering(list, filterConfig);
                    }

                    break;
                case 3:
                    {
                        var systemParamter = new Dictionary<string, object> { { "@formId", formId } };
                        var par = new DataInterfacePreviewInput()
                        {
                            paramList = dataSet.ParameterJson.ToObject<List<DataInterfaceParameter>>(),
                            systemParamter = systemParamter,
                            currentPage = 1,
                            pageSize = 20
                        };

                        list = (await _dataInterfaceService.GetResponseByType(dataSet.InterfaceId, 0, par)).ToObject<PageResult<Dictionary<string, object>>>().list;
                    }

                    break;
            }

            await _dataSetService.ConvertData(list, dataSet, version.ConvertConfig);
            dic.Add(dataSet.FullName, list);
        }

        return dic;
    }

    /// <summary>
    /// 模板数据.
    /// </summary>
    /// <param name="fullName"></param>
    /// <param name="version"></param>
    /// <param name="formId"></param>
    /// <param name="flowTaskId"></param>
    /// <returns></returns>
    private async Task<PrintDevDataOutput> GetPrintDevDataOutput(string fullName, PrintVersionEntity version, string formId, string flowTaskId)
    {
        var output = new PrintDevDataOutput();
        output.fullName = fullName;
        output.printData = await GetDataSetVersionDataList(version, formId);
        output.printTemplate = version.PrintTemplate;
        output.convertConfig = version.ConvertConfig;

        if (flowTaskId.IsNotEmptyOrNull())
        {
            output.operatorRecordList = await _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>()
                .Where(a => a.TaskId == flowTaskId)
                .Select(a => new PrintDevDataModel()
                {
                    id = a.Id,
                    handleId = a.HandleId,
                    handleOpinion = a.HandleOpinion,
                    handleStatus = a.HandleType,
                    nodeCode = a.NodeCode,
                    handleTime = a.HandleTime,
                    nodeName = a.NodeName,
                    signImg = a.SignImg,
                    taskId = a.TaskId,
                    operatorId = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.OperatorId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                    userName = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.HandleId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                    status = a.Status,
                    taskNodeId = a.NodeId,
                    taskOperatorId = a.OperatorId,
                }).ToListAsync();
        }

        return output;
    }

    /// <summary>
    /// 获取全局设置.
    /// </summary>
    /// <param name="fullName"></param>
    /// <param name="version"></param>
    /// <param name="formId"></param>
    /// <param name="flowTaskId"></param>
    /// <returns></returns>
    private async Task<List<PrintDevDataOutput>> GetGlobalConfigOutput(string fullName, PrintVersionEntity version, string formId, string flowTaskId)
    {
        var output = new List<PrintDevDataOutput>();
        var data = new PrintDevDataOutput() { fullName = fullName, printTemplate = version.PrintTemplate, globalConfig = version.GlobalConfig, convertConfig = version.ConvertConfig };
        if (flowTaskId.IsNotEmptyOrNull())
        {
            data.operatorRecordList = await _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>()
                .Where(a => a.TaskId == flowTaskId)
                .Select(a => new PrintDevDataModel()
                {
                    id = a.Id,
                    handleId = a.HandleId,
                    handleOpinion = a.HandleOpinion,
                    handleStatus = a.HandleType,
                    nodeCode = a.NodeCode,
                    handleTime = a.HandleTime,
                    nodeName = a.NodeName,
                    signImg = a.SignImg,
                    taskId = a.TaskId,
                    operatorId = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.OperatorId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                    userName = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.HandleId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                    status = a.Status,
                    taskNodeId = a.NodeId,
                    taskOperatorId = a.OperatorId,
                }).ToListAsync();
        }

        var printData = await GetDataSetVersionDataList(version, formId);
        var globalConfig = version.GlobalConfig.ToObject<PrintDevGlobalConfigModel>();
        var sliceConfig = globalConfig.sliceConfig.Where(it => it.dataSet.IsNotEmptyOrNull() && it.limit > 0).ToList();
        switch (sliceConfig.Count)
        {
            case 1:
                {
                    var config = sliceConfig[0];
                    var list = printData[config.dataSet].ToObject<List<object>>();
                    printData.Remove(config.dataSet);
                    var count = PartitionList(list, config.limit);
                    output.AddRange(GetGlobalConfigPrintData(data, count, config));
                }

                break;
            case 2:
                {
                    var config1 = sliceConfig[0];
                    var config2 = sliceConfig[1];
                    var list1 = printData[config1.dataSet].ToObject<List<object>>();
                    printData.Remove(config1.dataSet);
                    var list2 = printData[config2.dataSet].ToObject<List<object>>();
                    printData.Remove(config2.dataSet);
                    var count1 = PartitionList(list1, config1.limit);
                    var count2 = PartitionList(list2, config2.limit);
                    if (count1.Count > 0 && count2.Count > 0)
                    {
                        output.AddRange(GetGlobalConfigPrintData(data, count1, config1, count2, config2));
                    }
                    else if (count1.Count > 0)
                    {
                        output.AddRange(GetGlobalConfigPrintData(data, count1, config1));
                    }
                    else if (count2.Count > 0)
                    {
                        output.AddRange(GetGlobalConfigPrintData(data, count2, config2));
                    }
                    else
                    {
                        var dic = new Dictionary<string, object> { { "systemSlicePageNo", "1-1" } };
                        var newData = data.Adapt<PrintDevDataOutput>();
                        newData.printData = dic;
                        output.Add(newData);
                    }
                }

                break;
        }

        foreach (var item in output)
        {
            var newData = item.printData.ToObject<Dictionary<string, object>>();
            foreach (var otherData in printData) newData.Add(otherData.Key, otherData.Value);
            item.printData = newData;
        }

        return output;
    }

    /// <summary>
    /// 分割集合.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="num"></param>
    /// <returns></returns>
    private List<List<object>> PartitionList(List<object> list, int num)
    {
        var listGroup = new List<List<object>>();
        int j = num;
        for (int i = 0; i < list.Count; i += num)
        {
            var cList = new List<object>();
            cList = list.Take(j).Skip(i).ToList();
            j += num;
            listGroup.Add(cList);
        }

        return listGroup;
    }

    /// <summary>
    /// 获取全局设置的打印数据.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="count1"></param>
    /// <param name="config1"></param>
    /// <param name="count2"></param>
    /// <param name="config2"></param>
    /// <returns></returns>
    private List<PrintDevDataOutput> GetGlobalConfigPrintData(PrintDevDataOutput data, List<List<object>> count1, SliceConfig config1, List<List<object>>? count2 = null, SliceConfig? config2 = null)
    {
        var output = new List<PrintDevDataOutput>();
        for (var i = 0; i < count1.Count; i++)
        {
            if (count2.IsNotEmptyOrNull() && count2.Count > 0)
            {
                for (var j = 0; j < count2.Count; j++)
                {
                    var dic = new Dictionary<string, object>();
                    dic.Add(config1.dataSet, count1[i]);
                    dic.Add(config2.dataSet, count2[j]);
                    dic.Add("systemSlicePageNo", string.Format("{0}-{1}", j + 1, count2.Count));
                    var newData = data.Adapt<PrintDevDataOutput>();
                    newData.printData = dic;
                    output.Add(newData);
                }
            }
            else
            {
                var dic = new Dictionary<string, object>();
                dic.Add(config1.dataSet, count1[i]);
                dic.Add("systemSlicePageNo", string.Format("{0}-{1}", i + 1, count1.Count));
                var newData = data.Adapt<PrintDevDataOutput>();
                newData.printData = dic;
                output.Add(newData);
            }
        }

        return output;
    }
    #endregion
}