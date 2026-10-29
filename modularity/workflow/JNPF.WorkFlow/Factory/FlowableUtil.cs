using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.FriendlyException;
using JNPF.RemoteRequest.Extensions;
using JNPF.UnifyResult.Internal;
using JNPF.WorkFlow.Entitys.Dto.Flowable;
using JNPF.WorkFlow.Interfaces.Manager;
using System.Text;
using System.Web;

namespace JNPF.WorkFlow.Factory;

public class FlowableUtil : IBpmnEngine
{
    private readonly string flowablePath = App.GetConfig<AppOptions>("JNPF_App", true).FlowableDomain;

    #region 流程定义
    public async Task<string> DefinitionDeploy(string bpmnXml)
    {
        var request = new FlowableDefinitionRequest { bpmnXml = HttpUtility.UrlDecode(bpmnXml, Encoding.UTF8) };
        var path = flowablePath + "definition/deploy";
        var response = string.Empty;
        try
        {
            response = await path.SetBody(request).PostAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<FlowableDefinitionResponse>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "部署", result.msg);
        return result.data.deploymentId;
    }

    public async Task DefinitionDelete(string flowableIds)
    {
        try
        {
            foreach (var flowableId in flowableIds.Split(","))
            {
                var request = new FlowableDefinitionRequest { deploymentId = flowableId };
                var path = flowablePath + "definition";
                var response = await path.SetQueries(request).DeleteAsStringAsync();
            }
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
    }
    #endregion

    #region 流程任务
    public async Task<string> InstanceStart(string flowableId, Dictionary<string, bool> pairs)
    {
        var request = new FlowableInstanceRequest { deploymentId = flowableId, variables = pairs };
        var path = flowablePath + "instance/start";
        var response = string.Empty;
        try
        {
            response = await path.SetBody(request).PostAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<FlowableInstanceResponse>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程实例创建", result.msg);
        return result.data.instanceId;
    }

    public async Task<FlowableInstanceResponse> InstanceInfo(string instanceId)
    {
        var path = flowablePath + "instance/" + instanceId;
        var response = string.Empty;
        try
        {
            response = await path.GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<FlowableInstanceResponse>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程实例信息", result.msg);
        return result.data;
    }

    public async Task InstanceDelete(string instanceIds)
    {
        try
        {
            foreach (var instanceId in instanceIds.Split(","))
            {
                var request = new FlowableInstanceRequest { instanceId = instanceId };
                var path = flowablePath + "instance";
                var response = await path.SetQueries(request).DeleteAsStringAsync();
            }
        }
        catch (Exception)
        {
        }
    }
    #endregion

    #region 流程节点
    public async Task<List<FlowableNodeResponse>> GetCurrentNodeList(string instanceId)
    {
        var path = flowablePath + "task/list/" + instanceId;
        var response = string.Empty;
        try
        {
            response = await path.GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<FlowableNodeResponse>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程当前节点", result.msg);
        return result.data;
    }

    public async Task<List<string>> GetLineCode(string flowableId, string taskKey, string taskId)
    {
        var request = new FlowableNodeRequest { deploymentId = flowableId, taskKey = taskKey, taskId = taskId };
        var path = flowablePath + "task/outgoing/flows";
        var response = string.Empty;
        try
        {
            response = await path.SetQueries(request).GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程当前节点连线", result.msg);
        return result.data;
    }

    public async Task<string> GetLineNextNode(string flowableId, string flowKey)
    {
        var request = new FlowableNodeRequest { deploymentId = flowableId, flowKey = flowKey };
        var path = flowablePath + "task/flow/target";
        var response = string.Empty;
        try
        {
            response = await path.SetQueries(request).GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程连线指向节点", result.msg);
        return result.data.FirstOrDefault();
    }

    public async Task<List<FlowableNodeResponse>> GetNextNode(string flowableId, string taskKey, string taskId)
    {
        var request = new FlowableNodeRequest { deploymentId = flowableId, taskKey = taskKey, taskId = taskId };
        var path = flowablePath + "task/next";
        var response = string.Empty;
        try
        {
            response = await path.SetQueries(request).GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<FlowableNodeResponse>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程下一节点", result.msg);
        return result.data;
    }

    public async Task<bool> ComplateNode(string nodeId, Dictionary<string, bool> pairs)
    {
        var request = new FlowableNodeRequest { taskId = nodeId, variables = pairs };
        var path = flowablePath + "task/complete";
        var response = string.Empty;
        try
        {
            response = await path.SetBody(request).PostAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<bool>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程节点完成", result.msg);
        return result.data;
    }

    public async Task<List<string>> GetNodeIncoming(string nodeId)
    {
        var path = flowablePath + "task/incoming/flows/" + nodeId;
        var response = string.Empty;
        try
        {
            response = await path.GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程节点进线", result.msg);
        return result.data;
    }

    public async Task<List<string>> GetPrevNode(string flowableId, string taskKey, string taskId)
    {
        var request = new FlowableNodeRequest { deploymentId = flowableId, taskKey = taskKey, taskId = taskId };
        var path = flowablePath + "task/prev";
        var response = string.Empty;
        try
        {
            response = await path.SetQueries(request).GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程上一节点", result.msg);
        return result.data;
    }

    public async Task<List<string>> GetPrevNodeAll(string taskId)
    {
        var path = flowablePath + "task/fallbacks/" + taskId;
        var response = string.Empty;
        try
        {
            response = await path.GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程所有上级节点", result.msg);
        return result.data;
    }

    public async Task<List<string>> SendBackNode(string taskId, string targetKey)
    {
        var request = new FlowableNodeRequest { targetKey = targetKey, taskId = taskId };
        var path = flowablePath + "task/back";
        var response = string.Empty;
        try
        {
            response = await path.SetBody(request).PostAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程退回节点", result.msg);
        return result.data;
    }

    public async Task<List<string>> NoPassNode(string instanceId)
    {
        var path = flowablePath + "task/tobe/pass/" + instanceId;
        var response = string.Empty;
        try
        {
            response = await path.GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        //if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程未经过的节点", result.msg);
        return result.data.IsNotEmptyOrNull() && result.data.Any() ? result.data : new List<string>();
    }

    public async Task<bool> JumpNode(string instanceId, string currentNode, string jumpNode)
    {
        var request = new Dictionary<string, object>();
        request.Add("instanceId", instanceId);
        request.Add("source", currentNode.Split(","));
        request.Add("target", jumpNode.Split(","));
        var path = flowablePath + "task/jump";
        var response = string.Empty;
        try
        {
            response = await path.SetBody(request).PostAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<bool>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程节点跳转", result.msg);
        return result.data;
    }

    public async Task<List<string>> AfterNode(string deploymentId, string currentNode)
    {
        var request = new Dictionary<string, object>();
        request.Add("deploymentId", deploymentId);
        request.Add("taskKeys", currentNode.Split(","));
        var path = flowablePath + "task/after";
        var response = string.Empty;
        try
        {
            response = await path.SetBody(request).PostAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程指定节点下所有节点", result.msg);
        return result.data;
    }

    public async Task<List<FlowableNodeResponse>> Compensation(string instanceId, string currentNode)
    {
        var request = new Dictionary<string, object>();
        request.Add("instanceId", instanceId);
        request.Add("source", currentNode.Split(",").ToList());
        var path = flowablePath + "task/compensate";
        var response = string.Empty;
        try
        {
            response = await path.SetBody(request).PostAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<FlowableNodeResponse>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程异常补偿", result.msg);
        return result.data;
    }

    public async Task<List<FlowableProgressResponse>> GetHistory(string instanceId)
    {
        var path = flowablePath + "task/historic/" + instanceId;
        var response = string.Empty;
        try
        {
            response = await path.GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<FlowableProgressResponse>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程进程", result.msg);
        return result.data;
    }

    public async Task<bool> IsEnd(string instanceId)
    {
        var path = flowablePath + "task/historic/end/" + instanceId;
        var response = string.Empty;
        try
        {
            response = await path.GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<List<string>>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程任务结束节点", result.msg);
        return result.data.Any();
    }

    public async Task<List<string>> GetEndLine(string deploymentId, string targetKey)
    {
        var path = string.Format("{0}task/element/info?deploymentId={1}&key={2}", flowablePath, deploymentId, targetKey);
        var response = string.Empty;
        try
        {
            response = await path.GetAsStringAsync();
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.WF0071);
        }
        var result = response.ToObject<FlowableResult<FlowableNodeResponse>>();
        if (!result.success) throw Oops.Oh(ErrorCode.WF0020, "流程任务结束节点进出线", result.msg);
        return result.data.incomingList.Any() ? result.data.incomingList : new List<string>();
    }
    #endregion
}

