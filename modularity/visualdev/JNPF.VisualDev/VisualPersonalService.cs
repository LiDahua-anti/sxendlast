using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualPersonal;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualDev;

/// <summary>
/// 列表个性视图.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "Personal", Order = 177)]
[Route("api/visualdev/[controller]")]
public class VisualPersonalService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualPersonalEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="VisualPersonalService"/>类型的新实例.
    /// </summary>
    public VisualPersonalService(ISqlSugarRepository<VisualPersonalEntity> repository, IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns>返回列表.</returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] VisualPersonalListInput input)
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.MenuId == input.menuId && it.CreatorUserId == _userManager.UserId)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new VisualPersonalListOutput
            {
                id = it.Id,
                fullName = it.FullName,
                type = it.Type,
                status = it.Status,
                searchList = it.SearchList,
                columnList = it.ColumnList
            })
            .ToListAsync();

        var defaultData = new VisualPersonalEntity()
        {
            Id = "systemId",
            FullName = "系统视图",
            Type = 0,
            Status = list.Any(x => x.status.Equals(1)) ? 0 : 1
        };

        list.Insert(0, defaultData.Adapt<VisualPersonalListOutput>());

        return list;
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] VisualPersonalCrInput input)
    {
        var dataList = await _repository.AsQueryable().Where(x => x.DeleteMark == null && x.MenuId == input.menuId && x.CreatorUserId == _userManager.UserId).ToListAsync();
        if (dataList.Count >= 5) throw Oops.Oh(ErrorCode.D3201);
        if (input.fullName == "系统视图" || dataList.Any(x => x.FullName == input.fullName)) throw Oops.Oh(ErrorCode.COM1032);

        var entity = input.Adapt<VisualPersonalEntity>();
        entity.Status = 0;
        entity.Type = 1;
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] VisualPersonalUpInput input)
    {
        if (input.fullName == "系统视图" || await _repository.IsAnyAsync(x => x.Id != id && x.FullName == input.fullName && x.DeleteMark == null && x.MenuId == input.menuId && x.CreatorUserId == _userManager.UserId))
            throw Oops.Oh(ErrorCode.COM1032);
        var entity = input.Adapt<VisualPersonalEntity>();
        entity.Status = 0;
        entity.Type = 1;
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="menuId"></param>
    /// <returns></returns>
    [HttpPut("{id}/setDefault")]
    [UnitOfWork]
    public async Task SetDefault(string id, string menuId)
    {
        await _repository.AsUpdateable().SetColumns(it => it.Status == 0).Where(it => it.DeleteMark == null && it.MenuId == menuId && it.CreatorUserId == _userManager.UserId && it.Status == 1).ExecuteCommandAsync();

        if (id != "systemId")
            await _repository.AsUpdateable().SetColumns(it => it.Status == 1).Where(it => it.Id == id).ExecuteCommandAsync();
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
