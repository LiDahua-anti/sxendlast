namespace JNPF.WorkFlow.Entitys.Model.Properties;

public class TriggerProperties
{
    public string? type { get; set; }

    public string? nodeId { get; set; }

    public string? nodeName { get; set; }

    public string? groupId { get; set; }

    /// <summary>
    /// 触发条件.
    /// </summary>
    public List<object> ruleList { get; set; } = new List<object>();

    /// <summary>
    /// 条件规则匹配逻辑.
    /// </summary>
    public string ruleMatchLogic { get; set; } = "and";

    #region 触发事件
    /// <summary>
    /// 表单id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 1:异步  0:同步.
    /// </summary>
    public int isAsync { get; set; }

    /// <summary>
    /// 触发事件 1-表单事件 2-审批事件 3-空白事件.
    /// </summary>
    public int triggerEvent { get; set; }

    /// <summary>
    /// 触发表单事件 1-新增 2-修改 3-删除.
    /// </summary>
    public int triggerFormEvent { get; set; }

    /// <summary>
    /// 1-同意  2-拒绝  3-退回  4-办理 .
    /// </summary>
    public List<int> actionList { get; set; }

    /// <summary>
    /// 表单事件-修改数据-修改字段.
    /// </summary>
    public List<string> updateFieldList { get; set; }
    #endregion

    #region 事件触发

    #endregion

    #region 定时触发
    /// <summary>
    /// 触发开始时间.
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// cron表达式.
    /// </summary>
    public string cron { get; set; }

    /// <summary>
    /// 触发结束时间类型.
    /// </summary>
    public int endTimeType { get; set; }

    /// <summary>
    /// 触发次数.
    /// </summary>
    public int endLimit { get; set; }

    /// <summary>
    /// 触发指定时间.
    /// </summary>
    public DateTime? endTime { get; set; }
    #endregion

    #region 通知触发
    /// <summary>
    /// 消息模版id.
    /// </summary>
    public string noticeId { get; set; }
    #endregion

    #region webhook触发
    /// <summary>
    /// webhookUrl.
    /// </summary>
    public string webhookUrl { get; set; }

    /// <summary>
    /// webhook获取接口字段Url.
    /// </summary>
    public string webhookGetFieldsUrl { get; set; }

    /// <summary>
    /// webhook获取接口字段识别码.
    /// </summary>
    public string webhookRandomStr { get; set; }

    /// <summary>
    /// 表单/接口字段.
    /// </summary>
    public List<object> formFieldList { get; set; }
    #endregion
}
