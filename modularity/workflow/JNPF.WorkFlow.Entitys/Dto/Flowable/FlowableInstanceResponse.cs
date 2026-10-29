namespace JNPF.WorkFlow.Entitys.Dto.Flowable;

public class FlowableInstanceResponse
{
    /// <summary>
    /// flowable实例id.
    /// </summary>
    public string instanceId { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public string startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public string endTime { get; set; }

    /// <summary>
    /// 耗时.
    /// </summary>
    public string durationInMillis { get; set; }

    /// <summary>
    /// deleteReason.
    /// </summary>
    public string deleteReason { get; set; }
}
