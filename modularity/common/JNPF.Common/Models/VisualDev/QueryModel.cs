using JNPF.DependencyInjection;

namespace JNPF.Common.Models.VisualDev;

/// <summary>
/// 查询条件.
/// </summary>
[SuppressSniffer]
public class QueryModel
{
    /// <summary>
    /// 分组逻辑： 并且、或者.
    /// </summary>
    public string matchLogic { get; set; }

    /// <summary>
    /// 分组条件.
    /// </summary>
    public List<QueryConditionListModel> conditionList { get; set; }
}

/// <summary>
/// 查询条件集合.
/// </summary>
[SuppressSniffer]
public class QueryConditionListModel
{
    /// <summary>
    /// 条件.
    /// </summary>
    public string logic { get; set; }

    /// <summary>
    /// 分组.
    /// </summary>
    public List<QueryConditionGroupsListModel> groups { get; set; }
}

/// <summary>
/// 查询条件分组集合.
/// </summary>
[SuppressSniffer]
public class QueryConditionGroupsListModel
{
    /// <summary>
    /// 设置默认值为空字符串.
    /// </summary>
    public string __vModel__ { get; set; } = string.Empty;

    /// <summary>
    /// 控件KEY.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 字段过滤值.
    /// </summary>
    public object fieldValue { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 条件符号标识.
    /// </summary>
    public string symbol { get; set; }

    /// <summary>
    /// 显示绑定值的格式.
    /// </summary>
    public string format { get; set; }
}
