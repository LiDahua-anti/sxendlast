using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 数据集.
/// </summary>
[SugarTable("BASE_DATA_SET")]
public class DataSetEntity : CLDEntityBase
{
    /// <summary>
    /// 关联数据类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_TYPE")]
    public string ObjectType { get; set; }

    /// <summary>
    /// 关联的数据id.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID")]
    public string ObjectId { get; set; }

    /// <summary>
    /// 数据集名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 连接id.
    /// </summary>
    [SugarColumn(ColumnName = "F_DB_LINK_ID")]
    public string DbLinkId { get; set; }

    /// <summary>
    /// 数据sql语句.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_CONFIG_JSON")]
    public string DataConfigJson { get; set; }

    /// <summary>
    /// 输入参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARAMETER_JSON")]
    public string ParameterJson { get; set; }

    /// <summary>
    /// 显示字段.
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD_JSON")]
    public string FieldJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 类型：1-sql语句，2-配置式，3-数据接口.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 配置式.
    /// </summary>
    [SugarColumn(ColumnName = "F_VISUAL_CONFIG_JSON")]
    public string VisualConfigJson { get; set; }

    /// <summary>
    /// 条件配置.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILTER_CONFIG_JSON")]
    public string FilterConfigJson { get; set; }

    /// <summary>
    /// 数据接口id.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTERFACE_ID")]
    public string InterfaceId { get; set; }
}