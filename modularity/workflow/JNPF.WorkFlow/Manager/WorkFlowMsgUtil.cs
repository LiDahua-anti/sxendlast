using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Dtos.Message;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Message.Interfaces;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Interfaces.Repository;
using Newtonsoft.Json.Linq;

namespace JNPF.WorkFlow.Manager;

public class WorkFlowMsgUtil
{
    private readonly IMessageManager _messageManager;

    public readonly IWorkFlowRepository _repository;

    public readonly IUserManager _userManager;

    public readonly IUsersService _usersService;

    private readonly IDataInterfaceService _dataInterfaceService;

    public WorkFlowMsgUtil(IMessageManager messageManager, IWorkFlowRepository repository, IUserManager userManager, IUsersService usersService, IDataInterfaceService dataInterfaceService)
    {
        _messageManager = messageManager;
        _repository = repository;
        _userManager = userManager;
        _usersService = usersService;
        _dataInterfaceService = dataInterfaceService;
    }

    /// <summary>
    /// 组装消息跳转详情参数.
    /// </summary>
    /// <param name="wfParamter">流程实例.</param>
    /// <param name="userList">通知人员.</param>
    /// <param name="operatorList">经办实例.</param>
    /// <param name="type">0:发起，2：待办，5：抄送.</param>
    /// <param name="remark">备注.</param>
    /// <returns></returns>
    public Dictionary<string, object> GetMesBodyText(WorkFlowParamter wfParamter, List<string> userList, List<WorkFlowOperatorEntity> operatorList, int type, string remark = "")
    {
        var dic = new Dictionary<string, object>();
        switch (type)
        {
            case 2:
                if (operatorList.IsNotEmptyOrNull() && operatorList.Any())
                {
                    foreach (var item in operatorList)
                    {
                        var value1 = new {
                            flowId = wfParamter.taskEntity.FlowId,
                            taskId = item.TaskId,
                            operatorId = item.Id,
                            opType = item.SignTime.IsNullOrEmpty() ? 1 : item.StartHandleTime.IsNullOrEmpty() ? 2 : 3,
                            remark = remark
                        };
                        dic[item.HandleId] = value1;
                        var toUserId = _repository.GetDelegateUserId(item.HandleId, wfParamter.taskEntity.TemplateId, 1);
                        toUserId.ForEach(u => dic[u + "-delegate"] = value1);
                    }
                }
                break;
            case 5:
                if (wfParamter.circulateEntityList.IsNotEmptyOrNull() && wfParamter.circulateEntityList.Any())
                {
                    foreach (var item in wfParamter.circulateEntityList)
                    {
                        var value1 = new {
                            flowId = wfParamter.taskEntity.FlowId,
                            taskId = item.TaskId,
                            operatorId = item.Id,
                            opType = type,
                            remark = remark
                        };
                        dic[item.UserId] = value1;
                    }
                }
                break;
            default:
                var value3 = new {
                    flowId = wfParamter.taskEntity.FlowId,
                    taskId = wfParamter.taskEntity.Id,
                    operatorId = wfParamter.taskEntity.Id,
                    opType = type,
                    remark = remark
                };
                userList.ForEach(u => dic[u] = value3);
                break;
        }
        return dic;
    }

