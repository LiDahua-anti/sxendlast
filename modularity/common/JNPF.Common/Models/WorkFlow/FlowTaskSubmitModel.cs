namespace JNPF.Common.Models.WorkFlow
{
    /// <summary>
    /// 工作流提交模型.
    /// </summary>
    public class FlowTaskSubmitModel : FlowTaskOtherModel
    {
        /// <summary>
        /// 任务主键id(表单的f_flow_task_id与之关联).
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 流程id.
        /// </summary>
        public string flowId { get; set; }

        /// <summary>
        /// 任务标题.
        /// </summary>
        public string flowTitle { get; set; }

        /// <summary>
        /// 紧急程度.
        /// </summary>
        public int? flowUrgent { get; set; }

        /// <summary>
        /// 表单数据.
        /// </summary>
        public object formData { get; set; }

        /// <summary>
        /// 状态 0:保存，1提交..
        /// </summary>
        public int status { get; set; }

        /// <summary>
        /// 审批修改权限1：可写，0：可读..
        /// </summary>
        public int approvaUpType { get; set; } = 0;

        /// <summary>
        /// 是否流程（0-菜单 1-发起，由协同办公发起的流程任务暂存会产生任务数据，其余页面的则不产生）.
        /// </summary>
        public int isFlow { get; set; } = 0;

        /// <summary>
        /// 流程父流程id(0:顶级流程，其他：子流程) 工作流使用.
        /// </summary>
        public string parentId { get; set; } = "0";

        /// <summary>
        /// 流程发起人 工作流使用.
        /// </summary>
        public string crUser { get; set; } = null;

        /// <summary>
        /// 是否异步流程 工作流使用.
        /// </summary>
        public bool isAsync { get; set; } = false;

        /// <summary>
        /// 是否委托发起流程 工作流使用.
        /// </summary>
        public bool isDelegate { get; set; } = false;

        /// <summary>
        /// 子流程参数 工作流使用.
        /// </summary>
        public string subParameter { get; set; }

        /// <summary>
        /// 子流程节点编码 工作流使用.
        /// </summary>
        public string subCode { get; set; }

        /// <summary>
        /// 检验发起权限.
        /// </summary>
        public bool hasPermission { get; set; } = true;

    }

    /// <summary>
    /// 流程任务其他参数.
    /// </summary>
    public class FlowTaskOtherModel
    {
        /// <summary>
        /// 候选人.
        /// </summary>
        public Dictionary<string, List<string>> candidateList { get; set; }

        /// <summary>
        /// 异常审批人.
        /// </summary>
        public Dictionary<string, List<string>> errorRuleUserList { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// 自定义抄送人.
        /// </summary>
        public string? copyIds { get; set; }

        /// <summary>
        /// 委托发起人.
        /// </summary>
        public string delegateUser { get; set; }

        /// <summary>
        /// 子流程任务实例.
        /// </summary>
        public string childTask { get; set; }

        /// <summary>
        /// 是否自动提交.
        /// </summary>
        public bool autoSubmit { get; set; }

        /// <summary>
        /// 选择分支.
        /// </summary>
        public List<string> branchList { get; set; } = new List<string>();
    }
}
