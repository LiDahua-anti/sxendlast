namespace JNPF.WorkFlow.Entitys.Dto.TriggerTask;

public class TriggerTaskOutput
{
    /// <summary>
    /// 主键id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 重试任务主键id.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 重试任务开始时间.
    /// </summary>
    public DateTime? parentTime { get; set; }

    /// <summary>
    /// 发起时间.
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 状态(0-进行中 1-通过 2-异常 3-终止).
    /// </summary>
    public int? status { get; set; }

    /// <summary>
    /// 是否重试.
    /// </summary>
    public int? isRetry { get; set; }

    /// <summary>
    /// 流程状态.
    /// </summary>
    public int? templateStatus { get; set; }

    /// <summary>
    /// 所属应用.
    /// </summary>
    public string? systemName { get; set; }
}