    /// <summary>
    /// 通过消息模板获取消息通知.
    /// </summary>
    /// <param name="msgConfig">消息配置.</param>
    /// <param name="users">通知人员.</param>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="enCode">默认站内信编码.</param>
    /// <param name="bodyDic">跳转数据.</param>
    /// <returns></returns>
    public async Task Alerts(MsgConfig msgConfig, List<string> users, WorkFlowParamter wfParamter, string enCode, Dictionary<string, object> bodyDic = null)
    {
        //自定义消息
        if (msgConfig.on == 1 || msgConfig.on == 2)
        {
            foreach (var item in msgConfig.templateJson)
            {
                item.toUser = users;
                GetMsgContent(item.paramJson, wfParamter);
                await _messageManager.SendDefinedMsg(item, bodyDic);
            }
        }

        //默认消息
        if (msgConfig.on == 3)
        {
            var crUser = _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId, false);
            var paramDic = new Dictionary<string, string>();
            paramDic.Add("@Title", wfParamter.taskEntity.FullName);
            paramDic.Add("@CreatorUserName", crUser);
            var messageList = _messageManager.GetMessageList(enCode, users, paramDic, 2, bodyDic);
            await _messageManager.SendDefaultMsg(users, messageList);
        }
    }

    /// <summary>
    /// 获取消息模板内容.
    /// </summary>
    /// <param name="templateJsonItems">消息模板json.</param>
    /// <param name="wfParamter">任务参数.</param>
    public Dictionary<string, string> GetMsgContent(List<MessageSendParam> templateJsonItems, WorkFlowParamter wfParamter)
    {
        var jObj = wfParamter.formData?.ToObject<JObject>();
        var dic = new Dictionary<string, string>();
        foreach (var item in templateJsonItems)
        {
            var value = string.Empty;
            if (item.relationField.Equals("@flowOperatorUserId"))
            {
                value = _userManager.UserId;
            }
            else if (item.relationField.Equals("@taskId"))
            {
                value = wfParamter.taskEntity.Id;
            }
            else if (item.relationField.Equals("@taskNodeId"))
            {
                value = wfParamter.node?.nodeCode;
            }
            else if (item.relationField.Equals("@taskFullName"))
            {
                value = wfParamter.taskEntity.FullName;
            }
            else if (item.relationField.Equals("@launchUserId"))
            {
                value = wfParamter.taskEntity.CreatorUserId;
            }
            else if (item.relationField.Equals("@launchUserName"))
            {
                value = _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId, false);
            }
            else if (item.relationField.Equals("@flowOperatorUserName"))
            {
                value = _userManager.User.RealName;
            }
            else if (item.relationField.Equals("@flowId"))
            {
                value = wfParamter.taskEntity.FlowId;
            }
            else if (item.relationField.Equals("@flowFullName"))
            {
                value = wfParamter.taskEntity.FlowName;
            }
            else
            {
                if (jObj.IsNotEmptyOrNull())
                {
                    if (item.isSubTable)
                    {
                        var fields = item.relationField.Split("-").ToList();
                        // 子表键值
                        var tableField = fields[0];
                        // 子表字段键值
                        var keyField = fields[1];
                        if (jObj.ContainsKey(tableField) && jObj[tableField] is JArray)
                        {
                            var jar = jObj[tableField] as JArray;

                            value = jar.Where(x => x.ToObject<JObject>().ContainsKey(keyField)).Select(x => x.ToObject<JObject>()[keyField]).ToJsonString();
                        }
                    }
                    else
                    {
                        value = jObj.ContainsKey(item.relationField) ? jObj[item.relationField].ToString() : string.Empty;
                    }
                }
            }
            item.value = value;
            dic.Add(item.field, value);
        }
        if (!templateJsonItems.Any(x => x.field == "@Title"))
        {
            templateJsonItems.Add(new MessageSendParam
            {
                field = "@Title",
                value = wfParamter.taskEntity.FullName
            });
        }
        if (!templateJsonItems.Any(x => x.field == "@CreatorUserName"))
        {
            templateJsonItems.Add(new MessageSendParam
            {
                field = "@CreatorUserName",
                value = _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId)
            });
        }
        return dic;
    }

    /// <summary>
    /// 事件请求.
    /// </summary>
    /// <param name="funcConfig">事件配置.</param>
    /// <param name="flowTaskParamter">表单数据.</param>
    /// <param name="funcConfigEnum">事件类型.</param>
    /// <returns></returns>
    public async Task<bool> RequestEvents(FuncConfig funcConfig, WorkFlowParamter wfParamter, FuncConfigEnum funcConfigEnum)
    {
        var flag = false;
        if (funcConfig.IsNotEmptyOrNull() && funcConfig.on && funcConfig.interfaceId.IsNotEmptyOrNull())
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("@flowId", wfParamter.taskEntity.FlowId);
            parameters.Add("@taskId", wfParamter.taskEntity.Id);
            parameters.Add("@taskNodeId", wfParamter.node?.nodeCode);
            parameters.Add("@flowFullName", wfParamter.taskEntity.FlowName);
            parameters.Add("@taskFullName", wfParamter.taskEntity.FullName);
            parameters.Add("@launchUserId", wfParamter.taskEntity.CreatorUserId);
            parameters.Add("@launchUserName", _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId));
            parameters.Add("@flowOperatorUserId", _userManager.UserId);
            parameters.Add("@flowOperatorUserName", _userManager.User.RealName);
            var globalParameter = wfParamter.taskEntity.GlobalParameter.ToObject<Dictionary<string, object>>();
            foreach (var item in funcConfig.templateJson)
            {
                if (item.sourceType == 5 && globalParameter.ContainsKey(item.relationField.ToString()))
                {
                    item.relationField = globalParameter[item.relationField.ToString()];
                }
            }
            try
            {
                var input = new DataInterfacePreviewInput
                {
                    paramList = funcConfig.templateJson,
                    tenantId = _userManager.TenantId,
                    sourceData = wfParamter.formData,
                    systemParamter = parameters,
                };
                var reuslt = await _dataInterfaceService.GetResponseByType(funcConfig.interfaceId, 4, input);
                if (reuslt.IsNotEmptyOrNull())
                {
                    if (reuslt.ToJsonString().FirstOrDefault().Equals('['))
                    {
                        flag = true;
                    }
                    else
                    {
                        var jobj = reuslt.ToObject<JObject>();
                        flag = jobj.ContainsKey("code") && "200".Equals(jobj["code"].ToString());
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        else
        {
            flag = true;
        }
        return flag;

    }

    /// <summary>
    /// 外部请求.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="eventLog"></param>
    /// <param name="initFormData"></param>
    /// <returns></returns>
    public async Task OutSideRequest(WorkFlowParamter wfParamter, List<WorkFlowEventLogEntity> eventLog, Dictionary<string, object> initFormData)
    {
        var formData = wfParamter.formData.ToObject<Dictionary<string, object>>();
        var globalParameter = wfParamter.taskEntity.GlobalParameter.IsNotEmptyOrNull() ? wfParamter.taskEntity.GlobalParameter.ToObject<Dictionary<string, object>>() : new Dictionary<string, object>();
        var lastNodeCode = wfParamter.node.nodeCode;
        if (WorkFlowNodeTypeEnum.subFlow.ToString().Equals(wfParamter.node.nodeType))
        {
            var lastHandleNode = _repository.GetRecordList(x => x.TaskId == wfParamter.taskEntity.Id && (x.HandleType == 1 || x.HandleType == 2), o => o.HandleTime, SqlSugar.OrderByType.Desc).FirstOrDefault();
            if (lastHandleNode.IsNotEmptyOrNull() && lastHandleNode.NodeCode.IsNotEmptyOrNull())
            {
                lastNodeCode = lastHandleNode.NodeCode;
            }
            else
            {
                lastNodeCode = wfParamter.startPro.nodeId;
            }
        }
        foreach (var item in eventLog)
        {
            var outSideNode = wfParamter.nodeList.Find(x => x.nodeCode == item.NodeCode);
            if (outSideNode.nodePro.formId.IsNotEmptyOrNull())
            {
                var templateJson = outSideNode.nodePro.outsideOptions.ContainsKey(lastNodeCode) ? outSideNode.nodePro.outsideOptions[lastNodeCode] : new List<DataInterfaceParameter>();
                var sourceData = new Dictionary<string, object>();
                foreach (var temp in templateJson)
                {
                    if (temp.sourceType == 1)
                    {
                        if (temp.relationField.IsNullOrEmpty()) continue;
                        if ("@prevNodeFormId".Equals(temp.relationField.ToString()) || "@startNodeFormId".Equals(temp.relationField.ToString()))
                        {
                            temp.sourceType = 2;
                        }
                        else
                        {
                            var fieldList = temp.relationField.ToString().Split("|").ToList();
                            var field = fieldList[0];
                            var nodeCode = fieldList[1];
                            if (wfParamter.node.formId == nodeCode && formData.ContainsKey(field))
                            {
                                sourceData[temp.relationField.ToString()] = formData[field];
                            }
                            if (wfParamter.startPro.formId == nodeCode && initFormData.IsNotEmptyOrNull() && initFormData.ContainsKey(field))
                            {
                                sourceData[temp.relationField.ToString()] = initFormData[field];
                            }
                        }
                    }
                    if (temp.sourceType == 5 && globalParameter.ContainsKey(temp.relationField.ToString()))
                    {
                        temp.relationField = globalParameter[temp.relationField.ToString()];
                    }
                }
                var parameters = new Dictionary<string, object>();
                parameters.Add("@flowId", wfParamter.taskEntity.FlowId);
                parameters.Add("@taskId", wfParamter.taskEntity.Id);
                parameters.Add("@taskNodeId", item.NodeId);
                parameters.Add("@flowFullName", wfParamter.taskEntity.FlowName);
                parameters.Add("@taskFullName", wfParamter.taskEntity.FullName);
                parameters.Add("@launchUserId", wfParamter.taskEntity.CreatorUserId);
                parameters.Add("@launchUserName", _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId));
                parameters.Add("@flowOperatorUserId", _userManager.UserId);
                parameters.Add("@flowOperatorUserName", _userManager.User.RealName);
                parameters.Add("@prevNodeFormId", wfParamter.node.formId);
                parameters.Add("@startNodeFormId", wfParamter.startPro.formId);
                parameters.Add("@formData", wfParamter.formData.ToJsonStringOld());
                parameters.Add("@eventId", item.Id);
                var input = new DataInterfacePreviewInput
                {
                    paramList = templateJson,
                    tenantId = _userManager.TenantId,
                    sourceData = sourceData,
                    systemParamter = parameters,
                };
                try
                {
                    var reuslt = await _dataInterfaceService.GetResponseByType(item.InterfaceId, 4, input);
                    if (reuslt.IsNotEmptyOrNull())
                    {
                        if (reuslt.ToJsonString().FirstOrDefault().Equals('['))
                        {
                            item.Status = 0;
                            item.result = reuslt.ToJsonString();
                        }
                        else
                        {
                            var jobj = reuslt.ToObject<JObject>();
                            item.Status = jobj.ContainsKey("code") && "200".Equals(jobj["code"].ToString()) ? 0 : 1;
                            item.result = reuslt.ToJsonString();
                        }
                    }
                    else
                    {
                        item.Status = 0;
                        item.result = reuslt.ToJsonString();
                    }
                }
                catch (Exception ex)
                {
                    item.Status = 1;
                    item.result = string.Format("系统异常,错误信息:{0}", ex.StackTrace);
                    item.Data = input.ToJsonString();
                }
                _repository.UpdateEventLog(item);
            }
        }
    }

    /// <summary>
    /// 委托消息通知.
    /// </summary>
    /// <param name="delegateType">委托类型:发起，审批.</param>
    /// <param name="ToUserId">通知人员.</param>
    /// <param name="flowName">流程名.</param>
    /// <returns></returns>
    public async Task SendDelegateMsg(string delegateType, string ToUserId, string flowName)
    {
        var enCode = delegateType.Equals("发起") ? "MBXTLC015" : "MBXTLC016";
        var paramDic = new Dictionary<string, string>();
        paramDic.Add("delegateType", delegateType);
        paramDic.Add("flowName", flowName);
        paramDic.Add("@Mandatary", _userManager.RealName);
        paramDic.Add("@Title", flowName);
        var bodyDic = new Dictionary<string, object>();
        bodyDic.Add(ToUserId, new { type = "1" });
        var msgEntityList = _messageManager.GetMessageList(enCode, new List<string>() { ToUserId }, paramDic, 2, bodyDic, 2);
        await _messageManager.SendDefaultMsg(new List<string>() { ToUserId }, msgEntityList);
    }
}
