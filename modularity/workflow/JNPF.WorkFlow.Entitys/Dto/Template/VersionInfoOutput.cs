namespace JNPF.WorkFlow.Entitys.Dto.Template;

public class VersionInfoOutput
{
    /// <summary>
    /// 流程id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 流程版本id.
    /// </summary>
    public string? flowId { get; set; }

    /// <summary>
    /// 流程xml.
    /// </summary>
    public string? flowXml { get; set; }

    /// <summary>
    /// 流程设置.
    /// </summary>
    public string? configuration { get; set; }

    /// <summary>
    /// 节点列表.
    /// </summary>
    public Dictionary<string, object> flowNodes { get; set; }

    /// <summary>
    /// 操作类型 0-保存 1-发布.
    /// </summary>
    public int type { get; set; } = 0;

    /// <summary>
    /// 流程名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 流程设置.
    /// </summary>
    public string flowConfig { get; set; }

}
