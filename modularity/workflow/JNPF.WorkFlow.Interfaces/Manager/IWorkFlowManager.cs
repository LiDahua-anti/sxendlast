using JNPF.Common.Models.WorkFlow;
using JNPF.WorkFlow.Entitys.Dto.Operator;
using JNPF.WorkFlow.Entitys.Dto.Task;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.WorkFlow.Interfaces.Manager;

public interface IWorkFlowManager
{
    #region 发起

    /// <summary>
    /// 任务详情.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <param name="flowId">流程id.</param>
    /// <param name="opType">操作类型.</param>
    /// <param name="opId">操作id.</param>
    /// <returns></returns>
    Task<TaskInfoOutput> GetTaskInfo(string taskId, string flowId, string opType, string opId = null);

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    Task<WorkFlowParamter> Save(FlowTaskSubmitModel flowTaskSubmitModel);

    /// <summary>
    /// 提交.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    Task<dynamic> Submit(FlowTaskSubmitModel flowTaskSubmitModel);

    /// <summary>
    /// 撤回(发起).
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task RecallLaunch(WorkFlowParamter wfParamter);

    /// <summary>
    /// 催办.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task Press(WorkFlowParamter wfParamter);

    /// <summary>
    /// 撤销.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task Revoke(WorkFlowParamter wfParamter);

    /// <summary>
    /// 撤销审批.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    Task<OperatorOutput> RevokeAudit(WorkFlowParamter wfParamter);

    /// <summary>
    /// 外部节点回调审批.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    Task<OperatorOutput> WebHookAudit(WorkFlowEventLogEntity envntLog, object formData);

    /// <summary>
    /// 外部节点回调.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task Hooks(TaskHooksInput input, string tenantId);
    #endregion

    #region 审批

    /// <summary>
    /// 审批.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="isAuto">是否自动审批.</param>
    /// <returns></returns>
    Task<OperatorOutput> Audit(WorkFlowParamter wfParamter, bool isAuto = false);

    /// <summary>
    /// 退回.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task<dynamic> SendBack(WorkFlowParamter wfParamter, bool isTrigger = false);

    /// <summary>
    /// 撤回(审批).
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="flowTaskOperatorRecordEntity">经办记录.</param>
    Task RecallAudit(WorkFlowParamter wfParamter, WorkFlowRecordEntity flowTaskOperatorRecordEntity);

    /// <summary>
    /// 转办.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task Transfer(WorkFlowParamter wfParamter);

    /// <summary>
    /// 获取候选人.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">操作参数.</param>
    /// <param name="type">0:候选节点编码，1：候选人.</param>
    /// <returns></returns>
    Task<dynamic> GetCandidateModelList(string operatorId, WorkFlowHandleModel handleModel, int type = 0);

    /// <summary>
    /// 批量候选人.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <param name="operatorId">经办id.</param>
    /// <returns></returns>
    Task<dynamic> GetBatchCandidate(string flowId, string operatorId, int batchType);

    /// <summary>
    /// 表单数据.
    /// </summary>
    /// <param name="formId">表单id.</param>
    /// <param name="id">实例id.</param>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    Task<object> GetFormData(string formId, string id, string flowId = "");

    /// <summary>
    /// 退回节点列表.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <returns></returns>
    Task<dynamic> SendBackNodeList(string operatorId);

    /// <summary>
    /// 加签.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task AddSign(WorkFlowParamter wfParamter);

    /// <summary>
    /// 协办.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="isSave">是否保存协办.</param>
    /// <returns></returns>
    Task Assist(WorkFlowParamter wfParamter, bool isSave = false);

    /// <summary>
    /// 减签.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="input">审批参数.</param>
    /// <returns></returns>
    Task ReduceSign(string operatorId, [FromBody] TaskBatchInput input);
    #endregion

    #region 监控

    /// <summary>
    /// 终止.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    Task Cancel(WorkFlowParamter wfParamter);

    /// <summary>
    /// 指派.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task Assigned(WorkFlowParamter wfParamter, bool isAutoTransfer = false);

    /// <summary>
    /// 复活.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task Activate(WorkFlowParamter wfParamter);

    /// <summary>
    /// 暂停.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task Pause(WorkFlowParamter wfParamter);

    /// <summary>
    /// 恢复.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    Task Reboot(WorkFlowParamter wfParamter);

    #endregion

    #region 其他

    /// <summary>
    /// 详情操作验证.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">操作参数.</param>
    /// <returns></returns>
    Task<WorkFlowParamter> Validation(string operatorId, WorkFlowHandleModel handleModel);

    /// <summary>
    /// 删除任务.
    /// </summary>
    /// <param name="taskEntityList"></param>
    /// <returns></returns>
    Task DeleteTask(List<WorkFlowTaskEntity> taskEntityList);

    /// <summary>
    /// 执行超时提醒配置.
    /// </summary>
    /// <param name="nodePro">节点属性.</param>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="nodeCode">节点编码.</param>
    /// <param name="count">执行次数.</param>
    /// <param name="isTimeOut">是否超时.</param>
    /// <param name="isAtOnce">是否只执行一次.</param>
    /// <returns></returns>
    Task NotifyEvent(NodeProperties nodePro, WorkFlowParamter wfParamter, string nodeCode, int count, bool isTimeOut, bool isAtOnce = false);

    /// <summary>
    /// 通过类型获取对应人员.
    /// </summary>
    /// <param name="opId">操作id.</param>
    /// <param name="handleModel">操作参数.</param>
    /// <param name="type">操作类型.</param>
    /// <returns></returns>
    public dynamic GetUserIdList(string opId, WorkFlowHandleModel handleModel, int type = 0);

    /// <summary>
    /// 定时触发任务.
    /// </summary>
    /// <param name="triggerProperties"></param>
    /// <param name="flowId"></param>
    /// <returns></returns>
    Task TimeTriggerTask(TriggerProperties triggerProperties, string flowId);

    /// <summary>
    /// 自动审批.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="isTimeOut"></param>
    /// <param name="isRevoke"></param>
    /// <returns></returns>
    Task AutoAudit(WorkFlowParamter wfParamter, bool isTimeOut = false, bool isRevoke = false);

    /// <summary>
    /// 获取触发任务详情.
    /// </summary>
    /// <param name="triggerId"></param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetTriggerTaskInfo(string triggerId);
    #endregion
}
