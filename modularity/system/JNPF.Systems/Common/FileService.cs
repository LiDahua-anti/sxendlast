using JNPF.Common.Captcha.General;
using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.RemoteRequest;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace JNPF.Systems.Common;

/// <summary>
/// 业务实现：通用控制器.
/// </summary>
[ApiDescriptionSettings(Tag = "Common", Name = "File", Order = 161)]
[Route("api/[controller]")]
[AllowAnonymous]
[IgnoreLog]
public class FileService : IFileService, IDynamicApiController, ITransient
{
    private readonly AppOptions _appOptions;

    /// <summary>
    /// 验证码处理程序.
    /// </summary>
    private readonly IGeneralCaptcha _captchaHandler;

    private readonly ISqlSugarRepository<UserEntity> _repository;  // 用户表仓储

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 缓存服务.
    /// </summary>
    private readonly ICacheManager _cacheManager;


    private readonly IWebHostEnvironment _webHostEnvironment;


    /// <summary>
    /// 初始化一个<see cref="FileService"/>类型的新实例.
    /// </summary>
    public FileService(
        IOptions<AppOptions> appOptions,
        IGeneralCaptcha captchaHandler,
        IUserManager userManager,
        ISqlSugarRepository<UserEntity> userRepository,
        IWebHostEnvironment webHostEnvironment,
        ICacheManager cacheManager,
        IFileManager fileManager)
    {
        _appOptions = appOptions.Value;
        _repository = userRepository;
        _captchaHandler = captchaHandler;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
        _cacheManager = cacheManager;
        _fileManager = fileManager;
    }

    #region 自定义

    /// <summary>
    /// 上传图片
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Uploader/CustomImg")]
    public async Task<string> UploadImg(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw Oops.Oh(ErrorCode.D8000);

        var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
        {
            throw Oops.Oh(ErrorCode.D1800);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var newFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{timestamp}{fileExtension}";
        var webRootPath = Path.Combine(
                          _webHostEnvironment.ContentRootPath,
                          "wwwroot",
                          "CustomImg"
                          );
        if (!Directory.Exists(webRootPath))
            Directory.CreateDirectory(webRootPath);
        var filePath = Path.Combine(webRootPath, newFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);
        return $"/CustomImg/{newFileName}";

    }

    /// <summary>
    /// 删除临时文件
    /// </summary>
    /// <param name="filepath"></param>
    /// <returns></returns>
    [HttpGet("Uploader/DeleteTempFile")]
    public async Task UploadFile(string filepath)
    {
        var webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        var relativePath = filepath.TrimStart('/', '\\');
        var filePathDelete = Path.Combine(webRootPath, relativePath);
        if (File.Exists(filePathDelete))
        {
            File.Delete(filePathDelete);
        }
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Uploader/CustomFile")]
    [RequestSizeLimit(100_000_000)]
    public async Task<dynamic> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw Oops.Oh(ErrorCode.D1800);
        }

        // 文件大小限制
        if (file.Length > 100 * 1024 * 1024)
        {
            throw Oops.Oh(ErrorCode.D1808);
        }

        // 安全检查：禁止危险文件类型
        var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".sh", ".php", ".asp", ".aspx", ".jsp", ".js", ".vbs" };
        var fileExtension = Path.GetExtension(file.FileName)?.ToLower();

        if (dangerousExtensions.Contains(fileExtension))
        {
            throw Oops.Oh(ErrorCode.D1809);

        }

        // 创建临时文件目录
        var webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot", "TempFiles");
        if (!Directory.Exists(webRootPath))
            Directory.CreateDirectory(webRootPath);

        // 生成唯一文件名：原文件名_日期_随机数_GUID.扩展名
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        var uniqueFileName = $"{fileNameWithoutExt}_{timestamp}_{random}{fileExtension}";

