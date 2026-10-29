using Dm.util;
using JNPF.ClayObject;
using JNPF.ClayObject.Extensions;
using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.EventBus.Sources;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.FriendlyException;
using JNPF.JsonSerialization;
using JNPF.RemoteRequest.Extensions;
using JNPF.Schedule;
using JNPF.Systems.Entitys.Dto.Schedule;
using JNPF.Systems.Entitys.Permission;
using JNPF.UnifyResult;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.WorkFlow.Entitys.Dto.Operator;
using JNPF.WorkFlow.Entitys.Dto.TriggerTask;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using JNPF.WorkFlow.Factory;
using Mapster;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NPOI.Util;
using SqlSugar;

namespace JNPF.WorkFlow.TaskFlow;

/// <summary>
/// 任务流程事件订阅.
/// </summary>
public class TaskFlowEventSubscriber : IEventSubscriber, ISingleton, IDisposable
{
    /// <summary>
    /// 初始化客户端.
    /// </summary>
    private readonly ISqlSugarClient _sqlSugarClient;

    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceScope _serviceScope;

    /// <summary>
    /// 作业计划工厂服务.
    /// </summary>
    private readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// 调度管理.
    /// </summary>
    private readonly IJobManager _jobManager;

    private readonly IUserManager _userManager;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    private readonly ICacheManager _cacheManager;

