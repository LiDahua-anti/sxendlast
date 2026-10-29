using JNPF.Common.Dtos.Datainterface;
using JNPF.WorkFlow.Entitys.Model.Conifg;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

public class NodeProperties : MsgProperties
{
    /// <summary>
    /// 节点类型(global-全局、start-开始).
    /// </summary>
    public string? type { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string? nodeId { get; set; }

    /// <summary>
    /// 节点名.
    /// </summary>
    public string? nodeName { get; set; }

    /// <summary>
    /// 表单id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 自动审批.
    /// </summary>
    public bool hasAutoApprover { get; set; }

    /// <summary>
    /// 表单权限数据.
    /// </summary>
    public List<object>? formOperates { get; set; }

    /// <summary>
    /// 发起设置权限  1：公开  2：权限设置.
    /// </summary>
    public int launchPermission { get; set; } = 1;

    /// <summary>
    /// 打印配置.
    /// </summary>
    public PrintConfig printConfig { get; set; }

    /// <summary>
    /// 继承父流程字段数据.
    /// </summary>
    public List<AssignItem>? assignList { get; set; }

    /// <summary>
    /// 会签比例.
    /// </summary>
    public CounterSignRuleConfig counterSignConfig { get; set; } = new CounterSignRuleConfig();

    /// <summary>
    /// 审批类型（0：或签 1：会签 2：依次审批） .
    /// </summary>
    public int? counterSign { get; set; } = 0;

    /// <summary>
    /// 分流规则  parallel:所有分流都流转  inclusion:根据条件流转  exclusive:指定分支流转 choose:指定分支流转.
    /// </summary>
    public string divideRule { get; set; } = "parallel";

    /// <summary>
    /// 合流规则  parallel:所有分流都流转  inclusion:根据条件流转  exclusive:指定分支流转 choose:指定分支流转.
    /// </summary>
    public string confluenceRule { get; set; } = "parallel";

    /// <summary>
    /// 自动同意规则.
    /// </summary>
    public ConditionProperties autoAuditRule { get; set; }

    /// <summary>
    /// 自动拒绝规则.
    /// </summary>
    public ConditionProperties autoRejectRule { get; set; }

    /// <summary>
    /// 驳回类型(1:重新审批 2:从当前节点审批).
    /// </summary>
    public int? backType { get; set; }

    /// <summary>
    /// 驳回节点.
    /// </summary>
    public string? backNodeCode { get; set; }

    /// <summary>
    /// 节点参数.
    /// </summary>
    public List<object> parameterList { get; set; } = new List<object>();

    /// <summary>
    /// 环节节点编码.
    /// </summary>
    public string? approverNodeId { get; set; }

    /// <summary>
    /// 节点参数.
    /// </summary>
    public List<AuxiliaryItem> auxiliaryInfo { get; set; } = new List<AuxiliaryItem>();

    /// <summary>
    /// 外部请求参数模板配置json.
    /// </summary>
    public Dictionary<string, List<DataInterfaceParameter>> outsideOptions { get; set; }

    #region 子流程

    /// <summary>
    /// 异常处理规则
    /// 1:超级管理员处理、2:指定人员处理、6：流程发起人.
    /// </summary>
    public int errorRule { get; set; } = 1;

    /// <summary>
    /// 异常处理人.
    /// </summary>
    public List<string>? errorRuleUser { get; set; } = new List<string>();

    /// <summary>
    /// 子流程流程id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 自动提交.
    /// </summary>
    public bool autoSubmit { get; set; }

    /// <summary>
    /// 同步异步(异步:true).
    /// </summary>
    public bool isAsync { get; set; }

    /// <summary>
    /// 创建规则 0-同时 1-依次.
    /// </summary>
    public int createRule { get; set; }

    /// <summary>
    /// 检验发起权限.
    /// </summary>
    public bool hasPermission { get; set; }
    #endregion

    #region 人员

    /// <summary>
    /// 审批人类型（类型参考FlowTaskOperatorEnum类）.
    /// </summary>
    public int assigneeType { get; set; }

    /// <summary>
    /// 指定审批人.
    /// </summary>
    public List<string> approvers { get; set; } = new List<string>();

    /// <summary>
    /// 指定抄送人.
    /// </summary>
    public List<string> circulateUser { get; set; } = new List<string>();

    /// <summary>
    /// 依次审批人.
    /// </summary>
    public List<string> approversSortList { get; set; } = new List<string>();

    /// <summary>
    /// 接口服务.
    /// </summary>
    public FuncConfig interfaceConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 发起人主管级别.
    /// </summary>
    public int managerLevel { get; set; } = 1;

    /// <summary>
    /// 直属主管审批人类型  1：发起人  2：上节点审批人.
    /// </summary>
    public int approverType { get; set; } = 1;

    /// <summary>
    /// 表单字段.
    /// </summary>
    public string? formField { get; set; }

    /// <summary>
    /// 表单字段审核方式的类型(1-用户 2-部门).
    /// </summary>
    public int formFieldType { get; set; }

    /// <summary>
    /// 附加条件,默认无附加条件.
    /// 1:无附加条件、3:同一岗位、4:发起人上级、5:发起人下属、6:同一公司.
    /// </summary>
    public string extraRule { get; set; } = "1";

    /// <summary>
    /// 启用候选人.
    /// </summary>
    public bool isCandidates { get; set; }

    /// <summary>
    /// 逐级审批配置.
    /// </summary>
    public ApproversConfig approversConfig { get; set; }
    #endregion

    #region 抄送配置

    /// <summary>
    /// 抄送附加条件,默认无附加条件.
    /// 1:无附加条件、2:同一部门、3:同一岗位、4:发起人上级、5:发起人下属、6:同一公司.
    /// </summary>
    public string extraCopyRule { get; set; } = "1";

    /// <summary>
    /// 自定义抄送人.
    /// </summary>
    public bool isCustomCopy { get; set; }

    /// <summary>
    /// 是否抄送发起人.
    /// </summary>
    public bool isInitiatorCopy { get; set; }

    /// <summary>
    /// 是否抄送表单变量.
    /// </summary>
    public bool isFormFieldCopy { get; set; }

    /// <summary>
    /// 抄送表单字段.
    /// </summary>
    public string copyFormField { get; set; }

    /// <summary>
    /// 抄送表单字段类型.
    /// </summary>
    public string copyFormFieldType { get; set; }
    #endregion

    #region 超时

    /// <summary>
    /// 限时.
    /// </summary>
    public TimeOutConfig? timeLimitConfig { get; set; } = new TimeOutConfig();

    /// <summary>
    /// 超时.
    /// </summary>
    public TimeOutConfig? overTimeConfig { get; set; } = new TimeOutConfig();

    /// <summary>
    /// 提醒.
    /// </summary>
    public TimeOutConfig? noticeConfig { get; set; } = new TimeOutConfig();
    #endregion

    #region 容器数据
    public int handleStatus { get; set; } = -1;

    public string? groupId { get; set; }
    #endregion
}

