using JNPF.Common.Models.User;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys;
using JNPF.WorkFlow.Entitys.Dto.Operator;
using JNPF.WorkFlow.Entitys.Dto.Task;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Item;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using SqlSugar;
using System.Linq.Expressions;

namespace JNPF.WorkFlow.Interfaces.Repository;

public interface IWorkFlowRepository
{
    #region 流程列表

    /// <summary>
    /// 发起列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    dynamic GetLaunchList(TaskListQuery input);

    /// <summary>
    /// 监控列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    dynamic GetMonitorList(TaskListQuery input);

    /// <summary>
    /// 待签列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    dynamic GetWaitSignList(OperatorListQuery input);

    /// <summary>
    /// 待办列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    dynamic GetSignList(OperatorListQuery input);

    /// <summary>
    /// 在办列表
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    dynamic GetWaitList(OperatorListQuery input);

    /// <summary>
    /// 批量在办列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    dynamic GetBatchWaitList(OperatorListQuery input);

    /// <summary>
    /// 已办列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    dynamic GetTrialList(OperatorListQuery input);

    /// <summary>
    /// 抄送列表.
    /// </summary>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    dynamic GetCirculateList(OperatorListQuery input);

    #endregion

    #region 其他

    /// <summary>
    /// 流程信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    FlowModel GetFlowInfo(string id);

    /// <summary>
    /// 获取工作交接列表.
    /// </summary>
    /// <param name="userId">离职人.</param>
    /// <param name="type">1-待办事宜 2-负责流程.</param>
    /// <returns></returns>
    List<FlowWorkModel> GetWorkHandover(string userId, int type);

    /// <summary>
    /// 保存工作交接数据.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="taskIds"></param>
    void SaveWorkHandover(string userId, List<string> ids, int type, string handOverUserId = "");

    /// <summary>
    /// 获取组织树.
    /// </summary>
    /// <param name="orgId"></param>
    /// <returns></returns>
    List<string> GetOrgTree(string orgId);

    /// <summary>
    /// 流程任务列表.
    /// </summary>
    /// <returns></returns>
    List<OperatorListOutput> GetFlowTaskList(int type = 2, List<string> categoryList = null);

    /// <summary>
    /// 表单信息.
    /// </summary>
    /// <param name="formId"></param>
    /// <returns></returns>
    VisualDevReleaseEntity GetFromEntity(string formId);

    /// <summary>
    /// 流程列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    List<WorkFlowVersionEntity> GetFlowList(Expression<Func<WorkFlowVersionEntity, bool>> expression);

    /// <summary>
    /// 任务相关人员列表.
    /// </summary>
    /// <param name="taskId">任务id.</param>
    /// <returns></returns>
    List<string> GetTaskUserList(string taskId);

    /// <summary>
    /// 可发起流程.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isAll">是否包含公开流程.</param>
    /// <returns></returns>
    List<string> GetFlowIdList(string userId, List<string> systemIds = null, bool isAll = true);

    /// <summary>
    /// 可发起流程人员.
    /// </summary>
    /// <param name="flowId">流程id.</param>
    /// <returns></returns>
    List<string> GetObjIdList(string templateId);

    /// <summary>
    /// 是否存在归档文件.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    bool AnyFile(string taskId);

    /// <summary>
    /// 常用语使用次数.
    /// </summary>
    /// <param name="handleOpinion"></param>
    void SetCommonWordsCount(string handleOpinion);

    /// <summary>
    /// 设置默认签名.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="signImg"></param>
    void SetDefaultSignImg(string id, string signImg, bool useSignNext);

    /// <summary>
    /// 设置默认签名.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="signImg"></param>
    DictionaryDataEntity GetDictionaryData(string id);

    /// <summary>
    /// 流程模版.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isFilterStatus">是否过滤状态</param>
    /// <returns></returns>
    WorkFlowTemplateEntity GetTemplate(string id, bool isFilterStatus = true);

    /// <summary>
    /// 系统配置.
    /// </summary>
    /// <returns></returns>
    SysConfigOutput GetSysConfigInfo();

    /// <summary>
    /// 获取归档文件.
    /// </summary>
    /// <param name="templateId"></param>
    /// <param name="userId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    dynamic GetFileList(string templateId, string userId);

    /// <summary>
    /// 模版使用次数.
    /// </summary>
    /// <param name="templateId"></param>
    void SaveTemplateUseNum(string templateId);

    /// <summary>
    /// 菜单.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    List<ModuleEntity> GetModuleList(Expression<Func<ModuleEntity, bool>> expression);

    /// <summary>
    /// 根据类型获取权限id.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<string>> GetAuthorIdList(string objectType, string userId = "");
    #endregion

