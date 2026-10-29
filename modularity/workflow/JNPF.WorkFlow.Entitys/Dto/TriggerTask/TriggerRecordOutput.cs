namespace JNPF.WorkFlow.Entitys.Dto.TriggerTask;

public class TriggerRecordOutput
{
    /// <summary>
    /// 主键id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 触发主键.
    /// </summary>
    public string triggerId { get; set; }

    /// <summary>
    /// 标准任务主键.
    /// </summary>
    public string taskId { get; set; }

    /// <summary>
    /// 节点id.
    /// </summary>
    public string nodeId { get; set; }

    /// <summary>
    /// 节点编号.
    /// </summary>
    public string nodeCode { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    public string nodeName { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public DateTime? endTime { get; set; }

    /// <summary>
    /// 状态（0-通过 1-异常）.
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 错误提示.
    /// </summary>
    public string errorTip { get; set; }

    /// <summary>
    /// 错误数据.
    /// </summary>
    public string errorData { get; set; }
}
