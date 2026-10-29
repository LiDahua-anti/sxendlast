using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.WorkFlow;
using JNPF.Common.Security;
using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using JNPF.WorkFlow.Factory;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace JNPF.WorkFlow.Manager;

public class WorkFlowUserUtil
{
    private readonly IWorkFlowRepository _repository;
    private readonly IUsersService _usersService;
    private readonly IUserRelationService _userRelationService;
    private readonly IDataInterfaceService _dataInterfaceService;
    private readonly IUserManager _userManager;

    public WorkFlowUserUtil(IWorkFlowRepository repository, IUsersService usersService, IUserRelationService userRelationService, IDataInterfaceService dataInterfaceService, IUserManager userManager)
    {
        _repository = repository;
        _usersService = usersService;
        _userRelationService = userRelationService;
        _dataInterfaceService = dataInterfaceService;
        _userManager = userManager;
    }

    /// <summary>
    /// 候选人员列表.
    /// </summary>
    /// <param name="userIds">用户.</param>
    /// <param name="flowHandleModel">审批参数.</param>
    /// <param name="hasCandidates">是否存在候选人.</param>
    /// <returns></returns>
    public dynamic GetCandidateItems(List<string> userIds, WorkFlowHandleModel flowHandleModel, bool hasCandidates = true)
    {
        var input = new UserConditionInput()
        {
            userIds = userIds,
            pagination = flowHandleModel,
            userId = flowHandleModel.delegateUser
        };
        if (hasCandidates)
        {
            return _userRelationService.GetUserListPage(input);
        }
        else
        {
            return _userRelationService.GetUserPage(input);
        }
    }

    /// <summary>
    /// 获取候选人节点信息.
    /// </summary>
    /// <param name="candidateModels"></param>
    /// <param name="nextNodeList"></param>
    /// <param name="nodeList"></param>
    /// <returns></returns>
    public async Task GetCandidates(List<CandidateModel> candidateModels, List<WorkFlowNodeModel> nextNodeList)
    {
        foreach (var item in nextNodeList)
        {
            if (item.nodePro.IsNotEmptyOrNull() && (item.nodeType == WorkFlowNodeTypeEnum.subFlow.ToString() || item.nodeType == WorkFlowNodeTypeEnum.approver.ToString() || item.nodeType == WorkFlowNodeTypeEnum.processing.ToString()))
            {
                var candidateItem = new CandidateModel();
                candidateItem.nodeCode = item.nodeCode;
                candidateItem.nodeName = item.nodePro.nodeName;
                candidateItem.isCandidates = item.nodePro.isCandidates;
                candidateModels.Add(candidateItem);
            }
        }
    }

