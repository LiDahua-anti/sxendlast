using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.System.VisualKit;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 表单套件.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Kit", Order = 216)]
[Route("api/system/[controller]")]
public class VisualKitService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualKitEntity> _repository;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 初始化一个<see cref="VisualKitService"/>类型的新实例.
    /// </summary>
    public VisualKitService(
        ISqlSugarRepository<VisualKitEntity> repository,
        IBillRullService billRuleService,
        IUserManager userManager,
        IFileManager fileManager)
    {
        _repository = repository;
        _billRuleService = billRuleService;
        _userManager = userManager;
        _fileManager = fileManager;
    }

    #region Get

    /// <summary>
    /// 获取表单套件列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] VisualKitListInput input)
    {
        var data = await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword) || x.EnCode.Contains(input.keyword))
            .WhereIF(input.category.IsNotEmptyOrNull(), x => x.Category.Equals(input.category))
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), x => x.EnabledMark.Equals(input.enabledMark))
            .OrderBy(x => x.SortCode)
            .OrderBy(x => x.CreatorTime, OrderByType.Desc)
            .OrderByIF(!input.keyword.IsNullOrEmpty(), x => x.LastModifyTime, OrderByType.Desc)
            .Select(x => new VisualKitListOutput
            {
                id = x.Id,
                fullName = x.FullName,
                enCode = x.EnCode,
                category = SqlFunc.Subqueryable<DictionaryDataEntity>().EnableTableFilter().Where(d => d.Id == x.Category).Select(d => d.FullName),
                enabledMark = x.EnabledMark,
                sortCode = x.SortCode,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == x.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                creatorTime = x.CreatorTime,
                lastModifyTime = x.LastModifyTime,
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<VisualKitListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取表单套件列表下拉.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector()
    {
        var output = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.EnabledMark == 1)
            .OrderBy(it => it.SortCode).OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new VisualKitSelectorOutput
            {
                id = it.Id,
                fullName = it.FullName,
                icon = it.Icon,
                formData = it.FormData,
                SortCode = it.SortCode,
                parentId = it.Category
            }).ToListAsync();

        var pList = new List<VisualKitSelectorOutput>();
        var parentIds = output.Select(x => x.parentId).Distinct().ToList();
        var parentData = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().Where(x => parentIds.Contains(x.Id) && x.DeleteMark == null).OrderBy(x => x.SortCode).ToListAsync();
        foreach (var item in parentData)
        {
            var pData = item.Adapt<VisualKitSelectorOutput>();
            pData.parentId = "-1";
            pList.Add(pData);
        }

        return output.Union(pList).ToList().ToTree("-1");
    }

    /// <summary>
    /// 获取表单套件信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.AsQueryable().FirstAsync(x => x.DeleteMark == null && x.Id.Equals(id))).Adapt<VisualKitInfoOutput>();
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var data = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        var jsonStr = data.ToJsonString();
        return await _fileManager.Export(jsonStr, data.FullName, ExportFileType.bvk);
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task<dynamic> Create([FromBody] VisualKitCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1032);
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1031);
        var entity = input.Adapt<VisualKitEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        if (entity.EnCode.IsNullOrEmpty()) entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeVisualKit);
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);

        return entity.Id;
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] VisualKitUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1032);
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1031);
        var entity = input.Adapt<VisualKitEntity>();
        if (entity.EnCode.IsNullOrEmpty()) entity.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeVisualKit);
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        var entity = await _repository.AsQueryable().FirstAsync(it => it.DeleteMark == null && it.Id == id);
        var random = new Random().NextLetterAndNumberString(5).ToLower();
        entity.FullName = entity.FullName + ".副本" + random;
        entity.EnCode += random;
        entity.EnabledMark = 0;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        if (entity.FullName.Length >= 50 || entity.EnCode.Length >= 50)
            throw Oops.Oh(ErrorCode.COM1009);

        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1) throw Oops.Oh(ErrorCode.COM1008);
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
        if (!fileType.ToLower().Equals(ExportFileType.bvk.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        VisualKitEntity? data;
        try
        {
            data = josn.ToObject<VisualKitEntity>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (data == null) throw Oops.Oh(ErrorCode.D3006);

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

    #endregion
}
