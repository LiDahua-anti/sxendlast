using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.WorkFlow.Entitys.Enum;

/// <summary>
/// 任务节点类型枚举.
/// </summary>
[SuppressSniffer]
public enum WorkFlowNodeTypeEnum
{
    /// <summary>
    /// 开始.
    /// </summary>
    [Description("开始")]
    start,

    /// <summary>
    /// 审批.
    /// </summary>
    [Description("审批")]
    approver,

    /// <summary>
    /// 办理.
    /// </summary>
    [Description("办理")]
    processing,

    /// <summary>
    /// 外部.
    /// </summary>
    [Description("外部")]
    outside,

    /// <summary>
    /// 子流程.
    /// </summary>
    [Description("子流程")]
    subFlow,

    /// <summary>
    /// 条件.
    /// </summary>
    [Description("条件")]
    connect,

    /// <summary>
    /// 定时器.
    /// </summary>
    [Description("定时器")]
    timer,

    /// <summary>
    /// 结束.
    /// </summary>
    [Description("结束")]
    end,

    /// <summary>
    /// 全局.
    /// </summary>
    [Description("全局")]
    global,

    /// <summary>
    /// 触发事件.
    /// </summary>
    [Description("触发事件")]
    trigger,

    /// <summary>
    /// 事件触发.
    /// </summary>
    [Description("事件触发")]
    eventTrigger,

    /// <summary>
    /// 定时触发.
    /// </summary>
    [Description("定时触发")]
    timeTrigger,

    /// <summary>
    /// 通知触发.
    /// </summary>
    [Description("通知触发")]
    noticeTrigger,

    /// <summary>
    /// webhook触发.
    /// </summary>
    [Description("webhook触发")]
    webhookTrigger,

    /// <summary>
    /// 获取数据.
    /// </summary>
    [Description("获取数据")]
    getData,

    /// <summary>
    /// 创建数据.
    /// </summary>
    [Description("创建数据")]
    addData,

    /// <summary>
    /// 更新数据.
    /// </summary>
    [Description("更新数据")]
    updateData,

    /// <summary>
    /// 删除数据.
    /// </summary>
    [Description("删除数据")]
    deleteData,

    /// <summary>
    /// 数据接口.
    /// </summary>
    [Description("数据接口")]
    dataInterface,

    /// <summary>
    /// 消息通知.
    /// </summary>
    [Description("消息通知")]
    message,

    /// <summary>
    /// 发起审批.
    /// </summary>
    [Description("发起审批")]
    launchFlow,

    /// <summary>
    /// 创建日程.
    /// </summary>
    [Description("创建日程")]
    schedule,
}