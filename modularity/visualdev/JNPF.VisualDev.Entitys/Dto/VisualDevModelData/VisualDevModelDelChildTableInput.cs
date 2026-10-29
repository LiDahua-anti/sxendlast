using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

/// <summary>
/// 在线开发删除子表数据输入.
/// </summary>
[SuppressSniffer]
public class VisualDevModelDelChildTableInput
{
    public string table { get; set; }

    public string queryConfig { get; set; }
}