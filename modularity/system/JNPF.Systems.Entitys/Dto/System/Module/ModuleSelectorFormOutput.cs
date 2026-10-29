using JNPF.DependencyInjection;
using Newtonsoft.Json;

namespace JNPF.Systems.Entitys.Dto.Module;

/// <summary>
/// 获取菜单的表单列表输出.
/// </summary>
[SuppressSniffer]
public class ModuleSelectorFormOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 菜单名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 分类名称.
    /// </summary>
    public string typeName { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 系统名称.
    /// </summary>
    public string systemName { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 表单id.
    /// </summary>
    public string formId { get; set; }

    /// <summary>
    /// 表单id.
    /// </summary>
    [JsonIgnore]
    public string propertyJson { get; set; }
}