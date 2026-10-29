using JNPF.WorkFlow.Entitys.Dto.Flowable;

namespace JNPF.WorkFlow.Interfaces.Manager;

public interface IBpmnEngine
{
    #region 流程定义
    Task<string> DefinitionDeploy(string bpmnXml);

    Task DefinitionDelete(string flowableIds);
    #endregion

    #region 流程任务
    Task<string> InstanceStart(string flowableId, Dictionary<string, bool> pairs);

    Task<FlowableInstanceResponse> InstanceInfo(string instanceId);

    Task InstanceDelete(string instanceIds);
    #endregion

    #region 流程节点
    Task<List<FlowableNodeResponse>> GetCurrentNodeList(string instanceId);

    Task<List<string>> GetLineCode(string flowableId, string taskKey, string taskId);

    Task<string> GetLineNextNode(string flowableId, string flowKey);

    Task<List<FlowableNodeResponse>> GetNextNode(string flowableId, string taskKey, string taskId);

    Task<bool> ComplateNode(string nodeId, Dictionary<string, bool> pairs);

    Task<List<string>> GetNodeIncoming(string nodeId);

    Task<List<string>> GetPrevNode(string flowableId, string taskKey, string taskId);

    Task<List<string>> GetPrevNodeAll(string taskId);

    Task<List<string>> SendBackNode(string taskId, string targetKey);

    Task<List<string>> NoPassNode(string instanceId);

    Task<bool> JumpNode(string instanceId, string currentNode, string jumpNode);

    Task<List<string>> AfterNode(string deploymentId, string currentNode);

    Task<List<FlowableNodeResponse>> Compensation(string instanceId, string currentNode);

    Task<List<FlowableProgressResponse>> GetHistory(string instanceId);

    Task<bool> IsEnd(string instanceId);

    Task<List<string>> GetEndLine(string deploymentId, string targetKey);
    #endregion
}
