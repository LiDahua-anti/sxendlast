using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// AI 会话列表.
/// </summary>
[SugarTable("BASE_AI_CHAT")]
public class AlChatEntity : CLDSEntityBase
{
    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }
}