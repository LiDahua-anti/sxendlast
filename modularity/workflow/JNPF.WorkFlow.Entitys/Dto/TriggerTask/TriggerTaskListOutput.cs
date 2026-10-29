namespace JNPF.WorkFlow.Entitys.Dto.TriggerTask;

public class TriggerTaskListOutput
{
    /// <summary>
    /// 主键id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 主键id.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 发起时间.
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 是否异步.
    /// </summary>
    public int? isAsync { get; set; }

    /// <summary>
    /// 触发记录.
    /// </summary>
    public List<TriggerRecordOutput> recordList { get; set; } = new List<TriggerRecordOutput>();
}
