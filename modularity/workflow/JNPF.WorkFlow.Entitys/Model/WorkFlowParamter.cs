using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model.Properties;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;

namespace JNPF.WorkFlow.Entitys.Model;

public class WorkFlowParamter : WorkFlowHandleModel
{
    /// <summary>
    /// 当前任务.
    /// </summary>
    public WorkFlowTaskEntity taskEntity { get; set; }

    /// <summary>
    /// 开始节点属性.
    /// </summary>
    public NodeProperties startPro { get; set; }

    /// <summary>
    /// 全局节点属性.
    /// </summary>
    public GlobalProperties globalPro { get; set; }

    /// <summary>
    /// 当前节点.
    /// </summary>
    public WorkFlowNodeModel node { get; set; }

    /// <summary>
    /// 所有节点.
    /// </summary>
    public List<WorkFlowNodeModel> nodeList { get; set; } = new List<WorkFlowNodeModel>();

    /// <summary>
    /// 当前节点属性.
    /// </summary>
    public NodeProperties nodePro { get; set; }

    /// <summary>
    /// 当前经办.
    /// </summary>
    public WorkFlowOperatorEntity operatorEntity { get; set; }

    /// <summary>
    /// 当前节点所有经办.
    /// </summary>
    public List<WorkFlowOperatorEntity> operatorEntityList { get; set; } = new List<WorkFlowOperatorEntity>();

    /// <summary>
    /// 流程信息.
    /// </summary>
    public FlowModel flowInfo { get; set; }

    #region 容器

    /// <summary>
    /// 下一节点所有经办.
    /// </summary>
    public List<WorkFlowOperatorEntity> nextOperatorEntityList { get; set; } = new List<WorkFlowOperatorEntity>();

    /// <summary>
    /// 当前节点抄送.
    /// </summary>
    public List<WorkFlowCirculateEntity> circulateEntityList { get; set; } = new List<WorkFlowCirculateEntity>();

    /// <summary>
    /// 异常节点.
    /// </summary>
    public List<CandidateModel> errorNodeList { get; set; } = new List<CandidateModel>();

    /// <summary>
    /// 下一节点.
    /// </summary>
    public List<WorkFlowNodeModel> triggerNodeList { get; set; } = new List<WorkFlowNodeModel>();

    /// <summary>
    /// 下一节点表单数据.
    /// </summary>
    public object? nextNodeFormData { get; set; }
    #endregion

    /// <summary>
    /// 是否自动审批.
    /// </summary>
    public bool isAuto { get; set; } = false;

    /// <summary>
    /// 是否自动审批.
    /// </summary>
    public string engineId { get; set; }

    /// <summary>
    /// 是否撤销任务.
    /// </summary>
    public bool isRevoke { get; set; } = false;

    public WorkFlowRevokeEntity revokeEntity { get; set; }
}
