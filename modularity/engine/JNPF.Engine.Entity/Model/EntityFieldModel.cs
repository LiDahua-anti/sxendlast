using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 实体字段模型
/// 版 本：V3.0.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组.
/// </summary>
[SuppressSniffer]
public class EntityFieldModel
{
    /// <summary>
    /// 字段名称.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 字段说明.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 数据长度.
    /// </summary>
    public string dataLength { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public int? primaryKey { get; set; }

    /// <summary>
    /// 可空.
    /// </summary>
    public int? allowNull { get; set; }

    /// <summary>
    /// 自增.
    /// </summary>
    public int? identity { get; set; }
}