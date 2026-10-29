using JNPF.WorkFlow.Entitys.Entity;

namespace JNPF.WorkFlow.Entitys.Dto.Template;

public class TemplateImportOutput
{
    /// <summary>
    /// 流程实例.
    /// </summary>
    public WorkFlowTemplateEntity template { get; set; }

    /// <summary>
    /// 流程版本实例.
    /// </summary>
    public WorkFlowVersionEntity version { get; set; }

    /// <summary>
    /// 流程节点.
    /// </summary>
    public List<WorkFlowNodeEntity> nodeList { get; set; } = new List<WorkFlowNodeEntity>();
}
