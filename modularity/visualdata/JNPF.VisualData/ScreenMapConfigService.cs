using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.VisualData.Entity;
using JNPF.VisualData.Entitys.Dto.ScreenMap;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualData;

/// <summary>
/// 业务实现：大屏地图.
/// </summary>
[ApiDescriptionSettings(Tag = "BladeVisual", Name = "Map", Order = 160)]
[Route("api/blade-visual/[controller]")]
public class ScreenMapConfigService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualMapEntity> _visualMapRepository;

    /// <summary>
    /// 初始化一个<see cref="ScreenMapConfigService"/>类型的新实例.
    /// </summary>
    public ScreenMapConfigService(ISqlSugarRepository<VisualMapEntity> visualMapRepository)
    {
        _visualMapRepository = visualMapRepository;
    }

    #region Get

    /// <summary>
    /// 分页.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("lazy-list")]
    public async Task<dynamic> GetList([FromQuery] ScreenMapListQueryInput input)
    {
        var list = (await _visualMapRepository.AsQueryable()
            .Where(x => x.ParentId == input.parentId)
            .WhereIF(input.name.IsNotEmptyOrNull(), x => x.Name.Contains(input.name))
            .ToListAsync()).Adapt<List<ScreenMapListOutput>>();

        var parent = await _visualMapRepository.AsQueryable().FirstAsync(x => x.Id == input.parentId);
        foreach (var data in list)
        {
            if (parent.IsNotEmptyOrNull()) data.parentName = parent.Name;
            if (await _visualMapRepository.AsQueryable().AnyAsync(x => x.ParentId == data.id)) data.hasChildren = true;
        }

        return list;
    }

    /// <summary>
    /// 详情
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("detail")]
    public async Task<dynamic> GetInfo([FromQuery] string id)
    {
        VisualMapEntity? entity = await _visualMapRepository.AsQueryable().FirstAsync(v => v.Id == id);
        return entity.Adapt<ScreenMapInfoOutput>();
    }

    /// <summary>
    /// 数据详情.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonUnify]
    [HttpGet("data")]
    public dynamic GetDataInfo(string id)
    {
        VisualMapEntity? entity = _visualMapRepository.AsQueryable().First(v => v.Id == id);
        return entity.Data;
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <returns></returns>
    [HttpPost("save")]
    public async Task Create([FromBody] ScreenMapCrInput input)
    {
        VisualMapEntity? entity = input.Adapt<VisualMapEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        int isOk = await _visualMapRepository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task Update([FromBody] ScreenMapUpInput input)
    {
        VisualMapEntity? entity = input.Adapt<VisualMapEntity>();
        int isOk = await _visualMapRepository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <returns></returns>
    [HttpPost("remove")]
    public async Task Delete(string ids)
    {
        var entity = await _visualMapRepository.AsQueryable().FirstAsync(v => v.Id == ids);
        _ = entity ?? throw Oops.Oh(ErrorCode.COM1005);
        int isOk = await _visualMapRepository.AsDeleteable().Where(it => ids.Equals(it.Id)).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}