using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.BaseLang;

/// <summary>
/// 翻译管理导入数据输入.
/// </summary>
[SuppressSniffer]
public class BaseLangImportDataInput
{
    /// <summary>
    /// 文件名称.
    /// </summary>
    public string fileName { get; set; }
}