using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 文档中心记录表.
/// </summary>
[SugarTable("EXT_DOCUMENT_LOG")]
public class DocumentLogEntity : OnlyCLDEntityBase
{
    /// <summary>
    /// 文档父级.
    /// </summary>
    [SugarColumn(ColumnName = "F_DOCUMENT_ID")]
    public string? DocumentId { get; set; }

    /// <summary>
    /// 文档分类:【0-文件夹、1-文件】.
    /// </summary>
    [SugarColumn(ColumnName = "F_CHILD_DOCUMENT")]
    public string? ChildDocument { get; set; }
}
