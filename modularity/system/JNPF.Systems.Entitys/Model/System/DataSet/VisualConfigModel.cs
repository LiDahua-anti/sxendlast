using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.DataSet;

/// <summary>
/// 配置式.
/// </summary>
[SuppressSniffer]
public class VisualConfigModel : FilterConfigModel
{
    /// <summary>
    /// 父表.
    /// </summary>
    public string parentTable { get; set; }

    /// <summary>
    /// 表名.
    /// </summary>
    public string table { get; set; }

    /// <summary>
    /// 带注释的表名.
    /// </summary>
    public string tableName { get; set; }

    /// <summary>
    /// 字段列表.
    /// </summary>
    public List<Field> fieldList { get; set; }

    /// <summary>
    /// 数据连接.
    /// </summary>
    public RelationConfig relationConfig { get; set; }

    /// <summary>
    /// 子级.
    /// </summary>
    public List<VisualConfigModel> children { get; set; }
}

/// <summary>
/// 字段列表.
/// </summary>
[SuppressSniffer]
public class Field
{
    /// <summary>
    /// 字段名.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 带注释的字段名.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 字段类型.
    /// </summary>
    public string dataType { get; set; }
}

/// <summary>
/// 数据连接.
/// </summary>
[SuppressSniffer]
public class RelationConfig : FilterConfigModel
{
    /// <summary>
    /// 关联关系.
    /// 1:左连接,2:右连接,3:内连接,4:全连接.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 关联字段.
    /// </summary>
    public List<Relation> relationList { get; set; }
}

/// <summary>
/// 关联字段.
/// </summary>
[SuppressSniffer]
public class Relation
{
    /// <summary>
    /// 关联主键.
    /// </summary>
    public string pField { get; set; }

    /// <summary>
    /// 外键.
    /// </summary>
    public string field { get; set; }
}
