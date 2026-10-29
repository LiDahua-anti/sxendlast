using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.RemoteRequest;
using JNPF.WorkFlow.Entitys.Dto.Operator;
using JNPF.WorkFlow.Entitys.Dto.Task;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Interfaces.Manager;
using JNPF.WorkFlow.Interfaces.Repository;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.WorkFlow.Service;

/// <summary>
/// 流程审批.
/// </summary>
[ApiDescriptionSettings(Tag = "WorkFlowOperator", Name = "Operator", Order = 303)]
[Route("api/workflow/[controller]")]
public class OperatorService : IDynamicApiController, ITransient
{
    private readonly IWorkFlowRepository _repository;
    private readonly IWorkFlowManager _workFlowManager;
    private readonly IUserManager _userManager;

    public OperatorService(IWorkFlowRepository repository, IWorkFlowManager workFlowManager, IUserManager userManager)
    {
        _repository = repository;
        _workFlowManager = workFlowManager;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <param name="category">分类.</param>
    /// <returns></returns>
    [HttpGet("List/{category}")]
    public async Task<dynamic> GetList([FromQuery] OperatorListQuery input, string category)
    {
        try
        {
            switch (category)
            {
                case "0": // 待签
                    return _repository.GetWaitSignList(input);
                case "1": // 待办
                    return _repository.GetSignList(input);
                case "2": // 在办
                    return _repository.GetWaitList(input);
                case "3": // 已办
                    return _repository.GetTrialList(input);
                case "4": // 抄送
                    return _repository.GetCirculateList(input);
                case "5": // 批量在办
                    return _repository.GetBatchWaitList(input);
                default:
                    var pageList = new SqlSugarPagedList<OperatorListOutput>()
                    {
                        list = new List<OperatorListOutput>(),
                        pagination = new Pagination()
                        {
                            CurrentPage = input.currentPage,
                            PageSize = input.pageSize,
                            Total = 0
                        }
                    };
                    return PageResult<OperatorListOutput>.SqlSugarPageResult(pageList);
            }
        }
        catch (Exception)
        {
            var pageList = new SqlSugarPagedList<OperatorListOutput>()
            {
                list = new List<OperatorListOutput>(),
                pagination = new Pagination()
                {
                    CurrentPage = input.currentPage,
                    PageSize = input.pageSize,
                    Total = 0
                }
            };
            return PageResult<OperatorListOutput>.SqlSugarPageResult(pageList);
        }
    }

    /// <summary>
    /// 批量审批流程分类列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("BatchFlowSelector")]
    public async Task<dynamic> BatchFlowSelector()
    {
        var list = _repository.GetFlowTaskList();
        var output = new List<FlowItem>();
        foreach (var item in list.GroupBy(x => x.templateId))
        {
            output.Add(new FlowItem
            {
                id = item.Key,
                fullName = string.Format("{0}({1})", item.FirstOrDefault().flowName, item.Count()),
                count = item.Count()
            });
        };
        return output.OrderByDescending(x => x.count);
    }

    /// <summary>
    /// 批量审批流程版本列表.
    /// </summary>
    /// <param name="templateId"></param>
    /// <returns></returns>
    [HttpGet("BatchVersionSelector/{templateId}")]
    public async Task<dynamic> BatchFlowJsonList(string templateId)
    {
        var templateEntity = _repository.GetTemplate(templateId);
        return _repository.GetFlowList(x => x.TemplateId == templateId && x.Status != 0 && x.DeleteMark == null).Select(x => new { id = x.Id, fullName = string.Format("{0}(v{1})", templateEntity.FullName, x.Version) }).ToList();
    }

    /// <summary>
    /// 批量审批节点列表.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    [HttpGet("BatchNodeSelector/{flowId}")]
    public async Task<dynamic> NodeSelector(string flowId)
    {
        var flowInfo = _repository.GetFlowInfo(flowId);
        return _repository.GetNodeList(x => x.FlowId == flowId && x.DeleteMark == null && x.NodeType == WorkFlowNodeTypeEnum.approver.ParseToString()).Select(x => new { id = x.NodeCode, fullName = x.NodeJson.ToObject<NodeProperties>().nodeName }).ToList();
    }

    /// <summary>
    /// 退回节点列表.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <returns></returns>
    [HttpGet("SendBackNodeList/{operatorId}")]
    public async Task<dynamic> SendBackNodeList(string operatorId)
    {
        return await _workFlowManager.SendBackNodeList(operatorId);
    }

    /// <summary>
    /// 批量审批候选人.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <param name="operatorId">经办id.</param>
    /// <returns></returns>
    [HttpGet("BatchCandidate")]
    public async Task<dynamic> GetBatchCandidate([FromQuery] string flowId, [FromQuery] string operatorId, [FromQuery] int batchType)
    {
        await _workFlowManager.Validation(operatorId, new WorkFlowHandleModel());
        return await _workFlowManager.GetBatchCandidate(flowId, operatorId, batchType);
    }

    /// <summary>
    /// 批量审批节点属性.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <param name="nodeCode">节点编码.</param>
    /// <returns></returns>
    [HttpGet("BatchNode")]
    public async Task<dynamic> BatchNode([QueryString] string flowId, [QueryString] string nodeCode)
    {
        var node = _repository.GetNodeInfo(x => x.FlowId == flowId && x.NodeCode == nodeCode);
        if (node == null) throw Oops.Oh(ErrorCode.COM1007);
        return node.NodeJson.ToObject<Dictionary<string, object>>();
    }

    /// <summary>
    /// 验证站内信详情是否有查看权限.
    /// </summary>
    /// <param name="taskOperatorId">经办id.</param>
    /// <returns></returns>
    [HttpGet("{operatorId}/Info")]
    public async Task<dynamic> IsInfo(string operatorId, [FromQuery] int opType)
    {
        var circulateEntity = _repository.GetCirculateInfo(x => x.Id == operatorId && x.DeleteMark == null);
        if (circulateEntity.IsNotEmptyOrNull())
        {
            return new { opType = 5 };
        }
        else
        {
            var operatorEntity = _repository.GetOperatorInfo(x => x.Id == operatorId);
            var taskEntity = new WorkFlowTaskEntity();
            if (operatorEntity.IsNotEmptyOrNull())
            {
                taskEntity = _repository.GetTaskInfo(operatorEntity.TaskId);
                var template = _repository.GetTemplate(taskEntity.TemplateId, false);
                if (template.IsNullOrEmpty() || template.FlowId.IsNullOrEmpty() || template.Status == 3)
                {
                    throw Oops.Oh(ErrorCode.WF0070);
                }
                if (operatorEntity.HandleId == _userManager.UserId)
                {
                    if (operatorEntity.Status == WorkFlowOperatorStatusEnum.Invalid.ParseToInt() || taskEntity.Status == WorkFlowTaskStatusEnum.Cancel.ParseToInt())
                        throw Oops.Oh(ErrorCode.WF0029);
                }
                else
                {
                    var toUserId = _repository.GetDelegateUserId(operatorEntity.HandleId, taskEntity.TemplateId, 1);
                    if (!toUserId.Contains(_userManager.UserId) || operatorEntity.Status == WorkFlowOperatorStatusEnum.Invalid.ParseToInt() || taskEntity.Status == WorkFlowTaskStatusEnum.Cancel.ParseToInt())
                        throw Oops.Oh(ErrorCode.WF0029);
                }
                // true 跳转抄送页面 false 审批页面
                if (operatorEntity.SignTime.IsNullOrEmpty())
                {
                    opType = 1;
                }
                else if (operatorEntity.StartHandleTime.IsNullOrEmpty())
                {
                    opType = 2;
                }
                else if (operatorEntity.Completion == 0)
                {
                    opType = 3;
                }
                else
                {
                    opType = 5;
                }
                return new { opType = opType };
            }
            else
            {
                taskEntity = _repository.GetTaskInfo(operatorId);
                if (taskEntity.IsNotEmptyOrNull())
                {
                    var template = _repository.GetTemplate(taskEntity.TemplateId, false);
                    if (template.IsNullOrEmpty() || template.FlowId.IsNullOrEmpty() || template.Status == 3)
                    {
                        throw Oops.Oh(ErrorCode.WF0070);
                    }
                    opType = taskEntity.Status == WorkFlowTaskStatusEnum.Draft.ParseToInt() ? -1 : opType;
                }
                else
                {
                    throw Oops.Oh(ErrorCode.WF0029);
                }
                return new { opType = opType };
            }
        }
    }

    /// <summary>
    /// 进度节点经办列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    [HttpGet("RecordList")]
    public async Task<dynamic> GetRecordList([QueryString] string taskId, [QueryString] string nodeId)
    {
        var list = _repository.GetRecordModelList(taskId).FindAll(x => x.nodeId == nodeId).OrderByDescending(x => x.handleTime);
        foreach (var item in list)
        {
            item.approvalField = item.expandField?.ToObject<List<object>>();
            var userNameList = new List<string>();
            if (item.handleUserName.IsNotEmptyOrNull())
            {
                foreach (var userId in item.handleUserName.Split(","))
                {
                    var name = _userManager.GetUserName(userId);
                    if (name.IsNotEmptyOrNull())
                        userNameList.Add(name);
                }
                item.handleUserName = string.Join(",", userNameList);
            }
        }
        return list;
    }
    #endregion

    #region Post

    /// <summary>
    /// 候选节点.
    /// </summary>
    /// <param name="operatorId"></param>
    /// <param name="handleModel"></param>
    /// <returns></returns>
    [HttpPost("CandidateNode/{operatorId}")]
    public async Task<dynamic> CandidateNode(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var flowInfo = _repository.GetFlowInfo(handleModel.flowId);
        if (flowInfo.IsNullOrEmpty() || flowInfo.flowId.IsNullOrEmpty())
        {
            var template = _repository.GetTemplate(handleModel.flowId, false);
            if (template.IsNotEmptyOrNull() && template.FlowId.IsNotEmptyOrNull())
            {
                if ((operatorId == "0" && template.Status != 1) || (operatorId != "0" && template.Status == 3))
                {
                    throw Oops.Oh(ErrorCode.WF0070);
                }
                handleModel.flowId = template.FlowId;
            }
            else
            {
                throw Oops.Oh(ErrorCode.WF0033);
            }
        }
        else
        {
            if ((operatorId == "0" && flowInfo.status != 1) || (operatorId != "0" && flowInfo.status == 3))
            {
                throw Oops.Oh(ErrorCode.WF0070);
            }
        }
        return await _workFlowManager.GetCandidateModelList(operatorId, handleModel);
    }

    /// <summary>
    /// 候选人.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("CandidateUser/{operatorId}")]
    public async Task<dynamic> CandidateUser(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var flowInfo = _repository.GetFlowInfo(handleModel.flowId);
        if (flowInfo.IsNullOrEmpty() || flowInfo.flowId.IsNullOrEmpty())
        {
            var template = _repository.GetTemplate(handleModel.flowId);
            if (template.IsNotEmptyOrNull() && template.FlowId.IsNotEmptyOrNull()) { handleModel.flowId = template.FlowId; } else { throw Oops.Oh(ErrorCode.WF0033); }
        }
        return await _workFlowManager.GetCandidateModelList(operatorId, handleModel, 1);
    }

    /// <summary>
    /// 签收/退签.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Sign")]
    public async Task Sign([FromBody] TaskBatchInput input)
    {
        var operatorList = _repository.GetOperatorList(x => input.ids.Contains(x.Id));
        var index = 0;
        var index1 = 0;
        foreach (var item in operatorList)
        {
            var taskEntity = _repository.GetTaskInfo(item.TaskId);
            if (taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt()) { ++index1; }
            if (input.type == 0 && taskEntity.Status != WorkFlowTaskStatusEnum.Pause.ParseToInt())
            {
                item.SignTime = DateTime.Now;
            }
            else
            {
                var globalPro = _repository.GetNodeInfo(x => x.FlowId == taskEntity.FlowId && x.NodeType == WorkFlowNodeTypeEnum.global.ParseToString()).NodeJson.ToObject<GlobalProperties>();
                if (globalPro.hasSignFor && taskEntity.Status != WorkFlowTaskStatusEnum.Pause.ParseToInt())
                {
                    item.SignTime = null;
                }
                else
                {
                    ++index;
                }
            }
        }
        if (index1 == operatorList.Count) throw Oops.Oh(ErrorCode.WF0027);
        if (index == operatorList.Count) throw Oops.Oh(ErrorCode.WF0011);
        _repository.UpdateOperator(operatorList);
    }

    /// <summary>
    /// 办理.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Transact")]
    public async Task Transact([FromBody] TaskBatchInput input)
    {
        var operatorList = _repository.GetOperatorList(x => input.ids.Contains(x.Id));
        var index = 0;
        foreach (var item in operatorList)
        {
            var taskEntity = _repository.GetTaskInfo(item.TaskId);
            if (input.type == 0 && taskEntity.Status != WorkFlowTaskStatusEnum.Pause.ParseToInt())
            {
                item.StartHandleTime = DateTime.Now;
                ++index;
            }
        }
        _repository.UpdateOperator(operatorList);
        if (index == 0) throw Oops.Oh(ErrorCode.WF0027);
    }

    /// <summary>
    /// 审批.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Audit/{operatorId}")]
    public async Task<dynamic> Audit(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var wfParamter = await _workFlowManager.Validation(operatorId, handleModel);
        if (_repository.AnyRevoke(x => x.RevokeTaskId == wfParamter.taskEntity.Id && x.DeleteMark == null))
        {
            return await _workFlowManager.RevokeAudit(wfParamter);
        }
        if (wfParamter.operatorEntity.Status == WorkFlowOperatorStatusEnum.Revoke.ParseToInt())
        {
            await _workFlowManager.Revoke(wfParamter);
            return new OperatorOutput();
        }
        return await _workFlowManager.Audit(wfParamter);
    }

    /// <summary>
    /// 加签.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("AddSign/{operatorId}")]
    public async Task AddSign(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var wfParamter = await _workFlowManager.Validation(operatorId, handleModel);
        await _workFlowManager.AddSign(wfParamter);
    }

    /// <summary>
    /// 加签人员(未审批).
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("AddSignUserIdList/{recordId}")]
    public async Task<dynamic> GetAddSignUserIdList(string recordId, [FromBody] WorkFlowHandleModel handleModel)
    {
        return _workFlowManager.GetUserIdList(recordId, handleModel);
    }

