using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Model;

/// <summary>
/// 数据日志模型.
/// </summary>
[SuppressSniffer]
public class VisualLogModel
{
    /// <summary>
    /// jnpf识别符.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 旧数据.
    /// </summary>
    public string oldData { get; set; }

    /// <summary>
    /// 新数据.
    /// </summary>
    public string newData { get; set; }

    /// <summary>
    /// 类型（0：新建，1：修改）.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 显示已修改.
    /// </summary>
    public bool nameModified { get; set; }

    /// <summary>
    /// 子表数据.
    /// </summary>
    public List<Dictionary<string, object>>? chidData { get; set; }

    /// <summary>
    /// 子表字段.
    /// </summary>
    public List<ChildFieldModel>? chidField { get; set; }
}

public class ChildFieldModel
{
    /// <summary>
    /// jnpf识别符.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string label { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string prop { get; set; }

    /// <summary>
    /// 显示已修改.
    /// </summary>
    public bool nameModified { get; set; }
}
