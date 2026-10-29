using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Permission;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.OnineLog;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualDev;

/// <summary>
/// 数据日志.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "OnlineLog", Order = 176)]
[Route("api/visualdev/[controller]")]
public class OnlineLogService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualLogEntity> _repository;

    /// <summary>
    /// 初始化一个<see cref="OnlineLogService"/>类型的新实例.
    /// </summary>
    public OnlineLogService(
        ISqlSugarRepository<VisualLogEntity> repository)
    {
        _repository = repository;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns>返回列表.</returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] OnlineLogListInput input)
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.ModuleId == input.modelId && it.DataId == input.dataId)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new OnlineLogListOutput
            {
                id = it.Id,
                type = it.Type,
                dataLog = it.DataLog,
                headIcon = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(x => x.Id.Equals(it.CreatorUserId)).Select(x => SqlFunc.MergeString("/api/File/Image/userAvatar/", x.HeadIcon)),
                creatorTime = it.CreatorTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                creatorUserName = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(x => x.Id.Equals(it.CreatorUserId)).Select(x => x.RealName),
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<OnlineLogListOutput>.SqlSugarPageResult(list);
    }

    #endregion
}
