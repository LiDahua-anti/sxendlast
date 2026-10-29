using JNPF.Common.Core.EventBus.Sources;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.WorkFlow;
using JNPF.EventBus;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.System;
using JNPF.WorkFlow.Entitys.Dto.Task;
using JNPF.WorkFlow.Entitys.Dto.TriggerTask;
using JNPF.WorkFlow.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.TaskFlow;

/// <summary>
/// 触发任务.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkFlowTask", Name = "Trigger", Order = 306)]
[Route("api/workflow/[controller]")]
public class TriggerTaskService
{
    private readonly ISqlSugarRepository<WorkFlowTriggerTaskEntity> _repository;

    private readonly IEventPublisher _eventPublisher;

    private readonly IUserManager _userManager;

    public TriggerTaskService(ISqlSugarRepository<WorkFlowTriggerTaskEntity> repository, IUserManager userManager, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _userManager = userManager;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// 触发任务列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Task")]
    public async Task<dynamic> GetTriggerTask([FromQuery] TriggerTaskListQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<WorkFlowTriggerTaskEntity, WorkFlowVersionEntity, WorkFlowTemplateEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.FlowId == b.Id, JoinType.Left, b.TemplateId == c.Id))
             .Where(a => a.DeleteMark == null && a.TaskId == null)
             .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword))
             .WhereIF(input.systemId.IsNotEmptyOrNull(), (a, b, c) => c.SystemId == input.systemId)
             .WhereIF(!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty(), a => SqlFunc.Between(a.StartTime, input.startTime, input.endTime))
             .OrderBy(a => a.SortCode).OrderBy(a => a.StartTime, OrderByType.Desc)
             .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
             .Select((a, b, c) => new TriggerTaskOutput
             {
                 id = a.Id,
                 fullName = a.FullName,
                 parentTime = a.ParentTime,
                 parentId = a.ParentId,
                 startTime = a.StartTime,
                 status = a.Status,
                 isRetry = a.ParentId == "0" ? 0 : 1,
                 templateStatus = c.Status,
                 systemName = SqlFunc.Subqueryable<SystemEntity>().EnableTableFilter().Where(u => u.Id == c.SystemId).Select(u => u.FullName),
             }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<TriggerTaskOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 触发任务执行记录列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Task/List")]
    public async Task<dynamic> GetTriggerTaskList([FromQuery] string taskId, [FromQuery] string nodeCode)
    {
        var triggerList = (await _repository.AsSugarClient().Queryable<WorkFlowTriggerTaskEntity>().Where(x => x.TaskId == taskId && x.NodeId == nodeCode && x.DeleteMark == null).ToListAsync()).Adapt<List<TriggerTaskListOutput>>();
        var list = new List<TriggerTaskListOutput>();
        foreach (var item in triggerList)
        {
            var recordList = (await _repository.AsSugarClient().Queryable<WorkFlowTriggerRecordEntity>().Where(x => x.TriggerId == item.id && x.DeleteMark == null).ToListAsync()).Adapt<List<TriggerRecordOutput>>();
            item.recordList = recordList;
            list.Add(item);
        }
        return list;
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("Task")]
    public async Task Delete([FromBody] TaskBatchInput input)
    {
        await _repository.AsDeleteable().In(it => it.Id, input.ids).ExecuteCommandAsync();
        await _repository.AsSugarClient().Deleteable<WorkFlowTriggerRecordEntity>().Where(x => input.ids.Contains(x.TriggerId)).ExecuteCommandAsync();
    }

    /// <summary>
    /// 重试.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("task/Retry/{id}")]
    public async Task Retry(string id)
    {
        var triggerTask = await _repository.AsSugarClient().Queryable<WorkFlowTriggerTaskEntity>().FirstAsync(x => x.Id == id && x.DeleteMark == null);
        var version = await _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().FirstAsync(x => x.Id == triggerTask.FlowId && x.DeleteMark == null);
        if (_repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>().Any(x => x.Id == version.TemplateId && x.Status != 1))
            throw Oops.Oh(ErrorCode.WF0078);
        var model = new TaskFlowEventModel();
        model.TenantId = _userManager.TenantId;
        model.UserId = triggerTask.CreatorUserId;
        model.ModelId = id;
        model.isRetry = true;
        await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTask", model));
    }
}
