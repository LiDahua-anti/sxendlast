using JNPF.DependencyInjection;

namespace JNPF.Common.Models.Authorize;

/// <summary>
/// 数据权限条件字段.
/// </summary>
[SuppressSniffer]
public class AuthorizeModuleResourceConditionModel
{
    /// <summary>
    /// 逻辑.
    /// </summary>
    public string matchLogic { get; set; }

    /// <summary>
    /// 分组.
    /// </summary>
    public List<AuthorizeModuleResourceConditionModelList> conditionList { get; set; }
}

public class AuthorizeModuleResourceConditionModelTopInput
{
    /// <summary>
    /// 逻辑.
    /// </summary>
    public string matchLogic { get; set; }

    /// <summary>
    /// 分组.
    /// </summary>
    public List<AuthorizeModuleResourceConditionModelInput> conditionList { get; set; }
}

public class AuthorizeModuleResourceConditionModelInput
{
    /// <summary>
    /// 逻辑.
    /// </summary>
    public string logic { get; set; }

    /// <summary>
    /// 分组.
    /// </summary>
    public List<AuthorizeModuleResourceConditionItemModelInput> groups { get; set; }
}

public class AuthorizeModuleResourceConditionModelList
{
    /// <summary>
    /// 逻辑.
    /// </summary>
    public string logic { get; set; }

    /// <summary>
    /// 分组.
    /// </summary>
    public List<AuthorizeModuleResourceConditionItemModel> groups { get; set; }
}