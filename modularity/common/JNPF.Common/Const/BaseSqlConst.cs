using JNPF.DependencyInjection;

namespace JNPF.Common.Const;

/// <summary>
/// 基础sql语句常量.
/// </summary>
[SuppressSniffer]
public class BaseSqlConst
{
    /// <summary>
    /// 查询语句.
    /// </summary>
    public const string QUERY = "SELECT {0} FROM {1} ";

    /// <summary>
    /// 查询语句带别名.
    /// </summary>
    public const string QUERY_ALIAS = "(SELECT {0} FROM {1}) {2} ";

    /// <summary>
    /// 左连接.
    /// </summary>
    public const string LEFT_CONNECTION = "LEFT JOIN {0} ON {1} ";

    /// <summary>
    /// 右连接.
    /// </summary>
    public const string RIGHT_CONNECTION = "RIGHT JOIN {0} ON {1} ";

    /// <summary>
    /// 内连接.
    /// </summary>
    public const string INNER_CONNECTION = "INNER JOIN {0} ON {1} ";

    /// <summary>
    /// 全连接.
    /// </summary>
    public const string FULL_CONNECTION = "FULL JOIN {0} ON {1} ";
}