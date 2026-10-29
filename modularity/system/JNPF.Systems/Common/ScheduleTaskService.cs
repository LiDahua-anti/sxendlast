using JNPF.Common.Core.EventBus.Sources;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Extension;
using JNPF.Common.Models;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.EventBus;
using JNPF.Systems.Entitys.Entity.System;
using JNPF.Systems.Interfaces.System;
using JNPF.TaskScheduler.Entitys;
using JNPF.TaskScheduler.Interfaces.TaskScheduler;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Manager;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems.Common;

/// <summary>
/// 定时任务(内部调用).
/// </summary>
[ApiDescriptionSettings(Name = "ScheduleTask", Order = 306)]
[Route("[controller]")]
public class ScheduleTaskService : IDynamicApiController, ITransient
{
    private readonly IScheduleService _scheduleService;
    private readonly IWorkFlowManager _flowTaskManager;
    private readonly ITimeTaskService _timeTaskService;
    private readonly IDataInterfaceService _dataInterfaceService;
    private readonly IEventPublisher _eventPublisher;

    public ScheduleTaskService(
      IScheduleService scheduleService,
      IWorkFlowManager flowTaskManager,
      ITimeTaskService timeTaskService,
      IEventPublisher eventPublisher,
      IDataInterfaceService dataInterfaceService)
    {
        _scheduleService = scheduleService;
        _flowTaskManager = flowTaskManager;
        _timeTaskService = timeTaskService;
        _dataInterfaceService = dataInterfaceService;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// 定时任务.
    /// </summary>
    /// <param name="taskCode"></param>
    /// <param name="scheduleTaskModel"></param>
    /// <returns></returns>
    [HttpPost("{taskCode}")]
    public async Task<dynamic> ScheduleTask(string taskCode, [FromBody] ScheduleTaskModel scheduleTaskModel)
    {
        switch (taskCode)
        {
            case "schedule":
                var scheduleEntity = scheduleTaskModel.taskParams["entity"].ToObject<ScheduleEntity>();
                var userList = scheduleTaskModel.taskParams["userList"].ToObject<List<string>>();
                var type = scheduleTaskModel.taskParams["type"].ToString();
                var enCode = scheduleTaskModel.taskParams["enCode"].ToString();
                await _scheduleService.SendScheduleMsg(scheduleEntity, userList, type, enCode);
                break;
            case "flowtask":
                var nodePro = scheduleTaskModel.taskParams["nodePro"].ToObject<NodeProperties>();
                var wfParamter = scheduleTaskModel.taskParams["wfParamter"].ToObject<WorkFlowParamter>();
                var nodeCode = scheduleTaskModel.taskParams["nodeCode"].ToString();
                var count = scheduleTaskModel.taskParams["count"].ParseToInt();
                var isTimeOut = scheduleTaskModel.taskParams["isTimeOut"].ParseToBool();
                var isAtOnce = scheduleTaskModel.taskParams["isAtOnce"].ParseToBool();
                await _flowTaskManager.NotifyEvent(nodePro, wfParamter, nodeCode, count, isTimeOut, isAtOnce);
                break;
            case "autoaudit":
                var wfParamter1 = scheduleTaskModel.taskParams["wfParamter"].ToObject<WorkFlowParamter>();
                await _flowTaskManager.AutoAudit(wfParamter1);
                break;
            case "sendback":
                var wfParamter2 = scheduleTaskModel.taskParams["wfParamter"].ToObject<WorkFlowParamter>();
                await _flowTaskManager.SendBack(wfParamter2, true);
                break;
            case "timetask":
                var entity = scheduleTaskModel.taskParams["entity"].ToObject<TimeTaskEntity>();
                await _timeTaskService.PerformJob(entity);
                break;
            case "datainterface":
                var id = scheduleTaskModel.taskParams["id"].ToString();
                var input = scheduleTaskModel.taskParams["input"].ToString().ToObject<DataInterfacePreviewInput>();
                _dataInterfaceService.GetDatainterfaceParameter(input);
                return await _dataInterfaceService.GetDataInterfaceData(id, input, 3);
            case "taskflow":
                var tenantId = scheduleTaskModel.taskParams["tenantId"].ToString();
                var userId = scheduleTaskModel.taskParams["userId"].ToString();
                var modelId = scheduleTaskModel.taskParams["modelId"].ToString();
                var model = new TaskFlowEventModel();
                model.TenantId = tenantId;
                model.UserId = userId;
                model.ModelId = modelId;
                model.TriggerType = "timeTrigger";
                model.taskFlowData = new List<Dictionary<string, object>>();
                var workName = string.Format("JobTaskFlow_Http_{0}_{1}", tenantId, modelId);
                await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTask", model));
                break;
            case "outside":
                var formData = scheduleTaskModel.taskParams["formData"];
                var envntLog = scheduleTaskModel.taskParams["envntLog"].ToObject<WorkFlowEventLogEntity>();
                return await _flowTaskManager.WebHookAudit(envntLog, formData);
            default:
                break;
        }
        return string.Empty;
    }
}