    /// <summary>
    /// 减签.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="input">审批参数.</param>
    /// <returns></returns>
    [HttpPost("ReduceApprover/{recordId}")]
    public async Task ReduceSign(string recordId, [FromBody] TaskBatchInput input)
    {
        await _workFlowManager.ReduceSign(recordId, input);
    }

    /// <summary>
    /// 退回.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("SendBack/{operatorId}")]
    public async Task<dynamic> SendBack(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var wfParamter = _repository.GetWorkFlowParamterByOperatorId(operatorId, handleModel);
        if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt()) throw Oops.Oh(ErrorCode.WF0046);
        if (wfParamter.flowInfo.status == 3) throw Oops.Oh(ErrorCode.WF0070);
        if (handleModel.backType.IsNotEmptyOrNull())
        {
            wfParamter.nodePro.backType = handleModel.backType.ParseToInt();
        }
        return await _workFlowManager.SendBack(wfParamter);
    }

    /// <summary>
    /// 撤回(审批).
    /// </summary>
    /// <param name="recordId">经办记录id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Recall/{recordId}")]
    public async Task Recall(string recordId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var flowTaskOperatorRecord = _repository.GetRecordInfo(recordId);
        //撤回经办记录
        if (flowTaskOperatorRecord.Status == WorkFlowOperatorStatusEnum.Invalid.ParseToInt())
            throw Oops.Oh(ErrorCode.WF0010);
        var wfParamter = _repository.GetWorkFlowParamterByOperatorId(flowTaskOperatorRecord.OperatorId, handleModel);
        if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt() || wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.WF0046);
        if (wfParamter.flowInfo.status == 3) throw Oops.Oh(ErrorCode.WF0070);
        if (wfParamter.taskEntity.RejectDataId.IsNotEmptyOrNull()) throw Oops.Oh(ErrorCode.WF0023);
        await _workFlowManager.RecallAudit(wfParamter, flowTaskOperatorRecord);
    }

    /// <summary>
    /// 保存(审批).
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("SaveAudit/{operatorId}")]
    [UnitOfWork]
    public async Task Save(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var wfParamter = await _workFlowManager.Validation(operatorId, handleModel);
        wfParamter.operatorEntity.DraftData = handleModel.formData.ToJsonString();
        _repository.UpdateOperator(wfParamter.operatorEntity);
    }

    /// <summary>
    /// 转办.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Transfer/{operatorId}")]
    [UnitOfWork]
    public async Task Transfer(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var wfParamter = await _workFlowManager.Validation(operatorId, handleModel);
        await _workFlowManager.Transfer(wfParamter);
    }

    /// <summary>
    /// 协办.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("Assist/{operatorId}")]
    [UnitOfWork]
    public async Task Assist(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var wfParamter = await _workFlowManager.Validation(operatorId, handleModel);
        await _workFlowManager.Assist(wfParamter);
    }

    /// <summary>
    /// 批量审批.
    /// </summary>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("BatchOperation")]
    public async Task BatchOperation([FromBody] WorkFlowHandleModel handleModel)
    {
        var list = new List<OperatorOutput>();
        var index = 0;
        var isRevoke = false;
        foreach (var item in handleModel.ids)
        {
            var wfParamter = _repository.GetWorkFlowParamterByOperatorId(item, handleModel);
            if (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Pause.ParseToInt()) continue;
            if (wfParamter.operatorEntity.Status == WorkFlowOperatorStatusEnum.Revoke.ParseToInt() && handleModel.batchType > 1)
            {
                isRevoke = true;
                continue;
            }
            ++index;
            if (wfParamter.operatorEntity.Status != WorkFlowOperatorStatusEnum.Revoke.ParseToInt())
            {
                wfParamter.formData = await _workFlowManager.GetFormData(wfParamter.node.formId, wfParamter.taskEntity.Id, wfParamter.flowInfo.flowId);
                handleModel.formData = wfParamter.formData;
            }
            switch (handleModel.batchType)
            {
                case 0:
                case 1:
                    if (wfParamter.operatorEntity == null)
                        throw Oops.Oh(ErrorCode.COM1005);
                    if (wfParamter.operatorEntity.Completion != 0)
                        throw Oops.Oh(ErrorCode.WF0006);
                    wfParamter.handleStatus = handleModel.batchType == 0 ? 1 : 0;
                    var output = new OperatorOutput();
                    if (wfParamter.operatorEntity.Status == WorkFlowOperatorStatusEnum.Revoke.ParseToInt())
                    {
                        output = await _workFlowManager.RevokeAudit(wfParamter);
                    }
                    else
                    {
                        output = await _workFlowManager.Audit(wfParamter);
                    }
                    if (output.errorCodeList.Any())
                    {
                        list.Add(output);
                    }
                    break;
                case 2:
                    await Transfer(item, handleModel);
                    break;
                case 3:
                    await SendBack(item, handleModel);
                    break;
            }
        }
        if (index == 0)
        {
            if (isRevoke && handleModel.batchType == 2)
            {
                throw Oops.Oh(ErrorCode.WF0059);
            }
            if (isRevoke && handleModel.batchType == 3)
            {
                throw Oops.Oh(ErrorCode.WF0060);
            }
            throw Oops.Oh(ErrorCode.WF0027);
        }
        if (list.Count == handleModel.ids.Count)
            throw Oops.Oh(ErrorCode.WF0025);
    }

    /// <summary>
    /// 协办保存.
    /// </summary>
    /// <param name="operatorId">经办id.</param>
    /// <param name="handleModel">审批参数.</param>
    /// <returns></returns>
    [HttpPost("AssistSave/{operatorId}")]
    [UnitOfWork]
    public async Task AssistSave(string operatorId, [FromBody] WorkFlowHandleModel handleModel)
    {
        var wfParamter = await _workFlowManager.Validation(operatorId, handleModel);
        await _workFlowManager.Assist(wfParamter, true);
    }
    #endregion
}
