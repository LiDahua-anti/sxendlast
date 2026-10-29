using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.VisualData.Entity;
using JNPF.VisualData.Entitys.Dto.ScreenAssets;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualData;

/// <summary>
/// 业务实现：静态资源.
/// </summary>
[ApiDescriptionSettings(Tag = "BladeVisual", Name = "assets", Order = 160)]
[Route("api/blade-visual/[controller]")]
public class ScreenAssetsService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualAssetsEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ScreenAssetsService"/>类型的新实例.
    /// </summary>
    public ScreenAssetsService(ISqlSugarRepository<VisualAssetsEntity> repository, IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<dynamic> GetList([FromQuery] ScreenAssetsListQueryInput input)
    {
        var list = await _repository.AsQueryable()
            .Where(x => x.SystemId.Equals(_userManager.SystemId))
            .WhereIF(input.assetsName.IsNotEmptyOrNull(), x => x.AssetsName.Contains(input.assetsName))
            .Select(x => new ScreenAssetsListOutput
            {
                id = x.Id,
                assetsName = x.AssetsName,
                assetsSize = x.AssetsSize,
                assetsTime = SqlFunc.ToString(x.AssetsTime),
                assetsType = x.AssetsType,
                assetsUrl = x.AssetsUrl,
            })
            .ToPagedListAsync(input.current, input.size);
        return new { current = list.pagination.CurrentPage, pages = list.pagination.Total / list.pagination.PageSize, records = list.list, size = list.pagination.PageSize, total = list.pagination.Total };
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("detail")]
    public async Task<dynamic> GetInfo(string id)
    {
        return await _repository.AsQueryable()
            .Where(x => x.Id == id)
            .Select(x => new ScreenAssetsInfoOutput
            {
                id = x.Id,
                assetsName = x.AssetsName,
                assetsSize = x.AssetsSize,
                assetsTime = SqlFunc.ToString(x.AssetsTime),
                assetsType = x.AssetsType,
                assetsUrl = x.AssetsUrl,
            })
            .FirstAsync();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <returns></returns>
    [HttpPost("save")]
    public async Task Create([FromBody] ScreenAssetsCrInput input)
    {
        if (input.assetsTime.IsNullOrEmpty()) input.assetsTime = null;
        var entity = input.Adapt<VisualAssetsEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.SystemId = _userManager.SystemId;
        int isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task Update([FromBody] ScreenAssetsUpInput input)
    {
        if (input.assetsTime.IsNullOrEmpty()) input.assetsTime = null;
        var entity = input.Adapt<VisualAssetsEntity>();
        int isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <returns></returns>
    [HttpPost("remove")]
    public async Task Delete(string ids)
    {
        var entity = await _repository.AsQueryable().FirstAsync(it => it.Id == ids);
        _ = entity ?? throw Oops.Oh(ErrorCode.COM1005);
        int isOk = await _repository.AsDeleteable().Where(it => it.Id == ids).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}
