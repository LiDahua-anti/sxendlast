using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.BaseLang;
using JNPF.Systems.Entitys.Dto.System.BaseLang;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.Formula.Functions;
using SqlSugar;
using System.Data;
using System.Text.RegularExpressions;

namespace JNPF.Systems.System;

/// <summary>
/// 系统多语言.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "BaseLang", Order = 200)]
[Route("api/system/[controller]")]
public class BaseLangService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<BaseLangEntity> _repository;

    /// <summary>
    /// 用户管理器.
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
    /// 初始化一个<see cref="BaseLangService"/>类型的新实例.
    /// </summary>
    public BaseLangService(
        ISqlSugarRepository<BaseLangEntity> repository,
        IUserManager userManager,
        ICacheManager cacheManager,
        IFileManager fileManager)
    {
        _userManager = userManager;
        _repository = repository;
        _cacheManager = cacheManager;
        _fileManager = fileManager;
    }

    #region Get

    /// <summary>
    /// 获取语言列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] BaseLangListInput input)
    {
        if (input.type == 1) input.type = 2;

        var tableHead = new List<Dictionary<string, string>> { new() { ["label"] = "翻译标记", ["prop"] = "code", } };
        var languageList = await GetLanguageList();
        foreach (var language in languageList)
        {
            // 表头
            tableHead.Add(new Dictionary<string, string>()
            {
                ["label"] = language.FullName,
                ["prop"] = language.EnCode
            });
        }

        // 数据
        var allList = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.Type != 1)
            .WhereIF(input.type.IsNotEmptyOrNull(), it => it.Type == input.type)
            .OrderByDescending(it => it.CreatorTime)
            .ToListAsync();

        var groupIds = new List<string>();
        if (input.keyword.IsNotEmptyOrNull())
            groupIds = allList.FindAll(it => it.EnCode.Contains(input.keyword) || (it.FullName.IsNotEmptyOrNull() && it.FullName.Contains(input.keyword))).Select(it => it.GroupId).Distinct().ToList();
        else
            groupIds = allList.Select(it => it.GroupId).Distinct().ToList();

        var dicList = new List<Dictionary<string, object>>();
        foreach (var groupId in groupIds)
        {
            var dic = new Dictionary<string, object>();
            foreach (var data in allList.FindAll(it => it.GroupId == groupId))
            {
                dic["id"] = data.GroupId;
                dic["typeName"] = data.Type == 2 ? "服务端" : "客户端";
                dic["code"] = data.EnCode;
                dic[data.Language] = data.FullName;
            }
            dicList.Add(dic);
        }

        // 分页
        var pagination = new Dictionary<string, int>();
        pagination.Add("currentPage", input.currentPage);
        pagination.Add("pageSize", input.pageSize);
        pagination.Add("total", dicList.Count);
        dicList = dicList.Skip((input.currentPage - 1) * input.pageSize).Take(input.pageSize).ToList();

        return new { list = dicList, pagination = pagination, tableHead = tableHead };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var res = new Dictionary<string, object> { { "id", id } };
        var entityList = await _repository.GetListAsync(x => x.GroupId == id && x.DeleteMark == null);
        foreach (var entity in entityList)
        {
            if (entity.Type == 2) entity.Type = 1;
            res["enCode"] = entity.EnCode;
            res["type"] = entity.Type;
            res[entity.Language] = entity.FullName;
        }

        return res;
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("GetLanguageText/{enCode}")]
    public async Task<dynamic> GetLanguageText(string enCode)
    {
        var entityList = new List<BaseLangInfoOutput>();
        var cacheKey = string.Format("{0}-Language", _userManager.TenantId);
        if (_cacheManager.Exists(cacheKey))
        {
            entityList = _cacheManager.Get(cacheKey).ToObject<List<BaseLangInfoOutput>>();
        }
        else
        {
            entityList = (await _repository.GetListAsync(x => x.Type == 2 && x.EnabledMark == 1)).Adapt<List<BaseLangInfoOutput>>();
            _cacheManager.Set(cacheKey, entityList);
        }

        var Language = "zh-CN";
        if (App.HttpContext.Request.Headers.Keys.Contains("Accept-Language"))
            Language = !App.HttpContext.Request.Headers["Accept-Language"].ToString().Contains(Language) ? App.HttpContext.Request.Headers["Accept-Language"].ToString().Replace("_", "-") : Language;
        var entity = entityList.FirstOrDefault(x => x.Language == Language && x.enCode == enCode);
        return entity.IsNotEmptyOrNull() ? entity.fullName : string.Empty;
    }

    /// <summary>
    /// 模板下载.
    /// </summary>
    /// <returns></returns>
    [HttpGet("TemplateDownload")]
    public async Task<dynamic> TemplateDownload()
    {
        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.ModuleName = "BaseLang";
        excelconfig.Title = "填写说明:\n" +
                    "（1）翻译标记命名规则：只能输入字母、数字、点、横线和下划线，且以字母开头；\n" +
                    "（2）翻译标记全局唯一，不可重复；\n" +
                    "（3）翻译语言必须填写一项；";
        excelconfig.TitleFont = "微软雅黑";
        excelconfig.TitlePoint = 8;
        excelconfig.FileName = "翻译管理导入模板.xls";
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>
        {
            new() { Column = "enCode", ExcelColumn = "翻译标记", Required = true },
            new() { Column = "type", ExcelColumn = "翻译分类", SelectList = new List<string>() { "客户端", "服务端" }},
        };

        var languageList = await GetLanguageList();
        foreach (var lang in languageList) excelconfig.ColumnModel.Add(new() { Column = lang.EnCode, ExcelColumn = lang.FullName });

        var filePath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var fs = ExcelExportHelper<T>.ExportMemoryStream(null, excelconfig);
        await _fileManager.UploadFileByType(fs, FileVariable.TemporaryFilePath, excelconfig.FileName);
        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + filePath, "JNPF") };
    }

    /// <summary>
    /// 多语言json.
    /// </summary>
    /// <returns></returns>
    [HttpGet("LangJson")]
    public async Task<dynamic> GetLangJson()
    {
        // 多语言
        var language = App.HttpContext.Request.Headers["Accept-Language"].ToString();

        var allData = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.Language == language && it.Type == 0)
            .OrderByDescending(it => it.CreatorTime)
            .ToListAsync();

        var dicList = new Dictionary<string, string>();
        foreach (var data in allData) dicList.Add(data.EnCode, data.FullName);

        return dicList.ToJsonString();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] BaseLangCrInput input)
    {
        if (input.type == 1) input.type = 2;
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.Type != 1 && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2901);
        if (!input.map.Values.Any(it => it.IsNotEmptyOrNull()))
            throw Oops.Oh(ErrorCode.D2902);

        var entityList = new List<BaseLangEntity>();
        var groupId = SnowflakeIdHelper.NextId();
        foreach (var item in input.map)
        {
            entityList.Add(new BaseLangEntity()
            {
                GroupId = groupId,
                Type = input.type,
                EnCode = input.enCode,
                Language = item.Key,
                FullName = item.Value
            });
        }

        var isOk = await _repository.AsInsertable(entityList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
        var cacheKey = string.Format("{0}-Language", _userManager.TenantId);
        var list = (await _repository.GetListAsync(x => x.Type == 2 && x.EnabledMark == 1)).Adapt<List<BaseLangInfoOutput>>();
        if (_cacheManager.Exists(cacheKey))
        {
            _cacheManager.Del(cacheKey);
            _cacheManager.Set(cacheKey, list);
        }
        else
        {
            _cacheManager.Set(cacheKey, list);
        }
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] BaseLangUpInput input)
    {
        if (input.type == 1) input.type = 2;
        if (await _repository.IsAnyAsync(x => x.GroupId != id && x.EnCode == input.enCode && x.Type != 1 && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2901);
        if (!input.map.Values.Any(it => it.IsNotEmptyOrNull()))
            throw Oops.Oh(ErrorCode.D2902);

        try
        {
            var insert = new List<BaseLangEntity>();
            var isCr = new List<BaseLangEntity>();
            var isUp = new List<BaseLangEntity>();
            var entityList = await _repository.GetListAsync(x => x.GroupId == id && x.DeleteMark == null);
            foreach (var item in input.map)
            {
                if (entityList.IsNotEmptyOrNull())
                {
                    var entity = entityList.Find(it => it.Language == item.Key);
                    if (entity.IsNotEmptyOrNull())
                    {
                        entity.EnCode = input.enCode;
                        entity.Type = input.type;
                        entity.FullName = item.Value;
                        isUp.Add(entity);
                    }
                    else
                    {
                        var newEntity = entityList[0].Adapt<BaseLangEntity>();
                        newEntity.Id = SnowflakeIdHelper.NextId();
                        newEntity.EnCode = input.enCode;
                        newEntity.Type = input.type;
                        newEntity.Language = item.Key;
                        newEntity.FullName = item.Value;
                        newEntity.LastModifyUserId = _userManager.UserId;
                        newEntity.LastModifyTime = DateTime.Now;
                        insert.Add(newEntity);
                    }
                }
                else
                {
                    isCr.Add(new BaseLangEntity()
                    {
                        GroupId = id,
                        EnCode = input.enCode,
                        Type = input.type,
                        Language = item.Key,
                        FullName = item.Value
                    });
                }
            }

            await _repository.AsInsertable(insert).ExecuteCommandAsync();
            await _repository.AsInsertable(isCr).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            await _repository.AsUpdateable(isUp).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            var cacheKey = string.Format("{0}-Language", _userManager.TenantId);
            var list = (await _repository.GetListAsync(x => x.Type == 2 && x.EnabledMark == 1)).Adapt<List<BaseLangInfoOutput>>();
            if (_cacheManager.Exists(cacheKey))
            {
                _cacheManager.Del(cacheKey);
                _cacheManager.Set(cacheKey, list);
            }
            else
            {
                _cacheManager.Set(cacheKey, list);
            }
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.COM1001);
        }
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entityList = await _repository.GetListAsync(x => x.GroupId == id && x.DeleteMark == null);
        if (entityList == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable(entityList).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
        var cacheKey = string.Format("{0}-Language", _userManager.TenantId);
        var list = (await _repository.GetListAsync(x => x.Type == 2 && x.EnabledMark == 1)).Adapt<List<BaseLangInfoOutput>>();
        if (_cacheManager.Exists(cacheKey))
        {
            _cacheManager.Del(cacheKey);
            _cacheManager.Set(cacheKey, entityList);
        }
        else
        {
            _cacheManager.Set(cacheKey, entityList);
        }
    }

    /// <summary>
    /// 上传文件.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Uploader")]
    public async Task<dynamic> Uploader(IFormFile file)
    {
        var filePath = _fileManager.GetPathByType(string.Empty);
        var fileName = DateTime.Now.ToString("yyyyMMdd") + "_" + SnowflakeIdHelper.NextId() + Path.GetExtension(file.FileName);
        var stream = file.OpenReadStream();
        await _fileManager.UploadFileByType(stream, filePath, fileName);
        return new { name = fileName, url = string.Format("/api/File/Image/{0}/{1}", string.Empty, fileName) };
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("ImportData")]
    [UnitOfWork]
    public async Task ImportData([FromBody] BaseLangImportDataInput input)
    {
        var langList = await GetLanguageList();
        var savePath = Path.Combine(FileVariable.TemporaryFilePath, input.fileName);

        // 得到数据
        var sr = await _fileManager.GetFileStream(savePath);
        var excelData = ExcelImportHelper.ToDataTable(savePath, sr, 0, 1);
        if (excelData.DefaultView.Count > 1000) throw Oops.Oh(ErrorCode.D1423);

        // 验证表头
        var fileEncode = new List<ExportImportHelperModel>
        {
            new() { ColumnKey = "enCode", ColumnValue = "翻译标记", Required = true },
            new() { ColumnKey = "type", ColumnValue = "翻译分类", Required = true }
        };
        foreach (var lang in langList) fileEncode.Add(new ExportImportHelperModel() { ColumnKey = lang.EnCode, ColumnValue = lang.FullName });
        for (int i = 0; i < fileEncode.Count; i++)
        {
            if (i + 1 > excelData.Columns.Count) throw Oops.Oh(ErrorCode.D1807);
            DataColumn? column = excelData.Columns[i];
            if (!(fileEncode[i].ColumnKey == column.ColumnName && fileEncode[i].ColumnValue == column.Caption.Replace("*", string.Empty)))
                throw Oops.Oh(ErrorCode.D1807);
        }

        // 处理数据
        var resData = excelData.ToJsonStringOld().ToObjectOld<List<Dictionary<string, string>>>();
        resData.Remove(resData[0]);
        var isCr = new List<BaseLangEntity>();
        var isUp = new List<BaseLangEntity>();
        foreach (var data in resData)
        {
            var regex = "^[a-zA-Z][a-zA-Z0-9._-]*$";
            if (data.ContainsKey("enCode") && data["enCode"].IsNotEmptyOrNull() && Regex.IsMatch(data["enCode"], regex) &&
                data.Any(it => it.Key != "enCode" && it.Key != "type" && it.Value.IsNotEmptyOrNull()))
            {
                var entityList = await _repository.GetListAsync(x => x.EnCode == data["enCode"] && x.DeleteMark == null);
                if (entityList.Any()) // 更新
                {
                    foreach (var item in data)
                    {
                        if (item.Key != "enCode" && item.Key != "type" && langList.ConvertAll(x => x.EnCode).Contains(item.Key))
                        {
                            var entity = entityList.Find(it => it.Language == item.Key);
                            if (entity.IsNotEmptyOrNull())
                            {
                                entity.Type = data["type"] == "服务端" ? 2 : 0;
                                entity.FullName = item.Value;
                                isUp.Add(entity);
                            }
                            else
                            {
                                isCr.Add(new BaseLangEntity()
                                {
                                    GroupId = entityList[0].GroupId,
                                    Type = data["type"] == "服务端" ? 2 : 0,
                                    Language = item.Key,
                                    FullName = item.Value
                                });
                            }
                        }
                    }
                }
                else // 新增
                {
                    var groupId = SnowflakeIdHelper.NextId();
                    foreach (var item in data)
                    {
                        if (item.Key != "enCode" && item.Key != "type" && langList.ConvertAll(x => x.EnCode).Contains(item.Key))
                        {
                            isCr.Add(new BaseLangEntity()
                            {
                                GroupId = groupId,
                                EnCode = data["enCode"],
                                Type = data["type"] == "服务端" ? 2 : 0,
                                Language = item.Key,
                                FullName = item.Value
                            });
                        }
                    }
                }
            }
        }

        await _repository.AsInsertable(isCr).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        await _repository.AsUpdateable(isUp).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        var cacheKey = string.Format("{0}-Language", _userManager.TenantId);
        var list = (await _repository.GetListAsync(x => x.Type == 2 && x.EnabledMark == 1)).Adapt<List<BaseLangInfoOutput>>();
        if (_cacheManager.Exists(cacheKey))
        {
            _cacheManager.Del(cacheKey);
            _cacheManager.Set(cacheKey, list);
        }
        else
        {
            _cacheManager.Set(cacheKey, list);
        }
    }

    #endregion

    #region Private

    /// <summary>
    /// 获取语言种类.
    /// </summary>
    /// <returns></returns>
    private async Task<List<DictionaryDataEntity>> GetLanguageList()
    {
        return await _repository.AsSugarClient().Queryable<DictionaryTypeEntity, DictionaryDataEntity>((dt, dd) => new JoinQueryInfos(JoinType.Left, dt.Id == dd.DictionaryTypeId))
            .Where((dt, dd) => dd.DeleteMark == null && dt.EnCode.Equals("Language"))
            .OrderBy((dt, dd) => dd.SortCode).OrderByDescending((dt, dd) => dd.CreatorTime)
            .Select((dt, dd) => new DictionaryDataEntity
            {
                FullName = dd.FullName,
                EnCode = dd.EnCode
            }).ToListAsync();
    }

    #endregion
}