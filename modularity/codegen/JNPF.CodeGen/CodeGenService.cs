using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.Engine.Entity.Model.CodeGen;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Model.DataBase;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Common;
using JNPF.Systems.Interfaces.System;
using JNPF.ViewEngine;
using JNPF.VisualDev.Engine;
using JNPF.VisualDev.Engine.CodeGen;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Engine.Security;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.CodeGen;
using JNPF.VisualDev.Entitys.Enum;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SqlSugar;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace JNPF.CodeGen;

/// <summary>
/// 业务实现：代码生成.
/// </summary>
[ApiDescriptionSettings(Tag = "CodeGenerater", Name = "Generater", Order = 175)]
[Route("api/visualdev/[controller]")]
public class CodeGenService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevReleaseEntity> _repository;

    /// <summary>
    /// 视图引擎.
    /// </summary>
    private readonly IViewEngine _viewEngine;

    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 字典数据服务.
    /// </summary>
    private readonly IDictionaryDataService _dictionaryDataService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileService _fileService;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 数据库管理器.
    /// </summary>
    private readonly IDataBaseManager _databaseManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 初始化一个<see cref="CodeGenService"/>类型的新实例.
    /// </summary>
    public CodeGenService(
        ISqlSugarRepository<VisualDevReleaseEntity> visualDevRepository,
        IViewEngine viewEngine,
        IDbLinkService dbLinkService,
        IDictionaryDataService dictionaryDataService,
        IFileService fileService,
        IUserManager userManager,
        IOptions<TenantOptions> tenantOptions,
        ICacheManager cacheManager,
        IDataBaseManager databaseManager)
    {
        _repository = visualDevRepository;
        _viewEngine = viewEngine;
        _dbLinkService = dbLinkService;
        _dictionaryDataService = dictionaryDataService;
        _fileService = fileService;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _tenant = tenantOptions.Value;
        _databaseManager = databaseManager;
    }

    #region Post

    /// <summary>
    /// 下载代码.
    /// </summary>
    [HttpPost("{id}/Actions/DownloadCode")]
    public async Task<dynamic> DownloadCode(string id, [FromBody] DownloadCodeFormInput downloadCodeForm)
    {
        var templateEntity = await _repository.GetFirstAsync(v => v.Id == id && v.DeleteMark == null);
        _ = templateEntity ?? throw Oops.Oh(ErrorCode.COM1005);
        if (templateEntity.WebType.Equals(4)) templateEntity = GetCodeGenDataViewEntity(templateEntity);
        _ = templateEntity.Tables ?? throw Oops.Oh(ErrorCode.D2100);
        if (templateEntity.FormData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1026);
        if (templateEntity.WebType.Equals(2) && templateEntity.ColumnData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1027);

        templateEntity.EnableFlow = downloadCodeForm.enableFlow;
        var tableList = templateEntity.Tables.ToObject<List<DbTableRelationModel>>();
        var mainTable = tableList.Find(x => x.typeId.Equals("1"));

        // 表和字段重命名
        var aliasList = await _repository.AsSugarClient().Queryable<VisualAliasEntity>().Where(x => x.VisualId.Equals(templateEntity.Id)).ToListAsync();
        if (aliasList.Any(x => x.TableName.Equals(mainTable.table) && x.FieldName.IsNullOrEmpty() && x.AliasName.IsNotEmptyOrNull()))
            downloadCodeForm.className = aliasList.Find(x => x.TableName.Equals(mainTable.table) && x.FieldName.IsNullOrEmpty() && x.AliasName.IsNotEmptyOrNull()).AliasName;
        else
            downloadCodeForm.className = mainTable.table;
        var model = templateEntity.FormData.ToObjectOld<FormDataModel>();
        var dictionaryData = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().FirstAsync(it => it.Id.Equals(downloadCodeForm.module));
        downloadCodeForm.modulePackageName = dictionaryData.FullName;
        if (templateEntity.Type == 3)
            downloadCodeForm.modulePackageName = "WorkFlow";
        model.className = new List<string>() { downloadCodeForm.className.ParseToPascalCase() };
        model.areasName = downloadCodeForm.modulePackageName;
        string fileName = string.Format("{0}_{1:yyyyMMddHHmmss}", templateEntity.FullName, DateTime.Now);

        foreach (var item in tableList)
        {
            if (item.typeId.Equals("1"))
            {
                item.className = downloadCodeForm.className.ParseToPascalCase();
                item.tableName = downloadCodeForm.description;
            }
            else
            {
                if (aliasList.Any(x => x.TableName.Equals(item.table) && x.FieldName.IsNullOrEmpty() && x.AliasName.IsNotEmptyOrNull()))
                    item.className = aliasList.Find(x => x.TableName.Equals(item.table) && x.FieldName.IsNullOrEmpty() && x.AliasName.IsNotEmptyOrNull()).AliasName;
                else
                    item.className = item.table.ParseToPascalCase();
            }
        }

        templateEntity.FormData = model.ToJsonString();
        templateEntity.Tables = tableList.ToJsonString();

        // 模板数据聚合
        await TemplatesDataAggregation(fileName, templateEntity.Adapt<VisualDevEntity>(), aliasList);
        string randPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName);
        string downloadPath = randPath + ".zip";

        // 判断是否存在同名称文件
        if (File.Exists(downloadPath))
            File.Delete(downloadPath);

        ZipFile.CreateFromDirectory(randPath, downloadPath);
        if (!App.Configuration["OSS:Provider"].Equals("Invalid"))
            await _fileService.UploadFileByType(downloadPath, "CodeGenerate", string.Format("{0}.zip", fileName));
        var downloadFileName = string.Format("{0}|{1}.zip|codeGenerator", _userManager.UserId, fileName);
        _cacheManager.Set(fileName + ".zip", string.Empty);
        return new { name = fileName, url = "/api/File/Download?encryption=" + DESEncryption.Encrypt(downloadFileName, "JNPF") };
    }

    /// <summary>
    /// 预览代码.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="downloadCodeForm"></param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/CodePreview")]
    public async Task<dynamic> CodePreview(string id, [FromBody] DownloadCodeFormInput downloadCodeForm)
    {
        var tEntity = await _repository.GetFirstAsync(v => v.Id == id && v.DeleteMark == null);
        var dataList = await GetCodePreview(tEntity, downloadCodeForm);
        if (downloadCodeForm.contrast)
        {
            var oldDataList = new List<Dictionary<string, object>>();
            var vEntity = await _repository.AsSugarClient().Queryable<VisualDevEntity>().FirstAsync(v => v.Id == id && v.DeleteMark == null);
            if (vEntity.State.Equals(2))
            {
                oldDataList = await GetCodePreview(vEntity.Adapt<VisualDevReleaseEntity>(), downloadCodeForm);
            }
            else if (vEntity.State.Equals(1))
            {
                var oldEntity = tEntity.OldContent.IsNotEmptyOrNull() ? tEntity.OldContent.ToObject<VisualDevReleaseEntity>() : tEntity.ToObject<VisualDevReleaseEntity>();
                if (tEntity.OldContent.IsNotEmptyOrNull())
                {
                    oldEntity.EnCode = tEntity.EnCode;
                    oldEntity.FullName = tEntity.FullName;
                    oldEntity.Category = tEntity.Category;
                    oldEntity.DbLinkId = tEntity.DbLinkId;
                    oldEntity.CreatorTime = tEntity.CreatorTime;
                    oldEntity.CreatorUserId = tEntity.CreatorUserId;
                    if (oldEntity.WebType == null) oldEntity.WebType = tEntity.WebType;
                }
                oldDataList = await GetCodePreview(oldEntity, downloadCodeForm);
            }

            foreach (var item in dataList)
            {
                var old = oldDataList.Find(x => x["fileName"].Equals(item["fileName"]));
                if (old != null)
                {
                    var itemChildren = item["children"].ToObject<List<Dictionary<string, object>>>();
                    var oldChildren = old["children"].ToObject<List<Dictionary<string, object>>>();

                    foreach (var it in itemChildren)
                    {
                        var oldIT = oldChildren.Find(x => x["fileName"].Equals(it["fileName"]) && x["fileType"].Equals(it["fileType"]));
                        if (oldIT != null)
                        {
                            if (vEntity.State.Equals(1))
                            {
                                it["oldFileContent"] = oldIT["fileContent"];
                            }
                            else
                            {
                                it["oldFileContent"] = it["fileContent"];
                                it["fileContent"] = oldIT["fileContent"];
                            }
                        }
                    }

                    item["children"] = itemChildren;
                }
            }
        }

        return new { list = dataList };
    }

    private async Task<List<Dictionary<string, object>>> GetCodePreview(VisualDevReleaseEntity templateEntity, [FromBody] DownloadCodeFormInput downloadCodeForm)
    {
        _ = templateEntity ?? throw Oops.Oh(ErrorCode.COM1005);
        if (templateEntity.WebType.Equals(4)) templateEntity = GetCodeGenDataViewEntity(templateEntity);
        _ = templateEntity.Tables ?? throw Oops.Oh(ErrorCode.D2100);
        if (templateEntity.FormData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1028);
        if (templateEntity.WebType.Equals(2) && templateEntity.ColumnData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1029);

        templateEntity.EnableFlow = downloadCodeForm.enableFlow;
        var tableList = templateEntity.Tables.ToObject<List<DbTableRelationModel>>();
        var mainTable = tableList.Find(x => x.typeId.Equals("1"));

        // 表和字段重命名
        var aliasList = await _repository.AsSugarClient().Queryable<VisualAliasEntity>().Where(x => x.VisualId.Equals(templateEntity.Id)).ToListAsync();
        if (aliasList.Any(x => x.TableName.Equals(mainTable.table) && x.FieldName.IsNullOrEmpty() && x.AliasName.IsNotEmptyOrNull()))
            downloadCodeForm.className = aliasList.Find(x => x.TableName.Equals(mainTable.table) && x.FieldName.IsNullOrEmpty() && x.AliasName.IsNotEmptyOrNull()).AliasName;
        else
            downloadCodeForm.className = mainTable.table;
        var model = templateEntity.FormData.ToObjectOld<FormDataModel>();
        var dictionaryData = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().FirstAsync(it => it.Id.Equals(downloadCodeForm.module));
        downloadCodeForm.modulePackageName = dictionaryData.FullName;
        if (templateEntity.Type == 3)
            downloadCodeForm.modulePackageName = "WorkFlow";
        model.className = new List<string>() { downloadCodeForm.className.ParseToPascalCase() };
        model.areasName = downloadCodeForm.modulePackageName;
        string fileName = SnowflakeIdHelper.NextId();

        foreach (var item in tableList)
        {
            if (item.typeId.Equals("1"))
            {
                item.className = downloadCodeForm.className.ParseToPascalCase();
                item.tableName = downloadCodeForm.description;
            }
            else
            {
                if (aliasList.Any(x => x.TableName.Equals(item.table) && x.FieldName.IsNullOrEmpty() && x.AliasName.IsNotEmptyOrNull()))
                    item.className = aliasList.Find(x => x.TableName.Equals(item.table) && x.FieldName.IsNullOrEmpty() && x.AliasName.IsNotEmptyOrNull()).AliasName;
                else
                    item.className = item.table.ParseToPascalCase();
            }
        }

        templateEntity.FormData = model.ToJsonString();
        templateEntity.Tables = tableList.ToJsonString();

        await TemplatesDataAggregation(fileName, templateEntity.Adapt<VisualDevEntity>(), aliasList);
        string randPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName);
        var dataList = this.PriviewCode(randPath);
        if (dataList == null && dataList.Count == 0)
            throw Oops.Oh(ErrorCode.D2102);
        return dataList;
    }

    /// <summary>
    /// 获取命名规范.
    /// </summary>
    /// <param name="id"></param>
    [HttpGet("{id}/Alias/Info")]
    public async Task<dynamic> GetAliasInfo(string id)
    {
        var list = await _repository.AsSugarClient().Queryable<VisualAliasEntity>().Where(x => x.VisualId.Equals(id))
            .Select(x => new VisualAliasTableModel()
            {
                aliasName = x.AliasName,
                table = x.TableName,
                fieldName = x.FieldName,
            }).ToListAsync();

        var vEntity = await _repository.AsSugarClient().Queryable<VisualDevEntity>().Where(x => x.Id.Equals(id)).FirstAsync();
        var tInfo = new TemplateParsingBase(vEntity);

        var res = new List<VisualAliasTableModel>();

        foreach (var tItem in tInfo.AllTable)
        {
            var tableName = list.Find(x => x.fieldName.IsNullOrEmpty() && x.table.IsNotEmptyOrNull() && x.table.Equals(tItem.table))?.aliasName;
            var table = new VisualAliasTableModel() { aliasName = tableName, table = tItem.table, comment = tItem.tableName, fields = new List<VisualAliasTableFields>() };
            var fieldList = tInfo.AllFieldsModel.Where(x => x.__config__.parentVModel.IsNullOrEmpty() ? x.__config__.tableName.Equals(tItem.table) : x.__config__.relationTable.Equals(tItem.table)).ToList();
            foreach (var item in fieldList.Where(x => !x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
            {
                if (item.__vModel__.IsNotEmptyOrNull())
                {
                    var vModel = item.__config__.parentVModel.IsNotEmptyOrNull() ? item.__vModel__.Replace(item.__config__.parentVModel + "-", "") : item.__vModel__;
                    var fieldName = list.Find(x => x.fieldName.IsNotEmptyOrNull() && x.table.IsNotEmptyOrNull() && x.table.Equals(tItem.table) && x.fieldName.Equals(vModel))?.aliasName;
                    table.fields.Add(new VisualAliasTableFields() { aliasName = fieldName, field = vModel, fieldName = item.__config__.label });
                }
            }

            res.Add(table);
        }

        return res;
    }

    /// <summary>
    /// 保存命名规范.
    /// </summary>
    /// <param name="id"></param>
    [HttpPost("{id}/Alias/Save")]
    public async Task AliasSave(string id, [FromBody] VisualAliasInput input)
    {
        var systemKeyList = CommonConst.SYSTEMKEY.Split(",").ToList();

        var aliasList = new List<VisualAliasEntity>();
        Regex re = new Regex(@"^[0-9]");
        Regex re2 = new Regex(@"^[0-9a-zA-Z_]+$");
        if (input.tableList.Where(x => x.aliasName.IsNotEmptyOrNull()).Select(x => x.aliasName?.ToLower()).Distinct().Count() < input.tableList.Where(x => x.aliasName.IsNotEmptyOrNull()).Count())
        {
            var tableName = string.Empty;
            foreach (var it in input.tableList.Where(x => x.aliasName.IsNotEmptyOrNull()))
            {
                if (input.tableList.Where(x => x.aliasName.IsNotEmptyOrNull() && x.aliasName.ToLower().Equals(it.aliasName.ToLower())).Count() > 1)
                {
                    tableName = it.aliasName;
                    break;
                }
            }

            throw Oops.Oh(ErrorCode.D2118, tableName);
        }
        foreach (var table in input.tableList)
        {
            if (table.aliasName.IsNotEmptyOrNull())
            {
                if (systemKeyList.Contains(table.aliasName.ToUpper())) throw Oops.Oh(ErrorCode.D2117, string.Format(" {0} — {1} ", table.table, table.aliasName));
                if (re.IsMatch(table.aliasName)) throw Oops.Oh(ErrorCode.D2119, table.aliasName);
                if (!re2.IsMatch(table.aliasName)) throw Oops.Oh(ErrorCode.D2119, table.aliasName);

                aliasList.Add(new VisualAliasEntity()
                {
                    AliasName = table.aliasName,
                    TableName = table.table,
                    VisualId = id,
                });
            }

            if (table.fields.Where(x => x.aliasName.IsNotEmptyOrNull()).Select(x => x.aliasName.ToLower()).Distinct().Count() < table.fields.Where(x => x.aliasName.IsNotEmptyOrNull()).Count())
            {
                var fieldName = string.Empty;
                foreach (var it in table.fields.Where(x => x.aliasName.IsNotEmptyOrNull()))
                {
                    if (table.fields.Where(x => x.aliasName.IsNotEmptyOrNull() && x.aliasName.ToLower().Equals(it.aliasName.ToLower())).Count() > 1)
                    {
                        fieldName = it.aliasName;
                        break;
                    }
                }

                throw Oops.Oh(ErrorCode.D2116, fieldName);
            }
            foreach (var field in table.fields)
            {
                if (field.aliasName.IsNotEmptyOrNull() && systemKeyList.Contains(field.aliasName.ToUpper())) throw Oops.Oh(ErrorCode.D2117, string.Format(" {0} — {1} ", field.field, field.aliasName));
                if (field.aliasName.IsNotEmptyOrNull() && re.IsMatch(field.aliasName)) throw Oops.Oh(ErrorCode.D2119, field.aliasName);
                if (field.aliasName.IsNotEmptyOrNull() && !re2.IsMatch(field.aliasName)) throw Oops.Oh(ErrorCode.D2119, field.aliasName);

                aliasList.Add(new VisualAliasEntity()
                {
                    AliasName = field.aliasName,
                    FieldName = field.field,
                    TableName = table.table,
                    VisualId = id,
                });
            }
        }

        await _repository.AsSugarClient().Deleteable<VisualAliasEntity>().Where(x => x.VisualId.Equals(id)).ExecuteCommandAsync();
        await _repository.AsSugarClient().Insertable(aliasList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 模板数据聚合.
    /// </summary>
    /// <param name="fileName">生成ZIP文件名.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="aliasList">表字段别名.</param>
    /// <returns></returns>
    private async Task TemplatesDataAggregation(string fileName, VisualDevEntity templateEntity, List<VisualAliasEntity> aliasList)
    {
        // 类型名称
        var categoryName = (await _dictionaryDataService.GetInfo(templateEntity.Category)).EnCode;

        // 表关系
        List<DbTableRelationModel>? tableRelation = templateEntity.Tables.ToObject<List<DbTableRelationModel>>();

        // 表单数据
        var formDataModel = templateEntity.FormData.ToObjectOld<FormDataModel>();

        // 列表属性
        ColumnDesignModel? pcColumnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        ColumnDesignModel? appColumnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
        pcColumnDesignModel ??= new ColumnDesignModel() { btnsList = new List<ButtonConfigModel>(), columnBtnsList = new List<ButtonConfigModel>(), customBtnsList = new List<ButtonConfigModel>() };
        appColumnDesignModel ??= new ColumnDesignModel() { btnsList = new List<ButtonConfigModel>(), columnBtnsList = new List<ButtonConfigModel>(), customBtnsList = new List<ButtonConfigModel>() };

        // 既是行内编辑又是纯表单 强制改成普通列表.
        if (templateEntity.WebType.Equals(1) && pcColumnDesignModel.type.Equals(4))
        {
            var columnData = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
            columnData["type"] = 1;
            templateEntity.ColumnData = columnData.ToJsonString();
        }

        // 分组和树形表格去掉复杂表头
        if (pcColumnDesignModel.type.Equals(3) || pcColumnDesignModel.type.Equals(5))
        {
            var columnData = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
            columnData["complexHeaderList"] = new List<object>();
            templateEntity.ColumnData = columnData.ToJsonString();
        }

        if (pcColumnDesignModel.columnList != null)
        {
            pcColumnDesignModel.columnList = pcColumnDesignModel.columnList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.DATECALCULATE && x.__config__.jnpfKey != JnpfKeyConst.CALCULATE).ToList();
            pcColumnDesignModel.defaultColumnList = pcColumnDesignModel.defaultColumnList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.DATECALCULATE && x.__config__.jnpfKey != JnpfKeyConst.CALCULATE).ToList();
            pcColumnDesignModel.searchList = pcColumnDesignModel.searchList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.DATECALCULATE && x.__config__.jnpfKey != JnpfKeyConst.CALCULATE).ToList();
            templateEntity.ColumnData = pcColumnDesignModel.ToJsonString();
        }

        if (appColumnDesignModel.columnList != null)
        {
            appColumnDesignModel.columnList = appColumnDesignModel.columnList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.DATECALCULATE && x.__config__.jnpfKey != JnpfKeyConst.CALCULATE).ToList();
            appColumnDesignModel.defaultColumnList = appColumnDesignModel.defaultColumnList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.DATECALCULATE && x.__config__.jnpfKey != JnpfKeyConst.CALCULATE).ToList();
            appColumnDesignModel.searchList = appColumnDesignModel.searchList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.DATECALCULATE && x.__config__.jnpfKey != JnpfKeyConst.CALCULATE).ToList();
            templateEntity.AppColumnData = appColumnDesignModel.ToJsonString();
        }

        // 开启数据权限
        bool useDataPermission = false;

        if (pcColumnDesignModel.useDataPermission && appColumnDesignModel.useDataPermission)
        {
            useDataPermission = true;
        }
        else if (!pcColumnDesignModel.useDataPermission && appColumnDesignModel.useDataPermission)
        {
            useDataPermission = true;
        }
        else if (pcColumnDesignModel.useDataPermission && !appColumnDesignModel.useDataPermission)
        {
            useDataPermission = true;
        }
        else
        {
            useDataPermission = false;
        }

        switch (templateEntity.WebType)
        {
            case 1:
                useDataPermission = false;
                break;
        }

        // 剔除多余布局控件组
        var controls = TemplateAnalysis.AnalysisTemplateData(formDataModel.fields);
        var fieldsCopy = formDataModel.fields.ToJsonString().ToObject<List<FieldsModel>>();
        TemplateAnalysis.DataFormatReplace(controls);

        switch (templateEntity.WebType)
        {
            case 1:
                break;
            default:
                // 统一处理下表单内控件
                controls = CodeGenUnifiedHandlerHelper.UnifiedHandlerFormDataModel(controls, pcColumnDesignModel, appColumnDesignModel);
                controls = CodeGenUnifiedHandlerHelper.UnifiedHandlerControlRelationship(controls);
                break;
        }

        List<string> targetPathList = new List<string>();
        List<string> templatePathList = new List<string>();

        string tableName = string.Empty;
        CodeGenConfigModel codeGenConfigModel = new CodeGenConfigModel();

        // 主表代码生成配置模型
        CodeGenConfigModel codeGenMainTableConfigModel = new CodeGenConfigModel();

        // 子表表名和主键.
        var ctPrimaryKey = new Dictionary<string, string>();

        /*
         * 区分是纯主表、主带副、主带子、主带副与子
         * 1-纯主表、2-主带子、3-主带副、4-主带副与子
         * 生成模式
         * 因ORM原因 导航查询 一对多 列表查询
         * 不能使用ORM 自带函数 待作者开放.Select()
         * 导致一对多列表查询转换必须全使用子查询
         * 远端数据与静态数据无法列表转换所以全部ThenMapper内转换
         * 数据字典又分为两种值转换ID与EnCode
         */
        var modelType = JudgmentGenerationModel(tableRelation, controls);
        if (templateEntity.WebType.Equals(4)) modelType = GeneratePatterns.DataView;
        var defaultLink = _databaseManager.GetTenantDbLink(string.Empty, string.Empty);
        switch (modelType)
        {
            case GeneratePatterns.DataView:
                {
                    templateEntity.Tables = string.Empty;
                    var viewDataModel = templateEntity.FormData.ToObject<FormDataModel>();
                    codeGenConfigModel.NameSpace = viewDataModel.areasName;
                    codeGenConfigModel.BusName = templateEntity.FullName;
                    codeGenConfigModel.ClassName = viewDataModel.className.FirstOrDefault();
                    targetPathList = CodeGenTargetPathHelper.BackendDataViewTargetPathList(formDataModel.className.FirstOrDefault(), fileName);
                    templatePathList = CodeGenTargetPathHelper.BackendDataViewTemplatePathList("6-DataView");

                    var tContent = File.ReadAllText(templatePathList.First());
                    var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                        Id = templateEntity.Id,
                        NameSpace = codeGenConfigModel.NameSpace,
                        BusName = codeGenConfigModel.BusName,
                        ClassName = codeGenConfigModel.ClassName,
                    });
                    var dirPath = new DirectoryInfo(targetPathList.First()).Parent.FullName;
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);
                    File.WriteAllText(targetPathList.First(), tResult, Encoding.UTF8);
                }

                break;
            case GeneratePatterns.MainBelt:
                {
                    var link = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(m => m.Id == templateEntity.DbLinkId && m.DeleteMark == null);
                    var targetLink = link ?? _databaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

                    List<CodeGenTableRelationsModel> tableRelationsList = new List<CodeGenTableRelationsModel>();

                    var tableNo = 0;

                    // 生成子表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "0"))
                    {
                        tableNo++;
                        var controlId = string.Empty;
                        var children = controls.Find(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.tableName.Equals(item.table));
                        if (children == null) continue;
                        controlId = children.__vModel__;
                        if (children != null) controls = children.__config__.children;

                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);
                        KingbaseNetType(fieldList, targetLink);
                        fieldList = CodeGenWay.GetTableFieldModelReName(item.table, fieldList, aliasList);
                        ctPrimaryKey.Add(item.table, fieldList.Find(x => x.primaryKey).field);

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 默认主表开启自增子表也需要开启自增
                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.ChildTableBackEnd(item.table, item.className, fieldList, controls, templateEntity, controlId, item.tableField);
                        codeGenConfigModel.IsMapper = true;
                        codeGenConfigModel.BusName = children.__config__.label;
                        codeGenConfigModel.ClassName = item.className;

                        targetPathList = CodeGenTargetPathHelper.BackendChildTableTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.Type, codeGenConfigModel.IsMapper, codeGenConfigModel.IsShowSubTableField);
                        templatePathList = CodeGenTargetPathHelper.BackendChildTableTemplatePathList("SubTable", templateEntity.WebType, templateEntity.Type, codeGenConfigModel.IsMapper, codeGenConfigModel.IsShowSubTableField);

                        codeGenConfigModel.TableField = TableFieldOrderByLinkage(codeGenConfigModel.TableField);

                        // 生成子表相关文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            if (pcColumnDesignModel.btnsList == null) pcColumnDesignModel.btnsList = new List<ButtonConfigModel>();
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                Id = templateEntity.Id,
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                IsMainTable = false,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                NameSpace = formDataModel.areasName,
                                PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                                MainClassName = codeGenConfigModel.ClassName,
                                OriginalMainTableName = item.table,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsMapper = codeGenConfigModel.IsMapper,
                                WebType = templateEntity.WebType,
                                Type = templateEntity.Type,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.IsImportData,
                                EnableFlow = templateEntity.EnableFlow == 0 ? false : true,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = false,
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        tableRelationsList.Add(new CodeGenTableRelationsModel
                        {
                            ClassName = item.className,
                            OriginalTableName = item.table,
                            RelationTable = item.relationTable,
                            TableName = item.table.ParseToPascalCase(),
                            isNumLimit = children.isNumLimit,
                            numLimit = children.numLimit,
                            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
                            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
                            RelationField = codeGenConfigModel.PrimaryKey.ToLower() == item.relationField.ToLower() ? "id" : item.relationField.ToUpperCase(),
                            OriginalRelationField = item.relationField,
                            ControlTableComment = codeGenConfigModel.BusName,
                            TableComment = item.tableName,
                            ChilderColumnConfigList = codeGenConfigModel.TableField,
                            ChilderColumnConfigListCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && !it.ForeignKeyField && it.jnpfKey != null).Count(),
                            TableNo = tableNo,
                            ControlModel = controlId,
                            IsQueryWhether = codeGenConfigModel.TableField.Any(it => it.QueryWhether),
                            IsShowField = codeGenConfigModel.TableField.Any(it => it.IsShow),
                            IsUnique = codeGenConfigModel.TableField.Any(it => it.IsUnique),
                            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
                            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
                            IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
                        });

                        // 还原全部控件
                        controls = TemplateAnalysis.AnalysisTemplateData(formDataModel.fields);
                    }

                    // 将子表第一个创建人、修改人、所属岗位 切到最后面.
                    foreach (var item in tableRelationsList)
                    {
                        var tempColumnList = new List<TableColumnConfigModel>();
                        foreach (var it in item.ChilderColumnConfigList.Where(x => x.jnpfKey.IsNotEmptyOrNull()))
                        {
                            if (it.jnpfKey != null && (it.jnpfKey.Equals(JnpfKeyConst.CREATEUSER) || it.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER) || it.jnpfKey.Equals(JnpfKeyConst.CURRPOSITION)))
                            {
                                tempColumnList.Add(it);
                            }
                            else
                            {
                                break;
                            }
                        }

                        item.ChilderColumnConfigList.RemoveAll(x => tempColumnList.Contains(x));
                        item.ChilderColumnConfigList.AddRange(tempColumnList);
                    }

                    // 生成主表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "1"))
                    {
                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);
                        KingbaseNetType(fieldList, targetLink);
                        fieldList = CodeGenWay.GetTableFieldModelReName(item.table, fieldList, aliasList, true);
                        var pField = fieldList.Find(x => x.primaryKey && !x.field.ToLower().Equals("f_tenant_id"));
                        foreach (var subItem in tableRelationsList)
                        {
                            if (subItem.RelationField.ToLower().Equals(pField.field.ToLower()) && pField.reName.IsNotEmptyOrNull()) subItem.RelationField = pField.reName;
                            var reItem = fieldList.Find(x => x.field.ToLower().Equals(subItem.RelationField.ToLower()) && x.reName.IsNotEmptyOrNull());
                            if (reItem != null) subItem.RelationField = reItem.reName;
                        }

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 开启乐观锁
                        if (formDataModel.concurrencyLock && !fieldList.Any(it => it.field.ToLower().Equals("f_version")))
                            throw Oops.Oh(ErrorCode.D2107);

                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_id")))
                            throw Oops.Oh(ErrorCode.D2105);

                        // 列表带流程 或者 流程表单 自增ID
                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_task_id")))
                            throw Oops.Oh(ErrorCode.D2108);

                        if (formDataModel.logicalDelete && (!fieldList.Any(it => it.field.ToLower().Equals("f_delete_mark")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_time")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_user_id"))))
                            throw Oops.Oh(ErrorCode.D2110);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.MainBeltBackEnd(item.table, fieldList, controls, templateEntity);
                        codeGenConfigModel.IsMapper = true;
                        codeGenConfigModel.BusName = item.tableName;
                        codeGenConfigModel.TableRelations = tableRelationsList;
                        codeGenConfigModel.IsChildConversion = tableRelationsList.Any(it => it.IsConversion);
                        switch (templateEntity.WebType)
                        {
                            case 1:
                                switch (templateEntity.Type)
                                {
                                    case 3:
                                        targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(item.className, fileName, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("2-MainBelt", codeGenConfigModel.IsMapper);
                                        break;
                                    default:
                                        targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("2-MainBelt", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                        break;
                                }
                                break;
                            case 2:
                                switch (codeGenConfigModel.TableType)
                                {
                                    case 4:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList("2-MainBelt", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }

                                        break;
                                    default:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(item.className, fileName, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("2-MainBelt", codeGenConfigModel.IsMapper);
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("2-MainBelt", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }

                                        break;
                                }
                                break;
                        }

                        codeGenConfigModel.TableField = TableFieldOrderByLinkage(codeGenConfigModel.TableField);

                        // 生成后端文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            string tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                Id = templateEntity.Id,
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                NameSpace = codeGenConfigModel.NameSpace,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                PrimaryKey = codeGenConfigModel.PrimaryKey,
                                LowerPrimaryKey = codeGenConfigModel.LowerPrimaryKey,
                                OriginalPrimaryKey = codeGenConfigModel.OriginalPrimaryKey,
                                MainTable = codeGenConfigModel.MainTable,
                                LowerMainTable = codeGenConfigModel.LowerMainTable,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                hasPage = codeGenConfigModel.hasPage && !codeGenConfigModel.TableType.Equals(3),
                                Function = codeGenConfigModel.Function,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                TableFieldCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && it.jnpfKey != null).Count(),
                                DefaultSidx = codeGenConfigModel.DefaultSidx,
                                IsExport = codeGenConfigModel.IsExport,
                                IsBatchRemove = codeGenConfigModel.IsBatchRemove,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsTableRelations = codeGenConfigModel.IsTableRelations,
                                IsMapper = codeGenConfigModel.IsMapper,
                                IsSystemControl = codeGenConfigModel.IsSystemControl,
                                IsUpdate = codeGenConfigModel.IsUpdate,
                                IsBillRule = codeGenConfigModel.IsBillRule,
                                DbLinkId = codeGenConfigModel.DbLinkId,
                                FormId = codeGenConfigModel.FormId,
                                WebType = codeGenConfigModel.WebType,
                                Type = codeGenConfigModel.Type,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsMainTable = codeGenConfigModel.IsMainTable,
                                EnCode = codeGenConfigModel.EnCode,
                                UseDataPermission = useDataPermission,
                                SearchControlNum = codeGenConfigModel.SearchControlNum,
                                IsAuxiliaryTable = codeGenConfigModel.IsAuxiliaryTable,
                                ExportField = codeGenConfigModel.ExportField,
                                TableRelations = codeGenConfigModel.TableRelations,
                                ConfigId = _userManager.TenantId,
                                DBName = _userManager.TenantDbName,
                                PcUseDataPermission = pcColumnDesignModel.useDataPermission ? "true" : "false",
                                AppUseDataPermission = appColumnDesignModel.useDataPermission ? "true" : "false",
                                FullName = codeGenConfigModel.FullName,
                                IsConversion = codeGenConfigModel.IsConversion,
                                IsDetailConversion = codeGenConfigModel.IsDetailConversion,
                                HasSuperQuery = codeGenConfigModel.HasSuperQuery,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                ConcurrencyLock = codeGenConfigModel.ConcurrencyLock,
                                IsUnique = codeGenConfigModel.IsUnique || codeGenConfigModel.TableRelations.Any(it => it.IsUnique),
                                BusinessKeyList = codeGenConfigModel.BusinessKeyList,
                                BusinessKeyTip = formDataModel.businessKeyTip,
                                IsChildConversion = codeGenConfigModel.IsChildConversion,
                                IsChildIndexShow = codeGenConfigModel.TableRelations.Any(it => it.IsShowField),
                                GroupField = codeGenConfigModel.GroupField,
                                GroupShowField = codeGenConfigModel.GroupShowField,
                                IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.IsImportData,
                                ImportColumnField = CodeGenExportFieldHelper.ImportColumnField(templateEntity, codeGenConfigModel, targetLink),
                                DefaultDbName = CodeGenExportFieldHelper.GetDefaultDbNameByDbType(defaultLink),
                                ParsJnpfKeyConstList = codeGenConfigModel.ParsJnpfKeyConstList,
                                ParsJnpfKeyConstListDetails = codeGenConfigModel.ParsJnpfKeyConstListDetails,
                                ImportDataType = codeGenConfigModel.ImportDataType,
                                DataRuleJson = CodeGenControlsAttributeHelper.GetDataRuleList(templateEntity, codeGenConfigModel),
                                IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                                IsTreeTable = codeGenConfigModel.IsTreeTable,
                                ParentField = codeGenConfigModel.ParentField,
                                TreeShowField = codeGenConfigModel.TreeShowField,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = _tenant.MultiTenancy && _tenant.MultiTenancyType.Equals("COLUMN"),
                                PcKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "pc"),
                                AppKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "app"),
                                PcDefaultSortConfig = pcColumnDesignModel.defaultSortConfig != null && pcColumnDesignModel.defaultSortConfig.Any(),
                                AppDefaultSortConfig = appColumnDesignModel.defaultSortConfig != null && appColumnDesignModel.defaultSortConfig.Any(),
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        controls = TemplateAnalysis.AnalysisTemplateData(formDataModel.fields);

                        codeGenMainTableConfigModel = codeGenConfigModel;
                    }
                }

                break;
            case GeneratePatterns.MainBeltVice:
                {
                    var link = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(m => m.Id == templateEntity.DbLinkId && m.DeleteMark == null);
                    var targetLink = link ?? _databaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

                    List<CodeGenTableRelationsModel> tableRelationsList = new List<CodeGenTableRelationsModel>();

                    // 副表表字段配置
                    List<TableColumnConfigModel> auxiliaryTableColumnList = new List<TableColumnConfigModel>();

                    var tableNo = 0;
                    tableName = tableRelation.Find(it => it.typeId == "1").table;

                    // 生成副表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "0"))
                    {
                        tableNo++;
                        var auxiliaryControls = controls.FindAll(it => it.__config__.tableName == item.table);
                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);
                        KingbaseNetType(fieldList, targetLink);
                        fieldList = CodeGenWay.GetTableFieldModelReName(item.table, fieldList, aliasList);

                        // 默认主表开启自增副表也需要开启自增
                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                        {
                            throw Oops.Oh(ErrorCode.D2109);
                        }

                        codeGenConfigModel = CodeGenWay.AuxiliaryTableBackEnd(item.table, fieldList, auxiliaryControls, templateEntity, tableNo, 0, item.tableField);
                        codeGenConfigModel.IsMapper = true;
                        codeGenConfigModel.BusName = item.tableName;
                        codeGenConfigModel.ClassName = item.className;

                        targetPathList = CodeGenTargetPathHelper.BackendAuxiliaryTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.Type, templateEntity.EnableFlow);
                        templatePathList = CodeGenTargetPathHelper.BackendAuxiliaryTemplatePathList("3-Auxiliary", templateEntity.WebType, templateEntity.Type, templateEntity.EnableFlow);

                        codeGenConfigModel.TableField.ForEach(items =>
                        {
                            items.ClassName = item.className;
                        });

                        codeGenConfigModel.TableField = TableFieldOrderByLinkage(codeGenConfigModel.TableField);

                        // 生成副表相关文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                Id = templateEntity.Id,
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                IsMainTable = false,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                NameSpace = formDataModel.areasName,
                                PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                                AuxiliaryTable = item.table.ParseToPascalCase(),
                                MainTable = tableName.ParseToPascalCase(),
                                MainClassName = codeGenConfigModel.ClassName,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsMapper = true,
                                WebType = templateEntity.WebType,
                                Type = templateEntity.Type,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.IsImportData,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                IsTenantColumn = false,
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        var count = controls.Count(x => x.__vModel__.Contains(item.table));
                        var foreignKeyFieldDataType = string.Format(".ToObject<{0}>()", codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).NetType.Replace("?", ""));
                        tableRelationsList.Add(new CodeGenTableRelationsModel
                        {
                            ClassName = codeGenConfigModel.ClassName,
                            OriginalTableName = item.table,
                            RelationTable = item.relationTable,
                            TableName = item.table.ParseToPascalCase(),
                            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
                            ChilderColumnConfigList = codeGenConfigModel.TableField,
                            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
                            RelationField = codeGenConfigModel.PrimaryKey.ToLower() == item.relationField.ToLower() ? "id" : item.relationField.ToUpperCase(),
                            OriginalRelationField = item.relationField,
                            TableComment = item.tableName,
                            TableNo = tableNo,
                            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
                            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
                            IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
                            IsSystemControl = codeGenConfigModel.TableField.Any(it => it.IsSystemControl),
                            IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
                            FieldCount = count,
                            ForeignKeyFieldDataType = foreignKeyFieldDataType.Equals(".ToObject<string>()") ? string.Empty : foreignKeyFieldDataType,
                        });

                        auxiliaryTableColumnList.AddRange(codeGenConfigModel.TableField.FindAll(it => it.jnpfKey != null));
                    }

                    // 生成主表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "1"))
                    {
                        var fieldList = _databaseManager.GetFieldList(targetLink, tableName);
                        KingbaseNetType(fieldList, targetLink);
                        fieldList = CodeGenWay.GetTableFieldModelReName(item.table, fieldList, aliasList, true);
                        var pField = fieldList.Find(x => x.primaryKey && !x.field.ToLower().Equals("f_tenant_id"));
                        foreach (var auxItem in tableRelationsList)
                        {
                            if (auxItem.RelationField.ToLower().Equals(pField.field.ToLower()) && pField.reName.IsNotEmptyOrNull()) auxItem.RelationField = pField.reName;
                            var reItem = fieldList.Find(x => x.field.ToLower().Equals(auxItem.RelationField.ToLower()) && x.reName.IsNotEmptyOrNull());
                            if (reItem != null) auxItem.RelationField = reItem.reName;
                        }

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 开启乐观锁
                        if (formDataModel.concurrencyLock && !fieldList.Any(it => it.field.ToLower().Equals("f_version")))
                            throw Oops.Oh(ErrorCode.D2107);

                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_id")))
                            throw Oops.Oh(ErrorCode.D2105);

                        // 列表带流程 或者 流程表单 自增ID
                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_task_id")))
                            throw Oops.Oh(ErrorCode.D2108);

                        if (formDataModel.logicalDelete && (!fieldList.Any(it => it.field.ToLower().Equals("f_delete_mark")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_time")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_user_id"))))
                            throw Oops.Oh(ErrorCode.D2110);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.MainBeltViceBackEnd(item.table, fieldList, auxiliaryTableColumnList, controls, templateEntity);
                        codeGenConfigModel.IsMapper = true;

                        switch (templateEntity.WebType)
                        {
                            case 1:
                                switch (templateEntity.Type)
                                {
                                    case 3:
                                        targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(codeGenConfigModel.ClassName, fileName, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("4-MainBeltVice", codeGenConfigModel.IsMapper);
                                        break;
                                    default:
                                        targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("4-MainBeltVice", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                        break;
                                }
                                break;
                            case 2:
                                switch (codeGenConfigModel.TableType)
                                {
                                    case 4:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList("4-MainBeltVice", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }
                                        break;
                                    default:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(codeGenConfigModel.ClassName, fileName, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("4-MainBeltVice", codeGenConfigModel.IsMapper);
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("4-MainBeltVice", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }

                        codeGenConfigModel.TableField = TableFieldOrderByLinkage(codeGenConfigModel.TableField);

                        for (var i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                Id = templateEntity.Id,
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                NameSpace = codeGenConfigModel.NameSpace,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                PrimaryKey = codeGenConfigModel.PrimaryKey,
                                LowerPrimaryKey = codeGenConfigModel.LowerPrimaryKey,
                                OriginalPrimaryKey = codeGenConfigModel.OriginalPrimaryKey,
                                MainTable = codeGenConfigModel.MainTable,
                                LowerMainTable = codeGenConfigModel.LowerMainTable,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                hasPage = codeGenConfigModel.hasPage && !codeGenConfigModel.TableType.Equals(3),
                                Function = codeGenConfigModel.Function,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                TableFieldCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && it.jnpfKey != null).Count(),
                                DefaultSidx = codeGenConfigModel.DefaultSidx,
                                IsExport = codeGenConfigModel.IsExport,
                                IsBatchRemove = codeGenConfigModel.IsBatchRemove,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsTableRelations = codeGenConfigModel.IsTableRelations,
                                IsMapper = codeGenConfigModel.IsMapper,
                                IsBillRule = codeGenConfigModel.IsBillRule,
                                DbLinkId = codeGenConfigModel.DbLinkId,
                                FormId = codeGenConfigModel.FormId,
                                WebType = codeGenConfigModel.WebType,
                                Type = codeGenConfigModel.Type,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsMainTable = codeGenConfigModel.IsMainTable,
                                EnCode = codeGenConfigModel.EnCode,
                                UseDataPermission = useDataPermission,
                                SearchControlNum = codeGenConfigModel.SearchControlNum,
                                IsAuxiliaryTable = codeGenConfigModel.IsAuxiliaryTable,
                                ExportField = codeGenConfigModel.ExportField,
                                ConfigId = _userManager.TenantId,
                                DBName = _userManager.TenantDbName,
                                PcUseDataPermission = pcColumnDesignModel.useDataPermission ? "true" : "false",
                                AppUseDataPermission = appColumnDesignModel.useDataPermission ? "true" : "false",
                                AuxiliayTableRelations = tableRelationsList,
                                FullName = codeGenConfigModel.FullName,
                                IsConversion = codeGenConfigModel.IsConversion,
                                IsDetailConversion = codeGenConfigModel.IsDetailConversion,
                                IsMainConversion = codeGenConfigModel.TableField.Any(it => it.IsAuxiliary.Equals(false) && it.IsConversion.Equals(true)),
                                IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate.Equals(true) && it.IsAuxiliary.Equals(false) && it.jnpfKey != null),
                                HasSuperQuery = codeGenConfigModel.HasSuperQuery,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                ConcurrencyLock = codeGenConfigModel.ConcurrencyLock,
                                IsUnique = codeGenConfigModel.IsUnique,
                                BusinessKeyList = codeGenConfigModel.BusinessKeyList,
                                BusinessKeyTip = formDataModel.businessKeyTip,
                                GroupField = codeGenConfigModel.GroupField,
                                GroupShowField = codeGenConfigModel.GroupShowField,
                                IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.IsImportData,
                                ImportColumnField = CodeGenExportFieldHelper.ImportColumnField(templateEntity, codeGenConfigModel, targetLink),
                                DefaultDbName = CodeGenExportFieldHelper.GetDefaultDbNameByDbType(defaultLink),
                                ParsJnpfKeyConstList = codeGenConfigModel.ParsJnpfKeyConstList,
                                ParsJnpfKeyConstListDetails = codeGenConfigModel.ParsJnpfKeyConstListDetails,
                                ImportDataType = codeGenConfigModel.ImportDataType,
                                DataRuleJson = CodeGenControlsAttributeHelper.GetDataRuleList(templateEntity, codeGenConfigModel),
                                IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                                IsTreeTable = codeGenConfigModel.IsTreeTable,
                                ParentField = codeGenConfigModel.ParentField,
                                TreeShowField = codeGenConfigModel.TreeShowField,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = _tenant.MultiTenancy && _tenant.MultiTenancyType.Equals("COLUMN"),
                                PcKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "pc"),
                                AppKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "app"),
                                PcDefaultSortConfig = pcColumnDesignModel.defaultSortConfig != null && pcColumnDesignModel.defaultSortConfig.Any(),
                                AppDefaultSortConfig = appColumnDesignModel.defaultSortConfig != null && appColumnDesignModel.defaultSortConfig.Any(),
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);

                            codeGenMainTableConfigModel = codeGenConfigModel;
                        }
                    }
                }

                break;
            case GeneratePatterns.PrimarySecondary:
                {
                    var link = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(m => m.Id == templateEntity.DbLinkId && m.DeleteMark == null);
                    var targetLink = link ?? _databaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

                    // 解析子表
                    var subTable = new List<DbTableRelationModel>();
                    var secondaryTable = new List<DbTableRelationModel>();
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "0"))
                    {
                        // 解析子表比副表效率
                        // 先获取出子表 其他的默认为副表
                        switch (controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.tableName.Equals(item.table)))
                        {
                            case true:
                                subTable.Add(item);
                                break;
                            default:
                                secondaryTable.Add(item);
                                break;
                        }
                    }

                    List<CodeGenTableRelationsModel> subTableRelationsList = new List<CodeGenTableRelationsModel>();
                    List<CodeGenTableRelationsModel> secondaryTableRelationsList = new List<CodeGenTableRelationsModel>();

                    int tableNo = 1;

                    // 已经区分子表与副表
                    // 生成子表
                    foreach (DbTableRelationModel? item in subTable)
                    {
                        var controlId = string.Empty;
                        var children = controls.Find(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.tableName.Equals(item.table));
                        if (children == null) continue;
                        controlId = children.__vModel__;
                        if (children != null) controls = children.__config__.children;

                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);
                        KingbaseNetType(fieldList, targetLink);
                        fieldList = CodeGenWay.GetTableFieldModelReName(item.table, fieldList, aliasList);
                        ctPrimaryKey.Add(item.table, fieldList.Find(x => x.primaryKey).field);

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 默认主表开启自增子表也需要开启自增
                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.ChildTableBackEnd(item.table, item.className, fieldList, controls, templateEntity, controlId, item.tableField);
                        codeGenConfigModel.IsMapper = true;
                        codeGenConfigModel.BusName = children.__config__.label;
                        codeGenConfigModel.ClassName = item.className;

                        targetPathList = CodeGenTargetPathHelper.BackendChildTableTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.Type, codeGenConfigModel.IsMapper, codeGenConfigModel.IsShowSubTableField);
                        templatePathList = CodeGenTargetPathHelper.BackendChildTableTemplatePathList("SubTable", templateEntity.WebType, templateEntity.Type, codeGenConfigModel.IsMapper, codeGenConfigModel.IsShowSubTableField);

                        codeGenConfigModel.TableField = TableFieldOrderByLinkage(codeGenConfigModel.TableField);

                        // 生成子表相关文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                Id = templateEntity.Id,
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                IsMainTable = false,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                NameSpace = formDataModel.areasName,
                                PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                                MainClassName = codeGenConfigModel.ClassName,
                                OriginalMainTableName = item.table,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsMapper = codeGenConfigModel.IsMapper,
                                WebType = templateEntity.WebType,
                                Type = templateEntity.Type,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.IsImportData,
                                EnableFlow = templateEntity.EnableFlow == 0 ? false : true,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = false,
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        subTableRelationsList.Add(new CodeGenTableRelationsModel
                        {
                            ClassName = item.className,
                            OriginalTableName = item.table,
                            RelationTable = item.relationTable,
                            isNumLimit = children.isNumLimit,
                            numLimit = children.numLimit,
                            TableName = item.table.ParseToPascalCase(),
                            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
                            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
                            RelationField = codeGenConfigModel.PrimaryKey.ToLower() == item.relationField.ToLower() ? "id" : item.relationField.ToUpperCase(),
                            OriginalRelationField = item.relationField,
                            ControlTableComment = codeGenConfigModel.BusName,
                            TableComment = item.tableName,
                            ChilderColumnConfigList = codeGenConfigModel.TableField,
                            ChilderColumnConfigListCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && !it.ForeignKeyField && it.jnpfKey != null).Count(),
                            TableNo = tableNo,
                            ControlModel = controlId,
                            IsQueryWhether = codeGenConfigModel.TableField.Any(it => it.QueryWhether),
                            IsShowField = codeGenConfigModel.TableField.Any(it => it.IsShow),
                            IsUnique = codeGenConfigModel.TableField.Any(it => it.IsUnique),
                            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
                            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
                            IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
                        });
                        tableNo++;

                        // 还原全部控件
                        controls = TemplateAnalysis.AnalysisTemplateData(formDataModel.fields);
                    }

                    // 副表表字段配置
                    List<TableColumnConfigModel> auxiliaryTableColumnList = new List<TableColumnConfigModel>();

                    // 生成副表
                    foreach (DbTableRelationModel? item in secondaryTable)
                    {
                        var auxiliaryControls = controls.FindAll(it => it.__config__.tableName == item.table);
                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);
                        KingbaseNetType(fieldList, targetLink);
                        fieldList = CodeGenWay.GetTableFieldModelReName(item.table, fieldList, aliasList);

                        // 默认主表开启自增副表也需要开启自增
                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        codeGenConfigModel = CodeGenWay.AuxiliaryTableBackEnd(item.table, fieldList, auxiliaryControls, templateEntity, tableNo, 1, item.tableField);
                        codeGenConfigModel.IsMapper = true;
                        codeGenConfigModel.BusName = item.tableName;
                        codeGenConfigModel.ClassName = item.className;

                        targetPathList = CodeGenTargetPathHelper.BackendAuxiliaryTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.Type, templateEntity.EnableFlow);
                        templatePathList = CodeGenTargetPathHelper.BackendAuxiliaryTemplatePathList("3-Auxiliary", templateEntity.WebType, templateEntity.Type, templateEntity.EnableFlow);

                        codeGenConfigModel.TableField.ForEach(items => items.ClassName = item.className);
                        codeGenConfigModel.TableField = TableFieldOrderByLinkage(codeGenConfigModel.TableField);

                        // 生成副表相关文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                Id = templateEntity.Id,
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                IsMainTable = false,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                NameSpace = formDataModel.areasName,
                                PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                                AuxiliaryTable = item.table.ParseToPascalCase(),
                                MainTable = tableName.ParseToPascalCase(),
                                MainClassName = codeGenConfigModel.ClassName,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsMapper = true,
                                WebType = templateEntity.WebType,
                                Type = templateEntity.Type,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.IsImportData,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                IsTenantColumn = false,
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        var count = controls.Count(x => x.__vModel__.Contains(item.table));
                        var foreignKeyFieldDataType = string.Format(".ToObject<{0}>()", codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).NetType.Replace("?", ""));
                        secondaryTableRelationsList.Add(new CodeGenTableRelationsModel
                        {
                            ClassName = codeGenConfigModel.ClassName,
                            OriginalTableName = item.table,
                            RelationTable = item.relationTable,
                            TableName = item.table.ParseToPascalCase(),
                            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
                            ChilderColumnConfigList = codeGenConfigModel.TableField,
                            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
                            RelationField = codeGenConfigModel.PrimaryKey.ToLower() == item.relationField.ToLower() ? "id" : item.relationField.ToUpperCase(),
                            OriginalRelationField = item.relationField,
                            TableComment = item.tableName,
                            TableNo = tableNo,
                            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
                            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
                            IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
                            IsSystemControl = codeGenConfigModel.TableField.Any(it => it.IsSystemControl),
                            IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
                            FieldCount = count,
                            ForeignKeyFieldDataType = foreignKeyFieldDataType.Equals(".ToObject<string>()") ? string.Empty : foreignKeyFieldDataType,
                        });

                        auxiliaryTableColumnList.AddRange(codeGenConfigModel.TableField.FindAll(it => it.jnpfKey != null));
                    }

                    // 将子表第一个创建人、修改人、所属岗位 切到最后面.
                    foreach (var item in subTableRelationsList)
                    {
                        var tempColumnList = new List<TableColumnConfigModel>();
                        foreach (var it in item.ChilderColumnConfigList.Where(x => x.jnpfKey.IsNotEmptyOrNull()))
                        {
                            if (it.jnpfKey != null && (it.jnpfKey.Equals(JnpfKeyConst.CREATEUSER) || it.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER) || it.jnpfKey.Equals(JnpfKeyConst.CURRPOSITION)))
                            {
                                tempColumnList.Add(it);
                            }
                            else
                            {
                                break;
                            }
                        }

                        item.ChilderColumnConfigList.RemoveAll(x => tempColumnList.Contains(x));
                        item.ChilderColumnConfigList.AddRange(tempColumnList);
                    }

                    // 解析主表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "1"))
                    {
                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);
                        KingbaseNetType(fieldList, targetLink);
                        fieldList = CodeGenWay.GetTableFieldModelReName(item.table, fieldList, aliasList, true);
                        var pField = fieldList.Find(x => x.primaryKey && !x.field.ToLower().Equals("f_tenant_id"));
                        foreach (var subItem in subTableRelationsList)
                        {
                            if (subItem.RelationField.ToLower().Equals(pField.field.ToLower()) && pField.reName.IsNotEmptyOrNull()) subItem.RelationField = pField.reName;
                            var reItem = fieldList.Find(x => x.field.ToLower().Equals(subItem.RelationField.ToLower()) && x.reName.IsNotEmptyOrNull());
                            if (reItem != null) subItem.RelationField = reItem.reName;
                        }

                        foreach (var auxItem in secondaryTableRelationsList)
                        {
                            if (auxItem.RelationField.ToLower().Equals(pField.field.ToLower()) && pField.reName.IsNotEmptyOrNull()) auxItem.RelationField = pField.reName;
                            var reItem = fieldList.Find(x => x.field.ToLower().Equals(auxItem.RelationField.ToLower()) && x.reName.IsNotEmptyOrNull());
                            if (reItem != null) auxItem.RelationField = reItem.reName;
                        }

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 开启乐观锁
                        if (formDataModel.concurrencyLock && !fieldList.Any(it => it.field.ToLower().Equals("f_version")))
                            throw Oops.Oh(ErrorCode.D2107);

                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_id")))
                            throw Oops.Oh(ErrorCode.D2105);

                        // 列表带流程 或者 流程表单 自增ID
                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_task_id")))
                            throw Oops.Oh(ErrorCode.D2108);

                        if (formDataModel.logicalDelete && (!fieldList.Any(it => it.field.ToLower().Equals("f_delete_mark")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_time")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_user_id"))))
                            throw Oops.Oh(ErrorCode.D2110);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.PrimarySecondaryBackEnd(item.table, fieldList, auxiliaryTableColumnList, controls, templateEntity);

                        codeGenConfigModel.IsMapper = true;
                        codeGenConfigModel.BusName = tableRelation.Find(it => it.typeId.Equals("1")).tableName;
                        codeGenConfigModel.TableRelations = subTableRelationsList;
                        codeGenConfigModel.IsChildConversion = subTableRelationsList.Any(it => it.IsConversion);

                        switch (templateEntity.WebType)
                        {
                            case 1:
                                switch (templateEntity.Type)
                                {
                                    case 3:
                                        targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(item.className, fileName, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("5-PrimarySecondary", codeGenConfigModel.IsMapper);
                                        break;
                                    default:
                                        targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("5-PrimarySecondary", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                        break;
                                }
                                break;
                            case 2:
                                switch (codeGenConfigModel.TableType)
                                {
                                    case 4:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList("5-PrimarySecondary", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }

                                        break;
                                    default:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(item.className, fileName, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("5-PrimarySecondary", codeGenConfigModel.IsMapper);
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("5-PrimarySecondary", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }

                                        break;
                                }
                                break;
                        }

                        codeGenConfigModel.TableField = TableFieldOrderByLinkage(codeGenConfigModel.TableField);

                        // 生成后端文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            string tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                Id = templateEntity.Id,
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                NameSpace = codeGenConfigModel.NameSpace,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                PrimaryKey = codeGenConfigModel.PrimaryKey,
                                LowerPrimaryKey = codeGenConfigModel.LowerPrimaryKey,
                                OriginalPrimaryKey = codeGenConfigModel.OriginalPrimaryKey,
                                MainTable = codeGenConfigModel.MainTable,
                                LowerMainTable = codeGenConfigModel.LowerMainTable,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                hasPage = codeGenConfigModel.hasPage && !codeGenConfigModel.TableType.Equals(3),
                                Function = codeGenConfigModel.Function,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                TableFieldCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && it.jnpfKey != null).Count(),
                                DefaultSidx = codeGenConfigModel.DefaultSidx,
                                IsExport = codeGenConfigModel.IsExport,
                                IsBatchRemove = codeGenConfigModel.IsBatchRemove,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsTableRelations = codeGenConfigModel.IsTableRelations,
                                IsMapper = codeGenConfigModel.IsMapper,
                                IsBillRule = codeGenConfigModel.IsBillRule,
                                DbLinkId = codeGenConfigModel.DbLinkId,
                                FormId = codeGenConfigModel.FormId,
                                WebType = codeGenConfigModel.WebType,
                                Type = codeGenConfigModel.Type,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsMainTable = codeGenConfigModel.IsMainTable,
                                EnCode = codeGenConfigModel.EnCode,
                                UseDataPermission = useDataPermission,
                                SearchControlNum = codeGenConfigModel.SearchControlNum,
                                IsAuxiliaryTable = codeGenConfigModel.IsAuxiliaryTable,
                                ExportField = codeGenConfigModel.ExportField,
                                TableRelations = codeGenConfigModel.TableRelations,
                                ConfigId = _userManager.TenantId,
                                DBName = _userManager.TenantDbName,
                                PcUseDataPermission = pcColumnDesignModel.useDataPermission ? "true" : "false",
                                AppUseDataPermission = appColumnDesignModel.useDataPermission ? "true" : "false",
                                AuxiliayTableRelations = secondaryTableRelationsList,
                                FullName = codeGenConfigModel.FullName,
                                IsConversion = codeGenConfigModel.IsConversion,
                                IsDetailConversion = codeGenConfigModel.IsDetailConversion,
                                HasSuperQuery = codeGenConfigModel.HasSuperQuery,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                ConcurrencyLock = codeGenConfigModel.ConcurrencyLock,
                                IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate.Equals(true) && it.IsAuxiliary.Equals(false) && it.jnpfKey != null),
                                IsUnique = codeGenConfigModel.IsUnique || codeGenConfigModel.TableRelations.Any(it => it.IsUnique),
                                BusinessKeyList = codeGenConfigModel.BusinessKeyList,
                                BusinessKeyTip = formDataModel.businessKeyTip,
                                IsChildConversion = codeGenConfigModel.IsChildConversion,
                                IsChildIndexShow = codeGenConfigModel.TableRelations.Any(it => it.IsShowField),
                                GroupField = codeGenConfigModel.GroupField,
                                GroupShowField = codeGenConfigModel.GroupShowField,
                                IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.IsImportData,
                                ImportColumnField = CodeGenExportFieldHelper.ImportColumnField(templateEntity, codeGenConfigModel, targetLink),
                                DefaultDbName = CodeGenExportFieldHelper.GetDefaultDbNameByDbType(defaultLink),
                                ParsJnpfKeyConstList = codeGenConfigModel.ParsJnpfKeyConstList,
                                ParsJnpfKeyConstListDetails = codeGenConfigModel.ParsJnpfKeyConstListDetails,
                                ImportDataType = codeGenConfigModel.ImportDataType,
                                DataRuleJson = CodeGenControlsAttributeHelper.GetDataRuleList(templateEntity, codeGenConfigModel),
                                IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                                IsTreeTable = codeGenConfigModel.IsTreeTable,
                                ParentField = codeGenConfigModel.ParentField,
                                TreeShowField = codeGenConfigModel.TreeShowField,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = _tenant.MultiTenancy && _tenant.MultiTenancyType.Equals("COLUMN"),
                                PcKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "pc"),
                                AppKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "app"),
                                PcDefaultSortConfig = pcColumnDesignModel.defaultSortConfig != null && pcColumnDesignModel.defaultSortConfig.Any(),
                                AppDefaultSortConfig = appColumnDesignModel.defaultSortConfig != null && appColumnDesignModel.defaultSortConfig.Any(),
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);

                            codeGenMainTableConfigModel = codeGenConfigModel;
                        }
                    }
                }

                break;
            default:
                {
                    tableName = tableRelation.FirstOrDefault().table;
                    var link = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(m => m.Id == templateEntity.DbLinkId && m.DeleteMark == null);
                    var targetLink = link ?? _databaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
                    // 获取表结构
                    var fieldList = _databaseManager.GetFieldList(targetLink, tableName);
                    KingbaseNetType(fieldList, targetLink);
                    fieldList = CodeGenWay.GetTableFieldModelReName(tableName, fieldList, aliasList, true);

                    if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                    // 开启乐观锁
                    if (formDataModel.concurrencyLock && !fieldList.Any(it => it.field.ToLower().Equals("f_version")))
                        throw Oops.Oh(ErrorCode.D2107);

                    if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                        throw Oops.Oh(ErrorCode.D2109);

                    if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_id")))
                        throw Oops.Oh(ErrorCode.D2105);

                    // 列表带流程 或者 流程表单 自增ID
                    if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_task_id")))
                        throw Oops.Oh(ErrorCode.D2108);

                    if (formDataModel.logicalDelete && (!fieldList.Any(it => it.field.ToLower().Equals("f_delete_mark")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_time")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_user_id"))))
                        throw Oops.Oh(ErrorCode.D2110);

                    // 后端生成
                    codeGenConfigModel = CodeGenWay.SingleTableBackEnd(tableName, fieldList, controls, templateEntity);
                    codeGenConfigModel.IsMapper = true;

                    switch (templateEntity.WebType)
                    {
                        case 1:
                            switch (templateEntity.Type)
                            {
                                case 3:
                                    targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(codeGenConfigModel.ClassName, fileName, codeGenConfigModel.IsMapper);
                                    templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("1-SingleTable", codeGenConfigModel.IsMapper);
                                    break;
                                default:
                                    targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                    templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("1-SingleTable", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                    break;
                            }

                            break;
                        case 2:
                            switch (codeGenConfigModel.TableType)
                            {
                                case 4:
                                    switch (templateEntity.Type)
                                    {
                                        // 流程表单没有行内编辑
                                        case 3:
                                            break;
                                        default:
                                            targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                            templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList("1-SingleTable", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                            break;
                                    }
                                    break;
                                default:
                                    switch (templateEntity.Type)
                                    {
                                        case 3:
                                            targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(codeGenConfigModel.ClassName, fileName, codeGenConfigModel.IsMapper);
                                            templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("1-SingleTable", codeGenConfigModel.IsMapper);
                                            break;
                                        default:
                                            targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                            templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("1-SingleTable", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                            break;
                                    }
                                    break;
                            }
                            break;
                    }

                    codeGenConfigModel.TableField = TableFieldOrderByLinkage(codeGenConfigModel.TableField);

                    // 生成后端文件
                    for (var i = 0; i < templatePathList.Count; i++)
                    {
                        var tContent = File.ReadAllText(templatePathList[i]);
                        var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                            Id = templateEntity.Id,
                            IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                            IsMainTable = true,
                            NameSpace = codeGenConfigModel.NameSpace,
                            BusName = codeGenConfigModel.BusName,
                            ClassName = codeGenConfigModel.ClassName,
                            PrimaryKey = codeGenConfigModel.PrimaryKey,
                            LowerPrimaryKey = codeGenConfigModel.LowerPrimaryKey,
                            OriginalPrimaryKey = codeGenConfigModel.OriginalPrimaryKey,
                            MainTable = codeGenConfigModel.MainTable,
                            LowerMainTable = codeGenConfigModel.LowerMainTable,
                            OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                            hasPage = codeGenConfigModel.hasPage && !codeGenConfigModel.TableType.Equals(3),
                            Function = codeGenConfigModel.Function,
                            TableField = codeGenConfigModel.TableField,
                            RelationsField = codeGenConfigModel.RelationsField,
                            TableFieldCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && it.jnpfKey != null).Count(),
                            DefaultSidx = codeGenConfigModel.DefaultSidx,
                            IsExport = codeGenConfigModel.IsExport,
                            IsBatchRemove = codeGenConfigModel.IsBatchRemove,
                            IsUploading = codeGenConfigModel.IsUpload,
                            IsTableRelations = codeGenConfigModel.IsTableRelations,
                            IsMapper = codeGenConfigModel.IsMapper,
                            IsSystemControl = codeGenConfigModel.IsSystemControl,
                            IsUpdate = codeGenConfigModel.IsUpdate,
                            IsBillRule = codeGenConfigModel.IsBillRule,
                            DbLinkId = codeGenConfigModel.DbLinkId,
                            FormId = codeGenConfigModel.FormId,
                            WebType = codeGenConfigModel.WebType,
                            Type = codeGenConfigModel.Type,
                            EnableFlow = codeGenConfigModel.EnableFlow,
                            EnCode = codeGenConfigModel.EnCode,
                            UseDataPermission = useDataPermission,
                            SearchControlNum = codeGenConfigModel.SearchControlNum,
                            ExportField = codeGenConfigModel.ExportField,
                            ConfigId = _userManager.TenantId,
                            DBName = _userManager.TenantDbName,
                            PcUseDataPermission = pcColumnDesignModel.useDataPermission ? "true" : "false",
                            AppUseDataPermission = appColumnDesignModel.useDataPermission ? "true" : "false",
                            FullName = codeGenConfigModel.FullName,
                            IsConversion = codeGenConfigModel.IsConversion,
                            IsDetailConversion = codeGenConfigModel.IsDetailConversion,
                            HasSuperQuery = codeGenConfigModel.HasSuperQuery,
                            PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                            ConcurrencyLock = codeGenConfigModel.ConcurrencyLock,
                            IsUnique = codeGenConfigModel.IsUnique,
                            BusinessKeyList = codeGenConfigModel.BusinessKeyList,
                            BusinessKeyTip = formDataModel.businessKeyTip,
                            GroupField = codeGenConfigModel.GroupField,
                            GroupShowField = codeGenConfigModel.GroupShowField,
                            IsImportData = pcColumnDesignModel.btnsList.Any(x => x.value.Equals("upload") && x.show) && codeGenConfigModel.IsImportData,
                            ImportColumnField = CodeGenExportFieldHelper.ImportColumnField(templateEntity, codeGenConfigModel, targetLink),
                            DefaultDbName = CodeGenExportFieldHelper.GetDefaultDbNameByDbType(defaultLink),
                            ParsJnpfKeyConstList = codeGenConfigModel.ParsJnpfKeyConstList,
                            ParsJnpfKeyConstListDetails = codeGenConfigModel.ParsJnpfKeyConstListDetails,
                            ImportDataType = codeGenConfigModel.ImportDataType,
                            DataRuleJson = CodeGenControlsAttributeHelper.GetDataRuleList(templateEntity, codeGenConfigModel),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsTreeTable = codeGenConfigModel.IsTreeTable,
                            ParentField = codeGenConfigModel.ParentField,
                            TreeShowField = codeGenConfigModel.TreeShowField,
                            IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                            TableType = codeGenConfigModel.TableType,
                            IsTenantColumn = _tenant.MultiTenancy && _tenant.MultiTenancyType.Equals("COLUMN"),
                            PcKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "pc"),
                            AppKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "app"),
                            PcDefaultSortConfig = pcColumnDesignModel.defaultSortConfig != null && pcColumnDesignModel.defaultSortConfig.Any(),
                            AppDefaultSortConfig = appColumnDesignModel.defaultSortConfig != null && appColumnDesignModel.defaultSortConfig.Any(),
                        });
                        var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
                        File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                    }

                    codeGenMainTableConfigModel = codeGenConfigModel;
                }

                break;
        }

        // 强行将文件夹名称定义成 下载代码中输出配置的功能类名
        tableName = formDataModel.className.FirstOrDefault();

        // 还原模板
        controls = TemplateAnalysis.AnalysisTemplateData(fieldsCopy);

        // 捞取子表主键
        if (modelType.Equals(GeneratePatterns.MainBelt) || modelType.Equals(GeneratePatterns.PrimarySecondary))
        {
            foreach (var item in controls)
            {
                if (item.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                    item.TablePrimaryKey = ctPrimaryKey[item.__config__.tableName].ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            }
        }

        // 生成前端
        await GenFrondEnd(tableName.ToLowerCase(), codeGenConfigModel.DefaultSidx, formDataModel, controls, codeGenMainTableConfigModel.TableField, templateEntity, fileName, aliasList);
    }

    /// <summary>
    /// 预览代码.
    /// </summary>
    /// <param name="codePath"></param>
    /// <returns></returns>
    private List<Dictionary<string, object>> PriviewCode(string codePath)
    {
        var dataList = FileHelper.GetAllFiles(codePath);
        List<Dictionary<string, string>> datas = new List<Dictionary<string, string>>();
        List<Dictionary<string, object>> allDatas = new List<Dictionary<string, object>>();
        foreach (var item in dataList)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            var content = FileHelper.FileToString(item.FullName);

            switch (item.Extension)
            {
                case ".cs":
                    {
                        string fileName = item.FullName.ToLower();
                        if (fileName.Contains("listqueryinput") || fileName.Contains("crinput") || fileName.Contains("upinput") || fileName.Contains("upoutput") || fileName.Contains("listoutput") || fileName.Contains("infooutput") || fileName.Contains("detailoutput") || fileName.Contains("inlineeditoroutput"))
                        {
                            data.Add("folderName", "dto");
                        }
                        else if (fileName.Contains("mapper"))
                        {
                            data.Add("folderName", "mapper");
                        }
                        else if (fileName.Contains("entity"))
                        {
                            data.Add("folderName", "entity");
                        }
                        else
                        {
                            data.Add("folderName", "dotnet");
                        }

                        data.Add("fileName", item.Name);

                        data.Add("fileContent", content);
                        data.Add("fileType", item.Extension.Replace(".", string.Empty));
                        datas.Add(data);
                    }
                    break;
                case ".fff":
                    {
                        data.Add("folderName", "fff");
                        data.Add("id", SnowflakeIdHelper.NextId());
                        data.Add("fileName", item.Name);

                        data.Add("fileContent", content);
                        data.Add("fileType", item.Extension.Replace(".", string.Empty));
                        datas.Add(data);
                    }
                    break;
                case ".vue":
                case ".js":
                case ".ts":
                    {
                        if (item.FullName.ToLower().Contains("pc"))
                            data.Add("folderName", "web");
                        else if (item.FullName.ToLower().Contains("app"))
                            data.Add("folderName", "app");

                        data.Add("id", SnowflakeIdHelper.NextId());
                        data.Add("fileName", item.Name);

                        data.Add("fileContent", content);
                        data.Add("fileType", item.Extension.Replace(".", string.Empty));
                        datas.Add(data);
                    }
                    break;
            }
        }

        // datas 集合去重
        foreach (var item in datas.GroupBy(d => d["folderName"]).Select(d => d.First()).OrderBy(d => d["folderName"]).ToList())
        {
            Dictionary<string, object> dataMap = new Dictionary<string, object>();
            dataMap["fileName"] = item["folderName"];
            dataMap["id"] = SnowflakeIdHelper.NextId();
            dataMap["children"] = datas.FindAll(d => d["folderName"] == item["folderName"]);
            allDatas.Add(dataMap);
        }

        return allDatas;
    }

    /// <summary>
    /// 判断生成模式.
    /// </summary>
    /// <returns>1-纯主表、2-主带子、3-主带副、4-主带副与子.</returns>
    private GeneratePatterns JudgmentGenerationModel(List<DbTableRelationModel> tableRelation, List<FieldsModel> controls)
    {
        // 默认纯主表
        var codeModel = GeneratePatterns.PrimaryTable;

        // 找副表控件
        if (tableRelation.Count > 1 && controls.Any(x => x.__vModel__.Contains("_jnpf_")) && controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
            codeModel = GeneratePatterns.PrimarySecondary;
        else if (tableRelation.Count > 1 && controls.Any(x => x.__vModel__.Contains("_jnpf_")))
            codeModel = GeneratePatterns.MainBeltVice;
        else if (tableRelation.Count > 1 && controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
            codeModel = GeneratePatterns.MainBelt;
        switch (codeModel)
        {
            case GeneratePatterns.MainBelt:
                // 在子表模式下 设计子表控件数量对不上表扣除主表后数量 强制定义为主子副模式
                if (controls.Count(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) < tableRelation.Count - 1)
                {
                    codeModel = GeneratePatterns.PrimarySecondary;
                }
                break;
        }

        return codeModel;
    }

    /// <summary>
    /// 生成前端.
    /// </summary>
    /// <param name="tableName">表名称.</param>
    /// <param name="defaultSidx">默认排序.</param>
    /// <param name="formDataModel">表单JSON包.</param>
    /// <param name="controls">移除布局控件后的控件列表.</param>
    /// <param name="tableColumns">表字段.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="fileName">文件夹名称.</param>
    private async Task GenFrondEnd(string tableName, string defaultSidx, FormDataModel formDataModel, List<FieldsModel> controls, List<TableColumnConfigModel> tableColumns, VisualDevEntity templateEntity, string fileName, List<VisualAliasEntity> aliasList)
    {
        var categoryName = (await _dictionaryDataService.GetInfo(templateEntity.Category)).EnCode;
        List<string> targetPathList = new List<string>();
        List<string> templatePathList = new List<string>();

        FrontEndGenConfigModel frondEndGenConfig = new FrontEndGenConfigModel();
        switch (_userManager.VueVersion)
        {
            case 3:
                {
                    CodeGenFrontEndConfigModel frontEndGenConfig = new CodeGenFrontEndConfigModel();

                    // 前端生成 APP与PC合并 4-pc,5-app
                    foreach (int logic in new List<int> { 4 })
                    {
                        // 每次循环前重新定义表单数据
                        formDataModel = templateEntity.FormData.ToObject<FormDataModel>();

                        frontEndGenConfig = CodeGenWay.CodeGenFrontEndEngine(logic, formDataModel, controls, tableColumns, templateEntity);
                        frontEndGenConfig.EnableFlow = templateEntity.EnableFlow;
                        frontEndGenConfig.BasicInfo.MianTable = tableName;
                        frontEndGenConfig.BasicInfo.CreatorUserId = _userManager.UserId;
                        frontEndGenConfig.BasicInfo.AliasListJson = aliasList.ToJsonString();

                        if (templateEntity.WebType.Equals(4))
                        {
                            targetPathList = CodeGenTargetPathHelper.FrontEndTargetPathList(tableName, fileName, templateEntity.WebType, frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                            templatePathList = CodeGenTargetPathHelper.FrontEndTemplatePathList(templateEntity.WebType, frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                        }
                        else
                        {
                            switch (templateEntity.Type)
                            {
                                case 3:
                                    {
                                        targetPathList = CodeGenTargetPathHelper.FlowFrontEndTargetPathList(logic, tableName, fileName);
                                        templatePathList = CodeGenTargetPathHelper.FlowFrontEndTemplatePathList(logic, _userManager.VueVersion);
                                    }

                                    break;
                                default:
                                    {
                                        switch (logic)
                                        {
                                            case 4:
                                                var columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                                                switch (templateEntity.WebType)
                                                {
                                                    case 1:
                                                        frontEndGenConfig.TableConfig.HasSuperQuery = false;
                                                        frontEndGenConfig.HasSearch = false;
                                                        frontEndGenConfig.Type = 1;
                                                        break;
                                                }

                                                switch (frontEndGenConfig.Type)
                                                {
                                                    case 4:
                                                        targetPathList = CodeGenTargetPathHelper.FrontEndInlineEditorTargetPathList(tableName, fileName, frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                                                        templatePathList = CodeGenTargetPathHelper.FrontEndInlineEditorTemplatePathList(frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                                                        break;
                                                    default:
                                                        targetPathList = CodeGenTargetPathHelper.FrontEndTargetPathList(tableName, fileName, templateEntity.WebType, frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                                                        templatePathList = CodeGenTargetPathHelper.FrontEndTemplatePathList(templateEntity.WebType, frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                                                        break;
                                                }
                                                break;
                                        }
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            if (templatePathList[i].Contains("columnList.ts"))
                            {
                                var columnObj = templateEntity.ColumnData?.ToObjectOld<Dictionary<string, object>>().GetValueOrDefault("columnList");
                                var cList = new List<Dictionary<string, object>>();
                                foreach (var it in columnObj.ToObject<List<Dictionary<string, object>>>()) if (!it["jnpfKey"].Equals(JnpfKeyConst.CALCULATE)) cList.Add(it);
                                frontEndGenConfig.ColumnList = JsonConvert.SerializeObject(cList, Formatting.Indented);
                            }
                            if (templatePathList[i].Contains("searchList.ts"))
                            {
                                var columnObj = templateEntity.ColumnData?.ToObjectOld<Dictionary<string, object>>().GetValueOrDefault("searchList");
                                var cList = new List<Dictionary<string, object>>();
                                foreach (var it in columnObj.ToObject<List<Dictionary<string, object>>>()) if (!it["jnpfKey"].Equals(JnpfKeyConst.CALCULATE)) cList.Add(it);
                                frontEndGenConfig.SearchList = JsonConvert.SerializeObject(cList, Formatting.Indented);
                            }

                            var tResult = _viewEngine.RunCompileFromCached(tContent, frontEndGenConfig, builderAction: builder =>
                            {
                                builder.AddUsing("JNPF.Engine.Entity.Model.CodeGen");
                                builder.AddAssemblyReferenceByName("JNPF.Engine.Entity");
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }
                    }

                    foreach (int logic in new List<int> { 5 })
                    {
                        // 每次循环前重新定义表单数据
                        formDataModel = templateEntity.FormData.ToObject<FormDataModel>();

                        frondEndGenConfig = CodeGenWay.SingleTableFrontEnd(logic, formDataModel, controls, tableColumns, templateEntity);
                        frontEndGenConfig = CodeGenWay.CodeGenFrontEndEngine(logic, formDataModel, controls, tableColumns, templateEntity);
                        frontEndGenConfig.BasicInfo.MianTable = tableName;
                        frontEndGenConfig.BasicInfo.CreatorUserId = _userManager.UserId;
                        frontEndGenConfig.BasicInfo.AliasListJson = aliasList.ToJsonString();

                        if (templateEntity.WebType.Equals(4))
                        {
                            targetPathList = CodeGenTargetPathHelper.AppFrontEndTargetPathList(tableName, fileName, templateEntity.WebType, frondEndGenConfig.IsDetail);
                            templatePathList = CodeGenTargetPathHelper.AppFrontEndTemplatePathList(templateEntity.WebType, frondEndGenConfig.IsDetail, true);
                        }
                        else
                        {
                            switch (templateEntity.Type)
                            {
                                case 3:
                                    {
                                        targetPathList = CodeGenTargetPathHelper.FlowFrontEndTargetPathList(logic, tableName, fileName);
                                        templatePathList = CodeGenTargetPathHelper.FlowFrontEndTemplatePathList(logic, _userManager.VueVersion);
                                    }

                                    break;
                                default:
                                    {
                                        switch (logic)
                                        {
                                            case 4:
                                                var columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                                                var hasSuperQuery = false;
                                                switch (templateEntity.WebType)
                                                {
                                                    case 1:
                                                        hasSuperQuery = false;
                                                        frondEndGenConfig.Type = 1;
                                                        break;
                                                    default:
                                                        hasSuperQuery = columnDesignModel.hasSuperQuery;
                                                        break;
                                                }

                                                switch (frondEndGenConfig.Type)
                                                {
                                                    case 4:
                                                        targetPathList = CodeGenTargetPathHelper.FrontEndInlineEditorTargetPathList(tableName, fileName, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                        templatePathList = CodeGenTargetPathHelper.FrontEndInlineEditorTemplatePathList(templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                        break;
                                                    default:
                                                        targetPathList = CodeGenTargetPathHelper.FrontEndTargetPathList(tableName, fileName, templateEntity.WebType, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                        templatePathList = CodeGenTargetPathHelper.FrontEndTemplatePathList(templateEntity.WebType, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                        break;
                                                }
                                                break;
                                            case 5:
                                                switch (templateEntity.EnableFlow)
                                                {
                                                    case 0:
                                                        targetPathList = CodeGenTargetPathHelper.AppFrontEndTargetPathList(tableName, fileName, templateEntity.WebType, frondEndGenConfig.IsDetail);
                                                        templatePathList = CodeGenTargetPathHelper.AppFrontEndTemplatePathList(templateEntity.WebType, frondEndGenConfig.IsDetail, true);
                                                        break;
                                                    case 1:
                                                        targetPathList = CodeGenTargetPathHelper.AppFrontEndWorkflowTargetPathList(tableName, fileName, templateEntity.WebType);
                                                        templatePathList = CodeGenTargetPathHelper.AppFrontEndWorkflowTemplatePathList(templateEntity.WebType, true);
                                                        break;
                                                }
                                                break;
                                        }
                                    }

                                    break;
                            }
                        }

                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            if (templatePathList[i].Contains("columnList.js"))
                            {
                                var columnObj = templateEntity.AppColumnData?.ToObjectOld<Dictionary<string, object>>().GetValueOrDefault("columnList");
                                var cList = new List<Dictionary<string, object>>();
                                foreach (var it in columnObj.ToObject<List<Dictionary<string, object>>>()) if (!it["jnpfKey"].Equals(JnpfKeyConst.CALCULATE)) cList.Add(it);
                                frondEndGenConfig.ColumnList = JsonConvert.SerializeObject(cList, Formatting.Indented);
                            }
                            if (templatePathList[i].Contains("searchList.js"))
                            {
                                var columnObj = templateEntity.AppColumnData?.ToObjectOld<Dictionary<string, object>>().GetValueOrDefault("searchList");
                                var cList = new List<Dictionary<string, object>>();
                                foreach (var it in columnObj.ToObject<List<Dictionary<string, object>>>()) if (!it["jnpfKey"].Equals(JnpfKeyConst.CALCULATE)) cList.Add(it);
                                frontEndGenConfig.SearchList = JsonConvert.SerializeObject(cList, Formatting.Indented);
                            }
                            string tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                EnableFlow = templateEntity.EnableFlow,
                                NameSpace = frondEndGenConfig.NameSpace,
                                ClassName = frondEndGenConfig.ClassName,
                                FormRef = frondEndGenConfig.FormRef,
                                FormModel = frondEndGenConfig.FormModel,
                                Size = frondEndGenConfig.Size,
                                LabelPosition = frondEndGenConfig.LabelPosition,
                                LabelWidth = frondEndGenConfig.LabelWidth,
                                FormRules = frondEndGenConfig.FormRules,
                                GeneralWidth = frondEndGenConfig.GeneralWidth,
                                FullScreenWidth = frondEndGenConfig.FullScreenWidth,
                                DrawerWidth = frondEndGenConfig.DrawerWidth,
                                FormStyle = frondEndGenConfig.FormStyle,
                                Type = frondEndGenConfig.Type,
                                TreeRelation = frondEndGenConfig.TreeRelation,
                                TreeSelectType = frondEndGenConfig.TreeSelectType,
                                TreeAbleIds = frondEndGenConfig.TreeAbleIds,
                                TreeJnpfKey = frondEndGenConfig.TreeJnpfKey,
                                TreeTitle = frondEndGenConfig.TreeTitle,
                                TreePropsValue = frondEndGenConfig.TreePropsValue,
                                TreeDataSource = frondEndGenConfig.TreeDataSource,
                                TreeDictionary = frondEndGenConfig.TreeDictionary,
                                TreePropsUrl = frondEndGenConfig.TreePropsUrl,
                                TreePropsLabel = frondEndGenConfig.TreePropsLabel,
                                TreePropsChildren = frondEndGenConfig.TreePropsChildren,
                                TreeRelationControlKey = frondEndGenConfig.TreeRelationControlKey,
                                IsTreeRelationMultiple = frondEndGenConfig.IsTreeRelationMultiple,
                                IsExistQuery = frondEndGenConfig.IsExistQuery,
                                PrimaryKey = frondEndGenConfig.PrimaryKey,
                                FormList = frondEndGenConfig.FormList,
                                PopupType = frondEndGenConfig.PopupType,
                                SearchColumnDesign = frondEndGenConfig.SearchColumnDesign,
                                IsKeywordSearchColumn = frondEndGenConfig.SearchColumnDesign?.Any(x => x.IsKeyword),
                                IsAnyDefaultSearch = frondEndGenConfig.SearchColumnDesign?.Any(x => x.DefaultValues != "undefined"),
                                SortFieldDesign = frondEndGenConfig.SortFieldDesign,
                                TopButtonDesign = frondEndGenConfig.TopButtonDesign,
                                ColumnButtonDesign = frondEndGenConfig.ColumnButtonDesign,
                                ColumnDesign = frondEndGenConfig.ColumnDesign,
                                OptionsList = frondEndGenConfig.OptionsList,
                                IsBatchRemoveDel = frondEndGenConfig.IsBatchRemoveDel,
                                IsBatchPrint = frondEndGenConfig.IsBatchPrint,
                                PrintIds = frondEndGenConfig.PrintIds,
                                IsDownload = frondEndGenConfig.IsDownload,
                                IsRemoveDel = frondEndGenConfig.IsRemoveDel,
                                IsDetail = frondEndGenConfig.IsDetail,
                                IsEdit = frondEndGenConfig.IsEdit,
                                IsAdd = frondEndGenConfig.IsAdd,
                                IsSort = frondEndGenConfig.IsSort,
                                IsUpload = frondEndGenConfig.IsUpload,
                                FormAllContols = frondEndGenConfig.FormAllContols,
                                ComplexFormAllContols = frondEndGenConfig.ComplexFormAllContols,
                                CancelButtonText = frondEndGenConfig.CancelButtonText,
                                ConfirmButtonText = frondEndGenConfig.ConfirmButtonText,
                                UseBtnPermission = frondEndGenConfig.UseBtnPermission,
                                UseColumnPermission = frondEndGenConfig.UseColumnPermission,
                                UseFormPermission = frondEndGenConfig.UseFormPermission,
                                DefaultSidx = defaultSidx,
                                WebType = templateEntity.Type == 3 ? templateEntity.Type : templateEntity.WebType,
                                HasPage = frondEndGenConfig.HasPage,
                                IsSummary = frondEndGenConfig.IsSummary,
                                AddTitleName = frondEndGenConfig.TopButtonDesign?.Find(it => it.Value.Equals("add"))?.Label,
                                EditTitleName = frondEndGenConfig.ColumnButtonDesign?.Find(it => it.Value.Equals("edit"))?.Label,
                                DetailTitleName = frondEndGenConfig.ColumnButtonDesign?.Find(it => it.Value.Equals("detail"))?.Label,
                                PageSize = frondEndGenConfig.PageSize,
                                Sort = frondEndGenConfig.Sort,
                                HasPrintBtn = frondEndGenConfig.HasPrintBtn,
                                PrintButtonText = frondEndGenConfig.PrintButtonText,
                                PrintId = frondEndGenConfig.PrintId,
                                EnCode = templateEntity.EnCode,
                                FormId = templateEntity.Id,
                                FullName = templateEntity.FullName,
                                Category = categoryName,
                                Tables = templateEntity.Tables.ToJsonString(),
                                DbLinkId = templateEntity.DbLinkId,
                                MianTable = tableName,
                                PropertyJson = frondEndGenConfig.PropertyJson.ToJsonString(),
                                CreatorTime = DateTime.Now.ParseToUnixTime(),
                                CreatorUserId = _userManager.UserId,
                                IsChildDataTransfer = frondEndGenConfig.IsChildDataTransfer,
                                IsChildTableQuery = frondEndGenConfig.IsChildTableQuery,
                                IsChildTableShow = frondEndGenConfig.IsChildTableShow,
                                ColumnList = frondEndGenConfig.ColumnList,
                                HasSuperQuery = frondEndGenConfig.HasSuperQuery,
                                ColumnOptions = frondEndGenConfig.ColumnOptions,
                                IsInlineEditor = frondEndGenConfig.IsInlineEditor,
                                IndexDataType = frondEndGenConfig.IndexDataType,
                                GroupField = frondEndGenConfig.GroupField,
                                GroupShowField = frondEndGenConfig.GroupShowField,
                                PrimaryKeyPolicy = frondEndGenConfig.PrimaryKeyPolicy,
                                IsRelationForm = frondEndGenConfig.IsRelationForm,
                                ChildTableStyle = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) ? frondEndGenConfig.ChildTableStyle : 1,
                                IsFixed = frondEndGenConfig.IsFixed,
                                IsChildrenRegular = frondEndGenConfig.IsChildrenRegular,
                                TreeSynType = frondEndGenConfig.TreeSynType,
                                HasTreeQuery = frondEndGenConfig.HasTreeQuery,
                                ColumnData = frondEndGenConfig.ColumnData.ToJsonString(),
                                SummaryField = frondEndGenConfig.SummaryField.ToJsonString(),
                                ShowSummary = frondEndGenConfig.ShowSummary,
                                DefaultFormControlList = frondEndGenConfig.DefaultFormControlList,
                                IsDefaultFormControl = frondEndGenConfig.IsDefaultFormControl,
                                FormRealControl = frondEndGenConfig.FormRealControl,
                                QueryCriteriaQueryVarianceList = frondEndGenConfig.QueryCriteriaQueryVarianceList,
                                IsDateSpecialAttribute = frondEndGenConfig.IsDateSpecialAttribute,
                                IsTimeSpecialAttribute = frondEndGenConfig.IsTimeSpecialAttribute,
                                AllThousandsField = frondEndGenConfig.AllThousandsField,
                                IsChildrenThousandsField = frondEndGenConfig.IsChildrenThousandsField,
                                SpecifyDateFormatSet = frondEndGenConfig.SpecifyDateFormatSet,
                                AppThousandField = frondEndGenConfig.AppThousandField,
                                HasConfirmAndAddBtn = frondEndGenConfig.HasConfirmAndAddBtn,
                                ConfirmAndAddText = frondEndGenConfig.ConfirmAndAddText,
                                IsDefaultSearchField = frondEndGenConfig.IsDefaultSearchField,
                                DefaultSearchList = frondEndGenConfig.DefaultSearchList,
                                ShowOverflow = frondEndGenConfig.ShowOverflow,
                                TableConfig = frontEndGenConfig.TableConfig,
                                VueVersion = _userManager.VueVersion,
                                IsTabConfig = frontEndGenConfig.IsTabConfig,
                                TabRelationField = frontEndGenConfig.TabRelationField,
                                TabConfigHasAllTab = frontEndGenConfig.TabConfigHasAllTab,
                                TabConfigDataType = frontEndGenConfig.TabConfigDataType,
                                TabDictionaryType = frontEndGenConfig.TabDictionaryType,
                                TabDataSource = frontEndGenConfig.TabDataSource,
                                ExtraOptions = frondEndGenConfig.ExtraOptions,
                                InterfaceRes = frondEndGenConfig.InterfaceRes,
                                IsNumLimitSwitch = frondEndGenConfig.IsNumLimitSwitch,
                            }, builderAction: builder =>
                            {
                                builder.AddUsing("JNPF.Engine.Entity.Model.CodeGen");
                                builder.AddAssemblyReferenceByName("JNPF.Engine.Entity");
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }
                    }
                }
                break;
            default:
                {
                    // 前端生成 APP与PC合并 4-pc,5-app
                    foreach (int logic in new List<int> { 4, 5 })
                    {
                        // 每次循环前重新定义表单数据
                        formDataModel = templateEntity.FormData.ToObject<FormDataModel>();

                        frondEndGenConfig = CodeGenWay.SingleTableFrontEnd(logic, formDataModel, controls, tableColumns, templateEntity);
                        var defaultSortConfig = "[]";
                        switch (templateEntity.Type)
                        {
                            case 3:
                                {
                                    targetPathList = CodeGenTargetPathHelper.FlowFrontEndTargetPathList(logic, tableName, fileName);
                                    templatePathList = CodeGenTargetPathHelper.FlowFrontEndTemplatePathList(logic, _userManager.VueVersion);
                                }

                                break;
                            default:
                                {
                                    switch (logic)
                                    {
                                        case 4:
                                            var columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                                            defaultSortConfig = columnDesignModel?.defaultSortConfig != null ? columnDesignModel.defaultSortConfig?.ToJsonString() : "[]";
                                            var hasSuperQuery = false;
                                            switch (templateEntity.WebType)
                                            {
                                                case 1:
                                                    hasSuperQuery = false;
                                                    frondEndGenConfig.Type = 1;
                                                    break;
                                                default:
                                                    hasSuperQuery = columnDesignModel.hasSuperQuery;
                                                    break;
                                            }

                                            switch (frondEndGenConfig.Type)
                                            {
                                                case 4:
                                                    targetPathList = CodeGenTargetPathHelper.FrontEndInlineEditorTargetPathList(tableName, fileName, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    templatePathList = CodeGenTargetPathHelper.FrontEndInlineEditorTemplatePathList(templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    break;
                                                default:
                                                    targetPathList = CodeGenTargetPathHelper.FrontEndTargetPathList(tableName, fileName, templateEntity.WebType, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    templatePathList = CodeGenTargetPathHelper.FrontEndTemplatePathList(templateEntity.WebType, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    break;
                                            }
                                            break;
                                        case 5:
                                            columnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
                                            defaultSortConfig = columnDesignModel?.defaultSortConfig != null ? columnDesignModel.defaultSortConfig?.ToJsonString() : "[]";
                                            switch (templateEntity.EnableFlow)
                                            {
                                                case 0:
                                                    targetPathList = CodeGenTargetPathHelper.AppFrontEndTargetPathList(tableName, fileName, templateEntity.WebType, frondEndGenConfig.IsDetail);
                                                    templatePathList = CodeGenTargetPathHelper.AppFrontEndTemplatePathList(templateEntity.WebType, frondEndGenConfig.IsDetail);
                                                    break;
                                                case 1:
                                                    targetPathList = CodeGenTargetPathHelper.AppFrontEndWorkflowTargetPathList(tableName, fileName, templateEntity.WebType);
                                                    templatePathList = CodeGenTargetPathHelper.AppFrontEndWorkflowTemplatePathList(templateEntity.WebType);
                                                    break;
                                            }
                                            break;
                                    }
                                }

                                break;
                        }

                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                NameSpace = frondEndGenConfig.NameSpace,
                                ClassName = frondEndGenConfig.ClassName,
                                FormRef = frondEndGenConfig.FormRef,
                                FormModel = frondEndGenConfig.FormModel,
                                Size = frondEndGenConfig.Size,
                                LabelPosition = frondEndGenConfig.LabelPosition,
                                LabelWidth = frondEndGenConfig.LabelWidth,
                                FormRules = frondEndGenConfig.FormRules,
                                GeneralWidth = frondEndGenConfig.GeneralWidth,
                                FullScreenWidth = frondEndGenConfig.FullScreenWidth,
                                DrawerWidth = frondEndGenConfig.DrawerWidth,
                                FormStyle = frondEndGenConfig.FormStyle,
                                Type = frondEndGenConfig.Type,
                                TreeRelation = frondEndGenConfig.TreeRelation,
                                TreeSelectType = frondEndGenConfig.TreeSelectType,
                                TreeAbleIds = frondEndGenConfig.TreeAbleIds,
                                TreeJnpfKey = frondEndGenConfig.TreeJnpfKey,
                                TreeTitle = frondEndGenConfig.TreeTitle,
                                TreePropsValue = frondEndGenConfig.TreePropsValue,
                                TreeDataSource = frondEndGenConfig.TreeDataSource,
                                TreeDictionary = frondEndGenConfig.TreeDictionary,
                                TreePropsUrl = frondEndGenConfig.TreePropsUrl,
                                TreePropsLabel = frondEndGenConfig.TreePropsLabel,
                                TreePropsChildren = frondEndGenConfig.TreePropsChildren,
                                TreeRelationControlKey = frondEndGenConfig.TreeRelationControlKey,
                                IsTreeRelationMultiple = frondEndGenConfig.IsTreeRelationMultiple,
                                IsExistQuery = frondEndGenConfig.IsExistQuery,
                                PrimaryKey = frondEndGenConfig.PrimaryKey,
                                FormList = frondEndGenConfig.FormList,
                                PopupType = frondEndGenConfig.PopupType,
                                SearchColumnDesign = frondEndGenConfig.SearchColumnDesign,
                                IsKeywordSearchColumn = frondEndGenConfig.SearchColumnDesign?.Any(x => x.IsKeyword),
                                IsAnyDefaultSearch = frondEndGenConfig.SearchColumnDesign?.Any(x => x.DefaultValues != "undefined"),
                                SortFieldDesign = frondEndGenConfig.SortFieldDesign,
                                TopButtonDesign = frondEndGenConfig.TopButtonDesign,
                                ColumnButtonDesign = frondEndGenConfig.ColumnButtonDesign,
                                ColumnDesign = frondEndGenConfig.ColumnDesign,
                                OptionsList = frondEndGenConfig.OptionsList,
                                IsBatchRemoveDel = frondEndGenConfig.IsBatchRemoveDel,
                                IsBatchPrint = frondEndGenConfig.IsBatchPrint,
                                PrintIds = frondEndGenConfig.PrintIds,
                                IsDownload = frondEndGenConfig.IsDownload,
                                IsRemoveDel = frondEndGenConfig.IsRemoveDel,
                                IsDetail = frondEndGenConfig.IsDetail,
                                IsEdit = frondEndGenConfig.IsEdit,
                                IsAdd = frondEndGenConfig.IsAdd,
                                IsSort = frondEndGenConfig.IsSort,
                                IsUpload = frondEndGenConfig.IsUpload,
                                FormAllContols = frondEndGenConfig.FormAllContols,
                                ComplexFormAllContols = frondEndGenConfig.ComplexFormAllContols,
                                CancelButtonText = frondEndGenConfig.CancelButtonText,
                                ConfirmButtonText = frondEndGenConfig.ConfirmButtonText,
                                UseBtnPermission = frondEndGenConfig.UseBtnPermission,
                                UseColumnPermission = frondEndGenConfig.UseColumnPermission,
                                UseFormPermission = frondEndGenConfig.UseFormPermission,
                                DefaultSidx = defaultSidx,
                                WebType = templateEntity.Type == 3 ? templateEntity.Type : templateEntity.WebType,
                                HasPage = frondEndGenConfig.HasPage,
                                IsSummary = frondEndGenConfig.IsSummary,
                                AddTitleName = frondEndGenConfig.TopButtonDesign?.Find(it => it.Value.Equals("add"))?.Label,
                                EditTitleName = frondEndGenConfig.ColumnButtonDesign?.Find(it => it.Value.Equals("edit"))?.Label,
                                DetailTitleName = frondEndGenConfig.ColumnButtonDesign?.Find(it => it.Value.Equals("detail"))?.Label,
                                PageSize = frondEndGenConfig.PageSize,
                                Sort = frondEndGenConfig.Sort,
                                HasPrintBtn = frondEndGenConfig.HasPrintBtn,
                                PrintButtonText = frondEndGenConfig.PrintButtonText,
                                PrintId = frondEndGenConfig.PrintId,
                                EnCode = templateEntity.EnCode,
                                FormId = templateEntity.Id,
                                FullName = templateEntity.FullName,
                                Category = categoryName,
                                Tables = templateEntity.Tables.ToJsonString(),
                                DbLinkId = templateEntity.DbLinkId,
                                MianTable = tableName,
                                PropertyJson = frondEndGenConfig.PropertyJson.ToJsonString(),
                                CreatorTime = DateTime.Now.ParseToUnixTime(),
                                CreatorUserId = _userManager.UserId,
                                IsChildDataTransfer = frondEndGenConfig.IsChildDataTransfer,
                                IsChildTableQuery = frondEndGenConfig.IsChildTableQuery,
                                IsChildTableShow = frondEndGenConfig.IsChildTableShow,
                                ColumnList = frondEndGenConfig.ColumnList,
                                HasSuperQuery = frondEndGenConfig.HasSuperQuery,
                                ColumnOptions = frondEndGenConfig.ColumnOptions,
                                IsInlineEditor = frondEndGenConfig.IsInlineEditor,
                                IndexDataType = frondEndGenConfig.IndexDataType,
                                GroupField = frondEndGenConfig.GroupField,
                                GroupShowField = frondEndGenConfig.GroupShowField,
                                PrimaryKeyPolicy = frondEndGenConfig.PrimaryKeyPolicy,
                                IsRelationForm = frondEndGenConfig.IsRelationForm,
                                ChildTableStyle = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) ? frondEndGenConfig.ChildTableStyle : 1,
                                IsFixed = frondEndGenConfig.IsFixed,
                                IsChildrenRegular = frondEndGenConfig.IsChildrenRegular,
                                TreeSynType = frondEndGenConfig.TreeSynType,
                                HasTreeQuery = frondEndGenConfig.HasTreeQuery,
                                ColumnData = frondEndGenConfig.ColumnData.ToJsonString(),
                                SummaryField = frondEndGenConfig.SummaryField.ToJsonString(),
                                ShowSummary = frondEndGenConfig.ShowSummary,
                                DefaultFormControlList = frondEndGenConfig.DefaultFormControlList,
                                IsDefaultFormControl = frondEndGenConfig.IsDefaultFormControl,
                                FormRealControl = frondEndGenConfig.FormRealControl,
                                QueryCriteriaQueryVarianceList = frondEndGenConfig.QueryCriteriaQueryVarianceList,
                                IsDateSpecialAttribute = frondEndGenConfig.IsDateSpecialAttribute,
                                IsTimeSpecialAttribute = frondEndGenConfig.IsTimeSpecialAttribute,
                                AllThousandsField = frondEndGenConfig.AllThousandsField,
                                IsChildrenThousandsField = frondEndGenConfig.IsChildrenThousandsField,
                                SpecifyDateFormatSet = frondEndGenConfig.SpecifyDateFormatSet,
                                AppThousandField = frondEndGenConfig.AppThousandField,
                                HasConfirmAndAddBtn = frondEndGenConfig.HasConfirmAndAddBtn,
                                ConfirmAndAddText = frondEndGenConfig.ConfirmAndAddText,
                                IsDefaultSearchField = frondEndGenConfig.IsDefaultSearchField,
                                DefaultSearchList = frondEndGenConfig.DefaultSearchList,
                                ShowOverflow = frondEndGenConfig.ShowOverflow,
                                DefaultSortConfig = defaultSortConfig,
                                VueVersion = 2,
                                IsTabConfig = frondEndGenConfig.IsTabConfig,
                                TabRelationField = frondEndGenConfig.TabRelationField,
                                TabConfigHasAllTab = frondEndGenConfig.TabConfigHasAllTab,
                                TabConfigDataType = frondEndGenConfig.TabConfigDataType,
                                TabDictionaryType = frondEndGenConfig.TabDictionaryType,
                                TabDataSource = frondEndGenConfig.TabDataSource,
                            }, builderAction: builder =>
                            {
                                builder.AddUsing("JNPF.Engine.Entity.Model.CodeGen");
                                builder.AddAssemblyReferenceByName("JNPF.Engine.Entity");
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }
                    }
                }
                break;
        }

    }

    /// <summary>
    /// 数据视图.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <returns></returns>
    private VisualDevReleaseEntity GetCodeGenDataViewEntity(VisualDevReleaseEntity templateEntity)
    {
        if (templateEntity.ColumnData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1027);
        var tableName = string.Format("mt{0}", templateEntity.Id);
        string fileName = string.Format("{0}_{1:yyyyMMddHHmmss}", templateEntity.FullName, DateTime.Now);
        templateEntity.EnableFlow = 0;

        // 列表属性
        ColumnDesignModel? pcColumnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        ColumnDesignModel? appColumnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();

        // 分组和树形表格去掉复杂表头
        if (pcColumnDesignModel.type.Equals(3))
        {
            var columnData = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
            columnData["complexHeaderList"] = new List<object>();
            templateEntity.ColumnData = columnData.ToJsonString();
        }

        templateEntity.Tables = new List<DbTableRelationModel>() { new DbTableRelationModel() { className = tableName, table = tableName, typeId = "1" } }.ToJsonString();
        templateEntity.FormData = new FormDataModel() { fields = new List<FieldsModel>() }.ToJsonString();

        return templateEntity;
    }

    private void KingbaseNetType(List<DbTableFieldModel> fields, DbLinkEntity dblink)
    {
        if (dblink.DbType.ToLower().Contains("kingbase") || dblink.DbType.ToLower().Contains("postgre"))
        {
            foreach (var item in fields.Where(x => x.dataType == "timestamp")) item.dataType = "datetime";
        }
    }

    /// <summary>
    /// 联动控件排序优先.
    /// </summary>
    /// <param name="tableField"></param>
    /// <returns></returns>
    private List<TableColumnConfigModel> TableFieldOrderByLinkage(List<TableColumnConfigModel> tableField)
    {
        var linkFieldList = new List<TableColumnConfigModel>(tableField.Where(x => x.IsLinkage).ToList());
        tableField.RemoveAll(x => x.IsLinkage);
        tableField.InsertRange(0, linkFieldList);
        return tableField;
    }
    #endregion
}