using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.FriendlyException;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Interfaces;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using JNPF.WorkFlow.Interfaces.Repository;
using Newtonsoft.Json.Linq;
using NPOI.Util;
using SqlSugar;

namespace JNPF.WorkFlow.Manager;

public class WorkFlowOtherUtil
{
    private readonly IWorkFlowRepository _repository;
    private readonly IUsersService _usersService;
    private readonly IRunService _runService;
    private readonly IUserManager _userManager;
    private readonly IBillRullService _billRuleService;

    public WorkFlowOtherUtil(IWorkFlowRepository repository, IUsersService usersService, IRunService runService, IUserManager userManager, IBillRullService billRuleService)
    {
        _repository = repository;
        _usersService = usersService;
        _runService = runService;
        _userManager = userManager;
        _billRuleService = billRuleService;
    }

    /// <summary>
    /// 流程表单数据处理(新增/修改).
    /// </summary>
    /// <param name="flowTaskSubmitModel"></param>
    /// <returns></returns>
    public async Task<string> FlowDynamicDataManage(FlowTaskSubmitModel flowTaskSubmitModel)
    {
        try
        {
            var isUpdate = flowTaskSubmitModel.id.IsNotEmptyOrNull();
            var id = isUpdate ? flowTaskSubmitModel.id : SnowflakeIdHelper.NextId();
            var startProperties = (_repository.GetNodeInfo(x => x.FlowId == flowTaskSubmitModel.flowId && WorkFlowNodeTypeEnum.start.ParseToString().Equals(x.NodeType))).NodeJson?.ToObject<NodeProperties>();
            //  委托人系统控件数据.
            if (flowTaskSubmitModel.isDelegate)
            {
                var formDic = flowTaskSubmitModel.formData.ToObject<Dictionary<string, object>>();
                var delegateUser = _usersService.GetInfoByUserId(flowTaskSubmitModel.crUser);
                formDic["Jnpf_FlowDelegate_CurrPosition"] = delegateUser.PositionId;
                formDic["Jnpf_FlowDelegate_CurrOrganize"] = _repository.GetOrgTree(delegateUser.OrganizeId).ToJsonString();
                flowTaskSubmitModel.formData = formDic;
            }
            var fEntity = _repository.GetFromEntity(startProperties.formId);
            var formOperates = startProperties.formOperates.ToObject<List<FormOperatesModel>>();
            var systemControlList = formOperates.Where(x => !x.write).Select(x => x.id).ToList();
            if (flowTaskSubmitModel.autoSubmit) systemControlList = null;
            await _runService.SaveFlowFormData(fEntity, flowTaskSubmitModel.formData.ToJsonString(), id, flowTaskSubmitModel.flowId, isUpdate, systemControlList);
            return id;
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode, ex.Args);
        }
    }

    /// <summary>
    /// 获取流程任务名称.
    /// </summary>
    /// <param name="flowTaskSubmitModel">提交参数.</param>
    /// <returns></returns>
    public async Task GetFlowTitle(FlowTaskSubmitModel flowTaskSubmitModel, WorkFlowParamter wfParamter)
    {
        var userName = flowTaskSubmitModel.crUser.IsNotEmptyOrNull() ? _userManager.GetUserName(flowTaskSubmitModel.crUser, false) : _userManager.User.RealName;
        if (wfParamter.globalPro.titleType == 1)
        {
            var formDataDic = flowTaskSubmitModel.formData.ToObject<Dictionary<string, object>>();
            formDataDic.Add("@flowFullName", wfParamter.flowInfo.fullName);
            formDataDic.Add("@flowFullCode", wfParamter.flowInfo.enCode);
            formDataDic.Add("@launchUserName", userName);
            formDataDic.Add("@launchTime", DateTime.Now.ToString("yyyy-MM-dd"));
            foreach (var item in wfParamter.globalPro.titleContent.Substring3())
            {
                if (formDataDic.ContainsKey(item) && formDataDic[item] != null)
                {
                    wfParamter.globalPro.titleContent = wfParamter.globalPro.titleContent?.Replace("{" + item + "}", formDataDic[item].ToString());
                }
                else
                {
                    wfParamter.globalPro.titleContent = wfParamter.globalPro.titleContent?.Replace("{" + item + "}", string.Empty);
                }
            }
            flowTaskSubmitModel.flowTitle = wfParamter.globalPro.titleContent;
        }
        else
        {
            flowTaskSubmitModel.flowTitle = string.Format("{0}的{1}", userName, wfParamter.flowInfo.fullName);
        }
    }

    /// <summary>
    /// 会签比例计算.
    /// </summary>
    /// <param name="operatorEntityList"></param>
    /// <param name="nodePro"></param>
    /// <param name="handleStatus"></param>
    /// <returns></returns>
    public bool IsSatisfyProportion(List<WorkFlowOperatorEntity> operatorEntityList, NodeProperties nodePro, int handleStatus, bool isCandidate = false)
    {
        if (!operatorEntityList.Any()) return true;
        operatorEntityList = operatorEntityList.FindAll(x => x.ParentId == "0");
        var index = operatorEntityList.Count; // 审批人数.
        if (nodePro.counterSign == 2)
        {
            index = operatorEntityList.FirstOrDefault().HandleAll.Split(",").ToList<string>().Count;
            nodePro.counterSignConfig.auditType = 1;
            nodePro.counterSignConfig.auditRatio = 100;
            nodePro.counterSignConfig.rejectType = 0;
            nodePro.counterSignConfig.rejectRatio = 0;
        }
        nodePro.handleStatus = handleStatus;
        if (nodePro.counterSignConfig.calculateType == 2) // 完成人数不等于审批人数且为延后计算直接不计算
        {
            var completionIndex = operatorEntityList.FindAll(x => x.Completion == 1 && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.AddSign.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt()).Count.ParseToDouble() + 1;
            if (completionIndex != index) return false;
            if (ProportionCalculate(operatorEntityList, nodePro, 1, index, isCandidate, handleStatus == 1))
            {
                nodePro.handleStatus = 1;
                return true;
            }
            if (ProportionCalculate(operatorEntityList, nodePro, 0, index, isCandidate, handleStatus == 0))
            {
                nodePro.handleStatus = 0;
                return true;
            }
            return false;
        }
        else
        {
            return ProportionCalculate(operatorEntityList, nodePro, handleStatus, index, isCandidate);
        }
    }

    /// <summary>
    /// 比例计算.
    /// </summary>
    /// <param name="operatorEntityList"></param>
    /// <param name="nodePro"></param>
    /// <param name="handleStatus"></param>
    /// <param name="index"></param>
    /// <param name="isbreak"></param>
    /// <returns></returns>
    public bool ProportionCalculate(List<WorkFlowOperatorEntity> operatorEntityList, NodeProperties nodePro, int handleStatus, int index, bool isCandidate = false, bool isbreak = true)
    {
        //完成人数（加上当前审批人）
        var comNum = operatorEntityList.FindAll(x => x.HandleStatus == handleStatus && x.Completion == 1 && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.AddSign.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt()).Count.ParseToDouble();
        if (!isCandidate)
        {
            comNum = operatorEntityList.FindAll(x => x.HandleStatus == handleStatus && x.HandleTime != null && x.Completion == 1 && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.AddSign.ParseToInt() && x.Status != WorkFlowOperatorStatusEnum.Assist.ParseToInt()).Count.ParseToDouble();
        }
        if (isbreak)
        {
            ++comNum;
        }
        //完成比例
        var comRatio = (comNum / index.ParseToDouble() * 100).ParseToInt();
        //会签配置
        var configType = handleStatus == 0 ? nodePro.counterSignConfig.rejectType : nodePro.counterSignConfig.auditType;
        var configRatio = handleStatus == 0 ? nodePro.counterSignConfig.rejectRatio : nodePro.counterSignConfig.auditRatio;
        var configNum = handleStatus == 0 ? nodePro.counterSignConfig.rejectNum : nodePro.counterSignConfig.auditNum;
        if (configType == 0 && handleStatus == 0)
        {
            configType = nodePro.counterSignConfig.auditType;
            configRatio = 100 - nodePro.counterSignConfig.auditRatio;
            configNum = index - nodePro.counterSignConfig.auditNum;
        }
        return configType == 1 ? comRatio >= configRatio : comNum >= configNum;
    }

    /// <summary>
    /// 会签比例计算(加签).
    /// </summary>
    /// <param name="operatorEntityList"></param>
    /// <param name="item"></param>
    /// <param name="handleStatus"></param>
    /// <returns></returns>
    public bool IsAddSignProportion(List<WorkFlowOperatorEntity> operatorEntityList, AddSignItem item, int handleStatus)
    {
        if (!operatorEntityList.Any()) return true;
        operatorEntityList = operatorEntityList.FindAll(x => x.ParentId != "0");
        var comNum = operatorEntityList.FindAll(x => x.HandleStatus == handleStatus).Count.ParseToDouble();
        ++comNum;
        var comRatio = (comNum / operatorEntityList.Count.ParseToDouble() * 100).ParseToInt();
        if (item.counterSign == 2)
        {
            comRatio = (comNum / item.addSignUserIdList.Count.ParseToDouble() * 100).ParseToInt();
        }
        var auditRatio = handleStatus == 0 ? 100 - item.auditRatio : item.auditRatio;
        return comRatio >= auditRatio;
    }

    /// <summary>
    /// 保存当前未完成节点下个候选人节点的候选人.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    public List<WorkFlowCandidatesEntity> SaveNodeCandidates(WorkFlowParamter wfParamter)
    {
        var candidateList = new List<WorkFlowCandidatesEntity>();
        if (wfParamter.candidateList.IsNotEmptyOrNull())
        {
            foreach (var item in wfParamter.candidateList.Keys)
            {
                var node = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == item);
                if (node != null)
                {
                    var candidatesEntityList = _repository.GetCandidates(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == item && x.Type == 1);
                    if (candidatesEntityList.Any())
                    {
                        var nodeId = _repository.GetOperatorInfo(x => x.Id == candidatesEntityList.FirstOrDefault().OperatorId).NodeId;
                        if (wfParamter.operatorEntity.NodeId != nodeId || wfParamter.nodePro.counterSign == 0)
                        {
                            // 循环连线节点清除当前候选人数据
                            _repository.DeleteCandidates(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == item && x.Type == 1);
                        }
                    }
                    candidateList.Add(new WorkFlowCandidatesEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        TaskId = wfParamter.taskEntity.Id,
                        NodeCode = item,
                        HandleId = _userManager.UserId,
                        Account = _userManager.Account,
                        Candidates = string.Join(",", wfParamter.candidateList[item]),
                        OperatorId = wfParamter.operatorEntity.Id,
                        Type = 1
                    });
                }
            }
            _repository.CreateCandidates(candidateList);
        }
        if (wfParamter.errorRuleUserList.IsNotEmptyOrNull())
        {
            candidateList.Clear();
            foreach (var item in wfParamter.errorRuleUserList.Keys)
            {
                var node = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == item);
                if (node != null)
                {
                    var candidatesEntityList = _repository.GetCandidates(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == item && x.Type == 2);
                    if (candidatesEntityList.Any())
                    {
                        var nodeId = _repository.GetOperatorInfo(x => x.Id == candidatesEntityList.FirstOrDefault().OperatorId).NodeId;
                        if (wfParamter.operatorEntity.NodeId != nodeId || wfParamter.nodePro.counterSign == 0)
                        {
                            // 循环连线节点清除当前候选人数据
                            _repository.DeleteCandidates(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == item && x.Type == 2);
                        }
                    }
                    candidateList.Add(new WorkFlowCandidatesEntity()
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        TaskId = wfParamter.taskEntity.Id,
                        NodeCode = item,
                        HandleId = _userManager.UserId,
                        Account = _userManager.Account,
                        Candidates = string.Join(",", wfParamter.errorRuleUserList[item]),
                        OperatorId = wfParamter.operatorEntity.Id,
                        Type = 2
                    });
                }
            }
            _repository.CreateCandidates(candidateList);
        }
        if (wfParamter.branchList.Any() && wfParamter.node.nodeType != WorkFlowNodeTypeEnum.start.ToString())
        {
            candidateList.Clear();
            _repository.DeleteCandidates(x => x.NodeCode == wfParamter.node.nodeCode && x.TaskId == wfParamter.taskEntity.Id && x.Type == 3);
            candidateList.Add(new WorkFlowCandidatesEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                TaskId = wfParamter.taskEntity.Id,
                NodeCode = wfParamter.node.nodeCode,
                HandleId = _userManager.UserId,
                Account = _userManager.Account,
                Candidates = string.Join(",", wfParamter.branchList),
                Type = 3
            });
            _repository.CreateCandidates(candidateList);
        }
        return candidateList;
    }

    /// <summary>
    /// 获取子流程继承父流程的表单数据.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="childTaskProperties">子流程属性.</param>
    /// <returns></returns>
    public async Task<object> GetSubTaskFormData(WorkFlowParamter wfParamter, WorkFlowNodeModel subNode)
    {
        var dataSource = new Dictionary<string, object>();
        var thisFormId = wfParamter.node.formId;
        var starFormId = wfParamter.startPro.formId;
        var template = _repository.GetTemplate(subNode.nodePro.flowId);
        if (template.IsNullOrEmpty() || template.FlowId.IsNullOrEmpty())
        {
            throw Oops.Oh(ErrorCode.WF0072);
        }
        var flowId = template.FlowId;
        var nextFormId = _repository.GetNodeInfo(x => x.FlowId == flowId && x.NodeType == WorkFlowNodeTypeEnum.start.ParseToString()).FormId;
        var mapRule = GetMapRule(subNode.nodePro.assignList, wfParamter.node.nodeCode);
        var formData = wfParamter.formData.ToObject<Dictionary<string, object>>();
        dataSource.Add(thisFormId, formData);
        if (thisFormId != starFormId)
        {
            var dataStrat = await _runService.GetOrDelFlowFormData(starFormId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId);
            dataSource.Add(starFormId, dataStrat);
        }
        if (wfParamter.taskEntity.GlobalParameter.IsNotEmptyOrNull())
        {
            dataSource.Add("globalParameter", wfParamter.taskEntity.GlobalParameter.ToObject<Dictionary<string, object>>());
        }
        else
        {
            dataSource.Add("globalParameter", new Dictionary<string, object>());
        }
        var childFormData = await _runService.SaveDataToDataByFId(starFormId, thisFormId, nextFormId, mapRule, dataSource, true);
        return childFormData;
    }

    /// <summary>
    /// 获取表单传递字段.
    /// </summary>
    /// <param name="assignItems">传递规则.</param>
    /// <param name="nodeCode">传递节点编码.</param>
    /// <returns></returns>
    private List<Dictionary<string, string>> GetMapRule(List<AssignItem> assignItems, string nodeCode, string formId = null)
    {
        if (!assignItems.Any()) return new List<Dictionary<string, string>>();
        var ruleList = assignItems.Find(x => x.nodeId == nodeCode)?.ruleList;
        var mapRule = new List<Dictionary<string, string>>();
        if (ruleList.IsNotEmptyOrNull())
        {
            foreach (var item in ruleList)
            {
                if (item.parentField.IsNotEmptyOrNull())
                {
                    var dic = new Dictionary<string, string>();
                    if (!item.parentField.Equals("@startNodeFormId") && !item.parentField.Equals("@prevNodeFormId") && !item.parentField.Contains("|") && formId.IsNotEmptyOrNull())
                    {
                        item.parentField = string.Format("{0}|{1}", item.parentField, formId);
                    }
                    dic.Add(item.parentField, item.childField);
                    mapRule.Add(dic);
                }
            }
        }
        return mapRule;
    }

    /// <summary>
    /// 添加经办记录.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="handleStatus">审批类型（0：拒绝，1：同意）.</param>
    /// <returns></returns>
    public async Task CreateRecord(WorkFlowParamter wfParamter, int handleType, string handleUserId = "")
    {
        WorkFlowRecordEntity recordEntity = new WorkFlowRecordEntity();
        recordEntity.HandleTime = DateTime.Now;
        recordEntity.HandleType = handleType;
        recordEntity.HandleOpinion = wfParamter.handleOpinion;
        recordEntity.SignImg = wfParamter.signImg;
        recordEntity.Status = 0;
        recordEntity.FileList = wfParamter.fileList.ToJsonString();
        recordEntity.TaskId = wfParamter.taskEntity.Id;
        recordEntity.NodeCode = wfParamter.taskEntity.CurrentNodeCode;
        recordEntity.NodeName = wfParamter.taskEntity.CurrentNodeName;
        recordEntity.ExpandField = wfParamter.approvalField?.ToJsonString();
        if (handleUserId.IsNotEmptyOrNull())
        {
            recordEntity.HandleUserId = handleUserId;
        }
        #region 经办记录处理人
        if (handleType == WorkFlowRecordTypeEnum.Submit.ParseToInt())
        {
            recordEntity.HandleId = wfParamter.taskEntity.CreatorUserId;
        }
        else if (handleType == WorkFlowRecordTypeEnum.Transfer.ParseToInt())
        {
            if (wfParamter.isAuto)
            {
                recordEntity.HandleId = wfParamter.operatorEntity.HandleId == handleUserId ? "jnpf" : wfParamter.operatorEntity.HandleId;
            }
            else
            {
                recordEntity.HandleId = _userManager.UserId;
            }
        }
        else if (handleType == WorkFlowRecordTypeEnum.Assigned.ParseToInt() || handleType == WorkFlowRecordTypeEnum.Processing.ParseToInt())
        {
            recordEntity.HandleId = _userManager.UserId;
        }
        else
        {
            if (wfParamter.operatorEntity.IsNotEmptyOrNull() && wfParamter.operatorEntity.HandleId.IsNotEmptyOrNull())
            {
                recordEntity.HandleId = wfParamter.operatorEntity.HandleId;
                if (wfParamter.isAuto)
                {
                    recordEntity.HandleId = wfParamter.operatorEntity.HandleId;
                }
            }
            else
            {
                recordEntity.HandleId = _userManager.UserId;
            }
        }
        #endregion

        if (wfParamter.operatorEntity.IsNotEmptyOrNull())
        {
            recordEntity.NodeCode = wfParamter.operatorEntity.NodeCode;
            recordEntity.NodeName = wfParamter.operatorEntity.NodeName;
            if (handleType == 2)
            {
                recordEntity.NodeCode = wfParamter.startPro.nodeId;
                recordEntity.NodeName = wfParamter.startPro.nodeName;
            }
            recordEntity.OperatorId = wfParamter.operatorEntity.Id;
            if ("0,1,3,4,5,6,7,11,13,16,17,18".Contains(handleType.ToString()))
            {
                recordEntity.NodeId = wfParamter.operatorEntity.NodeId;
            }
        }

        _repository.CreateRecord(recordEntity);
    }

    /// <summary>
    /// 数据传递下一节点初始表单数据.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="nextNode"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetNextFormData(WorkFlowParamter wfParamter, WorkFlowNodeModel nextNode)
    {
        var nextNodeData = new Dictionary<string, object>();
        var mapRule = new List<Dictionary<string, string>>();
        var dataSource = new Dictionary<string, object>();
        var formId = wfParamter.node.formId;
        var starFormId = wfParamter.startPro.formId;
        if ((WorkFlowNodeTypeEnum.approver.ToString().Equals(nextNode.nodeType) || WorkFlowNodeTypeEnum.processing.ToString().Equals(nextNode.nodeType)) && wfParamter.globalPro.hasAloneConfigureForms)
        {
            if (WorkFlowNodeTypeEnum.subFlow.ToString().Equals(wfParamter.node.nodeType))
            {
                // 传递节点
                var extendNode = nextNode.nodePro.assignList.Select(x => x.nodeId).ToList();
                // 最后审批节点
                var lastHandleNode = (_repository.GetRecordList(x => x.TaskId == wfParamter.taskEntity.Id && (x.HandleType == 1 || x.HandleType == 2), o => o.HandleTime, SqlSugar.OrderByType.Desc)).FirstOrDefault();
                if (extendNode.Any())
                {
                    lastHandleNode = (_repository.GetRecordList(x => extendNode.Contains(x.NodeCode) && x.TaskId == wfParamter.taskEntity.Id && (x.HandleType == 1 || x.HandleType == 2), o => o.HandleTime, SqlSugar.OrderByType.Desc)).FirstOrDefault();
                }
                if (lastHandleNode.IsNotEmptyOrNull() && lastHandleNode.NodeCode.IsNotEmptyOrNull())
                {
                    formId = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == lastHandleNode.NodeCode)?.formId;
                    mapRule = GetMapRule(nextNode.nodePro.assignList, lastHandleNode.NodeCode);
                }
            }
            else if (WorkFlowNodeTypeEnum.outside.ToString().Equals(wfParamter.node.nodeType))
            {
                var envntLog = _repository.GetEventLog(x => x.NodeId == wfParamter.operatorEntity.NodeId && x.DeleteMark == null);
                formId = wfParamter.nodeList.FirstOrDefault(x => x.nodeCode == envntLog.UpCode)?.formId;
                mapRule = GetMapRule(nextNode.nodePro.assignList, wfParamter.node.nodeCode, "outsideParameter");
            }
            else
            {
                mapRule = GetMapRule(nextNode.nodePro.assignList, wfParamter.node.nodeCode);
            }

            var data = await _runService.GetOrDelFlowFormData(formId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId);
            if (data == null) data = wfParamter.formData.ToObject<Dictionary<string, object>>();
            if (!data.ContainsKey("id") || data["id"].IsNullOrEmpty() || data.ContainsKey("f_version") || data.ContainsKey("F_VERSION"))//针对表单自增长优化
            {
                data["id"] = wfParamter.taskEntity.Id;
            }
            dataSource.Add(formId, data);
            dataSource.Add("outsideParameter", wfParamter.formData);
            if (starFormId != formId)
            {
                var dataStrat = await _runService.GetOrDelFlowFormData(starFormId, wfParamter.taskEntity.Id, 0, wfParamter.flowInfo.flowId);
                if (dataStrat == null) data = wfParamter.formData.ToObject<Dictionary<string, object>>();
                if (!dataStrat.ContainsKey("id") || data["id"].IsNullOrEmpty())//针对表单自增长优化
                {
                    dataStrat["id"] = wfParamter.taskEntity.Id;
                }

                dataSource.Add(starFormId, dataStrat);
            }
            if (wfParamter.taskEntity.GlobalParameter.IsNotEmptyOrNull())
            {
                dataSource.Add("globalParameter", wfParamter.taskEntity.GlobalParameter.ToObject<Dictionary<string, object>>());
            }
            else
            {
                dataSource.Add("globalParameter", nextNodeData);
            }
            nextNodeData = await _runService.SaveDataToDataByFId(starFormId, formId, nextNode.formId, mapRule, dataSource, false);
        }
        else
        {
            nextNodeData = wfParamter.formData?.ToObject<Dictionary<string, object>>();
        }
        return nextNodeData;
    }

    /// <summary>
    /// 修改当前节点经办数据.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="handleStatus">审批类型（0：拒绝，1：同意）.</param>
    /// <param name="isRecursion">是否递归,递归不生成经办记录.</param>
    /// <returns></returns>
    public async Task<bool> CompleteOperator(WorkFlowParamter wfParamter, int handleStatus, bool isRecursion = false)
    {
        var isNodeCompletion = false;
        var updateOperatorList = new List<WorkFlowOperatorEntity>(); // 要修改的经办
        // 当前经办是加签
        if (!"0".Equals(wfParamter.operatorEntity.ParentId))
        {
            var addSignoperatorList = wfParamter.operatorEntityList.FindAll(x => x.ParentId == wfParamter.operatorEntity.ParentId);
            var addSignParameter = wfParamter.operatorEntity.HandleParameter.ToObject<AddSignItem>();
            wfParamter.branchList = addSignParameter.branchList;
            if (addSignParameter.counterSign == 2)
            {
                addSignParameter.addSignUserIdList = wfParamter.operatorEntity.HandleAll.Split(",").ToList();
            }
            // 加签审批完成
            if (addSignParameter.counterSign == 0 || IsAddSignProportion(addSignoperatorList, addSignParameter, handleStatus))
            {
                var parentOperator = _repository.GetOperatorInfo(x => x.Id == wfParamter.operatorEntity.ParentId);
                updateOperatorList = _repository.GetAddSignOperatorList(wfParamter.operatorEntity.ParentId, 0, false).FindAll(x => x.HandleStatus == null && x.HandleTime == null && x.Id != wfParamter.operatorEntity.Id);
                updateOperatorList.ForEach(item =>
                {
                    item.Completion = 1;
                });

                if (addSignParameter.addSignType == 1)
                {
                    parentOperator.Completion = 0;
                    parentOperator.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
                    parentOperator.CreatorTime = DateTime.Now;
                    parentOperator.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
                    _repository.UpdateOperator(parentOperator);
                    // 前加签回退时作废加签记录（用于前加签审批撤回）
                    _repository.DeleteRecord(x => x.TaskId == parentOperator.TaskId && x.OperatorId == parentOperator.Id && x.HandleType == WorkFlowRecordTypeEnum.AddSign.ParseToInt());
                }
                else
                {
                    var wfParamterParent = wfParamter.Copy();
                    wfParamterParent.operatorEntityList = wfParamter.operatorEntityList.FindAll(x => x.ParentId == parentOperator.ParentId);
                    wfParamterParent.operatorEntity = parentOperator;
                    isNodeCompletion = await CompleteOperator(wfParamterParent, handleStatus, true);
                    wfParamter.taskEntity = wfParamterParent.taskEntity;
                }
            }
            else if (addSignParameter.counterSign == 2)
            {
                var operatorEntityList = wfParamter.operatorEntity.HandleAll.Split(",").ToList();
                var index = operatorEntityList.IndexOf(wfParamter.operatorEntity.HandleId);
                var nextHandleId = operatorEntityList.GetIndex(index + 1);
                var addSignOperatorEntity = wfParamter.operatorEntity.Copy();
                addSignOperatorEntity.Id = SnowflakeIdHelper.NextId();
                addSignOperatorEntity.HandleId = nextHandleId;
                addSignOperatorEntity.CreatorTime = DateTime.Now;
                addSignOperatorEntity.Status = WorkFlowOperatorStatusEnum.AddSign.ParseToInt();
                addSignOperatorEntity.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
                addSignOperatorEntity.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
                addSignOperatorEntity.DraftData = null;
                _repository.CreateOperator(addSignOperatorEntity);
                wfParamter.nextOperatorEntityList.Add(addSignOperatorEntity);
            }
        }
        else
        {
            wfParamter.nodePro.handleStatus = wfParamter.handleStatus;
            if (wfParamter.nodePro.assigneeType == (int)WorkFlowOperatorTypeEnum.StepApprover) // 逐级审批
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
                            isNodeCompletion = currentLevel <= endLevel;
                        }
                        else
                        {
                            isNodeCompletion = true;
                        }
                    }
                    else
                    {
                        if (posIds.Count > endLevel)
                        {
                            isNodeCompletion = operatorCount == endLevel;
                        }
                        else
                        {
                            isNodeCompletion = currentLevel < 2;
                        }
                    }
                    if (!isNodeCompletion)
                    {
                        var posId = posIds.GetIndex(currentLevel - 2);
                        var nextHandleId = _usersService.GetDutyUserId(posId);
                        if (nextHandleId.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.WF0079);
                        var nextOperator = wfParamter.operatorEntity.Copy();
                        nextOperator.Id = SnowflakeIdHelper.NextId();
                        nextOperator.HandleId = nextHandleId;
                        nextOperator.CreatorTime = DateTime.Now;
                        nextOperator.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
                        nextOperator.Status = WorkFlowOperatorStatusEnum.Runing.ParseToInt();
                        nextOperator.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
                        _repository.CreateOperator(nextOperator);
                        wfParamter.operatorEntity.HandleAll = nextOperator.Id; // 记录下一经办供撤回使用
                        wfParamter.nextOperatorEntityList.Add(nextOperator);
                    }
                }
                else
                {
                    isNodeCompletion = true;
                }

                if (isNodeCompletion)
                {
                    if (stepLaunchUser.IsNotEmptyOrNull())
                    {
                        _repository.DeleteLaunchUser(x => x.Id == stepLaunchUser.Id);
                    }
                    if (wfParamter.operatorEntityList.FindAll(x => x.ParentId == "0").Any(x => x.HandleStatus == 0))
                    {
                        wfParamter.nodePro.handleStatus = 0;
                    }

                    if (wfParamter.nodePro.handleStatus == 0 && !wfParamter.globalPro.hasContinueAfterReject)
                    {
                        wfParamter.taskEntity.Status = WorkFlowTaskStatusEnum.Reject.ParseToInt();
                        wfParamter.taskEntity.EndTime = DateTime.Now;
                        updateOperatorList = _repository.GetOperatorList(x => x.Completion == 0 && x.TaskId == wfParamter.operatorEntity.TaskId);
                        var nodeRecord = new WorkFlowNodeRecordEntity
                        {
                            TaskId = wfParamter.taskEntity.Id,
                            NodeId = wfParamter.operatorEntity.NodeId,
                            NodeCode = wfParamter.operatorEntity.NodeCode,
                            NodeName = wfParamter.operatorEntity.NodeName,
                            NodeStatus = 3
                        };
                        _repository.CreateNodeRecord(nodeRecord);
                        isNodeCompletion = false;
                    }
                }
            }
            else if (wfParamter.nodePro.counterSign == 0 || wfParamter.operatorEntity.Status == WorkFlowOperatorStatusEnum.Assigned.ParseToInt() || IsSatisfyProportion(wfParamter.operatorEntityList, wfParamter.nodePro, handleStatus) || wfParamter.operatorEntity.HandleId == "jnpf")
            {
                if (wfParamter.nodePro.handleStatus == 0 && !wfParamter.globalPro.hasContinueAfterReject)
                {
                    wfParamter.taskEntity.Status = WorkFlowTaskStatusEnum.Reject.ParseToInt();
                    wfParamter.taskEntity.EndTime = DateTime.Now;
                    updateOperatorList = _repository.GetOperatorList(x => x.Completion == 0 && x.TaskId == wfParamter.operatorEntity.TaskId);
                    var nodeRecord = new WorkFlowNodeRecordEntity
                    {
                        TaskId = wfParamter.taskEntity.Id,
                        NodeId = wfParamter.operatorEntity.NodeId,
                        NodeCode = wfParamter.operatorEntity.NodeCode,
                        NodeName = wfParamter.operatorEntity.NodeName,
                        NodeStatus = 3
                    };
                    _repository.CreateNodeRecord(nodeRecord);
                }
                else
                {
                    isNodeCompletion = true;
                    updateOperatorList = wfParamter.operatorEntityList.FindAll(x => x.HandleStatus == null && x.HandleTime == null && x.Id != wfParamter.operatorEntity.Id);
                }

                updateOperatorList.ForEach(item =>
                {
                    item.Completion = 1;
                });
            }
            else if (wfParamter.nodePro.counterSign == 2 && !(wfParamter.handleStatus == 0 && !wfParamter.globalPro.hasContinueAfterReject))
            {
                var operatorEntityList = wfParamter.operatorEntity.HandleAll.Split(",").ToList<string>();
                var index = operatorEntityList.IndexOf(wfParamter.operatorEntity.HandleId);
                var nextHandleId = operatorEntityList.GetIndex(index + 1);
                var nextOperator = wfParamter.operatorEntity.Copy();
                nextOperator.Id = SnowflakeIdHelper.NextId();
                nextOperator.HandleId = nextHandleId;
                nextOperator.CreatorTime = DateTime.Now;
                nextOperator.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
                nextOperator.Status = WorkFlowOperatorStatusEnum.Runing.ParseToInt();
                nextOperator.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
                _repository.CreateOperator(nextOperator);
                wfParamter.nextOperatorEntityList.Add(nextOperator);
            }
        }
        wfParamter.operatorEntity.HandleStatus = handleStatus;
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
        updateOperatorList.Add(wfParamter.operatorEntity);
        _repository.DeleteOperator(x => x.ParentId == wfParamter.operatorEntity.Id && x.Status == WorkFlowOperatorStatusEnum.Assist.ParseToInt());
        _repository.UpdateOperator(updateOperatorList);
        if (!isRecursion)
        {
            await CreateRecord(wfParamter, handleStatus);
        }
        return isNodeCompletion;
    }

    /// <summary>
    /// 对审批人节点分组.
    /// </summary>
    /// <param name="flowTaskOperatorEntities">所有经办.</param>
    /// <returns></returns>
    public Dictionary<string, List<WorkFlowOperatorEntity>> GroupByOperator(List<WorkFlowOperatorEntity> flowTaskOperatorEntities)
    {
        var dic = new Dictionary<string, List<WorkFlowOperatorEntity>>();
        foreach (var item in flowTaskOperatorEntities.GroupBy(x => x.NodeCode))
        {
            dic.Add(item.Key, flowTaskOperatorEntities.FindAll(x => x.NodeCode == item.Key));
        }
        return dic;
    }

    /// <summary>
    /// 获取起始时间.
    /// </summary>
    /// <param name="timeOutConfig">限时配置.</param>
    /// <param name="receiveTime">接收时间.</param>
    /// <param name="createTime">发起时间.</param>
    /// <param name="formData">表单数据.</param>
    /// <returns></returns>
    public DateTime GetStartingTime(TimeOutConfig timeOutConfig, DateTime nowTime, DateTime receiveTime, DateTime createTime, string formData)
    {
        var dt = nowTime;
        switch (timeOutConfig.nodeLimit)
        {
            case 0:
                dt = receiveTime;
                break;
            case 1:
                dt = createTime;
                break;
            case 2:
                var jobj = formData.ToObjectOld<JObject>();
                if (jobj.ContainsKey(timeOutConfig.formField))
                {
                    try
                    {
                        var value = jobj[timeOutConfig.formField].ToString();
                        if (!DateTime.TryParse(value, out dt))
                        {
                            dt = jobj[timeOutConfig.formField].ParseToLong().TimeStampToDateTime();
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                break;
        }
        return dt;
    }

    /// <summary>
    /// 撤销任务保存.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task<WorkFlowParamter> RevokeSave(WorkFlowParamter wfParamter)
    {
        var wfParamterRevoke = wfParamter.Copy();
        #region 撤销表单
        var revokeFormData = new RevokeFormModel();
        revokeFormData.billRule = await _billRuleService.GetBillNumber("workflow_revoke");
        revokeFormData.creatorTime = DateTime.Now.ParseToUnixTime();
        revokeFormData.handleOpinion = wfParamter.handleOpinion;
        revokeFormData.revokeTaskId = wfParamter.taskEntity.Id;
        revokeFormData.revokeTaskName = wfParamter.taskEntity.FullName;
        #endregion

        #region 撤销任务
        var taskEntity = new WorkFlowTaskEntity();
        taskEntity.Id = SnowflakeIdHelper.NextId();
        taskEntity.FullName = string.Format("{0}的撤销申请", wfParamter.taskEntity.FullName);
        taskEntity.Urgent = wfParamter.taskEntity.Urgent;
        taskEntity.FlowId = wfParamter.taskEntity.FlowId;
        taskEntity.FlowCode = wfParamter.taskEntity.FlowCode;
        taskEntity.FlowName = wfParamter.taskEntity.FlowName;
        taskEntity.FlowType = wfParamter.taskEntity.FlowType;
        taskEntity.FlowCategory = wfParamter.taskEntity.FlowCategory;
        taskEntity.FlowVersion = wfParamter.taskEntity.FlowVersion;
        taskEntity.StartTime = DateTime.Now;
        taskEntity.CurrentNodeName = wfParamter.startPro.nodeName;
        taskEntity.CurrentNodeCode = wfParamter.startPro.nodeId;
        taskEntity.Status = WorkFlowTaskStatusEnum.Runing.ParseToInt();
        taskEntity.CreatorTime = DateTime.Now;
        taskEntity.CreatorUserId = wfParamter.taskEntity.CreatorUserId;
        taskEntity.ParentId = "0";
        taskEntity.IsAsync = 0;
        taskEntity.TemplateId = wfParamter.taskEntity.TemplateId;
        taskEntity.EngineId = wfParamter.taskEntity.EngineId;
        taskEntity.EngineType = 1;
        taskEntity.Type = 1;
        if (wfParamter.taskEntity.DelegateUserId.IsNotEmptyOrNull() && _userManager.UserId == wfParamter.taskEntity.DelegateUserId)
        {
            taskEntity.DelegateUserId = wfParamter.taskEntity.DelegateUserId;
        }
        wfParamterRevoke.taskEntity = taskEntity;
        #endregion

        #region 撤销关系
        var revokeEntity = new WorkFlowRevokeEntity();
        revokeEntity.TaskId = revokeFormData.revokeTaskId;
        revokeEntity.RevokeTaskId = taskEntity.Id;
        revokeEntity.FormData = revokeFormData.ToJsonString();
        _repository.CreateRevoke(revokeEntity);
        #endregion
        return wfParamterRevoke;
    }
}