    public async Task AddOperatorByType(WorkFlowParamter wfParamter, WorkFlowNodeModel nextNode)
    {
        try
        {
            if (WorkFlowNodeTypeEnum.approver.ToString().Equals(nextNode.nodeType) || WorkFlowNodeTypeEnum.processing.ToString().Equals(nextNode.nodeType))
            {
                var handleIds = await GetFlowUserId(wfParamter, nextNode);
                if (handleIds.Count == 0)
                {
                    switch (wfParamter.globalPro.errorRule)
                    {
                        case 1:
                            handleIds.Add(_userManager.GetAdminUserId());
                            break;
                        case 2:
                            handleIds = (await _usersService.GetList(wfParamter.globalPro.errorRuleUser)).Select(x => x.Id).ToList();
                            if (!handleIds.Any())
                            {
                                handleIds.Add(_userManager.GetAdminUserId());
                            }
                            break;
                        case 3:
                            var errorUserId = _repository.GetCandidates(nextNode.nodeCode, wfParamter.taskEntity.Id, 2);
                            if (errorUserId.IsNotEmptyOrNull() && errorUserId.Count > 0)
                            {
                                var userAllList = await _usersService.GetUserListByExp(x => x.DeleteMark == null && x.EnabledMark == 1, u => new UserEntity() { Id = u.Id });
                                errorUserId = errorUserId.Intersect(userAllList.Select(x => x.Id)).ToList(); // 过滤掉作废人员和非用户人员
                                handleIds = errorUserId.Any() ? errorUserId : new List<string> { _userManager.GetAdminUserId() };
                            }
                            else
                            {
                                if (!wfParamter.errorNodeList.Select(x => x.nodeCode).Contains(nextNode.nodeCode))
                                {
                                    wfParamter.errorNodeList.Add(nextNode.Adapt<CandidateModel>());
                                }
                            }
                            break;
                        case 4:
                            // 异常节点下一节点是否存在候选人节点.
                            var errorNextNodeList = (await BpmnEngineFactory.CreateBmpnEngine().GetNextNode(string.Empty, string.Empty, nextNode.nodeId)).Select(x => x.id).ToList();
                            var flag = wfParamter.nodeList.
                                Any(x => errorNextNodeList.Contains(x.nodeCode)
                                && WorkFlowNodeTypeEnum.approver.ToString().Equals(x.nodeType)
                                && x.nodePro.assigneeType == 7);
                            if (flag)
                            {
                                handleIds.Add(_userManager.GetAdminUserId());
                            }
                            else
                            {
                                handleIds.Add("jnpf");
                            }
                            break;
                        case 5:
                            throw Oops.Oh(ErrorCode.WF0035);
                    }
                }
                if (nextNode.nodePro.counterSign == 2)
                {
                    wfParamter.nextOperatorEntityList.Add(new WorkFlowOperatorEntity
                    {
                        Id = SnowflakeIdHelper.NextId(),
                        NodeCode = nextNode.nodeCode,
                        NodeName = nextNode.nodePro.nodeName,
                        NodeId = nextNode.nodeId,
                        HandleId = handleIds.FirstOrDefault(),
                        HandleAll = string.Join(",", handleIds),
                        EngineType = wfParamter.taskEntity.EngineType,
                        TaskId = wfParamter.taskEntity.Id,
                        Status = WorkFlowOperatorStatusEnum.Runing.ParseToInt(),
                        SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now,
                        StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now,
                        CreatorTime = DateTime.Now,
                        Completion = 0,
                        IsProcessing = nextNode.nodeType == WorkFlowNodeTypeEnum.processing.ToString() ? 1 : 0,
                    });
                }
                else
                {
                    foreach (var item in handleIds)
                    {
                        WorkFlowOperatorEntity operatorEntity = new WorkFlowOperatorEntity();
                        operatorEntity.Id = SnowflakeIdHelper.NextId();
                        operatorEntity.NodeCode = nextNode.nodeCode;
                        operatorEntity.NodeName = nextNode.nodePro.nodeName;
                        operatorEntity.NodeId = nextNode.nodeId;
                        operatorEntity.TaskId = wfParamter.taskEntity.Id;
                        operatorEntity.Status = WorkFlowOperatorStatusEnum.Runing.ParseToInt();
                        operatorEntity.SignTime = wfParamter.globalPro.hasSignFor ? null : DateTime.Now;
                        operatorEntity.EngineType = wfParamter.taskEntity.EngineType;
                        operatorEntity.HandleId = item;
                        operatorEntity.CreatorTime = DateTime.Now;
                        operatorEntity.StartHandleTime = _userManager.CommonModuleEnCodeList.Contains("workFlow.flowTodo") ? null : DateTime.Now;
                        operatorEntity.Completion = 0;
                        operatorEntity.IsProcessing = nextNode.nodeType == WorkFlowNodeTypeEnum.processing.ToString() ? 1 : 0;
                        wfParamter.nextOperatorEntityList.Add(operatorEntity);
                    }
                    if (nextNode.nodePro.assigneeType == WorkFlowOperatorTypeEnum.StepApprover.ParseToInt())
                    {
                        var steplaunchUser = _userManager.UserId;
                        if (nextNode.nodePro.approversConfig.start == 2)
                        {
                            steplaunchUser = wfParamter.operatorEntity?.HandleId;
                        }
                        else
                        {
                            if (wfParamter.taskEntity.IsNotEmptyOrNull() && wfParamter.taskEntity.CreatorUserId.IsNotEmptyOrNull())
                            {
                                steplaunchUser = wfParamter.taskEntity.CreatorUserId;
                            }
                        }
                        if (steplaunchUser.IsNotEmptyOrNull() && steplaunchUser != "jnpf")
                        {
                            _repository.CreateStepLaunchUser(steplaunchUser, wfParamter.taskEntity.Id, nextNode.nodeCode);
                        }
                    }
                }
            }
        }
        catch (AppFriendlyException ex)
        {
            throw Oops.Oh(ex.ErrorCode);
        }
    }

