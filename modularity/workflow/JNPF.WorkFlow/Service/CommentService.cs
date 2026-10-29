using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Message;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Message.Interfaces;
using JNPF.Systems.Entitys.Permission;
using JNPF.WorkFlow.Entitys.Dto.Comment;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model.Properties;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程评论.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkFlow", Name = "Comment", Order = 304)]
[Route("api/workflow/[controller]")]
public class CommentService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<WorkFlowCommentEntity> _repository;
    private readonly IMessageManager _messageManager;
    private readonly IUserManager _userManager;

    public CommentService(
        ISqlSugarRepository<WorkFlowCommentEntity> repository,
        IMessageManager messageManager,
        IUserManager userManager)
    {
        _repository = repository;
        _messageManager = messageManager;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] CommentListQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<WorkFlowCommentEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.CreatorUserId == b.Id))
            .Where((a, b) => a.TaskId == input.taskId && a.DeleteMark == null)
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select((a, b) => new CommentListOutput()
            {
                id = a.Id,
                taskId = a.TaskId,
                text = a.DeleteShow == 1 ? "该评论已被删除" : SqlFunc.MergeString(SqlFunc.ToString(a.Text), ""),
                image = a.DeleteShow == 1 ? null : SqlFunc.MergeString(SqlFunc.ToString(a.Image), ""),
                file = a.DeleteShow == 1 ? null : SqlFunc.MergeString(SqlFunc.ToString(a.File), ""),
                creatorUserId = b.Id,
                creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                creatorUserHeadIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", SqlFunc.ToString(b.HeadIcon)),
                isDel = SqlFunc.IF(a.DeleteShow == 1).Return(2).ElseIF(a.CreatorUserId == _userManager.UserId).Return(1).End(0),
                lastModifyTime = a.LastModifyTime,
                replyUser = SqlFunc.Subqueryable<WorkFlowCommentEntity>().LeftJoin<UserEntity>((i, y) => i.CreatorUserId == y.Id).Where((i, y) => i.Id == a.ReplyId).Select((i, y) => SqlFunc.MergeString(y.RealName, "/", y.Account)),
                replyText = SqlFunc.Subqueryable<WorkFlowCommentEntity>().EnableTableFilter().Where(w => w.Id == a.ReplyId && (w.DeleteShow == 1 || w.DeleteMark != null)).Any() ? "该评论已被删除" : SqlFunc.Subqueryable<WorkFlowCommentEntity>().Where(w => w.Id == a.ReplyId).Select(w => SqlFunc.MergeString(SqlFunc.ToString(w.Text), "")),
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<CommentListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<CommentInfoOutput>();
    }
    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] CommentCrInput input)
    {
        // 消息通知人
        var msgUserIdList = new List<string>();
        var entity = input.Adapt<WorkFlowCommentEntity>();
        var isOk = await _repository.AsInsertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
        var taskEntity = _repository.AsSugarClient().Queryable<WorkFlowTaskEntity>().First(x => x.Id == entity.TaskId);
        var startPro = _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().First(x => x.FlowId == taskEntity.FlowId && x.NodeType == WorkFlowNodeTypeEnum.start.ParseToString()).NodeJson?.ToObject<NodeProperties>();
        var msgConfig = startPro.commentMsgConfig;
        if (msgConfig.on != 0)
        {
            // 是否回复评论
            var info = await _repository.GetFirstAsync(x => x.Id == input.replyId && x.DeleteMark == null);
            if (info != null && info.CreatorUserId != _userManager.UserId) msgUserIdList.Add(info.CreatorUserId);
            var accountList = StringExtensions.Substring4(entity.Text);
            foreach (var item in accountList)
            {
                var account = item.Split("/").LastOrDefault();
                var user = _repository.AsSugarClient().Queryable<UserEntity>().First(x => x.Account == account && x.EnabledMark == 1 && x.DeleteMark == null);
                if (user != null && user.Id != _userManager.UserId) msgUserIdList.Add(user.Id);
            }
            msgUserIdList = msgUserIdList.Distinct().ToList();
            if (msgUserIdList.Any())
            {
                var value = new {
                    flowId = taskEntity.FlowId,
                    taskId = taskEntity.Id,
                    operatorId = "",
                    opType = 5,
                };
                var bodyDic = new Dictionary<string, object>();
                msgUserIdList.ForEach(x => bodyDic.Add(x, value));
                if (msgConfig.on == 1 || msgConfig.on == 2)
                {
                    foreach (var item in msgConfig.templateJson)
                    {
                        item.toUser = msgUserIdList;
                        item.paramJson.Clear();
                        item.paramJson.Add(new MessageSendParam
                        {
                            field = "@Title",
                            value = "有关于您的评论，请及时查看"
                        });
                        item.paramJson.Add(new MessageSendParam
                        {
                            field = "@CreatorUserName",
                            value = _userManager.GetUserName(taskEntity.CreatorUserId)
                        });
                        await _messageManager.SendDefinedMsg(item, bodyDic);
                    }
                }
                if (startPro.commentMsgConfig.on == 3)
                {
                    var paramDic = new Dictionary<string, string>();
                    paramDic.Add("@Title", taskEntity.FullName);
                    paramDic.Add("@CreatorUserName", _userManager.GetUserName(taskEntity.CreatorUserId));
                    var messageList = _messageManager.GetMessageList("MBXTLC017", msgUserIdList, paramDic, 2, bodyDic);
                    await _messageManager.SendDefaultMsg(msgUserIdList, messageList);
                }
            }
        }
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] CommentUpInput input)
    {
        var entity = input.Adapt<WorkFlowCommentEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        var taskEntity = _repository.AsSugarClient().Queryable<WorkFlowTaskEntity>().First(x => x.Id == entity.TaskId);
        var globalPro = _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().First(x => x.FlowId == taskEntity.FlowId && x.NodeType == WorkFlowNodeTypeEnum.global.ParseToString()).NodeJson?.ToObject<GlobalProperties>();
        if (globalPro.hasCommentDeletedTips)
        {
            entity.DeleteShow = 1;
            await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        }
        else
        {
            var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (isOk < 1)
                throw Oops.Oh(ErrorCode.COM1002);
        }
    }
    #endregion
}
