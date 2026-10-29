using JNPF.Common.Models.WorkFlow;
using JNPF.EventBus;
using Newtonsoft.Json;

namespace JNPF.Common.Core.EventBus.Sources;

/// <summary>
/// 任务流程时间数据源.
/// </summary>
public class TaskFlowEventSource : IEventSource
{
    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="eventId">事件ID.</param>
    /// <param name="userId">用户ID.</param>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="model">事件数据模型.</param>
    public TaskFlowEventSource(string eventId, TaskFlowEventModel model)
    {
        EventId = eventId;
        Model = model;
    }

    /// <summary>
    /// 集成助手模型.
    /// </summary>
    public TaskFlowEventModel Model { get; set; }

    /// <summary>
    /// 事件 Id.
    /// </summary>
    public string EventId { get; }

    /// <summary>
    /// 事件承载（携带）数据.
    /// </summary>
    public object Payload { get; }

    /// <summary>
    /// 取消任务 Token.
    /// </summary>
    /// <remarks>用于取消本次消息处理.</remarks>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// 事件创建时间.
    /// </summary>
    public DateTime CreatedTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// 消息是否只消费一次.
    /// </summary>
    public bool IsConsumOnce { get; set; }
}
