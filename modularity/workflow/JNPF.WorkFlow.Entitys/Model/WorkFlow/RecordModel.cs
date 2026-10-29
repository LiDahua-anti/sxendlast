using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.WorkFlow;

[SuppressSniffer]
public class RecordModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string? nodeCode { get; set; }

    /// <summary>
    /// 节点id.
    /// </summary>
    public string? nodeId { get; set; }

    /// <summary>
    /// 节点名.
    /// </summary>
    public string? nodeName { get; set; }

    /// <summary>
    /// 经办状态.
    /// </summary>
    public int? handleType { get; set; }

    /// <summary>
    /// 经办人.
    /// </summary>
    public string? handleId { get; set; }

    /// <summary>
    /// 经办时间.
    /// </summary>
    public DateTime? handleTime { get; set; }

    /// <summary>
    /// 经办意见.
    /// </summary>
    public string? handleOpinion { get; set; }

    /// <summary>
    /// 经办id.
    /// </summary>
    public string? operatorId { get; set; }

    /// <summary>
    /// 任务id.
    /// </summary>
    public string? taskId { get; set; }

    /// <summary>
    /// 经办人名称.
    /// </summary>
    public string? userName { get; set; }

    /// <summary>
    /// 签名.
    /// </summary>
    public string? signImg { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? status { get; set; } = 0;

    /// <summary>
    /// 流转操作人.
    /// </summary>
    public string? handleUserName { get; set; }

    /// <summary>
    /// 接收时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    public string? fileList { get; set; }

    /// <summary>
    /// 头像.
    /// </summary>
    public string headIcon { get; set; }

    /// <summary>
    /// 扩展字段.
    /// </summary>
    public string? expandField { get; set; }

    /// <summary>
    /// 扩展字段.
    /// </summary>
    public List<object>? approvalField { get; set; }

    /// <summary>
    /// 是否外部节点.
    /// </summary>
    public bool isOutSideNode { get; set; }
}
