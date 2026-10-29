using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.WorkFlow.Entitys.Enum;

/// <summary>
/// 经办状态枚举.
/// </summary>
[SuppressSniffer]
public enum WorkFlowOperatorStatusEnum
{
    /// <summary>
    /// 无效.
    /// </summary>
    [Description("无效")]
    Invalid = -1,

    /// <summary>
    /// 待签收.
    /// </summary>
    [Description("待签收")]
    WaitSign = 0,

    /// <summary>
    /// 流转中.
    /// </summary>
    [Description("流转中")]
    Runing = 1,

    /// <summary>
    /// 加签.
    /// </summary>
    [Description("加签")]
    AddSign = 2,

    /// <summary>
    /// 转办.
    /// </summary>
    [Description("转办")]
    Transfer = 3,

    /// <summary>
    /// 指派.
    /// </summary>
    [Description("指派")]
    Assigned = 4,

    /// <summary>
    /// 退回.
    /// </summary>
    [Description("退回")]
    SendBack = 5,

    /// <summary>
    /// 撤回.
    /// </summary>
    [Description("撤回")]
    Recall = 6,

    /// <summary>
    /// 协办.
    /// </summary>
    [Description("协办")]
    Assist = 7,

    /// <summary>
    /// 撤销.
    /// </summary>
    [Description("撤销")]
    Revoke = 8,

    /// <summary>
    /// 未激活.
    /// </summary>
    [Description("未激活")]
    unActivated = -2,
}