    #region 流程任务

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="flowId">引擎id.</param>
    /// <returns></returns>
    List<WorkFlowTaskEntity> GetTaskList(string flowId);

    /// <summary>
    /// 任务列表.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    List<WorkFlowTaskEntity> GetTaskList(Expression<Func<WorkFlowTaskEntity, bool>> expression);

    List<WorkFlowTaskEntity> GetChildTaskList(string taskId, bool isDelParentTask = false);

    /// <summary>
    /// 任务信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    WorkFlowTaskEntity GetTaskInfo(string id);

    /// <summary>
    /// 是否存在任务.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    bool AnyTask(Expression<Func<WorkFlowTaskEntity, bool>> expression);

    /// <summary>
    /// 任务删除.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<int> DeleteTask(WorkFlowTaskEntity entity, bool isDel = true);

    /// <summary>
    /// 任务创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    WorkFlowTaskEntity CreateTask(WorkFlowTaskEntity entity);

    /// <summary>
    /// 任务更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    bool UpdateTask(WorkFlowTaskEntity entity);

    /// <summary>
    /// 任务更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    bool UpdateTask(WorkFlowTaskEntity entity, Expression<Func<WorkFlowTaskEntity, object>> Expression = null);

    /// <summary>
    /// 打回流程删除所有相关数据.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="isClearRecord">是否清除记录.</param>
    /// <param name="isClearCandidates">是否清除候选人.</param>
    /// <returns></returns>
    void DeleteFlowTaskAllData(string taskId, bool isClearRecord = true, bool isClearCandidates = true);

    /// <summary>
    /// 删除子流程.
    /// </summary>
    /// <param name="flowTaskEntity"></param>
    /// <returns></returns>
    Task DeleteSubTask(WorkFlowTaskEntity flowTaskEntity);

    /// <summary>
    /// 发起任务删除.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    Task<List<string>> DeleteLaunchTask(List<string> taskIds);
    #endregion

    #region 流程节点

    /// <summary>
    /// 节点列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    List<WorkFlowNodeEntity> GetNodeList(Expression<Func<WorkFlowNodeEntity, bool>> expression, Expression<Func<WorkFlowNodeEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc);

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    bool AnyNode(Expression<Func<WorkFlowNodeEntity, bool>> expression);

    /// <summary>
    /// 节点信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    WorkFlowNodeEntity GetNodeInfo(Expression<Func<WorkFlowNodeEntity, bool>> expression);
    #endregion

    #region 流程经办

    /// <summary>
    /// 经办列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    List<WorkFlowOperatorEntity> GetOperatorList(Expression<Func<WorkFlowOperatorEntity, bool>> expression, Expression<Func<WorkFlowOperatorEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc);

    /// <summary>
    /// 获取加签数据.
    /// </summary>
    /// <param name="opId">当前经办id.</param>
    /// <param name="type">0-父级 1-子级.</param>
    /// <param name="isContainOneself">是否包含自己.</param>
    /// <returns></returns>
    List<WorkFlowOperatorEntity> GetAddSignOperatorList(string opId, int type = 0, bool isContainOneself = true);

    /// <summary>
    /// 经办信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    WorkFlowOperatorEntity GetOperatorInfo(Expression<Func<WorkFlowOperatorEntity, bool>> expression);

    /// <summary>
    /// 经办删除.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    int DeleteOperator(Expression<Func<WorkFlowOperatorEntity, bool>> expression);

    /// <summary>
    /// 经办创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    bool CreateOperator(List<WorkFlowOperatorEntity> entitys);

    /// <summary>
    /// 经办创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    bool CreateOperator(WorkFlowOperatorEntity entity);

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    bool UpdateOperator(WorkFlowOperatorEntity entity);

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    bool UpdateOperator(List<WorkFlowOperatorEntity> entitys);

    /// <summary>
    /// 经办更新.
    /// </summary>
    /// <returns></returns>
    int UpdateOperator(Expression<Func<WorkFlowOperatorEntity, bool>> filedNameExpression, Expression<Func<WorkFlowOperatorEntity, bool>> expression);
    #endregion

    #region 流程记录

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    List<WorkFlowRecordEntity> GetRecordList(string taskId);

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="orderByExpression"></param>
    /// <param name="orderByType"></param>
    /// <returns></returns>
    List<WorkFlowRecordEntity> GetRecordList(Expression<Func<WorkFlowRecordEntity, bool>> expression, Expression<Func<WorkFlowRecordEntity, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc);

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    List<RecordModel> GetRecordModelList(string taskId);

    /// <summary>
    /// 经办记录列表.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    List<UserItem> GetRecordItemList(Expression<Func<WorkFlowRecordEntity, bool>> expression);

    /// <summary>
    /// 经办记录信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    WorkFlowRecordEntity GetRecordInfo(string id);

