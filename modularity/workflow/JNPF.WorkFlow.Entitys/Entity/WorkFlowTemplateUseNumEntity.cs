using JNPF.Common.Const;
using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程模版使用次数.
/// </summary>
[SugarTable("WORKFLOW_TEMPLATE_USE_NUM")]
[Tenant(ClaimConst.TENANTID)]
public class WorkFlowTemplateUseNumEntity : CLDSEntityBase
{
    /// <summary>
    /// 模版id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_ID")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// 使用次数.
    /// </summary>
    [SugarColumn(ColumnName = "F_USE_NUM")]
    public int? UseNum { get; set; }

    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string? UserId { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
