using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Entity.Permission;
using JNPF.Systems.Entitys.Entity.System;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys;
using JNPF.WorkFlow.Entitys.Dto.Delegete;
using JNPF.WorkFlow.Entitys.Dto.Operator;
using JNPF.WorkFlow.Entitys.Dto.Task;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using JNPF.WorkFlow.Factory;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using SqlSugar;
using System.Linq.Expressions;

namespace JNPF.WorkFlow.Repository;

/// <summary>
/// 流程任务数据处理.
/// </summary>
public class WorkFlowRepository : IWorkFlowRepository, ITransient
{
    private readonly ISqlSugarRepository<WorkFlowTaskEntity> _repository;
    private readonly IUserManager _userManager;
    private readonly ITenant _db;

    /// <summary>
    /// 构造.
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="userManager"></param>
    /// <param name="context"></param>
    public WorkFlowRepository(
        ISqlSugarRepository<WorkFlowTaskEntity> repository,
        IUserManager userManager,
        ISqlSugarClient context)
    {
        _repository = repository;
        _userManager = userManager;
        _db = context.AsTenant();
    }

    #region 流程列表

    /// <summary>
    /// 发起列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public dynamic GetLaunchList(TaskListQuery input)
    {
        var whereLambda = LinqExpression.And<TaskListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (!input.templateId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"))
        {
            whereLambda = whereLambda.And(a => a.systemId == _userManager.SystemId);
        }
        var statusList = new List<int>();
        if (!input.status.IsNullOrEmpty())
        {
            if (input.status == 0)
            {
                statusList = new List<int> { 0, 8, 9 };
            }
            else if (input.status == 1)
            {
                statusList = new List<int> { 1, 5 };
            }
            else
            {
                statusList = new List<int> { 2, 3, 4, 7 };
            }
            whereLambda = whereLambda.And(x => statusList.Contains(SqlFunc.ToInt32(x.status)));
        }
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.fullName.Contains(input.keyword));
        var delegateList = GetDelegateUserId(_userManager.UserId, 0);
        if (delegateList.Any())
        {
            var delegateLambda = LinqExpression.Or<TaskListOutput>();
            delegateLambda = delegateLambda.Or(x => x.creatorUserId == _userManager.UserId);
            foreach (var item in delegateList)
            {
                if (item.flowId.IsNullOrEmpty())
                {
                    delegateLambda = delegateLambda.Or(x => x.delegateUser == _userManager.UserId && SqlFunc.Between(x.startTime, item.startTime, item.endTime));
                }
                else
                {
                    delegateLambda = delegateLambda.Or(x => x.delegateUser == _userManager.UserId && SqlFunc.Between(x.startTime, item.startTime, item.endTime) && item.flowId.Contains(x.templateId));
                }
            }
            whereLambda = whereLambda.And(delegateLambda);
        }
        else
        {
            whereLambda = whereLambda.And(x => x.creatorUserId == _userManager.UserId);
        }
        var statusList1 = new List<int> { 0, 1, 5, 8, 9 };
        var list = _repository.AsSugarClient().Queryable<WorkFlowTaskEntity, WorkFlowTemplateEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id)).Where((a, b) => a.DeleteMark == null && !(statusList1.Contains(a.Status) && b.Status == 3)).Select((a, b) => new TaskListOutput()
            {
                id = a.Id,
                fullName = a.FullName,
                flowName = a.FlowName,
                startTime = a.StartTime,
                currentNodeName = a.CurrentNodeName,
                flowUrgent = a.Urgent,
                status = a.Status,
                creatorTime = a.CreatorTime,
                flowCategory = a.FlowCategory,
                creatorUserId = a.CreatorUserId,
                endTime = a.EndTime,
                flowId = a.FlowId,
                templateId = a.TemplateId,
                delegateUser = a.DelegateUserId,
                systemId = b.SystemId,
            }).MergeTable().Where(whereLambda).OrderBy(a => a.status).OrderBy(a => a.startTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<TaskListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 监控列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public dynamic GetMonitorList(TaskListQuery input)
    {
        var whereLambda = LinqExpression.And<TaskListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (input.creatorUserId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.creatorUserId == input.creatorUserId);
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (!input.templateId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (!input.isFile.IsNullOrEmpty())
        {
            if (input.isFile == 0)
            {
                whereLambda = whereLambda.And(x => x.isFile == "否");
            }
            else
            {
                whereLambda = whereLambda.And(x => x.isFile == "是");
            }
        }
        if (!input.status.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(x => x.status == input.status);
        }
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.fullName.Contains(input.keyword));
        var list = _repository.AsSugarClient().Queryable<WorkFlowTaskEntity, WorkFlowTemplateEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
            .WhereIF(input.systemId.IsNotEmptyOrNull(), (a, b) => b.SystemId == input.systemId)
            .Where((a, b) => a.Status > 0 && a.StartTime != null && a.DeleteMark == null)
            .Select((a, b) => new TaskListOutput()
            {
                id = a.Id,
                creatorTime = a.CreatorTime,
                creatorUserId = a.CreatorUserId,
                flowCategory = a.FlowCategory,
                flowId = a.FlowId,
                flowName = a.FlowName,
                flowUrgent = a.Urgent,
                fullName = a.FullName,
                startTime = a.StartTime,
                currentNodeName = a.CurrentNodeName,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                status = a.Status,
                templateId = a.TemplateId,
                flowVersion = a.FlowVersion,
                isFile = SqlFunc.IF(a.IsFile == null).Return("").ElseIF(a.IsFile == 0).Return("否").End("是"),
                systemName = SqlFunc.Subqueryable<SystemEntity>().EnableTableFilter().Where(u => u.Id == b.SystemId).Select(u => u.FullName),
            }).MergeTable().Where(whereLambda)
           .OrderBy(a => a.startTime, OrderByType.Desc)
           .ToPagedList(input.currentPage, input.pageSize);
        return PageResult<TaskListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 待签列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public dynamic GetWaitSignList(OperatorListQuery input)
    {
        var whereLambda = LinqExpression.And<OperatorListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (input.flowCategory.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (input.templateId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (input.keyword.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.fullName.Contains(input.keyword));
        if (input.creatorUserId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        if (!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"))
        {
            whereLambda = whereLambda.And(a => a.systemId == _userManager.SystemId);
        }
        var delegateList = GetDelegateUserId(_userManager.UserId, 1);
        if (delegateList.Any())
        {
            var delegateLambda = LinqExpression.Or<OperatorListOutput>();
            delegateLambda = delegateLambda.Or(x => x.handleId == _userManager.UserId);
            foreach (var item in delegateList)
            {
                if (item.flowId.IsNullOrEmpty())
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7);
                }
                else
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7 && item.flowId.Contains(x.templateId));
                }
            }
            whereLambda = whereLambda.And(delegateLambda);
        }
        else
        {
            whereLambda = whereLambda.And(x => x.handleId == _userManager.UserId);
        }
        var list = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity, WorkFlowTaskEntity, WorkFlowTemplateEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.TaskId == b.Id, JoinType.Left, b.TemplateId == c.Id))
            .Where((a, b, c) => a.Status != -1 && a.SignTime == null && a.Completion == 0 && (b.Status == 1 || b.Status == 6 || b.Status == 5) && b.DeleteMark == null && c.Status != 3)
            .Select((a, b, c) => new OperatorListOutput()
            {
                id = a.Id,
                fullName = b.FullName,
                flowName = b.FlowName,
                startTime = b.StartTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == b.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                currentNodeName = a.NodeName,
                flowUrgent = b.Urgent,
                status = 0,
                creatorTime = a.CreatorTime,
                creatorUserId = b.CreatorUserId,
                flowCategory = b.FlowCategory,
                flowId = b.FlowId,
                taskId = a.TaskId,
                templateId = b.TemplateId,
                delegateUser = a.HandleId == _userManager.UserId ? "" : a.HandleId,
                handleId = a.HandleId,
                systemId = c.SystemId,
            }).MergeTable().Where(whereLambda).OrderBy(x => x.creatorTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<OperatorListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 待办列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public dynamic GetSignList(OperatorListQuery input)
    {
        var whereLambda = LinqExpression.And<OperatorListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (input.flowCategory.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (input.templateId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (input.keyword.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.fullName.Contains(input.keyword));
        if (input.creatorUserId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.status.IsNullOrEmpty() && input.status != -2)
        {
            whereLambda = whereLambda.And(x => x.status == input.status);
        }
        if (!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"))
        {
            whereLambda = whereLambda.And(a => a.systemId == _userManager.SystemId);
        }
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        var delegateList = GetDelegateUserId(_userManager.UserId, 1);
        if (delegateList.Any())
        {
            var delegateLambda = LinqExpression.Or<OperatorListOutput>();
            delegateLambda = delegateLambda.Or(x => x.handleId == _userManager.UserId);
            foreach (var item in delegateList)
            {
                if (item.flowId.IsNullOrEmpty())
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7);
                }
                else
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7 && item.flowId.Contains(x.templateId));
                }
            }
            whereLambda = whereLambda.And(delegateLambda);
        }
        else
        {
            whereLambda = whereLambda.And(x => x.handleId == _userManager.UserId);
        }
        var list = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity, WorkFlowTaskEntity, WorkFlowTemplateEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.TaskId == b.Id, JoinType.Left, b.TemplateId == c.Id))
            .Where((a, b, c) => a.Status >= 1 && a.SignTime != null && a.Completion == 0 && a.StartHandleTime == null && (b.Status == 1 || b.Status == 6 || b.Status == 5) && b.DeleteMark == null && c.Status != 3)
            .WhereIF(!input.status.IsNullOrEmpty() && input.status == -2, a => a.Duedate != null)
            .Select((a, b, c) => new OperatorListOutput()
            {
                id = a.Id,
                fullName = b.FullName,
                flowName = b.FlowName,
                startTime = b.StartTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == b.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                currentNodeName = a.NodeName,
                flowUrgent = b.Urgent,
                status = a.Status,
                creatorTime = a.CreatorTime,
                creatorUserId = b.CreatorUserId,
                flowCategory = b.FlowCategory,
                flowId = b.FlowId,
                taskId = a.TaskId,
                nodeCode = a.NodeCode,
                templateId = b.TemplateId,
                delegateUser = a.HandleId == _userManager.UserId ? "" : a.HandleId,
                handleId = a.HandleId,
                systemId = c.SystemId,
            }).MergeTable().Where(whereLambda).OrderBy(x => x.creatorTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<OperatorListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 在办列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public dynamic GetWaitList(OperatorListQuery input)
    {
        var whereLambda = LinqExpression.And<OperatorListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (input.flowCategory.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (input.templateId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (input.keyword.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.fullName.Contains(input.keyword));
        if (input.creatorUserId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.status.IsNullOrEmpty() && input.status != -2)
        {
            whereLambda = whereLambda.And(x => x.status == input.status);
        }
        if (!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"))
        {
            whereLambda = whereLambda.And(a => a.systemId == _userManager.SystemId);
        }
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        var delegateList = GetDelegateUserId(_userManager.UserId, 1);
        if (delegateList.Any())
        {
            var delegateLambda = LinqExpression.Or<OperatorListOutput>();
            delegateLambda = delegateLambda.Or(x => x.handleId == _userManager.UserId);
            foreach (var item in delegateList)
            {
                if (item.flowId.IsNullOrEmpty())
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7);
                }
                else
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7 && item.flowId.Contains(x.templateId));
                }
            }
            whereLambda = whereLambda.And(delegateLambda);
        }
        else
        {
            whereLambda = whereLambda.And(x => x.handleId == _userManager.UserId);
        }
        var list = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity, WorkFlowTaskEntity, WorkFlowTemplateEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.TaskId == b.Id, JoinType.Left, b.TemplateId == c.Id))
            .Where((a, b, c) => a.Status >= 1 && a.SignTime != null && a.StartHandleTime != null && a.Completion == 0 && (b.Status == 1 || b.Status == 6 || b.Status == 5) && b.DeleteMark == null && c.Status != 3)
            .WhereIF(!input.status.IsNullOrEmpty() && input.status == -2, a => a.Duedate != null)
            .Select((a, b, c) => new OperatorListOutput()
            {
                id = a.Id,
                fullName = b.FullName,
                flowName = b.FlowName,
                startTime = b.StartTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == b.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                currentNodeName = a.NodeName,
                flowUrgent = b.Urgent,
                status = a.Status == 3 && a.IsProcessing == 1 ? 9 : a.Status,
                creatorTime = a.CreatorTime,
                creatorUserId = b.CreatorUserId,
                flowCategory = b.FlowCategory,
                flowId = b.FlowId,
                taskId = a.TaskId,
                nodeCode = a.NodeCode,
                templateId = b.TemplateId,
                delegateUser = a.HandleId == _userManager.UserId ? "" : a.HandleId,
                handleId = a.HandleId,
                isProcessing = a.IsProcessing,
                systemId = c.SystemId,
            }).MergeTable().Where(whereLambda).OrderBy(x => x.creatorTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<OperatorListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 批量在办列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public dynamic GetBatchWaitList(OperatorListQuery input)
    {
        var whereLambda = LinqExpression.And<OperatorListOutput>();
        whereLambda = whereLambda.And(x => x.status != 2 || x.status != 7);
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (input.flowCategory.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (input.templateId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (!input.flowId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.flowId == input.flowId);
        if (input.keyword.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.fullName.Contains(input.keyword));
        if (input.creatorUserId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        if (!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"))
        {
            whereLambda = whereLambda.And(a => a.systemId == _userManager.SystemId);
        }
        if (!input.nodeCode.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.nodeCode.Contains(input.nodeCode));
        var delegateList = GetDelegateUserId(_userManager.UserId, 1);
        if (delegateList.Any())
        {
            var delegateLambda = LinqExpression.Or<OperatorListOutput>();
            delegateLambda = delegateLambda.Or(x => x.handleId == _userManager.UserId);
            foreach (var item in delegateList)
            {
                if (item.flowId.IsNullOrEmpty())
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7);
                }
                else
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && item.flowId.Contains(x.templateId) && x.status != 7);
                }
            }
            whereLambda = whereLambda.And(delegateLambda);
        }
        else
        {
            whereLambda = whereLambda.And(x => x.handleId == _userManager.UserId);
        }
        var list = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity, WorkFlowTaskEntity, WorkFlowTemplateEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.TaskId == b.Id, JoinType.Left, b.TemplateId == c.Id))
            .Where((a, b, c) => a.Status >= 1 && a.SignTime != null && a.StartHandleTime != null && a.Completion == 0 && (b.Status == 1 || b.Status == 6 || b.Status == 5) && b.DeleteMark == null && c.Status != 3)
            .Select((a, b, c) => new OperatorListOutput()
            {
                id = a.Id,
                fullName = b.FullName,
                flowName = b.FlowName,
                startTime = b.StartTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == b.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                currentNodeName = a.NodeName,
                flowUrgent = b.Urgent,
                status = a.Status == 3 && a.IsProcessing == 1 ? 9 : a.Status,
                creatorTime = a.CreatorTime,
                creatorUserId = b.CreatorUserId,
                flowCategory = b.FlowCategory,
                flowId = b.FlowId,
                nodeName = a.NodeName,
                nodeCode = a.NodeCode,
                templateId = b.TemplateId,
                flowVersion = b.FlowVersion,
                taskId = a.TaskId,
                delegateUser = a.HandleId == _userManager.UserId ? "" : a.HandleId,
                handleId = a.HandleId,
                systemId = c.SystemId,
            }).MergeTable().Where(whereLambda).OrderBy(x => x.creatorTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<OperatorListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 已办列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    public dynamic GetTrialList(OperatorListQuery input)
    {
        var statusList = new List<int>() { 0, 1, 3, 5, 7 };
        var whereLambda = LinqExpression.And<OperatorListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (input.templateId.IsNotEmptyOrNull())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (!input.creatorUserId.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.enCode.Contains(input.keyword) || m.fullName.Contains(input.keyword));
        if (!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"))
        {
            whereLambda = whereLambda.And(a => a.systemId == _userManager.SystemId);
        }
        if (!input.status.IsNullOrEmpty())
        {
            switch (input.status)
            {
                case 1:
                    statusList = new List<int>() { 1 };
                    break;
                case 2:
                    statusList = new List<int>() { 0 };
                    break;
                case 3:
                    statusList = new List<int>() { 7 };
                    break;
                case 4:
                    statusList = new List<int>() { 5 };
                    break;
                case 5:
                    statusList = new List<int>() { 3 };
                    break;
            }
        }
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        var delegateList = GetDelegateUserId(_userManager.UserId, 1);
        if (delegateList.Any())
        {
            var delegateLambda = LinqExpression.Or<OperatorListOutput>();
            delegateLambda = delegateLambda.Or(x => x.handleId == _userManager.UserId);
            foreach (var item in delegateList)
            {
                if (item.flowId.IsNullOrEmpty())
                {
                    delegateLambda = delegateLambda.Or(x => x.delegateUser == _userManager.UserId && SqlFunc.Between(x.creatorTime, item.startTime, item.endTime));
                }
                else
                {
                    delegateLambda = delegateLambda.Or(x => x.delegateUser == _userManager.UserId && SqlFunc.Between(x.creatorTime, item.startTime, item.endTime) && item.flowId.Contains(x.templateId));
                }
            }
            whereLambda = whereLambda.And(delegateLambda);
        }
        else
        {
            whereLambda = whereLambda.And(x => x.handleId == _userManager.UserId);
        }
        var list = _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>()
            .GroupBy(it => new { it.TaskId, it.NodeCode, it.HandleId }).Where(a => statusList.Contains(a.HandleType) && a.OperatorId != null)
            .Select(a => new { TaskId = a.TaskId, TaskNodeId = a.NodeCode, HandleId = a.HandleId, HandleTime = SqlFunc.AggregateMax(a.HandleTime) })
            .MergeTable().LeftJoin<WorkFlowRecordEntity>((a, b) => a.TaskId == b.TaskId && a.TaskNodeId == b.NodeCode && a.HandleId == b.HandleId)
            .LeftJoin<WorkFlowTaskEntity>((a, b, c) => b.TaskId == c.Id).LeftJoin<WorkFlowOperatorEntity>((a, b, c, d) => b.OperatorId == d.Id)
            .LeftJoin<WorkFlowTemplateEntity>((a, b, c, d, e) => e.Id == c.TemplateId)
            .Where((a, b, c) => a.HandleTime == b.HandleTime && statusList.Contains(b.HandleType) && b.OperatorId != null)
            .Select((a, b, c, d, e) => new OperatorListOutput()
            {
                enCode = c.EnCode,
                creatorUserId = c.CreatorUserId,
                creatorTime = b.HandleTime,
                currentNodeName = b.NodeName,
                flowCategory = c.FlowCategory,
                fullName = c.FullName,
                flowName = c.FlowName,
                status = b.HandleType,
                id = b.Id,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == c.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                flowCode = c.FlowCode,
                flowId = c.FlowId,
                flowUrgent = c.Urgent,
                startTime = c.StartTime,
                templateId = c.TemplateId,
                taskId = b.TaskId,
                handleId = b.HandleId,
                systemId = e.SystemId,
                delegateUser = b.HandleId != null && b.CreatorUserId != d.HandleId && c.Id != null && b.HandleType != 7 ? b.CreatorUserId : null,
            }).MergeTable().Where(whereLambda).OrderBy(a => a.creatorTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<OperatorListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 抄送列表.
    /// </summary>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    public dynamic GetCirculateList(OperatorListQuery input)
    {
        var whereLambda = LinqExpression.And<OperatorListOutput>();
        if (!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty())
        {
            whereLambda = whereLambda.And(a => SqlFunc.Between(a.startTime, input.startTime, input.endTime));
        }
        if (!input.flowCategory.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => input.flowCategory.Contains(x.flowCategory));
        if (!input.templateId.IsNullOrEmpty())
            whereLambda = whereLambda.And(x => x.templateId == input.templateId);
        if (!input.creatorUserId.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.creatorUserId.Contains(input.creatorUserId));
        if (!input.keyword.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.enCode.Contains(input.keyword) || m.fullName.Contains(input.keyword));
        if (!input.status.IsNullOrEmpty())
            whereLambda = whereLambda.And(m => m.isRead == input.status);
        if (!(_userManager.AppCode == "" || _userManager.AppCode == "workFlow"))
        {
            whereLambda = whereLambda.And(a => a.systemId == _userManager.SystemId);
        }
        if (!input.flowUrgent.IsNullOrEmpty())
        {
            if (input.flowUrgent == 1)
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent || x.flowUrgent == 0);
            }
            else
            {
                whereLambda = whereLambda.And(x => x.flowUrgent == input.flowUrgent);
            }
        }
        var list = _repository.AsSugarClient().Queryable<WorkFlowTaskEntity, WorkFlowCirculateEntity, UserEntity, WorkFlowTemplateEntity>((a, b, c, d) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, a.CreatorUserId == c.Id, JoinType.Left, a.TemplateId == d.Id))
            .Where((a, b) => b.UserId == _userManager.UserId)
            .Select((a, b, c, d) => new OperatorListOutput()
            {
                enCode = a.EnCode,
                creatorUserId = a.CreatorUserId,
                creatorTime = b.CreatorTime,
                currentNodeName = b.NodeName,
                flowCategory = a.FlowCategory,
                fullName = a.FullName,
                flowName = a.FlowName,
                status = a.Status,
                id = b.Id,
                creatorUser = SqlFunc.MergeString(c.RealName, "/", c.Account),
                flowCode = a.FlowCode,
                flowId = a.FlowId,
                flowUrgent = a.Urgent,
                startTime = a.StartTime,
                templateId = a.TemplateId,
                taskId = a.Id,
                systemId = d.SystemId,
                isRead = b.Read,
            }).MergeTable().Where(whereLambda).OrderBy(x => x.creatorTime, OrderByType.Desc).ToPagedList(input.currentPage, input.pageSize);
        return PageResult<OperatorListOutput>.SqlSugarPageResult(list);
    }
    #endregion

    #region 其他

    /// <summary>
    /// 流程信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public FlowModel GetFlowInfo(string id)
    {
        var flowModel = _repository.AsSugarClient().Queryable<WorkFlowVersionEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
             .Where((a, b) => a.Id == id && a.DeleteMark == null && b.DeleteMark == null).Select((a, b) => new FlowModel()
             {
                 id = a.TemplateId,
                 templateId = a.TemplateId,
                 flowId = a.Id,
                 version = a.Version,
                 type = b.Type,
                 enCode = b.EnCode,
                 category = b.Category,
                 fullName = b.FullName,
                 flowableId = a.FlowableId,
                 configuration = b.FlowConfig,
                 visibleType = b.VisibleType,
                 flowXml = a.Xml,
                 status = b.Status,
             }).First();
        if (flowModel.IsNotEmptyOrNull())
        {
            flowModel.flowConfig = flowModel.configuration.ToObject<FlowConfig>();
        }
        return flowModel;
    }

    /// <summary>
    /// 获取工作交接列表.
    /// </summary>
    /// <param name="userId">离职人.</param>
    /// <param name="type">1-待办事宜 2-负责流程.</param>
    /// <returns></returns>
    public List<FlowWorkModel> GetWorkHandover(string userId, int type)
    {
        var workHandoverList = new List<FlowWorkModel>();
        if (type == 1)//待办
        {
            workHandoverList = _repository.AsSugarClient().Queryable<WorkFlowTaskEntity, WorkFlowOperatorEntity, WorkFlowTemplateEntity>((a, b, c) =>
            new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, a.TemplateId == c.Id)).Where((a, b, c) => a.DeleteMark == null && b.HandleId == userId && b.Status != -1 && b.Completion == 0 && c.Status != 3)
            .OrderByDescending(a => a.Id)
            .Select((a, b) => new FlowWorkModel()
            {
                id = a.Id,
                fullName = a.FullName,
                icon = SqlFunc.Subqueryable<WorkFlowTemplateEntity>().EnableTableFilter().Where(u => u.Id == a.TemplateId).Select(u => u.Icon),
            }).Distinct().ToList();
        }
        else if (type == 2)
        {
            var flowIds = _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(x => x.NodeJson.Contains(userId) && x.DeleteMark == null).Select(x => x.FlowId).ToList();
            workHandoverList = _repository.AsSugarClient().Queryable<WorkFlowVersionEntity, WorkFlowTemplateEntity>((a, b) =>
            new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id)).Where((a, b) => a.Status == 1 && a.DeleteMark == null && b.DeleteMark == null && flowIds.Contains(a.Id) && b.Status == 1)
            .OrderByDescending(a => a.CreatorTime)
            .Select((a, b) => new FlowWorkModel()
            {
                id = a.Id,
                fullName = SqlFunc.MergeString(b.FullName, "(V", a.Version, ")"),
                icon = b.Icon,
            }).ToList();
        }
        return workHandoverList;
    }

    /// <summary>
    /// 保存工作交接数据.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="ids"></param>
    /// <param name="type"></param>
    public void SaveWorkHandover(string userId, List<string> ids, int type, string handOverUserId = "")
    {
        switch (type)
        {
            case 1:
                _repository.AsSugarClient().Updateable<WorkFlowOperatorEntity>().SetColumns(x => x.HandleId == userId).SetColumns(x => x.HandleAll == SqlFunc.Replace(x.HandleAll, handOverUserId, userId)).Where(x => ids.Contains(x.TaskId) && (x.HandleId == handOverUserId || x.HandleAll.Contains(handOverUserId))).ExecuteCommand();
                _repository.AsSugarClient().Updateable<WorkFlowCandidatesEntity>().SetColumns(x => x.Candidates == SqlFunc.Replace(x.Candidates, handOverUserId, userId)).Where(x => ids.Contains(x.TaskId)).ExecuteCommand();
                break;
            default:
                var userName = _userManager.GetUserName(userId);
                var handOverUserName = _userManager.GetUserName(handOverUserId);
                _repository.AsSugarClient().Updateable<WorkFlowNodeEntity>().SetColumns(x => x.NodeJson == SqlFunc.Replace(SqlFunc.Replace(x.NodeJson, handOverUserId, userId), handOverUserName, userName)).Where(x => ids.Contains(x.FlowId)).ExecuteCommand();
                break;
        }
    }

    /// <summary>
    /// 获取组织树.
    /// </summary>
    /// <param name="orgId"></param>
    /// <returns></returns>
    public List<string> GetOrgTree(string orgId)
    {
        return _repository.AsSugarClient().Queryable<OrganizeEntity>().First(x => x.Id == orgId && x.DeleteMark == null).OrganizeIdTree.Split(",").ToList();
    }

    /// <summary>
    /// 流程任务列表.
    /// </summary>
    /// <returns></returns>
    public List<OperatorListOutput> GetFlowTaskList(int type = 2, List<string> categoryList = null)
    {
        if (type == 3)
        {
            return _repository.AsSugarClient().Queryable<WorkFlowTaskEntity, WorkFlowRecordEntity, WorkFlowOperatorEntity, UserEntity>(
                        (a, b, c, d) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId, JoinType.Left, b.OperatorId == c.Id, JoinType.Left, c.HandleId == d.Id))
                        .Where((a, b, c) => b.HandleType < 2 && b.OperatorId != null && b.HandleId == _userManager.UserId)
                         .WhereIF(categoryList != null && categoryList.Any(), (a, b) => categoryList.Contains(a.FlowCategory))
                        .Select((a, b, c, d) => new OperatorListOutput()
                        {
                            id = b.Id,
                            enCode = a.EnCode,
                            fullName = a.FullName,
                            flowId = a.FlowId,
                            flowCode = a.FlowCode,
                            flowName = a.FlowName,
                            flowCategory = a.FlowCategory,
                            startTime = a.StartTime,
                            currentNodeName = b.NodeName,
                            status = b.HandleType,
                            creatorTime = b.HandleTime,
                            creatorUserId = a.CreatorUserId,
                        }).ToList();
        }
        if (type == 4)
        {
            return _repository.AsSugarClient().Queryable<WorkFlowTaskEntity, WorkFlowCirculateEntity>(
                (a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.TaskId))
                .Where((a, b) => b.UserId == _userManager.UserId).WhereIF(categoryList != null && categoryList.Any(), (a, b) => categoryList.Contains(a.FlowCategory))
                .Select((a, b) => new OperatorListOutput()
                {
                    id = b.Id,
                    enCode = a.EnCode,
                    fullName = a.FullName,
                    flowId = a.FlowId,
                    flowCode = a.FlowCode,
                    flowName = a.FlowName,
                    flowCategory = a.FlowCategory,
                    startTime = a.StartTime,
                    currentNodeName = b.NodeName,
                    creatorUserId = a.CreatorUserId,
                }).ToList();
        }
        if (type == 5)
        {
            var statusList1 = new List<int> { 0, 1, 5, 8, 9 };
            return _repository.AsSugarClient().Queryable<WorkFlowTaskEntity, WorkFlowTemplateEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id)).Where((a, b) => a.DeleteMark == null && a.CreatorUserId == _userManager.UserId && !(statusList1.Contains(a.Status) && b.Status == 3))
            .Select((a, b) => new OperatorListOutput()
            {
                id = a.Id,
                fullName = a.FullName,
                flowName = a.FlowName,
                startTime = a.StartTime,
                currentNodeName = a.CurrentNodeName,
                flowUrgent = a.Urgent,
                status = a.Status,
                creatorTime = a.CreatorTime,
                flowCategory = a.FlowCategory,
                creatorUserId = a.CreatorUserId,
                flowId = a.FlowId,
                templateId = a.TemplateId,
                delegateUser = a.DelegateUserId,
                systemId = b.SystemId,
            }).ToList();
        }
        var whereLambda = LinqExpression.And<OperatorListOutput>();
        var delegateList = GetDelegateUserId(_userManager.UserId, 1);
        if (delegateList.Any())
        {
            var delegateLambda = LinqExpression.Or<OperatorListOutput>();
            delegateLambda = delegateLambda.Or(x => x.handleId == _userManager.UserId);
            foreach (var item in delegateList)
            {
                if (item.flowId.IsNullOrEmpty())
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7);
                }
                else
                {
                    delegateLambda = delegateLambda.Or(x => x.handleId == item.userId && x.status != 7 && item.flowId.Contains(x.templateId));
                }
            }
            whereLambda = whereLambda.And(delegateLambda);
        }
        else
        {
            whereLambda = whereLambda.And(x => x.handleId == _userManager.UserId);
        }
        return _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity, WorkFlowTaskEntity, WorkFlowTemplateEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.TaskId == b.Id, JoinType.Left, b.TemplateId == c.Id))
            .Where((a, b, c) => a.Status >= 1 && a.Completion == 0 && (b.Status == 1 || b.Status == 5 || b.Status == 6) && b.DeleteMark == null && c.Status != 3)
            .WhereIF(type == 0, (a, b) => a.SignTime == null)
            .WhereIF(type == 1, (a, b) => a.StartHandleTime == null && a.SignTime != null)
            .WhereIF(type == 2, (a, b) => a.SignTime != null && a.StartHandleTime != null)
            .WhereIF(categoryList != null && categoryList.Any(), (a, b) => categoryList.Contains(b.FlowCategory))
            .Select((a, b) => new OperatorListOutput()
            {
                id = a.Id,
                fullName = b.FullName,
                flowName = b.FlowName,
                startTime = b.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == b.CreatorUserId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                currentNodeName = b.CurrentNodeName,
                flowUrgent = b.Urgent,
                status = a.Status == 3 && a.IsProcessing == 1 ? 9 : a.Status,
                creatorTime = a.CreatorTime,
                creatorUserId = b.CreatorUserId,
                flowCategory = b.FlowCategory,
                flowId = b.FlowId,
                taskId = a.TaskId,
                nodeCode = a.NodeCode,
                templateId = b.TemplateId,
                delegateUser = a.HandleId == _userManager.UserId ? "" : a.HandleId,
                handleId = a.HandleId,
            }).MergeTable().Where(whereLambda).ToList();
    }

    /// <summary>
    /// 表单信息.
    /// </summary>
    /// <param name="formId"></param>
    /// <returns></returns>
    public VisualDevReleaseEntity GetFromEntity(string formId)
    {
        return _repository.AsSugarClient().Queryable<VisualDevReleaseEntity>().First(a => a.Id == formId && a.DeleteMark == null);
    }

    /// <summary>
    /// 流程列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<WorkFlowVersionEntity> GetFlowList(Expression<Func<WorkFlowVersionEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowVersionEntity>().Where(expression).ToList();
    }

    /// <summary>
    /// 任务相关人员列表.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <returns></returns>
    public List<string> GetTaskUserList(string taskId)
    {
        var userIdList = new List<string>();
        var flowTaskEntity = _repository.GetFirst(x => x.DeleteMark == null && x.Id == taskId);
        if (flowTaskEntity.IsNotEmptyOrNull()) userIdList.Add(flowTaskEntity.CreatorUserId + "--user");
        var handleIds = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity>().Where(x => x.TaskId == taskId && x.Status != -1).Select(x => SqlFunc.MergeString(x.HandleId, "--user")).ToList();
        userIdList = userIdList.Union(handleIds).ToList();
        var recordUserIds = _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>().Where(x => x.TaskId == taskId).Select(x => SqlFunc.MergeString(x.HandleId, "--user")).ToList();
        userIdList = userIdList.Union(recordUserIds).ToList();
        var circulateUserIds = _repository.AsSugarClient().Queryable<WorkFlowCirculateEntity>().Where(x => x.TaskId == taskId).Select(x => SqlFunc.MergeString(x.UserId, "--user")).ToList();
        userIdList = userIdList.Union(circulateUserIds).Distinct().ToList();
        userIdList.Remove(_userManager.GetAdminUserId());
        return userIdList;
    }

    /// <summary>
    /// 可发起流程.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="systemId">应用id（多个用）.</param>
    /// <param name="isAll">是否包含公开流程.</param>
    /// <returns></returns>
    public List<string> GetFlowIdList(string userId, List<string> systemIds = null, bool isAll = true)
    {
        var groupIds = _userManager.GetPermissionIds(userId, isAll); // 委托给我的不需要验证委托人的身份
        var flowIds = _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(x => groupIds.Contains(x.ObjectId) && x.ItemType == CommonConst.AuthorizeItemFlow).Select(x => x.ItemId).ToList();
        var whereLambda = LinqExpression.And<WorkFlowTemplateEntity>();
        whereLambda = whereLambda.And(x => flowIds.Contains(x.Id) || x.VisibleType == 1); // 列表过滤委托人无权限的委托
        if (systemIds.IsNotEmptyOrNull())
        {
            whereLambda = whereLambda.And(x => systemIds.Contains(x.SystemId));
        }
        return _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>().Where(whereLambda).Where(x => x.EnabledMark == 1 && x.Status == 1 && x.DeleteMark == null).Select(x => x.Id).ToList();
    }

    /// <summary>
    /// 权限设置流程可发起人员.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    public List<string> GetObjIdList(string templateId)
    {
        var groupIds = _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(x => x.ItemId == templateId && x.ItemType == CommonConst.AuthorizeItemFlow).Select(x => x.ObjectId).ToList();
        var orgIds = _repository.AsSugarClient().Queryable<RoleRelationEntity>().Where(x => groupIds.Contains(x.RoleId)).Select(x => x.ObjectId).ToList();
        groupIds.AddRange(orgIds);
        groupIds = groupIds.Distinct().ToList();
        var userIds = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => groupIds.Contains(x.ObjectId) || groupIds.Contains(x.UserId)).Select(x => x.UserId).ToList();
        return userIds;
    }

    /// <summary>
    /// 是否存在归档文件.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public bool AnyFile(string taskId)
    {
        return _repository.AsSugarClient().Queryable<DocumentEntity>().Any(x => x.Description.Contains(taskId));
    }

    /// <summary>
    /// 常用语使用次数.
    /// </summary>
    /// <param name="handleOpinion"></param>
    public void SetCommonWordsCount(string handleOpinion)
    {
        _repository.AsSugarClient().Updateable<CommonWordsEntity>().SetColumns(it => it.UsesNum == it.UsesNum + 1).Where(x => x.CommonWordsType == 1 && x.CreatorUserId == _userManager.UserId && x.CommonWordsText == handleOpinion).ExecuteCommand();
    }

    /// <summary>
    /// 设置默认签名.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="signImg"></param>
    public void SetDefaultSignImg(string id, string signImg, bool useSignNext)
    {
        if (useSignNext)
        {
            if (!_repository.AsSugarClient().Queryable<SignImgEntity>().Any(x => x.Id == id))
            {
                var signImgEntity = new SignImgEntity();
                signImgEntity.SignImg = signImg;
                signImgEntity.IsDefault = 1;
                _repository.AsSugarClient().Updateable<SignImgEntity>().SetColumns(x => x.IsDefault == 0).Where(x => x.CreatorUserId == _userManager.UserId).ExecuteCommand();
                _repository.AsSugarClient().Insertable(signImgEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteReturnEntity();
            }
            else
            {
                _repository.AsSugarClient().Updateable<SignImgEntity>().SetColumns(x => x.IsDefault == 0).Where(x => x.Id != id && x.CreatorUserId == _userManager.UserId).ExecuteCommand();
                _repository.AsSugarClient().Updateable<SignImgEntity>().SetColumns(x => x.IsDefault == 1).Where(x => x.Id == id).ExecuteCommand();
            }
        }
    }

    /// <summary>
    /// 数据字典.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public DictionaryDataEntity GetDictionaryData(string id)
    {
        return _repository.AsSugarClient().Queryable<DictionaryDataEntity>().First(x => x.Id == id && x.EnabledMark == 1 && x.DeleteMark == null);
    }

    /// <summary>
    /// 流程模版.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isFilterStatus">是否过滤状态</param>
    /// <returns></returns>
    public WorkFlowTemplateEntity GetTemplate(string id, bool isFilterStatus = true)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowTemplateEntity>().WhereIF(isFilterStatus, x => x.Status == 1).First(x => x.Id == id && x.EnabledMark == 1 && x.DeleteMark == null);
    }

    /// <summary>
    /// 系统配置.
    /// </summary>
    /// <returns></returns>
    public SysConfigOutput GetSysConfigInfo()
    {
        var array = new Dictionary<string, string>();
        var data = _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Category.Equals("SysConfig")).ToList();
        foreach (var item in data)
        {
            if (!array.ContainsKey(item.Key)) array.Add(item.Key, item.Value);
        }

        return array.ToObject<SysConfigOutput>();
    }

    /// <summary>
    /// 获取归档文件.
    /// </summary>
    /// <param name="flowId"></param>
    /// <param name="userId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public dynamic GetFileList(string templateId, string userId)
    {
        var docIds = _repository.AsSugarClient().Queryable<DocumentShareEntity>().Where(x => x.ShareUserId == userId).Select(x => x.DocumentId).ToList();
        return _repository.AsSugarClient().Queryable<DocumentEntity>()
            .Where(x => x.EnabledMark == 1 && x.Description.Contains(templateId) && (x.CreatorUserId == userId || docIds.Contains(x.Id)))
            .OrderByDescending(x => x.CreatorTime)
            .Select(x => new {
                fileName = x.FullName,
                fileDate = x.CreatorTime,
                uploaderUrl = x.UploadUrl,
                id = x.Id
            }).Take(5).ToList();
    }

    /// <summary>
    /// 模版使用次数.
    /// </summary>
    /// <param name="templateId"></param>
    public void SaveTemplateUseNum(string templateId)
    {
        var entity = _repository.AsSugarClient().Queryable<WorkFlowTemplateUseNumEntity>().First(x => x.TemplateId == templateId && x.UserId == _userManager.UserId && x.DeleteMark == null);
        if (entity.IsNotEmptyOrNull())
        {
            _repository.AsSugarClient().Updateable<WorkFlowTemplateUseNumEntity>().SetColumns(it => it.UseNum == it.UseNum + 1).SetColumns(it => it.LastModifyTime == DateTime.Now).Where(it => it.Id == entity.Id).ExecuteCommand();
        }
        else
        {
            entity = new WorkFlowTemplateUseNumEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                TemplateId = templateId,
                UserId = _userManager.UserId,
                UseNum = 1,
                LastModifyTime = DateTime.Now
            };
            _repository.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteReturnEntity();
        }
    }

    /// <summary>
    /// 菜单.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<ModuleEntity> GetModuleList(Expression<Func<ModuleEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<ModuleEntity>().Where(expression).ToList();
    }

    /// <summary>
    /// 根据类型获取权限id.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<List<string>> GetAuthorIdList(string objectType, string userId = "")
    {
        var authorIds = await _userManager.GetUserAuthorizeItemIdList(objectType, userId);
        switch (objectType)
        {
            case CommonConst.AuthorizeItemSystem:
                return await _repository.AsSugarClient().Queryable<SystemEntity>().Where(x => x.DeleteMark == null && x.IsMain.Equals(0) && authorIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
            default:
                return authorIds;
        }
    }
    #endregion

    #region 流程任务

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="flowId">引擎id.</param>
    /// <returns></returns>
    public List<WorkFlowTaskEntity> GetTaskList(string flowId)
    {
        return _repository.GetList(x => x.DeleteMark == null && x.FlowId == flowId);
    }

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    public List<WorkFlowTaskEntity> GetTaskList(Expression<Func<WorkFlowTaskEntity, bool>> expression)
    {
        return _repository.GetList(expression);
    }

    /// <summary>
    /// 获取流程下所有子流程
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="isDelParentTask"></param>
    /// <returns></returns>
    public List<WorkFlowTaskEntity> GetChildTaskList(string taskId, bool isDelParentTask = false)
    {
        var childTaskList = _repository.AsSugarClient().Queryable<WorkFlowTaskEntity>().ToChildList(x => x.ParentId, taskId, !isDelParentTask);
        return childTaskList;
    }

    /// <summary>
    /// 任务信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public WorkFlowTaskEntity GetTaskInfo(string id)
    {
        if (_repository.AsSugarClient().CurrentConnectionConfig.DbType == DbType.SqlServer)
        {
            return _repository.GetFirst(x => x.DeleteMark == null && x.Id == id);
        }
        return _repository.GetFirst(x => x.DeleteMark == null && x.Id == id);
    }

    /// <summary>
    /// 是否存在任务.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    public bool AnyTask(Expression<Func<WorkFlowTaskEntity, bool>> expression)
    {
        return _repository.IsAny(expression);
    }

    /// <summary>
    /// 任务创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public WorkFlowTaskEntity CreateTask(WorkFlowTaskEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowTaskEntity>().AsInsertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntity();
    }

    /// <summary>
    /// 任务更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool UpdateTask(WorkFlowTaskEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowTaskEntity>().AsUpdateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChange();
    }

    /// <summary>
    /// 任务更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool UpdateTask(WorkFlowTaskEntity entity, Expression<Func<WorkFlowTaskEntity, object>> Expression = null)
    {
        return _repository.AsSugarClient().Updateable(entity).UpdateColumns(Expression).ExecuteCommandHasChange();
    }

    /// <summary>
    /// 打回流程删除所有相关数据.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="isClearRecord">是否清除记录.</param>
    /// <param name="isClearCandidates">是否清除候选人.</param>
    /// <returns></returns>
    public void DeleteFlowTaskAllData(string taskId, bool isClearRecord = true, bool isClearCandidates = true)
    {
        _repository.AsSugarClient().Updateable<WorkFlowOperatorEntity>().SetColumns(x => x.Status == -1).Where(x => x.TaskId == taskId).ExecuteCommand();
        if (isClearRecord)
            _repository.AsSugarClient().Updateable<WorkFlowRecordEntity>().SetColumns(x => x.Status == -1).Where(x => x.TaskId == taskId).ExecuteCommand();
        if (isClearCandidates)
            _repository.AsSugarClient().Deleteable<WorkFlowCandidatesEntity>(x => x.TaskId == taskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowLaunchUserEntity>(x => x.TaskId == taskId && x.Type == 2).ExecuteCommand();
    }

    /// <summary>
    /// 删除子流程.
    /// </summary>
    /// <param name="flowTaskEntity"></param>
    /// <returns></returns>
    public async Task DeleteSubTask(WorkFlowTaskEntity flowTaskEntity)
    {
        var entityList = GetTaskList(x => x.ParentId == flowTaskEntity.Id);
        if (entityList.Any())
        {
            foreach (var item in entityList)
            {
                await DeleteTask(item);
            }
        }
    }

    /// <summary>
    /// 任务删除.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public async Task<int> DeleteTask(WorkFlowTaskEntity entity, bool isDel = true)
    {
        entity.DeleteTime = DateTime.Now;
        entity.DeleteMark = 1;
        entity.DeleteUserId = _userManager.UserId;
        _repository.AsSugarClient().Deleteable<WorkFlowOperatorEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowRecordEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowCirculateEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowCandidatesEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowCommentEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowNodeRecordEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowLaunchUserEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        if (GetRevoke(x => x.RevokeTaskId == entity.Id).IsNotEmptyOrNull())
        {
            _repository.AsSugarClient().Deleteable<WorkFlowRevokeEntity>(x => entity.Id == x.RevokeTaskId).ExecuteCommand();
        }
        _repository.AsSugarClient().Deleteable<WorkFlowTaskLineEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowTriggerTaskEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowTriggerRecordEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        _repository.AsSugarClient().Deleteable<WorkFlowEventLogEntity>(x => entity.Id == x.TaskId).ExecuteCommand();
        if (entity.InstanceId.IsNotEmptyOrNull())
        {
            await BpmnEngineFactory.CreateBmpnEngine().InstanceDelete(entity.InstanceId);
        }
        if (isDel)
        {
            return _repository.Delete(x => x.Id == entity.Id) ? 1 : 0;
        }
        else
        {
            return _repository.AsSugarClient().Updateable(entity).UpdateColumns(it => new { it.DeleteTime, it.DeleteMark, it.DeleteUserId }).ExecuteCommand();
        }
    }

    /// <summary>
    /// 发起任务删除.
    /// </summary>
    /// <param name="taskIds"></param>
    /// <returns></returns>
    public async Task<List<string>> DeleteLaunchTask(List<string> taskIds)
    {
        var taskIdList = new List<string>();
        var error = false;
        foreach (var id in taskIds)
        {
            var taskEntity = GetTaskInfo(id);
            if (taskEntity.IsNotEmptyOrNull())
            {
                if (!taskIdList.Any() && id == taskIds.Last()) error = true;

                if (!taskEntity.ParentId.Equals("0") && taskEntity.ParentId.IsNotEmptyOrNull())
                {
                    if (error)
                        throw Oops.Oh(ErrorCode.WF0003, taskEntity.FullName);
                    else
                        continue;
                }
                if (taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt())
                {
                    if (error)
                        throw Oops.Oh(ErrorCode.WF0046);
                    else
                        continue;
                }
                if (!(taskEntity.Status == WorkFlowTaskStatusEnum.Draft.ParseToInt() || taskEntity.Status == WorkFlowTaskStatusEnum.Recall.ParseToInt()))
                {
                    if (error)
                        throw Oops.Oh(ErrorCode.WF0024);
                    else
                        continue;
                }

                await DeleteTask(taskEntity);
                taskIdList.Add(id);
            }
            else
            {
                taskIdList.Add(id);
            }
        }

        return taskIdList;
    }
    #endregion

    #region 流程节点

    /// <summary>
    /// 节点列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    public List<WorkFlowNodeEntity> GetNodeList(Expression<Func<WorkFlowNodeEntity, bool>> expression, Expression<Func<WorkFlowNodeEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Where(expression).OrderByIF(orderByExpression.IsNotEmptyOrNull(), orderByExpression, orderByType).ToList();
    }

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool AnyNode(Expression<Func<WorkFlowNodeEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().Any(expression);
    }

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public WorkFlowNodeEntity GetNodeInfo(Expression<Func<WorkFlowNodeEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowNodeEntity>().First(expression);
    }
    #endregion

    #region 流程经办

    /// <summary>
    /// 经办列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    public List<WorkFlowOperatorEntity> GetOperatorList(Expression<Func<WorkFlowOperatorEntity, bool>> expression, Expression<Func<WorkFlowOperatorEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity>().Where(expression).OrderByIF(orderByExpression.IsNotEmptyOrNull(), orderByExpression, orderByType).ToList();
    }

    /// <summary>
    /// 获取加签数据.
    /// </summary>
    /// <param name="opId">当前经办id.</param>
    /// <param name="type">0-父级 1-子级.</param>
    /// <param name="isContainOneself">是否包含自己.</param>
    /// <returns></returns>
    public List<WorkFlowOperatorEntity> GetAddSignOperatorList(string opId, int type = 0, bool isContainOneself = true)
    {
        var list = new List<WorkFlowOperatorEntity>();
        if (type == 0)
        {
            list = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity>().ToChildList(x => x.ParentId, opId, isContainOneself);
        }
        else
        {
            list = _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity>().ToParentList(x => x.ParentId, opId);
        }
        list = list.Where(x => x.Status == WorkFlowOperatorStatusEnum.AddSign.ParseToInt()).ToList();
        return list;
    }

    /// <summary>
    /// 经办信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public WorkFlowOperatorEntity GetOperatorInfo(Expression<Func<WorkFlowOperatorEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowOperatorEntity>().First(expression);
    }

    /// <summary>
    /// 经办删除.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public int DeleteOperator(Expression<Func<WorkFlowOperatorEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Updateable<WorkFlowOperatorEntity>().SetColumns(x => x.Status == -1).Where(expression).ExecuteCommand();
    }

    /// <summary>
    /// 经办创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public bool CreateOperator(List<WorkFlowOperatorEntity> entitys)
    {
        return _repository.AsSugarClient().Storageable(entitys).WhereColumns(it => it.Id).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 经办创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool CreateOperator(WorkFlowOperatorEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowOperatorEntity>().Insert(entity);
    }

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool UpdateOperator(WorkFlowOperatorEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowOperatorEntity>().Update(entity);
    }

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public bool UpdateOperator(List<WorkFlowOperatorEntity> entitys)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowOperatorEntity>().UpdateRange(entitys);
    }

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <returns></returns>
    public int UpdateOperator(Expression<Func<WorkFlowOperatorEntity, bool>> filedNameExpression, Expression<Func<WorkFlowOperatorEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Updateable<WorkFlowOperatorEntity>().SetColumns(filedNameExpression).Where(expression).ExecuteCommand();
    }
    #endregion

    #region 流程记录

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public List<WorkFlowRecordEntity> GetRecordList(string taskId)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>().Where(x => x.TaskId == taskId).OrderBy(o => o.HandleTime).ToList();
    }

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    public List<WorkFlowRecordEntity> GetRecordList(Expression<Func<WorkFlowRecordEntity, bool>> expression, Expression<Func<WorkFlowRecordEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>().Where(expression).OrderByIF(orderByExpression.IsNotEmptyOrNull(), orderByExpression, orderByType).ToList();
    }

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public List<RecordModel> GetRecordModelList(string taskId)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRecordEntity, WorkFlowOperatorEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.OperatorId == b.Id)).Where(a => a.TaskId == taskId).OrderBy(a => a.HandleTime).Select((a, b) => new RecordModel
        {
            id = a.Id,
            nodeCode = a.NodeCode,
            nodeName = a.NodeName,
            nodeId = a.NodeId,
            handleType = a.HandleType,
            handleId = a.HandleId,
            handleOpinion = a.HandleOpinion,
            handleTime = a.HandleTime,
            taskId = a.TaskId,
            operatorId = a.OperatorId,
            signImg = a.SignImg,
            status = a.Status,
            headIcon = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.HandleId).Select(u => SqlFunc.MergeString("/api/File/Image/userAvatar/", SqlFunc.ToString(u.HeadIcon))),
            userName = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.HandleId).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
            handleUserName = a.HandleUserId,
            creatorTime = b.CreatorTime == null ? a.HandleTime : b.CreatorTime,
            fileList = a.FileList,
            expandField = a.ExpandField,
        }).ToList();
    }

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<UserItem> GetRecordItemList(Expression<Func<WorkFlowRecordEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>().Where(expression).OrderByDescending(a => a.HandleTime).Select(a => new UserItem
        {
            handleType = a.HandleType,
            userId = a.HandleId,
            headIcon = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.HandleId).Select(u => SqlFunc.MergeString("/api/File/Image/userAvatar/", SqlFunc.ToString(u.HeadIcon))),
            userName = SqlFunc.Subqueryable<UserEntity>().EnableTableFilter().Where(u => u.Id == a.HandleId).Select(u => u.RealName),
        }).ToList();
    }

    /// <summary>
    /// 经办记录信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public WorkFlowRecordEntity GetRecordInfo(string id)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>().First(x => x.Id == id);
    }

    /// <summary>
    /// 经办记录信息.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    public WorkFlowRecordEntity GetRecordInfo(Expression<Func<WorkFlowRecordEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRecordEntity>().First(expression);
    }

    /// <summary>
    /// 经办记录创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool CreateRecord(WorkFlowRecordEntity entity)
    {
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorUserId = _userManager.UserId;
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowRecordEntity>().Insert(entity);
    }

    /// <summary>
    /// 经办记录作废.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public void DeleteRecord(List<string> ids)
    {
        _repository.AsSugarClient().Updateable<WorkFlowRecordEntity>().SetColumns(it => it.Status == -1).Where(x => ids.Contains(x.Id)).ExecuteCommand();
    }

    /// <summary>
    /// 经办记录作废.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public void DeleteRecord(Expression<Func<WorkFlowRecordEntity, bool>> expression)
    {
        _repository.AsSugarClient().Updateable<WorkFlowRecordEntity>().SetColumns(it => it.Status == -1).Where(expression).ExecuteCommand();
    }
    #endregion

    #region 流程抄送

    /// <summary>
    /// 传阅详情.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public WorkFlowCirculateEntity GetCirculateInfo(Expression<Func<WorkFlowCirculateEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowCirculateEntity>().First(expression);
    }

    /// <summary>
    /// 传阅创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    public bool CreateCirculate(List<WorkFlowCirculateEntity> entitys)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowCirculateEntity>().InsertRange(entitys);
    }

    /// <summary>
    /// 传阅已读.
    /// </summary>
    /// <param name="id"></param>
    public void UpdateCirculate(string id)
    {
        _repository.AsSugarClient().Updateable<WorkFlowCirculateEntity>().SetColumns(it => it.Read == 1).Where(x => x.Id == id).ExecuteCommand();
    }
    #endregion

    #region 流程候选人/异常处理人

    /// <summary>
    /// 候选人创建.
    /// </summary>
    /// <param name="entitys"></param>
    public void CreateCandidates(List<WorkFlowCandidatesEntity> entitys)
    {
        _repository.AsSugarClient().GetSimpleClient<WorkFlowCandidatesEntity>().InsertRange(entitys);
    }

    /// <summary>
    /// 候选人删除.
    /// </summary>
    /// <param name="expression"></param>
    public void DeleteCandidates(Expression<Func<WorkFlowCandidatesEntity, bool>> expression)
    {
        _repository.AsSugarClient().Deleteable(expression).ExecuteCommand();
    }

    /// <summary>
    /// 候选人获取.
    /// </summary>
    /// <param name="nodeId"></param>
    public List<string> GetCandidates(string nodeCode, string taskId, int type = 1)
    {
        var flowCandidates = new List<string>();
        var candidateUserIdList = _repository.AsSugarClient().GetSimpleClient<WorkFlowCandidatesEntity>().GetList(x => x.NodeCode == nodeCode && x.TaskId == taskId && x.Type == type).Select(x => x.Candidates).ToList();
        foreach (var item in candidateUserIdList)
        {
            flowCandidates = flowCandidates.Union(item.Split(",").ToList()).Distinct().ToList();
        }
        return flowCandidates;
    }

    /// <summary>
    /// 候选人获取.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<WorkFlowCandidatesEntity> GetCandidates(Expression<Func<WorkFlowCandidatesEntity, bool>> expression)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowCandidatesEntity>().GetList(expression);
    }
    #endregion

    #region 流程内置参数
    /// <summary>
    /// 根据任务id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    public WorkFlowParamter GetWorkFlowParamterByTaskId(string taskId, WorkFlowHandleModel flowHandleModel)
    {
        var entity = GetTaskInfo(taskId);
        if (entity == null) return null;
        var wfParamter = GetWorkFlowParamterByFlowId(entity.FlowId, flowHandleModel);
        wfParamter.taskEntity = entity;
        return wfParamter;
    }

    /// <summary>
    /// 根据经办id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    public WorkFlowParamter GetWorkFlowParamterByOperatorId(string operatorId, WorkFlowHandleModel flowHandleModel)
    {
        var entity = GetOperatorInfo(x => x.Id == operatorId);
        if (entity == null) return null;
        var wfParamter = GetWorkFlowParamterByTaskId(entity.TaskId, flowHandleModel);
        wfParamter.operatorEntity = entity;
        wfParamter.operatorEntityList = GetOperatorList(x => x.Status != -1 && x.TaskId == entity.TaskId && x.NodeCode == entity.NodeCode);
        wfParamter.node = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == entity.NodeCode);
        wfParamter.nodePro = wfParamter.node.nodePro;
        return wfParamter;
    }

    /// <summary>
    /// 根据经办id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    public WorkFlowParamter GetWorkFlowParamterByFlowId(string flowId, WorkFlowHandleModel flowHandleModel)
    {
        var wfParamter = flowHandleModel == null ? new WorkFlowParamter() : flowHandleModel.ToObject<WorkFlowParamter>();
        wfParamter.flowInfo = GetFlowInfo(flowId);
        wfParamter.engineId = wfParamter.flowInfo.flowableId;
        var nodeEntityList = GetNodeList(x => x.FlowId == flowId && x.DeleteMark == null);
        var startNode = nodeEntityList.FirstOrDefault(x => x.FlowId == flowId && x.DeleteMark == null && "start".Equals(x.NodeType));
        wfParamter.startPro = startNode.NodeJson.ToObject<NodeProperties>();
        wfParamter.globalPro = nodeEntityList.FirstOrDefault(x => x.FlowId == flowId && x.DeleteMark == null && "global".Equals(x.NodeType)).NodeJson.ToObject<GlobalProperties>();
        if (GetSysConfigInfo().flowSign == 0)
        {
            wfParamter.globalPro.hasSignFor = false;
        }
        foreach (var item in nodeEntityList)
        {
            if (!"global".Equals(item.NodeType) || !"connect".Equals(item.NodeType))
            {
                var nodeModel = new WorkFlowNodeModel();
                nodeModel.nodeJson = item.NodeJson;
                nodeModel.nodeType = item.NodeType;
                nodeModel.formId = item.FormId;
                nodeModel.nodePro = item.NodeJson.ToObject<NodeProperties>();
                if ("approver".Equals(nodeModel.nodeType) || "processing".Equals(nodeModel.nodeType))
                {
                    nodeModel.nodePro.timeLimitConfig = nodeModel.nodePro.timeLimitConfig.on == 2 ? wfParamter.startPro.timeLimitConfig : nodeModel.nodePro.timeLimitConfig; // 限时配置
                    nodeModel.nodePro.noticeConfig = nodeModel.nodePro.noticeConfig.on == 2 ? wfParamter.startPro.noticeConfig : nodeModel.nodePro.noticeConfig; // 提醒配置
                    nodeModel.nodePro.overTimeConfig = nodeModel.nodePro.overTimeConfig.on == 2 ? wfParamter.startPro.overTimeConfig : nodeModel.nodePro.overTimeConfig; // 超时配置
                    nodeModel.nodePro.approveMsgConfig = nodeModel.nodePro.approveMsgConfig.on == 2 ? wfParamter.startPro.approveMsgConfig : nodeModel.nodePro.approveMsgConfig; // 同意
                    nodeModel.nodePro.rejectMsgConfig = nodeModel.nodePro.rejectMsgConfig.on == 2 ? wfParamter.startPro.rejectMsgConfig : nodeModel.nodePro.rejectMsgConfig; // 拒绝
                    nodeModel.nodePro.backMsgConfig = nodeModel.nodePro.backMsgConfig.on == 2 ? wfParamter.startPro.backMsgConfig : nodeModel.nodePro.backMsgConfig; // 退回
                    nodeModel.nodePro.copyMsgConfig = nodeModel.nodePro.copyMsgConfig.on == 2 ? wfParamter.startPro.copyMsgConfig : nodeModel.nodePro.copyMsgConfig; // 抄送
                    nodeModel.nodePro.overTimeMsgConfig = nodeModel.nodePro.overTimeMsgConfig.on == 2 ? wfParamter.startPro.overTimeMsgConfig : nodeModel.nodePro.overTimeMsgConfig; // 超时
                    nodeModel.nodePro.noticeMsgConfig = nodeModel.nodePro.noticeMsgConfig.on == 2 ? wfParamter.startPro.noticeMsgConfig : nodeModel.nodePro.noticeMsgConfig; // 提醒
                }
                nodeModel.nodeCode = item.NodeCode;
                nodeModel.nodeName = nodeModel.nodePro.nodeName;
                wfParamter.nodeList.Add(nodeModel);
            }
        }
        wfParamter.node = wfParamter.nodeList.FirstOrDefault(x => "start".Equals(x.nodeType));
        wfParamter.nodePro = wfParamter.startPro;
        wfParamter.flowInfo.flowNodes = nodeEntityList.Any() ? nodeEntityList.ToDictionary(x => x.NodeCode, y => y.NodeJson.ToObject<object>()) : new Dictionary<string, object>();
        return wfParamter;
    }
    #endregion

    #region 流程发起人

    /// <summary>
    /// 新增任务发起人信息.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="taskId">任务id.</param>
    public void CreateLaunchUser(string userId, string taskId)
    {
        var launchUserEntity = _repository.AsSugarClient().Queryable<UserEntity>().First(a => a.Id == userId && a.DeleteMark == null && a.EnabledMark == 1).Adapt<WorkFlowLaunchUserEntity>();
        launchUserEntity.Id = SnowflakeIdHelper.NextId();
        launchUserEntity.TaskId = taskId;
        launchUserEntity.ManagerId = _userManager.CurrentUserSupervisor.ToJsonStringOld();
        launchUserEntity.Subordinate = _userManager.GetSubordinatesAllPos(userId, true).ToJsonStringOld();
        launchUserEntity.Type = 1;
        launchUserEntity.CreatorUserId = userId;
        _repository.AsSugarClient().Insertable(launchUserEntity).ExecuteCommand();
    }

    /// <summary>
    /// 新增逐级发起人信息.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="taskId">任务id.</param>
    /// <param name="taskId">节点编码.</param>
    public void CreateStepLaunchUser(string userId, string taskId, string nodeCode)
    {
        var launchUserEntity = _repository.AsSugarClient().Queryable<UserEntity>().First(a => a.Id == userId && a.DeleteMark == null && a.EnabledMark == 1).Adapt<WorkFlowLaunchUserEntity>();
        launchUserEntity.Id = SnowflakeIdHelper.NextId();
        launchUserEntity.TaskId = taskId;
        var ids = _repository.AsSugarClient().Queryable<UserEntity>()
                .Where(u => u.EnabledMark == 1 && u.DeleteMark == null && u.ManagerId == userId)
                .Select(u => u.Id).ToList();
        launchUserEntity.Subordinate = string.Join(",", ids);
        launchUserEntity.Type = 2;
        launchUserEntity.NodeCode = nodeCode;
        var org = _repository.AsSugarClient().Queryable<OrganizeEntity>().First(a => a.Id == launchUserEntity.OrganizeId && a.DeleteMark == null && a.EnabledMark == 1);
        if (org.IsNotEmptyOrNull())
        {
            launchUserEntity.OrganizeId = org.OrganizeIdTree;
        }
        var pos = _repository.AsSugarClient().Queryable<PositionEntity>().First(a => a.Id == launchUserEntity.PositionId && a.DeleteMark == null && a.EnabledMark == 1);
        if (pos.IsNotEmptyOrNull())
        {
            launchUserEntity.PositionId = pos.PositionIdTree;
        }
        _repository.AsSugarClient().Insertable(launchUserEntity).ExecuteCommand();
    }

    /// <summary>
    /// 获取任务发起人信息.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    public WorkFlowLaunchUserEntity GetLaunchUserInfo(Expression<Func<WorkFlowLaunchUserEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowLaunchUserEntity>().First(expression);
    }

    /// <summary>
    /// 发起任务删除.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public void DeleteLaunchUser(Expression<Func<WorkFlowLaunchUserEntity, bool>> expression)
    {
        _repository.AsSugarClient().Deleteable<WorkFlowLaunchUserEntity>(expression).ExecuteCommand();
    }
    #endregion

    #region 驳回重启数据
    /// <summary>
    /// 驳回数据信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public WorkFlowRejectDataEntity GetRejectDataInfo(string id)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRejectDataEntity>().First(x => x.Id == id);
    }

    /// <summary>
    /// 驳回数据创建.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="nodeCode"></param>
    /// <returns></returns>
    public string CreateRejectData(string taskId, string nodeCode, string backNodeCode)
    {
        var entity = new WorkFlowRejectDataEntity();
        entity.Id = SnowflakeIdHelper.NextId();
        var taskEntity = GetTaskInfo(taskId);
        entity.TaskJson = taskEntity.ToJsonString();
        entity.NodeCode = backNodeCode;
        entity.EventLogJson = GetEventLogList(x => x.TaskId == taskId && nodeCode.Contains(x.NodeCode)).ToJsonString();
        //entity.OperatorJson = GetOperatorList(x => x.TaskId == taskId && x.Status != -1 && x.ParentId == "0" && nodeCode.Contains(x.NodeCode)).ToJsonString(); 后加签人冻结撤回至发起节点在提交导致没有审批在办
        entity.OperatorJson = GetOperatorList(x => x.TaskId == taskId && nodeCode.Contains(x.NodeCode)).ToJsonString();
        _repository.AsSugarClient().GetSimpleClient<WorkFlowRejectDataEntity>().Insert(entity);
        return entity.Id;
    }

    /// <summary>
    /// 驳回数据重启.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public string UpdateRejectData(WorkFlowRejectDataEntity entity)
    {
        var taskEntity = entity.TaskJson.ToObject<WorkFlowTaskEntity>();
        var taskOperatorEntityList = entity.OperatorJson.ToObject<List<WorkFlowOperatorEntity>>();
        var eventLogEntityList = entity.OperatorJson.ToObject<List<WorkFlowEventLogEntity>>();
        UpdateTask(taskEntity);
        _repository.AsSugarClient().GetSimpleClient<WorkFlowOperatorEntity>().UpdateRange(taskOperatorEntityList);
        _repository.AsSugarClient().GetSimpleClient<WorkFlowEventLogEntity>().InsertRange(eventLogEntityList);
        return taskEntity.CurrentNodeCode;
    }
    #endregion

    #region 节点流转记录

    /// <summary>
    /// 节点流转记录.
    /// </summary>
    /// <param name="entity"></param>
    public List<WorkFlowNodeRecordEntity> GetNodeRecord(string taskId)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowNodeRecordEntity>().Where(x => x.TaskId == taskId).ToList();
    }

    /// <summary>
    /// 节点流转记录.
    /// </summary>
    /// <param name="entity"></param>
    public WorkFlowNodeRecordEntity GetNodeRecord(string taskId, string nodeId)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowNodeRecordEntity>().First(x => x.TaskId == taskId && x.NodeId == nodeId);
    }

    /// <summary>
    /// 节点流转记录.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public WorkFlowNodeRecordEntity GetNodeRecord(Expression<Func<WorkFlowNodeRecordEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowNodeRecordEntity>().OrderByDescending(x => x.CreatorTime).First(expression);
    }

    /// <summary>
    /// 节点流转记录创建.
    /// </summary>
    /// <param name="entity"></param>
    public void CreateNodeRecord(WorkFlowNodeRecordEntity entity)
    {
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;
        _repository.AsSugarClient().GetSimpleClient<WorkFlowNodeRecordEntity>().Insert(entity);
    }

    /// <summary>
    /// 节点流转记录更新.
    /// </summary>
    /// <param name="entity"></param>
    public bool UpdateNodeRecord(WorkFlowNodeRecordEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowNodeRecordEntity>().Update(entity);
    }
    #endregion

    #region 流程撤销

    /// <summary>
    /// 撤销信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public WorkFlowRevokeEntity GetRevoke(Expression<Func<WorkFlowRevokeEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRevokeEntity>().First(expression);
    }

    /// <summary>
    /// 撤销创建.
    /// </summary>
    /// <param name="entity"></param>
    public void CreateRevoke(WorkFlowRevokeEntity entity)
    {
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;
        _repository.AsSugarClient().GetSimpleClient<WorkFlowRevokeEntity>().Insert(entity);
    }

    /// <summary>
    /// 撤销删除.
    /// </summary>
    /// <param name="entity"></param>
    public void DeleteRevoke(WorkFlowRevokeEntity entity)
    {
        _repository.AsSugarClient().Updateable<WorkFlowRevokeEntity>().SetColumns(it => it.DeleteMark == -1).Where(x => x.Id == entity.Id).ExecuteCommand();
    }

    /// <summary>
    /// 撤销存在.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public bool AnyRevoke(Expression<Func<WorkFlowRevokeEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowRevokeEntity>().Any(expression);
    }
    #endregion

    #region 任务审批条件历史记录

    /// <summary>
    /// 任务审批条件历史记录信息.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public Dictionary<string, bool> GetTaskLine(string taskId)
    {
        var result = new Dictionary<string, bool>();
        var entityList = _repository.AsSugarClient().Queryable<WorkFlowTaskLineEntity>().Where(x => x.TaskId == taskId && x.DeleteMark == null).ToList();
        if (entityList.Any())
        {
            result = entityList.ToDictionary(x => x.LineKey, y => y.LineValue.ParseToBool());
        }
        return result;
    }

    /// <summary>
    /// 最新执行条件线.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public List<string> GetTaskLineList(string taskId)
    {
        var keyList = _repository.AsSugarClient().Queryable<WorkFlowTaskLineEntity>().GroupBy(it => new { it.TaskId, it.LineKey, it.CreatorTime }).Where(a => a.TaskId == taskId && (a.LineValue == "True" || a.LineValue == "true") && a.DeleteMark == null)
            .Select(a => new WorkFlowTaskLineEntity { LineKey = a.LineKey, CreatorTime = SqlFunc.AggregateMax(a.CreatorTime) }).Select(x => x.LineKey).ToList();
        return keyList;
    }

    /// <summary>
    /// 任务审批条件历史记录保存.
    /// </summary>
    /// <param name="entity"></param>
    public void SaveTaskLine(string taskId, Dictionary<string, bool> variables)
    {
        var entityList = new List<WorkFlowTaskLineEntity>();
        foreach (var item in variables.Keys)
        {
            var entity = new WorkFlowTaskLineEntity();
            entity.Id = SnowflakeIdHelper.NextId();
            entity.TaskId = taskId;
            entity.LineKey = item;
            entity.LineValue = variables[item].ToString();
            entity.CreatorTime = DateTime.Now;
            entity.CreatorUserId = _userManager.UserId;
            entityList.Add(entity);
        }
        _repository.AsSugarClient().Storageable(entityList).WhereColumns(it => new { it.TaskId, it.LineKey }).ExecuteCommand();
    }
    #endregion

    #region 委托/代理
    /// <summary>
    /// 委托/代理接受人.
    /// </summary>
    /// <param name="userId">委托/代理人.</param>
    /// <param name="templateId">模板id.</param>
    /// <param name="type">委托/代理.</param>
    /// <returns></returns>
    public List<string> GetDelegateUserId(string userId, string templateId, int type)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowDelegateEntity, WorkFlowDelegateInfoEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.DelegateId))
            .Where((a, b) => a.UserId == userId && a.Type == type && (a.FlowId.Contains(templateId) || a.FlowName == "全部流程") && a.EndTime > DateTime.Now && a.StartTime < DateTime.Now && a.DeleteMark == null && b.Status == 1)
            .Select((a, b) => b.ToUserId).ToList();
    }

    /// <summary>
    /// 委托/代理.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<WorkFlowDelegateEntity> GetDelegateList(Expression<Func<WorkFlowDelegateEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowDelegateEntity>().Where(expression).ToList();
    }

    /// <summary>
    /// 委托/代理人.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<WorkFlowDelegateInfoEntity> GetDelegateInfoList(Expression<Func<WorkFlowDelegateInfoEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(expression).ToList();
    }

    /// <summary>
    /// 获取用户委托可发起流程(不包含公开流程).
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<List<string>> GetDelegateFlowId(string userId)
    {
        var flowIdList = new List<string>();
        var delegateIdList = _repository.AsSugarClient().Queryable<WorkFlowDelegateInfoEntity>().Where(x => x.ToUserId == userId && x.Status == 1).Select(x => x.DelegateId).ToList();
        var delegateList = _repository.AsSugarClient().Queryable<WorkFlowDelegateEntity>().Where(x => delegateIdList.Contains(x.Id) && x.Type == 0 && x.EndTime > DateTime.Now && x.StartTime < DateTime.Now && x.DeleteMark == null).ToList();
        foreach (var item in delegateList)
        {
            if (_userManager.GetAdminUserId() != item.UserId)
            {
                var flowIds = GetFlowIdList(item.UserId, null, false);
                if (item.FlowId.IsNullOrEmpty())
                {
                    flowIdList.AddRange(flowIds);
                }
                else
                {
                    flowIds = flowIds.Intersect(item.FlowId.Split(",")).ToList();
                    flowIdList.AddRange(flowIds);
                }
            }
            else
            {
                if (item.FlowId.IsNullOrEmpty())
                {
                    flowIdList.AddRange(GetFlowIdList(item.UserId, null, false));
                }
                else
                {
                    flowIdList.AddRange(item.FlowId.Split(","));
                }
            }
        }
        return flowIdList;
    }

    /// <summary>
    /// 获取委托/代理给我的数据.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<DelegeteListOutput> GetDelegateUserId(string userId, int type)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowDelegateEntity, WorkFlowDelegateInfoEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.DelegateId))
            .Where((a, b) => b.ToUserId == userId && a.Type == type && a.EndTime > DateTime.Now && a.StartTime < DateTime.Now && a.DeleteMark == null && b.Status == 1)
            .Select((a, b) => new DelegeteListOutput
            {
                flowId = a.FlowId,
                userId = a.UserId,
                startTime = a.StartTime,
                endTime = a.EndTime,
            }).ToList();
    }
    #endregion

    #region 触发任务
    /// <summary>
    /// 触发任务列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<WorkFlowTriggerTaskEntity> GetTriggerTaskList(Expression<Func<WorkFlowTriggerTaskEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowTriggerTaskEntity>().Where(expression).ToList();
    }

    /// <summary>
    /// 触发任务记录详情.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public WorkFlowTriggerRecordEntity GetTriggerRecordInfo(Expression<Func<WorkFlowTriggerRecordEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowTriggerRecordEntity>().Where(expression).OrderByDescending(x => x.EndTime).First();
    }

    /// <summary>
    /// 触发任务记录列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<WorkFlowTriggerRecordEntity> GetTriggerRecordList(Expression<Func<WorkFlowTriggerRecordEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowTriggerRecordEntity>().Where(expression).OrderBy(x => x.StartTime).ToList();
    }

    /// <summary>
    /// 触发任务更新.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public bool UpdateTriggerTask(WorkFlowTriggerTaskEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowTriggerTaskEntity>().Update(entity);
    }
    #endregion

    #region 子流程(依次)

    /// <summary>
    /// 子流程(依次)详情.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public WorkFlowSubTaskDataEntity GetSubTaskData(Expression<Func<WorkFlowSubTaskDataEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowSubTaskDataEntity>().Where(expression).OrderBy(x => x.SortCode).First();
    }

    /// <summary>
    /// 子流程(依次)新增.
    /// </summary>
    /// <param name="entitys"></param>
    public void CreateSubTaskData(WorkFlowSubTaskDataEntity entity)
    {
        _repository.AsSugarClient().GetSimpleClient<WorkFlowSubTaskDataEntity>().Insert(entity);
    }

    /// <summary>
    /// 子流程(依次)删除.
    /// </summary>
    /// <param name="id"></param>
    public void DeleteSubTaskData(string id)
    {
        _repository.AsSugarClient().Deleteable<WorkFlowSubTaskDataEntity>(x => x.Id == id).ExecuteCommand();
    }
    #endregion

    #region 外部事件

    /// <summary>
    /// 外部事件请求列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public List<WorkFlowEventLogEntity> GetEventLogList(Expression<Func<WorkFlowEventLogEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowEventLogEntity>().Where(expression).ToList();
    }

    /// <summary>
    /// 外部事件请求详情.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public WorkFlowEventLogEntity GetEventLog(Expression<Func<WorkFlowEventLogEntity, bool>> expression)
    {
        return _repository.AsSugarClient().Queryable<WorkFlowEventLogEntity>().Where(expression).First();
    }

    /// <summary>
    /// 外部事件记录创建.
    /// </summary>
    /// <param name="entity"></param>
    public void CreateEventLog(WorkFlowEventLogEntity entity)
    {
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;
        _repository.AsSugarClient().GetSimpleClient<WorkFlowEventLogEntity>().Insert(entity);
    }

    /// <summary>
    /// 外部事件记录更新.
    /// </summary>
    /// <param name="entity"></param>
    public bool UpdateEventLog(WorkFlowEventLogEntity entity)
    {
        return _repository.AsSugarClient().GetSimpleClient<WorkFlowEventLogEntity>().Update(entity);
    }

    /// <summary>
    /// 外部事件记录删除.
    /// </summary>
    /// <param name="expression"></param>
    public void DeleteEventLog(Expression<Func<WorkFlowEventLogEntity, bool>> expression)
    {
        _repository.AsSugarClient().Deleteable<WorkFlowEventLogEntity>(expression).ExecuteCommand();
    }
    #endregion
}