    /// <summary>
    /// 经办记录信息.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    WorkFlowRecordEntity GetRecordInfo(Expression<Func<WorkFlowRecordEntity, bool>> expression);

    /// <summary>
    /// 经办记录创建.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    bool CreateRecord(WorkFlowRecordEntity entity);

    /// <summary>
    /// 经办记录作废.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    void DeleteRecord(List<string> ids);

    /// <summary>
    /// 经办记录作废.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    void DeleteRecord(Expression<Func<WorkFlowRecordEntity, bool>> expression);
    #endregion

    #region 流程抄送

    /// <summary>
    /// 传阅详情.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    WorkFlowCirculateEntity GetCirculateInfo(Expression<Func<WorkFlowCirculateEntity, bool>> expression);

    /// <summary>
    /// 传阅创建.
    /// </summary>
    /// <param name="entitys"></param>
    /// <returns></returns>
    bool CreateCirculate(List<WorkFlowCirculateEntity> entitys);

    /// <summary>
    /// 传阅已读.
    /// </summary>
    /// <param name="id"></param>
    void UpdateCirculate(string id);
    #endregion

    #region 流程候选人/异常处理人

    /// <summary>
    /// 候选人创建.
    /// </summary>
    /// <param name="entitys"></param>
    void CreateCandidates(List<WorkFlowCandidatesEntity> entitys);

    /// <summary>
    /// 候选人删除.
    /// </summary>
    /// <param name="expression"></param>
    void DeleteCandidates(Expression<Func<WorkFlowCandidatesEntity, bool>> expression);

    /// <summary>
    /// 候选人获取.
    /// </summary>
    /// <param name="nodeId"></param>
    List<string> GetCandidates(string nodeId, string taskId, int type = 1);

    /// <summary>
    /// 候选人获取.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    List<WorkFlowCandidatesEntity> GetCandidates(Expression<Func<WorkFlowCandidatesEntity, bool>> expression);
    #endregion

    #region 流程内置参数
    /// <summary>
    /// 根据任务id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    WorkFlowParamter GetWorkFlowParamterByTaskId(string taskId, WorkFlowHandleModel flowHandleModel);

    /// <summary>
    /// 根据经办id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    WorkFlowParamter GetWorkFlowParamterByOperatorId(string operatorId, WorkFlowHandleModel flowHandleModel);

    /// <summary>
    /// 根据经办id获取任务引擎参数.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="flowHandleModel"></param>
    /// <returns></returns>
    WorkFlowParamter GetWorkFlowParamterByFlowId(string flowId, WorkFlowHandleModel flowHandleModel);
    #endregion

    #region 流程发起人

    /// <summary>
    /// 新增任务发起人信息.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="taskId">任务id.</param>
    void CreateLaunchUser(string userId, string taskId);

    /// <summary>
    /// 新增逐级发起人信息.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="taskId">任务id.</param>
    /// <param name="taskId">节点编码.</param>
    public void CreateStepLaunchUser(string userId, string taskId, string nodeCode);

    /// <summary>
    /// 获取任务发起人信息.
    /// </summary>
    /// <param name="expression">条件.</param>
    /// <returns></returns>
    WorkFlowLaunchUserEntity GetLaunchUserInfo(Expression<Func<WorkFlowLaunchUserEntity, bool>> expression);

    /// <summary>
    /// 删除发起信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    void DeleteLaunchUser(Expression<Func<WorkFlowLaunchUserEntity, bool>> expression);

    #endregion

    #region 驳回重启数据
    /// <summary>
    /// 驳回数据信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    WorkFlowRejectDataEntity GetRejectDataInfo(string id);

    /// <summary>
    /// 驳回数据创建.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="nodeCode"></param>
    /// <returns></returns>
    string CreateRejectData(string taskId, string nodeCode, string backNodeCode);

    /// <summary>
    /// 驳回数据重启.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    string UpdateRejectData(WorkFlowRejectDataEntity entity);
    #endregion

    #region 节点流转记录

    /// <summary>
    /// 节点流转记录.
    /// </summary>
    /// <param name="entity"></param>
    List<WorkFlowNodeRecordEntity> GetNodeRecord(string taskId);

    /// <summary>
    /// 节点流转记录.
    /// </summary>
    /// <param name="entity"></param>
    WorkFlowNodeRecordEntity GetNodeRecord(string taskId, string nodeId);

    /// <summary>
    /// 节点流转记录.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    WorkFlowNodeRecordEntity GetNodeRecord(Expression<Func<WorkFlowNodeRecordEntity, bool>> expression);

    /// <summary>
    /// 节点流转记录创建.
    /// </summary>
    /// <param name="entity"></param>
    void CreateNodeRecord(WorkFlowNodeRecordEntity entity);

