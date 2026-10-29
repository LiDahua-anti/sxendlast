using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Document;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model.Properties;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.IO.Compression;

namespace JNPF.Systems;

/// <summary>
/// 知识管理
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[ApiDescriptionSettings(Tag = "File", Name = "Document", Order = 601)]
[Route("api/File/[controller]")]
public class DocumentService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<DocumentEntity> _repository;
    private readonly IFileManager _fileManager;
    private readonly IUserManager _userManager;
    private readonly ICacheManager _cacheManager;

    public DocumentService(ISqlSugarRepository<DocumentEntity> repository, IFileManager fileManager, IUserManager userManager, ICacheManager cacheManager)
    {
        _repository = repository;
        _fileManager = fileManager;
        _userManager = userManager;
        _cacheManager = cacheManager;
    }

    #region Get

    /// <summary>
    /// 列表（全部文档）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <param name="parentId">文档层级.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetAllList([FromQuery] KeywordInput input, string parentId)
    {
        var data = new List<DocumentListOutput>();

        if (input.keyword.IsNullOrEmpty())
        {
            data = (await _repository.AsQueryable().Where(m => m.CreatorUserId == _userManager.UserId && m.ParentId == parentId && m.EnabledMark == 1)
                .OrderBy(x => x.Type).OrderBy(x => x.CreatorTime, OrderByType.Desc)
                .ToListAsync()).Adapt<List<DocumentListOutput>>();
        }
        else
        {
            var list = new List<DocumentEntity>();
            await GetChildSrcList(parentId, list, 0, 1);
            data = (list.FindAll(it => it.Type == 1 && it.FullName.Contains(input.keyword))).Adapt<List<DocumentListOutput>>();
            foreach (var item in data) item.parentId = parentId;
        }

        string[]? typeList = new string[] { "doc", "docx", "xls", "xlsx", "ppt", "pptx", "pdf", "jpg", "jpeg", "gif", "png", "bmp", "txt" };
        foreach (var item in data)
        {
            string? type = item.fullName.Split('.').LastOrDefault();
            item.isPreview = typeList.Contains(type) ? "1" : null;
        }

        return new { list = data };
    }

    /// <summary>
    /// 列表（我的分享）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <param name="parentId"></param>
    /// <returns></returns>
    [HttpGet("Share")]
    public async Task<dynamic> GetShareOutList([FromQuery] KeywordInput input, string parentId)
    {
        var data = await _repository.AsQueryable()
            .Where(it => it.CreatorUserId == _userManager.UserId && it.EnabledMark == 1)
            .WhereIF(parentId == "0", it => it.IsShare > 0)
            .WhereIF(parentId != "0", it => it.ParentId == parentId)
            .OrderBy(it => it.Type).OrderBy(it => it.ShareTime, OrderByType.Desc)
            .ToListAsync();

        if (input.keyword.IsNotEmptyOrNull())
        {
            var newList = new List<DocumentEntity>();
            newList.AddRange(data);
            foreach (var item in data)
            {
                await GetChildSrcList(item.Id, newList, 0, 1);
            }

            data = newList.FindAll(it => it.Type == 1 && it.FullName.Contains(input.keyword));
            foreach (var item in data) item.ParentId = parentId;
        }

        foreach (var item in data)
        {
            if (parentId != "0") item.ShareTime = null;
        }

        return new { list = data.Adapt<List<DocumentShareOutput>>() };
    }

    /// <summary>
    /// 列表（共享给我）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <param name="parentId"></param>
    /// <returns></returns>
    [HttpGet("ShareTome")]
    public async Task<dynamic> GetShareTomeList([FromQuery] KeywordInput input, string parentId)
    {
        var userRelationList = _userManager.GetUserRelationAll(_userManager.UserId);
        var allData = await _repository.AsSugarClient().Queryable<DocumentEntity, DocumentShareEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.DocumentId))
            .Where((a, b) => a.EnabledMark == 1 && userRelationList.Contains(b.ShareUserId))
            .OrderBy(a => a.Type).OrderBy(a => a.ShareTime, OrderByType.Desc).Distinct()
            .ToListAsync();

        var data = new List<DocumentEntity>();
        if (parentId == "0")
        {
            data = allData;
            foreach (var item in data)
            {
                item.CreatorUserId = await _repository.AsSugarClient().Queryable<UserEntity>().Where(it => it.Id == item.CreatorUserId).Select(it => SqlFunc.MergeString(it.RealName, "/", it.Account)).FirstAsync();
            }
        }
        else
        {
            data = await _repository.AsQueryable()
                .Where(a => a.EnabledMark == 1 && a.ParentId == parentId)
                .OrderBy(a => a.Type).OrderBy(a => a.ShareTime, OrderByType.Desc).Distinct()
                .ToListAsync();

            foreach (var item in data)
            {
                if (!allData.Select(x => x.Id).Contains(item.Id))
                {
                    var shareParentEntity = await GetShareParentEntity(allData, item);
                    item.ShareTime = shareParentEntity.ShareTime;
                    item.CreatorUserId = shareParentEntity.CreatorUserId;
                }

                item.CreatorUserId = await _repository.AsSugarClient().Queryable<UserEntity>().Where(it => it.Id == item.CreatorUserId).Select(it => SqlFunc.MergeString(it.RealName, "/", it.Account)).FirstAsync();
            }
        }

        if (input.keyword.IsNotEmptyOrNull())
        {
            var newList = new List<DocumentEntity>();
            newList.AddRange(data);
            foreach (var item in data)
            {
                await GetChildSrcList(item.Id, newList, 1, 1);
            }

            data = newList.FindAll(it => it.Type == 1 && it.FullName.Contains(input.keyword));
            foreach (var item in data) item.ParentId = parentId;
        }

        return new { list = data.Adapt<List<DocumentShareTomeOutput>>() };
    }

    /// <summary>
    /// 列表（回收站）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("Trash")]
    public async Task<dynamic> GetTrashList([FromQuery] KeywordInput input)
    {
        var logList = await _repository.AsSugarClient().Queryable<DocumentLogEntity>()
            .Where(it => it.DeleteMark == null)
            .Select(it => it.DocumentId)
            .ToListAsync();

        var data = (await _repository.AsQueryable()
            .Where(it => it.CreatorUserId == _userManager.UserId && it.DeleteMark == null && it.EnabledMark == 0 && logList.Contains(it.Id))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), it => it.FullName.Contains(input.keyword))
            .OrderBy(it => it.Type).OrderBy(it => it.LastModifyTime, OrderByType.Desc)
            .ToListAsync()).Adapt<List<DocumentTrashOutput>>();
        return new { list = data };
    }

    /// <summary>
    /// 列表（共享人员）.
    /// </summary>
    /// <param name="documentId">文档主键.</param>
    /// <returns></returns>
    [HttpGet("ShareUser/{documentId}")]
    public async Task<dynamic> GetShareUserList(string documentId)
    {
        var data = (await _repository.AsSugarClient().Queryable<DocumentShareEntity>().Where(x => x.DocumentId == documentId).OrderBy(x => x.ShareTime, OrderByType.Desc).ToListAsync()).Adapt<List<DocumentShareUserOutput>>();
        return new { list = data };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = (await _repository.GetFirstAsync(x => x.Id == id && x.EnabledMark == 1)).Adapt<DocumentInfoOutput>();
        if (data.type == 1)
        {
            data.fullName = data.fullName.Replace("." + data.fileExtension, string.Empty);
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
    public async Task Create([FromBody] DocumentCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.CreatorUserId == _userManager.UserId && x.ParentId == input.parentId && x.Type == 0 && x.EnabledMark == 1))
            throw Oops.Oh(ErrorCode.Ex0008);
        var entity = input.Adapt<DocumentEntity>();
        var isOk = await _repository.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] DocumentUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.Type == input.type && x.FullName == input.fullName && x.CreatorUserId == _userManager.UserId && x.ParentId == input.parentId && x.EnabledMark == 1))
            throw Oops.Oh(ErrorCode.Ex0008);
        var entity = await _repository.GetFirstAsync(x => x.Id == id);
        entity.FullName = entity.Type == 1 ? string.Format("{0}.{1}", input.fullName, entity.FileExtension) : input.fullName;
        var isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (await _repository.IsAnyAsync(x => x.ParentId == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.Ex0006);
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 批量删除.
    /// </summary>
    /// <param name="input">主键值.</param>
    /// <returns></returns>
    [HttpPost("BatchDelete")]
    public async Task BatchDelete([FromBody] DocumentBatchInput input)
    {
        foreach (var id in input.ids)
        {
            var entity = await _repository.GetFirstAsync(x => x.Id == id && x.EnabledMark == 1);

            var list = new List<DocumentEntity>();
            if (entity.Type == 0) await GetChildSrcList(entity.Id, list, 0, 1);
            list.Add(entity);

            await _repository.AsUpdateable()
                .SetColumns(it => new DocumentEntity
                {
                    EnabledMark = 0,
                    LastModifyTime = SqlFunc.GetDate(),
                    LastModifyUserId = _userManager.UserId
                })
                .Where(it => list.Select(x => x.Id).Contains(it.Id)).ExecuteCommandHasChangeAsync();

            var childId = string.Join(",", list.Select(it => it.Id));
            var log = new DocumentLogEntity()
            {
                DocumentId = id,
                ChildDocument = childId
            };
            await _repository.AsSugarClient().Insertable(log).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 上传文件.
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="fileName"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Uploader")]
    public async Task Uploader(string parentId, string fileName, [FromForm] DocumentUploaderInput input)
    {
        var fileExtension = Path.GetExtension(input.file.FileName).Replace(".", string.Empty);
        var allowExtension = KeyVariable.AllowUploadFileType;
        if (!allowExtension.Contains(fileExtension)) throw Oops.Oh(ErrorCode.D1800);

        #region 上传图片
        var upFileName = fileName.IsNotEmptyOrNull() ? fileName : input.file.FileName;
        var saveFileName = string.Format("{0}{1}{2}", DateTime.Now.ToString("yyyyMMdd"), RandomExtensions.NextLetterAndNumberString(new Random(), 5), Path.GetExtension(input.file.FileName));
        if (await _repository.IsAnyAsync(x => x.CreatorUserId == _userManager.UserId && x.FullName == upFileName && x.Type == 1 && x.EnabledMark == 1))
        {
            var random = new Random().NextLetterAndNumberString(5);
            var newFileName = Path.GetFileNameWithoutExtension(upFileName);
            upFileName = string.Format("{0}副本{1}.{2}", newFileName, random, fileExtension);
        }
        var stream = input.file.OpenReadStream();
        var _filePath = _fileManager.GetPathByType("document");
        await _fileManager.UploadFileByType(stream, _filePath, saveFileName);
        Thread.Sleep(1000);
        #endregion

        #region 保存数据
        var entity = new DocumentEntity();
        entity.Type = 1;
        entity.FullName = upFileName;
        entity.ParentId = parentId;
        entity.FileExtension = fileExtension;
        entity.FilePath = saveFileName;
        entity.FileSize = input.file.Length.ToString();
        entity.UploadUrl = string.Format("/api/file/Image/document/{0}", entity.FilePath);
        var isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.D8001);
        #endregion
    }

    /// <summary>
    /// 分片组装.
    /// </summary>
    /// <param name="input">请求参数.</param>
    [HttpPost("merge")]
    public async Task<dynamic> merge([FromForm] ChunkModel input)
    {
        var fileName = input.fileName;
        if (await _repository.IsAnyAsync(x => x.CreatorUserId == _userManager.UserId && x.FullName == fileName && x.Type == 1 && x.EnabledMark == 1))
        {
            var random = new Random().NextLetterAndNumberString(5);
            var newFileName = Path.GetFileNameWithoutExtension(fileName);
            fileName = string.Format("{0}副本{1}.{2}", newFileName, random, input.extension);
        }
        input.isUpdateName = false;
        input.type = "document";
        var _filePath = _fileManager.GetPathByType(input.type);
        var output = await _fileManager.Merge(input);
        #region 保存数据
        var entity = new DocumentEntity();
        entity.Type = 1;
        entity.FullName = fileName;
        entity.ParentId = input.parentId;
        entity.FileExtension = input.extension;
        entity.FilePath = output.name;
        entity.FileSize = input.fileSize;
        entity.UploadUrl = string.Format("/api/file/Image/document/{0}", entity.FilePath);
        var isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.D8001);
        #endregion
        return output;
    }

    /// <summary>
    /// 下载文件.
    /// </summary>
    /// <param name="id">主键值.</param>
    [HttpPost("Download/{id}")]
    public async Task<dynamic> Download(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.EnabledMark == 1);
        if (entity == null)
            throw Oops.Oh(ErrorCode.D8000);
        var fileName = _userManager.UserId + "|" + entity.FilePath + "|document";
        _cacheManager.Set(entity.FilePath, string.Empty);
        return new {
            name = entity.FullName,
            url = "/api/File/Download?encryption=" + DESEncryption.Encrypt(fileName, "JNPF")
        };
    }

    /// <summary>
    /// 批量下载文件.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("PackDownload")]
    [AllowAnonymous]
    public async Task<dynamic> PackDownload([FromBody] DocumentBatchInput input)
    {
        // 单个文件直接下载
        if (input.ids.Count == 1)
        {
            var entity = await _repository.GetFirstAsync(it => it.EnabledMark == 1 && it.Id == input.ids[0]);
            if (entity.Type == 1)
            {
                var fileName = _userManager.UserId + "|" + entity.FilePath + "|document";
                _cacheManager.Set(entity.FilePath, string.Empty);
                return new {
                    name = entity.FullName,
                    url = "/api/File/Download?encryption=" + DESEncryption.Encrypt(fileName, "JNPF")
                };
            }
        }

        var directoryName = DateTime.Now.ToString("yyyyMMddHHmmss");
        var path = Path.Combine(_fileManager.GetPathByType("document"), directoryName);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        await FileCompress(input.ids, path);
        string downloadPath = path + ".zip";

        // 判断是否存在同名称文件
        if (File.Exists(downloadPath))
            File.Delete(downloadPath);
        ZipFile.CreateFromDirectory(path, downloadPath);
        if (!App.Configuration["OSS:Provider"].Equals("Invalid"))
        {
            FileStream? file = new FileStream(downloadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await _fileManager.UploadFileByType(file, _fileManager.GetPathByType("document"), string.Format("{0}.zip", directoryName));
        }
        var downloadFileName = string.Format("{0}|{1}.zip|document", _userManager.UserId, directoryName);
        _cacheManager.Set(string.Format("{0}.zip", directoryName), string.Empty);
        return new { name = string.Format("{0}.zip", directoryName), url = "/api/File/Download?encryption=" + DESEncryption.Encrypt(downloadFileName, "JNPF") };
    }

    /// <summary>
    /// 回收站（彻底删除）.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("Trash")]
    [UnitOfWork]
    public async Task TrashDelete([FromBody] DocumentBatchInput input)
    {
        foreach (var id in input.ids)
        {
            var list = new List<DocumentEntity>();
            await GetChildSrcList(id, list, 0, 0);
            foreach (var item in list)
            {
                if (item.Type == 1)
                {
                    await _fileManager.DeleteFile(item.FilePath);
                }
                await _repository.AsSugarClient().Updateable(item).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
            }

            var isOk = await _repository.AsUpdateable()
                .SetColumns(it => new DocumentEntity
                {
                    DeleteMark = 1,
                    DeleteUserId = _userManager.UserId,
                    DeleteTime = SqlFunc.GetDate()
                })
                .Where(it => it.Id == id && it.CreatorUserId == _userManager.UserId).ExecuteCommandHasChangeAsync();
            if (!isOk)
                throw Oops.Oh(ErrorCode.COM1002);
        }
    }

    /// <summary>
    /// 回收站（还原文件）.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Trash/Actions/Recovery")]
    public async Task TrashRecovery([FromBody] DocumentBatchInput input)
    {
        foreach (var id in input.ids)
        {
            var entity = await _repository.GetFirstAsync(it => it.Id == id);
            if (entity.ParentId != "0" && !await _repository.IsAnyAsync(it => it.Id == entity.ParentId && it.EnabledMark == 1))
            {
                await _repository.AsUpdateable().SetColumns(it => it.ParentId == "0").Where(it => it.Id == id).ExecuteCommandAsync();
            }
            var childId = await _repository.AsSugarClient().Queryable<DocumentLogEntity>().Where(it => it.DeleteMark == null && it.DocumentId == id).Select(it => it.ChildDocument).FirstAsync();
            await _repository.AsUpdateable().SetColumns(it => it.EnabledMark == 1).Where(it => childId.Contains(it.Id)).ExecuteCommandAsync();

            await _repository.AsSugarClient().Updateable<DocumentLogEntity>()
                .SetColumns(it => new DocumentLogEntity
                {
                    DeleteMark = 1,
                    DeleteTime = SqlFunc.GetDate(),
                    DeleteUserId = _userManager.UserId
                }).Where(it => it.DeleteMark == null && it.DocumentId == id).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 共享文件（创建）.
    /// </summary>
    /// <param name="input">共享人.</param>
    /// <returns></returns>
    [HttpPost("Actions/Share")]
    [UnitOfWork]
    public async Task ShareCreate([FromBody] DocumentActionsShareInput input)
    {
        foreach (var id in input.ids)
        {
            if (id == "@userId") continue;
            List<DocumentShareEntity> documentShareEntityList = new List<DocumentShareEntity>();
            foreach (var item in input.userIds)
            {
                if (!await _repository.AsSugarClient().Queryable<DocumentShareEntity>().AnyAsync(it => it.DocumentId == id && it.ShareUserId == item))
                {
                    documentShareEntityList.Add(new DocumentShareEntity
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        DocumentId = id,
                        ShareUserId = item,
                        ShareTime = DateTime.Now,
                    });
                }
            }

            var entity = await _repository.GetFirstAsync(x => x.Id == id && x.EnabledMark == 1);
            entity.IsShare = entity.IsShare.IsNotEmptyOrNull() ? entity.IsShare + documentShareEntityList.Count : documentShareEntityList.Count;
            entity.ShareTime = DateTime.Now;
            await _repository.AsSugarClient().Insertable(documentShareEntityList).ExecuteCommandAsync();
            var isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            if (!isOk)
                throw Oops.Oh(ErrorCode.COM1001);
        }
    }

    /// <summary>
    /// 共享文件（取消）.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Actions/CancelShare")]
    [UnitOfWork]
    public async Task CancelShare([FromBody] DocumentBatchInput input)
    {
        foreach (var id in input.ids)
        {
            var entity = await _repository.GetFirstAsync(x => x.Id == id && x.EnabledMark == 1);
            entity.IsShare = null;
            entity.ShareTime = null;
            _repository.AsSugarClient().Deleteable<DocumentShareEntity>().Where(x => x.DocumentId == id).ExecuteCommand();
            var isOk = await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            if (!isOk)
                throw Oops.Oh(ErrorCode.COM1001);
        }
    }

    /// <summary>
    /// 共享用户调整.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Actions/ShareAdjustment/{id}")]
    [UnitOfWork]
    public async Task ShareAdjustment(string id, [FromBody] DocumentShareAdjustmentInput input)
    {
        var list = new List<DocumentShareEntity>();
        foreach (var userId in input.userIds)
        {
            var shareEntity = new DocumentShareEntity();
            shareEntity.Id = SnowflakeIdHelper.NextId();
            shareEntity.DocumentId = id;
            shareEntity.ShareUserId = userId;
            shareEntity.ShareTime = DateTime.Now;
            shareEntity.CreatorUserId = _userManager.UserId;
            shareEntity.CreatorTime = DateTime.Now;
            list.Add(shareEntity);
        }

        await _repository.AsSugarClient().Deleteable<DocumentShareEntity>().Where(it => it.DocumentId == id).ExecuteCommandAsync();
        await _repository.AsSugarClient().Insertable(list).ExecuteCommandAsync();
        await _repository.AsUpdateable().SetColumns(it => it.IsShare == list.Count).SetColumns(it => it.ShareTime == SqlFunc.GetDate()).Where(it => it.Id == id).ExecuteCommandAsync();
    }

    /// <summary>
    /// 列表（文件夹树）.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("FolderTree")]
    public async Task<dynamic> GetFolderTree([FromBody] DocumentBatchInput input)
    {
        var data = new List<DocumentFolderTreeOutput>()
        {
            new() { id = "-1", fullName = "全部文档", parentId = "0", icon = "0"}
        };
        var list = (await _repository.AsQueryable()
            .Where(x => x.CreatorUserId == _userManager.UserId && x.Type == 0 && x.EnabledMark == 1)
            .OrderBy(x => x.CreatorTime, OrderByType.Desc)
            .ToListAsync()).Adapt<List<DocumentFolderTreeOutput>>();
        data.AddRange(list);

        if (input.ids != null)
        {
            data.RemoveAll(x => input.ids.Contains(x.id));
        }

        var treeList = data.ToTree();
        return new { list = treeList };
    }

    /// <summary>
    /// 文件/夹移动到.
    /// </summary>
    /// <param name="toId">将要移动到Id.</param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("Actions/MoveTo/{toId}")]
    public async Task MoveTo(string toId, [FromBody] DocumentBatchInput input)
    {
        foreach (var id in input.ids)
        {
            var entity = await _repository.GetFirstAsync(x => x.Id == id);
            var entityTo = await _repository.GetFirstAsync(x => x.Id == toId);
            if (id == toId && entity.Type == 0 && entityTo.Type == 0)
                throw Oops.Oh(ErrorCode.Ex0002);
            if (entityTo.IsNotEmptyOrNull() && id == entityTo.ParentId && entity.Type == 0 && entityTo.Type == 0)
                throw Oops.Oh(ErrorCode.Ex0005);
            entity.ParentId = toId;
            var isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            if (!isOk)
                throw Oops.Oh(ErrorCode.COM1001);
        }
    }

    /// <summary>
    /// 流程归档.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("UploadBlob")]
    public async Task UploadBlob([FromForm] DocumentUploaderInput input)
    {
        try
        {
            var taskEntity = await _repository.AsSugarClient().Queryable<WorkFlowTaskEntity>().FirstAsync(x => x.Id == input.taskId);
            if (taskEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1005);
            var globalNode = await _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().FirstAsync(x => x.FlowId == taskEntity.FlowId && x.NodeType == "global");
            if (globalNode.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1005);

            #region 参数处理
            var globalPro = globalNode.NodeJson.ToObject<GlobalProperties>();
            var fileName = string.Format("{0}-{1}.pdf", taskEntity.FullName, taskEntity.EndTime.ParseToDateTime().ToString("yyyyMMddHHmmss"));
            var entity = new DocumentEntity();
            entity.Id = SnowflakeIdHelper.NextId();
            entity.Type = 1;
            entity.FullName = fileName;
            entity.FileExtension = "pdf";
            entity.FilePath = fileName;
            entity.FileSize = input.file.Length.ToString();
            entity.UploadUrl = string.Format("/api/file/Image/document/{0}", entity.FilePath);
            entity.Description = string.Format("{0}-{1}", taskEntity.Id, taskEntity.TemplateId);
            entity.CreatorUserId = taskEntity.CreatorUserId;
            var shareInput = new DocumentActionsShareInput();
            shareInput.userIds = new List<string>();
            if (globalPro.fileConfig.permissionType == 1)
            {
                shareInput.ids = new List<string> { entity.Id };
                var handleUserIds = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity>().Where(x => x.TaskId == taskEntity.Id && x.HandleStatus != null && x.Status != 2 && x.Status != 3 && x.Status != -1 && x.HandleId != "jnpf").Select(x => x.HandleId).ToList();
                shareInput.userIds = shareInput.userIds.Union(handleUserIds).ToList();
                var circulateUserIds = _repository.AsSugarClient().Queryable<WorkFlowCirculateEntity>().Where(x => x.TaskId == taskEntity.Id).Select(x => x.UserId).ToList();
                shareInput.userIds = shareInput.userIds.Union(circulateUserIds).Distinct().ToList();
            }
            if (globalPro.fileConfig.permissionType == 3)
            {
                shareInput.ids = new List<string> { entity.Id };
                var OperatorList = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity>().Where(x => x.TaskId == taskEntity.Id && x.HandleStatus != null && x.Status != 2 && x.Status != 3 && x.Status != -1 && x.HandleId != "jnpf").OrderByDescending(x => x.HandleTime).ToList();
                if (OperatorList.Any())
                {
                    var operatorEntity = OperatorList.First(); // 最后一个审批人
                    var lastOperatorList = OperatorList.FindAll(x => x.NodeId == operatorEntity.NodeId).ToList();
                    entity.CreatorUserId = lastOperatorList.FirstOrDefault().HandleId;
                    var handIds = lastOperatorList.Select(x => x.HandleId).ToList();
                    shareInput.userIds = shareInput.userIds.Union(handIds).ToList();
                    var circulateUserIds = _repository.AsSugarClient().Queryable<WorkFlowCirculateEntity>().Where(x => x.TaskId == taskEntity.Id && x.NodeCode == operatorEntity.NodeCode).Select(x => x.UserId).ToList();
                    shareInput.userIds = shareInput.userIds.Union(circulateUserIds).Distinct().ToList();
                }
            }
            #endregion

            #region 归档路径判断
            var documentEntity = await _repository.GetFirstAsync(x => x.FullName == "流程归档" && x.CreatorUserId == entity.CreatorUserId && x.ParentId == "0" && x.Type == 0 && x.EnabledMark == 1);
            if (documentEntity.IsNullOrEmpty())
            {
                documentEntity = new DocumentEntity();
                documentEntity.Id = SnowflakeIdHelper.NextId();
                documentEntity.ParentId = "0";
                documentEntity.FullName = "流程归档";
                documentEntity.Type = 0;
                documentEntity.EnabledMark = 1;
                documentEntity.CreatorUserId = entity.CreatorUserId;
                await _repository.AsSugarClient().Insertable(documentEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            }
            entity.ParentId = documentEntity.Id;
            #endregion

            #region 文件上传
            var stream = input.file.OpenReadStream();
            var _filePath = _fileManager.GetPathByType("document");
            await _fileManager.UploadFileByType(stream, _filePath, fileName);
            Thread.Sleep(1000);
            #endregion

            #region 保存数据
            var isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            if (isOk < 1)
                throw Oops.Oh(ErrorCode.D8001);

            shareInput.userIds.Remove(entity.CreatorUserId);
            if (shareInput.userIds.Any()) //共享文件
            {
                shareInput.userIds = shareInput.userIds.Select(x => string.Format("{0}--{1}", x, CommonConst.RelationObjectUser)).ToList();
                await ShareCreate(shareInput);
            }
            taskEntity.IsFile = 1;
            await _repository.AsSugarClient().Updateable(taskEntity).ExecuteCommandAsync();
            #endregion
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D8001);
        }

    }
    #endregion

    #region Private

    /// <summary>
    /// 获取文件夹下的所有子文件.
    /// </summary>
    /// <param name="id">文件夹id.</param>
    /// <param name="list">数据.</param>
    /// <param name="type">0 自己的文件，1 所有文件.</param>
    /// <param name="enabledMark">启用标识.</param>
    private async Task GetChildSrcList(string id, List<DocumentEntity> list, int type, int enabledMark)
    {
        var entityList = await _repository.AsQueryable()
            .Where(it => it.EnabledMark == enabledMark && it.ParentId == id)
            .WhereIF(type == 0, it => it.CreatorUserId == _userManager.UserId)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .ToListAsync();
        foreach (var item in entityList)
        {
            if (!list.Any(it => it.Id == item.Id)) list.Add(item);
            if (item.Type == 0) await GetChildSrcList(item.Id, list, type, enabledMark);
        }
    }

    /// <summary>
    /// 文件打包.
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="directoryName"></param>
    /// <returns></returns>
    private async Task FileCompress(List<string> ids, string directoryName)
    {
        foreach (var id in ids)
        {
            var entity = await _repository.GetFirstAsync(it => it.EnabledMark == 1 && it.Id == id);
            if (entity.IsNotEmptyOrNull())
            {
                if (entity.Type == 0)
                {
                    var nextDirectoryName = Path.Combine(directoryName, entity.FullName);
                    if (!Directory.Exists(nextDirectoryName))
                        Directory.CreateDirectory(nextDirectoryName);
                    var idList = (await _repository.GetListAsync(x => x.ParentId == entity.Id && x.EnabledMark == 1 && x.DeleteMark == null)).Select(x => x.Id).ToList();
                    await FileCompress(idList, nextDirectoryName);
                }
                else
                {
                    await _fileManager.CopyFile(Path.Combine(_fileManager.GetPathByType("document"), entity.FilePath), Path.Combine(directoryName, entity.FilePath));
                }
            }
        }
    }

    /// <summary>
    /// 文件打包.
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="directoryName"></param>
    /// <returns></returns>
    private async Task FileCompress1(List<string> ids, string directoryName)
    {
        foreach (var id in ids)
        {
            var entity = await _repository.GetFirstAsync(it => it.EnabledMark == 1 && it.Id == id);
            if (entity.IsNotEmptyOrNull())
            {
                if (entity.Type == 0)
                {
                    var nextDirectoryName = Path.Combine(directoryName, entity.FullName);
                    if (!Directory.Exists(nextDirectoryName))
                        Directory.CreateDirectory(nextDirectoryName);
                    var idList = (await _repository.GetListAsync(x => x.ParentId == entity.Id && x.EnabledMark == 1 && x.DeleteMark == null)).Select(x => x.Id).ToList();
                    await FileCompress(idList, nextDirectoryName);
                }
                else
                {
                    string filePath = Path.Combine(_fileManager.GetPathByType("document"), entity.FilePath.Replace("@", "."));
                    if (!App.Configuration["OSS:Provider"].Equals("Invalid"))
                    {
                        var stream = await _fileManager.GetFileStream(filePath);
                        var uploadPath = Path.Combine(directoryName, entity.FullName);
                        if (!Directory.Exists(directoryName))
                            Directory.CreateDirectory(directoryName);
                        using (var streamLocal = File.Create(uploadPath))
                        {
                            await stream.CopyToAsync(streamLocal);
                        }
                    }
                    else
                    {
                        var toFilePath = Path.Combine(directoryName, entity.FullName);
                        if (FileHelper.Exists(toFilePath))
                        {
                            var random = new Random().NextLetterAndNumberString(5).ToLower();
                            var fileExtension = entity.FileExtension;
                            var newFileName = string.Format("{0}副本{1}{2}", entity.FullName.Replace(fileExtension, string.Empty), random, fileExtension);
                            toFilePath = Path.Combine(directoryName, newFileName);
                        }

                        FileHelper.CopyFile(filePath, toFilePath);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取父级分享文件.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    private async Task<DocumentEntity> GetShareParentEntity(List<DocumentEntity> list, DocumentEntity data)
    {
        var res = list.Find(x => x.Id.Equals(data.ParentId));
        if (res.IsNullOrEmpty())
        {
            var parentEntity = await _repository.AsQueryable().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(data.ParentId));
            res = await GetShareParentEntity(list, parentEntity);
        }

        return res;
    }

    #endregion
}