using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Permission.Action;
using JNPF.Systems.Entitys.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.Permission;

/// <summary>
/// 业务实现：动作管理.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Action", Order = 172)]
[Route("api/Permission/[controller]")]
public class ActionService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ActionEntity> _repository;

    /// <summary>
    /// 初始化一个<see cref="ActionService"/>类型的新实例.
    /// </summary>
    public ActionService(ISqlSugarRepository<ActionEntity> repository)
    {
        _repository = repository;
    }

    #region Get

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PageInputBase input)
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), it => it.FullName.Contains(input.keyword) || it.EnCode.Contains(input.keyword))
            .OrderBy(it => it.Type).OrderBy(it => it.CreatorTime)
            .Select(it => new ActionListOutput()
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                type = it.Type
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<ActionListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="id">参数.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.GetFirstAsync(it => it.DeleteMark == null && it.Id == id)).Adapt<ActionInfoOutput>();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] ActionCrInput input)
    {
        var entity = input.Adapt<ActionEntity>();
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1025);
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ActionUpInput input)
    {
        var entity = input.Adapt<ActionEntity>();
        if (await _repository.IsAnyAsync(x => x.Id != input.id && x.EnCode == input.enCode && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1025);
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

    #endregion
}
