using JNPF.Common.Configuration;
using JNPF.Common.Core.EventBus.Sources;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Job;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.Job;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.FriendlyException;
using JNPF.JsonSerialization;
using JNPF.Message.Interfaces;
using JNPF.RemoteRequest.Extensions;
using JNPF.Schedule;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.TaskQueue;
using JNPF.TaskScheduler.Entitys;
using JNPF.TaskScheduler.Entitys.Enum;
using JNPF.TimeCrontab;
using JNPF.UnifyResult;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Dto.Operator;
using JNPF.WorkFlow.Entitys.Dto.Task;
using JNPF.WorkFlow.Entitys.Dto.TriggerTask;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using JNPF.WorkFlow.Factory;
using JNPF.WorkFlow.Interfaces.Manager;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NPOI.Util;
using SqlSugar;
using StackExchange.Profiling.Internal;

namespace JNPF.WorkFlow.Manager;

public class WorkFlowManager : IWorkFlowManager, ITransient
{
    private readonly IWorkFlowRepository _repository;
    private readonly IUsersService _usersService;
    private readonly IRunService _runService;
    private readonly IUserManager _userManager;
    private readonly IJobManager _jobManager;
    private readonly ICacheManager _cacheManager;
    private readonly ITenantManager _tenantManager;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ITenant _db;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITaskQueue _taskQueue;
    private readonly IEventPublisher _eventPublisher;
    private WorkFlowUserUtil workFlowUserUtil;
    private WorkFlowNodeUtil workFlowNodeUtil;
    private WorkFlowMsgUtil workFlowMsgUtil;
    private WorkFlowOtherUtil workFlowOtherUtil;

    public WorkFlowManager(
        IWorkFlowRepository repository,
        IServiceScopeFactory serviceScopeFactory,
        IUsersService usersService,
        IOrganizeService organizeService,
        IDepartmentService departmentService,
        IUserRelationService userRelationService,
        IBillRullService billRuleService,
        IMessageManager messageManager,
        IDataInterfaceService dataInterfaceService,
        IRunService runService,
        IUserManager userManager,
        IJobManager jobManager,
        ISchedulerFactory schedulerFactory,
        ITaskQueue taskQueue,
        IDataBaseManager dataBaseManager,
        IEventPublisher eventPublisher,
        ICacheManager cacheManager,
        ITenantManager tenantManager,
        ISqlSugarClient context)
    {
        _repository = repository;
        _serviceScopeFactory = serviceScopeFactory;
        _usersService = usersService;
        _runService = runService;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _tenantManager = tenantManager;
        _jobManager = jobManager;
        _schedulerFactory = schedulerFactory;
        _taskQueue = taskQueue;
        _eventPublisher = eventPublisher;
        workFlowUserUtil = new WorkFlowUserUtil(repository, usersService, userRelationService, dataInterfaceService, userManager);
        workFlowNodeUtil = new WorkFlowNodeUtil(repository, dataBaseManager, userManager, usersService);
        workFlowMsgUtil = new WorkFlowMsgUtil(messageManager, repository, userManager, usersService, dataInterfaceService);
        workFlowOtherUtil = new WorkFlowOtherUtil(repository, usersService, runService, userManager, billRuleService);
        _db = context.AsTenant();
    }

    #region 发起

