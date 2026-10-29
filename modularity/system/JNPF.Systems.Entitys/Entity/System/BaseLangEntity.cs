using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 翻译管理.
/// </summary>
[SugarTable("BASE_LANGUAGE")]
public class BaseLangEntity : CLDSEntityBase
{
    /// <summary>
    /// 翻译标记id.
    /// </summary>
    [SugarColumn(ColumnName = "F_GROUP_ID")]
    public string GroupId { get; set; }

    /// <summary>
    /// 翻译标记.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 翻译内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 语种.
    /// </summary>
    [SugarColumn(ColumnName = "F_LANGUAGE")]
    public string Language { get; set; }

    /// <summary>
    /// 类型：0-客户端，1-服务端.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int Type { get; set; }
}