        var filePath = Path.Combine(webRootPath, uniqueFileName);
        // 异步保存文件（使用缓冲区优化）
        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                       bufferSize: 81920, useAsync: true))
        {
            await file.CopyToAsync(stream);
        }

        var userId = _userManager.UserId;
        var userInfo = await _repository.AsQueryable().FirstAsync(m => m.Id == userId);
        // 返回文件信息 - 根据您的代码修正取值
        return new {
            name = file.FileName,                    // 原始文件名
            url = $"/TempFiles/{uniqueFileName}", // 访问URL
            fileExtension = fileExtension,           // 文件扩展名
            fileSize = file.Length,                  // 文件大小
            fileName = uniqueFileName,               // 保存后的文件名
            creator = userInfo.RealName,
        };
    }

    #endregion

    #region GET

    /// <summary>
    /// 上传文件预览.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Uploader/Preview")]
    public async Task<dynamic> Preview(string fileName, string fileDownloadUrl)
    {
        string[]? typeList = new string[] { "doc", "docx", "xls", "xlsx", "ppt", "pptx", "pdf", "jpg", "jpeg", "gif", "png", "bmp", "txt" };
        string? type = fileName.Split('.').LastOrDefault();
        if (typeList.Contains(type))
        {
            if (fileName.IsNotEmptyOrNull())
            {
                string previewUrl = string.Empty;
                switch (_appOptions.PreviewType)
                {
                    case PreviewType.kkfile:
                        previewUrl = KKFileUploaderPreview(fileName, fileDownloadUrl);
                        break;
                    case PreviewType.yozo:
                        previewUrl = await YoZoUploaderPreview(fileName, 5, 1);
                        break;
                }

                return previewUrl;
            }
            else
            {
                throw Oops.Oh(ErrorCode.D8000);
            }
        }
        else
        {
            throw Oops.Oh(ErrorCode.D1802);
        }
    }

    /// <summary>
    /// 生成图片链接.
    /// </summary>
    /// <param name="type">图片类型.</param>
    /// <param name="fileName">注意 后缀名前端故意把 .替换@ .</param>
    /// <returns></returns>
    [HttpGet("Image/{type}/{fileName}")]
    public async Task<dynamic> GetImg(string type, string fileName, [QueryString] string s, [QueryString] string t)
    {
        if (!IsWhiteFolder(type))
        {
            if (s.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D1805);
            var tokenKey = AESEncryption.AesDecrypt(s, KeyVariable.AesKey);
            if (_cacheManager.Exists(tokenKey))
            {
                var token = _cacheManager.Get(tokenKey);
                if (token.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D1805);
            }
            else
            {
                throw Oops.Oh(ErrorCode.D1805);
            }
        }
        if (t.IsNotEmptyOrNull())
        {
            string? filePath = Path.Combine(_fileManager.GetPathByType(type), fileName.Replace("@", "."));
            return await _fileManager.DownloadFileByType(filePath, fileName);
        }
        else
        {
            string? url = string.Format("{0}|{1}|{2}", _userManager.UserId, fileName, type);
            string? encryptStr = DESEncryption.Encrypt(url, "JNPF");
            _cacheManager.Set(fileName, string.Empty);
            url = string.Format("{0}/api/file/Download?encryption={1}", App.Configuration["Message:ApiDoMain"], encryptStr);
            App.HttpContext.Response.Redirect(url);
            return new object();
        }
    }

    /// <summary>
    /// 生成大屏图片链接.
    /// </summary>
    /// <param name="type">图片类型.</param>
    /// <param name="fileName">注意 后缀名前端故意把 .替换@ .</param>
    /// <returns></returns>
    [HttpGet("VisusalImg/BiVisualPath/{type}/{fileName}")]
    public async Task<IActionResult> GetScreenImg(string type, string fileName)
    {
        string filePath = Path.Combine(_fileManager.GetPathByType(type), type, fileName.Replace("@", "."));
        return await _fileManager.DownloadFileByType(filePath, fileName);
    }

    /// <summary>
    /// 获取图形验证码.
    /// </summary>
    /// <param name="timestamp">时间戳.</param>
    /// <returns></returns>
    [HttpGet("ImageCode/{timestamp}")]
    [NonUnify]
    public async Task<IActionResult> GetCode(string timestamp)
    {
        return new FileContentResult(await _captchaHandler.CreateCaptchaImage(timestamp, 114, 32), "image/jpeg");
    }

    /// <summary>
    /// 下载.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="type"></param>
    [HttpGet("down/{fileName}")]
    public async Task FileDown(string fileName, [FromQuery] string type)
    {
        string? systemFilePath = Path.Combine(FileVariable.SystemFilePath, fileName);
        if (type.IsNotEmptyOrNull())
        {
            systemFilePath = Path.Combine(_fileManager.GetPathByType(type), fileName);
        }
        var fileStreamResult = await _fileManager.DownloadFileByType(systemFilePath, fileName);
        byte[] bytes = new byte[fileStreamResult.FileStream.Length];

        fileStreamResult.FileStream.Read(bytes, 0, bytes.Length);

        fileStreamResult.FileStream.Close();
        var httpContext = App.HttpContext;
        httpContext.Response.ContentType = "application/octet-stream";
        httpContext.Response.Headers.Add("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(fileName, Encoding.UTF8));
        httpContext.Response.Headers.Add("Content-Length", bytes.Length.ToString());
        await httpContext.Response.Body.WriteAsync(bytes);
        httpContext.Response.Body.Flush();
        httpContext.Response.Body.Close();
    }

    #region 下载附件

    /// <summary>
    /// 获取下载文件链接.
    /// </summary>
    /// <param name="type">图片类型.</param>
    /// <param name="fileName">文件名称.</param>
    /// <returns></returns>
    [HttpGet("Download/{type}/{fileName}")]
    public dynamic DownloadUrl(string type, string fileName)
    {
        string? url = string.Format("{0}|{1}|{2}", _userManager.UserId, fileName, type);
        string? encryptStr = DESEncryption.Encrypt(url, "JNPF");
        _cacheManager.Set(fileName, string.Empty);
        return new { name = fileName, url = string.Format("/api/file/Download?encryption={0}", encryptStr) };
    }

    /// <summary>
    /// 全部下载.
    /// </summary>
    /// <param name="type">图片类型.</param>
    /// <param name="fileName">文件名称.</param>
    /// <returns></returns>
    [HttpPost("PackDownload/{type}")]
    public async Task<dynamic> DownloadAll(string type, [FromBody] List<FileControlsModel> input)
    {
        var fileName = RandomExtensions.NextLetterAndNumberString(new Random(), 7);

        // 临时目录
        var directoryPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, "TemporaryFile", fileName);
        Directory.CreateDirectory(directoryPath);
        foreach (var item in input)
        {
            string filePath = Path.Combine(_fileManager.GetPathByType(type), item.fileId.Replace("@", "."));
            filePath = filePath.Replace(@",", "/");
            if (!App.Configuration["OSS:Provider"].Equals("Invalid"))
            {
                var stream = await _fileManager.GetFileStream(filePath);
                var uploadPath = Path.Combine(directoryPath, item.fileName);
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
                using (var streamLocal = File.Create(uploadPath))
                {
                    await stream.CopyToAsync(streamLocal);
                }
            }
            else
            {
                var toFilePath = Path.Combine(directoryPath, item.fileName);
                if (FileHelper.Exists(toFilePath))
                {
                    var random = new Random().NextLetterAndNumberString(5).ToLower();
                    var fileExtension = Path.GetExtension(item.fileName);
                    var newFileName = string.Format("{0}副本{1}{2}", item.fileName.Replace(fileExtension, string.Empty), random, fileExtension);
                    toFilePath = Path.Combine(directoryPath, newFileName);
                }

                FileHelper.CopyFile(filePath, toFilePath);
            }
        }

        // 压缩文件
        string downloadPath = directoryPath + ".zip";

        // 判断是否存在同名称文件
        if (File.Exists(downloadPath)) File.Delete(downloadPath);

        ZipFile.CreateFromDirectory(directoryPath, downloadPath);
        if (!App.Configuration["OSS:Provider"].Equals("Invalid"))
            await UploadFileByType(downloadPath, "TemporaryFile", string.Format("{0}.zip", fileName));

        var downloadFileName = string.Format("{0}|{1}.zip|TemporaryFile", _userManager.UserId, fileName);
        _cacheManager.Set(fileName + ".zip", string.Empty);
        return new { downloadName = string.Format("文件{0}.zip", fileName), downloadVo = new { name = fileName, url = "/api/File/Download?encryption=" + DESEncryption.Encrypt(downloadFileName, "JNPF") } };
    }

    /// <summary>
    /// 下载文件链接.
    /// </summary>
    [HttpGet("Download")]
    public async Task<dynamic> DownloadFile([FromQuery] string encryption, [FromQuery] string name)
    {
        string decryptStr = DESEncryption.Decrypt(encryption, "JNPF");
        List<string> paramsList = decryptStr.Split("|").ToList();
        if (paramsList.Count > 0)
        {
            string fileName = paramsList.Count > 1 ? paramsList[1] : string.Empty;
            if (_cacheManager.Exists(fileName))
            {
                _cacheManager.Del(fileName);
            }
            else
            {
                throw Oops.Oh(ErrorCode.D1805);
            }
            string type = paramsList.Count > 2 ? paramsList[2] : string.Empty;
            string filePath = Path.Combine(_fileManager.GetPathByType(type), fileName.Replace("@", "."));
            string fileDownloadName = name.IsNullOrEmpty() ? fileName : name;
            return await _fileManager.DownloadFileByType(filePath, fileDownloadName);
        }
        else
        {
            throw Oops.Oh(ErrorCode.D8000);
        }
    }

    /// <summary>
    /// App启动信息.
    /// </summary>
    [HttpGet("AppStartInfo/{appName}")]
    public async Task<dynamic> AppStartInfo(string appName)
    {
        return new { appVersion = KeyVariable.AppVersion, appUpdateContent = KeyVariable.AppUpdateContent };
    }

    #endregion

    /// <summary>
    /// 分片上传获取.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("chunk")]
    public async Task<dynamic> CheckChunk([FromQuery] ChunkModel input)
    {
        try
        {
            if (!AllowFileType(input.extension, input.extension))
                throw Oops.Oh(ErrorCode.D1800);
            string path = _fileManager.GetPathByType(string.Empty);
            string filePath = Path.Combine(path, input.identifier);
            var chunkFiles = FileHelper.GetAllFiles(filePath);
            List<int> existsChunk = chunkFiles.FindAll(x => !FileHelper.GetFileType(x).Equals("tmp"))
                .Select(x => x.FullName.Replace(input.identifier + "-", string.Empty).ParseToInt()).ToList();
            return new { chunkNumbers = existsChunk, merge = false };
        }
        catch (Exception)
        {

            throw;
        }
    }

    #endregion

    #region POST

    /// <summary>
    /// 上传文件/图片.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Uploader/{type}")]
    public async Task<dynamic> Uploader(string type, [FromForm] ChunkModel input)
    {
        string? fileType = Path.GetExtension(input.file.FileName).Replace(".", string.Empty);
        if (!AllowFileType(fileType, type))
            throw Oops.Oh(ErrorCode.D1800);
        string saveFileName = string.Format("{0}{1}{2}", DateTime.Now.ToString("yyyyMMdd"), RandomExtensions.NextLetterAndNumberString(new Random(), 5), Path.GetExtension(input.file.FileName));
        var stream = input.file.OpenReadStream();
        input.type = type;
        _fileManager.GetChunkModel(input, saveFileName);
        await _fileManager.UploadFileByType(stream, input.folder, saveFileName);
        if (AllowImageType(fileType) && type.Equals("annexpic"))
        {
            var slStram = await _fileManager.GetFileStream(Path.Combine(input.folder, saveFileName));
            await _fileManager.MakeThumbnail(slStram, saveFileName, input.folder);
            return new FileControlsModel { name = input.fileName, url = string.Format("/api/File/Image/{0}/{1}", type, input.fileName), thumbUrl = string.Format("/api/File/Image/{0}/{1}", type, input.slImgName), fileExtension = fileType, fileSize = input.file.Length, fileName = input.slImgName };
        }
        else
        {
            return new FileControlsModel { name = input.fileName, url = string.Format("/api/File/Image/{0}/{1}", type, input.fileName), fileExtension = fileType, fileSize = input.file.Length, fileName = input.fileName };
        }
    }

    /// <summary>
    /// 上传文件/图片.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Uploader/imgToBase64")]
    public async Task<dynamic> Uploader(IFormFile file)
    {
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();
            string base64String = Convert.ToBase64String(fileBytes);
            return string.Format("data:image/jpeg;base64,{0}", base64String);
        }
    }

    /// <summary>
    /// 上传图片.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Uploader/userAvatar")]
    public async Task<dynamic> UploadImage(IFormFile file)
    {
        string? ImgType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!this.AllowImageType(ImgType))
            throw Oops.Oh(ErrorCode.D5013);
        string? filePath = FileVariable.UserAvatarFilePath;
        string? fileName = string.Format("{0}{1}{2}", DateTime.Now.ToString("yyyyMMdd"), RandomExtensions.NextLetterAndNumberString(new Random(), 5), Path.GetExtension(file.FileName));
        var stream = file.OpenReadStream();
        await _fileManager.UploadFileByType(stream, filePath, fileName);
        return new FileControlsModel { name = fileName, url = string.Format("/api/file/Image/userAvatar/{0}", fileName), fileSize = file.Length, fileExtension = ImgType };
    }

    /// <summary>
    /// 分片上传附件.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("chunk")]
    public async Task<dynamic> UploadChunk([FromForm] ChunkModel input)
    {
        if (!AllowFileType(input.extension, input.extension))
            throw Oops.Oh(ErrorCode.D1800);
        return await _fileManager.UploadChunk(input);
    }

    /// <summary>
    /// 分片组装.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("merge")]
    public async Task<dynamic> Merge([FromForm] ChunkModel input)
    {
        return await _fileManager.Merge(input);
    }
    #endregion

    #region PublicMethod

    #region 多种存储文件

    /// <summary>
    /// 根据存储类型上传文件.
    /// </summary>
    /// <param name="uploadFilePath">上传文件地址.</param>
    /// <param name="directoryPath">保存文件夹.</param>
    /// <param name="fileName">新文件名.</param>
    /// <returns></returns>
    [NonAction]
    public async Task UploadFileByType(string uploadFilePath, string directoryPath, string fileName)
    {
        FileStream? file = new FileStream(uploadFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await _fileManager.UploadFileByType(file, directoryPath, fileName);
    }
    #endregion

    /// <summary>
    /// 是否文件白名单类型.
    /// </summary>
    /// <param name="type">文件类型.</param>
    /// <returns></returns>
    [NonAction]
    public bool IsWhiteFolder(string type)
    {
        switch (type)
        {
            case "userAvatar":
            case "screenShot":
            case "banner":
            case "bg":
            case "border":
            case "source":
            case "background":
                return true;
            default:
                return false;
        }
    }

    #region kkfile 文件预览

    /// <summary>
    /// KKFile 文件预览.
    /// </summary>
    /// <param name="fileName">文件名称.</param>
    /// <param name="fileDownloadUrl">文件地址.</param>
    /// <returns></returns>
    public string KKFileUploaderPreview(string fileName, string fileDownloadUrl)
    {
        var domain = App.Configuration["JNPF_APP:Domain"];
        var filePath = (domain + "/api/File/down/" + fileName).ToBase64String();
        if (fileDownloadUrl.IsNotEmptyOrNull())
        {
            var list = fileDownloadUrl.Split('/');
            var type = list.Length > 4 ? list[4] : string.Empty;
            filePath = string.Format("{0}{1}{2}?type={3}", domain, "/api/File/down/", fileName, type).ToBase64String();
        }
        var kkFileDoMain = App.Configuration["JNPF_APP:KKFileDomain"];
        var kkurl = kkFileDoMain + "/onlinePreview?url=";

        return kkurl + filePath;
    }

    #endregion

    #region YoZo 生成 sign 方法

    /// <summary>
    /// 调用YoZo 文件预览.
    /// </summary>
    /// <param name="fileName">文件名.</param>
    /// <param name="maxNumber">最多请求次数.</param>
    /// <param name="number">当前请求次数.</param>
    /// <returns></returns>
    public async Task<string> YoZoUploaderPreview(string fileName, int maxNumber, int number)
    {
        string domain = _appOptions.YOZO.Domain;
        string uploadAPI = _appOptions.YOZO.UploadAPI;
        string downloadAPI = _appOptions.YOZO.DownloadAPI;
        string yozoAppId = _appOptions.YOZO.AppId;
        string yozoAppKey = _appOptions.YOZO.AppKey;
        string outputFilePath = string.Format("{0}/api/File/Image/annex/{1}", domain, fileName);

        Dictionary<string, string[]> dic = new Dictionary<string, string[]>();
        dic.Add("fileUrl", new string[] { outputFilePath });
        dic.Add("appId", new string[] { yozoAppId });
        string? sign = generateSign(yozoAppKey, dic);
        uploadAPI = string.Format(uploadAPI, outputFilePath, yozoAppId, sign);
        string? resStr = await uploadAPI.PostAsStringAsync();
        if (resStr.IsNotEmptyOrNull())
        {
            Dictionary<string, object>? result = resStr.ToObject<Dictionary<string, object>>();
            if (result.ContainsKey("data"))
            {
                Dictionary<string, object>? data = result["data"].ToObject<Dictionary<string, object>>();
                if (data != null)
                {
                    string? fileVersionId = data.ContainsKey("fileVersionId") ? data["fileVersionId"].ToString() : string.Empty;

                    #region 生成签名sign

                    dic = new Dictionary<string, string[]>();
                    dic.Add("fileVersionId", new string[] { fileVersionId });
                    dic.Add("appId", new string[] { yozoAppId });
                    sign = generateSign(yozoAppKey, dic);

                    #endregion

                    return string.Format(downloadAPI, fileVersionId, yozoAppId, sign);
                }
                else
                {
                    return await YoZoUploaderPreview(fileName, maxNumber, number + 1);
                }
            }
            else
            {
                if (number >= maxNumber) return string.Empty;
                else return await YoZoUploaderPreview(fileName, maxNumber, number + 1);
            }
        }
        else
        {
            if (number >= maxNumber) return string.Empty;
            else return await YoZoUploaderPreview(fileName, maxNumber, number + 1);
        }
    }

    #endregion

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 生成令牌.
    /// </summary>
    /// <param name="secret">签名.</param>
    /// <param name="paramMap">参数集合.</param>
    /// <returns></returns>
    private string generateSign(string secret, Dictionary<string, string[]> paramMap)
    {
        string fullParamStr = uniqSortParams(paramMap);
        return HmacSHA256(fullParamStr, secret);
    }

    /// <summary>
    /// uniq类型参数.
    /// </summary>
    /// <param name="paramMap"></param>
    /// <returns></returns>
    private string uniqSortParams(Dictionary<string, string[]> paramMap)
    {
        paramMap.Remove("sign");
        paramMap = paramMap.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
        StringBuilder strB = new StringBuilder();
        foreach (KeyValuePair<string, string[]> kvp in paramMap)
        {
            string key = kvp.Key;
            string[] value = kvp.Value;
            if (value.Length > 0)
            {
                Array.Sort(value);
                foreach (string temp in value)
                {
                    strB.Append(key).Append("=").Append(temp);
                }
            }
            else
            {
                strB.Append(key).Append("=");
            }

        }

        return strB.ToString();
    }

    /// <summary>
    /// 加密.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private string HmacSHA256(string data, string key)
    {
        string signRet = string.Empty;
        using (HMACSHA256 mac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(data));
            signRet = hash.ToHexString();
        }

        return signRet;
    }

    /// <summary>
    /// 允许文件类型.
    /// </summary>
    /// <param name="fileExtension">文件后缀名.</param>
    /// <param name="type">文件类型.</param>
    /// <returns></returns>
    private bool AllowFileType(string fileExtension, string type)
    {
        List<string> allowExtension = KeyVariable.AllowUploadFileType.Distinct().ToList();
        if (fileExtension.IsNullOrEmpty() || type.IsNullOrEmpty()) return false;
        if (type.Equals("weixin"))
            allowExtension = KeyVariable.WeChatUploadFileType;
        foreach (var item in Enum.GetValues(typeof(ExportFileType)))
        {
            allowExtension.Add(item.ToString());
        }
        return allowExtension.Any(a => a == fileExtension.ToLower());
    }

    /// <summary>
    /// 允许文件类型.
    /// </summary>
    /// <param name="fileExtension">文件后缀名.</param>
    /// <returns></returns>
    private bool AllowImageType(string fileExtension)
    {
        return KeyVariable.AllowImageType.Any(a => a == fileExtension.ToLower());
    }

    #endregion
}