    /// <summary>
    /// 获取节点审批人员id.
    /// </summary>
    /// <param name="wfParamter">当前任务参数.</param>
    /// <param name="node">节点.</param>
    /// <param name="isCandidates">是否候选.</param>
    /// <returns></returns>
    public async Task<List<string>> GetFlowUserId(WorkFlowParamter wfParamter, WorkFlowNodeModel node, bool isCandidates = false)
    {
        var userIdList = new List<string>();
        var userAllList = await _usersService.GetUserListByExp(x => x.DeleteMark == null && x.EnabledMark == 1, u => new UserEntity() { Id = u.Id }); // 获取全部用户id
        var userEntity = _usersService.GetInfoByUserId(_userManager.UserId); // 发起者本人.
        if (wfParamter.taskEntity.IsNotEmptyOrNull() && wfParamter.taskEntity.CreatorUserId.IsNotEmptyOrNull())
            userEntity = _usersService.GetInfoByUserId(wfParamter.taskEntity.CreatorUserId);
        if (!isCandidates && node.nodePro.isCandidates)
        {
            userIdList = _repository.GetCandidates(node.nodeCode, wfParamter.taskEntity.Id);
            if (userIdList.Any())
            {
                userIdList = userIdList.Intersect(userAllList.Select(x => x.Id)).ToList();
                if (!userIdList.Any())
                {
                    userIdList = new List<string> { _userManager.GetAdminUserId() };
                }
            }
        }
        else
        {
            switch (node.nodePro.assigneeType)
            {
                // 直属主管
                case (int)WorkFlowOperatorTypeEnum.LaunchCharge:
                    if (node.nodePro.approverType == 2)
                    {
                        if (isCandidates)
                        {
                            //查上一节点 
                            var upNodeCodeList = await BpmnEngineFactory.CreateBmpnEngine().GetPrevNode(wfParamter.engineId, node.nodeCode, string.Empty);
                            if (upNodeCodeList.Contains(wfParamter.node.nodeCode))
                            {
                                var handleId = wfParamter.node.nodeType == WorkFlowNodeTypeEnum.start.ToString() ? userEntity.Id : wfParamter.operatorEntity?.HandleId;
                                var crDirector = GetManagerByLevel(handleId, node.nodePro.managerLevel, true);
                                if (crDirector.Any())
                                    userIdList.AddRange(crDirector);
                            }
                        }
                        else
                        {
                            var operatorList = wfParamter.operatorEntityList.FindAll(x => x.ParentId == "0" && x.HandleId != "jnpf" && x.Status >= 0 && x.HandleStatus != null && x.HandleTime != null).Select(x => x.HandleId).ToList();
                            if (wfParamter.operatorEntity.IsNotEmptyOrNull() && wfParamter.operatorEntity.HandleId != "jnpf")
                                operatorList.Add(wfParamter.operatorEntity.HandleId);
                            if (operatorList.Any())
                            {
                                foreach (var item in operatorList)
                                {
                                    var crDirector = GetManagerByLevel(item, node.nodePro.managerLevel, true);
                                    if (crDirector.Any())
                                        userIdList.AddRange(crDirector);
                                }
                            }
                        }
                    }
                    else
                    {
                        var crDirector = GetManagerByLevel(userEntity.Id, node.nodePro.managerLevel, true);
                        if (crDirector.Any())
                            userIdList.AddRange(crDirector);
                    }
                    break;

                // 发起者本人
                case (int)WorkFlowOperatorTypeEnum.InitiatorMe:
                    userIdList.Add(userEntity.Id);
                    break;

                // 表单变量
                case (int)WorkFlowOperatorTypeEnum.VariableApprover:
                    try
                    {
                        var jd = wfParamter.nextNodeFormData.IsNotEmptyOrNull() ? wfParamter.nextNodeFormData.ToObject<JObject>() : wfParamter.formData.ToObject<JObject>();
                        var fieldValueList = new List<string>();
                        var formField = node.nodePro.formField;
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
                        userIdList = _userRelationService.GetUserId(fieldValueList);
                    }
                    catch (Exception)
                    {
                        break;
                    }
                    break;

                // 环节(提交时下个节点是环节就跳过，审批则看环节节点是否是当前节点的上级)
                case (int)WorkFlowOperatorTypeEnum.LinkApprover:
                    // 环节节点所有经办人(过滤掉加签人)
                    userIdList = _repository.GetOperatorList(x =>
                    x.TaskId == wfParamter.taskEntity.Id && x.NodeCode.Equals(node.nodePro.approverNodeId)
                    && (x.HandleStatus == 0 || x.HandleStatus == 1) && x.ParentId == "0"
                    && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt() && x.HandleId != "jnpf").Select(x => x.HandleId).Distinct().ToList();
                    break;

                // 数据接口(接口结构为{"code":200,"data":{"handleId":"admin"},"msg":""})
                case (int)WorkFlowOperatorTypeEnum.ServiceApprover:
                    try
                    {
                        if (node.nodePro.interfaceConfig.IsNotEmptyOrNull() && node.nodePro.interfaceConfig.interfaceId.IsNotEmptyOrNull())
                        {
                            var parameters = new Dictionary<string, object>();
                            var globalParameter = new Dictionary<string, string>();
                            if (wfParamter.taskEntity.IsNotEmptyOrNull())
                            {
                                parameters.Add("@flowId", wfParamter.taskEntity.FlowId);
                                parameters.Add("@taskId", wfParamter.taskEntity.Id);
                                parameters.Add("@flowFullName", wfParamter.taskEntity.FlowName);
                                parameters.Add("@taskFullName", wfParamter.taskEntity.FullName);
                                parameters.Add("@launchUserId", wfParamter.taskEntity.CreatorUserId);
                                parameters.Add("@taskNodeId", wfParamter.node?.nodeCode);
                                parameters.Add("@launchUserName", _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId));
                                if (wfParamter.taskEntity.GlobalParameter.IsNotEmptyOrNull())
                                {
                                    globalParameter = wfParamter.taskEntity.GlobalParameter.ToObject<Dictionary<string, string>>();
                                    foreach (var item in node.nodePro.interfaceConfig.templateJson)
                                    {
                                        if (item.sourceType == 5 && globalParameter.ContainsKey(item.relationField.ToString()))
                                        {
                                            item.relationField = globalParameter[item.relationField.ToString()];
                                        }
                                    }
                                }
                            }
                            else
                            {
                                parameters.Add("@flowId", wfParamter.flowInfo.flowId);
                                parameters.Add("@flowFullName", wfParamter.flowInfo.fullName);
                                parameters.Add("@launchUserId", _userManager.UserId);
                                parameters.Add("@taskNodeId", wfParamter.node?.nodeCode);
                                parameters.Add("@launchUserName", _userManager.User.RealName);
                                globalParameter = wfParamter.globalPro.globalParameterList.ToDictionary(x => x.fieldName, y => y.defaultValue?.ToString());
                            }
                            foreach (var item in node.nodePro.interfaceConfig.templateJson)
                            {
                                if (item.sourceType == 5 && globalParameter.ContainsKey(item.relationField.ToString()))
                                {
                                    item.relationField = globalParameter[item.relationField.ToString()];
                                }
                            }
                            parameters.Add("@flowOperatorUserId", _userManager.UserId);
                            parameters.Add("@flowOperatorUserName", _userManager.User.RealName);
                            var input = new DataInterfacePreviewInput
                            {
                                paramList = node.nodePro.interfaceConfig.templateJson,
                                tenantId = _userManager.TenantId,
                                sourceData = wfParamter.nextNodeFormData.IsNotEmptyOrNull() ? wfParamter.nextNodeFormData : wfParamter.formData,
                                systemParamter = parameters,
                            };
                            var reuslt = await _dataInterfaceService.GetResponseByType(node.nodePro.interfaceConfig.interfaceId, 3, input);
                            if (reuslt.IsNotEmptyOrNull())
                            {
                                if (reuslt.ToJsonString().FirstOrDefault().Equals('['))
                                {
                                    var reusltList = reuslt.ToObject<List<Dictionary<string, object>>>();
                                    userIdList = reusltList.Where(x => x.ContainsKey("handleId")).Select(x => x["handleId"].ToString()).ToList();
                                }
                                else
                                {
                                    var jobj = reuslt.ToObject<JObject>();
                                    if (jobj.ContainsKey("handleId"))
                                    {
                                        userIdList = jobj["handleId"].ToString().Split(",").ToList();
                                    }
                                }
                            }
                        }
                    }
                    catch (AppFriendlyException)
                    {
                        break;
                    }

                    break;

                // 逐级审批.
                case (int)WorkFlowOperatorTypeEnum.StepApprover:
                    var steplaunchUser = node.nodePro.approversConfig.start == 2 ? wfParamter.operatorEntity?.HandleId : userEntity.Id;
                    if (steplaunchUser.IsNotEmptyOrNull() && steplaunchUser != "jnpf")
                    {
                        var crDirector = GetManagerByLevel(steplaunchUser, 1);
                        if (crDirector.Any())
                            userIdList.AddRange(crDirector);
                    }
                    break;

                default:
                    userIdList = GetUserDefined(node.nodePro, wfParamter.taskEntity?.Id);
                    break;
            }
        }
        userIdList = userIdList.Intersect(userAllList.Select(x => x.Id)).ToList(); // 过滤掉作废人员和非用户人员
        return userIdList.Distinct().ToList();

    }

    /// <summary>
    /// 获取级别主管.
    /// </summary>
    /// <param name="managerId">主管id.</param>
    /// <param name="level">级别.</param>
    /// <returns></returns>
    public List<string> GetManagerByLevel(string userId, int level, bool isAll = false)
    {
        var managerIds = new List<string>();
        var posIds = new List<string>();
        if (isAll)
        {
            posIds = _userRelationService.GetListByExp(x => x.UserId == userId && x.ObjectType == CommonConst.RelationObjectPosition).Select(x => x.ObjectId).ToList();
        }
        else
        {
            var user = _usersService.GetInfoByUserId(userId);
            if (user.IsNotEmptyOrNull())
                posIds.Add(user.PositionId);
        }
        foreach (var posId in posIds)
        {
            var userIdList = _usersService.GetUserManager(posId);
            if (userIdList.Any() && userIdList.Count >= level)
            {
                var managerId = userIdList.GetIndex(level - 1);
                managerIds.Add(managerId);
            }
        }
        return managerIds;
    }

    /// <summary>
    /// 递归获取加签人.
    /// </summary>
    /// <param name="id">经办id.</param>
    /// <param name="operatorList">所有经办.</param>
    /// <returns></returns>
    public async Task<List<WorkFlowOperatorEntity>> GetOperator(string id, List<WorkFlowOperatorEntity> operatorList)
    {
        var childEntity = _repository.GetOperatorInfo(x => x.ParentId == id && x.Status != -1);
        if (childEntity.IsNotEmptyOrNull())
        {
            childEntity.Status = -1;
            operatorList.Add(childEntity);
            return await GetOperator(childEntity.Id, operatorList);
        }
        else
        {
            return operatorList;
        }
    }

    /// <summary>
    /// 获取自定义人员.
    /// </summary>
    /// <param name="nodePro">节点属性.</param>
    /// <param name="userType">0：审批人员，1：抄送人员.</param>
    /// <returns></returns>
    public List<string> GetUserDefined(NodeProperties nodePro, string taskId, int userType = 0)
    {
        var userIdList = userType == 0 ? nodePro.approvers : nodePro.circulateUser;
        // 依次审批人员
        if (nodePro.counterSign == 2 && userType == 0 && nodePro.approversSortList.Any())
        {
            userIdList = nodePro.approversSortList;
        }
        var input = new UserConditionInput();
        var launchUserEntity = _repository.GetLaunchUserInfo(x => x.TaskId == taskId && x.Type == 1);
        if (launchUserEntity.IsNullOrEmpty())
        {
            launchUserEntity = new WorkFlowLaunchUserEntity()
            {
                CreatorUserId = _userManager.UserId,
                OrganizeId = _userManager.User.OrganizeId,
                PositionId = _userManager.User.PositionId,
                ManagerId = _userManager.CurrentUserSupervisor.ToJsonStringOld(),
                Subordinate = _userManager.GetSubordinates(_userManager.UserId, true).ToJsonStringOld()
            };
        }
        input.type = userType == 0 ? nodePro.extraRule : nodePro.extraCopyRule;
        input.userIds = userIdList;
        input.userId = launchUserEntity.CreatorUserId;
        input.pagination = new PageInputBase();
        userIdList = _userRelationService.GetUserList(input).Select(x => x.id).ToList();
        return userIdList;
    }

    /// <summary>
    /// 获取抄送人.
    /// </summary>
    /// <param name="wfParamter">任务参数.</param>
    /// <param name="handleStatus">审批类型（0：拒绝，1：同意）.</param>
    public async Task GetflowTaskCirculateEntityList(WorkFlowParamter wfParamter, int handleStatus)
    {
        var circulateUserList = wfParamter.copyIds.IsNotEmptyOrNull() ? wfParamter.copyIds.Split(",").ToList() : new List<string>();
        #region 抄送人
        if (handleStatus == 1)
        {
            var userList = GetUserDefined(wfParamter.nodePro, wfParamter.taskEntity.Id, 1);
            circulateUserList = circulateUserList.Union(userList).ToList();
            if (wfParamter.nodePro.isInitiatorCopy)
            {
                circulateUserList.Add(wfParamter.taskEntity.CreatorUserId);
            }
            if (wfParamter.nodePro.isFormFieldCopy)
            {
                var jd = wfParamter.formData.ToObject<JObject>();
                var fieldValueList = new List<string>();
                var copyFormField = wfParamter.nodePro.copyFormField;
                if (jd.ContainsKey(copyFormField))
                {
                    if (jd[copyFormField] is JArray)
                    {
                        fieldValueList = jd[copyFormField].ToObject<List<string>>();
                    }
                    else
                    {
                        if (jd[copyFormField].ToString().IsNotEmptyOrNull())
                        {
                            fieldValueList = jd[copyFormField].ToString().Split(",").ToList();
                        }
                    }
                }
                var userIdCopyList = _userRelationService.GetUserId(fieldValueList);
                circulateUserList = circulateUserList.Union(userIdCopyList).ToList();
            }
        }
        foreach (var item in circulateUserList.Distinct())
        {
            wfParamter.circulateEntityList.Add(new WorkFlowCirculateEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                UserId = item,
                NodeCode = wfParamter.node.nodeCode,
                NodeName = wfParamter.nodePro.nodeName,
                NodeId = wfParamter.operatorEntity.NodeId,
                TaskId = wfParamter.operatorEntity.TaskId,
                OperatorId = wfParamter.operatorEntity.Id,
                CreatorTime = DateTime.Now,
                Read = 0
            });
        }
        #endregion
    }

    public async Task<string> GetApproverUserName(WorkFlowNodeModel node, WorkFlowParamter wfParamter)
    {
        var userNameList = new List<string>();
        var userName = _userManager.GetUserName(wfParamter.taskEntity.CreatorUserId);
        if (node.nodeType.Equals(WorkFlowNodeTypeEnum.start.ToString()))
        {
            userNameList.Add(userName);
        }
        else if (node.nodeType.Equals(WorkFlowNodeTypeEnum.subFlow.ToString()))
        {
            var userIdList = _repository.GetTaskList(x => x.ParentId == wfParamter.taskEntity.Id && x.SubCode == node.nodeCode && x.DeleteMark == null).Select(x => x.CreatorUserId).Distinct().ToList();
            if (!userIdList.Any())
            {
                userIdList = await GetFlowUserId(wfParamter, node);
            }
            await GetUserNameDefined(node.nodePro, wfParamter.taskEntity.Id, userNameList, userIdList);
        }
        else
        {
            if (wfParamter.isRevoke)
            {
                var operatorList = _repository.GetOperatorList(x => x.TaskId == wfParamter.revokeEntity.TaskId && x.NodeCode == node.nodeCode && x.HandleStatus == 1 && x.Completion == 1 && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                var userIdList = operatorList.Select(x => x.HandleId).Distinct().ToList();
                await GetUserNameDefined(node.nodePro, wfParamter.taskEntity.Id, userNameList, userIdList);
            }
            else
            {
                var operatorList = _repository.GetOperatorList(x => x.TaskId == wfParamter.taskEntity.Id && x.NodeCode == node.nodeCode && x.ParentId == "0" && x.Status != WorkFlowOperatorStatusEnum.Invalid.ParseToInt());
                var userIdList = operatorList.Select(x => x.HandleId).Distinct().ToList();
                if (node.nodePro.counterSign == 2 && operatorList.FirstOrDefault().IsNotEmptyOrNull() && operatorList.FirstOrDefault().HandleAll.IsNotEmptyOrNull())
                {
                    userIdList.AddRange(operatorList.FirstOrDefault().HandleAll.Split(",").ToList<string>());
                }
                if (!userIdList.Any() && node.nodePro.assigneeType != WorkFlowOperatorTypeEnum.StepApprover.ParseToInt())
                {
                    userIdList = await GetFlowUserId(wfParamter, node);
                }
                await GetUserNameDefined(node.nodePro, wfParamter.taskEntity.Id, userNameList, userIdList);
            }
        }
        return string.Join(",", userNameList.Distinct());
    }

    /// <summary>
    /// 获取自定义人员名称.
    /// </summary>
    /// <param name="approversProperties">节点属性.</param>
    /// <param name="userNameList">用户名称容器.</param>
    /// <param name="userIdList">用户id容器.</param>
    /// <returns></returns>
    public async Task GetUserNameDefined(NodeProperties approversProperties, string taskId, List<string> userNameList, List<string> userIdList = null)
    {
        if (userIdList == null)
        {
            userIdList = GetUserDefined(approversProperties, taskId).Distinct().ToList();
        }
        foreach (var item in userIdList)
        {
            var name = _userManager.GetUserName(item);
            if (name.IsNotEmptyOrNull())
                userNameList.Add(name);
        }
    }

    /// <summary>
    /// 获取下一节点异常节点.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <param name="nextNodeList"></param>
    /// <returns></returns>
    public async Task GetErrorNode(WorkFlowParamter wfParamter, List<WorkFlowNodeModel> nextNodeList)
    {
        foreach (var item in nextNodeList)
        {
            if (WorkFlowNodeTypeEnum.approver.ToString().Equals(item.nodeType) || WorkFlowNodeTypeEnum.processing.ToString().Equals(item.nodeType))
            {
                var list = await GetFlowUserId(wfParamter, item);
                if (list.Count == 0)
                {
                    if (wfParamter.globalPro.errorRule == 3 && !wfParamter.errorNodeList.Select(x => x.nodeCode).Contains(item.nodeCode) && !wfParamter.errorRuleUserList.ContainsKey(item.nodeCode))
                    {
                        if (!_repository.GetCandidates(item.nodeCode, wfParamter.taskEntity.Id, 2).Any())
                        {
                            var candidateItem = item.Adapt<CandidateModel>();
                            wfParamter.errorNodeList.Add(candidateItem);
                        }
                    }
                    if (wfParamter.globalPro.errorRule == 5)
                        throw Oops.Oh(ErrorCode.WF0035);
                }
            }
        }
    }

    /// <summary>
    /// 发起按钮权限控制.
    /// </summary>
    /// <param name="wfParamter"></param>
    /// <returns></returns>
    public async Task<BtnProperties> GetLaunchBtn(WorkFlowParamter wfParamter)
    {
        var btnPro = new BtnProperties();
        // 撤回/退回只能遵循发起时选择的按钮再次提交
        if (wfParamter.taskEntity.IsNotEmptyOrNull() && (wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.SendBack.ParseToInt() || wfParamter.taskEntity.Status == WorkFlowTaskStatusEnum.Recall.ParseToInt()))
        {
            btnPro.hasSaveBtn = true;
            if (wfParamter.taskEntity.DelegateUserId.IsNotEmptyOrNull())
            {
                btnPro.hasDelegateSubmitBtn = true;
            }
            else
            {
                btnPro.hasSubmitBtn = true;
            }
            return btnPro;
        }

        if (_userManager.IsAdministrator)
        {
            btnPro.hasSaveBtn = true;
            btnPro.hasSubmitBtn = true;
        }
        else
        {
            var systemIds = await _repository.GetAuthorIdList(CommonConst.AuthorizeItemSystem);
            var flowIds_myself = _repository.GetFlowIdList(_userManager.UserId, systemIds); // 自己可发起
            if (flowIds_myself.Contains(wfParamter.flowInfo.templateId))
            {
                btnPro.hasSaveBtn = true;
                btnPro.hasSubmitBtn = true;
            }
        }
        btnPro.hasDelegateSubmitBtn = GetDelageteBtn(wfParamter.flowInfo);
        if (wfParamter.taskEntity.IsNotEmptyOrNull() && wfParamter.taskEntity.ParentId != "0") // 子流程忽略权限
        {
            btnPro.hasDelegateSubmitBtn = false;
            btnPro.hasSaveBtn = true;
            btnPro.hasSubmitBtn = true;
        }
        return btnPro;
    }

    /// <summary>
    /// 获取委托发起按钮.
    /// </summary>
    /// <param name="flowInfo"></param>
    /// <returns></returns>
    private bool GetDelageteBtn(FlowModel flowInfo)
    {
        // 选择流程
        var delegateList = _repository.GetDelegateList(x => x.Type == 0 && !SqlFunc.IsNullOrEmpty(x.FlowId) && x.FlowId.Contains(flowInfo.templateId) && x.EndTime > DateTime.Now && x.StartTime < DateTime.Now && x.DeleteMark == null);
        // 全部流程
        var delegateListAll = _repository.GetDelegateList(x => x.Type == 0 && x.FlowName == "全部流程" && x.EndTime > DateTime.Now && x.StartTime < DateTime.Now && x.DeleteMark == null);
        if (flowInfo.visibleType == 2)
        {
            // 当前流程可发起人员
            var objlist = _repository.GetObjIdList(flowInfo.templateId);
            var userIdList = _userRelationService.GetUserId(objlist);
            delegateList = delegateList.FindAll(x => userIdList.Contains(x.UserId));
            delegateListAll = delegateListAll.FindAll(x => userIdList.Contains(x.UserId));
        }
        var delegateIds = delegateList.Union(delegateListAll).Select(x => x.Id).Distinct().ToList();
        // 委托/代理给当前用户的数据
        var delegateInfos = _repository.GetDelegateInfoList(x => x.ToUserId == _userManager.UserId && x.Status == 1 && delegateIds.Contains(x.DelegateId));
        return delegateInfos.Any();
    }

    /// <summary>
    /// 验证发起人是否有发起流程权限.
    /// </summary>
    /// <param name="flowTaskSubmitModel"></param>
    /// <returns></returns>
    public async Task GetLaunchAuthorize(FlowTaskSubmitModel flowTaskSubmitModel)
    {
        if (flowTaskSubmitModel.hasPermission && flowTaskSubmitModel.parentId == "0")
        {
            var crUser = flowTaskSubmitModel.crUser.IsEmpty() ? _userManager.UserId : flowTaskSubmitModel.crUser;
            var userEntity = _usersService.GetInfoByUserId(crUser);
            if (userEntity.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.WF0052);
            var isNormal = crUser != _userManager.GetAdminUserId(); // 普通用户
            if (isNormal)
            {
                var flowInfo = _repository.GetFlowInfo(flowTaskSubmitModel.flowId);
                if (flowInfo == null) throw Oops.Oh(ErrorCode.WF0052);
                var fIds = await _userManager.GetUserAuthorizeItemIdList(CommonConst.AuthorizeItemFlow, crUser);
                if (flowInfo.visibleType == 2 && !fIds.Contains(flowInfo.id))
                {
                    throw Oops.Oh(ErrorCode.WF0052);
                }
            }
        }
    }

    /// <summary>
    /// 超时转审人员.
    /// </summary>
    /// <param name="timeOutConfig"></param>
    /// <param name="wfParamter"></param>
    /// <param name="handleId"></param>
    /// <returns></returns>
    public async Task<string> GetOverTimeUserId(TimeOutConfig timeOutConfig, WorkFlowParamter wfParamter, string handleId)
    {
        var userIds = new List<string>();
        if (timeOutConfig.overTimeType == 6)
        {
            userIds = _userRelationService.GetUserId(new List<string> { timeOutConfig.reApprovers });
        }
        else if (timeOutConfig.overTimeType == 9)
        {
            try
            {
                if (timeOutConfig.interfaceId.IsNotEmptyOrNull())
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
                    foreach (var item in timeOutConfig.templateJson)
                    {
                        if (item.sourceType == 5 && globalParameter.ContainsKey(item.relationField.ToString()))
                        {
                            item.relationField = globalParameter[item.relationField.ToString()];
                        }
                    }
                    var input = new DataInterfacePreviewInput
                    {
                        paramList = timeOutConfig.templateJson,
                        tenantId = _userManager.TenantId,
                        sourceData = wfParamter.formData,
                        systemParamter = parameters,
                    };
                    var reuslt = await _dataInterfaceService.GetResponseByType(timeOutConfig.interfaceId, 3, input);
                    if (reuslt.IsNotEmptyOrNull())
                    {
                        var handleIdList = new List<string>();
                        if (reuslt.ToJsonString().FirstOrDefault().Equals('['))
                        {
                            var reusltList = reuslt.ToObject<List<Dictionary<string, object>>>();
                            handleIdList = reusltList.Where(x => x.ContainsKey("handleId")).Select(x => x["handleId"].ToString()).ToList();
                        }
                        else
                        {
                            var jobj = reuslt.ToObject<JObject>();
                            if (jobj.ContainsKey("handleId"))
                            {
                                handleIdList = jobj["handleId"].ToString().Split(",").ToList();
                            }
                        }
                        var userList2 = await _usersService.GetUserListByExp(x => x.DeleteMark == null, u => new UserEntity() { Id = u.Id });
                        // 利用list交集方法过滤非用户数据
                        userIds = handleIdList.Intersect(userList2.Select(x => x.Id)).ToList();
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        else if (timeOutConfig.overTimeType == 11)
        {
            var input = new UserConditionInput()
            {
                type = timeOutConfig.overTimeExtraRule,
                pagination = new PageInputBase(),
                userId = handleId,
                userIds = (await _usersService.GetList()).Select(x => x.Id).ToList()
            };
            var data = _userRelationService.GetUserList(input);
            var ids = data.Select(x => x.id).ToList();
            ids.Remove(handleId);
            return ids.FirstOrDefault();
        }
        if (userIds.Any())
        {
            return userIds.FirstOrDefault();
        }
        else
            return string.Empty;
    }
}
