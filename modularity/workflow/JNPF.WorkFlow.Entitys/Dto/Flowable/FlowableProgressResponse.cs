namespace JNPF.WorkFlow.Entitys.Dto.Flowable;

public class FlowableProgressResponse
{
    /// <summary>
    /// flowable任务id(节点id).
    /// </summary>
    public string taskId { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string code { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public string startTime { get; set; }
}
