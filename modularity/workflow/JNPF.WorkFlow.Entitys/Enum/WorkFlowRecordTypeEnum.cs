using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.WorkFlow.Entitys.Enum;

/// <summary>
/// 操作记录枚举.
/// </summary>
[SuppressSniffer]
public enum WorkFlowRecordTypeEnum
{
    /// <summary>
    /// 拒绝.
    /// </summary>
    [Description("拒绝")]
    Reject = 0,

    /// <summary>
    /// 同意.
    /// </summary>
    [Description("同意")]
    Agree = 1,

    /// <summary>
    /// 提交.
    /// </summary>
    [Description("提交")]
    Submit = 2,

    /// <summary>
    /// 退回.
    /// </summary>
    [Description("退回")]
    SendBack = 3,

    /// <summary>
    /// 撤回.
    /// </summary>
    [Description("撤回")]
    Recall = 4,

    /// <summary>
    /// 加签.
    /// </summary>
    [Description("加签")]
    AddSign = 5,

    /// <summary>
    /// 减签.
    /// </summary>
    [Description("减签")]
    ReduceSign = 6,

    /// <summary>
    /// 转审.
    /// </summary>
    [Description("转审")]
    Transfer = 7,

    /// <summary>
    /// 暂停.
    /// </summary>
    [Description("暂停")]
    Pause = 8,

    /// <summary>
    /// 重启.
    /// </summary>
    [Description("重启")]
    Reboot = 9,

    /// <summary>
    /// 复活.
    /// </summary>
    [Description("复活")]
    Activate = 10,

    /// <summary>
    /// 指派.
    /// </summary>
    [Description("指派")]
    Assigned = 11,

    /// <summary>
    /// 催办.
    /// </summary>
    [Description("催办")]
    Press = 12,

    /// <summary>
    /// 协办.
    /// </summary>
    [Description("协办")]
    Assist = 13,

    /// <summary>
    /// 撤销.
    /// </summary>
    [Description("撤销")]
    Revoke = 14,

    /// <summary>
    /// 终止.
    /// </summary>
    [Description("终止")]
    Cancel = 15,

    /// <summary>
    /// 同意撤销.
    /// </summary>
    [Description("同意撤销")]
    AgreeRevoke = 16,

    /// <summary>
    /// 拒绝撤销.
    /// </summary>
    [Description("拒绝撤销")]
    RejectRevoke = 17,

    /// <summary>
    /// 转办.
    /// </summary>
    [Description("转办")]
    Processing = 18,
}