    /// <summary>
    /// 节点流转记录更新.
    /// </summary>
    /// <param name="entity"></param>
    bool UpdateNodeRecord(WorkFlowNodeRecordEntity entity);
    #endregion

    #region 流程撤销

    /// <summary>
    /// 撤销信息.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    WorkFlowRevokeEntity GetRevoke(Expression<Func<WorkFlowRevokeEntity, bool>> expression);

    /// <summary>
    /// 撤销创建.
    /// </summary>
    /// <param name="entity"></param>
    void CreateRevoke(WorkFlowRevokeEntity entity);

    /// <summary>
    /// 撤销删除.
    /// </summary>
    /// <param name="entity"></param>
    void DeleteRevoke(WorkFlowRevokeEntity entity);

    /// <summary>
    /// 撤销存在.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    bool AnyRevoke(Expression<Func<WorkFlowRevokeEntity, bool>> expression);
    #endregion

    #region 任务审批条件历史记录

    /// <summary>
    /// 任务审批条件历史记录信息.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    Dictionary<string, bool> GetTaskLine(string taskId);

    /// <summary>
    /// 最新执行条件线.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    List<string> GetTaskLineList(string taskId);

    /// <summary>
    /// 任务审批条件历史记录保存.
    /// </summary>
    /// <param name="entity"></param>
    void SaveTaskLine(string taskId, Dictionary<string, bool> variables);
    #endregion

    #region 委托/代理

    /// <summary>
    /// 委托/代理接受人.
    /// </summary>
    /// <param name="userId">委托/代理人.</param>
    /// <param name="templateId">模板id.</param>
    /// <param name="type">委托/代理.</param>
    /// <returns></returns>
    List<string> GetDelegateUserId(string userId, string templateId, int type);

    /// <summary>
    /// 委托/代理.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    List<WorkFlowDelegateEntity> GetDelegateList(Expression<Func<WorkFlowDelegateEntity, bool>> expression);

    /// <summary>
    /// 获取用户委托可发起流程.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<List<string>> GetDelegateFlowId(string userId);

    /// <summary>
    /// 委托/代理人.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    List<WorkFlowDelegateInfoEntity> GetDelegateInfoList(Expression<Func<WorkFlowDelegateInfoEntity, bool>> expression);
    #endregion

    #region 触发任务
    /// <summary>
    /// 触发任务列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    List<WorkFlowTriggerTaskEntity> GetTriggerTaskList(Expression<Func<WorkFlowTriggerTaskEntity, bool>> expression);

    /// <summary>
    /// 触发任务记录详情.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    WorkFlowTriggerRecordEntity GetTriggerRecordInfo(Expression<Func<WorkFlowTriggerRecordEntity, bool>> expression);

    /// <summary>
    /// 触发任务记录列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    List<WorkFlowTriggerRecordEntity> GetTriggerRecordList(Expression<Func<WorkFlowTriggerRecordEntity, bool>> expression);

    /// <summary>
    /// 触发任务更新.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    bool UpdateTriggerTask(WorkFlowTriggerTaskEntity entity);
    #endregion

    #region 子流程(依次)

    /// <summary>
    /// 子流程(依次)详情.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    WorkFlowSubTaskDataEntity GetSubTaskData(Expression<Func<WorkFlowSubTaskDataEntity, bool>> expression);

    /// <summary>
    /// 子流程(依次)新增.
    /// </summary>
    /// <param name="entitys"></param>
    void CreateSubTaskData(WorkFlowSubTaskDataEntity entity);

    /// <summary>
    /// 子流程(依次)删除.
    /// </summary>
    /// <param name="id"></param>
    void DeleteSubTaskData(string id);
    #endregion

    #region 外部事件

    /// <summary>
    /// 外部事件请求列表.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    List<WorkFlowEventLogEntity> GetEventLogList(Expression<Func<WorkFlowEventLogEntity, bool>> expression);

    /// <summary>
    /// 外部事件请求详情.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    WorkFlowEventLogEntity GetEventLog(Expression<Func<WorkFlowEventLogEntity, bool>> expression);

    /// <summary>
    /// 外部事件记录创建.
    /// </summary>
    /// <param name="entity"></param>
    void CreateEventLog(WorkFlowEventLogEntity entity);

    /// <summary>
    /// 外部事件记录更新.
    /// </summary>
    /// <param name="entity"></param>
    bool UpdateEventLog(WorkFlowEventLogEntity entity);

    /// <summary>
    /// 外部事件记录删除.
    /// </summary>
    /// <param name="expression"></param>
    void DeleteEventLog(Expression<Func<WorkFlowEventLogEntity, bool>> expression);
    #endregion
}
