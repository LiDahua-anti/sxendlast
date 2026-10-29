using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Position;

/// <summary>
/// 数据导出 输入.
/// </summary>
[SuppressSniffer]
public class PositionExportDataInput : CommonInput
{
    /// <summary>
    /// 机构ID.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 导出类型 (0：当前页，1：全部).
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 选择的导出 字段集合 按 , 号隔开.
    /// </summary>
    public string selectKey { get; set; }
}