using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// AI 会话记录.
/// </summary>
[SugarTable("BASE_AI_HISTORY")]
public class AlHistoryEntity : CLDSEntityBase
{
    /// <summary>
    /// 会话id.
    /// </summary>
    [SugarColumn(ColumnName = "F_CHAT_ID")]
    public string ChatId { get; set; }

    /// <summary>
    /// 问题内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_QUESTION_TEXT")]
    public string questionText { get; set; }

    /// <summary>
    /// 会话内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTENT")]
    public string content { get; set; }

    /// <summary>
    /// 类型（0-ai，1-用户）.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int Type { get; set; }
}