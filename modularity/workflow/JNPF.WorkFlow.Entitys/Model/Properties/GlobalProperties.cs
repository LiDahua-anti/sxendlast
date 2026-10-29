using JNPF.Common.Dtos.Datainterface;
using JNPF.DependencyInjection;
using JNPF.WorkFlow.Entitys.Model.Conifg;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

[SuppressSniffer]
public class GlobalProperties
{
    /// <summary>
    /// 实例标题类型 0：默认 1：自定义.
    /// </summary>
    public int titleType { get; set; } = 0;

    /// <summary>
    /// 实例标题格式.
    /// </summary>
    public string titleContent { get; set; }

    /// <summary>
    /// 审批任务是否签收.
    /// </summary>
    public bool hasSignFor { get; set; }

    /// <summary>
    /// 拒绝后允许流程继续流转审批.
    /// </summary>
    public bool hasContinueAfterReject { get; set; }

    /// <summary>
    /// 允许审批节点独立配置表单.
    /// </summary>
    public bool hasAloneConfigureForms { get; set; }

    /// <summary>
    /// 允许逾期催办.
    /// </summary>
    public bool hasInitiatorPressOverdueNode { get; set; }

    /// <summary>
    /// 自动提交规则.
    /// </summary>
    public AutoSubmitConfig autoSubmitConfig { get; set; }

    /// <summary>
    /// 流程撤回规则  1: 不允许撤回  2: 发起节点允许撤回  3:所有节点允许撤回.
    /// </summary>
    public int recallRule { get; set; } = 1;

    /// <summary>
    /// 异常处理规则  1：超级管理员  2：指定人员   3：上一节点审批人指定  4：默认审批通过  5：无法提交.
    /// </summary>
    public int errorRule { get; set; } = 1;

    /// <summary>
    /// 异常处理人.
    /// </summary>
    public List<string>? errorRuleUser { get; set; } = new List<string>();

    /// <summary>
    /// 流程归档配置.
    /// </summary>
    public FileConfig fileConfig { get; set; }

    /// <summary>
    /// 允许撤销.
    /// </summary>
    public bool hasRevoke { get; set; }

    /// <summary>
    /// 允许签名.
    /// </summary>
    public bool hasSign { get; set; }

    /// <summary>
    /// 允许评论.
    /// </summary>
    public bool hasComment { get; set; }

    /// <summary>
    /// 是否显示删除评论.
    /// </summary>
    public bool hasCommentDeletedTips { get; set; }

    /// <summary>
    /// 全局参数.
    /// </summary>
    public List<DataInterfaceParameter> globalParameterList { get; set; } = new List<DataInterfaceParameter>();

    /// <summary>
    /// 所有连接线编码.
    /// </summary>
    public List<string> connectList { get; set; } = new List<string>();

    #region 触发流程

    /// <summary>
    /// 执行失败消息.
    /// </summary>
    public MsgConfig? failMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 开始执行消息.
    /// </summary>
    public MsgConfig? startMsgConfig { get; set; } = new MsgConfig();

    /// <summary>
    /// 通知人.
    /// </summary>
    public List<string> msgUserIds { get; set; } = new List<string>();

    /// <summary>
    /// 通知人类型 1-创建人 2-超管 3-自定义.
    /// </summary>
    public List<int> msgUserType { get; set; } = new List<int>();
    #endregion
}
