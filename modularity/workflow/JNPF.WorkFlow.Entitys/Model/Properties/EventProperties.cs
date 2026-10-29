using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Dtos.Message;
using JNPF.WorkFlow.Entitys.Enum;
using JNPF.WorkFlow.Entitys.Model.Item;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

public class EventProperties
{
    /// <summary>
    /// 节点类型.
    /// </summary>
    public WorkFlowNodeTypeEnum type { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string? nodeId { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    public string? nodeName { get; set; }

    public string? groupId { get; set; }

    #region 获取数据
    /// <summary>
    /// 表单类型 1-从表单中获取 2-从流程中获取 3-从数据接口中获取 4-从子表.
    /// </summary>
    public int formType { get; set; }

    /// <summary>
    /// 触发表单/接口id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 获取条件规则.
    /// </summary>
    public List<GropsItem> ruleList { get; set; }

    /// <summary>
    /// 获取条件规则匹配逻辑.
    /// </summary>
    public string ruleMatchLogic { get; set; } = "and";

    /// <summary>
    /// 接口参数.
    /// </summary>
    public List<DataInterfaceParameter> interfaceTemplateJson { get; set; }

    /// <summary>
    /// 表单字段.
    /// </summary>
    public List<DataInterfaceParameter> formFieldList { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public List<SortItem> sortList { get; set; }
    #endregion

    #region 新增数据
    /// <summary>
    /// 字段设置.
    /// </summary>
    public List<TransferItem> transferList { get; set; }

    /// <summary>
    /// 数据源.
    /// </summary>
    public string dataSourceForm { get; set; }
    #endregion

    #region 更新数据
    /// <summary>
    /// 未找到数据时更新.
    /// </summary>
    public bool unFoundRule { get; set; }
    #endregion

    #region 删除数据
    /// <summary>
    /// 删除类型.
    /// </summary>
    public int deleteType { get; set; }

    /// <summary>
    /// 表类型  0-主表  1-子表.
    /// </summary>
    public int tableType { get; set; }

    /// <summary>
    /// 子表.
    /// </summary>
    public string subTable { get; set; }

    /// <summary>
    /// 删除条件  1-存在  2-不存在.
    /// </summary>
    public int deleteCondition { get; set; }
    #endregion

    #region 数据接口
    public List<DataInterfaceParameter> templateJson { get; set; }
    #endregion

    #region 消息通知
    /// <summary>
    /// 通知人.
    /// </summary>
    public object msgUserIds { get; set; }

    /// <summary>
    /// 消息模版id.
    /// </summary>
    public string msgId { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public List<MessageSendModel> msgTemplateJson { get; set; }

    /// <summary>
    /// 通知人类型.
    /// </summary>
    public int msgUserIdsSourceType { get; set; }
    #endregion

    #region 发起审批
    /// <summary>
    /// 流程模版id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 发起人.
    /// </summary>
    public List<string> initiator { get; set; }

    /// <summary>
    /// 检验发起权限.
    /// </summary>
    public bool hasPermission { get; set; }

    /// <summary>
    /// 变量字段.
    /// </summary>
    public string formField { get; set; }

    /// <summary>
    /// 变量类型.
    /// </summary>
    public int formFieldType { get; set; }

    /// <summary>
    /// 6-指定 4-变量.
    /// </summary>
    public int assigneeType { get; set; }
    #endregion

    #region 创建日程

    /// <summary>
    /// 日程标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 日程内容.
    /// </summary>
    public string contents { get; set; }

    /// <summary>
    /// 日程附件.
    /// </summary>
    public List<object> files { get; set; }

    /// <summary>
    /// 日程全天.
    /// </summary>
    public int allDay { get; set; }

    /// <summary>
    /// 日程开始日期.
    /// </summary>
    public object startDay { get; set; }

    /// <summary>
    /// 日程开始时间.
    /// </summary>
    public string startTime { get; set; }

    /// <summary>
    /// 日程时长.
    /// </summary>
    public int duration { get; set; }

    /// <summary>
    /// 日程结束日期.
    /// </summary>
    public object endDay { get; set; }

    /// <summary>
    /// 日程结束时间.
    /// </summary>
    public string endTime { get; set; }

    /// <summary>
    /// 日程创建人.
    /// </summary>
    public string creatorUserId { get; set; }

    /// <summary>
    /// 日程参与人.
    /// </summary>
    public object toUserIds { get; set; }

    /// <summary>
    /// 日程标签颜色.
    /// </summary>
    public string color { get; set; } = "#188ae2";

    /// <summary>
    /// 日程提醒时间.
    /// </summary>
    public int reminderTime { get; set; }

    /// <summary>
    /// 日程提醒方式.
    /// </summary>
    public int reminderType { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string send { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string sendName { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public int repetition { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public object repeatTime { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public int startDaySourceType { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public int endDaySourceType { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public int titleSourceType { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public int contentsSourceType { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public int creatorUserIdSourceType { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public int toUserIdsSourceType { get; set; }
    #endregion
}
