using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Extras.Thirdparty.JSEngine;
using JNPF.FriendlyException;
using JNPF.Systems.Interfaces.Permission;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using JNPF.WorkFlow.Factory;
using JNPF.WorkFlow.Interfaces.Repository;
using Newtonsoft.Json.Linq;
using NPOI.Util;
using System.Text;

namespace JNPF.WorkFlow.Manager;

public class WorkFlowNodeUtil
{
    private readonly IWorkFlowRepository _repository;
    private readonly IDataBaseManager _dataBaseManager;
    private readonly IUserManager _userManager;
    private readonly IUsersService _usersService;

    public WorkFlowNodeUtil(IWorkFlowRepository repository, IDataBaseManager dataBaseManager, IUserManager userManager, IUsersService usersService)
    {
        _repository = repository;
        _dataBaseManager = dataBaseManager;
        _userManager = userManager;
        _usersService = usersService;
    }

    /// <summary>
    /// 获取下一节点.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="nextNodeList"></param>
    /// <param name="skipSubFlow"></param>
    /// <returns></returns>
    public async Task GetNextNodeList(WorkFlowParamter wfParamter, List<WorkFlowNodeModel> nextNodeList, bool skipSubFlow = false)
    {
        // 条件变量
        var variables = await GetConditionVariables(wfParamter);
        foreach (var item in variables.Keys)
        {
            if (!wfParamter.globalPro.connectList.Contains(item)) continue;
            if (variables[item])
            {
                var nextNodeCode = await BpmnEngineFactory.CreateBmpnEngine().GetLineNextNode(wfParamter.engineId, item);
                var nextNode = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == nextNodeCode);
                if (nextNode.IsNotEmptyOrNull())
                {
                    if (!nextNodeList.Any(x => x.nodeCode == nextNode.nodeCode))
                    {
                        nextNodeList.Add(nextNode);
                    }
                    if ((WorkFlowNodeTypeEnum.subFlow.ToString().Equals(nextNode.nodeType) || WorkFlowNodeTypeEnum.outside.ToString().Equals(nextNode.nodeType)) && skipSubFlow)
                    {
                        var wfParamterNew = wfParamter.Copy();
                        wfParamterNew.node = nextNode;
                        wfParamterNew.nodePro = nextNode.nodePro;
                        await GetNextNodeList(wfParamterNew, nextNodeList, skipSubFlow);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取下一节点条件变量.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, bool>> GetConditionVariables(WorkFlowParamter wfParamter)
    {
        var variables = new Dictionary<string, bool>();
        var conditionCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetLineCode(wfParamter.engineId, wfParamter.node.nodeCode, wfParamter.taskEntity?.Id);
        var exclusiveFlag = true;
        if (!conditionCodeList.Any()) return variables;
        var defaultCondition = string.Empty; // 默认条件.
        foreach (var item in conditionCodeList)
        {
            var conditionNode = _repository.GetNodeInfo(x => x.FlowId == wfParamter.flowInfo.flowId && x.NodeCode == item);
            if (conditionNode.IsNotEmptyOrNull())
            {
                var conditionNodePro = conditionNode.NodeJson.ToObject<ConditionProperties>();
                if (conditionNodePro.isDefault)
                {
                    defaultCondition = item;
                }
                switch (wfParamter.nodePro.divideRule)
                {
                    case "inclusion": // 包容
                        if (conditionNodePro.conditions.Any())
                        {
                            var isMatching = ConditionNodeJudge(wfParamter, conditionNodePro);
                            variables.Add(item, isMatching);
                        }
                        else
                        {
                            variables.Add(item, true);
                        }
                        break;
                    case "exclusive": // 排他
                        if (exclusiveFlag)
                        {
                            if (conditionNodePro.conditions.Any())
                            {
                                var isMatching = ConditionNodeJudge(wfParamter, conditionNodePro);
                                if (isMatching)
                                {
                                    exclusiveFlag = false;
                                }
                                variables.Add(item, isMatching);
                            }
                            else
                            {
                                variables.Add(item, true);
                                exclusiveFlag = false;
                            }
                        }
                        else
                        {
                            variables.Add(item, false);
                        }
                        break;
                    case "choose": // 选择
                        if (wfParamter.branchList.Any() && wfParamter.globalPro.connectList.Contains(item))
                        {
                            var nextNodeCode = await BpmnEngineFactory.CreateBmpnEngine().GetLineNextNode(wfParamter.engineId, item);
                            if (wfParamter.branchList.Contains(nextNodeCode) || wfParamter.nodeList.Any(x => x.nodeCode == nextNodeCode && x.nodeType == WorkFlowNodeTypeEnum.trigger.ToString()))
                            {
                                variables.Add(item, true);
                            }
                            else
                            {
                                variables.Add(item, false);
                            }
                        }
                        else
                        {
                            variables.Add(item, true);
                        }
                        break;
                    default: // 并行
                        variables.Add(item, true);
                        break;
                }
            }
            else
            {
                if (wfParamter.branchList.Any() && wfParamter.globalPro.connectList.Contains(item))
                {
                    var nextNodeCode = await BpmnEngineFactory.CreateBmpnEngine().GetLineNextNode(wfParamter.engineId, item);
                    if (wfParamter.branchList.Contains(nextNodeCode) || wfParamter.nodeList.Any(x => x.nodeCode == nextNodeCode && x.nodeType == WorkFlowNodeTypeEnum.trigger.ToString()))
                    {
                        variables.Add(item, true);
                    }
                    else
                    {
                        variables.Add(item, false);
                    }
                }
                else
                {
                    variables.Add(item, true);
                }
            }
        }

        if (wfParamter.globalPro.connectList != null && wfParamter.globalPro.connectList.Any())
        {
            var dic = wfParamter.globalPro.connectList.Where(key => variables.ContainsKey(key)).ToDictionary(key => key, key => variables[key]);
            if (!dic.ContainsValue(true))
            {
                if (defaultCondition.IsNotEmptyOrNull())
                {
                    variables[defaultCondition] = true;
                }
                else
                {
                    throw Oops.Oh(ErrorCode.WF0021);
                }
            }
        }
        else
        {
            if (!variables.ContainsValue(true) || (variables.Keys.Count > 1 && variables.Count(x => x.Value) == 1))
            {
                if (defaultCondition.IsNotEmptyOrNull())
                {
                    variables[defaultCondition] = true;
                }
                else
                {
                    throw Oops.Oh(ErrorCode.WF0021);
                }
            }
        }
        return variables;
    }

    /// <summary>
    /// 驳回处理.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <returns></returns>
    public async Task<List<WorkFlowNodeModel>> RejectManager(WorkFlowParamter wfParamter)
    {
        var rejectNodeList = wfParamter.nodeList.FindAll(x => wfParamter.backNodeCode.Contains(x.nodeCode));
        if (!rejectNodeList.Any()) throw Oops.Oh(ErrorCode.WF0032);
        // 驳回节点下所有节点.
        var nodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().AfterNode(wfParamter.taskEntity.EngineId, wfParamter.backNodeCode);
        // 保存驳回现有数据.
        if (wfParamter.nodePro.backType == 2)
        {
            wfParamter.taskEntity.RejectDataId = _repository.CreateRejectData(wfParamter.taskEntity.Id, string.Join(",", nodeCodeList), wfParamter.backNodeCode);
        }

        var rejectNodeNextAllList = wfParamter.nodeList.FindAll(x => nodeCodeList.Contains(x.nodeCode));
        // 驳回到发起
        if (rejectNodeList.Any(x => WorkFlowNodeTypeEnum.start.ToString().Equals(x.nodeType)))
        {
            wfParamter.taskEntity.CurrentNodeCode = rejectNodeList.FirstOrDefault().nodeCode;
            wfParamter.taskEntity.CurrentNodeName = "开始";
            wfParamter.taskEntity.Urgent = 0;
            wfParamter.taskEntity.Status = WorkFlowTaskStatusEnum.SendBack.ParseToInt();
        }
        else
        {
            // 清空驳回节点下所有节点候选人/异常节点处理人
            if (wfParamter.nodePro.backType == 1)
            {
                _repository.DeleteCandidates(x => rejectNodeNextAllList.Select(x => x.nodeCode).Contains(x.NodeCode));
            }

            var rejectNodeIds = rejectNodeList.Union(rejectNodeNextAllList).ToList().Select(x => x.nodeCode).ToList();
            // 删除驳回节点下所有经办
            var rejectList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && rejectNodeIds.Contains(x.NodeCode)).OrderBy(x => x.HandleTime).Select(x => x.Id).ToList();
            _repository.DeleteOperator(x => rejectList.Contains(x.Id));
            //删除驳回节点经办记录
            var rejectRecodeList = _repository.GetRecordList(x => x.TaskId == wfParamter.taskEntity.Id && rejectNodeIds.Contains(x.NodeCode)).OrderBy(x => x.HandleTime).Select(x => x.Id).ToList();
            _repository.DeleteRecord(rejectRecodeList);
            _repository.DeleteEventLog(x => x.TaskId == wfParamter.taskEntity.Id && rejectNodeIds.Contains(x.NodeCode));
            if (wfParamter.nodePro.backType == 1)
            {
                _repository.DeleteLaunchUser(x => x.TaskId == wfParamter.taskEntity.Id && rejectNodeIds.Contains(x.NodeCode));
            }
        }
        return rejectNodeNextAllList.FindAll(x => WorkFlowNodeTypeEnum.subFlow.ToString().Equals(x.nodeType));
    }

    /// <summary>
    /// 流程异常补偿.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task Compensation(string taskId)
    {
        var taskEntity = _repository.GetTaskInfo(taskId);
        if (taskEntity.IsNotEmptyOrNull())
        {
            var nodeList = await BpmnEngineFactory.CreateBmpnEngine().Compensation(taskEntity.InstanceId, taskEntity.CurrentNodeCode);
            if (nodeList.Any())
            {
                var nodeCode = nodeList.Select(x => x.taskKey);
                var operatorList = _repository.GetOperatorList(x => x.TaskId == taskEntity.Id && nodeCode.Contains(x.NodeCode));
                foreach (var item in operatorList)
                {
                    if (nodeList.Any(x => x.taskKey == item.NodeCode))
                    {
                        item.NodeId = nodeList.FirstOrDefault(x => x.taskKey == item.NodeCode).taskId;
                    }
                }
                var eventLogList = _repository.GetEventLogList(x => x.TaskId == taskEntity.Id && nodeCode.Contains(x.NodeCode));
                foreach (var item in eventLogList)
                {
                    if (nodeList.Any(x => x.taskKey == item.NodeCode))
                    {
                        item.NodeId = nodeList.FirstOrDefault(x => x.taskKey == item.NodeCode).taskId;
                        _repository.UpdateEventLog(item);
                    }
                }
                _repository.UpdateOperator(operatorList);
                var subTaskList = _repository.GetTaskList(x => x.ParentId == taskEntity.Id && nodeCode.Contains(x.SubCode));
                foreach (var item in subTaskList)
                {
                    if (nodeList.Any(x => x.taskKey == item.SubCode))
                    {
                        var subParameter = item.SubParameter.ToObject<Dictionary<string, string>>();
                        subParameter["nodeId"] = nodeList.FirstOrDefault(x => x.taskKey == item.SubCode).taskId;
                        item.SubParameter = subParameter.ToJsonString();
                        _repository.UpdateTask(item);
                    }
                }
                if (nodeList.FirstOrDefault().instanceId.IsNotEmptyOrNull())
                {
                    taskEntity.InstanceId = nodeList.FirstOrDefault().instanceId;
                    _repository.UpdateTask(taskEntity);
                }
            }
        }
    }

    /// <summary>
    /// 保存流程参数.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task SaveGlobalParamter(WorkFlowParamter wfParamter, bool isUpdate = true)
    {
        if (wfParamter.nodePro.parameterList.Any() && wfParamter.taskEntity.GlobalParameter.IsNotEmptyOrNull())
        {
            var globalParamter = wfParamter.taskEntity.GlobalParameter.ToObject<Dictionary<string, object>>();
            foreach (var item in wfParamter.nodePro.parameterList)
            {
                var parameter = item.ToObject<ConditionsItem>();
                if (parameter.IsNotEmptyOrNull())
                {
                    switch (parameter.fieldValueType)
                    {
                        case 1:
                            var formData = wfParamter.formData.ToObject<JObject>();
                            var conditionValue = " ";
                            if (formData.ContainsKey(parameter.fieldValue))
                            {
                                if (formData[parameter.fieldValue] is JArray)
                                {
                                    try
                                    {
                                        var jar = formData[parameter.fieldValue].ToObject<List<string>>();
                                        if (jar.Count > 0)
                                        {
                                            conditionValue = string.Join(",", jar);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        var arr = formData[(string)parameter.fieldValue].ToObject<List<List<string>>>();
                                        conditionValue = string.Join(",", arr.Select(x => string.Join(",", x)).ToList());
                                    }
                                }
                                else
                                {
                                    if (formData[parameter.fieldValue] != null)
                                    {
                                        conditionValue = formData[parameter.fieldValue].ToString();
                                    }
                                    SysWidgetFormValue(wfParamter.taskEntity.Id, parameter.fieldValueJnpfKey, ref conditionValue);
                                }
                            }
                            globalParamter[parameter.field] = conditionValue;
                            break;
                        case 2:
                            globalParamter[parameter.field] = parameter.fieldValue;
                            break;
                        case 3:
                            switch (parameter.fieldValue)
                            {
                                case "@flowOperatorUserId":
                                    globalParamter[parameter.field] = _userManager.UserId;
                                    break;
                                case "@taskFullName":
                                    globalParamter[parameter.field] = wfParamter.taskEntity.FullName;
                                    break;
                                case "@taskId":
                                    globalParamter[parameter.field] = wfParamter.taskEntity.Id;
                                    break;
                                case "@taskNodeId":
                                    globalParamter[parameter.field] = wfParamter.node?.nodeCode;
                                    break;
                                case "@launchUserId":
                                    globalParamter[parameter.field] = wfParamter.taskEntity.CreatorUserId;
                                    break;
                                case "@launchUserName":
                                    globalParamter[parameter.field] = _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId, false);
                                    break;
                                case "@flowId":
                                    globalParamter[parameter.field] = wfParamter.taskEntity.FlowId;
                                    break;
                                case "@flowOperatorUserName":
                                    globalParamter[parameter.field] = _userManager.User.RealName;
                                    break;
                                case "@flowFullName":
                                    globalParamter[parameter.field] = wfParamter.taskEntity.FlowName;
                                    break;
                            }
                            break;
                    }
                }
            }
            wfParamter.taskEntity.GlobalParameter = globalParamter.ToJsonStringOld();
            if (isUpdate)
            {
                _repository.UpdateTask(wfParamter.taskEntity);
            }
        }
    }

    /// <summary>
    /// 撤回按钮显示.
    /// </summary>
    /// <param name="engineId"></param>
    /// <param name="nodeCode"></param>
    /// <param name="nodeList"></param>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task<bool> IsShowRecallBtn(string engineId, string nodeCode, string taskId)
    {
        var recallNextNode = (await BpmnEngineFactory.CreateBmpnEngine().GetNextNode(engineId, nodeCode, string.Empty)).Select(x => x.id).ToList();
        var childTaskList = _repository.GetTaskList(x => x.ParentId == taskId && recallNextNode.Contains(x.SubCode) && x.DeleteMark == null);
        if (childTaskList.Any())
        {
            if (childTaskList.Any(x => x.IsAsync == 1)) return false;
            if (childTaskList.Any(x => x.Status != WorkFlowTaskStatusEnum.Draft.ParseToInt())) return false;
        }
        var operatorList = _repository.GetOperatorList(x => x.TaskId == taskId && recallNextNode.Contains(x.NodeCode) && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
        if (operatorList.Any(x => x.DraftData != null)) return false;
        var opIds = operatorList.Select(x => x.Id).ToList();
        if (_repository.GetRecordList(x => x.TaskId == taskId && opIds.Contains(x.OperatorId)).Any()) return false;
        return true;
    }

    /// <summary>
    /// 跳过触发节点.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="variables"></param>
    /// <returns></returns>
    public async Task SkipTriggerNode(WorkFlowParamter wfParamter, Dictionary<string, bool> variables)
    {
        foreach (var item in variables.Keys)
        {
            if (!wfParamter.globalPro.connectList.Contains(item)) continue;
            if (variables[item])
            {
                var nextNodeCode = await BpmnEngineFactory.CreateBmpnEngine().GetLineNextNode(wfParamter.engineId, item);
                var nextNode = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == nextNodeCode);
                if (nextNode.IsNotEmptyOrNull() && WorkFlowNodeTypeEnum.trigger.ToString().Equals(nextNode.nodeType))
                {
                    variables[item] = false;
                }
            }
        }
    }

    #region 条件判断
    /// <summary>
    /// 条件判断.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="conditionPropertie"></param>
    /// <returns></returns>
    public bool ConditionNodeJudge(WorkFlowParamter wfParamter, ConditionProperties conditionPropertie)
    {
        try
        {
            var flagList = new List<bool>();
            foreach (var item in conditionPropertie.conditions)
            {
                var flag = ConditionNodeJudge(wfParamter, item);
                if (flag)
                {
                    if ("or".Equals(conditionPropertie.matchLogic))
                    {
                        return true;
                    }
                }
                else
                {
                    if ("and".Equals(conditionPropertie.matchLogic))
                    {
                        return false;
                    }
                }
                flagList.Add(flag);
            }
            switch (conditionPropertie.matchLogic)
            {
                case "and":
                    return !flagList.Contains(false);
                case "or":
                    return flagList.Contains(true);
                default:
                    return false;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool ConditionNodeJudge(WorkFlowParamter wfParamter, GropsItem conditions)
    {
        try
        {
            bool flag = false;
            StringBuilder expression = new StringBuilder();
            expression.AppendFormat("select * from base_user where  ");
            var formData = wfParamter.formData.ToObject<JObject>();
            int i = 0;
            foreach (ConditionsItem flowNodeWhereModel in conditions.groups)
            {
                var symbol = flowNodeWhereModel.symbol.Equals("==") ? "=" : flowNodeWhereModel.symbol;
                // 条件值
                var formValue = GetConditionValue(flowNodeWhereModel.fieldType.ParseToInt(), flowNodeWhereModel.field, wfParamter, flowNodeWhereModel.jnpfKey);
                // 匹配值
                var jnpfKey = flowNodeWhereModel.fieldValueType.ParseToInt() == 1 ? flowNodeWhereModel.fieldValueJnpfKey : flowNodeWhereModel.jnpfKey;
                var value = symbol.Equals("notIn") || symbol.Equals("in") ? null : GetConditionValue(flowNodeWhereModel.fieldValueType.ParseToInt(), flowNodeWhereModel.fieldValue, wfParamter, flowNodeWhereModel.jnpfKey, true);
                if (symbol.Equals("=") || symbol.Equals("<>"))
                {
                    expression.AppendFormat("('{0}'{1}'{2}')", formValue, symbol, value);
                }
                else if (symbol.Equals("like"))
                {
                    expression.AppendFormat("('{0}' {1} '%{2}%')", formValue, symbol, value);
                }
                else if (symbol.Equals("notLike"))
                {
                    expression.AppendFormat("('{0}' {1} '%{2}%')", formValue, "not like", value);
                }
                else if (symbol.Equals("null"))
                {
                    if (string.IsNullOrWhiteSpace(formValue))
                    {
                        expression.Append("(1=1)");
                    }
                    else
                    {
                        expression.Append("(1=2)");
                    }
                }
                else if (symbol.Equals("notNull"))
                {
                    if (!string.IsNullOrWhiteSpace(formValue))
                    {
                        expression.Append("(1=1)");
                    }
                    else
                    {
                        expression.Append("(1=2)");
                    }
                }
                else if (symbol.Equals("notIn"))
                {
                    var sqlBuilder = new List<string>();
                    foreach (var item in flowNodeWhereModel.fieldValue)
                    {
                        value = GetConditionValue(flowNodeWhereModel.fieldValueType.ParseToInt(), item.ToString(), wfParamter, flowNodeWhereModel.jnpfKey, true);
                        sqlBuilder.Add(string.Format("('{0}'{1}'{2}')", formValue, "<>", value));
                    }
                    expression.AppendFormat("({0})", string.Join(" and ", sqlBuilder));
                }
                else if (symbol.Equals("in"))
                {
                    var sqlBuilder = new List<string>();
                    foreach (var item in flowNodeWhereModel.fieldValue)
                    {
                        value = GetConditionValue(flowNodeWhereModel.fieldValueType.ParseToInt(), item.ToString(), wfParamter, flowNodeWhereModel.jnpfKey, true);
                        sqlBuilder.Add(string.Format("('{0}'{1}'{2}')", formValue, "=", value));
                    }
                    expression.AppendFormat("({0})", string.Join(" or ", sqlBuilder));
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(formValue) || string.IsNullOrWhiteSpace(value))
                    {
                        expression.Append("(1=2)");
                    }
                    else
                    {
                        expression.AppendFormat("({0}{1}{2})", formValue, symbol, value);
                    }
                }

                if (conditions.logic.IsNotEmptyOrNull() && i != conditions.groups.Count - 1)
                {
                    expression.Append(" " + conditions.logic + " ");
                }

                i++;
            }
            var link = _dataBaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
            flag = _dataBaseManager.WhereDynamicFilter(link, expression.ToString());
            return flag;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取条件匹配值.
    /// </summary>
    /// <param name="type">条件类型 1、字段 2、自定义 3、聚合函数.</param>
    /// <param name="field">关联字段.</param>
    /// <param name="wfParamter"></param>
    /// <param name="jnpfKey">控件key.</param>
    /// <param name="isValueType">是否值类型</param>
    /// <returns></returns>
    private string GetConditionValue(int type, dynamic field, WorkFlowParamter wfParamter, string jnpfKey, bool isValueType = false)
    {
        var conditionValue = " ";
        if (wfParamter.formData.IsNullOrEmpty() && type == 1) return conditionValue;
        if (field == null) return conditionValue;
        var formData = wfParamter.formData.ToObject<JObject>();
        var globalDic = new Dictionary<string, string>();
        if (wfParamter.taskEntity.IsNotEmptyOrNull() && wfParamter.taskEntity.GlobalParameter.IsNotEmptyOrNull())
        {
            globalDic = wfParamter.taskEntity.GlobalParameter.ToObject<Dictionary<string, string>>();
        }
        else
        {
            globalDic = wfParamter.globalPro.globalParameterList.ToDictionary(x => x.fieldName, y => y.defaultValue?.ToString());
        }
        switch (type)
        {
            case 1:
                if (formData.ContainsKey(field))
                {
                    if (formData[field] is JArray)
                    {
                        try
                        {
                            var jar = formData[field].ToObject<List<string>>();
                            if (jar.Count > 0)
                            {
                                conditionValue = string.Join(",", jar);
                            }
                        }
                        catch (Exception)
                        {
                            var arr = formData[(string)field].ToObject<List<List<string>>>();
                            conditionValue = string.Join(",", arr.Select(x => string.Join(",", x)).ToList());
                        }
                    }
                    else
                    {
                        if (formData[field] != null)
                        {
                            conditionValue = formData[field].ToString();
                        }
                        SysWidgetFormValue(wfParamter.taskEntity?.Id, jnpfKey, ref conditionValue);
                    }
                }
                break;
            case 3:
                if (isValueType)
                {
                    switch (field)
                    {
                        case "@flowOperatorUserId":
                            conditionValue = _userManager.UserId;
                            break;
                        case "@taskFullName":
                            conditionValue = wfParamter.taskEntity.FullName;
                            break;
                        case "@taskId":
                            conditionValue = wfParamter.taskEntity.Id;
                            break;
                        case "@taskNodeId":
                            conditionValue = wfParamter.node?.nodeCode;
                            break;
                        case "@launchUserId":
                            conditionValue = wfParamter.taskEntity.CreatorUserId;
                            break;
                        case "@launchUserName":
                            conditionValue = _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId, false);
                            break;
                        case "@flowId":
                            conditionValue = wfParamter.flowInfo.flowId;
                            break;
                        case "@flowOperatorUserName":
                            conditionValue = _userManager.User.RealName;
                            break;
                        case "@flowFullName":
                            conditionValue = wfParamter.flowInfo.fullName;
                            break;
                    }
                }
                else
                {
                    // 获取聚合函数要替换的参数key
                    foreach (var item in StringExtensions.Substring3(field))
                    {
                        if (formData.ContainsKey(item))
                        {
                            field = field.Replace("{" + item + "}", "'" + formData[item] + "'");
                        }
                        else if (item.Contains("tableField") && item.Contains("-"))
                        {
                            var fields = item.Split("-").ToList();
                            var tableField = fields[0];
                            var keyField = fields[1];
                            if (formData.ContainsKey(tableField) && formData[tableField] is JArray)
                            {
                                var jar = formData[tableField] as JArray;

                                var tableValue = jar.Where(x => x.ToObject<JObject>().ContainsKey(keyField)).Select(x => x.ToObject<JObject>()[keyField]).ToObject<List<string>>();
                                var valueStr = string.Join("','", tableValue);
                                field = field.Replace("{" + item + "}", "'" + valueStr + "'");
                            }
                        }
                        else
                        {
                            field = field.Replace("{" + item + "}", "''");
                        }
                    }
                    // 执行函数获取值
                    conditionValue = JsEngineUtil.AggreFunction(field).ToString();
                }
                break;
            case 4:
                if (globalDic.ContainsKey(field) && !string.IsNullOrEmpty(globalDic[field]))
                {
                    conditionValue = globalDic[field];
                }
                break;
            default:
                //数组类型控件
                var jnpfKeyList = new List<string>() { JnpfKeyConst.COMSELECT, JnpfKeyConst.ADDRESS, JnpfKeyConst.CURRORGANIZE };
                if (jnpfKeyList.Contains(jnpfKey) && field is Array && field.Count > 0)
                {
                    if (jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE))
                    {
                        conditionValue = field[field.Count - 1];
                    }
                    else
                    {
                        conditionValue = string.Join(",", field);
                    }
                }
                else
                {
                    conditionValue = field.ToString();
                }

                if ("currentUser".Equals(conditionValue))
                {
                    conditionValue = _userManager.UserId;
                }
                break;
        }
        if (JnpfKeyConst.TIME.Equals(jnpfKey))
        {
            conditionValue = conditionValue.Replace(":", string.Empty);
        }
        return conditionValue;
    }

    /// <summary>
    /// 系统控件条件匹配数据转换.
    /// </summary>
    /// <param name="taskId">任务id</param>
    /// <param name="jnpfKey">条件匹配字段类型</param>
    /// <param name="formValue">条件匹配值</param>
    private void SysWidgetFormValue(string taskId, string jnpfKey, ref string formValue)
    {
        var taskEntity = _repository.GetTaskInfo(taskId);
        if (taskEntity.IsNotEmptyOrNull())
        {
            var creatorUser = _usersService.GetInfoByUserId(taskEntity.CreatorUserId);
            switch (jnpfKey)
            {
                case JnpfKeyConst.CREATEUSER:
                    formValue = taskEntity.CreatorUserId;
                    break;
                case JnpfKeyConst.MODIFYUSER:
                    if (taskEntity.LastModifyUserId.IsNotEmptyOrNull())
                    {
                        formValue = _userManager.UserId;
                    }
                    break;
                case JnpfKeyConst.CURRORGANIZE:
                    if (creatorUser.OrganizeId.IsNotEmptyOrNull())
                    {
                        formValue = creatorUser.OrganizeId;
                    }
                    break;
                case JnpfKeyConst.CREATETIME:
                    formValue = ((DateTime)taskEntity.CreatorTime).ParseToUnixTime().ToString();
                    break;
                case JnpfKeyConst.MODIFYTIME:
                    if (formValue.IsNullOrEmpty())
                    {
                        formValue = taskEntity.LastModifyTime.IsNotEmptyOrNull() ? ((DateTime)taskEntity.LastModifyTime).ParseToUnixTime().ToString() : DateTime.Now.ParseToUnixTime().ToString();
                    }
                    else
                    {
                        formValue = formValue.ParseToDateTime().ParseToUnixTime().ToString();
                    }
                    break;
                case JnpfKeyConst.CURRPOSITION:
                    if (creatorUser.PositionId.IsNotEmptyOrNull())
                    {
                        formValue = creatorUser.PositionId;
                    }
                    break;
            }
        }
        else
        {
            switch (jnpfKey)
            {
                case JnpfKeyConst.CREATEUSER:
                    formValue = _userManager.UserId;
                    break;
                case JnpfKeyConst.MODIFYUSER:
                    formValue = " ";
                    break;
                case JnpfKeyConst.CURRORGANIZE:
                    if (_userManager.User.OrganizeId.IsNotEmptyOrNull())
                    {
                        formValue = _userManager.User.OrganizeId;
                    }
                    break;
                case JnpfKeyConst.CREATETIME:
                    formValue = DateTime.Now.ParseToUnixTime().ToString();
                    break;
                case JnpfKeyConst.MODIFYTIME:
                    formValue = "0";
                    break;
                case JnpfKeyConst.CURRPOSITION:
                    if (_userManager.User.PositionId.IsNotEmptyOrNull())
                    {
                        formValue = _userManager.User.PositionId;
                    }
                    break;
            }
        }
    }
    #endregion
}