    public TaskFlowEventSubscriber(
        IServiceScopeFactory serviceScopeFactory,
        ISqlSugarClient sqlSugarClient,
        IJobManager jobManager,
        IUserManager userManager,
        ITenantManager tenantManager,
        ICacheManager cacheManager,
        ISchedulerFactory schedulerFactory)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _sqlSugarClient = sqlSugarClient;
        _jobManager = jobManager;
        _userManager = userManager;
        _schedulerFactory = schedulerFactory;
        _cacheManager = cacheManager;
        _tenantManager = tenantManager;
    }

    #region 非标准

    /// <summary>
    /// 创建任务流程触发(非标准).
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [EventSubscribe("TaskFlow:CreateTask")]
    public async Task CreateTask(EventHandlerExecutingContext context)
    {
        try
        {
            var eventSource = (TaskFlowEventSource)context.Source;
            var model = eventSource.Model;
            if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(model.TenantId) && !_sqlSugarClient.AsTenant().IsAnyConnection(model.TenantId))
            {
                await _tenantManager.ChangTenant(_sqlSugarClient, model.TenantId);
            }

            var nodeList = new List<TriggerNodeModel>();
            if (model.isRetry)
            {
                var triggerTask = await _sqlSugarClient.CopyNew().Queryable<WorkFlowTriggerTaskEntity>().FirstAsync(x => x.Id == model.ModelId && x.DeleteMark == null);
                if (triggerTask.IsNotEmptyOrNull())
                {
                    model.taskFlowData = triggerTask.Data.ToObject<List<Dictionary<string, object>>>();
                    nodeList = await _sqlSugarClient.CopyNew().Queryable<WorkFlowNodeEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.FlowId == b.FlowId))
                    .Where((a, b) => a.FlowId == triggerTask.FlowId && a.NodeType.Contains("Trigger") && a.DeleteMark == null && b.Status == 1 && b.Type == 2 && b.DeleteMark == null)
                    .Select((a, b) => new TriggerNodeModel
                    {
                        flowName = b.FullName,
                        nodeJson = a.NodeJson,
                        flowId = a.FlowId,
                        engineId = b.FlowableId,
                    }).ToListAsync();
                }
                model.parentTime = triggerTask.StartTime;
            }
            else
            {
                nodeList = await _sqlSugarClient.CopyNew().Queryable<WorkFlowNodeEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.FlowId == b.FlowId))
                .Where((a, b) => a.FormId == model.ModelId && a.NodeType == model.TriggerType && a.DeleteMark == null && b.EnabledMark == 1 && b.Status == 1 && b.Type == 2 && b.DeleteMark == null)
                .Select((a, b) => new TriggerNodeModel
                {
                    id = a.Id,
                    flowName = b.FullName,
                    nodeJson = a.NodeJson,
                    flowId = a.FlowId,
                    engineId = b.FlowableId,
                }).ToListAsync();
            }

            if (nodeList.Any())
            {

                foreach (var item in nodeList)
                {
                    var triggerPro = item.nodeJson.ToObject<TriggerProperties>();
                    if (item.engineId.IsNullOrEmpty()) continue;
                    var triggerTask = await CreatTriggerTask(triggerPro, model, item);
                    if (triggerTask != null)
                    {
                        await TriggerTaskRun(triggerTask, model, new List<string>());
                    }
                }
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 创建触发任务.
    /// </summary>
    /// <param name="triggerPro"></param>
    /// <param name="model"></param>
    /// <param name="nodeModel"></param>
    /// <returns></returns>
    public async Task<WorkFlowTriggerTaskEntity> CreatTriggerTask(TriggerProperties triggerPro, TaskFlowEventModel model, TriggerNodeModel nodeModel)
    {
        WorkFlowTriggerTaskEntity triggerTask = new WorkFlowTriggerTaskEntity();
        WorkFlowTriggerRecordEntity triggerRecord = new WorkFlowTriggerRecordEntity();
        triggerRecord.StartTime = DateTime.Now;
        var isCreat = false;
        switch (triggerPro.type)
        {
            case "eventTrigger":
                if (triggerPro.triggerFormEvent != model.ActionType && !model.isRetry) break;
                if (triggerPro.triggerFormEvent == 3 && model.ActionType == 3)
                {
                    if (model.delNodeIdList.IsNotEmptyOrNull() && model.delNodeIdList.Contains(nodeModel.id))
                    {
                        isCreat = true;
                        model.dataSource[triggerPro.nodeId] = model.taskFlowData;
                    }
                }
                else
                {
                    string superQuery = new { matchLogic = triggerPro.ruleMatchLogic, conditionList = triggerPro.ruleList }.ToJsonString();
                    var parameter = new { currentPage = 1, modelId = triggerPro.formId, pageSize = 9999, superQueryJson = superQuery, isOnlyId = 0, flowIds = "jnpf" }.ToJsonString();
                    var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", triggerPro.formId);
                    var result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                    if (result.code == 200 && result.data.IsNotEmptyOrNull())
                    {
                        var resultData = Clay.Parse(result.data.ToString());
                        string list = resultData.list.ToString();
                        List<Dictionary<string, object>> dataList = list.ToObject<List<Dictionary<string, object>>>();
                        var ids = model.taskFlowData.Where(x => x.ContainsKey("id")).Select(x => x["id"].ToString()).ToList();
                        var idList = dataList.Where(x => x.ContainsKey("id")).Select(x => x["id"].ToString()).ToList();
                        isCreat = idList.Intersect(ids).ToList().Any();
                        if (triggerPro.triggerFormEvent == 2 && model.ActionType == 2 && isCreat)
                        {
                            if (triggerPro.updateFieldList.Any())
                            {
                                isCreat = triggerPro.updateFieldList.Intersect(model.upDateFieldList).ToList().Any();
                            }
                        }
                        model.dataSource[triggerPro.nodeId] = dataList.FindAll(x => x.ContainsKey("id") && ids.Contains(x["id"]));
                    }
                }
                break;
            case "timeTrigger":
                var workName = string.Format("JobTaskFlow_Http_{0}_{1}", model.TenantId, model.ModelId);
                if (_schedulerFactory.ContainsJob(workName))
                {
                    var job = _schedulerFactory.GetJob(workName);
                    var triggerId = string.Format("{0}_trigger_taskflow_{1}", model.TenantId, model.ModelId);
                    var triggerBuilder = job.GetTriggerBuilder(triggerId);
                    if (triggerPro.endTimeType == 1 && triggerBuilder.NumberOfRuns > triggerPro.endLimit)
                    {
                        _schedulerFactory.RemoveJob(workName);
                    }
                    else
                    {
                        isCreat = true;
                        model.dataSource[triggerPro.nodeId] = model.taskFlowData;
                    }
                }
                break;

            default:
                isCreat = true;
                model.dataSource[triggerPro.nodeId] = model.taskFlowData;
                break;
        }
        if (isCreat)
        {
            var stratNode = await _sqlSugarClient.CopyNew().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == nodeModel.flowId && x.NodeType == "start").FirstAsync();
            triggerTask.Id = SnowflakeIdHelper.NextId();
            triggerTask.ParentId = model.isRetry ? model.ModelId : "0";
            triggerTask.ParentTime = model.isRetry ? model.parentTime : null;
            triggerTask.FullName = nodeModel.flowName;
            triggerTask.StartTime = DateTime.Now;
            triggerTask.FlowId = nodeModel.flowId;
            triggerTask.Data = model.taskFlowData.ToJsonString();
            triggerTask.EngineType = 1;
            triggerTask.Status = WorkFlowTaskStatusEnum.Runing.ParseToInt();
            triggerTask.CreatorUserId = model.UserId;
            triggerTask.CreatorTime = DateTime.Now;
            var conditionCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetLineCode(nodeModel.engineId, stratNode.NodeCode, string.Empty);
            var variables = conditionCodeList.ToDictionary(x => x, y => true);
            var instanceId = await BpmnEngineFactory.CreateBmpnEngine().InstanceStart(nodeModel.engineId, variables);
            triggerTask.InstanceId = instanceId;
            await _sqlSugarClient.CopyNew().Insertable(triggerTask).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            triggerRecord.Id = SnowflakeIdHelper.NextId();
            triggerRecord.TriggerId = triggerTask.Id;
            triggerRecord.NodeName = stratNode.NodeJson.ToObject<NodeProperties>().nodeName;
            triggerRecord.EndTime = DateTime.Now;
            triggerRecord.Status = 0;
            triggerRecord.CreatorUserId = model.UserId;
            triggerRecord.CreatorTime = DateTime.Now;
            await _sqlSugarClient.CopyNew().Insertable(triggerRecord).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            await SendGlobalMsg(triggerTask.FlowId, model, triggerTask.FullName);
        }
        else
        {
            return null;
        }
        return triggerTask;
    }

    /// <summary>
    /// 执行触发.
    /// </summary>
    /// <param name="triggerTaskEntity"></param>
    /// <param name="model"></param>
    /// <param name="errorNodeCodeList"></param>
    /// <returns></returns>
    public async Task TriggerTaskRun(WorkFlowTriggerTaskEntity triggerTaskEntity, TaskFlowEventModel model, List<string> errorNodeCodeList)
    {
        var nextNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetCurrentNodeList(triggerTaskEntity.InstanceId);
        if (errorNodeCodeList != null && errorNodeCodeList.Any())
        {
            nextNodeCodeList = nextNodeCodeList.FindAll(x => !errorNodeCodeList.Contains(x.taskKey));
        }
        if (nextNodeCodeList.Any())
        {
            foreach (var item in nextNodeCodeList)
            {
                var nextNode = await _sqlSugarClient.CopyNew().Queryable<WorkFlowNodeEntity>().FirstAsync(x => x.NodeCode == item.taskKey && x.FlowId == triggerTaskEntity.FlowId);
                var nextNodePro = nextNode.NodeJson.ToObject<EventProperties>();
                WorkFlowTriggerRecordEntity triggerRecord = new WorkFlowTriggerRecordEntity();
                triggerRecord.StartTime = DateTime.Now;
                triggerRecord.Id = SnowflakeIdHelper.NextId();
                triggerRecord.TriggerId = triggerTaskEntity.Id;
                triggerRecord.NodeId = item.taskId;
                triggerRecord.NodeCode = nextNodePro.nodeId;
                triggerRecord.NodeName = nextNodePro.nodeName;
                triggerRecord.CreatorUserId = model.UserId;
                triggerRecord.CreatorTime = DateTime.Now;
                triggerRecord.Status = null;
                await _sqlSugarClient.CopyNew().Insertable(triggerRecord).ExecuteCommandAsync();
                var output = await TriggerNodeExecute(nextNodePro, model); // 节点是否完成.
                if (output.isComplete)
                {
                    var conditionCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetLineCode(string.Empty, item.taskKey, item.taskId);
                    var variables = conditionCodeList.ToDictionary(x => x, y => true);
                    await BpmnEngineFactory.CreateBmpnEngine().ComplateNode(item.taskId, variables);
                    triggerRecord.Status = 0;
                    triggerRecord.EndTime = DateTime.Now;
                    await _sqlSugarClient.CopyNew().Updateable(triggerRecord).ExecuteCommandAsync();
                }
                else
                {
                    errorNodeCodeList.Add(item.taskKey);
                    triggerRecord.Status = 1;
                    triggerRecord.EndTime = DateTime.Now;
                    triggerRecord.ErrorTip = output.result.IsNotEmptyOrNull() && output.result.msg.IsNotEmptyOrNull() ? output.result.msg.ToString() : "执行异常";
                    triggerRecord.ErrorData = output.result.msg.ToString();
                    await _sqlSugarClient.CopyNew().Updateable(triggerRecord).ExecuteCommandAsync();
                    await SendGlobalMsg(triggerTaskEntity.FlowId, model, triggerTaskEntity.FullName, true);
                }
            }
            var cacheKey = string.Format("{0}:{1}", "TriggerTask", model.TenantId);
            List<string> caCheList = await _cacheManager.GetAsync<List<string>>(cacheKey);
            caCheList ??= new List<string>();
            if (!caCheList.Contains(triggerTaskEntity.Id))
            {
                await TriggerTaskRun(triggerTaskEntity, model, errorNodeCodeList);
            }
        }
        else
        {
            var instance = await BpmnEngineFactory.CreateBmpnEngine().InstanceInfo(triggerTaskEntity.InstanceId);
            if (instance.endTime.IsNotEmptyOrNull())
            {
                triggerTaskEntity.Status = WorkFlowTaskStatusEnum.Pass.ParseToInt();
                await _sqlSugarClient.CopyNew().Updateable(triggerTaskEntity).ExecuteCommandAsync();
            }
            else
            {
                if (errorNodeCodeList != null && errorNodeCodeList.Any())
                {
                    triggerTaskEntity.Status = WorkFlowTaskStatusEnum.Error.ParseToInt(); ;
                    await _sqlSugarClient.CopyNew().Updateable(triggerTaskEntity).ExecuteCommandAsync();
                }
            }
        }
    }
    #endregion

    #region 标准

    /// <summary>
    /// 创建任务流程触发(标准).
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [EventSubscribe("TaskFlow:CreateTaskFlow")]
    public async Task CreateTaskFlow(EventHandlerExecutingContext context)
    {
        var eventSource = (TaskFlowEventSource)context.Source;
        var model = eventSource.Model;
        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(model.TenantId) && !_sqlSugarClient.AsTenant().IsAnyConnection(model.TenantId))
        {
            await _tenantManager.ChangTenant(_sqlSugarClient, model.TenantId);
        }
        var wfParamter = model.ParamterJson.ToObject<WorkFlowParamter>();

        if (!wfParamter.triggerNodeList.Any())
        {
            var conditionCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetLineCode(wfParamter.engineId, wfParamter.node.nodeCode, wfParamter.taskEntity?.Id);
            var variables = conditionCodeList.ToDictionary(x => x, y => true);
            await UpdateEndLine(variables, model, wfParamter);
            await BpmnEngineFactory.CreateBmpnEngine().ComplateNode(wfParamter.operatorEntity.NodeId, variables);
            var nextNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetCurrentNodeList(wfParamter.taskEntity.InstanceId);
            if (nextNodeCodeList.Any())
            {
                wfParamter.triggerNodeList = wfParamter.nodeList.FindAll(x => x.nodeType == "trigger" && nextNodeCodeList.Select(x => x.taskKey).Contains(x.nodeCode));
            }
        }
        // 当前节点下一节点
        var currentNodeNextNodeList = (await BpmnEngineFactory.CreateBmpnEngine().GetNextNode(wfParamter.engineId, wfParamter.operatorEntity.NodeCode, string.Empty)).Select(x => x.id).ToList();
        foreach (var item in wfParamter.triggerNodeList)
        {
            if (!currentNodeNextNodeList.Contains(item.nodeCode)) continue;
            var triggerPro = item.nodeJson.ToObject<TriggerProperties>();
            var triggerTask = await CreatTriggerTask(triggerPro, model, wfParamter);
            if (triggerTask != null)
            {
                await TriggerTaskRun(triggerTask, model, wfParamter, triggerPro.groupId);
            }
        }
    }

    /// <summary>
    /// 创建触发任务.
    /// </summary>
    /// <param name="triggerPro"></param>
    /// <param name="model"></param>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task<WorkFlowTriggerTaskEntity> CreatTriggerTask(TriggerProperties triggerPro, TaskFlowEventModel model, WorkFlowParamter wfParamter)
    {
        WorkFlowTriggerTaskEntity triggerTask = new WorkFlowTriggerTaskEntity();
        WorkFlowTriggerRecordEntity triggerRecord = new WorkFlowTriggerRecordEntity();
        triggerRecord.StartTime = DateTime.Now;
        var isCreat = true;
        if (triggerPro.triggerEvent == 2 && triggerPro.actionList.Any())
        {
            isCreat = triggerPro.actionList.Contains(model.ActionType);
        }
        if (isCreat)
        {
            var nextNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetCurrentNodeList(wfParamter.taskEntity.InstanceId);
            var triggerNode = nextNodeCodeList.Find(x => x.taskKey == triggerPro.nodeId);
            if (triggerNode.IsNullOrEmpty()) return null;
            var conditionCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetLineCode(string.Empty, triggerNode.taskKey, triggerNode.taskId);
            var variables = conditionCodeList.ToDictionary(x => x, y => isCreat);
            await BpmnEngineFactory.CreateBmpnEngine().ComplateNode(triggerNode.taskId, variables);
            triggerTask.Id = SnowflakeIdHelper.NextId();
            triggerTask.ParentId = "0";
            triggerTask.FullName = string.Format("{0}-{1}", wfParamter.taskEntity.FlowName, triggerPro.nodeName);
            triggerTask.StartTime = DateTime.Now;
            triggerTask.FlowId = wfParamter.taskEntity.FlowId;
            triggerTask.Data = model.taskFlowData.ToJsonString();
            triggerTask.EngineType = 1;
            triggerTask.InstanceId = wfParamter.taskEntity.InstanceId;
            triggerTask.TaskId = wfParamter.taskEntity.Id;
            triggerTask.IsAsync = triggerPro.isAsync;
            triggerTask.NodeCode = wfParamter.operatorEntity.NodeCode;
            triggerTask.NodeId = wfParamter.operatorEntity.NodeId;
            triggerTask.Status = WorkFlowTaskStatusEnum.Runing.ParseToInt();
            triggerTask.CreatorUserId = model.UserId;
            triggerTask.CreatorTime = DateTime.Now;
            await _sqlSugarClient.CopyNew().Insertable(triggerTask).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            var nextNode = wfParamter.nodeList.Find(x => x.nodeCode == triggerNode.taskKey);
            var nextNodePro = nextNode.nodeJson.ToObject<EventProperties>();
            triggerRecord.Id = SnowflakeIdHelper.NextId();
            triggerRecord.TriggerId = triggerTask.Id;
            triggerRecord.TaskId = triggerTask.TaskId;
            triggerRecord.NodeId = triggerNode.taskId;
            triggerRecord.NodeCode = nextNodePro.nodeId;
            triggerRecord.NodeName = nextNodePro.nodeName;
            triggerRecord.CreatorUserId = model.UserId;
            triggerRecord.CreatorTime = DateTime.Now;
            triggerRecord.Status = 0;
            await _sqlSugarClient.CopyNew().Insertable(triggerRecord).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            model.dataSource[triggerPro.nodeId] = model.taskFlowData;
        }
        else
        {
            return null;
        }
        return triggerTask;
    }

    /// <summary>
    /// 执行触发.
    /// </summary>
    /// <param name="triggerTaskEntity"></param>
    /// <param name="model"></param>
    /// <param name="wfParamter"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public async Task TriggerTaskRun(WorkFlowTriggerTaskEntity triggerTaskEntity, TaskFlowEventModel model, WorkFlowParamter wfParamter, string groupId)
    {
        try
        {
            var nodeTypeList = new List<string> { "getData", "addData", "updateData", "deleteData", "dataInterface", "message", "launchFlow", "schedule" };
            var triggerNodeList = wfParamter.nodeList.FindAll(x => nodeTypeList.Contains(x.nodeType));
            var recordEntityList = await _sqlSugarClient.CopyNew().Queryable<WorkFlowTriggerRecordEntity>().Where(x => x.TriggerId == triggerTaskEntity.Id).ToListAsync();
            var nextNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetCurrentNodeList(triggerTaskEntity.InstanceId);
            if (triggerNodeList != null && triggerNodeList.Any() && nextNodeCodeList != null) // 剔除审批节点
            {
                nextNodeCodeList = nextNodeCodeList.FindAll(x => triggerNodeList.Select(x => x.nodeCode).Contains(x.taskKey));
            }
            if (recordEntityList != null && recordEntityList.Any() && nextNodeCodeList != null) // 剔除已执行触发节点
            {
                nextNodeCodeList = nextNodeCodeList.FindAll(x => !recordEntityList.Select(x => x.NodeCode).Contains(x.taskKey));
            }
            if (nextNodeCodeList != null && nextNodeCodeList.Any())
            {
                var comList = new List<bool>();
                foreach (var item in nextNodeCodeList)
                {
                    WorkFlowTriggerRecordEntity triggerRecord = new WorkFlowTriggerRecordEntity();
                    triggerRecord.StartTime = DateTime.Now;
                    var nextNode = wfParamter.nodeList.Find(x => x.nodeCode == item.taskKey);
                    var nextNodePro = nextNode.nodeJson.ToObject<EventProperties>();
                    if (nextNodePro.groupId != groupId) continue;
                    triggerRecord.Id = SnowflakeIdHelper.NextId();
                    triggerRecord.TriggerId = triggerTaskEntity.Id;
                    triggerRecord.TaskId = triggerTaskEntity.TaskId;
                    triggerRecord.NodeId = item.taskId;
                    triggerRecord.NodeCode = nextNodePro.nodeId;
                    triggerRecord.NodeName = nextNodePro.nodeName;
                    triggerRecord.CreatorUserId = model.UserId;
                    triggerRecord.CreatorTime = DateTime.Now;
                    await _sqlSugarClient.CopyNew().Insertable(triggerRecord).ExecuteCommandAsync();
                    ReplaceInterFaceGlobalParamter(nextNodePro, wfParamter);
                    var output = await TriggerNodeExecute(nextNodePro, model); // 节点是否完成.
                    comList.Add(output.isComplete);
                    if (output.isComplete)
                    {
                        var isComLastNode = true; // 是否完成最后触发节点
                        if (model.ActionType == 3)
                        {
                            var nextNodeList = await BpmnEngineFactory.CreateBmpnEngine().GetNextNode(string.Empty, item.taskKey, item.taskId);
                            if (!nextNodeList.Any())
                            {
                                isComLastNode = false;
                            }
                        }
                        if (isComLastNode)
                        {
                            var conditionCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetLineCode(string.Empty, item.taskKey, item.taskId);
                            var variables = conditionCodeList.ToDictionary(x => x, y => true);
                            await BpmnEngineFactory.CreateBmpnEngine().ComplateNode(item.taskId, variables);
                        }
                        triggerRecord.Status = 0;
                        triggerRecord.EndTime = DateTime.Now;
                        await _sqlSugarClient.CopyNew().Updateable(triggerRecord).ExecuteCommandAsync();
                    }
                    else
                    {
                        triggerRecord.Status = 1;
                        triggerRecord.EndTime = DateTime.Now;
                        triggerRecord.ErrorTip = output.result.IsNotEmptyOrNull() && output.result.msg.IsNotEmptyOrNull() ? output.result.msg.ToString() : "执行异常";
                        triggerRecord.ErrorData = output.result.msg.ToString();
                        await _sqlSugarClient.CopyNew().Updateable(triggerRecord).ExecuteCommandAsync();
                    }
                }
                if (comList.Any())
                {
                    await TriggerTaskRun(triggerTaskEntity, model, wfParamter, groupId);
                }
                else
                {
                    triggerTaskEntity.Status = recordEntityList != null && recordEntityList.Any(x => x.Status == 1) ? WorkFlowTaskStatusEnum.Error.ParseToInt() : WorkFlowTaskStatusEnum.Pass.ParseToInt();
                    await _sqlSugarClient.CopyNew().Updateable(triggerTaskEntity).ExecuteCommandAsync();
                    var triggerTaskList = await _sqlSugarClient.CopyNew().Queryable<WorkFlowTriggerTaskEntity>().Where(x => x.NodeId == wfParamter.operatorEntity.NodeId && x.TaskId == wfParamter.taskEntity.Id && x.Status != WorkFlowTaskStatusEnum.Runing.ParseToInt()).ToListAsync();
                    if (triggerTaskList.Count == wfParamter.triggerNodeList.Where(x => x.nodeJson.ToObject<TriggerProperties>().actionList.Contains(model.ActionType) || x.nodeJson.ToObject<TriggerProperties>().triggerEvent == 3).Count()) // 触发结束回到审批节点
                    {
                        await EndTrigger(model, wfParamter);
                    }
                }
            }
            else
            {
                triggerTaskEntity.Status = recordEntityList != null && recordEntityList.Any(x => x.Status == 1) ? WorkFlowTaskStatusEnum.Error.ParseToInt() : WorkFlowTaskStatusEnum.Pass.ParseToInt();
                await _sqlSugarClient.CopyNew().Updateable(triggerTaskEntity).ExecuteCommandAsync();
                var triggerTaskList = await _sqlSugarClient.CopyNew().Queryable<WorkFlowTriggerTaskEntity>().Where(x => x.NodeId == wfParamter.operatorEntity.NodeId && x.TaskId == wfParamter.taskEntity.Id && x.Status != WorkFlowTaskStatusEnum.Runing.ParseToInt()).ToListAsync();
                if (triggerTaskList.Count == wfParamter.triggerNodeList.Where(x => x.nodeJson.ToObject<TriggerProperties>().actionList.Contains(model.ActionType) || x.nodeJson.ToObject<TriggerProperties>().triggerEvent == 3).Count()) // 触发结束回到审批节点
                {
                    await EndTrigger(model, wfParamter);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 触发结束回到主流程.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task EndTrigger(TaskFlowEventModel model, WorkFlowParamter wfParamter)
    {
        var scheduleTaskModel = new ScheduleTaskModel();
        var requestAddress = string.Empty;

        if (_sqlSugarClient.CopyNew().Queryable<WorkFlowTriggerTaskEntity>().Any(x => x.TaskId == wfParamter.taskEntity.Id && x.Status == 10 && x.IsAsync == 0))
        {
            var parameter = wfParamter.Adapt<WorkFlowHandleModel>().ToJsonString();
            requestAddress = string.Format("/api/workflow/Monitor/Cancel/{0}", wfParamter.taskEntity.Id);
            await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
        }
        else
        {
            if (model.ActionType == 1 || model.ActionType == 4)
            {
                if (wfParamter.triggerNodeList.Any(x => !x.nodePro.isAsync))
                {
                    var ids = wfParamter.nextOperatorEntityList.Select(x => x.Id).ToList();
                    await _sqlSugarClient.CopyNew().Updateable<WorkFlowOperatorEntity>().SetColumns(x => x.Status == 1).Where(x => ids.Contains(x.Id)).ExecuteCommandAsync();
                    scheduleTaskModel.taskParams.Add("wfParamter", wfParamter);
                    requestAddress = string.Format("/ScheduleTask/autoaudit");
                    await HttpClient(1, requestAddress, scheduleTaskModel, model.UserId, model.TenantId);
                }
            }
            else if (model.ActionType == 2)
            {
                if (wfParamter.globalPro.hasContinueAfterReject && wfParamter.triggerNodeList.Any(x => !x.nodePro.isAsync))
                {
                    var ids = wfParamter.nextOperatorEntityList.Select(x => x.Id).ToList();
                    await _sqlSugarClient.CopyNew().Updateable<WorkFlowOperatorEntity>().SetColumns(x => x.Status == 0).Where(x => ids.Contains(x.Id)).ExecuteCommandAsync();
                    scheduleTaskModel.taskParams.Add("wfParamter", wfParamter);
                    requestAddress = string.Format("/ScheduleTask/autoaudit");
                    await HttpClient(1, requestAddress, scheduleTaskModel, model.UserId, model.TenantId);
                }

                if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Reject.ParseToInt())
                {
                    await BpmnEngineFactory.CreateBmpnEngine().InstanceDelete(wfParamter.taskEntity.InstanceId);
                }
            }
            else
            {
                scheduleTaskModel.taskParams.Add("wfParamter", wfParamter);
                requestAddress = string.Format("/ScheduleTask/sendback");
                await HttpClient(1, requestAddress, scheduleTaskModel, model.UserId, model.TenantId);
            }
        }
    }

    /// <summary>
    /// 拒绝（不流转）和退回不走结束节点.
    /// </summary>
    /// <param name="variables"></param>
    /// <param name="model"></param>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task UpdateEndLine(Dictionary<string, bool> variables,TaskFlowEventModel model, WorkFlowParamter wfParamter)
    {
        if ((model.ActionType == 2 && !wfParamter.globalPro.hasContinueAfterReject) || model.ActionType == 3)
        {
            var endNodeCode = wfParamter.nodeList.FirstOrDefault(x => x.nodeType == WorkFlowNodeTypeEnum.end.ToString()).nodeCode;
            var endLines = await BpmnEngineFactory.CreateBmpnEngine().GetEndLine(wfParamter.flowInfo.flowableId, endNodeCode);
            foreach (var item in variables.Keys)
            {
                if (endLines.Contains(item))
                {
                    variables[item] = false;
                }
            }
        }
    }
    #endregion

    /// <summary>
    /// 触发节点执行.
    /// </summary>
    /// <param name="nextNodePro"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<TriggerNodeOutput> TriggerNodeExecute(EventProperties nextNodePro, TaskFlowEventModel model)
    {
        RESTfulResult<object> result = new RESTfulResult<object>() { code = 200 };
        var requestAddress = string.Empty;
        var input = new DataInterfacePreviewInput();
        var scheduleTaskModel = new ScheduleTaskModel();
        string parameter = string.Empty;
        var dataList = new List<Dictionary<string, object>>();
        try
        {
            switch (nextNodePro.type)
            {
                case WorkFlowNodeTypeEnum.getData:
                    switch (nextNodePro.formType)
                    {
                        case 1:
                        case 2:
                        case 4:
                            parameter = GetListParamter(nextNodePro, model.dataSource);
                            requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", nextNodePro.formId);
                            result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                            if (result.code == 200 && result.data.IsNotEmptyOrNull())
                            {
                                var resultData = Clay.Parse(result.data.ToString());
                                string list = resultData.list.ToString();
                                dataList = list.ToObject<List<Dictionary<string, object>>>();
                                if (nextNodePro.formType == 4)
                                {
                                    var subTableDataList = new List<Dictionary<string, object>>();
                                    foreach (var data in dataList)
                                    {
                                        if (data != null && data.ContainsKey(nextNodePro.subTable))
                                        {
                                            var subTableData = data[nextNodePro.subTable].ToJsonString().ToList<Dictionary<string, object>>();
                                            if (subTableData.Any())
                                                subTableDataList.AddRange(GetSubTableData(subTableData, nextNodePro.subTable));
                                        }
                                    }
                                    model.dataSource.Add(nextNodePro.nodeId, subTableDataList);
                                }
                                else
                                {
                                    model.dataSource.Add(nextNodePro.nodeId, dataList);
                                }
                            }
                            break;
                        case 3:
                            input = new DataInterfacePreviewInput
                            {
                                paramList = nextNodePro.interfaceTemplateJson,
                                tenantId = model.TenantId,
                                sourceData = GetDataInterFaceSource(nextNodePro.interfaceTemplateJson, model.dataSource),
                            };
                            scheduleTaskModel.taskParams.Add("id", nextNodePro.formId);
                            scheduleTaskModel.taskParams.Add("input", input.ToJsonString());
                            requestAddress = string.Format("/ScheduleTask/datainterface");
                            result = await HttpClient(1, requestAddress, scheduleTaskModel, model.UserId, model.TenantId);
                            if (result.code == 200 && result.data.IsNotEmptyOrNull())
                            {
                                // 数据接口返回数据data是数组才接收 其余不接收
                                if (result.data.ToJsonString().FirstOrDefault().Equals('['))
                                {
                                    dataList = result.data.ToJsonString().ToObject<List<Dictionary<string, object>>>();
                                    model.dataSource.Add(nextNodePro.nodeId, dataList);
                                }
                            }
                            break;
                    }
                    break;
                case WorkFlowNodeTypeEnum.addData:
                    if (nextNodePro.dataSourceForm.IsNotEmptyOrNull())
                    {
                        if (model.dataSource.ContainsKey(nextNodePro.dataSourceForm))
                            dataList = model.dataSource[nextNodePro.dataSourceForm].ToObject<List<Dictionary<string, object>>>();
                    }
                    else
                    {
                        dataList.Add(new Dictionary<string, object>());
                    }
                    foreach (var item in dataList)
                    {
                        var isInsert = true;
                        if (nextNodePro.ruleList.Any())
                        {
                            parameter = GetListParamter(nextNodePro, item);
                            requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", nextNodePro.formId);
                            result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                            if (result.code == 200 && result.data.IsNotEmptyOrNull())
                            {
                                var resultData = Clay.Parse(result.data.ToString());
                                string list = resultData.list.ToString();
                                var idList = list.ToObject<List<object>>();
                                if (idList.Any()) { isInsert = false; }
                            }
                        }
                        if (isInsert)
                        {
                            var targetForm = GetSaveData(item, model, nextNodePro);
                            parameter = new VisualDevModelBatchInput
                            {
                                id = string.Empty,
                                dataList = targetForm,
                                isCreate = true,
                                isInteAssis = false,
                            }.ToJsonString();
                            requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/Save", nextNodePro.formId);
                            result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                        }
                    }
                    break;
                case WorkFlowNodeTypeEnum.updateData:
                    if (nextNodePro.dataSourceForm.IsNotEmptyOrNull())
                    {
                        if (model.dataSource.ContainsKey(nextNodePro.dataSourceForm))
                            dataList = model.dataSource[nextNodePro.dataSourceForm].ToObject<List<Dictionary<string, object>>>();
                    }
                    else
                    {
                        dataList.Add(new Dictionary<string, object>());
                    }
                    foreach (var item in dataList)
                    {
                        var isUpdate = false;
                        parameter = GetListParamter(nextNodePro, item);
                        requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", nextNodePro.formId);
                        result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                        if (result.code == 200 && result.data.IsNotEmptyOrNull())
                        {
                            var resultData = Clay.Parse(result.data.ToString());
                            string list = resultData.list.ToString();
                            var updateDataList = list.ToObject<List<Dictionary<string, object>>>();
                            if (updateDataList.Any())
                            {
                                var targetForm = GetSaveData(item, model, nextNodePro, updateDataList);
                                parameter = new VisualDevModelBatchInput
                                {
                                    id = string.Empty,
                                    dataList = targetForm,
                                    isCreate = false,
                                    isInteAssis = false,
                                    isDelSubTableData = false,
                                }.ToJsonString();
                                isUpdate = true;
                            }
                            else
                            {
                                if (nextNodePro.unFoundRule) // 新增数据
                                {
                                    var targetForm = GetSaveData(item, model, nextNodePro);
                                    parameter = new VisualDevModelBatchInput
                                    {
                                        id = string.Empty,
                                        dataList = targetForm,
                                        isCreate = true,
                                        isInteAssis = false,
                                        isDelSubTableData = false,
                                    }.ToJsonString();
                                    isUpdate = true;
                                }
                            }
                            if (isUpdate)
                            {
                                requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/Save", nextNodePro.formId);
                                result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                            }
                        }
                    }
                    break;
                case WorkFlowNodeTypeEnum.deleteData:
                    var delIdList = new List<string>();
                    if (nextNodePro.deleteType == 0) // 直接删除表
                    {
                        parameter = GetListParamter(nextNodePro, model.dataSource);
                        requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", nextNodePro.formId);
                        result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                        if (result.code == 200 && result.data.IsNotEmptyOrNull())
                        {
                            var resultData = Clay.Parse(result.data.ToString());
                            string list = resultData.list.ToString();
                            var delDataList = list.ToObject<List<Dictionary<string, object>>>();
                            if (nextNodePro.tableType == 0)
                            {
                                delIdList = delDataList.Select(x => x["id"].ToString()).ToList();
                                parameter = new VisualDevModelDataBatchDelInput { ids = delIdList, isInteAssis = false, deleteRule = 1 }.ToJsonString();
                                requestAddress = string.Format("/api/visualdev/OnlineDev/batchDelete/{0}", nextNodePro.formId);
                                result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                            }
                            else
                            {
                                var rulelist = ReplaceConditionValue(nextNodePro.ruleList, model.dataSource, nextNodePro.deleteType == 0);
                                parameter = new VisualDevModelDelChildTableInput
                                {
                                    table = nextNodePro.subTable,
                                    queryConfig = new { matchLogic = nextNodePro.ruleMatchLogic, conditionList = rulelist }.ToJsonString(),
                                }.ToJsonString();
                                requestAddress = string.Format("/api/visualdev/OnlineDev/DelChildTable/{0}", nextNodePro.formId);
                                result = await HttpClient(3, requestAddress, parameter, model.UserId, model.TenantId);
                            }
                        }
                    }
                    else // 按节点删除
                    {
                        if (nextNodePro.dataSourceForm.IsNotEmptyOrNull())
                        {
                            if (model.dataSource.ContainsKey(nextNodePro.dataSourceForm))
                                dataList = model.dataSource[nextNodePro.dataSourceForm].ToObject<List<Dictionary<string, object>>>();
                        }
                        else
                        {
                            dataList.Add(new Dictionary<string, object>());
                        }
                        foreach (var item in dataList)
                        {
                            parameter = GetListParamter(nextNodePro, item);
                            requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", nextNodePro.formId);
                            result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                            if (result.code != 200) break;
                            if (result.code == 200 && result.data.IsNotEmptyOrNull())
                            {
                                var resultData = Clay.Parse(result.data.ToString());
                                string list = resultData.list.ToString();
                                var delDataList = list.ToObject<List<Dictionary<string, object>>>();
                                delIdList.AddRange(delDataList.Select(x => x["id"].ToString()).ToList());
                            }
                        }
                        parameter = new VisualDevModelDataBatchDelInput { ids = delIdList, isInteAssis = false, deleteRule = nextNodePro.deleteCondition == 1 ? 1 : 0 }.ToJsonString();
                        requestAddress = string.Format("/api/visualdev/OnlineDev/batchDelete/{0}", nextNodePro.formId);
                        result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                    }
                    break;
                case WorkFlowNodeTypeEnum.dataInterface:
                    if (nextNodePro.dataSourceForm.IsNotEmptyOrNull())
                    {
                        if (model.dataSource.ContainsKey(nextNodePro.dataSourceForm))
                            dataList = model.dataSource[nextNodePro.dataSourceForm].ToObject<List<Dictionary<string, object>>>();
                    }
                    else
                    {
                        dataList.Add(new Dictionary<string, object>());
                    }
                    requestAddress = string.Format("/ScheduleTask/datainterface");
                    foreach (var item in dataList)
                    {
                        input = new DataInterfacePreviewInput
                        {
                            paramList = nextNodePro.templateJson,
                            tenantId = model.TenantId,
                            sourceData = GetDataInterFaceSource(nextNodePro.templateJson, item, false),
                        };
                        scheduleTaskModel.taskParams["id"] = nextNodePro.formId;
                        scheduleTaskModel.taskParams["input"] = input.ToJsonString();
                        result = await HttpClient(1, requestAddress, scheduleTaskModel, model.UserId, model.TenantId);
                        if (result.code != 200) break;
                    }
                    break;
                case WorkFlowNodeTypeEnum.message:
                    parameter = ReplaceMsgValue(nextNodePro, model.dataSource);
                    requestAddress = string.Format("/api/Message/MessageNotice");
                    result = await HttpClient(1, requestAddress, parameter, model.UserId, model.TenantId);
                    break;
                case WorkFlowNodeTypeEnum.launchFlow:
                    var requestAddress1 = string.Format("/api/workflow/Operator/CandidateNode/0");
                    requestAddress = string.Format("/api/workflow/task");
                    if (nextNodePro.dataSourceForm.IsNotEmptyOrNull())
                    {
                        if (model.dataSource.ContainsKey(nextNodePro.dataSourceForm))
                            dataList = model.dataSource[nextNodePro.dataSourceForm].ToObject<List<Dictionary<string, object>>>();
                    }
                    else
                    {
                        dataList.Add(new Dictionary<string, object>());
                    }
                    if (nextNodePro.assigneeType == 4)
                    {
                        nextNodePro.initiator = new List<string>();
                        foreach (var item in dataList)
                        {
                            var jd = item.ToObject<JObject>();
                            var fieldValueList = new List<string>();
                            var formField = nextNodePro.formField;
                            if (jd.ContainsKey(formField))
                            {
                                if (jd[formField] is JArray)
                                {
                                    fieldValueList = jd[formField].ToObject<List<string>>();
                                }
                                else if (jd[formField].ToString().Contains("["))
                                {
                                    fieldValueList = jd[formField].ToString().ToObject<List<string>>();
                                }
                                else
                                {
                                    if (jd[formField].ToString().IsNotEmptyOrNull())
                                    {
                                        fieldValueList = jd[formField].ToString().Split(",").ToList();
                                    }
                                }
                            }
                            else
                            {
                                var fields = formField.Split("-").ToList();
                                // 子表键值
                                var tableField = fields[0];
                                // 子表字段键值
                                var keyField = fields[1];
                                if (jd.ContainsKey(tableField) && jd[tableField] is JArray)
                                {
                                    var jar = jd[tableField] as JArray;

                                    fieldValueList = jar.Where(x => x.ToObject<JObject>().ContainsKey(keyField)).Select(x => x.ToObject<JObject>()[keyField].ToString()).ToList();
                                }
                            }
                            nextNodePro.initiator.AddRange(fieldValueList);
                        }
                    }
                    var crUserIds = GetUserId(nextNodePro.initiator);
                    if (!crUserIds.Any()) throw Oops.Oh(ErrorCode.WF0064);
                    foreach (var item in dataList)
                    {
                        var targetFormList = GetSaveData(item, model, nextNodePro);
                        foreach (var formData in targetFormList)
                        {
                            parameter = new { candidateType = 3, countersignOver = true, flowId = nextNodePro.flowId, formData = formData, status = 1, hasPermission = nextNodePro.hasPermission }.ToJsonString();
                            var skinCount = 0;
                            foreach (var initiator in crUserIds)
                            {
                                result = await HttpClient(1, requestAddress1, parameter, initiator, model.TenantId);
                                if (result.code == 200)
                                {
                                    var output = result.data.ToObject<Dictionary<string, object>>();
                                    if (output.IsNotEmptyOrNull() && output.ContainsKey("type") && output["countersignOver"].ToString() == "True")
                                    {
                                        var type = output.ContainsKey("type") ? output["type"].ToString() : string.Empty;
                                        if (type == "1")
                                        {
                                            result = new RESTfulResult<object>
                                            {
                                                code = 400,
                                                msg = "发起节点设置了选择分支，无法发起审批"
                                            };
                                            break;
                                        }
                                        if (type == "2")
                                        {
                                            result = new RESTfulResult<object>
                                            {
                                                code = 400,
                                                msg = "第一个审批节点设置候选人，无法发起审批"
                                            };
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                                result = await HttpClient(1, requestAddress, parameter, initiator, model.TenantId);
                                if (result.code == 200)
                                {
                                    var output = result.data.ToObject<OperatorOutput>();
                                    if (output.IsNotEmptyOrNull() && output.errorCodeList.Any())
                                    {
                                        result = new RESTfulResult<object>
                                        {
                                            code = 400,
                                            msg = "第一个审批节点异常，无法自动发起审批"
                                        };
                                        break;
                                    }
                                }
                                else
                                {
                                    if (result.msg.Equals("[WF0052] 您没有发起该流程的权限"))
                                    {
                                        skinCount++;
                                        result = new RESTfulResult<object>
                                        {
                                            code = 200,
                                        };
                                        continue;
                                    }
                                    break;
                                }
                            }
                            if (skinCount == crUserIds.Count)
                            {
                                result = new RESTfulResult<object>
                                {
                                    code = 400,
                                    msg = "找不到发起人，发起失败"
                                };
                            }
                        }
                    }
                    break;
                case WorkFlowNodeTypeEnum.schedule:
                    var parameterList = ReplaceScheduleValue(nextNodePro, model.dataSource);
                    foreach (var item in parameterList)
                    {
                        requestAddress = string.Format("/api/system/Schedule");
                        result = await HttpClient(1, requestAddress, item, model.UserId, model.TenantId);
                        if (result.code != 200) break;
                    }
                    break;
                default:
                    result = new RESTfulResult<object>
                    {
                        code = 200,
                    };
                    break;
            }
        }
        catch (Exception ex)
        {
            result = new RESTfulResult<object>
            {
                code = 400,
                msg = "执行异常,异常原因:" + ex.Message
            };
        }
        return new TriggerNodeOutput { isComplete = result.code == 200, result = result };
    }

    /// <summary>
    /// 获取保存表单数据.
    /// </summary>
    /// <param name="dataSourceItem">数据源.</param>
    /// <param name="model"></param>
    /// <param name="eventPro"></param>
    /// <param name="updateDataList">更新字段.</param>
    /// <returns></returns>
    public List<Dictionary<string, object>> GetSaveData(Dictionary<string, object> dataSourceItem, TaskFlowEventModel model, EventProperties eventPro, List<Dictionary<string, object>> updateDataList = null)
    {
        var fromDataList = new List<Dictionary<string, object>>();
        #region 区分主子表
        foreach (var item in eventPro.transferList)
        {
            if (item.targetField.Contains("-"))
            {
                item.isSubTable_target = true;
                var fieldList = item.targetField.Split("-").ToList();
                item.subTableName_target = fieldList[0];
                item.subTableField_target = fieldList[1];
            }
            if (item.sourceType == 1)
            {
                if (item.sourceValue.Contains("|"))
                {
                    var fieldList = item.sourceValue.Split("|").ToList();
                    item.sourceField = fieldList[0];
                    if (item.sourceField.Contains("-") && !dataSourceItem.ContainsKey("jnpfsubtable"))
                    {
                        item.isSubTable_source = true;
                        var fieldList1 = item.sourceField.Split("-").ToList();
                        item.subTableName_source = fieldList1[0];
                        item.subTableField_source = fieldList1[1];
                    }
                    if (item.sourceField == "@formId") item.sourceField = "id";
                }
            }
        }
        #endregion

        try
        {
            if (updateDataList == null)
            {
                var index = 1; // 表单数据数量
                var fromData = new Dictionary<string, object>(); // 表单数据容器
                var childFiledList = eventPro.transferList.Where(x => x.isSubTable_source && !x.isSubTable_target).Select(x => x.subTableName_source).ToList();
                foreach (var childFiled in childFiledList)
                {
                    if (dataSourceItem.ContainsKey(childFiled))
                    {
                        var childDataList = dataSourceItem[childFiled].ToObject<List<object>>();
                        if (childDataList.Any() && childDataList.Count() > index) index = childDataList.Count();
                    }

                }
                for (int i = 0; i < index; i++)
                {
                    fromData = GetFormData(eventPro.transferList, model, dataSourceItem, i);
                    fromDataList.Add(fromData);
                }
            }
            else
            {
                var index = 0;
                foreach (var item in updateDataList)
                {
                    var fromData = new Dictionary<string, object>();
                    var sourceDic = dataSourceItem;
                    fromData = GetFormData(eventPro.transferList, model, item, sourceDic);
                    fromDataList.Add(fromData);
                    index++;
                }
            }
        }
        catch (Exception)
        {
        }
        return fromDataList;
    }

    /// <summary>
    /// 保存数据传递（新增）.
    /// </summary>
    /// <param name="transferItems"></param>
    /// <param name="model"></param>
    /// <param name="sourceDic"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetFormData(List<TransferItem> transferItems, TaskFlowEventModel model, Dictionary<string, object> sourceDic, int index)
    {
        var dic = new Dictionary<string, object>();
        foreach (var item in transferItems)
        {
            var tName = item.subTableName_target;
            var tField = item.subTableField_target;
            var sName = item.subTableName_source;
            var sField = item.subTableField_source;
            switch (item.sourceType)
            {
                case 1:
                    object value = null;
                    if (item.isSubTable_target)
                    {
                        var tChildDataList = new List<Dictionary<string, object>>();
                        if (dic.ContainsKey(tName))
                        {
                            tChildDataList = dic[tName].ToObject<List<Dictionary<string, object>>>();
                        }
                        else
                        {
                            dic.Add(tName, tChildDataList);
                        }

                        if (item.isSubTable_source) // 子-子
                        {
                            var sChildDataList = sourceDic[sName].ToObject<List<Dictionary<string, object>>>(); //源子表数据
                            for (int i = 0; i < sChildDataList.Count(); i++)
                            {
                                if (i < tChildDataList.Count())
                                {
                                    tChildDataList[i].Add(tField, sChildDataList[i][sField]);
                                }
                                else
                                {
                                    var childDic = new Dictionary<string, object>();
                                    childDic.Add(tField, sChildDataList[i][sField]);
                                    tChildDataList.Add(childDic);
                                }
                            }
                        }
                        else // 子-主
                        {
                            var childDic = new Dictionary<string, object>();
                            if (tChildDataList.Any())
                            {
                                childDic = tChildDataList[0];
                            }
                            else
                            {
                                tChildDataList.Add(new Dictionary<string, object>());
                            }
                            childDic[tField] = GetFormValue(sourceDic, item.sourceField);
                            tChildDataList[0] = childDic;
                        }
                        dic[tName] = tChildDataList;
                    }
                    else
                    {
                        if (item.isSubTable_source) // 主-子
                        {
                            var childDataList = sourceDic[sName].ToObject<List<Dictionary<string, object>>>();
                            if (index < childDataList.Count())
                            {
                                value = GetFormValue(childDataList[index], sField);
                            }
                            else
                            {
                                value = null;
                            }
                        }
                        else // 主-主
                        {
                            value = GetFormValue(sourceDic, item.sourceField);
                        }
                        dic.Add(item.targetField, value);
                    }
                    break;
                case 2:
                    if (item.isSubTable_target)
                    {
                        var tChildDataList = new List<Dictionary<string, object>>();
                        if (dic.ContainsKey(tName))
                        {
                            tChildDataList = dic[tName].ToObject<List<Dictionary<string, object>>>();
                        }
                        else
                        {
                            dic.Add(tName, tChildDataList);
                        }

                        var childDic = new Dictionary<string, object>();
                        if (tChildDataList.Any())
                        {
                            childDic = tChildDataList[0];
                        }
                        else
                        {
                            tChildDataList.Add(new Dictionary<string, object>());
                        }
                        childDic[tField] = item.sourceValue;
                        tChildDataList[0] = childDic;
                        dic[tName] = tChildDataList;
                    }
                    else
                    {
                        dic.Add(item.targetField, item.sourceValue);
                    }
                    break;
                case 4:
                    if (item.isSubTable_target)
                    {
                        var tChildDataList = new List<Dictionary<string, object>>();
                        if (dic.ContainsKey(tName))
                        {
                            tChildDataList = dic[tName].ToObject<List<Dictionary<string, object>>>();
                        }
                        else
                        {
                            dic.Add(tName, tChildDataList);
                        }

                        var childDic = new Dictionary<string, object>();
                        if (tChildDataList.Any())
                        {
                            childDic = tChildDataList[0];
                        }
                        else
                        {
                            tChildDataList.Add(new Dictionary<string, object>());
                        }
                        childDic[tField] = GetSystemParamter(item.sourceValue, model);
                        tChildDataList[0] = childDic;
                        dic[tName] = tChildDataList;
                    }
                    else
                    {
                        dic.Add(item.targetField, GetSystemParamter(item.sourceValue, model));
                    }
                    break;
                default:
                    break;
            }
        }
        return dic;
    }

    /// <summary>
    /// 保存数据传递（更新）.
    /// </summary>
    /// <param name="transferItems"></param>
    /// <param name="model"></param>
    /// <param name="targetDic"></param>
    /// <param name="sourceDic"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetFormData(List<TransferItem> transferItems, TaskFlowEventModel model, Dictionary<string, object> targetDic, Dictionary<string, object> sourceDic)
    {
        foreach (var item in transferItems)
        {
            var tName = item.subTableName_target;
            var tField = item.subTableField_target;
            var sName = item.subTableName_source;
            var sField = item.subTableField_source;
            switch (item.sourceType)
            {
                case 1:
                    if (item.isSubTable_target)
                    {
                        var tChildDataList = targetDic.ContainsKey(tName) ? targetDic[tName].ToObject<List<Dictionary<string, object>>>() : new List<Dictionary<string, object>>();
                        if (item.isSubTable_source)// 子-子
                        {
                            var sChildDataList = sourceDic[sName].ToObject<List<Dictionary<string, object>>>(); //源子表数据
                            var index = 0;
                            foreach (var cItem in tChildDataList)
                            {
                                if (sChildDataList.Count > index)
                                {
                                    cItem[tField] = GetFormValue(sChildDataList[index], sField);
                                }
                                index++;
                            }
                        }
                        else // 子-主
                        {
                            foreach (var cItem in tChildDataList)
                            {
                                cItem[tField] = GetFormValue(sourceDic, item.sourceField);
                            }
                        }
                        targetDic[tName] = tChildDataList;
                    }
                    else
                    {
                        if (item.isSubTable_source) // 主-子
                        {
                            targetDic[item.targetField] = GetFormValue(sourceDic[sName].ToObject<List<Dictionary<string, object>>>().FirstOrDefault(), sField);
                        }
                        else // 主-主
                        {
                            targetDic[item.targetField] = GetFormValue(sourceDic, item.sourceField);
                        }
                    }
                    break;
                case 2:
                    if (item.isSubTable_target)
                    {
                        var tChildDataList = targetDic.ContainsKey(tName) ? targetDic[tName].ToObject<List<Dictionary<string, object>>>() : new List<Dictionary<string, object>>();
                        foreach (var cItem in tChildDataList)
                        {
                            cItem[tField] = item.sourceValue;
                        }
                        targetDic[tName] = tChildDataList;
                    }
                    else
                    {
                        targetDic[item.targetField] = item.sourceValue;
                    }
                    break;
                case 4:
                    if (item.isSubTable_target)
                    {
                        var tChildDataList = targetDic.ContainsKey(tName) ? targetDic[tName].ToObject<List<Dictionary<string, object>>>() : new List<Dictionary<string, object>>();
                        foreach (var cItem in tChildDataList)
                        {
                            cItem[tField] = GetSystemParamter(item.sourceValue, model);
                        }
                        targetDic[tName] = tChildDataList;
                    }
                    else
                    {
                        targetDic[item.targetField] = GetSystemParamter(item.sourceValue, model);
                    }
                    break;
                default:
                    break;
            }
        }
        return targetDic;
    }

    /// <summary>
    /// 获取查询列表参数.
    /// </summary>
    /// <param name="eventPro"></param>
    /// <param name="dataSource"></param>
    /// <returns></returns>
    public string GetListParamter(EventProperties eventPro, Dictionary<string, object> dataSource)
    {
        var input = new VisualDevModelListQueryInput();
        input.currentPage = 1;
        input.pageSize = 999999;
        input.flowIds = "jnpf";
        input.sidx = GetSidx(eventPro.sortList);
        var ruleList = eventPro.ruleList.Copy();
        switch (eventPro.type)
        {
            case WorkFlowNodeTypeEnum.getData:
                ruleList = ReplaceConditionValue(ruleList, dataSource);
                input.superQueryJson = new { matchLogic = eventPro.ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                input.isConvertData = 1;
                if (eventPro.formType == 2)
                {
                    var versionList = _sqlSugarClient.CopyNew().Queryable<WorkFlowVersionEntity>().Where(x => x.TemplateId == eventPro.formId && x.DeleteMark == null).ToList();
                    input.flowIds = string.Join(",", versionList.Select(x => x.Id).ToList());
                    //当前版本发起表单id
                    var version = versionList.Find(x => x.Status == 1);
                    eventPro.formId = _sqlSugarClient.CopyNew().Queryable<WorkFlowNodeEntity>().First(x => x.FlowId == version.Id && x.DeleteMark == null && "start".Equals(x.NodeType))?.FormId;
                    input.isProcessReviewCompleted = 1;
                }
                break;
            case WorkFlowNodeTypeEnum.addData:
            case WorkFlowNodeTypeEnum.updateData:
                ruleList = ReplaceConditionValue(ruleList, dataSource, false);
                input.superQueryJson = new { matchLogic = eventPro.ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                input.isConvertData = 1;
                input.isOnlyId = 0;
                break;
            case WorkFlowNodeTypeEnum.deleteData:
                ruleList = ReplaceConditionValue(ruleList, dataSource, eventPro.deleteType == 0);
                input.superQueryJson = new { matchLogic = eventPro.ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                input.isConvertData = 1;
                input.isOnlyId = 0;
                break;
        }
        return input.ToJsonString();
    }

    /// <summary>
    /// 高级查询参数处理.
    /// </summary>
    /// <param name="gropsItems"></param>
    /// <param name="dataSource"></param>
    /// <param name="isGetData"></param>
    /// <returns></returns>
    public List<GropsItem> ReplaceConditionValue(List<GropsItem> gropsItems, Dictionary<string, object> dataSource, bool isGetData = true)
    {
        foreach (var item in gropsItems)
        {
            foreach (var group in item.groups)
            {
                if (group.fieldValueType == 1 && group.fieldValue != null && group.fieldValue != "")
                {
                    var fieldList = group.fieldValue.Split("|");
                    var field = (string)fieldList[0];
                    if (field == "@formId") field = "id";
                    if (isGetData)
                    {
                        var nodeCode = (string)fieldList[1];
                        if (dataSource.ContainsKey(nodeCode))
                        {
                            var dic = dataSource[nodeCode].ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
                            if (dic != null && dic.ContainsKey(field))
                            {
                                group.fieldValue = GetFormValue(dic, field);
                            }
                        }
                    }
                    else
                    {
                        group.fieldValue = GetFormValue(dataSource, field);
                    }
                }
            }
        }
        return gropsItems;
    }

    /// <summary>
    /// 消息参数处理.
    /// </summary>
    /// <param name="messageSends"></param>
    /// <param name="dataSource"></param>
    /// <returns></returns>
    public string ReplaceMsgValue(EventProperties eventPro, Dictionary<string, object> dataSource)
    {
        foreach (var item in eventPro.msgTemplateJson)
        {
            foreach (var param in item.paramJson)
            {
                if (param.sourceType == 1)
                {
                    if (param.relationField.IsNotEmptyOrNull())
                    {
                        var fieldList = param.relationField.Split("|").ToList();
                        var field = fieldList[0];
                        var nodeCode = fieldList[1];
                        if (field == "@formId") field = "id";
                        if (dataSource.ContainsKey(nodeCode))
                        {
                            var dic = dataSource[nodeCode].ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
                            if (dic.IsNotEmptyOrNull() && dic.ContainsKey(field))
                            {
                                param.value = GetFormValue(dic, field)?.ToString();
                            }
                        }
                    }
                }
                else
                {
                    param.value = param.relationField;
                }
            }
        }
        var msgUserIdList = new List<string>();
        if (eventPro.msgUserIdsSourceType == 1 && eventPro.msgUserIds.IsNotEmptyOrNull() && eventPro.msgUserIds.ToString().Contains("|"))
        {
            var toUserIdList = new List<string>();
            if (eventPro.msgUserIds.IsNotEmptyOrNull() && eventPro.msgUserIds.ToString().Contains("|"))
            {
                var msgUserIds = eventPro.msgUserIds.ToString();
                var fieldList = msgUserIds.Split("|").ToList();
                var field = fieldList[0];
                var nodeCode = fieldList[1];
                if (field == "@formId") field = "id";
                if (dataSource.ContainsKey(nodeCode))
                {
                    var dic = dataSource[nodeCode].ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
                    var value = GetFormValue(dic, field);
                    if (value.IsNotEmptyOrNull())
                    {
                        if (value is Array)
                        {
                            try
                            {
                                var valueList = value.ToObject<List<List<string>>>();
                                foreach (var item in valueList)
                                {
                                    toUserIdList.AddRange(item);
                                }
                            }
                            catch (Exception)
                            {
                                toUserIdList = value.ToObject<List<string>>();
                            }
                        }
                        else if (value.ToString().Contains("["))
                        {
                            try
                            {
                                var valueList = value.ToString().ToObject<List<List<string>>>();
                                foreach (var item in valueList)
                                {
                                    toUserIdList.AddRange(item);
                                }
                            }
                            catch (Exception)
                            {
                                toUserIdList = value.ToString().ToObject<List<string>>();
                            }
                        }
                        else
                        {
                            toUserIdList = value.ToString().Split(",").ToList();
                        }
                    }
                }

            }
            msgUserIdList = GetUserId(toUserIdList);
        }
        else
        {
            msgUserIdList = eventPro.msgUserIds.ToObject<List<string>>();
            msgUserIdList = GetUserId(msgUserIdList);
        }
        return new { templateJson = eventPro.msgTemplateJson, msgUserIds = msgUserIdList }.ToJsonString();
    }

    /// <summary>
    /// 数据接口参数处理.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="dataSource"></param>
    /// <param name="isGetData"></param>
    /// <returns></returns>
    public object GetDataInterFaceSource(List<DataInterfaceParameter> parameters, Dictionary<string, object> dataSource, bool isGetData = true)
    {
        var interFaceSource = new Dictionary<string, object>();
        var fieldParameters = parameters.FindAll(x => x.sourceType == 1); // 字段参数
        foreach (var item in fieldParameters)
        {
            if (item.relationField.IsNullOrEmpty()) continue;
            var fieldList = item.relationField.ToString().Split("|").ToList();
            var field = fieldList[0];
            if (field == "@formId") field = "id";
            if (isGetData)
            {
                var nodeCode = fieldList[1];
                if (dataSource.ContainsKey(nodeCode))
                {
                    var dic = dataSource[nodeCode].ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
                    if (dic.ContainsKey(field))
                    {
                        interFaceSource[item.relationField.ToString()] = GetFormValue(dic, field);
                    }
                }
            }
            else
            {
                if (dataSource.ContainsKey(field))
                {
                    interFaceSource[item.relationField.ToString()] = GetFormValue(dataSource, field);
                }
            }
        }
        return interFaceSource;
    }

    /// <summary>
    /// 日程参数处理.
    /// </summary>
    /// <param name="nextNodePro"></param>
    /// <param name="dataSource"></param>
    /// <returns></returns>
    public List<string> ReplaceScheduleValue(EventProperties nextNodePro, Dictionary<string, object> dataSource)
    {
        try
        {
            var input = new SysScheduleCrInput();
            input.category = "391233231405462789";
            input.urgent = 1;
            input.allDay = nextNodePro.allDay;
            if (nextNodePro.titleSourceType == 1 && nextNodePro.title.Contains("|"))
            {
                var fieldList = nextNodePro.title.Split("|").ToList();
                var field = fieldList[0];
                var nodeCode = fieldList[1];
                if (field == "@formId") field = "id";
                if (dataSource.ContainsKey(nodeCode))
                {
                    var dic = dataSource[nodeCode].ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
                    if (dic.IsNotEmptyOrNull() && dic.ContainsKey(field) && dic[field].IsNotEmptyOrNull())
                    {
                        input.title = dic[field]?.ToString();
                    }
                    else
                    {
                        throw new Exception("标题字段值为空");
                    }
                }
            }
            else
            {
                input.title = nextNodePro.title;
            }
            if (nextNodePro.contentsSourceType == 1 && nextNodePro.contents.Contains("|"))
            {
                var fieldList = nextNodePro.contents.Split("|").ToList();
                var field = fieldList[0];
                var nodeCode = fieldList[1];
                if (field == "@formId") field = "id";
                if (dataSource.ContainsKey(nodeCode))
                {
                    var dic = dataSource[nodeCode].ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
                    if (dic.IsNotEmptyOrNull() && dic.ContainsKey(field) && dic[field].IsNotEmptyOrNull())
                    {
                        input.content = dic[field]?.ToString();
                    }
                }
            }
            else
            {
                input.content = nextNodePro.contents;
            }
            if (nextNodePro.startDaySourceType == 1 && nextNodePro.startDay.ToString().Contains("|"))
            {
                var startDay = nextNodePro.startDay.ToString();
                var fieldList = startDay.Split("|").ToList();
                var field = fieldList[0];
                var nodeCode = fieldList[1];
                if (field == "@formId") field = "id";
                if (dataSource.ContainsKey(nodeCode))
                {
                    var dic = dataSource[nodeCode].ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>().FirstOrDefault();
                    if (dic.IsNotEmptyOrNull() && dic.ContainsKey(field) && dic[field].IsNotEmptyOrNull())
                    {
                        try
                        {
                            var value = GetFormValue(dic, field);
                            DateTime dtDate;
                            if (DateTime.TryParse(value.ToString(), out dtDate))
                            {
                                input.startDay = dtDate;
                            }
                            else
                            {
                                input.startDay = value.ToString().TimeStampToDateTime();
                            }
                        }
                        catch (Exception)
                        {
                            input.startDay = DateTime.Now;
                        }
                    }
                    else
                    {
                        throw new Exception("开始时间字段值为空");
                    }
                }
            }
            else
            {
                input.startDay = nextNodePro.startDay.IsNotEmptyOrNull() ? nextNodePro.startDay.ToString().TimeStampToDateTime() : DateTime.Now;
            }
            input.startTime = nextNodePro.startTime;
            if (nextNodePro.endDaySourceType == 1 && nextNodePro.endDay.IsNotEmptyOrNull() && nextNodePro.endDay.ToString().Contains("|"))
            {
                var endDay = nextNodePro.endDay.ToString();
                var fieldList = endDay.Split("|").ToList();
                var field = fieldList[0];
                var nodeCode = fieldList[1];
                if (field == "@formId") field = "id";
                if (dataSource.ContainsKey(nodeCode))
                {
                    var dic = dataSource[nodeCode].ToJsonStringOld().ToObjectOld<List<Dictionary<string, object>>>().FirstOrDefault();
                    if (dic.IsNotEmptyOrNull() && dic.ContainsKey(field) && dic[field].IsNotEmptyOrNull())
                    {
                        try
                        {
                            var value = GetFormValue(dic, field);
                            DateTime dtDate;
                            if (DateTime.TryParse(value.ToString(), out dtDate))
                            {
                                input.endDay = dtDate;
                            }
                            else
                            {
                                input.endDay = value.ToString().TimeStampToDateTime();
                            }
                        }
                        catch (Exception)
                        {
                            input.endDay = DateTime.Now;
                        }
                    }
                    else
                    {
                        throw new Exception("结束时间字段值为空");
                    }
                }
            }
            else
            {
                input.endDay = nextNodePro.endDay.IsNotEmptyOrNull() ? nextNodePro.endDay.ToString().TimeStampToDateTime() : DateTime.Now;
            }
            input.endTime = nextNodePro.endTime;
            input.duration = nextNodePro.duration;
            input.color = nextNodePro.color;
            input.reminderTime = nextNodePro.reminderTime;
            input.reminderType = nextNodePro.reminderType;
            input.send = nextNodePro.send;
            input.sendName = nextNodePro.sendName;
            input.repetition = nextNodePro.repetition.ToString();
            input.repeatTime = nextNodePro.repeatTime.IsNotEmptyOrNull() ? nextNodePro.repeatTime.ParseToLong().TimeStampToDateTime() : null;
            if (nextNodePro.toUserIdsSourceType == 1 && nextNodePro.toUserIds.IsNotEmptyOrNull() && nextNodePro.toUserIds.ToString().Contains("|"))
            {
                var toUserIdList = new List<string>();
                if (nextNodePro.toUserIds.IsNotEmptyOrNull() && nextNodePro.toUserIds.ToString().Contains("|"))
                {
                    var toUserIds = nextNodePro.toUserIds.ToString();
                    var fieldList = toUserIds.Split("|").ToList();
                    var field = fieldList[0];
                    var nodeCode = fieldList[1];
                    if (field == "@formId") field = "id";
                    if (dataSource.ContainsKey(nodeCode))
                    {
                        var dic = dataSource[nodeCode].ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
                        var value = GetFormValue(dic, field);
                        if (value.IsNotEmptyOrNull())
                        {
                            if (value is Array)
                            {
                                try
                                {
                                    var valueList = value.ToObject<List<List<string>>>();
                                    foreach (var item in valueList)
                                    {
                                        toUserIdList.AddRange(item);
                                    }
                                }
                                catch (Exception)
                                {
                                    toUserIdList = value.ToObject<List<string>>();
                                }
                            }
                            else if (value.ToString().Contains("["))
                            {
                                try
                                {
                                    var valueList = value.ToObject<List<List<string>>>();
                                    foreach (var item in valueList)
                                    {
                                        toUserIdList.AddRange(item);
                                    }
                                }
                                catch (Exception)
                                {
                                    toUserIdList = value.ToObject<List<string>>();
                                }
                            }
                            else
                            {
                                toUserIdList = value.ToString().Split(",").ToList();
                            }
                        }
                    }

                }
                input.toUserIds = GetUserId(toUserIdList);
            }
            else
            {
                input.toUserIds = nextNodePro.toUserIds.ToObject<List<string>>();
            }
            input.files = nextNodePro.files.ToJsonString();

            var userIds = new List<string>();
            if (nextNodePro.creatorUserIdSourceType == 1)
            {
                if (nextNodePro.creatorUserId.Contains("|"))
                {
                    var fieldList = nextNodePro.creatorUserId.Split("|").ToList();
                    var field = fieldList[0];
                    var nodeCode = fieldList[1];
                    if (field == "@formId") field = "id";
                    if (dataSource.ContainsKey(nodeCode))
                    {
                        var dic = dataSource[nodeCode].ToObject<List<Dictionary<string, object>>>().FirstOrDefault();
                        if (dic.IsNullOrEmpty()) throw new Exception(string.Format("数据源{0}为空", nodeCode));
                        var value = GetFormValue(dic, field);
                        if (value.IsNotEmptyOrNull())
                        {
                            if (value is Array)
                            {
                                try
                                {
                                    var valueList = value.ToObject<List<List<string>>>();
                                    foreach (var item in valueList)
                                    {
                                        userIds.AddRange(item);
                                    }
                                }
                                catch (Exception)
                                {
                                    userIds = value.ToObject<List<string>>();
                                }
                            }
                            else if (value.ToString().Contains("["))
                            {
                                try
                                {
                                    var valueList = value.ToObject<List<List<string>>>();
                                    foreach (var item in valueList)
                                    {
                                        userIds.AddRange(item);
                                    }
                                }
                                catch (Exception)
                                {
                                    userIds = value.ToObject<List<string>>();
                                }
                            }
                            else
                            {
                                userIds = value.ToString().Split(",").ToList();
                            }
                        }
                        else
                        {
                            throw new Exception("创建人字段值为空");
                        }
                    }
                }
                else
                {
                    userIds = new List<string>();
                }
            }
            else
            {
                userIds.Add(nextNodePro.creatorUserId);
            }

            var list = new List<string>();
            var crUserIds = GetUserId(userIds);
            if (!crUserIds.Any()) throw new Exception("创建人字段值为空");
            foreach (var item in GetUserId(userIds))
            {
                input.creatorUserId = item;
                list.Add(input.ToJsonString());
            }
            return list;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 排序参数处理.
    /// </summary>
    /// <param name="sortList"></param>
    /// <returns></returns>
    public string GetSidx(List<SortItem> sortList)
    {
        if (sortList != null)
        {
            var sidxList = new List<string>();
            foreach (var item in sortList)
            {
                if (item.sortType.Equals("asc"))
                {
                    sidxList.Add(item.field);
                }
                else
                {
                    sidxList.Add("-" + item.field);
                }
            }
            return string.Join(",", sidxList);
        }
        return "";
    }

    /// <summary>
    /// 系统参数处理.
    /// </summary>
    /// <param name="sourceValue"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public object GetSystemParamter(string sourceValue, TaskFlowEventModel model)
    {
        var userEntity = _sqlSugarClient.CopyNew().Queryable<UserEntity>().First(it => it.Id.Equals(model.UserId) && it.EnabledMark == 1);
        if (userEntity.IsNotEmptyOrNull())
        {
            switch (sourceValue)
            {
                case CommonConst.SysParamUserId:
                case "@flowOperatorUserId":
                    return userEntity.Id;
                case "@flowOperatorUserName":
                    return userEntity.RealName;
                default:
                    return "";
            }
        }
        else
        {
            return _userManager.GetSysParamValue(model.UserId);
        }

    }

    /// <summary>
    /// 表单映射key值处理.
    /// </summary>
    /// <param name="dic"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public object GetFormValue(Dictionary<string, object> dic, string key)
    {
        if (dic.IsNullOrEmpty()) return null;
        var keyId = key + "_jnpfId";
        if (dic.ContainsKey(keyId))
        {
            return dic[keyId];
        }
        else if (dic.ContainsKey(key))
        {
            return dic[key];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 全局消息发送.
    /// </summary>
    /// <param name="flowId"></param>
    /// <param name="model"></param>
    /// <param name="title"></param>
    /// <param name="isError"></param>
    /// <returns></returns>
    public async Task SendGlobalMsg(string flowId, TaskFlowEventModel model, string title, bool isError = false)
    {
        var globalNode = await _sqlSugarClient.CopyNew().Queryable<WorkFlowNodeEntity>().Where(x => x.FlowId == flowId && x.NodeType == "global").FirstAsync();
        var globalPro = globalNode.NodeJson.ToObject<GlobalProperties>();
        var msgConfig = isError ? globalPro.failMsgConfig : globalPro.startMsgConfig;
        var msgEnCode = isError ? "MBXTJC001" : "MBXTJC002";
        string parameter = new { mesConfig = msgConfig, msgEnCode = msgEnCode, defaultTitle = title, msgUserType = globalPro.msgUserType, msgUserIds = globalPro.msgUserIds, creatorUserId = model.UserId }.ToJsonString();
        string path = string.Format("/api/Message/ExecuteNotice");
        await HttpClient(1, path, parameter, model.UserId, model.TenantId);
    }

    /// <summary>
    /// 用户组件获取用户id
    /// </summary>
    /// <param name="Ids"></param>
    /// <returns></returns>
    public List<string> GetUserId(List<string> Ids)
    {
        if (Ids.Any())
        {
            var userIdList = new List<string>();
            var objIdList = new List<string>();
            foreach (var item in Ids)
            {
                if (item.Contains("--"))
                {
                    var strList = item.Split("--").ToList();
                    var objId = strList[0];
                    var type = strList[1];
                    if (type == "subOrg")
                    {
                        var ids = _sqlSugarClient.CopyNew().Queryable<OrganizeEntity>()
                           .Where(x => x.ParentId.Equals(objId))
                           .Select(x => x.Id).ToList();
                        objIdList.AddRange(ids);
                    }
                    if (type == "progenyOrg")
                    {
                        var idTreeList = _sqlSugarClient.CopyNew().Queryable<OrganizeEntity>()
                            .Where(x => x.OrganizeIdTree.Contains(objId))
                            .Select(x => x.Id).ToList();
                        objIdList.AddRange(idTreeList);
                    }
                    if (type == "subPos")
                    {
                        var ids = _sqlSugarClient.CopyNew().Queryable<PositionEntity>()
                           .Where(x => x.ParentId.Equals(objId))
                           .Select(x => x.Id).ToList();
                        objIdList.AddRange(ids);
                    }
                    if (type == "progenyPos")
                    {
                        var idTreeList = _sqlSugarClient.CopyNew().Queryable<PositionEntity>()
                            .Where(x => x.PositionIdTree.Contains(objId))
                            .Select(x => x.Id).ToList();
                        objIdList.AddRange(idTreeList);
                    }
                    if (type == "role")
                    {
                        var ids = _sqlSugarClient.CopyNew().Queryable<RoleRelationEntity>().Where(x => x.RoleId == objId).Select(x => x.ObjectId).ToList();
                        objIdList.AddRange(ids);
                    }
                    objIdList.Add(objId);
                }
                else
                {
                    if (item == "@userId")
                    {
                        objIdList.Add(_userManager.UserId);
                    }
                    else
                    {
                        var ids = _sqlSugarClient.CopyNew().Queryable<RoleRelationEntity>().Where(x => x.RoleId == item).Select(x => x.ObjectId).ToList();
                        objIdList.AddRange(ids);
                        objIdList.Add(item);
                    }
                }
            }
            return _sqlSugarClient.CopyNew().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.UserId == b.Id))
                  .Where((a, b) => (objIdList.Contains(a.ObjectId) || objIdList.Contains(a.UserId)) && b.DeleteMark == null && b.EnabledMark == 1).Select(a => a.UserId).Distinct().ToList();
        }
        else
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取子表数据.
    /// </summary>
    /// <param name="dicList"></param>
    /// <param name="subTable"></param>
    /// <returns></returns>
    public List<Dictionary<string, object>> GetSubTableData(List<Dictionary<string, object>> dicList, string subTable)
    {
        var output = new List<Dictionary<string, object>>();
        foreach (var item in dicList)
        {
            var dic = item.ToDictionary(x => subTable + "-" + x.Key, y => y.Value);
            dic["id"] = item.GetValueOrDefault("id");
            dic["jnpfsubtable"] = subTable;
            output.Add(dic);
        }
        return output;
    }

    /// <summary>
    /// 替换全局参数.
    /// </summary>
    /// <param name="nextNodePro"></param>
    /// <param name="wfParamter"></param>
    public void ReplaceInterFaceGlobalParamter(EventProperties nextNodePro, WorkFlowParamter wfParamter)
    {
        if (wfParamter.taskEntity.GlobalParameter.IsNotEmptyOrNull())
        {
            var globalParameter = wfParamter.taskEntity.GlobalParameter.ToObject<Dictionary<string, object>>();
            if (nextNodePro.type == WorkFlowNodeTypeEnum.getData && nextNodePro.formType == 3)
            {
                foreach (var item in nextNodePro.interfaceTemplateJson)
                {
                    if (item.sourceType == 5 && globalParameter.ContainsKey(item.relationField.ToString()))
                    {
                        item.relationField = globalParameter[item.relationField.ToString()];
                    }
                }
            }
            if (nextNodePro.type == WorkFlowNodeTypeEnum.dataInterface)
            {
                foreach (var item in nextNodePro.templateJson)
                {
                    if (item.sourceType == 5 && globalParameter.ContainsKey(item.relationField.ToString()))
                    {
                        item.relationField = globalParameter[item.relationField.ToString()];
                    }
                }
            }
        }
    }

    /// <summary>
    /// 远程请求客户端.
    /// </summary>
    /// <param name="requestModeType">请求方式.</param>
    /// <param name="requestAddress">请求地址.</param>
    /// <param name="parameter">请求参数.</param>
    /// <param name="userId">token用户ID.</param>
    /// <param name="tenantId">token租户.</param>
    /// <returns></returns>
    private async Task<RESTfulResult<object>> HttpClient(int requestModeType, string requestAddress, object parameter, string userId, string tenantId)
    {
        var response = new RESTfulResult<object>();

        var dicHerader = new Dictionary<string, object>();
        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(tenantId))
        {
            await _tenantManager.ChangTenant((SqlSugarScope)_sqlSugarClient, tenantId);
        }
        var user = await _sqlSugarClient.CopyNew().Queryable<UserEntity>().FirstAsync(it => it.Id.Equals(userId));

        // 生成实时token
        var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, tenantId);
        dicHerader.Add("Authorization", toKen);
        dicHerader.Add("jnpf-origin", "pc");
        var localAddress = GetLocalAddress();

        var path = string.Format("{0}{1}", localAddress, requestAddress);

        switch (requestModeType)
        {
            // post
            case 1:
                response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PostAsStringAsync()).ToObject<RESTfulResult<object>>();
                break;

            // put
            case 2:
                response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PutAsStringAsync()).ToObject<RESTfulResult<object>>();
                break;

            //Delete
            case 3:
                response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).DeleteAsStringAsync()).ToObject<RESTfulResult<object>>();
                break;
        }

        return response;
    }

    /// <summary>
    /// 获取当前进程地址.
    /// </summary>
    /// <returns></returns>
    private string GetLocalAddress()
    {
        var server = _serviceScope.ServiceProvider.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        return addresses?.FirstOrDefault()?.Replace("[::]", "localhost");
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }
}
