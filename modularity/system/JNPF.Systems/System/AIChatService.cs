using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.Thirdparty.AI;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.AIChat;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 数据集.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Aichat", Order = 200)]
[Route("api/system/[controller]")]
public class AIChatService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 会话列表服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<AlChatEntity> _chatRepository;

    /// <summary>
    /// 会话记录服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<AlHistoryEntity> _historyRepository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="AIChatService"/>类型的新实例.
    /// </summary>
    public AIChatService(
        ISqlSugarRepository<AlChatEntity> chatRepository,
        ISqlSugarRepository<AlHistoryEntity> historyRepository,
        IUserManager userManager)
    {
        _chatRepository = chatRepository;
        _historyRepository = historyRepository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("history/list")]
    public async Task<dynamic> GetList()
    {
        return (await _chatRepository.AsQueryable()
            .Where(x => x.DeleteMark == null && x.CreatorUserId == _userManager.UserId)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .ToListAsync()).Adapt<List<AIChatListInput>>();
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <returns></returns>
    [HttpGet("history/get/{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _historyRepository.AsQueryable()
            .Where(x => x.DeleteMark == null && x.ChatId == id)
            .ToListAsync()).Adapt<List<AIChatInfoOutput>>();
    }

    #endregion

    #region Post

    /// <summary>
    /// 请求 AI.
    /// </summary>
    /// <returns></returns>
    [HttpPost("send")]
    public async Task<dynamic> Send([FromBody] KeywordInput input)
    {
        var userQuestionList = new List<string>() { input.keyword };
        var result = await AIUtil.SendAIRequestAsync(userQuestionList);
        return result;
    }

    /// <summary>
    /// 保存.
    /// </summary>
    /// <returns></returns>
    [HttpPost("history/save")]
    public async Task Save([FromBody] AIChatSaveInput input)
    {
        var entity = input.Adapt<AlChatEntity>();
        if (entity.FullName.IsNullOrEmpty()) entity.FullName = null;
        if (entity.Id.IsNullOrEmpty())
        {
            entity.Id = SnowflakeIdHelper.NextId();
            await _chatRepository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        }
        else
        {
            await _chatRepository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }

        if (input.data.IsNotEmptyOrNull() && input.data.Count > 0)
        {
            var historyEntity = input.data.Last().Adapt<AlHistoryEntity>();
            historyEntity.ChatId = entity.Id;
            await _historyRepository.AsInsertable(historyEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("history/delete/{id}")]
    public async Task Delete(string id)
    {
        var entity = await _chatRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null) throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _chatRepository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}