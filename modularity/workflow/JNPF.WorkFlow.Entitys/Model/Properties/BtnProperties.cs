namespace JNPF.WorkFlow.Entitys.Model.Properties;

public class BtnProperties : FuncProperties
{
    #region 按钮

    /// <summary>
    /// 通过按钮开关.
    /// </summary>
    public bool hasAuditBtn { get; set; }

    /// <summary>
    /// 拒绝按钮开关.
    /// </summary>
    public bool hasRejectBtn { get; set; }

    /// <summary>
    /// 退回按钮开关.
    /// </summary>
    public bool hasBackBtn { get; set; }

    /// <summary>
    /// 加签按钮开关.
    /// </summary>
    public bool hasFreeApproverBtn { get; set; }

    /// <summary>
    /// 减签按钮开关.
    /// </summary>
    public bool hasReduceApproverBtn { get; set; }

    /// <summary>
    /// 转审按钮开关.
    /// </summary>
    public bool hasTransferBtn { get; set; }

    /// <summary>
    /// 协办按钮开关.
    /// </summary>
    public bool hasAssistBtn { get; set; }

    /// <summary>
    /// 审批暂存按钮开关.
    /// </summary>
    public bool hasSaveAuditBtn { get; set; }

    /// <summary>
    /// 提交按钮开关.
    /// </summary>
    public bool hasSubmitBtn { get; set; }

    /// <summary>
    /// 发起暂存按钮开关.
    /// </summary>
    public bool hasSaveBtn { get; set; }

    /// <summary>
    /// 发起撤回按钮开关.
    /// </summary>
    public bool hasRecallLaunchBtn { get; set; }

    /// <summary>
    /// 催办按钮开关.
    /// </summary>
    public bool hasPressBtn { get; set; }

    /// <summary>
    /// 撤销按钮开关.
    /// </summary>
    public bool hasRevokeBtn { get; set; }

    /// <summary>
    /// 审批撤回按钮开关.
    /// </summary>
    public bool hasRecallAuditBtn { get; set; }

    /// <summary>
    /// 签收按钮开关.
    /// </summary>
    public bool hasSignBtn { get; set; }

    /// <summary>
    /// 办理按钮开关.
    /// </summary>
    public bool hasTransactBtn { get; set; }

    /// <summary>
    /// 退签按钮开关.
    /// </summary>
    public bool hasReduceSignBtn { get; set; }

    /// <summary>
    /// 终止按钮开关.
    /// </summary>
    public bool hasCancelBtn { get; set; }

    /// <summary>
    /// 复活按钮开关.
    /// </summary>
    public bool hasActivateBtn { get; set; }

    /// <summary>
    /// 暂停按钮开关.
    /// </summary>
    public bool hasPauseBtn { get; set; }

    /// <summary>
    /// 恢复按钮开关.
    /// </summary>
    public bool hasRebootBtn { get; set; }

    /// <summary>
    /// 协办保存按钮开关.
    /// </summary>
    public bool hasAssistSaveBtn { get; set; }

    /// <summary>
    /// 指派按钮开关.
    /// </summary>
    public bool hasAssignBtn { get; set; }

    /// <summary>
    /// 打印按钮开关.
    /// </summary>
    public bool hasPrintBtn { get; set; }

    /// <summary>
    /// 归档按钮开关.
    /// </summary>
    public bool hasFileBtn { get; set; }

    /// <summary>
    /// 发起表单查看按钮开关.
    /// </summary>
    public bool hasViewStartFormBtn { get; set; }

    /// <summary>
    /// 委托发起按钮.
    /// </summary>
    public bool hasDelegateSubmitBtn { get; set; }

    /// <summary>
    ///  代理标识.
    /// </summary>
    public bool proxyMark { get; set; }
    #endregion
}