    /// <summary>
    /// 任务详情.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <param name="flowId">流程模版id.</param>
    /// <param name="opType">操作类型.</param>
    /// <param name="opId">操作id.</param>
    /// <returns></returns>
    public async Task<TaskInfoOutput> GetTaskInfo(string taskId, string flowId, string opType, string opId = null)
    {
        try
        {
            var output = new TaskInfoOutput();
            var wfParamter = await GetWorkFlowParamter(taskId, flowId, opType, opId, output);
            if (wfParamter.taskEntity.IsNotEmptyOrNull())
            {
                output.taskInfo = wfParamter.taskEntity.Adapt<TaskModel>();
                output.taskInfo.isRevokeTask = wfParamter.isRevoke;
                var creatorUser = _usersService.GetInfoByUserId(wfParamter.taskEntity.CreatorUserId);
                output.taskInfo.creatorUser = creatorUser?.RealName;
                output.taskInfo.headIcon = "/api/File/Image/userAvatar/" + creatorUser?.HeadIcon;
                output.taskInfo.flowCategory = _repository.GetDictionaryData(output.taskInfo.flowCategory).FullName;
                output.recordList = _repository.GetRecordModelList(taskId);
                output.recordList.ForEach(item => item.isOutSideNode = wfParamter.nodeList.Any(x => x.nodeType == WorkFlowNodeTypeEnum.outside.ToString() && x.nodeCode == item.nodeCode));
                output.lineKeyList = wfParamter.isRevoke ? _repository.GetTaskLineList(wfParamter.revokeEntity.TaskId) : _repository.GetTaskLineList(taskId);
                if (opType != "6")
                {
                    output.progressList = await GetTaskProgress(wfParamter, opType);
                }
                if (wfParamter.operatorEntity.IsNotEmptyOrNull())
                {
                    output.taskInfo.currentNodeName = wfParamter.nodePro.nodeName;
                    var dicProperties = wfParamter.flowInfo.flowNodes[wfParamter.nodePro.nodeId].ToObject<Dictionary<string, object>>();
                    if (wfParamter.nodePro.auxiliaryInfo.Any(x => x.config.on == 1))
                    {
                        dicProperties["auxiliaryInfo"] = wfParamter.nodePro.auxiliaryInfo;
                    }
                    else
                    {
                        dicProperties["auxiliaryInfo"] = new List<object>();
                    }
                    output.nodeProperties = dicProperties;
                    if (wfParamter.operatorEntity.DraftData.IsNotEmptyOrNull())
                    {
                        var draftData = wfParamter.operatorEntity.DraftData.ToObject<Dictionary<string, object>>();
                        draftData.Remove("f_version");
                        draftData.Remove("F_VERSION");
                        var formData = output.formData.ToObject<Dictionary<string, object>>();
                        DictionaryExtensions.ReplaceValue(formData, draftData);
                        output.formData = formData;
                    }
                }

                if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Cancel.ParseToInt() || wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.SendBack.ParseToInt())
                {
                    output.nodeList = wfParamter.nodeList.Adapt<List<NodeModel>>();
                    return output;
                }

                // 流程图节点显示完成情况以及审批人员
                var noPassNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().NoPassNode(wfParamter.taskEntity.InstanceId); // 未经过的节点
                foreach (var item in wfParamter.nodeList)
                {
                    var node = item.Adapt<NodeModel>();
                    if (item.nodeType == WorkFlowNodeTypeEnum.start.ToString())
                    {
                        node.type = "0";
                    }
                    else if (item.nodeType == WorkFlowNodeTypeEnum.approver.ToString() || item.nodeType == WorkFlowNodeTypeEnum.processing.ToString())
                    {
                        if (_repository.GetOperatorList(x => x.NodeCode == item.nodeCode && x.TaskId == wfParamter.taskEntity.Id && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt()).Any())
                        {
                            if (wfParamter.taskEntity.CurrentNodeCode.Contains(item.nodeCode) && wfParamter.taskEntity.Status != WorkFlowTaskStatusEnum.Reject.ParseToInt())
                            {
                                node.type = "1";
                            }
                            else
                            {
                                node.type = "0";
                            }
                        }
                        if (wfParamter.isRevoke && item.nodeType == WorkFlowNodeTypeEnum.processing.ToString() && !noPassNodeCodeList.Contains(node.nodeCode))
                        {
                            node.type = "0";
                        }
                    }
                    else if (item.nodeType == WorkFlowNodeTypeEnum.outside.ToString())
                    {
                        if (wfParamter.isRevoke)
                        {
                            var hisNodeList = (await BpmnEngineFactory.CreateBmpnEngine().GetHistory(wfParamter.taskEntity.InstanceId)).Select(x => x.code).ToList();
                            if (hisNodeList != null && hisNodeList.Any())
                            {
                                if (wfParamter.taskEntity.CurrentNodeCode.Contains(item.nodeCode))
                                {
                                    node.type = "1";
                                }
                                else if (hisNodeList.Contains(item.nodeCode))
                                {
                                    node.type = "0";
                                }
                            }
                        }
                        else
                        {
                            if (wfParamter.taskEntity.CurrentNodeCode.Contains(item.nodeCode) && wfParamter.taskEntity.Status != WorkFlowTaskStatusEnum.Reject.ParseToInt())
                            {
                                node.type = "1";
                            }
                            else if (_repository.GetEventLog(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == item.nodeCode && x.Status == 0).IsNotEmptyOrNull())
                            {
                                node.type = "0";
                            }
                        }
                    }
                    else if (item.nodeType == WorkFlowNodeTypeEnum.subFlow.ToString())
                    {
                        if (wfParamter.isRevoke)
                        {
                            var hisNodeList = (await BpmnEngineFactory.CreateBmpnEngine().GetHistory(wfParamter.taskEntity.InstanceId)).Select(x => x.code).ToList();
                            if (hisNodeList != null && hisNodeList.Any())
                            {
                                if (wfParamter.taskEntity.CurrentNodeCode.Contains(item.nodeCode))
                                {
                                    node.type = "1";
                                }
                                else if (hisNodeList.Contains(item.nodeCode))
                                {
                                    node.type = "0";
                                }
                            }
                        }
                        else
                        {
                            if (_repository.AnyTask(x => x.ParentId == wfParamter.taskEntity.Id && x.DeleteMark == null && x.SubCode == node.nodeCode))
                            {
                                if (!noPassNodeCodeList.Contains(node.nodeCode))
                                {
                                    if (wfParamter.taskEntity.CurrentNodeCode.Contains(item.nodeCode))
                                    {
                                        node.type = "1";
                                    }
                                    else
                                    {
                                        node.type = "0";
                                    }
                                }
                            }
                        }
                    }
                    else if (item.nodeType == WorkFlowNodeTypeEnum.end.ToString())
                    {
                        if (WorkFlowNodeTypeEnum.end.ToString().Equals(wfParamter.taskEntity.CurrentNodeCode))
                        {
                            node.type = "0";
                        }
                    }
                    else
                    {
                        var triggerRecord = _repository.GetTriggerRecordInfo(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == item.nodeCode);
                        if (triggerRecord.IsNotEmptyOrNull())
                        {
                            if (triggerRecord.Status == 0)
                            {
                                node.type = "0";
                            }
                            else if (triggerRecord.Status == 1)
                            {
                                node.type = "3";
                            }
                            else
                            {
                                node.type = "1";
                            }
                        }
                    }

                    if (!WorkFlowNodeTypeEnum.end.ToString().Equals(item.nodeType) || !WorkFlowNodeTypeEnum.outside.ToString().Equals(item.nodeType))
                    {
                        node.userName = await workFlowUserUtil.GetApproverUserName(item, wfParamter);
                    }
                    output.nodeList.Add(node);
                    if (opType == "3" || opType == "4")
                    {
                        if (node.nodeCode == wfParamter.nodePro.nodeId && node.type == "0" && wfParamter.nodePro.printConfig.on && wfParamter.nodePro.printConfig.printIds.Any() && wfParamter.nodePro.printConfig.conditionType == 2)
                        {
                            output.btnInfo.hasPrintBtn = true;
                        }
                    }
                }
            }
            return output;
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode);
        }
    }

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    public async Task<WorkFlowParamter> Save(FlowTaskSubmitModel flowTaskSubmitModel)
    {
        try
        {
            await workFlowUserUtil.GetLaunchAuthorize(flowTaskSubmitModel); // 验证发起人权限
            var taskEntity = new WorkFlowTaskEntity();
            var handleModel = flowTaskSubmitModel.ToObject<WorkFlowHandleModel>();
            var wfParamter = _repository.GetWorkFlowParamterByFlowId(flowTaskSubmitModel.flowId, handleModel);
            await workFlowOtherUtil.GetFlowTitle(flowTaskSubmitModel, wfParamter);
            // 表单数据处理
            var id = await workFlowOtherUtil.FlowDynamicDataManage(flowTaskSubmitModel);
            if (!_repository.AnyTask(x => x.Id == id && x.DeleteMark == null))
            {
                if (!(flowTaskSubmitModel.isFlow == 0 && flowTaskSubmitModel.status == 0 && flowTaskSubmitModel.parentId.Equals("0")))
                {
                    taskEntity.Id = id;
                    if (flowTaskSubmitModel.status == 0)
                    {
                        taskEntity.FullName = flowTaskSubmitModel.parentId.Equals("0") ? wfParamter.flowInfo.fullName : string.Format("{0}(子流程)", wfParamter.flowInfo.fullName);
                    }
                    else
                    {
                        taskEntity.FullName = flowTaskSubmitModel.parentId.Equals("0") ? flowTaskSubmitModel.flowTitle : string.Format("{0}(子流程)", flowTaskSubmitModel.flowTitle);
                    }
                    taskEntity.Urgent = flowTaskSubmitModel.flowUrgent;
                    taskEntity.FlowId = wfParamter.flowInfo.flowId;
                    taskEntity.FlowCode = wfParamter.flowInfo.enCode;
                    taskEntity.FlowName = wfParamter.flowInfo.fullName;
                    taskEntity.FlowType = wfParamter.flowInfo.type;
                    taskEntity.FlowCategory = wfParamter.flowInfo.category;
                    taskEntity.FlowVersion = wfParamter.flowInfo.version;
                    taskEntity.StartTime = flowTaskSubmitModel.status == 1 ? DateTime.Now : null;
                    taskEntity.CurrentNodeName = "开始";
                    taskEntity.CurrentNodeCode = "start";
                    taskEntity.Status = flowTaskSubmitModel.status;
                    taskEntity.CreatorTime = DateTime.Now;
                    taskEntity.CreatorUserId = flowTaskSubmitModel.crUser.IsEmpty() ? _userManager.UserId : flowTaskSubmitModel.crUser;
                    taskEntity.ParentId = flowTaskSubmitModel.parentId;
                    taskEntity.SubParameter = flowTaskSubmitModel.subParameter;
                    taskEntity.IsAsync = flowTaskSubmitModel.isAsync ? 1 : 0;
                    taskEntity.TemplateId = wfParamter.flowInfo.templateId;
                    taskEntity.DelegateUserId = flowTaskSubmitModel.isDelegate ? _userManager.UserId : null;
                    taskEntity.EngineId = wfParamter.engineId;
                    taskEntity.EngineType = 1;
                    taskEntity.SubCode = flowTaskSubmitModel.subCode;
                    taskEntity.IsFile = wfParamter.globalPro.fileConfig.on ? 0 : null;
                    taskEntity.Type = flowTaskSubmitModel.isFlow;
                    if (wfParamter.globalPro.globalParameterList != null && wfParamter.globalPro.globalParameterList.Any())
                    {
                        taskEntity.GlobalParameter = wfParamter.globalPro.globalParameterList.ToDictionary(x => x.fieldName, y => y.defaultValue?.ToString()).ToJsonString();
                    }
                    _repository.CreateLaunchUser(taskEntity.CreatorUserId, taskEntity.Id); // 保存发起人信息.
                    _repository.CreateTask(taskEntity);
                }
            }
            else
            {
                if (!(flowTaskSubmitModel.isFlow == 0 && flowTaskSubmitModel.status == 0 && flowTaskSubmitModel.parentId.Equals("0")))
                {
                    taskEntity = _repository.GetTaskInfo(flowTaskSubmitModel.id);
                    if (taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt())
                        throw Oops.Oh(ErrorCode.WF0046);
                    if (taskEntity.Status == WorkFlowTaskStatusEnum.Runing.ParseToInt() && flowTaskSubmitModel.approvaUpType == 0)
                        throw Oops.Oh(ErrorCode.WF0031);
                    taskEntity.Urgent = flowTaskSubmitModel.flowUrgent;
                    flowTaskSubmitModel.crUser = taskEntity.CreatorUserId;
                    await workFlowOtherUtil.GetFlowTitle(flowTaskSubmitModel, wfParamter);
                    if (flowTaskSubmitModel.status == 0)
                    {
                        taskEntity.FullName = taskEntity.ParentId.Equals("0") ? wfParamter.flowInfo.fullName : string.Format("{0}(子流程)", wfParamter.flowInfo.fullName);
                    }
                    else
                    {
                        taskEntity.FullName = taskEntity.ParentId.Equals("0") ? flowTaskSubmitModel.flowTitle : string.Format("{0}(子流程)", flowTaskSubmitModel.flowTitle);
                    }
                    if (flowTaskSubmitModel.status == 1)
                    {
                        taskEntity.Status = WorkFlowTaskStatusEnum.Runing.ParseToInt();
                        taskEntity.StartTime = DateTime.Now;
                    }
                    taskEntity.LastModifyTime = DateTime.Now;
                    taskEntity.LastModifyUserId = _userManager.UserId;
                    _repository.UpdateTask(taskEntity);
                }
            }
            wfParamter.taskEntity = taskEntity;
            return wfParamter;
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 提交.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    public async Task<dynamic> Submit(FlowTaskSubmitModel flowTaskSubmitModel)
    {
        try
        {
            var output = new OperatorOutput();
            if (!flowTaskSubmitModel.autoSubmit)
            {
                _db.BeginTran();
            }
            else
            {
                // 前置操作停止自动审批
                var flowHandModel = new WorkFlowHandleModel { flowId = flowTaskSubmitModel.flowId, formData = flowTaskSubmitModel.formData };
                var output1 = await GetCandidateModelList("0", flowHandModel);
                if (output1.type != 3) throw Oops.Oh(ErrorCode.WF0057);
            }

            var wfParamter = await this.Save(flowTaskSubmitModel);
            await workFlowNodeUtil.SaveGlobalParamter(wfParamter); // 保存流程参数
            output.taskId = wfParamter.taskEntity.Id;
            wfParamter.formData = await _runService.GetOrDelFlowFormData(wfParamter.node.formId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId, true);
            output.wfParamter = wfParamter;
            #region 开启flowable
            if (wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull())
            {
                var rejectDataEntity = _repository.GetRejectDataInfo(wfParamter.taskEntity.RejectDataId);
                var nodeCode = _repository.UpdateRejectData(rejectDataEntity);
                if (!flowTaskSubmitModel.autoSubmit) _db.CommitTran();
                await workFlowMsgUtil.RequestEvents(wfParamter.startPro.initFuncConfig, wfParamter, FuncConfigEnum.init);
                return output;
            }

            wfParamter.operatorEntity = wfParamter.node.Adapt<WorkFlowOperatorEntity>();
            wfParamter.operatorEntity.TaskId = wfParamter.taskEntity.Id;
            wfParamter.operatorEntity.Id = "0";
            wfParamter.operatorEntity.HandleId = wfParamter.taskEntity.CreatorUserId;
            #endregion

            #region 保存候选人/异常节点处理
            workFlowOtherUtil.SaveNodeCandidates(wfParamter);
            #endregion

            #region 流程经办
            await CreateNextNodeOperator(wfParamter, 2);
            if (wfParamter.errorNodeList.Any())
            {
                if (!flowTaskSubmitModel.autoSubmit)
                {
                    _db.RollbackTran();
                }
                else
                {
                    await _repository.DeleteTask(wfParamter.taskEntity);
                    if (wfParamter.taskEntity.ParentId == "0")
                    {
                        throw Oops.Oh(ErrorCode.WF0057);
                    }
                };
                output.errorCodeList = wfParamter.errorNodeList;
                await workFlowNodeUtil.Compensation(wfParamter.taskEntity.Id);
                return output;
            }
            #endregion

            #region 更新流程任务
            _repository.UpdateTask(wfParamter.taskEntity);
            var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
            await _runService.UpdateFlowState(fromIds, wfParamter.taskEntity.Id, wfParamter.taskEntity.Status);
            #endregion

            #region 更新当前抄送
            await workFlowUserUtil.GetflowTaskCirculateEntityList(wfParamter, 1);
            _repository.CreateCirculate(wfParamter.circulateEntityList);
            #endregion

            #region 流程经办记录
            await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Submit.ParseToInt());
            #endregion
            if (!flowTaskSubmitModel.autoSubmit) _db.CommitTran();

            #region 开始事件
            await workFlowMsgUtil.RequestEvents(wfParamter.startPro.initFuncConfig, wfParamter, FuncConfigEnum.init);
            #endregion

            #region 消息
            var bodyDic = new Dictionary<string, object>();
            #region 通知抄送人
            var userIdList = wfParamter.circulateEntityList.Select(x => x.UserId).ToList();
            if (userIdList.Any())
            {
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userIdList, null, 5);
                await workFlowMsgUtil.Alerts(wfParamter.nodePro.copyMsgConfig, userIdList, wfParamter, "MBXTLC007", bodyDic);
            }
            #endregion

            #region 通知发起人
            if (wfParamter.taskEntity.EndTime.IsNotEmptyOrNull()) // 结束消息
            {
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, new List<string>() { wfParamter.taskEntity.CreatorUserId }, null, 0);
                await workFlowMsgUtil.Alerts(wfParamter.startPro.endMsgConfig, new List<string>() { wfParamter.taskEntity.CreatorUserId }, wfParamter, "MBXTLC010", bodyDic);
            }
            if (flowTaskSubmitModel.isDelegate) //委托审批消息
            {
                await workFlowMsgUtil.SendDelegateMsg("发起", wfParamter.taskEntity.CreatorUserId, wfParamter.taskEntity.FullName);
            }
            #endregion

            #region 通知审批人
            var messageDic = workFlowOtherUtil.GroupByOperator(wfParamter.nextOperatorEntityList);
            foreach (var item in messageDic.Keys)
            {
                var userList = messageDic[item].Select(x => x.HandleId).ToList();
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, messageDic[item], 2);
                await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC001", bodyDic);
                // 超时提醒
                await TimeoutOrRemind(wfParamter, item, messageDic[item]);
            }
            #endregion
            #endregion

            #region 自动审批
            await AutoAudit(wfParamter);
            #endregion
            if (_repository.AnyTask(x => x.Id == wfParamter.taskEntity.Id && x.EndTime != null)) // 归档
            {
                #region 结束事件
                await workFlowMsgUtil.RequestEvents(wfParamter.startPro.endFuncConfig, wfParamter, FuncConfigEnum.end);
                #endregion
                output.isEnd = wfParamter.globalPro.fileConfig.on;
            }
            return output;
        }
        catch (AppFriendlyException ex)
        {
            if (!flowTaskSubmitModel.autoSubmit) _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 撤回(发起).
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task RecallLaunch(WorkFlowParamter wfParamter)
    {
        try
        {
            _db.BeginTran();
            if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt()) throw Oops.Oh(ErrorCode.WF0046);
            if (wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.WF0023);
            if (wfParamter.flowInfo.status == 3) throw Oops.Oh(ErrorCode.WF0070);
            var subTaskList = _repository.GetTaskList(x => wfParamter.taskEntity.Id == x.ParentId && x.DeleteMark == null);
            if (subTaskList.Any())
            {
                if (subTaskList.Any(x => x.IsAsync == 1)) throw Oops.Oh(ErrorCode.WF0023);
                if (subTaskList.Any(x => x.Status != WorkFlowTaskStatusEnum.Draft.ParseToInt())) throw Oops.Oh(ErrorCode.WF0023);
            }
            if (_repository.AnyTask(x => wfParamter.taskEntity.Id == x.ParentId && x.DeleteMark == null && x.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt())) throw Oops.Oh(ErrorCode.WF0046);
            if (wfParamter.taskEntity.Status != WorkFlowTaskStatusEnum.Runing.ParseToInt())
                throw Oops.Oh(ErrorCode.WF0010);
            wfParamter.operatorEntityList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
            var opIds = wfParamter.operatorEntityList.Select(x => x.Id).ToList();
            if (wfParamter.operatorEntityList.Any(x => x.DraftData.IsNotEmptyOrNull()) || _repository.GetRecordList(x => x.TaskId == wfParamter.taskEntity.Id && opIds.Contains(x.OperatorId) && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt()).Any())
                throw Oops.Oh(ErrorCode.WF0010);
            #region 撤回数据
            if (wfParamter.taskEntity.InstanceId.IsNotEmptyOrNull())
            {
                await BpmnEngineFactory.CreateBmpnEngine().InstanceDelete(wfParamter.taskEntity.InstanceId);
            }
            _repository.DeleteFlowTaskAllData(wfParamter.taskEntity.Id, false);
            var nodeRecord = _repository.GetNodeRecord(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == wfParamter.startPro.nodeId);
            nodeRecord.NodeStatus = 6;
            _repository.UpdateNodeRecord(nodeRecord);
            foreach (var item in wfParamter.nodeList)
            {
                _schedulerFactory.RemoveJob(string.Format("Job_CS_{0}_{1}", item.nodeCode, wfParamter.taskEntity.Id));
                _schedulerFactory.RemoveJob(string.Format("Job_TX_{0}_{1}", item.nodeCode, wfParamter.taskEntity.Id));
            }
            #endregion

            #region 更新实例
            wfParamter.taskEntity.CurrentNodeCode = wfParamter.startPro.nodeId;
            wfParamter.taskEntity.CurrentNodeName = "开始";
            wfParamter.taskEntity.Urgent = 0;
            wfParamter.taskEntity.Status = WorkFlowTaskStatusEnum.Recall.ParseToInt();
            _repository.UpdateTask(wfParamter.taskEntity);
            var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
            await _runService.UpdateFlowState(fromIds, wfParamter.taskEntity.Id, wfParamter.taskEntity.Status);
            #endregion

            #region 撤回记录
            await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Recall.ParseToInt());
            #endregion
            _db.CommitTran();

            #region 撤回删除子流程任务
            var childTaskList = _repository.GetChildTaskList(wfParamter.taskEntity.Id, true);
            await DeleteTask(childTaskList);
            #endregion
            wfParamter.formData = await _runService.GetOrDelFlowFormData(wfParamter.node.formId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId, true);
            await workFlowMsgUtil.RequestEvents(wfParamter.startPro.flowRecallFuncConfig, wfParamter, FuncConfigEnum.flowRecall);
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 催办.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Press(WorkFlowParamter wfParamter)
    {
        try
        {
            _db.BeginTran();
            var operatorEntityList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && x.Completion == 0 && x.Duedate != null);
            await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Press.ParseToInt());
            _db.CommitTran();

            var bodyDic = new Dictionary<string, object>();
            var messageDic = workFlowOtherUtil.GroupByOperator(operatorEntityList);
            foreach (var item in messageDic.Keys)
            {
                var userList = messageDic[item].Select(x => x.HandleId).ToList();
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, messageDic[item], 2);
                await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC004", bodyDic);
            }
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 撤销.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="isRevokeAudit">是否撤销审批.</param>
    /// <returns></returns>
    public async Task Revoke(WorkFlowParamter wfParamter)
    {
        try
        {
            _db.BeginTran();
            var wfParamterRevoke = await workFlowOtherUtil.RevokeSave(wfParamter);
            await workFlowOtherUtil.CreateRecord(wfParamterRevoke, WorkFlowRecordTypeEnum.Submit.ParseToInt());
            await CreateRevokeOperator(wfParamterRevoke, wfParamter.taskEntity.Id, 0);
            _repository.CreateTask(wfParamterRevoke.taskEntity);
            var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
            if (wfParamterRevoke.taskEntity.EndTime.IsNotEmptyOrNull())
            {
                var taskList = _repository.GetChildTaskList(wfParamter.taskEntity.Id);
                foreach (var item in taskList)
                {
                    item.Status = WorkFlowTaskStatusEnum.Revoke.ParseToInt();
                    var opList = _repository.GetOperatorList(x => x.TaskId == item.Id && x.Completion == 0);
                    if (opList.Any())
                    {
                        opList.ForEach(item =>
                        {
                            item.Completion = 1;
                        });
                        _repository.UpdateOperator(opList);
                    }
                    _repository.UpdateTask(item);
                    await _runService.UpdateFlowState(fromIds, item.Id, item.Status);
                }
            }
            _db.CommitTran();
            var bodyDic = new Dictionary<string, object>();
            var messageDic = workFlowOtherUtil.GroupByOperator(wfParamterRevoke.nextOperatorEntityList);
            foreach (var item in messageDic.Keys)
            {
                var userList = messageDic[item].Select(x => x.HandleId).ToList();
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamterRevoke, userList, messageDic[item], 2);
                await workFlowMsgUtil.Alerts(wfParamterRevoke.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamterRevoke, "MBXTLC001", bodyDic);
                // 超时提醒
                await TimeoutOrRemind(wfParamterRevoke, item, messageDic[item]);
            }
            await AutoAudit(wfParamterRevoke, false, true);

        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 撤销审批.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task<OperatorOutput> RevokeAudit(WorkFlowParamter wfParamter)
    {
        var output = new OperatorOutput();
        try
        {
            _db.BeginTran();
            var revokeEntity = _repository.GetRevoke(x => x.RevokeTaskId == wfParamter.taskEntity.Id && x.DeleteMark == null);
            if (revokeEntity == null) throw Oops.Oh(ErrorCode.COM1005);
            var revokeTaskEntity = _repository.GetTaskInfo(revokeEntity.TaskId);
            if (revokeTaskEntity == null) throw Oops.Oh(ErrorCode.COM1005);
            if (wfParamter.handleStatus == 0)
            {
                wfParamter.taskEntity.Status = WorkFlowTaskStatusEnum.Reject.ParseToInt();
                wfParamter.taskEntity.EndTime = DateTime.Now;
                var updateOperatorList = _repository.GetOperatorList(x => x.Completion == 0 && x.TaskId == wfParamter.operatorEntity.TaskId);
                var nodeRecord = new WorkFlowNodeRecordEntity
                {
                    TaskId = wfParamter.taskEntity.Id,
                    NodeId = wfParamter.operatorEntity.NodeId,
                    NodeCode = wfParamter.operatorEntity.NodeCode,
                    NodeName = wfParamter.operatorEntity.NodeName,
                    NodeStatus = 3
                };
                _repository.CreateNodeRecord(nodeRecord);
                updateOperatorList.ForEach(item =>
                {
                    item.Completion = 1;
                    if (wfParamter.operatorEntity.Id == item.Id)
                    {
                        wfParamter.operatorEntity.HandleStatus = wfParamter.handleStatus;
                        wfParamter.operatorEntity.HandleTime = DateTime.Now;
                        if (wfParamter.operatorEntity.SignTime.IsNullOrEmpty())
                        {
                            wfParamter.operatorEntity.SignTime = DateTime.Now;
                        }
                        if (wfParamter.operatorEntity.StartHandleTime.IsNullOrEmpty())
                        {
                            wfParamter.operatorEntity.StartHandleTime = DateTime.Now;
                        }
                    }
                });
                _repository.UpdateOperator(updateOperatorList);
                await workFlowOtherUtil.CreateRecord(wfParamter, wfParamter.handleStatus);
                _repository.DeleteRevoke(revokeEntity);
                await BpmnEngineFactory.CreateBmpnEngine().InstanceDelete(wfParamter.taskEntity.InstanceId);
                _repository.UpdateTask(wfParamter.taskEntity);
            }
            else
            {
                wfParamter.operatorEntity.HandleStatus = wfParamter.handleStatus;
                wfParamter.operatorEntity.HandleTime = DateTime.Now;
                if (wfParamter.operatorEntity.SignTime.IsNullOrEmpty())
                {
                    wfParamter.operatorEntity.SignTime = DateTime.Now;
                }
                if (wfParamter.operatorEntity.StartHandleTime.IsNullOrEmpty())
                {
                    wfParamter.operatorEntity.StartHandleTime = DateTime.Now;
                }
                wfParamter.operatorEntity.Completion = 1;
                _repository.UpdateOperator(wfParamter.operatorEntity);
                await workFlowOtherUtil.CreateRecord(wfParamter, wfParamter.handleStatus);
                if (!wfParamter.operatorEntityList.Any(x => x.HandleStatus == null && x.HandleTime == null && x.Id != wfParamter.operatorEntity.Id))
                {
                    await CreateRevokeOperator(wfParamter, revokeTaskEntity.Id, 1);
                    var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
                    if (wfParamter.taskEntity.EndTime.IsNotEmptyOrNull())
                    {
                        var taskList = _repository.GetChildTaskList(revokeTaskEntity.Id);
                        foreach (var item in taskList)
                        {
                            item.Status = WorkFlowTaskStatusEnum.Revoke.ParseToInt();
                            var opList = _repository.GetOperatorList(x => x.TaskId == item.Id && x.Completion == 0);
                            if (opList.Any())
                            {
                                opList.ForEach(item =>
                                {
                                    item.Completion = 1;
                                });
                                _repository.UpdateOperator(opList);
                            }
                            _repository.UpdateTask(item);
                            await _runService.UpdateFlowState(fromIds, item.Id, item.Status);
                        }
                    }
                    _repository.UpdateTask(wfParamter.taskEntity);
                }
            }
            _db.CommitTran();
            await AutoAudit(wfParamter, false, true);
            return output;
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            await workFlowNodeUtil.Compensation(wfParamter.taskEntity.Id);
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 外部节点回调审批.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task<OperatorOutput> WebHookAudit(WorkFlowEventLogEntity envntLog, object formData)
    {
        var handleModel = new WorkFlowHandleModel { formData = formData };
        var wfParamter = _repository.GetWorkFlowParamterByTaskId(envntLog.TaskId, handleModel);
        wfParamter.node = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == envntLog.NodeCode);
        var outSideOperator = new WorkFlowOperatorEntity();
        outSideOperator.Id = "0";
        outSideOperator.NodeCode = envntLog.NodeCode;
        outSideOperator.NodeName = envntLog.NodeName;
        outSideOperator.NodeId = envntLog.NodeId;
        wfParamter.operatorEntity = outSideOperator;
        wfParamter.operatorEntityList = new List<WorkFlowOperatorEntity> { outSideOperator };
        wfParamter.handleStatus = 1;
        if (_repository.AnyRevoke(x => x.RevokeTaskId == wfParamter.taskEntity.Id && x.DeleteMark == null))
        {
            return await RevokeAudit(wfParamter);
        }
        else
        {
            return await Audit(wfParamter);
        }
    }

    /// <summary>
    /// 外部节点回调.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task Hooks(TaskHooksInput input, string tenantId)
    {
        tenantId ??= "default";
        using var scope = _serviceScopeFactory.CreateScope();
        var sqlSugarClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        if (KeyVariable.MultiTenancy)
        {
            await _tenantManager.ChangTenant(sqlSugarClient, tenantId);
        }
        sqlSugarClient = sqlSugarClient.CopyNew();
        var envntLog = await sqlSugarClient.Queryable<WorkFlowEventLogEntity>().FirstAsync(x => x.Id == input.eventId && x.DeleteMark == null);
        if (envntLog.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.WF0082);
        var taskEntity = await sqlSugarClient.Queryable<WorkFlowTaskEntity>().FirstAsync(x => x.Id == envntLog.TaskId && x.DeleteMark == null);
        if (taskEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.WF0082);
        if (taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt()) throw Oops.Oh(ErrorCode.WF0046);
        if (taskEntity.Status == WorkFlowTaskStatusEnum.Cancel.ParseToInt()) throw Oops.Oh(ErrorCode.WF0036);
        var userEntity = await sqlSugarClient.Queryable<UserEntity>().FirstAsync(x => x.Id == envntLog.CreatorUserId && x.DeleteMark == null);
        var server = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        var localAddress = addresses.FirstOrDefault().Replace("[::]", "localhost");
        var token = NetHelper.GetToken(userEntity.Id, userEntity.Account, userEntity.RealName, userEntity.IsAdministrator, tenantId);
        var heraderDic = new Dictionary<string, object>();
        heraderDic.Add("Authorization", token);
        var scheduleTaskModel = new ScheduleTaskModel();
        scheduleTaskModel.taskParams.Add("envntLog", envntLog);
        scheduleTaskModel.taskParams.Add("formData", input.formData);
        var path = string.Format("{0}/ScheduleTask/outside", localAddress);
        var result = await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetHeaders(heraderDic).SetBody(scheduleTaskModel).PostAsStringAsync();
        if (result.IsNotEmptyOrNull())
        {
            var jobj = result.ToObject<RESTfulResult<JObject>>();
            if (jobj.code != 200)
            {
                throw Oops.Oh(ErrorCode.WF0083, jobj.msg);
            }
        }
        else
        {
            throw Oops.Oh(ErrorCode.WF0083, "接口异常");
        }
    }
    #endregion

    #region 审批

    /// <summary>
    /// 获取候选人.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">操作参数.</param>
    /// <param name="type">0:候选节点编码，1：候选人.</param>
    /// <returns></returns>
    public async Task<dynamic> GetCandidateModelList(string operatorId, WorkFlowHandleModel handleModel, int type = 0)
    {
        // 所有节点
        var wfParamter = _repository.GetWorkFlowParamterByFlowId(handleModel.flowId, handleModel);
        if (handleModel.delegateUser.IsNotEmptyOrNull())
        {
            var flowIds = _repository.GetFlowIdList(handleModel.delegateUser);
            if (!flowIds.Contains(wfParamter.flowInfo.templateId)) Oops.Oh(ErrorCode.WF0063);
        }
        if (type == 0)
        {
            // 候选人节点
            var output = new List<CandidateModel>();
            // 下个节点集合
            List<WorkFlowNodeModel> nextNodeList = new List<WorkFlowNodeModel>();
            // 当前任务Id
            var taskId = string.Empty;
            // 是否达到会签比例
            var isCom = true;
            if (_repository.AnyTask(x => x.Id == handleModel.id && !SqlFunc.IsNullOrEmpty(x.RejectDataId))) return new { list = output, countersignOver = isCom, type = 3 };
            if (_repository.AnyRevoke(x => x.RevokeTaskId == handleModel.id && x.DeleteMark == null)) return new { list = output, countersignOver = isCom, type = 3 };
            if (operatorId != "0")
            {
                wfParamter = _repository.GetWorkFlowParamterByOperatorId(operatorId, handleModel);
                if (wfParamter.operatorEntity.ParentId != "0" || wfParamter.operatorEntity.Status == WorkFlowOperatorStatusEnum.AddSign.ParseToInt() || wfParamter.operatorEntity.Status == WorkFlowOperatorStatusEnum.Revoke.ParseToInt()) return new { list = output, countersignOver = isCom, type = 3 }; // 加签人审批不弹窗
                if (wfParamter.nodePro.assigneeType == WorkFlowOperatorTypeEnum.StepApprover.ParseToInt() && wfParamter.operatorEntity.Status != WorkFlowOperatorStatusEnum.Assigned.ParseToInt())
                {
                    var stepLaunchUser = _repository.GetLaunchUserInfo(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == wfParamter.nodePro.nodeId && x.Type == 2);
                    if (stepLaunchUser.IsNotEmptyOrNull() && !(wfParamter.handleStatus == 0 && !wfParamter.globalPro.hasContinueAfterReject))
                    {
                        // 判断逐级发起人的层级是否大于配置层级
                        var posIds = stepLaunchUser.OrganizeId.Split(",").ToList();
                        posIds = posIds.SkipLast(1).ToList();
                        posIds.AddRange(stepLaunchUser.PositionId.Split(","));
                        var endLevel = wfParamter.nodePro.approversConfig.end != 3 ? wfParamter.nodePro.approversConfig.level : wfParamter.nodePro.approversConfig.originLevel;
                        var operatorCount = wfParamter.operatorEntityList.FindAll(x => x.ParentId == "0").Count;
                        var currentLevel = posIds.Count - operatorCount;
                        if (wfParamter.nodePro.approversConfig.end == 3)
                        {
                            if (posIds.Count > endLevel)
                            {
                                isCom = currentLevel <= endLevel;
                            }
                            else
                            {
                                isCom = true;
                            }
                        }
                        else
                        {
                            if (posIds.Count > endLevel)
                            {
                                isCom = operatorCount == endLevel;
                            }
                            else
                            {
                                isCom = operatorCount == posIds.Count;
                            }
                        }
                    }
                    else
                    {
                        isCom = true;
                    }
                    if (isCom && !wfParamter.globalPro.hasContinueAfterReject && wfParamter.handleStatus == 0)
                    {
                        return new { list = output, countersignOver = isCom, type = 3 };
                    }
                }
                else if (wfParamter.nodePro.counterSign != 0 && wfParamter.operatorEntity.Status != WorkFlowOperatorStatusEnum.Assigned.ParseToInt())
                {
                    if (wfParamter.nodePro.counterSign == 2)
                    {
                        var handleAll = wfParamter.operatorEntity.HandleAll.Split(",").ToList<string>();
                        var operatorAllList = new List<WorkFlowOperatorEntity>();
                        foreach (var item in handleAll)
                        {
                            if (item == wfParamter.operatorEntity.HandleId)
                            {
                                var operatorNewEntity = wfParamter.operatorEntity.Copy();
                                operatorNewEntity.Id = SnowflakeIdHelper.NextId();
                                operatorNewEntity.HandleId = item;
                                operatorAllList.Add(operatorNewEntity);
                            }
                        }
                    }
                    isCom = workFlowOtherUtil.IsSatisfyProportion(wfParamter.operatorEntityList, wfParamter.nodePro, 1, true);
                    if (isCom && !wfParamter.globalPro.hasContinueAfterReject && wfParamter.nodePro.handleStatus == 0)
                    {
                        return new { list = output, countersignOver = isCom, type = 3 };
                    }
                }
                await workFlowNodeUtil.SaveGlobalParamter(wfParamter, false);
            }
            await workFlowNodeUtil.GetNextNodeList(wfParamter, nextNodeList, true);
            await workFlowUserUtil.GetCandidates(output, nextNodeList);
            if (output.Any())
            {
                foreach (var item in output)
                {
                    item.isBranchFlow = wfParamter.nodePro.divideRule == "choose";
                    if (!item.isCandidates) continue;
                    if (operatorId != "0" && wfParamter.nodePro.counterSign != 0)
                    {
                        var candidatesList = _repository.GetCandidates(item.nodeCode, wfParamter.taskEntity.Id);
                        var candidatesNameList = new List<string>();
                        await workFlowUserUtil.GetUserNameDefined(wfParamter.nodePro, wfParamter.taskEntity.Id, candidatesNameList, candidatesList);
                        item.selected = string.Join(",", candidatesNameList);
                    }
                }
            }
            // 弹窗类型 1:条件分支弹窗(包含候选人) 2:候选人弹窗 3:无弹窗
            var branchType = output.Any(x => x.isBranchFlow) ? 1 : output.Any(x => x.isCandidates) ? 2 : 3;
            // 无弹窗：1.条件分支且未达到会签比例
            if (!isCom && branchType == 1)
            {
                branchType = 3;
                output.Clear();
            }
            return new { list = output, type = branchType, countersignOver = isCom };
        }
        else
        {
            var nextNode = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode.Equals(handleModel.nodeCode));
            if (operatorId != "0")
            {
                wfParamter = _repository.GetWorkFlowParamterByOperatorId(operatorId, handleModel);
                if (wfParamter.taskEntity.IsNotEmptyOrNull() && wfParamter.taskEntity.DelegateUserId.IsNotEmptyOrNull())
                {
                    handleModel.delegateUser = wfParamter.taskEntity.DelegateUserId;
                }
            }
            var userList = await workFlowUserUtil.GetFlowUserId(wfParamter, nextNode, true); // 候选人节点人员
            return workFlowUserUtil.GetCandidateItems(userList, handleModel);
        }
    }

    /// <summary>
    /// 审批.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="isAuto">是否自动审批.</param>
    /// <returns></returns>
    public async Task<OperatorOutput> Audit(WorkFlowParamter wfParamter, bool isAuto = false)
    {
        if (_cacheManager.Exists(string.Format("lock_{0}_{1}", wfParamter.taskEntity.Id, wfParamter.node.nodeCode)))
        {
            Oops.Oh(ErrorCode.WF0077);
        }
        else
        {
            _cacheManager.Set(string.Format("lock_{0}_{1}", wfParamter.taskEntity.Id, wfParamter.node.nodeCode), true, TimeSpan.FromSeconds(5));
        }
        var output = new OperatorOutput();
        output.taskId = wfParamter.taskEntity.Id;
        wfParamter.isAuto = isAuto;
        var candidates = wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull() ? new List<WorkFlowCandidatesEntity>() : workFlowOtherUtil.SaveNodeCandidates(wfParamter);
        try
        {
            _db.BeginTran();
            if (!isAuto)
            {
                // 审批修改表单数据.
                if (wfParamter.operatorEntity.Id.IsNotEmptyOrNull() && (WorkFlowNodeTypeEnum.approver.ToString().Equals(wfParamter.node.nodeType) || WorkFlowNodeTypeEnum.processing.ToString().Equals(wfParamter.node.nodeType)))
                {
                    var fEntity = _repository.GetFromEntity(wfParamter.node.formId);
                    var systemControlList = wfParamter.nodePro.formOperates.ToObject<List<FormOperatesModel>>().Where(x => !x.write && x.read).Select(x => x.id).ToList();
                    await _runService.SaveFlowFormData(fEntity, wfParamter.formData.ToJsonString(), wfParamter.taskEntity.Id, wfParamter.taskEntity.FlowId, true, systemControlList);
                    wfParamter.formData = await _runService.GetOrDelFlowFormData(wfParamter.node.formId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId, true);
                }
            }
            await workFlowNodeUtil.SaveGlobalParamter(wfParamter); // 保存流程参数
            var isNodeCompletion = false;
            if (wfParamter.operatorEntity.Id.IsNotEmptyOrNull())
            {
                if (wfParamter.nodeList.Any(x => x.nodeCode == wfParamter.operatorEntity.NodeCode && (x.nodeType == WorkFlowNodeTypeEnum.subFlow.ToString() || x.nodeType == WorkFlowNodeTypeEnum.outside.ToString())))
                {
                    var currentNode = wfParamter.nodeList.Find(x => x.nodeCode == wfParamter.operatorEntity.NodeCode);
                    if (currentNode.nodeType == WorkFlowNodeTypeEnum.outside.ToString())
                    {
                        await workFlowOtherUtil.CreateRecord(wfParamter, wfParamter.handleStatus);
                    }
                    isNodeCompletion = true;
                }
                else
                {
                    #region 更新当前经办数据
                    isNodeCompletion = await workFlowOtherUtil.CompleteOperator(wfParamter, wfParamter.handleStatus);
                    #endregion

                    #region 更新当前抄送
                    await workFlowUserUtil.GetflowTaskCirculateEntityList(wfParamter, wfParamter.handleStatus);
                    _repository.CreateCirculate(wfParamter.circulateEntityList);
                    #endregion
                }
            }

            #region 更新下一节点经办
            // 驳回审批
            if (wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull() && isNodeCompletion)
            {
                var flag = false;
                // 冻结驳回解冻必须是非前加签
                if (WorkFlowOperatorStatusEnum.AddSign.ParseToInt() == wfParamter.operatorEntity.Status)
                {
                    var addSignParameter = wfParamter.operatorEntity.HandleParameter.ToObject<AddSignItem>();
                    if (addSignParameter.addSignType == 2) flag = true;
                }
                else
                {
                    flag = true;
                }

                if (flag)
                {
                    var rejectDataEntity = _repository.GetRejectDataInfo(wfParamter.taskEntity.RejectDataId);
                    if (wfParamter.operatorEntity.NodeCode != rejectDataEntity.NodeCode)
                    {
                        Oops.Oh(ErrorCode.WF0036);
                    }
                    var nodeCode = _repository.UpdateRejectData(rejectDataEntity);
                    await BpmnEngineFactory.CreateBmpnEngine().JumpNode(wfParamter.taskEntity.InstanceId, wfParamter.taskEntity.CurrentNodeCode, nodeCode);
                    var nodeList = await BpmnEngineFactory.CreateBmpnEngine().GetCurrentNodeList(wfParamter.taskEntity.InstanceId);
                    foreach (var item in nodeList)
                    {
                        _repository.UpdateOperator(x => x.NodeId == item.taskId, y => y.NodeCode == item.taskKey && y.TaskId == wfParamter.taskEntity.Id);
                    }
                    var nodeRecord = new WorkFlowNodeRecordEntity
                    {
                        TaskId = wfParamter.taskEntity.Id,
                        NodeId = wfParamter.operatorEntity.NodeId,
                        NodeCode = wfParamter.operatorEntity.NodeCode,
                        NodeName = wfParamter.operatorEntity.NodeName,
                        NodeStatus = wfParamter.handleStatus == 1 ? 2 : 3
                    };
                    _repository.CreateNodeRecord(nodeRecord);
                    _db.CommitTran();
                    if (wfParamter.handleStatus == 0)
                    {
                        await workFlowMsgUtil.RequestEvents(wfParamter.nodePro.rejectFuncConfig, wfParamter, FuncConfigEnum.reject);
                    }
                    else
                    {
                        await workFlowMsgUtil.RequestEvents(wfParamter.nodePro.approveFuncConfig, wfParamter, FuncConfigEnum.approve);
                    }
                    _cacheManager.Del(string.Format("lock_{0}_{1}", wfParamter.taskEntity.Id, wfParamter.node.nodeCode));
                    return output;
                }
            }
            if (isNodeCompletion)
            {
                await CreateNextNodeOperator(wfParamter, 1);
                if (wfParamter.errorNodeList.Count > 0)
                {
                    _db.RollbackTran();
                    output.errorCodeList = wfParamter.errorNodeList;
                    await workFlowNodeUtil.Compensation(wfParamter.taskEntity.Id);
                    _cacheManager.Del(string.Format("lock_{0}_{1}", wfParamter.taskEntity.Id, wfParamter.node.nodeCode));
                    return output;
                }
                foreach (var item in wfParamter.nextOperatorEntityList)
                {
                    var nextNodeCodeList = (await BpmnEngineFactory.CreateBmpnEngine().GetNextNode(wfParamter.engineId, item.NodeCode, string.Empty)).Select(x => x.id).ToList();
                    if (nextNodeCodeList.Any())
                    {
                        var nextNode = wfParamter.nodeList.FirstOrDefault(m => m.nodeCode.Equals(item.NodeCode));
                        if (nextNode.nodePro.assigneeType == WorkFlowOperatorTypeEnum.CandidateApprover.ParseToInt() && isAuto)
                        {
                            _db.RollbackTran();
                            await workFlowNodeUtil.Compensation(wfParamter.taskEntity.Id);
                            _cacheManager.Del(string.Format("lock_{0}_{1}", wfParamter.taskEntity.Id, wfParamter.node.nodeCode));
                            return output;
                        }
                    }
                }
            }
            #endregion

            #region 更新任务
            if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Reject.ParseToInt())
            {
                if (wfParamter.taskEntity.ParentId != "0") // 子流程结束回到主流程下一节点
                {
                    await InsertSubTaskNextNode(wfParamter.taskEntity);
                }
                var nextNodeCodeList = (await BpmnEngineFactory.CreateBmpnEngine().GetNextNode(wfParamter.engineId, wfParamter.operatorEntity.NodeCode, string.Empty)).Select(x => x.id).ToList();
                if (!(nextNodeCodeList.Any() && wfParamter.nodeList.Any(x => WorkFlowNodeTypeEnum.trigger.ToString().Equals(x.nodeType) && nextNodeCodeList.Contains(x.nodeCode))))
                {
                    await BpmnEngineFactory.CreateBmpnEngine().InstanceDelete(wfParamter.taskEntity.InstanceId);
                }
            }
            _repository.UpdateTask(wfParamter.taskEntity);
            var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
            await _runService.UpdateFlowState(fromIds, wfParamter.taskEntity.Id, wfParamter.taskEntity.Status);
            #endregion
            _db.CommitTran();

            #region 消息
            var bodyDic = new Dictionary<string, object>();

            #region 通知抄送人
            var userIdList = wfParamter.circulateEntityList.Select(x => x.UserId).ToList();
            if (userIdList.Any())
            {
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userIdList, null, 5);
                await workFlowMsgUtil.Alerts(wfParamter.nodePro.copyMsgConfig, userIdList, wfParamter, "MBXTLC007", bodyDic);
            }
            #endregion

            #region 通知发起人
            if (isNodeCompletion || wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Reject.ParseToInt())
            {
                if (wfParamter.handleStatus == 0)
                {
                    await workFlowMsgUtil.RequestEvents(wfParamter.nodePro.rejectFuncConfig, wfParamter, FuncConfigEnum.reject);
                }
                else
                {
                    await workFlowMsgUtil.RequestEvents(wfParamter.nodePro.approveFuncConfig, wfParamter, FuncConfigEnum.approve);
                }
                userIdList = new List<string> { wfParamter.taskEntity.CreatorUserId };
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userIdList, null, 0);
                var msgConfig = wfParamter.handleStatus == 0 ? wfParamter.nodePro.rejectMsgConfig : wfParamter.nodePro.approveMsgConfig;
                var msgCode = wfParamter.handleStatus == 0 ? "MBXTLC018" : "MBXTLC002";
                await workFlowMsgUtil.Alerts(msgConfig, userIdList, wfParamter, msgCode, bodyDic);
            }

            if (wfParamter.taskEntity.EndTime.IsNotEmptyOrNull())
            {
                await workFlowMsgUtil.RequestEvents(wfParamter.startPro.endFuncConfig, wfParamter, FuncConfigEnum.end);
                await workFlowMsgUtil.Alerts(wfParamter.startPro.endMsgConfig, new List<string>() { wfParamter.taskEntity.CreatorUserId }, wfParamter, "MBXTLC010", bodyDic);
            }
            #endregion

            #region 通知审批人
            if (isNodeCompletion || wfParamter.nextOperatorEntityList.Any())
            {
                // 关闭当前节点超时提醒任务
                _schedulerFactory.RemoveJob(string.Format("Job_CS_{0}_{1}", wfParamter.node.nodeCode, wfParamter.taskEntity.Id));
                _schedulerFactory.RemoveJob(string.Format("Job_TX_{0}_{1}", wfParamter.node.nodeCode, wfParamter.taskEntity.Id));

                var messageDic = workFlowOtherUtil.GroupByOperator(wfParamter.nextOperatorEntityList);
                foreach (var item in messageDic.Keys)
                {
                    var userList = messageDic[item].Select(x => x.HandleId).ToList();
                    bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, messageDic[item], 2);
                    await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC001", bodyDic);
                    // 超时提醒
                    await TimeoutOrRemind(wfParamter, item, messageDic[item]);
                }

                //委托审批消息
                if (wfParamter.operatorEntity.HandleId.IsNotEmptyOrNull() && !_userManager.UserId.Equals(wfParamter.operatorEntity.HandleId) && !isAuto)
                {
                    await workFlowMsgUtil.SendDelegateMsg("审批", wfParamter.operatorEntity.HandleId, wfParamter.taskEntity.FullName);
                }
            }
            #endregion
            #endregion

            if (wfParamter.triggerNodeList.Any() || isNodeCompletion || wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Reject.ParseToInt())
            {
                var model = new TaskFlowEventModel();
                model.TenantId = _userManager.TenantId;
                model.UserId = _userManager.UserId;
                model.ParamterJson = wfParamter.ToJsonString();
                model.ActionType = wfParamter.handleStatus == 1 ? 1 : 2;
                if (wfParamter.node.nodeType == WorkFlowNodeTypeEnum.processing.ToString())
                {
                    model.ActionType = 4;
                }
                model.taskFlowData = new List<Dictionary<string, object>>();
                model.taskFlowData.Add(wfParamter.formData.ToObject<Dictionary<string, object>>());
                await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTaskFlow", model));
            }
            #region 自动审批
            if (isNodeCompletion)
            {
                await AutoAudit(wfParamter);
            }
            #endregion
            if (_repository.AnyTask(x => x.Id == wfParamter.taskEntity.Id && x.EndTime != null) && wfParamter.globalPro.fileConfig.on) // 归档
            {
                output.isEnd = true;
            }
            _repository.SetCommonWordsCount(wfParamter.handleOpinion);
            _repository.SetDefaultSignImg(wfParamter.signId, wfParamter.signImg, wfParamter.useSignNext);
            _cacheManager.Del(string.Format("lock_{0}_{1}", wfParamter.taskEntity.Id, wfParamter.node.nodeCode));
            return output;
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            _cacheManager.Del(string.Format("lock_{0}_{1}", wfParamter.taskEntity.Id, wfParamter.node.nodeCode));
            var ids = candidates.Select(x => x.Id).ToArray();
            _repository.DeleteCandidates(x => ids.Contains(x.Id));
            await workFlowNodeUtil.Compensation(wfParamter.taskEntity.Id);
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 退回.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task<dynamic> SendBack(WorkFlowParamter wfParamter, bool isTrigger = false)
    {
        try
        {
            _db.BeginTran();
            if (wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull() && !isTrigger) throw Oops.Oh(ErrorCode.WF0045);
            //表单数据
            wfParamter.formData = await _runService.GetOrDelFlowFormData(wfParamter.node.formId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId, true);

            #region 更新驳回经办
            var rejectsubFlowNodeList = await workFlowNodeUtil.RejectManager(wfParamter);
            await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.SendBack.ParseToInt());
            if (wfParamter.nodePro.backType == 1)//重新审批
            {
                // 删除退回节点与当前节点中的子流程节点任务数据
                var childTaskList = _repository.GetChildTaskList(wfParamter.taskEntity.Id);
                childTaskList = childTaskList.FindAll(x => rejectsubFlowNodeList.Select(item => item.nodeCode).Contains(x.SubCode));
                await DeleteTask(childTaskList);
            }
            if (!(wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.SendBack.ParseToInt() && wfParamter.nodePro.backType == 2))
            {
                var nextNodeCodeList = (await BpmnEngineFactory.CreateBmpnEngine().GetNextNode(wfParamter.engineId, wfParamter.operatorEntity.NodeCode, string.Empty)).Select(x => x.id).ToList();
                if (!isTrigger && wfParamter.nodeList.Any(x => nextNodeCodeList.Contains(x.nodeCode) && x.nodeType == "trigger" && x.nodeJson.ToObject<TriggerProperties>().actionList.Contains(3)) && wfParamter.nodePro.backType != 2)
                {
                    _db.RollbackTran();
                    var model = new TaskFlowEventModel();
                    model.TenantId = _userManager.TenantId;
                    model.UserId = _userManager.UserId;
                    model.ParamterJson = wfParamter.ToJsonString();
                    model.ActionType = 3;
                    model.taskFlowData = new List<Dictionary<string, object>>();
                    model.taskFlowData.Add(wfParamter.formData.ToObject<Dictionary<string, object>>());
                    await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTaskFlow", model));
                    return new List<CandidateModel>();
                }
                else
                {
                    await CreateNextNodeOperator(wfParamter, 0);
                }
            }

            var nodeRecord = new WorkFlowNodeRecordEntity
            {
                TaskId = wfParamter.taskEntity.Id,
                NodeId = wfParamter.operatorEntity.NodeId,
                NodeCode = wfParamter.operatorEntity.NodeCode,
                NodeName = wfParamter.operatorEntity.NodeName,
                NodeStatus = 5
            };
            _repository.CreateNodeRecord(nodeRecord);

            if (wfParamter.errorNodeList.Count > 0)
            {
                _db.RollbackTran();
                await workFlowNodeUtil.Compensation(wfParamter.taskEntity.Id);
                return wfParamter.errorNodeList;
            }

            #endregion

            #region 更新流程任务
            if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.SendBack.ParseToInt())
            {
                _repository.UpdateTask(wfParamter.taskEntity);
                _repository.DeleteFlowTaskAllData(wfParamter.taskEntity.Id, wfParamter.nodePro.backType == 1, wfParamter.nodePro.backType == 1);
                var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
                await _runService.UpdateFlowState(fromIds, wfParamter.taskEntity.Id, wfParamter.taskEntity.Status);
            }
            else
            {
                _repository.UpdateTask(wfParamter.taskEntity);
                wfParamter.nextOperatorEntityList.ForEach(x => x.Status = WorkFlowOperatorStatusEnum.SendBack.ParseToInt());
                _repository.CreateOperator(wfParamter.nextOperatorEntityList);
            }
            #endregion
            _db.CommitTran();

            await workFlowMsgUtil.RequestEvents(wfParamter.nodePro.backFuncConfig, wfParamter, FuncConfigEnum.back);

            #region 消息
            var bodyDic = new Dictionary<string, object>();

            #region 通知抄送人
            var userIdList = wfParamter.circulateEntityList.Select(x => x.UserId).ToList();
            if (userIdList.Any())
            {
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userIdList, null, 5);
                await workFlowMsgUtil.Alerts(wfParamter.nodePro.copyMsgConfig, userIdList, wfParamter, "MBXTLC007", bodyDic);
            }
            #endregion

            #region 通知发起人
            if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.SendBack.ParseToInt())
            {
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, new List<string>() { wfParamter.taskEntity.CreatorUserId }, null, 0);
                await workFlowMsgUtil.Alerts(wfParamter.nodePro.backMsgConfig, new List<string>() { wfParamter.taskEntity.CreatorUserId }, wfParamter, "MBXTLC003", bodyDic);
            }
            #endregion

            #region 通知审批人
            // 关闭当前节点超时提醒任务
            _schedulerFactory.RemoveJob(string.Format("Job_CS_{0}_{1}", wfParamter.node.nodeCode, wfParamter.taskEntity.Id));
            _schedulerFactory.RemoveJob(string.Format("Job_TX_{0}_{1}", wfParamter.node.nodeCode, wfParamter.taskEntity.Id));
            if (wfParamter.nextOperatorEntityList.Any())
            {
                //await workFlowMsgUtil.RequestEvents(wfParamter.nodePro.approveFuncConfig, wfParamter, FuncConfigEnum.approve);

                var messageDic = workFlowOtherUtil.GroupByOperator(wfParamter.nextOperatorEntityList);
                foreach (var item in messageDic.Keys)
                {
                    var userList = messageDic[item].Select(x => x.HandleId).ToList();
                    bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, messageDic[item], 2);
                    await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC001", bodyDic);
                    await workFlowMsgUtil.Alerts(wfParamter.nodePro.backMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC003", bodyDic);
                    // 超时提醒
                    await TimeoutOrRemind(wfParamter, item, messageDic[item]);
                }
            }

            // 委托审批消息
            if (wfParamter.operatorEntity.HandleId.IsNotEmptyOrNull() && !_userManager.UserId.Equals(wfParamter.operatorEntity.HandleId))
            {
                await workFlowMsgUtil.SendDelegateMsg("审批", wfParamter.operatorEntity.HandleId, wfParamter.taskEntity.FullName);
            }
            #endregion
            #endregion

            #region 默认审批
            if (wfParamter.nextOperatorEntityList.Count == 1 && wfParamter.nextOperatorEntityList.FirstOrDefault().HandleId == "jnpf")
            {
                var defaultAuditOperator = wfParamter.nextOperatorEntityList.FirstOrDefault();
                var handleModel = new WorkFlowHandleModel();
                handleModel.handleOpinion = "默认审批通过";
                handleModel.candidateList = wfParamter.candidateList;
                var formId = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == defaultAuditOperator.NodeCode).formId;
                handleModel.formData = await _runService.GetOrDelFlowFormData(formId, defaultAuditOperator.TaskId, 0, wfParamter.flowInfo.flowId, true);
                wfParamter = _repository.GetWorkFlowParamterByOperatorId(defaultAuditOperator.Id, handleModel);
                await this.Audit(wfParamter, true);
            }
            #endregion
            return new List<CandidateModel>();
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            await workFlowNodeUtil.Compensation(wfParamter.taskEntity.Id);
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 撤回(审批).
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="flowTaskOperatorRecordEntity">经办记录.</param>
    public async Task RecallAudit(WorkFlowParamter wfParamter, WorkFlowRecordEntity flowTaskOperatorRecordEntity)
    {
        try
        {
            _db.BeginTran();
            var wfParamterNew = wfParamter.Copy();
            var delOperatorIds = new List<string>();
            var upOperatorList = new List<WorkFlowOperatorEntity>();
            var delRecordIds = new List<string>();
            wfParamter.operatorEntity.HandleStatus = null;
            wfParamter.operatorEntity.HandleTime = null;
            wfParamter.operatorEntity.Completion = 0;
            wfParamter.operatorEntity.CreatorTime = DateTime.Now;
            wfParamter.operatorEntity.Status = WorkFlowOperatorStatusEnum.Recall.ParseToInt();
            upOperatorList.Add(wfParamter.operatorEntity);
            // 加签不能撤回.
            if (wfParamter.operatorEntity.ParentId != "0" && wfParamter.operatorEntity.HandleParameter.IsNotEmptyOrNull())
            {
                throw Oops.Oh(ErrorCode.WF0010);
            }
            else
            {
                // 初始人存在加签则要看加签是否完成 完成可以撤回
                if (wfParamterNew.operatorEntity.Completion == 0 || wfParamterNew.operatorEntity.HandleStatus.IsNullOrEmpty() || wfParamterNew.operatorEntity.HandleTime.IsNullOrEmpty() || wfParamterNew.operatorEntityList.Any(x => x.ParentId == wfParamter.operatorEntity.Id))
                {
                    throw Oops.Oh(ErrorCode.WF0010);
                }
                else
                {
                    if (wfParamter.operatorEntity.HandleAll.IsNotEmptyOrNull()) // 是否依次审批或逐级
                    {
                        if (wfParamter.nodePro.assigneeType == (int)WorkFlowOperatorTypeEnum.StepApprover)
                        {
                            var nextOperatorEntity = _repository.GetOperatorInfo(x => x.Id == wfParamter.operatorEntity.HandleAll && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                            if (nextOperatorEntity.IsNotEmptyOrNull())
                            {
                                if (nextOperatorEntity.DraftData.IsNotEmptyOrNull() || _repository.GetRecordList(x => x.TaskId == nextOperatorEntity.TaskId && nextOperatorEntity.Id == x.OperatorId && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt()).Any())
                                    throw Oops.Oh(ErrorCode.WF0010);
                                _repository.DeleteOperator(x => x.Id == nextOperatorEntity.Id);
                            }
                        }
                        else
                        {
                            var handIds = wfParamter.operatorEntity.HandleAll.Split(",").ToList<string>();
                            var index = handIds.IndexOf(wfParamter.operatorEntity.HandleId);
                            var nextHandleId = handIds.GetIndex(index + 1);
                            var nextOperatorEntity = _repository.GetOperatorInfo(x => x.TaskId == wfParamter.operatorEntity.TaskId && x.NodeCode == wfParamter.operatorEntity.NodeCode && x.HandleId == nextHandleId && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                            if (nextOperatorEntity.IsNotEmptyOrNull())
                            {
                                if (nextOperatorEntity.DraftData.IsNotEmptyOrNull() || _repository.GetRecordList(x => x.TaskId == nextOperatorEntity.TaskId && nextOperatorEntity.Id == x.OperatorId && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt()).Any())
                                    throw Oops.Oh(ErrorCode.WF0010);
                                _repository.DeleteOperator(x => x.Id == nextOperatorEntity.Id);
                            }
                        }
                    }
                }
            }
            if (!wfParamter.taskEntity.CurrentNodeCode.Contains(wfParamter.operatorEntity.NodeCode)) // 撤回节点已完成
            {
                var recallNextNode = (await BpmnEngineFactory.CreateBmpnEngine().GetNextNode(wfParamter.engineId, wfParamter.operatorEntity.NodeCode, string.Empty)).Select(x => x.id).ToList();
                var subTaskList = _repository.GetTaskList(x => x.ParentId == wfParamter.taskEntity.Id && x.DeleteMark == null && recallNextNode.Contains(x.SubCode));
                if (subTaskList.Any())
                {
                    if (subTaskList.Any(x => x.IsAsync == 1)) throw Oops.Oh(ErrorCode.WF0023);
                    if (subTaskList.Any(x => x.Status != WorkFlowTaskStatusEnum.Draft.ParseToInt())) throw Oops.Oh(ErrorCode.WF0023);
                    await DeleteTask(subTaskList);
                }
                var eventLogList = _repository.GetEventLogList(x => x.TaskId == wfParamter.taskEntity.Id && x.Type == WorkFlowNodeTypeEnum.outside.ToString() && recallNextNode.Contains(x.NodeCode));
                if (eventLogList.Any(x => x.Status == 0))
                    throw Oops.Oh(ErrorCode.WF0023);
                var recallNextOperatorList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && recallNextNode.Contains(x.NodeCode) && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                delOperatorIds = recallNextOperatorList.Select(x => x.Id).ToList();
                if (recallNextOperatorList.Any(x => x.DraftData.IsNotEmptyOrNull()) || _repository.GetRecordList(x => x.TaskId == wfParamter.operatorEntity.TaskId && delOperatorIds.Contains(x.OperatorId) && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt()).Any())
                    throw Oops.Oh(ErrorCode.WF0010);
                // 将触发节点下的所有节点放入撤销节点中
                var triggerNodeList = new List<string>();
                foreach (var item in recallNextNode)
                {
                    var triggerNode = wfParamter.nodeList.Find(x => x.nodeCode == item);
                    if (triggerNode.IsNotEmptyOrNull() && triggerNode.nodeType == WorkFlowNodeTypeEnum.trigger.ToString())
                    {
                        triggerNodeList.AddRange(wfParamter.nodeList.FindAll(x => x.nodePro.groupId == triggerNode.nodePro.groupId).Select(x => x.nodeCode));
                    }
                }
                recallNextNode = recallNextNode.Union(triggerNodeList).ToList();
                await BpmnEngineFactory.CreateBmpnEngine().JumpNode(wfParamter.taskEntity.InstanceId, string.Join(",", recallNextNode), wfParamter.operatorEntity.NodeCode);
                var nodeRecord = _repository.GetNodeRecord(wfParamter.taskEntity.Id, wfParamter.operatorEntity.NodeId);
                nodeRecord.CreatorTime = DateTime.Now;
                nodeRecord.NodeStatus = 6;
                _repository.UpdateNodeRecord(nodeRecord);
                delOperatorIds = recallNextOperatorList.Select(x => x.Id).ToList();
                _repository.DeleteOperator(x => delOperatorIds.Contains(x.Id)); // 撤回节点下一节点经办删除
                var notHanleOperatorList = _repository.GetOperatorList(x => x.ParentId == "0" && x.TaskId == flowTaskOperatorRecordEntity.TaskId && x.NodeCode == flowTaskOperatorRecordEntity.NodeCode && (x.HandleStatus == null || x.HandleTime == null));
                upOperatorList = upOperatorList.Union(notHanleOperatorList).ToList();
                var nodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetCurrentNodeList(wfParamter.taskEntity.InstanceId);
                var nodeId = nodeCodeList.FirstOrDefault(x => x.taskKey == wfParamter.operatorEntity.NodeCode).taskId;
                foreach (var item in upOperatorList)
                {
                    item.Completion = 0;
                    item.NodeId = nodeId;
                    item.CreatorTime = DateTime.Now;
                }
                _repository.UpdateOperator(x => x.NodeId == nodeId, y => y.TaskId == flowTaskOperatorRecordEntity.TaskId && y.NodeCode == flowTaskOperatorRecordEntity.NodeCode && y.ParentId == "0" && y.HandleStatus != null && y.HandleTime != null && y.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                wfParamter.taskEntity.CurrentNodeCode = string.Join(",", nodeCodeList.Select(x => x.taskKey));
                wfParamter.taskEntity.CurrentNodeName = string.Join(",", wfParamter.nodeList.FindAll(x => wfParamter.taskEntity.CurrentNodeCode.Contains(x.nodeCode)).Select(x => x.nodePro.nodeName));
                _repository.UpdateTask(wfParamter.taskEntity);
                var userList = upOperatorList.Select(x => x.HandleId).ToList();
                var idList = upOperatorList.Select(x => x.Id).ToList();
                foreach (var item in recallNextNode)
                {
                    var recallNode = wfParamter.nodeList.Find(x => x.nodeCode == item);
                    _repository.DeleteCandidates(x => x.TaskId == wfParamter.taskEntity.Id && item == x.NodeCode && x.Type == 2); //异常处理人
                    _repository.DeleteCandidates(x => x.TaskId == wfParamter.taskEntity.Id && item == x.NodeCode && userList.Contains(x.HandleId) && idList.Contains(x.OperatorId) && x.Type == 1);//候选人
                    _repository.DeleteLaunchUser(x => x.TaskId == wfParamter.taskEntity.Id && item == x.NodeCode && x.Type == 2);
                    _schedulerFactory.RemoveJob(string.Format("Job_CS_{0}_{1}", item, wfParamter.taskEntity.Id));
                    _schedulerFactory.RemoveJob(string.Format("Job_TX_{0}_{1}", item, wfParamter.taskEntity.Id));
                    if (upOperatorList.Any(x => x.NodeCode == item)) // 超时提醒
                    {
                        await TimeoutOrRemind(wfParamter, item, upOperatorList.FindAll(x => x.NodeCode == item));
                    }
                }
            }
            else
            {
                _repository.DeleteCandidates(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == wfParamter.operatorEntity.NodeCode && x.Type == 2); //异常处理人
                _repository.DeleteCandidates(x => x.TaskId == wfParamter.taskEntity.Id && x.HandleId == wfParamter.operatorEntity.HandleId && x.OperatorId == wfParamter.operatorEntity.Id && x.Type == 1);//候选人
            }

            foreach (var item in upOperatorList)
            {
                var operatorRecord = _repository.GetRecordList(x => x.TaskId == item.TaskId && x.NodeCode == item.NodeCode && x.OperatorId == item.Id && x.Status != -1);
                if (operatorRecord.IsNotEmptyOrNull())
                {
                    delRecordIds = delRecordIds.Union(operatorRecord.Select(x => x.Id).ToList()).ToList();
                }
            }
            _repository.UpdateOperator(upOperatorList);
            delRecordIds.Add(flowTaskOperatorRecordEntity.Id);
            _repository.DeleteRecord(delRecordIds);
            await workFlowOtherUtil.CreateRecord(wfParamterNew, WorkFlowRecordTypeEnum.Recall.ParseToInt());
            _db.CommitTran();
            wfParamter.formData = await _runService.GetOrDelFlowFormData(wfParamter.node.formId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId, true);
            await workFlowMsgUtil.RequestEvents(wfParamter.nodePro.recallFuncConfig, wfParamter, FuncConfigEnum.recall);
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            await workFlowNodeUtil.Compensation(wfParamter.taskEntity.Id);
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 转办.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Transfer(WorkFlowParamter wfParamter)
    {
        try
        {
            _db.BeginTran();
            if (wfParamter.operatorEntity.HandleId != _userManager.UserId && wfParamter.handleIds.Contains(wfParamter.operatorEntity.HandleId))
            {
                if (wfParamter.operatorEntity.IsProcessing == 1)
                {
                    throw Oops.Oh(ErrorCode.WF0075);
                }
                else
                {
                    throw Oops.Oh(ErrorCode.WF0044);
                }
            }
            if (wfParamter.handleIds.Contains(_userManager.UserId))
            {
                if (wfParamter.operatorEntity.IsProcessing == 1)
                {
                    throw Oops.Oh(ErrorCode.WF0074);
                }
                else
                {
                    throw Oops.Oh(ErrorCode.WF0013);
                }
            }
            if (wfParamter.operatorEntity == null)
                throw Oops.Oh(ErrorCode.COM1005);
            if (wfParamter.operatorEntity.Status == WorkFlowOperatorStatusEnum.Transfer.ParseToInt())
                throw Oops.Oh(ErrorCode.WF0007);
            if (wfParamter.operatorEntity.HandleAll.IsNotEmptyOrNull())
            {
                wfParamter.operatorEntity.HandleAll = wfParamter.operatorEntity.HandleAll.Replace(wfParamter.operatorEntity.HandleId, wfParamter.handleIds);
            }
            wfParamter.operatorEntity.HandleId = wfParamter.handleIds;
            wfParamter.operatorEntity.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
            wfParamter.operatorEntity.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
            wfParamter.operatorEntity.CreatorTime = DateTime.Now;
            wfParamter.operatorEntity.Status = WorkFlowOperatorStatusEnum.Transfer.ParseToInt();
            wfParamter.operatorEntity.DraftData = null;
            var isOk = _repository.UpdateOperator(wfParamter.operatorEntity);
            if (!isOk)
                throw Oops.Oh(ErrorCode.WF0007);

            #region 流转记录
            if (wfParamter.operatorEntity.IsProcessing == 1)
            {
                await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Processing.ParseToInt(), wfParamter.handleIds);
            }
            else
            {
                await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Transfer.ParseToInt(), wfParamter.handleIds);
            }
            #endregion
            _db.CommitTran();

            var userList = new List<string> { wfParamter.handleIds };
            var bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, new List<WorkFlowOperatorEntity>() { wfParamter.operatorEntity }, 2);
            await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC006", bodyDic);
            // 超时提醒
            await TimeoutOrRemind(wfParamter, wfParamter.operatorEntity.NodeCode, new List<WorkFlowOperatorEntity>() { wfParamter.operatorEntity });

            #region 自动审批
            await AutoAudit(wfParamter);
            #endregion
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 退回节点列表.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    public async Task<dynamic> SendBackNodeList(string operatorId)
    {
        var wfParamter = _repository.GetWorkFlowParamterByOperatorId(operatorId, null);
        var currentNodeList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode != wfParamter.operatorEntity.NodeCode && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt()).Select(x => x.NodeCode).ToList();
        currentNodeList.Add(wfParamter.startPro.nodeId);
        switch (wfParamter.nodePro.backNodeCode)
        {
            case "0":
                var upNodeCodeList0 = new List<object>() { new { id = "0", nodeCode = wfParamter.startPro.nodeId, nodeName = "流程发起" } };
                return new { list = upNodeCodeList0 };
            case "1":
                var upNodeCodeList1 = await BpmnEngineFactory.CreateBmpnEngine().GetPrevNode(string.Empty, string.Empty, wfParamter.operatorEntity.NodeId);
                if (wfParamter.nodeList.Any(x => upNodeCodeList1.Contains(x.nodeCode) && x.nodeType == WorkFlowNodeTypeEnum.outside.ToString())) throw Oops.Oh(ErrorCode.WF0018);
                if (wfParamter.nodeList.Any(x => upNodeCodeList1.Contains(x.nodeCode) && x.nodeType == WorkFlowNodeTypeEnum.subFlow.ToString())) throw Oops.Oh(ErrorCode.WF0019);
                upNodeCodeList1 = upNodeCodeList1.Intersect(currentNodeList).ToList();
                return new { list = new List<object>() { new { id = "0", nodeCode = string.Join(",", upNodeCodeList1), nodeName = "上级审批节点" } } };
            case "2":
                var upNodeList = await BpmnEngineFactory.CreateBmpnEngine().GetPrevNodeAll(wfParamter.operatorEntity.NodeId);
                upNodeList = upNodeList.Intersect(currentNodeList).ToList();
                var upNodeCodeList2 = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && upNodeList.Contains(x.NodeCode)).Select(x => new { id = "0", nodeCode = x.NodeCode, nodeName = x.NodeName }).Distinct().ToList();
                upNodeCodeList2.Add(new { id = "0", nodeCode = wfParamter.startPro.nodeId, nodeName = "流程发起" });
                return new { list = upNodeCodeList2 };
            default:
                var operatorEntity = _repository.GetOperatorInfo(x => x.NodeCode == wfParamter.nodePro.backNodeCode && x.TaskId == wfParamter.operatorEntity.TaskId && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                if (operatorEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.WF0054);
                var upNodeCodeList = new List<object>() { new { id = "0", nodeCode = operatorEntity.NodeCode, nodeName = operatorEntity.NodeName } };
                return new { list = upNodeCodeList };
        }
    }

    /// <summary>
    /// 批量候选人.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <param name="operatorId">经办id.</param>
    /// <returns></returns>
    public async Task<dynamic> GetBatchCandidate(string flowId, string operatorId, int batchType)
    {
        var wfParamter = _repository.GetWorkFlowParamterByOperatorId(operatorId, null);
        if (batchType == 0 || (batchType == 1 && wfParamter.globalPro.hasContinueAfterReject))
        {
            if (wfParamter.nodePro.divideRule == "choose")
                throw Oops.Oh(ErrorCode.WF0073);
            var conditionCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetLineCode(string.Empty, string.Empty, wfParamter.operatorEntity.NodeId);
            if (wfParamter.nodeList.Any(x => conditionCodeList.Contains(x.nodeCode) && x.nodeType == WorkFlowNodeTypeEnum.connect.ToString() && x.nodeJson.ToObject<ConditionProperties>().conditions.Any()))
                throw Oops.Oh(ErrorCode.WF0022);
        }

        var handleModel = new WorkFlowHandleModel
        {
            nodeCode = wfParamter.operatorEntity.NodeCode,
            flowId = flowId,
            handleStatus = batchType == 1 ? 0 : 1,
            formData = new { flowId = flowId, data = "{}", id = wfParamter.operatorEntity.TaskId }
        };
        return await GetCandidateModelList(operatorId, handleModel);
    }

    /// <summary>
    /// 表单数据.
    /// </summary>
    /// <param name="formId">表单id.</param>
    /// <param name="id">实例id.</param>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    public async Task<object> GetFormData(string formId, string id, string flowId = "")
    {
        return await _runService.GetOrDelFlowFormData(formId, id, 0, flowId, true);
    }

    /// <summary>
    /// 加签.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task AddSign(WorkFlowParamter wfParamter)
    {
        try
        {
            _db.BeginTran();
            if (wfParamter.operatorEntity.HandleId != _userManager.UserId && wfParamter.addSignParameter.addSignUserIdList.Contains(wfParamter.operatorEntity.HandleId))
                throw Oops.Oh(ErrorCode.WF0028);
            if (wfParamter.addSignParameter.addSignUserIdList.Contains(_userManager.UserId))
                throw Oops.Oh(ErrorCode.WF0016);
            var fEntity = _repository.GetFromEntity(wfParamter.node.formId);
            var systemControlList = wfParamter.nodePro.formOperates.ToObject<List<FormOperatesModel>>().Where(x => !x.write && x.read).Select(x => x.id).ToList();
            await _runService.SaveFlowFormData(fEntity, wfParamter.formData.ToJsonString(), wfParamter.taskEntity.Id, wfParamter.taskEntity.FlowId, true, systemControlList);
            workFlowOtherUtil.SaveNodeCandidates(wfParamter);
            #region 再次加签作废之前加签人数据
            var addSignOperatorOldList = _repository.GetAddSignOperatorList(wfParamter.operatorEntity.Id, 0, false);
            if (addSignOperatorOldList.Any())
            {
                addSignOperatorOldList.ForEach(item => item.Status = WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                _repository.UpdateOperator(addSignOperatorOldList);
            }
            #endregion
            var addSignOperatorList = new List<WorkFlowOperatorEntity>();
            var handleParameter = wfParamter.addSignParameter.Copy();
            if (wfParamter.operatorEntity.ParentId != "0")
            {
                var addSignItem = wfParamter.operatorEntity.HandleParameter.ToObject<AddSignItem>();
                var addSignLevel = addSignItem.addSignLevel;
                if (addSignLevel + 1 > _repository.GetSysConfigInfo().addSignLevel)
                    throw Oops.Oh(ErrorCode.WF0068);
                handleParameter.addSignLevel = addSignLevel + 1;
                handleParameter.branchList = addSignItem.branchList;
                handleParameter.rollBackId = wfParamter.addSignParameter.addSignType == 1 ? wfParamter.operatorEntity.Id : addSignItem.rollBackId;
            }
            else
            {
                handleParameter.addSignLevel = 1;
                handleParameter.branchList = wfParamter.branchList;
                if (wfParamter.addSignParameter.addSignType == 1)
                {
                    handleParameter.rollBackId = wfParamter.operatorEntity.Id;
                }
            }
            handleParameter.addSignUserIdList.Clear();
            foreach (var item in wfParamter.addSignParameter.addSignUserIdList)
            {
                var addSignOperatorEntity = wfParamter.operatorEntity.Adapt<WorkFlowOperatorEntity>();
                addSignOperatorEntity.Id = SnowflakeIdHelper.NextId();
                addSignOperatorEntity.ParentId = wfParamter.operatorEntity.Id;
                addSignOperatorEntity.HandleId = item;
                addSignOperatorEntity.CreatorTime = DateTime.Now;
                addSignOperatorEntity.HandleParameter = handleParameter.ToJsonString();
                addSignOperatorEntity.Status = WorkFlowOperatorStatusEnum.AddSign.ParseToInt();
                addSignOperatorEntity.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
                addSignOperatorEntity.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
                addSignOperatorEntity.DraftData = null;
                if (wfParamter.addSignParameter.counterSign == 2)
                {
                    if (!addSignOperatorList.Any())
                    {
                        addSignOperatorEntity.HandleAll = string.Join(",", wfParamter.addSignParameter.addSignUserIdList);
                        addSignOperatorList.Add(addSignOperatorEntity);
                    }
                }
                else
                {
                    addSignOperatorList.Add(addSignOperatorEntity);
                }
            }
            _repository.CreateOperator(addSignOperatorList);
            wfParamter.operatorEntity.Completion = 1;
            wfParamter.operatorEntity.HandleStatus = wfParamter.addSignParameter.addSignType == 2 ? 1 : null;
            _repository.UpdateOperator(wfParamter.operatorEntity);
            if (wfParamter.addSignParameter.addSignType == 2)
            {
                await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Agree.ParseToInt());
            }
            await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.AddSign.ParseToInt(), string.Join(",", wfParamter.addSignParameter.addSignUserIdList));
            _db.CommitTran();

            var userList = wfParamter.addSignParameter.addSignUserIdList;
            var bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, addSignOperatorList, 2);
            await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC001", bodyDic);

            // 超时提醒
            await TimeoutOrRemind(wfParamter, wfParamter.operatorEntity.NodeCode, addSignOperatorList);

            #region 自动审批
            await AutoAudit(wfParamter);
            #endregion
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 协办.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="isSave">是否保存协办.</param>
    /// <returns></returns>
    public async Task Assist(WorkFlowParamter wfParamter, bool isSave = false)
    {
        try
        {
            if (isSave)
            {
                var fEntity = _repository.GetFromEntity(wfParamter.node.formId);
                var systemControlList = wfParamter.nodePro.formOperates.ToObject<List<FormOperatesModel>>().Where(x => !x.write && x.read).Select(x => x.id).ToList();
                await _runService.SaveFlowFormData(fEntity, wfParamter.formData.ToJsonString(), wfParamter.taskEntity.Id, wfParamter.taskEntity.FlowId, true, systemControlList);
            }
            else
            {
                _db.BeginTran();
                if (wfParamter.operatorEntity.HandleId != _userManager.UserId && wfParamter.handleIds.Contains(wfParamter.operatorEntity.HandleId))
                    throw Oops.Oh(ErrorCode.WF0048);
                if (wfParamter.handleIds.Contains(_userManager.UserId))
                    throw Oops.Oh(ErrorCode.WF0017);
                var assistOperatorList = new List<WorkFlowOperatorEntity>();
                foreach (var item in wfParamter.handleIds.Split(",").ToList())
                {
                    var assistOperatorEntity = wfParamter.operatorEntity.Adapt<WorkFlowOperatorEntity>();
                    assistOperatorEntity.Id = SnowflakeIdHelper.NextId();
                    assistOperatorEntity.ParentId = wfParamter.operatorEntity.Id;
                    assistOperatorEntity.HandleId = item;
                    assistOperatorEntity.CreatorTime = DateTime.Now;
                    assistOperatorEntity.Status = WorkFlowOperatorStatusEnum.Assist.ParseToInt();
                    assistOperatorEntity.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
                    assistOperatorEntity.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
                    assistOperatorEntity.DraftData = null;
                    assistOperatorList.Add(assistOperatorEntity);
                }
                _repository.CreateOperator(assistOperatorList);
                await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Assist.ParseToInt(), wfParamter.handleIds);
                _db.CommitTran();
                var userList = wfParamter.handleIds.Split(",").ToList();
                var bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, assistOperatorList, 2);
                await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC001", bodyDic);

                // 超时提醒
                await TimeoutOrRemind(wfParamter, wfParamter.operatorEntity.NodeCode, assistOperatorList);
            }
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 减签.
    /// </summary>
    /// <param name="recordId">经办id.</param>
    /// <param name="input">审批参数.</param>
    /// <returns></returns>
    public async Task ReduceSign(string recordId, [FromBody] TaskBatchInput input)
    {
        try
        {
            var record = _repository.GetRecordInfo(x => x.Id == recordId);
            var wfParamter = _repository.GetWorkFlowParamterByOperatorId(record.OperatorId, null);
            var addSignOperator = wfParamter.operatorEntityList.FirstOrDefault(x => x.ParentId == record.OperatorId && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
            var addSignParameter = addSignOperator.HandleParameter.ToObject<AddSignItem>();
            if (addSignParameter.counterSign == 2)
            {
                // 当前所有加签待办.
                var addSignOperatorList = wfParamter.operatorEntityList.FindAll(x => x.ParentId == record.OperatorId && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                foreach (var item in addSignOperatorList)
                {
                    var addSignParameterItem = item.HandleParameter.ToObject<AddSignItem>();
                    addSignParameterItem.addSignUserIdList = item.HandleAll.Split(",").ToList();
                    if (item.Completion == 0 && input.ids.Contains(item.HandleId))
                    {
                        var index = addSignParameterItem.addSignUserIdList.IndexOf(item.HandleId);
                        var nextHandleId = addSignParameterItem.addSignUserIdList.GetIndex(index + 1);
                        if (nextHandleId.IsNullOrEmpty())
                            throw Oops.Oh(ErrorCode.WF0005);
                        item.HandleId = nextHandleId;
                    }
                    addSignParameterItem.addSignUserIdList.Remove(input.ids.FirstOrDefault());
                    item.HandleAll = string.Join(",", addSignParameterItem.addSignUserIdList);
                }
                _repository.UpdateOperator(addSignOperatorList);
            }
            else
            {
                if (_repository.GetOperatorList(x => x.ParentId == record.OperatorId && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && x.Completion == 0).Count == 1)
                    throw Oops.Oh(ErrorCode.WF0005);
                var addSignOperatorList = _repository.GetOperatorList(x => input.ids.Contains(x.HandleId) && x.ParentId == record.OperatorId && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                if (addSignOperatorList.Any(x => x.Completion == 1))
                {
                    throw Oops.Oh(ErrorCode.WF0058);
                }
                addSignOperatorList.ForEach(item => item.Status = WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                _repository.UpdateOperator(addSignOperatorList);
            }
            await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.ReduceSign.ParseToInt(), input.ids.FirstOrDefault());
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }
    #endregion

    #region 监控

    /// <summary>
    /// 终止.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    public async Task Cancel(WorkFlowParamter wfParamter)
    {
        try
        {
            if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt()) throw Oops.Oh(ErrorCode.WF0046);
            _db.BeginTran();
            var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
            var childTaskList = _repository.GetChildTaskList(wfParamter.taskEntity.Id);
            foreach (var item in childTaskList)
            {
                item.HisStatus = item.Status;
                item.Status = WorkFlowTaskStatusEnum.Cancel.ParseToInt();
                item.EndTime = DateTime.Now;
                _repository.UpdateTask(item);
                await _runService.UpdateFlowState(fromIds, item.Id, item.Status);
                foreach (var item1 in _repository.GetNodeList(x => x.FlowId == item.FlowId))
                {
                    _schedulerFactory.RemoveJob(string.Format("Job_CS_{0}_{1}", item1.NodeCode, item.Id));
                    _schedulerFactory.RemoveJob(string.Format("Job_TX_{0}_{1}", item1.NodeCode, item.Id));
                }
                wfParamter.taskEntity = item;
                await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Cancel.ParseToInt());
            }
            _db.CommitTran();
            //结束
            var bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, new List<string>() { wfParamter.taskEntity.CreatorUserId }, null, 0);
            await workFlowMsgUtil.Alerts(wfParamter.startPro.endMsgConfig, new List<string>() { wfParamter.taskEntity.CreatorUserId }, wfParamter, "MBXTLC010", bodyDic);
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 指派.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Assigned(WorkFlowParamter wfParamter, bool isAutoTransfer = false)
    {
        try
        {
            _db.BeginTran();
            wfParamter.operatorEntityList = _repository.GetOperatorList(x => x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && wfParamter.nodeCode.Contains(x.NodeCode) && x.TaskId == wfParamter.taskEntity.Id).OrderByDescending(x => x.CreatorTime).ToList();
            if (wfParamter.operatorEntityList.Any(x => x.Status == WorkFlowOperatorStatusEnum.Transfer.ParseToInt()) && isAutoTransfer)
                throw Oops.Oh(ErrorCode.WF0007);
            if (wfParamter.nodeList.Any(x => wfParamter.nodeCode.Contains(x.nodeCode) && x.nodeType == WorkFlowNodeTypeEnum.subFlow.ToString())) throw Oops.Oh(ErrorCode.WF0014);
            _repository.DeleteOperator(x => wfParamter.nodeCode.Contains(x.NodeCode) && x.TaskId == wfParamter.taskEntity.Id && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt());
            var OperatorRecordIds = _repository.GetRecordList(x => x.TaskId == wfParamter.taskEntity.Id && wfParamter.nodeCode.Contains(x.NodeCode)).Select(x => x.Id).ToList();
            _repository.DeleteRecord(OperatorRecordIds);
            _repository.DeleteLaunchUser(x => wfParamter.nodeCode.Contains(x.NodeCode) && x.TaskId == wfParamter.taskEntity.Id && x.Type == 2); // 指派逐级节点失效
            WorkFlowOperatorEntity operatorEntity = new WorkFlowOperatorEntity();
            operatorEntity.Id = SnowflakeIdHelper.NextId();
            operatorEntity.NodeCode = wfParamter.operatorEntityList.FirstOrDefault().NodeCode;
            operatorEntity.NodeName = wfParamter.operatorEntityList.FirstOrDefault().NodeName;
            operatorEntity.NodeId = wfParamter.operatorEntityList.FirstOrDefault().NodeId;
            operatorEntity.TaskId = wfParamter.taskEntity.Id;
            operatorEntity.Status = isAutoTransfer ? WorkFlowOperatorStatusEnum.Transfer.ParseToInt() : WorkFlowOperatorStatusEnum.Assigned.ParseToInt();
            operatorEntity.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
            operatorEntity.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
            operatorEntity.EngineType = wfParamter.taskEntity.EngineType;
            operatorEntity.HandleId = wfParamter.handleIds;
            operatorEntity.CreatorTime = DateTime.Now;
            operatorEntity.Completion = 0;
            wfParamter.operatorEntity = operatorEntity;

            var isOk = _repository.CreateOperator(wfParamter.operatorEntity);
            if (!isOk)
                throw Oops.Oh(ErrorCode.WF0008);

            #region 流转记录
            var handleType = isAutoTransfer ? WorkFlowRecordTypeEnum.Transfer.ParseToInt() : WorkFlowRecordTypeEnum.Assigned.ParseToInt();
            await workFlowOtherUtil.CreateRecord(wfParamter, handleType, wfParamter.handleIds);
            #endregion
            _db.CommitTran();

            _schedulerFactory.RemoveJob(string.Format("Job_CS_{0}_{1}", wfParamter.nodeCode, wfParamter.taskEntity.Id));
            _schedulerFactory.RemoveJob(string.Format("Job_TX_{0}_{1}", wfParamter.nodeCode, wfParamter.taskEntity.Id));
            var userList = wfParamter.handleIds.Split(",").ToList();
            var bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, new List<WorkFlowOperatorEntity>() { wfParamter.operatorEntity }, 2);
            var msgCode = isAutoTransfer ? "MBXTLC006" : "MBXTLC005";
            await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, msgCode, bodyDic);
            // 超时提醒
            await TimeoutOrRemind(wfParamter, wfParamter.operatorEntity.NodeCode, new List<WorkFlowOperatorEntity>() { wfParamter.operatorEntity });
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 复活.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Activate(WorkFlowParamter wfParamter)
    {
        try
        {
            _db.BeginTran();
            var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
            var childTaskList = _repository.GetChildTaskList(wfParamter.taskEntity.Id);
            var wfParamterNew = new WorkFlowParamter();
            foreach (var item in childTaskList)
            {
                item.Status = item.HisStatus;
                item.EndTime = null;
                _repository.UpdateTask(item);
                await _runService.UpdateFlowState(fromIds, item.Id, item.Status);
                wfParamterNew.taskEntity = item;
                await workFlowOtherUtil.CreateRecord(wfParamterNew, WorkFlowRecordTypeEnum.Activate.ParseToInt());
            }
            _db.CommitTran();
            //结束
            wfParamter.nextOperatorEntityList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && wfParamter.taskEntity.CurrentNodeCode.Contains(x.NodeCode) && x.Completion == 0);
            foreach (var item in wfParamter.nextOperatorEntityList)
            {
                item.CreatorTime = DateTime.Now;
            }
            _repository.UpdateOperator(wfParamter.nextOperatorEntityList);
            var messageDic = workFlowOtherUtil.GroupByOperator(wfParamter.nextOperatorEntityList);
            var bodyDic = new Dictionary<string, object>();
            foreach (var item in messageDic.Keys)
            {
                var userList = messageDic[item].Select(x => x.HandleId).ToList();
                bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, messageDic[item], 2);
                await workFlowMsgUtil.Alerts(wfParamter.startPro.waitMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC001", bodyDic);
                await workFlowMsgUtil.Alerts(wfParamter.nodePro.approveMsgConfig, bodyDic.Keys.ToList(), wfParamter, "MBXTLC002", bodyDic);
                // 超时提醒
                await TimeoutOrRemind(wfParamter, item, messageDic[item]);
            }
        }
        catch (AppFriendlyException ex)
        {
            _db.RollbackTran();
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 暂停.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Pause(WorkFlowParamter wfParamter)
    {
        var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
        var childTaskList = _repository.GetChildTaskList(wfParamter.taskEntity.Id);
        if (wfParamter.pause)
        {
            childTaskList = childTaskList.FindAll(x => x.IsAsync == 0);
        }
        foreach (var item in childTaskList)
        {
            if (item.Status != WorkFlowTaskStatusEnum.Pause.ParseToInt())
            {
                item.HisStatus = item.Status;
            }
            item.Status = WorkFlowTaskStatusEnum.Pause.ParseToInt();
            item.Restore = item.Id == wfParamter.taskEntity.Id ? 0 : 1;
            _repository.UpdateTask(item);
            await _runService.UpdateFlowState(fromIds, item.Id, item.Status);
            wfParamter.taskEntity = item;
            await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Pause.ParseToInt());
        }
    }

    /// <summary>
    /// 恢复.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task Reboot(WorkFlowParamter wfParamter)
    {
        var fromIds = wfParamter.globalPro.hasAloneConfigureForms ? wfParamter.nodeList.Select(x => x.formId).ToList() : new List<string> { wfParamter.startPro.formId };
        var childTaskList = _repository.GetChildTaskList(wfParamter.taskEntity.Id).FindAll(x => x.Restore == 1);
        childTaskList.Add(wfParamter.taskEntity);
        foreach (var item in childTaskList)
        {
            item.Status = item.HisStatus;
            _repository.UpdateTask(item);
            await _runService.UpdateFlowState(fromIds, item.Id, item.Status);
            wfParamter.taskEntity = item;
            await workFlowOtherUtil.CreateRecord(wfParamter, WorkFlowRecordTypeEnum.Reboot.ParseToInt());
        }
    }
    #endregion

    #region PublicMethod

    /// <summary>
    /// 详情操作验证.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">操作参数.</param>
    /// <returns></returns>
    public async Task<WorkFlowParamter> Validation(string operatorId, WorkFlowHandleModel handleModel)
    {
        var wfParamter = _repository.GetWorkFlowParamterByOperatorId(operatorId, handleModel);
        if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt()) throw Oops.Oh(ErrorCode.WF0046);
        if (wfParamter.flowInfo.status == 3) throw Oops.Oh(ErrorCode.WF0070);
        if (wfParamter.operatorEntity.IsNullOrEmpty() || wfParamter.operatorEntity.Status == -1)
            throw Oops.Oh(ErrorCode.WF0030);
        if (wfParamter.operatorEntity.Completion == 1)
            throw Oops.Oh(ErrorCode.WF0006);
        if (wfParamter.taskEntity.IsNullOrEmpty() || !(wfParamter.taskEntity.Status == 1 || wfParamter.taskEntity.Status == 6) || wfParamter.taskEntity.DeleteMark.IsNotEmptyOrNull())
            throw Oops.Oh(ErrorCode.WF0030);
        if (wfParamter.operatorEntity.HandleId != _userManager.UserId)
        {
            var toUserId = _repository.GetDelegateUserId(wfParamter.operatorEntity.HandleId, wfParamter.taskEntity.TemplateId, 1);
            if (!toUserId.Contains(_userManager.UserId))
                throw Oops.Oh(ErrorCode.WF0030);
        }
        return wfParamter;
    }

    /// <summary>
    /// 通过类型获取对应人员.
    /// </summary>
    /// <param name="opId">操作id.</param>
    /// <param name="handleModel">操作参数.</param>
    /// <param name="type">操作类型.</param>
    /// <returns></returns>
    public dynamic GetUserIdList(string opId, WorkFlowHandleModel handleModel, int type = 0)
    {
        var userIdList = new List<string>();
        switch (type)
        {
            case 0: // 加签人
                var record = _repository.GetRecordInfo(x => x.Id == opId);
                var operatorList = _repository.GetOperatorList(x => x.ParentId == record.OperatorId && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                if (operatorList.Any())
                {
                    var operatorInfo = operatorList.FirstOrDefault();
                    var addSignParameter = operatorInfo.HandleParameter.ToObject<AddSignItem>();
                    if (addSignParameter.counterSign == 2)
                    {
                        userIdList = operatorInfo.HandleAll.Split(",").Except(operatorList.FindAll(x => x.Completion == 1).Select(x => x.HandleId).ToList()).Select(x => x + "--user").ToList();
                    }
                    else
                    {
                        userIdList = operatorList.FindAll(x => x.Completion == 0).Select(x => x.HandleId + "--user").ToList();
                    }
                }
                break;
            case 1: // 评论人
                userIdList = _repository.GetTaskUserList(opId);
                break;
            case 2: // 流程权限发起人
                var objId = _repository.GetObjIdList(opId);
                foreach (var item in objId)
                {
                    userIdList.AddRange(item.Split(","));
                }
                break;
        }
        return workFlowUserUtil.GetCandidateItems(userIdList, handleModel, false);
    }

    /// <summary>
    /// 删除任务.
    /// </summary>
    /// <param name="taskEntityList"></param>
    /// <returns></returns>
    public async Task DeleteTask(List<WorkFlowTaskEntity> taskEntityList)
    {
        foreach (var item in taskEntityList)
        {
            var formIds = _repository.GetNodeList(x => x.FlowId == item.FlowId && (x.NodeType == "approver" || x.NodeType == "start" || x.NodeType == "processing")).Select(x => x.FormId).Distinct().ToList();
            var isRevokeTask = false;
            var revokeEntity = _repository.GetRevoke(x => x.RevokeTaskId == item.Id || x.TaskId == item.Id);
            if (revokeEntity.IsNotEmptyOrNull())
            {
                isRevokeTask = revokeEntity.RevokeTaskId == item.Id;
                if (isRevokeTask)
                {
                    if (taskEntityList.Any(x => x.Id == revokeEntity.TaskId))
                        continue;
                }
                else
                {
                    var revokeTask = _repository.GetTaskInfo(revokeEntity.RevokeTaskId);
                    if (revokeTask.IsNotEmptyOrNull())
                    {
                        await _repository.DeleteTask(revokeTask);
                    }
                }
            }
            if (!isRevokeTask && _repository.GetTaskInfo(item.Id).IsNotEmptyOrNull())
            {
                foreach (var id in formIds)
                {
                    await _runService.GetOrDelFlowFormData(id, item.Id, 1, item.FlowId);
                }
            }
            await _repository.DeleteTask(item);
        }
    }

    /// <summary>
    /// 触发任务详情.
    /// </summary>
    /// <param name="triggerId"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetTriggerTaskInfo(string triggerId)
    {
        var output = new Dictionary<string, object>();
        var triggerTask = _repository.GetTriggerTaskList(x => x.Id == triggerId && x.DeleteMark == null).FirstOrDefault();
        if (triggerTask != null)
        {
            var flowInfo = _repository.GetFlowInfo(triggerTask.FlowId);
            if (flowInfo.IsNullOrEmpty() || flowInfo.flowId.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.WF0026);
            var nodeEntityList = _repository.GetNodeList(x => x.FlowId == triggerTask.FlowId && x.DeleteMark == null);
            var nodeList = new List<NodeModel>();
            foreach (var item in nodeEntityList)
            {
                var node = new NodeModel();
                node.nodeCode = item.NodeCode;
                node.nodeType = item.NodeType;
                node.nodeName = item.NodeJson.ToObject<NodeProperties>().nodeName;
                var triggerRecord = _repository.GetTriggerRecordInfo(x => x.TriggerId == triggerTask.Id && x.NodeCode == item.NodeCode && x.DeleteMark == null);
                if (triggerRecord.IsNotEmptyOrNull())
                {
                    if (triggerRecord.Status == 0)
                    {
                        node.type = "0";
                    }
                    else if (triggerRecord.Status == 1)
                    {
                        node.type = "3";
                    }
                    else
                    {
                        node.type = "1";
                    }
                }
                if (node.nodeType == WorkFlowNodeTypeEnum.start.ToString())
                {
                    node.type = "0";
                }
                if (node.nodeType == WorkFlowNodeTypeEnum.end.ToString())
                {
                    node.type = "0";
                }
                if (triggerTask.Status == WorkFlowTaskStatusEnum.Cancel.ParseToInt())
                {
                    node.type = null;
                }
                nodeList.Add(node);
            }
            flowInfo.flowNodes = nodeEntityList.Any() ? nodeEntityList.ToDictionary(x => x.NodeCode, y => y.NodeJson.ToObject<object>()) : new Dictionary<string, object>();
            output["flowInfo"] = flowInfo;
            output["nodeList"] = nodeList;
            var recordList = _repository.GetTriggerRecordList(x => x.TriggerId == triggerTask.Id && x.DeleteMark == null).Adapt<List<TriggerRecordOutput>>();
            output["recordList"] = recordList;
            output["taskInfo"] = triggerTask.Adapt<TriggerTaskOutput>();
            if (triggerTask.Status == WorkFlowTaskStatusEnum.Runing.ParseToInt())
            {
                var btnPro = new BtnProperties();
                btnPro.hasCancelBtn = true;
                output["btnInfo"] = btnPro;
            }
        }
        return output;
    }
    #endregion

    #region 审批节点处理

    /// <summary>
    /// 根据当前审批节点插入下一节点经办.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="handleStatus">审批状态.</param>
    /// <param name="type">操作类型.</param>
    /// <returns></returns>
    private async Task CreateNextNodeOperator(WorkFlowParamter wfParamter, int type)
    {
        try
        {
            if (type == 0)
            {
                switch (wfParamter.nodePro.backNodeCode)
                {
                    case "0":
                        await BpmnEngineFactory.CreateBmpnEngine().InstanceDelete(wfParamter.taskEntity.InstanceId);
                        break;
                    default:
                        if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.SendBack.ParseToInt())
                        {
                            await BpmnEngineFactory.CreateBmpnEngine().InstanceDelete(wfParamter.taskEntity.InstanceId);
                        }
                        else
                        {
                            await BpmnEngineFactory.CreateBmpnEngine().SendBackNode(wfParamter.operatorEntity.NodeId, wfParamter.backNodeCode);
                        }
                        break;
                }
            }
            else if (type == 1)
            {
                if (!wfParamter.branchList.Any())
                {
                    var candidates = _repository.GetCandidates(x => x.NodeCode == wfParamter.node.nodeCode && x.TaskId == wfParamter.taskEntity.Id && x.Type == 3).FirstOrDefault();
                    if (candidates.IsNotEmptyOrNull() && candidates.Candidates.IsNotEmptyOrNull())
                    {
                        wfParamter.branchList = candidates.Candidates.Split(",").ToList();
                        _repository.DeleteCandidates(x => x.Id == candidates.Id);
                    }
                }
                var variables = await workFlowNodeUtil.GetConditionVariables(wfParamter);
                await BpmnEngineFactory.CreateBmpnEngine().ComplateNode(wfParamter.operatorEntity.NodeId, variables);
                _repository.SaveTaskLine(wfParamter.taskEntity.Id, variables);
                if (wfParamter.node.nodeType == WorkFlowNodeTypeEnum.approver.ToString() || wfParamter.node.nodeType == WorkFlowNodeTypeEnum.processing.ToString() || wfParamter.node.nodeType == WorkFlowNodeTypeEnum.outside.ToString())
                {
                    var nodeRecord = new WorkFlowNodeRecordEntity
                    {
                        TaskId = wfParamter.taskEntity.Id,
                        NodeId = wfParamter.operatorEntity.NodeId,
                        NodeCode = wfParamter.operatorEntity.NodeCode,
                        NodeName = wfParamter.operatorEntity.NodeName,
                        NodeStatus = wfParamter.handleStatus == 1 ? 2 : 3
                    };
                    if (wfParamter.nodePro.counterSignConfig.calculateType == 2)
                    {
                        nodeRecord.NodeStatus = wfParamter.nodePro.handleStatus == 1 ? 2 : 3;
                    }
                    _repository.CreateNodeRecord(nodeRecord);
                }
            }
            else
            {
                var variables = await workFlowNodeUtil.GetConditionVariables(wfParamter);
                var instanceId = await BpmnEngineFactory.CreateBmpnEngine().InstanceStart(wfParamter.engineId, variables);
                wfParamter.taskEntity.InstanceId = instanceId;
                _repository.UpdateTask(wfParamter.taskEntity); // 子流程未发起节点的下一节点递归回退时因instaceid没有值导致调用flowable接口报错.
                _repository.SaveTaskLine(wfParamter.taskEntity.Id, variables);
                var nodeRecord = new WorkFlowNodeRecordEntity
                {
                    TaskId = wfParamter.taskEntity.Id,
                    NodeCode = wfParamter.operatorEntity.NodeCode,
                    NodeName = wfParamter.operatorEntity.NodeName,
                    NodeStatus = 1
                };
                _repository.CreateNodeRecord(nodeRecord);
            }
            // 下个节点集合
            var nextNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetCurrentNodeList(wfParamter.taskEntity.InstanceId);
            var currentNodeCode = wfParamter.taskEntity.CurrentNodeCode;
            if (nextNodeCodeList.Any())
            {
                var isAsync = false;
                foreach (var item in nextNodeCodeList)
                {
                    if (!currentNodeCode.Contains(item.taskKey))
                    {
                        var nextNode = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == item.taskKey);
                        nextNode.nodeId = item.taskId;
                        if (WorkFlowNodeTypeEnum.approver.ToString().Equals(nextNode.nodeType) || WorkFlowNodeTypeEnum.processing.ToString().Equals(nextNode.nodeType))
                        {
                            var isAutoApprover = false;
                            if (nextNode.nodePro.hasAutoApprover)
                            {
                                var operatorEntity = new WorkFlowOperatorEntity
                                {
                                    Id = SnowflakeIdHelper.NextId(),
                                    NodeCode = nextNode.nodeCode,
                                    NodeName = nextNode.nodePro.nodeName,
                                    NodeId = nextNode.nodeId,
                                    HandleId = "jnpf",
                                    EngineType = wfParamter.taskEntity.EngineType,
                                    TaskId = wfParamter.taskEntity.Id,
                                    Status = WorkFlowOperatorStatusEnum.Runing.ParseToInt(),
                                    SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now,
                                    StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now,
                                    CreatorTime = DateTime.Now,
                                    Completion = 0,
                                    IsProcessing = WorkFlowNodeTypeEnum.processing.ToString().Equals(nextNode.nodeType) ? 1 : 0,
                                };
                                if (nextNode.nodePro.counterSign == 2)
                                {
                                    var userIds = await workFlowUserUtil.GetFlowUserId(wfParamter, nextNode);
                                    operatorEntity.HandleAll = string.Join(",", userIds);
                                }
                                if (workFlowNodeUtil.ConditionNodeJudge(wfParamter, nextNode.nodePro.autoAuditRule))
                                {
                                    isAutoApprover = true;
                                    operatorEntity.HandleStatus = 1;
                                }
                                else if (workFlowNodeUtil.ConditionNodeJudge(wfParamter, nextNode.nodePro.autoRejectRule))
                                {
                                    isAutoApprover = true;
                                    operatorEntity.HandleStatus = 0;
                                }

                                if (isAutoApprover)
                                {
                                    try
                                    {
                                        #region 验证下一节点的下一节点是否候选人节点
                                        List<WorkFlowNodeModel> nextNodeList = new List<WorkFlowNodeModel>();
                                        var candidateModels = new List<CandidateModel>();
                                        var wfParamterAuto = wfParamter.Copy();
                                        wfParamterAuto.node = nextNode;
                                        wfParamterAuto.nodePro = nextNode.nodePro;
                                        wfParamterAuto.operatorEntity = operatorEntity;
                                        wfParamterAuto.branchList = new List<string>();
                                        await workFlowNodeUtil.SaveGlobalParamter(wfParamterAuto, false);
                                        await workFlowNodeUtil.GetNextNodeList(wfParamterAuto, nextNodeList, true);
                                        await workFlowUserUtil.GetCandidates(candidateModels, nextNodeList);
                                        await workFlowUserUtil.GetErrorNode(wfParamterAuto, nextNodeList);
                                        #endregion
                                        if (!candidateModels.Any(x => x.isCandidates) && !wfParamterAuto.errorNodeList.Any() && wfParamterAuto.nodePro.divideRule != "choose")
                                        {
                                            wfParamter.nextOperatorEntityList.Add(operatorEntity);
                                        }
                                        else
                                        {
                                            isAutoApprover = false;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        isAutoApprover = false;
                                    }
                                }
                            }
                            wfParamter.nextNodeFormData = await workFlowOtherUtil.GetNextFormData(wfParamter, nextNode);
                            if (!isAutoApprover)
                            {
                                await workFlowUserUtil.AddOperatorByType(wfParamter, nextNode);
                            }
                        }
                        else if (WorkFlowNodeTypeEnum.trigger.ToString().Equals(nextNode.nodeType))
                        {
                            wfParamter.triggerNodeList.Add(nextNode);
                            item.isTriggerNode = true;
                        }
                        else if (WorkFlowNodeTypeEnum.subFlow.ToString().Equals(nextNode.nodeType))
                        {
                            #region 子流程下一节点异常审批人验证
                            var subNextNodeList = new List<WorkFlowNodeModel>();
                            var wfParamterAuto = wfParamter.Copy();
                            wfParamterAuto.node = nextNode;
                            wfParamterAuto.nodePro = nextNode.nodePro;
                            wfParamterAuto.operatorEntity.HandleId = "jnpf";
                            wfParamterAuto.branchList = new List<string>();
                            await workFlowNodeUtil.GetNextNodeList(wfParamterAuto, subNextNodeList, true);
                            foreach (var subNextNode in subNextNodeList)
                            {
                                if (WorkFlowNodeTypeEnum.subFlow.ToString().Equals(subNextNode.nodeType) || WorkFlowNodeTypeEnum.outside.ToString().Equals(subNextNode.nodeType)) continue;
                                var list = await workFlowUserUtil.GetFlowUserId(wfParamterAuto, subNextNode);
                                if (list.Count == 0)
                                {
                                    if (wfParamter.globalPro.errorRule == 3 && !wfParamterAuto.errorNodeList.Select(x => x.nodeCode).Contains(subNextNode.nodeCode))
                                    {
                                        if (!_repository.GetCandidates(subNextNode.nodeCode, wfParamter.taskEntity.Id, 2).Any())
                                        {
                                            var candidateItem = new CandidateModel();
                                            candidateItem.nodeCode = subNextNode.nodeCode;
                                            candidateItem.nodeName = subNextNode.nodePro.nodeName;
                                            wfParamter.errorNodeList.Add(candidateItem);
                                        }
                                    }
                                    if (wfParamter.globalPro.errorRule == 5)
                                        throw Oops.Oh(ErrorCode.WF0035);
                                }
                            }
                            #endregion
                            var childFormData = await workFlowOtherUtil.GetSubTaskFormData(wfParamter, nextNode);
                            var wfParamterChild = wfParamter.Copy();
                            wfParamterChild.formData = childFormData;
                            // 子流程发起人
                            var childTaskCrUserList = await workFlowUserUtil.GetFlowUserId(wfParamterChild, nextNode);
                            if (_repository.GetTemplate(nextNode.nodePro.flowId).VisibleType == 2 && nextNode.nodePro.hasPermission && childTaskCrUserList.Any())
                            {
                                var userAllList = await _usersService.GetList();
                                var adminUserIds = _userManager.UserOrigin.Equals("pc") ? userAllList.Where(x => "1" == x.Standing && x.IsAdministrator.Equals(1)).Select(x => x.Id).ToList() : userAllList.Where(x => x.AppStanding.Equals("1") && x.IsAdministrator.Equals(1)).Select(x => x.Id).ToList();
                                adminUserIds = childTaskCrUserList.Intersect(adminUserIds).ToList();
                                var ObjIdList = _repository.GetObjIdList(nextNode.nodePro.flowId);
                                var userIds = workFlowUserUtil.GetUserDefined(new NodeProperties { approvers = ObjIdList }, wfParamterChild.taskEntity.Id);
                                childTaskCrUserList = childTaskCrUserList.Intersect(userIds).ToList();
                                if (adminUserIds.Any())
                                {
                                    childTaskCrUserList.AddRange(adminUserIds);
                                }
                            }
                            if (childTaskCrUserList.Count == 0) // 子流程发起人异常验证
                            {
                                switch (nextNode.nodePro.errorRule)
                                {
                                    case 2:
                                        if ((await _usersService.GetList(nextNode.nodePro.errorRuleUser)).Any())
                                        {
                                            childTaskCrUserList = nextNode.nodePro.errorRuleUser;
                                        }
                                        else
                                        {
                                            childTaskCrUserList.Add(_userManager.GetAdminUserId());
                                        }
                                        break;
                                    case 6:
                                        childTaskCrUserList.Add(wfParamter.taskEntity.CreatorUserId);
                                        break;
                                    default:
                                        childTaskCrUserList.Add(_userManager.GetAdminUserId());
                                        break;
                                }
                            }
                            var subParameter = new { formId = wfParamter.node.formId, nodeId = nextNode.nodeId }.ToJsonString();
                            await CreateSubTask(nextNode, childFormData, wfParamter.taskEntity.Id, subParameter, childTaskCrUserList);
                            if (nextNode.nodePro.isAsync) // 异步子流程则跳过插入子流程下一节点经办 
                            {
                                // 异步子流程审批
                                var subParamter = wfParamter.Copy();
                                subParamter.node = nextNode;
                                subParamter.nodePro = nextNode.nodePro;
                                subParamter.operatorEntity = new WorkFlowOperatorEntity();
                                subParamter.operatorEntity.Id = "0";
                                subParamter.operatorEntity.NodeCode = nextNode.nodeCode;
                                subParamter.operatorEntity.NodeId = nextNode.nodeId;
                                subParamter.taskEntity.CurrentNodeCode = string.Join(",", nextNodeCodeList.Select(x => x.taskKey));
                                subParamter.taskEntity.CurrentNodeName = string.Join(",", wfParamter.nodeList.FindAll(x => subParamter.taskEntity.CurrentNodeCode.Contains(x.nodeCode)).Select(x => x.nodePro.nodeName));
                                await CreateNextNodeOperator(subParamter, 1);
                                wfParamter.taskEntity = subParamter.taskEntity;
                                isAsync = true;
                            }
                        }
                        else if (WorkFlowNodeTypeEnum.outside.ToString().Equals(nextNode.nodeType))
                        {
                            var eventNextNodeList = new List<WorkFlowNodeModel>();
                            var wfParamterAuto = wfParamter.Copy();
                            wfParamterAuto.node = nextNode;
                            wfParamterAuto.nodePro = nextNode.nodePro;
                            wfParamterAuto.operatorEntity.HandleId = "jnpf";
                            wfParamterAuto.branchList = new List<string>();
                            await workFlowNodeUtil.GetNextNodeList(wfParamterAuto, eventNextNodeList, true);
                            foreach (var subNextNode in eventNextNodeList)
                            {
                                if (WorkFlowNodeTypeEnum.subFlow.ToString().Equals(subNextNode.nodeType) || WorkFlowNodeTypeEnum.outside.ToString().Equals(subNextNode.nodeType)) continue;
                                var list = await workFlowUserUtil.GetFlowUserId(wfParamterAuto, subNextNode);
                                if (list.Count == 0)
                                {
                                    if (wfParamter.globalPro.errorRule == 3 && !wfParamterAuto.errorNodeList.Select(x => x.nodeCode).Contains(subNextNode.nodeCode))
                                    {
                                        if (!_repository.GetCandidates(subNextNode.nodeCode, wfParamter.taskEntity.Id, 2).Any())
                                        {
                                            var candidateItem = new CandidateModel();
                                            candidateItem.nodeCode = subNextNode.nodeCode;
                                            candidateItem.nodeName = subNextNode.nodePro.nodeName;
                                            wfParamter.errorNodeList.Add(candidateItem);
                                        }
                                    }
                                    if (wfParamter.globalPro.errorRule == 5)
                                        throw Oops.Oh(ErrorCode.WF0035);
                                }
                            }
                            var eventLog = new WorkFlowEventLogEntity
                            {
                                TaskId = wfParamter.taskEntity.Id,
                                NodeId = nextNode.nodeId,
                                NodeName = nextNode.nodeName,
                                NodeCode = nextNode.nodeCode,
                                UpCode = wfParamter.node.nodeCode,
                                Type = WorkFlowNodeTypeEnum.outside.ToString(),
                                InterfaceId = nextNode.nodePro.formId,
                            };
                            _repository.CreateEventLog(eventLog);
                        }
                    }
                }

                if (wfParamter.nextOperatorEntityList.Any())
                {
                    if (wfParamter.triggerNodeList != null && wfParamter.triggerNodeList.Any() && wfParamter.triggerNodeList.Any(x => !x.nodePro.isAsync))
                    {
                        wfParamter.nextOperatorEntityList.ForEach(x => x.Status = WorkFlowOperatorStatusEnum.unActivated.ParseToInt());
                    }
                    _repository.CreateOperator(wfParamter.nextOperatorEntityList);
                }

                if (!isAsync)
                {
                    var isEnd = await BpmnEngineFactory.CreateBmpnEngine().IsEnd(wfParamter.taskEntity.InstanceId);
                    if (isEnd)
                    {
                        wfParamter.taskEntity.CurrentNodeCode = "end";
                        wfParamter.taskEntity.CurrentNodeName = "结束";
                        wfParamter.taskEntity.Status = wfParamter.handleStatus == 0 ? WorkFlowTaskStatusEnum.Reject.ParseToInt() : WorkFlowTaskStatusEnum.Pass.ParseToInt();
                        wfParamter.taskEntity.EndTime = DateTime.Now;
                        if (wfParamter.taskEntity.ParentId != "0") // 子流程结束回到主流程下一节点
                        {
                            await InsertSubTaskNextNode(wfParamter.taskEntity);
                        }
                    }
                    else
                    {
                        wfParamter.taskEntity.CurrentNodeCode = string.Join(",", nextNodeCodeList.Where(x => !x.isTriggerNode).Select(x => x.taskKey));
                        wfParamter.taskEntity.CurrentNodeName = string.Join(",", wfParamter.nodeList.FindAll(x => wfParamter.taskEntity.CurrentNodeCode.Contains(x.nodeCode)).Select(x => x.nodePro.nodeName));
                    }
                }
            }
            else
            {
                if (type != 0) // 退回不验证结束
                {
                    var instance = await BpmnEngineFactory.CreateBmpnEngine().InstanceInfo(wfParamter.taskEntity.InstanceId);
                    if (instance.endTime.IsNotEmptyOrNull())
                    {
                        wfParamter.taskEntity.CurrentNodeCode = "end";
                        wfParamter.taskEntity.CurrentNodeName = "结束";
                        wfParamter.taskEntity.Status = wfParamter.handleStatus == 0 ? WorkFlowTaskStatusEnum.Reject.ParseToInt() : WorkFlowTaskStatusEnum.Pass.ParseToInt();
                        if (wfParamter.nodePro.counterSignConfig.calculateType == 2)
                        {
                            wfParamter.taskEntity.Status = wfParamter.nodePro.handleStatus == 0 ? WorkFlowTaskStatusEnum.Reject.ParseToInt() : WorkFlowTaskStatusEnum.Pass.ParseToInt();
                        }
                        wfParamter.taskEntity.EndTime = DateTime.Now;
                        if (wfParamter.taskEntity.ParentId != "0") // 子流程结束回到主流程下一节点
                        {
                            await InsertSubTaskNextNode(wfParamter.taskEntity);
                        }
                    }
                }
            }
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 自动同意审批.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="isTimeOut">是否超时审批.</param>
    /// <returns></returns>
    public async Task AutoAudit(WorkFlowParamter wfParamter, bool isTimeOut = false, bool isRevoke = false)
    {
        try
        {
            var eventLog = _repository.GetEventLogList(x => x.TaskId == wfParamter.taskEntity.Id && x.DeleteMark == null && x.Status == null);
            Dictionary<string, object> initFormData = null;
            if (wfParamter.node.formId != wfParamter.startPro.formId)
            {
                initFormData = await _runService.GetOrDelFlowFormData(wfParamter.startPro.formId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId);
            }
            await workFlowMsgUtil.OutSideRequest(wfParamter, eventLog, initFormData);
            var operatorEntityList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && x.Completion == 0 && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.unActivated.ParseToInt());
            foreach (var item in operatorEntityList)
            {
                var isAuto = false;
                var handleModel = new WorkFlowHandleModel();
                var autoNode = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == item.NodeCode);
                var remake = autoNode.nodeType == WorkFlowNodeTypeEnum.processing.ToString() ? "办理" : "审批";
                if (item.HandleId.Equals("jnpf"))
                {
                    isAuto = true;
                    handleModel.handleStatus = item.HandleStatus.IsNotEmptyOrNull() ? item.HandleStatus.ParseToInt() : 1;
                    handleModel.handleOpinion = item.HandleStatus.IsNotEmptyOrNull() ? string.Format("系统{0}", remake) : string.Format("默认{0}通过", remake);
                    if (isRevoke)
                    {
                        handleModel.handleOpinion = string.Format("系统{0}", remake);
                    }
                }
                else if (isTimeOut)
                {
                    isAuto = true;
                    handleModel.handleOpinion = string.Format("超时{0}通过", remake);
                }
                else
                {
                    if (wfParamter.globalPro.autoSubmitConfig.initiatorApproverRepeated && !isAuto)
                    {
                        isAuto = item.HandleId == wfParamter.taskEntity.CreatorUserId;
                    }
                    if (wfParamter.globalPro.autoSubmitConfig.approverHasApproval && !isAuto)
                    {
                        isAuto = _repository.GetRecordInfo(x => x.TaskId == item.TaskId && x.HandleId == item.HandleId && (x.HandleType == 0 || x.HandleType == 1) && x.Status >= 0).IsNotEmptyOrNull();
                    }
                    if (wfParamter.globalPro.autoSubmitConfig.adjacentNodeApproverRepeated && !isAuto)
                    {
                        var upNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetPrevNode(wfParamter.engineId, item.NodeCode, string.Empty);
                        isAuto = _repository.GetRecordInfo(x => x.TaskId == item.TaskId && upNodeCodeList.Contains(x.NodeCode) && x.HandleId == item.HandleId && x.HandleType == wfParamter.handleStatus && x.Status >= 0).IsNotEmptyOrNull();
                    }
                    handleModel.handleOpinion = string.Format("{0}自动{1}通过", _userManager.GetUserName(item.HandleId), remake);
                }
                if (isAuto)
                {
                    if (isRevoke)
                    {
                        var wfParamterAuto = _repository.GetWorkFlowParamterByOperatorId(item.Id, handleModel);
                        if (wfParamterAuto.operatorEntity.Completion == 0 && wfParamterAuto.taskEntity.EndTime.IsNullOrEmpty())
                        {
                            await this.RevokeAudit(wfParamterAuto);
                        }
                    }
                    else
                    {
                        handleModel.formData = await _runService.GetOrDelFlowFormData(autoNode.formId, item.TaskId, 0, wfParamter.flowInfo.flowId, true);
                        var wfParamterAuto = _repository.GetWorkFlowParamterByOperatorId(item.Id, handleModel);
                        var candidateModels = new List<CandidateModel>();
                        List<WorkFlowNodeModel> nextNodeList = new List<WorkFlowNodeModel>();
                        wfParamterAuto.branchList = new List<string>();
                        var isStopAuto = false;
                        try
                        {
                            await workFlowNodeUtil.SaveGlobalParamter(wfParamterAuto, false);
                            await workFlowNodeUtil.GetNextNodeList(wfParamterAuto, nextNodeList, true);
                            await workFlowUserUtil.GetCandidates(candidateModels, nextNodeList);
                            await workFlowUserUtil.GetErrorNode(wfParamterAuto, nextNodeList);
                            isStopAuto = candidateModels.Any(x => x.isCandidates) || wfParamterAuto.errorNodeList.Any() || autoNode.nodePro.divideRule == "choose";
                        }
                        catch (Exception)
                        {
                            isStopAuto = true;
                        }
                        if (isStopAuto && item.HandleId.Equals("jnpf")) // 如果自动审批下一节点是候选人由超管审批
                        {
                            item.HandleId = _userManager.GetAdminUserId();
                            item.HandleStatus = null;
                            _repository.UpdateOperator(item);
                        }
                        if (wfParamterAuto.operatorEntity.Completion == 0 && wfParamterAuto.taskEntity.EndTime.IsNullOrEmpty() && !isStopAuto)
                        {
                            await this.Audit(wfParamterAuto, true);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 获取任务参数以及流程按钮权限.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowId"></param>
    /// <param name="opType"></param>
    /// <param name="opId"></param>
    /// <param name="output.btnInfo"></param>
    /// <returns></returns>
    public async Task<WorkFlowParamter> GetWorkFlowParamter(string taskId, string flowId, string opType, string opId, TaskInfoOutput output)
    {
        WorkFlowParamter wfParamter = null;
        var recordEntity = new WorkFlowRecordEntity();
        var revokeEntity = _repository.GetRevoke(x => x.RevokeTaskId == taskId);
        if (opId.IsNotEmptyOrNull())
        {
            if (opType == "4")
            {
                recordEntity = _repository.GetRecordInfo(opId);
                if (_repository.GetOperatorInfo(x => x.Id == recordEntity.OperatorId).IsNotEmptyOrNull())
                {
                    wfParamter = _repository.GetWorkFlowParamterByOperatorId(recordEntity.OperatorId, null);
                }
            }
            else if (opType == "5")
            {
                _repository.UpdateCirculate(opId);
                var circulateEntity = _repository.GetCirculateInfo(x => x.Id == opId);
                if (circulateEntity.IsNotEmptyOrNull())
                {
                    wfParamter = _repository.GetWorkFlowParamterByOperatorId(circulateEntity.OperatorId, null);
                }
            }
            else
            {
                wfParamter = _repository.GetWorkFlowParamterByOperatorId(opId, null);
            }
        }

        if (wfParamter.IsNullOrEmpty())
        {
            if (_repository.AnyTask(x => x.Id == taskId && x.DeleteMark == null))
            {
                wfParamter = _repository.GetWorkFlowParamterByTaskId(taskId, null);
            }
            else
            {
                var template = _repository.GetTemplate(flowId, false);
                if (template.IsNullOrEmpty() || template.FlowId.IsNullOrEmpty())
                {
                    throw Oops.Oh(ErrorCode.WF0033);
                }
                else
                {
                    flowId = template.FlowId;
                }
                wfParamter = _repository.GetWorkFlowParamterByFlowId(flowId, null);
            }
        }

        if (revokeEntity.IsNullOrEmpty())
        {
            wfParamter.formData = await _runService.GetOrDelFlowFormData(wfParamter.node.formId, taskId, 0, wfParamter.flowInfo.flowId, true);
        }

        switch (opType)
        {
            case "0": // 我发起的详情
                if (revokeEntity.IsNotEmptyOrNull())
                {
                    break;
                }
                switch (wfParamter.taskEntity.Status)
                {
                    case (int)WorkFlowTaskStatusEnum.Draft:
                        output.btnInfo = await workFlowUserUtil.GetLaunchBtn(wfParamter);
                        break;
                    case (int)WorkFlowTaskStatusEnum.Runing:
                        if (wfParamter.globalPro.hasInitiatorPressOverdueNode && _repository.GetOperatorInfo(x => x.TaskId == taskId && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && x.Completion == 0 && x.Duedate != null).IsNotEmptyOrNull())
                        {
                            output.btnInfo.hasPressBtn = true;
                        }
                        if (wfParamter.globalPro.recallRule == 2 || wfParamter.globalPro.recallRule == 3)
                        {
                            output.btnInfo.hasRecallLaunchBtn = true;
                        }
                        break;
                    case (int)WorkFlowTaskStatusEnum.Pass:
                        if (wfParamter.taskEntity.ParentId == "0" && !_repository.AnyRevoke(x => x.TaskId == taskId && x.DeleteMark == null))
                        {
                            output.btnInfo.hasRevokeBtn = wfParamter.globalPro.hasRevoke;
                        }
                        break;
                    case (int)WorkFlowTaskStatusEnum.SendBack:
                        break;
                    case (int)WorkFlowTaskStatusEnum.Recall:
                        break;
                }
                if (wfParamter.taskEntity.Status != WorkFlowTaskStatusEnum.Draft.ParseToInt() && wfParamter.nodePro.printConfig.on && wfParamter.nodePro.printConfig.printIds.IsNotEmptyOrNull() && wfParamter.nodePro.printConfig.printIds.Any() && revokeEntity.IsNullOrEmpty())
                {
                    switch (wfParamter.nodePro.printConfig.conditionType)
                    {
                        case 2:
                            output.btnInfo.hasPrintBtn = true;
                            break;
                        case 3:
                            if (wfParamter.taskEntity.EndTime.IsNotEmptyOrNull())
                            {
                                output.btnInfo.hasPrintBtn = true;
                            }
                            break;
                        case 4:
                            if (workFlowNodeUtil.ConditionNodeJudge(wfParamter, wfParamter.nodePro.printConfig))
                            {
                                output.btnInfo.hasPrintBtn = true;
                            }
                            break;
                        default:
                            output.btnInfo.hasPrintBtn = true;
                            break;
                    }
                }
                break;
            case "1": // 待签事宜
                switch (wfParamter.taskEntity.Status)
                {
                    case (int)WorkFlowTaskStatusEnum.Runing:
                        output.btnInfo.hasSignBtn = true;
                        output.btnInfo.hasViewStartFormBtn = wfParamter.globalPro.hasAloneConfigureForms && wfParamter.nodePro.formId.IsNotEmptyOrNull();
                        output.btnInfo.proxyMark = wfParamter.operatorEntity.HandleId != _userManager.UserId;
                        break;
                    case (int)WorkFlowTaskStatusEnum.Revokeing:
                        switch (wfParamter.operatorEntity.Status)
                        {
                            case (int)WorkFlowOperatorStatusEnum.Revoke:
                                output.btnInfo.hasSignBtn = true;
                                break;
                        }
                        break;
                }
                break;
            case "2": // 待办事宜
                switch (wfParamter.taskEntity.Status)
                {
                    case (int)WorkFlowTaskStatusEnum.Runing:
                        output.btnInfo.hasTransactBtn = true;
                        output.btnInfo.hasReduceSignBtn = wfParamter.globalPro.hasSignFor;
                        output.btnInfo.hasViewStartFormBtn = wfParamter.globalPro.hasAloneConfigureForms && wfParamter.nodePro.formId.IsNotEmptyOrNull();
                        output.btnInfo.proxyMark = wfParamter.operatorEntity.HandleId != _userManager.UserId;
                        break;
                    case (int)WorkFlowTaskStatusEnum.Revokeing:
                        switch (wfParamter.operatorEntity.Status)
                        {
                            case (int)WorkFlowOperatorStatusEnum.Revoke:
                                output.btnInfo.hasTransactBtn = true;
                                output.btnInfo.hasReduceSignBtn = wfParamter.globalPro.hasSignFor;
                                break;
                        }
                        break;
                }
                break;
            case "3": // 在办事宜
                switch (wfParamter.taskEntity.Status)
                {
                    case (int)WorkFlowTaskStatusEnum.Runing:
                        switch (wfParamter.operatorEntity.Status)
                        {
                            case (int)WorkFlowOperatorStatusEnum.AddSign:
                                output.btnInfo.hasAuditBtn = wfParamter.nodePro.hasAuditBtn;
                                output.btnInfo.hasRejectBtn = wfParamter.nodePro.hasRejectBtn;
                                output.btnInfo.hasSaveAuditBtn = wfParamter.nodePro.hasSaveAuditBtn;
                                output.btnInfo.hasBackBtn = wfParamter.nodePro.hasBackBtn;
                                output.btnInfo.hasAssistBtn = wfParamter.nodePro.hasAssistBtn;
                                if (wfParamter.operatorEntity.HandleParameter.ToObject<AddSignItem>().addSignLevel + 1 <= _repository.GetSysConfigInfo().addSignLevel)
                                {
                                    output.btnInfo.hasFreeApproverBtn = wfParamter.nodePro.hasFreeApproverBtn;
                                }
                                break;
                            case (int)WorkFlowOperatorStatusEnum.Assist:
                                output.btnInfo.hasAssistSaveBtn = true;
                                break;
                            case (int)WorkFlowOperatorStatusEnum.Revoke:
                                output.btnInfo.hasAuditBtn = wfParamter.nodePro.hasAuditBtn;
                                output.btnInfo.hasRejectBtn = wfParamter.nodePro.hasRejectBtn;
                                break;
                            default:
                                if (wfParamter.operatorEntity.ParentId != "0")
                                {
                                    output.btnInfo.hasAuditBtn = wfParamter.nodePro.hasAuditBtn;
                                    output.btnInfo.hasRejectBtn = wfParamter.nodePro.hasRejectBtn;
                                    output.btnInfo.hasSaveAuditBtn = wfParamter.nodePro.hasSaveAuditBtn;
                                    output.btnInfo.hasBackBtn = wfParamter.nodePro.hasBackBtn;
                                    output.btnInfo.hasAssistBtn = wfParamter.nodePro.hasAssistBtn;
                                }
                                else
                                {
                                    output.btnInfo = wfParamter.nodePro.ToObject<BtnProperties>();
                                    output.btnInfo.hasReduceApproverBtn = false;
                                    output.btnInfo.hasRecallAuditBtn = false;
                                    if (wfParamter.operatorEntity.Status == (int)WorkFlowOperatorStatusEnum.Transfer)
                                    {
                                        output.btnInfo.hasTransferBtn = false;
                                        output.btnInfo.hasFreeApproverBtn = false;
                                    }
                                    if (wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull())
                                    {
                                        output.btnInfo.hasBackBtn = false;
                                    }
                                }
                                break;
                        }
                        output.btnInfo.hasViewStartFormBtn = wfParamter.globalPro.hasAloneConfigureForms && wfParamter.nodePro.formId.IsNotEmptyOrNull();
                        output.btnInfo.proxyMark = wfParamter.operatorEntity.HandleId != _userManager.UserId;
                        break;
                    case (int)WorkFlowTaskStatusEnum.Revokeing:
                        switch (wfParamter.operatorEntity.Status)
                        {
                            case (int)WorkFlowOperatorStatusEnum.Revoke:
                                output.btnInfo.hasAuditBtn = wfParamter.nodePro.hasAuditBtn;
                                output.btnInfo.hasRejectBtn = wfParamter.nodePro.hasRejectBtn;
                                break;
                        }
                        break;
                }
                if (wfParamter.nodePro.printConfig.on && wfParamter.nodePro.printConfig.printIds.Any() && revokeEntity.IsNullOrEmpty())
                {
                    switch (wfParamter.nodePro.printConfig.conditionType)
                    {
                        case 2:
                            break;
                        case 3:
                            if (wfParamter.taskEntity.EndTime.IsNotEmptyOrNull())
                            {
                                output.btnInfo.hasPrintBtn = true;
                            }
                            break;
                        case 4:
                            if (workFlowNodeUtil.ConditionNodeJudge(wfParamter, wfParamter.nodePro.printConfig))
                            {
                                output.btnInfo.hasPrintBtn = true;
                            }
                            break;
                        default:
                            output.btnInfo.hasPrintBtn = true;
                            break;
                    }
                }
                break;
            case "4": // 已办事宜
                if (recordEntity.IsNotEmptyOrNull() && recordEntity.OperatorId.IsNotEmptyOrNull())
                {
                    switch (wfParamter.taskEntity.Status)
                    {
                        case (int)WorkFlowTaskStatusEnum.Runing:
                            switch (recordEntity.HandleType)
                            {
                                case (int)WorkFlowRecordTypeEnum.Reject:
                                case (int)WorkFlowRecordTypeEnum.Agree:
                                    output.btnInfo.hasRecallAuditBtn = wfParamter.globalPro.recallRule == 3;
                                    break;
                                case (int)WorkFlowRecordTypeEnum.AddSign:
                                    if (_repository.GetOperatorList(x => x.ParentId == recordEntity.OperatorId && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt() && x.Completion == 0).Any())
                                    {
                                        output.btnInfo.hasReduceApproverBtn = wfParamter.nodePro.hasReduceApproverBtn;
                                    }
                                    break;
                            }
                            break;
                    }
                }
                else
                {
                    wfParamter = _repository.GetWorkFlowParamterByTaskId(taskId, null);
                }
                output.btnInfo.hasViewStartFormBtn = wfParamter.globalPro.hasAloneConfigureForms && wfParamter.nodePro.formId.IsNotEmptyOrNull();
                if (wfParamter.nodePro.printConfig.on && wfParamter.nodePro.printConfig.printIds.Any() && revokeEntity.IsNullOrEmpty())
                {
                    switch (wfParamter.nodePro.printConfig.conditionType)
                    {
                        case 2:
                            if (!wfParamter.taskEntity.CurrentNodeCode.Contains(wfParamter.node.nodeCode))
                            {
                                output.btnInfo.hasPrintBtn = true;
                            }
                            break;
                        case 3:
                            if (wfParamter.taskEntity.EndTime.IsNotEmptyOrNull())
                            {
                                output.btnInfo.hasPrintBtn = true;
                            }
                            break;
                        case 4:
                            if (workFlowNodeUtil.ConditionNodeJudge(wfParamter, wfParamter.nodePro.printConfig))
                            {
                                output.btnInfo.hasPrintBtn = true;
                            }
                            break;
                        default:
                            output.btnInfo.hasPrintBtn = true;
                            break;
                    }
                }
                break;
            case "5": // 抄送事宜
                break;
            case "6": // 流程监控
                switch (wfParamter.taskEntity.Status)
                {
                    case (int)WorkFlowTaskStatusEnum.Runing:
                        output.btnInfo.hasPauseBtn = true;
                        if (wfParamter.nodeList.Any(x => wfParamter.taskEntity.CurrentNodeCode.Contains(x.nodeCode) && (x.nodeType == WorkFlowNodeTypeEnum.approver.ToString() || x.nodeType == WorkFlowNodeTypeEnum.processing.ToString())))
                        {
                            output.btnInfo.hasAssignBtn = true;
                        }
                        if (wfParamter.taskEntity.ParentId == "0")
                        {
                            output.btnInfo.hasCancelBtn = true;
                        }
                        break;
                    case (int)WorkFlowTaskStatusEnum.Cancel:
                        if (wfParamter.taskEntity.ParentId == "0")
                        {
                            output.btnInfo.hasActivateBtn = true;
                        }
                        break;
                    case (int)WorkFlowTaskStatusEnum.Pause:
                        if (wfParamter.taskEntity.Restore.IsNullOrEmpty() || wfParamter.taskEntity.Restore == 0)
                        {
                            output.btnInfo.hasRebootBtn = true;
                        }
                        break;
                }
                if (wfParamter.taskEntity.EndTime.IsNotEmptyOrNull() && wfParamter.taskEntity.IsFile == 0)
                {
                    output.btnInfo.hasFileBtn = true;
                }
                if (wfParamter.flowInfo.status != 1)
                {
                    output.btnInfo.hasActivateBtn = false;
                }
                break;
            default: // 我发起的新建/编辑
                output.btnInfo = await workFlowUserUtil.GetLaunchBtn(wfParamter);
                break;
        }
        output.flowInfo = wfParamter.flowInfo;
        output.formInfo = _repository.GetFromEntity(wfParamter.node.formId).Adapt<FormModel>();
        if (output.formInfo == null) throw Oops.Oh(ErrorCode.WF0053);
        if (revokeEntity.IsNotEmptyOrNull())
        {
            wfParamter.isRevoke = true;
            wfParamter.revokeEntity = revokeEntity;
            var formModel = new FormModel
            {
                enCode = "revoke",
                type = 2
            };
            output.formInfo = formModel;
        }
        output.formOperates = revokeEntity.IsNullOrEmpty() ? wfParamter.nodePro.formOperates : new List<object>();
        if (wfParamter.nodePro.auxiliaryInfo.Any(x => x.id == 3 && x.config.on == 1))
        {
            var auxiliaryItem = wfParamter.nodePro.auxiliaryInfo.Find(x => x.id == 3);
            if (auxiliaryItem != null)
            {
                auxiliaryItem.config.fileList = _repository.GetFileList(wfParamter.flowInfo.id, _userManager.UserId);
                wfParamter.nodePro.auxiliaryInfo = wfParamter.nodePro.auxiliaryInfo.Select(x => x.id == 3 ? auxiliaryItem : x).ToList();
            }
        }
        output.nodeProperties = wfParamter.nodePro;
        output.formData = revokeEntity.IsNullOrEmpty() ? await _runService.GetOrDelFlowFormData(wfParamter.node.formId, taskId, 0, wfParamter.flowInfo.flowId) : revokeEntity.FormData.ToObjectOld<Dictionary<string, object>>();
        return wfParamter;
    }

    /// <summary>
    /// 流程进度.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task<List<ProgressModel>> GetTaskProgress(WorkFlowParamter wfParamter, string opType)
    {
        var progressList = _repository.GetNodeRecord(wfParamter.taskEntity.Id).OrderBy(x => x.CreatorTime).Adapt<List<ProgressModel>>();
        if (progressList.Any())
        {
            foreach (var item in progressList)
            {
                var node = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == item.nodeCode);
                item.nodeType = node.nodeType;
                item.assigneeType = node.nodePro.assigneeType;
                item.showTaskFlow = _repository.GetTriggerTaskList(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeId == item.nodeId).Any();
                item.outSideStatus = true;
                if (node.nodeType == WorkFlowNodeTypeEnum.approver.ToString() || node.nodeType == WorkFlowNodeTypeEnum.processing.ToString())
                {
                    item.counterSign = node.nodePro.counterSign;
                    var userItemList = _repository.GetRecordItemList(x => x.NodeCode == item.nodeCode && x.NodeId == item.nodeId && x.TaskId == wfParamter.taskEntity.Id);
                    item.approverCount = userItemList.Count;
                    if (userItemList.Count == 1 && userItemList.FirstOrDefault().userId == "jnpf") // 自动审批
                    {
                        var userItem = new UserItem();
                        userItem.userId = "jnpf";
                        userItem.userName = "系统";
                        userItem.headIcon = "/api/File/Image/userAvatar/001.png";
                        userItem.handleType = userItemList.FirstOrDefault().handleType;
                        userItemList.Clear();
                        userItemList.Add(userItem);
                    }
                    item.approver = userItemList.Take(4).ToList();
                }
            }
            if (wfParamter.taskEntity.EndTime.IsNotEmptyOrNull())
            {
                var progressModel = wfParamter.nodeList.FirstOrDefault(x => x.nodeType == WorkFlowNodeTypeEnum.end.ToString()).Adapt<ProgressModel>();
                progressModel.startTime = wfParamter.taskEntity.EndTime;
                if (progressModel.nodeName.IsNullOrEmpty()) //兼容简流
                {
                    progressModel.nodeName = "流程结束";
                }
                progressList.Add(progressModel);
            }
            else
            {
                if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Runing.ParseToInt())
                {
                    var progressModelList = wfParamter.nodeList.FindAll(x => wfParamter.taskEntity.CurrentNodeCode.Contains(x.nodeCode)).Adapt<List<ProgressModel>>();
                    foreach (var item in progressModelList)
                    {
                        if (item.nodeType == WorkFlowNodeTypeEnum.approver.ToString() || item.nodeType == WorkFlowNodeTypeEnum.processing.ToString())
                        {
                            var node = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == item.nodeCode);
                            item.assigneeType = node.nodePro.assigneeType;
                            item.counterSign = node.nodePro.counterSign;
                            if (item.nodeType == WorkFlowNodeTypeEnum.approver.ToString())
                            {
                                item.nodeStatus = 4;
                            }
                            if (item.nodeType == WorkFlowNodeTypeEnum.processing.ToString())
                            {
                                item.nodeStatus = 8;
                            }
                            var operatorEntityList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == item.nodeCode && x.Status != -1 && x.Status != 7).OrderByDescending(x => x.CreatorTime).OrderBy(x => x.Completion).ToList();
                            operatorEntityList = operatorEntityList.FindAll(x => !(x.ParentId != "0" && x.HandleStatus == null && x.HandleTime == null && x.Completion == 1));//剔除未审批的加签数据
                            if (operatorEntityList.Any(x => x.Status == -2))
                            {
                                item.nodeStatus = 7;
                            }
                            if (operatorEntityList.Any())
                            {
                                item.nodeId = operatorEntityList.FirstOrDefault().NodeId;
                                item.startTime = operatorEntityList.FirstOrDefault().CreatorTime;
                                item.approverCount = operatorEntityList.FindAll(x => x.NodeId == item.nodeId).Count;
                                var userItemList = new List<UserItem>();
                                foreach (var operatorEntity in operatorEntityList.Where(x => x.NodeId == item.nodeId).Take(4))
                                {
                                    var userItem = new UserItem();
                                    var user = _usersService.GetInfoByUserId(operatorEntity.HandleId);
                                    if (user.IsNullOrEmpty()) continue;
                                    userItem.userId = user.Id;
                                    userItem.userName = user.RealName;
                                    userItem.headIcon = "/api/File/Image/userAvatar/" + user.HeadIcon;
                                    if (operatorEntity.HandleStatus.IsNotEmptyOrNull())
                                    {
                                        userItem.handleType = operatorEntity.HandleStatus;
                                    }
                                    else
                                    {
                                        var recordEntity = _repository.GetRecordList(x => x.OperatorId == operatorEntity.Id && x.HandleId == user.Id && x.NodeCode == item.nodeCode && x.NodeId == item.nodeId && x.TaskId == wfParamter.taskEntity.Id, o => o.HandleTime, OrderByType.Desc).FirstOrDefault();
                                        if (recordEntity.IsNotEmptyOrNull())
                                        {
                                            if (recordEntity.HandleType == WorkFlowRecordTypeEnum.AddSign.ParseToInt() && operatorEntity.HandleStatus == null && operatorEntity.HandleTime == null && operatorEntity.Completion == 0)
                                            {
                                                userItem.handleType = -1;
                                            }
                                            else
                                            {
                                                if (recordEntity.HandleType == WorkFlowRecordTypeEnum.SendBack.ParseToInt())
                                                    userItem.handleType = -1;
                                                else
                                                    userItem.handleType = recordEntity.HandleType;
                                            }
                                        }
                                        else
                                        {
                                            userItem.handleType = item.nodeStatus == 4 ? -1 : -2;
                                            if (item.nodeType == WorkFlowNodeTypeEnum.processing.ToString())
                                            {
                                                userItem.handleType = item.nodeStatus == 8 ? -3 : -4;
                                            }
                                        }
                                    }
                                    userItemList.Add(userItem);
                                }
                                item.approver = userItemList;
                            }
                            progressList.Add(item);
                        }
                        if (item.nodeType == WorkFlowNodeTypeEnum.outside.ToString())
                        {
                            item.nodeStatus = 4;
                            var eventLog = _repository.GetEventLog(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == item.nodeCode);
                            if (eventLog.IsNotEmptyOrNull())
                            {
                                item.nodeId = eventLog.Id;
                                item.startTime = eventLog.CreatorTime;
                                item.outSideStatus = eventLog.Status == 0;
                                if (!item.outSideStatus)
                                {
                                    item.errorTip = "接口请求失败或接口数据异常";
                                    item.errorData = eventLog.result;
                                    if (opType == "0")
                                    {
                                        item.isRetry = true;
                                    }
                                    if (opType == "4")
                                    {
                                        item.isRetry = eventLog.UpCode == wfParamter.operatorEntity.NodeCode;
                                    }
                                }
                            }
                            progressList.Add(item);
                        }
                    }
                }
            }
        }
        return progressList;
    }

    /// <summary>
    /// 创建撤销下一节点经办.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="revokeTaskId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private async Task CreateRevokeOperator(WorkFlowParamter wfParamter, string revokeTaskId, int type)
    {
        try
        {
            var variables = _repository.GetTaskLine(revokeTaskId);
            if (type == 0)
            {
                var instanceId = await BpmnEngineFactory.CreateBmpnEngine().InstanceStart(wfParamter.taskEntity.EngineId, variables);
                wfParamter.taskEntity.InstanceId = instanceId;
                var nodeRecord = new WorkFlowNodeRecordEntity
                {
                    TaskId = wfParamter.taskEntity.Id,
                    NodeCode = wfParamter.taskEntity.CurrentNodeCode,
                    NodeName = wfParamter.taskEntity.CurrentNodeName,
                    NodeStatus = 1
                };
                _repository.CreateNodeRecord(nodeRecord);
            }
            else
            {
                // 撤销申请任务不走触发节点
                await workFlowNodeUtil.SkipTriggerNode(wfParamter, variables);
                await BpmnEngineFactory.CreateBmpnEngine().ComplateNode(wfParamter.operatorEntity.NodeId, variables);
                if (wfParamter.node.nodeType == WorkFlowNodeTypeEnum.approver.ToString())
                {
                    var nodeRecord = new WorkFlowNodeRecordEntity
                    {
                        TaskId = wfParamter.taskEntity.Id,
                        NodeId = wfParamter.operatorEntity.NodeId,
                        NodeCode = wfParamter.operatorEntity.NodeCode,
                        NodeName = wfParamter.operatorEntity.NodeName,
                        NodeStatus = wfParamter.handleStatus == 1 ? 2 : 3
                    };
                    _repository.CreateNodeRecord(nodeRecord);
                }
            }
            var currentNodeCode = wfParamter.taskEntity.CurrentNodeCode;
            // 下个节点集合
            var nextNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetCurrentNodeList(wfParamter.taskEntity.InstanceId);
            if (nextNodeCodeList.Any())
            {
                var isSubNode = false;
                foreach (var item in nextNodeCodeList)
                {
                    if (!currentNodeCode.Contains(item.taskKey))
                    {
                        var nextNode = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == item.taskKey);
                        nextNode.nodeId = item.taskId;
                        if (WorkFlowNodeTypeEnum.approver.ToString().Equals(nextNode.nodeType))
                        {
                            // 节点最后一次审批是否是拒绝
                            var rejectRecord = _repository.GetNodeRecord(revokeTaskId).Where(x => x.NodeCode == nextNode.nodeCode).OrderByDescending(x => x.CreatorTime).FirstOrDefault();
                            if (rejectRecord.NodeStatus == 3 && wfParamter.globalPro.hasContinueAfterReject)
                            {
                                var operatorEntity = new WorkFlowOperatorEntity
                                {
                                    Id = SnowflakeIdHelper.NextId(),
                                    NodeCode = nextNode.nodeCode,
                                    NodeName = nextNode.nodePro.nodeName,
                                    NodeId = nextNode.nodeId,
                                    HandleId = "jnpf",
                                    EngineType = wfParamter.taskEntity.EngineType,
                                    TaskId = wfParamter.taskEntity.Id,
                                    Status = WorkFlowOperatorStatusEnum.Runing.ParseToInt(),
                                    SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now,
                                    StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now,
                                    CreatorTime = DateTime.Now,
                                    Completion = 0,
                                    HandleStatus = 1,
                                    IsProcessing = WorkFlowNodeTypeEnum.processing.ToString().Equals(nextNode.nodeType) ? 1 : 0,
                                };
                                wfParamter.nextOperatorEntityList.Add(operatorEntity);
                            }
                            else
                            {
                                var nextNodeOperatorList = _repository.GetOperatorList(x => x.TaskId == revokeTaskId && x.NodeCode == nextNode.nodeCode && x.HandleStatus == 1 && x.Completion == 1 && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                                foreach (var operatorItem in nextNodeOperatorList)
                                {
                                    operatorItem.Id = SnowflakeIdHelper.NextId();
                                    operatorItem.ParentId = "0";
                                    operatorItem.NodeId = nextNode.nodeId;
                                    operatorItem.TaskId = wfParamter.taskEntity.Id;
                                    operatorItem.Status = WorkFlowOperatorStatusEnum.Revoke.ParseToInt();
                                    operatorItem.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
                                    operatorItem.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
                                    operatorItem.EngineType = wfParamter.taskEntity.EngineType;
                                    operatorItem.CreatorTime = DateTime.Now;
                                    operatorItem.Completion = 0;
                                    operatorItem.IsProcessing = WorkFlowNodeTypeEnum.processing.ToString().Equals(nextNode.nodeType) ? 1 : 0;
                                    operatorItem.HandleTime = null;
                                    operatorItem.HandleAll = null;
                                    if (operatorItem.HandleId != "jnpf")
                                    {
                                        operatorItem.HandleStatus = null;
                                    }
                                    operatorItem.HandleParameter = null;
                                    operatorItem.Duedate = null;
                                    operatorItem.DraftData = null;
                                }
                                wfParamter.nextOperatorEntityList.AddRange(nextNodeOperatorList);
                            }
                        }
                        else
                        {
                            // 子流程节点
                            var subParamter = wfParamter.Copy();
                            subParamter.node = nextNode;
                            subParamter.nodePro = nextNode.nodePro;
                            subParamter.operatorEntity = new WorkFlowOperatorEntity();
                            subParamter.operatorEntity.Id = "0";
                            subParamter.operatorEntity.NodeCode = nextNode.nodeCode;
                            subParamter.operatorEntity.NodeId = nextNode.nodeId;
                            subParamter.taskEntity.CurrentNodeCode = string.Join(",", nextNodeCodeList.Select(x => x.taskKey));
                            subParamter.taskEntity.CurrentNodeName = string.Join(",", wfParamter.nodeList.FindAll(x => subParamter.taskEntity.CurrentNodeCode.Contains(x.nodeCode)).Select(x => x.nodePro.nodeName));
                            await CreateRevokeOperator(subParamter, revokeTaskId, 1);
                            wfParamter.taskEntity = subParamter.taskEntity;
                            isSubNode = true;
                        }
                    }
                }
                if (wfParamter.nextOperatorEntityList.Any())
                {
                    _repository.CreateOperator(wfParamter.nextOperatorEntityList);
                }
                if (!isSubNode)
                {
                    wfParamter.taskEntity.CurrentNodeCode = string.Join(",", nextNodeCodeList.Select(x => x.taskKey));
                    wfParamter.taskEntity.CurrentNodeName = string.Join(",", wfParamter.nodeList.FindAll(x => wfParamter.taskEntity.CurrentNodeCode.Contains(x.nodeCode)).Select(x => x.nodePro.nodeName));
                }
            }
            else
            {
                var instance = await BpmnEngineFactory.CreateBmpnEngine().InstanceInfo(wfParamter.taskEntity.InstanceId);
                if (instance.endTime.IsNotEmptyOrNull())
                {
                    wfParamter.taskEntity.CurrentNodeCode = "end";
                    wfParamter.taskEntity.CurrentNodeName = "结束";
                    wfParamter.taskEntity.Status = WorkFlowTaskStatusEnum.Pass.ParseToInt();
                    wfParamter.taskEntity.EndTime = DateTime.Now;
                }
            }
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }
    #endregion

    #region 子流程处理

    /// <summary>
    /// 创建子流程任务.
    /// </summary>
    /// <param name="childTaskPro">子流程节点属性.</param>
    /// <param name="formData">表单数据.</param>
    /// <param name="parentId">子任务父id.</param>
    /// <param name="childTaskCrUsers">子任务创建人.</param>
    private async Task CreateSubTask(WorkFlowNodeModel subNode, object formData, string parentId, string subParameter, List<string> subTaskCrUsers)
    {
        var subFlowInfo = _repository.GetTemplate(subNode.nodePro.flowId);
        var index = 0;
        foreach (var item in subTaskCrUsers)
        {
            var title = string.Format("{0}的{1}", _userManager.GetUserName(item, false), subFlowInfo.FullName);
            var flowTaskSubmitModel = new FlowTaskSubmitModel
            {
                flowId = subFlowInfo.FlowId,
                flowTitle = title,
                flowUrgent = 0,
                formData = formData,
                status = subNode.nodePro.autoSubmit ? 1 : 0,
                approvaUpType = 0,
                parentId = parentId,
                crUser = item,
                isAsync = subNode.nodePro.isAsync,
                autoSubmit = subNode.nodePro.autoSubmit,
                subParameter = subParameter,
                subCode = subNode.nodeCode,
                hasPermission = subNode.nodePro.hasPermission
            };
            if (subNode.nodePro.createRule == 1 && index > 0)
            {
                var entity = new WorkFlowSubTaskDataEntity
                {
                    Id = SnowflakeIdHelper.NextId(),
                    SubTaskJson = flowTaskSubmitModel.ToJsonString(),
                    ParentId = parentId,
                    NodeCode = subNode.nodeCode,
                    SortCode = index
                };
                _repository.CreateSubTaskData(entity);
            }
            if (subNode.nodePro.createRule == 0 || index == 0)
            {
                var wfParamter = new WorkFlowParamter();
                if (subNode.nodePro.autoSubmit)
                {
                    OperatorOutput result = await this.Submit(flowTaskSubmitModel);
                    if (result.errorCodeList.Any()) throw Oops.Oh(ErrorCode.WF0056);
                    wfParamter = result.wfParamter;
                }
                else
                {
                    wfParamter = await this.Save(flowTaskSubmitModel);
                    #region 子流程发起通知
                    var bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, new List<string> { item }, null, -1);
                    await workFlowMsgUtil.Alerts(subNode.nodePro.launchMsgConfig, new List<string> { item }, wfParamter, "MBXTLC011", bodyDic);
                    #endregion
                }
            }
            index++;
        }
    }

    /// <summary>
    /// 插入子流程.
    /// </summary>
    /// <param name="subTaskEntity">子流程.</param>
    /// <returns></returns>
    private async Task InsertSubTaskNextNode(WorkFlowTaskEntity subTaskEntity)
    {
        try
        {
            var subTaskData = _repository.GetSubTaskData(x => x.ParentId == subTaskEntity.ParentId && x.NodeCode == subTaskEntity.SubCode);
            var handleModel = new WorkFlowHandleModel();
            var wfParamter = _repository.GetWorkFlowParamterByTaskId(subTaskEntity.ParentId, handleModel); // 父流程
            if (wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.WF0040);
            if (subTaskData.IsNotEmptyOrNull())
            {
                var nodePro = wfParamter.nodeList.Find(x => x.nodeCode == subTaskEntity.SubCode).nodePro;
                await CreatQueueSubTask(subTaskData, nodePro);
            }
            else
            {
                if (subTaskEntity.IsAsync == 0)
                {
                    // 所有子流程(不包括当前流程)
                    var subTaskAll = _repository.GetTaskList(x => x.ParentId == subTaskEntity.ParentId && x.Id != subTaskEntity.Id && x.IsAsync == 0 && x.SubCode == subTaskEntity.SubCode);

                    if (!subTaskAll.Any(x => x.EndTime == null)) // 当前子流程节点任务是否完成
                    {
                        var subParameter = subTaskEntity.SubParameter.ToObject<Dictionary<string, string>>();
                        var nodeId = subParameter["nodeId"];
                        var formId = subParameter["formId"];
                        wfParamter.formData = await _runService.GetOrDelFlowFormData(formId, subTaskEntity.ParentId, 0, wfParamter.taskEntity.FlowId, true);
                        wfParamter.node = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == subTaskEntity.SubCode);
                        var subOperator = new WorkFlowOperatorEntity();
                        subOperator.Id = "0";
                        subOperator.NodeCode = subTaskEntity.SubCode;
                        subOperator.NodeName = wfParamter.node.nodeName;
                        subOperator.NodeId = nodeId;
                        wfParamter.operatorEntity = subOperator;
                        wfParamter.operatorEntityList = new List<WorkFlowOperatorEntity> { subOperator };
                        wfParamter.handleStatus = subTaskEntity.Status == WorkFlowTaskStatusEnum.Reject.ParseToInt() ? 0 : 1;
                        await this.Audit(wfParamter);
                    }
                }
            }
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 创建依次子流程.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="subNodePro"></param>
    /// <returns></returns>
    private async Task CreatQueueSubTask(WorkFlowSubTaskDataEntity entity, NodeProperties subNodePro)
    {
        try
        {
            var flowTaskSubmitModel = entity.SubTaskJson.ToObject<FlowTaskSubmitModel>();
            var wfParamter = new WorkFlowParamter();
            _repository.DeleteSubTaskData(entity.Id);
            if (subNodePro.autoSubmit)
            {
                OperatorOutput result = await this.Submit(flowTaskSubmitModel);
                if (result.errorCodeList.Any()) throw Oops.Oh(ErrorCode.WF0056);
                wfParamter = result.wfParamter;
            }
            else
            {
                wfParamter = await this.Save(flowTaskSubmitModel);
                #region 子流程发起通知
                var bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, new List<string> { flowTaskSubmitModel.crUser }, null, -1);
                await workFlowMsgUtil.Alerts(subNodePro.launchMsgConfig, new List<string> { flowTaskSubmitModel.crUser }, wfParamter, "MBXTLC011", bodyDic);
                #endregion
            }
        }
        catch (Exception)
        {
            var subTaskData = _repository.GetSubTaskData(x => x.ParentId == entity.ParentId && x.NodeCode == entity.NodeCode);
            if (subTaskData.IsNotEmptyOrNull())
            {
                await CreatQueueSubTask(subTaskData, subNodePro);
            }
        }
    }
    #endregion

    #region 超时处理

    /// <summary>
    /// 超时/提醒.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="nodeId"></param>
    /// <param name="flowTaskOperatorEntities"></param>
    /// <returns></returns>
    private async Task TimeoutOrRemind(WorkFlowParamter wfParamter, string nodeCode, List<WorkFlowOperatorEntity> operatorEntities)
    {
        var isTest = !App.Configuration["WorkFlow:overTime"].ParseToBool(); // 是否测试环境.
        var node = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == nodeCode);
        wfParamter.formData = await _runService.GetOrDelFlowFormData(node.formId, wfParamter.taskEntity.Id, 0, wfParamter.taskEntity.FlowId, true);
        if (node.nodePro.timeLimitConfig.on > 0)
        {
            var nowTime = DateTime.Now;
            // 起始时间
            var startingTime = workFlowOtherUtil.GetStartingTime(node.nodePro.timeLimitConfig, nowTime, (DateTime)operatorEntities.FirstOrDefault()?.CreatorTime, (DateTime)wfParamter.taskEntity.CreatorTime, wfParamter.formData.ToJsonStringOld());
            Console.WriteLine(string.Format("节点{0}起始时间：{1}", nodeCode, startingTime.ToString("yyyy-MM-dd HH:mm:ss")));
            // 创建限时提醒
            if (node.nodePro.noticeConfig.on > 0 && startingTime != nowTime)
            {
                var startTime = isTest ? startingTime.AddMinutes(node.nodePro.noticeConfig.firstOver) : startingTime.AddHours(node.nodePro.noticeConfig.firstOver); // 提醒开始时间
                var endTime = isTest ? startingTime.AddMinutes(node.nodePro.timeLimitConfig.duringDeal).AddSeconds(20) : startingTime.AddHours(node.nodePro.timeLimitConfig.duringDeal).AddSeconds(20); // 提醒结束时间
                node.nodePro.noticeMsgConfig = node.nodePro.noticeMsgConfig.on == 2 ? wfParamter.startPro.noticeMsgConfig : node.nodePro.noticeMsgConfig;

                if (startTime <= endTime && DateTime.Now <= endTime)
                {
                    var interval = 1; // 第一次执行间隔
                    var runCount = 0; // 已执行次数
                    var isAtOnce = false; // 是否立即执行事件和自动审批
                    if (DateTime.Now <= startTime) // 当前时间小于开始时间
                    {
                        interval = (startTime - DateTime.Now).TotalMilliseconds.ParseToInt();
                    }
                    else if (startTime < DateTime.Now && DateTime.Now < endTime && node.nodePro.noticeConfig.overTimeDuring > 0) // 当前时间处于开始时间与结束时间区间内
                    {
                        var duration = isTest ? (DateTime.Now - startTime).TotalMinutes.ParseToInt() : (DateTime.Now - startTime).TotalHours.ParseToInt();
                        runCount = duration / node.nodePro.noticeConfig.overTimeDuring;
                        isAtOnce = true;
                    }
                    await _taskQueue.EnqueueAsync(
                        async (_, _) => { await OnceNotifyEvent(node.nodePro, wfParamter, nodeCode, runCount + 1, false, isAtOnce); }, interval);
                    if (node.nodePro.noticeConfig.overTimeDuring > 0 && startTime < endTime)
                    {
                        await MsgOrRequest(node.nodePro, wfParamter, startTime, endTime, nodeCode, runCount + 1,isTest);
                    }
                }
            }

            // 创建超时提醒
            if (node.nodePro.overTimeConfig.on > 0 && startingTime != nowTime)
            {
                var startTime = isTest ? startingTime.AddMinutes(node.nodePro.timeLimitConfig.duringDeal + node.nodePro.overTimeConfig.firstOver) : startingTime.AddHours(node.nodePro.timeLimitConfig.duringDeal + node.nodePro.overTimeConfig.firstOver); // 超时开始时间
                var interval = 1; // 第一次执行间隔
                var runCount = 0; // 已执行次数
                var isAtOnce = false; // 是否立即执行事件和自动审批
                if (DateTime.Now <= startTime)
                {
                    interval = (startTime - DateTime.Now).TotalMilliseconds.ParseToInt();
                }
                else
                {
                    if (node.nodePro.overTimeConfig.overTimeDuring > 0)
                    {
                        var duration = isTest ? (DateTime.Now - startTime).TotalMinutes.ParseToInt() : (DateTime.Now - startTime).TotalHours.ParseToInt();
                        runCount = duration / node.nodePro.overTimeConfig.overTimeDuring;
                    }
                    isAtOnce = true;
                }
                await _taskQueue.EnqueueAsync(async (_, _) => { await OnceNotifyEvent(node.nodePro, wfParamter, nodeCode, runCount + 1, true, isAtOnce); }, interval);
                if (node.nodePro.overTimeConfig.overTimeDuring > 0)
                {
                    await MsgOrRequest(node.nodePro, wfParamter, startTime, null, nodeCode, runCount + 1, isTest);
                }
            }
        }
    }

    /// <summary>
    /// 定时任务执行超时提醒.
    /// </summary>
    /// <param name="approPro"></param>
    /// <param name="wfParamter"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <param name="nodeId"></param>
    /// <param name="runCount"></param>
    /// <returns></returns>
    private async Task MsgOrRequest(NodeProperties nodePro, WorkFlowParamter wfParamter, DateTime? startTime, DateTime? endTime, string nodeCode, int runCount = 1, bool isDev = false)
    {
        // 提醒(false)/超时(true)
        var isTimeOut = endTime.IsNullOrEmpty();
        var workerName = isTimeOut ? string.Format("CS_{0}_{1}", nodeCode, wfParamter.taskEntity.Id) : string.Format("TX_{0}_{1}", nodeCode, wfParamter.taskEntity.Id);
        var createType = RequestTypeEnum.Http;

        TriggerBuilder? triggerBuilder = _jobManager.CreateTriggerBuilder(new JobTriggerModel
        {
            interval = isTimeOut ? nodePro.overTimeConfig.overTimeDuring : nodePro.noticeConfig.overTimeDuring,
            intervalType = isDev ? 2 : 3,
            TriggreId = string.Format("Trigger_{0}", workerName),
            Description = string.Format("节点:{0}调度触发器", workerName),
            StartTime = startTime,
            EndTime = endTime,
            RunOnStart = true,
        });
        triggerBuilder.SetResult(workerName + "任务创建");
        var scheduleTaskModel = new ScheduleTaskModel();
        scheduleTaskModel.taskParams.Add("nodePro", nodePro);
        scheduleTaskModel.taskParams.Add("wfParamter", wfParamter);
        scheduleTaskModel.taskParams.Add("nodeCode", nodeCode);
        scheduleTaskModel.taskParams.Add("count", runCount);
        scheduleTaskModel.taskParams.Add("isTimeOut", isTimeOut);
        scheduleTaskModel.taskParams.Add("isAtOnce", false);

        var jobDetail = _jobManager.ObtainJobHttpDetails(new JobDetailModel
        {
            JobId = string.Format("Job_{0}", workerName),
            Description = string.Format("租户`{0}`下名称为`{1}`的HTTP系统调度", _userManager.TenantId, workerName),
            GroupName = "FlowTask",
            RequestUri = "/ScheduleTask/flowtask",
            HttpMethod = HttpMethod.Post,
            Body = scheduleTaskModel.ToJsonString(),
            TaskId = string.Format("{0}_{1}", nodeCode, wfParamter.taskEntity.Id),
            TenantId = _userManager.TenantId,
            UserId = _userManager.UserId,
        });
        var jobType = typeof(JNPFHttpJob);

        _schedulerFactory.AddJob(
                JobBuilder.Create(jobType).LoadFrom(jobDetail)
                .SetJobType(jobType),
                triggerBuilder);

        // 延迟一下等待持久化写入，再执行其他字段的更新
        await Task.Delay(500);

        var job = _db.GetConnection("JNPF-Job");

        await job.Updateable<JobDetails>().SetColumns(u => new JobDetails { CreateType = createType })
                     .Where(u => u.JobId.Equals(jobDetail.JobId)).ExecuteCommandAsync();
    }

    /// <summary>
    /// 执行超时提醒配置.
    /// </summary>
    /// <param name="approPro"></param>
    /// <param name="wfParamter"></param>
    /// <param name="nodeCode"></param>
    /// <param name="count"></param>
    /// <param name="isTimeOut"></param>
    /// <param name="isAtOnce"></param>
    /// <returns></returns>
    public async Task NotifyEvent(NodeProperties nodePro, WorkFlowParamter wfParamter, string nodeCode, int count, bool isTimeOut, bool isAtOnce = false)
    {
        var taskEntity = _repository.GetTaskInfo(wfParamter.taskEntity.Id);
        var flowInfo = _repository.GetFlowInfo(wfParamter.taskEntity.FlowId);
        var StatusList = new List<int>
        {
            WorkFlowTaskStatusEnum.Runing.ParseToInt(),
            WorkFlowTaskStatusEnum.Revokeing.ParseToInt(),
        };
        if (taskEntity.IsNotEmptyOrNull() && StatusList.Contains(taskEntity.Status) && flowInfo.IsNotEmptyOrNull() && flowInfo.status == 1)
        {
            var workerName = isTimeOut ? string.Format("CS_{0}_{1}", nodeCode, wfParamter.taskEntity.Id) : string.Format("TX_{0}_{1}", nodeCode, wfParamter.taskEntity.Id);
            if (_schedulerFactory.ContainsJob("Job_" + workerName))
            {
                var job = _schedulerFactory.GetJob("Job_" + workerName);
                count = job.GetTriggerBuilder("Trigger_" + workerName).NumberOfRuns.ParseToInt() + count;
            }
            var msgEncode = isTimeOut ? "MBXTLC009" : "MBXTLC008";
            var msgReMark = isTimeOut ? "超时" : "提醒";
            var msgConfig = isTimeOut ? nodePro.overTimeMsgConfig : nodePro.noticeMsgConfig;
            var timeOutConfig = isTimeOut ? nodePro.overTimeConfig : nodePro.noticeConfig;

            var operatorList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == nodeCode && x.Completion == 0 && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
            // 通知
            if (timeOutConfig.overNotice)
            {
                if (operatorList.Any())
                {
                    var userList = operatorList.Select(x => x.HandleId).ToList();
                    var remark = string.Format("现在时间：{3},节点{0}：第{1}次{2}通知", workerName, count, msgReMark, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    Console.WriteLine(remark);
                    var bodyDic = workFlowMsgUtil.GetMesBodyText(wfParamter, userList, operatorList, 2, remark);
                    if (msgConfig.on > 0)
                    {
                        await workFlowMsgUtil.Alerts(msgConfig, userList, wfParamter, msgEncode, bodyDic);
                        if (isTimeOut)
                        {
                            var ids = operatorList.Select(x => x.Id).ToList();
                            if (ids.Any())
                            {
                                _repository.UpdateOperator(x => x.Duedate == SqlFunc.GetDate(), y => ids.Contains(y.Id));
                            }
                        }
                    }
                }
            }

            // 自动审批
            var autoFlag = isAtOnce ? count >= timeOutConfig.overAutoApproveTime : timeOutConfig.overAutoApproveTime == count;
            if (isTimeOut && autoFlag && timeOutConfig.overAutoApprove)
            {
                Console.WriteLine("开始自动审批，现在时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                await AutoAudit(wfParamter, true);
            }
            else
            {
                autoFlag = isAtOnce ? count >= timeOutConfig.overAutoTransferTime : timeOutConfig.overAutoTransferTime == count;
                if (isTimeOut && autoFlag && timeOutConfig.overAutoTransfer)
                {
                    if (timeOutConfig.overTimeType == 11)
                    {
                        foreach (var item in operatorList)
                        {
                            var userId = await workFlowUserUtil.GetOverTimeUserId(timeOutConfig, wfParamter, item.HandleId);
                            if (userId.IsNotEmptyOrNull() && userId != item.HandleId)
                            {
                                var transferWfParamter = _repository.GetWorkFlowParamterByOperatorId(item.Id, null);
                                transferWfParamter.handleIds = userId;
                                transferWfParamter.handleOpinion = "系统转审";
                                transferWfParamter.isAuto = true;
                                await Transfer(transferWfParamter);
                            }
                        }
                    }
                    else
                    {
                        var userId = await workFlowUserUtil.GetOverTimeUserId(timeOutConfig, wfParamter, null);
                        if (userId.IsNotEmptyOrNull() && !(operatorList.Any(x => x.HandleId == userId) && operatorList.Count == 1))
                        {
                            var zpWfParamter = wfParamter.Copy();
                            zpWfParamter.handleOpinion = "系统转审";
                            zpWfParamter.handleIds = userId;
                            zpWfParamter.nodeCode = nodeCode;
                            zpWfParamter.isAuto = true;
                            await Assigned(zpWfParamter, true);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 首次超时提醒.
    /// </summary>
    /// <param name="nodePro"></param>
    /// <param name="wfParamter"></param>
    /// <param name="nodeId"></param>
    /// <param name="count"></param>
    /// <param name="isTimeOut"></param>
    /// <param name="isAtOnce"></param>
    /// <returns></returns>
    public async Task OnceNotifyEvent(NodeProperties nodePro, WorkFlowParamter wfParamter, string nodeCode, int count, bool isTimeOut, bool isAtOnce = false)
    {
        var userEntity = _userManager.User;
        var tenantId = _userManager.TenantId;
        using var scope = _serviceScopeFactory.CreateScope();
        var server = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        var localAddress = addresses.FirstOrDefault().Replace("[::]", "localhost");
        var token = NetHelper.GetToken(userEntity.Id, userEntity.Account, userEntity.RealName, userEntity.IsAdministrator, tenantId);
        var heraderDic = new Dictionary<string, object>();
        heraderDic.Add("Authorization", token);
        var scheduleTaskModel = new ScheduleTaskModel();
        scheduleTaskModel.taskParams.Add("nodePro", nodePro);
        scheduleTaskModel.taskParams.Add("wfParamter", wfParamter);
        scheduleTaskModel.taskParams.Add("nodeCode", nodeCode);
        scheduleTaskModel.taskParams.Add("count", count);
        scheduleTaskModel.taskParams.Add("isTimeOut", isTimeOut);
        scheduleTaskModel.taskParams.Add("isAtOnce", isAtOnce);
        var path = string.Format("{0}/ScheduleTask/flowtask", localAddress);
        var result = await path.SetHeaders(heraderDic).SetBody(scheduleTaskModel).PostAsStringAsync();
    }

    /// <summary>
    /// 定时触发任务.
    /// </summary>
    /// <param name="triggerProperties"></param>
    /// <param name="flowId"></param>
    /// <returns></returns>
    public async Task TimeTriggerTask(TriggerProperties triggerProperties, string flowId)
    {
        var createType = RequestTypeEnum.Http;
        var job = _db.GetConnection("JNPF-Job");
        TriggerBuilder triggerBuilder = null;
        _schedulerFactory.RemoveJob(string.Format("JobTaskFlow_Http_{0}_{1}", _userManager.TenantId, flowId));
        if (triggerProperties.endTimeType == 1)
        {
            triggerBuilder = _jobManager.ObtainTriggerBuilder(new JobTriggerModel
            {
                TriggreId = string.Format("{0}_trigger_taskflow_{1}", _userManager.TenantId, flowId),
                Description = string.Format("{0}任务定时触发器", triggerProperties.nodeName),
                StartTime = triggerProperties.startTime,
            });
        }
        else
        {
            triggerBuilder = _jobManager.ObtainTriggerBuilder(new JobTriggerModel
            {
                TriggreId = string.Format("{0}_trigger_taskflow_{1}", _userManager.TenantId, flowId),
                Description = string.Format("{0}任务定时触发器", triggerProperties.nodeName),
                StartTime = triggerProperties.startTime,
                EndTime = triggerProperties.endTime
            });
        }

        switch (triggerProperties.cron.Split(" ").Length == 7)
        {
            case true:
                triggerBuilder.AlterToCron(triggerProperties.cron, CronStringFormat.WithSecondsAndYears);
                break;
            default:
                triggerBuilder.AlterToCron(triggerProperties.cron, CronStringFormat.WithSeconds);
                break;
        }

        triggerBuilder.SetResult("触发任务创建");

        var jobDetail = new JobDetails();
        var scheduleTaskModel = new ScheduleTaskModel();
        scheduleTaskModel.taskParams.Add("tenantId", _userManager.TenantId);
        scheduleTaskModel.taskParams.Add("userId", _userManager.UserId);
        scheduleTaskModel.taskParams.Add("modelId", flowId);
        jobDetail = _jobManager.ObtainJobHttpDetails(new JobDetailModel
        {
            JobId = string.Format("JobTaskFlow_Http_{0}_{1}", _userManager.TenantId, flowId),
            Description = string.Format("租户`{0}`下名称为`{1}_{2}`的HTTP任务定时", _userManager.TenantId, flowId, triggerProperties.nodeName),
            GroupName = "FlowTask",
            RequestUri = "/ScheduleTask/taskflow",
            HttpMethod = HttpMethod.Post,
            Body = scheduleTaskModel.ToJsonString(),
            TaskId = flowId,
            TenantId = _userManager.TenantId,
            UserId = _userManager.UserId,
        });
        Type jobType = typeof(JNPFHttpJob);

        _schedulerFactory.AddJob(
            JobBuilder.Create(jobType).LoadFrom(jobDetail)
            .SetJobType(jobType),
            triggerBuilder);

        // 延迟一下等待持久化写入，再执行其他字段的更新
        await Task.Delay(500);

        await job.Updateable<JobDetails>().SetColumns(u => new JobDetails { CreateType = createType })
              .Where(u => u.JobId.Equals(jobDetail.JobId)).ExecuteCommandAsync();
    }
    #endregion
}