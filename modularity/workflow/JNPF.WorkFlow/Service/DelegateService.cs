using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Message.Interfaces;
using JNPF.RemoteRequest;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.WorkFlow.Entitys.Dto.Delegete;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程委托.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkFlow", Name = "Delegate", Order = 300)]
[Route("api/workflow/[controller]")]
public class DelegateService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<WorkFlowDelegateEntity> _repository;
    private readonly IWorkFlowRepository _wfRepository;
    private readonly IMessageManager _messageManager;
    private readonly IOrganizeService _organizeService;
    private readonly IUserRelationService _userRelationService;
    private readonly IUserManager _userManager;

    public DelegateService(ISqlSugarRepository<WorkFlowDelegateEntity> repository, IWorkFlowRepository wfRepository, IMessageManager messageManager, IOrganizeService organizeService, IUserRelationService userRelationService, IUserManager userManager)
    {
        _repository = repository;
        _wfRepository = wfRepository;
        _messageManager = messageManager;
        _organizeService = organizeService;
        _userRelationService = userRelationService;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] DelegateQuery input)
    {
        var output = new SqlSugarPagedList<DelegeteListOutput>();
        var type = input.type == "1" || input.type == "2" ? 0 : 1;
        switch (input.type)
        {
            case "1":
            case "3":
                output = await _repository.AsQueryable().
                    Where(a => a.Type == type && a.UserId == _userManager.UserId && a.DeleteMark == null).
                    Select(a => new DelegeteListOutput
                    {
                        id = a.Id,
                        toUserId = SqlFunc.Subqueryable<WorkFlowDelegateInfoEntity>().EnableTableFilter().Where(d => d.DelegateId == a.Id).SelectStringJoin(d => d.ToUserId, ","),
                        toUserName = SqlFunc.Subqueryable<WorkFlowDelegateInfoEntity>().EnableTableFilter().Where(d => d.DelegateId == a.Id).SelectStringJoin(d => d.ToUserName, ","),
                        flowId = a.FlowId,
                        flowName = a.FlowName,
                        startTime = a.StartTime,
                        endTime = a.EndTime,
                        description = a.Description,
                        sortCode = a.SortCode,
                        creatorTime = a.CreatorTime,
                        lastModifyTime = a.LastModifyTime,
                    }).MergeTable().
                        WhereIF(!input.keyword.IsNullOrEmpty(), a => a.flowName.Contains(input.keyword) || a.toUserName.Contains(input.keyword)).
                        OrderBy(a => a.sortCode).OrderBy(x => x.creatorTime, OrderByType.Desc).
                        OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc).
                        ToPagedListAsync(input.currentPage, input.pageSize);
                foreach (var item in output.list)
                {
                    var infoList = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(d => d.DelegateId == item.id && d.DeleteMark == null).ToListAsync();
                    if (DateTime.Now > item.endTime || infoList.Count == infoList.Where(x => x.Status == 2).Count())
                    {
                        item.status = 2;
                    }
                    else if (DateTime.Now > item.startTime && infoList.Any(x => x.Status == 1))
                    {
                        item.status = 1;
                    }
                    else
                    {
                        item.status = 0;
                    }
                    item.isEdit = !infoList.Any(x => x.Status == 1);
                }
                break;
            case "2":
            case "4":
                output = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity, WorkFlowDelegateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.DelegateId == b.Id)).
                    Where((a, b) => a.ToUserId == _userManager.UserId && b.Type == type && b.DeleteMark == null).
                    Select((a, b) => new DelegeteListOutput
                    {
                        id = a.Id,
                        userName = b.UserName,
                        flowId = b.FlowId,
                        flowName = b.FlowName,
                        startTime = b.StartTime,
                        endTime = b.EndTime,
                        description = b.Description,
                        confirmStatus = a.Status,
                        delegateId = a.DelegateId,
                        sortCode = a.SortCode,
                        creatorTime = a.CreatorTime,
                        lastModifyTime = a.LastModifyTime,
                    }).MergeTable().
                        WhereIF(!input.keyword.IsNullOrEmpty(), a => a.flowName.Contains(input.keyword) || a.userName.Contains(input.keyword)).
                        OrderBy(a => a.sortCode).OrderBy(x => x.creatorTime, OrderByType.Desc).
                        OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc).
                        ToPagedListAsync(input.currentPage, input.pageSize);
                foreach (var item in output.list)
                {
                    var infoList = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(d => d.DelegateId == item.delegateId && d.DeleteMark == null).ToListAsync();
                    if (DateTime.Now > item.endTime || infoList.Count == infoList.Where(x => x.Status == 2).Count())
                    {
                        item.status = 2;
                    }
                    else if (DateTime.Now > item.startTime && infoList.Any(x => x.Status == 1))
                    {
                        item.status = 1;
                    }
                    else
                    {
                        item.status = 0;
                    }
                }
                break;
        }
        return PageResult<DelegeteListOutput>.SqlSugarPageResult(output);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("Info/{id}")]
    public async Task<dynamic> GetInfoList(string id)
    {
        return await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(x => x.DelegateId == id).Select(x => new { toUserName = x.ToUserName, status = x.Status }).ToListAsync();
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var info = (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<DelegeteInfoOutput>();
        var infoList = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(x => x.DelegateId == id).ToListAsync();
        info.toUserId = infoList.Select(x => x.ToUserId).ToList();
        info.toUserName = string.Join(",", infoList.Select(x => x.ToUserName).ToList());
        return info;
    }

    /// <summary>
    /// 发起流程委托人.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("userList")]
    public async Task<dynamic> GetFlowList([QueryString] string templateId)
    {
        var userIds = await GetUserIdByTemplateId(templateId);
        var userList = _repository.AsSugarClient()
          .Queryable<WorkFlowDelegateEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.UserId == b.Id))
          .Where((a, b) => a.Type == 0 && a.DeleteMark == null && userIds.Contains(a.UserId) && a.EndTime > DateTime.Now && a.StartTime < DateTime.Now).Select((a, b) => new UserListOutput
          {
              id = b.Id,
              headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", SqlFunc.ToString(b.HeadIcon)),
              fullName = SqlFunc.MergeString(b.RealName, "/", b.Account),
              organizeId = b.OrganizeId
          }).Distinct().ToList();
        if (!userList.Any())
            throw Oops.Oh(ErrorCode.WF0049);
        var orgList = _organizeService.GetOrgListTreeName();
        foreach (var item in userList)
        {
            if (orgList.FirstOrDefault(x => x.Id == item.organizeId).IsNotEmptyOrNull())
            {
                item.organize = orgList.FirstOrDefault(x => x.Id == item.organizeId).Description;
            }
        }
        return new { list = userList };
    }
    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        _repository.AsSugarClient().Deleteable<WorkFlowDelegateInfoEntity>().Where(x => x.DelegateId == id).ExecuteCommandHasChange();
        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="jd">新建参数.</param>
    [HttpPost("")]
    public async Task Create([FromBody] DelegeteCrInput input)
    {
        var isAck = await _repository.AsSugarClient().Queryable<SysConfigEntity>().WhereIF(input.type == "0", x => x.Key == "delegateAck" && SqlFunc.ToString(x.Value) == "1").WhereIF(input.type == "1", x => x.Key == "proxyAck" && SqlFunc.ToString(x.Value) == "1").AnyAsync();
        if (_userManager.IsAdministrator)
        {
            if (input.type == "0")
            {
                throw Oops.Oh(ErrorCode.WF0050);
            }
            else
            {
                throw Oops.Oh(ErrorCode.WF0051);
            }
        }
        await Validation(input.Adapt<DelegeteUpInput>());
        var entity = input.Adapt<WorkFlowDelegateEntity>();
        entity.UserId = _userManager.UserId;
        entity.UserName = _userManager.RealName;
        entity.Id = SnowflakeIdHelper.NextId();
        var userIdList = input.toUserId;
        var infoList = userIdList.Select(item => new WorkFlowDelegateInfoEntity
        {
            DelegateId = entity.Id,
            Status = isAck ? 0 : 1,
            ToUserId = item,
            ToUserName = _userManager.GetUserName(item)
        }).ToList();
        await _repository.AsSugarClient().Insertable(infoList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        var isOk = await _repository.AsInsertable(entity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);

        #region 发起委托/代理通知
        if (isAck)
        {
            await DelegateMsg("MBXTLC020", entity, userIdList);
        }
        else
        {
            await DelegateMsg("MBXTLC019", entity, userIdList);
        }
        #endregion
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="jd">修改参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] DelegeteUpInput input)
    {
        var isAck = await _repository.AsSugarClient().Queryable<SysConfigEntity>().WhereIF(input.type == "0", x => x.Key == "delegateAck" && SqlFunc.ToString(x.Value) == "1").WhereIF(input.type == "1", x => x.Key == "proxyAck" && SqlFunc.ToString(x.Value) == "1").AnyAsync();
        await Validation(input.Adapt<DelegeteUpInput>());
        var infoEntityList = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(x => x.DelegateId == id && x.DeleteMark == null).ToListAsync();
        if (infoEntityList.Any(x => x.Status == 1)) throw Oops.Oh(ErrorCode.WF0062);
        var userIdList = new List<string>();
        var entity = input.Adapt<WorkFlowDelegateEntity>();
        entity.UserId = _userManager.UserId;
        entity.UserName = _userManager.RealName;
        var infoList = new List<WorkFlowDelegateInfoEntity>();
        var msgUserIds = new List<string>();
        foreach (var item in input.toUserId)
        {
            var info = infoEntityList.Find(x => x.ToUserId == item);
            if (info.IsNullOrEmpty())
            {
                info = new WorkFlowDelegateInfoEntity
                {
                    DelegateId = entity.Id,
                    Status = isAck ? 0 : 1,
                    ToUserId = item,
                    ToUserName = _userManager.GetUserName(item)
                };
                msgUserIds.Add(item);
            }
            infoList.Add(info);
        }
        _repository.AsSugarClient().Deleteable<WorkFlowDelegateInfoEntity>().Where(x => x.DelegateId == id).ExecuteCommandHasChange();
        await _repository.AsSugarClient().Insertable(infoList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);

        #region 发起委托/代理通知
        if (msgUserIds.Any())
        {
            if (isAck)
            {
                await DelegateMsg("MBXTLC020", entity, msgUserIds);
            }
            else
            {
                await DelegateMsg("MBXTLC019", entity, msgUserIds);
            }
        }
        #endregion
    }

    /// <summary>
    /// 结束委托.
    /// </summary>
    /// <param name="id">id.</param>
    [HttpPut("Stop/{id}")]
    public async Task Stop(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        entity.StartTime = DateTime.Now;
        entity.EndTime = DateTime.Now;
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);

        #region 委托通知
        var userIdList = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(x => x.DelegateId == id).Select(x => x.ToUserId).ToListAsync();
        await DelegateMsg("MBXTLC021", entity, userIdList);
        #endregion
    }

    /// <summary>
    /// 确认委托/代理.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="type">1-接受 2-拒绝.</param>
    /// <returns></returns>
    [HttpPost("Notarize/{id}")]
    public async Task Notarize(string id, [QueryString] int type)
    {
        var info = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().FirstAsync(x => x.Id == id);
        if (info == null)
            throw Oops.Oh(ErrorCode.COM1005);
        info.Status = type;
        await _repository.AsSugarClient().Updateable(info).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        var entity = await _repository.GetFirstAsync(x => x.Id == info.DelegateId && x.DeleteMark == null);
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var action = type == 1 ? "接受" : "拒绝";
        await DelegateMsg("MBXTLC022", entity, new List<string> { entity.UserId }, action);
    }
    #endregion

    /// <summary>
    /// 委托验证.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task Validation(DelegeteUpInput input)
    {
        var error = input.type == "0" ? "委托" : "代理";
        foreach (var toUserId in input.toUserId)
        {
            if (_userManager.UserId.Equals(toUserId))
            {
                if (input.type == "0")
                {
                    throw Oops.Oh(ErrorCode.WF0001);
                }
                else
                {
                    throw Oops.Oh(ErrorCode.WF0069);
                }
            }
            if (_userManager.GetAdminUserId().Equals(toUserId)) throw Oops.Oh(ErrorCode.WF0061);
            //同委托人、被委托人、委托类型
            var list = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity, WorkFlowDelegateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.DelegateId == b.Id))
                    .Where((a, b) => b.UserId == _userManager.UserId && a.ToUserId == toUserId && b.Type.ToString() == input.type && b.Id != input.id && b.DeleteMark == null).Select((a, b) => b).ToListAsync();
            if (list.Any())
            {
                //同一时间段内
                list = list.FindAll(x => !((x.StartTime > input.startTime && x.StartTime > input.endTime) || (x.EndTime < input.startTime && x.EndTime < input.endTime)));
                if (list.Any())
                {
                    if (list.Any(x => x.FlowId.IsNullOrEmpty()) || input.flowId.IsNullOrEmpty())
                    {
                        throw Oops.Oh(ErrorCode.WF0041, error);
                    }
                    else
                    {
                        //非全部流程看存不存在相同流程
                        foreach (var item in input.flowId.Split(","))
                        {
                            if (list.Any(x => x.FlowId.Contains(item))) throw Oops.Oh(ErrorCode.WF0041, error);
                        }
                    }
                }
            }
            var list1 = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity, WorkFlowDelegateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.DelegateId == b.Id))
                    .Where((a, b) => b.UserId == toUserId && a.ToUserId == _userManager.UserId && b.Type.ToString() == input.type && b.Id != input.id && b.DeleteMark == null).Select((a, b) => b).ToListAsync();
            if (list1.Any())
            {
                //同一时间段内
                list1 = list1.FindAll(x => !((x.StartTime > input.startTime && x.StartTime > input.endTime) || (x.EndTime < input.startTime && x.EndTime < input.endTime)));
                if (list1.Any())
                {
                    if (list1.Any(x => x.FlowId.IsNullOrEmpty()) || input.flowId.IsNullOrEmpty())
                    {
                        throw Oops.Oh(ErrorCode.WF0042, error);
                    }
                    else
                    {
                        //非全部流程看存不存在相同流程
                        foreach (var item in input.flowId.Split(","))
                        {
                            if (list1.Any(x => x.FlowId.Contains(item))) throw Oops.Oh(ErrorCode.WF0042, error);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 委托/代理消息.
    /// </summary>
    /// <param name="enCode"></param>
    /// <param name="entity"></param>
    /// <param name="userIds"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    private async Task DelegateMsg(string enCode, WorkFlowDelegateEntity entity, List<string> userIds, string action = "")
    {
        var paramDic = new Dictionary<string, string>();
        var bodyDic = new Dictionary<string, object>();
        var parameter = new { type = entity.Type == 0 ? "2" : "4" };
        if (enCode == "MBXTLC021")
        {
            parameter = new { type = "0" };
        }
        if (enCode == "MBXTLC022")
        {
            parameter = new { type = entity.Type == 0 ? "1" : "3" };
        }
        foreach (var item in userIds)
        {
            bodyDic.Add(item, parameter);
        }
        var title = entity.Type == 0 ? "委托" : "代理";
        paramDic.Add("@Title", title);
        if (enCode == "MBXTLC022")
        {
            paramDic.Add("@Mandator", _userManager.User.RealName);
            paramDic.Add("@Action", action);
        }
        else
        {
            paramDic.Add("@Mandator", _userManager.GetUserName(entity.UserId, false));
        }

        var messageList = _messageManager.GetMessageList(enCode, userIds, paramDic, 2, bodyDic, 2);
        await _messageManager.SendDefaultMsg(userIds, messageList);
    }

    /// <summary>
    /// 获取委托人.
    /// </summary>
    /// <param name="templateId"></param>
    /// <returns></returns>
    private async Task<List<string>> GetUserIdByTemplateId(string templateId)
    {
        var flowInfo = _wfRepository.GetTemplate(templateId);
        // 选择流程
        var delegateList = await _repository.GetListAsync(x => x.Type == 0 && !SqlFunc.IsNullOrEmpty(x.FlowId) && x.FlowId.Contains(templateId) && x.EndTime > DateTime.Now && x.StartTime < DateTime.Now && x.DeleteMark == null);
        // 全部流程
        var delegateListAll = await _repository.GetListAsync(x => x.Type == 0 && x.FlowName == "全部流程" && x.EndTime > DateTime.Now && x.StartTime < DateTime.Now && x.DeleteMark == null);

        if (flowInfo.VisibleType == 2)
        {
            // 当前流程可发起人员
            var objlist = _wfRepository.GetObjIdList(templateId);
            var userIdList = _userRelationService.GetUserId(objlist);
            if (!userIdList.Any()) throw Oops.Oh(ErrorCode.WF0063);
            delegateListAll = delegateListAll.FindAll(x => userIdList.Contains(x.UserId));
        }
        delegateList = delegateList.Union(delegateListAll).ToList();
        // 委托/代理给当前用户的数据
        var delegateIds = await _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(x => x.ToUserId == _userManager.UserId && x.Status == 1).Select(x => x.DelegateId).ToListAsync();
        return delegateList.Where(x => delegateIds.Contains(x.Id)).Select(x => x.UserId).Distinct().ToList();
    }
}
