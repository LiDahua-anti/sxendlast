using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.WorkFlow.Entitys.Enum;

/// <summary>
/// 任务状态枚举.
/// </summary>
[SuppressSniffer]
public enum WorkFlowTaskStatusEnum
{
    /// <summary>
    /// 待提交.
    /// </summary>
    [Description("待提交")]
    Draft = 0,

    /// <summary>
    /// 进行中.
    /// </summary>
    [Description("进行中")]
    Runing = 1,

    /// <summary>
    /// 已通过.
    /// </summary>
    [Description("已通过")]
    Pass = 2,

    /// <summary>
    /// 已拒绝.
    /// </summary>
    [Description("已拒绝")]
    Reject = 3,

    /// <summary>
    /// 已终止.
    /// </summary>
    [Description("已终止")]
    Cancel = 4,

    /// <summary>
    /// 已暂停.
    /// </summary>
    [Description("已暂停")]
    Pause = 5,

    /// <summary>
    /// 撤销中.
    /// </summary>
    [Description("撤销中")]
    Revokeing = 6,

    /// <summary>
    /// 已撤销.
    /// </summary>
    [Description("已撤销")]
    Revoke = 7,

    /// <summary>
    /// 已退回.
    /// </summary>
    [Description("已退回")]
    SendBack = 8,

    /// <summary>
    /// 已撤回.
    /// </summary>
    [Description("已撤回")]
    Recall = 9,

    /// <summary>
    /// 异常.
    /// </summary>
    [Description("异常")]
    Error = 10,
}